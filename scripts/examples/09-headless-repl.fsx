// 09-headless-repl.fsx — Interactive headless engine REPL
//
// Starts a headless BAR engine and provides helper functions for
// interactive control via FSI. Load this script, then use the
// helpers to step frames, query state, issue commands, and
// optionally open a live visualization.
//
// Usage (from repo root):
//   dotnet build src/FSBar.Viz/
//   dotnet fsi scripts/examples/09-headless-repl.fsx
//
// Or from the FSI MCP server:
//   #load "/home/developer/projects/FSBarV1/scripts/examples/09-headless-repl.fsx"
//   step 10           // advance 10 frames
//   units ()          // list all known units
//   move 42 2000 1000 // move unit 42 to (2000, 1000)
//   viz ()            // open live visualization
//   economy ()        // show metal/energy

// Resolve paths relative to this script file
let private _scriptDir = __SOURCE_DIRECTORY__
let private _repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(_scriptDir, "..", ".."))
let private _binDir = System.IO.Path.Combine(_repoRoot, "tests", "FSBar.Viz.Tests", "bin", "Debug", "net10.0")

// Load native libs for viz support
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint private dlopen(string filename, int flags)
let private _np = System.IO.Path.Combine(_binDir, "runtimes", "linux-x64", "native")
let private _1 = dlopen(_np + "/libglfw.so.3", 0x2 ||| 0x100)
let private _2 = dlopen(_np + "/libSkiaSharp.so", 0x2 ||| 0x100)

#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FsGrpc.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Google.Protobuf.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/BarData.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Core.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.Common.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Maths.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Common.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Glfw.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.GLFW.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.OpenGL.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"

open System
open FSBar.Client
open FSBar.Client.Commands
open FSBar.Client.MapQuery
open FSBar.Viz
open BarData

// ── State ────────────────────────────────────────────────────
let mutable private _client: BarClient option = None
let mutable private _frame: GameFrame = { FrameNumber = 0u; Events = [] }
let mutable private _units: Map<int, {| Id: int; DefId: int; Name: string; X: float32; Z: float32; Hp: float32; MaxHp: float32 |}> = Map.empty
let mutable private _vizRunning = false
let mutable private _grid: MapGrid option = None

let private client () =
    match _client with
    | Some c -> c
    | None -> failwith "No session. Call start() first."

let private stream () = (client()).Stream

// ── Lifecycle ────────────────────────────────────────────────

/// Start a headless engine session.
let start () =
    if _client.IsSome then printfn "Session already running. Call stop() first."; ()
    else
    let config =
        { EngineConfig.defaultConfig () with
            EngineBin = "/home/developer/.local/state/Beyond All Reason/engine/recoil_2025.06.19/spring-headless"
            SpringDataDir = Some "/home/developer/.local/state/Beyond All Reason" }
    printfn "Starting headless engine (map: %s)..." config.MapName
    let c = new BarClient(config)
    c.Start()
    _client <- Some c
    printfn "Connected! Team %d" (Callbacks.getMyTeam c.Stream)

/// Start with a custom config.
let startWith (config: EngineConfig) =
    if _client.IsSome then printfn "Session already running."; ()
    else
    printfn "Starting engine (map: %s)..." config.MapName
    let c = new BarClient(config)
    c.Start()
    _client <- Some c
    printfn "Connected!"

/// Stop the session and clean up.
let stop () =
    if _vizRunning then GameViz.stop (); _vizRunning <- false
    match _client with
    | Some c -> c.Stop(); _client <- None; _units <- Map.empty; _grid <- None; printfn "Session stopped."
    | None -> printfn "No session running."

// ── Stepping ─────────────────────────────────────────────────

let private processFrame (frame: GameFrame) =
    _frame <- frame
    let s = stream ()
    for ev in frame.Events do
        match ev with
        | GameEvent.UnitCreated(id, _) | GameEvent.UnitFinished id ->
            try
                let (px, _, pz) = Callbacks.getUnitPos s id
                let defId = Callbacks.getUnitDef s id
                let name = Callbacks.getUnitDefName s defId
                let hp = Callbacks.getUnitHealth s id
                let maxHp = Callbacks.getUnitMaxHealth s id
                _units <- _units.Add(id, {| Id = id; DefId = defId; Name = name; X = px; Z = pz; Hp = hp; MaxHp = maxHp |})
            with _ -> ()
        | GameEvent.UnitDestroyed(id, _) ->
            _units <- _units.Remove id
        | GameEvent.Update _ ->
            let updated =
                _units |> Map.map (fun id u ->
                    try
                        let (px, _, pz) = Callbacks.getUnitPos s id
                        let hp = Callbacks.getUnitHealth s id
                        {| u with X = px; Z = pz; Hp = hp |}
                    with _ -> u)
            _units <- updated
        | _ -> ()
    if _vizRunning then GameViz.onFrame frame

