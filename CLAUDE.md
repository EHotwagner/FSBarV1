# FSBarV1 Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-06

## Active Technologies
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), BarData (unit definitions), xUnit 2.9.x (002-test-suite-report)
- N/A (file-based report output only) (002-test-suite-report)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk (003-live-game-tests)
- Filesystem only (temp dirs, socket files, log files, Markdown reports) (003-live-game-tests)
- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf), FSBar.Proto (generated types), BarData (unit definitions) (004-array-map-layers)
- In-memory Array2D grids + ConcurrentDictionary caching (004-array-map-layers)
- Filesystem (socket files, session dirs) (005-incorporate-highbarv2-fixes)
- F# / .NET 10.0 + xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (existing in FSBar.Client.Tests) (007-fix-surface-baselines)
- Filesystem — `.baseline` text files committed to git (007-fix-surface-baselines)
- F# / .NET 10.0 + Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Proto (in-repo) (008-game-viz)
- N/A (in-memory only) (008-game-viz)
- F# / .NET 10.0 + SkiaViewer 1.0.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Viz (in-repo) (010-map-gamestate-preview)
- Binary files on disk for MapGrid serialization (010-map-gamestate-preview)
- F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Viz (in-repo), SkiaViewer 1.0.0, SkiaSharp 2.88.6, Silk.NET 2.22.0 (011-live-map-viz)
- N/A (in-memory only, no persistence needed) (011-live-map-viz)

- F# / .NET 10.0 + FsGrpc 1.0.6 (protobuf generation), FsGrpc.Tools 1.0.6 (build-time), BarData (NuGet from local store) (001-fsharp-repl-client)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for F# / .NET 10.0

## Testing

Always run tests against the live environment. Do not use mocks, fakes, or in-memory substitutes.

Tests that cannot pass due to out-of-scope issues (e.g., missing server, external dependency unavailable, unimplemented upstream feature) MUST be marked as skipped or have their assertions relaxed. Never mark a failing test as passed.

## Code Style

F# / .NET 10.0: Follow standard conventions

## Recent Changes
- 011-live-map-viz: Added F# / .NET 10.0 + FSBar.Client (in-repo), FSBar.Viz (in-repo), SkiaViewer 1.0.0, SkiaSharp 2.88.6, Silk.NET 2.22.0
- 010-map-gamestate-preview: Added F# / .NET 10.0 + SkiaViewer 1.0.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Viz (in-repo)
- 009-harden-skiasharp-viewer: Added F# / .NET 10.0 + Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6


<!-- MANUAL ADDITIONS START -->

## FSI MCP Server

The FSI MCP server (`fsi-server`) runs at `http://127.0.0.1:5020/sse` and provides an F# Interactive session via MCP tools.

### Critical: DLL references are locked

FSI locks DLLs loaded via `#r`. After rebuilding a project, you **must restart FSI** to pick up the new DLLs. Use the `restart_fsi` MCP tool to do this without restarting the entire MCP server.

### Starting the MCP server

The server binary is at `/home/developer/tools/fsi-mcp-server/server/`. Start it with:
```
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --no-build
```
- `XDG_RUNTIME_DIR` is required for GLFW windowing (Silk.NET viz)
- `DISPLAY=:0` is required for graphical windows

### Loading FSBar assemblies in FSI

Before loading `#r` references, preload native libraries with `dlopen`:
```fsharp
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let np = "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)
```

Load DLLs from the test output directory (has all transitive dependencies):
```
#r ".../tests/FSBar.Viz.Tests/bin/Debug/net10.0/<DllName>.dll"
```

### GameViz notes

- The SkiaSharp GPU backend (GRContext) segfaults in this environment. The Viewer uses a raster SKSurface + GL texture upload instead.
- `Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform()` must be called before `Window.Create` (done in Viewer.fs).
- The engine proxy does not support `getCornersHeightMap` — heightmap data is empty. GameViz retries loading on each `onFrame` until data is available.
- Throttle viz updates to ~60fps when running the game loop. Calling `onFrame` on every `Step()` at high game speed will consume 100% CPU.

### Engine paths

- Headless engine: `/home/developer/.local/state/Beyond All Reason/engine/recoil_2025.06.19/spring-headless`
- Spring data dir: `/home/developer/.local/state/Beyond All Reason`

<!-- MANUAL ADDITIONS END -->
