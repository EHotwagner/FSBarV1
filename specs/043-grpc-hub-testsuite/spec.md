# Feature Specification: Comprehensive gRPC Hub Test Suite

**Feature Branch**: `043-grpc-hub-testsuite`  
**Created**: 2026-04-18  
**Status**: Draft  
**Input**: User description: "create a comprehensive testsuite using grpc to control the hub and the diagnostic stream to debug. test all admin functins speed, pause.... and much more"

## Clarifications

### Session 2026-04-18

- Q: Should the Hub suppress its GUI window during test runs, or run the full app with a virtual display? → A: Start the full `FSBar.Hub.App` with a virtual display (Xvfb), same as smoke tests.
- Q: What timeout should live tests use when waiting for engine acknowledgements in the log stream? → A: 30 seconds.
- Q: Should the test suite explicitly verify the `Coalesced` outcome when admin RPCs are submitted faster than the 100 ms quiet window? → A: Yes — add a dedicated test asserting `Coalesced n` for rapid repeated admin calls.
- Q: What is the maximum wait time for asserting overlay auto-cleanup after a client disconnects? → A: 5 seconds.
- Q: Should the test suite assert the outcome of two concurrent `LaunchSession` calls? → A: Yes — the second must return `FailedPrecondition`; exactly one session launches.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin Channel via gRPC (Priority: P1)

A developer or QA engineer drives every admin operation — pause, resume, set engine speed, force-end a match, and send admin messages — exclusively through the gRPC scripting API while a live BAR session runs. The diagnostic log stream captures each engine-level event so the test can assert that the admin wire reached the engine (not just that the Hub accepted the call).

**Why this priority**: Admin control is the core value-add of the Hub scripting API; correctness here gates every downstream scenario.

**Independent Test**: Can be exercised by launching the Hub, starting a session via `LaunchSession`, then issuing each admin RPC in sequence and watching the log stream for the matching engine acknowledgements.

**Acceptance Scenarios**:

1. **Given** a running session, **When** `Pause` RPC is called, **Then** the log stream emits an `AdminChannel`/`Info` entry confirming the pause packet was sent, and subsequent `GetHubState` shows `admin_channel_status = Attached`.
2. **Given** a paused session, **When** `Resume` RPC is called, **Then** the engine transitions to playing and the log stream records the `ServerStartPlaying` (or equivalent resume) acknowledgement.
3. **Given** a running session, **When** `SetEngineSpeed` is called with values 0.5×, 1×, 2×, 5×, and 10×, **Then** each call returns `Sent` and the log stream records a `ScriptingHub`/`Debug` dispatch entry for each invocation.
4. **Given** a running session, **When** `ForceEndMatch` RPC is called, **Then** the session terminates and the log stream records the `SERVER_QUIT` event received from the engine.
5. **Given** a running session, **When** `SendAdminMessage` is called with a non-empty string, **Then** the log stream records the outbound `SAYMESSAGE` packet.
6. **Given** the admin channel is unavailable (engine not started), **When** any admin RPC is called, **Then** the RPC returns an `AdminSubmitResult` with a non-`Attached` status rather than a gRPC error.
7. **Given** a running session, **When** the same admin RPC (e.g., `SetEngineSpeed`) is called twice within 100 ms, **Then** the second call returns `Coalesced n` where `n ≥ 1`.

---

### User Story 2 - Diagnostic Log Stream Validation (Priority: P1)

A developer opens a `StreamLogEntries` subscription with various filter combinations (category, severity floor) and verifies that the stream delivers entries matching only the requested criteria while the Hub performs operations. This validates the log stream contract independently of admin operations.

**Why this priority**: The log stream is the primary observability channel for all other test stories; its correctness must be established before it can be used as a verification oracle.

**Independent Test**: Can be exercised without a running game session by connecting to the Hub, triggering Hub operations (e.g., changing settings, loading a preset), and asserting the log stream delivers expected entries.

**Acceptance Scenarios**:

1. **Given** a subscriber with default filter (all categories, `Info` floor), **When** the Hub performs any Settings save, **Then** the stream delivers a `Settings`/`Info` entry without `Debug`-severity entries appearing.
2. **Given** a subscriber with `AdminChannel` category and `Debug` floor, **When** an admin RPC is issued, **Then** the stream delivers the `Debug`-level wire-trace entries alongside `Info` entries.
3. **Given** the maximum subscriber cap is reached (`HubSettings.MaxLogStreamSubscribers`), **When** a new `StreamLogEntries` call is made, **Then** the RPC receives `ResourceExhausted` with the cap value named in the reason string.
4. **Given** a subscriber whose channel falls behind, **When** the Hub produces more entries than the per-subscriber buffer can hold, **Then** the next delivered entry's `dropped_since_last` field reflects the count of dropped entries.
5. **Given** a long message (> 8 KiB), **When** emitted, **Then** the stream delivers the message truncated with the ` …[truncated N bytes]` marker and the truncated content is byte-identical across all active subscribers.
6. **Given** a subscriber that disconnects mid-stream, **When** the gRPC channel closes, **Then** the Hub releases the subscriber slot within 1 second so subsequent connections can fill it.

