namespace FSBar.Hub

/// Installs (and re-verifies) the bundled HighBarV2 skirmish AI into
/// the user's BAR data directory (feature 035-central-gui-hub US2).
///
/// Three discrete steps, each idempotent and independently observable
/// via `HubEvents.ProxyInstallProgress`:
///
///   1. `CopyAiFiles` → copy `libSkirmishAI.so` + `AIInfo.lua` +
///      `AIOptions.lua` from the repo's `proxy/bundled/<v>/` into
///      `<engineDir>/AI/Skirmish/HighBarV2/<v>/`.
///   2. `TouchDevMode` → ensure `<dataDir>/devmode.txt` exists.
///   3. `ToggleSimpleAiList` → targeted per-key rewrite of
///      `<dataDir>/LuaMenu/Config/IGL_data.lua` to set
///      `simpleAiList = false` without reformatting the file.
///
/// FR-010: refuses to touch any path under `packages/` or `pool/`.
///
/// The installer never raises on recoverable conditions — every
/// failure mode becomes a `StepOutcome.StepFailed reason` event and
/// contributes an entry to the returned error list. Unrecoverable
/// bugs (null refs, arithmetic overflow, etc.) still propagate.
module ProxyInstaller =

    /// On-disk snapshot of one engine's proxy-install state. Produced
    /// by `checkStatus`; consumed by `health` and by the Settings tab
    /// status display.
    type ProxyInstallStatus = {
        /// Engine version the status pertains to (e.g. `"2026.03.14"`).
        EngineVersion: string
        /// Absolute path to the target install directory under the
        /// engine, even when it does not yet exist.
        InstalledAtPath: string
        /// Version of the proxy currently installed at
        /// `InstalledAtPath`. `None` when no install is present.
        InstalledVersion: string option
        /// All three proxy files exist under `InstalledAtPath`.
        AiFilesPresent: bool
        /// `<dataDir>/devmode.txt` exists.
        DevModeFilePresent: bool
        /// `<dataDir>/LuaMenu/Config/IGL_data.lua` contains
        /// `simpleAiList = false` (targeted-edit check).
        SimpleAiListDisabled: bool
        /// `InstalledVersion = Some bundled.Version`, i.e. no upgrade
        /// required.
        MatchesBundled: bool
    }

    /// Operator-facing health projection over `ProxyInstallStatus`.
    type ProxyHealth =
        /// Everything is installed and current.
        | UpToDate
        /// No proxy install present at all.
        | NotInstalled
        /// An older version of the proxy is installed under the
        /// checked engine; an upgrade would swap files.
        | StaleVersion of installed: string * bundled: string
        /// The proxy is installed but under a different engine
        /// version than the hub's active engine — the user's
        /// `AI/Skirmish/HighBarV2/<v>/` subdir belongs to a previous
        /// recoil_* release.
        | StaleEngine of forEngine: string * activeEngine: string
        /// Files are in place but devmode.txt / simpleAiList is still
        /// missing or `true`.
        | ConfigIncomplete of reasons: string list

    /// Reads the filesystem and assembles a `ProxyInstallStatus` for
    /// the given engine. No writes.
    val checkStatus:
        install: BarInstall.BarInstall ->
        bundled: BundledProxy.BundledProxyInfo ->
            ProxyInstallStatus

    /// Pure projection: `ProxyInstallStatus → ProxyHealth`.
    val health: status: ProxyInstallStatus -> ProxyHealth

    /// Runs the three install steps in order, emitting a
    /// `HubEvents.ProxyInstallProgress` per step per outcome. Returns
    /// the post-install `ProxyInstallStatus` on success, or the list
    /// of step-level failure reasons on partial failure.
    ///
    /// **Idempotent**: if every step's precondition is already
    /// satisfied, every emitted event is `Skipped`, no file mtime
    /// changes, and the `Ok` status equals the pre-call status
    /// byte-for-byte (SC-008).
    ///
    /// **Force flag**: when the on-disk `libSkirmishAI.so` has a
    /// later mtime than the bundled copy — i.e. the user built a
    /// newer proxy locally — the copy step is `Skipped` with a
    /// warning message and `force = false`. Pass `force = true` to
    /// overwrite in that case (spec.md Edge Cases).
    val install:
        install: BarInstall.BarInstall ->
        bundled: BundledProxy.BundledProxyInfo ->
        events: HubEvents.IHubEventSink ->
        force: bool ->
            Result<ProxyInstallStatus, string list>

    /// Pure helper: applies the `simpleAiList` rewrite to the given
    /// file contents. Returns `Some new` when a write would change
    /// the byte content, `None` when the key is already `false` or
    /// absent entirely (caller skips the write in both cases).
    ///
    /// Regex per research.md R5 — anchored multiline, group 4 is
    /// the boolean token. Surrounding whitespace, comments, and key
    /// order are preserved byte-for-byte.
    val rewriteSimpleAiList: contents: string -> string option

    /// Human-readable rendering of a `ProxyHealth`. Used by the
    /// Settings tab's status row and diagnostics logs.
    val formatHealth: ProxyHealth -> string
