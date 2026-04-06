# Quickstart: Game State Visualization

## Prerequisites

- FSBarV1 repo built (`dotnet build`)
- BAR engine available (headless or graphical)

## Build

```bash
dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
```

## REPL Usage

```fsharp
#load "scripts/prelude.fsx"
open FSBar.Viz

// Start a headless game session
let client = BarClient.startHeadless()

// Open the visualization window (background thread)
GameViz.start None
GameViz.attachToClient client

// Game loop: step the game, viz updates automatically
let frame = client.Step()
GameViz.onFrame frame

// Switch base layer
GameViz.setBaseLayer LayerKind.SlopeMap

// Toggle overlays
GameViz.toggleOverlay OverlayKind.Units
GameViz.toggleOverlay OverlayKind.LosMap

// Customize
GameViz.setMarkerSize 8.0f
GameViz.setOverlayOpacity 0.6f

// Pan and zoom via REPL
GameViz.pan 100.0f 0.0f
GameViz.zoom 2.0f 512.0f 320.0f
GameViz.resetView()

// Stop
GameViz.stop()
client.Stop()
```

## Keyboard Controls

While the visualization window is focused:
- **1–0**: Switch base layer (height, slope, resource, LOS, radar, terrain, passability variants)
- **U/E/G/M/$**: Toggle overlays (units, events, grid, metal spots, economy)
- **Mouse wheel**: Zoom in/out
- **Click+drag**: Pan
- **Home**: Reset to full-map auto-fit view

## Running Tests

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
```

Note: Tests require a running BAR engine session. See `tests/FSBar.LiveTests/` for the existing live test pattern.
