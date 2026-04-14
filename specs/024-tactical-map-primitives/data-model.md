# Phase 1 Data Model — 024 Tactical Map Primitives

**Branch**: `024-tactical-map-primitives`
**Date**: 2026-04-13
**Scope**: F# record / DU shapes for every entity in the spec §Key Entities. These are advisory type sketches — the authoritative signatures live in `contracts/*.md` and in the final `.fsi` files. Consistent naming across modules is enforced here.

---

## 1. `SmfParser` — native Spring Map File reader

```fsharp
namespace FSBar.Client

/// A parsed Spring Map File. Structurally compatible with MapGrid so
/// downstream primitives can consume either source without branching.
type SmfMap = {
    /// Map dimensions in heightmap squares (1 square = 8 elmos).
    WidthHeightmap: int
    HeightHeightmap: int
    /// World-space dimensions in elmos (= heightmap × 8).
    WidthElmos: int
    HeightElmos: int
    /// Corners heightmap: (WidthHeightmap+1) × (HeightHeightmap+1) float32.
    /// Matches Callbacks.getCornersHeightMap output shape exactly.
    HeightMap: float32[,]
    /// Computed slope map: (WidthHeightmap/2) × (HeightHeightmap/2) float32.
    /// Derived locally via the Spring-engine formula — see research R3.
    SlopeMap: float32[,]
    /// Metal map (raw SMF byte values, 0-255). Dimensions:
    /// (WidthHeightmap/2) × (HeightHeightmap/2).
    MetalMap: uint8[,]
    /// Type map (terrain type indices, 0-255). Used only diagnostically
    /// for now; future features may use it for pathing cost weighting.
    TypeMap: uint8[,]
    /// Path to the .sd7 archive the map was parsed from (for logging /
    /// diagnostics only; tests assert this matches the input).
    SourceArchive: string
}
```

**Validation rules**:
- `WidthHeightmap × HeightHeightmap > 0` (non-empty map).
- `HeightMap.GetLength(0) = WidthHeightmap + 1`, `HeightMap.GetLength(1) = HeightHeightmap + 1`.
- `SlopeMap.GetLength(0) = WidthHeightmap / 2`, `SlopeMap.GetLength(1) = HeightHeightmap / 2`.
- `MetalMap` and `TypeMap` share dimensions with `SlopeMap`.
- All `HeightMap` values finite (no NaN / infinity) after parse.

**Conversion**: `SmfMap.toMapGrid : SmfMap → MapGrid` emits a value structurally indistinguishable from `MapGrid.loadFromEngine`'s output for downstream consumers. LOS / radar layers are zero-initialised (not available from an offline SMF — fine for static analysis).

---

## 2. `Pathing` — A* with slope-weighted edges and own-structures mask

```fsharp
namespace FSBar.Client

/// Footprint of a placed own structure, used as an impassable mask
/// overlay when pathing. Called by both Pathing and WallIn.
type OwnStructureFootprint = {
    /// World-coordinate centre of the structure.
    Centre: float32 * float32 * float32
    /// Footprint radius in elmos (covers a circular cell region around
    /// Centre). For non-square structures use the bounding circle.
    RadiusElmos: float32
    /// Optional free-form tag for diagnostics (def name, slot label).
    Tag: string option
}

type PathStatus =
    /// The goal was reached within budget.
    | Complete
    /// The search hit the wall-clock / node-count budget before
    /// finding the goal. Waypoints contain the best partial path
    /// toward the lowest-f-score frontier node.
    | Partial of budgetExhausted:bool

type PathFailure =
    /// start or goal cell is outside the map.
    | OutOfBounds
    /// Neither start nor goal is passable for the given MoveType.
    | EndpointImpassable
    /// A search completed within budget but no route exists.
    | NoRoute

type Path = {
    /// Ordered world-coordinate waypoints, start → goal.
    /// Invariant: any two consecutive waypoints are connected by a
    /// straight line passable for the MoveType used to find this path
    /// (so the caller can emit MoveCommand between consecutive waypoints).
    Waypoints: (float32 * float32 * float32) array
    /// Total estimated traversal cost (sum of edge weights).
    EstimatedCost: float32
    Status: PathStatus
}

type PathBudget = {
    /// Maximum wall-clock time before search aborts with Partial.
    WallClockMs: int
    /// Maximum cell expansions before search aborts with Partial.
    MaxExpansions: int
    /// Slope cost multiplier: edge cost = distance × (1 + slope × this).
    SlopeCost: float32
}

/// Default: 50 ms wall-clock, 50_000 expansions, slopeCost = 2.0.
val defaultPathBudget : PathBudget
```

**Invariants**:
- `findPath` is pure over `(grid, moveType, ownStructures, start, goal, budget)` — same inputs → same `Result<Path, PathFailure>`.
- Waypoint count > 1 for a successful path; a single-waypoint "start==goal" case returns `Ok Path { Waypoints = [| goal |]; EstimatedCost = 0.0f; Status = Complete }`.
- `Waypoints[0]` ≈ start (snapped to cell centre); `Waypoints[^1]` = goal (snapped).
- `Status = Partial` implies `Waypoints.Length ≥ 1` but goal may not be reached.

