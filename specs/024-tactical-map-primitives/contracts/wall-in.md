# Contract: `FSBar.Client.WallIn`

**FR link**: FR-019, FR-020, FR-021, FR-022, FR-023
**Tier**: 1 — compiled module, curated `.fsi`, surface-area baseline required.
**Clarification dependency**: Q3 (shared passability rules with `Pathing`).

## Purpose

Pure connectivity predicate: would placing this structure disconnect our base from anything? Called by `BasePlan.resolvePlan` on every slot resolution (FR-023), and directly by the bot whenever it's considering an ad-hoc placement outside the plan system.

## Public API surface (`WallIn.fsi`)

```fsharp
namespace FSBar.Client

type WallInReason =
    | DisconnectsStructures of names:string list
    | EnclosesBase

type WallInResult =
    | Passes
    | Fails of reason:WallInReason

type WallInQuery = {
    MoveType: MoveType
    RequireMapEdgeExit: bool
}

module WallIn =

    /// Default query: Kbot move type, RequireMapEdgeExit = true.
    val defaultWallInQuery : WallInQuery

    /// Check whether adding `proposed` to `ownStructures` would
    /// disconnect `baseCentre` from any currently-reachable structure
    /// or map edge. Pure; does not mutate any input. Shares
    /// passability rules with `Pathing.findPath` — a placement that
    /// passes this check cannot produce a `NoRoute` from `findPath`
    /// for a reachable start/goal pair through a corridor that didn't
    /// include the proposed cell.
    val wouldWallIn :
        grid: MapGrid ->
        baseCentre: float32 * float32 * float32 ->
        ownStructures: OwnStructureFootprint list ->
        proposed: OwnStructureFootprint ->
        query: WallInQuery ->
        WallInResult

    /// Compute the set of cells reachable from baseCentre given the
    /// current passability + structures mask. Exposed so `BasePlan`
    /// can reuse the reachability set across multiple slot checks
    /// within a single `resolvePlan` call without re-flood-filling
    /// per slot. Pure.
    val reachableCells :
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
        origin: float32 * float32 * float32 ->
        bool[,]
```

## Semantics

### Algorithm

1. Compute the **current reachability set** from `baseCentre` over the passability grid masked by `ownStructures`. Use `reachableCells` — a flood fill that returns a `bool[,]` of "cells reachable from origin".
2. For every currently-placed own structure in `ownStructures`, verify its centre cell is in the reachability set. (Sanity: if the caller passes a structure that's already unreachable, the check proceeds anyway — we're only reasoning about what the *proposed* placement changes.)
3. Compute the **post-placement reachability set** by adding `proposed` to the mask and re-flood-filling.
4. For every existing structure whose centre cell was in the pre-set but not in the post-set → `Fails (DisconnectsStructures names)`.
5. If `query.RequireMapEdgeExit` is true, additionally check that the post-set contains at least one map-edge cell. If not → `Fails EnclosesBase`.
6. Otherwise → `Passes`.

### Sharing passability with `Pathing`

`WallIn.reachableCells` and `Pathing.rasteriseFootprints` produce the same cell-level passability view. A concrete invariant: for any `(grid, moveType, ownStructures)`, the set of passable cells seen by `WallIn` equals the set of cells from which `findPath` can legally start a search. This is tested explicitly.

### Purity (FR-022)

`wouldWallIn` does not mutate `ownStructures`, `grid`, or `proposed`. It can be called repeatedly to probe candidate placements without the caller having to clone inputs.

### Diagnostic payload (FR-021)

`Fails (DisconnectsStructures names)` carries the **name** of every structure that becomes unreachable (pulled from `OwnStructureFootprint.Tag`). Callers emit `[wall-in-defect] proposed=<tag> cuts off <names>` stdout lines.

### Performance

Reachability is one O(N) flood fill where N = number of passable cells ≈ 0.7 × mapCellCount. On Avalanche 3.4 that's ~180k cells per call. Two flood fills per `wouldWallIn` call (pre + post) = ~360k operations, <10 ms in F#. `BasePlan.resolvePlan` does this per slot → ~50 ms for a 5-slot opening plan, which is inside the 20 ms target of Technical Context. A follow-up optimisation (reuse the pre-reachability set across slots — see `reachableCells` standalone for this) is noted in tasks.md.

## Test strategy

**Unit tests** (synthetic `MapGrid`):

- Single-corridor base with 8 surrounding structures and an open corridor to a factory → proposed structure in the corridor → `Fails (DisconnectsStructures ["factory"])`.
- Same base, proposed structure to the side of the corridor → `Passes`.
- Proposed placement creating a closed loop but with multiple surviving paths → `Passes`.
- `RequireMapEdgeExit = true`, proposed placement that isolates the base from the map edge but not from any existing structure → `Fails EnclosesBase`.
- Purity: call `wouldWallIn` on the same inputs twice → second call returns an identical result AND the `ownStructures` list is byte-for-byte unchanged.
- Shared passability: call `reachableCells` with and without a structure at a choke, and verify `Pathing.findPath` behaviour matches (passable set identical).

**Integration tests** (SMF-parsed real map):

- `wouldWallIn` against Avalanche 3.4 with the 5 opening structures + a synthetic proposed placement at base centre → `Fails EnclosesBase`.

**Live bot tests**:

- US5 integration: `BasePlan.resolvePlan` receiving a `WouldWallIn` failure for a slot emits `[wall-in-defect]` stdout → observable in the run directory.

## Surface-area baseline

`tests/FSBar.Client.Tests/Baselines/WallIn.baseline` — validated by the existing harness.
