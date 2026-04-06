// 06-game-viz-basic.fsx — Start a game with live visualization
//
// Usage:
//   dotnet build src/FSBar.Viz/FSBar.Viz.fsproj
//   dotnet fsi scripts/examples/06-game-viz-basic.fsx

#load "../prelude.fsx"

let client = BarClient.startHeadless ()

// Open the visualization window
GameViz.start None
GameViz.attachToClient client

// Enable unit and event overlays
GameViz.enableOverlay OverlayKind.Units
GameViz.enableOverlay OverlayKind.Events

// Run game for 300 frames, feeding each to the viz
printfn "Running 300 frames with visualization..."

for _ in 1..300 do
    let frame = client.Step()
    GameViz.onFrame frame

printfn "Done. Viz window remains open — press Ctrl+C to exit."
printfn "Close the viz with: GameViz.stop()"

// Keep the script alive so the window stays open
System.Threading.Thread.Sleep(10000)

GameViz.stop ()
client.Stop()
