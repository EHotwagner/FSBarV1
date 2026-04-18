# Phase 0 Research — Feature 042: Comprehensive gRPC Logging Stream

**Feature**: `042-grpc-log-stream`
**Date**: 2026-04-18
**Inputs**: `spec.md` (20 FRs, 6 SCs, 5 Clarifications) + prior-art
(features 035, 039, 040, 041).

All items below either (a) justify a design pick made during `/speckit.plan`
or (b) resolve a `NEEDS CLARIFICATION` uncovered while filling the
Technical Context. No open unknowns remain at the end of Phase 0.

---

## R1 — Zero-overhead emit path when no subscriber is attached (FR-016, SC-002)

**Decision**: `HubLog.emit` takes a *builder thunk* (`unit -> LogEntry`) and
evaluates it only after a lock-free snapshot of the subscriber array
confirms at least one subscriber's filter accepts the entry's category +
severity discriminants. The category and severity are passed *unboxed*
alongside the thunk so the filter check happens before any record
allocation or string formatting cost is paid.

```fsharp
// Illustrative shape — .fsi surface in contracts/HubLog.fsi
val emit: T -> category: LogCategory -> severity: LogSeverity -> (unit -> string) -> unit
```

Filter evaluation per subscriber reduces to two `enum` comparisons and a
bitset test (category whitelist represented as a `uint64` — 10 categories
fit comfortably in 64 bits, with headroom for future additions). On the
happy path with zero subscribers, the entire call is:

1. `Volatile.Read` of the subscriber-count field (1 cycle).
2. Branch: if zero, return immediately.

No `LogEntry` record is constructed, no `DateTime.UtcNow` is read, no
message string is formatted. This pins the "no listener" cost to a
single volatile-read + branch — negligible in every Hub hot path
(RPC handler, render tick, event pump).

**Rationale**: This matches the pattern proven in `ScriptingHub.fs`
feature 035 for the frame fan-out — a single volatile-read gate before
allocation. The builder-thunk pattern is standard in .NET logging
(`Microsoft.Extensions.Logging.ILogger.Log<TState>` variants) and is
already understood by contributors through `printfn`-style sprintf at
call sites. SC-002 is satisfied by construction, not by measurement.

**Alternatives considered**:

- *Always build the record, filter afterwards*: Rejected — dominated by
  record allocation at 100k+ potential emit sites per second. SC-002
  would become a benchmark gate rather than a structural guarantee.
- *Route emit through the existing `HubEventBus`*: Rejected —
  `HubEventBus` is an unbounded channel with a single pump task; it is
  the right tool for discrete domain events (a few per second) but
  becomes a contention point at 10 000 entries/s. `HubLog` keeps its
  own per-subscriber bounded channels and does not share the event-bus
  pump.
- *Source-generator for compile-time filter pruning*: Rejected as
  premature. Filter mutation at runtime (FR-006) requires runtime
  dispatch anyway; source-gen only helps the "all subscribers inactive"
  gate, which the volatile-read already handles.

---

## R2 — Subscriber fan-out buffering + drop handling (FR-011, FR-012, SC-003)

**Decision**: Per subscriber, one `System.Threading.Channels.BoundedChannel<LogEntryMessage>`
with capacity **256** and `BoundedChannelFullMode.DropOldest`. A per-subscriber
`int` counter increments every time a drop occurs; the *next* delivered
entry on that subscriber's stream carries the counter's current value in
its `dropped_since_last` field and the counter resets to zero atomically
(CAS loop).

Capacity 256 is chosen over `ScriptingHub.ScriptingHubOptions.FrameBufferCapacity`
(16) because log entries are smaller (~300 B amortised vs ~100 KiB per
render frame) and higher-frequency. 256 entries at ~300 B = ~75 KiB per
idle slow subscriber — well within budget.

Dispose/detach: cancellation of the subscriber's `CancellationToken`
(triggered by gRPC stream close, explicit client cancel, or Hub shutdown)
completes the channel writer and releases the channel reader. FR-013
requires release within 1 s — the cancellation callback runs
synchronously, and the writer task observes it on its next `await`.
Worst-case release latency is bounded by the per-entry enqueue time,
which is ≪ 1 s.

**Rationale**: Drop-oldest matches the feature description's requirement
that a slow consumer should still see recent activity rather than stale
history. Reporting `dropped_since_last` in the *next* delivered message
(rather than as a separate control frame) means a client reading
`LogEntryMessage` messages in order gets loss information interleaved
with content, which is natural to assert on.

