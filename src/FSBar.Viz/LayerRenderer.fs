namespace FSBar.Viz

open SkiaSharp
open FSBar.Client
open System.Collections.Concurrent
open System.Runtime.InteropServices

module LayerRenderer =

    let private cache = ConcurrentDictionary<string, SKBitmap>()
    let mutable private hits = 0
    let mutable private misses = 0

    let private cacheKey (layer: LayerKind) =
        match layer with
        | LayerKind.HeightMap -> "height"
        | LayerKind.SlopeMap -> "slope"
        | LayerKind.ResourceMap -> "resource"
        | LayerKind.LosMap -> "los"
        | LayerKind.RadarMap -> "radar"
        | LayerKind.TerrainClassification -> "terrain"
        | LayerKind.Passability mt ->
            match mt with
            | MoveType.Kbot -> "pass-kbot"
            | MoveType.Tank -> "pass-tank"
            | MoveType.Hover -> "pass-hover"
            | MoveType.Ship -> "pass-ship"

    let private isDynamic (layer: LayerKind) =
        match layer with
        | LayerKind.LosMap | LayerKind.RadarMap -> true
        | _ -> false

    let private copyPixelsToBitmap (pixels: byte[]) (bmp: SKBitmap) =
        let ptr = bmp.GetPixels()
        if ptr <> 0n then
            Marshal.Copy(pixels, 0, ptr, pixels.Length)

    let private renderFloatArray (data: float32[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        if h = 0 || w = 0 then
            new SKBitmap(1, 1)
        else
        let mutable minV = System.Single.MaxValue
        let mutable maxV = System.Single.MinValue
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = data.[z, x]
                if v < minV then minV <- v
                if v > maxV then maxV <- v
        let range = maxV - minV
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let norm = if range > 0.0f then (data.[z, x] - minV) / range else 0.5f
                let c = scheme.MapValue norm
                let i = (z * w + x) * 4
                pixels.[i] <- c.Red
                pixels.[i + 1] <- c.Green
                pixels.[i + 2] <- c.Blue
                pixels.[i + 3] <- c.Alpha
        copyPixelsToBitmap pixels bmp
        bmp

    let private renderIntArray (data: int[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        if h = 0 || w = 0 then
            new SKBitmap(1, 1)
        else
        let mutable maxV = 0
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let v = data.[z, x]
                if v > maxV then maxV <- v
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let norm = if maxV > 0 then float32 data.[z, x] / float32 maxV else 0.0f
                let c = scheme.MapValue norm
                let i = (z * w + x) * 4
                pixels.[i] <- c.Red
                pixels.[i + 1] <- c.Green
                pixels.[i + 2] <- c.Blue
                pixels.[i + 3] <- c.Alpha
        copyPixelsToBitmap pixels bmp
        bmp

    let private renderBoolArray (data: bool[,]) (scheme: ColorScheme) =
        let h = Array2D.length1 data
        let w = Array2D.length2 data
        if h = 0 || w = 0 then
            new SKBitmap(1, 1)
        else
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let norm = if data.[z, x] then 1.0f else 0.0f
                let c = scheme.MapValue norm
                let i = (z * w + x) * 4
                pixels.[i] <- c.Red
                pixels.[i + 1] <- c.Green
                pixels.[i + 2] <- c.Blue
                pixels.[i + 3] <- c.Alpha
        copyPixelsToBitmap pixels bmp
        bmp

    let private renderTerrainClassification (grid: MapGrid) (scheme: ColorScheme) =
        let h = grid.HeightHeightmap
        let w = grid.WidthHeightmap
        if h = 0 || w = 0 then
            new SKBitmap(1, 1)
        else
        let bmp = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul)
        let pixels = Array.zeroCreate<byte> (w * h * 4)
        for z = 0 to h - 1 do
            for x = 0 to w - 1 do
                let terrain = MapGrid.terrainAt grid x z
                let norm =
                    match terrain with
                    | Terrain.Water _ -> 0.0f
                    | Terrain.Land hardness -> 0.25f + hardness * 0.5f
                    | Terrain.Cliff _ -> 0.9f
                let c = scheme.MapValue norm
                let i = (z * w + x) * 4
                pixels.[i] <- c.Red
                pixels.[i + 1] <- c.Green
                pixels.[i + 2] <- c.Blue
                pixels.[i + 3] <- c.Alpha
        copyPixelsToBitmap pixels bmp
        bmp

    let renderLayer (grid: MapGrid) (layer: LayerKind) (scheme: ColorScheme) =
        let key = cacheKey layer
        if not (isDynamic layer) then
            match cache.TryGetValue(key) with
            | true, bmp ->
                System.Threading.Interlocked.Increment(&hits) |> ignore
                bmp
            | _ ->
                System.Threading.Interlocked.Increment(&misses) |> ignore
                let bmp =
                    match layer with
                    | LayerKind.HeightMap -> renderFloatArray grid.HeightMap scheme
                    | LayerKind.SlopeMap -> renderFloatArray grid.SlopeMap scheme
                    | LayerKind.ResourceMap -> renderIntArray grid.ResourceMap scheme
                    | LayerKind.LosMap -> renderIntArray grid.LosMap scheme
                    | LayerKind.RadarMap -> renderIntArray grid.RadarMap scheme
                    | LayerKind.TerrainClassification -> renderTerrainClassification grid scheme
                    | LayerKind.Passability mt -> renderBoolArray (MapGrid.passability grid mt) scheme
                cache.[key] <- bmp
                bmp
        else
            System.Threading.Interlocked.Increment(&misses) |> ignore
            match layer with
            | LayerKind.LosMap -> renderIntArray grid.LosMap scheme
            | LayerKind.RadarMap -> renderIntArray grid.RadarMap scheme
            | _ -> renderFloatArray grid.HeightMap scheme

    let invalidateCache (layer: LayerKind) =
        let key = cacheKey layer
        match cache.TryRemove(key) with
        | true, bmp -> bmp.Dispose()
        | _ -> ()

    let invalidateAll () =
        for kvp in cache do
            kvp.Value.Dispose()
        cache.Clear()
        hits <- 0
        misses <- 0

    let cacheStats () = (hits, misses)
