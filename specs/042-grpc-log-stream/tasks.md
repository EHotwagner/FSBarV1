---

description: "Task list for feature 042 — Comprehensive gRPC Logging Stream for Hub Diagnostics"
---

# Tasks: Comprehensive gRPC Logging Stream for Hub Diagnostics

**Input**: Design documents from `/specs/042-grpc-log-stream/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Tests are INCLUDED — feature spec FR-018 + plan §III (Test Evidence Is Mandatory) require paired unit + live coverage for every user story.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Maps task to a spec.md user story (US1–US5)
- Every task lists its target absolute path inside the FSBarV1 repo

## Path Conventions

- Repo root: `/home/developer/projects/FSBarV1/`
- Sources: `src/FSBar.Hub/`, `src/FSBar.Hub.App/`, `src/FSBar.Proto/Generated/`, `proto/hub/`
- Tests: `tests/FSBar.Hub.Tests/` (unit), `tests/FSBar.Hub.LiveTests/` (live)
- Scripts: `scripts/examples/`
- Spec: `specs/042-grpc-log-stream/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the build is green at the feature-041 baseline so subsequent changes are cleanly attributable, and lock in the additive-wire-contract guard input.

- [X] T001 Verify clean build + tests at feature-041 baseline: run `dotnet build FSBarV1.slnx` and `dotnet test FSBarV1.slnx --filter "Category!=AdminChannel&Category!=LogStream"` — record any pre-existing failures in `specs/042-grpc-log-stream/build-baseline.txt` so they are not later blamed on this feature
- [X] T002 [P] Snapshot feature-041 proto descriptor for the additive-only guard: from `proto/`, run `buf build -o specs/042-grpc-log-stream/baselines/scripting-041.bin` so `buf breaking --against` in T091 has a frozen comparison target

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land the wire contract, settings field, core `HubLog` bus, and `CorrelationId` carrier infrastructure that every user story phase consumes. Nothing in Phase 3+ can compile until this phase is complete.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Wire contract (proto + regeneration)

- [X] T003 Append the feature-042 additive block from `specs/042-grpc-log-stream/contracts/scripting.proto.delta` into `proto/hub/scripting.proto` — insert the `rpc StreamHubLog` line inside the existing `service ScriptingService { … }` block immediately after `rpc ClearLayers`, then append the two enums (`LogSeverity`, `LogCategory`) and three messages (`LogFilterWire`, `StreamHubLogRequest`, `LogEntryMessage`) at the end of the file
- [X] T004 Regenerate the F# protobuf bindings: from `proto/`, run `buf generate` (requires `protoc-gen-fsgrpc` per CLAUDE.md), then `dotnet build src/FSBar.Proto/FSBar.Proto.fsproj` to confirm `src/FSBar.Proto/Generated/hub/scripting.gen.fs` compiles

### `HubSettings.MaxLogStreamSubscribers` (R7)

- [X] T005 Add `MaxLogStreamSubscribers: int` field (default 8) to the record in `src/FSBar.Hub/HubSettings.fsi` and bump the documented schema version comment from 2 to 3; declare new validator `val updateMaxLogStreamSubscribers: HubSettings -> int -> Result<HubSettings, string>`
- [X] T006 Implement the field, default, schema-v2→v3 load migration (missing field → default 8, re-saves as v3 on next `save`), and `updateMaxLogStreamSubscribers` validator (range `[1, 32]`) in `src/FSBar.Hub/HubSettings.fs`
- [X] T007 [P] Update `tests/FSBar.Hub.Tests/Baselines/HubSettings.baseline` by re-running with `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~SurfaceArea"` and committing the regenerated baseline reflecting the new field + validator
- [X] T008 [P] Add unit test `HubSettingsLogStreamCapTests.fs` to `tests/FSBar.Hub.Tests/` covering: default value 8, validator rejects 0 and 33, validator accepts 1 and 32, v2 JSON without `MaxLogStreamSubscribers` loads with default 8, save round-trips as v3

### `CorrelationId` module (R3)

- [X] T009 Create `src/FSBar.Hub/CorrelationId.fsi` from `specs/042-grpc-log-stream/contracts/CorrelationId.fsi` verbatim, exposing `[<Struct>] CorrelationId`, `HeaderName`, `MaxClientSuppliedBytes`, `current`, `generate`, `withScope`, `tryParseClientHeader`, and the `[<Sealed>] type ServerInterceptor`
- [X] T010 Implement `src/FSBar.Hub/CorrelationId.fs`: `AsyncLocal<CorrelationId option>` carrier, `current`/`generate`/`withScope` (returns `IDisposable` that restores prior value on dispose, including `try…finally` exception path), `tryParseClientHeader` (rejects empty + > 64 UTF-8 bytes), and `ServerInterceptor : Grpc.Core.Interceptors.Interceptor` overriding `UnaryServerHandler`, `ServerStreamingServerHandler`, `ClientStreamingServerHandler`, `DuplexStreamingServerHandler` to read header → set scope → invoke continuation → echo trailer
- [X] T011 [P] Wire the new `.fsi`/`.fs` files into `src/FSBar.Hub/FSBar.Hub.fsproj` immediately before `HubLog.fsi` (CorrelationId has no dependency on HubLog)
- [X] T012 [P] Create `tests/FSBar.Hub.Tests/Baselines/CorrelationId.baseline` by running `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~SurfaceArea"` after T011

### `HubLog` core (R1, R5, R6)

