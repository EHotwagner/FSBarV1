# Data Model: Array2D Map Data Layers

**Feature**: 004-array-map-layers  
**Date**: 2026-04-05

## Entities

### Terrain (Discriminated Union)

Classifies a map cell's terrain type based on engine typemap indices.

| Case | Attributes | Description |
|------|-----------|-------------|
| Land | hardness: float32 | Standard traversable ground |
| Water | depth: float32 | Below sea level; depth is negative height |
| Cliff | slope: float32 | Impassable steep terrain |

**Derivation**: Height + slope heuristic: height < 0 → Water(depth), slope > threshold → Cliff(slope), else → Land(hardness). Typemap indices are not available via engine callbacks. Revisit if a typemap callback is added.

### MoveType (Discriminated Union)

Unit mobility class determining passability rules.

| Case | Description |
|------|-------------|
| Kbot | Bipedal/walking units — steep slope tolerance, blocked by deep water |
| Tank | Wheeled/tracked units — moderate slope tolerance, blocked by water |
| Hover | Hovercraft — crosses water, limited by extreme slopes |
| Ship | Naval units — water only |

### MapGrid (Record)

The core data structure bundling all map layers.

| Field | Type | Resolution | Source | Mutability |
|-------|------|-----------|--------|------------|
| WidthElmos | int | — | Derived: W × 8 | Immutable |
| HeightElmos | int | — | Derived: H × 8 | Immutable |
| WidthHeightmap | int | — | getMapWidth() | Immutable |
| HeightHeightmap | int | — | getMapHeight() | Immutable |
| HeightMap | float32[,] | (W+1) × (H+1), 8 elmos/cell | Callback 52 | Immutable |
| SlopeMap | float32[,] | (W+1) × (H+1), 8 elmos/cell | Callback 53 | Immutable |
| ResourceMap | int[,] | W × H | Callback 56 | Immutable |
| LosMap | int[,] | W × H | Callback 54 | Refreshable |
| RadarMap | int[,] | W × H | Callback 55 | Refreshable |

**Notes**:
- MetalMap and TypeMap are not directly available as separate callbacks (52-56 cover height, slope, LOS, radar, resource). Metal spot data is available via the existing `getMetalSpots()` callback. TypeMap may need to be inferred or added in a future callback extension.
- LosMap and RadarMap are marked refreshable — they can be individually re-fetched from the engine during gameplay.

### MapGridSummary (for REPL display)

Not a separate entity; implemented as `ToString()` override on MapGrid.

Format: `MapGrid { WxH elmos, WxH heightmap, N layers loaded }`

## Relationships

```
MapGrid
├── contains → HeightMap (float32[,])
├── contains → SlopeMap (float32[,])
├── contains → ResourceMap (int[,])
├── contains → LosMap (int[,], refreshable)
├── contains → RadarMap (int[,], refreshable)
├── derives → Passability per MoveType (bool[,], cached externally)
└── classifies via → Terrain DU (per-cell query)

MapCache
├── caches → MapGrid (one per session)
└── caches → Passability layers (one per MoveType)

MapQuery
├── reads → MapGrid layers
├── converts → elmo coordinates ↔ grid indices
└── returns → Terrain classification, height values, sub-regions
```

## Coordinate System

| Unit | Description | Conversion |
|------|-------------|------------|
| Elmo | Base engine unit | — |
| Heightmap cell | 8 × 8 elmos | elmo / 8 = cell index |
| Resource/LOS/Radar cell | Matches heightmap squares | Same as heightmap W × H |

**Conversion functions**:
- `elmoToHeightmapIndex(elmo) = elmo / 8` (integer division)
- `heightmapIndexToElmo(idx) = idx * 8`
- Out-of-bounds: Return `Error` with descriptive message

## Validation Rules

- HeightMap array length must equal `(W+1) * (H+1)` where W, H are from getMapWidth/Height
- SlopeMap array length must equal HeightMap array length
- LOS/Radar/Resource array length must equal `W * H`
- Empty arrays (length 0) from engine → raise descriptive error, do not create zero-dimension grid
- Coordinate queries: x must be in `[0, WidthElmos)`, z must be in `[0, HeightElmos)`

## State Transitions

MapGrid has two states:

1. **Loading**: Individual layers are fetched from engine. If any layer fetch fails, loading stops with an error listing which layers succeeded and which failed.
2. **Loaded**: All layers populated. Static layers are immutable. Dynamic layers (LOS, radar) can transition to **Refreshing** and back to **Loaded** via the refresh operation.

Passability layers are computed lazily on first request per MoveType and cached permanently for the session.
