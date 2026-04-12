namespace FSBar.Client

open System
open System.IO
open System.Net.Sockets
open System.Diagnostics
open System.Threading
open Highbar

/// Represents the lifecycle state of a BarClient session.
type SessionState =
    | Idle
    | Starting
    | Connected
    | Running
    | Stopped
    | Error of string

/// High-level client for orchestrating a BAR AI game session.
/// Manages the full lifecycle: engine launch, proxy connection, handshake, frame stepping, and cleanup.
type BarClient(config: EngineConfig) =
    let mutable state = Idle
    let mutable listener: Socket option = None
    let mutable clientSocket: Socket option = None
    let mutable stream: NetworkStream option = None
    let mutable engineProcess: Process option = None
    let mutable handshakeInfo: HandshakeInfo option = None
    let mutable sessionDir: string = ""
    let mutable pendingCommands: AICommand list = []
    let mutable gameState: GameState = GameState.empty

    // Observable infrastructure
    let subscribersLock = obj ()
    let mutable subscribers: IObserver<GameFrame> list = []
    let mutable frameThread: Thread option = None
    // True once the first frame has been received and we owe the engine
    // a frame response before the next receive. Shared between the sync
    // WaitFrames loop and the async frame thread so they stay consistent.
    let mutable firstFrameSent = false

    let requireStream () =
        match stream with
        | Some s -> s
        | None -> failwith "Not connected to proxy"

    let requireConnected () =
        match state with
        | Connected | Running -> ()
        | _ -> failwith $"No active session (state: {state})"

    let notifyNext (frame: GameFrame) =
        let subs = lock subscribersLock (fun () -> subscribers)
        for obs in subs do
            try obs.OnNext(frame) with _ -> ()

    let notifyCompleted () =
        let subs = lock subscribersLock (fun () -> subscribers)
        for obs in subs do
            try obs.OnCompleted() with _ -> ()

    let notifyError (ex: exn) =
        let subs = lock subscribersLock (fun () -> subscribers)
        for obs in subs do
            try obs.OnError(ex) with _ -> ()

    let startFrameThread () =
        let thread =
            Thread(fun () ->
                let s = requireStream ()
                let mutable running = true
                state <- Running
                while running do
                    try
                        if firstFrameSent then
                            Protocol.sendFrameResponse s pendingCommands
                            pendingCommands <- []
                        else
                            firstFrameSent <- true
                        match Protocol.receiveFrame s with
                        | Some frame ->
                            // Update game state
                            gameState <- GameState.processFrame gameState frame s
                            // Notify subscribers
                            notifyNext frame
                        | None ->
                            state <- Stopped
                            running <- false
                            notifyCompleted ()
                    with
                    | :? EngineDisconnectedException ->
                        state <- Stopped
                        running <- false
                        notifyCompleted ()
                    | ex ->
                        state <- Error ex.Message
                        running <- false
                        notifyError ex)
        thread.IsBackground <- true
        thread.Name <- "BarClient-FrameLoop"
        frameThread <- Some thread
        thread.Start()

    /// Gets the current lifecycle state of this session.
    member _.State = state

    /// Gets the engine configuration used to create this client.
    member _.Config = config

    /// Gets the handshake information received from the proxy, or None if not yet connected.
    member _.Handshake = handshakeInfo

    /// Gets the active network stream to the HighBar V2 proxy.
    member _.Stream = requireStream()

    /// Game frames as a push-based observable. Subscribers receive frames as they arrive from the engine.
    /// The observable completes when the engine disconnects or sends a shutdown message.
    member _.Frames : IObservable<GameFrame> =
        { new IObservable<GameFrame> with
            member _.Subscribe(observer: IObserver<GameFrame>) =
                lock subscribersLock (fun () ->
                    subscribers <- observer :: subscribers)
                // Start the frame thread on first subscription if connected
                if state = Connected && frameThread.IsNone then
                    startFrameThread ()
                { new IDisposable with
                    member _.Dispose() =
                        lock subscribersLock (fun () ->
                            subscribers <- subscribers |> List.filter (fun o -> not (obj.ReferenceEquals(o, observer)))) } }

    /// Queue commands to send with the next frame response.
    member _.SendCommands(commands: AICommand list) =
        match state with
        | Connected | Running ->
            pendingCommands <- commands
        | s ->
            raise (InvalidOperationException($"Cannot send commands: session is {s}"))

    /// Gets the current game state snapshot.
    member _.GameState = gameState

    /// Blocks and collects up to N frames, calling the handler for each.
    /// Reads frames synchronously on the calling thread — does NOT spawn a
    /// background frame thread. Safe to follow with synchronous callback
    /// queries (e.g. Callbacks.getMyTeam) because no other thread is reading
    /// the stream once this method returns. Mixing WaitFrames with
    /// `c.Frames.Subscribe` concurrently is unsupported.
    member this.WaitFrames (count: int) (handler: GameFrame -> unit) =
        if count <= 0 then ()
        else
        requireConnected ()
        let s = requireStream ()
        let mutable remaining = count
        let mutable stopped = false
        while remaining > 0 && not stopped do
            try
                if firstFrameSent then
                    Protocol.sendFrameResponse s pendingCommands
                    pendingCommands <- []
                else
                    firstFrameSent <- true
                match Protocol.receiveFrame s with
                | Some frame ->
                    gameState <- GameState.processFrame gameState frame s
                    notifyNext frame
                    try handler frame with _ -> ()
                    remaining <- remaining - 1
                | None ->
                    state <- Stopped
                    notifyCompleted ()
                    stopped <- true
            with
            | :? EngineDisconnectedException ->
                state <- Stopped
                notifyCompleted ()
                stopped <- true

    /// Launches the BAR engine, listens for the HighBar V2 proxy connection,
    /// performs the protocol handshake, and transitions to the Connected state.
    member this.Start() =
        if state <> Idle && state <> Stopped then
            failwith $"Cannot start from state {state}"

        state <- Starting
        try
            // Create listening socket
            let sock = Connection.createListener config.SocketPath
            listener <- Some sock
            printfn "Listening on %s..." config.SocketPath

            // Generate game setup script
            let scriptContent = ScriptGenerator.generate config

            // Launch engine
            sessionDir <- EngineLauncher.getSessionDir config
            let proc =
                match config.Mode with
                | Headless -> EngineLauncher.launchHeadless config scriptContent
                | Graphical -> EngineLauncher.launchGraphical config scriptContent
            engineProcess <- Some proc

            // Accept proxy connection
            let readTimeout = EngineConfig.resolveReadTimeout config
            let (accepted, netStream) = Connection.acceptConnection sock config.TimeoutMs readTimeout
            clientSocket <- Some accepted
            stream <- Some netStream

            // Close listener — only need one connection
            sock.Close()
            listener <- None

            // Handshake
            let hs = Protocol.handshake netStream
            handshakeInfo <- Some hs
            printfn "Proxy connected. Handshake OK (protocol v%d, team %d)" hs.ProtocolVersion hs.TeamId

            // Mini warmup: read ~60 frames BEFORE loading unit defs so we
            // capture early spawn events (e.g. commander UnitCreated at
            // ~frame 30) while the frame-reading path is still fast and
            // hasn't been polluted by the 2500-callback UnitDefCache batch
            // (which drops interleaved frames).
            printfn "Warming up (reading early frames)..."
            let mutable warmFrames = 0
            let mutable warmStopped = false
            while warmFrames < 60 && not warmStopped do
                try
                    if firstFrameSent then
                        Protocol.sendFrameResponse netStream []
                    else
                        firstFrameSent <- true
                    match Protocol.receiveFrame netStream with
                    | Some frame ->
                        gameState <- GameState.processFrame gameState frame netStream
                        warmFrames <- warmFrames + 1
                    | None ->
                        warmStopped <- true
                with
                | :? EngineDisconnectedException ->
                    warmStopped <- true
            printfn "Warmup read %d frames (game frame %d, units tracked: %d)" warmFrames gameState.FrameNumber gameState.Units.Count

            // Load unit definition cache. Interleaved frames during this
            // batch will drop events, but the critical early spawn events
            // have already been captured by the mini warmup above.
            printfn "Loading unit definitions..."
            let unitDefs = UnitDefCache.loadFromEngine netStream
            let defCount = UnitDefCache.all unitDefs |> Seq.length
            printfn "Loaded %d unit definitions." defCount
            // Preserve any units captured during mini warmup.
            gameState <- { gameState with UnitDefs = unitDefs }

            state <- Connected
        with ex ->
            state <- Error ex.Message
            this.CleanupResources()
            reraise ()

    /// Resets the in-game state by sending cheat commands to drain and restore resources.
    member _.Reset() =
        requireConnected ()
        let s = requireStream ()
        match Protocol.receiveFrame s with
        | Some _ ->
            Protocol.sendFrameResponse s [
                Commands.SendTextMessageCommand ".cheat" 0
                Commands.GiveMeResourceCommand 0 -1000000.0f
                Commands.GiveMeResourceCommand 1 -1000000.0f
                Commands.GiveMeResourceCommand 0 1000.0f
                Commands.GiveMeResourceCommand 1 1000.0f
            ]
        | None ->
            state <- Stopped
            failwith "Game ended (shutdown received)"
        for _ in 1..10 do
            match Protocol.receiveFrame s with
            | Some _ -> Protocol.sendFrameResponse s []
            | None ->
                state <- Stopped
                failwith "Game ended during reset"
        printfn "Game state reset."

    /// Stops the session and cleans up all resources. Safe to call from any state.
    member this.Stop() =
        match state with
        | Stopped | Idle -> ()
        | Error _ -> this.CleanupResources()
        | _ ->
            this.CleanupResources()
            state <- Stopped

    member private _.CleanupResources() =
        // Signal frame thread to stop
        match frameThread with
        | Some t when t.IsAlive ->
            // Stream disposal will cause the frame thread to exit
            ()
        | _ -> ()

        stream |> Option.iter (fun s -> try s.Dispose() with _ -> ())
        stream <- None

        // Wait for frame thread to finish
        match frameThread with
        | Some t when t.IsAlive ->
            if not (t.Join(TimeSpan.FromSeconds(5.0))) then
                printfn "Warning: frame thread did not exit within 5s"
        | _ -> ()
        frameThread <- None

        clientSocket |> Option.iter (fun s -> try s.Close(); s.Dispose() with _ -> ())
        clientSocket <- None

        listener |> Option.iter (fun s -> try s.Close(); s.Dispose() with _ -> ())
        listener <- None

        engineProcess |> Option.iter (fun proc ->
            EngineLauncher.stopEngine config.SocketPath proc
        )
        engineProcess <- None

        Connection.cleanup config.SocketPath None

        // Notify subscribers of completion
        notifyCompleted ()
        lock subscribersLock (fun () -> subscribers <- [])

    interface IDisposable with
        member this.Dispose() = this.Stop()

/// Convenience module functions for creating and starting BarClient instances.
module BarClient =
    let defaultConfig () = EngineConfig.defaultConfig ()

    let create (config: EngineConfig) =
        let client = new BarClient(config)
        client

    let startHeadless () =
        let config = defaultConfig ()
        let client = new BarClient(config)
        client.Start()
        client

    let startGraphical () =
        let config = { defaultConfig () with Mode = Graphical }
        let client = new BarClient(config)
        client.Start()
        client
