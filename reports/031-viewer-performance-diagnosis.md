# Viewer Performance Diagnosis: Full-Viz Trainer Mode

**Date**: 2026-04-16
**Feature**: 031-full-trainer-viz-run
**Author**: Diagnostic session during feature implementation

## Executive Summary

Running the trainer bot with the GameViz viewer active caused catastrophic
performance degradation in the macro bot (bot_macro.fsx): game simulation
slowed from ~150 game-fps to <1 game-fps. The simple bot (bot.fsx) was
also affected but to a lesser degree. Root cause is `onFrameWithState`
doing expensive work under `stateLock` on the bot's critical path,
combined with per-frame protocol delivery in the macro bot.

## Environment

- GPU: NVIDIA GeForce RTX 4070
- Backend: Vulkan (4x MSAA) -- upgraded from GL raster during this session
- SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6, Silk.NET 2.22.0
- Game speed: level 2 (5x, engine runs at ~150 game-fps)

## Problem Statement

With `--full-viz` (all 8 overlays + BaseTerrain), unit movement appeared
jerky despite the viewer rendering at a solid 60 fps. Subsequent
investigation revealed two distinct performance issues at different layers
of the architecture.

---

## Issue 1: Low State Update Rate (simple bot)

### Observed Behavior

- Viewer renders at 60 fps (confirmed via perf counter)
- Game state updates arrive at **4-5 per second** with **delta=31 game frames**
- Unit positions jump visibly every ~200ms

### Diagnosis

The HighBar V2 proxy uses a request-response protocol over a Unix domain
socket. The Spring/Recoil engine does not invoke the AI callback every
sim frame -- it uses a "slow update" cycle of approximately once per
game-second (~30 sim frames). At 5x game speed:

    30 base-fps x 5x speed = 150 sim-fps
    AI callback every ~30 frames = 5 callbacks/sec
    5 callbacks/sec x 31 frames/callback = 155 game-fps (consistent)

The bot calls `WaitFrames 1` in a tight loop, but each "protocol frame"
delivered by the proxy contains ~31 game frames worth of state. The bot
only sees state 5 times per second regardless of how fast it processes.

### Root Cause

**Spring engine AI callback frequency** -- architectural, not a bug.
The engine batches ~30 sim frames between AI interface invocations. The
proxy faithfully forwards one protocol message per callback.

### Resolution

**Position interpolation** in `GameViz.emitScene`. Between state updates,
unit positions are linearly interpolated (lerp) from previous to current
positions using a wall-clock timer:

```
interpT = min 1.0 (elapsedSinceLastUpdate / estimatedUpdateInterval)
position = prev + (current - prev) * interpT
```

This smooths the 5 ups into visually continuous 60 fps movement.
Implemented in `GameViz.fs` (`lerpUnit`, `emitScene`).

---

## Issue 2: Simulation Throughput Collapse (macro bot)

### Observed Behavior

With viewer active:
- Game advances at **<1 game frame per second** (frame 173 after 2 minutes)
- `state=0-1 ups`, `delta=1` (per-frame protocol delivery)
- Commander issues first BuildCommand but never completes any buildings

Without viewer:
- Game completes 20,825 frames in ~2.5 minutes (~140 game-fps)
- Full build order executes: 2 mex, 2 solar, factory, production phase, attack, win

With viewer started but **never fed frames** (viewerOnFrame disabled):
- Game completes 20,700 frames in ~2.5 minutes -- **identical to no-viewer**

### Diagnosis

The macro bot differs from the simple bot in a critical way: it performs
**callback round-trips** during frame processing (e.g., `Callbacks.getUnitPos`
for the commander-idle probe). Each callback forces the proxy to flush
frames individually instead of batching, resulting in `delta=1` (one game
frame per protocol message).

This means `viewerOnFrame` is called for every single game frame instead
of every 31st frame. Each `onFrameWithState` call:

1. Acquires `stateLock`
2. Processes all game events (list iteration, map lookups)
3. Rebuilds the entire `units` map (Map.add in loop over all units + enemies)
4. Ensures `DefProps` for all encountered DefIds (Map lookup + potential BarData resolve)
5. Rebuilds `unfinishedUnits` set
6. Prunes expired `EventIndicators`
7. Builds a `GameSnapshot` including `buildDisplayUnits()` which maps over all units
8. Releases `stateLock`

