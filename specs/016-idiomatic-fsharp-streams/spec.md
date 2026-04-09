# Feature Specification: Idiomatic F# Streams Refactor

**Feature Branch**: `016-idiomatic-fsharp-streams`  
**Created**: 2026-04-09  
**Status**: Draft  
**Input**: User description: "simplify the code and make it more fsharp idiomatic. fs files dont need private qualifiers, fsi already handles that. sealed classes are not really a thing. records, Discriminated Unions and pattern matching are the biggest part with mutable imperative code wherever convinient and performance relevant. fsbar.client should handle all the performance related complexity and output a simple stream of gamestate and events. another stream should take the control input commands."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consume Game State as a Simple Stream (Priority: P1)

A developer using FSBar.Client wants to receive game state updates and events as a simple, pull-based stream (sequence or async sequence) without managing connection lifecycle, framing, or protocol details. They subscribe to the stream and receive structured game frames containing events, processing each one with standard F# sequence operations (map, filter, iter).

**Why this priority**: This is the core value proposition -- hiding all protocol/connection complexity behind a simple stream abstraction that F# developers already know how to use.

**Independent Test**: Can be tested by connecting to a running engine, consuming frames from the stream, and verifying that game events arrive in order with correct data.

**Acceptance Scenarios**:

1. **Given** a configured and connected FSBar session, **When** the developer iterates the game state stream, **Then** they receive GameFrame values containing frame number and event list without managing sockets, protocol, or buffering.
2. **Given** the engine disconnects mid-session, **When** the stream encounters the disconnect, **Then** the stream terminates cleanly and the consumer can detect the end-of-stream condition.
3. **Given** a running game session, **When** the developer applies standard sequence operations (filter, map) to the stream, **Then** the operations compose naturally without special adapters.

---

### User Story 2 - Send Commands via a Separate Input Channel (Priority: P1)

A developer wants to send AI commands (move, attack, build, etc.) to the engine through a simple input mechanism that is decoupled from the game state stream. Commands are submitted per frame and the client library handles serialization, framing, and delivery.

**Why this priority**: Separating command input from state output is essential for clean F# architecture -- it enables composable pipelines and makes the API intuitive.

**Independent Test**: Can be tested by sending commands through the input channel during a live game session and verifying the engine executes them (units move, buildings start, etc.).

**Acceptance Scenarios**:

1. **Given** a connected session producing game frames, **When** the developer submits a list of commands for the current frame, **Then** the client delivers them to the engine in the correct protocol format.
2. **Given** no commands are submitted for a frame, **When** the frame advances, **Then** the client sends an empty response and the game continues normally.
3. **Given** multiple command types (move, attack, build), **When** submitted together for a single frame, **Then** all commands are delivered and executed by the engine.

---

### User Story 3 - Remove Unnecessary Private Qualifiers from .fs Files (Priority: P2)

A developer reading the FSBar.Client source code wants the .fs implementation files to be clean and free of redundant `private` qualifiers on functions and values that are already hidden by the corresponding .fsi signature files.

**Why this priority**: Reduces visual noise in the codebase and follows the idiomatic F# convention where .fsi files are the primary visibility mechanism.

**Independent Test**: Can be tested by removing `private` qualifiers, building the project, and verifying that the .fsi signatures still restrict the public API correctly.

**Acceptance Scenarios**:

1. **Given** .fs files with `private` qualifiers on module-level functions, **When** the qualifier is removed, **Then** the .fsi file continues to hide those functions from external consumers.
2. **Given** the refactored code, **When** the full test suite runs, **Then** all existing tests pass without modification.

---

### User Story 4 - Ensure Idiomatic F# Patterns Throughout (Priority: P2)

A developer reviewing or extending FSBar.Client wants the code to use idiomatic F# patterns: records and discriminated unions for data modeling, pattern matching for control flow, and mutable imperative code only where it provides clear performance benefits in hot paths.

**Why this priority**: Consistency with F# idioms makes the codebase more approachable and maintainable for F# developers.

**Independent Test**: Can be tested by code review verifying pattern usage and by running the existing test suite to confirm behavioral equivalence.

**Acceptance Scenarios**:

