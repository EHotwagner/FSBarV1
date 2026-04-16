// Repl.fsx — Interactive headless engine REPL with live visualization
//
// Starts a headless BAR engine with a SkiaViewer visualization window
// and provides helper functions for interactive control via FSI.
// Load this script, then use the helpers to step frames, query state,
// and issue commands.
//
// Usage (from repo root):
//   ./pack-dev.sh
//   dotnet fsi scripts/examples/Repl.fsx
//
// Or from the FSI MCP server:
//   #load "/home/developer/projects/FSBarV1/scripts/examples/Repl.fsx"
//   open Repl
//   start ()           // launch engine
//   step 10            // advance 10 frames
//   units ()           // list all known units
//   move 42 2000 1000  // move unit 42 to (2000, 1000)
//   viz ()             // open live visualization
//   economy ()         // show metal/energy

// NuGet package references — FSBar.Viz pulls in all transitive deps.
// Run ./pack-dev.sh before loading this script to ensure packages are current.
#r "nuget: FSBar.Viz, *-*"

// Load native libs for viz support (FSI doesn't auto-resolve NuGet native assets)
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint private dlopen(string filename, int flags)
let private _nugetBase = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), ".nuget", "packages")
let private _findNativeLib (packageGlob: string) (libName: string) =
    let pkgDir = System.IO.Path.Combine(_nugetBase, packageGlob)
    if System.IO.Directory.Exists pkgDir then
        System.IO.Directory.GetFiles(pkgDir, libName, System.IO.SearchOption.AllDirectories)
        |> Array.tryFind (fun p -> p.Contains "linux-x64")
    else None
let private _loadNative (packageGlob: string) (libName: string) =
    match _findNativeLib packageGlob libName with
    | Some path -> dlopen(path, 0x2 ||| 0x100) |> ignore
    | None -> eprintfn "Warning: could not find %s in NuGet cache" libName
do _loadNative "ultz.native.glfw" "libglfw.so.3"
do _loadNative "skiasharp.nativeassets.linux.nodependencies" "libSkiaSharp.so"

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

let stream () = (client()).Stream

// ── Frame processing ────────────────────────────────────────

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

// ── Lifecycle ────────────────────────────────────────────────

let private seedUnitsFromGameState () =
    let c = client ()
    let s = c.Stream
    for KeyValue(id, u) in c.GameState.Units do
        try
            let (px, _, pz) = u.Position
            let name =
                match UnitDefCache.tryFindById c.GameState.UnitDefs u.DefId with
                | Some info -> info.Name
                | None -> Callbacks.getUnitDefName s u.DefId
            _units <- _units.Add(id, {| Id = id; DefId = u.DefId; Name = name; X = px; Z = pz; Hp = u.Health; MaxHp = u.MaxHealth |})
        with _ -> ()

let private warmup () =
    let c = client ()
    // Seed REPL state from units already tracked in c.GameState — these were
    // picked up by the interleaved-frame handler during unit-def loading.
    seedUnitsFromGameState ()
    c.WaitFrames 30 processFrame
    printfn "Connected! Team %d | Frame %d | Units: %d" (Callbacks.getMyTeam c.Stream) _frame.FrameNumber _units.Count

let private openViz () =
    if not _vizRunning then
        GameViz.start None
        GameViz.attachToClient (client ())
        let s = stream ()
        let seed =
            _units |> Map.toList |> List.map (fun (_, u) ->
                let (_, py, _) = Callbacks.getUnitPos s u.Id
                { UnitId = u.Id; PositionX = u.X; PositionY = py; PositionZ = u.Z
                  TeamId = Callbacks.getMyTeam s; DefId = u.DefId
                  Health = u.Hp; MaxHealth = u.MaxHp; IsEnemy = false } : FSBar.Viz.UnitState)
        GameViz.seedUnits seed
        GameViz.enableOverlay OverlayKind.Units
        GameViz.enableOverlay OverlayKind.Events
        GameViz.enableOverlay OverlayKind.MetalSpots
        GameViz.enableOverlay OverlayKind.EconomyHud
        _vizRunning <- true

/// Start a headless engine session with live visualization.
let start () =
    if _client.IsSome then printfn "Session already running. Call stop() first."; ()
    else
    let config = EngineConfig.defaultConfig ()
    printfn "Starting headless engine (map: %s)..." config.MapName
    let c = new BarClient(config)
    c.Start()
    _client <- Some c
    warmup ()
    openViz ()
    printfn "Viz opened. Keys: 1-0=layers, U/E/G/M=overlays, Home=reset"

/// Start with a custom config.
let startWith (config: EngineConfig) =
    if _client.IsSome then printfn "Session already running."; ()
    else
    printfn "Starting engine (map: %s)..." config.MapName
    let c = new BarClient(config)
    c.Start()
    _client <- Some c
    warmup ()
    openViz ()
    printfn "Viz opened. Keys: 1-0=layers, U/E/G/M=overlays, Home=reset"

