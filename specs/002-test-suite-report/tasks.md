# Tasks: Test Suite and Functionality Report

**Input**: Design documents from `/specs/002-test-suite-report/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Yes — this feature IS the test suite. All test tasks are required.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the test project and report output directory

- [X] T001 Register test files in `src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` by adding `<Compile Include="...">` entries for all 7 test files in dependency order
- [X] T002 Create report output directory at `reports/testreports/`

**Checkpoint**: Test project compiles with empty test files, report directory exists

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No foundational tasks needed — the test project and xUnit dependencies are already configured. Phase 1 setup is sufficient.

**Checkpoint**: Foundation ready — user story implementation can begin

---

## Phase 3: User Story 1 - Run Comprehensive Test Suite (Priority: P1) MVP

**Goal**: Create and run xUnit tests for all 7 major FSBar.Client modules, producing clear pass/fail results per module.

**Independent Test**: Run `dotnet test src/FSBar.Client.Tests/` and verify tests execute with pass/fail output for each module.

### Implementation for User Story 1

#### Pure Unit Test Modules (no external dependencies)

- [X] T003 [P] [US1] Create EngineConfig tests in `src/FSBar.Client.Tests/EngineConfigTests.fs`: test `defaultConfig` returns Headless mode with expected defaults, test custom config overrides, test both EngineMode variants (Headless/Graphical)
- [X] T004 [P] [US1] Create ScriptGenerator tests in `src/FSBar.Client.Tests/ScriptGeneratorTests.fs`: test `generate` with headless config produces valid script, test with graphical config, verify script contains map name/game type/AI settings
- [X] T005 [P] [US1] Create Commands tests in `src/FSBar.Client.Tests/CommandsTests.fs`: test each command constructor (MoveCommand, BuildCommand, AttackCommand, PatrolCommand, GuardCommand, StopCommand, RepairCommand, ReclaimUnitCommand, FightCommand, SelfDestructCommand, SetWantedMaxSpeedCommand, CustomCommand, SendTextMessageCommand, GiveMeResourceCommand, GiveMeNewUnitCommand, CallLuaRulesCommand, CallLuaUICommand) returns valid AICommand with correct parameters
- [X] T006 [P] [US1] Create Events tests in `src/FSBar.Client.Tests/EventsTests.fs`: test `fromProto` maps each EngineEvent variant to correct GameEvent DU case, test unknown event maps to GameEvent.Unknown

#### Stream-Dependent Modules (MemoryStream-based testing)

- [X] T007 [P] [US1] Create Connection tests in `src/FSBar.Client.Tests/ConnectionTests.fs`: test `sendMessage`/`recvBytes` round-trip via MemoryStream, verify length-prefix framing (4-byte big-endian header), test empty message handling
- [X] T008 [P] [US1] Create Protocol tests in `src/FSBar.Client.Tests/ProtocolTests.fs`: test handshake message parsing from byte stream, test `receiveFrame` deserializes protobuf frame correctly, test `sendFrameResponse` serializes commands, test frame with multiple events produces correct GameFrame

#### State Machine Module

- [X] T009 [US1] Create BarClient tests in `src/FSBar.Client.Tests/BarClientTests.fs`: test initial state is Idle after `create`, test config is accessible and matches provided config, test `create` with custom config preserves settings, test error handling on non-existent socket path, test Dispose cleanup

#### Run and Verify

- [X] T010 [US1] Run full test suite via `dotnet test src/FSBar.Client.Tests/ --logger "trx;LogFileName=testresults.trx" --logger "console;verbosity=detailed"` and capture results

**Checkpoint**: All tests run with clear pass/fail output. Tests not requiring external dependencies pass. Test results are available for report generation.

---

## Phase 4: User Story 2 - Generate Functionality Report (Priority: P2)

**Goal**: Produce a Markdown report in `/reports/testreports/` summarizing which modules work and which don't.

**Independent Test**: Verify report exists at `reports/testreports/test-report.md` with structured sections covering each module's results.

### Implementation for User Story 2

- [X] T011 [US2] Analyze test results from T010 output, classify each module as Working / Partially Working / Not Working / Not Testable based on pass/fail outcomes
- [X] T012 [US2] Write test report to `reports/testreports/test-report.md` following contract format: header (date, branch, environment), executive summary (total pass/fail/skip), module status table, per-module detail sections with individual test results, failure details with context, untestable areas (Callbacks, EngineLauncher)

**Checkpoint**: Report exists, accurately reflects test outcomes, developer can determine module status within 2 minutes of reading

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and cleanup

- [X] T013 Verify all test files are properly registered in `src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj`
- [X] T014 Run `dotnet build` to confirm clean build with no warnings in test project
- [X] T015 Validate report completeness against contract at `specs/002-test-suite-report/contracts/test-report-format.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **User Story 1 (Phase 3)**: Depends on Phase 1 (T001 must complete before test files can compile)
- **User Story 2 (Phase 4)**: Depends on T010 (needs test results to generate report)
- **Polish (Phase 5)**: Depends on all previous phases

### User Story Dependencies

- **User Story 1 (P1)**: Independent — can start after Phase 1
- **User Story 2 (P2)**: Depends on US1 completion (needs test results)

### Within User Story 1

- T003, T004, T005, T006, T007, T008 can all run in parallel (different files, no dependencies)
- T009 can run in parallel with the above (different file)
- T010 depends on ALL test files being written (T003-T009)

### Parallel Opportunities

```
Phase 1: T001 → T002 (sequential, T002 is independent but quick)

Phase 3 (US1): 
  ┌─ T003 (EngineConfigTests.fs)
  ├─ T004 (ScriptGeneratorTests.fs)
  ├─ T005 (CommandsTests.fs)
  ├─ T006 (EventsTests.fs)      ──all parallel──→ T010 (run tests)
  ├─ T007 (ConnectionTests.fs)
  ├─ T008 (ProtocolTests.fs)
  └─ T009 (BarClientTests.fs)

Phase 4 (US2): T011 → T012 (sequential)

Phase 5: T013, T014, T015 (sequential validation)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T002)
2. Complete Phase 3: Write all 7 test files in parallel (T003-T009)
3. Run tests (T010)
4. **STOP and VALIDATE**: All unit tests pass, integration-dependent tests are clearly marked
5. Proceed to US2 for report generation

### Incremental Delivery

1. Setup → Test files → Run tests → **MVP: Test suite works**
2. Analyze results → Generate report → **Complete: Report delivered**
3. Polish → Validate → **Done**

---

## Notes

- All 7 test files (T003-T009) can be written in parallel — they target different modules and different files
- Test naming convention: `<module>_<function>_<scenario>` for report grouping
- Callbacks and EngineLauncher are documented as "Not Testable" in the report (require live engine/connection)
- No .fsi files needed for test code (constitution principle II is N/A)
