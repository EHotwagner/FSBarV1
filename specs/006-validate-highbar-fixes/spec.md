# Feature Specification: Validate HighBar Fixes Resolve Outstanding Issues

**Feature Branch**: `006-validate-highbar-fixes`  
**Created**: 2026-04-06  
**Status**: Complete  
**Input**: User description: "research the last fixes in highbar and test if they solve our outstanding issues."

## Clarifications

### Session 2026-04-06

- Q: Which heightmap resolution should the system use — center (w*h) or corner/vertex ((w+1)*(h+1))? → A: Switch to corners heightmap API — vertex-resolution is the ground truth in the Spring/BAR ecosystem.
- Q: What dimensions should the slope map use? → A: Fix to half-resolution (w/2 * h/2), matching the engine API.
- Q: Is the corners heightmap callback available in the HighBar proxy? → A: Yes — HighBar commit 026 already added `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59`. FSBarV1 needs to add this to its proto and consume it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verify Map Grid Dimension Handling (Priority: P1)

A developer runs the map-related integration tests to confirm that heightmap data returned by the engine is correctly processed without dimension mismatch errors. The system uses the corners heightmap callback (ID 59) which returns `(w+1)*(h+1)` vertex-resolution data, matching the existing dimension expectations in the code.

**Why this priority**: 11 out of 12 map tests currently fail due to a dimension mismatch caused by calling the center heightmap (w*h) while expecting corner dimensions ((w+1)*(h+1)). Switching to the corners heightmap callback resolves this directly.

**Independent Test**: Can be tested by running the map grid test suite against a live engine session and verifying all heightmap-dependent tests pass with correct dimensions.

**Acceptance Scenarios**:

1. **Given** a live engine session with a loaded map, **When** the map grid is loaded using the corners heightmap callback, **Then** the heightmap array contains exactly `(w+1)*(h+1)` values and reshapes without error
2. **Given** a live engine session, **When** the slope map is loaded, **Then** the slope array contains exactly `(w/2)*(h/2)` values at half-resolution
3. **Given** a dimension mismatch that cannot be resolved (e.g., proxy does not support the callback), **When** the test runs, **Then** the test is marked as skipped with a diagnostic message (not marked as passed)

---

### User Story 2 - Validate Typed Disconnection Exceptions (Priority: P2)

A developer runs integration tests that exercise engine connections and disconnections to confirm that the new `EngineDisconnectedException` type is raised correctly and caught by test infrastructure, preventing cascade failures.

**Why this priority**: Cascade failures from untyped exceptions were a major pain point. Confirming the typed exception works correctly validates the most critical HighBar fix.

**Independent Test**: Can be tested by running any integration test that interacts with the engine and observing that disconnection scenarios produce `EngineDisconnectedException` rather than generic `IOException` or `failwith` messages.

**Acceptance Scenarios**:

1. **Given** an active engine connection, **When** the engine disconnects unexpectedly, **Then** an `EngineDisconnectedException` is raised with a descriptive message
2. **Given** a test that encounters a disconnection, **When** the exception is caught, **Then** subsequent tests in the suite are not affected (no cascade failures)

---

### User Story 3 - Validate Configurable Read Timeouts (Priority: P3)

A developer configures a custom read timeout and verifies that the connection respects it, preventing indefinite hangs on unresponsive engines.

**Why this priority**: Timeout configuration prevents CI hangs and improves developer experience, but is less critical than data correctness (P1) and exception handling (P2).

**Independent Test**: Can be tested by setting `ReadTimeoutMs` in the engine config and verifying the connection times out within the expected window.

**Acceptance Scenarios**:

1. **Given** an engine config with `ReadTimeoutMs` set to a specific value, **When** a connection is established, **Then** the read timeout on the stream matches the configured value
2. **Given** no explicit timeout configured, **When** a connection is established, **Then** the system falls back to the environment variable or the 10-second default

---

### Edge Cases

- What happens when the engine returns a heightmap with zero values?
- How does the system handle a partial read where the stream is interrupted mid-transfer?
- What happens when `FSBAR_CLIENT_TIMEOUT_MS` environment variable contains a non-numeric value?
- What happens when the proxy does not yet support callback ID 59 (corners heightmap)?
- What happens when slope map dimensions are not evenly divisible by 2?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST use the corners heightmap callback (ID 59) to obtain vertex-resolution heightmap data at `(w+1)*(h+1)` dimensions
- **FR-002**: System MUST reshape the slope map at half-resolution `(w/2)*(h/2)`, matching the engine's native slope map dimensions
- **FR-003**: System MUST add `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59` to the local proto definition and generate corresponding F# bindings
- **FR-004**: System MUST raise `EngineDisconnectedException` (not generic exceptions) when the engine connection drops
- **FR-005**: System MUST respect the configured `ReadTimeoutMs` value, falling back to `FSBAR_CLIENT_TIMEOUT_MS` environment variable, then to a 10-second default
- **FR-006**: Tests that fail due to out-of-scope issues (e.g., proxy not supporting a callback) MUST be marked as skipped or have relaxed assertions, never marked as passed
- **FR-007**: System MUST provide diagnostic output when a dimension mismatch is detected, including expected vs actual counts
- **FR-008**: Integration test suite MUST not exhibit cascade failures when one test encounters a disconnection

### Key Entities

- **MapGrid**: Represents the loaded map data including heightmap (vertex-resolution, `(w+1)*(h+1)`), slope map (half-resolution, `(w/2)*(h/2)`), and other layers at `w*h`.
- **EngineDisconnectedException**: Typed exception carrying context about disconnection events, including optional last frame number.
- **EngineConfig**: Configuration record controlling connection parameters including read timeouts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 12 map-related integration tests either pass or are marked as skipped with a diagnostic — zero unhandled dimension mismatch exceptions
- **SC-002**: Heightmap loads at vertex resolution `(w+1)*(h+1)` matching the corners heightmap data from the engine
- **SC-003**: Slope map loads at half-resolution `(w/2)*(h/2)` without dimension errors
- **SC-004**: Disconnection events produce typed exceptions in 100% of cases (no generic `IOException` or `failwith` leaking to test output)
- **SC-005**: Test suite completes without cascade failures — a disconnection in one test does not cause subsequent tests to fail
- **SC-006**: Read timeout configuration is respected within a 500ms tolerance of the configured value
- **SC-007**: The full integration test suite runs to completion without hanging indefinitely

## Assumptions

- A live `spring-headless` engine and BAR game data are available in the test environment
- The HighBar proxy (V2, commit 026+) supports `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP` (ID 59)
- The HighBar fixes from commits 023-026 have been fully merged and are available in the proxy binary
- Map width and height values from the engine are even numbers (required for slope map `w/2` calculation)
- Tests are run against the same map that previously exhibited the dimension discrepancy
