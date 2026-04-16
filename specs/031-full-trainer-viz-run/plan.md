# Implementation Plan: Full Trainer Game with Complete Visualization

**Branch**: `031-full-trainer-viz-run` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/031-full-trainer-viz-run/spec.md`

## Summary

Add a `--full-viz` CLI flag to `bots/trainer/run.sh` that launches a trainer game with all visualization overlays enabled from the first frame, defaults to speed level 2 (5x), and removes the max_frames cap so the game runs to natural completion. This is a script-only change — no compiled library code or public API surfaces are modified.

## Technical Context

**Language/Version**: Bash (run.sh) + F# 9 on .NET 10.0 (bot scripts)
**Primary Dependencies**: FSBar.Client (BarClient, GameState), FSBar.Viz (GameViz, VizConfig, OverlayKind, LayerKind, VizDefaults)
**Storage**: Filesystem only — run artifacts under `bots/runs/` (gitignored)
**Testing**: Manual verification (script + viz window); no xUnit changes needed
**Target Platform**: Linux (Arch-based dev container with DISPLAY=:0)
**Project Type**: CLI (trainer runner scripts)
**Performance Goals**: Viewer at 60fps with all 8 overlays active at speed level 2
**Constraints**: Must not break existing `--viewer` behavior
**Scale/Scope**: 3 files changed (run.sh, viewer.fsx, bot.fsx or tactics.fsx)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| §I Spec-First Delivery | PASS | Spec at `specs/031-full-trainer-viz-run/spec.md` |
| §II Compiler-Enforced Contracts (.fsi) | N/A | No public F# module API changes — only `.fsx` scripts and bash |
| §III Test Evidence Is Mandatory | PASS | Manual verification of viewer overlays + game completion; no behavioral changes to compiled libraries |
| §IV Observability / Safe Failure | PASS | Existing diagnostics (meta.json, engine logs, result.json) are preserved; FR-007 adds full-viz flag to meta.json |
| §V Scripting Accessibility | N/A | No new public API; this feature *is* a script enhancement |
| F# exclusive stack | PASS | Bash for CLI, F# for bot scripts — consistent with existing pattern (run.sh + .fsx) |
| No new dependencies | PASS | Zero new dependencies |
| Surface-area baselines | N/A | No compiled public modules are changed |

**Post-design re-check**: No changes to gate status. All script-only modifications.

## Project Structure

### Documentation (this feature)

```text
specs/031-full-trainer-viz-run/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (minimal — no new entities)
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (files changed)

```text
bots/trainer/
├── run.sh                      # Add --full-viz flag parsing, env var export
├── bot.fsx                     # Read BOT_FULL_VIZ, pass unlimited maxFrames
└── helpers/
    └── viewer.fsx              # Read BOT_FULL_VIZ, expand ActiveOverlays + BaseLayer
```

**Structure Decision**: No new files or directories. Three existing files modified in-place: `run.sh` (CLI parsing + env vars), `bot.fsx` (max_frames override), `viewer.fsx` (overlay set expansion).

## Implementation Details

### R1: run.sh changes

Add `--full-viz` option parsing alongside existing `--viewer`:
- New variable `opt_full_viz` (initially empty)
- `--full-viz)` case sets `opt_full_viz=1` and `opt_viewer=1` (full-viz implies viewer)
- Speed default logic: if `opt_full_viz` and no explicit `--speed`, set speed level 2 (game speed 5) instead of viewer's default level 3 (game speed 10)
- Export `BOT_FULL_VIZ=1` when active
- Record `"full_viz": true` in `meta.json`

### R2: viewer.fsx changes

When `BOT_FULL_VIZ = "1"`:
- Set `ActiveOverlays` to all 8 gameplay overlays:
  `Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, FullNames`
- Set `BaseLayer = LayerKind.BaseTerrain` (instead of inheriting `HeightMap` from `VizDefaults.defaultConfig`)
- Existing `--viewer` (without `--full-viz`) behavior unchanged: 4 overlays + HeightMap base

### R3: bot.fsx / tactics.fsx changes

When `BOT_FULL_VIZ = "1"`:
- Set `maxFrames` to `Int32.MaxValue` (effectively unlimited)
- The existing `trainerLoopRun` frame-limit check (`lastFrameNumber >= maxFrames` at tactics.fsx:242) will never trigger, so the game runs until a natural termination condition (commander death, engine shutdown, or viewer window close)
- No changes to `trainerLoopRun` itself — only the value passed in changes

### R4: Graceful exit on viewer window close

The existing `GameViz.stop()` + `shutdownSeen` mechanism in `trainerLoopRun` already handles this — when the viewer window is closed, the engine shutdown signal propagates and `stepping <- false` fires at tactics.fsx:240. No additional work needed.
