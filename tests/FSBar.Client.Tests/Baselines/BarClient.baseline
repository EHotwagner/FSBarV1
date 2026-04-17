namespace FSBar.Client

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
type BarClient =
    /// Creates a new BarClient with the given engine configuration.
    new: config: EngineConfig -> BarClient

    /// Gets the current lifecycle state of this session.
    member State: SessionState

    /// Gets the engine configuration used to create this client.
    member Config: EngineConfig

    /// Gets the handshake information received from the proxy, or None if not yet connected.
    member Handshake: HandshakeInfo option

    /// Gets the active network stream to the HighBar V2 proxy.
    member Stream: System.Net.Sockets.NetworkStream

    /// Game frames as a push-based observable. Subscribers receive frames as they arrive from the engine.
    /// The observable completes when the engine disconnects or sends a shutdown message.
    member Frames: System.IObservable<GameFrame>

    /// Queue commands to send with the next frame response.
    member SendCommands: commands: Highbar.AICommand list -> unit

    /// Gets the current game state snapshot, updated each frame.
    member GameState: GameState

    /// Blocks and collects up to N frames from the observable, calling the handler for each.
    /// Useful for synchronous REPL-style stepping.
    member WaitFrames: count: int -> handler: (GameFrame -> unit) -> unit

    /// Launches the BAR engine, connects to the proxy, and performs the handshake.
    member Start: unit -> unit

    /// Resets the in-game state by sending cheat commands.
    member Reset: unit -> unit

    /// Stops the session and cleans up all resources. Safe to call from any state.
    member Stop: unit -> unit

    interface System.IDisposable

/// Convenience module functions for creating and starting BarClient instances.
module BarClient =
    val defaultConfig: unit -> EngineConfig
    val create: config: EngineConfig -> BarClient
    val startHeadless: unit -> BarClient
    val startGraphical: unit -> BarClient
