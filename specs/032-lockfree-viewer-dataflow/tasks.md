# Tasks: Lockfree Viewer Dataflow

**Input**: Design documents from `/specs/032-lockfree-viewer-dataflow/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Visual regression via trainer runs + existing xUnit tests + one new threading smoke test (T018).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new project structure needed — this feature modifies existing files only. Setup confirms the current state is green.

- [X] T001 Run `dotnet build src/FSBar.Viz/` and confirm clean build
- [X] T002 Run `dotnet test tests/FSBar.Viz.Tests/` and confirm all tests pass

**Checkpoint**: Existing code builds and tests pass — safe to begin refactoring.

---

## Phase 2: Foundational (Atomic State Publication)

**Purpose**: Introduce the `RawFrame` record and the atomic publish mechanism in GameViz.fs. This MUST be complete before any user story work, since all stories depend on the new dataflow.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 Define internal `RawFrame` record type (GameState, MapGrid, MyTeamId, MetalSpots, FrameCounter) at the top of the GameViz module in `src/FSBar.Viz/GameViz.fs`
- [X] T004 Define internal `RenderState` record type (CurrentSnapshot, PreviousSnapshot, Units, PrevUnits, Indicators, DefPropsCache, InterpStopwatch, LastFrameCounter) in `src/FSBar.Viz/GameViz.fs`
- [X] T005 Add `mutable latestFrame: RawFrame option` field to the GameViz class and a monotonic frame counter in `src/FSBar.Viz/GameViz.fs`
- [X] T006 Add render-local `RenderState` fields to the GameViz class in `src/FSBar.Viz/GameViz.fs`
- [X] T007 Rewrite `onFrameWithState` to build a `RawFrame` and atomically swap it via `Interlocked.Exchange` — remove lock acquisition and all derived-data computation from this method. Add a Stopwatch guard that logs a warning if publish exceeds 100μs (SC-004 tripwire, debug-only) in `src/FSBar.Viz/GameViz.fs`

**Checkpoint**: Bot thread now publishes state lock-free. Viewer will not render correctly yet (render thread not updated). Build must succeed.

---

## Phase 3: User Story 1 - Viewer Runs Without Slowing the Bot (Priority: P1) MVP

**Goal**: The bot completes games at full simulation speed (~140+ game-fps at 5x) regardless of whether the viewer is active. Zero blocking on the bot thread from viewer operations.

**Independent Test**: Run `cd bots/trainer && ./run.sh --speed 2 --full-viz` and compare game completion time against `--no-viz` baseline. Must be within 10%.

### Implementation for User Story 1

- [X] T008 [US1] In the FrameTick handler, read `latestFrame` via `Volatile.Read` and detect new frames by comparing `FrameCounter` against `lastProcessedCounter` in `src/FSBar.Viz/GameViz.fs`
- [X] T009 [US1] When a new frame is detected, rebuild `units: Map<int, UnitState>` from `GameState.Units` + `GameState.Enemies` on the render thread in `src/FSBar.Viz/GameViz.fs`
- [X] T010 [US1] Compute `EventIndicator` list by diffing current units against `renderPrevUnits` on the render thread in `src/FSBar.Viz/GameViz.fs`
- [X] T011 [US1] Build `DisplayUnits` via DefProps cache lookup on the render thread in `src/FSBar.Viz/GameViz.fs`
- [X] T012 [US1] Construct `GameSnapshot` from the derived data and update `renderSnapshot` / `renderPrevSnapshot` on the render thread in `src/FSBar.Viz/GameViz.fs`
- [X] T013 [US1] Remove `lock stateLock` from the FrameTick handler — render-local state is now exclusively owned by the render thread in `src/FSBar.Viz/GameViz.fs`
- [X] T014 [US1] Introduce a `configLock` monitor lock for infrequent config/view state mutations (`setConfig`, `toggleOverlay`, `pan`, `zoom`) — adequate for <10 ops/sec per research.md R4 — in `src/FSBar.Viz/GameViz.fs`
- [X] T015 [US1] Rename remaining `stateLock` to `lifecycleLock` and keep it only for `start`/`stop`/`attachToClient`/`attachWithState` lifecycle operations in `src/FSBar.Viz/GameViz.fs`
- [X] T016 [US1] Remove or relax the `vizFrameSkip=30` throttle in `bots/trainer/bot_macro.fsx` since the bot thread no longer blocks on viewer operations
- [X] T017 [US1] Run `dotnet build src/FSBar.Viz/` and `dotnet test tests/FSBar.Viz.Tests/` to confirm build and tests pass
- [X] T018 [US1] Add xUnit threading smoke test: spawn two threads — one calling `onFrameWithState` in a loop, one reading `latestFrame` via the render path — assert no deadlock within 2 seconds and no exceptions, in `tests/FSBar.Viz.Tests/GameVizThreadingTests.fs`

**Checkpoint**: Bot thread is fully decoupled from the viewer. Game completion time with viewer should match no-viewer baseline within 10%.

---

## Phase 4: User Story 2 - Smooth Visual Playback (Priority: P2)

**Goal**: Unit movement appears smooth and continuous at 60 fps, with position interpolation working correctly in the new render-thread-only architecture.

**Independent Test**: Run either bot with the viewer and visually confirm smooth unit movement. Perf counter should show ~60 render fps.

### Implementation for User Story 2

- [X] T019 [US2] Move position interpolation logic to the render thread: on every FrameTick (regardless of new frame), compute `interpT` from the interpolation stopwatch and lerp unit positions between `renderPrevSnapshot` and `renderSnapshot` in `src/FSBar.Viz/GameViz.fs`
- [X] T020 [US2] Reset interpolation stopwatch when a new snapshot arrives; ensure `interpT` clamps to [0, 1] and adapts to variable inter-snapshot intervals in `src/FSBar.Viz/GameViz.fs`
- [X] T021 [US2] Call `SceneBuilder.buildScene` with interpolated positions on every FrameTick and emit the scene to the viewer in `src/FSBar.Viz/GameViz.fs`
- [X] T022 [US2] Handle the edge case where no state has been published yet — render an empty/placeholder scene gracefully in `src/FSBar.Viz/GameViz.fs`
- [X] T023 [US2] Handle the edge case where state updates stop (engine stall/disconnect) — continue rendering last known state in `src/FSBar.Viz/GameViz.fs`

**Checkpoint**: Viewer shows smooth 60 fps unit movement with correct interpolation.

---

## Phase 5: User Story 3 - Input Responsiveness (Priority: P2)

**Goal**: Pan, scroll, zoom, and hotkey toggles respond immediately without waiting for the bot thread.

**Independent Test**: During a `--full-viz` macro bot run, rapidly pan and zoom. Inputs should respond within one render frame (~16ms).

### Implementation for User Story 3

- [X] T024 [US3] Ensure scroll/pan/drag input handlers in the FrameTick path do not acquire any bot-thread lock — they should update `ViewState` via `configLock` only in `src/FSBar.Viz/GameViz.fs`
- [X] T025 [US3] Ensure hotkey toggle handlers (W/L/C/N) update `VizConfig` via `configLock` without touching the bot-thread state path in `src/FSBar.Viz/GameViz.fs`
- [X] T026 [US3] Verify perf counter overlay, economy HUD, and all 8 overlay layers render correctly with the new threading model in `src/FSBar.Viz/GameViz.fs`

**Checkpoint**: All viewer interactions are responsive during high-speed bot runs.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Clean up, baseline update, and final validation across all stories.

- [X] T027 Update `tests/FSBar.Viz.Tests/Baselines/GameViz.baseline` to match current `GameViz.fsi` (pre-existing debt)
- [X] T028 Simplify `bots/trainer/helpers/viewer.fsx` if throttle/workaround code is no longer needed
- [X] T029 Run `dotnet test tests/FSBar.Viz.Tests/` — all tests must pass
- [X] T030 Run trainer with `--full-viz` and verify: smooth interpolation, responsive hotkeys, perf counter showing correct rates, all overlays functional (quickstart.md validation)
- [X] T031 Verify bot game completion time with viewer is within 10% of no-viewer baseline (SC-001)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — confirm green baseline
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — core decoupling
- **US2 (Phase 4)**: Depends on US1 (render thread must own derived state before interpolation can move there)
- **US3 (Phase 5)**: Depends on US1 (input handlers must use `configLock` not `stateLock`)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Foundational (Phase 2) — the core architectural change
- **User Story 2 (P2)**: Depends on US1 — interpolation moves to render thread after derived state is there
- **User Story 3 (P2)**: Depends on US1 — input locking changes require `configLock` from US1

### Within Each User Story

- Core data flow changes before scene building
- Lock removal after new mechanism is in place
- Bot script changes after library changes

### Parallel Opportunities

- T003 and T004 can run in parallel (independent record type definitions)
- T009, T010, T011 can be developed together (all render-thread derived-state steps, but sequentially dependent in the same function)
- US2 and US3 can run in parallel once US1 is complete (different concerns: interpolation vs. input handling)
- T027 and T028 can run in parallel (different files)

---

## Parallel Example: After US1 Completes

```bash
# US2 and US3 can proceed in parallel:
Task: "Move position interpolation to render thread in src/FSBar.Viz/GameViz.fs" (US2)
Task: "Ensure input handlers use configLock in src/FSBar.Viz/GameViz.fs" (US3)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (verify green baseline)
2. Complete Phase 2: Foundational (RawFrame + atomic swap)
3. Complete Phase 3: User Story 1 (full bot decoupling)
4. **STOP and VALIDATE**: Run trainer with `--full-viz`, measure game completion time
5. If within 10% of baseline → core problem solved

### Incremental Delivery

1. Setup + Foundational → Atomic state publication working
2. Add US1 → Bot fully decoupled → Measure performance (MVP!)
3. Add US2 → Smooth interpolation → Visual regression check
4. Add US3 → Responsive inputs → Full interactive check
5. Polish → Baseline update, cleanup, final validation

---

## Notes

- One new file: `tests/FSBar.Viz.Tests/GameVizThreadingTests.fs` (threading smoke test). All other changes are modifications to existing files
- The only shared mutable state after refactoring is `latestFrame` (atomic) and config/view state (`configLock`)
- `GameViz.fsi` signatures are unchanged — this is purely internal refactoring
- `SceneBuilder` mutable state is render-thread-local and unaffected
- The `GameViz.baseline` update in T027 is pre-existing debt, not introduced by this feature
