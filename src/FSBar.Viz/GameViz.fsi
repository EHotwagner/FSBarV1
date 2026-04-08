namespace FSBar.Viz

open FSBar.Client

/// Public REPL API for the game state visualization.
/// All functions are thread-safe and can be called from any thread.
module GameViz =
    /// Start the visualization window on a background thread.
    /// Only one visualization can be active at a time.
    val start: config': VizConfig option -> unit

    /// Stop the visualization and close the window.
    val stop: unit -> unit

    /// Attach the visualization to a BarClient session for data access.
    /// Must be called after start() and before the game loop begins.
    val attachToClient: client: BarClient -> unit

    /// Seed the visualization with pre-existing units (e.g. units that existed before viz started).
    val seedUnits: unitStates: UnitState list -> unit

    /// Notify the visualization that a new frame has been processed.
    /// Builds the GameSnapshot internally from the attached client's data.
    val onFrame: frame: GameFrame -> unit

    /// Mark the session as disconnected.
    val setDisconnected: unit -> unit

    /// Reset to auto-fit full map view.
    val resetView: unit -> unit

    /// Set the base map layer.
    val setBaseLayer: layer: LayerKind -> unit

    /// Toggle an overlay on or off.
    val toggleOverlay: overlay: OverlayKind -> unit

    /// Enable a specific overlay.
    val enableOverlay: overlay: OverlayKind -> unit

    /// Disable a specific overlay.
    val disableOverlay: overlay: OverlayKind -> unit

    /// Replace the full configuration.
    val setConfig: config': VizConfig -> unit

    /// Update the configuration using a function.
    val updateConfig: f: (VizConfig -> VizConfig) -> unit

    /// Set the color scheme for a specific layer.
    val setColorScheme: layer: LayerKind -> scheme: ColorScheme -> unit

    /// Set the unit marker radius in pixels.
    val setMarkerSize: size: float32 -> unit

    /// Set overlay opacity (0.0 = transparent, 1.0 = opaque).
    val setOverlayOpacity: opacity: float32 -> unit

    /// Toggle grid line visibility.
    val toggleGridLines: unit -> unit

    /// Pan the view by a pixel delta.
    val pan: dx: float32 -> dy: float32 -> unit

    /// Zoom by a factor around a screen-space center point.
    val zoom: factor: float32 -> centerX: float32 -> centerY: float32 -> unit

    /// Take a screenshot of the current viz window and save it to the given folder.
    val screenshot: folder: string -> Result<string, string>
