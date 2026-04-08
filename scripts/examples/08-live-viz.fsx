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

open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let np = __SOURCE_DIRECTORY__ + "/../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)

#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Google.Protobuf.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FsGrpc.dll"
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
