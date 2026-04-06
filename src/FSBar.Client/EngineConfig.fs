namespace FSBar.Client

open System

/// <namespacedoc>
/// <summary>
/// F# client library for orchestrating Beyond All Reason (BAR) AI games via the HighBar V2 proxy.
/// Provides typed commands, event handling, map analysis, and session management over Unix domain sockets.
/// </summary>
/// </namespacedoc>
module internal NamespaceDoc = ()

/// <summary>Specifies whether the BAR engine runs headless (no GUI) or with a graphical window.</summary>
type EngineMode =
    /// <summary>Run the engine without a graphical window. Suitable for automated testing and CI environments.</summary>
    | Headless
    /// <summary>Run the engine with a full graphical window. Useful for visual debugging and demonstrations.</summary>
    | Graphical

/// <summary>
/// Configuration record for a BAR engine session. Controls engine launch parameters,
/// socket communication, game setup (map, factions, opponent), and timeout behavior.
/// </summary>
type EngineConfig = {
    /// <summary>Whether to launch the engine in headless or graphical mode.</summary>
    Mode: EngineMode
    /// <summary>Filesystem path for the Unix domain socket used to communicate with the HighBar V2 proxy.</summary>
    SocketPath: string
    /// <summary>Name of the BAR map to load (e.g., "Red Rock Desert v2").</summary>
    MapName: string
    /// <summary>Game type identifier including version (e.g., "Beyond All Reason test-29840-d9b7dba").</summary>
    GameType: string
    /// <summary>Name of the opponent AI to play against (e.g., "NullAI").</summary>
    OpponentAI: string
    /// <summary>Faction for the opponent team (e.g., "Cortex").</summary>
    OpponentSide: string
    /// <summary>Faction for our AI team (e.g., "Armada").</summary>
    OurSide: string
    /// <summary>Timeout in milliseconds for the initial socket connection accept.</summary>
    TimeoutMs: int
    /// <summary>Executable name or path for the engine binary (e.g., "spring-headless").</summary>
    EngineBin: string
    /// <summary>Path to the BAR AppImage used for graphical mode launches.</summary>
    AppImagePath: string
    /// <summary>Optional override for the Spring data directory. When <c>None</c>, the engine default is used.</summary>
    SpringDataDir: string option
    /// <summary>Game speed multiplier. Higher values run the simulation faster (e.g., 100 for 100x speed).</summary>
    GameSpeed: int
    /// <summary>
    /// Optional override for the socket read timeout in milliseconds.
    /// When <c>None</c>, falls back to the <c>FSBAR_CLIENT_TIMEOUT_MS</c> environment variable or a 10 000 ms default.
    /// </summary>
    ReadTimeoutMs: int option
}

/// <summary>Functions for creating and querying <see cref="T:FSBar.Client.EngineConfig"/> values.</summary>
module EngineConfig =
    /// <summary>
    /// Creates a default <see cref="T:FSBar.Client.EngineConfig"/> with sensible defaults for headless testing.
    /// A unique socket path is generated using a partial GUID.
    /// </summary>
    /// <returns>A new <see cref="T:FSBar.Client.EngineConfig"/> with default values.</returns>
    let defaultConfig () =
        let guid = Guid.NewGuid().ToString("N").[..7]
        {
            Mode = Headless
            SocketPath = $"/tmp/fsbar-{guid}.sock"
            MapName = "Avalanche 3.4"
            GameType = "Beyond All Reason test-29871-90f4bc1"
            OpponentAI = "NullAI"
            OpponentSide = "Cortex"
            OurSide = "Armada"
            TimeoutMs = 30000
            EngineBin = "spring-headless"
            AppImagePath = "/home/developer/applications/Beyond-All-Reason-1.2988.0.AppImage"
            SpringDataDir = None
            GameSpeed = 100
            ReadTimeoutMs = None
        }

    /// <summary>
    /// Resolves the effective read timeout for socket operations.
    /// Checks, in order: <see cref="P:FSBar.Client.EngineConfig.ReadTimeoutMs"/>,
    /// the <c>FSBAR_CLIENT_TIMEOUT_MS</c> environment variable, then falls back to 10 000 ms.
    /// </summary>
    /// <param name="config">The engine configuration to resolve the timeout for.</param>
    /// <returns>The resolved read timeout in milliseconds.</returns>
    let resolveReadTimeout (config: EngineConfig) : int =
        config.ReadTimeoutMs
        |> Option.defaultWith (fun () ->
            match Environment.GetEnvironmentVariable("FSBAR_CLIENT_TIMEOUT_MS") with
            | null | "" -> 10000
            | value ->
                match Int32.TryParse(value) with
                | true, ms -> ms
                | false, _ -> 10000)
