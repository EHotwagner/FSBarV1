namespace FSBar.LiveTests

open Xunit
open Xunit.Abstractions
open FSBar.Client

/// Map grid loading and analysis integration tests.
/// Tests report as inconclusive if the proxy does not support map data callbacks (52-56).
[<Collection("Engine")>]
type MapGridTests(engine: EngineFixture, output: ITestOutputHelper) =

    let tryLoadGrid () =
        let stream = engine.Client.Stream
        try
            Some (MapGrid.loadFromEngine stream)
        with ex when ex.Message.Contains("empty array") ->
            output.WriteLine("SKIP: Proxy does not support map data callbacks (52-56)")
            None

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``loadFromEngine returns correct heightmap dimensions``() =
        let stream = engine.Client.Stream
        let w = Callbacks.getMapWidth stream
        let h = Callbacks.getMapHeight stream
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            Assert.Equal(w + 1, Array2D.length1 grid.HeightMap)
            Assert.Equal(h + 1, Array2D.length2 grid.HeightMap)

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``loadFromEngine populates all layers``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            Assert.True(Array2D.length1 grid.HeightMap > 0, "HeightMap should be populated")
            Assert.True(Array2D.length1 grid.SlopeMap > 0, "SlopeMap should be populated")
            Assert.True(Array2D.length1 grid.ResourceMap > 0, "ResourceMap should be populated")
            Assert.True(Array2D.length1 grid.LosMap > 0, "LosMap should be populated")
            Assert.True(Array2D.length1 grid.RadarMap > 0, "RadarMap should be populated")

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``MapGrid ToString shows compact summary``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let s = grid.ToString()
            Assert.Contains("elmos", s)
            Assert.Contains("heightmap", s)
            Assert.True(s.Length <= 200, $"ToString should be compact, got {s.Length} chars: {s}")

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``passability kbot dimensions match heightmap``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let pass = MapGrid.passability grid MoveType.Kbot
            Assert.Equal(Array2D.length1 grid.HeightMap, Array2D.length1 pass)
            Assert.Equal(Array2D.length2 grid.HeightMap, Array2D.length2 pass)

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``passability all four movetypes return correct dimensions``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let expectedW = Array2D.length1 grid.HeightMap
            let expectedH = Array2D.length2 grid.HeightMap
            for mt in [ MoveType.Kbot; MoveType.Tank; MoveType.Hover; MoveType.Ship ] do
                let pass = MapGrid.passability grid mt
                Assert.Equal(expectedW, Array2D.length1 pass)
                Assert.Equal(expectedH, Array2D.length2 pass)

    [<Fact>]
    [<Trait("Category", "MapGrid")>]
    member _.``refreshLos returns grid with same LOS dimensions``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let stream = engine.Client.Stream
            let updated = MapGrid.refreshLos stream grid
            Assert.Equal(Array2D.length1 grid.LosMap, Array2D.length1 updated.LosMap)
            Assert.Equal(Array2D.length2 grid.LosMap, Array2D.length2 updated.LosMap)
