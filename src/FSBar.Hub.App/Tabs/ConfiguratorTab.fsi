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

    /// Hub-level action surfaced by the tab. Most panel mutations
    /// update the `VizConfig` in place; these actions are the
    /// side-effectful ones the hub needs to route through its own
    /// event bus / diagnostics.
    [<RequireQualifiedAccess>]
    type ConfiguratorTabAction =
        /// The panel produced a new `VizConfig` — caller should
        /// replace the hub-wide config reference.
        | ConfigChanged of VizConfig
        /// Save the current config as a named preset (inherits the
        /// `ConfigPanel`'s action).
        | SavePreset of name: string
        /// Load a named preset from disk.
        | LoadPreset of name: string
        /// Delete a named preset.
        | DeletePreset of name: string
        /// Restore every attribute to the constructor default in
        /// `ConfigDescriptors.all`.
        | ResetDefaults

    /// Per-tab render state. Owned by the entrypoint.
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

    /// Paint the tab. Delegates to `ConfigPanel.buildPanel` against
    /// the current config; embeds the result inside the Configurator
    /// tab's content rectangle.
    val render:
        state: ConfiguratorTabState ->
        vizConfig: VizConfig ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Forward a mouse event to `ConfigPanel.handleInput`. Returns
    /// the updated tab state + optional hub-level action (the caller
    /// mutates the live VizConfig and/or fires side-effects).
    val handleInput:
        state: ConfiguratorTabState ->
        vizConfig: VizConfig ->
        event: InputEvent ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            ConfiguratorTabState * ConfiguratorTabAction option
