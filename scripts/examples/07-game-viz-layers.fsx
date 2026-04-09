// 07-game-viz-layers.fsx — Demonstrate layer switching and customization
//
// Usage:
//   dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
//   dotnet fsi scripts/examples/07-game-viz-layers.fsx

#load "../prelude.fsx"
open SkiaSharp

let client = BarClient.startHeadless ()

GameViz.start None
GameViz.attachToClient client

// Step a few frames to get initial data
client.WaitFrames 30 (fun frame -> GameViz.onFrame frame)

printfn "=== Layer Switching Demo ==="
printfn "Switching through base layers..."

let layers =
    [ LayerKind.HeightMap, "HeightMap"
      LayerKind.SlopeMap, "SlopeMap"
      LayerKind.ResourceMap, "ResourceMap"
      LayerKind.LosMap, "LosMap"
      LayerKind.RadarMap, "RadarMap"
      LayerKind.TerrainClassification, "TerrainClassification"
      LayerKind.Passability FSBar.Client.MoveType.Tank, "Passability (Tank)" ]

for (layer, name) in layers do
    printfn "  -> %s" name
    GameViz.setBaseLayer layer
    System.Threading.Thread.Sleep(2000)

    // Keep feeding frames
    client.WaitFrames 10 (fun frame -> GameViz.onFrame frame)

printfn ""
printfn "=== Overlay Demo ==="
GameViz.setBaseLayer LayerKind.HeightMap
GameViz.enableOverlay OverlayKind.Units
GameViz.enableOverlay OverlayKind.Grid
GameViz.enableOverlay OverlayKind.MetalSpots
GameViz.enableOverlay OverlayKind.EconomyHud
printfn "Enabled: Units, Grid, MetalSpots, EconomyHud"
System.Threading.Thread.Sleep(3000)

printfn ""
printfn "=== Customization Demo ==="

// Custom color scheme: green-to-purple for height
let customScheme: ColorScheme =
    { Name = "GreenPurple"
      MapValue = fun v ->
        let r = byte (v * 180.0f)
        let g = byte ((1.0f - v) * 200.0f)
        let b = byte (v * 255.0f)
        SKColor(r, g, b) }

GameViz.setColorScheme LayerKind.HeightMap customScheme
printfn "Applied custom GreenPurple color scheme to HeightMap"
System.Threading.Thread.Sleep(2000)

GameViz.setMarkerSize 8.0f
printfn "Set marker size to 8"

GameViz.setOverlayOpacity 0.5f
printfn "Set overlay opacity to 0.5"

GameViz.toggleGridLines ()
printfn "Toggled grid lines on"
System.Threading.Thread.Sleep(3000)

printfn ""
printfn "=== Pan/Zoom Demo ==="
GameViz.zoom 2.0f 512.0f 320.0f
printfn "Zoomed in 2x"
System.Threading.Thread.Sleep(2000)

GameViz.pan 100.0f 50.0f
printfn "Panned right and down"
System.Threading.Thread.Sleep(2000)

GameViz.resetView ()
printfn "Reset to auto-fit"
System.Threading.Thread.Sleep(2000)

printfn ""
printfn "Demo complete. Closing..."
GameViz.stop ()
client.Stop()
