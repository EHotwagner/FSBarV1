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
    | WeaponRanges
    | SightRanges
    | CommandQueue
    | FullNames

[<RequireQualifiedAccess>]
type EventKind =
    | UnitCreated
    | UnitDestroyed
    | EnemySpotted
    | Combat

[<RequireQualifiedAccess>]
type MovementShape =
    | Bot
    | Vehicle
    | Hover
    | Ship
    | Air
    | Building
    | Unknown

[<RequireQualifiedAccess>]
type Tier =
    | T1
    | T2
    | T3

[<RequireQualifiedAccess>]
type FactionId =
    | Armada
    | Cortex
    | Legion
    | Raptors
    | Scavengers
    | Neutral

[<RequireQualifiedAccess>]
type OrderKind =
    | Move
    | Attack
    | Patrol
    | Guard
    | Build
    | Reclaim
    | Other

type StatusFlags =
    { IsUnderConstruction: bool
      IsStunned: bool
      JustDamagedWithinMs: int option
      JustCompletedWithinMs: int option
      IsCloaked: bool }

type CommandWaypoint =
    { Order: OrderKind
      X: float32
      Y: float32
      Z: float32
      IsCurrent: bool }

type UnitDisplay =
    { UnitId: int
      DefId: int
      InternalName: string
      Shape: MovementShape
      Faction: FactionId
      Tier: Tier
      LabelCode: string
      FootprintWidthElmo: float32
      FootprintHeightElmo: float32
      TeamId: int
      PositionX: float32
      PositionY: float32
      PositionZ: float32
      HeadingRadians: float32
      CurrentHealth: float32
      MaxHealth: float32
      BuildProgress: float32
      Status: StatusFlags
      WeaponRangesElmo: float32 list
      SightRangeElmo: float32
      BuildRangeElmo: float32 option
      CommandQueue: CommandWaypoint list }

type FactionPalette =
    { Armada: SKColor
      Cortex: SKColor
      Legion: SKColor
      Raptors: SKColor
      Scavengers: SKColor
      Neutral: SKColor }

type TeamPalette =
    { ByTeamId: Map<int, SKColor>
      Fallback: SKColor }

type UnitGlyphStyle =
    { FactionPalette: FactionPalette
      TeamPalette: TeamPalette
      MinPixelRadius: float32
      T1StrokeWidth: float32
      T2StrokeWidth: float32
      T3StrokeWidth: float32
      FacingPipRadius: float32
      HpArcWidth: float32
      LowHpFraction: float32
      LabelFontSizePx: float32
      LabelLegibilityZoomThreshold: float32
      EventFlashDurationMs: int
      JustBuiltRingDurationMs: int }

[<RequireQualifiedAccess>]
type EventEffectKind =
    | UnderAttackFlash
    | JustBuiltRing
    | StunnedDesaturate

type EventEffect =
    { UnitId: int
      Kind: EventEffectKind
      StartedAtMs: int
      DurationMs: int }

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
      LabelColor: SKColor
      UseGlyphRenderer: bool
      GlyphStyle: UnitGlyphStyle }

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

// `VizDefaults` lives in its own file (VizDefaults.fs) so that
// `defaultConfig.GlyphStyle` can reference `UnitGlyphPalettes.defaults`
// without a forward reference.
