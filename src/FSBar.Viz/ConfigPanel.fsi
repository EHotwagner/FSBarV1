namespace FSBar.Viz

open SkiaViewer

/// High-level action emitted by the panel for the host (GameViz) to execute.
[<RequireQualifiedAccess>]
type ConfigPanelAction =
    | SavePreset of name: string
    | LoadPreset of name: string
    | DeletePreset of name: string
    | ResetDefaults

/// Result of `ConfigPanel.handleInput`: updated panel state, optional updated
/// config, and an optional action to execute on the host.
type ConfigPanelInputResult =
    { PanelState: ConfigPanelState
      UpdatedConfig: VizConfig option
      Action: ConfigPanelAction option }

/// Configurator side panel — renders UI and handles input.
module ConfigPanel =
    /// Fixed panel width in pixels.
    val panelWidth: float32

    /// Creates the initial (closed) panel state.
    val initialState: ConfigPanelState

    /// Toggle the panel open/closed.
    val toggle: panelState: ConfigPanelState -> ConfigPanelState

    /// Test whether a screen-space point falls within the panel bounds.
    val hitTest:
        x: float32 ->
        y: float32 ->
        panelState: ConfigPanelState ->
        windowWidth: float32 ->
        bool

    /// Build scene elements for the panel given current config and panel state.
    /// Returns empty list when panel is closed.
    val buildPanel:
        config: VizConfig ->
        panelState: ConfigPanelState ->
        windowWidth: float32 ->
        windowHeight: float32 ->
        presetNames: string list ->
        activePresetName: string option ->
        Element list

    /// Handle a mouse/keyboard event within the panel.
    /// Returns updated panel state, optional updated VizConfig, and optional host action.
    val handleInput:
        event: InputEvent ->
        config: VizConfig ->
        panelState: ConfigPanelState ->
        windowWidth: float32 ->
        windowHeight: float32 ->
        ConfigPanelInputResult
