// 16-hub-admin.fsx — Feature 039 scripting walkthrough.
//
// Opens a gRPC channel to a running `FSBar.Hub.App` instance on
// 127.0.0.1:5021 and exercises all five admin-channel RPCs:
//   Pause / Resume / SetEngineSpeed / SendAdminMessage / ForceEndMatch.
//
// Each call prints the AdminSubmitResult (outcome + reason + echoed
// admin-channel status). Run the Hub first, launch a session via the
// Setup tab, then run this script:
//
//   dotnet fsi scripts/examples/16-hub-admin.fsx

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
printfn "Opening gRPC channel to %s …" endpoint

use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

let reportResult (label: string) (result: AdminSubmitResult option) =
    match result with
    | Some r ->
        let statusStr =
            match r.AdminChannelStatus with
            | Some s -> sprintf "%A (%s)" s.State s.Reason
            | None -> "no status"
        printfn "%s → outcome=%A, dropped=%d, reason=%s, status=%s"
            label r.Outcome r.DroppedCount r.Reason statusStr
    | None ->
        printfn "%s → no result in response" label

let pauseResp = client.Pause PauseRequest.Unused
reportResult "Pause" pauseResp.Result

System.Threading.Thread.Sleep(1000)

let resumeResp = client.Resume ResumeRequest.Unused
reportResult "Resume" resumeResp.Result

let speedResp = client.SetEngineSpeed { Speed = 2.0f }
reportResult "SetEngineSpeed 2.0x" speedResp.Result

let msgResp = client.SendAdminMessage { Text = "hello from scripts/examples/16-hub-admin.fsx" }
reportResult "SendAdminMessage" msgResp.Result

printfn ""
printfn "Sending ForceEndMatch in 3 s — Ctrl+C now to skip."
System.Threading.Thread.Sleep(3000)

let endResp = client.ForceEndMatch ForceEndMatchRequest.Unused
reportResult "ForceEndMatch" endResp.Result
