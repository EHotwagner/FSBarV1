# Data Model: 032-lockfree-viewer-dataflow

**Date**: 2026-04-16

## Entities

### Existing (unchanged)

| Entity | Location | Role |
|--------|----------|------|
| `GameSnapshot` | VizTypes.fs:198 | Immutable record: complete renderable state for one frame |
| `UnitState` | VizTypes.fs:173 | Immutable record: per-unit position/health/team/def snapshot |
| `UnitDisplay` | VizTypes.fs:91 | Immutable record: enriched unit with glyph/faction/tier/commands |
| `EventIndicator` | VizTypes.fs:184 | Immutable record: transient visual event (create/destroy/spot/combat) |
| `EconomyData` | VizTypes.fs:192 | Immutable record: resource bar snapshot |
| `VizConfig` | VizTypes.fs:169 | Immutable record: visualization configuration |
| `ViewState` | VizTypes.fs:160 | Immutable record: camera/viewport state |

### New (internal to GameViz module)

#### `RawFrame`

An internal record capturing the raw inputs from the bot thread before
any derived-data computation. This is the type atomically published by
the bot thread and sampled by the render thread.

```
Fields:
  GameState    : GameState      // raw protocol state from FSBar.Client
  MapGrid      : MapGrid        // current map grid
  MyTeamId     : int            // local team for friend/foe classification
  MetalSpots   : (float32 * float32 * float32 * float32) array
  FrameCounter : int            // monotonic counter for change detection
```

**Not public** — internal to GameViz module, no .fsi entry needed.

#### `RenderState`

An internal record holding all render-thread-local derived state. Owned
exclusively by the render thread — never accessed by the bot thread.

```
Fields:
  CurrentSnapshot   : GameSnapshot          // latest built snapshot
  PreviousSnapshot  : GameSnapshot option   // for interpolation
  Units             : Map<int, UnitState>   // current unit states
  PrevUnits         : Map<int, UnitState>   // previous for lerp
  Indicators        : EventIndicator list   // active visual events
  DefPropsCache     : ConcurrentDictionary  // unit definition cache
  InterpStopwatch   : Stopwatch             // interpolation timer
  LastFrameCounter  : int                   // change detection
```

**Not public** — internal to GameViz module.

## State Flow Diagram

```
Bot Thread                    Shared                     Render Thread
──────────                    ──────                     ─────────────
GameState ──→ build RawFrame
              ──→ Interlocked.Exchange ──→ latestFrame
                                          (atomic ref)
                                                         FrameTick:
                                                         read latestFrame
                                                         ↓ (if new)
                                                         rebuild Units map
                                                         compute Indicators
                                                         build DisplayUnits
                                                         build GameSnapshot
                                                         ↓ (every tick)
                                                         interpolate positions
                                                         buildScene → Scene
                                                         emit to viewer
```

## Threading Invariants

1. `latestFrame` is the **only** shared mutable state between threads
2. It is accessed exclusively via `Interlocked.Exchange` (write) and
   `Volatile.Read` (read)
3. All `RenderState` fields are exclusively owned by the render thread
4. `VizConfig` and `ViewState` use a separate `configLock` for
   infrequent mutations from public API calls
5. `SceneBuilder` mutable state remains render-thread-local (unchanged)
