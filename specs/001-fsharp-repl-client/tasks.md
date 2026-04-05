# Tasks: F# REPL Client for BAR AI Orchestration

**Input**: Design documents from `/specs/001-fsharp-repl-client/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Included per constitution requirement (Section III: Test Evidence Is Mandatory).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create project structure, solution file, proto files, and build configuration

- [x] T001 Create solution file and directory structure per plan.md: `FSBarV1.sln`, `proto/`, `src/FSBar.Proto/`, `src/FSBar.Client/`, `src/FSBar.Client.Tests/`, `scripts/`, `scripts/examples/`
- [x] T002 Copy the 5 proto files from `/home/developer/projects/HighBarV2/proto/highbar/` to `proto/highbar/` (callbacks.proto, commands.proto, common.proto, events.proto, messages.proto)
- [x] T003 [P] Create `proto/buf.yaml` and `proto/buf.gen.yaml` for FsGrpc generation targeting `src/FSBar.Proto/Generated/`
- [x] T004 [P] Create `src/FSBar.Proto/FSBar.Proto.fsproj` with FsGrpc 1.0.6 and FsGrpc.Tools 0.6.3 package references, targeting net10.0
- [x] T005 [P] Create `nuget.config` at repo root with local NuGet source `/home/developer/.local/share/nuget-local/`
- [x] T006 [P] Create `src/FSBar.Client/FSBar.Client.fsproj` targeting net10.0 with project reference to FSBar.Proto and BarData NuGet package reference
- [x] T007 [P] Create `src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` targeting net10.0 with xUnit 2.9.x, Microsoft.NET.Test.Sdk, and project reference to FSBar.Client
- [x] T008 Add all projects to `FSBarV1.sln` and verify `dotnet build` succeeds with proto generation producing F# bindings in `src/FSBar.Proto/Generated/`
- [x] T009 [P] Update `.gitignore` to exclude `src/FSBar.Proto/Generated/` (auto-generated), `**/bin/`, `**/obj/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core protocol modules that ALL user stories depend on. Must complete before any story work.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T010 Verify FsGrpc proto generation produces correct F# types: build `src/FSBar.Proto/` and confirm generated files exist with correct DU types for `ProxyMessage`, `AIMessage`, `EngineEvent`, `AICommand`, `CallbackParam`, `CallbackResult` oneofs in `src/FSBar.Proto/Generated/`
- [x] T011 Implement `Connection` module in `src/FSBar.Client/Connection.fs` — Unix domain socket listener creation, accept connection, length-prefixed send/receive (4-byte LE header + protobuf payload). Reference: `HighBarV2/clients/fsharp/src/Client.fs` lines 22-41 for framing logic
- [x] T012 Create `src/FSBar.Client/Connection.fsi` signature file for Connection module
- [x] T013 Implement `Events` module in `src/FSBar.Client/Events.fs` — `GameEvent` discriminated union (28 variants) and `fromProto` conversion function from generated `EngineEvent` type. Reference: `HighBarV2/clients/fsharp/src/Events.fs` for all 28 variant mappings
- [x] T014 Create `src/FSBar.Client/Events.fsi` signature file for Events module
- [x] T015 [P] Implement `Commands` module in `src/FSBar.Client/Commands.fs` — typed command builder functions (MoveCommand, BuildCommand, AttackCommand, PatrolCommand, GuardCommand, StopCommand, RepairCommand, ReclaimUnitCommand, FightCommand, SelfDestructCommand, SetWantedMaxSpeedCommand, CustomCommand, SendTextMessageCommand, GiveMeResourceCommand, GiveMeNewUnitCommand, CallLuaRulesCommand, CallLuaUICommand). Reference: `HighBarV2/clients/fsharp/src/Commands.fs` for all 17 builders
- [x] T016 [P] Create `src/FSBar.Client/Commands.fsi` signature file for Commands module
- [x] T017 Implement `Protocol` module in `src/FSBar.Client/Protocol.fs` — handshake (receive ProxyMessage.Handshake, validate protocol version 1, send AIMessage.HandshakeResponse), single-frame exchange (receive Frame, send FrameResponse), callback request/response, shutdown detection. Uses Connection module for transport
- [x] T018 Create `src/FSBar.Client/Protocol.fsi` signature file for Protocol module
- [ ] T019 Write unit tests for Connection framing logic in `src/FSBar.Client.Tests/ProtocolTests.fs` — test length-prefixed encoding/decoding with known byte sequences
- [ ] T020 [P] Write unit tests for Events.fromProto conversion in `src/FSBar.Client.Tests/EventTests.fs` — test all 28 event variants map correctly from generated proto types to GameEvent DU
- [ ] T021 [P] Write unit tests for Commands builders in `src/FSBar.Client.Tests/CommandTests.fs` — test each builder produces correct protobuf AICommand with INTERNAL_ORDER flag (8u) and expected field values

