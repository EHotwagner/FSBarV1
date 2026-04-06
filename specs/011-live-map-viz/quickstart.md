# Quickstart: 011-live-map-viz

## What This Feature Adds

A `LiveSession` module in FSBar.Viz that orchestrates a complete live visualization pipeline: launch headless engine → connect client → run game loop on background thread → feed frames to GameViz at 60fps.

## Building

```bash
cd /home/developer/projects/FSBarV1
dotnet build src/FSBar.Viz/
dotnet pack src/FSBar.Viz/ -o ~/.local/share/nuget-local/
```

## Running Live Visualization (FSI Script)

```fsharp
// Load from test output (has all transitive deps)
#r "tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
// ... (see full prelude for native lib loading)

open FSBar.Client
open FSBar.Viz

let config = EngineConfig.defaults |> EngineConfig.withMap "Avalanche 3.4"
let session = LiveSession.start(config)
// Window opens with live 60fps map visualization
// Press 1-0 to switch layers, U/E/G/M for overlays
// Mouse scroll to zoom, drag to pan

// When done:
session.Dispose()
```

## Running Tests

```bash
# Unit tests (no engine required)
dotnet test tests/FSBar.Viz.Tests/ --filter "FullyQualifiedName~LiveSession"

# Integration tests (requires engine binary)
FSBAR_ENGINE_PATH=~/.local/state/engine-2025.06.21/spring-headless \
dotnet test tests/FSBar.Viz.Tests/ --filter "Collection=VizEngine"
```

## Key Controls

| Key | Action |
|-----|--------|
| 1-0 | Switch base layer (height/slope/resource/LOS/radar/terrain/passability) |
| U | Toggle unit overlay |
| E | Toggle event indicators |
| G | Toggle grid lines |
| M | Toggle metal spots |
| Home | Reset view (auto-fit) |
| Mouse scroll | Zoom in/out |
| Mouse drag | Pan |
