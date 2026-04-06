# Tasks: Live 60fps Map Visualization

**Input**: Design documents from `/specs/011-live-map-viz/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Required by constitution (test evidence is mandatory for behavior-changing code).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Project structure and .fsi contract for the new LiveSession module

- [x] T001 Create LiveSession.fsi signature file defining public API contract (LiveSessionHandle type with IDisposable/FrameCount/IsRunning, start and startWithClient functions) in src/FSBar.Viz/LiveSession.fsi
- [x] T002 Add LiveSession.fsi and LiveSession.fs to compile order in src/FSBar.Viz/FSBar.Viz.fsproj (after GameViz.fs)
- [x] T003 Add LiveSessionTests.fs and LiveSessionIntegrationTests.fs to compile order in tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj

**Checkpoint**: Project compiles (LiveSession.fs can be a stub with `failwith "not implemented"`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core LiveSession implementation that all user stories depend on

**CRITICAL**: No user story verification can begin until this phase is complete

- [x] T004 Implement LiveSession.fs with start function: create BarClient, call client.Start(), call GameViz.start(), call GameViz.attachToClient(), spawn background stepLoop thread, return LiveSessionHandle in src/FSBar.Viz/LiveSession.fs
- [x] T005 Implement stepLoop in LiveSession.fs: background thread calling client.Step() then GameViz.onFrame(frame) in a while loop guarded by volatile running flag, with try/catch that calls GameViz.setDisconnected() on error
- [x] T006 Implement startWithClient in LiveSession.fs: attach to existing connected BarClient without managing its lifecycle (for testing and REPL use)
- [x] T007 Implement IDisposable on LiveSessionHandle: set running=false, join step thread, call GameViz.stop(), call client.Stop() (only if LiveSession owns the client) in src/FSBar.Viz/LiveSession.fs
- [x] T008 Add structured logging for session state transitions (Starting, Connected, Running, Stopped, Error) in src/FSBar.Viz/LiveSession.fs
- [x] T009 Build and pack FSBar.Viz: run dotnet build src/FSBar.Viz/ and dotnet pack src/FSBar.Viz/ -o ~/.local/share/nuget-local/

**Checkpoint**: LiveSession compiles and can be instantiated. Foundation ready for story verification.

---

## Phase 3: User Story 1 - Live Heightmap Rendering During Game (Priority: P1) MVP

**Goal**: Launch a headless engine and see a real-time 60fps heightmap visualization window connected via the full proxy pipeline.

**Independent Test**: Launch engine with LiveSession.start(), observe window with color-coded heightmap rendering at 60fps. Run 100+ frames and verify MapGrid is loaded with valid heightmap data.

### Tests for User Story 1

- [x] T010 [US1] Create unit test in tests/FSBar.Viz.Tests/LiveSessionTests.fs: verify startWithClient creates a running session with IsRunning=true and FrameCount incrementing, using a real BarClient against a live engine
- [x] T011 [US1] Create unit test in tests/FSBar.Viz.Tests/LiveSessionTests.fs: verify Dispose stops the session cleanly (IsRunning=false, step thread joined)
- [x] T012 [US1] Create integration test in tests/FSBar.Viz.Tests/LiveSessionIntegrationTests.fs: start full LiveSession with engine, run 100+ frames, assert MapGrid heightmap is non-empty and dimensionally correct (width*height > 0), assert FrameCount >= 100

### Implementation for User Story 1

- [x] T013 [US1] Verify end-to-end: run LiveSession integration test (T012) against live headless engine, confirm heightmap renders in viz window at ~60fps, fix any issues found

**Checkpoint**: User Story 1 complete — live heightmap visualization works end-to-end with headless engine

---

## Phase 4: User Story 2 - Live Unit and Game State Overlay (Priority: P2)

**Goal**: Unit positions from the live game appear on the map overlay, updating each frame as units move. Teams are visually distinguished by color.

**Independent Test**: Run a game with units, verify unit markers appear at correct positions on the visualization, and that friendly (blue) and enemy (red) markers are distinguishable.

### Tests for User Story 2

- [x] T014 [US2] Create integration test in tests/FSBar.Viz.Tests/LiveSessionIntegrationTests.fs: run 200+ frames with a live engine, verify GameSnapshot.Units is non-empty (units exist on map), verify at least one unit has a valid position (X > 0 or Z > 0)

### Implementation for User Story 2

- [x] T015 [US2] Verify unit overlay renders during live session: run integration test (T014), observe unit markers on the viz window, confirm blue/red team distinction works. Fix any issues with unit position updates in the onFrame pipeline if needed.

**Checkpoint**: User Stories 1 AND 2 work — live map with unit overlay

---

## Phase 5: User Story 3 - Dynamic Map Layer Toggling (Priority: P3)

**Goal**: Developer can switch between map layers (heightmap, slope, LOS, radar, metal, terrain, passability) via keyboard during a live game session.

**Independent Test**: During a live session, press keys 1-5 and verify each layer renders with correct data. Press U/E/G/M to toggle overlays.

### Tests for User Story 3

- [x] T016 [US3] Create integration test in tests/FSBar.Viz.Tests/LiveSessionIntegrationTests.fs: run live session for 100+ frames, programmatically call GameViz.setBaseLayer for each LayerKind, verify no exceptions thrown and each layer's data is available in the MapGrid (LosMap, RadarMap, SlopeMap, ResourceMap all non-empty)

### Implementation for User Story 3

- [x] T017 [US3] Verify all layer types render during live session: run integration test (T016), manually test keyboard layer switching (keys 1-0) and overlay toggling (U/E/G/M) during a live game. Fix any issues with dynamic layers (LOS/Radar refresh) if needed.

**Checkpoint**: All 3 user stories work — full live visualization with layers and overlays

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Constitution compliance, scripting accessibility, surface baselines

- [x] T018 [P] Update surface-area baseline for LiveSession module in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs: add baseline test verifying LiveSession public API (start, startWithClient, LiveSessionHandle type members)
- [x] T019 [P] Create FSI example script scripts/examples/NN-live-viz.fsx: load FSBar.Client and FSBar.Viz DLLs, configure EngineConfig, call LiveSession.start(), demonstrate live visualization with comments explaining controls
- [x] T020 Validate quickstart.md: follow the quickstart instructions end-to-end (build, pack, run FSI script), verify they work without modification
- [x] T021 Run full test suite: dotnet test tests/FSBar.Viz.Tests/ to verify no regressions in existing tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — core pipeline
- **US2 (Phase 4)**: Depends on Foundational — can run parallel with US1 but benefits from US1 proof
- **US3 (Phase 5)**: Depends on Foundational — can run parallel with US1/US2
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends only on Foundational — no cross-story dependencies
- **User Story 2 (P2)**: Depends only on Foundational — unit overlay already in GameViz, just needs live proof
- **User Story 3 (P3)**: Depends only on Foundational — layer switching already in GameViz, just needs live proof

### Within Each User Story

- Tests written first to define expectations
- Implementation/verification follows
- Story complete when checkpoint passes

### Parallel Opportunities

- T018 and T019 can run in parallel (different files)
- US2 and US3 tests/verification can run in parallel after Foundational
- All Phase 1 tasks are sequential (dependency chain: .fsi → .fsproj)

---

## Parallel Example: Phase 6 Polish

```text
# These can run simultaneously:
Task T018: "Update surface-area baseline in SurfaceBaselineTests.fs"
Task T019: "Create FSI example script NN-live-viz.fsx"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (.fsi contract, project files)
2. Complete Phase 2: Foundational (LiveSession.fs implementation)
3. Complete Phase 3: User Story 1 (live heightmap rendering)
4. **STOP and VALIDATE**: Run integration test, observe 60fps heightmap
5. This is a deliverable MVP — live map visualization works

### Incremental Delivery

1. Setup + Foundational → LiveSession module compiles
2. Add US1 → Live heightmap at 60fps (MVP!)
3. Add US2 → Unit overlay on live map
4. Add US3 → Layer switching during live game
5. Polish → Baselines, FSI script, quickstart validation

### Key Insight

Most rendering functionality already exists in GameViz/SceneBuilder/LayerRenderer. This feature is primarily about:
1. **Orchestration** (LiveSession module — Phases 1-2)
2. **End-to-end proof** (integration tests — Phases 3-5)
3. **Accessibility** (FSI script, baselines — Phase 6)

---

## Notes

- All rendering, layer switching, unit overlays, and keyboard controls are already implemented in GameViz
- LiveSession is the only new module — it orchestrates engine→client→viz
- Integration tests require a running headless engine binary
- The CLAUDE.md documents the engine path: `/home/developer/.local/state/engine-2025.06.21/spring-headless`
- SkiaViewer handles the 60fps render loop; LiveSession only handles the engine step loop
- GameViz.stateLock provides thread safety between the step thread and render thread