- [X] T013 Create `src/FSBar.Hub/HubLog.fsi` from `specs/042-grpc-log-stream/contracts/HubLog.fsi` verbatim, exposing the `LogSeverity` / `LogCategory` DUs, `LogFilter`, `defaultFilter`, `availablePresets`, `resolveFilter`, `LogEntry`, `T`, `Subscription`, `AttachOutcome`, `create`, `attach`, `updateFilter`, `emit`, `emitSimple`, `emitFromDiagnosticsLine`, `truncateUtf8`, `subscriberCount`, `IDisposable` interface
- [X] T014 Implement `src/FSBar.Hub/HubLog.fs` core data types and pure helpers — `LogSeverity` ordering, `LogCategory` cases (9), `LogFilter` record, `defaultFilter` (`Categories=None; MinSeverity=Info; PresetName=None`), per-R6 `truncateUtf8` (compute UTF-8 byte count, walk back to a UTF-8 lead-byte boundary, append ` …[truncated N bytes]` marker, assert ≤ 8192 bytes by construction), per-R5 `availablePresets` map (`session-lifecycle`, `admin-channel`, `scripting-wire`), `resolveFilter` (case-insensitive preset lookup, explicit categories override preset, unknown preset/category → `Error`)
- [X] T015 Implement `src/FSBar.Hub/HubLog.fs` bus runtime — `T` opaque type holding `IHubEventSink + (unit -> HubSettings.HubSettings) + Subscriber[] (volatile reference) + attachLock (object) + disposed (volatile bool) + globalSequence (Interlocked counter)`; `create` constructor; `subscriberCount` (volatile read); `attach` (lock-acquire → re-check disposed → re-check cap against current `HubSettings.MaxLogStreamSubscribers` → allocate `BoundedChannel<LogEntry>` capacity 256 with `BoundedChannelFullMode.DropOldest` → produce `Subscription` whose `Dispose` is idempotent and removes the subscriber under the same lock); `updateFilter` (atomic `Interlocked.Exchange` of the per-subscriber filter ref); `emitSimple` shim
- [X] T016 Implement `src/FSBar.Hub/HubLog.fs` `emit` hot path per R1 — first do `Volatile.Read` of subscriber-count field, return immediately if zero; otherwise snapshot subscriber array, evaluate per-subscriber filter (category whitelist `Set` membership + severity floor compare) BEFORE invoking the `buildMessage` thunk; once at least one subscriber would accept, invoke thunk once, run `truncateUtf8`, read `CorrelationId.current ()`, build the `LogEntry` record (timestamp from a single `Stopwatch`-backed UTC adapter — no sequence field on the record; per-subscriber `sequence` is stamped by the wire mapper in T028), and `TryWrite` into each accepting subscriber's channel — on `false` (full → DropOldest already evicted), increment that subscriber's `DroppedSinceLast` counter
- [X] T017 Implement `src/FSBar.Hub/HubLog.fs` `emitFromDiagnosticsLine` per R8 — accept caller-supplied category, map `HubEvents.Severity.Info/Warning/Error` to `LogSeverity.Info/Warning/Error`, delegate to `emit`. Implement `IDisposable` on `T`: cancel a shared `CancellationTokenSource`, complete every per-subscriber channel writer, set `disposed=true`, re-acquire the attach lock to flush the subscriber array — guarantee total wall time ≤ 1 s by never `await`-ing on consumer drain
- [X] T018 [P] Wire `HubLog.fsi`/`.fs` into `src/FSBar.Hub/FSBar.Hub.fsproj` immediately after `CorrelationId.fsi`/`.fs`
- [X] T019 [P] Create `tests/FSBar.Hub.Tests/Baselines/HubLog.baseline` by running `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~SurfaceArea"` after T018

### Process-level wiring

- [X] T020 Edit `src/FSBar.Hub.App/Program.fs` to (a) construct one `HubLog.T` via `HubLog.create eventSink (fun () -> currentHubSettings)` at startup before the gRPC service is built, (b) register `services.AddGrpc(fun o -> o.Interceptors.Add<CorrelationId.ServerInterceptor>())`, (c) thread the `HubLog.T` instance through to the `ScriptingService` constructor, (d) dispose the bus on shutdown after `SessionManager` and `ScriptingService`
- [X] T021 Update `src/FSBar.Hub/ScriptingHub.fsi` constructor to accept `log: HubLog.T` as an additional parameter (additive, no removed parameters); update the corresponding `.fs` to store the field for later use
- [X] T022 [P] Update `tests/FSBar.Hub.Tests/Baselines/ScriptingHub.baseline` to reflect the extended constructor signature via `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~SurfaceArea"`
- [X] T023 Run `dotnet build FSBarV1.slnx` to confirm Phase 2 compiles end-to-end before any user-story work begins

**Checkpoint**: Foundation ready — proto regenerated, `HubLog` + `CorrelationId` modules live, settings field landed, surface-area baselines updated, full solution builds. User-story implementation can now begin.

---

## Phase 3: User Story 1 — Stream fine-grained hub logs to a remote test client (Priority: P1) 🎯 MVP

**Goal**: A scripting client opens `StreamHubLog` with no filter and receives every Hub log entry in real time, tagged with timestamp, severity, source category, and message — proving that the canonical session-launch flow emits sufficient detail to debug a test failure remotely.

**Independent Test**: Run `scripts/examples/22-hub-log-stream.fsx`. Within 1 s of triggering any observable Hub action (e.g. `ConfigureLobby` with an invalid map), at least one `LogEntryMessage` arrives whose `message` field matches the failure reason surfaced by the corresponding unary RPC response.

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL before implementation)

