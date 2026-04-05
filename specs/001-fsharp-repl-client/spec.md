# Feature Specification: F# REPL Client for BAR AI Orchestration

**Feature Branch**: `001-fsharp-repl-client`  
**Created**: 2026-04-05  
**Status**: Draft  
**Input**: User description: "Create an independent F# client library to be used from REPL/FSI. It uses the existing data library in HighBar. It creates and orchestrates the BAR processes, headless and full game. In REPL you instantiate a client object. That client listens for BAR processes running our proxy AI. The client starts the BAR process and connects to the proxy AI. The client controls the AI through Unix socket connection."

## User Scenarios & Testing

### User Story 1 - Interactive BAR Session from FSI (Priority: P1)

A developer opens F# Interactive (FSI), loads the FSBarV1 library, and creates a client object. The client creates a Unix domain socket listener, launches a headless BAR engine process with the HighBar V2 proxy AI configured to connect to that socket, and establishes the protobuf connection. The developer can then send commands (move units, build structures, query game state) and receive game events interactively from the REPL prompt.

**Why this priority**: This is the core value proposition — enabling interactive, scriptable control of a BAR game from the F# REPL. Without this, the library has no purpose.

**Independent Test**: Can be tested by loading the library in FSI, instantiating a client, verifying the headless engine starts, the proxy connects, handshake completes, and at least one game frame with events is received and a command response sent.

**Acceptance Scenarios**:

1. **Given** the HighBar V2 proxy is installed and a headless engine is available, **When** the developer creates a `BarClient` in FSI and calls a start method, **Then** a headless BAR process starts, the proxy connects to the client's socket, handshake succeeds, and game events begin flowing.
2. **Given** an active game session, **When** the developer calls a command function (e.g., move a unit), **Then** the command is serialized and sent to the proxy, and the unit responds in-game.
3. **Given** an active game session, **When** the developer queries game state (e.g., list own units, check resources), **Then** the client returns the current state derived from received events and callback queries.

---

### User Story 2 - Full (Graphical) Game Session from FSI (Priority: P2)

A developer launches a full graphical BAR game instead of headless, using the same client API. The client configures and launches the BAR AppImage in windowed mode, with the proxy AI assigned to a team. The developer observes the game visually while controlling the AI from the REPL.

**Why this priority**: Visual game sessions are essential for debugging AI behavior and observing the effects of commands, but the core communication protocol is the same as headless.

**Independent Test**: Can be tested by instantiating the client with a "graphical" mode option, verifying the AppImage process launches in windowed mode, and confirming the proxy connects and events flow.

**Acceptance Scenarios**:

1. **Given** the BAR AppImage is available, **When** the developer creates a `BarClient` with graphical mode enabled, **Then** the BAR application launches in windowed mode with the proxy AI configured.
2. **Given** a running graphical game session, **When** the developer issues commands from the REPL, **Then** the effects are visible in the game window.

---

### User Story 3 - Access BAR Unit Data Library (Priority: P2)

A developer uses the BarData library (from HighBarV2) to query the 953 BAR unit definitions — stats, weapons, economy data, capabilities — from the REPL. This data is available both offline (without a running game) and during an active session for AI decision-making.

**Why this priority**: The data library provides the knowledge base that makes AI decisions meaningful. It is a direct dependency reuse from the sister project.

