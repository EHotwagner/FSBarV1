# Quickstart: 032-lockfree-viewer-dataflow

**Date**: 2026-04-16

## What This Feature Does

Eliminates lock contention between the bot thread and the viewer render
thread by replacing the shared `stateLock` with an atomic
publish-sample pattern. The bot publishes game state via a lock-free
reference swap; the render thread independently samples the latest
state on each frame tick.

## Key Files to Change

| File | Change |
|------|--------|
| `src/FSBar.Viz/GameViz.fs` | Core refactoring: replace `stateLock` hot path with atomic reference swap, move derived-state computation to render thread |
| `src/FSBar.Viz/GameViz.fsi` | No signature changes needed (internal refactoring only) |
| `bots/trainer/bot_macro.fsx` | Remove or relax frame-skip throttle (no longer needed) |
| `tests/FSBar.Viz.Tests/Baselines/GameViz.baseline` | Update to match current .fsi (pre-existing debt) |

## How to Verify

### Bot Performance Test
```bash
# Baseline (no viewer):
cd bots/trainer && ./run.sh --speed 2 --no-viz
# Note game completion time and frame count

# With viewer (should match baseline within 10%):
cd bots/trainer && ./run.sh --speed 2 --full-viz
# Compare game completion time
```

### Visual Regression Test
```bash
# Run with viewer, visually confirm:
# - Smooth unit movement (interpolation working)
# - All 8 overlays rendering correctly
# - Perf counter showing ~60 fps render, ~5 ups state
# - Hotkeys (W/L/C/N) responsive during gameplay
# - Pan/scroll/zoom responsive during gameplay
```

### Automated Tests
```bash
dotnet test tests/FSBar.Viz.Tests/
```

## Architecture After Change

```
Before:  Bot ──lock──→ stateLock ←──lock── Render
         (both block on each other)

After:   Bot ──atomic swap──→ latestFrame ──read──→ Render
         (zero contention, both run independently)
```
