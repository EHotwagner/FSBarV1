# Feature Specification: Observable GameState API

**Feature Branch**: `017-observable-gamestate-api`  
**Created**: 2026-04-09  
**Status**: Draft  
**Input**: User description: "finish 016 and change streams to iobservable."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consume Game State as an IObservable Stream (Priority: P1)

A developer using FSBar.Client wants to receive game frames as an IObservable<GameFrame> stream, allowing them to subscribe with standard Rx-style patterns (subscribe, filter, map) and receive push-based notifications as each frame arrives from the engine. The observable replaces the current pull-based seq<GameFrame> approach, enabling multiple subscribers, composable pipelines, and natural integration with async/reactive workflows.

**Why this priority**: This is the core API change -- moving from pull-based seq to push-based IObservable enables richer composition, multiple subscribers (e.g., game logic + visualization simultaneously), and aligns with the reactive nature of game event processing.

**Independent Test**: Can be tested by connecting to a running engine, subscribing to the observable, and verifying that game frames arrive as push notifications with correct data.

**Acceptance Scenarios**:

1. **Given** a configured and connected FSBar session, **When** the developer subscribes to the game frame observable, **Then** they receive GameFrame values pushed as each frame arrives, without managing sockets, protocol, or polling.
2. **Given** the engine disconnects mid-session, **When** the observable encounters the disconnect, **Then** the observable completes (OnCompleted) and subscribers are notified cleanly.
3. **Given** a running game session, **When** multiple subscribers attach to the observable, **Then** each subscriber independently receives all frames.
4. **Given** a running game session, **When** the developer composes operators (filter, map, scan) on the observable, **Then** the operators compose naturally using standard IObservable combinators.

---

### User Story 2 - Centralized Game State Tracking (Priority: P1)

A developer wants the client to automatically maintain a centralized GameState that tracks all friendly units, known enemies, and economy -- updated each frame from the event stream. They should be able to query the current state at any point without writing event processing code.

**Why this priority**: Without centralized state tracking, every consumer (REPL, visualization, AI scripts) must duplicate event processing logic. Currently each consumer independently tracks units with duplicated code, and units existing before tracking starts are missed.

**Independent Test**: Start a game session, advance frames, and query the game state for all friendly units with positions, health, and idle status -- without writing any event processing code.

**Acceptance Scenarios**:

1. **Given** a new game session is started, **When** the first frame is processed, **Then** the game state contains the commander unit with correct position, health, and definition info.
2. **Given** a running game with tracked state, **When** a new unit is built and completes, **Then** the game state automatically includes the new unit with its current position and health.
3. **Given** a running game with tracked state, **When** a unit is destroyed, **Then** it is removed from the game state on the next frame.
4. **Given** a running game with tracked state, **When** the economy changes, **Then** metal and energy snapshots (current, income, usage, storage) are updated each frame.
5. **Given** a running game with tracked state, **When** an enemy enters line of sight, **Then** it appears in the enemies collection with position, health, and visibility flags.

---

### User Story 3 - Instant Unit Definition Lookup (Priority: P1)

A developer wants to look up unit definitions by name instantly so they can issue build commands without scanning hundreds of definitions over slow protocol round-trips.

**Why this priority**: Currently, finding the definition ID for a unit name requires hundreds of synchronous protocol calls that take minutes at low game speeds, blocking the entire workflow.

**Independent Test**: After game initialization, look up a unit by name and receive its definition ID, cost, build speed, and build options instantly.

**Acceptance Scenarios**:

1. **Given** a game session is initialized with tracked state, **When** a unit definition is looked up by name, **Then** the result is returned instantly without additional protocol calls.
2. **Given** a game session is initialized, **When** all unit definitions are queried, **Then** each definition includes name, cost, build speed, weapon range, and build options.
3. **Given** a game session is initialized, **When** a non-existent unit name is looked up, **Then** the result indicates the definition was not found.

---

### User Story 4 - Send Commands via a Separate Input Channel (Priority: P1)

