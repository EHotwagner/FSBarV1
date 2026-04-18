# Phase 1 Data Model — Feature 042: Comprehensive gRPC Logging Stream

**Feature**: `042-grpc-log-stream`
**Date**: 2026-04-18
**Source**: `spec.md §Key Entities`, `research.md`

Every entity below exists either (a) only in-memory at Hub runtime, or
(b) on the wire as a protobuf message. Persistence lives entirely in
`HubSettings` (one-field extension, covered under §HubSettings). No
log-entry storage — the stream is live-only per spec Assumptions.

## 1. `LogEntry` — one Hub observation

**Kind**: In-memory F# record, allocated inside `HubLog.emit` after the
filter-pass gate has confirmed at least one subscriber wants it.

**F# shape** (illustrative — authoritative in `contracts/HubLog.fsi`):

```fsharp
[<Struct>]
type CorrelationId = CorrelationId of string

type LogEntry = {
    /// Hub-local monotonically non-decreasing UTC timestamp.
    TimestampUnixMs: int64
    /// Severity — `Debug` requires explicit opt-in per Clarifications Q2.
    Severity: LogSeverity
    /// Source subsystem, closed DU enumerated in §2.
    Category: LogCategory
    /// Human-readable message. ≤ 8 KiB UTF-8 post-truncation (FR-012a).
    Message: string
    /// Correlation ID for the RPC that owns this entry (FR-009/009a).
    /// `None` when emitted outside any RPC handler (e.g., a background
    /// proxy-install step or a map-analysis warmup).
    CorrelationId: CorrelationId option
    /// Session ID for session-scoped entries (FR-010). `None` when the
    /// entry is about hub-global state.
    SessionId: System.Guid option
    /// Scripting-client ID for client-scoped entries (FR-010). `None`
    /// when the entry is not tied to a specific connected client.
    ScriptingClientId: System.Guid option
}
```

**Invariants**:

- `TimestampUnixMs` is sourced from a single `Stopwatch`-backed wall-
  clock adapter inside `HubLog`; two consecutive emits on the same
  thread observe non-decreasing timestamps. Across threads,
  monotonicity is "best-effort non-decreasing" — exact ordering is the
  subscriber-side `Sequence` field's job.
- `Message` is UTF-8 byte-capped at 8 KiB *including* the truncation
  marker (FR-012a / R6). The module-level helper
  `HubLog.truncateUtf8 message` is the single canonical implementation.
- `Severity = Debug` never reaches a subscriber that did not explicitly
  opt in (FR-005a / R5).
- `CorrelationId = None` for the `StreamHubLog` handler's own activity
  is acceptable; the handler assigns itself the stream's ID when it
  emits a filter ack or drop notification.

**Relationships**:

- Many-to-one → `LogCategory`, `LogSeverity`.
- Optional many-to-one → one active `RunningSession` (via `SessionId`)
  or one `ConnectedClient` (via `ScriptingClientId`).
- One-to-one on the wire → `LogEntryMessage` protobuf (§§ §12).

**State transitions**: N/A — log entries are immutable values from the
moment `HubLog.emit` builds them until subscriber delivery.

**Sequence semantics**: the wire field `LogEntryMessage.sequence` is NOT
a field on `LogEntry`. It is stamped per-subscriber by the wire mapper
in `ScriptingHub.fs` (T028) from the subscriber's `NextSequence: uint64
ref` counter (starting at 1, atomic `Interlocked.Increment`). This keeps
`LogEntry` immutable and shareable across subscribers; the same emitted
entry carries different `sequence` values to each subscriber's stream.
Clients therefore see per-subscriber monotonic sequences that are
independent of the global emit order.

---

## 2. `LogCategory` — enumerated subsystem tag

**Kind**: Closed F# DU in `HubLog.fsi`; mirrored 1:1 by a protobuf
enum.

```fsharp
type LogCategory =
    | SessionManager       // session lifecycle: launch / running / ending / failed / stop
    | AdminChannel         // autohost UDP wire + AdminChannelHost coalescing + status transitions
    | ScriptingHub         // gRPC RPC attach / detach / dispatch / completion (incl. Debug-level)
    | ProxyInstall         // ProxyInstaller per-step outcomes
    | HeadlessRenderer     // render tick + encode + subscribe / detach / overflow
    | HubStateStore        // successful mutators + rejected outcomes
    | PresetPersistence    // viz-preset save / load / delete
    | Lobby                // LobbyConfig validation + edits
    | Settings             // HubSettings persistence + reload diagnostics
