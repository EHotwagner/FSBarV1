module FSBar.Viz.Tests.GameVizIntegrationTests

open System
open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``start and stop without exception`` () =

    GameViz.start None
    System.Threading.Thread.Sleep(300)
    GameViz.stop ()

[<Fact>]
let ``setBaseLayer changes config without exception`` () =

    GameViz.start None
    try
        GameViz.setBaseLayer LayerKind.SlopeMap
        GameViz.setBaseLayer LayerKind.HeightMap
        System.Threading.Thread.Sleep(200)
    finally
        GameViz.stop ()

[<Fact>]
let ``toggleOverlay enables and disables`` () =

    GameViz.start None
    try
        GameViz.toggleOverlay OverlayKind.Units
        GameViz.toggleOverlay OverlayKind.Events
        GameViz.toggleOverlay OverlayKind.Grid
        GameViz.toggleOverlay OverlayKind.MetalSpots
        GameViz.toggleOverlay OverlayKind.EconomyHud
        GameViz.toggleOverlay OverlayKind.Units
        System.Threading.Thread.Sleep(200)
    finally
        GameViz.stop ()

[<Fact>]
let ``pan does not throw`` () =

    GameViz.start None
    try
        GameViz.pan 10.0f 20.0f
        GameViz.pan -5.0f -10.0f
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()

[<Fact>]
let ``zoom does not throw`` () =

    GameViz.start None
    try
        GameViz.zoom 1.5f 512.0f 320.0f
        GameViz.zoom 0.5f 512.0f 320.0f
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()

[<Fact>]
let ``enableOverlay and disableOverlay work`` () =

    GameViz.start None
    try
        GameViz.enableOverlay OverlayKind.Units
        GameViz.enableOverlay OverlayKind.EconomyHud
        GameViz.disableOverlay OverlayKind.Units
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()

[<Fact>]
let ``setMarkerSize and setOverlayOpacity work`` () =

    GameViz.start None
    try
        GameViz.setMarkerSize 10.0f
        GameViz.setOverlayOpacity 0.5f
        System.Threading.Thread.Sleep(100)
    finally
        GameViz.stop ()

[<Fact>]
let ``screenshot without viewer returns error`` () =
    GameViz.stop ()
    let result = GameViz.screenshot "/tmp"
    match result with
    | Result.Error _ -> ()
    | Result.Ok _ -> Assert.Fail("Expected error when no viewer is running")
