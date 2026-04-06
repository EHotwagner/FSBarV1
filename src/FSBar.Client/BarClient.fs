namespace FSBar.Client

open System
open System.IO
open System.Net.Sockets
open System.Diagnostics
open Highbar

/// <summary>
/// Represents the lifecycle state of a <see cref="T:FSBar.Client.BarClient"/> session.
/// </summary>
type SessionState =
    /// <summary>No session is active. The client has not been started or has been fully stopped.</summary>
    | Idle
    /// <summary>The client is in the process of launching the engine and establishing a connection.</summary>
    | Starting
    /// <summary>The client is connected to the HighBar V2 proxy and ready to step through frames.</summary>
    | Connected
    /// <summary>The client is actively processing a game frame.</summary>
    | Running
    /// <summary>The session has ended and all resources have been cleaned up.</summary>
    | Stopped
    /// <summary>The session encountered an error. Contains the error message.</summary>
    | Error of string

/// <summary>
/// High-level client for orchestrating a BAR AI game session.
/// Manages the full lifecycle: engine launch, proxy connection, handshake, frame stepping, and cleanup.
/// Implements <see cref="T:System.IDisposable"/> for deterministic resource cleanup.
/// </summary>
/// <param name="config">The engine configuration for this session.</param>
type BarClient(config: EngineConfig) =
    let mutable state = Idle
    let mutable listener: Socket option = None
    let mutable clientSocket: Socket option = None
    let mutable stream: NetworkStream option = None
    let mutable engineProcess: Process option = None
    let mutable handshakeInfo: HandshakeInfo option = None
    let mutable sessionDir: string = ""

    let requireStream () =
        match stream with
        | Some s -> s
        | None -> failwith "Not connected to proxy"

    let requireConnected () =
        match state with
        | Connected | Running -> ()
        | _ -> failwith $"No active session (state: {state})"

    /// <summary>Gets the current lifecycle state of this session.</summary>
    member _.State = state

    /// <summary>Gets the engine configuration used to create this client.</summary>
    member _.Config = config

    /// <summary>Gets the handshake information received from the proxy, or <c>None</c> if not yet connected.</summary>
    member _.Handshake = handshakeInfo

    /// <summary>
    /// Gets the active network stream to the HighBar V2 proxy.
    /// </summary>
    /// <exception cref="T:System.Exception">Thrown if not currently connected to the proxy.</exception>
    member _.Stream = requireStream()

    /// <summary>
    /// Launches the BAR engine, listens for the HighBar V2 proxy connection on the configured socket path,
    /// performs the protocol handshake, and transitions to the <see cref="F:FSBar.Client.SessionState.Connected"/> state.
    /// </summary>
    /// <remarks>
    /// Can only be called from <see cref="F:FSBar.Client.SessionState.Idle"/> or <see cref="F:FSBar.Client.SessionState.Stopped"/> states.
    /// On failure, transitions to <see cref="F:FSBar.Client.SessionState.Error"/> and cleans up all resources before re-raising the exception.
    /// </remarks>
    /// <exception cref="T:System.Exception">Thrown if the client is in an invalid state to start, or if connection/handshake fails.</exception>
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

            state <- Connected
        with ex ->
            state <- Error ex.Message
            this.CleanupResources()
            reraise ()

    /// <summary>
    /// Receives the next game frame from the engine and sends an empty command response (no-op).
    /// Use this for passive observation without issuing any AI commands.
    /// </summary>
    /// <returns>The received <see cref="T:FSBar.Client.GameFrame"/> containing frame number and events.</returns>
    /// <exception cref="T:System.Exception">Thrown if not connected, or if the game has ended (shutdown received).</exception>
    member _.Step() : GameFrame =
        requireConnected ()
        state <- Running
        match Protocol.receiveFrame (requireStream()) with
        | Some frame ->
            Protocol.sendFrameResponse (requireStream()) []
            state <- Connected
            frame
        | None ->
            state <- Stopped
            failwith "Game ended (shutdown received)"

    /// <summary>
    /// Receives the next game frame from the engine, passes it to the handler function, and sends
    /// the returned AI commands back to the engine.
    /// </summary>
    /// <param name="handler">A function that receives a <see cref="T:FSBar.Client.GameFrame"/> and returns a list of AI commands to execute.</param>
    /// <returns>The received <see cref="T:FSBar.Client.GameFrame"/>.</returns>
    /// <exception cref="T:System.Exception">Thrown if not connected, or if the game has ended (shutdown received).</exception>
    member _.StepWith(handler: GameFrame -> AICommand list) : GameFrame =
        requireConnected ()
        state <- Running
        match Protocol.receiveFrame (requireStream()) with
        | Some frame ->
            let commands = handler frame
            Protocol.sendFrameResponse (requireStream()) commands
            state <- Connected
            frame
        | None ->
            state <- Stopped
            failwith "Game ended (shutdown received)"

    /// <summary>
    /// Runs the game for a fixed number of frames, calling the handler for each frame to produce AI commands.
    /// </summary>
    /// <param name="frameCount">The number of frames to process.</param>
    /// <param name="handler">A function that receives a <see cref="T:FSBar.Client.GameFrame"/> and returns a list of AI commands.</param>
    /// <returns>A list of all received <see cref="T:FSBar.Client.GameFrame"/> values.</returns>
    member this.Run(frameCount: int, handler: GameFrame -> AICommand list) : GameFrame list =
        let frames = ResizeArray<GameFrame>()
        for _ in 1..frameCount do
            let frame = this.StepWith(handler)
            frames.Add(frame)
        frames |> Seq.toList

    /// <summary>
    /// Runs the game until the predicate returns <c>true</c> for a received frame, calling the handler
    /// for each frame to produce AI commands.
    /// </summary>
    /// <param name="predicate">A function that returns <c>true</c> when the loop should stop.</param>
    /// <param name="handler">A function that receives a <see cref="T:FSBar.Client.GameFrame"/> and returns a list of AI commands.</param>
    /// <returns>A list of all received <see cref="T:FSBar.Client.GameFrame"/> values, including the final one that triggered the stop.</returns>
    member this.RunUntil(predicate: GameFrame -> bool, handler: GameFrame -> AICommand list) : GameFrame list =
        let frames = ResizeArray<GameFrame>()
        let mutable stop = false
        while not stop do
            let frame = this.StepWith(handler)
            frames.Add(frame)
            if predicate frame then
                stop <- true
        frames |> Seq.toList

    /// <summary>
    /// Resets the in-game state by sending cheat commands to drain and restore resources.
    /// Runs several verification frames afterward to let the engine settle.
    /// </summary>
    /// <remarks>
    /// Requires an active connected session. Uses the engine's cheat mode to manipulate resources.
    /// Useful between test scenarios to start from a clean economic state.
    /// </remarks>
    member this.Reset() =
        requireConnected ()
        // Run a few frames to destroy units and reset resources
        let mutable sent = false
        this.StepWith(fun _ ->
            if not sent then
                sent <- true
                [
                    Commands.SendTextMessageCommand ".cheat" 0
                    Commands.GiveMeResourceCommand 0 -1000000.0f
                    Commands.GiveMeResourceCommand 1 -1000000.0f
                    Commands.GiveMeResourceCommand 0 1000.0f
                    Commands.GiveMeResourceCommand 1 1000.0f
                ]
            else []
        ) |> ignore
        // Run verification frames
        for _ in 1..10 do
            this.Step() |> ignore
        printfn "Game state reset."

    /// <summary>
    /// Stops the session and cleans up all resources (sockets, streams, engine process).
    /// Safe to call from any state. No-op if already stopped or idle.
    /// </summary>
    member this.Stop() =
        match state with
        | Stopped | Idle -> ()
        | Error _ -> this.CleanupResources()
        | _ ->
            this.CleanupResources()
            state <- Stopped

    member private _.CleanupResources() =
        stream |> Option.iter (fun s -> try s.Dispose() with _ -> ())
        stream <- None

        clientSocket |> Option.iter (fun s -> try s.Close(); s.Dispose() with _ -> ())
        clientSocket <- None

        listener |> Option.iter (fun s -> try s.Close(); s.Dispose() with _ -> ())
        listener <- None

        engineProcess |> Option.iter (fun proc ->
            EngineLauncher.stopEngine config.SocketPath proc
        )
        engineProcess <- None

        Connection.cleanup config.SocketPath None

    interface IDisposable with
        member this.Dispose() = this.Stop()