```

**Invariants**:

- Exhaustive: every new `HubLog.emit` call site MUST pick an existing
  case. Adding a case requires a surface-area baseline update and a
  proto enum extension at position 10+ (preserving 0 as
  `LOG_CATEGORY_UNSPECIFIED`).
- Stable wire numbering: `LOG_CATEGORY_SESSION_MANAGER = 1` through
  `LOG_CATEGORY_SETTINGS = 9`; position 0 is reserved for "unspecified"
  and MUST NOT be emitted by the Hub.

**Out-of-scope** (per Clarifications Q1): `EngineLauncher`,
`MapAnalysis`, `SyntheticData`, `VizRender`. These MAY be added in a
follow-up feature by extending the DU and enum.

---

## 3. `LogSeverity` — ordered severity tag

**Kind**: Closed F# DU in `HubLog.fsi`; mirrored 1:1 by a protobuf
enum.

```fsharp
type LogSeverity =
    | Debug
    | Info
    | Warning
    | Error
```

**Invariants**:

- Ordered: `Debug < Info < Warning < Error`. Subscriber filter's
  `MinSeverity` means "deliver if `entry.Severity >= minSeverity`".
- Wire numbering: `LOG_SEVERITY_DEBUG = 1` .. `LOG_SEVERITY_ERROR = 4`;
  `LOG_SEVERITY_UNSPECIFIED = 0` reserved.
- `DiagnosticsLine` bridge (R8) maps `HubEvents.Severity.Info` →
  `LogSeverity.Info`, `Warning` → `Warning`, `Error` → `Error`; there
  is no upstream `Debug` source today, so the bridge never emits
  `LogSeverity.Debug`.

---

## 4. `LogFilter` — effective subscription-level policy

**Kind**: In-memory immutable F# record held per subscriber; replaced
atomically on every filter-update message (R4).

```fsharp
type LogFilter = {
    /// Categories accepted. `None` (default when the client passes an
    /// empty list) means "all categories" per Clarifications Q2.
    Categories: Set<LogCategory> option
    /// Lowest severity delivered. Default `Info` per Clarifications Q2.
    MinSeverity: LogSeverity
    /// Optional named preset (§6) whose effect is baked into
    /// `Categories` + `MinSeverity` at filter resolution. Retained for
    /// diagnostics only — the live filter evaluation uses
    /// `Categories` + `MinSeverity` directly.
    PresetName: string option
}
```

**Invariants**:

- Internal compiled representation: the Hub precomputes a `uint64`
  category bitset from `Categories` so filter evaluation is one
  `AND` + one comparison. The compiled form is a `private` module-level
  `type LogFilterCompiled = ...` inside `HubLog.fs` — not part of the
  `.fsi`.
- Preset + explicit categories: when both are supplied, the explicit
  categories override the preset (FR-5, US5 AS2). The resolved filter
  has `PresetName = Some name` (for diagnostics) but the `Categories`
  field reflects the *explicit* list, not the preset's.
- Validation at resolve time: unknown category names reject the whole
  request with gRPC `InvalidArgument` (FR-007).

**State transitions**: `LogFilter` is immutable; mutation happens by
atomically replacing the per-subscriber reference. The old filter is
garbage-collected once no in-flight enqueue holds it.

---

## 5. `LogSubscriber` — one connected log-stream client

**Kind**: In-memory record, held in `HubLog`'s subscriber array.

```fsharp
// Illustrative — implementation inside HubLog.fs, not in .fsi.
type private LogSubscriber = {
    Id: System.Guid
    ClientLabel: string
    Filter: LogFilter ref           // atomically swapped
    Channel: BoundedChannel<LogEntry>
    CancellationToken: CancellationToken
    DroppedSinceLast: int ref       // atomically incremented / reset
    NextSequence: uint64 ref        // per-subscriber monotonic
    AttachedAtUnixMs: int64
}
```

**Invariants**:

- `Id` is unique across the Hub process lifetime.
- `DroppedSinceLast` is atomically reset to 0 at the point of
  successful delivery of the *next* entry (R2).
- `CancellationToken` is tied to the gRPC call's
  `ServerCallContext.CancellationToken` — when the client cancels or
  the Hub shuts down, the token fires and the subscriber is removed
  within 1 s (FR-013).
- `Channel.Capacity = 256` (R2), `FullMode = DropOldest`.

**Concurrency**: `HubLog.emit` takes a lock-free snapshot of the
subscriber array (copy-on-write `LogSubscriber[]`). Attach/detach
replace the array under a single mutex; emit never takes the mutex.

---

## 6. `LogPreset` — named category + severity bundle

**Kind**: Hub-shipped static table in `HubLog.fsi`.

```fsharp
val availablePresets: Map<string, LogFilter>

