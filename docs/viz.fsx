(**
---
title: Visualization
category: How-To
categoryindex: 3
index: 5
description: Live and preview sessions, layer rendering, and scene API via FSBar.Viz.
---
*)

(**
# Visualization (`FSBar.Viz`)

`FSBar.Viz` renders a running `BarClient` session — or saved map data — in a SkiaViewer
window. It uses SkiaSharp for 2D primitives on top of a Silk.NET/GLFW OpenGL window and
composes its output as a declarative `Scene` tree.

The library covers three use cases:

| Use case | Entry point |
|----------|-------------|
| Live game, background stepping | `LiveSession.start` |
| Live game, existing client | `LiveSession.startWithClient` |
| Offline map preview / playback | `PreviewSession.startWithMap` / `startWithSnapshot` / `startPlayback` |
| Hand-driven viewer from a REPL | `GameViz.start` + `GameViz.attachToClient` |

All rendering is **CPU-bound** because the SkiaSharp GPU backend segfaults in this
environment — see [Known Issues](known-issues.html).

## Core Types

### LayerKind

Selects which map layer is drawn as the base image.
*)

(*** do-not-eval ***)
open FSBar.Client
open FSBar.Viz

let layers : LayerKind list = [
    LayerKind.HeightMap
    LayerKind.SlopeMap
    LayerKind.ResourceMap
    LayerKind.LosMap
    LayerKind.RadarMap
    LayerKind.TerrainClassification
    LayerKind.Passability MoveType.Kbot
]

(**
### OverlayKind

Overlays draw on top of the base layer. Multiple overlays can be active at once.
*)

(*** do-not-eval ***)
let overlays : OverlayKind list = [
    OverlayKind.Units       // friendly/enemy circles at unit positions
    OverlayKind.Events      // expanding rings for UnitCreated / Destroyed / EnemySpotted
    OverlayKind.Grid        // grid lines
    OverlayKind.MetalSpots  // metal spot markers sized by richness
    OverlayKind.EconomyHud  // metal/energy HUD panel
]

(**
### GameSnapshot

`GameSnapshot` is the per-frame input to the renderer. It bundles the `MapGrid`, all
known units, event indicators, and both economy readings.
*)

(*** do-not-eval ***)
type GameSnapshot =
    { FrameNumber: int
      MapGrid: MapGrid
      Units: Map<int, UnitState>
      EventIndicators: EventIndicator list
      EconomyMetal: EconomyData
      EconomyEnergy: EconomyData
      MetalSpots: (float32 * float32 * float32 * float32) array
      Connected: bool }

(**
### VizConfig

`VizConfig` holds the current base layer, active overlays, color schemes per layer,
marker sizes, grid spacing, and background/label colors. `VizDefaults.defaultConfig` gives
a sensible starting point.
*)