---

### User Story 3 - Session Lifecycle via gRPC (Priority: P2)

A developer fully orchestrates a Hub session — listing maps, configuring the lobby, validating configuration, launching, monitoring until running, then stopping — entirely through the gRPC API. The log stream is used to confirm each lifecycle event.

**Why this priority**: Session control is the entry-point to any live testing; all higher-level scenarios depend on it.

**Independent Test**: Can be tested independently by completing a full launch-to-stop cycle and asserting log stream coverage of each phase.

**Acceptance Scenarios**:

1. **Given** no active session, **When** `ListMaps` is called, **Then** at least one map entry is returned.
2. **Given** a lobby config built from `ListMaps` output, **When** `ValidateLobby` is called with a valid config, **Then** it returns success; when called with a missing map field, it returns a validation error.
3. **Given** a valid lobby config, **When** `LaunchSession` is called, **Then** `GetHubState` eventually shows an active session, and the log stream delivers a `SessionManager`/`Info` entry recording the launch.
4. **Given** an active session, **When** `StopSession` is called, **Then** the session terminates and the log stream records the `SERVER_QUIT` acknowledgement.
5. **Given** `startPaused = true` in the launch config, **When** the session starts, **Then** the engine is paused at frame zero before any frame ticks are processed.

---

### User Story 4 - Viz Config and Camera Control via gRPC (Priority: P2)

A developer reads and modifies the viewer's visualization config and camera state through gRPC RPCs (`SetVizConfig`, `SetVizAttribute`, `ToggleOverlay`, `SetCamera`, `SetActiveTab`) and confirms that `GetHubState` reflects each change and the log stream records the `HubStateStore` mutations.

**Why this priority**: Viz and camera control is a primary scripting use case for recording and automation tooling.

**Independent Test**: Can be tested without a running game by exercising config and camera RPCs against the idle Hub and verifying state via `GetHubState`.

**Acceptance Scenarios**:

1. **Given** the Hub is idle, **When** `SetVizAttribute` is called to change a named attribute, **Then** `GetHubState` reflects the new value and the log stream shows a `HubStateStore`/`Info` mutation event.
2. **Given** a `SetVizConfig` call with a fully populated config, **When** applied, **Then** a follow-up `GetHubState` returns a VizConfig equal to the submitted value.
3. **Given** an overlay toggle command (`ToggleOverlay`), **When** called for each overlay kind, **Then** `GetHubState` toggles the overlay state and the log stream records the change.
4. **Given** `SetCamera` with explicit coordinates, **When** applied, **Then** `GetHubState` camera fields match the submitted values.
5. **Given** `SetActiveTab` called with each valid tab name, **When** applied, **Then** `GetHubState` reports the correct active tab.
6. **Given** a `SetVizAttribute` call with an unknown attribute key, **When** applied, **Then** the RPC returns a descriptive error and the log stream records a `HubStateStore`/`Warning` rejection entry.

---

### User Story 5 - Overlay Layer Management via gRPC (Priority: P2)

A developer pushes overlay layers containing various primitive types (lines, polygons, circles, text, images) via `PutLayer`, lists them, and deletes them via `DeleteLayer` / `ClearLayers`. The feature validates cap enforcement and automatic cleanup on disconnect.

**Why this priority**: Overlay scripting is a key differentiator for analysis tooling built on top of the Hub.

**Independent Test**: Can be exercised without a live match by creating and querying layers and asserting cap enforcement.

**Acceptance Scenarios**:

1. **Given** a connected client, **When** `PutLayer` is called with each primitive type (Line, Polyline, Polygon, Rectangle, Circle, Path, Text, Image), **Then** `ListLayers` returns the layer with the correct primitive count.
2. **Given** a client with 16 layers already created, **When** a 17th `PutLayer` call is made, **Then** it returns a capacity-exceeded error.
3. **Given** a layer with 501 primitives in one push, **When** submitted, **Then** the RPC returns an error naming the 500-primitive limit.
4. **Given** an active client with layers, **When** the client disconnects, **Then** all that client's layers are removed within 5 seconds and a subsequent `ListLayers` returns an empty list.
5. **Given** `ClearLayers` called by the owning client, **When** applied, **Then** `ListLayers` returns an empty list for that client.

---

### User Story 6 - Hub State Observation Stream (Priority: P3)

A developer subscribes to `StreamHubStateEvents` and observes that each Hub mutation (tab change, viz config update, session status change) triggers a corresponding event on the stream without polling.

