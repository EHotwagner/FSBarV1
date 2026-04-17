namespace FSBar.Viz

open SkiaSharp

module ColorMaps =

    let clamp01 (v: float32) =
        if v < 0.0f then 0.0f
        elif v > 1.0f then 1.0f
        else v

    let lerp (a: byte) (b: byte) (t: float32) : byte =
        let result = float32 a + (float32 b - float32 a) * t
        byte (clamp01 (result / 255.0f) * 255.0f)

    let lerpColor (c1: SKColor) (c2: SKColor) (t: float32) : SKColor =
        SKColor(lerp c1.Red c2.Red t, lerp c1.Green c2.Green t, lerp c1.Blue c2.Blue t, 255uy)

    let fromStops (stops: (float32 * SKColor) list) (v: float32) : SKColor =
        let v = clamp01 v
        match stops with
        | [] -> SKColors.Black
        | [ (_, c) ] -> c
        | _ ->
            let rec find pairs =
                match pairs with
                | (t1, c1) :: (t2, c2) :: _ when v >= t1 && v <= t2 ->
                    let t = if t2 = t1 then 0.0f else (v - t1) / (t2 - t1)
                    lerpColor c1 c2 t
                | _ :: rest when rest.Length > 0 -> find rest
                | (_, c) :: _ -> c
                | [] -> SKColors.Black
            find stops

    let grayscale: ColorScheme =
        { Name = "Grayscale"
          MapValue = fromStops [ (0.0f, SKColors.Black); (1.0f, SKColors.White) ] }

    let terrain: ColorScheme =
        { Name = "Terrain"
          MapValue =
            fromStops
                [ (0.0f, SKColor(0uy, 0uy, 128uy))
                  (0.25f, SKColor(34uy, 139uy, 34uy))
                  (0.5f, SKColor(139uy, 90uy, 43uy))
                  (0.75f, SKColor(205uy, 170uy, 125uy))
                  (1.0f, SKColors.White) ] }

    let heatMap: ColorScheme =
        { Name = "HeatMap"
          MapValue =
            fromStops
                [ (0.0f, SKColors.Blue)
                  (0.35f, SKColors.Cyan)
                  (0.5f, SKColors.Green)
                  (0.65f, SKColors.Yellow)
                  (1.0f, SKColors.Red) ] }

    let binary: ColorScheme =
        { Name = "Binary"
          MapValue = fun v -> if v > 0.5f then SKColors.Green else SKColors.Red }

    let identity: ColorScheme =
        { Name = "Identity"
          MapValue = fun _ -> SKColors.Black }

    let colorSchemeFor (layer: LayerKind) : ColorScheme =
        match layer with
        | LayerKind.BaseTerrain -> identity
        | LayerKind.HeightMap -> terrain
        | LayerKind.TerrainClassification -> terrain
        | LayerKind.SlopeMap -> heatMap
        | LayerKind.ResourceMap -> heatMap
        | LayerKind.LosMap -> binary
        | LayerKind.RadarMap -> binary
        | LayerKind.Passability _ -> binary
