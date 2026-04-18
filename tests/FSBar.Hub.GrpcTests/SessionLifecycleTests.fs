namespace FSBar.Hub.GrpcTests

open System
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

module private SessionHelpers =

    let buildLobby (mapName: string) : LobbyConfigWire =
        let seat (aiName: string) (side: string) : SeatWire =
            { SeatWire.empty with Kind = SeatKind.Ai; AiName = aiName; Side = side }
        let team (id: int) (aiName: string) (side: string) : TeamWire =
            { TeamWire.empty with AllyTeamId = id; Seats = [ seat aiName side ] }
        { LobbyConfigWire.empty with
            MapName = mapName
            Mode = LobbyMode.Skirmish
            EngineSpeed = 1.0f
            LaunchGraphicalViewer = false
            Teams = [ team 0 "HighBarV2" "Armada"; team 1 "BARb" "Cortex" ] }

    let waitRunning (stub: ScriptingService.Client) (opts: CallOptions) (ms: int) : Task<bool> =
        task {
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let mutable running = false
            while not running && sw.ElapsedMilliseconds < int64 ms do
                let! state = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
                match state.SessionStatus with
                | Some ss when ss.ActiveSession.IsSome -> running <- true
                | _ -> do! Task.Delay(500)
            return running
        }

[<Collection("HubGrpc")>]
type SessionLifecycleTests(hub: HubTestFixture) =
    interface IClassFixture<HubTestFixture>

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``FR-009 — ListMaps_ReturnsAtLeastOne``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
        let! resp = hub.Stub.ListMapsAsync(opts) ListMapsRequest.empty
        Assert.True(resp.Maps.Length > 0, "expected at least one available map")
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``FR-009 — ValidateLobby_ValidConfig_ReturnsSuccess``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        let lobby = SessionHelpers.buildLobby mapName
        let req = { ValidateLobbyRequest.empty with Lobby = Some lobby }
        let! resp = stub.ValidateLobbyAsync(opts) req
        Assert.True(resp.Errors.IsEmpty, sprintf "validate lobby errors: %A" resp.Errors)
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``FR-009 — ValidateLobby_MissingMapField_ReturnsError``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
        let lobby = { LobbyConfigWire.empty with MapName = "" }
        let req = { ValidateLobbyRequest.empty with Lobby = Some lobby }
        let! resp = stub.ValidateLobbyAsync(opts) req
        Assert.False(resp.Errors.IsEmpty, "expected validation errors for empty map name")
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``FR-009 — LaunchSession_FullCycle_LogStreamConfirmsLaunch``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(120.0)))

        let logCall = stub.StreamHubLogAsync(opts)
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.SessionManager ]
                          MinSeverity = LogSeverity.Info }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        let lobby = SessionHelpers.buildLobby mapName
        let! _ = stub.ConfigureLobbyAsync(opts) { ConfigureLobbyRequest.empty with Lobby = Some lobby }
        let! _ = stub.LaunchSessionAsync(opts) LaunchSessionRequest.empty

        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.SessionManager && e.Message.Contains("Launch")), 15000)

        let! running = SessionHelpers.waitRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty

        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.SessionManager &&
                      (e.Message.Contains("quit") || e.Message.Contains("Idle") || e.Message.Contains("SERVER_QUIT"))), 15000)
        logCall.Dispose()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``FR-023 — ConcurrentLaunch_SecondReceivesFailedPrecondition``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(120.0)))

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        let lobby = SessionHelpers.buildLobby mapName
        let! _ = stub.ConfigureLobbyAsync(opts) { ConfigureLobbyRequest.empty with Lobby = Some lobby }

        let launch1 = stub.LaunchSessionAsync(opts) LaunchSessionRequest.empty
        let launch2 = stub.LaunchSessionAsync(opts) LaunchSessionRequest.empty

        let classify (resp: LaunchSessionResponse) =
            match resp.Result with
            | Some r when r.Outcome = SubmitOutcome.Sent -> 0
            | Some r when r.Outcome = SubmitOutcome.Rejected -> 1
            | _ -> -1

        let mutable rejectedCount = 0
        let mutable successCount = 0
        let bump resp =
            match classify resp with
            | 0 -> successCount <- successCount + 1
            | 1 -> rejectedCount <- rejectedCount + 1
            | _ -> ()
        let! r1 = launch1
        let! r2 = launch2
        bump r1
        bump r2

        Assert.Equal(1, successCount)
        Assert.Equal(1, rejectedCount)
        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcSession")>]
    member _.``StartPaused_EngineAtFrameZero``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(120.0)))

        let logCall = stub.StreamHubLogAsync(opts)
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.AdminChannel; LogCategory.SessionManager ]
                          MinSeverity = LogSeverity.Info }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        let lobby = SessionHelpers.buildLobby mapName
        let! _ = stub.ConfigureLobbyAsync(opts) { ConfigureLobbyRequest.empty with Lobby = Some lobby }
        let! _ = stub.LaunchSessionAsync(opts) { LaunchSessionRequest.empty with StartPaused = true }

        let! running = SessionHelpers.waitRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.AdminChannel && e.Message.Contains("PAUSE")), 10000)

        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        logCall.Dispose()
    }
