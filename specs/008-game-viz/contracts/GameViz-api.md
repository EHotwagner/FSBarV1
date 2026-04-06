# GameViz Public API Contract

**Module**: `FSBar.Viz.GameViz` (exposed via `GameViz.fsi`)

## Lifecycle

```fsharp
/// Start the visualization window (module-level singleton).
/// The window opens on a background thread; the calling thread (REPL) remains responsive.
/// Call stop() to close. Only one visualization can be active at a time.
val start: config: VizConfig option -> unit

/// Stop the visualization and close the window.
val stop: unit -> unit
```

## Game State Updates

```fsharp
/// Attach the visualization to a BarClient session. GameViz will hook into
/// the client's frame loop to build and update GameSnapshot internally
/// (map layers, unit tracking, economy queries, event processing).
/// Must be called after start() and before the game loop begins.
val attachToClient: client: BarClient -> unit

/// Notify the visualization that a new frame has been processed.
/// Call this after each BarClient.Step() or inside a StepWith handler.
/// GameViz reads the latest MapGrid, queries unit positions/economy via
/// Callbacks, and processes GameFrame events to update the snapshot.
val onFrame: frame: GameFrame -> unit

/// Mark the session as disconnected (freezes display on last known state).
val setDisconnected: unit -> unit
```

## Layer Control

```fsharp
/// Set the base map layer.
val setBaseLayer: layer: LayerKind -> unit

/// Toggle an overlay on or off.
val toggleOverlay: overlay: OverlayKind -> unit

/// Enable a specific overlay.
val enableOverlay: overlay: OverlayKind -> unit

/// Disable a specific overlay.
val disableOverlay: overlay: OverlayKind -> unit
```

## View Control

```fsharp
/// Pan the view by a pixel delta.
val pan: dx: float32 -> dy: float32 -> unit

/// Zoom by a factor around a screen-space center point.
val zoom: factor: float32 -> centerX: float32 -> centerY: float32 -> unit

/// Reset to auto-fit full map view.
val resetView: unit -> unit
```

## Customization

```fsharp
/// Replace the full configuration.
val setConfig: config: VizConfig -> unit

/// Update the configuration using a function (read-modify-write).
val updateConfig: f: (VizConfig -> VizConfig) -> unit

/// Set the color scheme for a specific layer.
val setColorScheme: layer: LayerKind -> scheme: ColorScheme -> unit

/// Set the unit marker radius in pixels.
val setMarkerSize: size: float32 -> unit

/// Set overlay opacity (0.0 = transparent, 1.0 = opaque).
val setOverlayOpacity: opacity: float32 -> unit

/// Toggle grid line visibility.
val toggleGridLines: unit -> unit
```

## Keyboard Shortcuts (Default Bindings)

| Key | Action |
|-----|--------|
| 1 | Base layer: HeightMap |
| 2 | Base layer: SlopeMap |
| 3 | Base layer: ResourceMap |
| 4 | Base layer: LosMap |
| 5 | Base layer: RadarMap |
| 6 | Base layer: TerrainClassification |
| 7 | Base layer: Passability (Kbot) |
| 8 | Base layer: Passability (Tank) |
| 9 | Base layer: Passability (Hover) |
| 0 | Base layer: Passability (Ship) |
| U | Toggle overlay: Units |
| E | Toggle overlay: Events |
| G | Toggle overlay: Grid |
| M | Toggle overlay: MetalSpots |
| $ | Toggle overlay: EconomyHud |
| Home | Reset view (auto-fit) |
| Mouse wheel | Zoom in/out |
| Click+drag | Pan |
