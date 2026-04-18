namespace FSBar.Hub

/// Feature 042 FR-004a — Debug-level RPC dispatch tracer. Emits a
/// single `ScriptingHub` Debug entry when an RPC handler is entered
/// and another when it completes (with elapsed ms). Attach to the
/// same Kestrel gRPC pipeline as `CorrelationId.ServerInterceptor`;
/// register AFTER the correlation-ID interceptor so log entries
/// inherit the active correlation scope.
module DispatchTracer =

    [<Sealed>]
    type DebugDispatchInterceptor =
        inherit Grpc.Core.Interceptors.Interceptor
        new: log: HubLog.T -> DebugDispatchInterceptor
