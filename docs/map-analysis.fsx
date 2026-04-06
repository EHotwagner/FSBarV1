(**
---
title: Map Analysis
category: Tutorials
categoryindex: 2
index: 5
---
*)

(**
# Map Analysis

FSBarV1 provides three modules for working with map data: `MapGrid` for loading and classifying
terrain layers, `MapQuery` for point queries and region analysis, and `MapCache` for session-level
caching.

## MapGrid

The `MapGrid` record bundles all map data layers loaded from the engine into `Array2D` grids.

### MapGrid Record
*)

(*** do-not-eval ***)
open FSBar.Client

type MapGrid =
    { WidthElmos: int           // Map width in elmo units
      HeightElmos: int          // Map height in elmo units
      WidthHeightmap: int       // Map width in heightmap squares
      HeightHeightmap: int      // Map height in heightmap squares
      HeightMap: float32[,]     // Height at each vertex (W+1 x H+1)
      SlopeMap: float32[,]      // Slope at each cell
      ResourceMap: int[,]       // Resource distribution
      LosMap: int[,]            // Line of sight coverage
      RadarMap: int[,] }        // Radar coverage

(**
### Loading from the Engine

`MapGrid.loadFromEngine` calls all map data callbacks and assembles the grid:
*)

(*** do-not-eval ***)
let stream = client.Stream
let grid = MapGrid.loadFromEngine stream
printfn "%s" (grid.ToString())
// e.g., "MapGrid 8192x8192 elmos, 1025x1025 heightmap"

(**
### Refreshing Dynamic Layers

LOS and radar change as units move. Refresh them without reloading the full grid:
*)

(*** do-not-eval ***)
let updatedGrid = MapGrid.refreshLos stream grid
let updatedGrid2 = MapGrid.refreshRadar stream updatedGrid

(**
### Terrain Classification

The `Terrain` discriminated union classifies each heightmap cell:

- `Land of hardness: float32` -- above water, slope < 0.5
- `Water of depth: float32` -- height below 0 (depth = abs height)
- `Cliff of slope: float32` -- slope >= 0.5
*)

(*** do-not-eval ***)
let terrain = MapGrid.terrainAt grid 100 100
match terrain with
| Terrain.Land hardness -> printfn "Land (hardness %.2f)" hardness
| Terrain.Water depth -> printfn "Water (depth %.2f)" depth
| Terrain.Cliff slope -> printfn "Cliff (slope %.2f)" slope

(**
### Active Patterns

F# active patterns provide ergonomic pattern matching on terrain:
*)

(*** do-not-eval ***)
let describeTerrain t =
    match t with
    | MapGrid.Land h -> sprintf "Buildable land (%.1f)" h
    | MapGrid.Water d -> sprintf "Water (%.1f deep)" d
    | MapGrid.Cliff s -> sprintf "Cliff (slope %.2f)" s

(**
### Passability

Compute whether each cell is passable for a given movement type:
*)

(*** do-not-eval ***)
let passGrid = MapGrid.passability grid MoveType.Kbot
// passGrid.[x, z] = true means Kbot units can traverse that cell

// Four movement types available:
// MoveType.Kbot  -- bipedal robots
// MoveType.Tank  -- wheeled/tracked vehicles
// MoveType.Hover -- hovercraft (can cross water)
// MoveType.Ship  -- ships (only in water)

(**
The `Passable|Impassable` active pattern checks a specific cell:
*)

(*** do-not-eval ***)
// Note: this active pattern takes grid and moveType as parameters
// (|Passable|Impassable|) : grid -> moveType -> x -> z -> Choice<unit, unit>

