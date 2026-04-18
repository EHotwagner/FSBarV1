namespace FSBar.Hub

open System
open System.Text
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Core.Interceptors

module CorrelationId =

    [<Struct>]
    type CorrelationId = CorrelationId of string

    let HeaderName = "x-fsbar-correlation-id"
    let MaxClientSuppliedBytes = 64

    let private carrier = AsyncLocal<CorrelationId option>()

    let current () : CorrelationId option =
        carrier.Value

    let generate () : CorrelationId =
        CorrelationId(Guid.NewGuid().ToString("N"))

    type private Scope(prior: CorrelationId option) =
        let mutable disposed = 0
        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    carrier.Value <- prior

    let withScope (cid: CorrelationId option) : IDisposable =
        let prior = carrier.Value
        carrier.Value <- cid
        new Scope(prior) :> IDisposable

    let tryParseClientHeader (raw: string) : Result<CorrelationId, string> =
        if isNull raw then
            Error "x-fsbar-correlation-id header is empty"
        elif String.IsNullOrEmpty(raw) then
            Error "x-fsbar-correlation-id header is empty"
        else
            let byteCount = Encoding.UTF8.GetByteCount(raw)
            if byteCount > MaxClientSuppliedBytes then
                Error
                    (sprintf "x-fsbar-correlation-id header length %d bytes exceeds %d"
                        byteCount MaxClientSuppliedBytes)
            else
                Ok (CorrelationId raw)

    let private effectiveId (context: ServerCallContext) : CorrelationId =
        // Pull header once; gRPC metadata is case-insensitive on lookup
        // via the indexer-free Find pattern — we do a manual scan since
        // `RequestHeaders` is a simple list.
        let mutable found : string = null
        let headers = context.RequestHeaders
        if not (isNull headers) then
            for entry in headers do
                if isNull found
                   && String.Equals(entry.Key, HeaderName, StringComparison.OrdinalIgnoreCase) then
                    found <- entry.Value
        if isNull found then
            generate ()
        else
            match tryParseClientHeader found with
            | Ok cid -> cid
            | Error reason ->
                raise (RpcException(Status(StatusCode.InvalidArgument, reason)))

    let private writeTrailer (context: ServerCallContext) (CorrelationId raw) =
        try
            context.ResponseTrailers.Add(HeaderName, raw)
        with _ ->
            // If the call was aborted before trailers could be written,
            // swallow — the exception that terminated the call will
            // surface to the client.
            ()

    [<Sealed>]
    type ServerInterceptor() =
        inherit Interceptor()

        override _.UnaryServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (request: 'TRequest,
                 context: ServerCallContext,
                 continuation: UnaryServerMethod<'TRequest, 'TResponse>) : Task<'TResponse> =
            task {
                let cid = effectiveId context
                use _ = withScope (Some cid)
                try
                    let! resp = continuation.Invoke(request, context)
                    writeTrailer context cid
                    return resp
                with ex ->
                    writeTrailer context cid
                    return raise ex
            }

        override _.ServerStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (request: 'TRequest,
                 responseStream: IServerStreamWriter<'TResponse>,
                 context: ServerCallContext,
                 continuation: ServerStreamingServerMethod<'TRequest, 'TResponse>) : Task =
            task {
                let cid = effectiveId context
                use _ = withScope (Some cid)
                try
                    do! continuation.Invoke(request, responseStream, context)
                    writeTrailer context cid
                with ex ->
                    writeTrailer context cid
                    return raise ex
            } :> Task

        override _.ClientStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (requestStream: IAsyncStreamReader<'TRequest>,
                 context: ServerCallContext,
                 continuation: ClientStreamingServerMethod<'TRequest, 'TResponse>) : Task<'TResponse> =
            task {
                let cid = effectiveId context
                use _ = withScope (Some cid)
                try
                    let! resp = continuation.Invoke(requestStream, context)
                    writeTrailer context cid
                    return resp
                with ex ->
                    writeTrailer context cid
                    return raise ex
            }

        override _.DuplexStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (requestStream: IAsyncStreamReader<'TRequest>,
                 responseStream: IServerStreamWriter<'TResponse>,
                 context: ServerCallContext,
                 continuation: DuplexStreamingServerMethod<'TRequest, 'TResponse>) : Task =
            task {
                let cid = effectiveId context
                use _ = withScope (Some cid)
                try
                    do! continuation.Invoke(requestStream, responseStream, context)
                    writeTrailer context cid
                with ex ->
                    writeTrailer context cid
                    return raise ex
            } :> Task
