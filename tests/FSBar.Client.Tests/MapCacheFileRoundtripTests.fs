module FSBar.Client.Tests.MapCacheFileRoundtripTests

open System.IO
open Xunit
open FSBar.Client

let tinyGrid () : MapGrid =
    let w, h = 8, 8
    let heightMap = Array2D.init (w + 1) (h + 1) (fun i j -> float32 (i * 10 + j))
    let slopeW, slopeH = 4, 4
    let slopeMap = Array2D.init slopeW slopeH (fun i j -> float32 (i + j) * 0.05f)
    let resourceMap = Array2D.init w h (fun i j -> i * 100 + j)
    let losMap = Array2D.create w h 0
    let radarMap = Array2D.create w h 0
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = heightMap
      SlopeMap = slopeMap
      ResourceMap = resourceMap
      LosMap = losMap
      RadarMap = radarMap }

let tinySupported () : MapCacheFile.SupportedMap =
    { MapName = "Synth Tiny"
      Sd7FileStem = "synth_tiny"
      BaseCentre = (32.0f, 0.0f, 32.0f)
      ChokepointQuery =
        { Chokepoints.defaultChokepointQuery MoveType.Kbot with
            MaxWidthElmos = 40.0f
            SearchRadiusElmos = 200.0f } }

let array2DEqual (a: 'a[,]) (b: 'a[,]) =
    Array2D.length1 a = Array2D.length1 b
    && Array2D.length2 a = Array2D.length2 b
    && seq {
        for i in 0 .. Array2D.length1 a - 1 do
            for j in 0 .. Array2D.length2 a - 1 do
                yield a.[i, j] = b.[i, j]
    }
    |> Seq.forall id

let mapA () : MapCacheFile.SupportedMap * MapGrid =
    let w, h = 8, 8
    let grid : MapGrid =
        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = Array2D.init (w + 1) (h + 1) (fun i j -> float32 (i + j))
          SlopeMap = Array2D.init (w / 2) (h / 2) (fun i j -> float32 (i * j) * 0.01f)
          ResourceMap = Array2D.init w h (fun i j -> i + j * 2)
          LosMap = Array2D.create w h 0
          RadarMap = Array2D.create w h 0 }
    let supported : MapCacheFile.SupportedMap =
        { MapName = "Synth A 8x8"
          Sd7FileStem = "synth_a"
          BaseCentre = (32.0f, 0.0f, 32.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Kbot with
                MaxWidthElmos = 40.0f
                SearchRadiusElmos = 200.0f } }
    supported, grid

let mapB () : MapCacheFile.SupportedMap * MapGrid =
    let w, h = 16, 16
    let grid : MapGrid =
        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = Array2D.init (w + 1) (h + 1) (fun i j -> float32 (i * 2 + j * 3))
          SlopeMap = Array2D.init (w / 2) (h / 2) (fun i j -> float32 (i + j) * 0.02f)
          ResourceMap = Array2D.init w h (fun i j -> i * 3 + j)
          LosMap = Array2D.create w h 0
          RadarMap = Array2D.create w h 0 }
    let supported : MapCacheFile.SupportedMap =
        { MapName = "Synth B 16x16"
          Sd7FileStem = "synth_b"
          BaseCentre = (64.0f, 0.0f, 64.0f)
          ChokepointQuery =
            { Chokepoints.defaultChokepointQuery MoveType.Tank with
                MaxWidthElmos = 60.0f
                SearchRadiusElmos = 300.0f } }
    supported, grid

[<Fact>]
let ``two distinct SupportedMaps roundtrip and cross-read fails with MapNameMismatch`` () =
    let supA, gridA = mapA ()
    let supB, gridB = mapB ()
    let cpsA = Chokepoints.findChokepoints gridA supA.BaseCentre supA.ChokepointQuery
    let cpsB = Chokepoints.findChokepoints gridB supB.BaseCentre supB.ChokepointQuery
    let pA = Path.GetTempFileName()
    let pB = Path.GetTempFileName()
    try
        MapCacheFile.write supA gridA cpsA pA
        MapCacheFile.write supB gridB cpsB pB
        match MapCacheFile.read supA pA with
        | Result.Ok loaded -> Assert.Equal(supA.MapName, loaded.MapName)
        | Result.Error e -> Assert.Fail(sprintf "A self-read failed: %A" e)
        match MapCacheFile.read supB pB with
        | Result.Ok loaded -> Assert.Equal(supB.MapName, loaded.MapName)
        | Result.Error e -> Assert.Fail(sprintf "B self-read failed: %A" e)
        match MapCacheFile.read supB pA with
        | Result.Error (MapCacheFile.MapNameMismatch _) -> ()
        | other -> Assert.Fail(sprintf "expected MapNameMismatch cross-read A-with-B, got %A" other)
    finally
        File.Delete pA
        File.Delete pB

[<Fact>]
let ``tryFindSupportedMap returns None for unknown and Some for Avalanche 3.4`` () =
    Assert.Equal(None, MapCacheFile.tryFindSupportedMap "Nonexistent Map 99")
    match MapCacheFile.tryFindSupportedMap "Avalanche 3.4" with
    | Some s -> Assert.Equal("Avalanche 3.4", s.MapName)
    | None -> Assert.Fail("expected Avalanche 3.4 in supportedMaps")

[<Fact>]
let ``write then read returns the same grid and chokepoints`` () =
    let grid = tinyGrid ()
    let supported = tinySupported ()
    let cps = Chokepoints.findChokepoints grid supported.BaseCentre supported.ChokepointQuery
    let path = Path.GetTempFileName()
    try
        MapCacheFile.write supported grid cps path
        match MapCacheFile.read supported path with
        | Result.Error e -> Assert.Fail(sprintf "read returned Error %A" e)
        | Result.Ok loaded ->
            Assert.True(array2DEqual grid.HeightMap loaded.Grid.HeightMap, "heightMap mismatch")
            Assert.True(array2DEqual grid.SlopeMap loaded.Grid.SlopeMap, "slopeMap mismatch")
            Assert.True(array2DEqual grid.ResourceMap loaded.Grid.ResourceMap, "resourceMap mismatch")
            Assert.Equal<Chokepoint list>(cps, loaded.Chokepoints)
            Assert.Equal(supported.BaseCentre, loaded.BaseCentre)
            Assert.Equal(supported.MapName, loaded.MapName)
    finally
        if File.Exists path then File.Delete path