(**
## MapQuery

The `MapQuery` module provides point queries and region operations on a loaded `MapGrid`.
All functions use elmo coordinates (the game's world coordinate system) and convert internally.

### Point Queries

Query height and slope at specific elmo coordinates:
*)

(*** do-not-eval ***)
match MapQuery.heightAtElmo grid 2048 2048 with
| Ok height -> printfn "Height at (2048, 2048): %.1f" height
| Error msg -> printfn "Error: %s" msg

match MapQuery.slopeAtElmo grid 2048 2048 with
| Ok slope -> printfn "Slope at (2048, 2048): %.3f" slope
| Error msg -> printfn "Error: %s" msg

match MapQuery.terrainAtElmo grid 2048 2048 with
| Ok terrain -> printfn "Terrain: %A" terrain
| Error msg -> printfn "Error: %s" msg

(**
Out-of-bounds coordinates return `Error` with a descriptive message.

### Sub-Region Extraction

Extract a rectangular portion of the heightmap by elmo bounds:
*)

(*** do-not-eval ***)
// Extract heightmap for a 1024x1024 elmo region
match MapQuery.heightSubRegion grid 0 0 1024 1024 with
| Ok region ->
    printfn "Sub-region: %d x %d cells"
        (Array2D.length1 region) (Array2D.length2 region)
    // region is a float32[,] of the requested area
| Error msg ->
    printfn "Error: %s" msg

(**
### Resource Hotspots

Find cells where resource values exceed a threshold, sorted by value descending:
*)

(*** do-not-eval ***)
// Find resource-rich cells across the entire map with threshold 0
let hotspots =
    MapQuery.resourceHotspots grid 0 0 grid.WidthElmos grid.HeightElmos 0

for (gx, gz, value) in hotspots |> List.truncate 10 do
    let (ex, ez) = MapQuery.gridToElmo gx gz
    printfn "Resource at elmo (%d, %d): value=%d" ex ez value

(**
### Coordinate Conversion

Convert between elmo coordinates and heightmap grid indices:
*)

(*** do-not-eval ***)
// Elmo to grid (divides by 8)
let (gx, gz) = MapQuery.elmoToGrid 2048 4096
printfn "Elmo (2048, 4096) -> Grid (%d, %d)" gx gz

// Grid to elmo (multiplies by 8)
let (ex, ez) = MapQuery.gridToElmo gx gz
printfn "Grid (%d, %d) -> Elmo (%d, %d)" gx gz ex ez

(**
## MapCache

The `MapCache` module provides session-level caching so you only load expensive map data once.

### Cached Grid Loading

`MapCache.fromEngine` loads the grid on first call and returns the cached version thereafter:
*)

(*** do-not-eval ***)
let grid1 = MapCache.fromEngine stream   // Loads from engine (slow)
let grid2 = MapCache.fromEngine stream   // Returns cached grid (instant)

(**
### Cached Passability

`MapCache.passability` computes passability grids once per movement type:
*)

(*** do-not-eval ***)
let kbotPass = MapCache.passability grid MoveType.Kbot   // Computed (slow first time)
let kbotPass2 = MapCache.passability grid MoveType.Kbot  // Cached (instant)

(**
### Cache Clearing

Clear all cached data when starting a new game session:
*)

(*** do-not-eval ***)
MapCache.clear ()

(**
## Complete Example

Loading map data and analyzing the terrain around a start position:
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()
for _ in 1..30 do client.Step() |> ignore

let stream = client.Stream
let grid = MapCache.fromEngine stream

// Find our start position
let (sx, _, sz) = Callbacks.getStartPos stream 0
printfn "Start position: (%.0f, %.0f)" sx sz

// Analyze terrain around start
let radius = 512
let x1, z1 = int sx - radius, int sz - radius
let x2, z2 = int sx + radius, int sz + radius

match MapQuery.heightSubRegion grid x1 z1 x2 z2 with
| Ok region ->
    let mutable landCount = 0
    let mutable waterCount = 0
    for x in 0 .. Array2D.length1 region - 1 do
        for z in 0 .. Array2D.length2 region - 1 do
            if region.[x, z] >= 0.0f then landCount <- landCount + 1
            else waterCount <- waterCount + 1
    printfn "Around start: %d land cells, %d water cells" landCount waterCount
| Error e -> printfn "Error: %s" e

// Find nearby metal spots
let metalSpots = Callbacks.getMetalSpots stream
let nearbyMetal =
    metalSpots
    |> Array.filter (fun (mx, _, mz, _) ->
        abs (mx - sx) < 1024.0f && abs (mz - sz) < 1024.0f)
printfn "Metal spots within 1024 elmos: %d" nearbyMetal.Length

client.Stop()