**Checkpoint**: Foundation ready — protocol layer verified with unit tests. User story implementation can now begin.

---

## Phase 3: User Story 1 — Interactive BAR Session from FSI (Priority: P1) 🎯 MVP

**Goal**: Developer opens FSI, creates a BarClient, starts a headless game, receives events, sends commands, and queries game state interactively.

**Independent Test**: Instantiate client in FSI, verify headless engine starts, proxy connects, handshake completes, at least one frame with events received and command response sent.

### Implementation for User Story 1

- [x] T022 [P] [US1] Implement `EngineConfig` module in `src/FSBar.Client/EngineConfig.fs` — EngineMode DU (Headless | Graphical), EngineConfig record with all fields from data-model.md, `defaultConfig` function with defaults: Headless mode, socket `/tmp/fsbar-<guid>.sock`, map "Red Rock Desert v2", opponent NullAI, timeout 30000ms, game speed 100
- [x] T023 [P] [US1] Create `src/FSBar.Client/EngineConfig.fsi` signature file for EngineConfig module
- [x] T024 [US1] Implement `ScriptGenerator` module in `src/FSBar.Client/ScriptGenerator.fs` — generate game-setup.txt content from EngineConfig. Template must match HighBarV2 format: `[GAME]` block with GameType, MapName, `[MODOPTIONS]` (GameMode=3, deathmode=neverend, debugcommands=1:cheat|3:globallos, MinSpeed/MaxSpeed from config), `[PLAYER0]` spectator, `[AI0]` HighBarV2 with socket_path option, `[AI1]` opponent AI, `[TEAM0]`/`[TEAM1]` with sides, `[ALLYTEAM0]`/`[ALLYTEAM1]`. Reference: `HighBarV2/tests/fixtures/game-setup.txt`
- [x] T025 [US1] Create `src/FSBar.Client/ScriptGenerator.fsi` signature file for ScriptGenerator module
- [x] T026 [US1] Implement `EngineLauncher` module in `src/FSBar.Client/EngineLauncher.fs` — launch headless engine: create session dir in `/tmp/fsbar-<guid>/`, write game-setup.txt via ScriptGenerator, copy ArchiveCache20.lua from SPRING_DATADIR if available, launch `spring-headless` process with script as argument and HIGHBAR_SOCKET_PATH + SPRING_WRITEDIR env vars, write PID file, return Process handle. Reference: `HighBarV2/tests/fixtures/start-headless.sh` and `HighBarV2/tests/persistent/fsharp/PersistentHarness.fs` lines 316-360
- [x] T027 [US1] Add graceful shutdown to EngineLauncher — SIGTERM → wait 5s → SIGKILL, clean up socket file and PID file. Reference: `HighBarV2/tests/fixtures/stop-headless.sh`
- [x] T028 [US1] Create `src/FSBar.Client/EngineLauncher.fsi` signature file for EngineLauncher module
- [x] T029 [US1] Implement `Callbacks` module in `src/FSBar.Client/Callbacks.fs` — convenience functions wrapping Protocol.sendCallback for all engine queries: getMyTeam, getMyAllyTeam, getMapWidth, getMapHeight, getStartPos, getMetalSpots, getUnitPos, getUnitHealth, getUnitMaxHealth, getUnitDef, getUnitDefName, getBuildOptions, getMaxWeaponRange, getBuildSpeed, getUnitDefCost, getEconomyCurrent, getEconomyIncome, getEconomyUsage, getEconomyStorage, getUnitDefs. Reference: `HighBarV2/clients/fsharp/src/Client.fs` lines 131-370 for callback patterns
- [x] T030 [US1] Create `src/FSBar.Client/Callbacks.fsi` signature file for Callbacks module
- [x] T031 [US1] Implement `BarClient` type in `src/FSBar.Client/BarClient.fs` — SessionState DU (Idle|Starting|Connected|Running|Stopped|Error), HandshakeInfo record. BarClient class with: State property, Config property, Handshake property, Start method (create listening socket → launch engine via EngineLauncher → accept connection via Connection → handshake via Protocol → emit lifecycle console output → transition to Connected), Stop method (shutdown engine → clean up → transition to Stopped), IDisposable. Module functions: defaultConfig, startHeadless, startGraphical, create
- [x] T032 [US1] Create `src/FSBar.Client/BarClient.fsi` signature file for BarClient module
- [x] T033 [US1] Update `src/FSBar.Client/FSBar.Client.fsproj` with correct Compile item ordering: EngineConfig.fsi, EngineConfig.fs, Connection.fsi, Connection.fs, Protocol.fsi, Protocol.fs, Events.fsi, Events.fs, Commands.fsi, Commands.fs, ScriptGenerator.fsi, ScriptGenerator.fs, EngineLauncher.fsi, EngineLauncher.fs, Callbacks.fsi, Callbacks.fs, BarClient.fsi, BarClient.fs
- [x] T034 [US1] Write unit test for ScriptGenerator in `src/FSBar.Client.Tests/ScriptGeneratorTests.fs` — verify generated game-setup.txt contains correct socket path, map name, AI assignments, and MODOPTIONS
- [x] T035 [US1] Write integration test in `src/FSBar.Client.Tests/IntegrationTests.fs` — test BarClient.startHeadless() connects, receives Init event, sends a MoveCommand, and stops cleanly. Mark with `[Trait("Category", "Integration")]`
- [x] T036 [US1] Create `scripts/prelude.fsx` — `#r` references to FSBar.Proto.dll, FSBar.Client.dll, BarData.dll from packed output; `open` FSBar.Client, FSBar.Client.Commands, BarData. Must be loadable with single `#load "scripts/prelude.fsx"` from repo root
- [x] T037 [US1] Create `scripts/examples/01-hello-bar.fsx` — minimal example: load prelude, startHeadless, Step 5 frames printing events, Stop

