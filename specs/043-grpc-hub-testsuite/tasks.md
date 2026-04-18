# Tasks: 043-grpc-hub-testsuite — Comprehensive gRPC Hub Test Suite

**Input**: Design documents from `/specs/043-grpc-hub-testsuite/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in all descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Source code changes and new project scaffolding that must exist before any tests can compile.

- [X] T001 Add `FSBAR_HUB_GRPC_PORT` env-var override to `src/FSBar.Hub.App/Program.fs` (R8: replace `(getSettings ()).GrpcPort` with a `grpcPort` binding that checks the env var first; update Kestrel bind and log-line uses)
- [X] T002 Create `tests/FSBar.Hub.GrpcTests/FSBar.Hub.GrpcTests.fsproj` — xUnit test project referencing `FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Proto`, `FSBar.Client`; add `Grpc.Net.Client 2.67.*`, `Xunit.SkippableFact 1.4.13`, `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`; list all `.fs` source files in compile order
- [X] T003 Add `tests/FSBar.Hub.GrpcTests/FSBar.Hub.GrpcTests.fsproj` to `FSBarV1.slnx`
- [X] T004 Verify `dotnet build FSBarV1.slnx` succeeds with the new (empty) project

**Checkpoint**: Solution builds — test project exists and compiles.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core test infrastructure (`HubTestFixture`, `LogStreamHarness`, `AdminRpcClient`, `SkipGuards`) that every test class depends on. Must be complete before any user-story test file is written.

- [X] T005 Implement `tests/FSBar.Hub.GrpcTests/SkipGuards.fs` — `requireBarInstall: unit -> unit` and `requireEngineInstalled: unit -> unit` matching existing live-test skip pattern; use `[<SkippableFact>]` via `Xunit.SkippableFact`
- [X] T006 Implement `tests/FSBar.Hub.GrpcTests/HubTestFixture.fs` — `IAsyncLifetime` class that: (1) picks a free loopback port via `TcpListener(IPAddress.Loopback, 0)`, (2) spawns `dotnet run --project src/FSBar.Hub.App --no-build` with env vars `FSBAR_HUB_GRPC_PORT`, `DISPLAY=:0`, `XDG_RUNTIME_DIR=/tmp/runtime-developer`, `XDG_CONFIG_HOME=<tempdir>`, (3) polls `GetHubState` every 500 ms up to 15 s for readiness, (4) exposes `Stub: ScriptingHub.ScriptingClient` and `Port: int`; `DisposeAsync` kills process tree, shuts down channel, deletes tempdir
- [X] T007 Implement `tests/FSBar.Hub.GrpcTests/LogStreamHarness.fs` — wraps `IAsyncStreamReader<LogEntry>` with `WaitForEntry(predicate, timeoutMs)`, `CollectN(n, timeoutMs)`, `AssertNoUnexpected(predicate, windowMs)` all using `CancellationTokenSource` with timeout; implements `IDisposable`
- [X] T008 Implement `tests/FSBar.Hub.GrpcTests/AdminRpcClient.fs` — typed façade over `ScriptingHub.ScriptingClient` for `Pause`, `Resume`, `SetEngineSpeed`, `ForceEndMatch`, `SendAdminMessage` with configurable `defaultTimeoutMs` (5000 non-live, 30000 live)
- [X] T009 Verify foundation compiles: `dotnet build tests/FSBar.Hub.GrpcTests/FSBar.Hub.GrpcTests.fsproj`

**Checkpoint**: Foundation ready — all test classes can now be implemented.

---

## Phase 3: User Story 1 — Admin Channel via gRPC (Priority: P1) 🎯 MVP

**Goal**: Exercise every admin RPC and assert `AdminSubmitResult` outcomes, including `Sent`, `Rejected`, and `Coalesced`; use the log stream to confirm engine-level events.

**Independent Test**: Launch Hub, open `StreamLogEntries`, call each admin RPC, assert log entries. Live tests skip when engine absent.

- [X] T010 [US1] Implement `tests/FSBar.Hub.GrpcTests/AdminChannelTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-001: `Pause_WhenNoSession_ReturnsRejected` — call `Pause` before session; assert `AdminSubmitResult` status is not `Attached`; no gRPC error thrown
  - FR-001: `Resume_WhenNoSession_ReturnsRejected` — same pattern for `Resume`
  - FR-001a: `SetEngineSpeed_RapidTwice_SecondIsCoalesced` — `[<SkippableFact>]` live; fire two `SetEngineSpeed` calls via `Task.WhenAll` within 100 ms; assert at least one result is `Coalesced`
  - FR-002: `Pause_LiveSession_LogStreamConfirmsPauseSent` — `[<SkippableFact>]` live; launch session, call `Pause`, wait for `AdminChannel`/`Info` log entry confirming PAUSE packet sent
  - FR-003: `SetEngineSpeed_AllMultipliers_AllReturnSent` — `[<SkippableFact>]` live; call `SetEngineSpeed` with 0.5, 1.0, 2.0, 5.0, 10.0; all return `Sent`
  - FR-004: `PauseResume_RoundTrip_LogStreamConfirmsBoth` — `[<SkippableFact>]` live; pause→assert paused log entry→resume→assert resumed log entry
  - FR-005: `ForceEndMatch_TerminatesSession_LogStreamConfirmsServerQuit` — `[<SkippableFact>]` live; `ForceEndMatch`, wait for `SERVER_QUIT` log entry, assert session no longer active in `GetHubState`
  - All tests tagged `[<Trait("Category", "GrpcAdmin")>]`

