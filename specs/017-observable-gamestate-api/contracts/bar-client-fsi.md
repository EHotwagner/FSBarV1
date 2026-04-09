# .fsi Contracts: Observable GameState API

**Branch**: `017-observable-gamestate-api` | **Date**: 2026-04-09

## New Module: GameState.fsi

```fsharp
namespace FSBar.Client

/// Snapshot of a single resource type's economy.
type EconomySnapshot = {
    Current: float32
    Income: float32
    Usage: float32
    Storage: float32
}

/// A friendly unit tracked by the game state.
type TrackedUnit = {
    UnitId: int
    DefId: int
    Position: float32 * float32 * float32
    Health: float32
    MaxHealth: float32
    IsFinished: bool
    IsIdle: bool
}

/// A known enemy unit tracked by the game state.
type TrackedEnemy = {
    EnemyId: int
    DefId: int option
    Position: float32 * float32 * float32
    Health: float32 option
    InLOS: bool
    InRadar: bool
}

/// Central game state record, updated each frame from the event stream.
type GameState = {
    FrameNumber: uint32
    TeamId: int
    Units: Map<int, TrackedUnit>
    Enemies: Map<int, TrackedEnemy>
    Metal: EconomySnapshot
    Energy: EconomySnapshot
    UnitDefs: UnitDefCache
    Events: GameEvent list
}

/// Functions for creating and updating game state.
module GameState =
    /// Creates an empty initial game state.
    val empty: GameState

    /// Processes a game frame and returns the updated game state.
    val processFrame: state: GameState -> frame: GameFrame -> stream: System.Net.Sockets.NetworkStream -> GameState
```

## New Module: UnitDefCache.fsi

```fsharp
namespace FSBar.Client

/// Cached unit definition data.
type UnitDefInfo = {
    DefId: int
    Name: string
    Cost: float32
    BuildSpeed: float32
    MaxWeaponRange: float32
    BuildOptions: int array
}

/// Cache of all unit definitions, loaded once at initialization.
type UnitDefCache

/// Functions for loading and querying unit definitions.
module UnitDefCache =
    /// Loads all unit definitions from the engine via callbacks. One-time operation.
    val loadFromEngine: stream: System.Net.Sockets.NetworkStream -> UnitDefCache

    /// Looks up a unit definition by its ID. Returns None if not found.
    val tryFindById: cache: UnitDefCache -> defId: int -> UnitDefInfo option

    /// Looks up a unit definition by name. Returns None if not found.
    val tryFindByName: cache: UnitDefCache -> name: string -> UnitDefInfo option

    /// Returns all cached unit definitions.
    val all: cache: UnitDefCache -> UnitDefInfo seq
```

## Modified Module: BarClient.fsi (changes only)

```fsharp
// CHANGED: seq<GameFrame> → IObservable<GameFrame>
member Frames: System.IObservable<GameFrame>

// NEW: current game state snapshot (updated each frame)
member GameState: GameState
```

All other BarClient members remain unchanged.

## Modified Module: MapQuery.fsi (additions only)

```fsharp
// NEW: Find the nearest metal spot to a world position.
val nearestMetalSpot:
    spots: (float32 * float32 * float32 * float32) array ->
    position: float32 * float32 * float32 ->
    (float32 * float32 * float32 * float32) option
```

## Modified Module: MapCache.fsi (additions only)

```fsharp
// NEW: Refresh dynamic map layers (LOS, radar) from engine.
val refreshDynamic: stream: System.Net.Sockets.NetworkStream -> unit
```

## Unchanged Modules

The following .fsi files require no changes:
- Events.fsi
- Commands.fsi
- Callbacks.fsi
- Connection.fsi
- Protocol.fsi
- MapGrid.fsi
- EngineConfig.fsi
- EngineDiscovery.fsi
- EngineLauncher.fsi
- ScriptGenerator.fsi