**Checkpoint**: User Story 1 complete. Developer can `#load "scripts/prelude.fsx"` in FSI, start a headless game, receive events, send commands, and stop. MVP delivered.

---

## Phase 4: User Story 2 — Full (Graphical) Game Session (Priority: P2)

**Goal**: Developer launches a full graphical BAR game from FSI using the same client API.

**Independent Test**: Create BarClient with graphical mode, verify BAR AppImage launches windowed, proxy connects, events flow.

### Implementation for User Story 2

- [x] T038 [US2] Extend EngineLauncher in `src/FSBar.Client/EngineLauncher.fs` — add graphical launch mode: locate BAR AppImage at configured path, launch with `--window` flag and game-setup.txt script, set same env vars (HIGHBAR_SOCKET_PATH, SPRING_WRITEDIR). Handle AppImage extraction if needed
- [x] T039 [US2] Update `src/FSBar.Client/EngineLauncher.fsi` signature to include graphical launch
- [x] T040 [US2] Verify BarClient.startGraphical() works end-to-end — update `scripts/prelude.fsx` if needed to document graphical mode usage
- [x] T041 [US2] Create `scripts/examples/02-graphical-game.fsx` — example: load prelude, startGraphical, run 300 frames with empty handler, stop

**Checkpoint**: User Story 2 complete. Developer can launch graphical BAR and control AI from REPL while watching the game.

---

## Phase 5: User Story 3 — Access BAR Unit Data Library (Priority: P2)

**Goal**: Developer queries 953 BAR unit definitions from FSI, both offline and during active sessions.

**Independent Test**: Load library in FSI, query `BarData.AllUnits.all`, verify 953 units returned with correct stats, without starting any game.

### Implementation for User Story 3

- [x] T042 [US3] Verify BarData NuGet package is available — run `dotnet pack` in HighBarV2 `data/bar/` targeting local store, confirm `src/FSBar.Client/FSBar.Client.fsproj` resolves the BarData package reference and `dotnet build` succeeds
- [x] T043 [US3] Verify BarData types accessible from FSBar.Client — write a smoke test in `src/FSBar.Client.Tests/BarDataAccessTests.fs` that queries `BarData.AllUnits.all`, asserts count is 953, checks a known unit by name (e.g., "armcom" has isBuilder=true)
- [x] T044 [US3] Ensure `scripts/prelude.fsx` includes BarData.dll reference and `open BarData` so unit data is immediately available in FSI
- [x] T045 [US3] Create `scripts/examples/03-query-units.fsx` — example: load prelude, query all builders, query all armed units with range > 500, look up specific unit by name, print metal/energy costs

