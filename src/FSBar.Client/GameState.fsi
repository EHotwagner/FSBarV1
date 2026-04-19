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

    /// <summary>
    /// Pure mapper: applies a batched <c>GameStateSnapshotResult</c>
    /// (from <see cref="M:FSBar.Client.Callbacks.getGameStateSnapshot"/>)
    /// to an existing <c>GameState</c>. Returns the updated state with
    /// <c>Units</c>, <c>Enemies</c>, <c>Metal</c>, and <c>Energy</c>
    /// replaced per spec 045 §Mapping.
    /// </summary>
    /// <remarks>
    /// Enemies absent from both <c>LosEnemies</c> and <c>RadarOnlyEnemies</c>
    /// retain their prior <c>Position</c> but have <c>InLOS</c>/<c>InRadar</c>
    /// cleared and <c>Health = None</c> (FR-007 frozen-last-known).
    /// Radar-only entries always carry <c>Health = None</c> even when the
    /// prior state had <c>Some _</c> (FR-004).
    /// </remarks>
    val applySnapshot: state: GameState -> snapshot: GameStateSnapshotResult -> GameState

    /// Processes a game frame and returns the updated game state.
    val processFrame: state: GameState -> frame: GameFrame -> stream: NetworkStream -> GameState
