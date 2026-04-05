namespace FSBar.Client

[<RequireQualifiedAccess>]
type GameEvent =
    | Init of teamId: int
    | Release
    | Update of frame: int
    | Message of player: int * message: string
    | UnitCreated of unitId: int * builderId: int
    | UnitFinished of unitId: int
    | UnitIdle of unitId: int
    | UnitMoveFailed of unitId: int
    | UnitDamaged of unitId: int * attackerId: int option * damage: float32 * weaponDefId: int * isParalyzer: bool
    | UnitDestroyed of unitId: int * attackerId: int option
    | UnitGiven of unitId: int * oldTeamId: int * newTeamId: int
    | UnitCaptured of unitId: int * oldTeamId: int * newTeamId: int
    | EnemyEnterLOS of enemyId: int
    | EnemyLeaveLOS of enemyId: int
    | EnemyEnterRadar of enemyId: int
    | EnemyLeaveRadar of enemyId: int
    | EnemyDamaged of enemyId: int * attackerId: int option * damage: float32 * weaponDefId: int
    | EnemyDestroyed of enemyId: int * attackerId: int option
    | WeaponFired of unitId: int * weaponDefId: int
    | PlayerCommand of units: int list * commandTopicId: int * commandId: int
    | SeismicPing of x: float32 * y: float32 * z: float32 * strength: float32
    | CommandFinished of unitId: int * commandId: int * commandTopicId: int
    | Load
    | Save
    | EnemyCreated of enemyId: int
    | EnemyFinished of enemyId: int
    | LuaMessage of data: string * inMessageId: int
    | Shutdown of reason: string
    | Unknown

type GameFrame = {
    FrameNumber: uint32
    Events: GameEvent list
}

module Events =

    val fromProto: Highbar.EngineEvent -> GameEvent
