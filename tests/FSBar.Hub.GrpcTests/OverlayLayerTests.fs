namespace FSBar.Hub.GrpcTests

open System
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

module private OverlayHelpers =

    let pt (x: float32) (y: float32) : OverlayPoint option =
        Some { OverlayPoint.empty with X = x; Y = y }

    let linePrim () =
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Line
                { LinePrimitive.empty with From = pt 0.0f 0.0f; To = pt 1.0f 1.0f } }

    let polylinePrim () =
        let pts = [ { OverlayPoint.empty with X = 0.0f; Y = 0.0f }
                    { OverlayPoint.empty with X = 1.0f; Y = 1.0f }
                    { OverlayPoint.empty with X = 2.0f; Y = 0.0f } ]
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Polyline
                { PolylinePrimitive.empty with Points = pts } }

    let polygonPrim () =
        let pts = [ { OverlayPoint.empty with X = 0.0f; Y = 0.0f }
                    { OverlayPoint.empty with X = 1.0f; Y = 0.0f }
                    { OverlayPoint.empty with X = 0.5f; Y = 1.0f } ]
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Polygon
                { PolygonPrimitive.empty with Points = pts } }

    let rectPrim () =
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Rectangle
                { RectanglePrimitive.empty with X = 0.0f; Y = 0.0f; Width = 10.0f; Height = 10.0f } }

    let circlePrim () =
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Circle
                { CirclePrimitive.empty with Center = pt 5.0f 5.0f; Radius = 3.0f } }

    let pathPrim () =
        let moveTo = { PathVerb.empty with Verb = PathVerb.VerbCase.MoveTo { OverlayPoint.empty with X = 0.0f; Y = 0.0f } }
        let lineTo = { PathVerb.empty with Verb = PathVerb.VerbCase.LineTo { OverlayPoint.empty with X = 5.0f; Y = 5.0f } }
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Path
                { PathPrimitive.empty with Verbs = [ moveTo; lineTo ] } }

    let textPrim () =
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Text
                { TextPrimitive.empty with Anchor = pt 5.0f 5.0f; Text = "test"; FontSize = 12.0f } }

    let imagePrim () =
        let pngBytes =
            [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
               0x00uy; 0x00uy; 0x00uy; 0x0Duy; 0x49uy; 0x48uy; 0x44uy; 0x52uy
               0x00uy; 0x00uy; 0x00uy; 0x01uy; 0x00uy; 0x00uy; 0x00uy; 0x01uy
               0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy; 0x90uy; 0x77uy; 0x53uy
               0xDEuy; 0x00uy; 0x00uy; 0x00uy; 0x0Cuy; 0x49uy; 0x44uy; 0x41uy
               0x54uy; 0x08uy; 0xD7uy; 0x63uy; 0xF8uy; 0xCFuy; 0xC0uy; 0x00uy
               0x00uy; 0x00uy; 0x02uy; 0x00uy; 0x01uy; 0xE2uy; 0x21uy; 0xBCuy
               0x33uy; 0x00uy; 0x00uy; 0x00uy; 0x00uy; 0x49uy; 0x45uy; 0x4Euy
               0x44uy; 0xAEuy; 0x42uy; 0x60uy; 0x82uy |]
        { OverlayPrimitive.empty with
            Space = CoordinateSpace.Screen
            Primitive = OverlayPrimitive.PrimitiveCase.Image
                { ImagePrimitive.empty with
                    Anchor = pt 0.0f 0.0f
                    Width = 1
                    Height = 1
                    Bytes = FsGrpc.Bytes.CopyFrom(pngBytes) } }

    let makeLayer (name: string) (prims: OverlayPrimitive list) : OverlayLayerWire =
        { OverlayLayerWire.empty with Name = name; Primitives = prims }

