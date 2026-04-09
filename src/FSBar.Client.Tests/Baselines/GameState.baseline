namespace FSBar.Client

open System.Net.Sockets

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
    val processFrame: state: GameState -> frame: GameFrame -> stream: NetworkStream -> GameState
