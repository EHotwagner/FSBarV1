/// Graphical BARb rush — launches windowed BAR game, moves commander to enemy base.
/// Usage: cd tests/FSBar.LiveTests && dotnet fsi BarbRushGraphical.fsx

open System
open System.IO

let display = Environment.GetEnvironmentVariable("DISPLAY")
if String.IsNullOrEmpty(display) then
    eprintfn "ERROR: DISPLAY not set. Need a display for graphical mode."
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

let dataDir =
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(enginePath), "..", ".."))

printfn "=== BARb Rush — Graphical ==="
printfn ""

// Use the graphical spring binary (same directory as spring-headless)
let graphicalEngine = Path.Combine(Path.GetDirectoryName(enginePath), "spring")
if not (File.Exists(graphicalEngine)) then
    eprintfn "ERROR: Graphical spring binary not found at %s" graphicalEngine
    exit 1

let config =
    { EngineConfig.defaultConfig () with
        Mode = Graphical
        EngineBin = enginePath
        AppImagePath = graphicalEngine
        SpringDataDir = Some dataDir
        OpponentAI = "BARb"
        TimeoutMs = 120000
        GameSpeed = 5 }

printfn "Graphical engine: %s" graphicalEngine
printfn "Opponent: %s" config.OpponentAI
printfn "Map: %s" config.MapName
printfn "Speed: %dx (slow enough to watch)" config.GameSpeed
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
            printfn "Commander: unit %d" uid
        | _ -> ()

if commanderUnitId < 0 then
    printfn "No commander found!"
    client.Stop()
    exit 1

let enemyX = 4608.0f
let enemyY = 100.0f
let enemyZ = 4096.0f
let stream = client.Stream

printfn "Sending commander to enemy base at (%.0f, %.0f)..." enemyX enemyZ
printfn "Watch the game window! Press Ctrl+C to stop."
printfn ""

let mutable frameCount = 0
let mutable lastReport = 0

while running do
    try
        match Protocol.receiveFrame stream with
        | None ->
            printfn "Game ended."
            running <- false
        | Some frame ->
            frameCount <- frameCount + 1

            // Send move command on first frame and every 500 frames
            if frameCount = 1 || frameCount % 500 = 0 then
                Protocol.sendFrameResponse stream [ Commands.MoveCommand commanderUnitId enemyX enemyY enemyZ ]
            else
                Protocol.sendFrameResponse stream []

            if frameCount % 500 = 0 then
                printfn "  [frame %d] game frame %d" frameCount frame.FrameNumber

            for evt in frame.Events do
                match evt with
                | GameEvent.UnitDamaged(uid, _, dmg, _, _) when uid = commanderUnitId ->
                    if frameCount > lastReport + 100 then
                        lastReport <- frameCount
                        printfn "  [frame %d] Commander taking damage: %.0f" frame.FrameNumber dmg
                | GameEvent.UnitDestroyed(uid, _) when uid = commanderUnitId ->
                    printfn "  [frame %d] COMMANDER DESTROYED!" frame.FrameNumber
                    running <- false
                | _ -> ()
    with ex ->
        printfn "Engine stopped: %s" ex.Message
        running <- false

printfn ""
printfn "Ran %d frames." frameCount
client.Stop()
printfn "Done."
