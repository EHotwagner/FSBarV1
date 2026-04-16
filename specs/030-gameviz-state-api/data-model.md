# Data Model: GameViz State-Based Rendering API

**Feature**: 030-gameviz-state-api  
**Date**: 2026-04-16

## Entities

### Existing Entities (no changes)

| Entity | Module | Notes |
|--------|--------|-------|
| `GameState` | `FSBar.Client.GameState` | Input from trainer bot. Contains `Units`, `Enemies`, `Metal`, `Energy`, `UnitDefs`, `Events`. |
| `MapGrid` | `FSBar.Client.MapGrid` | Terrain data. Passed directly from bot warmup or MapCacheFile. |
| `UnitDefCache` | `FSBar.Client.UnitDefCache` | Unit definition lookup by ID → name. Already in `GameState.UnitDefs`. |
| `TrackedUnit` | `FSBar.Client.GameState` | Friendly unit with `UnitId`, `DefId`, `Position`, `Health`, `MaxHealth`, `IsFinished`, `IsIdle`. |
| `TrackedEnemy` | `FSBar.Client.GameState` | Enemy unit with `EnemyId`, `DefId option`, `Position`, `Health option`, `InLOS`, `InRadar`. |
| `EconomySnapshot` | `FSBar.Client.GameState` | `Current`, `Income`, `Usage`, `Storage` per resource type. |
| `GameEvent` | `FSBar.Client.Events` | Discriminated union of 28+ event types including creation, destruction, damage, LOS. |
| `GameFrame` | `FSBar.Client.Events` | `FrameNumber` + `Events` list. Used by existing socket path. |
| `UnitState` | `FSBar.Viz.VizTypes` | Internal viz unit record: position, team, defId, health, isEnemy. |
| `UnitDisplay` | `FSBar.Viz.VizTypes` | Enriched display record with shape, faction, tier, label, weapon ranges, etc. |
| `GameSnapshot` | `FSBar.Viz.VizTypes` | Complete frame for SceneBuilder: MapGrid, units, display units, indicators, economy, metal spots. |
| `DefProps` | `FSBar.Viz.GameViz` (private) | Cached visual properties per DefId: name, shape, faction, tier, label, footprint, weapons, sight. |
| `EventIndicator` | `FSBar.Viz.VizTypes` | Transient visual marker: position, kind, frame created, duration. |

### New API Surface

No new types are introduced. The feature adds two new functions to the existing `GameViz` module:

```fsharp
// GameViz.fsi additions:
val attachWithState: mapGrid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> teamId: int -> unit
val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit
```

## Data Flow: State-Based Path

```
GameState (from BarClient)          GameViz Module State
─────────────────────────────────   ──────────────────────
                                    
attachWithState(mapGrid, spots, id)
  mapGrid ─────────────────────────→ mapGridRef
  metalSpots ──────────────────────→ metalSpots
  teamId ──────────────────────────→ myTeamId
                                    
onFrameWithState(gameState, mapGrid)
  gameState.Events ────────────────→ process events → indicators
  gameState.Units ─────────────────→ rebuild units (friendly, isEnemy=false)
  gameState.Enemies ───────────────→ rebuild units (enemy, isEnemy=true)
  gameState.UnitDefs ──────────────→ ensureDefProps (name lookup, no socket)
  gameState.Metal ─────────────────→ metalEcon
  gameState.Energy ────────────────→ energyEcon
  mapGrid ─────────────────────────→ mapGridRef (updated)
                                    
  buildSnapshot() ─────────────────→ snapshot (with DisplayUnits)
  emitScene() ─────────────────────→ Scene → Viewer window
```

## State Transitions

### Module State (`GameViz` mutable fields)

| State | Before attachWithState | After attachWithState | After onFrameWithState |
|-------|----------------------|----------------------|----------------------|
| `mapGridRef` | `None` | `Some mapGrid` | `Some mapGrid` (updated) |
| `metalSpots` | `[||]` | Populated | Unchanged |
| `myTeamId` | `0` | Set | Unchanged |
| `clientRef` | `None` | `None` (not used) | `None` (not used) |
| `units` | `Map.empty` | `Map.empty` | Rebuilt from GameState |
| `defPropsCache` | `Map.empty` | `Map.empty` | Populated per DefId |
| `unfinishedUnits` | `Set.empty` | `Set.empty` | Updated from events |
| `indicators` | `[]` | `[]` | Updated from events |
| `snapshot` | `None` | `None` | `Some (buildSnapshot ...)` |

### Key Difference from Socket Path

In the socket path, `units` is incrementally maintained across frames (events add/remove entries). In the state-based path, `units` is **rebuilt from GameState each frame** — this ensures the visualization always matches the bot's current view exactly. Event indicators are still maintained across frames (with expiry) for visual continuity.

## Validation Rules

1. `mapGrid` passed to `attachWithState` must have non-zero `WidthHeightmap` and `HeightHeightmap`.
2. `teamId` must be >= 0.
3. `gameState.UnitDefs` must be able to resolve DefIds present in `gameState.Units`/`Enemies` — unknown DefIds produce a fallback `DefProps` (no crash).
4. `TrackedEnemy.DefId = None` produces a generic bot shape with neutral faction (graceful degradation).
