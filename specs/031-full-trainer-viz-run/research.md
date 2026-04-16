# Research: Full Trainer Game with Complete Visualization

**Feature**: 031-full-trainer-viz-run
**Date**: 2026-04-16

## R1: How to add a CLI flag to run.sh

**Decision**: Add `--full-viz` as a new flag that implies `--viewer`.

**Rationale**: The existing `--viewer` flag pattern (lines 48-50 of run.sh) establishes the convention: parse in the getopts loop, set `opt_full_viz=1`, and export as `BOT_FULL_VIZ`. Since full-viz is a superset of viewer behavior, `--full-viz` sets `opt_viewer=1` internally so all viewer-dependent logic (viz DLL build, tee to stdout) activates automatically.

**Alternatives considered**:
- `--viewer --all-overlays` (two flags): Rejected — spec FR-001 calls for a single option, and splitting would also require a separate mechanism for the frame-limit removal.
- `--viewer=full` (value-style flag): Rejected — inconsistent with existing flag-only pattern (`--viewer`, `--speed`).

## R2: Overlay expansion in viewer.fsx

**Decision**: When `BOT_FULL_VIZ = "1"`, replace the 4-overlay default set with all 8 `OverlayKind` cases (excluding `Grid` which is structural, not gameplay information) and set `BaseLayer = LayerKind.BaseTerrain`.

**Rationale**: The spec (FR-002) enumerates exactly 8 overlays: Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, FullNames. The `Grid` overlay is omitted because the spec's Assumptions section defines "full viz" as gameplay-information overlays, and Grid is a structural debug aid. `BaseTerrain` is chosen over `HeightMap` (the VizDefaults default) because FR-004 calls for the base terrain layer, and `BaseTerrain` renders the textured map surface rather than a grayscale elevation view.

**Alternatives considered**:
- Include `Grid` overlay: Rejected — adds visual noise without gameplay value.
- Keep `HeightMap` as base: Rejected — spec FR-004 explicitly requires "base terrain layer."

## R3: Removing the frame cap

**Decision**: When `BOT_FULL_VIZ = "1"`, set `maxFrames = Int32.MaxValue` in `bot.fsx` before passing to `trainerLoopRun`.

**Rationale**: The frame-limit check in `trainerLoopRun` (tactics.fsx:242) is `lastFrameNumber >= maxFrames`. Setting `maxFrames` to `Int32.MaxValue` (2,147,483,647) effectively disables the limit without modifying the loop logic. At 30 game-fps this allows ~828 days of game time, which is effectively unlimited. The developer retains control via: (a) closing the viewer window triggers engine shutdown, (b) Ctrl+C in the terminal.

**Alternatives considered**:
- Add a `noFrameLimit: bool` parameter to `trainerLoopRun`: Rejected — modifies a shared helper used by all bot scripts; the `Int32.MaxValue` sentinel achieves the same result with zero code changes to `trainerLoopRun`.
- Use `0` or `-1` as sentinel for "no limit": Rejected — requires changing the comparison logic in `trainerLoopRun`, which is shared infrastructure.

## R4: Speed default logic

**Decision**: `--full-viz` without explicit `--speed` defaults to speed level 2 (game speed 5x). The three-way priority: (1) explicit `--speed` always wins, (2) `--full-viz` without `--speed` → level 2, (3) `--viewer` without `--speed` → level 3, (4) neither → max.

**Rationale**: Spec FR-003 requires speed level 2 as the full-viz default. This slots into the existing speed-resolution cascade in run.sh (lines 164-174) as a new middle tier between `--speed` override and `--viewer` fallback.

**Alternatives considered**:
- Always force speed 2 in full-viz: Rejected — spec edge case says `--speed` must take precedence.

## R5: Viewer window close → graceful exit

**Decision**: No new code needed. Existing mechanism is sufficient.

**Rationale**: When the viewer window closes, `GameViz.stop()` fires. The BarClient eventually sees the engine shutdown signal, setting `shutdownSeen = true` in `trainerLoopRun` (tactics.fsx:240), which breaks the frame loop. The result is recorded with `Outcome = "shutdown"`. This path was verified during feature 029-trainer-viewer-options.

**Alternatives considered**: None — existing behavior matches spec requirement (US2 AS3).
