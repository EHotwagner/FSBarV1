# Implementation Plan: Live Headless and Full Game Tests

**Branch**: `003-live-game-tests` | **Date**: 2026-04-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-live-game-tests/spec.md`

## Summary

Add live engine integration tests to FSBarV1 that exercise the full communication chain (spring-headless → C proxy → Unix socket → FSBar.Client) with automated assertions for connection, commands, and events. Include a graphical launch mode for manual visual validation and a unified test runner script with category-based filtering and prerequisite auto-detection.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: FSBar.Client (in-repo), FSBar.Proto (in-repo), BarData (NuGet), xUnit 2.9.x, Microsoft.NET.Test.Sdk
**Storage**: Filesystem only (temp dirs, socket files, log files, Markdown reports)
**Testing**: xUnit with IAsyncLifetime fixtures, dotnet test, bash test runner
**Target Platform**: Linux (BAR engine is Linux-only in this environment)
**Project Type**: Test infrastructure (new test project + shell scripts)
**Performance Goals**: Engine connection + handshake within 30 seconds; full integration suite completes within 2 minutes
**Constraints**: Requires spring-headless binary and BAR game data installed; graphical mode requires DISPLAY
**Scale/Scope**: ~3 test files (Connection, Commands, Events), 1 fixture, 1 prerequisite script, 1 test runner script

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec and plan artifacts exist; test code maps to user stories |
| II. Compiler-Enforced Structural Contracts | PASS | EngineFixture is test-only infrastructure — no public API surface changes to FSBar.Client. No new `.fsi` files needed (test project, not library) |
| III. Test Evidence Is Mandatory | PASS | This feature IS the test evidence — adds integration tests that validate FSBar.Client against the live engine |
| IV. Observability and Safe Failure | PASS | EngineFixture provides diagnostic log extraction on failure; prerequisite script reports clear errors; test runner generates summary reports |
| V. Scripting Accessibility | N/A | Test infrastructure does not expose a public API requiring FSI scripting |

**Post-design re-check**: All gates still pass. No public API changes, no new dependencies beyond xUnit (already used), no `.fsi` files required for test-only code.

## Project Structure

### Documentation (this feature)

```text
specs/003-live-game-tests/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Entity model
├── quickstart.md        # Setup and usage guide
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
tests/
├── FSBar.LiveTests/           # NEW: Live engine integration tests
│   ├── FSBar.LiveTests.fsproj # xUnit test project referencing FSBar.Client
│   ├── EngineFixture.fs       # Shared engine lifecycle (xUnit IAsyncLifetime + ICollectionFixture)
│   ├── ConnectionTests.fs     # Handshake, frame exchange, disconnect tests
│   ├── CommandTests.fs        # Move, build, stop command tests
│   └── EventTests.fs          # Init, UnitCreated, UnitFinished, combat event tests
├── check-prerequisites.sh     # NEW: Engine prerequisite validation script
├── engine-version.json        # NEW: Pinned engine/game/map versions
└── run-all.sh                 # NEW: Unified test runner script

reports/
└── testreports/               # NEW: Test report output directory (gitignored)

src/
├── FSBar.Client/              # UNCHANGED: Existing client library
├── FSBar.Client.Tests/        # UNCHANGED: Existing unit tests
└── FSBar.Proto/               # UNCHANGED: Generated protobuf bindings
```

**Structure Decision**: New `tests/` directory at repo root for live tests, separate from `src/FSBar.Client.Tests/` which contains fast unit tests. This mirrors HighBarV2's structure (`tests/integration/`, `tests/unit/` etc.) and keeps engine-dependent tests isolated.

## Complexity Tracking

No constitution violations — no complexity justification needed.

## Implementation Phases

### Phase 1: Engine Fixture + Connection Tests (P1 — Story 1, partial)

**Goal**: Establish the engine lifecycle fixture and basic connection validation.

1. Create `tests/engine-version.json` with pinned engine/game/map versions
2. Create `tests/check-prerequisites.sh` — validates engine binary, SPRING_DATADIR, game data, map
3. Create `tests/FSBar.LiveTests/FSBar.LiveTests.fsproj` — xUnit test project referencing FSBar.Client
4. Create `tests/FSBar.LiveTests/EngineFixture.fs`:
   - `EngineFixture` type implementing `IAsyncLifetime`
   - `InitializeAsync`: check prerequisites, create `BarClient` with headless config, call `Start()`, run 30 warm-up frames via `Step()`, capture `initialFrames` and `initialEvents`
   - `DisposeAsync`: call `client.Stop()`, verify cleanup
   - Expose: `Client`, `InitialFrames`, `InitialEvents`, `IsEngineAlive`, diagnostic helpers
   - `EngineCollection` collection definition with `ICollectionFixture<EngineFixture>`
5. Create `tests/FSBar.LiveTests/ConnectionTests.fs`:
   - `Harness smoke test — engine starts and socket is available`
   - `Client connects to engine proxy socket`
   - `Handshake completes with valid protocol metadata`
   - `First frame contains Init event`
   - `Empty command responses work for consecutive frames`
   - `Graceful disconnect after receiving frames`

**Verification**: `dotnet test tests/FSBar.LiveTests/ --filter "Category=Connection"` passes

### Phase 2: Command + Event Tests (P1 — Story 1, complete)

**Goal**: Validate command execution and event delivery against the live engine.

1. Create `tests/FSBar.LiveTests/CommandTests.fs`:
   - `MoveCommand causes unit to change position` — send MoveCommand to commander, run 35 frames
   - `BuildCommand triggers unit creation` — send BuildCommand, run 70 frames, check for UnitCreated
   - `StopCommand halts a moving unit` — send Move then Stop, verify no crash
   - `Patrol and combat commands accepted without crashing` — smoke test Guard, Attack, Patrol, Fight
2. Create `tests/FSBar.LiveTests/EventTests.fs`:
   - `Init event received with valid team ID` — from warm-up frames
   - `Update events received with matching frame numbers` — 5-frame run
   - `UnitCreated event received for builder unit` — from warm-up frames
   - `UnitFinished event received for commander` — lifecycle validation
   - `Unknown events do not crash the frame loop` — 10-frame resilience test

**Verification**: `dotnet test tests/FSBar.LiveTests/` — all tests pass

### Phase 3: Test Runner + Graphical Mode (P2 + P3 — Stories 2 & 3)

**Goal**: Unified test runner and graphical launch.

1. Create `tests/run-all.sh`:
   - Category support: `--category unit`, `--category integration`, `--graphical`
   - Default: run unit + integration (skip integration if engine unavailable)
   - Auto-detect engine via `check-prerequisites.sh`
   - Generate Markdown reports to `reports/testreports/`
   - Signal handling for clean interruption
2. Add `reports/testreports/` to `.gitignore`
3. Verify graphical mode: `./tests/run-all.sh --graphical` launches windowed BAR game with AI connected

**Verification**: `./tests/run-all.sh` completes with summary; `--graphical` launches game window
