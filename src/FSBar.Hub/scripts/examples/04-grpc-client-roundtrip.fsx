// src/FSBar.Hub/scripts/examples/04-grpc-client-roundtrip.fsx
//
// Minimal happy-path client against the hub's scripting endpoint.
// Calls each of the four RPCs exactly once:
//   * GetSessionStatus  — unary, always returns
//   * GetUnitDef        — unary, looks up by internal name
//   * SendCommand       — unary, expected NOT_FOUND when Idle
//   * StreamGameFrames  — server streaming, first frame or 2s
//
// Intended as the smallest possible end-to-end exercise of the
// contract — run it against any hub state to confirm the endpoint
// is reachable and the schema matches.

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

printfn "[roundtrip] connecting to %s" endpoint
let channel = GrpcChannel.ForAddress(endpoint)
let client = new ScriptingService.Client(channel)

// --- 1. GetSessionStatus -----------------------------------------------

let status = client.GetSessionStatus (CallOptions()) GetSessionStatusRequest.Unused
printfn "[1/4] GetSessionStatus  → state=%A  engine=%s  bundled=%s"
    status.State status.ActiveEngineVersion status.BundledProxyVersion

// --- 2. GetUnitDef by internal name -----------------------------------

let udReq : GetUnitDefRequest =
    { Selector = GetUnitDefRequest.SelectorCase.InternalName "armcom" }
let udResp = client.GetUnitDef (CallOptions()) udReq
match udResp.UnitDef with
| Some ud ->
    printfn "[2/4] GetUnitDef armcom → defId=%d metal=%d energy=%d"
        ud.DefId ud.MetalCost ud.EnergyCost
| None ->
    printfn "[2/4] GetUnitDef armcom → not found (no session has loaded UnitDefs yet)"

// --- 3. SendCommand (expect NOT_FOUND when no session) ----------------

let cmd : Highbar.AICommand =
    { Command =
        Highbar.AICommand.CommandCase.SendTextMessage
            { Text = "roundtrip"; Zone = 0 } }
try
    let resp = client.SendCommand (CallOptions()) { Command = Some cmd }
    printfn "[3/4] SendCommand       → forwardedAtFrame=%d" resp.ForwardedAtFrame
with
| :? RpcException as ex when ex.StatusCode = StatusCode.NotFound ->
    printfn "[3/4] SendCommand       → NOT_FOUND (expected when no session is Running)"

// --- 4. StreamGameFrames (wait up to 2s for first frame) --------------

let streamReq : StreamGameFramesRequest =
    { ClientLabel = "04-roundtrip"
      CloseOnSessionEnd = true }
let cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0))
use call =
    client.StreamGameFramesAsync
        (CallOptions(cancellationToken = cts.Token)) streamReq
try
    let ok = call.ResponseStream.MoveNext(cts.Token).GetAwaiter().GetResult()
    if ok then
        let msg = call.ResponseStream.Current
        let frameNum =
            msg.Frame
            |> Option.map (fun f -> int f.FrameNumber)
            |> Option.defaultValue -1
        printfn "[4/4] StreamGameFrames  → first frame seq=%d frame=%d"
            msg.ClientSequence frameNum
    else
        printfn "[4/4] StreamGameFrames  → stream ended immediately"
with
| :? OperationCanceledException ->
    printfn "[4/4] StreamGameFrames  → no frame within 2s (no session Running)"
| :? RpcException as ex when ex.StatusCode = StatusCode.Cancelled ->
    printfn "[4/4] StreamGameFrames  → Cancelled after 2s (no session Running)"

printfn ""
printfn "[roundtrip] done."
