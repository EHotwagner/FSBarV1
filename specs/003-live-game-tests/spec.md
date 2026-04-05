# Feature Specification: Live Headless and Full Game Tests

**Feature Branch**: `003-live-game-tests`  
**Created**: 2026-04-05  
**Status**: Draft  
**Input**: User description: "create and test live headless and full game tests."

## Clarifications

### Session 2026-04-05

- Q: What should the headless integration test scope cover? → A: Connection + commands + events — test sending build/move commands and validating game events (e.g., UnitCreated after build order).
- Q: Should the graphical test be a manual observation tool or include automated assertions? → A: Manual only — launches the game for developer visual validation, no automated assertions.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Headless Engine Test Suite (Priority: P1)

As a developer, I want to run automated integration tests against a live headless BAR engine instance so that I can verify the FSBar.Client library works end-to-end with the real game engine — without needing a display or GPU rendering.

**Why this priority**: Headless tests are the foundation for CI and fast developer feedback. They validate the complete communication chain (engine -> C proxy -> Unix socket -> F# client) without external dependencies like a display server.

**Independent Test**: Can be fully tested by running `dotnet test` on the headless test project — launches spring-headless, connects via socket, exchanges frames, and validates responses. Delivers confidence that the client works against the real engine.

**Acceptance Scenarios**:

1. **Given** the headless engine prerequisites are met (spring-headless binary, BAR game data, maps installed), **When** the test suite runs, **Then** the engine starts, the proxy connects to the client socket, and the handshake completes within 30 seconds.
2. **Given** a connected headless session, **When** the test harness runs frame processing, **Then** the first frames contain an Init event and UnitCreated events for the commander unit.
3. **Given** a connected headless session, **When** multiple frames are processed with empty command responses, **Then** frame numbers increment monotonically and no errors occur.
4. **Given** a connected headless session, **When** a build command is sent (e.g., build a structure via the commander), **Then** subsequent frames contain a UnitCreated event for the ordered unit.
5. **Given** a connected headless session, **When** a move command is sent to a unit, **Then** subsequent frames reflect the unit's position changing toward the target.
6. **Given** a connected headless session, **When** all tests complete, **Then** the engine process is cleanly shut down and temporary files (socket, PID file, session dir) are removed.

---

### User Story 2 - Full Graphical Game Test (Priority: P2)

As a developer, I want to launch a full graphical BAR game session with the AI client connected so that I can visually observe and validate game behavior, AI unit commands, and rendering — using the installed BAR AppImage.

**Why this priority**: Visual validation is essential for debugging AI behavior, verifying unit movement and combat, and confirming the engine renders correctly with GPU passthrough. It complements the headless tests but requires a display.

**Independent Test**: Can be tested by running a graphical launch command — starts the BAR AppImage (windowed mode), connects the AI client, and keeps the game running until the user stops it. Delivers visual confirmation of the full stack.

**Acceptance Scenarios**:

1. **Given** a display is available (DISPLAY env var set) and the BAR AppImage is installed, **When** the graphical test is launched, **Then** the game window opens in windowed mode with the AI playing on one team against NullAI.
2. **Given** a running graphical game session, **When** the F# client is connected, **Then** the client can receive frames and send commands in real time while the game renders.
3. **Given** a running graphical session, **When** the user presses Ctrl+C or the game ends, **Then** the engine shuts down cleanly and resources are released.

---

### User Story 3 - Unified Test Runner (Priority: P3)

As a developer, I want a single entry point to run all test categories (unit, headless integration, graphical) so that I can quickly validate the entire project with one command, with engine-dependent tests automatically skipped when prerequisites are not met.

**Why this priority**: A unified runner reduces friction and prevents developers from forgetting to run certain test categories. Auto-detection of engine availability avoids noisy failures in environments without the engine installed.

**Independent Test**: Can be tested by running the unified test script with different category flags — verifies that each category runs the correct tests and that missing prerequisites result in clean skips rather than failures.

**Acceptance Scenarios**:

1. **Given** the test runner is invoked with no arguments, **When** the engine is available, **Then** all non-graphical tests run (unit + headless integration) and a summary report is generated.
2. **Given** the test runner is invoked with no arguments, **When** the engine is NOT available, **Then** unit tests still run and engine-dependent tests are skipped with a clear message.
3. **Given** the test runner is invoked with `--category integration`, **When** the engine is available, **Then** only headless integration tests run.
4. **Given** the test runner is invoked with `--graphical`, **When** a display is available, **Then** the graphical game session launches.

---

### Edge Cases

- What happens when the engine binary is not found or is an unexpected version?
- How does the system handle the engine crashing mid-test (proxy disconnect)?
- What happens when the socket path already exists from a previous aborted run?
- How does the system handle timeout when the proxy never connects (engine startup failure)?
- What happens when SPRING_DATADIR cannot be auto-detected?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST launch the spring-headless engine binary and accept a proxy connection over a Unix domain socket within a configurable timeout.
- **FR-002**: System MUST perform a protobuf handshake with the proxy and validate protocol version and team ID before proceeding with tests.
- **FR-003**: System MUST capture initial warm-up frames (including Init and UnitCreated events) and make them available to all tests in the suite.
- **FR-004**: System MUST share a single engine instance across all headless tests in a collection to avoid the cost of restarting the engine per test.
- **FR-005**: System MUST cleanly shut down the engine process, remove socket files, PID files, and session directories after tests complete (including on failure or interruption).
- **FR-006**: System MUST support launching the full graphical engine (BAR AppImage) in windowed mode with the AI connected for manual visual validation by the developer (no automated assertions).
- **FR-007**: System MUST auto-detect engine prerequisites (binary location, SPRING_DATADIR, game data, maps) and report clear errors when prerequisites are missing.
- **FR-008**: System MUST provide a unified test runner that supports category-based filtering (unit, integration, graphical) and generates a summary report.
- **FR-009**: System MUST detect stale socket files from previous runs and clean them up before starting a new session.
- **FR-010**: System MUST provide diagnostic output (engine logs, stderr) when tests fail due to engine issues.

### Key Entities

- **EngineFixture**: Manages the lifecycle of a headless engine instance — creates socket, starts engine, accepts connection, performs handshake, captures warm-up frames, and tears down cleanly. Shared across all integration tests via xUnit collection fixture.
- **Test Runner Script**: Orchestrates test categories, auto-detects engine prerequisites, runs dotnet test for each category, and generates summary reports.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Headless integration tests connect to the live engine and complete a full handshake + frame exchange within 30 seconds of engine startup.
- **SC-002**: All headless integration tests run against a single shared engine instance (no per-test engine restarts).
- **SC-003**: The test suite correctly auto-detects whether engine prerequisites are met and skips engine tests cleanly when they are not.
- **SC-004**: Engine cleanup is complete after test runs — no orphaned engine processes, no leftover socket files, no leftover session directories.
- **SC-005**: Graphical game launch opens a windowed BAR game with the AI actively connected and processing frames.
- **SC-006**: The unified test runner produces a human-readable summary showing pass/fail/skip counts per category.

## Assumptions

- The BAR (Beyond All Reason) game engine and content are installed in this environment (spring-headless binary, game data, maps).
- The BAR AppImage is available at `/home/developer/applications/Beyond-All-Reason-1.2988.0.AppImage` for graphical tests.
- GPU passthrough is enabled for graphical tests.
- The HighBarV2 C proxy (libSkirmishAI.so) is already built and deployed to the engine's AI directory.
- The FSBar.Client library (BarClient, Connection, Protocol, etc.) is functional and can communicate with the proxy over Unix domain sockets.
- Tests follow HighBarV2's proven patterns: xUnit collection fixtures for shared engine lifecycle, frame-based test assertions, and script-based test runner orchestration.
- The existing unit tests in FSBar.Client.Tests remain unchanged; live tests are additive.
