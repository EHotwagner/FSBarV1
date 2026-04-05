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

    let private shutdownReasonToString (reason: Highbar.ShutdownReason) =
        match reason with
        | Highbar.ShutdownReason.GameOver -> "GameOver"
        | Highbar.ShutdownReason.Disconnect -> "Disconnect"
        | Highbar.ShutdownReason.Error -> "Error"
        | _ -> "Unknown"

    let fromProto (engineEvent: Highbar.EngineEvent) : GameEvent =
        match engineEvent.Event with
        | Highbar.EngineEvent.EventCase.Init e ->
            GameEvent.Init(e.TeamId)
        | Highbar.EngineEvent.EventCase.Release _ ->
            GameEvent.Release
        | Highbar.EngineEvent.EventCase.Update e ->
            GameEvent.Update(e.Frame)
        | Highbar.EngineEvent.EventCase.Message e ->
            GameEvent.Message(e.Player, e.Message)
        | Highbar.EngineEvent.EventCase.UnitCreated e ->
            GameEvent.UnitCreated(e.UnitId, e.BuilderId)
        | Highbar.EngineEvent.EventCase.UnitFinished e ->
            GameEvent.UnitFinished(e.UnitId)
        | Highbar.EngineEvent.EventCase.UnitIdle e ->
            GameEvent.UnitIdle(e.UnitId)
        | Highbar.EngineEvent.EventCase.UnitMoveFailed e ->
            GameEvent.UnitMoveFailed(e.UnitId)
        | Highbar.EngineEvent.EventCase.UnitDamaged e ->
            GameEvent.UnitDamaged(e.UnitId, e.AttackerId, e.Damage, e.WeaponDefId, e.IsParalyzer)
        | Highbar.EngineEvent.EventCase.UnitDestroyed e ->
            GameEvent.UnitDestroyed(e.UnitId, e.AttackerId)
        | Highbar.EngineEvent.EventCase.UnitGiven e ->
            GameEvent.UnitGiven(e.UnitId, e.OldTeamId, e.NewTeamId)
        | Highbar.EngineEvent.EventCase.UnitCaptured e ->
            GameEvent.UnitCaptured(e.UnitId, e.OldTeamId, e.NewTeamId)
        | Highbar.EngineEvent.EventCase.EnemyEnterLos e ->
            GameEvent.EnemyEnterLOS(e.EnemyId)
        | Highbar.EngineEvent.EventCase.EnemyLeaveLos e ->
            GameEvent.EnemyLeaveLOS(e.EnemyId)
        | Highbar.EngineEvent.EventCase.EnemyEnterRadar e ->
            GameEvent.EnemyEnterRadar(e.EnemyId)
        | Highbar.EngineEvent.EventCase.EnemyLeaveRadar e ->
            GameEvent.EnemyLeaveRadar(e.EnemyId)
        | Highbar.EngineEvent.EventCase.EnemyDamaged e ->
            GameEvent.EnemyDamaged(e.EnemyId, e.AttackerId, e.Damage, e.WeaponDefId)
        | Highbar.EngineEvent.EventCase.EnemyDestroyed e ->
            GameEvent.EnemyDestroyed(e.EnemyId, e.AttackerId)
        | Highbar.EngineEvent.EventCase.WeaponFired e ->
            GameEvent.WeaponFired(e.UnitId, e.WeaponDefId)
        | Highbar.EngineEvent.EventCase.PlayerCommand e ->
            GameEvent.PlayerCommand(e.Units, e.CommandTopicId, e.CommandId)
        | Highbar.EngineEvent.EventCase.SeismicPing e ->
            let pos = e.Position |> Option.defaultValue { X = 0.0f; Y = 0.0f; Z = 0.0f }
            GameEvent.SeismicPing(pos.X, pos.Y, pos.Z, e.Strength)
        | Highbar.EngineEvent.EventCase.CommandFinished e ->
            GameEvent.CommandFinished(e.UnitId, e.CommandId, e.CommandTopicId)
        | Highbar.EngineEvent.EventCase.Load _ ->
            GameEvent.Load
        | Highbar.EngineEvent.EventCase.Save _ ->
            GameEvent.Save
        | Highbar.EngineEvent.EventCase.EnemyCreated e ->
            GameEvent.EnemyCreated(e.EnemyId)
        | Highbar.EngineEvent.EventCase.EnemyFinished e ->
            GameEvent.EnemyFinished(e.EnemyId)
        | Highbar.EngineEvent.EventCase.LuaMessage e ->
            GameEvent.LuaMessage(e.Data, e.InMessageId)
        | Highbar.EngineEvent.EventCase.None ->
            GameEvent.Unknown
