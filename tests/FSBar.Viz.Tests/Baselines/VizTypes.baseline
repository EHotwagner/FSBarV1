namespace FSBar.Viz

open SkiaSharp
open FSBar.Client

/// Identifies which base map layer to render.
[<RequireQualifiedAccess>]
type LayerKind =
    | HeightMap
    | SlopeMap
    | ResourceMap
    | LosMap
    | RadarMap
    | TerrainClassification
    | Passability of MoveType

/// Identifies toggleable overlay layers drawn on top of the base layer.
[<RequireQualifiedAccess>]
type OverlayKind =
    | Units
    | Events
    | Grid
    | MetalSpots
    | EconomyHud

/// Defines how scalar values in [0..1] map to colors for a given layer.
type ColorScheme =
    { Name: string
      MapValue: float32 -> SKColor }

/// Camera/viewport state for the map view.
type ViewState =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      WindowWidth: int
      WindowHeight: int
      AutoFit: bool }

/// User-customizable visualization settings.
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

/// Kind of visual event indicator.
[<RequireQualifiedAccess>]
type EventKind =
    | UnitCreated
    | UnitDestroyed
    | EnemySpotted
    | Combat

/// Tracked state of a single game unit.
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

/// Transient visual indicator for a game event.
type EventIndicator =
    { PositionX: float32
      PositionY: float32
      PositionZ: float32
      Kind: EventKind
      FrameCreated: int
      DurationFrames: int }

/// Economy data for a single resource.
type EconomyData =
    { Current: float32
      Income: float32
      Usage: float32
      Storage: float32 }

/// Complete game state for a single visualization frame.
type GameSnapshot =
    { FrameNumber: int
      MapGrid: MapGrid
      Units: Map<int, UnitState>
      EventIndicators: EventIndicator list
      EconomyMetal: EconomyData
      EconomyEnergy: EconomyData
      MetalSpots: (float32 * float32 * float32 * float32) array
      Connected: bool }

/// Unified command type for all user interactions.
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

/// Default configuration values.
module VizDefaults =
    val defaultViewState: ViewState
    val defaultEconomy: EconomyData
    val defaultConfig: VizConfig
