---
name: "fsi-fsbar-load"
description: "Load FSBar assemblies into the FSI MCP server, including the libglfw/libSkiaSharp dlopen preload dance required before any #r. Use when the user wants to poke at FSBar.Viz / FSBar.Client / FSBar.Hub interactively via FSI."
user-invocable: true
---

## FSI MCP server

Runs at `http://127.0.0.1:5020/sse`. Binary at `/home/developer/tools/fsi-mcp-server/server/`.

Start with:
```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --no-build
```

## DLL references are locked

FSI locks DLLs loaded via `#r`. After rebuilding a project, **restart FSI** to pick up new DLLs — use the `mcp__fsi-server__restart_fsi` MCP tool (no full server restart needed).

## Native library preload (required before #r)

```fsharp
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let np = "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)
```

## Reference assemblies

Load from the test output directory — it has all transitive dependencies resolved:

```fsharp
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/<DllName>.dll"
```

## GameViz gotchas

- SkiaSharp GPU backend (GRContext) segfaults here — Viewer uses raster `SKSurface` + GL texture upload instead.
- `Silk.NET.Windowing.Glfw.GlfwWindowing.RegisterPlatform()` must be called before `Window.Create` (done in `Viewer.fs`).
- Throttle viz updates to ~60 fps — calling `onFrame` every `Step()` at high game speed pegs a core.
- `getCornersHeightMap` is live (HighBar `c70559a`, feature 006); empty heightmaps are a query-timing or MapGrid-reshape bug, not a missing proxy callback.
