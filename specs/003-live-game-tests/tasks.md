# Tasks: Live Headless and Full Game Tests

**Input**: Design documents from `/specs/003-live-game-tests/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, quickstart.md

**Tests**: This feature IS the test infrastructure. All implementation tasks produce test code. No separate test tasks needed.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization, engine version config, and prerequisite validation

- [x] T001 Create engine version config in tests/engine-version.json with pinned engine binary (spring-headless), game (Beyond All Reason test-29840-d9b7dba), and map (Red Rock Desert v2) — reference HighBarV2/tests/engine-version.json for format
- [x] T002 Create prerequisite check script in tests/check-prerequisites.sh that validates engine binary on PATH, SPRING_DATADIR auto-detection, game archive in packages/, and map in maps/ — outputs JSON with per-check pass/fail, exit codes 0/1/2 — reference HighBarV2/tests/check-prerequisites.sh
- [x] T003 Create xUnit test project in tests/FSBar.LiveTests/FSBar.LiveTests.fsproj targeting net10.0 with references to FSBar.Client project, xUnit 2.9.x, Microsoft.NET.Test.Sdk, and xunit.runner.visualstudio

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Engine fixture that all live tests depend on — MUST be complete before any user story tasks

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Create EngineFixture in tests/FSBar.LiveTests/EngineFixture.fs — implement IAsyncLifetime: InitializeAsync runs check-prerequisites, creates BarClient with headless config, calls Start(), captures 30 warm-up frames via Step(), exposes Client/InitialFrames/InitialEvents/IsEngineAlive/diagnostic helpers. DisposeAsync calls client.Stop(). Add EngineCollection collection definition with ICollectionFixture<EngineFixture>. Reference HighBarV2/tests/integration/fsharp/Harness.fs for pattern
- [x] T005 Verify EngineFixture builds and runs — execute `dotnet build tests/FSBar.LiveTests/` and confirm no compilation errors

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 1 — Headless Engine Test Suite (Priority: P1) MVP

**Goal**: Automated integration tests that validate connection, commands, and events against a live headless BAR engine instance

**Independent Test**: `dotnet test tests/FSBar.LiveTests/` — launches spring-headless, connects via socket, exchanges frames, and validates all assertions

### Implementation for User Story 1

- [x] T006 [P] [US1] Create ConnectionTests in tests/FSBar.LiveTests/ConnectionTests.fs — Collection("Engine"), tests: harness smoke test (engine alive + socket exists), client connects to proxy, handshake completes with valid protocol metadata, first frame contains Init event, empty command responses for consecutive frames with monotonic frame numbers, graceful disconnect. Reference HighBarV2/tests/integration/fsharp/ConnectionTests.fs
- [x] T007 [P] [US1] Create CommandTests in tests/FSBar.LiveTests/CommandTests.fs — Collection("Engine"), helper to get first unit ID from InitialEvents, tests: MoveCommand causes unit position change (35 frames), BuildCommand triggers UnitCreated event (70 frames), StopCommand halts moving unit (send Move then Stop, 25 frames), Patrol/Guard/Attack/Fight smoke test (30 frames, verify no crash). Reference HighBarV2/tests/integration/fsharp/CommandTests.fs
- [x] T008 [P] [US1] Create EventTests in tests/FSBar.LiveTests/EventTests.fs — Collection("Engine"), tests: Init event with valid team ID (from warm-up), Update events with matching frame numbers (5-frame run), UnitCreated for builder unit (from warm-up, unitId > 0), UnitFinished for commander (lifecycle, verify finished ID matches created ID), unknown events don't crash frame loop (10-frame resilience). Reference HighBarV2/tests/integration/fsharp/EventTests.fs
- [x] T009 [US1] Run full headless test suite — execute `dotnet test tests/FSBar.LiveTests/ --verbosity normal` and verify all connection, command, and event tests pass against the live engine

**Checkpoint**: User Story 1 fully functional — headless integration tests pass against live engine

---

## Phase 4: User Story 2 — Full Graphical Game Test (Priority: P2)

**Goal**: Launch a full graphical BAR game session with the AI client connected for manual visual validation

**Independent Test**: `./tests/run-all.sh --graphical` — opens windowed BAR game with AI playing, developer observes

### Implementation for User Story 2

- [x] T010 [US2] Create standalone graphical launch script in tests/FSBar.LiveTests/GraphicalLaunch.fsx — #r FSBar.Client.dll, creates BarClient with graphical config (AppImage path, windowed mode), calls Start(), runs frames in a loop until process is interrupted (Ctrl+C), calls Stop() on exit. Checks DISPLAY env var and exits with clear error if not set. Reference HighBarV2/tests/fixtures/start-live.sh for engine launch pattern
- [x] T011 [US2] Verify graphical mode — execute `dotnet fsi tests/FSBar.LiveTests/GraphicalLaunch.fsx` with DISPLAY set, confirm BAR window opens in windowed mode with AI connected and processing frames

**Checkpoint**: User Story 2 functional — graphical game launches with AI connected

---

## Phase 5: User Story 3 — Unified Test Runner (Priority: P3)

**Goal**: Single entry point to run all test categories with auto-detection and summary reports

**Independent Test**: `./tests/run-all.sh` — runs unit + integration tests, generates summary report, skips engine tests cleanly when prerequisites not met

### Implementation for User Story 3

- [x] T012 [US3] Create unified test runner in tests/run-all.sh — argument parsing for --category (unit/integration) and --graphical flag, auto-detect engine via check-prerequisites.sh, run dotnet test for unit (src/FSBar.Client.Tests/) and integration (tests/FSBar.LiveTests/) categories, parse pass/fail/skip from output, generate Markdown report to reports/testreports/, signal handling for SIGINT/SIGTERM cleanup, print summary. --graphical delegates to tests/FSBar.LiveTests/GraphicalLaunch.fsx (from T010). Reference HighBarV2/tests/run-all.sh for full pattern
- [x] T013 [P] [US3] Add reports/testreports/ to .gitignore
- [x] T014 [US3] Verify unified test runner — run `./tests/run-all.sh` with engine available (all categories pass), run `./tests/run-all.sh --category unit` (unit tests only, no engine needed), verify report generated in reports/testreports/

**Checkpoint**: All user stories independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [x] T015 Verify engine cleanup after tests — confirm no orphaned spring-headless processes, no leftover /tmp/fsbar-*.sock files, no leftover /tmp/fsbar-* session directories after test run completes
- [x] T016 Run quickstart.md validation — follow all commands in specs/003-live-game-tests/quickstart.md and verify they work as documented

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase — headless tests need EngineFixture
- **User Story 2 (Phase 4)**: Depends on Phase 1 only (standalone GraphicalLaunch.fsx) — can run in parallel with US1
- **User Story 3 (Phase 5)**: Depends on Phase 1 + at minimum US1 completion (needs test projects to exist for the runner to invoke)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — no dependencies on other stories
- **User Story 2 (P2)**: Can start after Setup (Phase 1) — standalone GraphicalLaunch.fsx uses BarClient.startGraphical() directly, no dependency on run-all.sh or US1
- **User Story 3 (P3)**: Depends on US1 (test runner needs integration test project) and US2 (--graphical delegates to GraphicalLaunch.fsx)

### Within Each User Story

- T006, T007, T008 (US1) can all run in parallel — different files, no dependencies
- T010 must complete before T011 (US2)
- T012 must complete before T014 (US3); T013 can run in parallel with T012

### Parallel Opportunities

- T001, T002, T003 (Setup) can all run in parallel
- T006, T007, T008 (US1 implementation) can all run in parallel after T004 completes
- US1 and US2 can proceed in parallel after their respective prerequisites are met

---

## Parallel Example: User Story 1

```text
# After T004 (EngineFixture) completes, launch all US1 tasks in parallel:
Task T006: "Create ConnectionTests in tests/FSBar.LiveTests/ConnectionTests.fs"
Task T007: "Create CommandTests in tests/FSBar.LiveTests/CommandTests.fs"
Task T008: "Create EventTests in tests/FSBar.LiveTests/EventTests.fs"

# Then run verification:
Task T009: "Run full headless test suite"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T005)
3. Complete Phase 3: User Story 1 (T006–T009)
4. **STOP and VALIDATE**: `dotnet test tests/FSBar.LiveTests/` — all tests pass
5. This delivers the core value: live engine integration tests

### Incremental Delivery

1. Complete Setup + Foundational → foundation ready
2. Add User Story 1 → Test independently → Live headless tests working (MVP!)
3. Add User Story 2 → Test independently → Graphical game launch working
4. Add User Story 3 → Test independently → Unified test runner working
5. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- Reference files from HighBarV2 for proven patterns — adapt namespaces from HighBar.Tests → FSBar.LiveTests, HighBarClient → BarClient
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
