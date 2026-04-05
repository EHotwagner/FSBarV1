# Quickstart: Array2D Map Data Layers

**Feature**: 004-array-map-layers  
**Date**: 2026-04-05

## Prerequisites

- BAR engine running with HighBar V2 proxy
- FSBar.Client built (`dotnet build src/FSBar.Client`)
- F# Interactive (FSI) available

## Load in REPL

```fsharp
#load "scripts/prelude.fsx"

open FSBar.Client
open FSBar.Client.MapGrid
open FSBar.Client.MapQuery
open FSBar.Client.MapCache
```

## Load Map Data

```fsharp
// Connect to engine
let client = BarClient.startHeadless()
let stream = client.Stream

// Load all map layers (cached after first call)
let map = MapCache.fromEngine stream
// → MapGrid { 16384x16384 elmos, 2049x2049 heightmap, 5 layers loaded }
```

## Point Queries

```fsharp
// Height at an elmo coordinate
MapQuery.heightAtElmo map 1024 2048
// → Ok 142.5f

// Terrain classification
MapQuery.terrainAtElmo map 1024 2048
// → Ok (Terrain.Land 200.0f)

// Slope at coordinate
MapQuery.slopeAtElmo map 1024 2048
// → Ok 0.15f
```

## Region Extraction

```fsharp
// Extract a sub-region of the heightmap
let region = MapQuery.heightSubRegion map 0 0 4096 4096
// → Ok float32[,] (512x512)

// Find resource hotspots in a region
let hotspots = MapQuery.resourceHotspots map 0 0 8192 8192 50
// → [(128, 256, 200); (384, 512, 180); ...]
```

## Terrain Classification

```fsharp
// Classify a specific cell
MapGrid.terrainAt map 128 256
// → Terrain.Land 200.0f

// Pattern matching in AI logic
match MapGrid.terrainAt map x z with
| Terrain.Land _ -> "can build here"
| Terrain.Water depth -> sprintf "water, depth %.1f" depth
| Terrain.Cliff slope -> sprintf "cliff, slope %.1f" slope
```

## Passability

```fsharp
// Get passability grid for kbots (cached after first call)
let kbotPass = MapCache.passability map MoveType.Kbot
// → bool[,] (2049x2049)

// Check a specific cell
kbotPass.[128, 256]  // → true

// Compare movement types
let tankPass = MapCache.passability map MoveType.Tank
let hoverPass = MapCache.passability map MoveType.Hover
let shipPass = MapCache.passability map MoveType.Ship
```

## Dynamic Layer Refresh

```fsharp
// Refresh LOS after game state changes
let updatedMap = MapGrid.refreshLos stream map
updatedMap.LosMap.[128, 256]  // → updated visibility value

// Refresh radar
let updatedMap2 = MapGrid.refreshRadar stream updatedMap
```

## Coordinate Conversion

```fsharp
// Elmo → grid index
MapQuery.elmoToGrid 1024 2048  // → (128, 256)

// Grid index → elmo
MapQuery.gridToElmo 128 256    // → (1024, 2048)
```

## Build & Test

```bash
# Build
dotnet build src/FSBar.Client

# Run live integration tests (requires running engine)
dotnet test tests/FSBar.LiveTests --filter "MapGrid"
```
