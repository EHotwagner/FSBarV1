namespace FSBar.LiveTests

open System
open Xunit
open FSBar.Client

/// Live test against BARb AI — moves commander into enemy base.
/// Uses its own EngineFixture with BARb as opponent (not shared with other tests).
type BarbFixture() =
    let mutable client: BarClient option = None
    let mutable initialFrames: GameFrame list = []
    let mutable initialEvents: GameEvent list = []

    member _.Client =
        client |> Option.defaultWith (fun () -> failwith "Client not initialized")

    member _.InitialFrames = initialFrames
    member _.InitialEvents = initialEvents

    interface IAsyncLifetime with
        member _.InitializeAsync() = task {
            let enginePath =
                let searchDir =
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".local/state/Beyond All Reason/engine")
                let candidates =
                    System.IO.Directory.GetFiles(searchDir, "spring-headless", System.IO.SearchOption.AllDirectories)
                if candidates.Length = 0 then failwith "spring-headless not found"
                candidates.[0]

            let dataDir =
                System.IO.Path.GetFullPath(
                    System.IO.Path.Combine(System.IO.Path.GetDirectoryName(enginePath), "..", ".."))

            let config =
                { EngineConfig.defaultConfig () with
                    EngineBin = enginePath
                    SpringDataDir = Some dataDir
                    OpponentAI = "BARb"
                    GameSpeed = 100 }

            let c = new BarClient(config)
            c.Start()

            let warmup = ResizeArray<GameFrame>()
            for frame in c.Frames |> Seq.truncate 30 do
                warmup.Add(frame)

            initialFrames <- warmup |> Seq.toList
            initialEvents <- initialFrames |> List.collect (fun f -> f.Events)
            client <- Some c
        }

        member _.DisposeAsync() =
            client |> Option.iter (fun c -> try c.Stop() with _ -> ())
            client <- None

            Threading.Tasks.Task.CompletedTask

[<CollectionDefinition("BARb")>]
type BarbCollection() =
    interface ICollectionFixture<BarbFixture>