**Why this priority**: Event-driven observation reduces polling overhead in scripting tools; important but not blocking for other test stories.

**Independent Test**: Can be verified by subscribing to the event stream and triggering Hub operations through other gRPC calls while asserting the expected events arrive.

**Acceptance Scenarios**:

1. **Given** a `StreamHubStateEvents` subscriber, **When** `SetActiveTab` is called, **Then** the stream delivers an event reflecting the tab change within 500 ms.
2. **Given** a `StreamHubStateEvents` subscriber, **When** a session launches, **Then** the stream delivers a session-state-changed event.
3. **Given** two concurrent `StreamHubStateEvents` subscribers, **When** a mutation occurs, **Then** both receive the event.

---

### User Story 7 - Preset and Encyclopedia Operations via gRPC (Priority: P3)

A developer saves, lists, loads, and deletes style presets and queries the unit encyclopedia through the gRPC API, verifying that persistence round-trips correctly and that unit listings match the known BarData catalogue.

**Why this priority**: Preset and encyclopedia access complete the full scripting surface; important for authoring tooling.

**Independent Test**: Can be exercised without a running session.

**Acceptance Scenarios**:

1. **Given** the Hub is running, **When** `SavePreset` is called with a name and current config, **Then** `ListPresets` includes that name.
2. **Given** a saved preset, **When** `LoadPreset` is called, **Then** `GetHubState` reflects the preset's config values.
3. **Given** a saved preset, **When** `DeletePreset` is called, **Then** `ListPresets` no longer includes the name.
4. **Given** `ListUnits` called with no filter, **Then** the result count matches the number of entries in the unit catalogue.
5. **Given** `SelectUnit` called with a valid internal name, **When** applied, **Then** `GetHubState` shows the selected unit.

---

### Edge Cases

- What happens when the Hub receives admin RPCs before any session is launched?
- How does the log stream behave when the Hub process is under high load (many rapid mutations)?
- What occurs if `SetEngineSpeed` is called with a value outside the valid range?
- How does the test suite behave if the BAR engine binary is absent from the expected path?
- What happens when `StreamLogEntries` is established after the Hub has already emitted many entries (no historical replay expected)?
- How are correlation IDs propagated from the gRPC request header through to the log stream entries?
- What occurs if two clients simultaneously call `LaunchSession`? The second must receive `FailedPrecondition`; exactly one session is permitted at a time.
- What happens when `SendAdminMessage` is called with an empty string?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The test suite MUST exercise every admin RPC (`Pause`, `Resume`, `SetEngineSpeed`, `ForceEndMatch`, `SendAdminMessage`) and assert the `AdminSubmitResult` outcome, including `Sent`, `Rejected`, and `Coalesced` variants.
- **FR-001a**: The test suite MUST include a scenario where the same admin RPC is fired twice within 100 ms and assert the second call returns `Coalesced n` (not `Sent`).
- **FR-002**: Each admin live test MUST use the `StreamLogEntries` stream to verify the corresponding engine-level event was observed (not only Hub-side acceptance).
- **FR-003**: The test suite MUST cover all `SetEngineSpeed` multipliers documented in the Hub spec: 0.5×, 1×, 2×, 5×, 10×.
- **FR-004**: The test suite MUST include a `Pause → verify paused → Resume → verify resumed` round-trip scenario.
- **FR-005**: The test suite MUST include a `ForceEndMatch` scenario that confirms session termination via both the RPC response and the log stream.
- **FR-006**: The test suite MUST validate `StreamLogEntries` subscriber cap enforcement (`MaxLogStreamSubscribers`) and the `ResourceExhausted` gRPC status.
- **FR-007**: The test suite MUST validate per-subscriber buffer overflow behaviour by asserting `dropped_since_last` is non-zero after intentional backpressure.
- **FR-008**: The test suite MUST validate message truncation at the 8 KiB boundary and confirm the truncation marker is present and byte-identical across all active subscribers.
- **FR-009**: The test suite MUST exercise the full session lifecycle (`ListMaps`, `ValidateLobby`, `LaunchSession`, wait-for-running, `StopSession`) and confirm each phase via the log stream.
- **FR-010**: The test suite MUST cover `SetVizConfig`, `SetVizAttribute`, `ToggleOverlay`, `SetCamera`, and `SetActiveTab` RPCs and verify state via `GetHubState`.
- **FR-011**: The test suite MUST cover overlay layer CRUD (`PutLayer`, `ListLayers`, `DeleteLayer`, `ClearLayers`) for all documented primitive types.
- **FR-012**: The test suite MUST assert overlay capacity limits (16 layers/client, 500 primitives/layer) return appropriate gRPC error codes.
- **FR-013**: The test suite MUST verify automatic overlay cleanup on client disconnect by asserting `ListLayers` returns empty within 5 seconds of gRPC channel close.
- **FR-014**: The test suite MUST subscribe to `StreamHubStateEvents` and assert that mutations from other RPCs produce corresponding events.
- **FR-015**: The test suite MUST cover preset operations: `SavePreset`, `ListPresets`, `LoadPreset`, `DeletePreset` with round-trip config equality.
- **FR-016**: The test suite MUST cover `ListUnits` and `SelectUnit` and assert unit count correctness.
- **FR-017**: The test suite MUST validate that `SendAdminMessage` with an empty string is rejected before reaching the engine.
- **FR-018**: The test suite MUST validate correlation ID propagation by setting the `x-fsbar-correlation-id` request header and asserting the response trailer echoes it.
- **FR-019**: Each test MUST be categorised by `[<Trait("Category", ...)>]` to allow targeted `dotnet test --filter` execution.
- **FR-020**: Tests that require a running BAR engine MUST be skipped (not failed) when no engine is available.
- **FR-021**: The test suite MUST start the full `FSBar.Hub.App` process (with a virtual display via Xvfb) with a known gRPC port before assertions begin and tear it down after each test class.
- **FR-022**: The test suite MUST clean up all created presets, sessions, and overlay layers in teardown to avoid cross-test pollution.
- **FR-023**: The test suite MUST include a concurrent-launch scenario: two clients call `LaunchSession` simultaneously and the test asserts exactly one succeeds and the other receives `FailedPrecondition`.

