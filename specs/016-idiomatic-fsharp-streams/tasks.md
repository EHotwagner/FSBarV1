# Tasks: Idiomatic F# Streams Refactor

**Input**: Design documents from `/specs/016-idiomatic-fsharp-streams/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No new project structure needed. This is a refactoring of an existing library.

- [x] T001 Verify clean build of current codebase with `dotnet build src/FSBar.Client/`
- [x] T002 Verify all existing tests pass with `dotnet test src/FSBar.Client.Tests/`

---

## Phase 2: Foundational — Remove Private Qualifiers

**Purpose**: Remove all redundant `private` qualifiers from module-level bindings in .fs files where the corresponding .fsi file already restricts visibility. This is a mechanical, safe change that unblocks the idiomatic cleanup stories.

**CRITICAL**: These tasks modify different files and can all run in parallel.

- [x] T003 [P] Remove 7 `private` qualifiers from helper functions (intParam, getInt, getFloat, getString, getVector3, getFloatArray, getIntArray) in src/FSBar.Client/Callbacks.fs
- [x] T004 [P] Remove 6 `private` qualifiers from helper functions (standardDataDir, isExecutable, tryBinary, resolveFromEnvVar, resolveFromConfigFile, sourceLabel) in src/FSBar.Client/EngineDiscovery.fs
- [x] T005 [P] Remove 5 `private` qualifiers from helper functions (extractGuid, detectSpringDataDir, copyArchiveCache, writePidFile, launchEngine) in src/FSBar.Client/EngineLauncher.fs
- [x] T006 [P] Remove 2 `private` qualifiers from array conversion helpers (toFloat32Array2D, toIntArray2D) in src/FSBar.Client/MapGrid.fs
- [x] T007 [P] Remove 2 `private` qualifiers from cache dictionaries (gridCache, passabilityCache) in src/FSBar.Client/MapCache.fs
- [x] T008 [P] Remove 2 `private` qualifiers from module-level bindings (protocolVersion, nextRequestId) in src/FSBar.Client/Protocol.fs
- [x] T009 [P] Remove 1 `private` qualifier from readExact helper in src/FSBar.Client/Connection.fs
- [x] T010 [P] Remove 1 `private` qualifier from shutdownReasonToString helper in src/FSBar.Client/Events.fs
- [x] T011 [P] Remove 1 `private` qualifier from boundsCheck helper in src/FSBar.Client/MapQuery.fs
- [x] T012 [P] Remove 2 `private` qualifiers from constants (INTERNAL_ORDER, MAX_TIMEOUT) in src/FSBar.Client/Commands.fs
- [x] T013 Build and test: run `dotnet build src/FSBar.Client/` and `dotnet test src/FSBar.Client.Tests/` to verify no regressions from private qualifier removal

**Checkpoint**: All `private` qualifiers removed from module-level bindings. Build and tests pass. Do NOT remove `member private` from BarClient.fs (class members need explicit private).

---

## Phase 3: User Story 1 — Consume Game State as a Simple Stream (Priority: P1) MVP

**Goal**: Expose game frames as `seq<GameFrame>` on the BarClient class so consumers can iterate with standard F# sequence operations.

**Independent Test**: Connect to engine, iterate `session.Frames`, verify GameFrame values arrive with correct frame numbers and events.

### Implementation for User Story 1

- [x] T014 [US1] Add `mutable pendingCommands: Highbar.AICommand list` field (initialized to `[]`) and `mutable firstFrame: bool` field (initialized to `true`) to BarClient class in src/FSBar.Client/BarClient.fs
- [x] T015 [US1] Implement `Frames` property as `seq<GameFrame>` using a sequence expression in src/FSBar.Client/BarClient.fs. The sequence must: (1) require Connected state, (2) set state to Running, (3) loop: send pendingCommands (or empty) via Protocol.sendFrameResponse for previous frame (skip on first frame), reset pendingCommands to [], call Protocol.receiveFrame, yield GameFrame on Some, set state to Stopped and end on None, (4) catch EngineDisconnectedException and transition to Stopped
- [x] T016 [US1] Add `Frames: seq<Protocol.GameFrame>` property to the BarClient class signature in src/FSBar.Client/BarClient.fsi with doc comment explaining lazy iteration and clean termination on disconnect
- [x] T017 [US1] Update BarClientTests to add test `frames_property_exists_on_client` verifying the Frames property is accessible on a created (Idle) BarClient in src/FSBar.Client.Tests/BarClientTests.fs

**Checkpoint**: BarClient exposes `Frames` property. Build passes. Existing tests still pass (Step/StepWith/Run/RunUntil still present at this point).

---

## Phase 4: User Story 2 — Send Commands via Separate Input Channel (Priority: P1)

**Goal**: Add `SendCommands` method to BarClient for submitting AI commands decoupled from frame iteration.

**Independent Test**: Call `SendCommands` with a command list, verify commands are queued and delivered on next frame iteration.

### Implementation for User Story 2

- [x] T018 [US2] Implement `SendCommands` method on BarClient class in src/FSBar.Client/BarClient.fs. Must: (1) validate session is Connected or Running, (2) set pendingCommands to provided list, (3) raise InvalidOperationException if session is Stopped/Idle/Error with message "Cannot send commands: session is {state}"
- [x] T019 [US2] Add `SendCommands: commands: Highbar.AICommand list -> unit` to BarClient class signature in src/FSBar.Client/BarClient.fsi with doc comment explaining command queuing behavior
- [x] T020 [US2] Add test `send_commands_raises_when_idle` verifying SendCommands raises InvalidOperationException on an Idle client in src/FSBar.Client.Tests/BarClientTests.fs
- [x] T021 [US2] Remove `Step`, `StepWith`, `Run`, `RunUntil` method implementations from BarClient class in src/FSBar.Client/BarClient.fs
- [x] T022 [US2] Remove `Step`, `StepWith`, `Run`, `RunUntil` signatures from BarClient class in src/FSBar.Client/BarClient.fsi
- [x] T023 [US2] Rewrite `Reset` method in src/FSBar.Client/BarClient.fs to use internal Protocol calls directly instead of StepWith (receive frame, send cheat commands via Protocol.sendFrameResponse, run verification steps via Protocol.receiveFrame + Protocol.sendFrameResponse)
- [x] T024 [US2] Update BarClientTests in src/FSBar.Client.Tests/BarClientTests.fs: remove `stream_access_before_connect_throws` test (if it references removed methods) and verify remaining tests pass with new API

**Checkpoint**: BarClient has Frames + SendCommands, Step/StepWith/Run/RunUntil removed. Reset rewritten. Build and tests pass.

---

## Phase 5: Script Updates (Constitution §V Compliance)

**Purpose**: Update prelude and example scripts to use the new Frames+SendCommands API. Broken scripts are treated as build defects per constitution §V.

**CRITICAL**: These tasks modify different files and can all run in parallel.

- [x] T025 [P] Update scripts/prelude.fsx: replace Step() comment/usage with Frames iteration pattern
- [x] T026a [P] Update scripts/examples/01-hello-bar.fsx: replace client.Step() with Frames iteration
- [x] T026b [P] Update scripts/examples/02-graphical-game.fsx: replace client.Run(n, handler) with Frames + SendCommands loop
- [x] T026c [P] Update scripts/examples/04-step-by-step.fsx: replace Step()/Run() with Frames + SendCommands pattern
- [x] T026d [P] Update scripts/examples/05-map-layers.fsx: replace Step() with Frames |> Seq.take
- [x] T026e [P] Update scripts/examples/06-game-viz-basic.fsx: replace Step() with Frames iteration
- [x] T026f [P] Update scripts/examples/07-game-viz-layers.fsx: replace Step() with Frames iteration
- [x] T026g [P] Update scripts/examples/Repl.fsx: replace Step()/StepWith() with Frames + SendCommands throughout (~15 call sites)
- [x] T026h [P] Update scripts/examples/ReplGraphical.fsx: replace Step()/StepWith() with Frames + SendCommands throughout (~15 call sites)
- [x] T026i Verify all updated scripts load in FSI without errors

**Checkpoint**: All scripts updated to new API. Constitution §V compliance restored.

---

## Phase 6: User Story 4 — Ensure Idiomatic F# Patterns Throughout (Priority: P2)

**Goal**: Confirm the codebase uses idiomatic F# patterns consistently. The codebase is already largely idiomatic; this is a review and minor cleanup pass.

**Independent Test**: Code review confirms records/DUs for data, pattern matching for control flow, mutable only in performance paths. All tests pass.

- [x] T027 [P] [US4] Review BarClient class in src/FSBar.Client/BarClient.fs: confirm class is justified (IDisposable + mutable session state), verify no unnecessary OOP patterns remain after Step/StepWith/Run/RunUntil removal
- [x] T028 [P] [US4] Review all .fs files in src/FSBar.Client/ for any remaining non-idiomatic patterns: unnecessary type annotations, imperative loops that could be simpler functional expressions (except in performance-critical Connection.fs, Protocol.fs, MapGrid.fs where mutable is justified)

**Checkpoint**: Codebase review complete. All patterns are idiomatic F#.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, baseline regeneration, and cleanup.

- [x] T029 Regenerate surface area baselines by running `UPDATE_BASELINES=true dotnet test src/FSBar.Client.Tests/` to update baseline files reflecting new API surface (Frames, SendCommands added; Step, StepWith, Run, RunUntil removed)
- [x] T030 Run full build: `dotnet build` for entire solution to verify no downstream compilation errors in other projects referencing FSBar.Client
- [x] T031 Run full test suite: `dotnet test` for all test projects
- [x] T032 Final grep verification: confirm zero `private` on module-level bindings in .fs files with .fsi counterparts (excluding `member private` in BarClient.fs)
- [x] T033 Verify frame throughput: in FSI, run a 100-frame loop with the new Frames API and confirm no observable latency increase vs the Step-based baseline (qualitative check, not benchmarked)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — verify current state
- **Foundational (Phase 2)**: Depends on Phase 1 — remove private qualifiers (all [P] tasks parallel)
- **User Story 1 (Phase 3)**: Depends on Phase 2 — add Frames property
- **User Story 2 (Phase 4)**: Depends on Phase 3 — add SendCommands, remove old API
- **Script Updates (Phase 5)**: Depends on Phase 4 — scripts must use new API after old API is removed
- **User Story 4 (Phase 6)**: Depends on Phase 4 — review after all changes
- **Polish (Phase 7)**: Depends on all previous phases complete

### User Story Dependencies

- **US1 (Frames)**: Requires foundational phase. Independent of US3/US4.
- **US2 (SendCommands)**: Requires US1 (Frames must exist before removing old API). Independent of US3/US4.
- **US3 (Private qualifiers)**: Completed in foundational phase (Phase 2). No story dependencies.
- **US4 (Idiomatic review)**: Best done after US1+US2 to review final state.
- **Script Updates**: Depends on US2 (old API must be removed first to validate scripts compile against new API).

### Within Each User Story

- .fsi signature updates before or alongside .fs implementation
- Implementation before tests that exercise new behavior
- Build verification after each phase

### Parallel Opportunities

- Phase 2: All 10 private qualifier removal tasks (T003–T012) can run in parallel
- Phase 3–4: US1 and US2 are sequential (US2 depends on Frames existing)
- Phase 5: All 9 script update tasks (T025, T026a–T026h) can run in parallel
- Phase 6: Review tasks (T027, T028) can run in parallel

---

## Parallel Example: Phase 2 (Private Qualifier Removal)

```bash
# All these tasks modify different files and can run simultaneously:
Task T003: "Remove private qualifiers in Callbacks.fs"
Task T004: "Remove private qualifiers in EngineDiscovery.fs"
Task T005: "Remove private qualifiers in EngineLauncher.fs"
Task T006: "Remove private qualifiers in MapGrid.fs"
Task T007: "Remove private qualifiers in MapCache.fs"
Task T008: "Remove private qualifiers in Protocol.fs"
Task T009: "Remove private qualifiers in Connection.fs"
Task T010: "Remove private qualifiers in Events.fs"
Task T011: "Remove private qualifiers in MapQuery.fs"
Task T012: "Remove private qualifiers in Commands.fs"
```

## Parallel Example: Phase 5 (Script Updates)

```bash
# All these tasks modify different files and can run simultaneously:
Task T025: "Update prelude.fsx"
Task T026a: "Update 01-hello-bar.fsx"
Task T026b: "Update 02-graphical-game.fsx"
Task T026c: "Update 04-step-by-step.fsx"
Task T026d: "Update 05-map-layers.fsx"
Task T026e: "Update 06-game-viz-basic.fsx"
Task T026f: "Update 07-game-viz-layers.fsx"
Task T026g: "Update Repl.fsx"
Task T026h: "Update ReplGraphical.fsx"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup verification
2. Complete Phase 2: Remove private qualifiers (all parallel)
3. Complete Phase 3: Add Frames property (US1)
4. Complete Phase 4: Add SendCommands + remove old API (US2)
5. **STOP and VALIDATE**: Build, test, verify stream API works end-to-end

### Full Delivery

1. MVP (above)
2. Phase 5: Update all scripts to new API (constitution §V compliance, all parallel)
3. Phase 6: Idiomatic review pass (US4)
4. Phase 7: Polish — regenerate baselines, full verification, performance check

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- `member private` on BarClient.CleanupResources is NOT removed (class members need explicit private)
- Reset method must be rewritten to use Protocol directly (not StepWith)
- Surface area baselines must be regenerated AFTER all API changes
- Downstream consumers (viz, scripts) are out of scope per clarification
