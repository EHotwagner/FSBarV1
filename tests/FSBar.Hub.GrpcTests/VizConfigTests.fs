namespace FSBar.Hub.GrpcTests

open System
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

[<Collection("HubGrpc")>]
type VizConfigTests(hub: HubTestFixture) =

    let opts () = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))

    let boolAttr v =
        Some { VizAttributeValue.empty with Value = VizAttributeValue.ValueCase.BoolValue v }

    let getBoolAttr (state: HubStateSnapshot) (key: string) =
        state.VizConfig
        |> Option.bind (fun vc -> vc.Attributes |> Map.tryFind key)
        |> Option.map (fun v ->
            match v.Value with
            | VizAttributeValue.ValueCase.BoolValue b -> b
            | _ -> false)
        |> Option.defaultValue false

    let assertSent (result: MutationResult option) =
        match result with
        | Some r when r.Outcome = SubmitOutcome.Rejected ->
            Assert.Fail(sprintf "mutation rejected: %s" r.Reason)
        | _ -> ()

    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 — SetVizAttribute_ValidKey_ReflectedInGetHubState``() = task {
        let stub = hub.Stub

        let logCall = stub.StreamHubLogAsync(opts())
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.HubStateStore ]
                          MinSeverity = LogSeverity.Info }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let req = { SetVizAttributeRequest.empty with
                        Key = "overlays.weaponRanges"
                        Value = boolAttr true }
        let! resp = stub.SetVizAttributeAsync(opts()) req
        assertSent resp.Result

        let! state = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        Assert.True(getBoolAttr state "overlays.weaponRanges")

        let! _ = harness.WaitForEntry((fun e -> e.Category = LogCategory.HubStateStore), 5000)
        logCall.Dispose()
    }

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 — SetVizConfig_FullConfig_GetHubStateReturnsEqual``() = task {
        let stub = hub.Stub
        let! initialState = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        let initialConfig = initialState.VizConfig |> Option.defaultValue VizConfigWire.empty

        let flippedAttrs =
            initialConfig.Attributes
            |> Map.map (fun _ v ->
                match v.Value with
                | VizAttributeValue.ValueCase.BoolValue b ->
                    { v with Value = VizAttributeValue.ValueCase.BoolValue (not b) }
                | _ -> v)
        let newConfig = { initialConfig with Attributes = flippedAttrs }
        let req = { SetVizConfigRequest.empty with VizConfig = Some newConfig }
        let! _ = stub.SetVizConfigAsync(opts()) req

        let! state = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        let returned = state.VizConfig |> Option.defaultValue VizConfigWire.empty
        for kvp in flippedAttrs do
            match returned.Attributes |> Map.tryFind kvp.Key with
            | Some v -> Assert.Equal(kvp.Value, v)
            | None -> ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 — ToggleOverlay_EachKind_TogglesState``() = task {
        let stub = hub.Stub
        let overlayKeys =
            [ OverlayKey.WeaponRanges, "overlays.weaponRanges"
              OverlayKey.SightRanges, "overlays.sightRanges"
              OverlayKey.CommandQueue, "overlays.commandQueue"
              OverlayKey.FullNames, "overlays.fullNames" ]
        for (overlay, attrKey) in overlayKeys do
            let! before = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
            let beforeVal = getBoolAttr before attrKey
            let req = { ToggleOverlayRequest.empty with
                            Overlay = overlay
                            Target = OverlayTargetState.Toggle }
            let! toggleResp = stub.ToggleOverlayAsync(opts()) req
            assertSent toggleResp.Result
            let! after = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
            let afterVal = getBoolAttr after attrKey
            Assert.NotEqual(beforeVal, afterVal)
    }

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 — SetCamera_ExplicitCoords_ReflectedInGetHubState``() = task {
        let stub = hub.Stub
        let cam = { ViewerCameraWire.empty with
                        Scale = 2.5f
                        OriginX = 100.0f
                        OriginY = 200.0f
                        AutoFit = false }
        let req = { SetCameraRequest.empty with Camera = Some cam }
        let! resp = stub.SetCameraAsync(opts()) req
        assertSent resp.Result

        let! state = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        match state.Camera with
        | Some c ->
            Assert.Equal(2.5f, c.Scale)
            Assert.Equal(100.0f, c.OriginX)
            Assert.Equal(200.0f, c.OriginY)
            Assert.False(c.AutoFit)
        | None -> Assert.Fail("expected camera in state snapshot")
    }

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 — SetActiveTab_EachTab_ReflectedInGetHubState``() = task {
        let stub = hub.Stub
        for tab in [ HubTab.Setup; HubTab.Viewer; HubTab.Units; HubTab.Style; HubTab.Cfg; HubTab.Grpc ] do
            let req = { SetActiveTabRequest.empty with Tab = tab }
            let! resp = stub.SetActiveTabAsync(opts()) req
            assertSent resp.Result
            let! state = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
            Assert.Equal(tab, state.ActiveTab)
    }

    [<Fact>]
    [<Trait("Category", "GrpcViz")>]
    member _.``FR-010 negative — SetVizAttribute_UnknownKey_ReturnsError_LogWarning``() = task {
        let stub = hub.Stub

        let logCall = stub.StreamHubLogAsync(opts())
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.HubStateStore ]
                          MinSeverity = LogSeverity.Warning }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let req = { SetVizAttributeRequest.empty with
                        Key = "nonexistent-key-xyz"
                        Value = boolAttr true }
        let! resp = stub.SetVizAttributeAsync(opts()) req
        match resp.Result with
        | Some r when r.Outcome = SubmitOutcome.Rejected ->
            Assert.False(String.IsNullOrEmpty(r.Reason))
        | other -> Assert.Fail(sprintf "expected Rejected result, got: %A" other)

        let! _ = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.HubStateStore && e.Severity >= LogSeverity.Warning), 5000)
        logCall.Dispose()
    }
