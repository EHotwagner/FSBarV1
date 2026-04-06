namespace FSBar.Client

open System
open System.IO
open System.Net.Sockets
open System.Diagnostics
open Highbar

type SessionState =
    | Idle
    | Starting
    | Connected
    | Running
    | Stopped
    | Error of string

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

    member _.State = state
    member _.Config = config
    member _.Handshake = handshakeInfo
    member _.Stream = requireStream()

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

    member this.Run(frameCount: int, handler: GameFrame -> AICommand list) : GameFrame list =
        let frames = ResizeArray<GameFrame>()
        for _ in 1..frameCount do
            let frame = this.StepWith(handler)
            frames.Add(frame)
        frames |> Seq.toList

    member this.RunUntil(predicate: GameFrame -> bool, handler: GameFrame -> AICommand list) : GameFrame list =
        let frames = ResizeArray<GameFrame>()
        let mutable stop = false
        while not stop do
            let frame = this.StepWith(handler)
            frames.Add(frame)
            if predicate frame then
                stop <- true
        frames |> Seq.toList

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
