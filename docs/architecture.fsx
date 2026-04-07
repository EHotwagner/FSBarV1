(**
---
title: Architecture Overview
category: Design
categoryindex: 4
index: 1
---
*)

(**
# Architecture Overview

FSBarV1 is an F# client library that communicates with the Beyond All Reason (BAR) real-time strategy
game engine via the HighBar V2 proxy. This page describes the system design, component responsibilities,
and key architectural decisions.

## System Summary

```
+----------------+       Unix Domain Socket       +------------------+       Engine API       +-------------+
|  F# AI Client  | <--(protobuf over socket)-->   | HighBar V2 Proxy | <--(native C++ API)--> | BAR Engine  |
|  (FSBar.Client)|                                 |  (in-engine AI)  |                        | (Spring RTS)|
+----------------+                                 +------------------+                        +-------------+
```

The F# client never talks to the engine directly. All communication flows through the HighBar V2 proxy,
which runs as an AI module inside the engine process. The proxy translates between its protobuf-based
socket protocol and the engine's native C++ AI callback interface.

## Communication Flow

1. **Launch**: `EngineLauncher` starts the engine with a generated game script
2. **Connect**: `Connection` creates a Unix domain socket listener; the proxy connects to it
3. **Handshake**: `Protocol` exchanges version info and team assignment
4. **Frame Loop**: Each frame: receive events, process them, send commands back
5. **Callbacks**: Mid-frame queries (unit positions, map data) via request/response on the same socket
6. **Shutdown**: Engine sends a shutdown message; client cleans up socket and process

## Component Diagram

```
FSBar.Client Assembly
+------------------------------------------------------------------+
|                                                                  |
|  EngineConfig          ScriptGenerator        EngineLauncher     |
|  (session params)      (game script text)     (process mgmt)     |
|       |                      |                      |            |
|       v                      v                      v            |
|  +----------------------------------------------------------+   |
|  |                      BarClient                            |   |
|  |  (orchestrates lifecycle: start, step, run, stop)         |   |
|  +----------------------------------------------------------+   |
|       |              |               |              |            |
|       v              v               v              v            |
|  Connection      Protocol        Commands        Events          |
|  (socket I/O)    (wire format)   (cmd builders)  (event parse)   |
|       |              |                                           |
|       v              v                                           |
|  Callbacks       MapGrid          MapQuery       MapCache        |
|  (engine queries)(terrain layers) (point queries)(caching)       |
|                                                                  |
+------------------------------------------------------------------+
```

## Module Responsibilities

### EngineConfig

Defines the `EngineConfig` record and `EngineMode` discriminated union. All session parameters live here:
socket path, map name, game type, factions, timeouts, engine binary path, and game speed.
`defaultConfig()` generates a config with sensible defaults and a unique socket path.

### ScriptGenerator

Produces the Spring engine game script (`[GAME]` format) from an `EngineConfig`. This script configures
two teams, two ally-teams, a human player (spectating), our HighBar V2 AI, and the opponent AI.

### EngineLauncher

Starts the engine process (`spring-headless` or AppImage), manages session directories for logs
and scripts, and handles process teardown. Each session gets a directory under `/tmp/`.

### Connection

Low-level socket operations: create a Unix domain socket listener, accept connections with timeout,
send/receive length-prefixed binary frames, and clean up socket files.

### Protocol

Implements the wire protocol on top of `Connection`:
- **Handshake**: exchange `ProxyMessage.Handshake` / `AIMessage.HandshakeResponse`
- **receiveFrame**: deserialize `ProxyMessage.Frame` into `GameFrame`, handle `SaveRequest` transparently
- **sendFrameResponse**: serialize commands as `AIMessage.FrameResponse`
- **sendCallback**: send a callback request and block for the response

### Commands

17 pure functions that construct `Highbar.AICommand` protobuf messages. Each function takes typed
parameters (unit IDs, positions, etc.) and returns a ready-to-send command. All movement/action
commands set the internal order flag (options = 8).

### Events

Defines the `GameEvent` discriminated union (28 cases) and the `fromProto` function that converts
`Highbar.EngineEvent` protobuf messages to F# DU values. Also defines the `GameFrame` record
(frame number + event list).

### Callbacks

26 functions for mid-frame engine queries. Each takes a `NetworkStream` and optional parameters,
sends a callback request via `Protocol.sendCallback`, and returns typed results. Categories include
team info, map info, unit info, economy, and raw map data.

### MapGrid

Loads all map layers (heightmap, slope, resource, LOS, radar) into a `MapGrid` record of `Array2D`
grids. Provides terrain classification (`Land`, `Water`, `Cliff`) and passability computation for
four movement types (`Kbot`, `Tank`, `Hover`, `Ship`). Includes active patterns for pattern matching.

### MapQuery

Point queries and region operations on `MapGrid` data. Converts between elmo coordinates (game world)
and heightmap grid indices. Supports height/slope lookups, rectangular sub-region extraction, and
resource hotspot detection.

### MapCache

Session-level caching using `ConcurrentDictionary`. Caches the `MapGrid` (loaded once per session)
and passability grids (computed once per movement type). `clear()` resets for new sessions.

### BarClient

The main orchestrator. Manages the full session lifecycle:
- Creates socket listener and launches engine
- Accepts connection and performs handshake
- Provides `Step`, `StepWith`, `Run`, `RunUntil` for frame processing
- Handles cleanup on `Stop` or `Dispose`
- Tracks session state: `Idle -> Starting -> Connected -> Running -> Stopped`

## Design Decisions

### Why .fsi Signature Files?

Every module has a corresponding `.fsi` file that defines its public API surface. This provides:
- **Explicit contracts**: The public API is declared, not inferred
- **Surface area testing**: The `SurfaceAreaTests` compare `.fsi` files against baselines to detect accidental API changes
- **Documentation**: Signature files serve as concise API reference

### Why Protobuf (FsGrpc)?

The HighBar V2 proxy uses Protocol Buffers for serialization. FsGrpc 1.0.6 generates idiomatic F#
types from `.proto` files, giving us type-safe message construction and pattern matching on DU-style
message cases.

### Why Unix Domain Sockets?

Unix domain sockets provide low-latency, zero-copy IPC between the F# client and the in-engine proxy.
They avoid TCP overhead and work naturally with the single-machine architecture. The socket path is
generated per session to allow concurrent test runs.

### External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| FsGrpc | 1.0.6 | Protobuf code generation and runtime |
| FsGrpc.Tools | 1.0.6 | Build-time protobuf compilation |
| BarData | (NuGet) | BAR unit definitions and game constants |
| xUnit | 2.9.x | Test framework |
| SkiaViewer | 1.0.0 | Cross-platform windowed OpenGL viewer for SkiaSharp |
| SkiaSharp | 2.88.6 | 2D graphics library (Skia bindings for .NET) |
| Silk.NET | 2.22.0 | Windowing, OpenGL, and input (GLFW backend) |

## Visualization: SkiaViewer + FSBar.Viz

FSBarV1 includes a real-time game visualization system built on the **SkiaViewer** NuGet package.
SkiaViewer is a minimal, standalone viewer that opens an OpenGL window on a background thread and
calls a render callback at a fixed FPS. FSBar.Viz wraps it to display live game state.

### Architecture

```
+-------------+    GameFrame    +----------+    GameSnapshot    +--------------+    SKCanvas    +------------+
| BarClient   | -------------> | GameViz  | ----------------> | SceneBuilder | ------------> | SkiaViewer |
| (engine I/O)|   per frame    | (state)  |   lock-protected  | (compositing)|  60fps render | (window)   |
+-------------+                +----------+                    +--------------+               +------------+
                                    |                               |
                                    v                               v
                               MapGrid/Callbacks            LayerRenderer
                               (query engine)               (Array2D -> SKBitmap)
```

### SkiaViewer Public API

SkiaViewer exposes a single type and function:
*)

