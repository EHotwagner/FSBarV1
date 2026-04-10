namespace FSBar.SyntheticData

open FSBar.Client

/// Enemy visibility state machine and simulation.
module EnemySim =
    /// Visibility state for an enemy.
    type VisState =
        | NotVisible
        | RadarOnly
        | InLineOfSight

    /// Internal state for a tracked enemy with scheduled visibility transitions.
    type SimEnemy = {
        Enemy: TrackedEnemy
        DefId: int
        MaxHealth: float32
        Position: float32 * float32 * float32
        VisState: VisState
        Transitions: (int * VisState) list
        Speed: float32
        TargetX: float32
        TargetZ: float32
    }

    /// Create a SimEnemy with scheduled visibility transitions.
    val create:
        enemyId: int ->
        defId: int ->
        maxHealth: float32 ->
        position: (float32 * float32 * float32) ->
        speed: float32 ->
        transitions: (int * VisState) list ->
        SimEnemy

    /// Step one frame. Returns updated SimEnemy and any generated events.
    val step:
        se: SimEnemy ->
        frame: int ->
        mapWidth: float32 ->
        mapHeight: float32 ->
        (SimEnemy * GameEvent list)
