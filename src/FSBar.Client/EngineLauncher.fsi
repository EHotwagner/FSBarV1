namespace FSBar.Client

open System.Diagnostics

module EngineLauncher =
    /// Launch the headless engine with the given script content.
    val launchHeadless: config: EngineConfig -> scriptContent: string -> Process

    /// Launch the graphical engine with the given script content.
    val launchGraphical: config: EngineConfig -> scriptContent: string -> Process

    /// Stop an engine process and clean up socket/PID files.
    val stopEngine: socketPath: string -> proc: Process -> unit

    /// Get the session directory path for a given config.
    val getSessionDir: config: EngineConfig -> string
