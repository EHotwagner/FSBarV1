namespace FSBar.Hub

/// Resolves the user's BAR installation — data directory, installed
/// engine versions, the active engine per user override, and the
/// skirmish AIs registered under the active engine.
///
/// Every path this module returns is verified at resolution time;
/// callers can hand the result to downstream modules (proxy installer,
/// session launcher) without re-checking existence.
///
/// Wraps `FSBar.Client.EngineDiscovery` — no filesystem logic is
/// duplicated — but adds a data-model vocabulary (`EngineVersionEntry`,
/// `BarInstall`) and structured error reporting tuned for the hub's
/// Settings / First-Run UI.
module BarInstall =

    /// Descriptor for one installed engine version under
    /// `<dataDir>/engine/recoil_<version>/`.
    type EngineVersionEntry = {
        /// Version string as it appears after the `recoil_` prefix
        /// (e.g. `"2026.03.14"`).
        Version: string
        /// Absolute path to the engine version directory.
        EngineDir: string
        /// True when the `spring-headless` binary exists and is
        /// executable. The hub refuses to launch against an engine
        /// that lacks this.
        HasHeadlessBin: bool
        /// True when the `spring` binary (graphical) exists and is
        /// executable. Required only for US4 (launch graphical viewer
        /// alongside the headless engine).
        HasGraphicalBin: bool
        /// Absolute path to `<EngineDir>/AI/Skirmish`. Directory may
        /// not yet exist if no AI has been installed — the path is
        /// handed to `ProxyInstaller` which creates it on demand.
        AiSkirmishDir: string
    }

    /// Complete hub view of the install.
    type BarInstall = {
        /// Resolved data directory (override > XDG default).
        DataDir: string
        /// All detected engines, newest first.
        Engines: EngineVersionEntry list
        /// The engine chosen per user override or the default policy
        /// (newest installed). Always a member of `Engines`.
        ActiveEngine: EngineVersionEntry
        /// True when `DataDir` equals the XDG-default
        /// `$HOME/.local/state/Beyond All Reason`. The GUI flags this
        /// in the Settings tab so users know they're using the default.
        DataDirIsDefault: bool
    }

    /// Failure modes surfaced by `detect`.
    type BarInstallError =
        /// The resolved data directory (override or default) does not
        /// exist on disk.
        | DataDirNotFound of path: string
        /// The data directory exists but does not contain an `engine/`
        /// subdirectory.
        | EngineSubdirMissing of path: string
        /// The data directory and `engine/` subdirectory both exist
        /// but no `recoil_*` versions are installed.
        | NoEngineVersions of path: string
        /// `HubSettings.EngineVersionOverride` names a version that
        /// does not exist under `<dataDir>/engine/`.
        | OverriddenEngineNotFound of version: string

    /// Returns the absolute data directory path the hub should use —
    /// `settings.BarDataDirOverride` when `Some`, else the XDG default
    /// (`$HOME/.local/state/Beyond All Reason`). No filesystem check.
    val resolveDataDir: settings: HubSettings.HubSettings -> string

    /// Enumerates installed engines and selects the active one per the
    /// settings override. All filesystem checks run here; callers can
    /// treat an `Ok` result as fully validated.
    val detect: settings: HubSettings.HubSettings -> Result<BarInstall, BarInstallError>

    /// Lists the skirmish AIs installed under `<engine.AiSkirmishDir>`
    /// by scanning for immediate subdirectories. Returns the AI names
    /// (directory basenames) sorted alphabetically. Missing
    /// `AI/Skirmish` directory yields an empty list.
    val listSkirmishAis: engine: EngineVersionEntry -> string list

    /// Human-readable rendering of a `BarInstallError`. Used by the
    /// GUI and by diagnostics logs.
    val formatError: BarInstallError -> string
