# Quickstart: FSBar.Viz (Revamped)

**Branch**: `019-revamp-viz-library` | **Date**: 2026-04-10

## Build

```bash
cd /home/developer/projects/FSBarV1
dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
```

## Run Tests

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
```

## Interactive Use (FSI)

```fsharp
#load "src/FSBar.Viz/scripts/prelude.fsx"

// Preview a synthetic scene
open FSBar.SyntheticData
let scene = Scenes.generate SceneId.SceneA

// Convert to GameSnapshots and play back
open FSBar.Viz
let snapshots = scene.Frames |> Array.map convertToSnapshot |> Array.toSeq
let handle = PreviewSession.startPlayback snapshots 30
// ... interactive exploration ...
handle.Dispose()
```

## Key Concepts

### Declarative Scene Pipeline

The library emits `SkiaViewer.Scene` trees (not imperative canvas drawing):

```
GameSnapshot → SceneBuilder.buildScene → Scene → SkiaViewer.Viewer.run
```

### Keyboard Shortcuts

| Key | Action |
|-----|--------|
| 1-9, 0 | Switch base layer (HeightMap through Passability) |
| U | Toggle Units overlay |
| E | Toggle Events overlay |
| G | Toggle Grid overlay |
| M | Toggle MetalSpots overlay |
| H | Toggle Economy HUD |
| Home | Reset view (auto-fit) |

### Mouse Controls

| Input | Action |
|-------|--------|
| Scroll | Zoom in/out centered on cursor |
| Drag | Pan the viewport |

## Architecture Overview

```
VizTypes (types) → ColorMaps (color schemes)
                  → LayerRenderer (map data → SKBitmap, cached)
                  → SceneBuilder (snapshot → Scene tree with shaders/effects)
                  → MapData (binary save/load)
                  → MockSnapshot (test builders)
                  → PreviewSession (offline preview via SkiaViewer)
                  → GameViz (live REPL API, emits Scene observable)
                  → LiveSession (engine → GameViz orchestration)
```

## Visual Effects

- **Terrain layers**: SKBitmap rendered through LayerRenderer, displayed via Element.Image
- **Unit markers**: Ellipses with RadialGradient shader (cyan=friendly, red=enemy)
- **Event indicators**: Animated expanding rings with opacity fade and blur glow
- **Economy HUD**: Linear gradient bar gauges with Perlin noise textured background
- **Metal spots**: Radial gradient circles showing richness intensity
