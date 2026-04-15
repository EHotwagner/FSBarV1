namespace FSBar.Viz

open SkiaSharp
open FSBar.Client

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

[<RequireQualifiedAccess>]
type OverlayKind =
    | Units
    | Events
    | Grid
    | MetalSpots
    | EconomyHud

[<RequireQualifiedAccess>]
type EventKind =
    | UnitCreated
    | UnitDestroyed
    | EnemySpotted
    | Combat

type ColorScheme =
    { Name: string
      MapValue: float32 -> SKColor }

type ViewState =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      WindowWidth: int
      WindowHeight: int
      AutoFit: bool }

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

type EventIndicator =
    { PositionX: float32
      PositionY: float32
      PositionZ: float32
      Kind: EventKind
      FrameCreated: int
      DurationFrames: int }

type EconomyData =
    { Current: float32
      Income: float32
      Usage: float32
      Storage: float32 }

type GameSnapshot =
    { FrameNumber: int
      MapGrid: MapGrid
      Units: Map<int, UnitState>
      EventIndicators: EventIndicator list
      EconomyMetal: EconomyData
      EconomyEnergy: EconomyData
      MetalSpots: (float32 * float32 * float32 * float32) array
      Connected: bool }

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

module VizDefaults =
    let defaultViewState =
        { Scale = 1.0f
          OriginX = 0.0f
          OriginY = 0.0f
          WindowWidth = 1024
          WindowHeight = 640
          AutoFit = true }

    let defaultEconomy =
        { Current = 0.0f
          Income = 0.0f
          Usage = 0.0f
          Storage = 0.0f }

    let defaultConfig =
        { BaseLayer = LayerKind.BaseTerrain
          ActiveOverlays = Set.ofList [ OverlayKind.MetalSpots ]
          ColorSchemes = Map.empty
          UnitMarkerSize = 6.0f
          OverlayOpacity = 0.8f
          ShowGridLines = false
          GridLineSpacing = 16
          BackgroundColor = SKColors.Black
          LabelColor = SKColors.White }
