namespace FSBar.Hub.GrpcTests

open System
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

[<Collection("HubGrpc")>]
type StateEventStreamTests(hub: HubTestFixture) =

    let opts () = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(15.0)))
    let stub () = hub.Stub

    let waitForEvent (stream: Grpc.Core.IAsyncStreamReader<HubStateEvent>) (predicate: HubStateEvent -> bool) (timeoutMs: int) =
        task {
            let cts = new CancellationTokenSource(timeoutMs)
            let mutable found = false
            let mutable result = None
            try
                while not found do
                    let! moved = stream.MoveNext(cts.Token)
                    if not moved then found <- true
                    elif predicate stream.Current then
                        found <- true
                        result <- Some stream.Current
            with
            | :? OperationCanceledException -> ()
            | :? RpcException as ex when ex.StatusCode = StatusCode.Cancelled -> ()
            return result
        }

    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcStateEvents")>]
    member _.``FR-014 — SetActiveTab_ProducesStateEvent_Within500ms``() = task {
        let stub = stub()
        let call = stub.StreamHubStateEventsAsync(opts()) StreamHubStateEventsRequest.empty

        do! Task.Delay(100)

        let req = { SetActiveTabRequest.empty with Tab = HubTab.Units }
        let! _ = stub.SetActiveTabAsync(opts()) req

        let! evt = waitForEvent call.ResponseStream
                       (fun e ->
                           match e.Change with
                           | HubStateEvent.ChangeCase.ActiveTab tab -> tab = HubTab.Units
                           | _ -> false)
                       500
        Assert.True(evt.IsSome, "expected ActiveTab state event within 500 ms")
        call.Dispose()

        let! _ = stub.SetActiveTabAsync(opts()) { SetActiveTabRequest.empty with Tab = HubTab.Setup }
        ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcStateEvents")>]
    member _.``FR-014 — TwoConcurrentSubscribers_BothReceiveEvent``() = task {
        let stub = stub()
        let call1 = stub.StreamHubStateEventsAsync(opts()) StreamHubStateEventsRequest.empty
        let call2 = stub.StreamHubStateEventsAsync(opts()) StreamHubStateEventsRequest.empty

        do! Task.Delay(100)

        let req = { SetActiveTabRequest.empty with Tab = HubTab.Style }
        let! _ = stub.SetActiveTabAsync(opts()) req

        let wait1 = waitForEvent call1.ResponseStream
                        (fun e ->
                            match e.Change with
                            | HubStateEvent.ChangeCase.ActiveTab tab -> tab = HubTab.Style
                            | _ -> false)
                        2000
        let wait2 = waitForEvent call2.ResponseStream
                        (fun e ->
                            match e.Change with
                            | HubStateEvent.ChangeCase.ActiveTab tab -> tab = HubTab.Style
                            | _ -> false)
                        2000
        let! evt1 = wait1
        let! evt2 = wait2
        Assert.True(evt1.IsSome, "subscriber 1 should receive ActiveTab state event")
        Assert.True(evt2.IsSome, "subscriber 2 should receive ActiveTab state event")
        call1.Dispose()
        call2.Dispose()

        let! _ = stub.SetActiveTabAsync(opts()) { SetActiveTabRequest.empty with Tab = HubTab.Setup }
        ()
    }

    [<SkippableFact>]
    [<Trait("Category", "GrpcStateEvents")>]
    member _.``SessionLaunch_ProducesStateEvent``() = task {
        FSBar.Hub.GrpcTests.SkipGuards.requireEngineInstalled ()
        // HubStateEvent does not carry a SessionStatus change case;
        // session state is observable only via GetSessionStatus /
        // GetHubState polling. Skip until the Hub grows a
        // SessionStatusChanged HubEvent.
        raise (SkipException "StreamHubStateEvents does not emit SessionStatus changes")
        let stub = stub()
        let longOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(90.0)))
        let call = stub.StreamHubStateEventsAsync(longOpts) StreamHubStateEventsRequest.empty

        do! Task.Delay(100)

        let! mapsResp = stub.ListMapsAsync(longOpts) ListMapsRequest.empty
        if mapsResp.Maps.IsEmpty then raise (SkipException "no maps available")
        let mapName = mapsResp.Maps |> List.head |> fun m -> m.Name
        let lobby =
            let seat (ai: string) (side: string) = { SeatWire.empty with Kind = SeatKind.Ai; AiName = ai; Side = side }
            let team (id: int) (ai: string) (side: string) = { TeamWire.empty with AllyTeamId = id; Seats = [ seat ai side ] }
            { LobbyConfigWire.empty with
                MapName = mapName
                Mode = LobbyMode.Skirmish
                EngineSpeed = 1.0f
                LaunchGraphicalViewer = false
                Teams = [ team 0 "HighBarV2" "Armada"; team 1 "BARb" "Cortex" ] }
        let! _ = stub.ConfigureLobbyAsync(longOpts) { ConfigureLobbyRequest.empty with Lobby = Some lobby }
        let! _ = stub.LaunchSessionAsync(longOpts) LaunchSessionRequest.empty

        let! evt = waitForEvent call.ResponseStream
                       (fun e ->
                           match e.Change with
                           | HubStateEvent.ChangeCase.SessionStatus _ -> true
                           | _ -> false)
                       60000
        Assert.True(evt.IsSome, "expected SessionStatus state event after launch")
        call.Dispose()

        let! _ = stub.StopSessionAsync(longOpts) StopSessionRequest.empty
        ()
    }
