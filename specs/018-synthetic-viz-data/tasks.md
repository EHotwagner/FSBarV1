# Tasks: Synthetic Visualization Test Data

**Input**: Design documents from `/specs/018-synthetic-viz-data/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — constitution requires test evidence for behavior-changing code.

**Organization**: Tasks grouped by user story. All three user stories are P1 but have natural sequencing: US1 (single scene generator) must work before US2 (three scenes) or US3 (continuity validation).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Project initialization, .fsproj files, and dependency wiring

- [x] T001 Create FSBar.SyntheticData project at src/FSBar.SyntheticData/FSBar.SyntheticData.fsproj with net10.0 target, ProjectReference to FSBar.Client, PackageId FSBar.SyntheticData
- [x] T002 Create FSBar.SyntheticData.Tests project at src/FSBar.SyntheticData.Tests/FSBar.SyntheticData.Tests.fsproj with xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x, ProjectReference to FSBar.SyntheticData
- [x] T003 Add both new projects to the solution file (if sln exists) or verify dotnet build works from repo root

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types and simulation primitives that all scenes depend on

**CRITICAL**: No scene generation can begin until these modules exist

- [x] T004 [P] Create SceneTypes.fsi and SceneTypes.fs at src/FSBar.SyntheticData/ — define SceneId (SceneA|SceneB|SceneC), Scene record (Id, Name, MapWidth, MapHeight, Frames: GameState array, GameFrames: GameFrame array, UnitDefs: UnitDefCache)
- [x] T005 [P] Create UnitDefs.fsi and UnitDefs.fs at src/FSBar.SyntheticData/ — define helper functions to build UnitDefInfo records and pre-built UnitDefCache sets for each scene (arm_commander, arm_solar, arm_wind, arm_mex, arm_lab, arm_peewee, arm_flash, arm_rockko, arm_samson, cor_commander, cor_gator, cor_thud, etc. with realistic Cost, BuildSpeed, MaxWeaponRange, BuildOptions)
- [x] T006 [P] Create EconomySim.fsi and EconomySim.fs at src/FSBar.SyntheticData/ — pure economy simulation: step function takes current EconomySnapshot + income delta + usage delta and returns next EconomySnapshot with Current clamped to [0, Storage]
- [x] T007 [P] Create UnitSim.fsi and UnitSim.fs at src/FSBar.SyntheticData/ — unit movement simulation: step function moves TrackedUnit toward a target waypoint at a given speed (max 5 elmos/frame per axis), clamps to map bounds, updates IsIdle when target reached
- [x] T008 [P] Create EnemySim.fsi and EnemySim.fs at src/FSBar.SyntheticData/ — enemy visibility state machine: tracks InRadar/InLOS per enemy, generates EnemyEnterRadar/EnemyEnterLOS/EnemyLeaveLOS/EnemyLeaveRadar events on transitions, updates TrackedEnemy fields to match
- [x] T009 Create Validation.fsi and Validation.fs at src/FSBar.SyntheticData/ — validate a Scene: check all FR-006 through FR-010 invariants (position bounds, economy consistency, event/state consistency, lifecycle ordering, visibility transitions), return string list of errors

**Checkpoint**: All simulation primitives compile and are independently testable

---

## Phase 3: User Story 1 — Generate a Complete Synthetic Game Scene (Priority: P1) MVP

**Goal**: Produce a single scene of 300 GameState/GameFrame snapshots with units, enemies, economy, and events using real FSBar.Client types

**Independent Test**: Generate Scene A, validate with Validation.validate, assert 300 frames, non-empty units/enemies/economy

### Implementation for User Story 1

- [x] T010 [US1] Create Scenes.fsi at src/FSBar.SyntheticData/ — declare `val generate: SceneId -> Scene` and `val generateAll: unit -> Scene list`
- [x] T011 [US1] Create Scenes.fs at src/FSBar.SyntheticData/ — implement Scene A ("Small Map - Early Game Buildup"): 4096x4096 map, start with 1 arm_commander (UnitId=1), schedule UnitCreated/UnitFinished events to build ~12 units over 300 frames (arm_mex at frame 10, arm_solar at 20, arm_lab at 40, arm_peewee x3 starting frame 80, etc.), ramp economy from 0 income to ~15 metal/s income, 2-3 enemies enter radar/LOS around frame 150-250
- [x] T012 [US1] Wire up all modules in Scenes.fs for Scene A: on each frame call UnitSim.step for all units, EconomySim.step for metal+energy, EnemySim.step for enemies, collect generated GameEvents, build GameState and GameFrame, include Update event every frame and Init event on frame 1
- [x] T013 [US1] Update FSBar.SyntheticData.fsproj Compile list with correct file ordering: SceneTypes.fsi, SceneTypes.fs, UnitDefs.fsi, UnitDefs.fs, EconomySim.fsi, EconomySim.fs, UnitSim.fsi, UnitSim.fs, EnemySim.fsi, EnemySim.fs, Validation.fsi, Validation.fs, Scenes.fsi, Scenes.fs

### Tests for User Story 1

- [x] T014 [P] [US1] Create SceneATests.fs at src/FSBar.SyntheticData.Tests/ — test Scene A generation: assert 300 frames, FrameNumbers 1-300, non-empty Units map by frame 50, Init event in frame 1, Update event in every frame, all DefIds exist in UnitDefCache
- [x] T015 [P] [US1] Create ValidationTests.fs at src/FSBar.SyntheticData.Tests/ — test Validation.validate returns empty error list for Scene A; test intentionally broken scenes return specific errors (out-of-bounds position, economy > storage, missing UnitCreated event)
- [x] T016 [US1] Verify dotnet test src/FSBar.SyntheticData.Tests/ passes with Scene A tests green

**Checkpoint**: Scene A generates 300 valid frames, all tests pass

---

## Phase 4: User Story 2 — Three Distinct Scenes with Different Maps (Priority: P1)

**Goal**: Add Scenes B and C with different map sizes, unit compositions, and tactical situations

**Independent Test**: Generate all 3 scenes, verify different map dimensions, different unit counts, different event distributions

### Implementation for User Story 2

- [x] T017 [US2] Implement Scene B in Scenes.fs — "Medium Map - Mid-Game Skirmish": 8192x8192 map, start with ~20 friendly units (mix of arm_peewee, arm_flash, arm_rockko, arm_samson, arm_commander) and ~15 enemies, schedule combat from frame 30: WeaponFired + UnitDamaged/EnemyDamaged events, destroy 5-8 units over 300 frames, economy fluctuating with factory production (~25 metal/s income, ~20 metal/s usage)
- [x] T018 [US2] Implement Scene C in Scenes.fs — "Large Map - Late-Game Siege": 16384x16384 map, start with ~50 friendly units and ~40 enemies (diverse types including heavy units), high event density (~10+ events per frame), economy near storage caps (storage=10000, current oscillating 8000-10000), units clustered in 2-3 groups around attack/defense positions
- [x] T019 [US2] Implement generateAll in Scenes.fs — call generate for each SceneId, return list of 3 scenes

### Tests for User Story 2

- [x] T020 [P] [US2] Create SceneBTests.fs at src/FSBar.SyntheticData.Tests/ — test Scene B: assert 300 frames, >= 20 combat events (UnitDamaged + EnemyDamaged + WeaponFired), unit count decreases over time, at least one UnitDestroyed event, map bounds 8192x8192
- [x] T021 [P] [US2] Create SceneCTests.fs at src/FSBar.SyntheticData.Tests/ — test Scene C: assert 300 frames, >= 50 starting units, >= 40 starting enemies, >= 5 distinct DefIds, economy Current near Storage, map bounds 16384x16384
- [x] T022 [US2] Verify all 3 scenes validate clean: Validation.validate returns [] for each, all scenes have different MapWidth/MapHeight

**Checkpoint**: All 3 scenes generate valid, distinct data

---

## Phase 5: User Story 3 — Realistic Frame-to-Frame Continuity (Priority: P1)

**Goal**: Verify and enforce that consecutive frames represent plausible state evolution

**Independent Test**: Iterate all consecutive frame pairs across all scenes and assert movement, economy, and event continuity bounds

### Implementation for User Story 3

- [x] T023 [US3] Add continuity validation to Validation.fs — new function `validateContinuity: Scene -> string list` that checks: position deltas <= 6 elmos/frame per axis, economy Current delta consistent with Income/Usage, units appear only with UnitCreated event, units disappear only with UnitDestroyed event, enemy visibility flags match latest visibility event
- [x] T024 [US3] Ensure all scenes pass continuity validation — fix any scene generation logic that produces discontinuities (e.g., units spawning without events, economy jumps)

### Tests for User Story 3

- [x] T025 [US3] Create ContinuityTests.fs at src/FSBar.SyntheticData.Tests/ — test all 3 scenes: assert validateContinuity returns [] for each scene, explicitly test edge cases: unit created+destroyed within window, enemy LOS toggle cycle, economy at storage cap

**Checkpoint**: All scenes pass both structural and continuity validation

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Constitution compliance, surface area baselines, scripting accessibility

- [x] T026 [P] Create SurfaceAreaTests.fs at src/FSBar.SyntheticData.Tests/ — surface-area baseline tests for Scenes and Validation modules
- [x] T027 [P] Create Baselines/Scenes.baseline and Baselines/Validation.baseline at src/FSBar.SyntheticData.Tests/ — initial surface-area baseline snapshots
- [x] T028 [P] Create prelude.fsx at src/FSBar.SyntheticData/scripts/ — FSI prelude that loads FSBar.SyntheticData.dll and FSBar.Client.dll, exposes generate/generateAll/validate helpers
- [x] T029 [P] Create 01-generate-scene.fsx at src/FSBar.SyntheticData/scripts/examples/ — example script demonstrating scene generation and inspection
- [x] T030 Verify dotnet pack src/FSBar.SyntheticData/ produces a valid .nupkg
- [x] T031 Run full test suite: dotnet test src/FSBar.SyntheticData.Tests/ — all tests green
- [x] T032 Validate quickstart.md scenarios work end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — MVP, must complete first
- **US2 (Phase 4)**: Depends on Phase 3 (extends Scenes.fs with B and C)
- **US3 (Phase 5)**: Depends on Phase 4 (validates all 3 scenes)
- **Polish (Phase 6)**: Depends on Phase 5

### Within Each Phase

- Tasks marked [P] can run in parallel
- Unmarked tasks run sequentially in listed order
- Tests depend on their corresponding implementation tasks

### Parallel Opportunities

- Phase 2: T004-T009 are all [P] — 6 modules can be written in parallel (different files, no dependencies)
- Phase 3: T014 and T015 are [P] — test files can be written in parallel
- Phase 4: T020 and T021 are [P] — test files can be written in parallel
- Phase 6: T026-T029 are all [P] — surface area, baselines, and scripts can be written in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
2. Complete Phase 2: Foundational (T004-T009)
3. Complete Phase 3: User Story 1 (T010-T016)
4. **STOP and VALIDATE**: Scene A generates 300 valid frames, tests pass
5. This is a usable MVP — visualization can start testing with 1 scene

### Incremental Delivery

1. Setup + Foundational → simulation primitives ready
2. Add US1 (Scene A) → test independently → MVP
3. Add US2 (Scenes B, C) → test independently → full scene coverage
4. Add US3 (continuity validation) → test independently → quality assurance
5. Polish → baselines, scripts, packaging → production ready

---

## Notes

- All user stories are P1 but have natural sequencing (US1 → US2 → US3)
- The generator must be deterministic — same SceneId always produces identical output
- Constitution requires .fsi files for all public modules (handled in Phase 2)
- Constitution requires surface-area baselines (handled in Phase 6)
- Constitution requires FSI scripting accessibility (handled in Phase 6)
