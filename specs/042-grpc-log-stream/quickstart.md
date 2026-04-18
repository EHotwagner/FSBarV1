# Quickstart — Feature 042: gRPC Hub Log Stream

**Audience**: A new contributor who wants to see the log stream working
end-to-end within five minutes of cloning the repo (SC-005).

## Prerequisites

- FSBarV1 repo checked out and built at least once:
  `dotnet build FSBarV1.slnx`.
- Beyond All Reason engine installed — auto-detected by
  `EngineDiscovery` from `~/.local/state/Beyond All Reason/engine/recoil_*/`.
  Override with `HIGHBAR_TEST_ENGINE` if needed.
- Bundled HighBarV2 proxy installed (the Hub's Setup tab will prompt if
  not).

## 1. Launch the Hub

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

The gRPC scripting service binds `http://127.0.0.1:5021` by default
(configurable via `HubSettings.GrpcPort`).

## 2. Subscribe to the default log stream

In a second terminal:

```bash
dotnet fsi scripts/examples/22-hub-log-stream.fsx
```

The script is the authoritative end-to-end walkthrough (see its source
for the full flow). It:

1. Opens a `Grpc.Net.Client.GrpcChannel` to `http://127.0.0.1:5021`.
2. Opens a `StreamHubLog` bidi call.
3. Sends an initial `StreamHubLogRequest` with the
   `"session-lifecycle"` preset and no explicit categories.
4. Starts a background reader that prints every `LogEntryMessage` with
   its `[timestamp] severity category correlation_id — message` line.
5. Issues `Pause`, `Resume`, `SetEngineSpeed 2.0`, and `SendAdminMessage`
   RPCs against the running session, capturing the correlation ID the
   Hub echoes back in each response's trailing metadata.
6. Asserts — interactively — that at least one `AdminChannel`-category
   entry arrives within two seconds of each admin RPC and that its
   `correlation_id` matches the response trailer.
7. Sends a filter update that adds `ScriptingHub` category at `Debug`
   floor; subsequent RPC dispatch traces are visible.
8. Cancels the stream cleanly.

## 3. Drive a full session from the script

The script also walks through the launch → running → pause → resume →
stop cycle using the existing feature-040 RPCs (`ConfigureLobby`,
`LaunchSession`, `StopSession`). This demonstrates the FR-018 motivating
scenario — admin-channel + session-lifecycle coverage — without touching
the GUI.

## 4. Assert from a live test

The xUnit live suite `LiveAdminChannelLogStreamTests` (tagged
`[<Trait("Category", "LogStream")>]`) wraps the same walkthrough into
assertable tests:

```bash
dotnet test tests/FSBar.Hub.LiveTests --filter "Category=LogStream"
```

Each test:

- Uses the `LiveSession` helper (established in feature 039 live tests)
  to launch a real engine session bound to a throwaway map.
- Opens a `StreamHubLog` stream with a narrow filter.
- Drives a sequence of admin RPCs.
- Asserts on the captured `LogEntryMessage` list — category, severity,
  correlation-ID continuity, drop-counter zero.
- Tears down the session cleanly.

## 5. Add a new emission site

To instrument a new Hub code path:

1. Pick the owning `LogCategory` (extend the DU + proto enum only if no
   existing case fits — surface-area baseline update required).
2. At the emission point, call:
   ```fsharp
   HubLog.emit log category severity sessionIdOpt clientIdOpt (fun () ->
       sprintf "descriptive message with %A context" state)
   ```
   The `fun () -> ...` thunk is evaluated only when at least one
   subscriber passes the filter for that `(category, severity)` pair
   (R1).
3. For a site inside a gRPC handler, the correlation ID flows
   automatically via `AsyncLocal<_>` — no need to pass it.
4. For background work launched from an RPC handler, wrap the
   background block in `use _ = CorrelationId.withScope (CorrelationId.current ())`
   to preserve the ID.
5. Add a unit test in `tests/FSBar.Hub.Tests/HubLogTests.fs` (or a
   category-specific test file) that asserts the new emit fires under
   the expected filter.

## 6. Change the subscriber cap

Edit `$XDG_CONFIG_HOME/fsbar-hub/settings.json`:

```json
{
  "SchemaVersion": 3,
  "MaxLogStreamSubscribers": 16,
  … other fields …
}
```

Or programmatically via the Settings tab (which uses
`HubSettings.updateMaxLogStreamSubscribers`). The cap takes effect on
the next `StreamHubLog` attach — in-flight subscribers are not
affected.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `ResourceExhausted: max log-stream subscribers (8) reached` | Too many concurrent stream clients | Raise `HubSettings.MaxLogStreamSubscribers` or close idle clients |
| `InvalidArgument: unknown log category 'Foo'` | Typo in filter | Use an enum value from `LogCategory` |
| `InvalidArgument: unknown preset 'verbose'` | Typo / unshipped preset name | Use `session-lifecycle`, `admin-channel`, or `scripting-wire` |
| Stream empty despite Hub activity | Filter excludes the relevant categories / severity | Pass no filter (default = all at `Info`) or widen explicitly |
| `dropped_since_last > 0` appearing | Slow consumer or narrow consumer with burst | Widen filter, speed up reader, or raise buffer via `HubLog` code change (rare) |
| `correlation_id` empty on admin-channel inbound entries | Inbound events are unsolicited (no RPC context) | Expected — inbound wire traces carry no correlation ID |
| Local GUI diagnostics pane empty | `DiagnosticsLine` bridge miswired at a specific call site | Grep `HubLog.emitFromDiagnosticsLine` — every remaining `HubEvents.DiagnosticsLine` call should have a matching sibling emit |

## Further reading

- `specs/042-grpc-log-stream/spec.md` — user stories, FRs, SCs.
- `specs/042-grpc-log-stream/research.md` — design decisions (R1–R10).
- `specs/042-grpc-log-stream/data-model.md` — entity shapes +
  invariants.
- `specs/042-grpc-log-stream/contracts/` — the proto delta + `.fsi`
  sketches that ground implementation.
- `scripts/examples/22-hub-log-stream.fsx` — runnable walkthrough.
