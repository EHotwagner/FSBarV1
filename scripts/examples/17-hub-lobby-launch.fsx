// 17-hub-lobby-launch.fsx — Feature 040 US1 walkthrough.
//
// Demonstrates the headless orchestration path: a scripting client
// connects to `FSBar.Hub.App` on 127.0.0.1:5021, discovers maps,
// configures a lobby, launches a session, waits for Running, and then
// stops the session — all without a human click on the Setup tab.
//
// Prerequisites:
//   1. Hub is running (`dotnet run --project src/FSBar.Hub.App`).
//   2. BAR data dir contains avalanche_3.4.sd7 + HighBarV2 + BARb AIs.
//
// Run:
//   dotnet fsi scripts/examples/17-hub-lobby-launch.fsx

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.Threading
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
printfn "Opening gRPC channel to %s …" endpoint

use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

// 1. Discover available maps.
let maps = client.ListMaps(ListMapsRequest.Unused).Maps
printfn "Found %d map(s) installed." maps.Count
for m in maps |> Seq.truncate 5 do
    printfn "  · %s — %s" m.Name m.FilePath

// 2. Build a lobby: Avalanche 3.4, Skirmish, HighBarV2 (Armada) vs BARb (Cortex).
let pickMap () =
    maps
    |> Seq.tryFind (fun m -> m.Name = "Avalanche 3.4")
    |> Option.orElseWith (fun () -> Seq.tryHead maps)
    |> Option.map (fun m -> m.Name)
    |> Option.defaultValue ""

let lobby : LobbyConfigWire =
    { MapName = pickMap ()
      Mode = LobbyMode.Skirmish
      EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams =
          [ { AllyTeamId = 0
              Seats =
                  [ { Kind = SeatKind.Ai
                      Side = "Armada"
                      Handicap = 0.0f
                      AiName = "HighBarV2"
                      HumanName = "" } ] }
            { AllyTeamId = 1
              Seats =
                  [ { Kind = SeatKind.Ai
                      Side = "Cortex"
                      Handicap = 0.0f
                      AiName = "BARb"
                      HumanName = "" } ] } ]
      Spectators = [] }

printfn ""
printfn "Selected map: %s" lobby.MapName
if String.IsNullOrEmpty lobby.MapName then
    eprintfn "No map installed — aborting."
    exit 2

// 3. Validate the lobby first (cheap pre-flight check).
let validateResp = client.ValidateLobby({ Lobby = Some lobby } : ValidateLobbyRequest)
if not validateResp.Errors.IsEmpty then
    eprintfn "Lobby failed validation:"
    for err in validateResp.Errors do eprintfn "  · %s" err
    exit 2
printfn "Lobby validated OK."

// 4. Push the lobby through ConfigureLobby. The Hub GUI's Setup tab
// observes HubEvent.LobbyChanged and mirrors this state.
let cfgResp = client.ConfigureLobby({ Lobby = Some lobby } : ConfigureLobbyRequest)
match cfgResp.Result with
| Some r when r.Outcome = SubmitOutcome.Sent ->
    printfn "ConfigureLobby: SENT"
| Some r ->
    eprintfn "ConfigureLobby rejected: %s" r.Reason
    for e in cfgResp.ValidationErrors do eprintfn "  · %s" e
    exit 2
| None ->
    eprintfn "ConfigureLobby returned no MutationResult"
    exit 2

// 5. Launch the session paused + headless. The Hub spawns the engine
// asynchronously; the RPC returns as soon as the transition is armed.
let launchResp =
    client.LaunchSession(
        { StartPaused = true
          LaunchGraphicalViewer = false } : LaunchSessionRequest)
match launchResp.Result with
| Some r when r.Outcome = SubmitOutcome.Sent ->
    match launchResp.SessionId with
    | Some id -> printfn "LaunchSession: SENT, session_id = %s" id
    | None -> printfn "LaunchSession: SENT (session id pending)"
| Some r ->
    eprintfn "LaunchSession rejected: %s" r.Reason
    exit 2
| None ->
    eprintfn "LaunchSession returned no MutationResult"
    exit 2

// 6. Poll GetSessionStatus until RUNNING or until we give up.
let deadline = DateTime.UtcNow + TimeSpan.FromSeconds 40.0
let mutable reachedRunning = false
while not reachedRunning && DateTime.UtcNow < deadline do
    let status = client.GetSessionStatus(GetSessionStatusRequest.Unused)
    if status.State = GetSessionStatusResponse.State.Running then
        reachedRunning <- true
        match status.ActiveSession with
        | Some act -> printfn "RUNNING — map=%s mode=%s" act.MapName act.Mode
        | None -> printfn "RUNNING"
    else
        Thread.Sleep 500

if not reachedRunning then
    eprintfn "Session did not reach Running within 40s — giving up."
else
    // 7. Stop the session cleanly.
    printfn ""
    printfn "Stopping session in 2 s …"
    Thread.Sleep 2000
    let stopResp = client.StopSession(StopSessionRequest.Unused)
    match stopResp.Result with
    | Some r -> printfn "StopSession → outcome=%A reason=%s" r.Outcome r.Reason
    | None -> printfn "StopSession returned no MutationResult"
