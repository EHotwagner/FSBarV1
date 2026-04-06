# Contract Delta: MapQuery.fsi

**Date**: 2026-04-06

## Modified Public API

`slopeAtElmo` signature is unchanged but its internal behavior changes:
- Bounds check uses slope map dimensions `(w/2, h/2)` instead of heightmap dimensions
- Grid index conversion uses `x / 16, z / 16` instead of `x / 8, z / 8`

`terrainAtElmo` internally calls both heightmap and slope lookups — the slope lookup must use the new slope-map coordinates.

## No Signature Changes

All existing function signatures in `MapQuery.fsi` remain identical. The changes are behavioral only, affecting how slope data is indexed.
