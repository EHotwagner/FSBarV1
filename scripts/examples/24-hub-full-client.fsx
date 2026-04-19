// 24-hub-full-client.fsx — Feature 046 walkthrough.
//
// End-to-end demo that a gRPC-only scripting client can act as a
// fully-fledged headless BAR bot: launch a session, stream per-tick
// GameStateFrame + typed GameEventEnvelope, query the map, look up an
// extended unit def, and batch-submit AICommands.
//
// Prereqs: FSBar.Hub.App running on 127.0.0.1:5021; Avalanche 3.4 installed.
//   * Requires FSBar.Hub ≥ the version that ships feature 046 (repack
//     `dotnet pack src/FSBar.Hub` into `~/.local/share/nuget-local/` if
//     the wire types GameStateFrame / UnitDefInfoExtended / etc. are
//     unresolved — same constraint as `16..23-hub-*.fsx`).
//
//   dotnet fsi scripts/examples/24-hub-full-client.fsx

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let opts = GrpcChannelOptions(MaxReceiveMessageSize = Nullable(64 * 1024 * 1024))
use channel = GrpcChannel.ForAddress("http://127.0.0.1:5021", opts)
let c = ScriptingService.ScriptingServiceClient(channel)

let seat side name = { Kind = SeatKind.Ai; Side = side; Handicap = 0.0f; AiName = name; HumanName = "" }
let lobby : LobbyConfigWire =
    { MapName = "Avalanche 3.4"; Mode = LobbyMode.Skirmish; EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams = [ { AllyTeamId = 0; Seats = [ seat "Armada" "HighBarV2" ] }
                { AllyTeamId = 1; Seats = [ seat "Cortex" "BARb" ] } ]
      Spectators = [] }
c.ConfigureLobby({ Lobby = Some lobby }) |> ignore
c.LaunchSession({ StartPaused = false; LaunchGraphicalViewer = false }) |> ignore

let call = c.StreamGameFrames({ ClientLabel = "24-demo"; CloseOnSessionEnd = false })
let mutable n = 0
while n < 10 && call.ResponseStream.MoveNext(CancellationToken.None).Result do
    let m = call.ResponseStream.Current
    m.GameState |> Option.iter (fun gs ->
        printfn "  frame=%d friendlies=%d enemies=%d events=%d"
            gs.FrameNumber (List.length gs.Friendlies) (List.length gs.Enemies) (List.length m.GameEvents)
        n <- n + 1)

let mi = c.GetMapInfo(GetMapInfoRequest.empty)
printfn "\nMap: %s  %d×%d" mi.MapName mi.Width mi.Height
printfn "Metal spots: %d" (List.length (c.ListMetalSpots(ListMetalSpotsRequest.empty)).Spots)

let armcom = c.GetUnitDefExtended({ Selector = GetUnitDefRequest.SelectorCase.InternalName "armcom" })
armcom.UnitDef |> Option.iter (fun u ->
    printfn "armcom: cost=%A sight=%.0f build_options=%d" u.Cost u.SightRangeElmo (List.length u.BuildOptions))

let mk id =
    { Command = Highbar.AICommand.CommandCase.MoveUnit
                 { UnitId = id; GroupId = 0; Options = 0u; Timeout = 0
                   ToPosition = Some { X = 512.0f; Y = 0.0f; Z = 512.0f } } } : Highbar.AICommand
let r = c.SendCommandBatch({ Commands = [ mk 1; mk 2; mk 3 ] })
printfn "Batch forwarded@%d accepted=%d/%d" r.ForwardedAtFrame
    (r.Outcomes |> List.filter (fun o -> o.Accepted) |> List.length) (List.length r.Outcomes)

c.StopSession(StopSessionRequest.empty) |> ignore
