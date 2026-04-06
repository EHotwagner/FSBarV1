# Tasks: Incorporate HighBarV2 Client and Test Fixes

**Input**: Design documents from `/specs/005-incorporate-highbarv2-fixes/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Existing map tests (MapGridTests, MapQueryTests) serve as verification. No new test tasks needed — the 11/12 currently failing tests are the acceptance criteria.

**Organization**: Tasks grouped by user story. US1 (EngineDisconnectedException) and US2 (read timeouts) are foundational for US3 (map test fixes).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: No project initialization needed — all target files exist. This phase is empty.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `EngineDisconnectedException` type must exist before any downstream code can reference it.

- [x] T001 Define `EngineDisconnectedException` type in `src/FSBar.Client/Connection.fs` — add before the `module Connection` declaration. Inherits `System.IO.IOException`, constructor takes `(message: string, ?lastFrameNumber: uint32, ?innerException: exn)`, exposes `LastFrameNumber: uint32 option` member.
- [x] T002 Update `src/FSBar.Client/Connection.fsi` — add `EngineDisconnectedException` type declaration before the `module Connection` section. Declare the constructor and `LastFrameNumber` member.

**Checkpoint**: `dotnet build src/FSBar.Client/` compiles with new exception type visible to all downstream modules.

---

## Phase 3: User Story 1 — Robust Disconnection Detection (Priority: P1) 🎯 MVP

**Goal**: Replace generic `failwith` in `readExact` with typed `EngineDisconnectedException` so disconnection errors are distinguishable from protocol errors.

**Independent Test**: Build succeeds; existing tests that trigger disconnection now receive `EngineDisconnectedException` instead of generic exceptions.

### Implementation for User Story 1

- [x] T003 [US1] Modify `readExact` in `src/FSBar.Client/Connection.fs` — wrap `stream.Read` call in try/catch for `System.IO.IOException`, raise `EngineDisconnectedException("Engine proxy read timeout", innerException = ex)`. Replace the `failwith "Connection closed while reading data"` with `raise (EngineDisconnectedException("Engine proxy closed connection"))`.
- [x] T004 [US1] Verify build: `dotnet build src/FSBar.Client/` compiles cleanly with the updated `readExact`.

**Checkpoint**: `readExact` raises typed exceptions. Downstream code can catch `EngineDisconnectedException` specifically.

---

## Phase 4: User Story 2 — Configurable Read Timeouts (Priority: P2)

**Goal**: Add configurable `NetworkStream.ReadTimeout` via explicit config, env var, or 10s default — preventing indefinite hangs.

**Independent Test**: A client created with default config applies a 10s read timeout to the stream. Setting `FSBAR_CLIENT_TIMEOUT_MS` overrides it.

### Implementation for User Story 2

- [x] T005 [P] [US2] Add `ReadTimeoutMs: int option` field to `EngineConfig` record in `src/FSBar.Client/EngineConfig.fs` — default `None` in `defaultConfig()`.
- [x] T006 [P] [US2] Update `src/FSBar.Client/EngineConfig.fsi` — add `ReadTimeoutMs: int option` to the record declaration.
- [x] T007 [US2] Add `resolveReadTimeout` helper to `EngineConfig` module in `src/FSBar.Client/EngineConfig.fs` — resolution chain: `config.ReadTimeoutMs |> Option.defaultWith (fun () -> match env var "FSBAR_CLIENT_TIMEOUT_MS" | valid int -> that | _ -> 10000)`.
- [x] T008 [US2] Update `src/FSBar.Client/EngineConfig.fsi` — declare `val resolveReadTimeout: config: EngineConfig -> int`.
- [x] T009 [US2] Modify `Connection.acceptConnection` in `src/FSBar.Client/Connection.fs` — add `readTimeoutMs: int` parameter, set `stream.ReadTimeout <- readTimeoutMs` on the `NetworkStream` before returning.
- [x] T010 [US2] Update `src/FSBar.Client/Connection.fsi` — add `readTimeoutMs: int` parameter to `acceptConnection` signature.
- [x] T011 [US2] Update `BarClient.Start()` in `src/FSBar.Client/BarClient.fs` — call `Connection.acceptConnection sock config.TimeoutMs (EngineConfig.resolveReadTimeout config)` passing the resolved read timeout. Update `src/FSBar.Client/BarClient.fsi` if signature changes (it shouldn't — `Start` signature is unchanged).
- [x] T012 [US2] Verify build: `dotnet build src/FSBar.Client/` compiles cleanly with timeout wired through.

**Checkpoint**: `NetworkStream.ReadTimeout` is set on every new connection. Reads that exceed timeout raise `IOException` → caught and wrapped as `EngineDisconnectedException` by T003.

---

## Phase 5: User Story 3 — Resilient Map Test Execution (Priority: P3)

**Goal**: Map tests catch disconnection errors and report skip instead of cascade-failing. Target: 0 failures (all pass or skip).

**Independent Test**: `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"` — all 12 tests pass or skip, none fail.

