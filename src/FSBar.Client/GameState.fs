namespace FSBar.Client

open System.Net.Sockets

type EconomySnapshot = {
    Current: float32
    Income: float32
    Usage: float32
    Storage: float32
}

type TrackedUnit = {
    UnitId: int
    DefId: int
    Position: float32 * float32 * float32
    Health: float32
    MaxHealth: float32
    IsFinished: bool
    IsIdle: bool
}

type TrackedEnemy = {
    EnemyId: int
    DefId: int option
    Position: float32 * float32 * float32
    Health: float32 option
    InLOS: bool
    InRadar: bool
}

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

module GameState =
    let emptyEconomy : EconomySnapshot =
        { Current = 0.0f; Income = 0.0f; Usage = 0.0f; Storage = 0.0f }

    let empty : GameState =
        { FrameNumber = 0u
          TeamId = 0
          Units = Map.empty
          Enemies = Map.empty
          Metal = emptyEconomy
          Energy = emptyEconomy
          UnitDefs = UnitDefCache.empty
          Events = [] }

    let refreshEconomy (stream: NetworkStream) (resourceId: int) : EconomySnapshot =
        { Current = Callbacks.getEconomyCurrent stream resourceId
          Income = Callbacks.getEconomyIncome stream resourceId
          Usage = Callbacks.getEconomyUsage stream resourceId
          Storage = Callbacks.getEconomyStorage stream resourceId }

    let refreshUnit (stream: NetworkStream) (unit: TrackedUnit) : TrackedUnit =
        let pos = Callbacks.getUnitPos stream unit.UnitId
        let health = Callbacks.getUnitHealth stream unit.UnitId
        let posChanged = pos <> unit.Position
        { unit with
            Position = pos
            Health = health
            IsIdle = if posChanged then false else unit.IsIdle }

    let processEvent (state: GameState) (stream: NetworkStream) (evt: GameEvent) : GameState =
        match evt with
        | GameEvent.Init teamId ->
            { state with TeamId = teamId }

        | GameEvent.UnitCreated (unitId, _builderId) ->
            let defId = Callbacks.getUnitDef stream unitId
            let pos = Callbacks.getUnitPos stream unitId
            let health = Callbacks.getUnitHealth stream unitId
            let maxHealth = Callbacks.getUnitMaxHealth stream unitId
            let unit =
                { UnitId = unitId
                  DefId = defId
                  Position = pos
                  Health = health
                  MaxHealth = maxHealth
                  IsFinished = false
                  IsIdle = false }
            { state with Units = Map.add unitId unit state.Units }

        | GameEvent.UnitFinished unitId ->
            match Map.tryFind unitId state.Units with
            | Some unit -> { state with Units = Map.add unitId { unit with IsFinished = true } state.Units }
            | None -> state

        | GameEvent.UnitIdle unitId ->
            match Map.tryFind unitId state.Units with
            | Some unit -> { state with Units = Map.add unitId { unit with IsIdle = true } state.Units }
            | None -> state

        | GameEvent.UnitDestroyed (unitId, _) ->
            { state with Units = Map.remove unitId state.Units }

        | GameEvent.UnitGiven (unitId, _oldTeam, newTeam) ->
            if newTeam <> state.TeamId then
                { state with Units = Map.remove unitId state.Units }
            else
                state

        | GameEvent.UnitCaptured (unitId, _oldTeam, newTeam) ->
            if newTeam <> state.TeamId then
                { state with Units = Map.remove unitId state.Units }
            else
                state

        | GameEvent.EnemyEnterLOS enemyId ->
            let existing = Map.tryFind enemyId state.Enemies
            let pos = Callbacks.getUnitPos stream enemyId
            let health = Callbacks.getUnitHealth stream enemyId
            let defId =
                match existing with
                | Some e -> e.DefId
                | None ->
                    let d = Callbacks.getUnitDef stream enemyId
                    if d > 0 then Some d else None
            let enemy =
                { EnemyId = enemyId
                  DefId = defId
                  Position = pos
                  Health = Some health
                  InLOS = true
                  InRadar = existing |> Option.map (fun e -> e.InRadar) |> Option.defaultValue false }
            { state with Enemies = Map.add enemyId enemy state.Enemies }

        | GameEvent.EnemyLeaveLOS enemyId ->
            match Map.tryFind enemyId state.Enemies with
            | Some enemy -> { state with Enemies = Map.add enemyId { enemy with InLOS = false; Health = None } state.Enemies }
            | None -> state

        | GameEvent.EnemyEnterRadar enemyId ->
            match Map.tryFind enemyId state.Enemies with
            | Some enemy -> { state with Enemies = Map.add enemyId { enemy with InRadar = true } state.Enemies }
            | None ->
                let enemy =
                    { EnemyId = enemyId
                      DefId = None
                      Position = (0.0f, 0.0f, 0.0f)
                      Health = None
                      InLOS = false
                      InRadar = true }
                { state with Enemies = Map.add enemyId enemy state.Enemies }

        | GameEvent.EnemyLeaveRadar enemyId ->
            match Map.tryFind enemyId state.Enemies with
            | Some enemy -> { state with Enemies = Map.add enemyId { enemy with InRadar = false } state.Enemies }
            | None -> state

        | GameEvent.EnemyDestroyed (enemyId, _) ->
            { state with Enemies = Map.remove enemyId state.Enemies }

        | GameEvent.EnemyCreated enemyId ->
            if not (Map.containsKey enemyId state.Enemies) then
                let enemy =
                    { EnemyId = enemyId
                      DefId = None
                      Position = (0.0f, 0.0f, 0.0f)
                      Health = None
                      InLOS = false
                      InRadar = false }
                { state with Enemies = Map.add enemyId enemy state.Enemies }
            else
                state

        | GameEvent.EnemyFinished enemyId ->
            match Map.tryFind enemyId state.Enemies with
            | Some enemy ->
                let defId =
                    match enemy.DefId with
                    | Some _ -> enemy.DefId
                    | None ->
                        let d = Callbacks.getUnitDef stream enemyId
                        if d > 0 then Some d else None
                { state with Enemies = Map.add enemyId { enemy with DefId = defId } state.Enemies }
            | None -> state

        | GameEvent.Update _ ->
            // Refresh positions and health for all tracked friendly units
            let updatedUnits =
                state.Units
                |> Map.map (fun _id unit -> refreshUnit stream unit)
            let metal = refreshEconomy stream 0
            let energy = refreshEconomy stream 1
            { state with
                Units = updatedUnits
                Metal = metal
                Energy = energy }

        | _ -> state

    let processFrame (state: GameState) (frame: GameFrame) (stream: NetworkStream) : GameState =
        let mutable s = { state with FrameNumber = frame.FrameNumber; Events = frame.Events }
        for evt in frame.Events do
            s <- processEvent s stream evt
        s