/// Stop the session and clean up.
let stop () =
    if _vizRunning then GameViz.stop (); _vizRunning <- false
    match _client with
    | Some c -> c.Stop(); _client <- None; _units <- Map.empty; _grid <- None; printfn "Session stopped."
    | None -> printfn "No session running."

// ── Stepping ─────────────────────────────────────────────────

/// Advance N frames (default 1). Returns the last frame number.
let step (n: int) =
    let c = client ()
    c.WaitFrames n processFrame
    printfn "Frame %d (units: %d)" _frame.FrameNumber _units.Count

/// Advance 1 frame.
let step1 () = step 1

/// Advance N frames and send commands from a handler each frame.
let stepWith (n: int) (handler: GameFrame -> Highbar.AICommand list) =
    let c = client ()
    c.WaitFrames n (fun frame ->
        let cmds = handler frame
        if not cmds.IsEmpty then
            c.SendCommands cmds
        processFrame frame)
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
    let c = client ()
    let cmd = MoveCommand unitId x 0.0f z
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d -> (%.0f, %.0f)" unitId x z

/// Attack-move a unit to (x, z).
let fight (unitId: int) (x: float32) (z: float32) =
    let c = client ()
    let cmd = FightCommand unitId x 0.0f z
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d fight-move -> (%.0f, %.0f)" unitId x z

/// Patrol a unit to (x, z).
let patrol (unitId: int) (x: float32) (z: float32) =
    let c = client ()
    let cmd = PatrolCommand unitId x 0.0f z
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d patrol -> (%.0f, %.0f)" unitId x z

/// Attack a target unit.
let attack (unitId: int) (targetId: int) =
    let c = client ()
    let cmd = AttackCommand unitId targetId
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d attacking %d" unitId targetId

/// Guard another unit.
let guard (unitId: int) (guardId: int) =
    let c = client ()
    let cmd = GuardCommand unitId guardId
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d guarding %d" unitId guardId

/// Stop a unit.
let halt (unitId: int) =
    let c = client ()
    let cmd = StopCommand unitId
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d stopped" unitId

/// Build a structure at (x, z) with facing (0=S, 1=E, 2=N, 3=W).
let build (builderId: int) (defId: int) (x: float32) (z: float32) (facing: int) =
    let c = client ()
    let cmd = BuildCommand builderId defId x 0.0f z facing
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    let name = Callbacks.getUnitDefName (stream ()) defId
    printfn "Unit %d building %s at (%.0f, %.0f)" builderId name x z

/// Self-destruct a unit.
let selfDestruct (unitId: int) =
    let c = client ()
    let cmd = SelfDestructCommand unitId
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    printfn "Unit %d self-destructing" unitId

/// Send multiple commands in one frame step.
let send (cmds: Highbar.AICommand list) =
    let c = client ()
    c.WaitFrames 1 (fun frame ->
        c.SendCommands cmds
        processFrame frame)
    printfn "Sent %d commands (frame %d)" cmds.Length _frame.FrameNumber

// ── Cheats ───────────────────────────────────────────────────

/// Give resources (0=metal, 1=energy).
let give (resourceId: int) (amount: float32) =
    let c = client ()
    let cmd = GiveMeResourceCommand resourceId amount
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
    let name = if resourceId = 0 then "metal" else "energy"
    printfn "Gave %.0f %s" amount name

/// Spawn a unit at (x, z) by definition ID.
let spawn (defId: int) (x: float32) (z: float32) =
    let c = client ()
    let cmd = GiveMeNewUnitCommand defId x 0.0f z
    c.WaitFrames 1 (fun frame ->
        c.SendCommands [cmd]
        processFrame frame)
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

/// Open the live visualization window (called automatically by start).
let viz () =
    if _vizRunning then printfn "Viz already running."; ()
    else
    openViz ()
    printfn "Viz opened. Keys: 1-0=layers, U/E/G/M=overlays, Home=reset"

/// Take a screenshot of the viz window.
let screenshot () =
    if not _vizRunning then printfn "Viz not running."
    else
    match GameViz.screenshot "/tmp" with
    | Ok path -> printfn "Screenshot: %s" path
    | Result.Error msg -> printfn "Screenshot failed: %s" msg

/// Close the visualization window.
let noviz () =
    if _vizRunning then GameViz.stop (); _vizRunning <- false; printfn "Viz closed."
    else printfn "Viz not running."

// ── Help ─────────────────────────────────────────────────────

let help () =
    printfn """
FSBar REPL (auto-viz)
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
Viz:        viz()  noviz()  screenshot()
            (viewer opens automatically on start)
═══════════════════════════════════════════════════════"""

// ── Ready ────────────────────────────────────────────────────

printfn "FSBar REPL loaded. Call start() to launch engine, help() for commands."
