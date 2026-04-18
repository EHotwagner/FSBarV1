namespace FSBar.Hub

/// Hub-wide user-editable settings. Persisted as a single JSON document
/// under the XDG config home.
///
/// **Phase 2 scope note**: the planning sketch included a `LastLobby`
/// field of type `LobbyConfig option` for persisting the user's most
/// recent lobby. That field is deferred to Phase 3 (task T027, when
/// `LobbyConfig` lands); the current JSON schema reserves room for it
/// via a forward-compatible `SchemaVersion` field but does not yet
/// emit / consume it.
module HubSettings =

    /// JSON-serialisable record. All fields are required on write;
    /// missing fields on read are filled from `defaults`.
    type HubSettings = {
        /// Absolute path to the BAR data directory. `None` uses the XDG
        /// default (`$HOME/.local/state/Beyond All Reason`).
        BarDataDirOverride: string option
        /// Pinned engine version (e.g. `"2026.03.14"`). `None` selects
        /// the newest installed `recoil_*` directory.
        EngineVersionOverride: string option
        /// Localhost TCP port the hub's gRPC scripting service binds.
        /// Validated to `[1024, 65535]` on load; out-of-range loads
        /// trigger a fallback to `defaults.GrpcPort`.
        GrpcPort: int
        /// Initial state of the "Launch original BAR viewer" toggle on
        /// the Setup tab (US4).
        LaunchGraphicalViewerDefault: bool
        /// Initial state of the "Start paused" toggle on the Setup tab
        /// (feature 038, FR-004a). Defaults to `true` on fresh install;
        /// persisted across Hub restarts. When `true`, every launched
        /// match starts paused via a `/pause` chat command on the first
        /// `Running` transition.
        StartPausedDefault: bool
        /// Maximum number of concurrent `StreamRenderFrames` subscribers
        /// served by `HeadlessRenderer` (feature 040 US2). Validated to
        /// `[1, 32]`; default `8`. Persisted starting at schema v2.
        /// v1 files load this field as the default and are rewritten as
        /// v2 on the next `save`.
        MaxRenderFrameSubscribers: int
        /// Maximum number of concurrent `StreamHubLog` subscribers
        /// served by `HubLog` (feature 042, FR-015a). Validated to
        /// `[1, 32]`; default `8`. Persisted starting at schema v3.
        /// v2 files load this field as the default and are rewritten as
        /// v3 on the next `save`.
        MaxLogStreamSubscribers: int
        /// Schema version. Currently `3` (feature 042 bumped from 2 when
        /// `MaxLogStreamSubscribers` was added; feature 040 bumped from 1
        /// when `MaxRenderFrameSubscribers` was added). Increments when
        /// additive / destructive changes require a migration step on
        /// load; missing-field additions do not bump.
        SchemaVersion: int
    }

    /// Factory values identical to a fresh-install state:
    /// no overrides, port `5021`, graphical-viewer toggle off,
    /// start-paused toggle on, 8 concurrent render subscribers,
    /// 8 concurrent log-stream subscribers, schema `3`.
    val defaults: HubSettings

    /// Returns the absolute filesystem path where settings are read /
    /// written. Resolves in this order: `$XDG_CONFIG_HOME/fsbar-hub/settings.json`
    /// when the variable is set and non-empty; otherwise
    /// `$HOME/.config/fsbar-hub/settings.json`.
    val settingsPath: unit -> string

    /// Loads settings from `settingsPath ()`.
    ///
    /// Behaviour:
    ///   * File absent → `defaults`.
    ///   * Parse error or missing required field → `defaults` with a
    ///     diagnostic written to stderr. Never raises on malformed JSON.
    ///   * `GrpcPort` outside `[1024, 65535]` → the field is clamped to
    ///     `defaults.GrpcPort`; other fields are preserved.
    val load: unit -> HubSettings

    /// Atomically writes `settings` to `settingsPath ()`.
    ///
    /// The write is temp-file-plus-rename to prevent torn reads: serialises
    /// to a sibling `*.tmp` file, flushes, then renames over the target.
    ///
    /// Returns `Ok ()` on success; `Error msg` carries an operator-visible
    /// reason on failure (permissions, I/O error, serialisation).
    val save: settings: HubSettings -> Result<unit, string>

    /// Return a copy of `settings` with `StartPausedDefault = value`.
    /// Pure transformation; callers persist via `save` and publish a
    /// `HubEvent.HubSettingsChanged` if desired.
    val updateStartPausedDefault:
        settings: HubSettings -> value: bool -> HubSettings

    /// Return a copy of `settings` with
    /// `LaunchGraphicalViewerDefault = value`. Pure transformation.
    val updateLaunchGraphicalViewerDefault:
        settings: HubSettings -> value: bool -> HubSettings

    /// Return a validated copy of `settings` with
    /// `MaxRenderFrameSubscribers = value`. Rejects values outside
    /// `[1, 32]` with an operator-visible reason (data-model §HubSettings).
    val updateMaxRenderFrameSubscribers:
        settings: HubSettings ->
        value: int ->
            Result<HubSettings, string>

    /// Return a validated copy of `settings` with
    /// `MaxLogStreamSubscribers = value`. Rejects values outside
    /// `[1, 32]` with an operator-visible reason (feature 042 R7).
    val updateMaxLogStreamSubscribers:
        settings: HubSettings ->
        value: int ->
            Result<HubSettings, string>