(*** do-not-eval ***)
// ViewerConfig record — configures the window and render callbacks
type ViewerConfig = {
    Title: string
    Width: int
    Height: int
    TargetFps: int
    ClearColor: SkiaSharp.SKColor
    OnRender: SkiaSharp.SKCanvas -> unit   // called each frame
    OnResize: int -> int -> unit           // window resized
    OnKeyDown: Silk.NET.Input.Key -> unit  // keyboard input
    OnMouseScroll: float32 -> float32 -> float32 -> unit  // zoom
    OnMouseDrag: float32 -> float32 -> unit               // pan
}

// Viewer.run launches a window on a background thread, returns IDisposable
// val run: ViewerConfig -> IDisposable

(**
### Rendering Pipeline

SkiaViewer uses a **raster surface + GL texture upload** pipeline because the SkiaSharp GPU backend
(GRContext) segfaults in this environment:

1. **Each frame**: SkiaViewer allocates a raster `SKSurface` (CPU-side bitmap)
2. **OnRender callback**: FSBar.Viz draws to the `SKCanvas` using SkiaSharp primitives
3. **Pixel upload**: SkiaViewer extracts pixel data and uploads to an OpenGL texture
4. **Display**: A fullscreen quad renders the texture via Silk.NET's GLFW window
5. **Swap**: Buffers swap at the configured `TargetFps` (default 60)

All rendering is CPU-bound — there is no GPU-accelerated path available.

### What FSBar.Viz Displays

**Map Layers** (switch with keyboard 1-9, 0):

| Key | Layer | Color Scheme |
|-----|-------|-------------|
| 1 | HeightMap | Terrain (blue → green → brown → white) |
| 2 | SlopeMap | Grayscale (flat=dark, steep=bright) |
| 3 | ResourceMap | Heat map (blue → yellow → red) |
| 4 | LOS (Line of Sight) | Binary (red=unseen, green=visible) |
| 5 | Radar coverage | Binary |
| 6 | Terrain classification | Color-coded (Land, Water, Cliff) |
| 7 | Kbot passability | Binary (red=blocked, green=passable) |
| 8 | Tank passability | Binary |
| 9 | Hover passability | Binary |
| 0 | Ship passability | Binary |

**Overlays** (toggle with keyboard shortcuts):

| Key | Overlay | Description |
|-----|---------|-------------|
| U | Units | Blue circles (friendly), red circles (enemy) at game positions |
| E | Events | Expanding/fading rings — green (created), red (destroyed), yellow (spotted) |
| G | Grid | Grid lines over the map |
| M | Metal Spots | Gray circles sized by resource richness |
| — | Economy HUD | Top-right panel: metal/energy current, income, usage, storage |

**Mouse interaction**: scroll to zoom (centered on cursor), drag to pan. Press **Home** to reset
the view (auto-fit entire map).

### How GameViz Connects

`GameViz` is the glue between BarClient and SkiaViewer:
*)

