# Data Model: Map & GameState Preview via SkiaViewer

**Date**: 2026-04-06

## Entities

### MapData (new module)

Handles binary serialization of MapGrid + metal spots to/from disk.

**File Format (version 1)**:

| Offset | Size | Type | Field |
|--------|------|------|-------|
| 0 | 4 | byte[] | Magic bytes: "FSMG" |
| 4 | 4 | int32 | Format version (1) |
| 8 | 4 | int32 | WidthHeightmap |
| 12 | 4 | int32 | HeightHeightmap |
| 16 | varies | float32[] | HeightMap flattened row-major, (W+1)*(H+1) elements |
| varies | varies | float32[] | SlopeMap flattened, (W/2)*(H/2) elements |
| varies | varies | int32[] | ResourceMap flattened, W*H elements |
| varies | varies | int32[] | LosMap flattened, W*H elements |
| varies | varies | int32[] | RadarMap flattened, W*H elements |
| varies | 4 | int32 | MetalSpotCount |
| varies | varies | float32[] | MetalSpots: 4 floats per spot (x, y, z, value) |

**Size estimates** for 512x512 map:
- HeightMap: (513 * 513 * 4) = ~1.0 MB
- SlopeMap: (256 * 256 * 4) = ~0.25 MB
- ResourceMap + LosMap + RadarMap: (512 * 512 * 4) * 3 = ~3.0 MB
- Total: ~4.3 MB + header + metal spots

**Validation rules**:
- Magic bytes must equal "FSMG"
- Version must be 1 (or supported)
- WidthHeightmap and HeightHeightmap must be > 0
- Array sizes must match expected dimensions from header

### MockSnapshot (new module)

Pipeline builder functions operating on existing GameSnapshot records. No new types — uses VizTypes.GameSnapshot, UnitState, EventIndicator, EconomyData.

**Builder state**: Each function returns a new GameSnapshot with the modified field. An internal mutable counter generates unique UnitIds for convenience helpers.

| Function | Input | Effect |
|----------|-------|--------|
| emptySnapshot | MapGrid | Creates snapshot with map, frame 0, empty units/events/economy |
| withUnits | UnitState list | Replaces Units map |
| withFriendlyAt | (x, y, z) | Adds a friendly unit at position |
| withEnemyAt | (x, y, z) | Adds an enemy unit at position |
| withEvent | EventKind, (x,y,z), frame | Adds an event indicator |
| withEconomy | current, income, usage, storage | Sets metal economy |
| withEnergyEconomy | current, income, usage, storage | Sets energy economy |
| withMetalSpots | array | Sets metal spot positions |
| withFrame | int | Sets frame number |

### PreviewSession (new module)

Manages a SkiaViewer instance for offline preview. Internal mutable state for VizConfig and ViewState (same pattern as GameViz).

| State Field | Type | Description |
|-------------|------|-------------|
| config | VizConfig | Active visualization config |
| viewState | ViewState | Pan/zoom/window state |
| snapshot | GameSnapshot | Current snapshot being rendered |
| viewer | IDisposable option | Active SkiaViewer handle |
| playbackFrames | GameSnapshot[] option | Sequence for animated playback |
| playbackStopwatch | Stopwatch option | Timing for animated playback |
| gameFps | int | Game state advancement rate |

**State Transitions**:

```
Idle → Started (startWithMap/startWithSnapshot/startPlayback)
Started → Rendering (viewer opened, frames displaying)
Rendering → Idle (stop/dispose)
```

For playback:
```
Rendering → frame index advances with elapsed time
           → wraps or stops at end of sequence
```