---

## 3. `Chokepoints` — distance-transform ridge detection

```fsharp
namespace FSBar.Client

/// Stable identifier for a chokepoint, derived deterministically from
/// the (MapGrid-version, ridge-cell-index) tuple. Two calls to
/// findChokepoints with the same inputs produce the same IDs.
type ChokepointId = ChokepointId of uint32

type Chokepoint = {
    Id: ChokepointId
    /// Representative world position at the narrowest point of the
    /// corridor.
    Position: float32 * float32 * float32
    /// Estimated width of the corridor at the narrowest point, in elmos.
    WidthElmos: float32
    /// Unit vector pointing outward from baseCentre through the
    /// chokepoint (normalised).
    OutwardDir: float32 * float32
    /// Straight-line distance from baseCentre to Position.
    DistanceFromBase: float32
}

type ChokepointQuery = {
    /// Maximum width (elmos) for a passage to qualify as a chokepoint.
    /// Default 40.0f (5 heightmap cells).
    MaxWidthElmos: float32
    /// Search radius in elmos from baseCentre.
    /// Default 2500.0f.
    SearchRadiusElmos: float32
    /// MoveType used to evaluate passability (see MapGrid.passability).
    MoveType: MoveType
}

val defaultChokepointQuery : MoveType -> ChokepointQuery
```

**Invariants**:
- Returned list is sorted by `DistanceFromBase` ascending (closest first).
- Empty list when no chokepoints satisfy `MaxWidthElmos AND primary-route` (FR-009/FR-010).
- IDs are stable across calls with the same `(grid, baseCentre, query)` (FR-011).

---

## 4. `BasePlan` — declarative structure layout

```fsharp
namespace FSBar.Client

/// Where should this slot's structure be placed? Each chooser is
/// resolved by the plan resolver against a MapGrid + collection of
/// already-placed structures.
type PositionChooser =
    /// Pick the nth nearest free metal spot to baseCentre.
    | NearestMetalSpot of spotIndex:int
    /// Pick an offset (dx, dz) from the current commander position.
    | NearCommander of dx:float32 * dz:float32
    /// Pick an offset (dx, dz) from a pinned base centre.
    | NearBaseCentre of dx:float32 * dz:float32
    /// Place at the head (inward-facing side) of a specific chokepoint.
    | AtChokepointHead of chokepointIndex:int
    /// Place at a literal absolute world position. Used sparingly,
    /// mostly for diagnostic plans and tests.
    | AtLiteralPosition of x:float32 * y:float32 * z:float32

/// One structure slot in a named plan.
type PlanSlot = {
    /// Short symbolic name — e.g. "mex#1", "opening-factory".
    Name: string
    /// Structure def name (e.g. "armmex", "armsolar", "armlab").
    DefName: string
    /// How to pick the world position.
    Chooser: PositionChooser
    /// Def name of the builder required. "commander" | "armck" | etc.
    BuilderDefName: string
    /// Edge-to-edge clearance margin in elmos — empty space between
    /// this slot's footprint and any other placed/resolved structure's
    /// footprint. See clarification Q4.
    ClearanceMargin: float32
    /// Max retries if placement fails; beyond this the slot is marked
    /// unfulfillable.
    MaxRetries: int
}

/// A named, reusable base plan. Input to resolvePlan.
type BasePlan = {
    Name: string
    /// Human-readable strategy tag ("armada-opening", "turtle",
    /// "turtle-with-chokepoint-defence", …).
    Strategy: string
    Slots: PlanSlot list
}

/// Reason a slot failed to resolve. Consumed by the caller for
/// diagnostic logging and retry decisions.
type SlotFailure =
    | TerrainNotBuildable of reason:string
    | ClearanceCollision of againstSlotName:string
    | OutOfBuilderReach of builderDefName:string * distanceElmos:float32
    | OffMap
    | WouldWallIn of unreachableStructureNames:string list
    | UnresolvedDependency of chokepointIndex:int  // for AtChokepointHead
    | NoMetalSpot of requestedIndex:int
    | RetryBudgetExhausted of lastReason:string

/// Output of resolvePlan — one record per input slot.
type ResolvedSlot = {
    Slot: PlanSlot
    /// Concrete resolution. Failure leaves this at None.
    Position: (float32 * float32 * float32) option
    /// True when the slot is buildable by its declared builder given
    /// the current MapGrid and placed-structure set.
    BuildableNow: bool
    /// None on success; Some reason on failure.
    Failure: SlotFailure option
}

/// Plan-consumption state. Immutable; the caller replaces it after
/// each incremental resolvePlan call.
type PlanProgress = {
    /// Slot names already consumed (structure built or under construction).
    ConsumedSlots: Set<string>
    /// Slot names previously resolved but not yet consumed (in flight).
    InFlight: Set<string>
    /// Slots marked permanently unfulfillable (no retry).
    Unfulfillable: Map<string, SlotFailure>
}

/// Empty progress — start of a match.
val emptyPlanProgress : PlanProgress
```

