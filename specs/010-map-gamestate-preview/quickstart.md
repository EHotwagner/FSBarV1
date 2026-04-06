# Quickstart: Map & GameState Preview via SkiaViewer

**Date**: 2026-04-06

## Prerequisites

- .NET 10.0 SDK
- X11 display (DISPLAY=:0)
- Built FSBar.Viz project

## Build

```bash
dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
```

## Run Preview Tests (no engine required)

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~MapDataTests|FullyQualifiedName~MockSnapshotTests|FullyQualifiedName~PreviewSessionTests"
```

## Quick Usage via FSI

```fsharp
// Load SkiaViewer and FSBar.Viz assemblies (see CLAUDE.md for dlopen prereqs)
#r ".../FSBar.Viz.dll"

open FSBar.Viz

// 1. Save map data from a live session
// (assumes client is connected)
let grid = MapGrid.loadFromEngine client.Stream
let spots = Callbacks.getMetalSpots client.Stream
MapData.save "/tmp/my-map.fsmg" grid spots

// 2. Load saved map data offline
let (loadedGrid, loadedSpots) = MapData.load "/tmp/my-map.fsmg"

// 3. Build a mock game snapshot
let snapshot =
    MockSnapshot.emptySnapshot loadedGrid
    |> MockSnapshot.withMetalSpots loadedSpots
    |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
    |> MockSnapshot.withFriendlyAt (200.0f, 0.0f, 150.0f)
    |> MockSnapshot.withEnemyAt (400.0f, 0.0f, 400.0f)
    |> MockSnapshot.withEconomy 500.0f 10.0f 8.0f 1000.0f
    |> MockSnapshot.withEvent EventKind.UnitCreated (100.0f, 0.0f, 100.0f) 0

// 4. Preview the snapshot in SkiaViewer
use session = PreviewSession.startWithSnapshot snapshot
// Window opens at 60fps with map + units + events
// Pan: mouse drag, Zoom: scroll, Layers: 1-0, Overlays: U/E/G/M

// 5. Animated playback
let frames =
    [| for i in 0..59 ->
        MockSnapshot.emptySnapshot loadedGrid
        |> MockSnapshot.withFriendlyAt (float32 i * 5.0f, 0.0f, 100.0f)
        |> MockSnapshot.withFrame i |]
use playback = PreviewSession.startPlayback frames 30
// Plays at 30 game-fps, renders at 60 viewer-fps
```

## Key Files

| File | Purpose |
|------|---------|
| `src/FSBar.Viz/MapData.fs` | Binary save/load for MapGrid + metal spots |
| `src/FSBar.Viz/MockSnapshot.fs` | GameSnapshot builder helpers |
| `src/FSBar.Viz/PreviewSession.fs` | SkiaViewer-based offline preview |
| `tests/FSBar.Viz.Tests/MapDataTests.fs` | Save/load round-trip tests |
| `tests/FSBar.Viz.Tests/MockSnapshotTests.fs` | Builder + rendering tests |
| `tests/FSBar.Viz.Tests/PreviewSessionTests.fs` | Preview lifecycle tests |
