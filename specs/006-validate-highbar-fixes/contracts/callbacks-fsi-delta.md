# Contract Delta: Callbacks.fsi

**Date**: 2026-04-06

## New Public API

```fsharp
/// Get the corners heightmap as a flat float32 list (row-major order).
/// Returns (mapWidth+1)*(mapHeight+1) vertex-resolution height values.
val getCornersHeightMap: stream: NetworkStream -> float32 list
```

## Unchanged Public API

All existing signatures in `Callbacks.fsi` remain unchanged. `getHeightMap` is retained for backward compatibility.