### Key Entities

- **HubTestFixture**: Manages Hub lifetime, gRPC channel creation, and post-test cleanup for all test classes.
- **LogStreamHarness**: Wraps a `StreamLogEntries` subscription and provides assertion helpers (wait for entry matching predicate, assert category/severity, collect N entries, assert no unexpected entries within a window).
- **AdminRpcClient**: Typed wrapper over the raw gRPC stub for admin RPCs, adding timeout logic appropriate for live-engine latency.
- **HubStateSnapshot**: A value captured from `GetHubState` at a point in time, used for before/after comparison assertions.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All admin RPCs are covered with at least one positive and one negative test case each; 100% of the admin RPC surface has at least one assertion against the log stream.
- **SC-002**: Every gRPC RPC covered by a user story in this spec is exercised by at least one test (positive path); RPCs with documented error conditions have corresponding negative-path tests. The following RPCs are explicitly out of scope for this test suite and are excluded from coverage: `ConfigureLobby` (lobby state is built inline from `ListMaps` output), `StreamRenderFrames` and `GetRenderFrame` (render-frame streaming is exercised by the visual smoke test suite), `GetHubSettings` and `SetHubSettings` (settings persistence is covered by `HubSettings` unit tests), `InstallProxy` and `RefreshProxyStatus` (proxy install requires a live proxy binary and is deferred to a dedicated proxy-management test suite).
- **SC-003**: The full non-live test suite completes in under 60 seconds on a developer workstation. Individual live assertions have a maximum wait of 30 seconds before failing.
- **SC-004**: No test passes solely on the absence of a gRPC error — every assertion is tied to observed state or log stream content.
- **SC-005**: Live tests are correctly skipped when the engine is absent; the suite reports 0 failures (not 0 skips) in that environment.
- **SC-006**: Capacity-limit tests trigger the correct gRPC status code (`ResourceExhausted` or `InvalidArgument`) 100% of the time.
- **SC-007**: The test suite is wire-contract stable — running `buf breaking` against the proto used by the tests passes without errors after the feature is merged.

## Assumptions

- The Hub's gRPC service is accessible on a loopback port assigned dynamically by the test fixture; tests do not depend on a fixed port number.
- A BAR engine installation exists at the standard path for live tests; tests detect its absence and skip gracefully (per FR-020).
- The test project joins `FSBarV1.slnx` as a new project (`tests/FSBar.Hub.GrpcTests/`) alongside existing live test projects.
- The `HubTestFixture` launches `FSBar.Hub.App` as a child process with a virtual display (Xvfb), connecting to it via loopback gRPC on a dynamically assigned port. The fixture waits for the Hub's gRPC server to become ready before returning to tests.
- Preset files written during tests use a test-specific prefix (`test-043-*`) and are deleted in fixture teardown so user presets are unaffected.
- The log stream harness uses a bounded wait of 5 s for non-live assertions and 30 s for live engine-acknowledgement assertions, to avoid hanging tests on broken log delivery.
- Test isolation relies on a fresh Hub instance per test class (not per individual test), since Hub startup has non-trivial overhead.
- All overlay primitives are created in `Screen` coordinate space for tests that do not require a running session, to avoid dependency on a live map.
- The `startPaused = true` scenario requires a live BAR engine session and is tagged `Live` per FR-020.
- Correlation ID propagation tests rely on the `DebugDispatchInterceptor` log entries already emitted by the Hub (feature 042), requiring no new server-side instrumentation.
