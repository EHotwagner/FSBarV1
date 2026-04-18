namespace FSBar.Hub

/// Opaque per-RPC correlation identifier (feature 042 FR-009 / FR-009a).
/// Flows across `await` boundaries via `AsyncLocal<_>`; read by
/// `HubLog.emit` on every log entry; set by the gRPC server
/// interceptor on every unary and streaming RPC entering
/// `FSBar.Hub.ScriptingHub.ScriptingService`.
///
/// Hub-assigned form: `Guid.NewGuid().ToString("N")`.
/// Client-supplied form: any non-empty UTF-8 string ≤ 64 bytes,
/// carried on the request metadata header `x-fsbar-correlation-id`.
/// The effective ID is echoed back on the response trailer
/// `x-fsbar-correlation-id` so clients can assert on it without a
/// schema change to every existing response message (R3).
module CorrelationId =

    /// Opaque value type. Carried by `HubLog.LogEntry.CorrelationId`
    /// and echoed on every unary RPC response trailer.
    [<Struct>]
    type CorrelationId = CorrelationId of string

    /// Standard request/response metadata header. Constant: value is
    /// `"x-fsbar-correlation-id"`. Lower-case per gRPC convention.
    val HeaderName: string

    /// Maximum accepted client-supplied length in UTF-8 bytes.
    /// Requests exceeding this reject with gRPC `InvalidArgument`.
    val MaxClientSuppliedBytes: int

    /// Current correlation ID for the logical call-flow, read from
    /// `AsyncLocal<_>`. Returns `None` outside any RPC handler or
    /// explicit `withScope`. `HubLog.emit` calls this on the
    /// post-gate path.
    val current: unit -> CorrelationId option

    /// Generate a fresh Hub-assigned correlation ID
    /// (`Guid.NewGuid().ToString("N")`).
    val generate: unit -> CorrelationId

    /// Open an explicit correlation scope covering a block of code —
    /// used when an RPC handler hands off background work via
    /// `Task.Run` / `Async.StartAsTask` and wants its log entries to
    /// retain the RPC's ID.
    ///
    /// ```fsharp
    /// let cid = CorrelationId.current ()
    /// Task.Run(fun () ->
    ///     use _ = CorrelationId.withScope cid
    ///     // HubLog.emit calls inside this scope carry `cid`
    ///     doWorkInBackground ()
    /// )
    /// ```
    val withScope: CorrelationId option -> System.IDisposable

    /// Try to parse a client-supplied header value. Returns
    /// `Error reason` when empty or longer than
    /// `MaxClientSuppliedBytes`.
    val tryParseClientHeader: raw: string -> Result<CorrelationId, string>

    /// Server-side gRPC interceptor. Registered globally in
    /// `FSBar.Hub.App/Program.fs` via
    /// `services.AddGrpc(fun o -> o.Interceptors.Add<ServerInterceptor>())`.
    /// Runs on every unary, server-streaming, client-streaming, and
    /// bidi-streaming RPC on `ScriptingService`.
    [<Sealed>]
    type ServerInterceptor =
        inherit Grpc.Core.Interceptors.Interceptor
        new: unit -> ServerInterceptor
