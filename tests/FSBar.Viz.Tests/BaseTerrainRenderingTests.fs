module FSBar.Viz.Tests.BaseTerrainRenderingTests

open Xunit
open SkiaSharp
open FSBar.Client
open FSBar.Viz
open FSBar.Viz.Tests.VizEngineFixture

let makeGrid (heights: float32[,]) : MapGrid =
    let h = Array2D.length1 heights
    let w = Array2D.length2 heights
    let slopeW = max 1 (w / 2)
    let slopeH = max 1 (h / 2)
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = heights
      SlopeMap = Array2D.init slopeH slopeW (fun _ _ -> 0.0f)
      ResourceMap = Array2D.init h w (fun _ _ -> 0)
      LosMap = Array2D.init h w (fun _ _ -> 0)
      RadarMap = Array2D.init h w (fun _ _ -> 0) }

let pixelAt (bmp: SKBitmap) (x: int) (z: int) : byte * byte * byte * byte =
    let c = bmp.GetPixel(x, z)
    c.Red, c.Green, c.Blue, c.Alpha

let luminance (r: byte) (g: byte) (b: byte) : float =
    0.2126 * float r + 0.7152 * float g + 0.0722 * float b

let scheme = ColorMaps.colorSchemeFor LayerKind.BaseTerrain

[<Fact>]
let ``renders land cells on brown ramp and water cells on blue ramp`` () =
    LayerRenderer.invalidateAll ()
    // 4x4 heightmap: top two rows water (negative), bottom two rows land (positive)
    let heights =
        Array2D.init 4 4 (fun z x ->
            if z < 2 then -10.0f - float32 x
            else 10.0f + float32 x)
    let grid = makeGrid heights
    let bmp = LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme
    Assert.Equal(4, bmp.Width)
    Assert.Equal(4, bmp.Height)
    for z in 0 .. 3 do
        for x in 0 .. 3 do
            let r, _g, b, a = pixelAt bmp x z
            Assert.Equal(255uy, a)
            if z < 2 then
                Assert.True(int b > int r, $"Water cell ({x},{z}) should be blue-dominant: r={r} b={b}")
            else
                Assert.True(int r > int b, $"Land cell ({x},{z}) should be brown-dominant: r={r} b={b}")

[<Fact>]
let ``is deterministic given identical input`` () =
    LayerRenderer.invalidateAll ()
    let heights =
        Array2D.init 8 8 (fun z x -> float32 (x - 4) * 2.5f - float32 z)
    let grid = makeGrid heights
    let bmp1 = LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme
    let bytes1 = bmp1.Bytes
    LayerRenderer.invalidateAll ()
    let bmp2 = LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme
    let bytes2 = bmp2.Bytes
    Assert.Equal<byte[]>(bytes1, bytes2)

[<Fact>]
let ``scales ramp to per-map min and max`` () =
    LayerRenderer.invalidateAll ()
    // All-land grid with tiny variation in [0.1, 0.2]
    let heights =
        Array2D.init 4 4 (fun z x ->
            0.1f + (float32 (z * 4 + x) / 15.0f) * 0.1f)
    let grid = makeGrid heights
    let bmp = LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme
    let minCell = pixelAt bmp 0 0
    let maxCell = pixelAt bmp 3 3
    let r1, g1, b1, _ = minCell
    let r2, g2, b2, _ = maxCell
    // min and max must differ (ramp scaled to actual range, not [-large, +large])
    Assert.True((r1, g1, b1) <> (r2, g2, b2), $"Min cell {minCell} should differ from max cell {maxCell}")
    // Max cell must be lighter than min cell (higher luminance on the brown ramp)
    Assert.True(luminance r2 g2 b2 > luminance r1 g1 b1)

[<Fact>]
let ``monotonic lightness with elevation on land`` () =
    LayerRenderer.invalidateAll ()
    // Strictly increasing x-gradient, all land
    let heights =
        Array2D.init 1 8 (fun _z x -> 5.0f + float32 x * 2.0f)
    let grid = makeGrid heights
    let bmp = LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme
    let mutable prev = -1.0
    for x in 0 .. 7 do
        let r, g, b, _ = pixelAt bmp x 0
        let lum = luminance r g b
        Assert.True(lum >= prev, $"Luminance must be non-decreasing along x, got {prev} -> {lum} at x={x}")
        prev <- lum

[<Fact>]
let ``handles empty grids gracefully`` () =
    LayerRenderer.invalidateAll ()
    let empty =
        { WidthElmos = 0
          HeightElmos = 0
          WidthHeightmap = 0
          HeightHeightmap = 0
          HeightMap = Array2D.init 0 0 (fun _ _ -> 0.0f)
          SlopeMap = Array2D.init 0 0 (fun _ _ -> 0.0f)
          ResourceMap = Array2D.init 0 0 (fun _ _ -> 0)
          LosMap = Array2D.init 0 0 (fun _ _ -> 0)
          RadarMap = Array2D.init 0 0 (fun _ _ -> 0) }
    let bmp = LayerRenderer.renderLayer empty LayerKind.BaseTerrain scheme
    Assert.NotNull(bmp)
    Assert.Equal(1, bmp.Width)
    Assert.Equal(1, bmp.Height)