**Alternatives considered**:

- *Unbounded per-subscriber channel + periodic trim*: Rejected — a slow
  client could accumulate tens of MiB before a trim pass, violating
  FR-011's implicit bound on Hub memory.
- *Drop-newest*: Rejected — spec Edge Case language (see FR-012 text:
  "older queued entries are dropped (not newer)") makes drop-oldest the
  canonical choice.
- *Separate `LogStreamControl` oneof case for loss reports*: Rejected as
  two-message-per-gap overhead; carrying the counter on the next
  content message is free on the wire (single `int32` field, default 0).

---

## R3 — Correlation-ID propagation across async boundaries (FR-009, FR-009a)

**Decision**: A server-side gRPC interceptor
(`FSBar.Hub.CorrelationId.ServerInterceptor`) runs on every unary RPC on
`ScriptingService`. It:

1. Inspects the incoming `ServerCallContext.RequestHeaders` for a
   metadata entry named `x-fsbar-correlation-id`. If present and
   non-empty, parses it as the effective correlation ID (opaque UTF-8
   string, ≤ 64 bytes; longer values are rejected with `InvalidArgument`
   to prevent unbounded log-line growth).
2. Otherwise, generates a fresh `Guid.NewGuid().ToString("N")`.
3. Stores the effective ID in an `AsyncLocal<CorrelationId option>`
   held by the `CorrelationId` module, invokes the downstream handler,
   and on completion writes the effective ID back as a response
   metadata trailer named `x-fsbar-correlation-id`.
4. Clears the `AsyncLocal<_>` cell when the handler returns, whether
   normally or via exception, via `try…finally`.

`HubLog.emit` reads `CorrelationId.current ()` at record-construction time
(after the filter-pass gate, so the read is off the no-subscriber hot
path). `AsyncLocal<_>` — rather than `ThreadLocal<_>` — ensures the
value flows through `Task.Run`, `Async.StartAsTask`, and `async { ... }`
hops that occur inside typical RPC handlers.

For the server-streaming `StreamHubLog` RPC itself: the interceptor still
fires, but the correlation ID stored there is the *stream's own* ID. Log
entries emitted *by the stream infrastructure* (subscribe, filter
update, drop notification) carry that ID; entries about *other* Hub
activity carry whatever ID is active in the emitter's flow.

For background tasks handed off *after* an RPC has returned (e.g.,
`SessionManager.Launch` kicks off a background connect task), the
handler explicitly captures the ID with
`let cid = CorrelationId.current ()` before `Task.Run` and restores it
inside the task with `use _ = CorrelationId.withScope cid`. This is
documented in the research note referenced by `data-model.md` and the
unit test `CorrelationIdInterceptorTests.backgroundTaskInheritsViaExplicitScope`.

**Rationale**: Both-side ownership (client-supplied override, Hub
auto-assign fallback) exactly matches Clarifications Q3. The echo-back
via trailing metadata avoids a schema change for every existing unary
response; clients read the trailer off the `ResponseHeadersAsync`
awaitable or `CallInvoker` trailers. This is standard gRPC
practice (e.g., OpenTelemetry propagation uses the same pattern).

**Alternatives considered**:

- *Add `string correlation_id = N` to every existing response message*:
  Rejected — violates FR-017 additive-only contract (would require a
  field on every one of ~25 existing response messages).
- *Use `ThreadLocal<_>` instead of `AsyncLocal<_>`*: Rejected —
  `ThreadLocal<_>` does not flow across `await` points; most Hub RPC
  handlers use `async { ... }` or `Task.FromResult`, so the correlation
  ID would silently disappear.
- *Client supplies the ID on every RPC via a message field*: Rejected —
  forces a breaking proto change and shifts correlation responsibility
  onto the client even when they don't need it. The metadata header +
  Hub auto-assign default is a strictly better ergonomic.

---

## R4 — Filter mutation over the open stream (FR-006)

**Decision**: The `StreamHubLog` RPC is **bidirectional streaming**. The
client's first request message carries the initial filter; subsequent
client messages carry replacement filters. The server-side handler
attaches one subscriber on receipt of the first filter, then swaps the
subscriber's filter atomically (single `Interlocked.Exchange` on a
reference to the immutable `LogFilter` record) on each follow-up message.

After each successful filter update the server emits *in-stream* one
`LogEntryMessage` with severity `Debug`, category `ScriptingHub`, and a
message of the form
`log-stream filter updated: categories=[…], minSeverity=Info`
(reserving a follow-up refactor if the filter ack needs structured
fields). Clarifications session did not require a dedicated
`filter_ack` oneof case, so the simpler path — ordinary `LogEntryMessage`
with a conventional text prefix — is used.

