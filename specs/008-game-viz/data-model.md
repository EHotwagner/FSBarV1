# Data Model: Game State Visualization

**Feature**: 008-game-viz | **Date**: 2026-04-06

## Core Types

### LayerKind (Discriminated Union)

Identifies which base map layer to render.

```
LayerKind =
  | HeightMap
  | SlopeMap
  | ResourceMap
  | LosMap
  | RadarMap
  | TerrainClassification
  | Passability of MoveType    // Kbot | Tank | Hover | Ship (from FSBar.Client.MapGrid)
```

**Lifecycle**: Immutable enum-like values. Selected by user via keyboard or REPL.

### OverlayKind (Discriminated Union)

Identifies toggleable overlay layers drawn on top of the base layer.

```
OverlayKind =
  | Units
  | Events
  | Grid
  | MetalSpots
  | EconomyHud
```

**Lifecycle**: Toggled on/off independently. Multiple can be active simultaneously.

### ColorScheme (Record)

Defines how scalar values map to colors for a given layer.

```
ColorScheme = {
  Name: string
  MapValue: float32 -> SKColor    // Maps normalized [0..1] value to a color
}
```

**Built-in schemes**: Grayscale, Terrain (blue→green→brown→white), HeatMap (blue→red), Binary (red/green for passability).

### ViewState (Record)

Camera/viewport state for the map view.

```
ViewState = {
  Scale: float32                   // Pixels per grid cell
  Origin: float32 * float32        // Pixel offset (pan position)
  WindowSize: int * int            // Current window dimensions in pixels
  AutoFit: bool                    // If true, scale/origin computed from window size + map size
}
```

**State transitions**: AutoFit=true on startup and after reset. Manual pan/zoom sets AutoFit=false. Window resize recomputes if AutoFit=true.

### VizConfig (Record)

User-customizable visualization settings.

```
VizConfig = {
  BaseLayer: LayerKind
  ActiveOverlays: Set<OverlayKind>
  ColorSchemes: Map<LayerKind, ColorScheme>   // Per-layer color scheme override
  UnitMarkerSize: float32                      // Radius in pixels
  OverlayOpacity: float32                      // 0.0–1.0 for overlay layers
  ShowGridLines: bool
  GridLineSpacing: int                         // In grid cells
  BackgroundColor: SKColor
  LabelColor: SKColor
}
```

**Lifecycle**: Mutable via REPL API or keyboard shortcuts. Persists for session duration.

### UnitState (Record)

Tracked state of a single game unit.

```
UnitState = {
  UnitId: int
  Position: float32 * float32 * float32    // Elmo coordinates (x, y, z)
  TeamId: int
  DefId: int                                // UnitDef ID
  Health: float32
  MaxHealth: float32
  IsEnemy: bool
}
```

**Lifecycle**: Created on UnitCreated/EnemyCreated, updated per frame via callbacks, removed on UnitDestroyed/EnemyDestroyed.

### EventIndicator (Record)

Transient visual indicator for a game event.

```
EventIndicator = {
  Position: float32 * float32 * float32
  Kind: EventKind
  FrameCreated: int
  DurationFrames: int                        // How many frames to display
}

EventKind =
  | UnitCreatedEvent
  | UnitDestroyedEvent
  | EnemySpottedEvent
  | CombatEvent
```

**Lifecycle**: Created on event, rendered for `DurationFrames` frames, then removed.

### GameSnapshot (Record)

Complete game state for a single visualization frame.

```
GameSnapshot = {
  FrameNumber: int
  MapGrid: MapGrid                           // From FSBar.Client.MapGrid
  Units: Map<int, UnitState>                 // unitId → state
  EventIndicators: EventIndicator list       // Active indicators
  EconomyMetal: EconomyData
  EconomyEnergy: EconomyData
  MetalSpots: (float32 * float32 * float32 * float32) array   // From callbacks
  Connected: bool                            // False if session disconnected
}

EconomyData = {
  Current: float32
  Income: float32
  Usage: float32
  Storage: float32
}
```

**Lifecycle**: Built from game frame data each tick. Old snapshot replaced atomically. Render thread reads the latest snapshot.

### VizCommand (Discriminated Union)

Unified command type for all user interactions (keyboard, mouse, REPL).

```
VizCommand =
  | SetBaseLayer of LayerKind
  | ToggleOverlay of OverlayKind
  | Pan of dx: float32 * dy: float32
  | Zoom of factor: float32 * centerX: float32 * centerY: float32
  | ResetView
  | SetColorScheme of LayerKind * ColorScheme
  | SetMarkerSize of float32
  | SetOverlayOpacity of float32
  | ToggleGridLines
  | Stop
```

**Lifecycle**: Produced by InputHandler (keyboard/mouse) or GameViz API (REPL). Consumed by the viewer to update VizConfig/ViewState.

## Entity Relationships

```
GameViz (public API)
├── owns → VizConfig (mutable, lock-guarded)
├── owns → ViewState (mutable, lock-guarded)
├── owns → GameSnapshot (mutable, lock-guarded, updated by game thread)
├── owns → Viewer (Silk.NET window on background thread)
│   ├── reads → GameSnapshot (per frame)
│   ├── reads → VizConfig
│   ├── reads → ViewState
│   └── uses → LayerRenderer, UnitRenderer, SceneBuilder
└── processes → VizCommand (from InputHandler or REPL API)

LayerRenderer
├── reads → MapGrid (from GameSnapshot)
├── reads → ColorScheme (from VizConfig)
└── produces → SKBitmap (cached per LayerKind, invalidated on data change)

UnitRenderer
├── reads → Units, EventIndicators (from GameSnapshot)
├── reads → ViewState (for coordinate transform)
└── draws → SKCanvas (circles, labels, indicators)

SceneBuilder
├── uses → LayerRenderer (base layer bitmap)
├── uses → UnitRenderer (overlay drawing)
├── reads → VizConfig (which overlays active)
└── draws → SKCanvas (composited frame)
```

## Data Flow

```
Game Thread                    Render Thread (background)
───────────                    ──────────────────────────
BarClient.Step()
  → GameFrame received
  → Update GameSnapshot:
    - Refresh LOS/radar in MapGrid
    - Update unit positions via callbacks
    - Process events → EventIndicators
    - Query economy data
    - Atomic write to shared state
                               Viewer.Render callback (60fps):
                                 → Read GameSnapshot (atomic)
                                 → Read VizConfig, ViewState
                                 → LayerRenderer: get/build base bitmap
                                 → SceneBuilder: blit base + draw overlays
                                 → canvas.Flush()
                                 → gl.Flush()

REPL / Keyboard
  → VizCommand
  → Update VizConfig or ViewState
  → Render thread picks up on next frame
```
