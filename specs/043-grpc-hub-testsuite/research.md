# Research: 043-grpc-hub-testsuite

## R1 — Port injection into FSBar.Hub.App

**Decision**: Add `FSBAR_HUB_GRPC_PORT` environment variable to `src/FSBar.Hub.App/Program.fs`.

When set, it overrides `(getSettings()).GrpcPort` for the Kestrel bind address only (settings file is not mutated). This is ~3 lines in Program.fs before `let grpcEndpointUrl =`.

**Rationale**: The Hub currently reads its gRPC port from `HubSettings.GrpcPort` (default 5021, from `$XDG_CONFIG_HOME/fsbar-hub/settings.json`). Writing a temp settings file per test is fragile — the Hub's settings-load path also writes defaults back on load, which could mutate the user's real settings dir if the XDG override isn't hermetic. An env var is the standard 12-factor pattern for test isolation and requires minimal code change.

**Alternatives considered**:
- Temp XDG_CONFIG_HOME: Would require creating a full settings-file layout; Hub may load other things from there (viz presets, etc.) that could interfere. Rejected.
- Fixed port (5099): Conflicts with other test projects and the user's running Hub. Rejected.

**How to apply**: `HubTestFixture` picks a free TCP port with `TcpListener(IPAddress.Loopback, 0)`, then passes `FSBAR_HUB_GRPC_PORT=<port>` when spawning the Hub process. The Program.fs change checks `Environment.GetEnvironmentVariable("FSBAR_HUB_GRPC_PORT")` and, if non-null and parseable, overrides the Kestrel port.

---

## R2 — Hub process readiness detection

**Decision**: Poll the gRPC channel with a `GetHubState` unary call every 500 ms, up to 15 s, catching `RpcException`.

**Rationale**: The Hub emits `[hub] gRPC scripting service listening on http://...` to stderr, but stdout/stderr parsing is brittle (buffering, encoding). The cleanest approach is to attempt a `GetHubState` unary call and retry on `RpcException` until it succeeds or the timeout expires. HTTP/2 cleartext gRPC on localhost returns quickly on connection failure.

**Alternatives considered**:
- TCP connect probe: Confirms port is listening but not that the gRPC service is registered. Rejected.
- Parse stderr: Fragile; breaks if log format changes. Rejected.

**How to apply**: `HubTestFixture.InitializeAsync()` spawns the process, then polls `GetHubState` in a loop.

---

## R3 — xUnit async fixture lifecycle pattern

**Decision**: Implement `HubTestFixture` as `IAsyncLifetime` and share it with `IClassFixture<HubTestFixture>` per test class.

**Rationale**: xUnit's `IAsyncLifetime` interface (`InitializeAsync` / `DisposeAsync`) supports async process startup/teardown without `Thread.Sleep`. F# xUnit integration supports `IAsyncLifetime` via `Task` return types. The existing Hub.LiveTests use manual `Stopwatch` polling because they don't need async process lifecycle — they use in-process `SessionManager`. The new fixture launches a real process, so async teardown (`process.WaitForExitAsync`) is needed to avoid zombie processes.

**How to apply**: 
```fsharp
type HubTestFixture() =
    interface IAsyncLifetime with
        member _.InitializeAsync() = task { ... } :> Task
        member _.DisposeAsync() = ... :> ValueTask
```
Test classes take it as `IClassFixture<HubTestFixture>`:
```fsharp
type AdminTests(hub: HubTestFixture) =
    interface IClassFixture<HubTestFixture>
```

---

## R4 — Xvfb / display for Hub.App process

**Decision**: Use `DISPLAY=:0` (the existing virtual display already running in the dev container) rather than spawning Xvfb per test run.

