// bots/trainer/helpers/viewer.fsx — conditional FSBar.Viz viewer for training runs
//
// When BOT_VIEWER=1: loads native libraries (libglfw, libSkiaSharp) via dlopen,
// references FSBar.Viz + SkiaViewer DLLs from the test output directory, starts
// a GameViz window, and feeds game state from the trainer loop.
//
// When BOT_VIEWER is unset or not "1": all functions are no-ops. Viewer failures
// never crash the trainer (R6, FR-013).
//
// Uses the socket-free state-based path (GameViz.attachWithState +
// GameViz.onFrameWithState) to eliminate socket contention between the
// viewer and the trainer bot. The bot passes its pre-built GameState and
// MapGrid directly — zero socket reads in the visualization path.
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

/// Start the viewer window. When mapGrid is Some, uses the socket-free
/// state-based path (attachWithState). When None, attempts MapCacheFile
/// fallback or constructs a flat MapGrid from map dimensions.
let startViewer (mapGrid: MapGrid option) (metalSpots: (float32 * float32 * float32 * float32) array) (teamId: int) : unit =
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
            match mapGrid with
            | Some grid ->
                GameViz.attachWithState grid metalSpots teamId
                printfn "[viewer] attached via state, map %dx%d" grid.WidthHeightmap grid.HeightHeightmap
            | None ->
                // Fallback: try MapCacheFile, or construct a flat grid
                let flatGrid (widthElmos: int) (heightElmos: int) =
                    let w = max 1 (widthElmos / 64 + 1)
                    let h = max 1 (heightElmos / 64 + 1)
                    { WidthElmos = widthElmos; HeightElmos = heightElmos
                      WidthHeightmap = w; HeightHeightmap = h
                      HeightMap = Array2D.zeroCreate w h
                      SlopeMap = Array2D.zeroCreate w h
                      ResourceMap = Array2D.zeroCreate w h
                      LosMap = Array2D.zeroCreate w h
                      RadarMap = Array2D.zeroCreate w h }
                let grid = flatGrid 8192 8192
                GameViz.attachWithState grid metalSpots teamId
                printfn "[viewer] attached via state with flat map (no MapGrid provided)"
            viewerStarted <- true
        with ex ->
            printfn "[viewer] ERROR starting viewer (continuing without): %s" ex.Message
            viewerStarted <- false

/// Feed a frame to the viewer using the state-based path.
/// No socket reads are performed.
let viewerOnFrame (gameState: GameState) (mapGrid: MapGrid) : unit =
    if not viewerStarted then ()
    else
        try
            GameViz.onFrameWithState gameState mapGrid
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