Filter validation happens in the server's request-message handler:
unknown category names are rejected with gRPC `InvalidArgument` and
terminate the stream (FR-007). This failure mode is documented in the
proto comment above the `StreamHubLogRequest` message so clients know
to expect it.

**Rationale**: Bidi streaming is the natural fit for "open once, mutate
live, close on cancel". The alternative (server-streaming + a separate
`UpdateLogFilter` unary RPC keyed by subscriber ID) requires the server
to expose a client-visible subscriber ID, which in turn requires the
first message on the stream to carry that ID back — effectively half a
bidi stream with extra ceremony. Bidi keeps the client-side F# code to
a simple `stream.RequestStream.WriteAsync filter` call.

**Alternatives considered**:

- *Server-streaming only; reconnect to change filter*: Rejected —
  violates FR-006 ("without the client having to reconnect").
- *Server-streaming + separate unary update RPC*: Rejected — two RPCs
  where one suffices, and introduces ordering questions between
  "update applied" and "filter change visible in stream".
- *Add a dedicated `LogStreamControlMessage` oneof carrying ack +
  drops*: Rejected as more surface area than the spec demands. The
  conventional text-prefix ack is enough; SC-001 asserts every entry
  is attributable, not that acks are structured.

---

## R5 — Log category enumeration + default filter (FR-004, FR-005a, Clarifications Q2)

**Decision**: `LogCategory` is a closed F# DU in `HubLog.fsi`:

```fsharp
type LogCategory =
    | SessionManager       // session lifecycle
    | AdminChannel         // wire protocol + host-level orchestration
    | ScriptingHub         // RPC attach/detach + dispatch + completion
    | ProxyInstall         // bundled proxy install steps
    | HeadlessRenderer     // viewer render pipeline
    | HubStateStore        // UI state mutations
    | PresetPersistence    // style preset save/load/delete
    | Lobby                // lobby validation + configuration
    | Settings             // HubSettings persistence + reload
```

Nine cases, one bit each in the filter bitset with six bits reserved for
future additions. Wire representation: the proto `LogCategory` enum
mirrors the DU 1:1 with `LOG_CATEGORY_UNSPECIFIED = 0` sentinel at
position 0 so unknown values surface as validation errors (FR-007)
rather than silent inclusion.

Default filter when the client sends an empty category list with no
severity floor (per Clarifications Q2): **all nine categories at `Info`
severity floor**. `Debug`-level entries are therefore invisible until
the client explicitly sets `minSeverity = Debug` or selects a preset
whose floor is `Debug`.

**Rationale**: Nine categories cover every subsystem listed in FR-004
and match the nine `HubEvent` emitter owners enumerated by a grep over
`src/FSBar.Hub/*.fs`. Out-of-scope owners (engine-launcher `infolog.txt`,
`FSBar.Client` map analysis, `FSBar.SyntheticData`, `FSBar.Viz`) are
deliberately absent per Clarifications Q1; adding them later is a single
DU case extension (additive, surface-area baseline update only).

`Debug`-as-opt-in protects tests that forget to pass a filter from
being flooded by the admin-channel wire trace (potentially one entry
per UDP datagram), which at a modest game-speed is dozens per second.

**Alternatives considered**:

- *String-based category identifiers*: Rejected — no compile-time
  exhaustiveness, no bitset representation, easy to mistype. The DU +
  enum pair costs a surface-area baseline line each and buys
  exhaustive match warnings forever.
- *`Debug` on by default*: Rejected — violates the Clarifications Q2
  decision and would flood CI test logs on every run.
- *One category per feature flag*: Rejected — too granular for the
  spec's "subsystem" framing and multiplies filter bitset bits
  without clear benefit.

---

## R6 — Message truncation policy (FR-012a, Clarifications Q4)

**Decision**: Implemented inside `HubLog.emit` after the filter-pass
gate but before the fan-out enqueue, so every subscriber sees the same
byte-identical truncated message. Algorithm:

1. Compute `byteCount = Encoding.UTF8.GetByteCount(message)`.
2. If `byteCount ≤ 8192` → no work, ship unchanged.
3. Else: compute a byte-safe UTF-8 cut point at or below
   `8192 - len(marker)` bytes (the marker's UTF-8 byte length is fixed
   and pre-computed at module init). Ensure the cut does not land
   inside a multi-byte sequence — walk backwards until a leading byte
   is found.
