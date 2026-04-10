// Example 02: Render each LayerKind with custom ColorSchemes
#load "../prelude.fsx"
open Prelude
open FSBar.Viz
open FSBar.Client
open SkiaSharp

// Create a test grid with some height variation
let w, h = 64, 64
let grid: MapGrid =
    { WidthElmos = w * 8; HeightElmos = h * 8
      WidthHeightmap = w; HeightHeightmap = h
      HeightMap = Array2D.init (h + 1) (w + 1) (fun z x -> float32 x * 2.0f + float32 z * 1.5f)
      SlopeMap = Array2D.init (h / 2) (w / 2) (fun z x -> float32 x / float32 (w / 2))
      ResourceMap = Array2D.init h w (fun z x -> if (x + z) % 10 = 0 then 100 else 0)
      LosMap = Array2D.init h w (fun _ x -> if x < w / 2 then 1 else 0)
      RadarMap = Array2D.init h w (fun z _ -> if z < h / 2 then 1 else 0) }

// Render each layer kind
let layers = [
    LayerKind.HeightMap; LayerKind.SlopeMap; LayerKind.ResourceMap
    LayerKind.LosMap; LayerKind.RadarMap; LayerKind.TerrainClassification
    LayerKind.Passability MoveType.Kbot
]

for layer in layers do
    let scheme = ColorMaps.colorSchemeFor layer
    let bmp = LayerRenderer.renderLayer grid layer scheme
    printfn "%A: %dx%d bitmap" layer bmp.Width bmp.Height

LayerRenderer.invalidateAll ()
printfn "All layers rendered and cache cleared."
