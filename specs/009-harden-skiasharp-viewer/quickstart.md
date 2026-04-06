# Quickstart: Harden SkiaSharp OpenGL Viewer

**Date**: 2026-04-06

## Prerequisites

- .NET 10.0 SDK
- X11 display (DISPLAY=:0)
- GLFW native library (via Silk.NET)
- SkiaSharp native library (via SkiaSharp.NativeAssets.Linux.NoDependencies)

## Build

```bash
dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
```

## Run Standalone Viewer Tests

These tests exercise the viewer with SkiaSharp primitives only — no game engine required:

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~ViewerTests"
```

## Run All Viz Tests (requires game engine)

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
```

## Quick Smoke Test via FSI

```fsharp
open System.Runtime.InteropServices
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let np = "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)

#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.dll"
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Glfw.dll"
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.OpenGL.dll"
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.dll"
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "/home/developer/projects/FSBarV1/tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"

open FSBar.Viz
open SkiaSharp

let config: ViewerConfig =
    { Title = "Viewer Smoke Test"
      Width = 800; Height = 600; TargetFps = 60
      ClearColor = SKColors.DarkSlateGray
      OnRender = fun canvas fbSize ->
          use paint = new SKPaint(Color = SKColors.Coral, IsAntialias = true)
          canvas.DrawRect(50.0f, 50.0f, 200.0f, 100.0f, paint)
          paint.Color <- SKColors.White
          canvas.DrawText("Viewer OK", 80.0f, 110.0f, paint)
      OnResize = fun _ _ -> ()
      OnKeyDown = fun _ -> ()
      OnMouseScroll = fun _ _ _ -> ()
      OnMouseDrag = fun _ _ -> () }

let viewer = Viewer.run config
// ... observe window, then:
// viewer.Dispose()
```

## Key Files

| File | Purpose |
|------|---------|
| `src/FSBar.Viz/Viewer.fs` | Core viewer (primary hardening target) |
| `src/FSBar.Viz/Viewer.fsi` | Public API contract |
| `tests/FSBar.Viz.Tests/ViewerTests.fs` | NEW: standalone hardening tests |
| `tests/FSBar.Viz.Tests/Baselines/Viewer.baseline` | Surface area baseline |
