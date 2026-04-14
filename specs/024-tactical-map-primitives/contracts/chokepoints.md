# Contract: `FSBar.Client.Chokepoints`

**FR link**: FR-007, FR-008, FR-009, FR-010, FR-011
**Tier**: 1 — compiled module, curated `.fsi`, surface-area baseline required.
**Research**: R2 (distance-transform ridges with primary-route filter).

## Purpose

Identify narrow corridors that approach a specified base-centre, returning width-annotated chokepoint descriptors for use by the macro bot's defend interrupt and by `BasePlan` as a position chooser for defensive slots.

## Public API surface (`Chokepoints.fsi`)

```fsharp
namespace FSBar.Client

type ChokepointId = ChokepointId of uint32

type Chokepoint = {
    Id: ChokepointId
    Position: float32 * float32 * float32
    WidthElmos: float32
    OutwardDir: float32 * float32
    DistanceFromBase: float32
}

type ChokepointQuery = {
    MaxWidthElmos: float32
    SearchRadiusElmos: float32
    MoveType: MoveType
}

module Chokepoints =

    /// Default chokepoint query for a given move type.
    /// MaxWidthElmos = 40.0, SearchRadius = 2500.0.
    val defaultChokepointQuery : MoveType -> ChokepointQuery

    /// Find all chokepoints within the query's search radius around
    /// `baseCentre`, sorted by distance from base (closest first).
    /// Returns an empty list when no passage satisfies the width
    /// threshold AND the primary-route predicate (FR-009, FR-010).
    val findChokepoints :
        grid: MapGrid ->
        baseCentre: float32 * float32 * float32 ->
        query: ChokepointQuery ->
        Chokepoint list

    /// Deterministically compute the stable ID for a ridge cell.
    /// Exposed so callers can reference a chokepoint by its Id across
    /// successive queries without re-running the full detection.
    val chokepointIdOf : grid:MapGrid -> ridgeCellX:int -> ridgeCellZ:int -> ChokepointId

    /// Return the distance-transform raw values for diagnostic /
    /// visualisation use. Dimensions match `MapGrid.passability` for
    /// the given move type. Pure.
    val computeDistanceTransform :
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
        float32[,]
```

## Semantics

### Algorithm (per research R2)

1. Compute `passable : bool[,]` from `MapGrid.passability grid query.MoveType`, optionally overlaying `ownStructures` footprints. Chokepoint detection uses the current static map; passing no structures is the typical call at warmup.
2. Distance transform: for every passable cell `c`, compute `dt[c] = distance (in cells) to nearest impassable cell or map edge`. Implementation: two-pass Felzenszwalb-Huttenlocher squared-Euclidean distance transform, then take `sqrt` on demand.
3. Find ridge cells: cells where `dt[c]` is a local maximum along the corridor direction. Use a 3×3 neighbourhood maximum filter: cell `c` is a ridge if `dt[c] ≥ dt[n]` for every neighbour `n` **and** at least one neighbour has `dt[n] < dt[c]` (strict along one axis rules out plateaus in open terrain).
4. Filter by radius: keep only ridges within `query.SearchRadiusElmos` of `baseCentre`.
5. Filter by width: keep only ridges where `2 × dt[c] × 8 < query.MaxWidthElmos` (the factor of 8 converts cells to elmos).
6. Filter by "primary route" (FR-010): for each candidate ridge, temporarily mark it impassable, flood-fill from `baseCentre`, and check whether the reachable region shrinks by more than a configurable threshold (default: ≥10% of cells). Ridges that don't materially affect connectivity are dropped — they're local narrow points, not actual chokepoints.
7. Compute each surviving chokepoint's `OutwardDir`: the unit vector from `baseCentre` to the ridge cell's world position.
8. Sort by `DistanceFromBase` ascending and return.

### Empty result (FR-009)

If no ridge cell satisfies all four filters, the function returns `[]`. This is the correct answer for open terrain; the caller's defend interrupt should fall back to 023's "nearest-enemy" behaviour.

### Stable IDs (FR-011)

`ChokepointId` is derived as:

```text
id = (uint32) hash(widthHeightmap, heightHeightmap, ridgeCellZ * widthHeightmap + ridgeCellX)
```

The grid dimensions are included so two different maps' ridge cell #1000 don't collide. Two `findChokepoints` calls on the same `(grid, baseCentre, query)` produce identical `Id` values — asserted by a determinism test.

### Performance

Distance transform: O(N) two-pass = ~262k operations on Avalanche 3.4's 512×512 grid. Ridge detection: O(N). Radius/width filters: O(ridgeCount). Primary-route filter: O(ridgeCount × reachableCells). On realistic base queries the ridge candidate set is <50 cells so the primary-route filter is ~50 small flood fills. Total <200 ms on Avalanche 3.4 (research R2 estimate, to be verified in unit tests).

## Test strategy

**Unit tests** (synthetic MapGrid fixtures):

- Synthetic 32×32 grid with one central impassable wall containing a single 3-cell gap → `findChokepoints` returns exactly one chokepoint at the gap, width ≈ 24 elmos.
- Same grid with the wall split into two parallel walls creating two gaps → returns two chokepoints ordered by distance from base centre.
- Fully open 32×32 grid → `[]` (FR-009 verification).
- Two calls with identical inputs → identical result lists (determinism).
- `ChokepointId.chokepointIdOf` returns the same value for the same ridge cell across two calls → stability (FR-011).

**Integration tests** (SMF-parsed real maps):

- SC-003: `findChokepoints` on Avalanche 3.4 from base centre `(500, 397)` returns a list whose top-1 result is within ±150 elmos of the human-recognised canyon leading to the NullAI spawn area. (The exact canyon position is captured in a test constant after operator inspection.)
- `findChokepoints` on Red Rock Desert v2 (if installed) returns ≥1 chokepoint at a reasonable position.
- `findChokepoints` on Comet Catcher Remake (if installed) returns a reasonable result (operator-verified).

**Live bot tests**:

- US5 integration: macro bot's defend interrupt on BARb/dev uses the chokepoint list to position interceptors instead of chasing each raider. Observable via the `[defend] chokepoint pos=(X,Y) width=W` stdout trace (see data-model.md §6 and spec US5 acceptance scenario 3).

## Surface-area baseline

`tests/FSBar.Client.Tests/Baselines/Chokepoints.baseline` — validated by the existing baseline test harness.
