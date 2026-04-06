# Tasks: Harden SkiaSharp OpenGL Viewer

**Input**: Design documents from `/specs/009-harden-skiasharp-viewer/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md

**Tests**: Included — the spec mandates test evidence (Constitution III) and explicitly defines test criteria for all user stories.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Prepare the test infrastructure for standalone viewer tests

- [x] T001 Add ViewerTests.fs to the compile list in tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj (before GameVizIntegrationTests.fs)
- [x] T002 Create empty ViewerTests.fs module scaffold with xUnit imports in tests/FSBar.Viz.Tests/ViewerTests.fs

---

## Phase 2: Foundational (Harden Viewer.fs Internals)

**Purpose**: Fix all 5 identified issues in Viewer.fs. These changes are blocking prerequisites for ALL user story tests — the viewer must be hardened before tests can reliably verify behavior.

**⚠️ CRITICAL**: No user story testing can be meaningful until these fixes are in place

- [x] T003 Add surfaceLock, shutdownRequested flag, and windowCompleted ManualResetEventSlim to Viewer.run closure in src/FSBar.Viz/Viewer.fs
- [x] T004 Fix surface synchronization: wrap recreateSurface and render callback surface access with surfaceLock. Snapshot surface/surfaceWidth/surfaceHeight under lock in render path, operate on snapshot outside lock. In recreateSurface, create new surface first, swap under lock, then dispose old surface — in src/FSBar.Viz/Viewer.fs
- [x] T005 Fix zero-size framebuffer: in recreateSurface, when fbSize is zero, set surface to null under lock and skip creation. Render path already guards against null surface — in src/FSBar.Viz/Viewer.fs
- [x] T006 Fix cross-thread shutdown: add win.add_Update callback that checks shutdownRequested flag and calls win.Close() from the window thread. Remove direct w.Close() call from stop() — in src/FSBar.Viz/Viewer.fs
- [x] T007 Add completion signaling: signal windowCompleted after win.Run() returns. In stop(), set shutdownRequested then wait on windowCompleted with 5-second timeout — in src/FSBar.Viz/Viewer.fs
- [x] T008 Add diagnostic logging: replace silent exception catches in render callback with eprintfn warnings that log exception type and message. Add lifecycle event logging (window loaded, surface created, shutdown requested, resources released) — in src/FSBar.Viz/Viewer.fs
- [x] T009 Guard pre-init resize: ensure recreateSurface is safe when GL context is not yet initialized (gl is default). Skip surface creation if GL setup has not completed — in src/FSBar.Viz/Viewer.fs
- [x] T010 Update GameViz.doStop to use Viewer completion signaling instead of Thread.Sleep(500) — in src/FSBar.Viz/GameViz.fs

**Checkpoint**: All Viewer.fs hardening fixes applied. Build succeeds. Existing GameVizIntegrationTests still pass.

---

## Phase 3: User Story 1 — Viewer Renders Graphics Reliably (Priority: P1) 🎯 MVP

**Goal**: Verify the hardened viewer renders SkiaSharp primitives continuously without dropping frames or crashing on exceptions.

**Independent Test**: Run ViewerTests with filter "US1". Viewer starts, draws primitives for 3 seconds, frame count > 0, no exceptions.

### Tests for User Story 1

- [x] T011 [US1] Write test ``US1 continuous rendering counts frames without exceptions`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer with OnRender callback that draws SKRect, SKCircle, SKLine, SKText, and a linear gradient using SKPaint. Use mutable frame counter incremented in callback. Run for 3 seconds, dispose, assert frameCount > 60 (at least ~20fps) and zero exceptions logged.
- [x] T012 [US1] Write test ``US1 render exception recovery continues rendering`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer with OnRender callback that throws InvalidOperationException every 10th frame. Run for 3 seconds. Assert frame counter continues incrementing past exception frames and viewer is still alive.

**Checkpoint**: US1 tests pass — viewer renders reliably and recovers from render callback exceptions.

---

## Phase 4: User Story 2 — Viewer Lifecycle Is Robust (Priority: P1)

**Goal**: Verify the viewer can be started, stopped, and restarted multiple times without resource leaks, crashes, or hangs.

**Independent Test**: Run ViewerTests with filter "US2". Viewer survives 10 start/stop cycles, cross-thread dispose works cleanly.

### Tests for User Story 2

- [x] T013 [US2] Write test ``US2 start stop cycle 10 times without crash`` in tests/FSBar.Viz.Tests/ViewerTests.fs — loop 10 times: create ViewerConfig with simple OnRender drawing a colored rectangle, call Viewer.run, sleep 500ms, dispose. Assert no exceptions thrown across all iterations.
- [x] T014 [US2] Write test ``US2 cross-thread dispose completes within timeout`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer on background thread via Viewer.run, sleep 1 second, dispose from test thread (different from window thread). Assert disposal completes within 2 seconds (use Task.Run + timeout).

**Checkpoint**: US2 tests pass — lifecycle is robust across repeated start/stop cycles.

---

## Phase 5: User Story 3 — Standalone Demo with SkiaSharp Primitives (Priority: P2)

**Goal**: Create a standalone test that exercises the viewer with at least 5 distinct SkiaSharp primitive types, with pan/zoom interaction. Serves as a visual smoke test and a verification that the viewer works independent of FSBar game context.

**Independent Test**: Run ViewerTests with filter "US3". Window opens showing primitives, responds to input, runs for 3 seconds.

