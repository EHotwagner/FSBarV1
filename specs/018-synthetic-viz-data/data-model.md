# Data Model: Synthetic Visualization Test Data

## Consumed Types (from FSBar.Client)

These types are used as-is. No new data types are created — the generator produces values of these existing types.

### GameState
- `FrameNumber: uint32` — monotonically increasing, 1-300
- `TeamId: int` — set to 0 (player team)
- `Units: Map<int, TrackedUnit>` — friendly units keyed by UnitId
- `Enemies: Map<int, TrackedEnemy>` — enemy units keyed by EnemyId
- `Metal: EconomySnapshot` — metal resource state
- `Energy: EconomySnapshot` — energy resource state
- `UnitDefs: UnitDefCache` — shared across all frames in a scene
- `Events: GameEvent list` — events that occurred this frame

### TrackedUnit
- `UnitId: int` — unique, positive, assigned sequentially
- `DefId: int` — references UnitDefCache.ById
- `Position: float32 * float32 * float32` — (X, Y, Z) within map bounds
- `Health: float32` — 0 < health <= MaxHealth
- `MaxHealth: float32` — fixed per unit type
- `IsFinished: bool` — true after UnitFinished event
- `IsIdle: bool` — true after UnitIdle event, false when moving

### TrackedEnemy
- `EnemyId: int` — unique, positive, separate ID space from friendly units (starting at 1000)
- `DefId: int option` — Some when identified (in LOS), None when only on radar
- `Position: float32 * float32 * float32` — updated when InLOS
- `Health: float32 option` — Some when InLOS, None otherwise
- `InLOS: bool` — true between EnemyEnterLOS/EnemyLeaveLOS events
- `InRadar: bool` — true between EnemyEnterRadar/EnemyLeaveRadar events

### EconomySnapshot
- `Current: float32` — 0 <= Current <= Storage
- `Income: float32` — >= 0, sum of production building outputs
- `Usage: float32` — >= 0, sum of factory/constructor drain
- `Storage: float32` — >= 0, increases with storage buildings

### UnitDefInfo
- `DefId: int` — unique, positive, sequential from 1
- `Name: string` — BAR-style name (e.g., "arm_commander", "arm_solar")
- `Cost: float32` — metal cost, > 0
- `BuildSpeed: float32` — construction speed, >= 0
- `MaxWeaponRange: float32` — 0 for non-combat units
- `BuildOptions: int array` — DefIds this unit can construct

### GameFrame
- `FrameNumber: uint32` — matches corresponding GameState.FrameNumber
- `Events: GameEvent list` — events that caused this frame's state transition

## Generator Internal State (not exposed)

The generator maintains internal mutable state during scene generation. This is not part of the public API — only the output arrays of GameState and GameFrame are exposed.

Internal state includes:
- Current positions and velocities of all units
- Target waypoints for moving units
- Scheduled actions (spawn, attack, destroy)
- Economy accumulators
- Enemy visibility state machines

## Scene Definitions

Each scene is parameterized by:
- **Map dimensions**: width and height in elmos
- **Unit defs**: the UnitDefCache entries for this scene
- **Scheduled actions**: frame-indexed list of events to trigger
- **Initial state**: starting units, economy values

## Validation Rules

1. All UnitIds in TrackedUnit map must be referenced by at least one UnitCreated event
2. All DefIds in TrackedUnit/TrackedEnemy must exist in UnitDefCache.ById
3. Position X in [0, mapWidth], Z in [0, mapHeight], Y in [0, 400]
4. Economy: 0 <= Current <= Storage for both Metal and Energy
5. Frame-to-frame position delta <= 6 elmos per axis
6. Enemy InLOS/InRadar flags must match the latest visibility event
7. No unit appears in Units map before its UnitCreated event frame
8. No unit appears in Units map after its UnitDestroyed event frame
