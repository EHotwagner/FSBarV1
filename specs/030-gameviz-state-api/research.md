# Research: GameViz State-Based Rendering API

**Feature**: 030-gameviz-state-api  
**Date**: 2026-04-16

## R1: How does the current GameViz socket-based pipeline work?

**Decision**: The state-based path must mirror the existing pipeline's data flow but source all data from pre-built `GameState` + `MapGrid` instead of socket callbacks.

**Rationale**: The current `onFrame` in `GameViz.fs` (lines 323–445) performs three categories of socket work:

1. **Map refresh** — `MapGrid.refreshLos/refreshRadar` per frame (2 socket calls). The spec explicitly says LOS/radar overlay is not needed for trainer viz (Assumptions, line 95), so the state-based path simply skips these.

2. **Unit state tracking** — For each `GameEvent` (`UnitCreated`, `EnemyEnterLOS`, `Update`, etc.), the module queries `Callbacks.getUnitPos`, `getUnitDef`, `getUnitHealth`, `getUnitMaxHealth` to build its internal `units: Map<int, UnitState>`. In the state-based path, this map is derived directly from `GameState.Units` (friendly) and `GameState.Enemies` (enemy) each frame — no per-event socket queries.

3. **Economy** — 8 `Callbacks.getEconomy*` calls per frame. Replaced by `GameState.Metal` and `GameState.Energy` fields.

**Alternatives considered**: Wrapping the existing `onFrame` with a "virtual socket" adapter was rejected — it would require implementing a fake `NetworkStream`, adding unnecessary complexity. Direct state injection is simpler and more robust.

## R2: How to resolve unit visual properties without socket access?

**Decision**: Use BarData static lookup (already implemented in `GameViz.fs` lines 24–68 as `resolveDefPropsFromBarData`) keyed by unit internal name from `GameState.UnitDefs` (`UnitDefCache`).

**Rationale**: The `defPropsCache` in GameViz already works this way — `ensureDefProps` calls `Callbacks.getUnitDefName` (socket) to get the name, then passes it to `resolveDefPropsFromBarData` (pure, no socket). The only socket dependency is the name lookup. Since `GameState.UnitDefs` contains `UnitDefInfo.Name` for every known DefId, we can call `UnitDefCache.tryFindById` to get the name, then use the existing `resolveDefPropsFromBarData`.

**Alternatives considered**: Using `SyntheticDataAdapter` was considered but rejected — it has a hardcoded `classTable` for only ~23 synthetic DefIds, whereas the BarData approach handles all ~600 unit types.

## R3: How to handle event-driven indicators (destruction, damage, creation) from GameState?

**Decision**: Diff the previous frame's unit map against the current `GameState.Units`/`Enemies` to detect creation, destruction, and damage events, supplemented by `GameState.Events`.

**Rationale**: `GameState.Events` already contains `UnitCreated`, `UnitDestroyed`, `UnitDamaged`, `EnemyEnterLOS`, `EnemyLeaveLOS`, `EnemyDestroyed` events for the current frame. The state-based `onFrameWithState` can iterate these events to create `EventIndicator` entries (same as current code, lines 343–421), but instead of querying socket for unit position/def, it reads the data from the `GameState.Units`/`Enemies` maps.

For unit destruction specifically: the event fires on the frame where the unit disappears from GameState. The indicator must be created at the unit's last known position (from the previous frame's data, still in the module's `units` map at event processing time).

**Alternatives considered**: Pure diff-based detection (comparing Maps between frames) was considered but would miss damage events. The hybrid approach (events for indicators, full state rebuild from Maps) is more accurate.

## R4: What is the minimal change to the `GameViz.fsi` public API?

**Decision**: Add two new functions alongside the existing API:

```fsharp
val attachWithState: mapGrid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> teamId: int -> unit
val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit
```

**Rationale**: This preserves full backward compatibility (FR-007) — `attachToClient` and `onFrame` remain unchanged. The new functions are parallel entry points that skip all socket access. Two functions rather than one because initialization (map data, metal spots, team ID) is a one-time operation separate from per-frame updates.

**Alternatives considered**: A single `onFrameWithState` that also handles initialization was considered but rejected — it would require checking initialization state on every frame call, adding per-frame branching overhead. Explicit `attachWithState` is clearer and matches the existing `attachToClient`/`onFrame` pattern.

## R5: How should the trainer bot helper (`viewer.fsx`) be updated?

**Decision**: Replace `attachToClient + onFrame` calls with `attachWithState + onFrameWithState`. The macro bot passes its `MapGrid` (from warmup or MapCacheFile) and `GameState` directly. The simpler bot falls back to MapCacheFile or a flat MapGrid from map dimensions.

**Rationale**: The macro bot (`bot.fsx`) already has `MapGrid` from its warmup phase (loaded via `MapCacheFile.read` or `MapGrid.loadFromEngine`). It also has `client.GameState` updated each frame. The viewer helper just needs to pass these through instead of routing through socket callbacks.

**Alternatives considered**: Keeping the deferred-attach pattern was considered but is unnecessary — `attachWithState` doesn't read from the socket, so it can be called immediately at startup without the WaitFrames dance.

## R6: What about the non-macro bot (FR-006)?

**Decision**: Accept an optional `MapGrid` parameter. When `None`, construct a flat MapGrid from map dimensions (available from `BarClient.Config` or engine callbacks during warmup) or load from `MapCacheFile` if available.

**Rationale**: The simpler bot doesn't run full map analysis, but map dimensions are always available. A flat MapGrid (zero heightmap, empty slope/resource) still allows unit rendering with correct positioning. Metal spots can be loaded from the map cache file if available.

**Alternatives considered**: Making MapGrid mandatory was considered — simpler but would break non-macro bot visualization (FR-006).

## R7: Thread safety considerations

**Decision**: The new functions follow the same `lock stateLock` pattern as existing functions. No additional synchronization needed.

**Rationale**: `onFrameWithState` writes to the same mutable state (`units`, `indicators`, `defPropsCache`, `unfinishedUnits`, `snapshot`). The existing `stateLock` mutex protects these. The FrameTick handler (which calls `emitScene`) also takes the lock, ensuring consistent snapshot reads.
