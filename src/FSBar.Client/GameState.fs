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

    /// Pure mapper from a batched <c>GameStateSnapshotResult</c> to an
    /// updated <c>GameState</c>. Extracted from the <c>GameEvent.Update</c>
    /// branch so it can be unit-tested without a live stream. Preserves
    /// <c>MaxHealth</c>, <c>IsFinished</c>, and <c>IsIdle</c> from prior
    /// state where available; clears <c>InLOS</c>/<c>InRadar</c>/<c>Health</c>
    /// on enemies absent from the snapshot (they keep their last-known
    /// position per FR-007).
    let applySnapshot (state: GameState) (snap: GameStateSnapshotResult) : GameState =
        let updatedUnits =
            snap.Friendlies
            |> List.map (fun f ->
                let prior = Map.tryFind f.UnitId state.Units
                let posChanged =
                    match prior with
                    | Some p -> p.Position <> f.Position
                    | None -> true
                let maxHealth =
                    match prior with
                    | Some p -> p.MaxHealth
                    | None -> 0.0f
                let isFinished =
                    match prior with
                    | Some p -> p.IsFinished
                    | None -> false
                let isIdle =
                    match prior with
                    | Some p -> if posChanged then false else p.IsIdle
                    | None -> false
                f.UnitId,
                { UnitId = f.UnitId
                  DefId = f.UnitDefId
                  Position = f.Position
                  Health = f.Health
                  MaxHealth = maxHealth
                  IsFinished = isFinished
                  IsIdle = isIdle })
            |> Map.ofList

        let baseline =
            state.Enemies
            |> Map.map (fun _ e ->
                { e with InLOS = false; InRadar = false; Health = None })

        let withLos =
            snap.LosEnemies
            |> List.fold (fun acc l ->
                let prior = Map.tryFind l.UnitId acc
                let updated =
                    match prior with
                    | Some e ->
                        { e with
                            InLOS = true
                            Position = l.Position
                            Health = Some l.Health
                            DefId = Some l.UnitDefId }
                    | None ->
                        { EnemyId = l.UnitId
                          DefId = Some l.UnitDefId
                          Position = l.Position
                          Health = Some l.Health
                          InLOS = true
                          InRadar = false }
                Map.add l.UnitId updated acc) baseline

        let updatedEnemies =
            snap.RadarOnlyEnemies
            |> List.fold (fun acc r ->
                let prior = Map.tryFind r.UnitId acc
                let updated =
                    match prior with
                    | Some e ->
                        { e with
                            InRadar = true
                            Position = r.Position
                            Health = None
                            DefId = e.DefId |> Option.orElse (Some r.UnitDefId) }
                    | None ->
                        { EnemyId = r.UnitId
                          DefId = Some r.UnitDefId
                          Position = r.Position
                          Health = None
                          InLOS = false
                          InRadar = true }
                Map.add r.UnitId updated acc) withLos

        let eco = snap.Economy
        let metal : EconomySnapshot =
            { Current = eco.MetalCurrent
              Income = eco.MetalIncome
              Usage = eco.MetalUsage
              Storage = eco.MetalStorage }
        let energy : EconomySnapshot =
            { Current = eco.EnergyCurrent
              Income = eco.EnergyIncome
              Usage = eco.EnergyUsage
              Storage = eco.EnergyStorage }

        { state with
            Units = updatedUnits
            Enemies = updatedEnemies
            Metal = metal
            Energy = energy }

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
            let pos = Callbacks.getUnitPos stream enemyId
            match Map.tryFind enemyId state.Enemies with
            | Some enemy ->
                let newPos = if enemy.InLOS then enemy.Position else pos
                { state with Enemies = Map.add enemyId { enemy with InRadar = true; Position = newPos } state.Enemies }
            | None ->
                let enemy =
                    { EnemyId = enemyId
                      DefId = None
                      Position = pos
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
            // Spec 045: single-RPC batched snapshot replaces the legacy
            // per-unit / per-enemy / per-resource refresh. Any snapshot
            // failure leaves prior state untouched and propagates.
            let snap = Callbacks.getGameStateSnapshot stream
            applySnapshot state snap

        | _ -> state


    let processFrame (state: GameState) (frame: GameFrame) (stream: NetworkStream) : GameState =
        let mutable s = { state with FrameNumber = frame.FrameNumber; Events = frame.Events }
        for evt in frame.Events do
            s <- processEvent s stream evt
        s