A developer wants to send AI commands (move, attack, build, etc.) to the engine through a simple input mechanism that is decoupled from the game state observable. Commands are submitted per frame and the client handles serialization and delivery.

**Why this priority**: Separating command input from state output is essential for clean reactive architecture -- producers and consumers should be independent.

**Independent Test**: Can be tested by sending commands during a live game session and verifying the engine executes them.

**Acceptance Scenarios**:

1. **Given** a connected session producing frames, **When** the developer submits commands for the current frame, **Then** the client delivers them to the engine in the correct protocol format.
2. **Given** no commands are submitted for a frame, **When** the frame advances, **Then** the client sends an empty response and the game continues normally.
3. **Given** commands are submitted after the session has ended, **When** the submission is attempted, **Then** the client raises an error to make the bug visible immediately.

---

### User Story 5 - Permanent Queryable Map (Priority: P2)

A developer wants a persistent map representation that loads static data once and refreshes dynamic layers (visibility, radar) each frame, with query functions for metal spots and terrain.

**Why this priority**: The current map loads all layers on demand via expensive callbacks. There is no way to quickly query nearest resources or terrain passability without loading the full map and writing custom code.

**Independent Test**: After game initialization, query the nearest metal spot to a position and check terrain passability, getting results instantly from cached data.

**Acceptance Scenarios**:

1. **Given** a game session with a loaded map, **When** the nearest metal spot to a position is queried, **Then** the correct spot is returned with coordinates and richness value.
2. **Given** a game session with a loaded map, **When** terrain passability is checked for a unit type at given coordinates, **Then** the result correctly reflects traversability.
3. **Given** a game session with a loaded map, **When** frames advance, **Then** line-of-sight and radar layers are refreshed while static layers remain cached.
4. **Given** a game session, **When** the map is queried before any explicit load call, **Then** the map loads automatically on first access and caches for subsequent queries.

---

### User Story 6 - Idiomatic F# Code Patterns (Priority: P2)

A developer reading or extending FSBar.Client wants the code to use idiomatic F# patterns: records and discriminated unions for data modeling, pattern matching for control flow, no unnecessary private qualifiers in .fs files (relying on .fsi for visibility), and mutable code only where it provides clear performance benefits.

**Why this priority**: Consistency with F# idioms makes the codebase approachable and maintainable.

**Independent Test**: Code review confirms pattern usage, and the full test suite passes confirming behavioral equivalence.

**Acceptance Scenarios**:

1. **Given** the refactored codebase, **When** reviewed, **Then** all domain concepts use records or discriminated unions rather than classes (except where IDisposable or mutable session state requires it).
2. **Given** .fs files with redundant `private` qualifiers, **When** the qualifier is removed, **Then** the .fsi file continues to restrict the public API correctly.
3. **Given** the refactored code, **When** the full test suite runs, **Then** all existing tests pass (updated only to match the new API surface).

---

### Edge Cases