(**
## GameViz: Singleton Live API

`GameViz` owns a single process-wide viewer. Start it, attach it to a running client, and
`GameViz.onFrame` feeds the snapshot forward each frame.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()

// Start the viewer with default config
GameViz.start None

// Attach to the client — GameViz subscribes to client.Frames internally
GameViz.attachToClient client

// Or feed frames manually (e.g. when driving via WaitFrames)
client.WaitFrames 1000 (fun frame ->
    GameViz.onFrame frame)

// Toggle base layer and overlays at runtime
GameViz.setBaseLayer LayerKind.TerrainClassification
GameViz.enableOverlay OverlayKind.Units
GameViz.enableOverlay OverlayKind.MetalSpots
GameViz.enableOverlay OverlayKind.EconomyHud

// Pan / zoom / reset view
GameViz.pan -100.0f 0.0f
GameViz.zoom 1.5f 0.5f 0.5f
GameViz.resetView ()

// Capture a screenshot to disk
match GameViz.screenshot "/tmp" with
| Ok path -> printfn "Saved screenshot: %s" path
| Error msg -> printfn "Screenshot failed: %s" msg

GameViz.stop ()

(**
### Keyboard Shortcuts

The viewer interprets the following keys by default (handled internally by `GameViz`):

| Key | Action |
|-----|--------|
| 1 | HeightMap base layer |
| 2 | SlopeMap |
| 3 | ResourceMap |
| 4 | LOS |
| 5 | Radar |
| 6 | Terrain classification |
| 7 | Kbot passability |
| 8 | Tank passability |
| 9 | Hover passability |
| 0 | Ship passability |
| U | Toggle Units overlay |
| E | Toggle Events overlay |
| G | Toggle Grid overlay |
| M | Toggle MetalSpots overlay |
| Home | Reset view (auto-fit entire map) |

Mouse: scroll to zoom (centered on cursor), drag to pan.

## LiveSession: Managed Lifecycle

`LiveSession.start` launches the engine **and** the viewer, returning a disposable
handle that owns both. This is the simplest way to run "a live game with a window" from
a script.
*)

(*** do-not-eval ***)
let engineConfig = BarClient.defaultConfig ()
let vizConfig = None  // use defaults

use session = LiveSession.start engineConfig vizConfig

// Introspection
printfn "Frames delivered: %d" session.FrameCount
printfn "Running: %b" session.IsRunning
match session.LastError with
| Some msg -> printfn "Last error: %s" msg
| None -> ()

(**
If you already have a `BarClient` (for example, because you want to `WaitFrames` and
issue commands), use `startWithClient` instead — it will not create or own the engine.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()
use session = LiveSession.startWithClient client None

client.WaitFrames 2000 (fun _ -> ())

(**
## PreviewSession: Offline Viewing

`PreviewSession` shows saved or synthetic data without a live engine. This is useful for:

- Inspecting a captured `MapGrid` from an earlier run
- Validating synthetic scenes from `FSBar.SyntheticData`
- Building static diagrams for documentation

### From a saved MapGrid

`MapData.save` / `MapData.load` round-trip a `MapGrid` plus metal spots as a binary file.
*)

(*** do-not-eval ***)
let (grid, metalSpots) = MapData.load "/tmp/avalanche.mapgrid"
use _ = PreviewSession.startWithMap grid

(**
### From a single snapshot

Show one static `GameSnapshot` (useful for reproducing a specific frame from a bug).
*)

(*** do-not-eval ***)
let snapshot : GameSnapshot = MockSnapshot.emptySnapshot grid |> MockSnapshot.withMetalSpots metalSpots
use _ = PreviewSession.startWithSnapshot snapshot

(**
### Playback loop

Play a sequence of `GameSnapshot` values at the given game FPS, looping indefinitely.
Combine with `FSBar.SyntheticData` to preview generated scenes.
*)

(*** do-not-eval ***)
open FSBar.SyntheticData

let scene = Scenes.generate SceneA
let snapshots = scene.Frames |> Array.map (fun _ -> snapshot)  // project to GameSnapshot
use _ = PreviewSession.startPlayback snapshots 30

(**
## MockSnapshot: Builders for Tests and Samples

`MockSnapshot` offers a tiny fluent builder so unit tests and docs can construct a
`GameSnapshot` without a running engine.
*)

(*** do-not-eval ***)
let demo =
    MockSnapshot.emptySnapshot grid
    |> MockSnapshot.withFriendlyAt (2000.0f, 100.0f, 2000.0f)
    |> MockSnapshot.withEnemyAt (3000.0f, 100.0f, 3000.0f)
    |> MockSnapshot.withEconomy current=500.0f income=10.0f usage=7.0f storage=1000.0f
    |> MockSnapshot.withEvent EventKind.UnitCreated (2000.0f, 100.0f, 2000.0f) 0
    |> MockSnapshot.withFrame 0

(**
## SceneBuilder: Declarative Rendering

`SceneBuilder.buildScene` converts a `GameSnapshot`, `VizConfig`, and `ViewState` into a
SkiaViewer `Scene` tree — a pure, declarative description of everything to draw. This is
the same function `GameViz` and `PreviewSession` use internally. You can call it directly
to drive a custom SkiaViewer instance or to inspect what the viz will produce.
*)

(*** do-not-eval ***)
open SkiaViewer

let viewState = VizDefaults.defaultViewState
let config = VizDefaults.defaultConfig
let scene : Scene = SceneBuilder.buildScene demo config viewState
// 'scene' is a declarative tree — hand it to SkiaViewer's renderer

(**
## LayerRenderer: Cached Layer Bitmaps

For map layers (heightmap, slope, etc.) `SceneBuilder` delegates to `LayerRenderer`,
which caches the rendered `SKBitmap` per layer. Use `invalidateCache` when a layer's
underlying data changes (typically LOS/Radar refreshes each frame, static layers never).
*)

(*** do-not-eval ***)
let bitmap = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
LayerRenderer.invalidateCache LayerKind.LosMap       // when LOS data changes
LayerRenderer.invalidateAll ()                       // after load_from_engine refresh
let (hits, misses) = LayerRenderer.cacheStats ()
printfn "Cache: %d hits, %d misses" hits misses

(**
## ColorMaps: Built-in Schemes

Four built-in color schemes plus a per-layer default selector.
*)

(*** do-not-eval ***)
let grayscale : ColorScheme = ColorMaps.grayscale
let terrain   : ColorScheme = ColorMaps.terrain
let heat      : ColorScheme = ColorMaps.heatMap
let binary    : ColorScheme = ColorMaps.binary
let forLayer  : ColorScheme = ColorMaps.colorSchemeFor LayerKind.SlopeMap

(**
## Next Steps

- [Synthetic Data](synthetic-data.html) — produce `GameSnapshot` sequences without an engine
- [Game State](gamestate.html) — the `BarClient` side that feeds `GameViz`
- [Map Analysis](map-analysis.html) — `MapGrid` / `MapQuery` / `MapCache` the viewer
  consumes
*)
