# Data Model: GameState API

**Feature**: 016-gamestate-api
**Date**: 2026-04-08

## Entities

### UnitDefInfo

Cached unit definition. Loaded once at init, immutable for the session.

| Field | Type | Description |
|-------|------|-------------|
| DefId | int | Engine definition ID |
| Name | string | Engine name (e.g., "armmex") |
| Cost | float32 | Total resource cost |
| BuildSpeed | float32 | Build rate |
| MaxWeaponRange | float32 | Maximum weapon range in elmos |
| BuildOptions | int array | DefIds this unit can build |

**Identity**: DefId (unique within session)
**Lifecycle**: Created at init, never modified or destroyed

### TrackedUnit

A friendly unit tracked in the game state.

| Field | Type | Description |
|-------|------|-------------|
| UnitId | int | Engine unit instance ID |
| DefId | int | Unit definition ID |
| Name | string | Definition name (cached from UnitDefInfo) |
| X | float32 | World position X (elmos) |
| Y | float32 | World position Y (elmos, height) |
| Z | float32 | World position Z (elmos) |
| Health | float32 | Current health points |
| MaxHealth | float32 | Maximum health points |
| IsFinished | bool | True if construction complete |
| IsIdle | bool | True if unit has no orders |

**Identity**: UnitId (unique within session)
**Lifecycle**: Created on `UnitCreated` event → updated on `UnitFinished`, `Update`, `UnitIdle` → removed on `UnitDestroyed`, `UnitGiven`, `UnitCaptured`

### TrackedEnemy

A known enemy unit (seen in LOS or radar).

| Field | Type | Description |
|-------|------|-------------|
| UnitId | int | Engine enemy unit ID |
| DefId | int | Unit definition ID (if known from LOS) |
| Name | string | Definition name (if known) |
| X | float32 | Last known world position X |
| Y | float32 | Last known world position Y |
| Z | float32 | Last known world position Z |
| Health | float32 | Last known health |
| MaxHealth | float32 | Max health |
| InLOS | bool | Currently visible in line of sight |
| InRadar | bool | Currently visible on radar |
| LastSeenFrame | int | Frame number when last position was updated |

**Identity**: UnitId
**Lifecycle**: Created on `EnemyEnterLOS`/`EnemyEnterRadar` → updated on re-entry to LOS → flags cleared on leave LOS/radar → removed only on `EnemyDestroyed`

### EconomySnapshot

Resource state for one resource type.

| Field | Type | Description |
|-------|------|-------------|
| Current | float32 | Current resource level |
| Income | float32 | Income rate per frame |
| Usage | float32 | Usage rate per frame |
| Storage | float32 | Maximum storage capacity |

**Identity**: Identified by context (Metal or Energy)
**Lifecycle**: Refreshed every frame

### GameState

Central record holding all tracked state.

| Field | Type | Description |
|-------|------|-------------|
| Frame | uint32 | Current frame number |
| TeamId | int | Our team ID |
| Units | Map<int, TrackedUnit> | Friendly units by UnitId |
| Enemies | Map<int, TrackedEnemy> | Known enemies by UnitId |
| Metal | EconomySnapshot | Metal economy state |
| Energy | EconomySnapshot | Energy economy state |
| UnitDefs | Map<int, UnitDefInfo> | All unit definitions by DefId |
| UnitDefsByName | Map<string, int> | Name → DefId lookup |
| Events | GameEvent list | Events from the most recent frame |

**Identity**: Singleton per session
**Lifecycle**: Created at `init` → updated each frame via `processFrame` → discarded at session end

### GameEvent (reused type)

The `Events` field on `GameState` reuses the existing `GameEvent` discriminated union defined in `src/FSBar.Client/Events.fs`. No new type is needed. Each frame, `processFrame` copies the incoming `GameFrame.Events` list into `GameState.Events`, replacing the previous frame's events.

**Lifecycle**: Populated each frame by `processFrame` from the incoming `GameFrame.Events`, cleared and replaced on the next frame.

## Relationships

```
GameState 1──* TrackedUnit (via Units map)
GameState 1──* TrackedEnemy (via Enemies map)
GameState 1──* UnitDefInfo (via UnitDefs map)
GameState 1──1 EconomySnapshot (Metal)
GameState 1──1 EconomySnapshot (Energy)
TrackedUnit *──1 UnitDefInfo (via DefId)
TrackedEnemy *──1 UnitDefInfo (via DefId)
```

## State Transitions

### TrackedUnit Lifecycle

```
[UnitCreated] → Tracking(IsFinished=false, IsIdle=false)
  → [UnitFinished] → Tracking(IsFinished=true)
  → [UnitIdle] → Tracking(IsIdle=true)
  → [Position change detected on Update] → Tracking(IsIdle=false)
  → [Update] → Tracking(position/health refreshed)
  → [UnitDestroyed] → Removed
  → [UnitGiven/Captured] → Removed
```

### TrackedEnemy Lifecycle

```
[EnemyEnterLOS] → Known(InLOS=true, InRadar=true)
  → [EnemyLeaveLOS] → Known(InLOS=false)
  → [EnemyLeaveRadar] → Known(InLOS=false, InRadar=false, stale position)
  → [EnemyEnterLOS again] → Known(InLOS=true, position refreshed)
  → [EnemyDestroyed] → Removed
```
