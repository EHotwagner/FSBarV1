module FSBar.Hub.Tests.OverlayLayerStoreTests

// Feature 040 T058 — unit tests for OverlayLayerStore (US6).

open System
open System.Threading
open Xunit
open FSBar.Hub

let private makeStore () =
    let bus = HubEvents.create ()
    let store = OverlayLayerStore.create bus.Sink
    store, bus

let private defaultStyle : OverlayStyle =
    { StrokeColorRgba = 0xFFFFFFFFu
      StrokeWidth = 1.0f
      FillColorRgba = None
      Opacity = 1.0f
      Dash = None }

let private circlePrim (x: float32) (y: float32) : OverlayPrimitive =
    Circle({ X = x; Y = y }, 10.0f, defaultStyle, World)

let private pngMagic =
    // minimal byte array that passes the PNG-magic check
    [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

[<Fact>]
let ``T058a — put/replace/delete/list roundtrip`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let layer1 : OverlayLayer = {
            Name = "demo"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [ circlePrim 100.0f 100.0f ]
        }
        match OverlayLayerStore.putLayer store clientA layer1 with
        | Ok () -> ()
        | Error e -> Assert.Fail(sprintf "first put should succeed: %A" e)

        let list1 = OverlayLayerStore.listLayers store clientA
        Assert.Single(list1) |> ignore
        Assert.Equal<string>("demo", list1.[0].Name)
        Assert.Equal(1, list1.[0].PrimitiveCount)

        // Replace with more primitives.
        let layer2 = { layer1 with Primitives = [ circlePrim 0.0f 0.0f; circlePrim 10.0f 10.0f ] }
        match OverlayLayerStore.putLayer store clientA layer2 with
        | Ok () -> ()
        | Error e -> Assert.Fail(sprintf "replace should succeed: %A" e)

        let list2 = OverlayLayerStore.listLayers store clientA
        Assert.Single(list2) |> ignore
        Assert.Equal(2, list2.[0].PrimitiveCount)

        // Delete.
        match OverlayLayerStore.deleteLayer store clientA "demo" with
        | Sent -> ()
        | Rejected r -> Assert.Fail(sprintf "delete rejected: %s" r)

        Assert.Empty(OverlayLayerStore.listLayers store clientA)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058b — per-client isolation`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let clientB = Guid.NewGuid()
        let layerA : OverlayLayer = {
            Name = "a-layer"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [ circlePrim 0.0f 0.0f ]
        }
        OverlayLayerStore.putLayer store clientA layerA |> ignore
        Assert.Empty(OverlayLayerStore.listLayers store clientB)
        OverlayLayerStore.deleteLayer store clientB "a-layer" |> ignore
        // Client A's layer is untouched.
        Assert.Single(OverlayLayerStore.listLayers store clientA) |> ignore
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058c — cap: primitives per layer`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let prims = [ for i in 1 .. 501 -> circlePrim (float32 i) 0.0f ]
        let layer : OverlayLayer = {
            Name = "too-many"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = prims
        }
        match OverlayLayerStore.putLayer store clientA layer with
        | Error (CapExceeded PrimitivesPerLayer) -> ()
        | r -> Assert.Fail(sprintf "expected CapExceeded PrimitivesPerLayer; got %A" r)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058d — cap: layers per client`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        // Add 16 layers — all should succeed.
        for i in 1 .. 16 do
            let layer : OverlayLayer = {
                Name = sprintf "layer-%d" i
                ZHint = 0
                UploadedAtUnixMs = 0L
                Primitives = [ circlePrim 0.0f 0.0f ]
            }
            match OverlayLayerStore.putLayer store clientA layer with
            | Ok () -> ()
            | Error e -> Assert.Fail(sprintf "layer %d unexpectedly failed: %A" i e)
        // 17th triggers the cap.
        let overflow : OverlayLayer = {
            Name = "overflow"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [ circlePrim 0.0f 0.0f ]
        }
        match OverlayLayerStore.putLayer store clientA overflow with
        | Error (CapExceeded LayersPerClient) -> ()
        | r -> Assert.Fail(sprintf "expected CapExceeded LayersPerClient; got %A" r)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058e — validation: invalid name rejected`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let badLayer : OverlayLayer = {
            Name = "has/slash"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [ circlePrim 0.0f 0.0f ]
        }
        match OverlayLayerStore.putLayer store clientA badLayer with
        | Error (InvalidName _) -> ()
        | r -> Assert.Fail(sprintf "expected InvalidName; got %A" r)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058f — validation: polygon needs >= 3 points`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let layer : OverlayLayer = {
            Name = "bad-polygon"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [
                Polygon([ { X = 0.0f; Y = 0.0f }; { X = 1.0f; Y = 0.0f } ],
                    defaultStyle, World)
            ]
        }
        match OverlayLayerStore.putLayer store clientA layer with
        | Error (ValidationFailed errs) ->
            Assert.Contains(errs, fun e -> e.Contains("polygon needs"))
        | r -> Assert.Fail(sprintf "expected ValidationFailed; got %A" r)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058g — snapshot orders by (ownerId, zHint, uploadedAt)`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let clientB = Guid.NewGuid()
        let mk name z upAt : OverlayLayer =
            { Name = name; ZHint = z; UploadedAtUnixMs = upAt
              Primitives = [ circlePrim 0.0f 0.0f ] }
        OverlayLayerStore.putLayer store clientA (mk "a-hi" 10 100L) |> ignore
        OverlayLayerStore.putLayer store clientA (mk "a-lo" 0 200L) |> ignore
        OverlayLayerStore.putLayer store clientB (mk "b-z" 5 50L) |> ignore
        let snap = OverlayLayerStore.snapshot store
        Assert.Equal(3, snap.Entries.Length)
        // Entries should be sorted by (ownerId, zHint, uploadedAt) —
        // zHint ordering is the most observable assertion.
        let aEntries =
            snap.Entries
            |> Array.filter (fun (id, _) -> id = clientA)
            |> Array.map (fun (_, l) -> l.ZHint)
        Assert.Equal<int array>([| 0; 10 |], aEntries)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058h — removeClient drops all layers`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        for i in 1 .. 3 do
            let layer : OverlayLayer = {
                Name = sprintf "layer-%d" i
                ZHint = 0
                UploadedAtUnixMs = 0L
                Primitives = [ circlePrim 0.0f 0.0f ]
            }
            OverlayLayerStore.putLayer store clientA layer |> ignore
        OverlayLayerStore.removeClient store clientA
        Assert.Empty(OverlayLayerStore.listLayers store clientA)
    finally
        (bus :> IDisposable).Dispose()

[<Fact>]
let ``T058i — image primitive requires PNG/JPEG magic`` () =
    let store, bus = makeStore ()
    try
        let clientA = Guid.NewGuid()
        let fakeBytes = [| 0x00uy; 0x01uy; 0x02uy; 0x03uy |]
        let layer : OverlayLayer = {
            Name = "bad-image"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [
                Image({ X = 0.0f; Y = 0.0f }, 16, 16, fakeBytes, World)
            ]
        }
        match OverlayLayerStore.putLayer store clientA layer with
        | Error (ValidationFailed errs) ->
            Assert.Contains(errs, fun e -> e.Contains("magic"))
        | r -> Assert.Fail(sprintf "expected ValidationFailed; got %A" r)
        // Valid PNG magic should pass (given valid dimensions).
        let goodLayer : OverlayLayer = {
            Name = "good-image"
            ZHint = 0
            UploadedAtUnixMs = 0L
            Primitives = [
                Image({ X = 0.0f; Y = 0.0f }, 16, 16, pngMagic, World)
            ]
        }
        match OverlayLayerStore.putLayer store clientA goodLayer with
        | Ok () -> ()
        | Error e -> Assert.Fail(sprintf "PNG-magic image unexpectedly rejected: %A" e)
    finally
        (bus :> IDisposable).Dispose()
