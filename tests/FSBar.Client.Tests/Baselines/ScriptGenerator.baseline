namespace FSBar.Client

/// <summary>Generates Spring engine startup scripts from an <see cref="T:FSBar.Client.EngineConfig"/>.</summary>
module ScriptGenerator =
    /// <summary>
    /// Generates a Spring engine TDF-format startup script that configures a two-team game
    /// with the HighBar V2 AI proxy on team 0 and the specified opponent AI on team 1.
    /// The script includes mod options for faction selection, game speed, and debug commands.
    /// </summary>
    /// <param name="config">The engine configuration specifying map, factions, socket path, and opponent AI.</param>
    /// <returns>A complete TDF-format script string ready to be written to disk for engine launch.</returns>
    val generate: config: EngineConfig -> string

    /// <summary>
    /// Generates the contents of <c>springsettings.cfg</c> for a session. Always forces
    /// windowed mode (<c>Fullscreen=0</c>, <c>XResolution=1280</c>, <c>YResolution=720</c>).
    /// When <c>config.AutohostPort = Some p</c>, also emits <c>AutohostIP=127.0.0.1</c> and
    /// <c>AutohostPort=p</c> so the engine dials back to the hub-bound UDP socket (feature 039).
    /// When <c>config.AutohostPort = None</c>, neither autohost line appears.
    /// </summary>
    /// <param name="config">The engine configuration.</param>
    /// <returns>A complete <c>springsettings.cfg</c> text body (each entry terminated by <c>\n</c>).</returns>
    val generateSpringSettings: config: EngineConfig -> string
