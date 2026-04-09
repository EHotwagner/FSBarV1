# Tasks: Observable GameState API

**Input**: Design documents from `/specs/017-observable-gamestate-api/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Test tasks are included per constitution (III. Test Evidence Is Mandatory).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Add new files to project, establish compile order

- [x] T001 Create GameState.fsi signature file in src/FSBar.Client/GameState.fsi per contracts/bar-client-fsi.md
- [x] T002 [P] Create UnitDefCache.fsi signature file in src/FSBar.Client/UnitDefCache.fsi per contracts/bar-client-fsi.md
- [x] T003 Add GameState.fsi, GameState.fs, UnitDefCache.fsi, UnitDefCache.fs to compile order in src/FSBar.Client/FSBar.Client.fsproj (before BarClient.fs, after Callbacks/MapQuery/MapCache)
- [x] T004 [P] Create stub GameState.fs in src/FSBar.Client/GameState.fs that compiles against GameState.fsi
- [x] T005 [P] Create stub UnitDefCache.fs in src/FSBar.Client/UnitDefCache.fs that compiles against UnitDefCache.fsi
- [x] T006 Verify `dotnet build src/FSBar.Client/` succeeds with new stubs

**Checkpoint**: Project compiles with new module stubs. No behavior changes yet.

---

## Phase 2: Foundational — UnitDefCache (Blocking Prerequisite)

**Purpose**: UnitDefCache is needed by GameState (Phase 4) and must be complete first

**⚠️ CRITICAL**: GameState cannot be implemented without UnitDefCache

- [x] T007 Implement UnitDefCache.loadFromEngine in src/FSBar.Client/UnitDefCache.fs — call getUnitDefs to get all def IDs, then batch-load name/cost/buildSpeed/maxWeaponRange/buildOptions for each, store in Map<int,UnitDefInfo> and Map<string,int>
- [x] T008 Implement UnitDefCache.tryFindById, tryFindByName, and all in src/FSBar.Client/UnitDefCache.fs
- [x] T009 Create src/FSBar.Client.Tests/UnitDefCacheTests.fs — test tryFindById returns correct UnitDefInfo, tryFindByName returns correct ID, tryFindByName for nonexistent name returns None, all returns full set
- [x] T010 Create surface-area baseline src/FSBar.Client.Tests/Baselines/UnitDefCache.baseline from UnitDefCache.fsi
- [x] T011 Verify `dotnet test src/FSBar.Client.Tests/ --filter UnitDefCache` passes

**Checkpoint**: UnitDefCache is functional. Can be tested independently against live engine (loads all ~2500 defs).

---

## Phase 3: User Story 1 — IObservable Frame Stream (Priority: P1)

**Goal**: Replace seq<GameFrame> with IObservable<GameFrame> so consumers subscribe to pushed frames

**Independent Test**: Subscribe to Frames observable, verify frames arrive as push notifications, verify OnCompleted on disconnect, verify multiple subscribers each receive all frames

### Implementation

- [x] T012 [US1] Implement custom IObservable<GameFrame> in src/FSBar.Client/BarClient.fs — add internal FrameObservable class with lock-protected subscriber list, OnNext/OnCompleted/OnError dispatch, Subscribe returns IDisposable that removes observer
- [x] T013 [US1] Replace seq<GameFrame> loop in BarClient.Frames with background thread that reads frames via Protocol.receiveFrame and pushes to FrameObservable.OnNext in src/FSBar.Client/BarClient.fs
- [x] T014 [US1] Update BarClient.fsi — change `member Frames: seq<GameFrame>` to `member Frames: System.IObservable<GameFrame>` in src/FSBar.Client/BarClient.fsi
- [x] T015 [US1] Wire OnCompleted on engine disconnect (EngineDisconnectedException) and OnError on unexpected protocol errors in src/FSBar.Client/BarClient.fs
- [x] T016 [US1] Update src/FSBar.Client.Tests/BarClientTests.fs — test subscribe/receive frames, multi-subscriber independence, OnCompleted on disconnect
- [x] T017 [US1] Update surface-area baseline src/FSBar.Client.Tests/Baselines/BarClient.baseline from updated BarClient.fsi
- [x] T018 [US1] Verify `dotnet test src/FSBar.Client.Tests/ --filter BarClient` passes

**Checkpoint**: Frames observable works. Consumers subscribe and receive pushed frames. seq<GameFrame> is gone.

---

## Phase 4: User Story 2 — Centralized GameState Tracking (Priority: P1)

**Goal**: Automatically track friendly units, enemies, and economy each frame — no consumer-side event processing needed

**Independent Test**: Start a session, advance frames, query GameState for friendly units/enemies/economy without writing event processing code

### Implementation

- [x] T019 [US2] Implement GameState.empty and GameState.processFrame in src/FSBar.Client/GameState.fs — process all GameEvent cases per data-model.md state transitions (UnitCreated→add, UnitDestroyed→remove, UnitIdle→flag, EnemyEnterLOS→add, etc.)
- [x] T020 [US2] Implement economy refresh in GameState.processFrame — call Callbacks.getEconomyCurrent/Income/Usage/Storage for metal (0) and energy (1) on each Update event in src/FSBar.Client/GameState.fs
- [x] T021 [US2] Implement unit position/health refresh in GameState.processFrame — on Update event, call Callbacks.getUnitPos/getUnitHealth for each tracked unit, reset IsIdle if position changed, in src/FSBar.Client/GameState.fs
- [x] T022 [US2] Implement pre-existing unit seeding — on Init event, discover commander and other pre-placed units via callbacks, populate Units map with TrackedUnit records in src/FSBar.Client/GameState.fs
- [x] T023 [US2] Add `member GameState: GameState` to BarClient — subscribe internally to Frames observable, call GameState.processFrame each frame, expose latest snapshot via volatile mutable field in src/FSBar.Client/BarClient.fs
- [x] T024 [US2] Update src/FSBar.Client/BarClient.fsi — add `member GameState: GameState`
- [x] T025 [US2] Create src/FSBar.Client.Tests/GameStateTests.fs — test: empty state, processFrame with UnitCreated adds unit, UnitDestroyed removes unit, UnitIdle sets flag, EnemyEnterLOS adds enemy, EnemyDestroyed removes enemy, economy snapshot updates
- [x] T026 [US2] Create surface-area baseline src/FSBar.Client.Tests/Baselines/GameState.baseline from GameState.fsi
- [x] T027 [US2] Update surface-area baseline src/FSBar.Client.Tests/Baselines/BarClient.baseline for new GameState member
- [x] T028 [US2] Verify `dotnet test src/FSBar.Client.Tests/ --filter "GameState|BarClient"` passes

**Checkpoint**: GameState tracks all units/enemies/economy automatically. Querying client.GameState returns current snapshot.

---

## Phase 5: User Story 3 — Instant Unit Definition Lookup (Priority: P1)

**Goal**: Look up unit definitions by name instantly from the cache loaded at initialization

**Independent Test**: After session init, look up a unit by name and get its DefId/cost/buildSpeed/buildOptions in sub-millisecond time

### Implementation

- [x] T029 [US3] Wire UnitDefCache.loadFromEngine into BarClient.Start — after handshake, before first frame, load all unit defs and store in GameState.UnitDefs in src/FSBar.Client/BarClient.fs
- [x] T030 [US3] Verify UnitDefCache is accessible via client.GameState.UnitDefs and lookups work — add test in src/FSBar.Client.Tests/GameStateTests.fs that verifies tryFindByName returns correct info after init
- [x] T031 [US3] Verify `dotnet test src/FSBar.Client.Tests/` passes (full suite)

**Checkpoint**: Unit defs cached at init. Name lookups are instant.

---

## Phase 6: User Story 4 — Separate Command Input Channel (Priority: P1)

**Goal**: SendCommands remains decoupled from the observable — commands queued and delivered with next protocol response

**Independent Test**: Send commands during a live session, verify engine executes them. Verify error raised on commands after session end.

### Implementation

- [x] T032 [US4] Verify SendCommands still works correctly with IObservable background thread — commands queued in BarClient and sent by the background frame-reading thread after receiving each frame, in src/FSBar.Client/BarClient.fs
- [x] T033 [US4] Add post-session command error — if SendCommands called when State is Stopped or Error, raise InvalidOperationException in src/FSBar.Client/BarClient.fs
- [x] T034 [US4] Add test in src/FSBar.Client.Tests/BarClientTests.fs — verify SendCommands after Stop raises InvalidOperationException
- [x] T035 [US4] Verify `dotnet test src/FSBar.Client.Tests/ --filter BarClient` passes

**Checkpoint**: Command channel works alongside observable. Errors on post-session submission.

---

## Phase 7: User Story 5 — Permanent Queryable Map (Priority: P2)

**Goal**: Map auto-loads on first access, caches static layers, refreshes LOS/radar each frame. Nearest metal spot query available.

**Independent Test**: After session init, query nearest metal spot and terrain passability — results return instantly from cached data

### Implementation

- [x] T036 [US5] Add MapCache.refreshDynamic function in src/FSBar.Client/MapCache.fs — calls MapGrid.refreshLos and MapGrid.refreshRadar on the cached grid
- [x] T037 [US5] Update src/FSBar.Client/MapCache.fsi — add `val refreshDynamic: stream: System.Net.Sockets.NetworkStream -> unit`
- [x] T038 [US5] Add MapQuery.nearestMetalSpot function in src/FSBar.Client/MapQuery.fs — linear scan of metal spots array, return closest by Euclidean distance to given position, return None if array is empty
- [x] T039 [US5] Update src/FSBar.Client/MapQuery.fsi — add `val nearestMetalSpot: spots: (float32 * float32 * float32 * float32) array -> position: float32 * float32 * float32 -> (float32 * float32 * float32 * float32) option`
- [x] T040 [US5] Wire MapCache.refreshDynamic into GameState.processFrame — on each Update event, if map is loaded, call refreshDynamic in src/FSBar.Client/GameState.fs
- [x] T041 [US5] Update surface-area baselines src/FSBar.Client.Tests/Baselines/MapCache.baseline and src/FSBar.Client.Tests/Baselines/MapQuery.baseline
- [x] T042 [US5] Add tests for nearestMetalSpot in src/FSBar.Client.Tests/ — empty array returns None, single spot returns it, multiple spots returns closest
- [x] T043 [US5] Verify `dotnet test src/FSBar.Client.Tests/` passes (full suite)

**Checkpoint**: Map caches static layers, refreshes dynamic layers per frame. Metal spot queries work.

---

## Phase 8: User Story 6 — Idiomatic F# Cleanup (Priority: P2)

**Goal**: Remove unnecessary private qualifiers, ensure records/DUs used consistently, retain mutable code in hot paths

**Independent Test**: Code review confirms patterns; full test suite passes confirming behavioral equivalence

### Implementation

- [x] T044 [P] [US6] Audit and remove unnecessary `private` qualifiers from module-level bindings in src/FSBar.Client/*.fs where the corresponding .fsi already restricts visibility
- [x] T045 [P] [US6] Audit all types in src/FSBar.Client/*.fs — verify records and DUs used for data modeling, classes only for IDisposable/mutable session state (BarClient)
- [x] T046 [US6] Verify `dotnet build src/FSBar.Client/` succeeds after cleanup — .fsi signatures still enforce correct visibility
- [x] T047 [US6] Verify `dotnet test src/FSBar.Client.Tests/` passes — full suite, behavioral equivalence confirmed

**Checkpoint**: Codebase follows idiomatic F# patterns. No behavioral changes.

---

## Phase 9: Consumer Updates

**Purpose**: Update all consumers of the old seq<GameFrame> API to use IObservable

- [x] T048 Update src/FSBar.Viz/LiveSession.fs — replace `for frame in client.Frames` iteration with `client.Frames.Subscribe(fun frame -> ...)` on background thread
- [x] T049 [P] Update src/FSBar.Viz/LiveSession.fsi if signature changes
- [x] T050 Update scripts/prelude.fsx — update frame consumption helpers for IObservable API
- [x] T051 [P] Update scripts/examples/04-step-by-step.fsx for IObservable subscription pattern
- [x] T052 [P] Update scripts/examples/08-live-viz.fsx for IObservable subscription pattern
- [x] T053 [P] Update scripts/examples/Repl.fsx and scripts/examples/ReplGraphical.fsx for IObservable API
- [x] T054 Update surface-area baseline src/FSBar.Client.Tests/Baselines/ for any changed .fsi files
- [x] T055 Verify `dotnet build` succeeds for all projects (FSBar.Client, FSBar.Viz, FSBar.Client.Tests, FSBar.Viz.Tests, FSBar.LiveTests)
- [x] T056 Verify `dotnet test src/FSBar.Client.Tests/` passes — full suite including SurfaceAreaTests

**Checkpoint**: All consumers updated. Full build and test suite green.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and documentation

- [x] T057 Run `dotnet test` across all test projects — src/FSBar.Client.Tests, tests/FSBar.Viz.Tests, tests/FSBar.LiveTests
- [x] T058 Verify all 15 surface-area baselines exist and match (13 existing + 2 new: GameState, UnitDefCache) via SurfaceAreaTests
- [x] T059 Run quickstart.md validation — execute build, test, and baseline update commands from specs/017-observable-gamestate-api/quickstart.md
- [x] T060 Verify `dotnet pack src/FSBar.Client/` succeeds (NuGet packability per constitution)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (UnitDefCache)**: Depends on Phase 1 — BLOCKS Phase 4 (GameState) and Phase 5 (Unit Def Lookup)
- **Phase 3 (IObservable)**: Depends on Phase 1 — can run in PARALLEL with Phase 2
- **Phase 4 (GameState)**: Depends on Phase 2 (UnitDefCache) AND Phase 3 (IObservable)
- **Phase 5 (Unit Def Lookup)**: Depends on Phase 4 (GameState wires UnitDefCache)
- **Phase 6 (Command Channel)**: Depends on Phase 3 (IObservable) — can run in PARALLEL with Phase 4/5
- **Phase 7 (Map)**: Depends on Phase 4 (GameState refreshes map)
- **Phase 8 (Cleanup)**: Can run in PARALLEL with Phases 5-7
- **Phase 9 (Consumers)**: Depends on Phase 3 (IObservable API must be stable)
- **Phase 10 (Polish)**: Depends on all previous phases

### Parallel Opportunities

```
Phase 1 (Setup)
    ├──→ Phase 2 (UnitDefCache)  ──┐
    │                               ├──→ Phase 4 (GameState) ──→ Phase 5 (UnitDef Lookup)
    └──→ Phase 3 (IObservable)  ──┤                         └──→ Phase 7 (Map)
                                   ├──→ Phase 6 (Commands)
                                   └──→ Phase 9 (Consumers)
                                                            Phase 8 (Cleanup) [anytime after Phase 1]
                                                                    ↓
                                                            Phase 10 (Polish)