### Implementation for User Story 3

- [x] T013 [P] [US3] Update `tryLoadGrid()` in `tests/FSBar.LiveTests/MapGridTests.fs` — expand the `with` clause to catch `EngineDisconnectedException`, `IOException`, and the existing `"empty array"` pattern. Log a SKIP message via `output.WriteLine` and return `None` for each.
- [x] T014 [P] [US3] Update `tryLoadGrid()` in `tests/FSBar.LiveTests/MapQueryTests.fs` — same catch expansion as T013.
- [x] T015 [US3] Run map tests: `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery" --logger "console;verbosity=detailed"` — verify 0 failures.

**Checkpoint**: All 12 map tests complete without hanging or failing. Tests that cannot reach the proxy skip cleanly with diagnostic output.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [x] T016 Run full test suite: `dotnet test tests/FSBar.LiveTests/` — verify no regressions in non-map tests.
- [x] T017 Run quickstart.md validation steps to confirm end-to-end.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: Empty — skip.
- **Phase 2 (Foundational)**: T001, T002 — defines the exception type. BLOCKS all user stories.
- **Phase 3 (US1)**: T003–T004 — depends on Phase 2. Can run before US2.
- **Phase 4 (US2)**: T005–T012 — depends on Phase 2. Can run in parallel with US1 (different files) except T011 which modifies BarClient.fs.
- **Phase 5 (US3)**: T013–T015 — depends on Phase 2 (uses `EngineDisconnectedException` type in catch). Can be implemented independently of US1/US2 since it catches the base `IOException` as well.
- **Phase 6 (Polish)**: T016–T017 — depends on all previous phases.

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only. No cross-story dependencies.
- **US2 (P2)**: Depends on Phase 2 only. Enhances US1 (timeouts → IOException → EngineDisconnectedException) but not blocked by it.
- **US3 (P3)**: Depends on Phase 2 for the exception type. Works even without US1/US2 because it also catches base `IOException`.

### Parallel Opportunities

- T005 and T006 can run in parallel (different files: .fs and .fsi)
- T013 and T014 can run in parallel (different test files)
- US1 (T003) and US2 (T005–T010) touch different files except T011 (BarClient.fs)

---

## Parallel Example: User Story 2

```bash
# Launch .fsi and .fs config changes in parallel:
Task: "T005 Add ReadTimeoutMs to EngineConfig.fs"
Task: "T006 Add ReadTimeoutMs to EngineConfig.fsi"

# Then sequentially: T007 → T008 → T009 → T010 → T011 → T012
```

## Parallel Example: User Story 3

```bash
# Launch both test file fixes in parallel:
Task: "T013 Update tryLoadGrid() in MapGridTests.fs"
Task: "T014 Update tryLoadGrid() in MapQueryTests.fs"

# Then: T015 (run tests to verify)
```

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 3)

1. Complete Phase 2: Define EngineDisconnectedException
2. Complete Phase 3: Wire into readExact (US1)
3. Complete Phase 5: Fix map test catches (US3)
4. **STOP and VALIDATE**: Run map tests — should all pass/skip
5. This alone fixes the 11/12 test failures

### Incremental Delivery

1. Phase 2 → Exception type exists
2. Phase 3 (US1) → Disconnections are typed → Test independently
3. Phase 5 (US3) → Map tests resilient → Test independently (this is the user's primary goal)
4. Phase 4 (US2) → Timeouts prevent future hangs → Test independently
5. Phase 6 → Full validation

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- The 11/12 failing map tests are the primary acceptance criteria — 0 failures after implementation
- Constitution requires .fsi updates for all public API changes (T002, T006, T008, T010)
- No new test files needed — existing MapGridTests and MapQueryTests are the test evidence
