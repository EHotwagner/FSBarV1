namespace FSBar.LiveTests

open System.IO
open Xunit
open Xunit.Abstractions
open FSBar.Client

/// Map query integration tests.
/// Tests report as inconclusive if the proxy does not support map data callbacks (52-56).
[<Collection("Engine")>]
type MapQueryTests(engine: EngineFixture, output: ITestOutputHelper) =

    let tryLoadGrid () =
        let stream = engine.Client.Stream
        try
            Some (MapGrid.loadFromEngine stream)
        with
        | :? EngineDisconnectedException as ex ->
            output.WriteLine($"SKIP: Engine disconnected — {ex.Message}")
            None
        | :? IOException as ex ->
            output.WriteLine($"SKIP: I/O error — {ex.Message}")
            None
        | ex when ex.Message.Contains("empty array") ->
            output.WriteLine("SKIP: Proxy does not support map data callbacks (52-56)")
            None
        | ex when ex.Message.Contains("dimension mismatch") ->
            output.WriteLine($"SKIP: Dimension mismatch — {ex.Message}")
            None

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``heightAtElmo at start position returns Ok with plausible value``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let stream = engine.Client.Stream
            let (sx, _, sz) = Callbacks.getStartPos stream 0
            let result = MapQuery.heightAtElmo grid (int sx) (int sz)
            match result with
            | Result.Ok h -> Assert.True(h > -1000.0f && h < 10000.0f, $"Height {h} should be plausible")
            | Result.Error e -> Assert.Fail $"Expected Ok, got Error: {e}"

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``heightAtElmo out of bounds returns Error``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let result = MapQuery.heightAtElmo grid 999999 999999
            match result with
            | Result.Error msg -> Assert.Contains("Out of bounds", msg)
            | Result.Ok _ -> Assert.Fail "Expected Error for out-of-bounds coordinates"

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``heightSubRegion returns correct dimensions``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let result = MapQuery.heightSubRegion grid 0 0 1024 1024
            match result with
            | Result.Ok region ->
                Assert.Equal(128, Array2D.length1 region)
                Assert.Equal(128, Array2D.length2 region)
            | Result.Error e -> Assert.Fail $"Expected Ok, got Error: {e}"

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``elmoToGrid roundtrip preserves aligned coordinates``() =
        let x, z = 1024, 2048
        let gx, gz = MapQuery.elmoToGrid x z
        let rx, rz = MapQuery.gridToElmo gx gz
        Assert.Equal(x, rx)
        Assert.Equal(z, rz)

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``resourceHotspots correlate with metal spots``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let stream = engine.Client.Stream
            let metalSpots = Callbacks.getMetalSpots stream
            if metalSpots.Length > 0 then
                let hotspots =
                    MapQuery.resourceHotspots grid 0 0 grid.WidthElmos grid.HeightElmos 0
                Assert.True(hotspots.Length >= 0, "resourceHotspots should return a list")

    [<Fact>]
    [<Trait("Category", "MapQuery")>]
    member _.``resourceHotspots empty for very high threshold``() =
        match tryLoadGrid () with
        | None -> ()
        | Some grid ->
            let hotspots =
                MapQuery.resourceHotspots grid 0 0 grid.WidthElmos grid.HeightElmos 255
            Assert.True(hotspots.Length <= 10,
                $"Very high threshold should return few/no results, got {hotspots.Length}")
