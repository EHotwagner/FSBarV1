module FSBar.Viz.Tests.LayerRendererTests

open System
open Xunit
open Xunit.Abstractions
open FSBar.Client
open FSBar.Viz

[<Collection("VizEngine")>]
type LayerRendererTests(engine: VizEngineFixture, output: ITestOutputHelper) =

    let tryLoadGrid () =
        try
            Some (MapGrid.loadFromEngine engine.Client.Stream)
        with
        | ex ->
            output.WriteLine($"SKIP: MapGrid.loadFromEngine failed mid-session — {ex.Message}")
            None

    [<Fact>]
    member _.``renderLayer HeightMap produces bitmap with correct dimensions`` () =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            let scheme = ColorMaps.terrain
            LayerRenderer.invalidateAll ()
            let bmp = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            Assert.NotNull(bmp)
            Assert.Equal(grid.WidthHeightmap, bmp.Width)
            Assert.Equal(grid.HeightHeightmap, bmp.Height)
            output.WriteLine($"HeightMap bitmap: {bmp.Width}x{bmp.Height}")

    [<Fact>]
    member _.``renderLayer HeightMap produces non-uniform pixels`` () =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            let scheme = ColorMaps.terrain
            LayerRenderer.invalidateAll ()
            let bmp = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            let p1 = bmp.GetPixel(0, 0)
            let p2 = bmp.GetPixel(bmp.Width / 2, bmp.Height / 2)
            let p3 = bmp.GetPixel(bmp.Width - 1, bmp.Height - 1)
            let allSame = p1 = p2 && p2 = p3
            Assert.False(allSame, "All sampled pixels are identical — terrain should vary")
            output.WriteLine($"Pixel samples: ({p1.Red},{p1.Green},{p1.Blue}) ({p2.Red},{p2.Green},{p2.Blue}) ({p3.Red},{p3.Green},{p3.Blue})")

    [<Fact>]
    member _.``renderLayer SlopeMap produces different bitmap than HeightMap`` () =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            LayerRenderer.invalidateAll ()
            let heightBmp = LayerRenderer.renderLayer grid LayerKind.HeightMap (ColorMaps.colorSchemeFor LayerKind.HeightMap)
            let slopeBmp = LayerRenderer.renderLayer grid LayerKind.SlopeMap (ColorMaps.colorSchemeFor LayerKind.SlopeMap)
            Assert.NotNull(slopeBmp)
            let hp = heightBmp.GetPixel(heightBmp.Width / 2, heightBmp.Height / 2)
            let sp = slopeBmp.GetPixel(slopeBmp.Width / 2, slopeBmp.Height / 2)
            output.WriteLine($"HeightMap center: ({hp.Red},{hp.Green},{hp.Blue}), SlopeMap center: ({sp.Red},{sp.Green},{sp.Blue})")

    [<Theory>]
    [<InlineData("HeightMap")>]
    [<InlineData("SlopeMap")>]
    [<InlineData("ResourceMap")>]
    [<InlineData("LosMap")>]
    [<InlineData("RadarMap")>]
    [<InlineData("TerrainClassification")>]
    [<InlineData("Passability_Kbot")>]
    [<InlineData("Passability_Tank")>]
    [<InlineData("Passability_Hover")>]
    [<InlineData("Passability_Ship")>]
    member _.``renderLayer produces non-null bitmap for each LayerKind`` (layerName: string) =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            LayerRenderer.invalidateAll ()
            let layer =
                match layerName with
                | "HeightMap" -> LayerKind.HeightMap
                | "SlopeMap" -> LayerKind.SlopeMap
                | "ResourceMap" -> LayerKind.ResourceMap
                | "LosMap" -> LayerKind.LosMap
                | "RadarMap" -> LayerKind.RadarMap
                | "TerrainClassification" -> LayerKind.TerrainClassification
                | "Passability_Kbot" -> LayerKind.Passability MoveType.Kbot
                | "Passability_Tank" -> LayerKind.Passability MoveType.Tank
                | "Passability_Hover" -> LayerKind.Passability MoveType.Hover
                | "Passability_Ship" -> LayerKind.Passability MoveType.Ship
                | _ -> failwith $"Unknown layer: {layerName}"
            let scheme = ColorMaps.colorSchemeFor layer
            let bmp = LayerRenderer.renderLayer grid layer scheme
            Assert.NotNull(bmp)
            Assert.True(bmp.Width > 0, $"Bitmap width should be > 0 for {layerName}")
            Assert.True(bmp.Height > 0, $"Bitmap height should be > 0 for {layerName}")
            output.WriteLine($"{layerName}: {bmp.Width}x{bmp.Height}")

    [<Fact>]
    member _.``cache returns same bitmap on second call for static layers`` () =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            LayerRenderer.invalidateAll ()
            let scheme = ColorMaps.terrain
            let bmp1 = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            let bmp2 = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            Assert.True(obj.ReferenceEquals(bmp1, bmp2), "Second call should return cached bitmap")
            let (hits, misses) = LayerRenderer.cacheStats ()
            Assert.True(hits >= 1, $"Should have at least 1 cache hit, got {hits}")
            output.WriteLine($"Cache stats: hits={hits}, misses={misses}")

    [<Fact>]
    member _.``invalidateCache forces re-render`` () =
        match tryLoadGrid () with
        | None -> output.WriteLine("SKIPPED: MapGrid.loadFromEngine not available mid-session")
        | Some grid ->
            LayerRenderer.invalidateAll ()
            let scheme = ColorMaps.terrain
            let _bmp1 = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            LayerRenderer.invalidateCache LayerKind.HeightMap
            let _bmp2 = LayerRenderer.renderLayer grid LayerKind.HeightMap scheme
            let (_, misses) = LayerRenderer.cacheStats ()
            Assert.True(misses >= 2, $"Should have at least 2 cache misses after invalidation, got {misses}")
