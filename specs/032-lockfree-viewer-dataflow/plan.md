# Implementation Plan: Lockfree Viewer Dataflow

**Branch**: `032-lockfree-viewer-dataflow` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/032-lockfree-viewer-dataflow/spec.md`

## Summary

Eliminate the `stateLock` bottleneck in GameViz that causes catastrophic
simulation slowdown when the viewer runs alongside the macro bot. Replace
the shared lock with an atomic publish-sample pattern: the bot thread
swaps an immutable state reference in O(1), and the render thread
independently samples and derives display data on its own time budget.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0  
**Primary Dependencies**: FSBar.Client (GameState, MapGrid, UnitDefCache), FSBar.Viz (GameViz, SceneBuilder, VizTypes), SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6  
**Storage**: N/A (in-memory only)  
**Testing**: xUnit 2.9.x + visual regression via trainer runs  
**Target Platform**: Linux (x86_64)  
**Project Type**: Library (FSBar.Viz) + bot scripts  
**Performance Goals**: Bot thread: 0 blocking on viewer. Render: 60 fps sustained. State publish: <100us.  
**Constraints**: No new NuGet dependencies. No public API surface changes.  
**Scale/Scope**: ~200-500 units typical, <1000 max. Single viewer instance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-First Delivery (§I) | PASS | Spec exists with user stories and acceptance criteria |
| Tier 1 classification | N/A | No public API surface changes — internal refactoring only |
| .fsi Signature Contracts (§II) | PASS | No .fsi changes needed; `GameViz.fsi` signatures unchanged |
| Surface-Area Baselines (§II) | PASS | Baseline update needed (pre-existing debt, not from this feature) |
| Test Evidence (§III) | PASS | Visual regression via trainer runs + existing xUnit tests |
| Observability (§IV) | PASS | Perf counter overlay retained; stdout logging retained |
| Scripting Accessibility (§V) | N/A | No new public API; existing FSI scripts unaffected |
| F# Exclusive Stack | PASS | F# only |
| No New Dependencies | PASS | Uses only System.Threading.Interlocked (BCL) |
| Packable Library | PASS | FSBar.Viz remains packable |

**Post-design re-check**: All gates still pass. No new types added to
.fsi. Internal `RawFrame` and `RenderState` types are module-private.

## Project Structure

### Documentation (this feature)

```text
specs/032-lockfree-viewer-dataflow/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: architecture research
├── data-model.md        # Phase 1: data model
├── quickstart.md        # Phase 1: verification guide
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── GameViz.fs           # Core refactoring target
├── GameViz.fsi          # Unchanged (internal refactoring)
├── SceneBuilder.fs      # Unchanged
├── VizTypes.fs          # Unchanged (GameSnapshot already suitable)
└── VizTypes.fsi         # Unchanged

bots/trainer/
├── bot_macro.fsx        # Remove/relax frame-skip throttle
└── helpers/
    └── viewer.fsx       # May simplify (no throttle needed)

tests/FSBar.Viz.Tests/
└── Baselines/
    └── GameViz.baseline # Update to match current .fsi (pre-existing)
```

**Structure Decision**: No new files or directories. All changes are
modifications to existing files within existing projects.

## Implementation Phases

### Phase 1: Introduce Atomic State Publication

**Goal**: Make `onFrameWithState` lock-free by publishing a raw frame
reference via `Interlocked.Exchange`.

**Steps**:
1. Define internal `RawFrame` record in GameViz.fs (GameState, MapGrid,
   MyTeamId, MetalSpots, monotonic counter)
2. Add `mutable latestFrame: RawFrame option` field
3. Rewrite `onFrameWithState` to: build `RawFrame`, atomically swap it
   into `latestFrame`. Remove lock acquisition. Remove all derived-data
   computation (unit map rebuild, event processing, indicator management,
   snapshot construction).
4. Bot thread is now fully decoupled — FR-001, FR-003, FR-005 satisfied.

### Phase 2: Move Derived State to Render Thread

**Goal**: The render thread independently samples and processes the
latest raw frame, building all derived data on its own time budget.

**Steps**:
1. Add render-local state fields: `renderUnits`, `renderPrevUnits`,
   `renderIndicators`, `renderSnapshot`, `lastProcessedCounter`
2. In the FrameTick handler, read `latestFrame` via `Volatile.Read`
3. If `latestFrame.FrameCounter > lastProcessedCounter`:
   - Rebuild `renderUnits` map from `GameState.Units` + `GameState.Enemies`
   - Compute new `EventIndicators` by diffing against `renderPrevUnits`
   - Build `DisplayUnits` via DefProps cache lookup
   - Construct new `GameSnapshot`
   - Update `renderPrevUnits`, reset interpolation stopwatch
4. On every tick (regardless of new frame): compute `interpT`, lerp
   positions, call `SceneBuilder.buildScene`, emit scene
5. FR-002, FR-004, FR-006 satisfied.

### Phase 3: Remove stateLock from Hot Path

**Goal**: Eliminate the lock from frame processing and scene emission.

**Steps**:
1. Remove `lock stateLock` from FrameTick handler — render-local state
   is now exclusively owned by the render thread
2. Remove `lock stateLock` from `onFrameWithState` — already done in
   Phase 1
3. Introduce a lightweight `configLock` monitor lock for infrequent
   config/view state mutations from public API calls (`setConfig`,
   `toggleOverlay`, `pan`, `zoom`, etc.). A simple monitor is adequate
   for <10 ops/sec (research.md R4).
4. Keep `stateLock` (renamed to `lifecycleLock`) only for `start`/`stop`/
   `attachToClient`/`attachWithState` lifecycle operations
5. FR-007 satisfied.

### Phase 4: Clean Up and Validate

**Goal**: Remove workarounds, update baselines, validate.

**Steps**:
1. In `bot_macro.fsx`: remove or relax the `vizFrameSkip=30` throttle
   (FR-008). The bot can now call `viewerOnFrame` on every frame without
   performance impact.
2. Update `GameViz.baseline` to match current `.fsi` (pre-existing debt)
3. Run `dotnet test tests/FSBar.Viz.Tests/` — all tests must pass
4. Run trainer with `--full-viz` and measure game completion time
   against no-viewer baseline (SC-001)
5. Visually verify: smooth interpolation (SC-002), responsive hotkeys,
   perf counter showing correct rates, all overlays functional (SC-005)
6. FR-009, FR-010 satisfied.

## Complexity Tracking

No constitution violations. No complexity justification needed.
