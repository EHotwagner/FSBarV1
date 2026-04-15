// Feature 027 — Browse cached maps with the new BaseTerrain viz.
//
// Usage:
//   dotnet fsi src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx [map name]
//
// With no arguments the viewer loads the first entry of
// MapCacheFile.supportedMaps. With a map name as the first argument the
// named map is loaded; unknown names fall back to index 0 with a stderr
// warning.
//
// Keybindings (inside the viewer window):
//   B          → switch to the new BaseTerrain layer (default for this
//                script). Brown-on-land, blue-on-water elevation gradient.
//   1..0       → switch to the raw debug layers (HeightMap, SlopeMap,
//                ResourceMap, LosMap, RadarMap, TerrainClassification,
//                Passability Kbot/Tank/Hover/Ship).
//   M          → toggle the metal-spot overlay (default on).
//   G          → toggle the grid overlay.
//   Home       → re-fit the current map to the window (AutoFit on).
//   ]  or  .   → next supported map (cycling wraps).
//   [  or  ,   → previous supported map (cycling wraps).
//   Mouse wheel → zoom toward cursor (disables AutoFit).
//   Left-drag  → pan (disables AutoFit).
//
// Fault-injection example (per quickstart step 6): rename the committed
// cache file aside and re-run — the viewer displays an error banner via
// MapCacheFile.formatLoadError instead of crashing:
//
//   mv bots/trainer/map-cache/avalanche_3.4.json /tmp/avalanche-backup.json
//   dotnet fsi src/FSBar.Viz/scripts/examples/04-base-terrain-cache.fsx
//
// Close the viewer window (or press Enter in the terminal) to exit.

// Preload native libraries and reference the FSBar.Viz test-output bin dir
// (which has every transitive dependency). We avoid `../prelude.fsx` because
// its legacy relative `#r` paths don't resolve from every cwd.
open System.Runtime.InteropServices
[<DllImport("libdl.so.2", EntryPoint = "dlopen")>]
extern nativeint dlopen_ (string filename, int flags)
let private _nativeDir =
    __SOURCE_DIRECTORY__ + "/../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let private _glfwHandle = dlopen_ (_nativeDir + "/libglfw.so.3", 0x2 ||| 0x100)
let private _skiaHandle = dlopen_ (_nativeDir + "/libSkiaSharp.so", 0x2 ||| 0x100)

#I "../../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0"
#r "FSBar.Proto.dll"
#r "FSBar.Client.dll"
#r "FSBar.Viz.dll"
#r "SkiaSharp.dll"
#r "SkiaViewer.dll"

open FSBar.Client
open FSBar.Viz

let args = fsi.CommandLineArgs
let initial =
    if args.Length > 1 && not (System.String.IsNullOrWhiteSpace args.[1]) then
        Some args.[1]
    else
        None

let handle = PreviewSession.startWithCachedMaps MapCacheFile.supportedMaps initial

eprintfn "Viewer running — press Enter in this terminal to exit."
System.Console.ReadLine() |> ignore

handle.Dispose()
