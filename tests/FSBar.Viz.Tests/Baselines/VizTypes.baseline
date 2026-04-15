namespace FSBar.Viz

open SkiaSharp
open FSBar.Client

/// Selects which map data layer to render as the base.
[<RequireQualifiedAccess>]
type LayerKind =
    | BaseTerrain
    | HeightMap
    | SlopeMap
    | ResourceMap
    | LosMap
    | RadarMap
    | TerrainClassification
    | Passability of MoveType

/// Toggle-able visual overlays rendered on top of the base layer.
[<RequireQualifiedAccess>]
type OverlayKind =
    | Units
    | Events
    | Grid
    | MetalSpots
    | EconomyHud

/// Transient visual event types for animated indicators.
[<RequireQualifiedAccess>]
type EventKind =
    | UnitCreated
    | UnitDestroyed
    | EnemySpotted
    | Combat

/// Maps a normalized float32 value in [0..1] to a color.
type ColorScheme =
    { Name: string
      MapValue: float32 -> SKColor }

/// Camera/viewport state controlling the current view.
type ViewState =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      WindowWidth: int
      WindowHeight: int
      AutoFit: bool }

/// Visualization configuration record.
type VizConfig =
    { BaseLayer: LayerKind
      ActiveOverlays: Set<OverlayKind>
      ColorSchemes: Map<LayerKind, ColorScheme>
      UnitMarkerSize: float32
      OverlayOpacity: float32
      ShowGridLines: bool
      GridLineSpacing: int
      BackgroundColor: SKColor
      LabelColor: SKColor }

/// Tracked unit snapshot for visualization.
type UnitState =
    { UnitId: int
      PositionX: float32
      PositionY: float32
      PositionZ: float32
      TeamId: int
      DefId: int
      Health: float32
      MaxHealth: float32
      IsEnemy: bool }

/// Transient visual effect with lifecycle.
type EventIndicator =
    { PositionX: float32
      PositionY: float32
      PositionZ: float32
      Kind: EventKind
      FrameCreated: int
      DurationFrames: int }

/// Resource economy snapshot for HUD display.
type EconomyData =
    { Current: float32
      Income: float32
      Usage: float32
      Storage: float32 }

/// Complete game state for one rendered frame.
type GameSnapshot =
    { FrameNumber: int
      MapGrid: MapGrid
      Units: Map<int, UnitState>
      EventIndicators: EventIndicator list
      EconomyMetal: EconomyData
      EconomyEnergy: EconomyData
      MetalSpots: (float32 * float32 * float32 * float32) array
      Connected: bool }

/// User interaction commands.
[<RequireQualifiedAccess>]
type VizCommand =
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

/// Default values for visualization state.
module VizDefaults =
    val defaultViewState: ViewState
    val defaultEconomy: EconomyData
    val defaultConfig: VizConfig
