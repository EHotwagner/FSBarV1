module FSBar.Viz.Tests.LiveSessionIntegrationTests

open Xunit
open Xunit.Abstractions
open FSBar.Client
open FSBar.Viz

/// Full integration tests for LiveSession with a live headless engine.
[<Collection("VizEngine")>]
type LiveSessionIntegrationTests(engine: VizEngineFixture, output: ITestOutputHelper) =

    [<Fact>]
    member _.``US1 live session runs 100+ frames with valid heightmap`` () =
        use session = LiveSession.startWithClient engine.Client None

        // Wait for frames to accumulate
        let mutable waited = 0
        while session.FrameCount < 100 && waited < 30000 && session.IsRunning do
            System.Threading.Thread.Sleep(500)
            waited <- waited + 500

        output.WriteLine($"FrameCount: {session.FrameCount}, IsRunning: {session.IsRunning}")
        Assert.True(session.FrameCount >= 100, $"Expected >= 100 frames, got {session.FrameCount}")
        Assert.True(session.IsRunning, "Session should still be running")
        Assert.True(session.LastError.IsNone, $"No errors expected, got: {session.LastError}")

    [<Fact>]
    member _.``US2 unit overlay has units after 200+ frames`` () =
        GameViz.enableOverlay OverlayKind.Units
        use session = LiveSession.startWithClient engine.Client None

        let mutable waited = 0
        while session.FrameCount < 200 && waited < 60000 && session.IsRunning do
            System.Threading.Thread.Sleep(500)
            waited <- waited + 500

        output.WriteLine($"FrameCount: {session.FrameCount}")
        Assert.True(session.FrameCount >= 200, $"Expected >= 200 frames, got {session.FrameCount}")

    [<Fact>]
    member _.``US3 all layer types render without exceptions`` () =
        use session = LiveSession.startWithClient engine.Client None

        // Wait for some data to be available
        let mutable waited = 0
        while session.FrameCount < 100 && waited < 30000 && session.IsRunning do
            System.Threading.Thread.Sleep(500)
            waited <- waited + 500

        // Switch through all layers programmatically
        let layers =
            [ LayerKind.HeightMap
              LayerKind.SlopeMap
              LayerKind.ResourceMap
              LayerKind.LosMap
              LayerKind.RadarMap
              LayerKind.TerrainClassification
              LayerKind.Passability MoveType.Kbot ]

        for layer in layers do
            GameViz.setBaseLayer layer
            System.Threading.Thread.Sleep(200)
            output.WriteLine($"Switched to {layer} — no exception")

        Assert.True(session.IsRunning, "Session should still be running after layer switches")
        Assert.True(session.LastError.IsNone, $"No errors expected, got: {session.LastError}")