1. **Given** the refactored codebase, **When** reviewed for data modeling, **Then** all domain concepts use records or discriminated unions rather than classes (except where IDisposable or mutable session state genuinely requires it).
2. **Given** performance-sensitive code paths (socket I/O, protocol parsing, map grid operations), **When** reviewed, **Then** mutable imperative patterns are used where they provide measurable benefit.
3. **Given** the refactored code, **When** the full test suite runs, **Then** all tests pass, confirming behavioral equivalence.

---

### Edge Cases

- What happens when the command input channel receives commands after the session has ended? The client MUST raise an error to make the bug visible immediately.
- What happens when the game state stream is iterated by multiple consumers? The stream should support a single consumer (the primary game loop).
- How does the stream handle the engine proxy startup delay before the first frame arrives? The stream blocks (or awaits) until the first frame is available.

## Clarifications

### Session 2026-04-09

- Q: How should the ~25 synchronous game state query functions (Callbacks module) fit into the new stream-based API? → A: Keep queries as on-demand functions available on the session object alongside the stream.
- Q: Should this feature include updating downstream consumers (viz tools) or just FSBar.Client? → A: FSBar.Client and its direct tests only; consumer updates are follow-up work.
- Q: Should commands sent after session end raise an error or be silently discarded? → A: Raise an error to make bugs visible immediately.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The client library MUST expose game state as a stream (sequence or async sequence) of GameFrame values, where each frame contains a frame number and a list of game events.
- **FR-002**: The client library MUST accept commands through a separate input mechanism decoupled from the game state stream, accepting a list of commands per frame.
- **FR-003**: The client library MUST handle all protocol complexity (handshake, framing, serialization, request IDs) internally, invisible to the consumer.
- **FR-004**: The client library MUST manage connection lifecycle (socket setup, teardown, error handling) internally.
- **FR-005**: All `private` qualifiers on module-level bindings in .fs files MUST be removed where the corresponding .fsi signature file already restricts visibility.
- **FR-006**: Data types MUST use F# records and discriminated unions rather than classes, except where IDisposable or mutable session lifecycle management requires a class.
- **FR-007**: Mutable imperative code MUST be retained (or introduced) in performance-critical paths such as socket I/O, protocol buffer parsing, and map grid operations.
- **FR-008**: The refactored API MUST maintain behavioral equivalence with the existing API -- all existing tests MUST continue to pass (with test code updated only to match the new API surface).
- **FR-009**: The stream MUST terminate cleanly when the engine disconnects, allowing the consumer to detect end-of-stream.
- **FR-010**: The existing game state query functions (Callbacks module: unit positions, health, resources, etc.) MUST remain available as on-demand functions on the session object, callable during frame processing alongside the stream.

### Key Entities

- **GameFrame**: A record containing frame number and list of game events for that frame.
- **GameEvent**: A discriminated union representing all possible engine events (unit created, unit damaged, etc.).
- **AICommand**: A record representing a command to send to the engine (move, attack, build, etc.).
- **Session**: The managed connection lifecycle encapsulating socket, protocol state, and engine process.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can consume game state by iterating a standard F# sequence/async sequence with no protocol or connection management code.
- **SC-002**: Commands are submitted through a channel that is independent of the game state stream, enabling clean separation of input and output.
- **SC-003**: Zero `private` qualifiers remain on module-level bindings in .fs files that have corresponding .fsi signature files (except where private is genuinely needed for a binding not in the .fsi).
- **SC-004**: All existing tests pass after refactoring, confirming behavioral equivalence.
- **SC-005**: The public API surface (as defined by .fsi files) is simpler -- fewer methods/types exposed compared to the current step-based handler API.
- **SC-006**: No regressions in game loop performance -- frame processing throughput remains equivalent to the current implementation.

## Assumptions

- The existing .fsi signature files comprehensively define the intended public API surface, making `private` qualifiers in .fs files redundant for module-level bindings.
- The current step-based API (`StepWith`, `Run`, `RunUntil`) will be replaced by the stream-based API, not layered alongside it.
- The stream abstraction will be synchronous (seq) or asynchronous (async seq / IAsyncEnumerable) based on what integrates best with the existing blocking socket I/O model.
- FSBar.Client, its direct tests (FSBar.Client.Tests), and scripting files (prelude.fsx, example scripts) are in scope. Viz tools and other downstream consumers will be updated in follow-up work.
- Performance-critical sections (Connection, Protocol, MapGrid) may retain or increase their use of mutable imperative patterns where beneficial.
