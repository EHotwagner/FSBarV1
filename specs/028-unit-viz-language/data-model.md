# Phase 1: Data Model — Unit Visual Representation for SkiaViewer

**Feature**: 028-unit-viz-language
**Date**: 2026-04-15

This document describes the types added or extended for the feature. All types are declared in `.fsi` signatures under `src/FSBar.Viz/` with full structural contracts. Types are listed in dependency order.

---

## 1. Enumerations

### `MovementShape`

The six silhouette categories the renderer can draw. Derived by `UnitGlyph.classifyShape` from `BarData.UnitDef`. One shape is a fallback for unknown classes.

```fsharp
[<RequireQualifiedAccess>]
type MovementShape =
    | Bot          // circle
    | Vehicle      // square
    | Hover        // diamond
    | Ship         // rounded rectangle
    | Air          // triangle
    | Building     // hexagon
    | Unknown      // neutral fallback silhouette
```

Validation: classification is total; no panics. `Unknown` is emitted exactly once per distinct `movementClass` seen, with a structured log event naming the unit.

### `Tier`

Derived by reading `customParams["techlevel"]` → fallback to `category` `LEVEL{1,2,3}` scan → default T1.

```fsharp
[<RequireQualifiedAccess>]
type Tier = T1 | T2 | T3
```

### `FactionId`

Derived from `subfolder` second path segment → fallback to name prefix → `Neutral`.

```fsharp
[<RequireQualifiedAccess>]
type FactionId =
    | Armada
    | Cortex
    | Legion
    | Raptors
    | Scavengers
    | Neutral
```

### `StatusFlags`

Bitset-style record carrying the per-unit transient state that drives automatic event effects. Populated by the data source (synthetic or, later, live).

```fsharp
type StatusFlags = {
    IsUnderConstruction: bool
    IsStunned: bool
    JustDamagedWithinMs: int option   // None = not recently damaged
    JustCompletedWithinMs: int option // None = not a fresh build
    IsCloaked: bool
}
```

---

## 2. Core display record

### `UnitDisplay`

The input to the renderer for a single unit. Merges static `BarData` facts (cached per `DefId`) with live per-frame state. Constructed by the data source (MVP: `FSBar.SyntheticData`; follow-up: a `TrackedUnit`→`UnitDisplay` adapter).

```fsharp
type UnitDisplay = {
    // Identity
    UnitId: int
    DefId: int
    InternalName: string

    // Static classification (cached from BarData)
    Shape: MovementShape
    Faction: FactionId
    Tier: Tier
    LabelCode: string        // 2- or 3-char code from UnitLabels table

    // Static footprint (from BarData footprintX/Z, already in elmo units)
    FootprintWidthElmo: float32
    FootprintHeightElmo: float32

    // Live per-frame state
    TeamId: int
    PositionX: float32
    PositionY: float32
    PositionZ: float32
    HeadingRadians: float32  // 0 = +X, CCW positive
    CurrentHealth: float32
    MaxHealth: float32
    BuildProgress: float32   // 0.0 = just placed, 1.0 = finished
    Status: StatusFlags

    // Optional per-unit overlay data (filled only when the owning data source has it)
    WeaponRangesElmo: float32 list    // max range per weapon
    SightRangeElmo: float32
    BuildRangeElmo: float32 option
    CommandQueue: CommandWaypoint list
}
```

Validation:
- `MaxHealth ≥ 0`. If `MaxHealth = 0`, renderer suppresses the HP arc and low-HP shader (edge case in spec).
- `CurrentHealth ≤ MaxHealth` is expected but not enforced; the renderer clamps if violated and logs once.
- `BuildProgress ∈ [0, 1]`. Renderer clamps.
- `HeadingRadians` is real-valued; if `nan`, the renderer draws the pip at a fixed default angle and applies the "facing unknown" visual flag.
- `LabelCode` must be 2 or 3 characters. Guaranteed by `UnitLabels` lookup.

### `CommandWaypoint`

```fsharp
[<RequireQualifiedAccess>]
type OrderKind = Move | Attack | Patrol | Guard | Build | Reclaim | Other

type CommandWaypoint = {
    Order: OrderKind
    X: float32
    Y: float32
    Z: float32
    IsCurrent: bool    // true for the order the unit is executing now
}
```

---

## 3. Palette records

### `FactionPalette` and `TeamPalette`

Independent color tables so faction reads survive any team-color configuration and vice versa.

```fsharp
type FactionPalette = {
    Armada: SKColor
    Cortex: SKColor
    Legion: SKColor
    Raptors: SKColor
    Scavengers: SKColor
    Neutral: SKColor
}

/// Lookup from raw teamId → fill color.
type TeamPalette = {
    ByTeamId: Map<int, SKColor>
    Fallback: SKColor
}
```

### `UnitGlyphStyle`

The per-feature visual tuning knob. Consumers can override at `VizConfig` level.

```fsharp
type UnitGlyphStyle = {
    FactionPalette: FactionPalette
    TeamPalette: TeamPalette
    MinPixelRadius: float32             // FR-002 clamp at low zoom
    T1StrokeWidth: float32
    T2StrokeWidth: float32
    T3StrokeWidth: float32
    FacingPipRadius: float32
    HpArcWidth: float32
    LowHpFraction: float32              // default 0.25
    LabelFontSizePx: float32
    LabelLegibilityZoomThreshold: float32
    EventFlashDurationMs: int
    JustBuiltRingDurationMs: int
}
```

