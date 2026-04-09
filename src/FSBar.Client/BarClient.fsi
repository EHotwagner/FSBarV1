namespace FSBar.Client

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
type BarClient =
    /// <summary>Creates a new <see cref="T:FSBar.Client.BarClient"/> with the given engine configuration.</summary>
    /// <param name="config">The engine configuration for this session.</param>
    new: config: EngineConfig -> BarClient

    /// <summary>Gets the current lifecycle state of this session.</summary>
    member State: SessionState

    /// <summary>Gets the engine configuration used to create this client.</summary>
    member Config: EngineConfig

    /// <summary>Gets the handshake information received from the proxy, or <c>None</c> if not yet connected.</summary>
    member Handshake: HandshakeInfo option

    /// <summary>
    /// Gets the active network stream to the HighBar V2 proxy.
    /// </summary>
    /// <exception cref="T:System.Exception">Thrown if not currently connected to the proxy.</exception>
    member Stream: System.Net.Sockets.NetworkStream

    /// <summary>
    /// Game frames as a lazy sequence. Iterating pulls frames from the engine one at a time.
    /// Between iterations, any commands queued via SendCommands are delivered to the engine.
    /// The sequence terminates when the engine disconnects or sends a shutdown message.
    /// </summary>
    member Frames: seq<GameFrame>

    /// <summary>
    /// Queue commands to send with the next frame response.
    /// Commands are delivered when the consumer requests the next frame from the Frames sequence.
    /// </summary>
    /// <exception cref="T:System.InvalidOperationException">Thrown if the session is not Connected or Running.</exception>
    member SendCommands: commands: Highbar.AICommand list -> unit

    /// <summary>
    /// Launches the BAR engine, listens for the HighBar V2 proxy connection on the configured socket path,
    /// performs the protocol handshake, and transitions to the Connected state.
    /// </summary>
    /// <exception cref="T:System.Exception">Thrown if the client is in an invalid state to start, or if connection/handshake fails.</exception>
    member Start: unit -> unit

    /// <summary>
    /// Resets the in-game state by sending cheat commands to drain and restore resources.
    /// </summary>
    /// <remarks>Requires an active connected session and engine cheat mode.</remarks>
    member Reset: unit -> unit

    /// <summary>
    /// Stops the session and cleans up all resources. Safe to call from any state.
    /// </summary>
    member Stop: unit -> unit

    interface System.IDisposable

/// <summary>Convenience module functions for creating and starting <see cref="T:FSBar.Client.BarClient"/> instances.</summary>
module BarClient =
    /// <summary>Creates a default <see cref="T:FSBar.Client.EngineConfig"/> for headless testing.</summary>
    /// <returns>A new default engine configuration.</returns>
    val defaultConfig: unit -> EngineConfig

    /// <summary>
    /// Creates a new <see cref="T:FSBar.Client.BarClient"/> with the given configuration without starting it.
    /// </summary>
    /// <param name="config">The engine configuration for the new client.</param>
    /// <returns>A new <see cref="T:FSBar.Client.BarClient"/> in the Idle state.</returns>
    val create: config: EngineConfig -> BarClient

    /// <summary>
    /// Creates and starts a headless <see cref="T:FSBar.Client.BarClient"/> with default configuration.
    /// </summary>
    /// <returns>A started client in the Connected state.</returns>
    /// <exception cref="T:System.Exception">Thrown if the engine fails to launch or connection times out.</exception>
    val startHeadless: unit -> BarClient

    /// <summary>
    /// Creates and starts a graphical <see cref="T:FSBar.Client.BarClient"/> with default configuration.
    /// </summary>
    /// <returns>A started client in the Connected state.</returns>
    /// <exception cref="T:System.Exception">Thrown if the engine fails to launch or connection times out.</exception>
    val startGraphical: unit -> BarClient