```

### Within Each Phase

- Tasks marked [P] within a phase can run in parallel
- .fsi before .fs (signature before implementation)
- Implementation before tests
- Tests before checkpoint verification

---

## Parallel Example: After Phase 1 Setup

```
# These three can run simultaneously:
Agent 1: Phase 2 — UnitDefCache implementation (T007-T011)
Agent 2: Phase 3 — IObservable implementation (T012-T018)
Agent 3: Phase 8 — F# idiom cleanup (T044-T047)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 4)

1. Complete Phase 1: Setup
2. Complete Phase 3: IObservable (US1) + Phase 6: Commands (US4)
3. **STOP and VALIDATE**: Subscribe to observable, send commands, verify frames arrive
4. This is a working replacement of the old seq API

### Incremental Delivery

1. Setup + UnitDefCache + IObservable → Core API working
2. Add GameState (US2) + Unit Def Lookup (US3) → Centralized tracking with instant lookups
3. Add Map (US5) → Permanent queryable map
4. Add Cleanup (US6) + Consumers → Polish and update all dependents
5. Each increment is independently testable and delivers value

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution requires: .fsi for all public modules, surface-area baselines, test evidence
- UnitDefCache is the key blocking dependency — GameState cannot function without cached defs
- IObservable implementation uses no external Rx dependency — BCL IObservable<T> only
- Background thread for frame reading is single-writer; GameState snapshot is immutable (safe to read from any thread)