/// Advance N frames (default 1). Returns the last frame number.
let step (n: int) =
    let c = client ()
    for _ in 1 .. n do
        let frame = c.Step()
        processFrame frame
    printfn "Frame %d (units: %d)" _frame.FrameNumber _units.Count

/// Advance 1 frame.
let step1 () = step 1

/// Advance N frames and send commands from a handler each frame.
let stepWith (n: int) (handler: GameFrame -> Highbar.AICommand list) =
    let c = client ()
    for _ in 1 .. n do
        let frame = c.StepWith handler
        processFrame frame
    printfn "Frame %d" _frame.FrameNumber

// ── Queries ──────────────────────────────────────────────────

/// Show all tracked units.
let units () =
    if _units.IsEmpty then printfn "No units tracked yet. Call step() first."
    else
    printfn "%-6s %-20s %8s %8s %10s" "ID" "Name" "X" "Z" "HP"
    printfn "%s" (String('-', 56))
    _units |> Map.iter (fun _ u ->
        printfn "%-6d %-20s %8.0f %8.0f %5.0f/%5.0f" u.Id u.Name u.X u.Z u.Hp u.MaxHp)

/// Show economy (metal and energy).
let economy () =
    let s = stream ()
    let mc = Callbacks.getEconomyCurrent s 0
    let mi = Callbacks.getEconomyIncome s 0
    let mu = Callbacks.getEconomyUsage s 0
    let ms = Callbacks.getEconomyStorage s 0
    let ec = Callbacks.getEconomyCurrent s 1
    let ei = Callbacks.getEconomyIncome s 1
    let eu = Callbacks.getEconomyUsage s 1
    let es = Callbacks.getEconomyStorage s 1
    printfn "Metal:  %.0f/%.0f  (+%.1f -%.1f)" mc ms mi mu
    printfn "Energy: %.0f/%.0f  (+%.1f -%.1f)" ec es ei eu

/// Show current status.
let status () =
    printfn "Frame: %d | Units: %d | Viz: %b" _frame.FrameNumber _units.Count _vizRunning

/// Get unit info by ID.
let unit' (id: int) =
    match _units.TryFind id with
    | Some u -> printfn "%d: %s at (%.0f, %.0f) HP=%.0f/%.0f" u.Id u.Name u.X u.Z u.Hp u.MaxHp
    | None -> printfn "Unit %d not tracked." id

/// List metal spots on the map.
let metalSpots () =
    let spots = Callbacks.getMetalSpots (stream ())
    printfn "%d metal spots:" spots.Length
    for (x, _, z, income) in spots do
        printfn "  (%.0f, %.0f) income=%.3f" x z income

/// Show map info.
let mapInfo () =
    let s = stream ()
    let w = Callbacks.getMapWidth s
    let h = Callbacks.getMapHeight s
    printfn "Map: %dx%d heightmap (%dx%d elmos)" w h (w*8) (h*8)

/// Load the full map grid from the engine.
let loadMap () =
    let g = MapGrid.loadFromEngine (stream ())
    _grid <- Some g
    printfn "MapGrid loaded: %dx%d" g.WidthHeightmap g.HeightHeightmap
    g

/// Query terrain at elmo coordinates.
let terrain (x: int) (z: int) =
    match _grid with
    | None -> printfn "Load map first: loadMap()"
    | Some g ->
        match terrainAtElmo g x z with
        | Result.Ok t -> printfn "(%d, %d): %A" x z t
        | Result.Error e -> printfn "Error: %s" e

/// Get the start position for a team.
let startPos (teamId: int) =
    let (x, y, z) = Callbacks.getStartPos (stream ()) teamId
    printfn "Team %d start: (%.0f, %.0f, %.0f)" teamId x y z
    (x, y, z)

/// List build options for a unit definition.
let buildOptions (defId: int) =
    let s = stream ()
    let opts = Callbacks.getBuildOptions s defId
    let name = Callbacks.getUnitDefName s defId
    printfn "%s (def %d) can build %d units:" name defId opts.Length
    for optId in opts do
        let optName = Callbacks.getUnitDefName s optId
        let cost = Callbacks.getUnitDefCost s optId
        printfn "  %d: %-25s (cost: %.0f)" optId optName cost

// ── Commands ─────────────────────────────────────────────────

/// Move a unit to (x, z).
let move (unitId: int) (x: float32) (z: float32) =
    let cmd = MoveCommand unitId x 0.0f z
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d -> (%.0f, %.0f)" unitId x z

