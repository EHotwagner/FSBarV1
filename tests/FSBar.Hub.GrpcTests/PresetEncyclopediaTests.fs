namespace FSBar.Hub.GrpcTests

open System
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

[<Collection("HubGrpc")>]
type PresetEncyclopediaTests(hub: HubTestFixture) =

    let opts () = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
    let stub () = hub.Stub

    let testPresetName (suffix: string) = sprintf "test-043-%s" suffix

    let assertSent (result: MutationResult option) =
        match result with
        | Some r when r.Outcome = SubmitOutcome.Rejected ->
            Assert.Fail(sprintf "mutation rejected: %s" r.Reason)
        | _ -> ()

    let savePreset (name: string) =
        task {
            let req = { SavePresetRequest.empty with Name = name }
            let! resp = stub().SavePresetAsync(opts()) req
            assertSent resp.Result
        }

    let deletePreset (name: string) =
        task {
            let! _ = stub().DeletePresetAsync(opts()) { DeletePresetRequest.empty with Name = name }
            ()
        }

    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcPreset")>]
    member _.``FR-015 — SavePreset_AppearsInListPresets``() = task {
        let name = testPresetName "save-list"
        do! savePreset name
        let! listResp = stub().ListPresetsAsync(opts()) ListPresetsRequest.Unused
        let names = listResp.Presets |> List.map (fun p -> p.Name)
        do! deletePreset name
        Assert.Contains(name, names)
    }

    [<Fact>]
    [<Trait("Category", "GrpcPreset")>]
    member _.``FR-015 — LoadPreset_ReflectedInGetHubState``() = task {
        let stub = stub()
        let name = testPresetName "load-reflect"
        let! initialState = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        let initialConfig = initialState.VizConfig |> Option.defaultValue VizConfigWire.empty
        let req = { SavePresetRequest.empty with Name = name; VizConfig = Some initialConfig }
        let! saveResp = stub.SavePresetAsync(opts()) req
        assertSent saveResp.Result

        let! loadResp = stub.LoadPresetAsync(opts()) { LoadPresetRequest.empty with Name = name }
        assertSent loadResp.Result

        let! afterState = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        let afterConfig = afterState.VizConfig |> Option.defaultValue VizConfigWire.empty
        do! deletePreset name
        for kvp in initialConfig.Attributes do
            match afterConfig.Attributes |> Map.tryFind kvp.Key with
            | Some v -> Assert.Equal(kvp.Value, v)
            | None -> ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcPreset")>]
    member _.``FR-015 — DeletePreset_RemovedFromListPresets``() = task {
        let name = testPresetName "delete-check"
        do! savePreset name
        do! deletePreset name
        let! listResp = stub().ListPresetsAsync(opts()) ListPresetsRequest.Unused
        let names = listResp.Presets |> List.map (fun p -> p.Name)
        Assert.DoesNotContain(name, names)
    }

    [<Fact>]
    [<Trait("Category", "GrpcPreset")>]
    member _.``FR-016 — ListUnits_CountMatchesBarDataCatalogue``() = task {
        let! resp = stub().ListUnitsAsync(opts()) ListUnitsRequest.empty
        let expected = BarData.AllUnitDefs.all |> Seq.length
        Assert.Equal(expected, resp.Entries.Length)
    }

    [<Fact>]
    [<Trait("Category", "GrpcPreset")>]
    member _.``FR-016 — SelectUnit_ValidName_ReflectedInGetHubState``() = task {
        let stub = stub()
        let req = { SelectUnitRequest.empty with Selector = SelectUnitRequest.SelectorCase.InternalName "armcom" }
        let! resp = stub.SelectUnitAsync(opts()) req
        assertSent resp.Result
        let! state = stub.GetHubStateAsync(opts()) GetHubStateRequest.empty
        let hasSelection =
            state.Encyclopedia
            |> Option.map (fun e -> e.SelectedDefId.IsSome)
            |> Option.defaultValue false
        Assert.True(hasSelection, "expected encyclopedia selection reflected in hub state after SelectUnit armcom")
    }
