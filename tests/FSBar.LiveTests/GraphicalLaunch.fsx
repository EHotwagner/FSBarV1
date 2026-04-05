/// Graphical BAR game launch for manual visual validation.
/// Starts the BAR AppImage in windowed mode with the AI client connected.
///
/// Usage: dotnet fsi tests/FSBar.LiveTests/GraphicalLaunch.fsx
///
/// Requires: DISPLAY env var set, BAR AppImage installed.

open System

// Check DISPLAY is set
let display = Environment.GetEnvironmentVariable("DISPLAY")
if String.IsNullOrEmpty(display) then
    eprintfn "ERROR: DISPLAY environment variable is not set."
    eprintfn "Graphical mode requires a display server (X11/Wayland)."
    exit 1

// Find the test output directory to load compiled assemblies
let scriptDir = __SOURCE_DIRECTORY__
let binDir =
    let candidate = System.IO.Path.Combine(scriptDir, "bin", "Debug", "net10.0")
    if System.IO.Directory.Exists(candidate) then candidate
    else
        eprintfn "ERROR: Test project not built. Run 'dotnet build tests/FSBar.LiveTests/' first."
        exit 1

// Load assemblies
#r "nuget: FsGrpc, 1.0.6"
#r "nuget: Google.Protobuf, 3.28.3"

for dll in ["FSBar.Proto.dll"; "FSBar.Client.dll"; "BarData.dll"] do
    let path = System.IO.Path.Combine(binDir, dll)
    if System.IO.File.Exists(path) then
        System.Reflection.Assembly.LoadFrom(path) |> ignore
    else
        eprintfn $"ERROR: {dll} not found at {path}. Run 'dotnet build tests/FSBar.LiveTests/' first."
        exit 1

open FSBar.Client

printfn "=== FSBar Graphical Game Launch ==="
printfn "Display: %s" display
printfn ""

let config = { EngineConfig.defaultConfig () with Mode = Graphical }
printfn "Engine: %s" config.AppImagePath
printfn "Map: %s" config.MapName
printfn "Game: %s" config.GameType
printfn ""

use client = new BarClient(config)

// Handle Ctrl+C
let mutable running = true
Console.CancelKeyPress.Add(fun args ->
    args.Cancel <- true
    running <- false
    printfn "\nShutting down...")

printfn "Starting graphical engine..."
client.Start()
printfn "Connected. Processing frames. Press Ctrl+C to stop."
printfn ""

let mutable frameCount = 0
while running do
    try
        let frame = client.Step()
        frameCount <- frameCount + 1
        if frameCount % 100 = 0 then
            printfn "Frame %d (game frame %d)" frameCount frame.FrameNumber
    with
    | ex ->
        printfn "Engine stopped: %s" ex.Message
        running <- false

printfn ""
printfn "Processed %d frames." frameCount
printfn "Done."
