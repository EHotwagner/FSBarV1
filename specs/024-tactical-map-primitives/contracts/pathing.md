# Contract: `FSBar.Client.Pathing`

**FR link**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-006a
**Tier**: 1 — compiled module, curated `.fsi`, surface-area baseline required.
**Research**: R1 (A\* with slope-weighted edges + `ownStructures` mask).

## Purpose

Compute slope-aware paths over a `MapGrid` for a given `MoveType`, with friendly structures treated as impassable overlays. Used by the macro bot's attack phase and by the plan resolver for "can the builder reach this slot".

## Public API surface (`Pathing.fsi`)

```fsharp
namespace FSBar.Client

/// Footprint of a placed own structure — see data-model.md §2.
type OwnStructureFootprint = {
    Centre: float32 * float32 * float32
    RadiusElmos: float32
    Tag: string option
}

type PathStatus =
    | Complete
    | Partial of budgetExhausted:bool

type PathFailure =
    | OutOfBounds
    | EndpointImpassable
    | NoRoute

type Path = {
    Waypoints: (float32 * float32 * float32) array
    EstimatedCost: float32
    Status: PathStatus
}

type PathBudget = {
    WallClockMs: int
    MaxExpansions: int
    SlopeCost: float32
}

module Pathing =

    /// Default budget: 50 ms wall clock, 50 000 cell expansions, slope
    /// cost multiplier 2.0.
    val defaultPathBudget : PathBudget

    /// Find a path from `start` to `goal` over `grid` for `moveType`,
    /// treating every cell covered by an `ownStructures` footprint as
    /// impassable. Returns `Error PathFailure` when no path can be
    /// found or the endpoints are invalid. Never blocks beyond the
    /// configured budget.
    val findPath :
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
        start: float32 * float32 * float32 ->
        goal: float32 * float32 * float32 ->
        budget: PathBudget ->
        Result<Path, PathFailure>

    /// Sum the edge weights of a precomputed path. Equivalent to
    /// `path.EstimatedCost` but exposed so callers can recompute cost
    /// against a modified budget (e.g. re-scoring with a different
    /// slopeCost for "how would this path change under a heavier unit").
    val pathCost : grid:MapGrid -> moveType:MoveType -> path:Path -> slopeCost:float32 -> float32

    /// Given a collection of structure footprints, rasterise them into
    /// a boolean grid overlay that callers can reuse across multiple
    /// findPath calls for the same structure set. Pure, cache-friendly.
    val rasteriseFootprints :
        grid: MapGrid ->
        ownStructures: OwnStructureFootprint seq ->
        bool[,]
```

## Semantics

### Neighbour model

8-connectivity (octile). Each cell has up to 8 neighbours: 4 cardinal + 4 diagonal.

### Edge cost

```text
baseDist =
    | cardinal neighbour  → 1.0f
    | diagonal neighbour  → sqrt(2) ≈ 1.4142135f

slopeCell = SlopeMap[cell / 2]   // slope map is half-resolution
edgeCost  = baseDist × (1.0f + slopeCell × budget.SlopeCost)
```

### Heuristic

Octile distance: `h(a, b) = max(|dx|, |dy|) + (sqrt(2) - 1) × min(|dx|, |dy|)`. Multiplied by the minimum possible edge cost (= 1.0f, i.e. flat ground) to preserve admissibility.

### Passability

```text
passable(x, z) =
    MapGrid.passability grid moveType [x, z]
    AND NOT footprint-overlay[x, z]
```

The footprint overlay is computed by `rasteriseFootprints`. `findPath` calls it internally on every invocation (callers can pre-rasterise and pass a pre-masked MapGrid via a future helper if benchmark data shows the overhead matters — not in scope for 024).

### Budget enforcement

`findPath` keeps a `Stopwatch` started at entry. Every 256 cell expansions it checks `stopwatch.ElapsedMilliseconds > budget.WallClockMs` **and** the expansion counter against `budget.MaxExpansions`. If either threshold is crossed, the search aborts; the best partial path so far (reconstructed from the lowest-f-score node that has a parent trail back to `start`) is returned with `Status = Partial true`.

### Waypoint spacing

After the raw grid-cell path is recovered, consecutive cells collinear with the previous direction are collapsed. The resulting waypoints are spaced such that any two consecutive waypoints are connected by a straight line that's fully passable for `moveType` — this is FR-004. Verified post-recovery with a Bresenham walk between consecutive waypoints; if the walk crosses an impassable cell, the intermediate cells are re-inserted until the invariant holds.

### Determinism (FR-003)

Priority queue ties break on linearised cell index (`z × width + x`). Two identical input sets produce identical expansion order → identical path. Tests assert this by calling `findPath` twice with the same inputs and comparing waypoint arrays byte-for-byte.

### Cache ownership (FR-006a)

`findPath` is pure over its inputs. It does not cache results internally. Callers that want memoisation key their own cache on `(mapGridVersion, ownStructuresVersion, start, goal, moveType, budget)`. See `bot_macro.fsx` Phase 5 integration in data-model.md §6 for the expected caller pattern.

## Test strategy

**Unit tests** (`PathingTests.fs`, synthetic MapGrid fixtures):

- Straight-line path on a flat 64×64 synthetic grid → waypoints trace the straight line, cost = straight-line distance.
- Path around a central cliff → returned waypoints detour, every waypoint is Kbot-passable.
- No route (grid with two regions separated by an uncrossable ridge for `Tank` move type) → `Error NoRoute`.
- `start` off-map → `Error OutOfBounds`.
- `start` on an impassable cell → `Error EndpointImpassable`.
- `ownStructures` mask blocks an otherwise-clear route → returned path goes around.
- Budget exhaustion → `Status = Partial true` with a non-empty partial path.
- Determinism: two consecutive `findPath` calls with identical inputs produce identical waypoint arrays.
- `pathCost` re-scores a path under a higher `slopeCost` → returned value ≥ the path's original `EstimatedCost`.

**Integration tests** (real maps via `SmfParser`):

- `findPath` over `avalanche_3.4.sd7` from base (500, 397) to enemy commander (3699, 3601) for `MoveType.Kbot` → returns a `Complete` path with ≥2 waypoints and cost within an operator-verified range.
- SC-002: same call on a saved `MapGrid` with a known central ridge returns a detour path.

**Live bot tests** (`bot_macro.fsx` US5 integration):

- Refactored macro bot's Attack phase issues `MoveCommand` for each waypoint → unit telemetry confirms combat units reach within 32 elmos of goal.
- Rush bot (`bot.fsx`) unchanged → still wins on NullAI after 024 integration commits.

## Surface-area baseline

`tests/FSBar.Client.Tests/Baselines/Pathing.baseline` regenerated from `Pathing.fsi` at first pass. Any new public symbol fails the baseline test.
