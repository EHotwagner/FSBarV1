namespace FSBar.Hub.App.Tabs

open SkiaViewer
open FSBar.Viz

/// Configurator tab (feature 035-central-gui-hub T058) — wraps the
/// existing `FSBar.Viz.ConfigPanel` + `StylePreset` machinery so the
/// user can tune the active `VizConfig` live from inside the hub.
///
/// Scope: Phase-3 parity with the standalone viewer's side panel —
/// color swatches, sliders, toggles for every attribute in
/// `ConfigDescriptors.all`, plus the save / load / reset action
/// buttons. Changes propagate into the same `VizConfig` the
/// `ViewerTab` renders against, so a color tweak is visible in the
/// embedded viewer within one frame (AS-6.1).
///
/// Not yet: preset-name entry dialog, JSON preview pane, undo stack.
/// The underlying `FSBar.Viz.StylePreset.save` enforces filesystem-
/// safe names and the configurator surfaces errors inline.
module ConfiguratorTab =

    /// Hub-level action surfaced by the tab. Whole-config mutations
    /// route through `HubStateStore.setVizConfig` inside the tab
    /// itself (feature 041 FR-017/FR-018); these actions cover the
    /// preset-name file-system operations the entrypoint must run.
    [<RequireQualifiedAccess>]
    type ConfiguratorTabAction =
        /// Save the current config as a named preset.
        | SavePreset of name: string
        /// Load a named preset from disk.
        | LoadPreset of name: string
        /// Delete a named preset.
        | DeletePreset of name: string
        /// Restore every attribute to the constructor default in
        /// `ConfigDescriptors.all`.
        | ResetDefaults

    /// Per-tab render state. Owned by the entrypoint. Excludes any
    /// field already authoritatively held by `HubStateStore` (R6).
    type ConfiguratorTabState = {
        Panel: ConfigPanelState
        PresetNames: string list
        ActivePreset: string option
        /// Most recent save / load / delete outcome. `None` when the
        /// tab has never run an action; `Some (Ok _)` / `Some (Error _)`
        /// when the last user-initiated file I/O finished.
        LastPresetResult: Result<string, string> option
    }

    /// Construct the initial tab state. Reads the on-disk preset
    /// list so the tab can render even without a live session.
    val init: unit -> ConfiguratorTabState

    /// Paint the tab. Reads `VizConfig` from the supplied store
    /// (`HubStateStore.current store`) so remote gRPC writes are
    /// reflected in the next paint without going through the
    /// entrypoint (feature 041 FR-017).
    val render:
        state: ConfiguratorTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Forward a mouse event to `ConfigPanel.handleInput`. Whole-
    /// config mutations are written back through `HubStateStore.setVizConfig`
    /// before returning (feature 041 FR-018); preset/reset side-
    /// effects bubble up via `ConfiguratorTabAction`.
    val handleInput:
        state: ConfiguratorTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        event: InputEvent ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            ConfiguratorTabState * ConfiguratorTabAction option
