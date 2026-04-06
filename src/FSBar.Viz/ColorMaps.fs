namespace FSBar.Viz

open SkiaSharp

module ColorMaps =
    let private lerp (a: byte) (b: byte) (t: float32) =
        byte (float32 a + (float32 b - float32 a) * t)

    let private lerpColor (c1: SKColor) (c2: SKColor) (t: float32) =
        SKColor(lerp c1.Red c2.Red t, lerp c1.Green c2.Green t, lerp c1.Blue c2.Blue t)

    let grayscale =
        { Name = "Grayscale"
          MapValue = fun v ->
            let b = byte (v * 255.0f |> max 0.0f |> min 255.0f)
            SKColor(b, b, b) }

    let terrain =
        { Name = "Terrain"
          MapValue = fun v ->
            let v = v |> max 0.0f |> min 1.0f
            if v < 0.2f then
                lerpColor (SKColor(0uy, 0uy, 180uy)) (SKColor(0uy, 100uy, 200uy)) (v / 0.2f)
            elif v < 0.4f then
                lerpColor (SKColor(0uy, 100uy, 200uy)) (SKColor(34uy, 139uy, 34uy)) ((v - 0.2f) / 0.2f)
            elif v < 0.6f then
                lerpColor (SKColor(34uy, 139uy, 34uy)) (SKColor(139uy, 90uy, 43uy)) ((v - 0.4f) / 0.2f)
            elif v < 0.8f then
                lerpColor (SKColor(139uy, 90uy, 43uy)) (SKColor(180uy, 180uy, 180uy)) ((v - 0.6f) / 0.2f)
            else
                lerpColor (SKColor(180uy, 180uy, 180uy)) SKColors.White ((v - 0.8f) / 0.2f) }

    let heatMap =
        { Name = "HeatMap"
          MapValue = fun v ->
            let v = v |> max 0.0f |> min 1.0f
            if v < 0.5f then
                lerpColor (SKColor(0uy, 0uy, 255uy)) (SKColor(255uy, 255uy, 0uy)) (v / 0.5f)
            else
                lerpColor (SKColor(255uy, 255uy, 0uy)) (SKColor(255uy, 0uy, 0uy)) ((v - 0.5f) / 0.5f) }

    let binary =
        { Name = "Binary"
          MapValue = fun v ->
            if v > 0.5f then SKColor(0uy, 200uy, 0uy)
            else SKColor(200uy, 0uy, 0uy) }

    let colorSchemeFor (layer: LayerKind) =
        match layer with
        | LayerKind.HeightMap -> terrain
        | LayerKind.SlopeMap -> heatMap
        | LayerKind.ResourceMap -> heatMap
        | LayerKind.LosMap -> grayscale
        | LayerKind.RadarMap -> grayscale
        | LayerKind.TerrainClassification -> terrain
        | LayerKind.Passability _ -> binary
