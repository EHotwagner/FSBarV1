module FSBar.Viz.Tests.MapDataTests

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Viz

let private makeTestGrid () : MapGrid =
    let w = 4
    let h = 4
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = Array2D.init (h + 1) (w + 1) (fun r c -> float32 (r * 10 + c))
      SlopeMap = Array2D.init (h / 2) (w / 2) (fun r c -> float32 (r + c) * 0.1f)
      ResourceMap = Array2D.init h w (fun r c -> r * 100 + c)
      LosMap = Array2D.init h w (fun r c -> if r = c then 1 else 0)
      RadarMap = Array2D.init h w (fun r c -> if r + c > 2 then 1 else 0) }

let private makeTestSpots () =
    [| (10.0f, 0.0f, 20.0f, 5.0f)
       (30.0f, 0.0f, 40.0f, 3.0f) |]

[<Fact>]
let ``round-trip save and load preserves all fields`` () =
    let grid = makeTestGrid ()
    let spots = makeTestSpots ()
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-test-{Guid.NewGuid()}.fsmg")
    try
        MapData.save path grid spots
        let (loaded, loadedSpots) = MapData.load path

        Assert.Equal(grid.WidthHeightmap, loaded.WidthHeightmap)
        Assert.Equal(grid.HeightHeightmap, loaded.HeightHeightmap)
        Assert.Equal(grid.WidthElmos, loaded.WidthElmos)
        Assert.Equal(grid.HeightElmos, loaded.HeightElmos)

        // HeightMap
        for r in 0 .. Array2D.length1 grid.HeightMap - 1 do
            for c in 0 .. Array2D.length2 grid.HeightMap - 1 do
                Assert.Equal(grid.HeightMap.[r, c], loaded.HeightMap.[r, c])

        // SlopeMap
        for r in 0 .. Array2D.length1 grid.SlopeMap - 1 do
            for c in 0 .. Array2D.length2 grid.SlopeMap - 1 do
                Assert.Equal(grid.SlopeMap.[r, c], loaded.SlopeMap.[r, c])

        // ResourceMap
        for r in 0 .. Array2D.length1 grid.ResourceMap - 1 do
            for c in 0 .. Array2D.length2 grid.ResourceMap - 1 do
                Assert.Equal(grid.ResourceMap.[r, c], loaded.ResourceMap.[r, c])

        // LosMap
        for r in 0 .. Array2D.length1 grid.LosMap - 1 do
            for c in 0 .. Array2D.length2 grid.LosMap - 1 do
                Assert.Equal(grid.LosMap.[r, c], loaded.LosMap.[r, c])

        // RadarMap
        for r in 0 .. Array2D.length1 grid.RadarMap - 1 do
            for c in 0 .. Array2D.length2 grid.RadarMap - 1 do
                Assert.Equal(grid.RadarMap.[r, c], loaded.RadarMap.[r, c])

        // Metal spots
        Assert.Equal(spots.Length, loadedSpots.Length)
        for i in 0 .. spots.Length - 1 do
            Assert.Equal(spots.[i], loadedSpots.[i])
    finally
        if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``load rejects file with wrong magic bytes`` () =
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-bad-{Guid.NewGuid()}.fsmg")
    try
        File.WriteAllBytes(path, [| 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy; 0uy |])
        let ex = Assert.Throws<Exception>(fun () -> MapData.load path |> ignore)
        Assert.Contains("magic bytes", ex.Message)
    finally
        if File.Exists(path) then File.Delete(path)

[<Fact>]
let ``load rejects truncated file`` () =
    let path = Path.Combine(Path.GetTempPath(), $"mapdata-trunc-{Guid.NewGuid()}.fsmg")
    try
        // Write valid header but truncate before array data
        use fs = File.Create(path)
        use bw = new BinaryWriter(fs)
        bw.Write([| byte 'F'; byte 'S'; byte 'M'; byte 'G' |], 0, 4)
        bw.Write(1) // version
        bw.Write(4) // width
        bw.Write(4) // height
        // No array data — truncated
        bw.Flush()
        fs.Close()

        let ex = Assert.Throws<Exception>(fun () -> MapData.load path |> ignore)
        Assert.Contains("Truncated", ex.Message)
    finally
        if File.Exists(path) then File.Delete(path)
