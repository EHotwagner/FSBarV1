# Research: 006-validate-highbar-fixes

**Date**: 2026-04-06  
**Feature Spec**: [spec.md](spec.md)

## R1: Heightmap Dimension Mismatch Root Cause

**Decision**: Switch from center heightmap (`Map_getHeightMap`, callback 52) to corners heightmap (`Map_getCornersHeightMap`, callback 59).

**Rationale**: The Spring/Recoil engine exposes two heightmap APIs:
- `Map_getHeightMap` — returns `mapx * mapy` center-of-square heights (917,504 for an 896x1024 map)
- `Map_getCornersHeightMap` — returns `(mapx+1) * (mapy+1)` vertex/corner heights (919,425)

The FSBarV1 code calls `getHeightMap` (center) but reshapes the result using `(w+1) * (h+1)` (corner dimensions), causing the dimension mismatch. The corners heightmap is the ground truth in the Spring/BAR ecosystem — it matches the SMF file format, map editors, and terrain rendering.

**Alternatives considered**:
- Use center heightmap with `w*h` dimensions: Simpler but loses vertex precision; inconsistent with Spring ecosystem conventions.
- Support both: Unnecessary complexity; corners heightmap subsumes center data.

**Sources**: Spring engine `SSkirmishAICallbackImpl.cpp` (lines 1785-1814), SpringRTS wiki SMF format spec, BAR mapping guide, `spring-map-parser` npm library.

## R2: Slope Map Dimensions

**Decision**: Reshape slope map at half-resolution `(w/2) * (h/2)` instead of `(w+1) * (h+1)`.

**Rationale**: The Spring engine's `Map_getSlopeMap` returns slope values at 2x2 map-square granularity, yielding `(mapx/2) * (mapy/2)` values. For the 896x1024 map, that's 448x512 = 229,376 values. The current code wrongly uses heightmap dimensions.

**Alternatives considered**:
- Derive slopes locally from corners heightmap: Higher resolution but CPU-costly and diverges from engine's authoritative slope calculation.
- Runtime adaptation: Unnecessary now that engine behavior is documented.

## R3: HighBar Proxy Callback Availability

**Decision**: Consume `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59` from HighBar V2 commit 026.

**Rationale**: HighBar V2 already added this callback in commit `c70559a` (026-corners-heightmap-callback). The proxy allocates `(width+1) * (height+1)` floats and calls the engine's native `Map_getCornersHeightMap()`. FSBarV1 only needs to:
1. Add the enum value to `proto/highbar/callbacks.proto`
2. Regenerate F# bindings
3. Add a `getCornersHeightMap` wrapper in `Callbacks.fs`

**HighBar commits consumed by this feature**:

| Commit | Feature | Status in FSBarV1 |
|--------|---------|-------------------|
| 023+024 | `EngineDisconnectedException` + read timeouts | Already ported (branch 005) |
| 025 | Map client wrappers, slope/LOS/radar/resource | Dimension fix needed |
| 026 | Corners heightmap callback (ID 59) | New — must add to proto + callbacks |

## R4: MapQuery Impact from Dimension Changes

**Decision**: `MapQuery` functions (`slopeAtElmo`, `heightAtElmo`, etc.) use heightmap dimensions for bounds checking. Since the heightmap remains `(w+1)*(h+1)`, these functions continue to work correctly. However, `slopeAtElmo` currently bounds-checks against heightmap dimensions but indexes into the slope map — this will break with the slope map at half-resolution. The `slopeAtElmo` function needs its own bounds check against slope map dimensions, and its elmo-to-grid conversion should use `/ 16` instead of `/ 8` to account for the coarser resolution.

**Alternatives considered**: None — this is a correctness requirement.

## R5: Test Infrastructure

**Decision**: Tests that cannot load map data (e.g., proxy doesn't support callback 59) must skip, never pass silently or fail with unhandled exceptions.

**Rationale**: Per project CLAUDE.md guidelines, tests with out-of-scope failures must be marked as skipped or relaxed — never marked as passed. The existing `tryLoadGrid()` pattern already handles this for disconnections and empty arrays; it needs extension for the new callback.
