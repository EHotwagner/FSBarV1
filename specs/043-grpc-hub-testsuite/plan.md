# Implementation Plan: Comprehensive gRPC Hub Test Suite

**Branch**: `043-grpc-hub-testsuite` | **Date**: 2026-04-18 | **Spec**: `specs/043-grpc-hub-testsuite/spec.md`  
**Input**: Feature specification from `/specs/043-grpc-hub-testsuite/spec.md`

## Summary

Create a new xUnit test project `tests/FSBar.Hub.GrpcTests/` that launches `FSBar.Hub.App` as a real child process (with a virtual display) and exercises every gRPC RPC in the `fsbar.hub.scripting.v1` service through a live `GrpcChannel`. A `LogStreamHarness` wraps the `StreamLogEntries` server-streaming RPC to serve as the primary assertion oracle across all user stories. One minimal change to `Program.fs` adds `FSBAR_HUB_GRPC_PORT` env-var support so the fixture can inject a dynamically-assigned port.

## Technical Context

**Language/Version**: F# 9 / .NET 10.0  
**Primary Dependencies**: `FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Proto`, `FSBar.Client` (in-repo); `Grpc.AspNetCore 2.67.0`, `FsGrpc 1.0.6` (transitive via Hub); `Grpc.Net.Client 2.67.*` (new, explicit — needed for `GrpcChannel.ForAddress` in the fixture); `xUnit 2.9.x`, `Xunit.SkippableFact 1.4.13`, `Microsoft.NET.Test.Sdk 17.x`  
**Storage**: N/A — test-only; preset cleanup uses `test-043-*` prefix naming convention  
**Testing**: xUnit 2.9.x with `IAsyncLifetime` per-class fixture; `[<SkippableFact>]` for live-engine tests  
**Target Platform**: Linux (dev container), `DISPLAY=:0`, `XDG_RUNTIME_DIR=/tmp/runtime-developer`  
**Project Type**: Test-only project (not packable); joins `FSBarV1.slnx`  
**Performance Goals**: Full non-live suite < 60 s (SC-003); live assertions timeout at 30 s  
**Constraints**: No test passes solely on absence of gRPC error (SC-004); live tests skip gracefully when engine absent (SC-005)  
**Scale/Scope**: ~40–60 test cases covering all 23 FRs; 7 Trait categories

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| §I Spec-first | ✅ PASS | Feature spec exists at `specs/043-grpc-hub-testsuite/spec.md` |
| §II .fsi contracts | ✅ PASS | New test project has no public API; no `.fsi` required. `Program.fs` change adds no new public symbols. |
| §II Surface-area baselines | ✅ PASS | No library modules change. `FSBar.Hub.App` is not packable; no baseline. |
| §III Test evidence | ✅ PASS | This feature IS the test evidence; tests compile and run as proof |
| §IV Observability | ✅ PASS | `HubTestFixture` captures Hub stderr on process start failure and includes in `XunitException` |
| §V FSI scripting | N/A | Test project is not a public library |
| §Eng F# only | ✅ PASS | All code is F# |
| §Eng new dependency | ⚠️ JUSTIFIED | `Grpc.Net.Client 2.67.*` — needed for `GrpcChannel.ForAddress`; Microsoft-maintained; same version family as `Grpc.AspNetCore 2.67.0` already in graph; no maintenance concern; test-project-only scope |

**Post-design re-check**: No library surface changes → gates unchanged.

## Project Structure

### Documentation (this feature)

```text
specs/043-grpc-hub-testsuite/
├── plan.md              # This file
├── research.md          # Phase 0 complete
├── data-model.md        # Phase 1 complete
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code Changes

```text
src/FSBar.Hub.App/
└── Program.fs           # +6 lines: FSBAR_HUB_GRPC_PORT env-var override (R8)

