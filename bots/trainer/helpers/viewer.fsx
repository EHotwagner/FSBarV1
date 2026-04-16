// bots/trainer/helpers/viewer.fsx — conditional FSBar.Viz viewer for training runs
//
// When BOT_VIEWER=1: loads native libraries (libglfw, libSkiaSharp) via dlopen,
// references FSBar.Viz + SkiaViewer DLLs from the test output directory, starts
// a GameViz window, and feeds frames from the trainer loop.
//
// When BOT_VIEWER is unset or not "1": all functions are no-ops. Viewer failures
// never crash the trainer (R6, FR-013).
//
// CRITICAL DESIGN NOTE:
// GameViz.attachToClient does heavy callback reads (MapGrid.loadFromEngine,
// getMetalSpots, getMyTeam) that consume frames from the proxy socket. These
// reads MUST happen inside a WaitFrames callback where replayBufferEnabled
// handles frame interleaving. Therefore attachToClient is deferred to the
// first viewerOnFrame call (inside the trainer loop), NOT called at startup.
//
// Must be #loaded AFTER helpers/prelude.fsx (for FSBar.Client types).

open System

let private viewerEnabled =
    match Environment.GetEnvironmentVariable("BOT_VIEWER") with
    | "1" -> true
    | _ -> false

let private displayAvailable =
    match Environment.GetEnvironmentVariable("DISPLAY") with
    | null | "" -> false
    | _ -> true

open System.Runtime.InteropServices

[<DllImport("libdl.so.2")>]
extern nativeint private dlopen(string filename, int flags)

let private nativePath =
    "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"

if viewerEnabled && displayAvailable then
    let np =
        System.IO.Path.GetFullPath(
            System.IO.Path.Combine(__SOURCE_DIRECTORY__, nativePath))
    let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
    let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)
    printfn "[viewer] native libraries loaded from %s" np

    let skiaSharpAsm =
        System.Reflection.Assembly.Load("SkiaSharp")
    NativeLibrary.SetDllImportResolver(skiaSharpAsm,
        DllImportResolver(fun name (_asm: System.Reflection.Assembly) (_path: System.Nullable<DllImportSearchPath>) ->
            if name = "libSkiaSharp" then
                NativeLibrary.Load(np + "/libSkiaSharp.so")
            else
                IntPtr.Zero))
    printfn "[viewer] registered DllImportResolver for SkiaSharp"
elif viewerEnabled then
    printfn "[viewer] WARNING: BOT_VIEWER=1 but DISPLAY is unset — viewer disabled"

#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Core.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Maths.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Common.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Glfw.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.GLFW.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.Common.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.Glfw.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.OpenGL.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"

open FSBar.Client
open FSBar.Viz

let mutable private viewerStarted = false
let mutable private clientAttached = false
let mutable private pendingClient : BarClient option = None

/// Start the viewer window. Does NOT call attachToClient — that is deferred
/// to the first viewerOnFrame call inside the trainer loop where socket
/// interleaving is safe.
let startViewer (client: BarClient) : unit =
    if not viewerEnabled then ()
    elif not displayAvailable then
        printfn "[viewer] skipped — no DISPLAY"
    else
        try
            printfn "[viewer] starting GameViz..."
            let vizCfg =
                { VizDefaults.defaultConfig with
                    ActiveOverlays =
                        Set.ofList
                            [ OverlayKind.Units
                              OverlayKind.Events
                              OverlayKind.MetalSpots
                              OverlayKind.EconomyHud ] }
            GameViz.start (Some vizCfg)
            pendingClient <- Some client
            viewerStarted <- true
            printfn "[viewer] viewer window opened (attachToClient deferred to first frame)"
        with ex ->
            printfn "[viewer] ERROR starting viewer (continuing without): %s" ex.Message
            viewerStarted <- false

/// Feed a frame to the viewer. Call from inside the trainer's WaitFrames
/// callback so all socket reads are serialized. On first call, attaches
/// the client (heavy socket reads for map data) and seeds existing units.
let viewerOnFrame (client: BarClient) (frame: GameFrame) : unit =
    if not viewerStarted then ()
    else
        try
            // Deferred attach: runs inside WaitFrames where replayBufferEnabled
            // handles frame interleaving from callback reads.
            if not clientAttached then
                match pendingClient with
                | Some c ->
                    GameViz.attachToClient c
                    let existingUnits =
                        [ for (KeyValue(uid, u)) in c.GameState.Units do
                            let (px, py, pz) = u.Position
                            yield { UnitId = uid
                                    PositionX = px; PositionY = py; PositionZ = pz
                                    TeamId = 0; DefId = u.DefId
                                    Health = u.Health; MaxHealth = u.MaxHealth
                                    IsEnemy = false }
                          for (KeyValue(eid, e)) in c.GameState.Enemies do
                            let (px, py, pz) = e.Position
                            yield { UnitId = eid
                                    PositionX = px; PositionY = py; PositionZ = pz
                                    TeamId = 1
                                    DefId = (e.DefId |> Option.defaultValue 0)
                                    Health = (e.Health |> Option.defaultValue 100.0f)
                                    MaxHealth = 100.0f
                                    IsEnemy = true } ]
                    if not (List.isEmpty existingUnits) then
                        GameViz.seedUnits existingUnits
                    printfn "[viewer] attached + seeded %d units (deferred)" existingUnits.Length
                    clientAttached <- true
                    pendingClient <- None
                | None -> ()
            GameViz.onFrame frame
        with ex ->
            printfn "[viewer] onFrame error (non-fatal): %s" ex.Message

/// Stop the viewer gracefully. Safe to call even if never started.
let stopViewer () : unit =
    if not viewerStarted then ()
    else
        try
            GameViz.stop ()
            viewerStarted <- false
            clientAttached <- false
            pendingClient <- None
            printfn "[viewer] stopped"
        with ex ->
            printfn "[viewer] WARNING on stop: %s" ex.Message

printfn "[viewer] helper loaded (enabled=%b display=%b)" viewerEnabled displayAvailable
