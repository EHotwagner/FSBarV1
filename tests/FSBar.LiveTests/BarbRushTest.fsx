/// Test: Move commander into enemy base against BARb AI.
/// Usage: cd tests/FSBar.LiveTests && dotnet fsi BarbRushTest.fsx

open System
open System.IO

#r "bin/Debug/net10.0/Google.Protobuf.dll"
#r "bin/Debug/net10.0/FsGrpc.dll"
#r "bin/Debug/net10.0/NodaTime.dll"
#r "bin/Debug/net10.0/BarData.dll"
#r "bin/Debug/net10.0/FSBar.Proto.dll"
#r "bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client

// Detect engine
let enginePath =
    let searchDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local/state/Beyond All Reason/engine")
    Directory.GetFiles(searchDir, "spring-headless", SearchOption.AllDirectories).[0]

let dataDir =
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(enginePath), "..", ".."))

printfn "=== BARb Rush Test ==="
printfn "Engine: %s" enginePath

let config =
    { EngineConfig.defaultConfig () with
        EngineBin = enginePath
        SpringDataDir = Some dataDir
        OpponentAI = "BARb"
        GameSpeed = 100 }

printfn "Opponent: %s" config.OpponentAI
printfn "Map: %s" config.MapName
printfn "Speed: %dx" config.GameSpeed
printfn ""

let client = new BarClient(config)
client.Start()
printfn "Connected!"

// Warm up — capture commander
let mutable commanderUnitId = -1
for _ in 1..30 do
    let frame = client.Step()
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitCreated(uid, _) when commanderUnitId < 0 ->
            commanderUnitId <- uid
            printfn "Commander: unit %d" uid
        | _ -> ()

if commanderUnitId < 0 then
    printfn "ERROR: No commander"
    client.Stop()
    exit 1

// Enemy base position from the game script (Team 1)
let enemyX = 4608.0f
let enemyY = 100.0f
let enemyZ = 4096.0f
printfn "Enemy base: (%.0f, %.0f)" enemyX enemyZ
printfn ""

// Use the raw protocol to interleave callbacks with frame processing.
// Protocol flow: receive frame → (optional callbacks) → send frame response
printfn "Querying initial position..."
let stream = client.Stream

// Receive a frame, query position via callback, then respond
let receiveAndQuery () =
    match Protocol.receiveFrame stream with
    | Some frame ->
        // Between receive and response, we can issue callbacks
        let (cx, _, cz) = Callbacks.getUnitPos stream commanderUnitId
        let hp = Callbacks.getUnitHealth stream commanderUnitId
        Protocol.sendFrameResponse stream []
        Some (frame, cx, cz, hp)
    | None -> None

// Get initial position
match receiveAndQuery () with
| Some (_, cx, _, _) ->
    printfn "Starting position: (%.0f, %.0f)" cx 0.0f
| None -> ()

// Send the move command
printfn "Sending commander to enemy base..."
match Protocol.receiveFrame stream with
| Some frame ->
    Protocol.sendFrameResponse stream [ Commands.MoveCommand commanderUnitId enemyX enemyY enemyZ ]
    printfn "Move command sent on frame %d" frame.FrameNumber
| None -> ()

// Run and track progress
let mutable frameCount = 0
let mutable lastDmgReport = 0
let mutable arrivedFrame = 0
let mutable commanderDead = false
let maxFrames = 6000

printfn ""
while frameCount < maxFrames && not commanderDead do
    let frame = Protocol.receiveFrame stream
    match frame with
    | None ->
        printfn "Game ended."
        commanderDead <- true
    | Some frame ->
        frameCount <- frameCount + 1

        // Query position every 500 frames
        let mutable cx = 0.0f
        let mutable cz = 0.0f
        let mutable hp = 0.0f
        if frameCount % 500 = 0 then
            let (px, _, pz) = Callbacks.getUnitPos stream commanderUnitId
            hp <- Callbacks.getUnitHealth stream commanderUnitId
            cx <- px
            cz <- pz
            let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
            printfn "  [frame %d] Pos: (%.0f, %.0f) | Dist: %.0f | HP: %.0f"
                frame.FrameNumber cx cz dist hp
            if dist < 300.0 && arrivedFrame = 0 then
                arrivedFrame <- frameCount
                printfn "  >>> ARRIVED at enemy base!"

        // Re-send move every 1000 frames
        if frameCount % 1000 = 0 then
            Protocol.sendFrameResponse stream [ Commands.MoveCommand commanderUnitId enemyX enemyY enemyZ ]
        else
            Protocol.sendFrameResponse stream []

        // Check events
        for evt in frame.Events do
            match evt with
            | GameEvent.UnitDamaged(uid, attacker, dmg, _, _) when uid = commanderUnitId ->
                if frameCount > lastDmgReport + 200 then
                    lastDmgReport <- frameCount
                    let src = match attacker with Some a -> $"unit {a}" | None -> "unknown"
                    printfn "  [frame %d] Commander hit! dmg=%.0f from %s" frame.FrameNumber dmg src
            | GameEvent.UnitDestroyed(uid, _) when uid = commanderUnitId ->
                printfn "  [frame %d] COMMANDER DESTROYED!" frame.FrameNumber
                commanderDead <- true
            | GameEvent.EnemyEnterLOS eid ->
                if frameCount < 100 || frameCount % 500 < 5 then
                    printfn "  [frame %d] Enemy spotted: %d" frame.FrameNumber eid
            | _ -> ()

printfn ""
if arrivedFrame > 0 then
    printfn "Result: Commander reached enemy base at frame %d" arrivedFrame
    if commanderDead then
        printfn "  ...and was subsequently destroyed"
elif commanderDead then
    printfn "Result: Commander destroyed en route"
else
    printfn "Result: Test ended after %d frames — commander still moving" maxFrames

client.Stop()
printfn "Done."
