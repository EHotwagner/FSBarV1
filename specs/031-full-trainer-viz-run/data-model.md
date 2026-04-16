# Data Model: Full Trainer Game with Complete Visualization

**Feature**: 031-full-trainer-viz-run
**Date**: 2026-04-16

## Overview

No new data entities are introduced. This feature modifies the configuration passed to existing types and adds one field to the run metadata record.

## Modified Entities

### meta.json (run metadata)

New field added when full-viz mode is active:

| Field | Type | Description |
|-------|------|-------------|
| `full_viz` | boolean | `true` when `--full-viz` was used, absent otherwise |
| `initial_overlays` | string[] | List of overlay names enabled at viewer start (only present when `full_viz` is true) |

Example addition to existing meta.json:
```json
{
  "viewer": true,
  "full_viz": true,
  "initial_overlays": ["Units", "Events", "MetalSpots", "EconomyHud", "WeaponRanges", "SightRanges", "CommandQueue", "FullNames"],
  "speed_level": 2,
  "game_speed": 5
}
```

### Environment Variables (run.sh → bot.fsx)

| Variable | Value | Description |
|----------|-------|-------------|
| `BOT_FULL_VIZ` | `"1"` | Set when `--full-viz` is active; unset otherwise |

This is consumed by `bot.fsx` (to override `maxFrames`) and `viewer.fsx` (to expand overlays).

## Unchanged Entities

- `VizConfig` — no structural changes; different values passed at runtime
- `OverlayKind` — no new cases
- `LayerKind` — no new cases
- `TrainerMatchResult` — no schema changes; `Outcome` values unchanged
- `result.json` — no schema changes
