# Data Model: 043-grpc-hub-testsuite

This feature is a test-only project. There are no new persistent entities or storage formats. The entities below are in-process test helpers only.

---

## HubTestFixture

Manages the lifetime of one `FSBar.Hub.App` child process and one gRPC channel for an entire test class.

```
HubTestFixture
├── process: Process                    // Hub.App child process (started in InitializeAsync)
├── grpcPort: int                       // dynamically-assigned loopback port
├── channel: GrpcChannel                // GrpcChannel.ForAddress("http://127.0.0.1:<port>")
├── stub: ScriptingHub.ScriptingClient  // generated gRPC stub
├── InitializeAsync: unit -> Task       // spawn process, wait for gRPC readiness (15 s timeout)
└── DisposeAsync: unit -> ValueTask     // kill process, dispose channel
```

**State machine**: `Unstarted → Starting → Ready → Disposed`  
Only `Ready` allows test code to call `stub`.

---

## LogStreamHarness

Wraps a server-streaming `StreamLogEntries` call with assertion helpers.

```
LogStreamHarness
├── reader: IAsyncStreamReader<LogEntry>
├── cts: CancellationTokenSource
├── WaitForEntry: (LogEntry -> bool) -> timeoutMs:int -> Task<LogEntry>
│       // polls MoveNextAsync until predicate matches or timeout → raises Xunit.SkipException on timeout
├── CollectN: n:int -> timeoutMs:int -> Task<LogEntry list>
│       // collects exactly N entries within window
├── AssertNoUnexpected: (LogEntry -> bool) -> windowMs:int -> Task
│       // reads for windowMs; fails if any entry matches the "unexpected" predicate
└── Dispose: unit -> unit              // cancels cts
```

---

## AdminRpcClient

Typed façade over the raw stub for admin operations, pre-configured with a per-operation deadline.

```
AdminRpcClient
├── stub: ScriptingHub.ScriptingClient
├── defaultTimeoutMs: int              // 30 000 for live, 5 000 for non-live
├── Pause: unit -> Task<AdminSubmitResult>
├── Resume: unit -> Task<AdminSubmitResult>
├── SetEngineSpeed: float32 -> Task<AdminSubmitResult>
├── ForceEndMatch: unit -> Task<AdminSubmitResult>
└── SendAdminMessage: string -> Task<AdminSubmitResult>
```

---

## HubStateSnapshot

Captures a `GetHubState` response at a point in time for before/after comparison.

```
HubStateSnapshot
├── raw: GetHubStateResponse
├── ActiveTab: HubTab
├── VizConfig: VizConfigWire option
├── Camera: ViewerCameraWire option
├── AdminChannelStatus: AdminChannelStatusInfo option
└── static Capture: ScriptingClient -> Task<HubStateSnapshot>
```

---

## Test categories (Trait values)

| Trait value | Meaning |
|-------------|---------|
| `GrpcAdmin` | Admin RPCs (Pause/Resume/Speed/ForceEnd/Message) — requires live engine |
| `GrpcLogStream` | StreamLogEntries validation — no engine needed |
| `GrpcSession` | Session lifecycle via gRPC — requires live engine |
| `GrpcViz` | VizConfig/Camera/Overlay RPCs — no engine needed |
| `GrpcOverlay` | Overlay layer CRUD — no engine needed |
| `GrpcStateEvents` | StreamHubStateEvents — no engine needed |
| `GrpcPreset` | Preset + encyclopedia — no engine needed |
