namespace FSBar.Hub

/// Hub-wide user-editable settings. Persisted as a single JSON document
/// under the XDG config home.
module HubSettings =

    type HubSettings = {
        BarDataDirOverride: string option
        EngineVersionOverride: string option
        GrpcPort: int
        /// Initial state of the "Launch original BAR viewer" toggle on
        /// the Setup tab (US4 / FR-005). Defaults to `false` on a
        /// fresh install; persisted across Hub restarts.
        LaunchGraphicalViewerDefault: bool
        /// NEW (feature 038). Initial state of the "Start paused"
        /// toggle on the Setup tab (FR-004a). Defaults to `true` on
        /// a fresh install; persisted across Hub restarts.
        StartPausedDefault: bool
        /// Schema version. `1` today; increments when additive /
        /// destructive changes require a migration step on load.
        /// Adding `StartPausedDefault` does not bump the version
        /// because missing fields fall back to defaults.
        SchemaVersion: int
    }

    /// Factory values identical to a fresh-install state.
    /// `StartPausedDefault = true`, `LaunchGraphicalViewerDefault = false`.
    val defaults: HubSettings

    val settingsPath: unit -> string

    val load: unit -> HubSettings

    val save: settings: HubSettings -> Result<unit, string>