4. Concatenate the truncated prefix + the marker
   ` …[truncated N bytes]` where `N = byteCount - cutBytes`.
5. Final delivered-message `byteCount` ≤ 8 KiB by construction.

Truncation does not log a separate event — the marker is the log of
the truncation. A subscriber that cares about tail content can grep for
the marker suffix.

**Rationale**: Truncating at emit time (not per subscriber) means a slow
subscriber cannot see an un-truncated message that a fast one missed —
simpler invariant to assert in tests and documentation. The 8 KiB
threshold aligns with common stack-trace lengths (~4–6 KiB for a typical
.NET async exception) and the typical size of a BAR engine diagnostic
line (~100 B) — common cases fit uncut, pathological cases get a single
marker instead of an OOM risk.

**Alternatives considered**:

- *Cut at character count*: Rejected — gRPC wire is byte-oriented;
  character-count truncation doesn't bound the wire size.
- *Compress instead of truncate*: Rejected — gRPC already supports
  per-message `Deflate` / `Gzip`; message compression is a transport
  concern, not an application one.
- *Truncate per subscriber based on client preference*: Rejected as
  surface-area bloat for no demonstrated use case.

---

## R7 — `HubSettings.MaxLogStreamSubscribers` migration (FR-015a, Clarifications Q5)

**Decision**: Add a new field `MaxLogStreamSubscribers: int` to
`HubSettings.HubSettings` with default value **8** and validated range
`[1, 32]`. Bump `SchemaVersion` from `2` (set in feature 040) to **3**.
The load path treats the field as missing-on-v2 → default 8, re-saves
as v3 on the next explicit `save` call (same pattern feature 040 used
when adding `MaxRenderFrameSubscribers`).

New validator `updateMaxLogStreamSubscribers: HubSettings -> int ->
Result<HubSettings, string>` rejects out-of-range values, mirroring
`updateMaxRenderFrameSubscribers`.

**Rationale**: Matches the exact pattern established in feature 040 and
documented in CLAUDE.md. Tests in `HubSettingsTests.fs` already exercise
the v1→v2 migration; a sibling test asserts v2→v3 behaviour. Live
coverage comes from the new `HubSettings.baseline` surface-area baseline
capturing the extra field.

**Alternatives considered**:

- *Hardcode 8 with no setting*: Rejected — operators may legitimately
  want more (dense CI matrix) or fewer (tight-memory dev box) subscribers.
- *Environment variable*: Rejected — discoverable settings go in the
  JSON file per the established Hub convention; env vars are reserved
  for test-harness overrides (`FSBAR_HUB_*`).

---

## R8 — `HubEvent.DiagnosticsLine` bridging (FR-014)

**Decision**: Add an adapter inside `HubLog` that *mirrors* every
published `HubEvent.DiagnosticsLine(severity, message)` to a `HubLog`
emit with:

- Category: `SessionManager` when the emitter is one of the session-
  lifecycle owners, `ScriptingHub` when the emitter is one of the RPC
  handlers, else a fallback to the owning emitter's category. Because
  every existing `DiagnosticsLine` call site has a well-identified
  owner (verified by `rg HubEvents.DiagnosticsLine src/FSBar.Hub`, 24
  call sites across 10 files), each call site is updated to use
  `HubLog.emitFromDiagnosticsLine` which takes the owning category as
  an explicit parameter. Local GUI consumers keep subscribing to the
  `HubEventBus` as before; their view is unchanged.
- Severity: map `Info / Warning / Error` directly to the corresponding
  `LogSeverity` values. Existing `HubEvent.DiagnosticsLine` has no
  `Debug` case and never will — `Debug` is the new log stream's
  privileged severity, reserved for new emission sites (FR-004a).

This means the gRPC log stream is a strict superset of what the local
GUI sees today — any test that previously had to read the local
diagnostics pane can now assert on the log stream instead.