**Checkpoint**: User Story 3 complete. Developer can query all 953 unit definitions from FSI without a running game.

---

## Phase 6: User Story 4 — Game Lifecycle Management (Priority: P3)

**Goal**: Developer manages full game lifecycle from REPL: start, reset, stop, restart.

**Independent Test**: Start headless, run some frames, reset, verify engine alive, run more frames, stop, start again, stop.

### Implementation for User Story 4

- [x] T046 [US4] Implement `Reset` method on BarClient in `src/FSBar.Client/BarClient.fs` — send SendTextMessageCommand ".destroy <unitId>" for each known non-initial unit, send GiveMeResourceCommand to reset metal/energy, run verification frames. Reference: `HighBarV2/tests/persistent/fsharp/PersistentHarness.fs` lines 273-313 for reset pattern
- [x] T047 [US4] Implement restart capability on BarClient — after Stop, allow Start to create a new session (new socket, new engine process, fresh state). Ensure all previous resources are cleaned up
- [x] T048 [US4] Add engine crash detection to BarClient — monitor Process.HasExited during frame operations, detect socket disconnection, transition to Error state with diagnostic message including engine log tail. Reference: `HighBarV2/tests/persistent/fsharp/PersistentHarness.fs` lines 187-203 for crash diagnostics
- [x] T049 [US4] Update `src/FSBar.Client/BarClient.fsi` signature with Reset method
- [x] T050 [US4] Write integration test for lifecycle in `src/FSBar.Client.Tests/IntegrationTests.fs` — test start → run 10 frames → reset → run 10 frames → stop → start → run 10 frames → stop cycle. Mark with `[Trait("Category", "Integration")]`

**Checkpoint**: User Story 4 complete. Developer can do iterative development loops without restarting FSI.

---

## Phase 7: User Story 5 — Frame-by-Frame and Continuous Execution (Priority: P3)

**Goal**: Developer controls game execution pace — step one frame, run N frames, run until condition, cancel continuous loops.

**Independent Test**: Run exactly N frames and verify correct count of frame exchanges.

### Implementation for User Story 5

- [x] T051 [US5] Implement `Step` and `StepWith` methods on BarClient in `src/FSBar.Client/BarClient.fs` — Step receives one frame via Protocol, sends empty FrameResponse, returns GameFrame. StepWith takes handler `(GameFrame -> AICommand list)`, sends commands in FrameResponse
- [x] T052 [US5] Implement `Run` and `RunUntil` methods on BarClient in `src/FSBar.Client/BarClient.fs` — Run takes frame count and handler, calls StepWith N times, returns all frames. RunUntil takes predicate `(GameFrame -> bool)` and handler, stops when predicate returns true
- [x] T053 [US5] Add cancellation support to Run/RunUntil — accept optional `CancellationToken`, check between frames, stop loop gracefully after current frame completes
- [x] T054 [US5] Update `src/FSBar.Client/BarClient.fsi` signature with Step, StepWith, Run, RunUntil methods
- [x] T055 [US5] Write integration test for frame stepping in `src/FSBar.Client.Tests/IntegrationTests.fs` — test Step returns exactly 1 frame, Run(10, handler) returns exactly 10 frames, RunUntil stops at correct condition. Mark with `[Trait("Category", "Integration")]`
- [x] T056 [US5] Create `scripts/examples/04-step-by-step.fsx` — example: load prelude, start headless, Step 5 frames inspecting each, then Run 100 frames with a handler that prints unit events

**Checkpoint**: User Story 5 complete. Developer has full control over game execution pace from REPL.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Surface-area baselines, documentation, and final validation

