namespace FSBar.Client

/// <summary>Represents a validated engine installation found during directory scanning.</summary>
type DiscoveredEngine = {
    /// <summary>Engine version string extracted from directory name (e.g., "2025.06.21").</summary>
    VersionString: string
    /// <summary>Full path to the engine version directory.</summary>
    VersionDir: string
    /// <summary>Path to the spring-headless binary, if present and executable.</summary>
    HeadlessBin: string option
    /// <summary>Path to the graphical spring binary, if present and executable.</summary>
    GraphicalBin: string option
    /// <summary>Resolved Spring data directory containing maps/ and packages/.</summary>
    DataDir: string
}

/// <summary>Represents a resolved game version from the rapid versioning system.</summary>
type DiscoveredGame = {
    /// <summary>The rapid tag used for lookup (e.g., "byar:test").</summary>
    Tag: string
    /// <summary>The full game name (e.g., "Beyond All Reason test-29876-f8bb848").</summary>
    Name: string
    /// <summary>The package hash from versions.gz.</summary>
    Hash: string
}

/// <summary>Indicates how the engine was resolved.</summary>
type ResolutionSource =
    /// <summary>Resolved via HIGHBAR_TEST_ENGINE environment variable.</summary>
    | OverrideEnvVar
    /// <summary>Resolved via engine-version.json configuration file.</summary>
    | ConfigFile
    /// <summary>Resolved via automatic directory scanning.</summary>
    | AutoDetected

/// <summary>The result of the full engine resolution process.</summary>
type EngineResolution = {
    /// <summary>How the engine was resolved.</summary>
    Source: ResolutionSource
    /// <summary>The selected engine installation.</summary>
    Engine: DiscoveredEngine
    /// <summary>The selected game version.</summary>
    Game: DiscoveredGame
}

/// <summary>Functions for discovering and resolving BAR engine installations.</summary>
module EngineDiscovery =
    /// <summary>
    /// Returns the standard BAR data directory path if it exists and contains
    /// the expected maps/ and packages/ subdirectories.
    /// </summary>
    /// <returns>The data directory path, or None if not found.</returns>
    val defaultDataDir: unit -> string option

    /// <summary>
    /// Scans the given data directory for installed engine versions.
    /// Returns a list of discovered engines sorted by version string descending (newest first).
    /// </summary>
    /// <param name="dataDir">The BAR data directory to scan.</param>
    /// <returns>A list of discovered engine installations, newest first.</returns>
    val discoverEngines: dataDir: string -> DiscoveredEngine list

    /// <summary>
    /// Parses the rapid versions.gz file to resolve a game version tag
    /// to a concrete game name and hash.
    /// </summary>
    /// <param name="dataDir">The BAR data directory containing rapid/ subdirectory.</param>
    /// <param name="tag">The rapid tag to resolve (e.g., "byar:test").</param>
    /// <returns>The discovered game version, or None if the tag is not found.</returns>
    val discoverGameVersion: dataDir: string -> tag: string -> DiscoveredGame option

    /// <summary>
    /// Validates that an engine binary exists and is executable.
    /// Raises an exception with an actionable error message if validation fails.
    /// </summary>
    /// <param name="binaryPath">The path to the engine binary to validate.</param>
    /// <param name="versionString">The version string for error reporting.</param>
    val validateEngine: binaryPath: string -> versionString: string -> unit

    /// <summary>
    /// Resolves the engine to use by checking, in order:
    /// (1) HIGHBAR_TEST_ENGINE environment variable,
    /// (2) engine-version.json config file (if configPath is provided),
    /// (3) automatic detection from the standard BAR data directory.
    /// Logs the resolved version and source. Raises an exception if no engine is found.
    /// </summary>
    /// <param name="configPath">Optional path to engine-version.json for version pinning.</param>
    /// <returns>The resolved engine, game version, and resolution source.</returns>
    val resolveEngine: configPath: string option -> EngineResolution
