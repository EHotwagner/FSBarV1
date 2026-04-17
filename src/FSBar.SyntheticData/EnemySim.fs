namespace FSBar.SyntheticData

open FSBar.Client

module EnemySim =

    type VisState =
        | NotVisible
        | RadarOnly
        | InLineOfSight

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

    let pseudoRandom (seed: int) (range: float32) : float32 =
        let s = abs ((seed * 1103515245 + 12345) % 2147483647)
        (float32 s / 2147483647.0f) * range

    let create
        (enemyId: int)
        (defId: int)
        (maxHealth: float32)
        (position: float32 * float32 * float32)
        (speed: float32)
        (transitions: (int * VisState) list)
        : SimEnemy =
        let (x, _, z) = position
        { Enemy =
            { EnemyId = enemyId
              DefId = None
              Position = position
              Health = None
              InLOS = false
              InRadar = false }
          DefId = defId
          MaxHealth = maxHealth
          Position = position
          VisState = NotVisible
          Transitions = transitions |> List.sortBy fst
          Speed = speed
          TargetX = x + 200.0f
          TargetZ = z + 200.0f }

    let step (se: SimEnemy) (frame: int) (mapWidth: float32) (mapHeight: float32) : SimEnemy * GameEvent list =
        // Move enemy
        let (x, y, z) = se.Position
        let movedPos =
            if se.Speed <= 0.0f then se.Position
            else
                let dx = se.TargetX - x
                let dz = se.TargetZ - z
                let dist = sqrt (dx * dx + dz * dz)
                if dist < se.Speed then
                    let ntx = pseudoRandom (frame * 41 + se.Enemy.EnemyId * 19) mapWidth
                    let ntz = pseudoRandom (frame * 43 + se.Enemy.EnemyId * 23) mapHeight
                    // We'll update target below
                    (max 0.0f (min se.TargetX mapWidth), y, max 0.0f (min se.TargetZ mapHeight))
                else
                    let scale = se.Speed / dist
                    let nx = max 0.0f (min (x + dx * scale) mapWidth)
                    let nz = max 0.0f (min (z + dz * scale) mapHeight)
                    (nx, y, nz)

        // Update target if reached
        let newTx, newTz =
            let (mx, _, mz) = movedPos
            let dx = se.TargetX - mx
            let dz = se.TargetZ - mz
            let dist = sqrt (dx * dx + dz * dz)
            if dist < se.Speed then
                pseudoRandom (frame * 41 + se.Enemy.EnemyId * 19) mapWidth,
                pseudoRandom (frame * 43 + se.Enemy.EnemyId * 23) mapHeight
            else
                se.TargetX, se.TargetZ

        // Check for visibility transitions this frame
        let transitionsThisFrame, remainingTransitions =
            se.Transitions |> List.partition (fun (f, _) -> f = frame)

        let mutable newVisState = se.VisState
        let mutable events = []

        for (_, targetVis) in transitionsThisFrame do
            let oldVis = newVisState
            newVisState <- targetVis
            match oldVis, targetVis with
            | NotVisible, RadarOnly ->
                events <- GameEvent.EnemyEnterRadar se.Enemy.EnemyId :: events
            | NotVisible, InLineOfSight ->
                events <- GameEvent.EnemyEnterRadar se.Enemy.EnemyId :: events
                events <- GameEvent.EnemyEnterLOS se.Enemy.EnemyId :: events
            | RadarOnly, InLineOfSight ->
                events <- GameEvent.EnemyEnterLOS se.Enemy.EnemyId :: events
            | InLineOfSight, RadarOnly ->
                events <- GameEvent.EnemyLeaveLOS se.Enemy.EnemyId :: events
            | InLineOfSight, NotVisible ->
                events <- GameEvent.EnemyLeaveLOS se.Enemy.EnemyId :: events
                events <- GameEvent.EnemyLeaveRadar se.Enemy.EnemyId :: events
            | RadarOnly, NotVisible ->
                events <- GameEvent.EnemyLeaveRadar se.Enemy.EnemyId :: events
            | _ -> ()

        // Build updated TrackedEnemy
        let updatedEnemy =
            match newVisState with
            | NotVisible ->
                { se.Enemy with
                    Position = movedPos
                    InLOS = false
                    InRadar = false
                    Health = None
                    DefId = None }
            | RadarOnly ->
                { se.Enemy with
                    Position = movedPos
                    InLOS = false
                    InRadar = true
                    Health = None
                    DefId = None }
            | InLineOfSight ->
                { se.Enemy with
                    Position = movedPos
                    InLOS = true
                    InRadar = true
                    Health = Some se.MaxHealth
                    DefId = Some se.DefId }

        let updatedSe =
            { se with
                Enemy = updatedEnemy
                Position = movedPos
                VisState = newVisState
                Transitions = remainingTransitions
                TargetX = newTx
                TargetZ = newTz }

        updatedSe, List.rev events