**Rationale**: CLAUDE.md confirms `DISPLAY=:0` is the standard env for this environment. Spawning Xvfb in the fixture adds flakiness (port conflicts, cleanup). The Hub.App in screenshot mode exits cleanly after one render pass — but tests don't use screenshot mode. Instead they connect via gRPC and the SkiaViewer window runs minimised/hidden on `:0`. `XDG_RUNTIME_DIR=/tmp/runtime-developer` is also required.

**How to apply**: `HubTestFixture` sets `DISPLAY=:0` and `XDG_RUNTIME_DIR=/tmp/runtime-developer` in the child process's environment, same as the standard run command.

---

## R5 — Streaming RPC testing in F#

**Decision**: Use `AsyncSeq`-style consumption via `IAsyncEnumerable<T>` from FsGrpc's streaming client, wrapped in a `LogStreamHarness` helper with bounded `Task.WhenAny(actual, Task.Delay(timeout))` pattern.

**Rationale**: FsGrpc generates `IAsyncStreamReader<T>` for server-streaming RPCs. Reading from a `StreamLogEntries` call requires `MoveNextAsync` + cancellation. The `LogStreamHarness` wraps this with:
- `WaitForEntry(predicate, timeoutMs)` — polls `MoveNextAsync` until predicate matches or timeout
- `CollectN(n, timeoutMs)` — collects exactly N entries
- `AssertNoUnexpected(windowMs)` — reads for a window and fails if anything unexpected arrives

All harness operations use `CancellationTokenSource` with `timeoutMs` to avoid hanging tests.

**How to apply**: `LogStreamHarness` is a private module in the test project. Tests open a `StreamLogEntries` call, wrap it in `LogStreamHarness.create`, then call helper methods.

---

## R6 — gRPC client dependency

**Decision**: Add `Grpc.Net.Client 2.67.*` as an explicit `PackageReference` in the new test project.

**Rationale**: `Grpc.Net.Client` is the .NET gRPC client library needed for `GrpcChannel.ForAddress`. It is not currently referenced by name in any existing `*.fsproj` (the existing live tests call service methods directly, bypassing the channel). It is a transitive dependency of `Grpc.AspNetCore` (same version family), so version alignment is straightforward. It is maintained by Microsoft as part of `grpc-dotnet`.

**Alternatives considered**: Use `Grpc.AspNetCore` client-factory: Requires ASP.NET Core DI, heavyweight for test usage. Rejected.

---

## R7 — Test isolation strategy

**Decision**: One `HubTestFixture` (= one Hub process) per test class; test classes use `[<Collection>]` to serialize all Hub-process-dependent tests.

**Rationale**: Hub startup involves SkiaViewer window creation and gRPC Kestrel bind — each takes ~2–3 s. Starting one Hub per individual test would make the suite prohibitively slow. Sharing one Hub per test class (IClassFixture) balances isolation vs. speed. The `[<Collection("HubGrpc")>]` xUnit collection serializes test classes within the same process, preventing port conflicts when multiple classes run concurrently.

**Note on live vs. non-live**: Tests that require a BAR engine session call `skipIfLiveUnavailable()` (same pattern as existing live tests). Non-live tests (overlay CRUD, viz config, log stream without engine, etc.) run against the Hub's idle state only and complete fast.

---

## R8 — Program.fs change scope

**Decision**: Minimal: one `let grpcPort = ...` binding that checks the env var before `let grpcEndpointUrl = ...` in `Program.fs`. Does not change `HubSettings`, does not persist to file.

```fsharp
let grpcPort =
    match System.Environment.GetEnvironmentVariable("FSBAR_HUB_GRPC_PORT") with
    | null -> (getSettings ()).GrpcPort
    | s -> match System.Int32.TryParse(s) with
           | true, p when p > 1023 && p <= 65535 -> p
           | _ -> (getSettings ()).GrpcPort
```

Replace `(getSettings ()).GrpcPort` with `grpcPort` in the two subsequent uses (Kestrel bind + log line).

This change is backward-compatible and does not alter the surface area of any library module. No `.fsi` or baseline update required.
