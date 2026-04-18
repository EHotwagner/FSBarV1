// 22-hub-log-stream.fsx — Feature 042 walkthrough.
//
// Opens a gRPC channel to a running `FSBar.Hub.App` on 127.0.0.1:5021
// and subscribes to the StreamHubLog bidi RPC. Drives a few admin-
// channel RPCs (Pause / Resume / SetEngineSpeed) and prints every log
// entry received, including a correlation-id column so scripts can
// prove the entries between their call and response carry their ID.
//
// Prereqs: Hub is running and a session is active (Setup tab).
//
//   dotnet fsi scripts/examples/22-hub-log-stream.fsx

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
printfn "Opening gRPC channel to %s …" endpoint

use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

let severityLabel (s: LogSeverity) =
    match s with
    | LogSeverity.Debug -> "DEBUG"
    | LogSeverity.Info -> "INFO "
    | LogSeverity.Warning -> "WARN "
    | LogSeverity.Error -> "ERROR"
    | _ -> "?????"

let categoryLabel (c: LogCategory) =
    match c with
    | LogCategory.SessionManager -> "Session"
    | LogCategory.AdminChannel -> "Admin  "
    | LogCategory.ScriptingHub -> "Scripts"
    | LogCategory.ProxyInstall -> "Proxy  "
    | LogCategory.HeadlessRenderer -> "Render "
    | LogCategory.HubStateStore -> "State  "
    | LogCategory.PresetPersistence -> "Preset "
    | LogCategory.Lobby -> "Lobby  "
    | LogCategory.Settings -> "Settings"
    | _ -> "?????"

printfn "Opening StreamHubLog with preset 'session-lifecycle' …"

use cts = new CancellationTokenSource()
let callOptions = CallOptions(cancellationToken = cts.Token)
let stream = client.StreamHubLog(callOptions)

// Initial subscription — use the "session-lifecycle" preset.
let initial : StreamHubLogRequest = {
    ClientLabel = "22-hub-log-stream.fsx"
    Filter = Some {
        Categories = []
        MinSeverity = LogSeverity.Unspecified
        PresetName = "session-lifecycle"
    }
}

(stream.RequestStream.WriteAsync(initial)).GetAwaiter().GetResult()

let readerTask =
    task {
        try
            let! hasNext =
                stream.ResponseStream.MoveNext(cts.Token)
            let mutable ok = hasNext
            while ok do
                let entry = stream.ResponseStream.Current
                let cid = if String.IsNullOrEmpty(entry.CorrelationId) then "-" else entry.CorrelationId
                printfn "[%d] %s %s corr=%s seq=%d %s"
                    entry.TimestampUnixMs
                    (severityLabel entry.Severity)
                    (categoryLabel entry.Category)
                    cid
                    entry.Sequence
                    entry.Message
                let! next = stream.ResponseStream.MoveNext(cts.Token)
                ok <- next
        with
        | :? OperationCanceledException -> ()
        | :? RpcException as ex -> printfn "stream ended: %s" ex.Status.Detail
    } :> Task

// Give the stream a moment to attach.
Thread.Sleep(500)

let withCorrelation (cid: string) =
    let md = Metadata()
    md.Add("x-fsbar-correlation-id", cid)
    CallOptions(headers = md)

let showTrailer (label: string) (trailers: Metadata) =
    let t = trailers
    if isNull t then
        printfn "%s — no trailers" label
    else
        for kv in t do
            if kv.Key = "x-fsbar-correlation-id" then
                printfn "%s trailer: %s=%s" label kv.Key kv.Value

printfn ""
printfn "Driving Pause → Resume → SetEngineSpeed sequence …"

let pauseCall =
    client.PauseAsync(PauseRequest.Unused, withCorrelation "script-pause-001")
pauseCall.ResponseAsync.Wait()
showTrailer "Pause" (pauseCall.GetTrailers())

Thread.Sleep(1500)

let resumeCall =
    client.ResumeAsync(ResumeRequest.Unused, withCorrelation "script-resume-002")
resumeCall.ResponseAsync.Wait()
showTrailer "Resume" (resumeCall.GetTrailers())

Thread.Sleep(800)

let speedCall =
    client.SetEngineSpeedAsync(
        { Speed = 2.0f },
        withCorrelation "script-speed-003")
speedCall.ResponseAsync.Wait()
showTrailer "SetEngineSpeed" (speedCall.GetTrailers())

Thread.Sleep(1500)

printfn ""
printfn "Widening filter to include ScriptingHub (Debug dispatch traces) …"

let wider : StreamHubLogRequest = {
    ClientLabel = ""
    Filter = Some {
        Categories = [
            LogCategory.SessionManager
            LogCategory.AdminChannel
            LogCategory.ScriptingHub
        ]
        MinSeverity = LogSeverity.Debug
        PresetName = ""
    }
}
(stream.RequestStream.WriteAsync(wider)).GetAwaiter().GetResult()

Thread.Sleep(800)

let msgCall =
    client.SendAdminMessageAsync(
        { Text = "hello from 22-hub-log-stream.fsx" },
        withCorrelation "script-msg-004")
msgCall.ResponseAsync.Wait()
showTrailer "SendAdminMessage" (msgCall.GetTrailers())

Thread.Sleep(2000)

printfn ""
printfn "Closing stream …"
(stream.RequestStream.CompleteAsync()).GetAwaiter().GetResult()
cts.Cancel()
try readerTask.Wait(1000) |> ignore with _ -> ()
printfn "done."