tests/FSBar.Hub.GrpcTests/
├── FSBar.Hub.GrpcTests.fsproj
├── HubTestFixture.fs          # IAsyncLifetime: spawn Hub.App, poll readiness, dispose
├── LogStreamHarness.fs        # WaitForEntry / CollectN / AssertNoUnexpected helpers
├── AdminRpcClient.fs          # Typed admin RPC façade with deadline
├── SkipGuards.fs              # requireEngineInstalled / requireBarInstall (same pattern as LiveTests)
├── AdminChannelTests.fs       # US1 — Pause/Resume/Speed/ForceEnd/Message + Coalesced (FR-001..FR-005)
├── LogStreamTests.fs          # US2 — category filter, severity floor, cap, buffer overflow, truncation (FR-006..FR-008)
├── SessionLifecycleTests.fs   # US3 — ListMaps/ValidateLobby/Launch/Stop/startPaused (FR-009)
├── VizConfigTests.fs          # US4 — SetVizConfig/Attribute/ToggleOverlay/Camera/Tab (FR-010)
├── OverlayLayerTests.fs       # US5 — PutLayer/List/Delete/Clear, cap limits, auto-cleanup (FR-011..FR-013)
├── StateEventStreamTests.fs   # US6 — StreamHubStateEvents (FR-014)
├── PresetEncyclopediaTests.fs # US7 — SavePreset/List/Load/Delete, ListUnits/SelectUnit (FR-015..FR-016)
└── CorrelationIdTests.fs      # Cross-cutting: header propagation, empty SendAdminMessage (FR-017..FR-018)
```

**Structure Decision**: Single new test project alongside existing test projects. Source code change limited to `Program.fs` (no new public modules). Test files map 1:1 to user stories for traceability.

## Complexity Tracking

No constitution violations. No justification table needed.

---

## Phase 0: Research *(complete — see research.md)*

All unknowns resolved:
- **R1**: Port injection via `FSBAR_HUB_GRPC_PORT` env var (minimal `Program.fs` change)
- **R2**: Readiness detection via polling `GetHubState` unary RPC every 500 ms, 15 s max
- **R3**: xUnit `IAsyncLifetime` for async fixture lifecycle
- **R4**: `DISPLAY=:0` + `XDG_RUNTIME_DIR=/tmp/runtime-developer` for Hub.App child process
- **R5**: `IAsyncStreamReader<T>` wrapped by `LogStreamHarness` for streaming RPC tests
- **R6**: `Grpc.Net.Client 2.67.*` as explicit new dependency (justified)
- **R7**: `IClassFixture<HubTestFixture>` per test class; `[<Collection("HubGrpc")>]` for serialization
- **R8**: Minimal `Program.fs` change — 6 lines, no surface-area impact

---

## Phase 1: Design *(complete — see data-model.md)*

### Key entities

- **`HubTestFixture`** (`IAsyncLifetime`): spawns Hub.App process with dynamic port, polls readiness, exposes `Stub` (generated `ScriptingClient`) and `Port`.
- **`LogStreamHarness`**: wraps `IAsyncStreamReader<LogEntry>` with `WaitForEntry` / `CollectN` / `AssertNoUnexpected` and cancellation.
- **`AdminRpcClient`**: typed façade for the 5 admin RPCs with configurable deadline.
- **`SkipGuards`**: `requireBarInstall` / `requireEngineInstalled` matching existing live-test pattern.

### HubTestFixture lifecycle

```
InitializeAsync:
  1. Pick free port via TcpListener(Loopback, 0) → bind → get port → close
  2. Start Process: `dotnet run --project src/FSBar.Hub.App --no-build`
       env: FSBAR_HUB_GRPC_PORT=<port>, DISPLAY=:0, XDG_RUNTIME_DIR=/tmp/runtime-developer
       env: XDG_CONFIG_HOME=<tempdir>  ← isolates Hub settings from user's ~/.config
  3. Poll GetHubState every 500 ms until success or 15 s timeout
  4. If timeout: kill process, collect stderr, throw XunitException with log

DisposeAsync:
  1. Try StopSession (best effort)
  2. Process.Kill(entireProcessTree=true)
  3. await Process.WaitForExitAsync(3 s)
  4. channel.ShutdownAsync()
  5. Delete tempdir (settings isolation dir)
```

**Note on settings isolation**: `XDG_CONFIG_HOME=<tempdir>` ensures the Hub starts with factory-default settings and never touches the user's real settings file. The Hub's settings-load path writes defaults to `$XDG_CONFIG_HOME/fsbar-hub/settings.json` on first run — this lands in tempdir and is cleaned up in `DisposeAsync`.

### Streaming RPC pattern (F# + FsGrpc)

```fsharp
// Open a StreamLogEntries subscription
let call = stub.StreamLogEntries(req, headers=metadata, deadline=deadline)
use harness = LogStreamHarness.create call.ResponseStream cts.Token

// Wait for a specific entry
let! entry = harness.WaitForEntry(fun e -> e.Category = LogCategory.AdminChannel) 30000

