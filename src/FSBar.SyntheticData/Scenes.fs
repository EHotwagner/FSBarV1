namespace FSBar.SyntheticData

open FSBar.Client

module Scenes =

    // --- Scheduled action types ---
    type private ScheduledAction =
        | SpawnUnit of unitId: int * defId: int * position: (float32 * float32 * float32) * builderId: int
        | FinishUnit of unitId: int
        | SpawnEnemy of enemyId: int * defId: int * position: (float32 * float32 * float32)
        | DestroyUnit of unitId: int
        | DamageUnit of unitId: int * attackerId: int option * damage: float32 * weaponDefId: int
        | DamageEnemy of enemyId: int * attackerId: int option * damage: float32 * weaponDefId: int
        | FireWeapon of unitId: int * weaponDefId: int
        | DestroyEnemy of enemyId: int
        | AddIncome of metalIncome: float32 * energyIncome: float32
        | AddUsage of metalUsage: float32 * energyUsage: float32

    let private generateScene
        (sceneId: SceneId)
        (name: string)
        (mapWidth: float32)
        (mapHeight: float32)
        (unitDefs: UnitDefCache)
        (initialUnits: (int * int * (float32 * float32 * float32)) list)
        (initialEnemies: (int * int * (float32 * float32 * float32) * float32 * EnemySim.VisState * (int * EnemySim.VisState) list) list)
        (scheduledActions: (int * ScheduledAction) list)
        (initialMetal: EconomySnapshot)
        (initialEnergy: EconomySnapshot)
        : Scene =

        let mutable units = Map.empty<int, UnitSim.MovingUnit>
        let mutable enemies = Map.empty<int, EnemySim.SimEnemy>
        let mutable metal = initialMetal
        let mutable energy = initialEnergy
        let mutable createdUnitIds = Set.empty<int>

        // Initialize friendly units
        for (unitId, defId, pos) in initialUnits do
            let maxHealth = UnitDefs.maxHealthFor defId unitDefs
            let speed = UnitDefs.speedFor defId unitDefs
            let tracked =
                { UnitId = unitId
                  DefId = defId
                  Position = pos
                  Health = maxHealth
                  MaxHealth = maxHealth
                  IsFinished = true
                  IsIdle = true }
            units <- Map.add unitId (UnitSim.create tracked speed mapWidth mapHeight (unitId * 7)) units
            createdUnitIds <- Set.add unitId createdUnitIds

        // Initialize enemies
        for (enemyId, defId, pos, speed, _initVis, transitions) in initialEnemies do
            let maxHealth = UnitDefs.maxHealthFor defId unitDefs
            enemies <- Map.add enemyId (EnemySim.create enemyId defId maxHealth pos speed transitions) enemies

        // Sort scheduled actions by frame
        let actionsByFrame =
            scheduledActions
            |> List.groupBy fst
            |> List.map (fun (f, acts) -> f, acts |> List.map snd)
            |> Map.ofList

        let frames = Array.zeroCreate<GameState> 300
        let gameFrames = Array.zeroCreate<GameFrame> 300

        for frameIdx in 0 .. 299 do
            let frameNum = frameIdx + 1
            let mutable events = []

            // Frame 1: Init event
            if frameNum = 1 then
                events <- GameEvent.Init 0 :: events
                // Emit UnitCreated + UnitFinished for initial units
                for (unitId, defId, _) in initialUnits do
                    events <- GameEvent.UnitCreated(unitId, 0) :: events
                    events <- GameEvent.UnitFinished unitId :: events
                // Emit EnemyCreated for initial enemies
                for (enemyId, _, _, _, _, _) in initialEnemies do
                    events <- GameEvent.EnemyCreated enemyId :: events

            // Process scheduled actions for this frame
            match Map.tryFind frameNum actionsByFrame with
            | Some actions ->
                for action in actions do
                    match action with
                    | SpawnUnit (unitId, defId, pos, builderId) ->
                        let maxHealth = UnitDefs.maxHealthFor defId unitDefs
                        let speed = UnitDefs.speedFor defId unitDefs
                        let tracked =
                            { UnitId = unitId
                              DefId = defId
                              Position = pos
                              Health = maxHealth * 0.1f // starts at 10% during construction
                              MaxHealth = maxHealth
                              IsFinished = false
                              IsIdle = false }
                        units <- Map.add unitId (UnitSim.create tracked speed mapWidth mapHeight (unitId * 7 + frameNum)) units
                        createdUnitIds <- Set.add unitId createdUnitIds
                        events <- GameEvent.UnitCreated(unitId, builderId) :: events

                    | FinishUnit unitId ->
                        match Map.tryFind unitId units with
                        | Some mu ->
                            let finished = { mu.Unit with IsFinished = true; Health = mu.Unit.MaxHealth }
                            units <- Map.add unitId { mu with Unit = finished } units
                            events <- GameEvent.UnitFinished unitId :: events
                        | None -> ()

                    | SpawnEnemy (enemyId, defId, pos) ->
                        if not (Map.containsKey enemyId enemies) then
                            let maxHealth = UnitDefs.maxHealthFor defId unitDefs
                            let speed = UnitDefs.speedFor defId unitDefs
                            enemies <- Map.add enemyId (EnemySim.create enemyId defId maxHealth pos speed []) enemies
                            events <- GameEvent.EnemyCreated enemyId :: events

                    | DestroyUnit unitId ->
                        units <- Map.remove unitId units
                        events <- GameEvent.UnitDestroyed(unitId, None) :: events

                    | DamageUnit (unitId, attackerId, damage, weaponDefId) ->
                        match Map.tryFind unitId units with
                        | Some mu ->
                            let damaged = { mu.Unit with Health = max 0.0f (mu.Unit.Health - damage) }
                            units <- Map.add unitId { mu with Unit = damaged } units
                            events <- GameEvent.UnitDamaged(unitId, attackerId, damage, weaponDefId, false) :: events
                        | None -> ()

                    | DamageEnemy (enemyId, attackerId, damage, weaponDefId) ->
                        match Map.tryFind enemyId enemies with
                        | Some se ->
                            let newHealth =
                                match se.Enemy.Health with
                                | Some h -> Some (max 0.0f (h - damage))
                                | None -> None
                            let updatedEnemy = { se.Enemy with Health = newHealth }
                            enemies <- Map.add enemyId { se with Enemy = updatedEnemy } enemies
                            events <- GameEvent.EnemyDamaged(enemyId, attackerId, damage, weaponDefId) :: events
                        | None -> ()

                    | FireWeapon (unitId, weaponDefId) ->
                        events <- GameEvent.WeaponFired(unitId, weaponDefId) :: events

                    | DestroyEnemy enemyId ->
                        enemies <- Map.remove enemyId enemies
                        events <- GameEvent.EnemyDestroyed(enemyId, None) :: events

                    | AddIncome (mi, ei) ->
                        metal <- { metal with Income = metal.Income + mi }
                        energy <- { energy with Income = energy.Income + ei }

                    | AddUsage (mu, eu) ->
                        metal <- { metal with Usage = metal.Usage + mu }
                        energy <- { energy with Usage = energy.Usage + eu }
            | None -> ()

            // Step unit movement
            let mutable updatedUnits = Map.empty
            for kv in units do
                let mu = UnitSim.step kv.Value mapWidth mapHeight frameNum
                updatedUnits <- Map.add kv.Key mu updatedUnits
            units <- updatedUnits

            // Step enemy simulation
            let mutable updatedEnemies = Map.empty
            for kv in enemies do
                let (se, enemyEvents) = EnemySim.step kv.Value frameNum mapWidth mapHeight
                updatedEnemies <- Map.add kv.Key se updatedEnemies
                events <- (List.rev enemyEvents) @ events
            enemies <- updatedEnemies

            // Step economy
            metal <- EconomySim.step metal
            energy <- EconomySim.step energy

            // Update event (always present)
            events <- GameEvent.Update frameNum :: events

            let eventList = List.rev events

            // Build unit map for GameState
            let unitMap =
                units |> Map.map (fun _ mu -> mu.Unit)

            // Build enemy map for GameState (only visible enemies)
            let enemyMap =
                enemies
                |> Map.map (fun _ se -> se.Enemy)

            let state =
                { FrameNumber = uint32 frameNum
                  TeamId = 0
                  Units = unitMap
                  Enemies = enemyMap
                  Metal = metal
                  Energy = energy
                  UnitDefs = unitDefs
                  Events = eventList }

            let gameFrame =
                { FrameNumber = uint32 frameNum
                  Events = eventList }

            frames.[frameIdx] <- state
            gameFrames.[frameIdx] <- gameFrame

        { Id = sceneId
          Name = name
          MapWidth = mapWidth
          MapHeight = mapHeight
          Frames = frames
          GameFrames = gameFrames
          UnitDefs = unitDefs }

    // ============================================================
    // Scene A: Small Map - Early Game Buildup (4096x4096)
    // ============================================================
    let private generateSceneA () =
        let unitDefs = UnitDefs.sceneA
        let initialUnits = [
            (1, UnitDefs.ArmCommander, (2048.0f, 100.0f, 2048.0f))
        ]
        let initialEnemies = [
            (1000, UnitDefs.CorCommander, (3500.0f, 100.0f, 3500.0f), 1.5f, EnemySim.NotVisible,
                [ (150, EnemySim.RadarOnly); (200, EnemySim.InLineOfSight); (240, EnemySim.RadarOnly); (270, EnemySim.NotVisible) ])
            (1001, UnitDefs.CorGator, (3800.0f, 100.0f, 3200.0f), 4.0f, EnemySim.NotVisible,
                [ (180, EnemySim.RadarOnly); (210, EnemySim.InLineOfSight); (250, EnemySim.RadarOnly) ])
            (1002, UnitDefs.CorThud, (3600.0f, 100.0f, 3800.0f), 2.0f, EnemySim.NotVisible,
                [ (220, EnemySim.InLineOfSight); (260, EnemySim.RadarOnly); (290, EnemySim.NotVisible) ])
        ]
        let scheduledActions = [
            // Commander builds mex
            (10, SpawnUnit(2, UnitDefs.ArmMex, (1900.0f, 100.0f, 2000.0f), 1))
            (20, FinishUnit 2)
            (20, AddIncome(2.0f, 0.0f))
            // Commander builds solar
            (25, SpawnUnit(3, UnitDefs.ArmSolar, (2100.0f, 100.0f, 1950.0f), 1))
            (40, FinishUnit 3)
            (40, AddIncome(0.0f, 20.0f))
            // Commander builds second mex
            (45, SpawnUnit(4, UnitDefs.ArmMex, (2200.0f, 100.0f, 2100.0f), 1))
            (55, FinishUnit 4)
            (55, AddIncome(2.0f, 0.0f))
            // Commander builds wind
            (60, SpawnUnit(5, UnitDefs.ArmWind, (1850.0f, 100.0f, 2150.0f), 1))
            (70, FinishUnit 5)
            (70, AddIncome(0.0f, 5.0f))
            // Commander builds lab
            (40, SpawnUnit(6, UnitDefs.ArmLab, (2000.0f, 100.0f, 1800.0f), 1))
            (75, FinishUnit 6)
            (75, AddUsage(3.0f, 5.0f))
            // Lab produces peewee 1
            (80, SpawnUnit(7, UnitDefs.ArmPeewee, (2000.0f, 100.0f, 1750.0f), 6))
            (100, FinishUnit 7)
            // Lab produces peewee 2
            (105, SpawnUnit(8, UnitDefs.ArmPeewee, (2000.0f, 100.0f, 1750.0f), 6))
            (125, FinishUnit 8)
            // Lab produces flash
            (130, SpawnUnit(9, UnitDefs.ArmFlash, (2000.0f, 100.0f, 1750.0f), 6))
            (150, FinishUnit 9)
            // Lab produces rockko
            (155, SpawnUnit(10, UnitDefs.ArmRockko, (2000.0f, 100.0f, 1750.0f), 6))
            (180, FinishUnit 10)
            // Commander builds another solar
            (100, SpawnUnit(11, UnitDefs.ArmSolar, (1800.0f, 100.0f, 1900.0f), 1))
            (115, FinishUnit 11)
            (115, AddIncome(0.0f, 20.0f))
            // Lab produces samson
            (185, SpawnUnit(12, UnitDefs.ArmSamson, (2000.0f, 100.0f, 1750.0f), 6))
            (210, FinishUnit 12)
            // Lab produces peewee 3
            (215, SpawnUnit(13, UnitDefs.ArmPeewee, (2000.0f, 100.0f, 1750.0f), 6))
            (235, FinishUnit 13)
            // Commander builds third mex
            (130, SpawnUnit(14, UnitDefs.ArmMex, (1700.0f, 100.0f, 2200.0f), 1))
            (145, FinishUnit 14)
            (145, AddIncome(2.0f, 0.0f))
            // Lab produces fark
            (240, SpawnUnit(15, UnitDefs.ArmFark, (2000.0f, 100.0f, 1750.0f), 6))
            (260, FinishUnit 15)
        ]
        let initialMetal = { Current = 1000.0f; Income = 0.0f; Usage = 0.0f; Storage = 1000.0f }
        let initialEnergy = { Current = 1000.0f; Income = 0.0f; Usage = 0.0f; Storage = 1000.0f }

        generateScene SceneA "Small Map - Early Game Buildup" 4096.0f 4096.0f unitDefs
            initialUnits initialEnemies scheduledActions initialMetal initialEnergy

    // ============================================================
    // Scene B: Medium Map - Mid-Game Skirmish (8192x8192)
    // ============================================================
    let private generateSceneB () =
        let unitDefs = UnitDefs.sceneB
        // 20 friendly units: commander + 4 mex + 3 solar + 1 lab + 4 peewee + 3 flash + 2 rockko + 2 samson
        let initialUnits = [
            (1, UnitDefs.ArmCommander, (3000.0f, 100.0f, 4000.0f))
            (2, UnitDefs.ArmMex, (2800.0f, 100.0f, 3800.0f))
            (3, UnitDefs.ArmMex, (3200.0f, 100.0f, 3900.0f))
            (4, UnitDefs.ArmMex, (2700.0f, 100.0f, 4200.0f))
            (5, UnitDefs.ArmMex, (3400.0f, 100.0f, 4100.0f))
            (6, UnitDefs.ArmSolar, (2900.0f, 100.0f, 3700.0f))
            (7, UnitDefs.ArmSolar, (3100.0f, 100.0f, 3600.0f))
            (8, UnitDefs.ArmSolar, (3300.0f, 100.0f, 3700.0f))
            (9, UnitDefs.ArmLab, (3000.0f, 100.0f, 3500.0f))
            (10, UnitDefs.ArmPeewee, (4000.0f, 100.0f, 5000.0f))
            (11, UnitDefs.ArmPeewee, (4100.0f, 100.0f, 5100.0f))
            (12, UnitDefs.ArmPeewee, (4200.0f, 100.0f, 4900.0f))
            (13, UnitDefs.ArmPeewee, (3900.0f, 100.0f, 5200.0f))
            (14, UnitDefs.ArmFlash, (4300.0f, 100.0f, 5000.0f))
            (15, UnitDefs.ArmFlash, (4400.0f, 100.0f, 5100.0f))
            (16, UnitDefs.ArmFlash, (4500.0f, 100.0f, 4800.0f))
            (17, UnitDefs.ArmRockko, (4000.0f, 100.0f, 4700.0f))
            (18, UnitDefs.ArmRockko, (4100.0f, 100.0f, 4600.0f))
            (19, UnitDefs.ArmSamson, (3800.0f, 100.0f, 4800.0f))
            (20, UnitDefs.ArmSamson, (3700.0f, 100.0f, 4900.0f))
        ]
        // 15 enemies
        let initialEnemies = [
            (1000, UnitDefs.CorCommander, (6000.0f, 100.0f, 5000.0f), 1.5f, EnemySim.NotVisible,
                [ (10, EnemySim.RadarOnly); (30, EnemySim.InLineOfSight) ])
            (1001, UnitDefs.CorGator, (5500.0f, 100.0f, 5200.0f), 4.0f, EnemySim.NotVisible,
                [ (5, EnemySim.RadarOnly); (20, EnemySim.InLineOfSight) ])
            (1002, UnitDefs.CorGator, (5600.0f, 100.0f, 5300.0f), 4.0f, EnemySim.NotVisible,
                [ (5, EnemySim.RadarOnly); (20, EnemySim.InLineOfSight) ])
            (1003, UnitDefs.CorGator, (5400.0f, 100.0f, 5100.0f), 4.0f, EnemySim.NotVisible,
                [ (8, EnemySim.RadarOnly); (25, EnemySim.InLineOfSight) ])
            (1004, UnitDefs.CorThud, (5700.0f, 100.0f, 4800.0f), 2.0f, EnemySim.NotVisible,
                [ (10, EnemySim.RadarOnly); (30, EnemySim.InLineOfSight) ])
            (1005, UnitDefs.CorThud, (5800.0f, 100.0f, 4900.0f), 2.0f, EnemySim.NotVisible,
                [ (10, EnemySim.RadarOnly); (30, EnemySim.InLineOfSight) ])
            (1006, UnitDefs.CorStorm, (5300.0f, 100.0f, 5400.0f), 3.0f, EnemySim.NotVisible,
                [ (8, EnemySim.RadarOnly); (25, EnemySim.InLineOfSight) ])
            (1007, UnitDefs.CorStorm, (5200.0f, 100.0f, 5500.0f), 3.0f, EnemySim.NotVisible,
                [ (12, EnemySim.RadarOnly); (28, EnemySim.InLineOfSight) ])
            (1008, UnitDefs.CorGator, (5900.0f, 100.0f, 5000.0f), 4.0f, EnemySim.NotVisible,
                [ (15, EnemySim.RadarOnly); (35, EnemySim.InLineOfSight) ])
            (1009, UnitDefs.CorThud, (6100.0f, 100.0f, 4700.0f), 2.0f, EnemySim.NotVisible,
                [ (20, EnemySim.RadarOnly); (40, EnemySim.InLineOfSight) ])
            (1010, UnitDefs.CorStorm, (5100.0f, 100.0f, 5600.0f), 3.0f, EnemySim.NotVisible,
                [ (15, EnemySim.RadarOnly); (35, EnemySim.InLineOfSight) ])
            (1011, UnitDefs.CorGator, (5500.0f, 100.0f, 4600.0f), 4.0f, EnemySim.NotVisible,
                [ (18, EnemySim.RadarOnly); (38, EnemySim.InLineOfSight) ])
            (1012, UnitDefs.CorGator, (5600.0f, 100.0f, 4500.0f), 4.0f, EnemySim.NotVisible,
                [ (22, EnemySim.RadarOnly); (42, EnemySim.InLineOfSight) ])
            (1013, UnitDefs.CorStorm, (5400.0f, 100.0f, 5700.0f), 3.0f, EnemySim.NotVisible,
                [ (25, EnemySim.RadarOnly); (45, EnemySim.InLineOfSight) ])
            (1014, UnitDefs.CorThud, (6200.0f, 100.0f, 5100.0f), 2.0f, EnemySim.NotVisible,
                [ (20, EnemySim.RadarOnly); (40, EnemySim.InLineOfSight) ])
        ]
        // Combat actions from frame 30 onward
        let scheduledActions = [
            // Combat: weapon fires and damage exchanges
            (35, FireWeapon(10, UnitDefs.ArmPeewee))
            (35, DamageEnemy(1001, Some 10, 25.0f, UnitDefs.ArmPeewee))
            (36, FireWeapon(11, UnitDefs.ArmPeewee))
            (36, DamageEnemy(1002, Some 11, 25.0f, UnitDefs.ArmPeewee))
            (38, FireWeapon(14, UnitDefs.ArmFlash))
            (38, DamageEnemy(1003, Some 14, 30.0f, UnitDefs.ArmFlash))
            (40, DamageUnit(10, Some 1001, 35.0f, UnitDefs.CorGator))
            (42, FireWeapon(17, UnitDefs.ArmRockko))
            (42, DamageEnemy(1004, Some 17, 80.0f, UnitDefs.ArmRockko))
            (45, DamageUnit(11, Some 1002, 35.0f, UnitDefs.CorGator))
            (48, FireWeapon(10, UnitDefs.ArmPeewee))
            (48, DamageEnemy(1001, Some 10, 25.0f, UnitDefs.ArmPeewee))
            (50, FireWeapon(12, UnitDefs.ArmPeewee))
            (50, DamageEnemy(1006, Some 12, 25.0f, UnitDefs.ArmPeewee))
            (55, DamageUnit(14, Some 1003, 40.0f, UnitDefs.CorGator))
            (58, FireWeapon(15, UnitDefs.ArmFlash))
            (58, DamageEnemy(1007, Some 15, 30.0f, UnitDefs.ArmFlash))
            (60, DamageUnit(12, Some 1006, 30.0f, UnitDefs.CorStorm))
            (65, FireWeapon(17, UnitDefs.ArmRockko))
            (65, DamageEnemy(1004, Some 17, 80.0f, UnitDefs.ArmRockko))
            (70, DamageUnit(10, Some 1001, 35.0f, UnitDefs.CorGator))
            (75, FireWeapon(18, UnitDefs.ArmRockko))
            (75, DamageEnemy(1005, Some 18, 80.0f, UnitDefs.ArmRockko))
            (80, DamageUnit(11, Some 1002, 35.0f, UnitDefs.CorGator))
            // Unit destroyed
            (85, DamageUnit(10, Some 1001, 200.0f, UnitDefs.CorGator))
            (85, DestroyUnit 10)
            (90, FireWeapon(13, UnitDefs.ArmPeewee))
            (90, DamageEnemy(1001, Some 13, 25.0f, UnitDefs.ArmPeewee))
            // More combat
            (100, FireWeapon(14, UnitDefs.ArmFlash))
            (100, DamageEnemy(1008, Some 14, 30.0f, UnitDefs.ArmFlash))
            (105, DamageUnit(14, Some 1008, 40.0f, UnitDefs.CorGator))
            (110, FireWeapon(19, UnitDefs.ArmSamson))
            (110, DamageEnemy(1009, Some 19, 50.0f, UnitDefs.ArmSamson))
            (115, DamageUnit(15, Some 1007, 35.0f, UnitDefs.CorStorm))
            (120, FireWeapon(17, UnitDefs.ArmRockko))
            (120, DamageEnemy(1004, Some 17, 80.0f, UnitDefs.ArmRockko))
            // Enemy destroyed
            (125, DamageEnemy(1004, Some 17, 200.0f, UnitDefs.ArmRockko))
            (125, DestroyEnemy 1004)
            (130, DamageUnit(16, Some 1011, 40.0f, UnitDefs.CorGator))
            (135, FireWeapon(16, UnitDefs.ArmFlash))
            (135, DamageEnemy(1011, Some 16, 30.0f, UnitDefs.ArmFlash))
            // Second unit destroyed
            (140, DamageUnit(11, Some 1002, 200.0f, UnitDefs.CorGator))
            (140, DestroyUnit 11)
            (145, FireWeapon(13, UnitDefs.ArmPeewee))
            (145, DamageEnemy(1002, Some 13, 25.0f, UnitDefs.ArmPeewee))
            (150, FireWeapon(18, UnitDefs.ArmRockko))
            (150, DamageEnemy(1005, Some 18, 80.0f, UnitDefs.ArmRockko))
            (160, DamageUnit(13, Some 1006, 30.0f, UnitDefs.CorStorm))
            (170, FireWeapon(19, UnitDefs.ArmSamson))
            (170, DamageEnemy(1010, Some 19, 50.0f, UnitDefs.ArmSamson))
            // Third unit destroyed
            (180, DamageUnit(14, Some 1008, 200.0f, UnitDefs.CorGator))
            (180, DestroyUnit 14)
            (190, FireWeapon(20, UnitDefs.ArmSamson))
            (190, DamageEnemy(1013, Some 20, 50.0f, UnitDefs.ArmSamson))
            // Second enemy destroyed
            (200, DamageEnemy(1005, Some 18, 200.0f, UnitDefs.ArmRockko))
            (200, DestroyEnemy 1005)
            (210, FireWeapon(17, UnitDefs.ArmRockko))
            (210, DamageEnemy(1009, Some 17, 80.0f, UnitDefs.ArmRockko))
            // Fourth unit destroyed
            (220, DamageUnit(15, Some 1007, 200.0f, UnitDefs.CorStorm))
            (220, DestroyUnit 15)
            (230, FireWeapon(16, UnitDefs.ArmFlash))
            (230, DamageEnemy(1011, Some 16, 30.0f, UnitDefs.ArmFlash))
            // Third enemy destroyed
            (240, DamageEnemy(1011, Some 16, 200.0f, UnitDefs.ArmFlash))
            (240, DestroyEnemy 1011)
            (250, FireWeapon(18, UnitDefs.ArmRockko))
            (250, DamageEnemy(1006, Some 18, 80.0f, UnitDefs.ArmRockko))
            // Fifth unit destroyed
            (260, DamageUnit(16, Some 1012, 200.0f, UnitDefs.CorGator))
            (260, DestroyUnit 16)
            (270, FireWeapon(13, UnitDefs.ArmPeewee))
            (270, DamageEnemy(1012, Some 13, 25.0f, UnitDefs.ArmPeewee))
            // Lab produces reinforcement
            (100, SpawnUnit(21, UnitDefs.ArmPeewee, (3000.0f, 100.0f, 3450.0f), 9))
            (120, FinishUnit 21)
            (150, SpawnUnit(22, UnitDefs.ArmFlash, (3000.0f, 100.0f, 3450.0f), 9))
            (170, FinishUnit 22)
            (200, SpawnUnit(23, UnitDefs.ArmRockko, (3000.0f, 100.0f, 3450.0f), 9))
            (225, FinishUnit 23)
        ]
        let initialMetal = { Current = 500.0f; Income = 25.0f; Usage = 20.0f; Storage = 2000.0f }
        let initialEnergy = { Current = 800.0f; Income = 60.0f; Usage = 40.0f; Storage = 2000.0f }

        generateScene SceneB "Medium Map - Mid-Game Skirmish" 8192.0f 8192.0f unitDefs
            initialUnits initialEnemies scheduledActions initialMetal initialEnergy

    // ============================================================
    // Scene C: Large Map - Late-Game Siege (16384x16384)
    // ============================================================
    let private generateSceneC () =
        let unitDefs = UnitDefs.sceneC
        // 50 friendly units in 3 clusters
        let cluster1Base = (4000.0f, 100.0f, 4000.0f) // Defense base
        let cluster2Base = (8000.0f, 100.0f, 8000.0f) // Attack force
        let cluster3Base = (12000.0f, 100.0f, 6000.0f) // Flanking force
        let spread (bx, by, bz) (dx: float32) (dz: float32) = (bx + dx, by, bz + dz)

        let initialUnits =
            [
                // Cluster 1: Defense base (commander, buildings, some defenders)
                (1, UnitDefs.ArmCommander, cluster1Base)
                (2, UnitDefs.ArmMex, spread cluster1Base -200.0f -200.0f)
                (3, UnitDefs.ArmMex, spread cluster1Base 200.0f -200.0f)
                (4, UnitDefs.ArmMex, spread cluster1Base -200.0f 200.0f)
                (5, UnitDefs.ArmMex, spread cluster1Base 200.0f 200.0f)
                (6, UnitDefs.ArmMex, spread cluster1Base -400.0f 0.0f)
                (7, UnitDefs.ArmSolar, spread cluster1Base -300.0f -300.0f)
                (8, UnitDefs.ArmSolar, spread cluster1Base 300.0f -300.0f)
                (9, UnitDefs.ArmSolar, spread cluster1Base -300.0f 300.0f)
                (10, UnitDefs.ArmSolar, spread cluster1Base 300.0f 300.0f)
                (11, UnitDefs.ArmLab, spread cluster1Base 0.0f -400.0f)
                (12, UnitDefs.ArmAdvLab, spread cluster1Base 0.0f -600.0f)
                (13, UnitDefs.ArmStorage, spread cluster1Base -500.0f -100.0f)
                (14, UnitDefs.ArmStorage, spread cluster1Base 500.0f -100.0f)
                (15, UnitDefs.ArmRockko, spread cluster1Base -100.0f 400.0f)
                (16, UnitDefs.ArmSamson, spread cluster1Base 100.0f 400.0f)
                (17, UnitDefs.ArmFark, spread cluster1Base 0.0f 300.0f)
                // Cluster 2: Attack force
                (18, UnitDefs.ArmZeus, spread cluster2Base 0.0f 0.0f)
                (19, UnitDefs.ArmZeus, spread cluster2Base 100.0f 50.0f)
                (20, UnitDefs.ArmZeus, spread cluster2Base -100.0f 50.0f)
                (21, UnitDefs.ArmZeus, spread cluster2Base 50.0f -100.0f)
                (22, UnitDefs.ArmAnni, spread cluster2Base -50.0f -200.0f)
                (23, UnitDefs.ArmRockko, spread cluster2Base 200.0f 100.0f)
                (24, UnitDefs.ArmRockko, spread cluster2Base -200.0f 100.0f)
                (25, UnitDefs.ArmRockko, spread cluster2Base 150.0f -50.0f)
                (26, UnitDefs.ArmSamson, spread cluster2Base 250.0f -100.0f)
                (27, UnitDefs.ArmSamson, spread cluster2Base -250.0f -100.0f)
                (28, UnitDefs.ArmPeewee, spread cluster2Base 300.0f 200.0f)
                (29, UnitDefs.ArmPeewee, spread cluster2Base -300.0f 200.0f)
                (30, UnitDefs.ArmPeewee, spread cluster2Base 350.0f 0.0f)
                (31, UnitDefs.ArmFlash, spread cluster2Base -350.0f 0.0f)
                (32, UnitDefs.ArmFlash, spread cluster2Base 400.0f 100.0f)
                (33, UnitDefs.ArmFlash, spread cluster2Base -400.0f 100.0f)
                // Cluster 3: Flanking force
                (34, UnitDefs.ArmFlash, spread cluster3Base 0.0f 0.0f)
                (35, UnitDefs.ArmFlash, spread cluster3Base 100.0f 50.0f)
                (36, UnitDefs.ArmFlash, spread cluster3Base -100.0f 50.0f)
                (37, UnitDefs.ArmFlash, spread cluster3Base 50.0f -100.0f)
                (38, UnitDefs.ArmFlash, spread cluster3Base -50.0f -100.0f)
                (39, UnitDefs.ArmPeewee, spread cluster3Base 200.0f 0.0f)
                (40, UnitDefs.ArmPeewee, spread cluster3Base -200.0f 0.0f)
                (41, UnitDefs.ArmPeewee, spread cluster3Base 0.0f 200.0f)
                (42, UnitDefs.ArmPeewee, spread cluster3Base 0.0f -200.0f)
                (43, UnitDefs.ArmRockko, spread cluster3Base 150.0f 150.0f)
                (44, UnitDefs.ArmRockko, spread cluster3Base -150.0f 150.0f)
                (45, UnitDefs.ArmSamson, spread cluster3Base 250.0f 100.0f)
                (46, UnitDefs.ArmSamson, spread cluster3Base -250.0f 100.0f)
                (47, UnitDefs.ArmZeus, spread cluster3Base 0.0f 100.0f)
                (48, UnitDefs.ArmZeus, spread cluster3Base 100.0f -50.0f)
                (49, UnitDefs.ArmFark, spread cluster3Base -100.0f -50.0f)
                (50, UnitDefs.ArmFark, spread cluster3Base 100.0f 100.0f)
            ]

        // 40 enemies in 2 clusters
        let enemyBase1 = (12000.0f, 100.0f, 12000.0f) // Main enemy base
        let enemyBase2 = (10000.0f, 100.0f, 10000.0f) // Forward enemy force
        let initialEnemies =
            [
                // Forward enemy force - visible from start
                for i in 0 .. 14 do
                    let defId = [| UnitDefs.CorGator; UnitDefs.CorThud; UnitDefs.CorStorm; UnitDefs.CorSumo; UnitDefs.CorGoliath |].[i % 5]
                    let dx = float32 ((i % 5) - 2) * 200.0f
                    let dz = float32 (i / 5) * 200.0f
                    let (bx, by, bz) = enemyBase2
                    let speed =
                        match defId with
                        | d when d = UnitDefs.CorGator -> 4.0f
                        | d when d = UnitDefs.CorStorm -> 3.0f
                        | d when d = UnitDefs.CorThud -> 2.0f
                        | d when d = UnitDefs.CorSumo -> 1.5f
                        | _ -> 1.2f
                    (1000 + i, defId, (bx + dx, by, bz + dz), speed, EnemySim.NotVisible,
                        [ (5, EnemySim.RadarOnly); (15, EnemySim.InLineOfSight) ])
                // Main base enemies - appear later
                for i in 0 .. 24 do
                    let defId = [| UnitDefs.CorGator; UnitDefs.CorThud; UnitDefs.CorStorm; UnitDefs.CorSumo; UnitDefs.CorGoliath |].[i % 5]
                    let dx = float32 ((i % 5) - 2) * 250.0f
                    let dz = float32 (i / 5) * 250.0f
                    let (bx, by, bz) = enemyBase1
                    let speed =
                        match defId with
                        | d when d = UnitDefs.CorGator -> 4.0f
                        | d when d = UnitDefs.CorStorm -> 3.0f
                        | d when d = UnitDefs.CorThud -> 2.0f
                        | d when d = UnitDefs.CorSumo -> 1.5f
                        | _ -> 1.2f
                    (1015 + i, defId, (bx + dx, by, bz + dz), speed, EnemySim.NotVisible,
                        [ (50 + i * 3, EnemySim.RadarOnly); (80 + i * 3, EnemySim.InLineOfSight) ])
            ]

        // Heavy combat scheduled throughout
        let mutable actions = []
        // Continuous combat from frame 20 onward — every 3-5 frames
        for f in 20 .. 3 .. 290 do
            let attackerBase = 18 + ((f / 3) % 16) // cycle through attack force units 18-33
            let targetEnemy = 1000 + ((f / 3) % 15) // cycle through forward enemies
            actions <- (f, FireWeapon(attackerBase, UnitDefs.ArmZeus)) :: actions
            actions <- (f, DamageEnemy(targetEnemy, Some attackerBase, 40.0f, UnitDefs.ArmZeus)) :: actions
            // Counter-attacks every 5 frames
            if f % 5 = 0 then
                let counterTarget = 18 + ((f / 5) % 16)
                let counterAttacker = 1000 + ((f / 5) % 15)
                actions <- (f, DamageUnit(counterTarget, Some counterAttacker, 30.0f, UnitDefs.CorGator)) :: actions

        // Some enemy kills
        actions <- (100, DestroyEnemy 1000) :: actions
        actions <- (150, DestroyEnemy 1001) :: actions
        actions <- (200, DestroyEnemy 1002) :: actions
        actions <- (250, DestroyEnemy 1003) :: actions
        // Some friendly kills
        actions <- (120, DestroyUnit 28) :: actions
        actions <- (180, DestroyUnit 30) :: actions
        actions <- (240, DestroyUnit 31) :: actions

        // Lab produces reinforcements
        actions <- (50, SpawnUnit(51, UnitDefs.ArmRockko, (4000.0f, 100.0f, 3600.0f), 11)) :: actions
        actions <- (75, FinishUnit 51) :: actions
        actions <- (80, SpawnUnit(52, UnitDefs.ArmZeus, (4000.0f, 100.0f, 3400.0f), 12)) :: actions
        actions <- (120, FinishUnit 52) :: actions
        actions <- (130, SpawnUnit(53, UnitDefs.ArmAnni, (4000.0f, 100.0f, 3400.0f), 12)) :: actions
        actions <- (200, FinishUnit 53) :: actions

        let scheduledActions = List.rev actions

        let initialMetal = { Current = 8500.0f; Income = 80.0f; Usage = 60.0f; Storage = 10000.0f }
        let initialEnergy = { Current = 9000.0f; Income = 200.0f; Usage = 150.0f; Storage = 10000.0f }

        generateScene SceneC "Large Map - Late-Game Siege" 16384.0f 16384.0f unitDefs
            initialUnits initialEnemies scheduledActions initialMetal initialEnergy

    let generate (sceneId: SceneId) : Scene =
        match sceneId with
        | SceneA -> generateSceneA ()
        | SceneB -> generateSceneB ()
        | SceneC -> generateSceneC ()

    let generateAll () : Scene list =
        [ generate SceneA; generate SceneB; generate SceneC ]