(*** do-not-eval ***)
open FSBar.Viz

// Start the viewer window
GameViz.start None

// Attach to a running BarClient for live data
GameViz.attachToClient myClient

// Enable overlays
GameViz.enableOverlay OverlayKind.Units
GameViz.enableOverlay OverlayKind.Events
GameViz.enableOverlay OverlayKind.MetalSpots
GameViz.enableOverlay OverlayKind.EconomyHud

// Feed frames from the game loop
for _ in 1..1000 do
    let frame = myClient.Step()
    GameViz.onFrame frame  // updates snapshot, triggers re-render

// Stop when done
GameViz.stop ()

(**
Each call to `GameViz.onFrame` queries the engine for unit positions, economy, and dynamic map layers
(LOS/Radar), builds a `GameSnapshot`, and stores it behind a lock. The SkiaViewer render callback
(running at 60fps on its own thread) reads the latest snapshot and composites the scene.

### Session Types

FSBar.Viz provides two session modes:

- **LiveSession**: Launches the engine, creates a BarClient, connects GameViz, and runs a background
  stepping thread. Use this for automated games with real-time visualization.
- **PreviewSession**: Displays saved `MapGrid` or `GameSnapshot` data without a live engine.
  Useful for offline analysis of captured map data.

### Known Limitations

- **CPU-only rendering**: The GPU backend segfaults, so all drawing is raster-based. Large maps
  (512x512) can be CPU-intensive at 60fps.
- **Single window**: Only one SkiaViewer instance per process. Starting a new one stops the previous.
- **No heightmap corners**: The engine proxy does not support `getCornersHeightMap`, so GameViz
  retries loading on each `onFrame` until data is available.
- **Decoupled FPS**: The viewer always renders at 60fps regardless of engine speed. At high game
  speeds (100x), the viewer samples whatever snapshot is latest.
*)
