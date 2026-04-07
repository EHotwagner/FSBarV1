/// Graphical test: Commander rushes enemy base, finds enemy commander, kills it.
/// Usage: cd tests/FSBar.LiveTests && dotnet fsi BarbAssassinGraphical.fsx

open System
open System.IO

let display = Environment.GetEnvironmentVariable("DISPLAY")
if String.IsNullOrEmpty(display) then
    eprintfn "ERROR: DISPLAY not set."
    exit 1

#r "bin/Debug/net10.0/Google.Protobuf.dll"
#r "bin/Debug/net10.0/FsGrpc.dll"
#r "bin/Debug/net10.0/NodaTime.dll"
#r "bin/Debug/net10.0/BarData.dll"
#r "bin/Debug/net10.0/FSBar.Proto.dll"
#r "bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client

let enginePath =
    let searchDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local/state/Beyond All Reason/engine")
    Directory.GetFiles(searchDir, "spring-headless", SearchOption.AllDirectories).[0]

let graphicalEngine = Path.Combine(Path.GetDirectoryName(enginePath), "spring")
let dataDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(enginePath), "..", ".."))

printfn "=== BARb Assassin — Graphical ==="
printfn "Engine: %s" graphicalEngine
printfn ""

let config =
    { EngineConfig.defaultConfig () with
        Mode = Graphical
        EngineBin = enginePath
        AppImagePath = graphicalEngine
        SpringDataDir = Some dataDir
        OpponentAI = "BARb"
        TimeoutMs = 120000
        GameSpeed = 5 }

printfn "Opponent: %s" config.OpponentAI
printfn "Map: %s" config.MapName
printfn "Speed: %dx" config.GameSpeed
printfn ""

let client = new BarClient(config)

let mutable running = true
Console.CancelKeyPress.Add(fun args ->
    args.Cancel <- true
    running <- false
    printfn "\nStopping...")

printfn "Starting graphical engine..."
client.Start()
printfn "Connected!"

// Warm up
let mutable commanderUnitId = -1
for _ in 1..30 do
    let frame = client.Step()
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitCreated(uid, _) when commanderUnitId < 0 ->
            commanderUnitId <- uid
            printfn "Our commander: unit %d" uid
        | _ -> ()

if commanderUnitId < 0 then
    printfn "No commander!"
    client.Stop()
    exit 1

let stream = client.Stream
let enemyX = 3200.0f
let enemyY = 100.0f
let enemyZ = 3200.0f

printfn "Target: enemy base at (%.0f, %.0f)" enemyX enemyZ
printfn "Mission: find and destroy enemy commander"
printfn "Press Ctrl+C to abort."
printfn ""

let mutable phase = "move"
let mutable frameCount = 0
let mutable enemyComId = -1
let mutable enemyComDead = false
let mutable ourComDead = false
let enemiesInLOS = Collections.Generic.HashSet<int>()
let checkedDefs = Collections.Generic.HashSet<int>()

while running && not enemyComDead && not ourComDead do
    match Protocol.receiveFrame stream with
    | None ->
        printfn "Game ended."
        running <- false
    | Some frame ->
        frameCount <- frameCount + 1

        // Collect events
        for evt in frame.Events do
            match evt with
            | GameEvent.EnemyEnterLOS eid ->
                enemiesInLOS.Add(eid) |> ignore
            | GameEvent.EnemyDestroyed(eid, _) when eid = enemyComId ->
                enemyComDead <- true
                printfn "  [frame %d] ENEMY COMMANDER DESTROYED!" frame.FrameNumber
            | GameEvent.UnitDestroyed(deadUid, _) when deadUid = commanderUnitId ->
                ourComDead <- true
                printfn "  [frame %d] Our commander died!" frame.FrameNumber
            | GameEvent.UnitDamaged(uid, _, dmg, _, _) when uid = commanderUnitId ->
                if frameCount % 300 < 2 then
                    printfn "  [frame %d] Our commander hit: %.0f dmg" frame.FrameNumber dmg
            | _ -> ()

        // Try to identify enemy commander from spotted enemies
        if enemyComId < 0 && frameCount % 50 = 0 then
            for eid in enemiesInLOS do
                if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
                    checkedDefs.Add(eid) |> ignore
                    let defId = Callbacks.getUnitDef stream eid
                    if defId > 0 then
                        let defName = Callbacks.getUnitDefName stream defId
                        if defName.Contains("commander") || defName.Contains("com_") ||
                           (defName.StartsWith("arm") && defName.Contains("com")) ||
                           (defName.StartsWith("cor") && defName.Contains("com")) then
                            enemyComId <- eid
                            phase <- "kill"
                            printfn "  [frame %d] ENEMY COMMANDER FOUND: unit %d (def: %s)" frame.FrameNumber eid defName

        // Build commands
        let commands =
            match phase with
            | "move" ->
                if frameCount = 1 || frameCount % 1000 = 0 then
                    [ Commands.MoveCommand commanderUnitId enemyX enemyY enemyZ ]
                else []
            | "hunt" ->
                if frameCount % 300 = 0 then
                    let angle = float frameCount / 300.0 * Math.PI / 3.0
                    let px = enemyX + 500.0f * float32 (Math.Cos(angle))
                    let pz = enemyZ + 500.0f * float32 (Math.Sin(angle))
                    [ Commands.MoveCommand commanderUnitId px enemyY pz ]
                else []
            | "kill" ->
                if frameCount % 200 = 0 then
                    [ Commands.AttackCommand commanderUnitId enemyComId ]
                else []
            | _ -> []

        // Check if arrived at enemy base
        if phase = "move" && frameCount % 500 = 0 then
            let (cx, _, cz) = Callbacks.getUnitPos stream commanderUnitId
            let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
            printfn "  [frame %d] Phase: %s | Dist: %.0f | Enemies spotted: %d"
                frame.FrameNumber phase dist enemiesInLOS.Count
            if dist < 400.0 then
                phase <- "hunt"
                printfn "  [frame %d] Arrived at enemy base — hunting for commander..." frame.FrameNumber

        if phase = "hunt" && frameCount % 500 = 0 then
            printfn "  [frame %d] Hunting... %d enemies spotted, %d checked"
                frame.FrameNumber enemiesInLOS.Count checkedDefs.Count

        if phase = "kill" && frameCount % 500 = 0 then
            let hp = Callbacks.getUnitHealth stream commanderUnitId
            printfn "  [frame %d] Attacking enemy commander %d | Our HP: %.0f"
                frame.FrameNumber enemyComId hp

        Protocol.sendFrameResponse stream commands

printfn ""
if enemyComDead then
    printfn "MISSION COMPLETE: Enemy commander eliminated at frame %d!" frameCount
elif ourComDead then
    printfn "MISSION FAILED: Our commander was destroyed at frame %d" frameCount
else
    printfn "Mission aborted at frame %d (phase: %s)" frameCount phase

client.Stop()
printfn "Done."
