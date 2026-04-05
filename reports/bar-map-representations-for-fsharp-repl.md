# BAR Map Representations for F# Scripting/REPL

**Date:** 2026-04-05
**Scope:** Evaluate convenient representations of Beyond All Reason maps for use from an F# scripting/REPL environment, including type providers, graph-based pathfinding, and alternative approaches.

---

## Table of Contents

1. [Current State in FSBarV1](#1-current-state-in-fsbarv1)
2. [BAR Map Data: What's Available](#2-bar-map-data-whats-available)
3. [Approach 1: F# Type Provider](#3-approach-1-f-type-provider)
4. [Approach 2: Graph for Pathfinding](#4-approach-2-graph-for-pathfinding)
5. [Approach 3: Array2D Grid Layers](#5-approach-3-array2d-grid-layers)
6. [Approach 4: DUs + Records + Active Patterns](#6-approach-4-dus--records--active-patterns)
7. [Approach 5: Computation Expression DSL](#7-approach-5-computation-expression-dsl)
8. [Approach 6: Memoized Lazy Modules](#8-approach-6-memoized-lazy-modules)
9. [Terrain Analysis Patterns (Influence Maps, Regions, Chokes)](#9-terrain-analysis-patterns)
10. [Comparison Matrix](#10-comparison-matrix)
11. [Recommended Architecture](#11-recommended-architecture)
12. [References](#12-references)

---

## 1. Current State in FSBarV1

### Implemented Map Callbacks

The `Callbacks` module currently wraps four map queries via the engine's gRPC bridge:

| Function | Returns | Notes |
|----------|---------|-------|
| `getMapWidth()` | `int` | Width in heightmap squares |
| `getMapHeight()` | `int` | Height in heightmap squares |
| `getStartPos(teamId)` | `(x, y, z)` | Team start position as Vector3 |
| `getMetalSpots()` | `(x, y, z, value)[]` | All metal extraction points |

### Defined but NOT Yet Wrapped (in callbacks.proto)

| Callback ID | Name | Data Type |
|-------------|------|-----------|
| 52 | `CALLBACK_MAP_GET_HEIGHT_MAP` | `FloatArray` — full heightmap grid |
| 53 | `CALLBACK_MAP_GET_SLOPE_MAP` | `FloatArray` — slope at each cell |
| 54 | `CALLBACK_MAP_GET_LOS_MAP` | `IntArray` — line-of-sight visibility |
| 55 | `CALLBACK_MAP_GET_RADAR_MAP` | `IntArray` — radar coverage |
| 56 | `CALLBACK_MAP_GET_RESOURCE_MAP` | `FloatArray` — metal/energy distribution |

### No Map Data Structures Exist

The library is purely a command/event interface. There are no structured map types, no terrain data models, no visualization, and no SMF file parsing. Everything below is net-new.

---

## 2. BAR Map Data: What's Available

### SMF Binary Format (Offline/Pre-game)

BAR maps ship as `.sd7` archives containing compiled `.smf` binary files. The SMF format is well-documented:

| Layer | Resolution | Type | Description |
|-------|-----------|------|-------------|
| Heightmap | `(W/8+1) × (H/8+1)` | `uint16[]` | Terrain elevation |
| Typemap | `(W/16) × (H/16)` | `byte[]` | Terrain type indices (maps to move speed multipliers) |
| Metalmap | `(W/16) × (H/16)` | `byte[]` | Metal density 0–255 |
| Minimap | 1024×1024 | DXT1 compressed | Preview image |
| Features | variable | struct entries | Trees, rocks, geo vents with position/rotation |
| Grass map | `(W/32) × (H/32)` | `byte[]` | Optional vegetation density |

Coordinate system: 1 elmo is the base unit. Heightmap = 8 elmos/pixel, metalmap/typemap = 16 elmos/pixel, 1 SMU = 512 elmos.

### Engine Runtime API (In-game via Callbacks)

The Recoil engine exposes per-point queries through Lua (which FSBarV1 bridges via protobuf):

- `GetGroundHeight(x, z)` — current height (post-terraforming)
- `GetGroundNormal(x, z)` — surface normal + slope
- `GetGroundInfo(x, z)` — terrain type, metal, move speed multipliers
- `GetMetalAmount(x, z)` — metal at metalmap coordinates
- `TestBuildOrder(defId, x, y, z, facing)` — build site validity
- `TestMoveOrder(defId, x, y, z)` — movement validity
- `RequestPath(moveType, start, end)` — engine pathfinding

### Existing Parsers (No .NET)

- **PyMapConv** (Python): SMF compiler/decompiler — the primary community tool
- **SpringMapEdit** (Java): Standalone map editor
- **BAR Map Generator** (Python): Procedural generation pipeline
- **Engine source** (C++): Authoritative parser in `rts/Map/SMF/SmfMapFile.cpp`
- **No F#, C#, or .NET SMF parser exists** — would need to be built from scratch

---

---


### Engine vs. Client Pathfinding

Important distinction: the Recoil engine already has a sophisticated **QTPFS (Quad-Tree Path Finder System)** that handles:
- Per-MoveType path layers (tanks vs. kbots vs. hovers vs. ships)
- Dynamic terrain changes
- Multi-threaded path resolution

The engine exposes `Spring.RequestPath()` which returns waypoints. For an AI client, you often want **strategic pathfinding** (coarse-grained, "which route avoids enemy territory") rather than **tactical pathfinding** (fine-grained, "navigate around this building"), which the engine handles. A client-side graph is most useful for:

- Pre-game route planning (before connecting to engine)
- Strategic overlays (threat-aware routing, shortest safe path to expansion)
- Region/choke analysis (see Section 9)
- "What-if" queries (can I reach X if the enemy controls Y?)

### Verdict: RECOMMENDED (Custom Grid A* + Region Graph)

Use a custom A* on `Array2D` for fine-grained pathfinding. Build a coarse **region graph** (see Section 9) for strategic queries. QuikGraph is a good fallback if you need algorithms beyond A*/Dijkstra. Avoid FSharp.FGL for this use case — it lacks the necessary algorithms and won't scale.

---

## 5. Approach 3: Array2D Grid Layers

### Concept

Represent each map data layer as a native F# `Array2D`, the most direct mapping from the SMF binary format:

```fsharp
type MapGrid = {
    Width: int
    Height: int
    HeightMap: float32[,]    // (W/8+1) × (H/8+1)
    SlopeMap: float32[,]     // derived from heightmap
    MetalMap: byte[,]        // (W/16) × (H/16)
    TypeMap: byte[,]         // (W/16) × (H/16)
    Passability: bool[,]     // derived: per move-type
}
```

### F# Array2D Operations

```fsharp
// Creation
let heightMap = Array2D.init 1025 1025 (fun x y -> readHeight x y)

// Access
let h = heightMap.[x, y]

// Transformation
let slopes = Array2D.init w h (fun x y ->
    let dx = heightMap.[x+1,y] - heightMap.[x-1,y]
    let dz = heightMap.[x,y+1] - heightMap.[x,y-1]
    sqrt(dx*dx + dz*dz))

// Sub-region extraction
let region = Array2D.sub heightMap startX startY lenX lenY

// Iteration
Array2D.iteri (fun x y h -> if h > threshold then ...) heightMap
```

### Loading from Engine (Runtime)

Wrapping the existing but unexposed protobuf callbacks:

```fsharp
// These would need to be added to Callbacks module
let heightMap = Callbacks.getHeightMap client  // FloatArray → float32[,]
let slopeMap = Callbacks.getSlopeMap client
let losMap = Callbacks.getLosMap client         // IntArray → int[,]
let radarMap = Callbacks.getRadarMap client
let resourceMap = Callbacks.getResourceMap client
```

### Loading from SMF File (Offline)

No .NET parser exists, but the format is straightforward with `BinaryReader`:

```fsharp
let parseSmf (path: string) =
    use reader = new BinaryReader(File.OpenRead(path))
    let magic = reader.ReadBytes(16)  // "spring map file\0"
    let version = reader.ReadInt32()
    let mapId = reader.ReadUInt32()
    let width = reader.ReadInt32()
    let height = reader.ReadInt32()
    // ... read offsets, seek to data sections, parse arrays
```

### Pros

- **O(1) random access** — the fastest possible lookup
- **Cache-friendly** — contiguous memory layout, hardware prefetch works
- **Minimal memory overhead** — no graph edges, no object headers per cell
- **Built-in F# operations** — `init`, `map`, `mapi`, `iter`, `iteri`, `copy`, `sub`
- **Natural fit** — BAR map layers ARE dense regular grids
- **Excellent REPL experience** — immediate, no compilation step
- **Composable** — stack multiple layers, derive new layers via `Array2D.init`

### Cons

- **Fixed size** — no dynamic resizing (not a problem for maps)
- **Mutable** — not idiomatic functional style (pragmatic for performance)
- **No built-in spatial queries** — neighbor iteration, range queries must be hand-written
- **Memory for large maps** — a 16×16 map at heightmap resolution = 1025×1025 × 4 bytes = ~4 MB per layer. Manageable, but multiple layers add up

### Verdict: STRONGLY RECOMMENDED

This is the foundational layer. Every other approach (graphs, analysis, DSLs) builds on top of grid data stored in `Array2D`. It's the most natural, performant, and ergonomic representation for dense map data in F#.

---

## 6. Approach 4: DUs + Records + Active Patterns

### Concept

Model the map domain using F#'s algebraic data types for type safety and pattern matching:

```fsharp
// Terrain classification
type Terrain =
    | Land of hardness: float
    | Water of depth: float
    | Cliff
    | Lava

// Map features
type MapFeature =
    | MexSpot of x: float * z: float * metalValue: float
    | GeoVent of x: float * z: float
    | StartPosition of teamId: int * x: float * z: float
    | Ramp of fromCell: int * int * toCell: int * int

// Structured map data
type BarMap = {
    Name: string
    WidthElmos: int
    HeightElmos: int
    HeightMap: float32[,]
    MetalMap: byte[,]
    TypeMap: byte[,]
    Features: MapFeature list
    MetalSpots: MexSpot list
    StartPositions: Map<int, float * float * float>
}
```

### Active Patterns for Terrain Queries

```fsharp
// Classify terrain by type index
let (|Passable|Impassable|Aquatic|) typeIndex =
    match typeIndex with
    | t when t < 10 -> Passable
    | t when t < 20 -> Aquatic
    | _ -> Impassable

// Check if cell is within radius
let (|InRadius|_|) (cx, cz) radius (x, z) =
    let dx, dz = float(x - cx), float(z - cz)
    if dx*dx + dz*dz <= radius*radius then Some () else None

// Height-based classification
let (|HighGround|LowGround|Valley|) (map: BarMap) (x, y) =
    let h = map.HeightMap.[x, y]
    let avg = averageHeightAround map x y 5
    if h > avg + 20.0f then HighGround
    elif h < avg - 20.0f then Valley
    else LowGround

// Usage in AI logic
match terrainAt map x y with
| Passable & HighGround -> "good defensive position"
| Passable & Valley -> "vulnerable corridor"
| Aquatic -> "need naval units"
| Impassable -> "blocked"
```

### Pros

- **Exhaustive matching** — compiler catches missing terrain cases
- **Self-documenting** — code reads like domain language
- **Composable** — active patterns combine with `&` and `|`
- **Zero runtime overhead** for simple single/multi-case patterns
- **Excellent REPL experience** — full IntelliSense, no compilation lag
- **Testable** — pure functions, no side effects

### Cons

- **Multi-case active patterns limited to 7 cases** — enough for terrain, but not for 100+ unit types
- **Active patterns on hot paths** may be slower than direct field access (function call overhead)
- **Manual maintenance** when domain evolves

### Verdict: STRONGLY RECOMMENDED

This is how you make map data *meaningful* in F#. The grid layers (Approach 3) store raw numbers; DUs and active patterns give them domain semantics. Every AI decision function should pattern-match on these types.

---

## 7. Approach 5: Computation Expression DSL

### Concept

Define a custom `builder { ... }` syntax for map queries and spatial operations:

```fsharp
// Spatial query DSL
let expansionSites = mapQuery {
    inRegion (0, 0) (4096, 4096)
    where (fun cell -> cell.Metal > 50uy)
    excludeRadius enemyBasePos 1500.0
    sortBy MetalDensity Descending
    take 5
}

// Pathfinding DSL
let route = pathQuery {
    from myBasePos
    toward enemyBasePos
    avoiding threatMap 0.7  // threshold
    preferring HighGround
    withMoveType Kbot
}

// Influence map builder
let threatMap = influence {
    layer "enemyUnits" {
        sources (knownEnemies |> List.map (fun u -> u.Pos, u.DPS))
        decay 0.95
        radius 800.0
    }
    layer "myDefenses" {
        sources (myTurrets |> List.map (fun u -> u.Pos, -u.DPS))
        decay 0.90
        radius 600.0
    }
    combine Add
}
```

### Implementation Sketch

```fsharp
type MapQueryBuilder() =
    member _.Yield(_) = QueryState.Empty
    [<CustomOperation("inRegion")>]
    member _.InRegion(state, (x1,z1), (x2,z2)) = { state with Bounds = Some (x1,z1,x2,z2) }
    [<CustomOperation("where")>]
    member _.Where(state, predicate) = { state with Filters = predicate :: state.Filters }
    // ...
    member _.Run(state) = executeQuery state

let mapQuery = MapQueryBuilder()
```

### Pros

- **Highly readable** — AI logic reads like natural language
- **Full F# type system** — compose with existing code, IntelliSense works
- **Hot-reloadable** — re-evaluate in FSI, no restart needed
- **Extensible** — add new operations as custom operations
- **Chainable** — build complex queries incrementally

### Cons

- **Medium development effort** — CE internals (Bind, Return, CustomOperation) have a learning curve
- **No compile-time schema introspection** — the DSL is defined manually
- **Debugging** — CE-generated code can produce confusing stack traces
- **Overkill for simple queries** — `map.HeightMap.[x,y]` is simpler than a DSL for point lookups

### Verdict: RECOMMENDED (for complex spatial queries)

Best suited as a higher-level API layered on top of the grid and type foundations. Don't build this first — start with Array2D + DUs, then add CEs when query patterns become repetitive.

---

## 8. Approach 6: Memoized Lazy Modules

### Concept

Cache expensive map computations (parsing, derived layers, pathfinding results) at module level:

```fsharp
module MapCache =
    let private cache = ConcurrentDictionary<string, Lazy<BarMap>>()

    let load (path: string) =
        cache.GetOrAdd(path, lazy (SmfParser.parse path)).Value

    let fromEngine (client: BarClient) =
        cache.GetOrAdd("__engine__", lazy (
            let w = Callbacks.getMapWidth client
            let h = Callbacks.getMapHeight client
            let heightMap = Callbacks.getHeightMap client
            let metalMap = Callbacks.getMetalMap client
            { Width = w; Height = h; HeightMap = heightMap; MetalMap = metalMap; ... }
        )).Value

module DerivedLayers =
    let private slopeCache = ConcurrentDictionary<string, Lazy<float32[,]>>()

    let slopeMap (map: BarMap) =
        slopeCache.GetOrAdd(map.Name, lazy (computeSlopes map.HeightMap)).Value

    let passabilityMap (map: BarMap) (moveType: MoveType) =
        // cached per map+moveType combination
        ...
```

### Pros

- **Thread-safe** with `ConcurrentDictionary` + `Lazy`
- **Pay-once** — expensive operations computed at most once per session
- **Transparent** — callers don't need to know about caching
- **Composable** — works with all other approaches
- **REPL-friendly** — caches persist across FSI evaluations within a session

### Cons

- **Memory management** — module-level caches never free (acceptable for REPL sessions)
- **Cache invalidation** — manual; stale data if map changes mid-session (rare)
- **Hidden state** — purists dislike global mutable state

### Verdict: RECOMMENDED (as infrastructure)

Essential plumbing for REPL ergonomics. Map parsing and derived computations should always be cached. This isn't an alternative to other approaches — it's a supporting layer that makes them practical.

---

## 9. Terrain Analysis Patterns

These are higher-level algorithms that consume the map data structures from the approaches above.

### 9.1 Influence Maps

A 2D grid where each cell stores a scalar value representing the "influence" of game entities (units, threats, resources). Values propagate outward with distance decay.

```fsharp
let computeInfluence (sources: (int * int * float) list) (decay: float) (grid: float[,]) =
    for (sx, sz, strength) in sources do
        for x in 0 .. Array2D.length1 grid - 1 do
            for z in 0 .. Array2D.length2 grid - 1 do
                let dist = sqrt(float((x-sx)*(x-sx) + (z-sz)*(z-sz)))
                grid.[x, z] <- grid.[x, z] + strength * (decay ** dist)
```

**Use cases**: Threat assessment, territory control, expansion site scoring, unit positioning.

**Resolution**: Metalmap resolution (16 elmos/cell) is a good default — coarse enough to be fast, fine enough for strategic decisions.

### 9.2 Flow Fields

Compute a vector field from a destination by running Dijkstra once. Every unit reads its local vector to move.

**Advantage over A***: For N units moving to the same destination, flow field costs O(cells) vs. A*'s O(N × path_length). Ideal for army movement in RTS.

**Implementation**: Single BFS/Dijkstra from goal → every cell stores direction to next cell toward goal.

### 9.3 Region Decomposition & Choke Points

Decompose the map into strategically meaningful regions connected at choke points.

**Approaches from academic literature**:

| Method | Technique | Speed | Quality |
|--------|-----------|-------|---------|
| **BWTA** | Voronoi diagram on walkable terrain | Slow (>1 min/map) | Good |
| **BWTA2** | Contour tracing (10x faster than BWTA) | Medium | Better |
| **BWEM** | Optimized C++ analysis | Fast | Comparable |
| **Simple flood-fill** | BFS from open areas, walls as barriers | Very fast | Approximate |

For F#/REPL use, a simplified approach:
1. Compute passability from heightmap slopes + terrain types
2. Flood-fill to identify connected walkable regions
3. Find narrow passages between regions (cells where passable width < threshold) → choke points
4. Build a **region graph**: nodes = regions, edges = choke connections with width/cost

```fsharp
type Region = { Id: int; Cells: (int * int) Set; Center: int * int; Area: int }
type ChokePoint = { RegionA: int; RegionB: int; Position: int * int; Width: float }
type RegionGraph = { Regions: Region[]; Chokes: ChokePoint[]; Adjacency: Map<int, int list> }
```

This region graph enables **fast strategic pathfinding** — A* on ~20 region nodes instead of ~1M grid cells.

### 9.4 Potential Fields

Generated via differential equations; gradient descent gives navigation vectors. Handle moving obstacles naturally.

**Use case**: Unit maneuvering, formation movement, kiting behavior.

---

## 10. Comparison Matrix

| Approach | Dev Effort | REPL Experience | Performance | Type Safety | Recommended? |
|----------|-----------|-----------------|-------------|-------------|-------------|
| **Type Provider** | Very High (2–4 weeks) | Poor (no hot reload, DLL lock) | N/A | Excellent | No |
| **Graph — QuikGraph** | Low (wrapping) | Good | Good | Good | Fallback |
| **Graph — FSharp.FGL** | Medium | Good | Fair | Excellent | No |
| **Graph — Custom A*** | Medium (~400 LOC) | Excellent | Best | Good | Yes |
| **Array2D Grid Layers** | Low | Excellent | Best | Good | Yes (foundation) |
| **DUs + Records** | Low | Excellent | Excellent | Excellent | Yes (domain model) |
| **Active Patterns** | Low | Excellent | Good | Excellent | Yes (queries) |
| **Computation Expr DSL** | Medium | Excellent | Good | Good | Yes (complex queries) |
| **Memoized Modules** | Low | Excellent | Excellent | Good | Yes (infrastructure) |
| **Influence Maps** | Low–Medium | Excellent | Good | Good | Yes (AI analysis) |
| **Region Graph** | Medium | Excellent | Excellent (strategic) | Good | Yes (strategic pathfinding) |

---

## 11. Recommended Architecture

### Layer 1: Raw Data (Array2D Grid Layers)

The foundation. Each map layer is a native `Array2D`:

```
┌─────────────────────────────────────────────┐
│  MapGrid                                     │
│  ├── HeightMap:  float32[,]  (8 elmos/cell)  │
│  ├── SlopeMap:   float32[,]  (derived)       │
│  ├── MetalMap:   byte[,]     (16 elmos/cell) │
│  ├── TypeMap:    byte[,]     (16 elmos/cell) │
│  ├── LosMap:     int[,]      (runtime only)  │
│  └── RadarMap:   int[,]      (runtime only)  │
└─────────────────────────────────────────────┘
```

**Data source**: Engine callbacks (runtime) or SMF binary parser (offline).

### Layer 2: Domain Model (DUs + Records + Active Patterns)

Give raw data meaning:

```
┌────────────────────────────────────────────┐
│  BarMap record                              │
│  ├── Terrain DU (Land|Water|Cliff|Lava)    │
│  ├── MapFeature DU (Mex|Geo|Start|Ramp)   │
│  ├── Active patterns for classification     │
│  └── Member functions for point queries     │
└────────────────────────────────────────────┘
```

### Layer 3: Analysis (Derived Layers + Graphs)

Computed from Layer 1+2, cached via memoized modules:

```
┌──────────────────────────────────────────────┐
│  Analysis layers                              │
│  ├── Passability:  bool[,]  per MoveType     │
│  ├── Influence:    float[,] (threat/value)   │
│  ├── Regions:      Region[] + ChokePoint[]   │
│  ├── RegionGraph:  adjacency for strategic A* │
│  └── FlowFields:   (dx,dz)[,] per target    │
└──────────────────────────────────────────────┘
```

### Layer 4: Query API (CEs + Active Patterns)

High-level REPL-friendly interface:

```fsharp
// In FSI session:
#load "prelude.fsx"

let map = MapCache.fromEngine client

// Point queries
map.HeightAt(1024, 2048)
map.TerrainAt(1024, 2048)  // → Land { hardness = 200.0 }

// Spatial queries
mapQuery { inRegion (0,0) (4096,4096); where (fun c -> c.Metal > 50uy); take 5 }

// Pathfinding
let route = Pathfinder.astar map.Passability startPos goalPos
let strategicRoute = Pathfinder.regionPath map.RegionGraph startRegion goalRegion

// Analysis
let threats = Influence.compute enemyPositions 0.95 map.Grid
let chokes = Regions.chokePoints map
```

### Implementation Priority

1. **First**: Wrap the 5 unwrapped map callbacks (heightmap, slope, LOS, radar, resource) in `Callbacks` module — unlocks all runtime map data
2. **Second**: Define `BarMap` record type + `MapGrid` with `Array2D` layers — the data foundation
3. **Third**: Add terrain DUs + active patterns — makes data queryable
4. **Fourth**: Implement custom A* on grid + region decomposition — strategic pathfinding
5. **Fifth**: Add computation expression DSL — ergonomic complex queries
6. **Optional**: SMF binary parser for offline analysis without running the engine
7. **Skip**: Type provider — cost/benefit doesn't justify it

---

## 12. References

### BAR/Spring Map Format
- [Mapdev:SMF format — Spring Wiki](https://springrts.com/wiki/Mapdev:SMF_format)
- [BAR Mapping Guide: File Structure](https://www.beyondallreason.info/guide/mapping-1-file-structure-prerequisites)
- [Mapdev:mapinfo.lua — Spring Wiki](https://springrts.com/wiki/Mapdev:mapinfo.lua)

### Engine APIs
- [Lua SyncedRead — Spring Wiki](https://springrts.com/wiki/Lua_SyncedRead)
- [Lua PathFinder — Spring Wiki](https://springrts.com/wiki/Lua_PathFinder)
- [BAR Microblog: New QTPFS Pathfinding](https://www.beyondallreason.info/microblogs/48)

### Existing Tools
- [PyMapConv — SMF compiler](https://github.com/Beherith/springrts_smf_compiler)
- [SpringMapEdit — Java editor](https://github.com/aeonios/SpringMapEdit)
- [BAR Map Generator](https://github.com/hendkai/bar-map-generator)

### F# Type Providers
- [Tutorial: Create a Type Provider — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fsharp/tutorials/type-providers/creating-a-type-provider)
- [FSharp.TypeProviders.SDK](https://github.com/fsprojects/FSharp.TypeProviders.SDK)
- [Generative Type Providers — Medium](https://medium.com/@haumohio/the-trips-and-traps-of-creating-a-generative-type-provider-in-f-75162d99622c)
- [FSharp.Data Providers](https://fsprojects.github.io/FSharp.Data/)

### Graph & Pathfinding Libraries
- [QuikGraph — GitHub](https://github.com/KeRNeLith/QuikGraph)
- [FSharp.FGL — GitHub](https://github.com/CSBiology/FSharp.FGL)
- [Dijkstra.NET — NuGet](https://www.nuget.org/packages/Dijkstra.NET/)

### Spatial Libraries
- [NetTopologySuite Quadtree API](https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.Index.Quadtree.Quadtree-1.html)
- [Array2D Module — FSharp.Core](https://fsharp.github.io/fsharp-core-docs/reference/fsharp-collections-array2dmodule.html)

### RTS AI Terrain Analysis (Academic)
- [Terrain Analysis in RTS Games — Perkins, AAAI 2010](https://ojs.aaai.org/index.php/AIIDE/article/view/12405)
- [Improving Terrain Analysis — AAAI](https://ojs.aaai.org/index.php/AIIDE/article/view/12889)
- [Terrain Analysis in StarCraft 1 and 2 — ResearchGate](https://www.researchgate.net/publication/360698312)
- [RTS Pathfinding: Flow Fields — jdxdev](https://www.jdxdev.com/blog/2020/05/03/flowfields/)
- [How to RTS: Basic Flow Fields](https://howtorts.github.io/2014/01/04/basic-flow-fields.html)
- [Red Blob Games: Grid Pathfinding](https://www.redblobgames.com/pathfinding/grids/algorithms.html)
