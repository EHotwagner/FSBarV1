# Viz Debugging Session — Unit Rendering & Stability Fixes

**Date:** 2026-04-08
**Components:** FSBar.Viz (GameViz, SceneBuilder, LayerRenderer), Repl.fsx, SkiaViewer

## Issues Found & Fixed

### 1. Commander not visible on viz

**Root cause:** GameViz only tracked units from `UnitCreated` events going forward. The commander existed before the viz started (created at game start, before frame 97), so it was never added to GameViz's internal `units` map.

**Fix:** Added `GameViz.seedUnits` to populate the viz with pre-existing units. The Repl's `viz()` function now seeds GameViz from its own tracked unit state when opening the visualization.

### 2. Viz window crash after ~6 seconds

**Root cause:** `LayerRenderer` used `SKBitmap.InstallPixels` with a pinned `GCHandle`, then immediately freed the handle. The bitmap held a raw pointer to the pixel array, but after `Free()` the GC could relocate that memory. When Vulkan read the stale pixel data on a subsequent frame, the process crashed.

**Fix:** Replaced `InstallPixels` with `copyPixelsToBitmap` — a helper that copies pixel data into the bitmap's own buffer via `GetPixels()` and `Buffer.MemoryCopy`. All five render functions (heightmap, float array, int array, bool array, terrain classification) were updated.

### 3. Unit marker too small and wrong color

**Root cause:** Default `UnitMarkerSize` was 4.0f pixels — barely visible on a 1024x640 window. Friendly unit color was blue `(0, 120, 255)` which blended into water on the map.

**Fix:** Increased default marker size to 10.0f. Changed friendly color to magenta `(255, 0, 255)` and hostile color to pure red `(255, 0, 0)`.

### 4. `screenshot` function not exposed

**Root cause:** `GameViz.fsi` signature file did not include the new `screenshot` function, so it was compiled but not publicly accessible.

**Fix:** Added `val screenshot` and `val seedUnits` to `GameViz.fsi`.

### 5. `Error` DU case shadowed by `SessionState.Error`

**Root cause:** `FSBar.Client.SessionState` has an `Error` case. Since GameViz.fs and Repl.fsx open `FSBar.Client`, bare `Error` resolves to `SessionState.Error` instead of `Result.Error`.

**Fix:** Used fully qualified `Result.Error` in both GameViz.fs and Repl.fsx.

### 6. SkiaSharp native library not found in FSI

**Root cause:** SkiaSharp's P/Invoke looks for `libSkiaSharp` by short name, but the native library lives in `runtimes/linux-x64/native/`. The `dlopen` preload in Repl.fsx loads the library into the process, but .NET's DllImport resolver doesn't consult the global symbol table.

**Fix:** Created a symlink: `ln -sf runtimes/linux-x64/native/libSkiaSharp.so tests/FSBar.Viz.Tests/bin/Debug/net10.0/libSkiaSharp.so`. Note: this symlink must be recreated after `dotnet clean` / `dotnet build`.

## Files Changed

- `src/FSBar.Viz/GameViz.fs` — added `seedUnits`, `screenshot`; changed viewer type to `ViewerHandle`
- `src/FSBar.Viz/GameViz.fsi` — exposed `seedUnits`, `screenshot`
- `src/FSBar.Viz/SceneBuilder.fs` — changed unit colors (magenta/red)
- `src/FSBar.Viz/VizTypes.fs` — increased default marker size to 10.0f
- `src/FSBar.Viz/LayerRenderer.fs` — replaced `InstallPixels` with safe `copyPixelsToBitmap`
- `scripts/examples/Repl.fsx` — added `screenshot()`, seed existing units on `viz()`
- `tests/FSBar.Viz.Tests/ViewerTests.fs` — explicit `IDisposable` cast for `ViewerHandle.Dispose`
