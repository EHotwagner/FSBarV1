namespace FSBar.Viz

open System.Collections.Concurrent
open System.Runtime.InteropServices
open SkiaSharp
open FSBar.Client

module LayerRenderer =
    let private cache = ConcurrentDictionary<string, SKBitmap>()
    let mutable private hits = 0
    let mutable private misses = 0

    let private cacheKey (layer: LayerKind) =
        match layer with
        | LayerKind.HeightMap -> "HeightMap"
        | LayerKind.SlopeMap -> "SlopeMap"
        | LayerKind.ResourceMap -> "ResourceMap"
        | LayerKind.LosMap -> "LosMap"
        | LayerKind.RadarMap -> "RadarMap"
        | LayerKind.TerrainClassification -> "TerrainClassification"
        | LayerKind.Passability mt ->
            match mt with
            | MoveType.Kbot -> "Passability_Kbot"
            | MoveType.Tank -> "Passability_Tank"
            | MoveType.Hover -> "Passability_Hover"
            | MoveType.Ship -> "Passability_Ship"

    let private renderHeightMap (grid: MapGrid) (scheme: ColorScheme) =
        let w = grid.WidthHeightmap
        let h = grid.HeightHeightmap
        let hm = grid.HeightMap
        // Find min/max for normalization
        let mutable minH = System.Single.MaxValue
        let mutable maxH = System.Single.MinValue
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = hm.[z, x]
                if v < minH then minH <- v
                if v > maxH then maxH <- v
        let range = maxH - minH
        let range = if range < 0.001f then 1.0f else range
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = (hm.[z, x] - minH) / range
                let c = scheme.MapValue v
                let idx = (z * w + x) * 4
                pixels.[idx] <- c.Red
                pixels.[idx + 1] <- c.Green
                pixels.[idx + 2] <- c.Blue
                pixels.[idx + 3] <- 255uy
        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        try
            bmp.InstallPixels(SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), handle.AddrOfPinnedObject(), w * 4) |> ignore
        finally
            handle.Free()
        bmp

    let private renderFloatArray (data: float32[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        let mutable minV = System.Single.MaxValue
        let mutable maxV = System.Single.MinValue
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = data.[z, x]
                if v < minV then minV <- v
                if v > maxV then maxV <- v
        let range = maxV - minV
        let range = if range < 0.001f then 1.0f else range
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = (data.[z, x] - minV) / range
                let c = scheme.MapValue v
                let idx = (z * w + x) * 4
                pixels.[idx] <- c.Red
                pixels.[idx + 1] <- c.Green
                pixels.[idx + 2] <- c.Blue
                pixels.[idx + 3] <- 255uy
        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        try
            bmp.InstallPixels(SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), handle.AddrOfPinnedObject(), w * 4) |> ignore
        finally
            handle.Free()
        bmp

    let private renderIntArray (data: int[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        let mutable maxV = 1
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = data.[z, x]
                if v > maxV then maxV <- v
        let maxF = float32 maxV
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = float32 data.[z, x] / maxF
                let c = scheme.MapValue v
                let idx = (z * w + x) * 4
                pixels.[idx] <- c.Red
                pixels.[idx + 1] <- c.Green
                pixels.[idx + 2] <- c.Blue
                pixels.[idx + 3] <- 255uy
        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        try
            bmp.InstallPixels(SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), handle.AddrOfPinnedObject(), w * 4) |> ignore
        finally
            handle.Free()
        bmp

    let private renderBoolArray (data: bool[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = if data.[z, x] then 1.0f else 0.0f
                let c = scheme.MapValue v
                let idx = (z * w + x) * 4
                pixels.[idx] <- c.Red
                pixels.[idx + 1] <- c.Green
                pixels.[idx + 2] <- c.Blue
                pixels.[idx + 3] <- 255uy
        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        try
            bmp.InstallPixels(SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), handle.AddrOfPinnedObject(), w * 4) |> ignore
        finally
            handle.Free()
        bmp

    let private renderTerrainClassification (grid: MapGrid) (scheme: ColorScheme) =
        let w = grid.WidthHeightmap
        let h = grid.HeightHeightmap
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let t = MapGrid.terrainAt grid x z
                let c =
                    match t with
                    | Terrain.Land _ -> SKColor(34uy, 139uy, 34uy)
                    | Terrain.Water _ -> SKColor(0uy, 80uy, 200uy)
                    | Terrain.Cliff _ -> SKColor(139uy, 90uy, 43uy)
                let idx = (z * w + x) * 4
                pixels.[idx] <- c.Red
                pixels.[idx + 1] <- c.Green
                pixels.[idx + 2] <- c.Blue
                pixels.[idx + 3] <- 255uy
        let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)
        try
            bmp.InstallPixels(SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul), handle.AddrOfPinnedObject(), w * 4) |> ignore
        finally
            handle.Free()
        bmp

    let private renderFresh (grid: MapGrid) (layer: LayerKind) (scheme: ColorScheme) =
        match layer with
        | LayerKind.HeightMap -> renderHeightMap grid scheme
        | LayerKind.SlopeMap -> renderFloatArray grid.SlopeMap scheme
        | LayerKind.ResourceMap -> renderIntArray grid.ResourceMap scheme
        | LayerKind.LosMap -> renderIntArray grid.LosMap scheme
        | LayerKind.RadarMap -> renderIntArray grid.RadarMap scheme
        | LayerKind.TerrainClassification -> renderTerrainClassification grid scheme
        | LayerKind.Passability mt -> renderBoolArray (MapGrid.passability grid mt) scheme

    let renderLayer (grid: MapGrid) (layer: LayerKind) (scheme: ColorScheme) =
        let key = cacheKey layer
        // LOS and Radar change every frame, don't cache
        match layer with
        | LayerKind.LosMap | LayerKind.RadarMap ->
            misses <- misses + 1
            renderFresh grid layer scheme
        | _ ->
            match cache.TryGetValue key with
            | true, bmp ->
                hits <- hits + 1
                bmp
            | _ ->
                misses <- misses + 1
                let bmp = renderFresh grid layer scheme
                cache.[key] <- bmp
                bmp

    let invalidateCache (layer: LayerKind) =
        let key = cacheKey layer
        match cache.TryRemove key with
        | true, bmp -> bmp.Dispose()
        | _ -> ()

    let invalidateAll () =
        for kvp in cache do
            kvp.Value.Dispose()
        cache.Clear()

    let cacheStats () = (hits, misses)
