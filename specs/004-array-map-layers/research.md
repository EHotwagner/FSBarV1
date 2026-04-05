# Research: Array2D Map Data Layers

**Feature**: 004-array-map-layers  
**Date**: 2026-04-05

## R1: Reshaping Flat Arrays to Array2D

**Decision**: Convert `float32 list` / `int list` from protobuf `FloatArray.Values` / `IntArray.Values` into `Array2D` using `Array2D.init` with row-major indexing.

**Rationale**: The engine returns map data as flat arrays in row-major order (x varies fastest, z is the row index). F# `Array2D` provides O(1) indexing with `.[x, z]` and built-in operations (`init`, `iteri`, `sub`, `map`). The conversion is a one-time cost at load time.

**Implementation pattern**:
```fsharp
let toArray2D (width: int) (height: int) (values: float32 list) : float32[,] =
    let arr = values |> List.toArray
    Array2D.init width height (fun x z -> arr.[z * width + x])
```

**Alternatives considered**:
- Jagged arrays (`float32[][]`): Less ergonomic, no built-in 2D operations, no `sub` extraction.
- Span/Memory: Better for zero-copy, but not compatible with F# `Array2D.init/iteri/sub` operations needed for REPL ergonomics.
- Keep as flat array with manual index math: Error-prone, poor discoverability in REPL.

## R2: Array Dimensions per Layer

**Decision**: Derive dimensions from `getMapWidth()` and `getMapHeight()` which return heightmap squares.

| Layer | Width | Height | Element Type | Source Callback |
|-------|-------|--------|-------------|-----------------|
| HeightMap | W + 1 | H + 1 | float32 | CallbackMapGetHeightMap (52) |
| SlopeMap | W + 1 | H + 1 | float32 | CallbackMapGetSlopeMap (53) |
| LosMap | W | H | int | CallbackMapGetLosMap (54) |
| RadarMap | W | H | int | CallbackMapGetRadarMap (55) |
| ResourceMap | W | H | int (verify: report says FloatArray) | CallbackMapGetResourceMap (56) |

**T005a Finding (2026-04-05)**: Live engine testing revealed that the HighBar proxy returns **empty arrays** for all five map data callbacks (52-56). The callbacks are defined in the proto and the engine responds with a CallbackResponse, but the response contains empty FloatArray/IntArray data. This is a proxy limitation — the proxy needs to be updated to relay these bulk map data callbacks from the engine. The client-side implementation (Callbacks, MapGrid, MapQuery, MapCache) is correct and will work once the proxy supports these callbacks. Additionally, `Protocol.sendCallback` was fixed to handle interleaved Frame messages that arrive during callback round-trips.

**Rationale**: Heightmap and slope are at 8 elmos/cell resolution and include the +1 boundary vertices. LOS, radar, and resource maps are at the same heightmap-square resolution without the boundary. These dimensions match the Spring/Recoil engine conventions documented in the SMF format spec.

**Note**: The engine may return LOS/radar at different resolutions (e.g., losmap at W/2 × H/2). The implementation should validate returned array length against expected dimensions and fall back to inferring dimensions from the array length if they don't match.

**Alternatives considered**:
- Hardcode dimensions per known map: Fragile, fails for custom maps.
- Query dimensions from array length: Used as fallback, but primary approach derives from map dimensions.

## R3: Terrain Type Index Mapping

**Decision**: Map engine typemap byte values to a `Terrain` discriminated union using the Spring/Recoil terrain type convention.

**Rationale**: The engine defines terrain types 0-255 in `mapinfo.lua` per map. The common convention in BAR maps:
- Types 0-3: Standard land terrain (varying speed multipliers)
- Type 4+: Often water/special terrain

Since type indices are map-specific (defined in each map's `mapinfo.lua`), the classification should use the engine's `GetGroundInfo` or rely on a configurable mapping. For initial implementation, use a simple heuristic: treat the typemap value combined with height (below 0 = water) as the classifier.

**Revised approach per clarification**: Use typemap indices as authoritative. Provide a default mapping that can be overridden. The default treats index 0 as standard land and applies height < 0 as water override (since most BAR maps use sea level = 0).

**Alternatives considered**:
- Pure height-based: Misses terrain types like lava or road.
- Engine query per-cell: Too slow for full-map classification (would require W×H callbacks).

## R4: Passability per Movement Type

**Decision**: Compute passability as `bool[,]` per movement type using slope thresholds from the engine's slope map and terrain type from the typemap.

**Rationale**: Each movement type has different slope tolerances and terrain traversability:
- **Kbot**: Can traverse steep slopes, blocked by deep water
- **Tank**: Limited slope tolerance, blocked by water
- **Hover**: Can cross water, limited by very steep slopes
- **Ship**: Only traverses water

Thresholds are approximate since exact values are defined in the engine's `moveinfo.tdf` / `moveDefs`. Initial implementation uses conservative defaults; future iterations can query engine move defs.

**Alternatives considered**:
- Engine `TestMoveOrder` per cell: Authoritative but requires W×H callbacks per move type — far too slow.
- Single passability layer: Insufficient for mixed armies (hover vs. tank pathing differs significantly).

## R5: Caching Strategy

**Decision**: Use module-level `ConcurrentDictionary<string, Lazy<'T>>` for static layers and derived computations. Dynamic layers (LOS, radar) are stored as mutable fields on the MapGrid record that can be refreshed.

**Rationale**: 
- Static layers (heightmap, slope, metal, type, resource) never change during a game session → cache permanently.
- Derived layers (passability per move type) depend only on static data → cache permanently, keyed by move type.
- Dynamic layers (LOS, radar) change every frame → store on the record but provide a `refresh` function that re-fetches from engine.
- `ConcurrentDictionary + Lazy` is thread-safe, idiomatic .NET, and well-suited to REPL sessions where the cache persists across evaluations.

**Alternatives considered**:
- Immutable record with copy-on-refresh: Allocates new records for every LOS/radar update; wasteful for large arrays.
- No caching (always re-fetch): Defeats REPL ergonomics; static data never changes.
- `MailboxProcessor` actor: Overkill for a caching layer.

## R6: REPL Display

**Decision**: Override `ToString()` on the `MapGrid` record to show a compact summary. Do not print array contents.

**Rationale**: FSI calls `ToString()` on evaluated expressions. A 4097×4097 float32 array printed to console is unusable. A summary like `MapGrid { 16384×16384 elmos, 2049×2049 heightmap, 5 layers loaded }` is immediately informative.

**Alternatives considered**:
- Custom FSI printer via `fsi.AddPrinter`: Works but requires registration in prelude.fsx; `ToString()` override is simpler and works everywhere.
- Both: Provide `ToString()` override for universal use, document `fsi.AddPrinter` in quickstart for richer formatting.
