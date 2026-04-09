# Data Model: Observable GameState API

**Branch**: `017-observable-gamestate-api` | **Date**: 2026-04-09

## New Types

### GameState

Central immutable record holding all tracked state for a game session. Updated each frame by processing events from the IObservable<GameFrame> stream.

```
GameState
├── FrameNumber: uint32              — current simulation frame
├── TeamId: int                      — our AI's team ID
├── Units: Map<int, TrackedUnit>     — friendly units by engine ID
├── Enemies: Map<int, TrackedEnemy>  — known enemy units by engine ID
├── Metal: EconomySnapshot           — current metal economy
├── Energy: EconomySnapshot          — current energy economy
├── UnitDefs: UnitDefCache           — cached unit definition lookup
└── Events: GameEvent list           — events from the current frame
```

### TrackedUnit

Represents a friendly unit with live-updated state.

```
TrackedUnit
├── UnitId: int                     — engine-assigned unit ID
├── DefId: int                      — unit definition ID
├── Position: float32 * float32 * float32  — world position (x, y, z) in elmos
├── Health: float32                 — current health points
├── MaxHealth: float32              — maximum health points
├── IsFinished: bool                — true when construction is complete
└── IsIdle: bool                    — true when command queue is empty
```

### TrackedEnemy

Represents a known enemy unit with last-known state and visibility flags.

```
TrackedEnemy
├── EnemyId: int                    — engine-assigned unit ID
├── DefId: int option               — unit definition ID (if identified)
├── Position: float32 * float32 * float32  — last-known world position
├── Health: float32 option          — last-known health (if in LOS)
├── InLOS: bool                     — currently in line of sight
└── InRadar: bool                   — currently in radar coverage
```

### EconomySnapshot

Resource state for a single resource type at a point in time.

```
EconomySnapshot
├── Current: float32                — current stockpile
├── Income: float32                 — per-frame income rate
├── Usage: float32                  — per-frame usage rate
└── Storage: float32                — maximum storage capacity
```

### UnitDefInfo

Cached unit definition data, loaded once at initialization.

```
UnitDefInfo
├── DefId: int                      — unit definition ID
├── Name: string                    — internal name (e.g., "armcom", "armmex")
├── Cost: float32                   — total resource cost
├── BuildSpeed: float32             — build speed value
├── MaxWeaponRange: float32         — maximum weapon range in elmos
└── BuildOptions: int array         — definition IDs this unit can build
```

### UnitDefCache

Opaque type wrapping the two lookup maps for unit definitions.

```
UnitDefCache
├── ById: Map<int, UnitDefInfo>     — lookup by definition ID
└── ByName: Map<string, int>        — reverse lookup: name → definition ID
```

## Existing Types (unchanged)

### GameFrame
```
GameFrame
├── FrameNumber: uint32
└── Events: GameEvent list
```

### GameEvent (discriminated union — 27 cases)
Unchanged. All existing cases preserved.

### SessionState (discriminated union)
Unchanged: Idle | Starting | Connected | Running | Stopped | Error of string

## State Transitions

### GameState Event Processing

```
Init(teamId)           → Set TeamId, seed pre-existing units, load UnitDefCache
UnitCreated(id, bld)   → Add TrackedUnit { IsFinished=false, IsIdle=false }
UnitFinished(id)       → Set IsFinished=true
UnitIdle(id)           → Set IsIdle=true
UnitDamaged(id, ...)   → Update Health (via callback refresh)
UnitDestroyed(id, _)   → Remove from Units map
UnitGiven(id, _, new)  → Remove if given away from our team
UnitCaptured(id,_,new) → Remove if captured by another team
EnemyEnterLOS(id)      → Add/update TrackedEnemy { InLOS=true }
EnemyLeaveLOS(id)      → Set InLOS=false
EnemyEnterRadar(id)    → Set InRadar=true
EnemyLeaveRadar(id)    → Set InRadar=false
EnemyDestroyed(id, _)  → Remove from Enemies map
EnemyCreated(id)       → Add TrackedEnemy (minimal info)
EnemyFinished(id)      → Update TrackedEnemy
Update(frame)          → Refresh positions/health for all units via callbacks,
                         reset IsIdle if position changed, refresh economy
```

### Session Lifecycle

```
Idle → Starting → Connected → Running → Stopped
                                  ↓
                                Error
```

Observable is created on Start(). Background thread begins reading frames on first subscription or on entering Running state. Observable completes on Stop() or engine disconnect.

## Relationships

```
BarClient 1──1 IObservable<GameFrame>     (exposes frame stream)
BarClient 1──1 GameState                  (maintains current snapshot)
GameState 1──* TrackedUnit                (via Units map)
GameState 1──* TrackedEnemy               (via Enemies map)
GameState 1──1 UnitDefCache               (loaded at init)
GameState 1──2 EconomySnapshot            (metal + energy)
UnitDefCache 1──* UnitDefInfo             (via ById map)
TrackedUnit *──1 UnitDefInfo              (via DefId → cache lookup)
```
