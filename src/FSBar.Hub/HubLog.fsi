namespace FSBar.Hub

/// Canonical in-process log-emit surface for the Hub. Multi-subscriber
/// bidi gRPC stream (`StreamHubLog`) is layered on top in
/// `ScriptingHub`; local GUI diagnostics continue to flow through the
/// `HubEventBus` via the per-call-site bridging documented in
/// research.md §R8.
///
/// Zero-overhead when no subscriber is attached: `emit` gates on a
/// single `Volatile.Read` of the subscriber count before any
/// `LogEntry` allocation, timestamp read, or message formatting (R1).
///
/// Subscriber fan-out mirrors the `ScriptingHub` pattern: per-
/// subscriber `BoundedChannel<LogEntryMessage>` with
/// `BoundedChannelFullMode.DropOldest`, capacity 256 (R2). Dropped
/// counts are carried on the *next* delivered entry's
/// `DroppedSinceLast` field.
///
/// Thread-safety: `emit` is fully lock-free on the hot path;
/// `attach` / `detach` serialize on a single mutex that `emit` never
/// takes.
module HubLog =

    /// Severity levels, orderable so a subscriber can specify a floor
    /// (`entry.Severity >= minSeverity`).
    type LogSeverity =
        | Debug
        | Info
        | Warning
        | Error

    /// Closed enumeration of Hub subsystems that emit on the log
    /// stream. Adding a case is an additive surface-area change (new
    /// baseline line + proto enum extension at position 10+).
    type LogCategory =
        | SessionManager
        | AdminChannel
        | ScriptingHub
        | ProxyInstall
        | HeadlessRenderer
        | HubStateStore
        | PresetPersistence
        | Lobby
        | Settings

    /// Opaque correlation ID re-exported from
    /// `FSBar.Hub.CorrelationId`. Carried on every `LogEntry` whose
    /// emitter ran under a gRPC-interceptor-established scope.
    type CorrelationId = CorrelationId.CorrelationId

    /// Effective per-subscriber filter. Immutable; mutated by
    /// replacing the subscriber's reference atomically (R4).
    type LogFilter = {
        /// Category whitelist. `None` or empty list = "all categories"
        /// per Clarifications Q2.
        Categories: Set<LogCategory> option
        /// Severity floor; default `Info` per Clarifications Q2.
        MinSeverity: LogSeverity
        /// Optional hub-shipped preset name. When both `PresetName`
        /// and explicit `Categories` are supplied, explicit overrides
        /// preset (US5 AS2).
        PresetName: string option
    }

    /// Default filter applied when a client sends an empty filter
    /// request: all categories, `Info` severity floor (FR-005a).
    val defaultFilter: LogFilter

    /// Hub-shipped presets. Case-insensitive lookup; canonical keys
    /// are lowercase. Current set: `session-lifecycle`,
    /// `admin-channel`, `scripting-wire`.
    val availablePresets: Map<string, LogFilter>

    /// Resolve a wire-level filter request to a `LogFilter`.
    /// Returns `Error msg` when the preset is unknown or a category
    /// is `Unspecified` / a string name is unknown (FR-007).
    val resolveFilter:
        categories: LogCategory list ->
        minSeverity: LogSeverity option ->
        presetName: string option ->
            Result<LogFilter, string>

    /// One observation. Public shape — the fields map 1:1 onto
    /// `LogEntryMessage` on the wire, except `sequence` which is
    /// assigned per-subscriber by the wire mapper in `ScriptingHub`.
    type LogEntry = {
        TimestampUnixMs: int64
        Severity: LogSeverity
        Category: LogCategory
        Message: string
        CorrelationId: CorrelationId option
        SessionId: System.Guid option
        ScriptingClientId: System.Guid option
    }

    /// Opaque handle for the log bus. One per Hub process, created
    /// by `create` and disposed at shutdown. All subscribers share
    /// one bus. Implements `System.IDisposable`; dispose cancels every
    /// subscriber `CancellationTokenSource`, completes the per-
    /// subscriber channel writers, and guarantees resource release
    /// within 1 s (FR-013).
    [<Sealed>]
    type T =
        interface System.IDisposable

    /// Subscription handle. Disposing detaches the subscriber and
    /// completes the channel writer within 1 s (FR-013).
    type Subscription = {
        Id: System.Guid
        Reader: System.Threading.Channels.ChannelReader<LogEntry>
        Dispose: unit -> unit
    }

    /// Outcome of an `attach` call. `Rejected` when the subscriber
    /// cap (`HubSettings.MaxLogStreamSubscribers`) would be exceeded
    /// (FR-015a).
    type AttachOutcome =
        | Attached of Subscription
        | Rejected of reason: string

    /// Construct a fresh log bus. The supplied
    /// `HubSettings.HubSettings` thunk is invoked each time
    /// `attach` checks the cap so operator edits to
    /// `MaxLogStreamSubscribers` take effect on the next attach
    /// without requiring a bus rebuild.
    ///
    /// The supplied `IHubEventSink` is retained only for the
    /// `DiagnosticsLine` bridge (R8 — emitting overflow warnings
    /// etc. back into the existing GUI diagnostics pane). Log
    /// entries themselves do NOT fan out through the event bus.
    val create:
        events: HubEvents.IHubEventSink ->
        settings: (unit -> HubSettings.HubSettings) ->
            T

    /// Attach a new subscriber with the given filter and client
    /// label. Returns `Rejected` when the current subscriber count
    /// equals `HubSettings.MaxLogStreamSubscribers`.
    val attach:
        T ->
        clientLabel: string ->
        filter: LogFilter ->
        cancellationToken: System.Threading.CancellationToken ->
            AttachOutcome

    /// Atomically replace the filter for an existing subscriber.
    /// Returns `Ok ()` when the subscriber is found; `Error msg`
    /// when it has already been detached.
    val updateFilter:
        T -> subscriberId: System.Guid -> newFilter: LogFilter ->
            Result<unit, string>

    /// Emit one log entry. Non-blocking; O(1) work when no
    /// subscriber is attached (R1). The `buildMessage` thunk is
    /// invoked only after the filter-pass gate has confirmed at
    /// least one subscriber wants the entry, so string formatting
    /// cost is paid lazily.
    ///
    /// `sessionId` and `scriptingClientId` are passed explicitly by
    /// the emitter — `CorrelationId` is read implicitly from
    /// `CorrelationId.current ()` at emit time.
    val emit:
        T ->
        category: LogCategory ->
        severity: LogSeverity ->
        sessionId: System.Guid option ->
        scriptingClientId: System.Guid option ->
        buildMessage: (unit -> string) ->
            unit

    /// Convenience wrapper with no session or client context.
    /// Equivalent to `emit bus category severity None None buildMessage`.
    val emitSimple:
        T ->
        category: LogCategory ->
        severity: LogSeverity ->
        buildMessage: (unit -> string) ->
            unit

    /// Bridge helper: `HubEvent.DiagnosticsLine` call sites invoke
    /// this in addition to the existing event-bus publish, passing
    /// the caller's owning category. Maps
    /// `HubEvents.Severity.Info/Warning/Error` onto
    /// `LogSeverity.Info/Warning/Error` and delegates to `emit`.
    val emitFromDiagnosticsLine:
        T ->
        category: LogCategory ->
        severity: HubEvents.Severity ->
        sessionId: System.Guid option ->
        scriptingClientId: System.Guid option ->
        message: string ->
            unit

    /// Byte-safe UTF-8 truncation at 8 KiB per FR-012a / R6.
    /// Public for tests; emit callers should not invoke directly
    /// (emit calls this internally post-filter-gate).
    val truncateUtf8: message: string -> string

    /// Current subscriber count. Exposed for tests + Settings-tab
    /// diagnostics.
    val subscriberCount: T -> int

    /// Increment the per-subscriber wire sequence counter and return the
    /// new value (starts at 1). Returns 0 if the subscriber is no longer
    /// attached. Used by the `ScriptingHub.StreamHubLog` mapper to stamp
    /// `LogEntryMessage.sequence` per subscriber.
    val nextSequenceFor: T -> subscriberId: System.Guid -> uint64

    /// Atomically read and reset the per-subscriber drop counter to 0.
    /// Returns 0 if the subscriber is no longer attached. Used by the
    /// wire mapper to stamp `LogEntryMessage.dropped_since_last`.
    val exchangeDroppedSinceLast: T -> subscriberId: System.Guid -> int
