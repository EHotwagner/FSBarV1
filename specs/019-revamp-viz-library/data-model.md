# Data Model: Revamp Viz Library with Declarative SkiaViewer

**Branch**: `019-revamp-viz-library` | **Date**: 2026-04-10

## Entities

### LayerKind (unchanged)

Discriminated union selecting which map data layer to render as the base.

- HeightMap, SlopeMap, ResourceMap, LosMap, RadarMap, TerrainClassification
- Passability of MoveType (Kbot | Tank | Hover | Ship)

**Relationships**: Used by VizConfig.BaseLayer, LayerRenderer.renderLayer, ColorMaps.colorSchemeFor

### OverlayKind (unchanged)

Discriminated union for toggle-able visual overlays.

- Units, Events, Grid, MetalSpots, EconomyHud

**Relationships**: Used by VizConfig.ActiveOverlays (Set<OverlayKind>)

### ColorScheme (unchanged)

Functional mapping from normalized float32 [0..1] to SKColor.

- Name: string
- MapValue: float32 -> SKColor

**Relationships**: Used by VizConfig.ColorSchemes (Map<LayerKind, ColorScheme>), LayerRenderer

### ViewState (unchanged)

Camera/viewport state.

- Scale: float32 (zoom level, 1.0 = 1:1)
- OriginX, OriginY: float32 (top-left corner in map coordinates)
- WindowWidth, WindowHeight: int (viewport pixel dimensions)
- AutoFit: bool (auto-center/scale to fit map)

**Relationships**: Used by SceneBuilder to compute viewport Transform, updated by pan/zoom/resize events

### VizConfig (unchanged)

Visualization configuration record.

- BaseLayer: LayerKind
- ActiveOverlays: Set<OverlayKind>
- ColorSchemes: Map<LayerKind, ColorScheme>
- UnitMarkerSize: float32
- OverlayOpacity: float32 (0.0..1.0)
- ShowGridLines: bool
- GridLineSpacing: int
- BackgroundColor: SKColor
- LabelColor: SKColor

**Relationships**: Central configuration consumed by SceneBuilder, GameViz, PreviewSession

### EventKind (unchanged)

Discriminated union for transient visual event types.

- UnitCreated, UnitDestroyed, EnemySpotted, Combat

**Relationships**: Used by EventIndicator.Kind

### UnitState (unchanged)

Tracked unit snapshot for visualization.

- UnitId: int
- PositionX, PositionY, PositionZ: float32 (world coordinates in elmos)
- TeamId: int
- DefId: int
- Health, MaxHealth: float32
- IsEnemy: bool

**Relationships**: Contained in GameSnapshot.Units (Map<int, UnitState>)

### EventIndicator (unchanged)

Transient visual effect with lifecycle.

- PositionX, PositionY, PositionZ: float32
- Kind: EventKind
- FrameCreated: int
- DurationFrames: int

**Relationships**: Contained in GameSnapshot.EventIndicators (list)
**State transitions**: Created on game event → Active (animated over DurationFrames) → Expired (removed)

### EconomyData (unchanged)

Resource economy snapshot.

- Current, Income, Usage, Storage: float32

**Relationships**: GameSnapshot.EconomyMetal, GameSnapshot.EconomyEnergy

### GameSnapshot (unchanged)

Complete game state for one rendered frame.

- FrameNumber: int
- MapGrid: MapGrid
- Units: Map<int, UnitState>
- EventIndicators: EventIndicator list
- EconomyMetal, EconomyEnergy: EconomyData
- MetalSpots: (float32 * float32 * float32 * float32) array
- Connected: bool

**Relationships**: Built by GameViz.onFrame or test code; consumed by SceneBuilder.buildScene

### VizCommand (unchanged)

User interaction command.

- SetBaseLayer of LayerKind
- ToggleOverlay of OverlayKind
- Pan of dx * dy
- Zoom of factor * centerX * centerY
- ResetView
- SetColorScheme of LayerKind * ColorScheme
- SetMarkerSize of float32
- SetOverlayOpacity of float32
- ToggleGridLines
- Stop

**Relationships**: Generated from InputEvent processing; applied to VizConfig/ViewState

### LiveSessionHandle (unchanged)

Session lifecycle handle.

- FrameCount: int (read-only)
- IsRunning: bool (read-only)
- LastError: string option (read-only)
- IDisposable

**Relationships**: Returned by LiveSession.start/startWithClient

## New Internal State (not public types)

### AnimationState (new, internal to SceneBuilder)

Per-indicator animation progress tracking.

- Progress: float32 (0.0..1.0, computed from FrameTick elapsed time)
- Used to drive: ring radius expansion, opacity fade, glow intensity

**Note**: This is internal state, not exposed in .fsi. SceneBuilder computes animation parameters from EventIndicator.FrameCreated + DurationFrames relative to current GameSnapshot.FrameNumber.

## Validation Rules

- ViewState.Scale must be > 0
- VizConfig.OverlayOpacity clamped to [0.0, 1.0]
- VizConfig.UnitMarkerSize must be > 0
- EconomyData.Current clamped to [0.0, Storage]
- EventIndicator.DurationFrames must be > 0
- GameSnapshot.MapGrid dimensions must be consistent (HeightMap is (h+1) × (w+1), others are h × w)
