# Research: 003-live-game-tests

**Date**: 2026-04-05

## R1: Engine Fixture Pattern for xUnit

**Decision**: Use xUnit `IAsyncLifetime` + `ICollectionFixture<T>` pattern (same as HighBarV2).

**Rationale**: FSBarV1's `BarClient` already handles the full engine lifecycle (Start → Step/StepWith/Run → Stop). The fixture wraps BarClient, starts it in `InitializeAsync`, captures warm-up frames, and disposes in `DisposeAsync`. A single `[CollectionDefinition("Engine")]` shares the instance across all integration test classes.

**Alternatives considered**:
- Per-test engine instance — rejected due to ~10s startup cost per engine launch
- Class fixture (`IClassFixture<T>`) — rejected because it limits sharing to a single test class

## R2: Engine Binary Auto-Detection

**Decision**: Reuse the detection logic already in `EngineLauncher.fs` (searches `which spring-headless`, derives SPRING_DATADIR from engine binary grandparent). Add a prerequisite check script (`tests/check-prerequisites.sh`) that validates engine binary, game data, and map availability before running tests.

**Rationale**: HighBarV2's `check-prerequisites.sh` pattern works well — returns JSON with pass/fail per check, exit code 0/1/2. The FSBarV1 `EngineConfig.defaultConfig()` already has the correct engine binary name and game type hardcoded.

**Alternatives considered**:
- Embed prerequisite checks in F# test code — rejected because shell script enables use from the test runner script too
- Environment variable only — rejected because auto-detection from PATH is more ergonomic

## R3: Test Project Structure

**Decision**: Create a new test project `tests/FSBar.LiveTests/` separate from the existing `src/FSBar.Client.Tests/` unit tests. The live tests require engine infrastructure and have much longer execution times.

**Rationale**: Separation allows the existing unit tests to run instantly without engine dependencies. The unified test runner can invoke each project independently by category.

**Alternatives considered**:
- Add live tests to existing `FSBar.Client.Tests` project with trait-based filtering — rejected because it couples fast unit tests with slow integration tests and makes the test runner more complex

## R4: Warm-Up Frame Capture

**Decision**: Capture 30 warm-up frames during fixture initialization (matching HighBarV2). These frames contain one-time events (Init, UnitCreated for commander, UnitFinished) that all tests reference.

**Rationale**: The BAR engine emits Init and initial unit spawn events only once. Capturing them during warm-up ensures all tests can assert on them without racing the engine. 30 frames is sufficient for the commander to fully spawn.

**Alternatives considered**:
- No warm-up, each test captures its own events — rejected because Init/UnitCreated would only be visible to the first test that runs

## R5: Graphical Launch Implementation

**Decision**: Graphical launch uses `BarClient.startGraphical()` which already uses the AppImage path from `EngineConfig`. The test runner script provides a `--graphical` flag that invokes this mode. No automated assertions — the developer observes the game window.

**Rationale**: The `EngineLauncher.launchGraphical` function already handles AppImage launch with SPRING_DATADIR auto-detection. The graphical mode is for manual validation only (per clarification).

**Alternatives considered**:
- Separate graphical launch script independent of F# client — rejected because the BarClient already handles the full lifecycle cleanly

## R6: Unified Test Runner

**Decision**: Create `tests/run-all.sh` following HighBarV2's pattern — supports `--category` (unit, integration, graphical), auto-detects engine prerequisites, generates Markdown summary reports to `reports/testreports/`.

**Rationale**: HighBarV2's test runner is battle-tested with tier-based execution, pass/fail parsing from `dotnet test` output, and clean skip behavior when prerequisites are missing.

**Alternatives considered**:
- Makefile-based — rejected because shell script is more portable and follows established project pattern
- `dotnet test` solution-wide with filters — rejected because it can't handle the graphical launch mode or clean prerequisite skipping