// Shipped presets (values illustrative; authoritative in HubLog.fs):
//   "session-lifecycle" → Categories = Some { SessionManager; AdminChannel; ProxyInstall },
//                         MinSeverity = Info
//   "admin-channel"     → Categories = Some { AdminChannel },
//                         MinSeverity = Debug
//   "scripting-wire"    → Categories = Some { ScriptingHub },
//                         MinSeverity = Debug
```

**Invariants**:

- Preset names are case-insensitive on lookup (clients may submit
  `"Admin-Channel"` etc.); canonical form stored lowercase.
- Unknown preset name → gRPC `InvalidArgument` on the initial
  `StreamHubLogRequest` (FR-007 applies equally to presets).
- Presets are versioned informally via CLAUDE.md documentation; any
  change to a shipped preset's contents must be called out in the
  feature's `fsdoc` refresh.

---

## 7. `HubSettings.MaxLogStreamSubscribers` — persistent cap

**Kind**: Additive field on the existing `HubSettings.HubSettings`
record; persists in `$XDG_CONFIG_HOME/fsbar-hub/settings.json`.

```fsharp
// Addition to HubSettings.HubSettings (see HubSettings.fsi):
MaxLogStreamSubscribers: int
// Default 8; valid range [1, 32]. SchemaVersion bumps 2 → 3.
```

**Invariants**:

- Load: missing field on v2 → default 8; re-saved as v3 on next
  `save`.
- Validator: `updateMaxLogStreamSubscribers` rejects values outside
  `[1, 32]` with a human-readable `Error` string.
- Runtime enforcement: `ScriptingService.StreamHubLog` checks the
  current subscriber count against the *current* `HubSettings` value
  at attach time; exceeding returns `ResourceExhausted` with a reason
  naming the cap (FR-015a).

---

## 8. `CorrelationId` — opaque per-RPC identifier

**Kind**: In-memory; flows via `AsyncLocal<CorrelationId option>` inside
`FSBar.Hub.CorrelationId`. Never persisted.

```fsharp
[<Struct>]
type CorrelationId = CorrelationId of string

module CorrelationId =
    val current: unit -> CorrelationId option
    val withScope: CorrelationId option -> System.IDisposable
    val generate: unit -> CorrelationId
```

**Invariants**:

- Hub-assigned form: `Guid.NewGuid().ToString("N")` (32-char hex).
- Client-supplied form: any non-empty UTF-8 string ≤ 64 bytes. Longer
  values → gRPC `InvalidArgument`.
- Echoed back in every unary RPC response as a trailing metadata
  header `x-fsbar-correlation-id` (R3); the client picks it up via
  the standard gRPC response-trailers API.
- `HubLog.emit` reads `CorrelationId.current ()` *after* the
  filter-pass gate (so no allocation on the no-subscriber path).

---

## 9. Wire-level messages (preview — authoritative in `contracts/scripting.proto.delta`)

```text
// Appended to fsbar.hub.scripting.v1 package. All additive.