Meanwhile, the viewer's render thread acquires the same `stateLock` on
every FrameTick (60/sec) to run `emitScene` (which includes
`SceneBuilder.buildScene` with 8 active overlays).

The combined effect:
- Bot thread: ~150 `onFrameWithState` calls/sec, each holding `stateLock` for ms
- Render thread: 60 `emitScene` calls/sec, each holding `stateLock` for ms
- Result: massive lock contention, both threads mostly waiting

### Hypothesis Confirmation

| Configuration | Game FPS | Outcome |
|---|---|---|
| No viewer (`--speed 2`) | ~140 | Win at 20825 frames |
| Viewer started, never fed | ~140 | Win at 20700 frames |
| Viewer fed every frame | <1 | Timeout at frame 173 |
| Viewer fed every 5th frame | <1 | Timeout at frame 185 |

The skip-5 throttle didn't help because each `onFrameWithState` call
(even at 1/5 frequency) still holds `stateLock` long enough to block both
the render thread and the bot thread's next protocol round-trip.

### Root Cause

**`onFrameWithState` performs O(n) work under a shared lock** where n is
the number of tracked units. With the macro bot's per-frame protocol
delivery, this work runs 150x more frequently than with the simple bot's
batched delivery (150/sec vs 5/sec). The `stateLock` creates a
bidirectional bottleneck between the simulation thread and the render
thread.

### Current Mitigation

Frame-skip throttle of 30 in `bot_macro.fsx` (only call `viewerOnFrame`
every 30th game frame). At 150 game-fps this yields ~5 viewer updates/sec
-- matching the simple bot's natural cadence. Not yet validated under
full game conditions.

### Recommended Long-Term Fix

Decouple state ingestion from scene building:

1. **`onFrameWithState`**: Store only raw `GameState` reference (O(1), no
   lock needed -- use `Interlocked.Exchange` or volatile write)
2. **`emitScene`** (render thread): Read the latest `GameState` reference,
   rebuild `units` map and `DisplayUnits` on the render thread, build scene
3. **Remove `stateLock`** from the hot path entirely

This eliminates lock contention: the bot thread runs at full speed
publishing state references, and the render thread independently samples
the latest state at 60 fps. The only cost is that the render thread does
more work per frame, but at 60 fps with GPU-accelerated Vulkan rendering
this is well within budget.

---

## Ancillary Findings

### GL Raster Backend Was Unnecessary

The `PreferredBackend = Backend.GL` setting in `GameViz.fs` was a
workaround for a historical segfault in the Vulkan/GRContext
initialization path. Testing on 2026-04-16 confirmed Vulkan works
correctly with the current driver (NVIDIA 595.58.03):

```
[Viewer] Backend selected: Vulkan (NVIDIA GeForce RTX 4070)
[Viewer] Vulkan MSAA: max=8, using=4x
```

Switched to `Backend.Vulkan` permanently. The GL raster path
(CPU render + texture upload) is no longer needed.

### Perf Counter Added

`GameViz.emitScene` now displays a performance overlay in the bottom-left:

```
render 60 fps | state 5 ups | game frame 3586 (delta 31)
```

This was essential for diagnosing the state update rate vs render rate
mismatch. Logs to stdout every 5 seconds for headless diagnostics.

### Economy HUD Enlarged

`SceneBuilder.buildEconomyHud` dimensions increased from 220x90 to
320x110 with larger bars (260x20), 16pt labels, and 13pt white value
text for readability at a glance during full-viz sessions.

---

## Files Changed

| File | Change |
|---|---|
| `src/FSBar.Viz/GameViz.fs` | Vulkan backend, interpolation, perf counter, state tracking |
| `src/FSBar.Viz/SceneBuilder.fs` | Larger economy HUD, white font |
| `bots/trainer/bot.fsx` | Map cache loading, wall-clock viewer throttle |
| `bots/trainer/bot_macro.fsx` | `BOT_FULL_VIZ` maxFrames override, frame-skip viewer throttle |
| `bots/trainer/helpers/viewer.fsx` | `fullVizEnabled` flag, expanded overlays + BaseTerrain |
| `bots/trainer/run.sh` | `--full-viz` flag, speed default, `BOT_FULL_VIZ` export, meta.json |