**Rationale**: Mirroring rather than migrating keeps the FR-014
guarantee ("existing local GUI consumers continue to see DiagnosticsLine
unchanged") a *structural* property, not a behavioural one. Tests in
`HubEventsTests.fs` for the existing event bus continue to pass
unchanged; a new sibling test `HubLogBridgeTests` asserts that every
mirrored call produces a log stream entry with the expected category +
severity.

**Alternatives considered**:

- *Make `HubEvent.DiagnosticsLine` publish *into* `HubLog` only, and
  have the GUI read `HubLog` directly*: Rejected — violates FR-014
  structurally (requires GUI rewire) and couples the GUI rendering to
  a bounded channel rather than the current unbounded event bus.
- *Auto-mirror in the event-bus pump*: Rejected — loses the
  emitter-category attribution (the pump only sees
  `HubEvent.DiagnosticsLine`, not who published it), breaking SC-001.

---

## R9 — gRPC service hosting + interceptor wiring

**Decision**: The `CorrelationId.ServerInterceptor` is registered in
`FSBar.Hub.App/Program.fs` via the existing
`services.AddGrpc(fun o -> o.Interceptors.Add<ServerInterceptor>())`
idiom already used by `Grpc.AspNetCore`. No Kestrel-level changes are
required. The interceptor handles all four RPC kinds (unary, server-
streaming, client-streaming, bidi-streaming) with the same
`AsyncLocal` setup — the base class `Grpc.Core.Interceptors.Interceptor`
offers hooks for each.

For the `StreamHubLog` bidi handler specifically, the correlation-ID
handling is an edge case covered in R3: the stream's own correlation ID
flows through every emit the *stream handler itself* performs; emits
from *other* Hub activity pick up whichever correlation ID is active in
the emitter's flow.

**Rationale**: Using the built-in interceptor surface keeps the change
contained to `CorrelationId.fs` + a two-line edit in `Program.fs`. No
custom pipeline, no Kestrel-level middleware, no new DI scopes.

**Alternatives considered**:

- *ASP.NET `IMiddleware` at the HTTP layer*: Rejected — would force
  us to parse gRPC-specific header semantics ourselves and lose access
  to `ServerCallContext`. Interceptors are the supported surface.
- *Per-service interceptor hand-registration*: Rejected — `ScriptingService`
  is the only gRPC service the Hub hosts; global registration is
  equivalent and less error-prone.

---

## R10 — Live-test infrastructure reuse

**Decision**: Reuse the existing `LiveSession` test helper established
in `LiveAdminPauseTests.fs`. The new `LiveAdminChannelLogStreamTests.fs`
suite opens a gRPC channel (as `16-hub-admin.fsx` does), subscribes to
`StreamHubLog` with categories `[AdminChannel; SessionManager]` at
`Debug` floor, drives the launch → pause → resume → set-speed →
force-end sequence, and asserts on the collected `LogEntryMessage`
list. Tagged `[<Trait("Category", "LogStream")>]` so
`dotnet test --filter "Category=LogStream"` runs the full suite for
FR-018 verification.

**Rationale**: Follows the pattern the feature-039 live tests
established; no new infrastructure. The `AdminChannel` live suite is
the motivating use case from the user's description, so pinning it
live (not just in unit tests) delivers SC-004 directly.

**Alternatives considered**:

- *Pure unit-test coverage*: Rejected — SC-004 explicitly requires an
  end-to-end demonstration against a live session.
- *Add a new live-test project*: Rejected — the existing project
  already has the engine-discovery scaffolding and live fixtures.

---

## Open Questions

*None.* All `NEEDS CLARIFICATION` from the plan draft have been resolved
above. The five spec-side clarifications from session 2026-04-18 are
incorporated into R5 (Q2, default filter), R6 (Q4, truncation), R7 (Q5,
subscriber cap), R3 (Q3, correlation-ID ownership), and the overall
feature scope (Q1, new emission sites — out-of-scope subsystems
explicitly enumerated).

## Summary of Decisions

| ID | Area | Decision |
|----|------|----------|
| R1 | Hot-path overhead | Builder-thunk emit; volatile-read gate before any allocation |
| R2 | Fan-out buffering | Per-subscriber bounded channel, capacity 256, drop-oldest + counter |
| R3 | Correlation ID | `AsyncLocal` + gRPC interceptor; metadata header override; trailer echo |
| R4 | Filter mutation | Bidi streaming RPC; client resends filter; in-stream ack |
| R5 | Categories | 9-case F# DU; default filter = all categories, `Info` floor |
| R6 | Truncation | Byte-safe 8 KiB cut + trailing marker, emit-time not per-subscriber |
| R7 | Settings | New `MaxLogStreamSubscribers` field, schema v3, default 8, range 1–32 |
| R8 | `DiagnosticsLine` bridge | Per-call-site mirroring with explicit category attribution |
| R9 | Hosting | `Grpc.AspNetCore` interceptor; global registration in `Program.fs` |
| R10 | Live tests | New `LiveAdminChannelLogStreamTests` tagged `Category=LogStream` |