**Checkpoint**: US1 complete — all admin RPC positive and negative paths covered; log-stream oracle validates engine acknowledgements.

---

## Phase 4: User Story 2 — Diagnostic Log Stream Validation (Priority: P1)

**Goal**: Validate `StreamLogEntries` filter semantics, subscriber cap, buffer overflow, message truncation, and slot release on disconnect.

**Independent Test**: No engine needed — connect to idle Hub, trigger Settings save or preset write, assert stream content.

- [X] T011 [US2] Implement `tests/FSBar.Hub.GrpcTests/LogStreamTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-006: `SubscriberCap_WhenExceeded_ReturnsResourceExhausted` — fill cap with `MaxLogStreamSubscribers` open streams, open one more, assert `RpcException` with `StatusCode.ResourceExhausted` and cap value in reason
  - FR-007: `BufferOverflow_DroppedSinceLastIsNonZero` — open stream, pause `MoveNextAsync` consumption, trigger many Hub mutations (rapid `SetVizAttribute` calls), resume, assert first received entry has `dropped_since_last > 0`
  - FR-008: `LongMessage_TruncatedWithMarker` — open a `StreamLogEntries` subscription with `ScriptingHub` category and `Debug` severity floor (so `DebugDispatchInterceptor` entries are delivered); call `SendAdminMessage` with a 9000-character ASCII string; the interceptor emits a `Debug` entry serialising the full request, which exceeds 8 KiB; assert the delivered entry contains the ` …[truncated` marker
  - FR-008: `TruncatedContent_ByteIdenticalAcrossSubscribers` — open two streams simultaneously, trigger truncation, assert both receive byte-identical truncated content
  - Default filter: `DefaultFilter_InfoFloor_NoDebugEntries` — open stream with default filter, trigger Settings save, collect entries for 2 s, assert none have `Severity.Debug`
  - Debug floor: `AdminChannelCategory_DebugFloor_DeliversDebugEntries` — open stream with `AdminChannel` category + `Debug` floor, issue admin RPC (no-session), assert at least one `Debug` entry arrives
  - Slot release: `SubscriberDisconnect_SlotReleasedWithin1s` — fill cap, disconnect one subscriber, wait 1 s, assert a new subscriber can connect without `ResourceExhausted`
  - All tests tagged `[<Trait("Category", "GrpcLogStream")>]`

**Checkpoint**: US2 complete — log stream contract fully validated; safe to use as oracle in remaining stories.

---

## Phase 5: User Story 3 — Session Lifecycle via gRPC (Priority: P2)

**Goal**: Full `ListMaps → ValidateLobby → LaunchSession → wait-for-running → StopSession` cycle, confirmed via log stream.

**Independent Test**: Requires live engine; all tests use `SkipGuards.requireEngineInstalled ()`.

- [X] T012 [US3] Implement `tests/FSBar.Hub.GrpcTests/SessionLifecycleTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-009: `ListMaps_ReturnsAtLeastOne` — `[<SkippableFact>]` live; assert non-empty map list
  - FR-009: `ValidateLobby_ValidConfig_ReturnsSuccess` — `[<SkippableFact>]` live; build lobby from `ListMaps` first result, call `ValidateLobby`, assert success
  - FR-009: `ValidateLobby_MissingMapField_ReturnsError` — call `ValidateLobby` with empty `MapName`, assert error response (no gRPC exception)
  - FR-009: `LaunchSession_FullCycle_LogStreamConfirmsLaunch` — `[<SkippableFact>]` live; `LaunchSession`, poll `GetHubState` until active session, wait for `SessionManager`/`Info` launch log entry, call `StopSession`, wait for `SERVER_QUIT` log entry
  - FR-023: `ConcurrentLaunch_SecondReceivesFailedPrecondition` — `[<SkippableFact>]` live; two concurrent `LaunchSession` calls via `Task.WhenAll`; assert exactly one succeeds and the other returns `FailedPrecondition`
  - `StartPaused_EngineAtFrameZero` — `[<SkippableFact>]` live; launch with `startPaused = true`, assert session is paused before frame ticks (log stream confirms pause command was sent)
  - All tests tagged `[<Trait("Category", "GrpcSession")>]`

**Checkpoint**: US3 complete — full session orchestration via gRPC verified.

---

## Phase 6: User Story 4 — Viz Config and Camera Control via gRPC (Priority: P2)

**Goal**: All viz/camera RPCs mutate Hub state observable via `GetHubState`; unknown attribute key returns error with log stream warning.

**Independent Test**: No engine needed — all tests work against idle Hub.

- [X] T013 [P] [US4] Implement `tests/FSBar.Hub.GrpcTests/VizConfigTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-010: `SetVizAttribute_ValidKey_ReflectedInGetHubState` — set a named attribute, capture `GetHubState`, assert new value present; log stream shows `HubStateStore`/`Info` mutation entry
  - FR-010: `SetVizConfig_FullConfig_GetHubStateReturnsEqual` — submit fully-populated `VizConfigWire`, assert round-trip equality via `GetHubState`
  - FR-010: `ToggleOverlay_EachKind_TogglesState` — for each overlay kind, call `ToggleOverlay`, assert `GetHubState` flips the overlay flag
  - FR-010: `SetCamera_ExplicitCoords_ReflectedInGetHubState` — `SetCamera` with specific x/y/zoom, assert `GetHubState` camera fields match
  - FR-010: `SetActiveTab_EachTab_ReflectedInGetHubState` — call `SetActiveTab` for each valid tab enum value, assert `GetHubState` active tab matches
  - FR-010 negative: `SetVizAttribute_UnknownKey_ReturnsError_LogWarning` — call `SetVizAttribute` with `key = "nonexistent-key-xyz"`, assert descriptive error returned; wait for `HubStateStore`/`Warning` log entry
  - All tests tagged `[<Trait("Category", "GrpcViz")>]`

**Checkpoint**: US4 complete — all viz/camera RPCs validated; state observable via `GetHubState`.

---

## Phase 7: User Story 5 — Overlay Layer Management via gRPC (Priority: P2)

**Goal**: Full overlay CRUD for all primitive types; cap enforcement returns correct gRPC status codes; auto-cleanup on disconnect verified within 5 s.

**Independent Test**: No engine needed — all tests use `Screen` coordinate space.

- [X] T014 [P] [US5] Implement `tests/FSBar.Hub.GrpcTests/OverlayLayerTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-011: `PutLayer_AllPrimitiveTypes_ListLayersReturnsCorrectCount` — one `PutLayer` call per primitive type (Line, Polyline, Polygon, Rectangle, Circle, Path, Text, Image) in `Screen` space; assert `ListLayers` returns each with correct primitive count
  - FR-011: `ClearLayers_RemovesAllClientLayers` — create 3 layers, call `ClearLayers`, assert `ListLayers` returns empty
  - FR-011: `DeleteLayer_RemovesSingleLayer` — create 2 layers, delete one by name, assert `ListLayers` returns exactly the remaining layer
  - FR-012: `PutLayer_17th_ReturnsCapacityError` — create 16 layers, attempt 17th, assert `ResourceExhausted` status
  - FR-012: `PutLayer_501Primitives_ReturnsInvalidArgument` — single `PutLayer` with 501 Line primitives, assert `InvalidArgument` naming the 500-primitive limit
  - FR-013: `ClientDisconnect_LayersRemovedWithin5s` — create layers on a separate `GrpcChannel`, close that channel, poll `ListLayers` every 500 ms for up to 5 s, assert empty
  - All tests tagged `[<Trait("Category", "GrpcOverlay")>]`

**Checkpoint**: US5 complete — overlay CRUD, cap enforcement, and auto-cleanup all validated.

---

## Phase 8: User Story 6 — Hub State Observation Stream (Priority: P3)

**Goal**: `StreamHubStateEvents` delivers mutation events to subscribers without polling; two concurrent subscribers both receive events.

**Independent Test**: No engine needed — mutate Hub via other RPCs while subscribed.

- [X] T015 [P] [US6] Implement `tests/FSBar.Hub.GrpcTests/StateEventStreamTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-014: `SetActiveTab_ProducesStateEvent_Within500ms` — subscribe `StreamHubStateEvents`, call `SetActiveTab`, assert event reflecting tab change arrives within 500 ms
  - FR-014: `TwoConcurrentSubscribers_BothReceiveEvent` — open two `StreamHubStateEvents` streams, call `SetActiveTab`, assert both streams deliver the event
  - `SessionLaunch_ProducesStateEvent` — `[<SkippableFact>]` live; subscribe, `LaunchSession`, assert session-state-changed event arrives
  - All tests tagged `[<Trait("Category", "GrpcStateEvents")>]`

**Checkpoint**: US6 complete — event-driven observation of Hub state validated.

---

## Phase 9: User Story 7 — Preset and Encyclopedia Operations via gRPC (Priority: P3)

**Goal**: Full preset CRUD with round-trip config equality; `ListUnits` count matches BarData catalogue; `SelectUnit` updates Hub state.

**Independent Test**: No engine needed.

- [X] T016 [P] [US7] Implement `tests/FSBar.Hub.GrpcTests/PresetEncyclopediaTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-015: `SavePreset_AppearsinListPresets` — save preset with `test-043-` prefixed name, assert `ListPresets` includes it
  - FR-015: `LoadPreset_ReflectedInGetHubState` — save preset, load it back, assert `GetHubState` VizConfig matches preset's config
  - FR-015: `DeletePreset_RemovedFromListPresets` — save then delete, assert `ListPresets` no longer contains name
  - FR-016: `ListUnits_CountMatchesBarDataCatalogue` — call `ListUnits` with no filter, assert count equals `BarData.AllUnitDefs |> Seq.length` evaluated in the test (compile-time reference, not a hardcoded integer, so it stays correct if BarData is updated)
  - FR-016: `SelectUnit_ValidName_ReflectedInGetHubState` — call `SelectUnit` with `InternalName = "armcom"`, assert `GetHubState` shows selected unit
  - Cleanup: all presets created in test class use `test-043-` prefix; `DisposeAsync` in `HubTestFixture` deletes any `test-043-*` presets via `DeletePreset`
  - All tests tagged `[<Trait("Category", "GrpcPreset")>]`

**Checkpoint**: US7 complete — preset persistence and encyclopedia queries validated.

---

## Phase 10: Cross-Cutting — Correlation ID and Edge Cases

**Goal**: Validate correlation ID header propagation and empty `SendAdminMessage` rejection.

- [X] T017 [P] Implement `tests/FSBar.Hub.GrpcTests/CorrelationIdTests.fs` with `[<Collection("HubGrpc")>]` and `IClassFixture<HubTestFixture>`:
  - FR-018: `CorrelationId_EchoedInResponseTrailer` — set `x-fsbar-correlation-id` header on `GetHubState` call, assert trailer echoes the same value
  - FR-018: `CorrelationId_GeneratedWhenAbsent_PresentInTrailer` — call `GetHubState` without header, assert trailer still contains a non-empty correlation ID
  - FR-017: `SendAdminMessage_EmptyString_RejectedBeforeEngine` — call `SendAdminMessage ""`, assert `AdminSubmitResult` rejection (not a gRPC exception); log stream must NOT show any outbound SAYMESSAGE entry
  - All tests tagged `[<Trait("Category", "GrpcAdmin")>]`

**Checkpoint**: Cross-cutting concerns validated.

---

## Phase 11: Polish & Verification

**Purpose**: End-to-end build and test run validation.

- [X] T018 Run `dotnet build FSBarV1.slnx` and confirm zero errors
- [X] T019 Run `dotnet test --filter "Category=GrpcLogStream"` and confirm all non-live tests pass within 60 s (SC-003)
- [X] T020 Run `dotnet test --filter "Category=GrpcViz|Category=GrpcOverlay|Category=GrpcStateEvents|Category=GrpcPreset"` and confirm zero failures
- [X] T021 Run `dotnet test tests/FSBar.Hub.GrpcTests` with no engine installed; confirm zero failures, only skips for live tests (SC-005)
- [X] T022 Run `dotnet test --filter "Category=GrpcAdmin|Category=GrpcSession"` with engine installed; confirm all live tests pass (FR-019 / FR-020)
- [X] T023 Run `buf breaking` against `proto/hub/scripting.proto` to confirm wire-contract stability (SC-007)

---

## Dependencies

```
T001 (Program.fs) → T006 (HubTestFixture — needs FSBAR_HUB_GRPC_PORT)
T002, T003 (project setup) → T004 (build check) → T005..T009 (foundation)
T005..T009 (foundation) → T010..T017 (all story test files)
T010..T017 → T018..T023 (verification)

Parallel opportunities within foundation:
  T005 (SkipGuards) ∥ T006 (HubTestFixture) ∥ T007 (LogStreamHarness) ∥ T008 (AdminRpcClient)
  (T006 depends on T005 for skip pattern reference; T007, T008 are independent)

Parallel opportunities in story phases (after foundation complete):
  T013 (VizConfigTests) ∥ T014 (OverlayLayerTests) ∥ T015 (StateEventStreamTests) ∥ T016 (PresetEncyclopediaTests) ∥ T017 (CorrelationIdTests)
  T010 (AdminChannelTests) must come after T011 (LogStreamTests) is validated if log stream is used as oracle
```

## Implementation Strategy

**MVP scope** (deliver US1 + US2 first):
1. T001–T004 (setup) → T005–T009 (foundation) → T010 (AdminChannelTests) + T011 (LogStreamTests)
2. Non-live subset runs clean; live tests skip gracefully on no-engine
3. Add remaining stories (T012–T017) incrementally; each is independent

**Suggested first-pass order**: T001 → T002 → T003 → T004 → T005 → T007 → T008 → T006 → T009 → T011 → T010 → T013 → T014 → T015 → T016 → T017 → T012 → T018–T023