service ScriptingService {
  // ... existing RPCs unchanged ...
  rpc StreamHubLog(stream StreamHubLogRequest) returns (stream LogEntryMessage);
}

enum LogSeverity {
  LOG_SEVERITY_UNSPECIFIED = 0;
  LOG_SEVERITY_DEBUG   = 1;
  LOG_SEVERITY_INFO    = 2;
  LOG_SEVERITY_WARNING = 3;
  LOG_SEVERITY_ERROR   = 4;
}

enum LogCategory {
  LOG_CATEGORY_UNSPECIFIED         = 0;
  LOG_CATEGORY_SESSION_MANAGER     = 1;
  LOG_CATEGORY_ADMIN_CHANNEL       = 2;
  LOG_CATEGORY_SCRIPTING_HUB       = 3;
  LOG_CATEGORY_PROXY_INSTALL       = 4;
  LOG_CATEGORY_HEADLESS_RENDERER   = 5;
  LOG_CATEGORY_HUB_STATE_STORE     = 6;
  LOG_CATEGORY_PRESET_PERSISTENCE  = 7;
  LOG_CATEGORY_LOBBY               = 8;
  LOG_CATEGORY_SETTINGS            = 9;
}

message LogFilterWire {
  repeated LogCategory categories = 1;   // empty = all
  LogSeverity min_severity = 2;          // unspecified = Info
  string preset_name = 3;                // empty = none
}

message StreamHubLogRequest {
  string client_label = 1;
  LogFilterWire filter = 2;
}

message LogEntryMessage {
  int64 timestamp_unix_ms = 1;
  LogSeverity severity    = 2;
  LogCategory category    = 3;
  string message          = 4;
  string correlation_id   = 5;         // empty when none
  string session_id       = 6;         // empty when none
  string scripting_client_id = 7;      // empty when none
  uint64 sequence         = 8;
  int32  dropped_since_last = 9;
}
```

**Wire-level invariants**:

- Purely additive vs feature 041: no existing RPC, message, field, or
  enum is renumbered, removed, or repurposed (FR-017). Verified via
  `buf breaking --against ./baselines/scripting-041.bin` in CI.
- `string` fields carrying identifiers use the empty string as the
  absence marker rather than proto3 `optional`, matching the existing
  `session_id` / `client_id` treatment in `ConnectedClient`.

---

## 10. Ownership + lifecycle

| Entity | Owner | Created | Destroyed |
|--------|-------|---------|-----------|
| `HubLog` bus | `Program.fs` | Hub startup | Hub shutdown (dispose) |
| `LogSubscriber` | `HubLog` | `StreamHubLog` accept | Client cancel OR Hub shutdown (≤ 1 s) |
| `LogEntry` | `HubLog.emit` | Per-call | GC after last subscriber drains it |
| `LogFilter` | `HubLog` (one per subscriber) | Initial request + each update | Replaced on update; freed with subscriber |
| `CorrelationId` context | `CorrelationId` | Interceptor enter | Interceptor exit (`AsyncLocal` clear) |
| `HubSettings.MaxLogStreamSubscribers` | `HubSettings` | Load from JSON / default | `HubSettings.save` writes v3 |

Disposal order at Hub shutdown:

1. `SessionManager.Dispose` — may emit final `SessionManager`-category
   entries.
2. `ScriptingService.Dispose` — completes any in-flight
   `StreamHubLog` calls with `OK` (graceful stream end).
3. `HubLog.Dispose` — cancels the subscriber `CancellationTokenSource`,
   completes every per-subscriber channel writer.
4. `HubEventBus.Dispose` (unchanged).

All steps complete within 1 s by construction — no step blocks on
subscriber consumption (per-subscriber channels are drop-oldest).
