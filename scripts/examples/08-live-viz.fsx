// 08-live-viz.fsx — Live 60fps map visualization with a headless engine
//
// Launches a headless BAR engine, connects via the HighBar V2 proxy,
// and opens a real-time visualization window at 60fps.
//
// Controls:
//   1-0    Switch base layer (height/slope/resource/LOS/radar/terrain/passability)
//   U      Toggle unit overlay
//   E      Toggle event indicators
//   G      Toggle grid lines
//   M      Toggle metal spots
//   Home   Reset view (auto-fit)
//   Scroll Zoom in/out
//   Drag   Pan
//
// Usage:
//   dotnet fsi scripts/examples/08-live-viz.fsx

System.Environment.SetEnvironmentVariable("XDG_RUNTIME_DIR", "/tmp/runtime-developer")
System.Environment.SetEnvironmentVariable("DISPLAY", ":0")

// NuGet package references — FSBar.Viz pulls in all transitive deps.
// Run ./pack-dev.sh before loading this script to ensure packages are current.
#r "nuget: FSBar.Viz, *-*"

// Load native libs for viz support (FSI doesn't auto-resolve NuGet native assets)
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
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

open FSBar.Client
open FSBar.Viz

let config = EngineConfig.defaultConfig ()

printfn "Starting live visualization (map: %s)..." config.MapName
let session = LiveSession.start config None

printfn "Live viz running! Press Ctrl+C to stop."
printfn "Use keys 1-0 to switch layers, U/E/G/M for overlays."

// Keep the script alive until interrupted
while session.IsRunning do
    System.Threading.Thread.Sleep(1000)
    printfn "  Frame %d" session.FrameCount

(session :> System.IDisposable).Dispose()
printfn "Session ended. Total frames: %d" session.FrameCount