- [x] T057 [P] Create surface-area baseline tests in `src/FSBar.Client.Tests/SurfaceAreaTests.fs` — snapshot public API surface of FSBar.Client (all public types and members), fail if surface changes without explicit baseline update. Per constitution Section II
- [x] T058 [P] Verify all `.fsi` files are complete and consistent with implementations — compile with `dotnet build` and ensure no public symbols leak beyond signatures
- [x] T059 [P] Configure `src/FSBar.Client/FSBar.Client.fsproj` for `dotnet pack` — add NuGet metadata (PackageId=FSBar.Client, Version, Description), verify `dotnet pack -o ~/.local/share/nuget-local/` succeeds. Per constitution requirement
- [x] T060 [P] Configure `src/FSBar.Proto/FSBar.Proto.fsproj` for `dotnet pack` — same NuGet metadata, verify packable
- [x] T061 [P] Write soak test in `src/FSBar.Client.Tests/IntegrationTests.fs` — run 1000+ continuous frames via BarClient.Run, assert no connection errors, no exceptions, and no significant memory growth. Mark with `[Trait("Category", "Integration")]`. Validates SC-005
- [x] T062 Run all unit tests: `dotnet test src/FSBar.Client.Tests/ --filter "Category!=Integration"`
- [x] T063 Run integration tests (requires headless engine): `dotnet test src/FSBar.Client.Tests/ --filter "Category=Integration"`
- [x] T064 Run quickstart.md validation — execute each code block from `specs/001-fsharp-repl-client/quickstart.md` and verify expected outputs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational — this is the MVP
- **US2 (Phase 4)**: Depends on US1 (extends EngineLauncher)
- **US3 (Phase 5)**: Depends on Setup only (BarData NuGet) — can parallel with US1
- **US4 (Phase 6)**: Depends on US1 (extends BarClient)
- **US5 (Phase 7)**: Depends on US1 (extends BarClient)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 — no other story dependencies. **MVP target.**
- **US2 (P2)**: Depends on US1 (extends EngineLauncher with graphical mode)
- **US3 (P2)**: Independent of other stories — only needs Phase 1 setup for BarData NuGet. **Can parallel with US1.**
- **US4 (P3)**: Depends on US1 (adds Reset, restart, crash detection to BarClient)
- **US5 (P3)**: Depends on US1 (adds Step/Run/RunUntil to BarClient)

### Within Each User Story

- .fsi signature files before or alongside .fs implementations
- Core modules before dependent modules (EngineConfig → ScriptGenerator → EngineLauncher → BarClient)
- Implementation before integration tests
- Integration tests require headless engine available

### Parallel Opportunities

- Phase 1: T003, T004, T005, T006, T007, T009 can all run in parallel
- Phase 2: T015/T016 (Commands) can parallel with T011/T012 (Connection) and T013/T014 (Events). T020, T021 tests can parallel
- Phase 3: T022/T023 (EngineConfig) can parallel with T024 (ScriptGenerator is sequential after)
- US3 (Phase 5) can run entirely in parallel with US1 (Phase 3)

---

## Parallel Example: Phase 2 Foundational

```text
# These can run in parallel (different files, no dependencies):
T011: Implement Connection module in src/FSBar.Client/Connection.fs
T013: Implement Events module in src/FSBar.Client/Events.fs
T015: Implement Commands module in src/FSBar.Client/Commands.fs

# After above complete:
T017: Implement Protocol module (depends on Connection, Events)

# Tests can parallel after their targets:
T020: Event conversion tests (after T013)
T021: Command builder tests (after T015)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T009)
2. Complete Phase 2: Foundational (T010-T021)
3. Complete Phase 3: User Story 1 (T022-T037)
4. **STOP and VALIDATE**: Run `scripts/examples/01-hello-bar.fsx` in FSI
5. Deploy MVP — developer can control headless BAR from REPL

### Incremental Delivery

1. Setup + Foundational → Protocol layer verified
2. Add US1 → Interactive headless sessions (MVP!)
3. Add US3 → Unit data queries (can parallel with US1)
4. Add US2 → Graphical game sessions
5. Add US4 → Lifecycle management (reset/restart)
6. Add US5 → Frame stepping control
7. Polish → Surface-area baselines, packaging, full validation

### Recommended Execution Order (Single Developer)

Phase 1 → Phase 2 → Phase 3 (MVP) → Phase 5 (US3, quick win) → Phase 4 (US2) → Phase 6 (US4) → Phase 7 (US5) → Phase 8 (Polish)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Constitution requires `.fsi` for all public modules — tasks include these explicitly
- Constitution requires test evidence — integration tests need `spring-headless` available
- HighBarV2 source paths are references only — no runtime dependency permitted
- All proto-generated code goes in FSBar.Proto, not FSBar.Client
