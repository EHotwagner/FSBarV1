module FSBar.Viz.Tests.LayerRendererTests

open Xunit
open SkiaSharp
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

// NOTE: LayerRenderer uses a static cache with InstallPixels/GCHandle.
// Calling invalidateAll() disposes cached SKBitmaps. We must be careful not to
// use a disposed bitmap. Tests that render layers should call invalidateAll()
// BEFORE rendering (not after), and should not hold references to bitmaps across
// invalidateAll() calls.

[<Fact>]
let ``renderLayer with HeightMap produces non-null bitmap with correct dimensions`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let bmp = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
    Assert.NotNull(bmp)
    Assert.True(bmp.Width > 0, "Bitmap width should be > 0")
    Assert.True(bmp.Height > 0, "Bitmap height should be > 0")

[<Fact>]
let ``renderLayer with SlopeMap produces bitmap`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let bmp = LayerRenderer.renderLayer grid LayerKind.SlopeMap ColorMaps.heatMap
    Assert.NotNull(bmp)
    Assert.True(bmp.Width > 0)
    Assert.True(bmp.Height > 0)

[<Fact>]
let ``renderLayer with ResourceMap produces bitmap`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let bmp = LayerRenderer.renderLayer grid LayerKind.ResourceMap ColorMaps.heatMap
    Assert.NotNull(bmp)
    Assert.True(bmp.Width > 0)
    Assert.True(bmp.Height > 0)

[<Fact>]
let ``renderLayer with Passability produces bitmap`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let bmp = LayerRenderer.renderLayer grid (LayerKind.Passability MoveType.Kbot) ColorMaps.binary
    Assert.NotNull(bmp)
    Assert.True(bmp.Width > 0)
    Assert.True(bmp.Height > 0)

[<Fact>]
let ``different color schemes produce bitmaps`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let bmp1 = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
    Assert.NotNull(bmp1)
    // Invalidate and render with different scheme
    LayerRenderer.invalidateAll ()
    let bmp2 = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.grayscale
    Assert.NotNull(bmp2)
    // Both produce valid bitmaps
    Assert.True(bmp2.Width > 0)
    Assert.True(bmp2.Height > 0)

[<Fact>]
let ``cache hit and miss counting works`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let _bmp1 = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
    let (h1, m1) = LayerRenderer.cacheStats ()
    Assert.Equal(0, h1)
    Assert.Equal(1, m1)
    // Render same layer again - should be a cache hit
    let _bmp2 = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
    let (h2, m2) = LayerRenderer.cacheStats ()
    Assert.Equal(1, h2)
    Assert.Equal(1, m2)

[<Fact>]
let ``invalidateAll clears cache and resets counters`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let _bmp = LayerRenderer.renderLayer grid LayerKind.HeightMap ColorMaps.terrain
    LayerRenderer.invalidateAll ()
    let (h, m) = LayerRenderer.cacheStats ()
    Assert.Equal(0, h)
    Assert.Equal(0, m)

[<Fact>]
let ``LosMap is always a cache miss (dynamic layer)`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let _bmp1 = LayerRenderer.renderLayer grid LayerKind.LosMap ColorMaps.binary
    let _bmp2 = LayerRenderer.renderLayer grid LayerKind.LosMap ColorMaps.binary
    let (h, m) = LayerRenderer.cacheStats ()
    // LosMap is dynamic, so both renders should be misses
    Assert.Equal(0, h)
    Assert.Equal(2, m)
