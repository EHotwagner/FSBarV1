// 20-hub-state-observer.fsx — Feature 040 US5 walkthrough.
//
// Demonstrates the StreamHubStateEvents + GetHubState rehydration RPCs.
// Connects to a running FSBar.Hub.App, fetches the initial state
// snapshot via GetHubState, then subscribes to StreamHubStateEvents to
// mirror future mutations.
//
// Expected output: the startup snapshot, followed by any events emitted
// by concurrent gRPC / GUI interactions (e.g. run `19-hub-vizconfig-drive.fsx`
// in another terminal — its ToggleOverlay calls will appear here).

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

// ---- Step 1: Snapshot on connect ----
printfn "— GetHubState: initial rehydration ——"
let snap = client.GetHubState GetHubStateRequest.Unused
printfn "  active tab: %A" snap.ActiveTab
match snap.SessionStatus with
| Some s -> printfn "  session state: %A" s.State
| None -> printfn "  session status: (none)"
match snap.Camera with
| Some c ->
    printfn "  camera scale=%.2f origin=(%.1f, %.1f) autoFit=%b"
        c.Scale c.OriginX c.OriginY c.AutoFit
| None -> printfn "  camera: (none)"
match snap.Lobby with
| Some l ->
    printfn "  lobby map=%s mode=%A speed=%.1fx teams=%d"
        l.MapName l.Mode l.EngineSpeed l.Teams.Length
| None -> printfn "  lobby: (none)"
printfn "  presets: %d" snap.Presets.Count
match snap.HubSettings with
| Some h ->
    printfn "  hub settings: grpcPort=%d maxRenderSubs=%d startPaused=%b"
        h.GrpcPort h.MaxRenderFrameSubscribers h.StartPausedDefault
| None -> printfn "  hub settings: (none)"

// ---- Step 2: Stream future events ----
printfn ""
printfn "— StreamHubStateEvents: listening for 30 s ——"
let req : StreamHubStateEventsRequest = { ClientLabel = "20-hub-state-observer" }
use call = client.StreamHubStateEvents req
let cts = new CancellationTokenSource(TimeSpan.FromSeconds 30.0)

let pump =
    task {
        try
            let! moreOpt =
                call.ResponseStream.MoveNext(cts.Token)
            let mutable more = moreOpt
            while more do
                let evt = call.ResponseStream.Current
                let label =
                    match evt.Change with
                    | HubStateEvent.ChangeOneofCase.ActiveTab -> sprintf "ActiveTab → %A" evt.ActiveTab
                    | HubStateEvent.ChangeOneofCase.VizAttribute ->
                        sprintf "VizAttribute %s" evt.VizAttribute.Key
                    | HubStateEvent.ChangeOneofCase.Camera ->
                        sprintf "Camera scale=%.2f" evt.Camera.Scale
                    | HubStateEvent.ChangeOneofCase.Lobby ->
                        sprintf "Lobby map=%s" evt.Lobby.MapName
                    | HubStateEvent.ChangeOneofCase.Preset ->
                        sprintf "Preset %A %s" evt.Preset.Kind evt.Preset.Name
                    | HubStateEvent.ChangeOneofCase.SessionStatus ->
                        sprintf "SessionStatus → %A" evt.SessionStatus.State
                    | HubStateEvent.ChangeOneofCase.HubSettings ->
                        "HubSettings changed"
                    | _ -> sprintf "%A" evt.Change
                printfn "  [%d] src=%s | %s" evt.EmittedAtUnixMs evt.Source label
                let! next = call.ResponseStream.MoveNext(cts.Token)
                more <- next
        with :? OperationCanceledException -> printfn "  (timeout — stopping)"
    }

pump.GetAwaiter().GetResult()
printfn "Done."
