# Tasks: Map & GameState Preview via SkiaViewer

**Input**: Design documents from `/specs/010-map-gamestate-preview/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, quickstart.md

**Tests**: Included — Constitution III mandates test evidence for behavior-changing code.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Add new module entries to FSBar.Viz.fsproj and create test file stubs

- [x] T001 Add MapData.fsi, MapData.fs, MockSnapshot.fsi, MockSnapshot.fs, PreviewSession.fsi, PreviewSession.fs compile entries to src/FSBar.Viz/FSBar.Viz.fsproj — insert before GameViz.fsi in the compile order
- [x] T002 [P] Add MapDataTests.fs, MockSnapshotTests.fs, PreviewSessionTests.fs compile entries to tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj — insert before ViewerTests.fs
- [x] T003 [P] Add "MapData", "MockSnapshot", "PreviewSession" InlineData entries to SurfaceBaselineTests.fs baseline_matches_fsi_surface test in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

---

## Phase 2: Foundational — MapData Module (Shared by US1, US2, US3, US4)

**Purpose**: Implement MapGrid binary serialization. This module is needed by ALL user stories — US1 directly for save/load, and US2-US4 indirectly for loading test map data.

**⚠️ CRITICAL**: No user story testing can proceed without map data to render

- [x] T004 Create MapData.fsi with public API: `save: string -> MapGrid -> (float32 * float32 * float32 * float32) array -> unit` and `load: string -> MapGrid * (float32 * float32 * float32 * float32) array` in src/FSBar.Viz/MapData.fsi
- [x] T005 Implement MapData.fs with binary format: magic bytes "FSMG", version 1, dimensions header, flattened row-major 2D arrays (HeightMap, SlopeMap, ResourceMap, LosMap, RadarMap), metal spots. Include validation on load (magic bytes, version, dimension checks) in src/FSBar.Viz/MapData.fs
- [x] T006 Create MapDataTests.fs with round-trip test: create a MapGrid with known values (small 4x4 map), save to temp file, load back, assert all fields match original in tests/FSBar.Viz.Tests/MapDataTests.fs
- [x] T007 Add validation test to MapDataTests.fs: attempt to load a file with wrong magic bytes, assert it throws with descriptive error in tests/FSBar.Viz.Tests/MapDataTests.fs
- [x] T008 Add dimension validation test to MapDataTests.fs: attempt to load a truncated file, assert it throws with descriptive error in tests/FSBar.Viz.Tests/MapDataTests.fs
- [x] T009 Build and verify all MapData tests pass — ``dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~MapDataTests"``

**Checkpoint**: MapData save/load works for arbitrary MapGrid + metal spots. Build succeeds.

---

## Phase 3: User Story 1 — Save and Load Map Data for Offline Preview (Priority: P1) 🎯 MVP

**Goal**: Developer can save map data from a live session, load it offline, and render terrain layers in SkiaViewer at 60fps.

**Independent Test**: Save a test map, load it, render in SkiaViewer, verify layers display and frame counting works.

### Implementation for User Story 1

- [x] T010 [US1] Create PreviewSession.fsi with public API: `startWithMap: MapGrid -> IDisposable`, `startWithSnapshot: GameSnapshot -> IDisposable`, `startPlayback: GameSnapshot seq -> int -> IDisposable`, `stop: unit -> unit` in src/FSBar.Viz/PreviewSession.fsi
- [x] T011 [US1] Implement PreviewSession.fs with startWithMap: creates VizConfig (default overlays), ViewState (auto-fit), and launches SkiaViewer with OnRender calling SceneBuilder.drawFrame. Wire keyboard shortcuts for layer switching (1-0) and overlay toggling (U/E/G/M). Wire mouse scroll for zoom and mouse drag for pan. Render at 60fps in src/FSBar.Viz/PreviewSession.fs
- [x] T012 [US1] Write test ``US1 load saved map and render in viewer`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — create a synthetic 8x8 MapGrid with varied height/slope/resource data, save via MapData.save, load via MapData.load, start PreviewSession.startWithMap, count frames for 2 seconds, assert > 0 frames rendered, dispose
- [x] T013 [US1] Write test ``US1 layer switching works during preview`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — start PreviewSession.startWithMap with a test map, run for 2 seconds with frame counting, assert frames rendered (layer switching is verified by the fact that SceneBuilder.drawFrame executes without exceptions)
- [x] T014 [US1] Run US1 tests — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~PreviewSessionTests"``

**Checkpoint**: US1 complete — map data saves, loads, and renders in SkiaViewer at 60fps.

---

## Phase 4: User Story 2 — Mock GameState for Unit and Event Visualization (Priority: P1)

**Goal**: Developer constructs mock GameSnapshots with units, events, economy, metal spots and renders them in SkiaViewer.

**Independent Test**: Build a mock snapshot with known elements, render in viewer, verify frame counting.

### Implementation for User Story 2

- [x] T015 [P] [US2] Create MockSnapshot.fsi with public API: `emptySnapshot: MapGrid -> GameSnapshot`, `withUnits: UnitState list -> GameSnapshot -> GameSnapshot`, `withFriendlyAt: float32 * float32 * float32 -> GameSnapshot -> GameSnapshot`, `withEnemyAt: float32 * float32 * float32 -> GameSnapshot -> GameSnapshot`, `withEvent: EventKind -> float32 * float32 * float32 -> int -> GameSnapshot -> GameSnapshot`, `withEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot`, `withEnergyEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot`, `withMetalSpots: (float32 * float32 * float32 * float32) array -> GameSnapshot -> GameSnapshot`, `withFrame: int -> GameSnapshot -> GameSnapshot` in src/FSBar.Viz/MockSnapshot.fsi
- [x] T016 [P] [US2] Implement MockSnapshot.fs with pipeline builder functions using ``{ snapshot with ... }`` record update syntax. Use a mutable counter (Interlocked.Increment) for auto-generating unique UnitIds in withFriendlyAt/withEnemyAt helpers in src/FSBar.Viz/MockSnapshot.fs
- [x] T017 [US2] Write test ``US2 mock snapshot with units renders in viewer`` in tests/FSBar.Viz.Tests/MockSnapshotTests.fs — create a synthetic 8x8 MapGrid, build a snapshot with 5 friendly units, 3 enemy units, 2 event indicators, economy data, and metal spots via MockSnapshot builders. Start PreviewSession.startWithSnapshot, count frames for 2 seconds, assert > 0 frames rendered
- [x] T018 [US2] Write test ``US2 mock snapshot with 100 units renders at 60fps`` in tests/FSBar.Viz.Tests/MockSnapshotTests.fs — build a snapshot with 100 units at random positions via MockSnapshot.withFriendlyAt in a loop. Start PreviewSession.startWithSnapshot, count frames for 3 seconds, assert frameCount > 120 (at least 40fps)
- [x] T019 [US2] Write test ``US2 empty snapshot renders without crash`` in tests/FSBar.Viz.Tests/MockSnapshotTests.fs — build emptySnapshot with a test map, zero units/events/economy. Start PreviewSession.startWithSnapshot, count frames for 1 second, assert > 0 frames rendered
- [x] T019b [US2] Write test ``US2 out-of-bounds unit positions render without crash`` in tests/FSBar.Viz.Tests/MockSnapshotTests.fs — build a snapshot with units at positions beyond map bounds (negative coords and coords > map size). Start PreviewSession.startWithSnapshot, count frames for 1 second, assert > 0 frames rendered and no exceptions
- [x] T020 [US2] Run US2 tests — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~MockSnapshotTests"``

**Checkpoint**: US2 complete — mock snapshots build and render correctly with all overlay elements.

---

## Phase 5: User Story 3 — Animated GameState Sequence Playback (Priority: P2)

**Goal**: Developer creates a sequence of GameSnapshots and plays them back at a configurable game-fps in SkiaViewer (viewer always renders at 60fps).

**Independent Test**: Create a 60-frame sequence, play back, verify frame counting and smooth playback.

### Implementation for User Story 3

- [x] T021 [US3] Implement startPlayback in PreviewSession.fs: accept a GameSnapshot sequence and game-fps int. Use a Stopwatch to track elapsed time. In the OnRender callback, compute current frame index as (elapsed.TotalSeconds * gameFps) mod sequenceLength, read the snapshot at that index, and call SceneBuilder.drawFrame. Viewer always renders at 60fps in src/FSBar.Viz/PreviewSession.fs
- [x] T022 [US3] Write test ``US3 animated playback renders 60 frame sequence`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — create 60 GameSnapshots with a unit moving across the map (incrementing X position). Start PreviewSession.startPlayback at 30 game-fps. Count viewer frames for 3 seconds, assert > 100 frames rendered (viewer at 60fps)
- [x] T023 [US3] Write test ``US3 playback loops back to start`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — create 10 snapshots, play at 30 game-fps. Run for 2 seconds (should loop multiple times). Assert > 0 frames rendered and no exceptions
- [x] T024 [US3] Run US3 tests — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~PreviewSessionTests"``

**Checkpoint**: US3 complete — animated playback runs smoothly at 60fps viewer rate.

---

## Phase 6: User Story 4 — Interactive Map Navigation in Preview Mode (Priority: P2)

**Goal**: Pan, zoom, layer switching, and overlay toggling work during preview — all existing keyboard/mouse interactions.

**Independent Test**: Start preview, verify interactive controls respond without exceptions.

### Implementation for User Story 4

- [x] T025 [US4] Write test ``US4 pan and zoom work during preview`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — start PreviewSession.startWithMap, count frames for 2 seconds. (Pan/zoom are wired via ViewerConfig callbacks in T011; this test verifies they don't crash during rendering)
- [x] T026 [US4] Write test ``US4 preview session start stop cycle`` in tests/FSBar.Viz.Tests/PreviewSessionTests.fs — start and stop PreviewSession 5 times in a loop, assert no crashes or hangs (lifecycle robustness for preview mode)
- [x] T027 [US4] Run US4 tests — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~PreviewSessionTests"``

**Checkpoint**: US4 complete — all interactive controls work in preview mode.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Update baselines, verify no regressions, ensure flaky-free

- [x] T028 Generate baselines for new modules by running with UPDATE_BASELINES=true — ``UPDATE_BASELINES=true dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj --filter "FullyQualifiedName~SurfaceBaseline"``
- [x] T029 Run full test suite to verify no regressions — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj``
- [x] T030 Run preview tests 3 consecutive times to verify no flaky tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — MapData module blocks all user stories
- **US1 (Phase 3)**: Depends on Foundational — needs MapData + PreviewSession
- **US2 (Phase 4)**: Depends on Foundational + US1 (needs PreviewSession from US1)
  - MockSnapshot module (T015-T016) can start in parallel with US1
- **US3 (Phase 5)**: Depends on US1 + US2 (needs PreviewSession + MockSnapshot)
- **US4 (Phase 6)**: Depends on US1 (needs PreviewSession with interactive controls)
- **Polish (Phase 7)**: Depends on all user stories

### User Story Dependencies

- **US1 (P1)**: Depends on MapData (Phase 2). Creates PreviewSession module.
- **US2 (P1)**: MockSnapshot module independent of US1. Tests depend on PreviewSession (from US1).
- **US3 (P2)**: Depends on PreviewSession (US1) + MockSnapshot (US2) for building animated sequences.
- **US4 (P2)**: Depends on PreviewSession (US1). Tests verify interactive controls.

### Within Each User Story

- `.fsi` signature first, then `.fs` implementation, then tests
- Tests verify via frame callback counting (no pixel comparison)

### Parallel Opportunities

- T002 and T003 can run in parallel with T001 (different files)
- T015-T016 (MockSnapshot module) can run in parallel with T010-T011 (PreviewSession module)
- T006-T008 (MapData tests) can run in parallel (independent test methods)

---

## Parallel Example: Foundational + US1 + US2

```bash
# Phase 2 (sequential — same file):
T004: MapData.fsi
T005: MapData.fs
T006-T008: MapData tests

# Then in parallel:
# US1 track:                    # US2 track (MockSnapshot only):
T010: PreviewSession.fsi        T015: MockSnapshot.fsi
T011: PreviewSession.fs         T016: MockSnapshot.fs
T012-T013: US1 tests            (US2 tests wait for PreviewSession)
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: MapData module (T004-T009)
3. Complete Phase 3: US1 — PreviewSession + map rendering (T010-T014)
4. **STOP and VALIDATE**: Save/load map, render in viewer at 60fps
5. This alone enables offline map preview without a game engine

### Incremental Delivery

1. Setup + MapData → Serialization ready
2. Add US1 → Offline map preview (MVP!)
3. Add US2 → Mock game states with units, events, economy
4. Add US3 → Animated playback
5. Add US4 → Interactive navigation verified
6. Polish → Baselines, regression check

---

## Notes

- All tests require X11 display (DISPLAY=:0) and XDG_RUNTIME_DIR
- Tests use synthetic small maps (8x8) for speed — no real BAR map data needed
- Frame callback counting is the verification method (no pixel comparison)
- PreviewSession tests should use xUnit Collection("Viewer") with DisableParallelization for GLFW safety
- The MapData binary format uses magic bytes "FSMG" and version 1 for forward compatibility