**Independent Test**: Can be tested by loading the library in FSI and querying unit data (e.g., get all builder units, look up a unit's metal cost) without starting any game process.

**Acceptance Scenarios**:

1. **Given** the library is loaded in FSI, **When** the developer queries the unit data (e.g., `BarData.AllUnits.all`), **Then** the full list of 953 unit definitions is returned with correct stats.
2. **Given** an active game session, **When** the developer looks up a unit definition by name to inform a build command, **Then** the data matches the engine's runtime data for that unit.

---

### User Story 4 - Game Lifecycle Management (Priority: P3)

A developer manages the full lifecycle of BAR game sessions from the REPL: starting games, resetting game state (via cheat commands), stopping engine processes, and starting new games — all without leaving FSI.

**Why this priority**: Lifecycle management enables iterative development loops (start game, test AI logic, reset, tweak, repeat) which is the primary workflow for AI development.

**Independent Test**: Can be tested by starting a headless session, running some frames, resetting state, verifying the engine is still alive, then cleanly stopping it.

**Acceptance Scenarios**:

1. **Given** an active game session, **When** the developer calls a reset method, **Then** all spawned units are destroyed, resources are reset, and the game continues from a clean state.
2. **Given** an active game session, **When** the developer calls a stop method, **Then** the engine process is terminated gracefully, the socket is cleaned up, and the client returns to an idle state.
3. **Given** a stopped client, **When** the developer calls start again, **Then** a new game session begins successfully.

---

### User Story 5 - Frame-by-Frame and Continuous Execution Modes (Priority: P3)

A developer controls the game execution pace from the REPL. They can step through individual frames (receiving events and choosing commands per frame), or run the game continuously with a registered handler function.

**Why this priority**: Frame-by-frame stepping is critical for debugging and understanding game dynamics. Continuous mode is needed for longer runs and performance testing.

**Independent Test**: Can be tested by running exactly N frames and verifying the correct number of frame responses were exchanged.

**Acceptance Scenarios**:

1. **Given** an active game session, **When** the developer calls a "step" function, **Then** exactly one frame is received and the developer can inspect events before sending commands.
2. **Given** an active game session, **When** the developer calls a "run" function with a handler and a frame count, **Then** the specified number of frames execute with the handler processing each frame.
3. **Given** a running continuous loop, **When** the developer signals stop (e.g., via cancellation), **Then** the loop stops after the current frame completes.

---

### Edge Cases

- What happens when the engine process crashes during a session? The client detects this via process exit monitoring and socket disconnection, and reports it clearly rather than hanging.
- What happens when the proxy does not connect within the timeout? The client cleans up the listening socket and engine process, and reports a timeout error.
- What happens when the user tries to send commands without an active session? The client raises a clear error indicating no game is running.
- What happens when the socket path already exists (stale socket)? The client cleans up the stale file before binding.
- What happens when the BAR AppImage or headless engine binary is not found? The client reports a clear prerequisite error with actionable guidance.

## Requirements

### Functional Requirements

- **FR-001**: The library MUST provide a client object that can be instantiated from FSI with a single constructor call, accepting optional configuration parameters.
- **FR-002**: The client MUST create a Unix domain socket listener, launch a BAR engine process (headless or graphical), and accept the proxy's incoming connection.
- **FR-003**: The client MUST perform the HighBar V2 protobuf handshake with the proxy, validating protocol version compatibility.
- **FR-004**: The client MUST support receiving game frames containing 28 event types and sending command responses using the full AICommand protocol (42+ command types), with typed builder functions for the 17 most common commands.
- **FR-005**: The client MUST support headless mode (using `spring-headless`) and graphical mode (using the BAR AppImage in windowed mode).
- **FR-006**: The client MUST expose the BarData unit data library (953 unit definitions) for offline and in-session unit lookups.
- **FR-007**: The client MUST support frame-by-frame stepping (receive one frame, return events, accept commands) and continuous execution (run N frames or indefinitely with a handler function).
- **FR-008**: The client MUST support game state reset via cheat commands (destroy units, reset resources) without restarting the engine.
- **FR-009**: The client MUST support clean shutdown — terminating the engine process gracefully, cleaning up sockets and PID files.
- **FR-010**: The client MUST expose engine callbacks (query map dimensions, unit positions, economy state, unit definitions) through the proxy's callback protocol.
- **FR-011**: The client MUST detect engine process crashes and socket disconnections, reporting them as actionable errors rather than hanging.
- **FR-012**: The client MUST emit console output for key lifecycle events (connecting, handshake, engine started, engine stopped, errors). Per-frame traffic MUST NOT produce console output by default.
- **FR-013**: The client MUST support configurable socket paths, timeouts, map names, and game mode options.
- **FR-014**: The library MUST generate native F# protobuf bindings from the HighBarV2 `.proto` schema files using the `fsgrpc-proto` skill. No runtime dependency on HighBar.Client or HighBar.Proto is permitted.
- **FR-015**: The library MUST consume the BarData library as a pre-packed NuGet package from the local NuGet store (`~/.local/share/nuget-local/`).
- **FR-016**: The library MUST provide a prelude script (`scripts/prelude.fsx`) loadable with a single `#load` directive for FSI use.
- **FR-017**: The library MUST generate and manage engine start scripts (game-setup.txt format) with configurable map, factions, socket path, and AI assignments. The default opponent MUST be NullAI (passive).

### Key Entities

- **BarClient**: The primary client object. Manages engine lifecycle, socket connection, and protocol communication. Stateful — tracks session state (idle, starting, connected, running, stopped).
- **GameSession**: Represents an active game connection — wraps the socket, stream, and frame loop. Provides methods for stepping frames, running continuously, querying state, and sending commands.
- **EngineConfig**: Configuration for launching the engine — mode (headless/graphical), map name, factions, socket path, timeouts, engine binary paths.
- **GameFrame**: A single frame received from the proxy, containing a frame number and a list of typed game events.
- **GameEvent**: Discriminated union of the 28 engine event types (Init, UnitCreated, UnitIdle, EnemyEnterLOS, etc.).
- **AICommand**: Protobuf command messages sent to the proxy (Move, Build, Attack, Patrol, Guard, Stop, etc.).

## Success Criteria

### Measurable Outcomes

- **SC-001**: A developer can go from opening FSI to receiving the first game event in under 30 seconds (headless mode).
- **SC-002**: The client successfully connects to the proxy and completes handshake on the first attempt in at least 95% of launch attempts.
- **SC-003**: The library supports all 28 engine event types and all 17 command types from the HighBar V2 protocol without data loss.
- **SC-004**: Frame-by-frame stepping allows the developer to inspect every event and choose commands per-frame with no dropped frames.
- **SC-005**: The client can run 1000+ continuous frames without connection errors or memory growth.
- **SC-006**: Engine crashes and disconnections are detected and reported within 5 seconds, with no hung processes or leaked sockets.
- **SC-007**: All 953 BarData unit definitions are accessible from FSI without a running game session.
- **SC-008**: A complete start-play-reset-play-stop cycle completes without errors or resource leaks.

## Clarifications

### Session 2026-04-05

- Q: How should FSBarV1 consume HighBarV2 libraries (dependency strategy)? → A: Pure F# reimplementation. Use the `fsgrpc-proto` skill (from fsGRPCSkills) to generate native F# protobuf bindings from HighBarV2's `.proto` schema files. Reimplement the socket communication and protocol handling in pure F#, using HighBar.Client source code as a behavioral reference only — no runtime dependency on HighBar.Client or HighBar.Proto. The BarData library is consumed as a pre-packed NuGet package from the local store.
- Q: What should the default opponent AI be in generated game scenarios? → A: NullAI (passive, do-nothing opponent). Provides a safe, predictable environment for iterative REPL development.
- Q: What diagnostic output should the client produce during normal operation? → A: Lifecycle events to console (connecting, handshake, engine started/stopped, errors). No per-frame output to keep the REPL clean.

## Assumptions

- The HighBarV2 proxy AI (`libSkirmishAI.so`) is pre-built and installed into the BAR engine's Skirmish AI directory. This library does not build the C proxy.
- The BarData library from HighBarV2 is consumed as a pre-packed NuGet package from the local NuGet store. The data generation pipeline is not part of this project.
- The BAR AppImage (`Beyond-All-Reason-*.AppImage`) is available at a known path for graphical mode. The headless engine binary (`spring-headless`) is available on PATH or at a configured path.
- The game scenario configuration follows the same format as HighBarV2's `game-setup.txt` template, with substitutable socket path, map name, and game type fields.
- The Unix domain socket path defaults to `/tmp/fsbar-<guid>.sock` to avoid collisions with HighBarV2 test sessions.
- This library targets .NET 10.0 to match the HighBarV2 ecosystem.
- The HighBar.Client source code (in HighBarV2 `clients/fsharp/`) is used as a behavioral reference for protocol implementation. FSBarV1 reimplements the protocol in pure F# with no runtime dependency on HighBar.Client or HighBar.Proto.
- The HighBarV2 `.proto` schema files (in HighBarV2 `proto/highbar/`) are the source of truth for protocol definitions. F# bindings are generated using the `fsgrpc-proto` skill from fsGRPCSkills.