/// Attack-move a unit to (x, z).
let fight (unitId: int) (x: float32) (z: float32) =
    let cmd = FightCommand unitId x 0.0f z
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d fight-move -> (%.0f, %.0f)" unitId x z

/// Patrol a unit to (x, z).
let patrol (unitId: int) (x: float32) (z: float32) =
    let cmd = PatrolCommand unitId x 0.0f z
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d patrol -> (%.0f, %.0f)" unitId x z

/// Attack a target unit.
let attack (unitId: int) (targetId: int) =
    let cmd = AttackCommand unitId targetId
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d attacking %d" unitId targetId

/// Guard another unit.
let guard (unitId: int) (guardId: int) =
    let cmd = GuardCommand unitId guardId
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d guarding %d" unitId guardId

/// Stop a unit.
let halt (unitId: int) =
    let cmd = StopCommand unitId
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d stopped" unitId

/// Build a structure at (x, z) with facing (0=S, 1=E, 2=N, 3=W).
let build (builderId: int) (defId: int) (x: float32) (z: float32) (facing: int) =
    let cmd = BuildCommand builderId defId x 0.0f z facing
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    let name = Callbacks.getUnitDefName (stream ()) defId
    printfn "Unit %d building %s at (%.0f, %.0f)" builderId name x z

/// Self-destruct a unit.
let selfDestruct (unitId: int) =
    let cmd = SelfDestructCommand unitId
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    printfn "Unit %d self-destructing" unitId

/// Send multiple commands in one frame step.
let send (cmds: Highbar.AICommand list) =
    (client()).StepWith(fun _ -> cmds) |> processFrame
    printfn "Sent %d commands (frame %d)" cmds.Length _frame.FrameNumber

// ── Cheats ───────────────────────────────────────────────────

/// Give resources (0=metal, 1=energy).
let give (resourceId: int) (amount: float32) =
    let cmd = GiveMeResourceCommand resourceId amount
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    let name = if resourceId = 0 then "metal" else "energy"
    printfn "Gave %.0f %s" amount name

/// Spawn a unit at (x, z) by definition ID.
let spawn (defId: int) (x: float32) (z: float32) =
    let cmd = GiveMeNewUnitCommand defId x 0.0f z
    (client()).StepWith(fun _ -> [cmd]) |> processFrame
    let name = Callbacks.getUnitDefName (stream ()) defId
    printfn "Spawned %s at (%.0f, %.0f)" name x z

/// Spawn a unit by name (searches BarData).
let spawnByName (name: string) (x: float32) (z: float32) =
    match AllUnits.all |> List.tryFind (fun u -> u.name = name) with
    | Some _ ->
        let defId = Callbacks.getUnitDefs (stream ()) 1000
                    |> Array.tryFind (fun id -> Callbacks.getUnitDefName (stream ()) id = name)
        match defId with
        | Some id -> spawn id x z
        | None -> printfn "Unit '%s' not found in engine defs" name
    | None -> printfn "Unknown unit '%s'. Try: AllUnits.all |> List.map (fun u -> u.name)" name

// ── Visualization ────────────────────────────────────────────

/// Open the live visualization window.
let viz () =
    if _vizRunning then printfn "Viz already running."; ()
    else
    GameViz.start None
    GameViz.attachToClient (client ())
    GameViz.enableOverlay OverlayKind.Units
    GameViz.enableOverlay OverlayKind.Events
    GameViz.enableOverlay OverlayKind.MetalSpots
    GameViz.enableOverlay OverlayKind.EconomyHud
    _vizRunning <- true
    printfn "Viz opened. Keys: 1-0=layers, U/E/G/M=overlays, Home=reset"

/// Close the visualization window.
let noviz () =
    if _vizRunning then GameViz.stop (); _vizRunning <- false; printfn "Viz closed."
    else printfn "Viz not running."

// ── Help ─────────────────────────────────────────────────────

let help () =
    printfn """
FSBar Headless REPL
═══════════════════════════════════════════════════════
Session:    start()  stop()  status()
Stepping:   step N   step1()   stepWith N handler
Queries:    units()  unit' id  economy()  mapInfo()
            metalSpots()  startPos teamId  loadMap()
            terrain x z   buildOptions defId
Commands:   move id x z     fight id x z    patrol id x z
            attack id tgt   guard id tgt    halt id
            build id defId x z facing       send [cmds]
Cheats:     give resId amt  spawn defId x z
            spawnByName "armcom" x z
Viz:        viz()  noviz()
═══════════════════════════════════════════════════════"""

// ── Ready ────────────────────────────────────────────────────

printfn "FSBar REPL loaded. Call start() to launch engine, help() for commands."
