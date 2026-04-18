namespace FSBar.Hub

open System
open System.Diagnostics
open System.Threading.Tasks
open Grpc.Core
open Grpc.Core.Interceptors

module DispatchTracer =

    let private emitEnter (log: HubLog.T) (name: string) =
        HubLog.emitSimple log HubLog.ScriptingHub HubLog.Debug (fun () ->
            sprintf "rpc dispatch: %s entered" name)

    let private emitExit (log: HubLog.T) (name: string) (sw: Stopwatch) =
        sw.Stop()
        HubLog.emitSimple log HubLog.ScriptingHub HubLog.Debug (fun () ->
            sprintf "rpc dispatch: %s completed (%dms)" name sw.ElapsedMilliseconds)

    [<Sealed>]
    type DebugDispatchInterceptor(log: HubLog.T) =
        inherit Interceptor()

        override _.UnaryServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (request: 'TRequest,
                 context: ServerCallContext,
                 continuation: UnaryServerMethod<'TRequest, 'TResponse>) : Task<'TResponse> =
            task {
                let name = context.Method
                emitEnter log name
                let sw = Stopwatch.StartNew()
                try
                    let! resp = continuation.Invoke(request, context)
                    emitExit log name sw
                    return resp
                with ex ->
                    emitExit log name sw
                    return raise ex
            }

        override _.ServerStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (request: 'TRequest,
                 responseStream: IServerStreamWriter<'TResponse>,
                 context: ServerCallContext,
                 continuation: ServerStreamingServerMethod<'TRequest, 'TResponse>) : Task =
            task {
                let name = context.Method
                emitEnter log name
                let sw = Stopwatch.StartNew()
                try
                    do! continuation.Invoke(request, responseStream, context)
                    emitExit log name sw
                with ex ->
                    emitExit log name sw
                    return raise ex
            } :> Task

        override _.ClientStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (requestStream: IAsyncStreamReader<'TRequest>,
                 context: ServerCallContext,
                 continuation: ClientStreamingServerMethod<'TRequest, 'TResponse>) : Task<'TResponse> =
            task {
                let name = context.Method
                emitEnter log name
                let sw = Stopwatch.StartNew()
                try
                    let! resp = continuation.Invoke(requestStream, context)
                    emitExit log name sw
                    return resp
                with ex ->
                    emitExit log name sw
                    return raise ex
            }

        override _.DuplexStreamingServerHandler<'TRequest, 'TResponse when 'TRequest : not struct and 'TResponse : not struct>
                (requestStream: IAsyncStreamReader<'TRequest>,
                 responseStream: IServerStreamWriter<'TResponse>,
                 context: ServerCallContext,
                 continuation: DuplexStreamingServerMethod<'TRequest, 'TResponse>) : Task =
            task {
                let name = context.Method
                emitEnter log name
                let sw = Stopwatch.StartNew()
                try
                    do! continuation.Invoke(requestStream, responseStream, context)
                    emitExit log name sw
                with ex ->
                    emitExit log name sw
                    return raise ex
            } :> Task