- [X] T024 [P] [US1] Add `tests/FSBar.Hub.Tests/HubLogTests.fs` with `streamReceivesEmittedEntries` (attach one subscriber, emit one `Info` entry, drain channel, assert delivery), `truncateUtf8DoesNotExceed8KiBOnPathologicalInputs` (10 KiB ASCII + 10 KiB mixed UTF-8 → both ≤ 8 KiB total bytes including marker, marker present), and `noSubscriberMeansThunkNotInvoked` (assert build-message thunk's side-effect counter remains 0 after 1000 emits with zero subscribers)
- [X] T025 [P] [US1] Add `tests/FSBar.Hub.Tests/HubLogFanOutTests.fs` with `multiSubscriberSeesIdenticalEntries` (attach 3 subscribers each accepting all categories at `Info`, emit 5 entries, drain all three readers, assert byte-identical `message`/`category`/`severity`/`timestamp` across subscribers, and per-subscriber `sequence` on the outgoing wire messages runs `1..5` independently on each reader)
- [X] T026 [P] [US1] Add `tests/FSBar.Hub.LiveTests/LiveAdminChannelLogStreamTests.fs` skeleton (file + `module` + `[<Trait("Category", "LogStream")>]`) with stub `LaunchSessionEmitsAdminChannelTrace : Task` that opens a real gRPC channel against a freshly launched session, subscribes to `StreamHubLog` with no filter, calls `Pause`, drains entries for 2 s, asserts at least one entry exists with `Category = AdminChannel` and message containing `"PAUSE"` — initially expected to fail until T029–T035 land

### Implementation for User Story 1

- [X] T027 [US1] Add the `StreamHubLog` bidi-streaming handler to `src/FSBar.Hub/ScriptingHub.fs` (no `.fsi` change — gRPC service methods are wired through the generated stub, not the F# surface). Read the first request message, call `HubLog.resolveFilter` on its `LogFilterWire` fields, call `HubLog.attach` against the cap, on `Rejected` throw an `RpcException(Status(StatusCode.ResourceExhausted, reason))`, on `Attached` start a write-loop that drains the subscription's `ChannelReader<LogEntry>` to the response stream as `LogEntryMessage` values until the call's `ServerCallContext.CancellationToken` fires, and dispose the subscription in `finally`
- [X] T028 [US1] Add a private mapper inside `src/FSBar.Hub/ScriptingHub.fs` from `HubLog.LogEntry` to the generated `LogEntryMessage` proto record, mapping `LogSeverity`/`LogCategory` 1:1 to the proto enums, emitting empty strings for `None` correlation/session/client IDs, assigning `sequence = Interlocked.Increment(&subscriber.NextSequence)` (per-subscriber counter starting at 1), and reading the subscriber's `DroppedSinceLast` (atomic exchange to 0) into `dropped_since_last` on each delivery
- [X] T029 [P] [US1] Add session-lifecycle `Info` emit sites in `src/FSBar.Hub/SessionManager.fs` at every state transition (Launching, Running, Pausing, Paused, Resuming, Ending, Stopped, Failed) and on every admin dispatch (`Pause`, `Resume`, `SetEngineSpeed`, `ForceEnd`, `SendAdminMessage`) carrying the session's GUID via the explicit `sessionId` parameter; no `.fsi` change required
- [X] T030 [P] [US1] Add `Info`-level `AdminChannel` emit sites in `src/FSBar.Hub/AdminChannelHost.fs` for every status transition (`Attached`, `Lost(reason)`, `Unavailable(reason)`) and every `SubmitOutcome` (`Sent`, `Coalesced n`, `Rejected r`); no `.fsi` change required
- [X] T031 [P] [US1] Take a `log: HubLog.T` parameter into the `ScriptingService` constructor (already added in T021) and propagate it via `Program.fs` (already wired in T020) — verify the `StreamHubLog` handler can call `log.attach` and emit infrastructure entries
- [X] T032 [US1] Create `scripts/examples/22-hub-log-stream.fsx` per quickstart §2 — open `GrpcChannel` to `http://127.0.0.1:5021`, open `StreamHubLog`, send initial `StreamHubLogRequest` with empty filter (default = all categories at `Info`), spawn a background reader that prints `[timestamp] severity category corrId — message`, drive the launch → pause → resume → set-speed → force-end sequence using existing feature-040 RPCs, exit cleanly on Ctrl-C
- [X] T033 [US1] Run T024 + T025 unit tests; expect them to pass with `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~HubLog"`
- [X] T034 [US1] Run T026 live test against a real session: `dotnet test tests/FSBar.Hub.LiveTests --filter "FullyQualifiedName~LaunchSessionEmitsAdminChannelTrace"`; expect the `AdminChannel`-tagged entry to be observed within the 2 s window
- [X] T035 [US1] Manual smoke test of `scripts/examples/22-hub-log-stream.fsx` end-to-end: confirm at least one entry per session-state transition appears, no entry has `category = LOG_CATEGORY_UNSPECIFIED`, and the stream cancels cleanly on Ctrl-C

**Checkpoint**: User Story 1 fully functional — fine-grained logs reach a connected client over gRPC for every observable Hub action in the launch / admin-dispatch path.

---

## Phase 4: User Story 2 — Filter logs by feature/category and severity (Priority: P1)

**Goal**: A subscriber can submit a category whitelist + severity floor at attach time, mutate it later over the open stream, and `Debug`-level entries (admin-channel wire trace, scripting RPC dispatch) are delivered when explicitly requested.

**Independent Test**: A test client opens `StreamHubLog` with `categories=[AdminChannel]` and `min_severity=DEBUG`, drives a series of preset-save and camera-pan actions (which emit nothing on those categories), then triggers a pause — receives only admin-channel entries. Sends an update adding `SessionManager` → both categories now flow.

### Tests for User Story 2 ⚠️

- [X] T036 [P] [US2] Add `tests/FSBar.Hub.Tests/HubLogFilterTests.fs` with `categoryWhitelistExcludesOthers` (subscribe to `[AdminChannel]`, emit one `AdminChannel` + one `SessionManager`, assert only first delivered), `severityFloorDropsLower` (subscribe at `Warning`, emit `Debug`/`Info`/`Warning`/`Error`, assert only the latter two delivered), `filterMutationAppliesOnNextEntry` (call `updateFilter` adding a new category, emit one entry under each category, assert new category's entry now delivered while in-flight under the old filter is still delivered), `unknownCategoryNameRejected` (`resolveFilter` with an `LOG_CATEGORY_UNSPECIFIED` value returns `Error`)
- [X] T037 [P] [US2] Add `tests/FSBar.Hub.Tests/HubLogTruncationTests.fs` with `messageOver8KiBTruncatedWithMarker` (build a 12 KiB ASCII message, emit, assert delivered byte length ≤ 8192 and ends with `…[truncated N bytes]` where N is correct), `multiByteUtf8RespectsCodePointBoundary` (build a string of 4 KiB of `"日本語"` cycles + 6 KiB of ASCII tail, assert truncation cut does not split a multi-byte sequence — re-decoding succeeds), `messageBelow8KiBUnchanged` (assert short message passes through byte-identical, no marker)
- [X] T038 [P] [US2] Extend `tests/FSBar.Hub.LiveTests/LiveAdminChannelLogStreamTests.fs` with `FilterMutationTakesEffectMidSession` — subscribe with `[AdminChannel]` at `Debug`, drive a `Pause`, drain entries, assert only `AdminChannel` entries; then send an in-stream filter update adding `SessionManager` and emitting one ack `Debug` entry, drive a second `Pause`, assert both categories now appear

### Implementation for User Story 2

- [X] T039 [US2] In `src/FSBar.Hub/ScriptingHub.fs`, extend the `StreamHubLog` handler to also poll the `IAsyncStreamReader<StreamHubLogRequest>` request side after the initial request: on each subsequent message, call `HubLog.resolveFilter` on its `filter` field, call `HubLog.updateFilter`, on success emit one synthetic ack entry via `HubLog.emitSimple log ScriptingHub Debug (fun () -> sprintf "log-stream filter updated: categories=[%s], minSeverity=%A" (string categories) minSeverity)`, on failure throw `RpcException(InvalidArgument, reason)` and terminate the call
- [X] T040 [US2] In `src/FSBar.Hub/ScriptingHub.fs`, add `Debug`-level emit at every RPC dispatch entry and completion (FR-004a) — wrap each existing handler with `HubLog.emit log ScriptingHub Debug None (Some clientGuid) (fun () -> sprintf "rpc dispatch: %s entered" rpcName)` on entry and `... "rpc dispatch: %s completed (%dms)" rpcName elapsed` on exit (in `finally`); use a small helper to avoid copy-pasting the wrapper across the 25+ existing RPCs
- [X] T041 [US2] In `src/FSBar.Hub/AdminChannelHost.fs`, add `Debug`-level emit per FR-004a for every outbound wire datagram (after framing, before send: command kind + payload field summary) and every inbound parsed event (after parse, before dispatch: event kind + payload field summary); session GUID flows through the existing context
- [X] T042 [US2] Run T036 + T037 unit tests: `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~HubLogFilter|FullyQualifiedName~HubLogTruncation"`; expect green
- [X] T043 [US2] Run T038 live test against a real session: `dotnet test tests/FSBar.Hub.LiveTests --filter "FullyQualifiedName~FilterMutationTakesEffectMidSession"`; expect green

**Checkpoint**: User Stories 1 + 2 both functional — clients can subscribe with no filter (wide stream) or narrow to category/severity, and mutate the filter live.

---

## Phase 5: User Story 3 — Correlate log entries with RPC calls and session events (Priority: P2)

**Goal**: Every log entry emitted while handling a unary RPC carries a correlation identifier — Hub-assigned by default, client-supplied via metadata header when present — and the Hub echoes the effective ID back in the response trailer so a test can assert "all entries between my call and my response carry my ID."

**Independent Test**: A test client issues a `Pause` RPC supplying `x-fsbar-correlation-id: my-test-id-42` in the request metadata. The response trailing metadata echoes the same value. At least one entry on the parallel `StreamHubLog` between the client-side call and client-side response carries `correlation_id = "my-test-id-42"`.

### Tests for User Story 3 ⚠️

- [X] T044 [P] [US3] Add `tests/FSBar.Hub.Tests/CorrelationIdInterceptorTests.fs` with `autoAssignsIdWhenHeaderAbsent` (invoke handler through interceptor with empty headers, assert `current()` inside handler returns `Some (CorrelationId hex32)`, assert response trailer present with same value), `honoursClientSuppliedId` (invoke with header `x-fsbar-correlation-id: client-id-abc`, assert `current()` returns `Some (CorrelationId "client-id-abc")` and trailer echoes), `tooLongClientHeaderRejected` (invoke with 65-byte header, assert `RpcException(InvalidArgument)`), `backgroundTaskInheritsViaExplicitScope` (capture id outside, `Task.Run` with `use _ = withScope cidOpt`, assert `current()` inside task matches), `scopeRestoresPriorOnDispose` (nested `withScope` calls restore correctly)
- [X] T045 [P] [US3] Extend `tests/FSBar.Hub.Tests/HubLogTests.fs` with `emitPicksUpAsyncLocalCorrelationId` — open `withScope (Some cid)`, call `HubLog.emit`, assert delivered `LogEntry.CorrelationId = Some cid`; without scope, assert `None`
- [X] T046 [P] [US3] Extend `tests/FSBar.Hub.LiveTests/LiveAdminChannelLogStreamTests.fs` with `PauseRpcLogsCarryCorrelationId` — open `StreamHubLog` (default filter), call `Pause` with metadata header `x-fsbar-correlation-id: live-test-001`, drain entries 2 s, assert at least one `AdminChannel` or `SessionManager` entry has `correlation_id = "live-test-001"`, and `TwoConcurrentPausesGetDistinctIds` — issue two `Pause` calls in parallel, assert their entries carry distinct IDs that match each call's response trailer

### Implementation for User Story 3

- [X] T047 [US3] Verify the `CorrelationId.ServerInterceptor` from T010 is wired in `Program.fs` (T020) and runs for unary, server-streaming, client-streaming, and bidi RPCs. The interceptor MUST NOT depend on `HubLog` (preserves the module order pinned in T011 — `CorrelationId` compiles before `HubLog`). Visibility of RPC entry on `StreamHubLog` is delivered by T040's ScriptingHub-side Debug dispatch emits, which run inside the correlation scope the interceptor established and therefore carry the effective ID.
- [X] T048 [US3] In `src/FSBar.Hub/SessionManager.fs`, document the background-task pattern from R3 — wherever an RPC handler kicks off `Task.Run` / `Async.StartAsTask` (e.g. `Launch`'s background connect task), capture `let cid = CorrelationId.current ()` in the handler's body and wrap the spawned block with `use _ = CorrelationId.withScope cid` so its log entries inherit the RPC's ID; apply this transform to every existing background spawn site in `SessionManager` and `AdminChannelHost`
- [X] T049 [US3] Run T044 + T045 unit tests: `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~CorrelationId|FullyQualifiedName~emitPicksUpAsyncLocal"`; expect green
- [X] T050 [US3] Run T046 live tests: `dotnet test tests/FSBar.Hub.LiveTests --filter "FullyQualifiedName~PauseRpcLogsCarryCorrelationId|FullyQualifiedName~TwoConcurrentPausesGetDistinctIds"`; expect green
- [X] T051 [US3] Update `scripts/examples/22-hub-log-stream.fsx` from T032 to demonstrate correlation: send a custom `x-fsbar-correlation-id` header on at least one admin RPC, read the response trailer, and verify in the printed stream that entries between call/response carry the same ID

**Checkpoint**: User Stories 1 + 2 + 3 functional — log entries are attributable to their originating RPC end-to-end.

---

## Phase 6: User Story 4 — Control log volume and retention to avoid drowning the transport (Priority: P2)

**Goal**: A slow subscriber causes per-client backpressure — the Hub drops the oldest queued entries (not newest), reports the dropped count on the next delivered entry, and never blocks any non-logging Hub operation. Disposed subscriptions release per-client state within 1 s.

**Independent Test**: A throttled client consumes 1 entry/s while the Hub emits 100 entries/s for 10 s (1000 entries × 4 bytes hub-side cost). The client receives a non-zero `dropped_since_last` on at least one entry; the Hub's interactive frame rate measured in parallel is unaffected. After cancelling, attach a fresh subscriber and confirm the previous subscriber's state is gone.

### Tests for User Story 4 ⚠️

- [X] T052 [P] [US4] Extend `tests/FSBar.Hub.Tests/HubLogFanOutTests.fs` with `slowSubscriberDropsOldestAndReportsCount` (attach a subscriber that does not drain, `emit` 1024 entries — assert subscriber still has channel ≤ capacity 256 — drain one entry, assert `DroppedSinceLast > 0` and the entry is not the very-first-emitted one), `disposeReleasesPerSubscriberStateWithin1s` (attach subscriber, dispose, assert `subscriberCount` returns 0 within 1 s — measured), `cancellationTokenFromGrpcReleasesSubscriber` (attach with a cancellation token, cancel the token, assert subscriber removed within 1 s), `slowSubscriberDoesNotBlockOtherSubscribers` (one fast + one slow subscriber, fast subscriber drains all `N` entries while slow gets some + a drop counter; emit loop never exceeds wall clock proportional to `N / fastDrainRate`)

### Implementation for User Story 4

- [X] T053 [US4] Verify the `BoundedChannel` capacity 256 + `BoundedChannelFullMode.DropOldest` is in place in `HubLog.fs` from T015; add an explicit assert at channel construction (`if capacity <> 256 then failwith …`) so future regressions are immediately visible
- [X] T054 [US4] In `src/FSBar.Hub/HubLog.fs`, on the emit hot path's `TryWrite` returning `false`, atomically `Interlocked.Increment` the per-subscriber `DroppedSinceLast`; on the delivery path inside `ScriptingHub.fs` mapper (T028), atomically `Interlocked.Exchange` the counter to 0 when copying it into the outgoing `LogEntryMessage.dropped_since_last`
- [X] T055 [US4] In `src/FSBar.Hub/HubLog.fs`, ensure `Subscription.Dispose` is idempotent and runs the entire detach (cancel CTS, complete channel writer, remove from subscriber array) synchronously — no `Async.Start` / `Task.Run` — so FR-013's 1-second budget is structural, not best-effort
- [X] T056 [US4] Run T052 unit tests: `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~slowSubscriber|FullyQualifiedName~disposeReleases|FullyQualifiedName~cancellationToken"`; expect green

**Checkpoint**: User Stories 1 + 2 + 3 + 4 functional — overflow + cancellation are correct, hub remains responsive under slow consumers.

---

## Phase 7: User Story 5 — Default sensible filters for common debug sessions (Priority: P3)

**Goal**: A test author who supplies a preset name in `LogFilterWire.preset_name` gets the documented category + severity bundle without enumerating cases. When both a preset and an explicit category list are supplied, the explicit list overrides the preset.

**Independent Test**: A scripting client subscribes with `preset_name="session-lifecycle"` (no explicit categories). The delivered entries match the preset's documented set: `SessionManager + AdminChannel + ProxyInstall` at `Info`. A subscription with `preset_name="admin-channel"` AND `categories=[SessionManager]` delivers only `SessionManager` (explicit overrides preset).

### Tests for User Story 5 ⚠️

- [X] T057 [P] [US5] Extend `tests/FSBar.Hub.Tests/HubLogTests.fs` with `presetBundlesCategoriesAndFloor` (call `resolveFilter [] None (Some "session-lifecycle")`, assert returned `LogFilter.Categories` contains exactly `SessionManager + AdminChannel + ProxyInstall` and `MinSeverity = Info`), `explicitCategoriesOverridePreset` (call `resolveFilter [SessionManager] None (Some "admin-channel")`, assert returned `Categories = Some {SessionManager}` not the preset's `{AdminChannel}`), `presetNameLookupCaseInsensitive` (verify `"Session-Lifecycle"` and `"SESSION-LIFECYCLE"` resolve identically), `unknownPresetNameRejected` (`resolveFilter [] None (Some "verbose")` returns `Error` with the bad name in the message)

### Implementation for User Story 5

- [X] T058 [US5] Confirm `availablePresets` from T014 contains exactly the three documented entries (`session-lifecycle`, `admin-channel`, `scripting-wire`) with the values in research.md R5 and data-model.md §6; if any drift exists, correct in `src/FSBar.Hub/HubLog.fs`
- [X] T059 [US5] In `src/FSBar.Hub/HubLog.fs`, ensure `resolveFilter` returns a `LogFilter` whose `PresetName` field is preserved for diagnostics even when explicit categories override the preset (data-model.md §4 invariant)
- [X] T060 [US5] Run T057 unit tests: `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~preset"`; expect green
- [X] T061 [US5] Update `scripts/examples/22-hub-log-stream.fsx` to use `preset_name="session-lifecycle"` for its primary subscription so a new contributor sees the preset surface in action (SC-005)

**Checkpoint**: All user stories now functional — clients can use presets for ergonomic defaults while retaining explicit override.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Land the remaining `HubLog` emit sites for full FR-004 coverage, the FR-014 `DiagnosticsLine` bridge, FR-018's motivating live test, the additive-wire-contract guard, and documentation refresh.

### Remaining FR-004 emission sites (no `.fsi` changes)

- [X] T062 [P] Add `Info`-level emit sites in `src/FSBar.Hub/HeadlessRenderer.fs` for subscribe / detach / overflow events; `Debug`-level emit on each frame encode summary (frame number, byte size, encode duration ms)
- [X] T063 [P] Add `Info`-level emit sites in `src/FSBar.Hub/HubStateStore.fs` for every successful mutator (`setVizConfig`, `setVizAttribute`, `setEncyclopedia`, `setSettings`, `setActiveTab`) and `Warning`-level for every `SubmitOutcome.Rejected reason` (per the existing CLAUDE.md "HubStateStore.<mutator> rejected: <reason>" convention)
- [X] T064 [P] Add `Info`-level emit sites in `src/FSBar.Hub/ProxyInstaller.fs` per install step (in addition to the existing `HubEvent` publish — feature 042 mirrors, does not replace)
- [X] T065 [P] Add `Warning`-level emit sites in `src/FSBar.Hub/LobbyConfig.fs` for every validation failure path (one entry per discrete error)
- [X] T066 [P] Add `Info`-level emit sites in `src/FSBar.Hub/HubSettings.fs` for load (with schema version detected) and successful save (with schema version written)
- [X] T067 [P] Add `Info`-level emit sites for preset persistence inside `src/FSBar.Hub/ScriptingHub.fs` (the `SavePreset` / `LoadPreset` / `DeletePreset` RPC handlers per plan §1.3) tagged `LogCategory.PresetPersistence`

### FR-014 `DiagnosticsLine` bridge (R8)

- [X] T068 In `src/FSBar.Hub/HubLog.fs`, ensure `emitFromDiagnosticsLine` is implemented (T017) and exposed in the `.fsi` (T013); this is the single helper every existing `HubEvents.DiagnosticsLine` call site will adopt
- [X] T069 Audit every existing `HubEvents.DiagnosticsLine` publish site across `src/FSBar.Hub/` (`rg "DiagnosticsLine" src/FSBar.Hub` — research.md cites 24 sites across 10 files); for each call site, add a sibling `HubLog.emitFromDiagnosticsLine log <owningCategory> severity sessionId clientId message` invocation. The `HubEventBus.Publish` call MUST remain so local GUI consumers continue to receive the event unchanged (FR-014)
- [X] T070 [P] Add `tests/FSBar.Hub.Tests/HubLogBridgeTests.fs` with `mirroredDiagnosticsLineProducesLogEntry` (publish a `DiagnosticsLine(Warning, "x")` from a known caller, assert both: (a) `HubEventBus` subscriber observes the event, (b) `HubLog` subscriber observes a sibling entry with the expected category + `LogSeverity.Warning`), `bridgeAttributesEachCallSiteToCorrectCategory` (parametric across the 24 call sites identified in T069 — each gets the documented owning category)

### FR-018 motivating end-to-end live test

- [X] T071 Extend `tests/FSBar.Hub.LiveTests/LiveAdminChannelLogStreamTests.fs` with `FullAdminCycleEmitsExpectedEntries` — launch a session, subscribe to `StreamHubLog` with `[AdminChannel; SessionManager]` at `Debug`, drive `Pause → Resume → SetEngineSpeed 2.0 → ForceEnd`, assert: (a) every operation produced ≥ 1 entry on `AdminChannel`, (b) every entry's `correlation_id` matches its operation's response trailer, (c) at least one `AdminChannelStatusChanged`-equivalent transition is reflected in the stream's narrative; tagged `[<Trait("Category", "LogStream")>]`
- [X] T072 Run the full LogStream live matrix: `dotnet test tests/FSBar.Hub.LiveTests --filter "Category=LogStream"`; expect every test green; record runtime in `specs/042-grpc-log-stream/livetest-runtime.txt` for SC-002/SC-003 baseline

### SC-003 10-minute soak (FR-011 / SC-003)

- [X] T072a [P] Add `tests/FSBar.Hub.LiveTests/LogStreamSoakTests.fs` with `SlowLogSubscriberDoesNotStallRenderOrEventsOver10Min` tagged `[<Trait("Category", "LogStreamSoak")>]` — launch a session, subscribe concurrently to (a) `StreamHubLog` at `Debug` across all categories with a consumer throttled to 1 entry/s, (b) `StreamRenderFrames`, (c) `StreamHubStateEvents`. While the Hub emits at ≥ 100 log entries/s (driven by a background loop calling `HubLog.emitSimple` on a benign category), assert over a 10-min window: (1) render-frame arrival rate stays within 10% of the no-slow-subscriber baseline captured in T072, (2) every `SetActiveTab` / `SetVizConfig` / `ToggleOverlay` round-trip completes within the feature-041 p99 budget, (3) the slow subscriber receives at least one entry carrying `dropped_since_last > 0`, (4) no `LogEntry` arrives out of per-subscriber `sequence` order. Tagged separately from `Category=LogStream` so CI can opt out by default.
- [X] T072b Run the soak: `dotnet test tests/FSBar.Hub.LiveTests --filter "Category=LogStreamSoak"`; expect green. Record runtime + frame-rate deltas in `specs/042-grpc-log-stream/livetest-soak.txt`.

### SC-006 additive-only contract verification

- [X] T073 From `proto/`, run `buf breaking proto --against ../specs/042-grpc-log-stream/baselines/scripting-041.bin`; expect zero breaking changes — a non-zero exit must block the feature
- [X] T074 [P] Run feature-040 + feature-041 example scripts unmodified (`dotnet fsi scripts/examples/16-hub-admin.fsx`, `17-hub-lobby-launch.fsx`, `18-hub-render-frames.fsx`, `19-hub-vizconfig-drive.fsx`, `20-hub-state-observer.fsx`, `21-hub-overlay-layers.fsx`) against the updated Hub; expect every script to still run end-to-end (SC-006)

### Documentation refresh

- [X] T075 [P] Append a `## Hub log stream (feature 042)` section to `CLAUDE.md` at the project root summarising: `HubLog` module location, the nine `LogCategory` cases, `defaultFilter`, the three shipped presets, the `MaxLogStreamSubscribers` setting, the `x-fsbar-correlation-id` header convention, and the example script `scripts/examples/22-hub-log-stream.fsx`
- [X] T076 [P] Refresh `tests/README.md` to add the new `Category=LogStream` test trait alongside `Category=AdminChannel` so contributors discover the live suite filter
- [X] T077 [P] Run `dotnet build FSBarV1.slnx -warnaserror` (or equivalent CI command) and `dotnet test FSBarV1.slnx --filter "Category!=AdminChannel&Category!=LogStream"` to confirm no regressions to non-live tests
- [X] T078 Run `specs/042-grpc-log-stream/quickstart.md` end-to-end manually: launch the Hub, run the example script, observe the documented behaviour for steps 2–6 within 5 minutes (SC-005)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 + T002 — independent, can run in parallel
- **Foundational (Phase 2)**: depends on Setup completion; T003 → T004 sequential (proto regen needs the `.proto` edit); T005 → T006 → T007/T008 sequential per `HubSettings`; T009 → T010 → T011/T012 sequential per `CorrelationId`; T013 → T014 → T015 → T016 → T017 → T018/T019 sequential per `HubLog`; T020 → T021 → T022 sequential per gRPC wiring; T023 final integration check after all foundational work
- **User Stories (Phase 3+)**: every user-story phase depends on Foundational (Phase 2) being complete (T023 green)
  - US1 (P1): tests T024–T026 may be authored in parallel; impl T027 → T028 → T029/T030/T031 (parallel) → T032 → T033/T034/T035 sequential
  - US2 (P1): depends on US1's `StreamHubLog` handler (T027); tests T036–T038 in parallel; impl T039 → T040 → T041 → T042/T043
  - US3 (P2): depends on Foundational `CorrelationId` module (T010 + T020); tests T044–T046 in parallel; impl T047 → T048 → T049/T050 → T051
  - US4 (P2): depends on US1's `HubLog` runtime (Foundational T015); tests T052 in parallel with each other; impl T053 → T054 → T055 → T056
  - US5 (P3): depends on Foundational `resolveFilter` (T014); tests T057 in parallel; impl T058 → T059 → T060 → T061
- **Polish (Phase 8)**: depends on US1–US5 complete; T062–T067 in parallel; T068 → T069 → T070 sequential per bridge; T071 → T072 sequential per live test; T072a → T072b sequential for the 10-min soak (independent of T073/T074, but runs long so schedule accordingly); T073 / T074 / T075–T078 in parallel

### User Story Dependencies

- **US1 (P1, MVP)**: depends only on Foundational (Phase 2)
- **US2 (P1)**: depends on US1's `StreamHubLog` handler (T027) — filter mutation extends the same handler
- **US3 (P2)**: depends only on Foundational (Phase 2) — independent of US1's emit sites; can be implemented in parallel with US2 once Foundational is green
- **US4 (P2)**: depends only on Foundational (Phase 2) `HubLog` runtime — independent of US1/US2/US3 emit-site work; can be implemented in parallel
- **US5 (P3)**: depends only on Foundational (Phase 2) `resolveFilter` — independent of US1–US4 in implementation; example script update in T061 needs T032 to exist

### Within Each User Story

- Tests are authored FIRST and expected to FAIL before the implementation tasks land
- Implementation moves left-to-right by file: protocol/wire → core types → handler → emit sites → wiring
- Each user-story phase ends in a unit-test green run + (where applicable) a live-test green run before moving on
- Commit at each Checkpoint so the bisect range stays narrow

### Parallel Opportunities

- **Phase 1**: T001 + T002 in parallel
- **Phase 2 fan-out**: once T004 is done, the four module-level work streams (HubSettings T005–T008, CorrelationId T009–T012, HubLog T013–T019, wiring T020–T023) can each be picked up by a different contributor; within each stream the order is fixed
- **US1 tests**: T024 + T025 + T026 can be authored in parallel
- **US1 emit sites**: T029 + T030 + T031 in parallel (different files)
- **US2/US3/US4/US5 in parallel team**: once Foundational is green, US2 / US3 / US4 / US5 can each go to a separate contributor since they touch different code paths (US2 = filter mutation in `ScriptingHub.fs` + `AdminChannelHost.fs` Debug emits; US3 = interceptor + `SessionManager.fs` background scopes; US4 = `HubLog.fs` runtime + mapper; US5 = `HubLog.fs` preset table)
- **Polish emission sites**: T062–T067 each touch a different file — fully parallel
- **Polish docs**: T075 + T076 + T077 in parallel

---

## Parallel Example: Foundational Phase

```bash
# After T004 (proto regen) lands, four contributors can pick up:
# Stream A: HubSettings
Task: "T005 Add MaxLogStreamSubscribers field to HubSettings.fsi"
Task: "T006 Implement field + migration + validator in HubSettings.fs"
# Stream B: CorrelationId
Task: "T009 Create CorrelationId.fsi from contracts/CorrelationId.fsi"
Task: "T010 Implement CorrelationId.fs with AsyncLocal + ServerInterceptor"
# Stream C: HubLog
Task: "T013 Create HubLog.fsi from contracts/HubLog.fsi"
Task: "T014 Implement HubLog.fs core types + resolveFilter + truncateUtf8"
# Stream D: gRPC wiring (waits for T021 surface change)
Task: "T020 Edit Program.fs to construct HubLog + register interceptor"
```

## Parallel Example: User Story 1

```bash
# Author tests in parallel:
Task: "T024 Add HubLogTests.fs with streamReceivesEmittedEntries + truncate + noSubscriber tests"
Task: "T025 Add HubLogFanOutTests.fs with multiSubscriberSeesIdenticalEntries"
Task: "T026 Add LiveAdminChannelLogStreamTests.fs skeleton + LaunchSessionEmitsAdminChannelTrace stub"

# Author emit sites in parallel:
Task: "T029 Add SessionManager Info emits at every state transition"
Task: "T030 Add AdminChannelHost Info emits at every status transition + SubmitOutcome"
Task: "T031 Verify ScriptingService receives HubLog parameter and StreamHubLog can attach"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T002)
2. Complete Phase 2: Foundational (T003–T023) — wire contract + bus + interceptor + settings ready
3. Complete Phase 3: User Story 1 (T024–T035) — clients receive a fine-grained log stream end-to-end
4. **STOP and VALIDATE**: run `scripts/examples/22-hub-log-stream.fsx` against a real session; verify a connected client sees session-state and admin-channel entries within 1 s of the corresponding action
5. Ship the MVP — the motivating debug-from-tests scenario from the spec is covered

### Incremental Delivery

1. Setup + Foundational → all infrastructure ready
2. + US1 (P1) → MVP: clients see every Hub action remotely
3. + US2 (P1) → narrow filters + live mutation + Debug-level wire trace
4. + US3 (P2) → correlation IDs make tests structurally assertable
5. + US4 (P2) → drop handling makes long CI runs robust
6. + US5 (P3) → presets onboard new contributors faster
7. + Polish (Phase 8) → full FR-004 coverage, DiagnosticsLine bridge, additive-wire-contract guard, docs

### Parallel Team Strategy

With 4 contributors after Foundational lands:

- **Contributor A**: US1 then US2 (same code file `ScriptingHub.fs` for the bidi handler — must be sequential)
- **Contributor B**: US3 (interceptor + `SessionManager.fs` background scopes)
- **Contributor C**: US4 (`HubLog.fs` runtime hardening)
- **Contributor D**: US5 + Polish T062–T067 emission sites

All four converge on Phase 8 final tasks (T068–T078).

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks; safe to issue in parallel
- [Story] label maps task to its spec.md user story for traceability — every entry in Phase 3+ has one
- Tests are authored FIRST per Plan §III; observe them fail before the corresponding implementation task lands
- Surface-area baselines (T007, T012, T019, T022) MUST be regenerated whenever a `.fsi` changes — failure to do so yields a baseline-diff failure on `dotnet test`
- The proto regen in T004 requires `protoc-gen-fsgrpc` on PATH; install via `~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh` per CLAUDE.md
- The additive-wire-contract guard in T073 + T074 is non-negotiable — feature 040 + 041 example scripts MUST keep working unmodified (FR-017 / SC-006)
- Avoid: skipping the `HubEventBus.Publish` calls when adding sibling `HubLog.emitFromDiagnosticsLine` invocations (T069) — the bridge mirrors, never replaces
- Commit after each task or each numbered Checkpoint so a bisect through the feature stays narrow
