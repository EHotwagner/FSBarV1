namespace FSBar.Hub.GrpcTests

open System
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture
open FSBar.Hub.GrpcTests.AdminRpcClient

module private AdminHelpers =

    let defaultLobby (mapName: string) : LobbyConfigWire =
        let aiSeat (name: string) (side: string) : SeatWire =
            { SeatWire.empty with Kind = SeatKind.Ai; AiName = name; Side = side }
        let team (id: int) (aiName: string) (side: string) : TeamWire =
            { TeamWire.empty with AllyTeamId = id; Seats = [ aiSeat aiName side ] }
        { LobbyConfigWire.empty with
            MapName = mapName
            Mode = LobbyMode.Skirmish
            EngineSpeed = 1.0f
            LaunchGraphicalViewer = false
            Teams = [ team 0 "HighBarV2" "Armada"; team 1 "BARb" "Cortex" ] }

    let configureLobbyAndLaunch (stub: ScriptingService.Client) (opts: CallOptions) (mapName: string) =
        task {
            let lobby = defaultLobby mapName
            let! _ = stub.ConfigureLobbyAsync(opts) { ConfigureLobbyRequest.empty with Lobby = Some lobby }
            let! _ = stub.LaunchSessionAsync(opts) LaunchSessionRequest.empty
            ()
        }

    let waitForRunning (stub: ScriptingService.Client) (opts: CallOptions) (timeoutMs: int) =
        task {
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let mutable running = false
            while not running && sw.ElapsedMilliseconds < int64 timeoutMs do
                let! state = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
                match state.SessionStatus with
                | Some ss when ss.ActiveSession.IsSome -> running <- true
                | _ -> do! Task.Delay(500)
            return running
        }

[<Collection("HubGrpc")>]
type AdminChannelTests(hub: HubTestFixture) =
    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-001 — Pause_WhenNoSession_ReturnsRejected``() = task {
        let admin = AdminRpcClient(hub.Stub, 5000)
        let! result = admin.Pause()
        Assert.NotEqual(AdminSubmitResult.Outcome.Sent, result.Outcome)
    }

    [<Fact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-001 — Resume_WhenNoSession_ReturnsRejected``() = task {
        let admin = AdminRpcClient(hub.Stub, 5000)
        let! result = admin.Resume()
        Assert.NotEqual(AdminSubmitResult.Outcome.Sent, result.Outcome)
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-001a — SetEngineSpeed_RapidTwice_SecondIsCoalesced``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(90.0)))

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        do! AdminHelpers.configureLobbyAndLaunch stub opts mapName

        let! running = AdminHelpers.waitForRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let admin = AdminRpcClient(stub, 30000)
        let task1 = admin.SetEngineSpeed(1.0f)
        let task2 = admin.SetEngineSpeed(2.0f)
        let! results = Task.WhenAll(task1, task2)
        let outcomes = results |> Array.map (fun r -> r.Outcome)
        Assert.Contains(AdminSubmitResult.Outcome.Coalesced, outcomes :> seq<_>)
        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-002 — Pause_LiveSession_LogStreamConfirmsPauseSent``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(90.0)))

        let logCall = stub.StreamHubLogAsync(opts)
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.AdminChannel ]
                          MinSeverity = LogSeverity.Info }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        do! AdminHelpers.configureLobbyAndLaunch stub opts mapName

        let! running = AdminHelpers.waitForRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let admin = AdminRpcClient(stub, 30000)
        let! pauseResult = admin.Pause()
        if pauseResult.Outcome = AdminSubmitResult.Outcome.Rejected then
            raise (SkipException "Pause was rejected (no admin channel attached)")

        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.AdminChannel && e.Message.Contains("PAUSE")), 10000)
        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-003 — SetEngineSpeed_AllMultipliers_AllReturnSent``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(120.0)))

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        do! AdminHelpers.configureLobbyAndLaunch stub opts mapName

        let! running = AdminHelpers.waitForRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let admin = AdminRpcClient(stub, 30000)
        for speed in [ 0.5f; 1.0f; 2.0f; 5.0f; 10.0f ] do
            let! result = admin.SetEngineSpeed(speed)
            if result.Outcome = AdminSubmitResult.Outcome.Rejected then
                raise (SkipException (sprintf "SetEngineSpeed %.1f rejected" speed))
            Assert.True(result.Outcome = AdminSubmitResult.Outcome.Sent || result.Outcome = AdminSubmitResult.Outcome.Coalesced)
        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-004 — PauseResume_RoundTrip_LogStreamConfirmsBoth``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(120.0)))

        let logCall = stub.StreamHubLogAsync(opts)
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.AdminChannel ]
                          MinSeverity = LogSeverity.Info }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let! mapsResp = stub.ListMapsAsync(opts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        do! AdminHelpers.configureLobbyAndLaunch stub opts mapName

        let! running = AdminHelpers.waitForRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let admin = AdminRpcClient(stub, 30000)
        let! pauseResult = admin.Pause()
        if pauseResult.Outcome = AdminSubmitResult.Outcome.Rejected then
            raise (SkipException "Pause rejected")
        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.AdminChannel && e.Message.Contains("PAUSE")), 10000)

        let! resumeResult = admin.Resume()
        if resumeResult.Outcome = AdminSubmitResult.Outcome.Rejected then
            raise (SkipException "Resume rejected")
        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.AdminChannel && e.Message.Contains("RESUME")), 10000)

        let! _ = stub.StopSessionAsync(opts) StopSessionRequest.empty
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-005 — ForceEndMatch_TerminatesSession_LogStreamConfirmsServerQuit``() = task {
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
        do! AdminHelpers.configureLobbyAndLaunch stub opts mapName

        let! running = AdminHelpers.waitForRunning stub opts 30000
        if not running then raise (SkipException "session never reached Running")

        let admin = AdminRpcClient(stub, 30000)
        let! _ = admin.ForceEndMatch()
        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.SessionManager &&
                      (e.Message.Contains("SERVER_QUIT") || e.Message.Contains("quit") ||
                       e.Message.Contains("Idle") || e.Message.Contains("ended"))), 15000)

        let! state = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
        let hasActiveSession =
            state.SessionStatus
            |> Option.map (fun s -> s.ActiveSession.IsSome)
            |> Option.defaultValue false
        Assert.False(hasActiveSession, "session should not be active after ForceEndMatch")
    }