// Assert no unexpected Debug entries with Info-floor filter
do! harness.AssertNoUnexpected (fun e -> e.Severity = Severity.Debug) 2000
```

### Correlation ID propagation pattern

```fsharp
let corrId = System.Guid.NewGuid().ToString("N")
let headers = Metadata()
headers.Add("x-fsbar-correlation-id", corrId)
let! resp = stub.GetHubState(req, headers=headers)
let echo = resp.Trailers.GetValue("x-fsbar-correlation-id")
Assert.Equal(corrId, echo)
```

### Coalesced test pattern (FR-001a)

```fsharp
// Fire two SetEngineSpeed calls within 100 ms
let! r1 = admin.SetEngineSpeed(2.0f)
let! r2 = admin.SetEngineSpeed(2.0f)  // simultaneous in Task.WhenAll
// One must be Sent, the other Coalesced (or both Coalesced on very fast system)
Assert.Contains([r1; r2], fun r -> r.Outcome = AdminSubmitResult.Outcome.Coalesced)
```

### Buffer overflow / dropped_since_last pattern (FR-007)

```fsharp
// Pause subscriber consumption while Hub emits many entries
// Then resume and assert first delivered entry has dropped_since_last > 0
// Achieved by: open stream, pause MoveNextAsync, trigger rapid Hub mutations, resume
```

---

## Phase 2: Tasks *(to be generated by `/speckit.tasks`)*

Tasks will be story-grouped with verification steps per user story. Key task groups:

| Group | Tasks |
|-------|-------|
| T001 | `Program.fs`: add `FSBAR_HUB_GRPC_PORT` env-var override |
| T002 | Create `FSBar.Hub.GrpcTests.fsproj`; add to `FSBarV1.slnx` |
| T003 | `HubTestFixture.fs` — process lifecycle + gRPC channel |
| T004 | `LogStreamHarness.fs` — streaming assertion helpers |
| T005 | `AdminRpcClient.fs` + `SkipGuards.fs` |
| T006 | `AdminChannelTests.fs` (US1, FR-001..FR-005) |
| T007 | `LogStreamTests.fs` (US2, FR-006..FR-008) |
| T008 | `SessionLifecycleTests.fs` (US3, FR-009) |
| T009 | `VizConfigTests.fs` (US4, FR-010) |
| T010 | `OverlayLayerTests.fs` (US5, FR-011..FR-013) |
| T011 | `StateEventStreamTests.fs` (US6, FR-014) |
| T012 | `PresetEncyclopediaTests.fs` (US7, FR-015..FR-016) |
| T013 | `CorrelationIdTests.fs` (FR-017..FR-018) |
| T014 | Verify: `dotnet test --filter "Category=GrpcLogStream"` (non-live subset) |
| T015 | Verify: `dotnet test --filter "Category=GrpcAdmin"` (live subset, skip on no-engine) |

### Acceptance criteria traceability

| FR | Test file | Method pattern |
|----|-----------|----------------|
| FR-001 | AdminChannelTests | `Pause returns SENT`, `Pause when no session returns Rejected` |
| FR-001a | AdminChannelTests | `Rapid SetEngineSpeed returns Coalesced` |
| FR-002 | AdminChannelTests | `Pause — log stream confirms PAUSE_SENT entry` |
| FR-003 | AdminChannelTests | `SetEngineSpeed 0.5x/1x/2x/5x/10x all return SENT` |
| FR-004 | AdminChannelTests | `Pause-Resume round trip` |
| FR-005 | AdminChannelTests | `ForceEndMatch terminates session` |
| FR-006 | LogStreamTests | `Subscriber cap ResourceExhausted` |
| FR-007 | LogStreamTests | `Buffer overflow — dropped_since_last non-zero` |
| FR-008 | LogStreamTests | `8 KiB message truncated with marker` |
| FR-009 | SessionLifecycleTests | `Full ListMaps→Launch→Stop cycle` |
| FR-010 | VizConfigTests | `SetVizAttribute mutates GetHubState` |
| FR-011 | OverlayLayerTests | `PutLayer all primitive types` |
| FR-012 | OverlayLayerTests | `17th layer returns capacity error` |
| FR-013 | OverlayLayerTests | `Disconnect clears layers within 5 s` |
| FR-014 | StateEventStreamTests | `SetActiveTab produces StreamHubStateEvents event` |
| FR-015 | PresetEncyclopediaTests | `Save→List→Load→Delete round-trip` |
| FR-016 | PresetEncyclopediaTests | `ListUnits count matches BarData catalogue` |
| FR-017 | CorrelationIdTests | `Empty SendAdminMessage rejected` |
| FR-018 | CorrelationIdTests | `x-fsbar-correlation-id echoed in trailer` |
| FR-019 | All | `[<Trait("Category", ...)>]` on every test |
| FR-020 | All live | `SkipGuards.requireEngineInstalled ()` at test start |
| FR-021 | HubTestFixture | `InitializeAsync` spawns Hub.App process |
| FR-022 | HubTestFixture | `DisposeAsync` cleans presets + overlays |
| FR-023 | SessionLifecycleTests | `Concurrent LaunchSession — second receives FailedPrecondition` |