[<Collection("HubGrpc")>]
type OverlayLayerTests(hub: HubTestFixture) =

    let opts () = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(15.0)))
    let stub () = hub.Stub

    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-011 — PutLayer_AllPrimitiveTypes_ListLayersReturnsCorrectCount``() = task {
        let stub = stub()

        let layers =
            [ "overlay-test-line",    [ OverlayHelpers.linePrim() ]
              "overlay-test-polyline", [ OverlayHelpers.polylinePrim() ]
              "overlay-test-polygon",  [ OverlayHelpers.polygonPrim() ]
              "overlay-test-rect",     [ OverlayHelpers.rectPrim() ]
              "overlay-test-circle",   [ OverlayHelpers.circlePrim() ]
              "overlay-test-path",     [ OverlayHelpers.pathPrim() ]
              "overlay-test-text",     [ OverlayHelpers.textPrim() ]
              "overlay-test-image",    [ OverlayHelpers.imagePrim() ] ]

        try
            for (name, prims) in layers do
                let layer = OverlayHelpers.makeLayer name prims
                let! resp = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some layer }
                match resp.Result with
                | Some r when r.Outcome = SubmitOutcome.Rejected ->
                    Assert.Fail(sprintf "PutLayer '%s' rejected: %s (cap: %s, errors: %A)" name r.Reason resp.ExceededCap resp.ValidationErrors)
                | _ -> ()

            let! listResp = stub.ListLayersAsync(opts()) ListLayersRequest.Unused
            let layerNames = listResp.Layers |> List.map (fun l -> l.Name) |> Set.ofList
            for (name, _) in layers do
                Assert.Contains(name, layerNames)
        finally
            for (name, _) in layers do
                let! _ = stub.DeleteLayerAsync(opts()) { DeleteLayerRequest.empty with Name = name }
                ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-011 — ClearLayers_RemovesAllClientLayers``() = task {
        let stub = stub()
        let names = [ "overlay-clear-1"; "overlay-clear-2"; "overlay-clear-3" ]
        for name in names do
            let layer = OverlayHelpers.makeLayer name [ OverlayHelpers.linePrim() ]
            let! _ = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some layer }
            ()

        let! _ = stub.ClearLayersAsync(opts()) ClearLayersRequest.Unused
        let! listResp = stub.ListLayersAsync(opts()) ListLayersRequest.Unused
        let remaining = listResp.Layers |> List.filter (fun l -> names |> List.contains l.Name)
        Assert.Empty(remaining)
    }

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-011 — DeleteLayer_RemovesSingleLayer``() = task {
        let stub = stub()
        let! _ = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some (OverlayHelpers.makeLayer "overlay-del-keep" [ OverlayHelpers.linePrim() ]) }
        let! _ = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some (OverlayHelpers.makeLayer "overlay-del-remove" [ OverlayHelpers.linePrim() ]) }

        let! _ = stub.DeleteLayerAsync(opts()) { DeleteLayerRequest.empty with Name = "overlay-del-remove" }

        let! listResp = stub.ListLayersAsync(opts()) ListLayersRequest.Unused
        let names = listResp.Layers |> List.map (fun l -> l.Name)
        Assert.Contains("overlay-del-keep", names)
        Assert.DoesNotContain("overlay-del-remove", names)
        let! _ = stub.DeleteLayerAsync(opts()) { DeleteLayerRequest.empty with Name = "overlay-del-keep" }
        ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-012 — PutLayer_17th_ReturnsCapacityError``() = task {
        let stub = stub()
        let names = ResizeArray<string>()
        try
            for i in 1 .. 16 do
                let name = sprintf "overlay-cap-test-%02d" i
                names.Add(name)
                let layer = OverlayHelpers.makeLayer name [ OverlayHelpers.linePrim() ]
                let! _ = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some layer }
                ()

            let! resp = stub.PutLayerAsync(opts())
                            { PutLayerRequest.empty with
                                Layer = Some (OverlayHelpers.makeLayer "overlay-cap-test-17" [ OverlayHelpers.linePrim() ]) }
            match resp.Result with
            | Some r when r.Outcome = SubmitOutcome.Rejected ->
                Assert.False(String.IsNullOrEmpty(resp.ExceededCap), "expected ExceededCap to name the violated cap")
                Assert.Contains("layers_per_client", resp.ExceededCap)
            | other -> Assert.Fail(sprintf "expected rejection for 17th layer, got: %A" other)
        finally
            for name in names do
                let! _ = stub.DeleteLayerAsync(opts()) { DeleteLayerRequest.empty with Name = name }
                ()
            let! _ = stub.DeleteLayerAsync(opts()) { DeleteLayerRequest.empty with Name = "overlay-cap-test-17" }
            ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-012 — PutLayer_501Primitives_ReturnsInvalidArgument``() = task {
        let stub = stub()
        let prims = List.init 501 (fun _ -> OverlayHelpers.linePrim())
        let layer = OverlayHelpers.makeLayer "overlay-prim-cap-test" prims
        let! resp = stub.PutLayerAsync(opts()) { PutLayerRequest.empty with Layer = Some layer }
        match resp.Result with
        | Some r when r.Outcome = SubmitOutcome.Rejected ->
            Assert.False(String.IsNullOrEmpty(resp.ExceededCap), "expected ExceededCap for primitive count exceeded")
            Assert.Contains("primitives_per_layer", resp.ExceededCap)
        | other -> Assert.Fail(sprintf "expected rejection for 501 primitives, got: %A" other)
    }

    [<Fact>]
    [<Trait("Category", "GrpcOverlay")>]
    member _.``FR-013 — ClientDisconnect_LayersRemovedWithin5s``() = task {
        let ch = GrpcChannel.ForAddress(sprintf "http://127.0.0.1:%d" hub.Port)
        let otherStub = ScriptingService.Client(ch)
        let otherOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(15.0)))

        let! _ = otherStub.PutLayerAsync(otherOpts)
                     { PutLayerRequest.empty with
                         Layer = Some (OverlayHelpers.makeLayer "overlay-disconnect-test" [ OverlayHelpers.linePrim() ]) }
        ch.Dispose()

        let sw = System.Diagnostics.Stopwatch.StartNew()
        let mutable layersGone = false
        while not layersGone && sw.ElapsedMilliseconds < 5000L do
            let! listResp = stub().ListLayersAsync(opts()) ListLayersRequest.Unused
            let hasLayer = listResp.Layers |> List.exists (fun l -> l.Name = "overlay-disconnect-test")
            if not hasLayer then layersGone <- true
            else do! Task.Delay(500)
        Assert.True(layersGone, "layers should be auto-removed within 5s of client disconnect")
    }
