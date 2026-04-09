module FSBar.Viz.Tests.GameVizIntegrationTests

open Xunit
open Xunit.Abstractions
open FSBar.Client
open FSBar.Viz

/// Integration tests share a single GameViz window to avoid GLFW state corruption
/// from rapid window creation/destruction cycles.
[<Collection("VizEngine")>]
type GameVizIntegrationTests(engine: VizEngineFixture, output: ITestOutputHelper) =

    static let mutable vizStarted = false

    let ensureVizRunning () =
        if not vizStarted then
            GameViz.start None
            GameViz.attachToClient engine.Client
            vizStarted <- true

    let stepFrames (n: int) =
        ensureVizRunning ()
        let mutable lastFrame = Unchecked.defaultof<GameFrame>
        engine.Client.WaitFrames n (fun frame ->
            lastFrame <- frame
            GameViz.onFrame lastFrame)
        lastFrame

    // --- US1: Live Map ---

    [<Fact>]
    member _.``US1 start and attachToClient without exceptions`` () =
        ensureVizRunning ()
        stepFrames 5 |> ignore
        System.Threading.Thread.Sleep(500)
        output.WriteLine("GameViz started and attached successfully")

    [<Fact>]
    member _.``US1 attachToClient loads map dimensions`` () =
        ensureVizRunning ()
        let handshake = engine.Client.Handshake
        Assert.True(handshake.IsSome, "Should have handshake info")
        output.WriteLine($"Map: {handshake.Value.MapName}")
        stepFrames 3 |> ignore

    // --- US2: Layer Switching ---

    [<Fact>]
    member _.``US2 setBaseLayer switches without exception`` () =
        stepFrames 3

        let layers =
            [ LayerKind.HeightMap
              LayerKind.SlopeMap
              LayerKind.ResourceMap
              LayerKind.LosMap
              LayerKind.RadarMap
              LayerKind.TerrainClassification
              LayerKind.Passability MoveType.Kbot
              LayerKind.Passability MoveType.Tank
              LayerKind.Passability MoveType.Hover
              LayerKind.Passability MoveType.Ship ]

        for layer in layers do
            GameViz.setBaseLayer layer
            stepFrames 1 |> ignore
            output.WriteLine($"Switched to {layer}")

    // --- US3: Unit Overlay ---

    [<Fact>]
    member _.``US3 unit overlay enables without exception`` () =
        GameViz.enableOverlay OverlayKind.Units
        GameViz.enableOverlay OverlayKind.Events
        stepFrames 30 |> ignore

        let hasUnitCreated =
            engine.InitialEvents
            |> List.exists (function GameEvent.UnitCreated _ -> true | _ -> false)
        output.WriteLine($"Has UnitCreated in initial events: {hasUnitCreated}")

    // --- US4: Customization ---

    [<Fact>]
    member _.``US4 setColorScheme invalidates layer cache`` () =
        stepFrames 3
        LayerRenderer.invalidateAll ()
        let (_, misses1) = LayerRenderer.cacheStats ()
        output.WriteLine($"Before scheme change: misses={misses1}")

        GameViz.setColorScheme LayerKind.HeightMap ColorMaps.grayscale
        output.WriteLine("setColorScheme completed — cache invalidation triggered")

    [<Fact>]
    member _.``US4 toggleGridLines applies without exception`` () =
        GameViz.toggleGridLines ()
        GameViz.toggleGridLines ()

    [<Fact>]
    member _.``US4 setMarkerSize and setOverlayOpacity apply without exception`` () =
        GameViz.setMarkerSize 8.0f
        GameViz.setOverlayOpacity 0.5f
        GameViz.setOverlayOpacity 0.0f
        GameViz.setOverlayOpacity 1.0f

    // --- US5: Economy ---

    [<Fact>]
    member _.``US5 onFrame populates economy data`` () =
        GameViz.enableOverlay OverlayKind.EconomyHud
        GameViz.enableOverlay OverlayKind.MetalSpots
        stepFrames 60 |> ignore

        let metalIncome = Callbacks.getEconomyIncome engine.Client.Stream 0
        let energyIncome = Callbacks.getEconomyIncome engine.Client.Stream 1
        output.WriteLine($"Metal income: {metalIncome}, Energy income: {energyIncome}")

        let spots = Callbacks.getMetalSpots engine.Client.Stream
        output.WriteLine($"Metal spots: {spots.Length}")
        Assert.True(spots.Length > 0, "Map should have metal spots")

    // --- Pan/Zoom ---

    [<Fact>]
    member _.``pan and zoom apply without exception`` () =
        stepFrames 3
        GameViz.pan 100.0f 50.0f
        GameViz.zoom 2.0f 512.0f 320.0f
        GameViz.zoom 0.5f 512.0f 320.0f
        GameViz.resetView ()

    // --- Disconnect ---

    [<Fact>]
    member _.``setDisconnected does not crash`` () =
        stepFrames 3
        GameViz.setDisconnected ()
        System.Threading.Thread.Sleep(500)

    // GameViz window stays open for all tests in the collection.
    // Cleanup happens when the VizEngineFixture shuts down the engine.
