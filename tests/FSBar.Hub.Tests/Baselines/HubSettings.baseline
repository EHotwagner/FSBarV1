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
        /// Schema version. `1` today; increments when additive /
        /// destructive changes require a migration step on load.
        /// Adding `StartPausedDefault` does not bump the version because
        /// missing fields fall back to defaults.
        SchemaVersion: int
    }

    /// Factory values identical to a fresh-install state:
    /// no overrides, port `5021`, graphical-viewer toggle off,
    /// start-paused toggle on, schema `1`.
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
