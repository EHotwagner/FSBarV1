# Public API Contract: Array2D Map Data Layers

**Feature**: 004-array-map-layers  
**Date**: 2026-04-05

This document defines the public API surface that will be exposed via `.fsi` signature files. These signatures serve as compiler-enforced contracts per Constitution Principle II.

## Module: Callbacks (additions to existing)

```fsharp
// Additions to existing Callbacks.fsi

/// Get the full heightmap as a flat float32 list (row-major, (W+1)*(H+1) elements).
val getHeightMap: stream: NetworkStream -> float32 list

/// Get the full slope map as a flat float32 list (row-major, (W+1)*(H+1) elements).
val getSlopeMap: stream: NetworkStream -> float32 list

/// Get the line-of-sight map as a flat int list (row-major, W*H elements).
val getLosMap: stream: NetworkStream -> int list

/// Get the radar coverage map as a flat int list (row-major, W*H elements).
val getRadarMap: stream: NetworkStream -> int list

/// Get the resource distribution map as a flat int list (row-major, W*H elements).
val getResourceMap: stream: NetworkStream -> int list
```

## Module: MapGrid

```fsharp
namespace FSBar.Client

open System.Net.Sockets

/// Terrain classification based on engine typemap indices.
[<RequireQualifiedAccess>]
type Terrain =
    | Land of hardness: float32
    | Water of depth: float32
    | Cliff of slope: float32

/// Unit movement type for passability computation.
[<RequireQualifiedAccess>]
type MoveType =
    | Kbot
    | Tank
    | Hover
    | Ship

/// Bundled map data layers from the BAR engine.
type MapGrid =
    { WidthElmos: int
      HeightElmos: int
      WidthHeightmap: int
      HeightHeightmap: int
      HeightMap: float32[,]
      SlopeMap: float32[,]
      ResourceMap: int[,]
      LosMap: int[,]
      RadarMap: int[,] }
    override ToString: unit -> string

module MapGrid =

    /// Load all map layers from the engine into a MapGrid record.
    /// Raises on empty arrays or dimension mismatches.
    val loadFromEngine: stream: NetworkStream -> MapGrid

    /// Refresh the LOS map layer from the engine.
    val refreshLos: stream: NetworkStream -> grid: MapGrid -> MapGrid

    /// Refresh the radar map layer from the engine.
    val refreshRadar: stream: NetworkStream -> grid: MapGrid -> MapGrid

    /// Classify terrain at a heightmap grid cell.
    val terrainAt: grid: MapGrid -> x: int -> z: int -> Terrain

    /// Compute a passability grid for the given movement type.
    val passability: grid: MapGrid -> moveType: MoveType -> bool[,]
```

## Module: MapQuery

```fsharp
namespace FSBar.Client

module MapQuery =

    /// Get the height at an elmo coordinate. Returns Error for out-of-bounds.
    val heightAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// Get the slope at an elmo coordinate. Returns Error for out-of-bounds.
    val slopeAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// Get the terrain classification at an elmo coordinate.
    val terrainAtElmo: grid: MapGrid -> x: int -> z: int -> Result<Terrain, string>

    /// Extract a rectangular sub-region of the heightmap by elmo bounds.
    val heightSubRegion: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> Result<float32[,], string>

    /// Find all cells in a region where resource value exceeds threshold, sorted descending.
    val resourceHotspots: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> threshold: int -> (int * int * int) list

    /// Convert elmo coordinates to heightmap grid indices.
    val elmoToGrid: x: int -> z: int -> int * int

    /// Convert heightmap grid indices to elmo coordinates.
    val gridToElmo: x: int -> z: int -> int * int
```

## Module: MapCache

```fsharp
namespace FSBar.Client

open System.Net.Sockets

module MapCache =

    /// Load a MapGrid from the engine, caching the result.
    /// Subsequent calls return the cached grid.
    val fromEngine: stream: NetworkStream -> MapGrid

    /// Get or compute a passability grid for the given movement type.
    /// Cached per MoveType after first computation.
    val passability: grid: MapGrid -> moveType: MoveType -> bool[,]

    /// Clear all cached data (useful for new game sessions).
    val clear: unit -> unit
```

## Active Patterns (in MapGrid module)

```fsharp
/// Classify terrain by type — for use in pattern matching.
val (|Land|Water|Cliff|): Terrain -> Choice<float32, float32, float32>

/// Check if a heightmap cell is passable for a given move type.
val (|Passable|Impassable|): MapGrid -> MoveType -> int -> int -> Choice<unit, unit>
```

## Compile Order (additions to FSBar.Client.fsproj)

New files are inserted before `BarClient.fsi`/`BarClient.fs` (which is the top-level orchestrator):

```xml
<Compile Include="MapGrid.fsi" />
<Compile Include="MapGrid.fs" />
<Compile Include="MapQuery.fsi" />
<Compile Include="MapQuery.fs" />
<Compile Include="MapCache.fsi" />
<Compile Include="MapCache.fs" />
```
