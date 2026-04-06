namespace FSBar.Client

/// <summary>
/// Discriminated union representing all game events that the BAR engine can emit
/// during an AI session. Each case corresponds to a protobuf event from the HighBar V2 proxy.
/// </summary>
[<RequireQualifiedAccess>]
type GameEvent =
    /// <summary>AI initialization with the assigned team ID. First event received after handshake.</summary>
    | Init of teamId: int
    /// <summary>AI release signal. The engine is shutting down this AI's participation.</summary>
    | Release
    /// <summary>Frame update tick. Carries the current simulation frame number.</summary>
    | Update of frame: int
    /// <summary>Chat or system message from a player.</summary>
    | Message of player: int * message: string
    /// <summary>A friendly unit has been created (construction started). Includes the builder's unit ID.</summary>
    | UnitCreated of unitId: int * builderId: int
    /// <summary>A friendly unit has finished construction and is now fully operational.</summary>
    | UnitFinished of unitId: int
    /// <summary>A friendly unit has become idle (completed its command queue).</summary>
    | UnitIdle of unitId: int
    /// <summary>A friendly unit failed to reach its move destination (path blocked or unreachable).</summary>
    | UnitMoveFailed of unitId: int
    /// <summary>A friendly unit took damage. Includes attacker (if known), damage amount, weapon def, and paralyzer flag.</summary>
    | UnitDamaged of unitId: int * attackerId: int option * damage: float32 * weaponDefId: int * isParalyzer: bool
    /// <summary>A friendly unit was destroyed. Includes the attacker's unit ID if known.</summary>
    | UnitDestroyed of unitId: int * attackerId: int option
    /// <summary>A friendly unit was given to another team (e.g., via share).</summary>
    | UnitGiven of unitId: int * oldTeamId: int * newTeamId: int
    /// <summary>A friendly unit was captured by another team.</summary>
    | UnitCaptured of unitId: int * oldTeamId: int * newTeamId: int
    /// <summary>An enemy unit entered our line of sight.</summary>
    | EnemyEnterLOS of enemyId: int
    /// <summary>An enemy unit left our line of sight.</summary>
    | EnemyLeaveLOS of enemyId: int
    /// <summary>An enemy unit entered our radar coverage.</summary>
    | EnemyEnterRadar of enemyId: int
    /// <summary>An enemy unit left our radar coverage.</summary>
    | EnemyLeaveRadar of enemyId: int
    /// <summary>An enemy unit took damage. Includes attacker (if known), damage amount, and weapon def.</summary>
    | EnemyDamaged of enemyId: int * attackerId: int option * damage: float32 * weaponDefId: int
    /// <summary>An enemy unit was destroyed. Includes the attacker's unit ID if known.</summary>
    | EnemyDestroyed of enemyId: int * attackerId: int option
    /// <summary>A friendly unit fired a weapon.</summary>
    | WeaponFired of unitId: int * weaponDefId: int
    /// <summary>A player issued a command to one or more units.</summary>
    | PlayerCommand of units: int list * commandTopicId: int * commandId: int
    /// <summary>A seismic ping was detected at the given world position with a given strength.</summary>
    | SeismicPing of x: float32 * y: float32 * z: float32 * strength: float32
    /// <summary>A unit's command finished execution.</summary>
    | CommandFinished of unitId: int * commandId: int * commandTopicId: int
    /// <summary>Game load event (save/load system).</summary>
    | Load
    /// <summary>Game save event (save/load system).</summary>
    | Save
    /// <summary>An enemy unit was created (first sighted during construction).</summary>
    | EnemyCreated of enemyId: int
    /// <summary>An enemy unit finished construction.</summary>
    | EnemyFinished of enemyId: int
    /// <summary>A Lua message was received from the game's widget/gadget layer.</summary>
    | LuaMessage of data: string * inMessageId: int
    /// <summary>The game is shutting down. The reason indicates why (GameOver, Disconnect, Error).</summary>
    | Shutdown of reason: string
    /// <summary>An unrecognized event type. Indicates a protocol extension or unknown event.</summary>
    | Unknown

/// <summary>
/// A single simulation frame received from the engine, containing the frame number
/// and all events that occurred during that frame.
/// </summary>
type GameFrame = {
    /// <summary>The engine simulation frame number (monotonically increasing).</summary>
    FrameNumber: uint32
    /// <summary>All game events that occurred during this frame.</summary>
    Events: GameEvent list
}

/// <summary>Functions for converting protobuf engine events to typed <see cref="T:FSBar.Client.GameEvent"/> values.</summary>
module Events =

    let private shutdownReasonToString (reason: Highbar.ShutdownReason) =
        match reason with
        | Highbar.ShutdownReason.GameOver -> "GameOver"
        | Highbar.ShutdownReason.Disconnect -> "Disconnect"
        | Highbar.ShutdownReason.Error -> "Error"
        | _ -> "Unknown"

    /// <summary>
    /// Converts a protobuf <see cref="T:Highbar.EngineEvent"/> into a typed <see cref="T:FSBar.Client.GameEvent"/>.
    /// Unrecognized event types map to <see cref="F:FSBar.Client.GameEvent.Unknown"/>.
    /// </summary>
    /// <param name="engineEvent">The raw protobuf engine event from the HighBar V2 proxy.</param>
    /// <returns>The corresponding typed game event.</returns>
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