[<Collection("BARb")>]
type BarbRushTests(fixture: BarbFixture) =

    let getCommanderUnitId () =
        fixture.InitialEvents
        |> List.tryPick (function
            | GameEvent.UnitCreated(uid, _) -> Some uid
            | _ -> None)

    [<Fact>]
    [<Trait("Category", "BARb")>]
    member _.``Commander reaches enemy base against BARb AI``() =
        let commanderId = getCommanderUnitId ()
        Assert.True(commanderId.IsSome, "Should have a commander unit")
        let uid = commanderId.Value

        let stream = fixture.Client.Stream
        let enemyX = 3200.0f
        let enemyY = 100.0f
        let enemyZ = 3200.0f

        let mutable frameCount = 0
        let mutable arrived = false
        let mutable destroyed = false
        let maxFrames = 5000

        while frameCount < maxFrames && not arrived && not destroyed do
            match Protocol.receiveFrame stream with
            | None ->
                destroyed <- true
            | Some frame ->
                frameCount <- frameCount + 1

                // Query position every 500 frames
                if frameCount % 500 = 0 then
                    let (cx, _, cz) = Callbacks.getUnitPos stream uid
                    let hp = Callbacks.getUnitHealth stream uid
                    let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
                    if dist < 300.0 then
                        arrived <- true

                // Send move command every 1000 frames, empty otherwise
                if frameCount = 1 || frameCount % 1000 = 0 then
                    Protocol.sendFrameResponse stream [ Commands.MoveCommand uid enemyX enemyY enemyZ ]
                else
                    Protocol.sendFrameResponse stream []

                // Check for commander death
                for evt in frame.Events do
                    match evt with
                    | GameEvent.UnitDestroyed(deadUid, _) when deadUid = uid ->
                        destroyed <- true
                    | _ -> ()

        Assert.True(arrived, $"Commander should reach enemy base within {maxFrames} frames (got to frame {frameCount}, destroyed={destroyed})")

    [<Fact>]
    [<Trait("Category", "BARb")>]
    member _.``Commander assassinates enemy commander``() =
        let commanderId = getCommanderUnitId ()
        Assert.True(commanderId.IsSome, "Should have a commander unit")
        let uid = commanderId.Value

        let stream = fixture.Client.Stream
        let enemyX = 3200.0f
        let enemyY = 100.0f
        let enemyZ = 3200.0f

        // Phase tracking
        let mutable phase = "move"  // move -> hunt -> kill
        let mutable frameCount = 0
        let mutable enemyComId = -1
        let mutable enemyComDead = false
        let mutable ourComDead = false
        let enemiesInLOS = ResizeArray<int>()
        let maxFrames = 12000

        // Seed enemies from warmup and prior test frames (EnemyEnterLOS/EnemyCreated may have fired earlier)
        for evt in fixture.InitialEvents do
            match evt with
            | GameEvent.EnemyEnterLOS eid | GameEvent.EnemyCreated eid ->
                if not (enemiesInLOS.Contains(eid)) then
                    enemiesInLOS.Add(eid)
            | _ -> ()

        // Cache commander def names so we only look them up once
        let checkedDefs = System.Collections.Generic.HashSet<int>()

        while frameCount < maxFrames && not enemyComDead && not ourComDead do
            match Protocol.receiveFrame stream with
            | None -> ourComDead <- true
            | Some frame ->
                frameCount <- frameCount + 1

                // Collect enemies entering LOS or being created
                for evt in frame.Events do
                    match evt with
                    | GameEvent.EnemyEnterLOS eid | GameEvent.EnemyCreated eid ->
                        if not (enemiesInLOS.Contains(eid)) then
                            enemiesInLOS.Add(eid)
                    | GameEvent.EnemyDestroyed(eid, _) when eid = enemyComId ->
                        enemyComDead <- true
                    | GameEvent.UnitDestroyed(deadUid, _) when deadUid = uid ->
                        ourComDead <- true
                    | _ -> ()

                // Every 100 frames, try to identify enemy commander from spotted enemies
                if enemyComId < 0 && frameCount % 100 = 0 && enemiesInLOS.Count > 0 then
                    for eid in enemiesInLOS do
                        if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
                            checkedDefs.Add(eid) |> ignore
                            let defId = Callbacks.getUnitDef stream eid
                            if defId > 0 then
                                let defName = Callbacks.getUnitDefName stream defId
                                if defName.Contains("commander") || defName.Contains("com_") ||
                                   defName.StartsWith("arm") && defName.Contains("com") ||
                                   defName.StartsWith("cor") && defName.Contains("com") then
                                    enemyComId <- eid
                                    phase <- "kill"

                // Build commands based on phase
                let commands =
                    match phase with
                    | "move" ->
                        if frameCount = 1 || frameCount % 1000 = 0 then
                            [ Commands.MoveCommand uid enemyX enemyY enemyZ ]
                        else []
                    | "kill" when enemyComId > 0 ->
                        if frameCount % 200 = 0 then
                            [ Commands.AttackCommand uid enemyComId ]
                        else []
                    | _ -> []

                // Check if we've arrived at enemy base — switch to hunting
                if phase = "move" && frameCount % 500 = 0 then
                    let (cx, _, cz) = Callbacks.getUnitPos stream uid
                    let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
                    if dist < 400.0 then
                        phase <- "hunt"

                // In hunt phase, patrol around enemy base to find their commander
                let finalCommands =
                    if phase = "hunt" && frameCount % 300 = 0 then
                        // Patrol in a circle around enemy base to spot units
                        let angle = float frameCount / 300.0 * Math.PI / 3.0
                        let px = enemyX + 500.0f * float32 (Math.Cos(angle))
                        let pz = enemyZ + 500.0f * float32 (Math.Sin(angle))
                        [ Commands.MoveCommand uid px enemyY pz ]
                    else
                        commands

                Protocol.sendFrameResponse stream finalCommands

        Assert.True(enemyComDead,
            $"Enemy commander should be destroyed (phase={phase}, frame={frameCount}, enemyComId={enemyComId}, enemies spotted={enemiesInLOS.Count}, ourComDead={ourComDead})")
