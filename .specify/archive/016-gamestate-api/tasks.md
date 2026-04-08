# Tasks: GameState API, Unit Debugging, and Permanent Map

**Input**: Design documents from `/specs/016-gamestate-api/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Add new files to the project and establish compile order

- [ ] T001 Add GameState.fsi and GameState.fs entries to src/FSBar.Client/FSBar.Client.fsproj after MapCache.fs and before BarClient.fsi
- [ ] T002 Add UnitWatch.fsi and UnitWatch.fs entries to src/FSBar.Client/FSBar.Client.fsproj after GameState.fs and before BarClient.fsi

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types and modules that all user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [ ] T003 [P] Create EconomySnapshot record type in src/FSBar.Client/GameState.fsi (Current, Income, Usage, Storage fields)
- [ ] T004 [P] Create UnitDefInfo record type in src/FSBar.Client/GameState.fsi (DefId, Name, Cost, BuildSpeed, MaxWeaponRange, BuildOptions fields)
- [ ] T005 [P] Create TrackedUnit record type in src/FSBar.Client/GameState.fsi (UnitId, DefId, Name, X, Y, Z, Health, MaxHealth, IsFinished, IsIdle fields)
- [ ] T006 [P] Create TrackedEnemy record type in src/FSBar.Client/GameState.fsi (UnitId, DefId, Name, X, Y, Z, Health, MaxHealth, InLOS, InRadar, LastSeenFrame fields)
- [ ] T007 Create GameState record type in src/FSBar.Client/GameState.fsi (Frame, TeamId, Units, Enemies, Metal, Energy, UnitDefs, UnitDefsByName, Events fields)
- [ ] T008 Create GameState module signature in src/FSBar.Client/GameState.fsi with init, processFrame, defByName, defById, unitsByName, idleUnits function signatures
- [ ] T009 Implement GameState.init in src/FSBar.Client/GameState.fs — load all unit defs via getUnitDefs + getUnitDefName/Cost/BuildSpeed/MaxWeaponRange/BuildOptions, build UnitDefs and UnitDefsByName maps, return initial empty state
- [ ] T010 Verify project compiles with dotnet build src/FSBar.Client/

**Checkpoint**: Foundation types exist and compile. GameState.init can load unit definitions.

---

## Phase 3: User Story 1 — Centralized Game State Tracking (Priority: P1) MVP

**Goal**: Automatic tracking of friendly units, enemies, and economy via processFrame

**Independent Test**: Start game, step frames, query state for commander — no manual event processing needed

### Implementation for User Story 1

- [ ] T011 [US1] Implement GameState.processFrame event handling for UnitCreated and UnitFinished in src/FSBar.Client/GameState.fs — add to Units map with pos/health/def queries via Callbacks
- [ ] T012 [US1] Implement GameState.processFrame event handling for UnitDestroyed, UnitGiven, UnitCaptured in src/FSBar.Client/GameState.fs — remove from Units map
- [ ] T013 [US1] Implement GameState.processFrame event handling for UnitIdle in src/FSBar.Client/GameState.fs — set IsIdle=true on TrackedUnit
- [ ] T013b [US1] Implement idle reset in GameState.processFrame Update handler in src/FSBar.Client/GameState.fs — when refreshing unit positions, detect position change and set IsIdle=false for units that moved since last frame
- [ ] T014 [US1] Implement GameState.processFrame event handling for Update in src/FSBar.Client/GameState.fs — refresh positions and health of all tracked friendly units via Callbacks
- [ ] T015 [US1] Implement GameState.processFrame economy refresh in src/FSBar.Client/GameState.fs — query 8 economy callbacks (current/income/usage/storage for metal and energy)
- [ ] T016 [US1] Implement GameState.processFrame event handling for EnemyEnterLOS, EnemyLeaveLOS, EnemyEnterRadar, EnemyLeaveRadar, EnemyDestroyed in src/FSBar.Client/GameState.fs
- [ ] T017 [US1] Implement GameState.defByName, defById, unitsByName, idleUnits query functions in src/FSBar.Client/GameState.fs
- [ ] T018 [US1] Add StepTracked and StepTrackedWith members to src/FSBar.Client/BarClient.fsi — StepTracked returns GameState, StepTrackedWith takes (GameState -> AICommand list) handler
- [ ] T019 [US1] Add GameState property and InitGameState member to src/FSBar.Client/BarClient.fsi
- [ ] T020 [US1] Implement StepTracked, StepTrackedWith, InitGameState, and GameState property in src/FSBar.Client/BarClient.fs — internally maintain mutable GameState option, call processFrame after each step
- [ ] T021 [US1] Create surface area baseline tests/FSBar.Client.Tests/Baselines/GameState.baseline and add GameState module to tests/FSBar.Client.Tests/SurfaceBaselineTests.fs theory data (project created in T059)
- [ ] T022 [US1] Update surface area baseline tests/FSBar.Client.Tests/Baselines/BarClient.baseline for new StepTracked/StepTrackedWith/InitGameState/GameState members
- [ ] T023 [US1] Verify with dotnet build tests/FSBar.Viz.Tests/ and dotnet test tests/FSBar.Client.Tests/ (surface area tests)

**Checkpoint**: GameState tracks units, enemies, and economy. StepTracked works. Commander appears in state after warmup frames.

---

## Phase 4: User Story 2 — Instant Unit Definition Lookup (Priority: P1)

**Goal**: O(1) unit def lookup by name via cached UnitDefs and UnitDefsByName maps

**Independent Test**: After init, call defByName "armmex" and get result instantly

### Implementation for User Story 2

- [ ] T024 [US2] Verify GameState.init loads all unit defs with name, cost, buildSpeed, maxWeaponRange, buildOptions — already implemented in T009, validate by starting REPL and calling defByName "armmex"
- [ ] T025 [US2] Verify defByName returns None for non-existent names — test with defByName "nonexistent"

**Checkpoint**: Unit def lookup is instant. No protocol round-trips after init.

---

## Phase 5: User Story 3 — Permanent Queryable Map (Priority: P2)

**Goal**: MapCache enhanced with refreshDynamic, metalSpots, nearestMetalSpot, isPassable

**Independent Test**: Query nearest metal spot and passability from cached data

### Implementation for User Story 3

- [ ] T026 [P] [US3] Add refreshDynamic function signature to src/FSBar.Client/MapCache.fsi — takes stream, returns MapGrid with refreshed LOS+radar
- [ ] T027 [P] [US3] Add current function signature to src/FSBar.Client/MapCache.fsi — returns MapGrid option (cached grid or None)
- [ ] T028 [P] [US3] Add metalSpots function signature to src/FSBar.Client/MapCache.fsi — takes stream, returns cached metal spots array
- [ ] T029 [P] [US3] Add nearestMetalSpot function signature to src/FSBar.Client/MapCache.fsi — takes stream, x, z, returns nearest spot option
- [ ] T030 [P] [US3] Add isPassable function signature to src/FSBar.Client/MapCache.fsi — takes moveType, x (elmo), z (elmo), returns bool
- [ ] T031 [US3] Implement refreshDynamic in src/FSBar.Client/MapCache.fs — call MapGrid.refreshLos and refreshRadar on cached grid, update cache
- [ ] T032 [US3] Implement current in src/FSBar.Client/MapCache.fs — return cached grid option without triggering load
- [ ] T033 [US3] Implement metalSpots with caching in src/FSBar.Client/MapCache.fs — load via Callbacks.getMetalSpots on first call, cache for subsequent calls
- [ ] T034 [US3] Implement nearestMetalSpot in src/FSBar.Client/MapCache.fs — iterate cached metal spots, return closest by Euclidean distance to (x, z)
- [ ] T035 [US3] Implement isPassable in src/FSBar.Client/MapCache.fs — convert elmo coords to grid coords via MapQuery.elmoToGrid, check cached passability grid
- [ ] T036 [US3] Update surface area baseline tests/FSBar.Client.Tests/Baselines/MapCache.baseline for new functions
- [ ] T037 [US3] Verify with dotnet build src/FSBar.Client/ and dotnet test tests/FSBar.Client.Tests/

**Checkpoint**: MapCache provides instant queries for metal spots, passability, and dynamic layer refresh.

---

## Phase 6: User Story 4 — Unit Debugging and Watch System (Priority: P2)

**Goal**: Watch specific units and get per-frame status reports

**Independent Test**: Watch commander, step frames, see automatic status output

### Implementation for User Story 4

- [ ] T038 [P] [US4] Create UnitWatch module signature in src/FSBar.Client/UnitWatch.fsi with watch, unwatch, clear, watched, report, setAutoReport, autoReportEnabled functions
- [ ] T039 [US4] Implement UnitWatch module in src/FSBar.Client/UnitWatch.fs — mutable Set<int> ref for watched units, bool ref for auto-report, report function prints watched unit status from GameState
- [ ] T040 [US4] Integrate UnitWatch.report into BarClient.StepTracked in src/FSBar.Client/BarClient.fs — call report after processFrame when autoReportEnabled
- [ ] T041 [US4] Create surface area baseline tests/FSBar.Client.Tests/Baselines/UnitWatch.baseline and add UnitWatch to SurfaceBaselineTests.fs theory data
- [ ] T042 [US4] Verify with dotnet build src/FSBar.Client/ and dotnet test tests/FSBar.Client.Tests/

**Checkpoint**: Unit watch system works. Auto-report prints watched units each frame.

---

## Phase 7: User Story 5 — Simplified REPL and Visualization (Priority: P3)

**Goal**: REPL and GameViz use centralized GameState, eliminating duplicated tracking

**Independent Test**: Start game with viz, verify REPL and viz show same units

### Implementation for User Story 5

- [ ] T043 [US5] Rewrite scripts/examples/Repl.fsx — remove _units mutable and processFrame function, replace with _state: GameState option, use StepTracked/StepTrackedWith for all stepping
- [ ] T044 [US5] Update units() in scripts/examples/Repl.fsx to read from _state.Value.Units
- [ ] T045 [US5] Update economy() in scripts/examples/Repl.fsx to read from _state.Value.Metal and _state.Value.Energy
- [ ] T046 [US5] Update spawnByName in scripts/examples/Repl.fsx to use GameState.defByName for O(1) lookup
- [ ] T047 [US5] Add watch, unwatch, watches REPL helper functions in scripts/examples/Repl.fsx — delegate to UnitWatch module
- [ ] T048 [US5] Add getState, defByName, nearestMetal REPL helper functions in scripts/examples/Repl.fsx
- [ ] T049 [US5] Add onGameState function signature to src/FSBar.Viz/GameViz.fsi — takes GameState, updates viz snapshot
- [ ] T050 [US5] Implement onGameState in src/FSBar.Viz/GameViz.fs — convert GameState.Units/Enemies to viz UnitState map, update economy from GameState, process GameState.Events for event indicators
- [ ] T051 [US5] Update viz() in scripts/examples/Repl.fsx to call GameViz.onGameState instead of GameViz.onFrame during tracked stepping
- [ ] T052 [US5] Update help() in scripts/examples/Repl.fsx to document new commands (watch, unwatch, watches, getState, defByName, nearestMetal)
- [ ] T053 [US5] Verify with dotnet build tests/FSBar.Viz.Tests/

**Checkpoint**: REPL and viz use centralized GameState. No duplicated event processing. Both show consistent unit data.

---

## Phase 7.5: Behavioral Tests (Constitution III Compliance)

**Purpose**: Automated integration tests that verify runtime behavior against acceptance scenarios

- [ ] T059 Create tests/FSBar.Client.Tests/ project: dotnet new xunit -lang F# -o tests/FSBar.Client.Tests, add references to FSBar.Client and FSBar.Proto, create SurfaceBaselineTests.fs mirroring FSBar.Viz.Tests pattern
- [ ] T060 [US1] Add live integration test in tests/FSBar.Client.Tests/GameStateTests.fs: start headless game, StepTracked 30 frames, assert commander appears in GameState.Units with Health > 0 and correct DefId
- [ ] T061 [US1] Add live integration test: StepTracked until a factory builds a unit, assert new unit appears in GameState.Units after UnitFinished
- [ ] T062 [US2] Add live integration test: after InitGameState, call defByName "armmex" and assert result is Some with correct fields
- [ ] T063 [US3] Add live integration test: after map load, call nearestMetalSpot and assert returned spot has positive richness value
- [ ] T064 [US4] Add live integration test: watch commander, StepTracked 5 frames with autoReport enabled, assert report function executes without error

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [ ] T054 [P] Update GameViz.fsi surface area baseline if changed
- [ ] T055 Run full surface area test suite with dotnet test tests/FSBar.Client.Tests/
- [ ] T056 Start REPL, run start(), viz(), step 100, screenshot() — verify end-to-end flow
- [ ] T057 Validate quickstart.md scenarios work against the implementation
- [ ] T058 Commit all changes with descriptive commit message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — core state tracking
- **US2 (Phase 4)**: Depends on US1 — validates init already done in US1
- **US3 (Phase 5)**: Depends on Foundational — can run in parallel with US1/US2
- **US4 (Phase 6)**: Depends on US1 (needs GameState for report function)
- **US5 (Phase 7)**: Depends on US1, US3, US4 (integrates all new APIs)
- **Behavioral Tests (Phase 7.5)**: Depends on US1, US2, US3, US4 (tests against implemented features)
- **Polish (Phase 8)**: Depends on all user stories and Phase 7.5

### User Story Dependencies

- **US1 (P1)**: Foundational only — no other story dependencies
- **US2 (P1)**: Depends on US1 — validates that init populated defs correctly via StepTracked/REPL
- **US3 (P2)**: Foundational only — can run in parallel with US1
- **US4 (P2)**: Depends on US1 (needs GameState type and processFrame)
- **US5 (P3)**: Depends on US1 + US3 + US4 (integrates everything)

### Within Each User Story

- .fsi signature before .fs implementation
- Implementation before baseline updates
- Baseline updates before test validation

### Parallel Opportunities

- T003-T006 (foundational types) can all run in parallel
- T026-T030 (MapCache signatures) can all run in parallel
- US1 and US3 can be worked in parallel after Foundational
- US2 is a validation-only phase, very lightweight

---

## Parallel Example: User Story 3 (MapCache)

```text
# Launch all MapCache signature tasks together:
T026: Add refreshDynamic to MapCache.fsi
T027: Add current to MapCache.fsi
T028: Add metalSpots to MapCache.fsi
T029: Add nearestMetalSpot to MapCache.fsi
T030: Add isPassable to MapCache.fsi
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 2: Foundational (T003-T010)
3. Complete Phase 3: User Story 1 (T011-T023)
4. **STOP and VALIDATE**: Start REPL, start(), step 30 — verify commander appears in GameState
5. Can demo centralized state tracking

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Centralized game state tracking (MVP!)
3. US2 → Validate unit def lookup works
4. US3 → MapCache enhancements (can parallel with US1)
5. US4 → Unit watch/debugging
6. US5 → Rewrite REPL + viz to use GameState
7. Polish → End-to-end validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Phase 7.5 provides behavioral test tasks per Constitution III
- Surface area baselines serve as structural contract tests per constitution
- Live engine tests for integration validation at checkpoints
- Commit after each phase or logical group
