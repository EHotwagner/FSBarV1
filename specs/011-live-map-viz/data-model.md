# Data Model: 011-live-map-viz

## Existing Entities (No Changes)

These entities already exist in FSBar.Viz and FSBar.Client and require no modification:

- **MapGrid** (FSBar.Client) — 2D arrays for heightmap, slope, LOS, radar, resource data
- **GameSnapshot** (FSBar.Viz.VizTypes) — Complete frame state: MapGrid + units + events + economy + metal spots
- **UnitState** (FSBar.Viz.VizTypes) — Unit position, team, health, def ID, enemy flag
- **EventIndicator** (FSBar.Viz.VizTypes) — Transient visual event with position, kind, lifetime
- **VizConfig** (FSBar.Viz.VizTypes) — Layer selection, overlay toggles, color schemes, marker sizes
- **ViewState** (FSBar.Viz.VizTypes) — Camera: scale, origin, window size, auto-fit flag
- **LayerKind** (FSBar.Viz.VizTypes) — HeightMap | SlopeMap | ResourceMap | LosMap | RadarMap | TerrainClassification | Passability of MoveType
- **EconomyData** (FSBar.Viz.VizTypes) — Current, income, usage, storage

## New Entity: LiveSessionConfig

Configuration for the orchestration layer that connects engine → client → viz.

| Field | Type | Description |
|-------|------|-------------|
| EngineConfig | EngineConfig | BarClient configuration (engine path, map, socket, etc.) |
| VizConfig | VizConfig option | Optional viz config override (defaults to VizDefaults.defaultConfig) |
| GameSpeed | int | Engine game speed multiplier (default: 1) |
| MaxFrames | int option | Optional frame limit (None = run until window closed or engine stops) |
| OnFrame | (GameFrame -> unit) option | Optional per-frame callback for external consumers |

## New Entity: LiveSessionState

Runtime state for a live session.

| Field | Type | Description |
|-------|------|-------------|
| Client | BarClient | Active engine client |
| StepThread | Thread | Background thread running engine step loop |
| Running | bool (volatile) | Flag to signal step thread to stop |
| FrameCount | int | Number of frames processed |
| LastError | string option | Last error encountered (if any) |

## State Transitions

```
Idle → Starting → Connected → Running → Stopped
                                  ↓
                                Error
```

- **Idle**: No engine, no client
- **Starting**: BarClient.Start() in progress (engine launch + proxy handshake)
- **Connected**: Handshake complete, MapGrid loaded, viz window open
- **Running**: Step thread active, frames flowing to GameViz.onFrame()
- **Stopped**: Clean shutdown (window closed or MaxFrames reached)
- **Error**: Engine crashed, socket disconnected, or timeout
