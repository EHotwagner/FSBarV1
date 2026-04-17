module FSBar.Viz.Tests.MapDataTests

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``save then load round-trips a MapGrid and metal spots`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let spots = [| (100.0f, 0.0f, 200.0f, 1.5f); (300.0f, 0.0f, 400.0f, 2.0f) |]
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-test-{Guid.NewGuid()}.fsmg")
    try
        MapData.save path grid spots
        let (loadedGrid, loadedSpots) = MapData.load path
        Assert.Equal(grid.WidthElmos, loadedGrid.WidthElmos)
        Assert.Equal(grid.HeightElmos, loadedGrid.HeightElmos)
        Assert.Equal(grid.WidthHeightmap, loadedGrid.WidthHeightmap)
        Assert.Equal(grid.HeightHeightmap, loadedGrid.HeightHeightmap)
        Assert.Equal(spots.Length, loadedSpots.Length)
        for i in 0 .. spots.Length - 1 do
            let (x1, y1, z1, r1) = spots.[i]
            let (x2, y2, z2, r2) = loadedSpots.[i]
            Assert.Equal(x1, x2)
            Assert.Equal(y1, y2)
            Assert.Equal(z1, z2)
            Assert.Equal(r1, r2)
    finally
        if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``load with wrong magic bytes throws`` () =
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-bad-magic-{Guid.NewGuid()}.fsmg")
    try
        File.WriteAllBytes(path, [| 0uy; 0uy; 0uy; 0uy; 1uy; 0uy; 0uy; 0uy |])
        let ex = Assert.Throws<Exception>(fun () -> MapData.load path |> ignore)
        Assert.Contains("magic", ex.Message, StringComparison.OrdinalIgnoreCase)
    finally
        if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``load with wrong version throws`` () =
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-bad-version-{Guid.NewGuid()}.fsmg")
    try
        use stream = new FileStream(path, FileMode.Create, FileAccess.Write)
        use writer = new BinaryWriter(stream)
        writer.Write("FSMG"B)
        writer.Write(99) // wrong version
        stream.Close()
        let ex = Assert.Throws<Exception>(fun () -> MapData.load path |> ignore)
        Assert.Contains("version", ex.Message, StringComparison.OrdinalIgnoreCase)
    finally
        if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``loaded grid dimensions match saved grid`` () =
    let grid = SyntheticMapGrid.build {| width = 24; height = 32; seed = None |}
    let spots = Array.empty
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-dims-{Guid.NewGuid()}.fsmg")
    try
        MapData.save path grid spots
        let (loadedGrid, _) = MapData.load path
        Assert.Equal(24, loadedGrid.WidthHeightmap)
        Assert.Equal(32, loadedGrid.HeightHeightmap)
        Assert.Equal(24 * 8, loadedGrid.WidthElmos)
        Assert.Equal(32 * 8, loadedGrid.HeightElmos)
        // Verify heightmap dimensions
        Assert.Equal(25, Array2D.length1 loadedGrid.HeightMap)
        Assert.Equal(33, Array2D.length2 loadedGrid.HeightMap)
    finally
        if File.Exists(path) then File.Delete(path)