- What happens when the command input channel receives commands after the session has ended? The client MUST raise an error.
- What happens when the observable is subscribed to after the session has already started? Late subscribers receive frames from the point of subscription, not historical frames.
- How does the system handle units that existed before tracking started (e.g., commander at game start)? Pre-existing units are seeded into the game state at initialization.
- How does enemy tracking behave when an enemy leaves both LOS and radar? It is kept as stale intel (marked not visible) and only removed on explicit destroy events.
- What happens when the engine connection is lost during a tracked frame? The observable completes with OnCompleted and the game state retains its last known values.
- What happens when a unit definition lookup is attempted before the game state is initialized? An error is raised indicating the session is not ready.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The client library MUST expose game frames as an IObservable<GameFrame>, pushing each frame to subscribers as it arrives from the engine.
- **FR-002**: The observable MUST support multiple concurrent subscribers, each receiving all frames independently.
- **FR-003**: The observable MUST complete (OnCompleted) when the engine disconnects, allowing subscribers to detect session end.
- **FR-004**: The client library MUST accept commands through a separate input mechanism decoupled from the observable, accepting a list of commands per frame.
- **FR-005**: The client library MUST handle all protocol complexity (handshake, framing, serialization) internally.
- **FR-006**: The system MUST maintain a centralized GameState that tracks all friendly units with position, health, definition, and idle status.
- **FR-007**: The system MUST track known enemy units with position, health, definition, and visibility flags (in LOS, in radar).
- **FR-008**: The system MUST cache all unit definitions at initialization time, providing instant lookup by name or ID.
- **FR-009**: The system MUST update economy data (metal and energy: current, income, usage, storage) each frame.
- **FR-010**: The system MUST seed pre-existing units into the game state at initialization.
- **FR-011**: The system MUST cache static map layers (height, slope, resources) after first load and only refresh dynamic layers (LOS, radar) each frame.
- **FR-012**: The system MUST provide a nearest-metal-spot query from cached data.
- **FR-013**: The system MUST provide terrain passability checks for different unit movement types.
- **FR-014**: All `private` qualifiers on module-level bindings in .fs files MUST be removed where the corresponding .fsi signature file already restricts visibility.
- **FR-015**: Data types MUST use F# records and discriminated unions rather than classes, except where IDisposable or mutable session state requires it.
- **FR-016**: Mutable imperative code MUST be retained in performance-critical paths such as socket I/O, protocol parsing, and map grid operations.
- **FR-017**: The existing game state query functions (Callbacks module) MUST remain available as on-demand functions on the session object.
- **FR-018**: The refactored API MUST maintain behavioral equivalence -- all existing tests MUST pass (updated only to match the new API surface).

### Key Entities

- **GameFrame**: A record containing frame number and list of game events for that frame.
- **GameEvent**: A discriminated union representing all possible engine events (unit created, unit damaged, etc.).
- **GameState**: Central record holding frame number, team ID, friendly units, known enemies, economy, and cached unit definitions.
- **TrackedUnit**: A friendly unit with ID, definition, position, health, completion status, and idle status.
- **TrackedEnemy**: A known enemy unit with ID, definition, last-known position, health, and visibility flags.
- **UnitDefInfo**: Cached unit definition with ID, name, cost, build speed, weapon range, and build options.
- **EconomySnapshot**: Resource state for one resource type (current level, income rate, usage rate, storage capacity).
- **AICommand**: A type representing a command to send to the engine (move, attack, build, etc.).
- **Session**: The managed connection lifecycle encapsulating socket, protocol state, observable stream, and game state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can consume game frames by subscribing to an IObservable with no protocol or connection management code.
- **SC-002**: Multiple subscribers (e.g., game logic and visualization) can independently receive frames from the same session.
- **SC-003**: Commands are submitted through a channel independent of the observable, enabling clean separation of input and output.
- **SC-004**: Unit definition lookup by name completes in under 1 millisecond after initialization.
- **SC-005**: Pre-existing units appear in the game state after initialization without requiring consumer-side event processing.
- **SC-006**: Nearest metal spot queries return results in under 1 millisecond from cached data.
- **SC-007**: All existing tests pass after refactoring, confirming behavioral equivalence.
- **SC-008**: No regressions in game loop performance -- frame processing throughput remains equivalent to the current implementation.

## Assumptions

- The current pull-based seq<GameFrame> API will be replaced by IObservable<GameFrame>, not layered alongside it. Existing consumers will be updated.
- IObservable is used from the standard System namespace -- no external Rx library dependency is required for the core implementation. Consumers may optionally use System.Reactive for advanced operators.
- The observable internally runs a background thread that reads frames from the socket and pushes them to subscribers.
- Unit definition IDs are stable within a session. Definitions are loaded once at initialization and not refreshed.
- The one-time cost of loading all unit definitions at initialization (estimated ~2500 protocol round-trips) is acceptable.
- Enemy units that leave both LOS and radar are kept in state with stale positions (marked not visible), only removed on destroy events.
- FSBar.Client, its tests, and scripting files are in scope. Viz tools and other downstream consumers will be updated in follow-up work.
- Performance-critical sections (Connection, Protocol, MapGrid) may retain mutable imperative patterns.