Module `UnitGlyphPalettes` exposes a `defaults: UnitGlyphStyle` value with sensible initial values.

---

## 4. Overlay state

### Toggle set

Extends the existing `OverlayKind` enum in `VizTypes.fsi` with the four MVP overlay kinds. The legacy `OverlayKind.Units` / `OverlayKind.Events` etc. are retained.

```fsharp
// VizTypes.fsi (extended)
[<RequireQualifiedAccess>]
type OverlayKind =
    // existing
    | Units
    | Events
    | Grid
    | MetalSpots
    | EconomyHud
    // new (MVP)
    | WeaponRanges       // W
    | SightRanges        // L
    | CommandQueue       // C
    | FullNames          // N
```

Deferred overlays (`R E B T V I X`) are NOT added to this enum in MVP. Adding them later is an additive change.

### `VizConfig` delta

Additive fields on the existing record:

```fsharp
// VizConfig extended:
    UseGlyphRenderer: bool          // feature flag (default true)
    GlyphStyle: UnitGlyphStyle
```

All existing consumers continue to compile; `VizDefaults.defaultConfig` is updated to include `UseGlyphRenderer = true` and `GlyphStyle = UnitGlyphPalettes.defaults`.

---

## 5. Event effects

### `EventEffect`

Transient per-unit visual animation, driven by frame-delta triggers. Lives in the renderer only; not part of the data source input.

```fsharp
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
```

Lifecycle: detected by comparing successive frames (HP decreased → `UnderAttackFlash`; `IsUnderConstruction` transitioned false → `JustBuiltRing`; `IsStunned = true` → `StunnedDesaturate` that persists while the flag holds). Each effect removes itself when `(nowMs - StartedAtMs) ≥ DurationMs` or when the underlying state resolves. Stored in a module-private mutable list scoped to the renderer session, flushed on session start/stop.

---

## 6. Label table

### `UnitLabels.generated.fs` (generated file)

Exposes one public value:

```fsharp
// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.
module FSBar.Viz.UnitLabels

val BarDataVersion: string           // e.g. "1.0.3"
val GeneratedAtUtc: string           // ISO 8601
val Labels: Map<string, string>      // internalName → 2- or 3-char code

val tryLookup: internalName: string -> string option
val lookupOrFallback: internalName: string -> string   // returns "??" on miss
```

Contract:
- `Labels` contains no duplicate values.
- Every value is 2 or 3 characters long.
- ≥ 90% of values are exactly 2 characters (SC-002).
- Regenerating against the same `BarData` version produces a byte-identical file.

---

## 7. Relationships

```text
BarData.UnitDef  ─┐                          SyntheticData.Scene
                  │                                 │
                  ▼                                 ▼
           UnitGlyph.classifyShape            UnitDisplay (per unit, per frame)
                  │                                 │
                  ▼                                 ▼
         MovementShape + Tier + Faction       ┌──────────────┐
                  │                           │  UnitGlyph.  │
                  │                           │  buildScene  │
         UnitLabels.Labels[name]              └──────┬───────┘
                  │                                  │
                  └──────────────┬───────────────────┘
                                 ▼
                   SkiaViewer.Scene (primitives)
```

The classifier and the label table are evaluated once per `DefId`; results are cached in a `ConcurrentDictionary<int, UnitGlyphStaticCache>` keyed by `DefId` inside `UnitGlyph`. The per-frame renderer only sees `UnitDisplay` (which already has `Shape`, `Faction`, `Tier`, `LabelCode` populated) and does no `BarData` lookups on the hot path.

---

## 8. State transitions

### Construction → Operational

```text
buildProgress ∈ [0, 1)        buildProgress = 1        (no transition — unit is gone)
    │                              │
    ▼                              ▼
dashed stroke,                solid stroke,       JustBuiltRing effect
alpha ∝ progress              alpha = 1           (1 s green ring fade)
```

### Operational → Under Attack → Low HP → Destroyed

```text
health = max                HP decrease detected        health < 0.25 × max
    │                            │                           │
    ▼                            ▼                           ▼
no HP arc              HP arc drawn                 HP arc drawn in red
                       red stroke flash (300 ms)    + low-HP noise shader
                                                    + continuous red pulse on damage
```

### Operational ↔ Stunned

```text
Status.IsStunned = false                 Status.IsStunned = true
         │                                        │
         ▼                                        ▼
full saturation                           desaturated + slow pulse
```

All transitions are observed by comparing the previous frame's `UnitDisplay` with the current frame's `UnitDisplay`; no in-engine events are consumed.

---

## 9. What does NOT change

- `FSBar.Client.GameState.TrackedUnit` — untouched. The adapter that maps it to `UnitDisplay` is out of scope for this feature (follow-up).
- `FSBar.SyntheticData.Scene` — structurally unchanged, but `UnitSim` may gain deterministic synthetic heading and buildProgress values so the renderer has data to display.
- `SkiaViewer` — no changes. All new rendering uses the existing `Scene` API.
- Existing `OverlayKind` values and their legacy rendering paths in `SceneBuilder.fs`.
