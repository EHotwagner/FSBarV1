// 19-hub-vizconfig-drive.fsx — Feature 040 US3 walkthrough.
//
// Drives the Viewer-tab viz + camera + tab state from a scripting
// client. Demonstrates every US3 RPC: SetVizAttribute, SetVizConfig,
// ToggleOverlay, SetCamera, SetActiveTab.
//
// Run after `scripts/examples/17-hub-lobby-launch.fsx` so the Hub
// has a running session. Watching the Hub's Viewer tab during this
// script shows every mutation applied live.

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

let report (label: string) (outcome: SubmitOutcome) (reason: string) =
    printfn "%-30s → %A %s" label outcome (if reason = "" then "" else "— " + reason)

// ---- Toggle every overlay in sequence ----
printfn "— ToggleOverlay: flip each overlay on/off ————"
for key in [ OverlayKey.WeaponRanges; OverlayKey.SightRanges
             OverlayKey.CommandQueue; OverlayKey.FullNames ] do
    let req : ToggleOverlayRequest = { Overlay = key; Target = OverlayTargetState.On }
    let resp = client.ToggleOverlay req
    match resp.Result with
    | Some r -> report (sprintf "ToggleOverlay %A ON" key) r.Outcome r.Reason
    | None -> printfn "  %A: no result" key

Thread.Sleep 500

// ---- Push a single attribute via SetVizAttribute ----
printfn ""
printfn "— SetVizAttribute: show_grid_lines = true ————"
let attrReq : SetVizAttributeRequest = {
    Key = "overlays.showGridLines"
    Value = Some ({ Value = VizAttributeValue.ValueCase.BoolValue true } : VizAttributeValue)
}
let attrResp = client.SetVizAttribute attrReq
match attrResp.Result with
| Some r -> report "SetVizAttribute gridLines" r.Outcome r.Reason
| None -> printfn "  SetVizAttribute returned no result"

// ---- SetCamera pan + zoom ----
printfn ""
printfn "— SetCamera: pan + 1.5× zoom ————"
let camReq : SetCameraRequest = {
    Camera = Some ({ Scale = 1.5f; OriginX = 200.0f; OriginY = 200.0f; AutoFit = false }
                   : ViewerCameraWire)
}
let camResp = client.SetCamera camReq
match camResp.Result with
| Some r -> report "SetCamera" r.Outcome r.Reason
| None -> printfn "  SetCamera returned no result"

// ---- SetActiveTab cycles through every tab ----
printfn ""
printfn "— SetActiveTab: cycle each tab ————"
for tab in [ Fsbar.Hub.Scripting.V1.HubTab.Setup
             Fsbar.Hub.Scripting.V1.HubTab.Viewer
             Fsbar.Hub.Scripting.V1.HubTab.Units
             Fsbar.Hub.Scripting.V1.HubTab.Style
             Fsbar.Hub.Scripting.V1.HubTab.Cfg
             Fsbar.Hub.Scripting.V1.HubTab.Grpc
             Fsbar.Hub.Scripting.V1.HubTab.Viewer ] do
    let tabReq : SetActiveTabRequest = { Tab = tab }
    let resp = client.SetActiveTab tabReq
    match resp.Result with
    | Some r -> report (sprintf "SetActiveTab %A" tab) r.Outcome r.Reason
    | None -> printfn "  SetActiveTab %A: no result" tab
    Thread.Sleep 400

printfn ""
printfn "Done. Check the Hub's Viewer tab + status bar to see live state."
