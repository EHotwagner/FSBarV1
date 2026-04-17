// src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx
//
// Attaches to a running hub's gRPC scripting endpoint, queries
// session status, and — if a session is Running — streams the first
// 5 frames + sends one no-op command. This is the script that
// `specs/035-central-gui-hub/quickstart.md` US7 runs for the
// SC-004 "first frame ≤ 2s after connect" check.
//
// Prerequisites:
//   1. Build the hub:  dotnet build src/FSBar.Hub.App/FSBar.Hub.App.fsproj
//   2. Run the hub:    XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
//                        dotnet run --project src/FSBar.Hub.App
//   3. In the hub UI: pick a map → click Launch → wait for "Running"
//      in the status bar
//   4. In another shell: dotnet fsi src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx
//
// By default connects to http://127.0.0.1:5021; override with the
// FSBAR_HUB_ENDPOINT env var if the hub's GrpcPort was customised
// via HubSettings.

#load "../prelude.fsx"

open System
open System.Threading
open Grpc.Core
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint =
    match Environment.GetEnvironmentVariable("FSBAR_HUB_ENDPOINT") with
    | null | "" -> "http://127.0.0.1:5021"
    | v -> v

printfn "[client] connecting to %s" endpoint
let channel = GrpcChannel.ForAddress(endpoint)
let client = new ScriptingService.Client(channel)

// --- Unary: GetSessionStatus --------------------------------------------

let status = client.GetSessionStatus (CallOptions()) GetSessionStatusRequest.Unused
printfn ""
printfn "=== GetSessionStatus ==="
printfn "  state                  : %A" status.State
printfn "  barDataDir             : %s" status.BarDataDir
printfn "  activeEngineVersion    : %s" status.ActiveEngineVersion
printfn "  bundledProxyVersion    : %s" status.BundledProxyVersion
printfn "  grpcPort               : %d" status.GrpcPort
printfn "  connected clients      : %d" status.Clients.Length
match status.ActiveSession with
| Some a ->
    printfn "  active session:"
    printfn "    sessionId            : %s" a.SessionId
    printfn "    map                  : %s" a.MapName
    printfn "    mode                 : %s" a.Mode
    printfn "    engineSpeed          : %.2f" a.EngineSpeed
    printfn "    paused               : %b" a.Paused
| None ->
    printfn "  active session         : (none — launch a session from the Setup tab)"
match status.Failure with
| Some f ->
    printfn "  last failure           : %s" f.Reason
| None -> ()

if status.State <> GetSessionStatusResponse.State.Running then
    eprintfn ""
    eprintfn "⚠ no session is Running — stream will block until one starts."
    eprintfn "  Launch a session from the Setup tab, then re-run this script."
    exit 0

// --- Server-streaming: StreamGameFrames ---------------------------------

printfn ""
printfn "=== StreamGameFrames (5 frames max, 10s timeout) ==="
let streamReq : StreamGameFramesRequest =
    { ClientLabel = "03-launch-and-stream"
      CloseOnSessionEnd = true }

let cts = new CancellationTokenSource(TimeSpan.FromSeconds(10.0))
use call =
    client.StreamGameFramesAsync
        (CallOptions(cancellationToken = cts.Token))
        streamReq

let reader = call.ResponseStream
let received = ResizeArray<GameFrameMessage>()
let sw = System.Diagnostics.Stopwatch.StartNew()

let rec pump () : unit =
    if received.Count >= 5 then ()
    else
        let ok = reader.MoveNext(cts.Token).GetAwaiter().GetResult()
        if not ok then () else
        let msg = reader.Current
        received.Add(msg)
        let frameNum =
            msg.Frame
            |> Option.map (fun f -> int f.FrameNumber)
            |> Option.defaultValue -1
        printfn "  [%4d ms] seq=%-4d frame=%d"
            sw.ElapsedMilliseconds msg.ClientSequence frameNum
        pump ()

try pump ()
with :? OperationCanceledException ->
    eprintfn "⚠ stream cancelled (10s timeout or session ended)"

printfn ""
printfn "Received %d frame(s) over %d ms" received.Count sw.ElapsedMilliseconds

// --- Unary: SendCommand (no-op text chat) -------------------------------

if status.State = GetSessionStatusResponse.State.Running && not received.IsEmpty then
    printfn ""
    printfn "=== SendCommand (SendTextMessage) ==="
    let cmd : Highbar.AICommand =
        { Command =
            Highbar.AICommand.CommandCase.SendTextMessage
                { Text = "hello from scripting client"; Zone = 0 } }
    let resp =
        client.SendCommand (CallOptions()) { Command = Some cmd }
    printfn "  forwardedAtFrame: %d" resp.ForwardedAtFrame

printfn ""
printfn "[client] done."