/// <summary>Convenience module functions for creating and starting <see cref="T:FSBar.Client.BarClient"/> instances.</summary>
module BarClient =
    /// <summary>Creates a default <see cref="T:FSBar.Client.EngineConfig"/> for headless testing.</summary>
    /// <returns>A new default engine configuration.</returns>
    let defaultConfig () = EngineConfig.defaultConfig ()

    /// <summary>
    /// Creates a new <see cref="T:FSBar.Client.BarClient"/> with the given configuration without starting it.
    /// Call <see cref="M:FSBar.Client.BarClient.Start"/> to launch the engine and connect.
    /// </summary>
    /// <param name="config">The engine configuration for the new client.</param>
    /// <returns>A new <see cref="T:FSBar.Client.BarClient"/> in the <see cref="F:FSBar.Client.SessionState.Idle"/> state.</returns>
    let create (config: EngineConfig) =
        let client = new BarClient(config)
        client

    /// <summary>
    /// Creates and starts a headless <see cref="T:FSBar.Client.BarClient"/> with default configuration.
    /// The engine runs without a graphical window.
    /// </summary>
    /// <returns>A started <see cref="T:FSBar.Client.BarClient"/> in the <see cref="F:FSBar.Client.SessionState.Connected"/> state.</returns>
    /// <exception cref="T:System.Exception">Thrown if the engine fails to launch or the proxy connection times out.</exception>
    let startHeadless () =
        let config = defaultConfig ()
        let client = new BarClient(config)
        client.Start()
        client

    /// <summary>
    /// Creates and starts a graphical <see cref="T:FSBar.Client.BarClient"/> with default configuration.
    /// The engine runs with a full graphical window, useful for visual debugging.
    /// </summary>
    /// <returns>A started <see cref="T:FSBar.Client.BarClient"/> in the <see cref="F:FSBar.Client.SessionState.Connected"/> state.</returns>
    /// <exception cref="T:System.Exception">Thrown if the engine fails to launch or the proxy connection times out.</exception>
    let startGraphical () =
        let config = { defaultConfig () with Mode = Graphical }
        let client = new BarClient(config)
        client.Start()
        client