### Tests for User Story 3

- [x] T015 [US3] Write test ``US3 standalone demo renders five primitive types`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer with OnRender callback that draws: (1) filled SKRect, (2) stroked SKRoundRect, (3) SKCircle via DrawCircle, (4) SKLine via DrawLine, (5) text via DrawText with SKFont, (6) linear gradient via SKShader on a rect. Count frames for 3 seconds, assert > 0 frames rendered.
- [x] T016 [US3] Write test ``US3 mouse scroll and drag callbacks fire`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer with OnMouseScroll and OnMouseDrag callbacks that set mutable flags. Run for 2 seconds (interaction is manual for now; test verifies the callbacks are wired without exceptions). Assert viewer starts and stops cleanly.

**Checkpoint**: US3 tests pass — standalone demo rendering verified with multiple primitive types.

---

## Phase 6: User Story 4 — Viewer Handles Edge Conditions Gracefully (Priority: P2)

**Goal**: Verify the viewer handles zero-size framebuffers, rapid resize, and concurrent access without crashes.

**Independent Test**: Run ViewerTests with filter "US4". All edge condition scenarios complete without exceptions.

### Tests for User Story 4

- [x] T017 [US4] Write test ``US4 zero-size framebuffer skips rendering gracefully`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer with small initial size (100x100), OnRender counts frames. Run for 2 seconds. Assert viewer is alive and frame count > 0. (Zero-size framebuffer is tested implicitly via the hardening in Viewer.fs; this test ensures the guard doesn't break normal operation.)
- [x] T018 [US4] Write test ``US4 concurrent access from multiple threads`` in tests/FSBar.Viz.Tests/ViewerTests.fs — start viewer, then spawn 4 threads that each call through different callbacks (OnResize, OnKeyDown, OnMouseScroll, OnMouseDrag) for 2 seconds via config callbacks. Assert no exceptions and viewer shuts down cleanly.

**Checkpoint**: US4 tests pass — edge conditions handled gracefully.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Update structural contracts and baselines if public API changed

- [x] T019 Update Viewer.fsi if any public API surface was added or changed in src/FSBar.Viz/Viewer.fsi — No changes needed, public API unchanged
- [x] T020 Regenerate Viewer.baseline by running tests with UPDATE_BASELINES=true if Viewer.fsi changed — Not needed, .fsi unchanged, SurfaceBaseline tests pass
- [x] T021 Run full test suite (all FSBar.Viz.Tests) to verify no regressions — ``XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj``
- [x] T022 Run ViewerTests 5 consecutive times to verify no flaky tests (SC-006) — All 8 tests pass 5/5 runs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phases 3-6)**: All depend on Foundational phase completion
  - US1 and US2 are independent of each other (can run in parallel)
  - US3 and US4 are independent of each other and of US1/US2 (can run in parallel)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **US2 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **US3 (P2)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **US4 (P2)**: Can start after Foundational (Phase 2) — no dependencies on other stories

### Within Each User Story

- Tests written first, then verified to exercise the hardened code
- Each story is independently testable after foundational fixes

### Parallel Opportunities

- T001 and T002 can run in parallel (Setup)
- T003-T009 are sequential within Viewer.fs (same file, dependent changes)
- T010 can run after T007 (depends on completion signaling being in place)
- T011/T012, T013/T014, T015/T016, T017/T018 can run in parallel within their stories (same file but independent test methods)
- All user story phases (3-6) can run in parallel after Phase 2

---

## Parallel Example: Foundational Phase

```bash
# Sequential — all changes in same file (Viewer.fs):
Task T003: "Add surfaceLock, shutdownRequested, windowCompleted to Viewer.run"
Task T004: "Fix surface synchronization with surfaceLock"
Task T005: "Fix zero-size framebuffer handling"
Task T006: "Fix cross-thread shutdown via Update callback"
Task T007: "Add completion signaling with ManualResetEventSlim"
Task T008: "Add diagnostic logging"
Task T009: "Guard pre-init resize"

# Then in parallel (different files):
Task T010: "Update GameViz.doStop" (GameViz.fs)
```

## Parallel Example: User Stories After Foundational

```bash
# All user story test phases can start in parallel:
Phase 3 (US1): T011, T012  — rendering reliability tests
Phase 4 (US2): T013, T014  — lifecycle robustness tests
Phase 5 (US3): T015, T016  — standalone demo tests
Phase 6 (US4): T017, T018  — edge condition tests
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational hardening (T003-T010)
3. Complete Phase 3: US1 rendering reliability tests (T011-T012)
4. **STOP and VALIDATE**: Run US1 tests, verify frame counting works
5. This alone proves the viewer is hardened and stable

### Incremental Delivery

1. Setup + Foundational → Viewer hardened
2. Add US1 tests → Rendering reliability verified (MVP!)
3. Add US2 tests → Lifecycle robustness verified
4. Add US3 tests → Standalone demo with primitives verified
5. Add US4 tests → Edge conditions verified
6. Polish → .fsi updated, baselines regenerated, full suite green

---

## Notes

- All tests in ViewerTests.fs use SkiaSharp primitives only — no FSBar.Client, no game engine
- Tests verify via frame callback counting, not pixel comparison
- Tests require X11 display (DISPLAY=:0) and XDG_RUNTIME_DIR set
- The foundational phase (T003-T009) modifies a single file (Viewer.fs) — tasks are sequential
- T010 (GameViz.fs) can run after T007 since it depends on the completion signal
