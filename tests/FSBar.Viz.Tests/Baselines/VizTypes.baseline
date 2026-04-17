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
    // --- Unit-glyph overlays (feature 028-unit-viz-language) ---
    | WeaponRanges
    | SightRanges
    | CommandQueue
    | FullNames

/// Transient visual event types for animated indicators.
[<RequireQualifiedAccess>]
type EventKind =
    | UnitCreated
    | UnitDestroyed
    | EnemySpotted
    | Combat

// --- Unit-glyph types (feature 028-unit-viz-language) ---

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
      LabelColor: SKColor
      // --- Unit-glyph renderer (feature 028-unit-viz-language) ---
      UseGlyphRenderer: bool
      GlyphStyle: UnitGlyphStyle }

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
      /// BarData-enriched unit displays. When non-empty, SceneBuilder uses these
      /// instead of the legacy UnitState→UnitDisplay adapter.
      DisplayUnits: Map<int, UnitDisplay>
      EventIndicators: EventIndicator list
      EconomyMetal: EconomyData
      EconomyEnergy: EconomyData
      MetalSpots: (float32 * float32 * float32 * float32) array
      Connected: bool }

/// State for the configurator side panel (feature 033-viz-style-configurator).
/// ExpandedSections uses string names of AttributeCategory cases (kept as
/// strings here so VizTypes remains independent of ConfigDescriptors).
type ConfigPanelState =
    { IsOpen: bool
      ScrollOffset: float32
      ExpandedSections: Set<string>
      ActiveControl: string option
      DirtyIndicator: bool }

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

// `VizDefaults` has moved to VizDefaults.fsi — it consumes `UnitGlyphPalettes`
// (feature 028-unit-viz-language), which compiles after this file.
