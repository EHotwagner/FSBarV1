namespace FSBar.Viz

open FSBar.Client

/// Live REPL visualization API. Thread-safe, single viewer instance.
module GameViz =
    val start: cfg: VizConfig option -> unit
    val stop: unit -> unit
    val attachToClient: client: BarClient -> unit
    val attachWithState: mapGrid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> teamId: int -> unit
    val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit
    val seedUnits: unitStates: UnitState list -> unit
    val onFrame: frame: GameFrame -> unit
    val setDisconnected: unit -> unit
    val resetView: unit -> unit
    val setBaseLayer: layer: LayerKind -> unit
    val toggleOverlay: overlay: OverlayKind -> unit
    val enableOverlay: overlay: OverlayKind -> unit
    val disableOverlay: overlay: OverlayKind -> unit
    /// Returns the current set of active overlays. Thread-safe; takes a
    /// snapshot under the same lock the `toggleOverlay` / `enableOverlay`
    /// / `disableOverlay` mutators hold. Added for feature
    /// 035-central-gui-hub (FR-017): hub chrome reads this each frame
    /// to reflect the in-viewer hotkey state in its own toolbar.
    val getActiveOverlays: unit -> Set<OverlayKind>
    /// Replaces the current overlay set wholesale. Use this when the
    /// hub chrome's toolbar is the authoritative input, so a single
    /// click updates many overlays atomically rather than through a
    /// series of toggle calls. Thread-safe.
    val setActiveOverlays: overlays: Set<OverlayKind> -> unit
    val setConfig: cfg: VizConfig -> unit
    val updateConfig: f: (VizConfig -> VizConfig) -> unit
    val setColorScheme: layer: LayerKind -> scheme: ColorScheme -> unit
    val setMarkerSize: size: float32 -> unit
    val setOverlayOpacity: opacity: float32 -> unit
    val toggleGridLines: unit -> unit
    val pan: dx: float32 -> dy: float32 -> unit
    val zoom: factor: float32 -> centerX: float32 -> centerY: float32 -> unit
    val screenshot: folder: string -> Result<string, string>
    // --- Configurator panel (feature 033-viz-style-configurator) ---
    /// Toggle the style configurator side panel open/closed.
    val toggleConfigPanel: unit -> unit
    /// True if the configurator panel is currently open.
    val isConfigPanelOpen: unit -> bool
