# Data Model: 006-validate-highbar-fixes

**Date**: 2026-04-06  
**Feature Spec**: [spec.md](spec.md)

## Entities

### MapGrid (modified)

Represents bundled map data layers from the BAR engine.

| Field | Type | Dimensions | Change |
|-------|------|-----------|--------|
| WidthElmos | int | scalar | unchanged |
| HeightElmos | int | scalar | unchanged |
| WidthHeightmap | int | scalar | unchanged — stores `w` (map squares) |
| HeightHeightmap | int | scalar | unchanged — stores `h` (map squares) |
| HeightMap | float32[,] | `(w+1) * (h+1)` | **fix**: use corners heightmap callback (was center) |
| SlopeMap | float32[,] | `(w/2) * (h/2)` | **fix**: half-resolution (was `(w+1)*(h+1)`) |
| ResourceMap | int[,] | `w * h` | unchanged |
| LosMap | int[,] | `w * h` | unchanged |
| RadarMap | int[,] | `w * h` | unchanged |

**Dimension summary for 896x1024 map**:

| Layer | Width | Height | Total cells |
|-------|-------|--------|-------------|
| HeightMap | 897 | 1025 | 919,425 |
| SlopeMap | 448 | 512 | 229,376 |
| ResourceMap | 896 | 1024 | 917,504 |
| LosMap | 896 | 1024 | 917,504 |
| RadarMap | 896 | 1024 | 917,504 |

### CallbackId enum (modified)

New enum value in `proto/highbar/callbacks.proto`:

| Value | Name | Returns |
|-------|------|---------|
| 59 | `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP` | float array, `(w+1)*(h+1)` values |

### Callbacks module (modified)

New function:

| Function | Signature | Callback ID |
|----------|-----------|-------------|
| `getCornersHeightMap` | `stream: NetworkStream -> float32 list` | 59 |

Existing `getHeightMap` (callback 52) remains for backward compatibility but is no longer used by `MapGrid.loadFromEngine`.

## Validation Rules

- HeightMap array length must equal `(w+1) * (h+1)` where w/h come from `getMapWidth`/`getMapHeight`
- SlopeMap array length must equal `(w/2) * (h/2)`
- LosMap, RadarMap, ResourceMap array lengths must equal `w * h`
- Map width and height must be positive even integers (required for `w/2` calculation)

## Impact on MapQuery

- `heightAtElmo`: No change — continues to use heightmap grid indices via `x / 8`
- `slopeAtElmo`: **Must change** — needs bounds check against slope map dimensions and use `x / 16` for grid index conversion (slope map is 2x coarser)
- `terrainAtElmo`: Uses both heightmap and slope — slope access needs adjustment
- `heightSubRegion`: No change — operates on heightmap only
- `resourceHotspots`: No change — operates on resource map at `w * h`
