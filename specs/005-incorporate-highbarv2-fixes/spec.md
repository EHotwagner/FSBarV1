# Feature Specification: Incorporate HighBarV2 Client and Test Fixes

**Feature Branch**: `005-incorporate-highbarv2-fixes`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "read up on the fixes in the last commits in HighBarV2 and incorporate anything relevant. also rerun the map tests that should be fixed."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Robust Disconnection Detection (Priority: P1)

When the game engine disconnects unexpectedly (crash, timeout, socket close), the client should report a clear, typed error rather than a generic I/O exception or hang.

**Why this priority**: Socket hangs and ambiguous errors are the most disruptive failure mode — they stall tests and provide no diagnostic information. HighBarV2 solved this with a dedicated `EngineDisconnectedException`.

**Independent Test**: Can be tested by simulating a disconnection mid-frame and verifying the exception type and message contain frame context.

**Acceptance Scenarios**:

1. **Given** a connected client reading frames, **When** the engine closes the socket mid-read, **Then** an `EngineDisconnectedException` is raised with a descriptive message including the last frame number.
2. **Given** a connected client waiting for data, **When** the read exceeds the configured timeout, **Then** an `EngineDisconnectedException` is raised wrapping the underlying `IOException`.

---

### User Story 2 - Configurable Read Timeouts (Priority: P2)

Users and test harnesses should be able to configure client read timeouts both programmatically and via environment variable, preventing indefinite hangs on unresponsive engines.

**Why this priority**: Default timeouts prevent hangs, but different environments (CI vs interactive) need different values. HighBarV2 added env var `HIGHBAR_CLIENT_TIMEOUT_MS` support.

**Independent Test**: Can be tested by creating a client with explicit timeout and verifying the stream's `ReadTimeout` property is set, and by setting the env var and verifying the default is picked up.

**Acceptance Scenarios**:

1. **Given** no explicit timeout is specified, **When** a client connects, **Then** the read timeout defaults to 10 seconds.
2. **Given** an environment variable `FSBAR_CLIENT_TIMEOUT_MS` is set to a value, **When** a client connects without explicit timeout, **Then** the read timeout uses the environment variable value.
3. **Given** an explicit timeout is passed to the client, **When** a client connects, **Then** the explicit value takes precedence over the environment variable.

---

### User Story 3 - Resilient Map Test Execution (Priority: P3)

Map-related tests should gracefully handle proxy disconnections during map callbacks rather than crashing the test run, and should reset game state between map queries to avoid stale state interference.

**Why this priority**: HighBarV2 found that map callback queries could trigger proxy disconnects and that stale engine state between tests caused false failures. Adding error recovery and state resets fixes these.

**Independent Test**: Can be tested by running the full map test suite against a live engine and verifying all tests either pass or report skip (not crash).

**Acceptance Scenarios**:

1. **Given** the proxy does not support a map callback, **When** a map test calls that callback, **Then** the test reports a skip with a descriptive message rather than failing the test run.
2. **Given** a map callback causes a disconnection, **When** the test catches the error, **Then** it logs the disconnection and reports the test as skipped.
3. ~~**Given** multiple map tests run sequentially, **When** each test starts, **Then** it does not rely on state left by the previous test.~~ *Removed — see FR-006 removal rationale.*

### Edge Cases

- What happens when the engine crashes between two sequential map queries in the same test?
- How does the client behave when a partial message header is received before disconnection?
- What happens when the timeout environment variable contains an invalid (non-numeric) value?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a typed `EngineDisconnectedException` that wraps disconnection-related errors (zero-byte reads, read timeouts) with context (last frame number, original exception).
- **FR-002**: System MUST support configurable read timeouts via: (a) explicit parameter, (b) `FSBAR_CLIENT_TIMEOUT_MS` environment variable, (c) 10-second default — in that precedence order.
- **FR-003**: System MUST set `ReadTimeout` on the `NetworkStream` when establishing a connection to enforce the configured timeout.
- **FR-004**: The low-level read function MUST raise `EngineDisconnectedException` (not generic `failwith`) when it detects a zero-byte read or a timeout `IOException`.
- **FR-005**: Map test helper functions MUST catch `EngineDisconnectedException` and `IOException` during map callbacks and report as skipped rather than failing the test run.
- ~~**FR-006**~~: *Removed — FSBarV1's shared EngineFixture means a broken stream affects all subsequent tests equally. The catch-and-skip pattern (FR-005) handles this by design.*

### Key Entities

- **EngineDisconnectedException**: Custom exception carrying a message, optional last frame number, and optional inner exception.
- **Client timeout configuration**: Priority chain of explicit parameter > environment variable > default value.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All map grid and map query tests complete without hanging (each test finishes within 30 seconds).
- **SC-002**: When the engine disconnects, the error message includes enough context (exception type, frame number or callback name) to diagnose the cause without reading logs.
- **SC-003**: Map tests that encounter unsupported callbacks report as skipped, not failed.
- **SC-004**: The full test suite can run to completion even if the engine crashes mid-session.

## Assumptions

- The FSBarV1 proxy already supports map callbacks (52-56); this feature addresses client-side error handling, not proxy implementation.
- The `readExact` function in `Connection.fs` is the single point of low-level read operations that needs the timeout/disconnection upgrade.
- The existing `EngineConfig` timeout (`TimeoutMs = 30000`) is for connection acceptance, not stream reads — the new read timeout is a separate concern.
- The environment variable name will be `FSBAR_CLIENT_TIMEOUT_MS` (matching the FSBar naming convention, analogous to HighBarV2's `HIGHBAR_CLIENT_TIMEOUT_MS`).
