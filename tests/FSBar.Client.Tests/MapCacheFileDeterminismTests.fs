module FSBar.Client.Tests.MapCacheFileDeterminismTests

open System.IO
open Xunit
open FSBar.Client

let gridOf (w: int) (h: int) : MapGrid =
    let heightMap = Array2D.init (w + 1) (h + 1) (fun i j -> float32 (i * 3 + j * 7))
    let slopeMap = Array2D.init (w / 2) (h / 2) (fun i j -> float32 (i + j) * 0.01f)
    let resourceMap = Array2D.init w h (fun i j -> (i * 13 + j * 17) % 31)
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = heightMap
      SlopeMap = slopeMap
      ResourceMap = resourceMap
      LosMap = Array2D.create w h 0
      RadarMap = Array2D.create w h 0 }

let supportedOf mapName : MapCacheFile.SupportedMap =
    { MapName = mapName
      Sd7FileStem = "synth"
      BaseCentre = (64.0f, 0.0f, 64.0f)
      ChokepointQuery =
        { Chokepoints.defaultChokepointQuery MoveType.Kbot with
            MaxWidthElmos = 40.0f
            SearchRadiusElmos = 500.0f } }

let writeTwice (w: int) (h: int) =
    let grid = gridOf w h
    let supported = supportedOf (sprintf "Synth %dx%d" w h)
    let cps = Chokepoints.findChokepoints grid supported.BaseCentre supported.ChokepointQuery
    let p1 = Path.GetTempFileName()
    let p2 = Path.GetTempFileName()
    MapCacheFile.write supported grid cps p1
    MapCacheFile.write supported grid cps p2
    p1, p2

[<Fact>]
let ``two writes of same small grid produce byte-identical files`` () =
    let p1, p2 = writeTwice 8 8
    try
        let b1 = File.ReadAllBytes p1
        let b2 = File.ReadAllBytes p2
        Assert.Equal<byte[]>(b1, b2)
    finally
        File.Delete p1
        File.Delete p2

[<Fact>]
let ``two writes of same large grid produce byte-identical files`` () =
    let p1, p2 = writeTwice 64 64
    try
        let b1 = File.ReadAllBytes p1
        let b2 = File.ReadAllBytes p2
        Assert.Equal<byte[]>(b1, b2)
    finally
        File.Delete p1
        File.Delete p2
