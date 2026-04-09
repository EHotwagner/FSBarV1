// 05-map-layers.fsx — Map data exploration: load grid layers, query terrain, check passability
// Usage: dotnet fsi scripts/examples/05-map-layers.fsx

#load "../prelude.fsx"

printfn "Starting headless BAR session..."
use client = BarClient.startHeadless()

// Warm up a few frames so the engine is ready
for _ in client.Frames |> Seq.truncate 5 do ()

let stream = client.Stream

// Load all map layers
printfn "\nLoading map grid..."
let map = MapGrid.loadFromEngine stream
printfn "  %O" map

// Point queries at start position
let (sx, _, sz) = Callbacks.getStartPos stream 0
printfn "\nStart position: (%.0f, %.0f)" sx sz

match MapQuery.heightAtElmo map (int sx) (int sz) with
| Result.Ok h -> printfn "  Height: %.1f" h
| Result.Error e -> printfn "  Height error: %s" e

match MapQuery.slopeAtElmo map (int sx) (int sz) with
| Result.Ok s -> printfn "  Slope: %.3f" s
| Result.Error e -> printfn "  Slope error: %s" e

match MapQuery.terrainAtElmo map (int sx) (int sz) with
| Result.Ok t -> printfn "  Terrain: %A" t
| Result.Error e -> printfn "  Terrain error: %s" e

// Passability check
printfn "\nPassability at start position:"
for mt in [ MoveType.Kbot; MoveType.Tank; MoveType.Hover; MoveType.Ship ] do
    let pass = MapGrid.passability map mt
    let gx, gz = MapQuery.elmoToGrid (int sx) (int sz)
    printfn "  %A: %b" mt pass.[gx, gz]

// Sub-region extraction
printfn "\nExtracting 1024x1024 elmo region from origin..."
match MapQuery.heightSubRegion map 0 0 1024 1024 with
| Result.Ok region ->
    printfn "  Region size: %dx%d cells" (Array2D.length1 region) (Array2D.length2 region)
    printfn "  Height at (0,0): %.1f" region.[0, 0]
| Result.Error e -> printfn "  Error: %s" e

// Resource hotspots
printfn "\nResource hotspots (top 5):"
let hotspots = MapQuery.resourceHotspots map 0 0 map.WidthElmos map.HeightElmos 0
for (x, z, v) in hotspots |> List.truncate 5 do
    let ex, ez = MapQuery.gridToElmo x z
    printfn "  (%d, %d) elmos — value: %d" ex ez v

// Refresh dynamic layer
printfn "\nRefreshing LOS map..."
let updated = MapGrid.refreshLos stream map
printfn "  LOS dimensions: %dx%d" (Array2D.length1 updated.LosMap) (Array2D.length2 updated.LosMap)

printfn "\nDone."
client.Stop()
