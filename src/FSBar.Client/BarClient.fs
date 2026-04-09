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
    let mutable pendingCommands: AICommand list = []
    let mutable firstFrame: bool = true

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
    /// Game frames as a lazy sequence. Iterating pulls frames from the engine one at a time.
    /// Between iterations, any commands queued via SendCommands are delivered to the engine.
    /// The sequence terminates when the engine disconnects or sends a shutdown message.
    /// </summary>
    member _.Frames : seq<GameFrame> =
        seq {
            requireConnected ()
            state <- Running
            firstFrame <- true
            let s = requireStream ()
            let mutable running = true
            while running do
                if not firstFrame then
                    Protocol.sendFrameResponse s pendingCommands
                    pendingCommands <- []
                else
                    firstFrame <- false
                match Protocol.receiveFrame s with
                | Some frame ->
                    yield frame
                | None ->
                    state <- Stopped
                    running <- false
        }

    /// <summary>
    /// Queue commands to send with the next frame response.
    /// Commands are delivered when the consumer requests the next frame from the Frames sequence.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Thrown if the session is not Connected or Running.</exception>
    member _.SendCommands(commands: AICommand list) =
        match state with
        | Connected | Running ->
            pendingCommands <- commands
        | s ->
            raise (System.InvalidOperationException($"Cannot send commands: session is {s}"))

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
    /// Resets the in-game state by sending cheat commands to drain and restore resources.
    /// Runs several verification frames afterward to let the engine settle.
    /// </summary>
    /// <remarks>
    /// Requires an active connected session. Uses the engine's cheat mode to manipulate resources.
    /// Useful between test scenarios to start from a clean economic state.
    /// </remarks>
    member _.Reset() =
        requireConnected ()
        let s = requireStream ()
        // Send cheat commands on first frame
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
        // Run verification frames
        for _ in 1..10 do
            match Protocol.receiveFrame s with
            | Some _ -> Protocol.sendFrameResponse s []
            | None ->
                state <- Stopped
                failwith "Game ended during reset"
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
