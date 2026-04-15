// Additive delta to src/FSBar.Viz/VizTypes.fsi.
// The final VizTypes.fsi keeps every existing declaration and adds the
// types below. No existing type or value is removed or renamed; the
// surface-area baseline is updated additively.

namespace FSBar.Viz

open SkiaSharp

// --- New enums ---

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

// --- New records ---

type StatusFlags = {
    IsUnderConstruction: bool
    IsStunned: bool
    JustDamagedWithinMs: int option
    JustCompletedWithinMs: int option
    IsCloaked: bool
}

type CommandWaypoint = {
    Order: OrderKind
    X: float32
    Y: float32
    Z: float32
    IsCurrent: bool
}

type UnitDisplay = {
    UnitId: int
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
    CommandQueue: CommandWaypoint list
}

type FactionPalette = {
    Armada: SKColor
    Cortex: SKColor
    Legion: SKColor
    Raptors: SKColor
    Scavengers: SKColor
    Neutral: SKColor
}

type TeamPalette = {
    ByTeamId: Map<int, SKColor>
    Fallback: SKColor
}

type UnitGlyphStyle = {
    FactionPalette: FactionPalette
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
    JustBuiltRingDurationMs: int
}

[<RequireQualifiedAccess>]
type EventEffectKind =
    | UnderAttackFlash
    | JustBuiltRing
    | StunnedDesaturate

type EventEffect = {
    UnitId: int
    Kind: EventEffectKind
    StartedAtMs: int
    DurationMs: int
}

// --- OverlayKind extension (additive cases) ---
// The existing enum gains four new cases. Existing cases are unchanged.
//
// [<RequireQualifiedAccess>]
// type OverlayKind =
//     | Units
//     | Events
//     | Grid
//     | MetalSpots
//     | EconomyHud
//     | WeaponRanges      // NEW (W)
//     | SightRanges       // NEW (L)
//     | CommandQueue      // NEW (C)
//     | FullNames         // NEW (N)

// --- VizConfig extension (additive fields) ---
// The existing VizConfig record gains:
//     UseGlyphRenderer: bool
//     GlyphStyle: UnitGlyphStyle
// All other fields unchanged. VizDefaults.defaultConfig is updated to
// supply UseGlyphRenderer = true and GlyphStyle = UnitGlyphPalettes.defaults.
