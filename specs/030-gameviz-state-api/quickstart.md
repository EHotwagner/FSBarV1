# Quickstart: GameViz State-Based Rendering API

**Feature**: 030-gameviz-state-api  
**Date**: 2026-04-16

## What This Feature Does

Adds socket-free entry points to `GameViz` so the trainer bot can drive the
visualizer by passing pre-built `GameState` + `MapGrid` directly, eliminating
protocol corruption and deadlocks from shared socket access.

## New API

```fsharp
// One-time initialization (no socket reads):
GameViz.attachWithState mapGrid metalSpots teamId

// Per-frame rendering (no socket reads):
GameViz.onFrameWithState gameState mapGrid
```

## Usage in Trainer Bot

### Macro bot (has MapGrid from warmup)

```fsharp
// During warmup — map data already loaded:
let mapGrid = cachedMap.Grid   // from MapCacheFile.read
let metalSpots = allSpots  // raw array from Callbacks.getMetalSpots during warmup
let teamId = client.GameState.TeamId

// Start viewer:
GameViz.start (Some vizCfg)
GameViz.attachWithState mapGrid metalSpots teamId

// In frame loop:
GameViz.onFrameWithState client.GameState mapGrid
```

### Simpler bot (no pre-computed MapGrid)

```fsharp
// Construct flat MapGrid from map dimensions:
let flatGrid = MapGrid.flat mapWidth mapHeight

// Or load from cache:
let mapGrid =
    match MapCacheFile.read supportedMap cachePath with
    | Ok loaded -> loaded.Grid
    | Error _ -> MapGrid.flat mapWidth mapHeight

GameViz.start (Some vizCfg)
GameViz.attachWithState mapGrid [||] teamId

// In frame loop:
GameViz.onFrameWithState client.GameState mapGrid
```

## What Changes for Existing Users

Nothing. The existing `attachToClient` + `onFrame` socket-based path remains
fully functional for non-trainer use cases (LiveSession, PreviewSession, REPL
scripts).

## Build & Test

```bash
# Build all projects:
dotnet build

# Run viz tests:
dotnet test tests/FSBar.Viz.Tests

# Run with trainer bot:
cd bots/trainer && ./run.sh --viewer bot.fsx -- <engine-args>
```