**Invariants**:
- `resolvePlan` returns one `ResolvedSlot` per `PlanSlot` in the input order (no reordering).
- A slot with `Failure.IsSome` has `Position = None` and `BuildableNow = false`.
- `resolvePlan` is pure — no mutation of `progress`, caller-supplied structure list, or `MapGrid`.
- `PlanSlot.ClearanceMargin` is enforced edge-to-edge (not centre-to-centre) per clarification Q4.
- Every resolved slot is internally anti-wall-in-checked (FR-023): a slot whose placement would disconnect any existing structure from base centre gets `Failure = Some (WouldWallIn _)`.

---

## 5. `WallIn` — connectivity predicate

```fsharp
namespace FSBar.Client

type WallInReason =
    /// Proposed placement disconnects baseCentre from the given structures.
    | DisconnectsStructures of names:string list
    /// Proposed placement creates an enclosed pocket around baseCentre
    /// (all exits blocked).
    | EnclosesBase

type WallInResult =
    /// Placement is safe — base remains connected to every existing
    /// structure and to at least one map-edge exit.
    | Passes
    /// Placement would wall in. Diagnostic payload included.
    | Fails of reason:WallInReason

type WallInQuery = {
    /// MoveType used for connectivity semantics (usually Kbot or Hover,
    /// matching the commander's movement).
    MoveType: MoveType
    /// Should the check also require at least one passable route from
    /// baseCentre to the map edge? Default true.
    RequireMapEdgeExit: bool
}

val defaultWallInQuery : WallInQuery
```

**Invariants**:
- `wouldWallIn` is pure over `(grid, baseCentre, ownStructures, proposed, query)`.
- A placement that passes the check MUST also be a valid `findPath` target for every currently-placed structure (shared passability rules per FR-020).
- `Passes` ⇒ there is at least one route from `baseCentre` to every structure in `ownStructures` that doesn't pass through `proposed`, AND (if `RequireMapEdgeExit`) at least one route from `baseCentre` to the nearest map edge that doesn't pass through `proposed`.
- `Fails` MUST include enough information for a `[wall-in-defect]` stdout line (FR-021).

---

## 6. Integration shapes (consumed by `bot_macro.fsx`)

These are not new F# entities; they're how the existing 023 records in `bot_macro.fsx` and its helpers evolve when US5 ships. Documented here so the plan → tasks → implementation chain is traceable.

**Opening integration**:

```fsharp
// 023 baseline (after extraction of opening_build.fsx):
let mutable openingProgress : OpeningProgress = emptyProgress

// 024 deep integration (US5):
let mutable planProgress : PlanProgress = emptyPlanProgress
let mutable currentResolution : ResolvedSlot list = []
// Opening_build.fsx is replaced by BasePlan / resolvePlan calls against
// FSBar.Client.BasePlan.defaultArmadaOpening.
```

**Attack integration**:

```fsharp
// 023 baseline:
combatUnitsLaunched |> ... MoveCommand uid tx 100.0f tz

// 024 deep integration:
let path = Pathing.findPath mapGrid Kbot ownStructures factoryPos enemyCommanderPos defaultPathBudget
match path with
| Ok p ->
    // Issue MoveCommand between consecutive waypoints per unit
    for uid in freshCombat do
        for (wx, wy, wz) in p.Waypoints do
            yield MoveCommand uid wx wy wz
| Error _ ->
    // Fall back to straight-line MoveCommand (023 behaviour)
    yield MoveCommand uid tx 100.0f tz
```

**Defend integration**:

```fsharp
// 023 baseline:
match nearestEnemyId client.GameState centre intruders with
| Some targetEid -> [ for uid in myIds -> AttackCommand uid targetEid ]

// 024 deep integration:
let chokepoints = Chokepoints.findChokepoints mapGrid Kbot baseCentre 2500.0f
match chokepoints, intruders with
| cp :: _, _ when Set.count intruders > 0 ->
    // Interpose combat units at the chokepoint Position
    for uid in combatUnits do yield MoveCommand uid cp.Position
| _, _ -> // no chokepoint → fall back to 023 nearest-intruder attack
    ...
```

---

## 7. Extension notes

- **`MapGrid` / `MapQuery`** are **unchanged**. The new modules consume them as-is.
- **`UnitDefCache`** is unchanged; `BasePlan` resolves `DefName` / `BuilderDefName` via `UnitDefCache.tryFindByName` the same way 023's `opening_build.fsx` does.
- **Existing 023 helpers** (`opening_build.fsx`, `production_queue.fsx`, `constructor_dispatch.fsx`, `upgrade_gate.fsx`, `attack_launch.fsx`) are **not removed**. US5's deep refactor replaces their *consumers* in `bot_macro.fsx`; the scripts themselves remain in-tree so other bots / tests / historical iterations can still load them.
- **`FSBar.Viz`** and **`FSBar.SyntheticData`** pick up the new modules transitively via the FSBar.Client reference — no project edits required.
