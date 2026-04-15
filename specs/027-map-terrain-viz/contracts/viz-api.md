# Phase 1 — Interface Contracts

Feature: 027-map-terrain-viz

The feature touches public F# API surface in two in-repo projects:
`FSBar.Viz` and `FSBar.Client`. Constitution Principle II requires that
each public change land with an updated `.fsi` signature file and a
refreshed surface-area baseline. This document enumerates the exact
additions/changes below, grouped by file.

No protocol (`.proto`, OpenAPI) contracts change. No NuGet surface
changes outside these in-repo public signatures.

---

## `src/FSBar.Viz/VizTypes.fsi`

### Change: extend `LayerKind` discriminated union

```fsharp
[<RequireQualifiedAccess>]
type LayerKind =
    | BaseTerrain          // NEW — elevation-shaded brown/blue terrain
    | HeightMap
    | SlopeMap
    | ResourceMap
    | LosMap
    | RadarMap
    | TerrainClassification
    | Passability of MoveType
```

- Place `BaseTerrain` as the **first** variant so that DU defaulting and
  `LayerKind.BaseTerrain` pattern-match ordering in reviews reads as
  "this is the primary path".
- `HeightMap` and all other existing variants remain (FR-016).

### Change: `VizDefaults.defaultConfig` default values

No `.fsi` declaration changes for `VizDefaults` (already just lists the
three default vals). The `.fs` implementation moves to:
- `BaseLayer = LayerKind.BaseTerrain` (was `HeightMap`)
- `ActiveOverlays = Set.ofList [ OverlayKind.MetalSpots ]` (was
  whatever the current default is — default-off today)

This is a behavior change, not a signature change, but must be called
out in the PR body as an intentional public-facing default shift driven
by FR-015.

### Post-change acceptance

- `dotnet build src/FSBar.Viz` must succeed.
- Surface-area baseline for `FSBar.Viz` must be regenerated and committed.

---

## `src/FSBar.Viz/PreviewSession.fsi`

### Change: add cached-map cycling entry point

```fsharp
namespace FSBar.Viz

open FSBar.Client

/// Offline preview and playback via SkiaViewer.
module PreviewSession =
    /// Opens a viewer showing the given map grid with the default layer.
    val startWithMap: grid: MapGrid -> System.IDisposable

    /// Opens a viewer showing a single static snapshot.
    val startWithSnapshot: snapshot: GameSnapshot -> System.IDisposable

    /// Opens a viewer playing back a sequence of snapshots at the given
    /// game FPS, looping.
    val startPlayback:
        frames: GameSnapshot seq -> gameFps: int -> System.IDisposable

    /// Opens a viewer on the first map in `supportedMaps` (or
    /// `initialMapName` when provided and found in the list) loaded via
    /// `MapCacheFile.read`. Installs in-viewer next/prev keybindings
    /// (`]`/`,` to advance, `[`/`.` to retreat) that cycle through
    /// `supportedMaps` in the order supplied, wrapping at the ends.
    ///
    /// On a `MapCacheFile.LoadError` for any map, the viewer displays a
    /// formatted error banner (via `MapCacheFile.formatLoadError`) over
    /// the last successfully-loaded scene and does not advance the index;
    /// it never crashes or shows a blank window. `supportedMaps` must be
    /// non-empty — otherwise raises `System.ArgumentException`.
    val startWithCachedMaps:
        supportedMaps: MapCacheFile.SupportedMap list
     -> initialMapName: string option
     -> System.IDisposable

    /// Stops the current preview session.
    val stop: unit -> unit
```

### Semantics

- `startWithCachedMaps` internally resolves `repoRoot` via the same
  technique `MapCacheFile` tests use (walking up from `AppContext.
  BaseDirectory` until a `.specify` / `bots/` marker directory is found)
  so that the FSI script does not need to pass the path.
- Switching maps calls `LayerRenderer.invalidateAll ()` and resets the
  view to `AutoFit = true` so the next FrameTick refits the new map.
- Pan/Zoom/ResetView key bindings inside the active session (already
  handled by `processKey`) are unaffected.

### Post-change acceptance

- `FSBar.Viz.PreviewSession.startWithCachedMaps` appears in the
  surface-area baseline.
- `tests/FSBar.Viz.Tests/PreviewSessionCyclingTests.fs` exercises a
  three-element synthetic `supportedMaps` list and asserts the cycling
  index wraps correctly.

---

## `src/FSBar.Viz/LayerRenderer.fsi`

### Change: none

- `renderLayer` signature stays the same.
- Implementation adds a new match arm in `LayerRenderer.fs` and a new
  private `renderBaseTerrain` helper.
- Call this out in the PR body: **behavior extension with no signature
  change**, so the surface-area baseline stays stable for this file —
  but the build *must* still confirm the baseline did not drift.

---

## `src/FSBar.Viz/ColorMaps.fsi`

### Change: none required

- The brown-land and blue-water ramps live as private `let` bindings in
  `ColorMaps.fs`.
- `ColorMaps.colorSchemeFor` gains a `LayerKind.BaseTerrain -> <identity
  scheme>` arm but keeps the same signature.
- No `.fsi` edit; no baseline drift.

---

## `src/FSBar.Client/MapQuery.fsi`

### Change: add `metalSpotsFromResourceMap`

Appended to the existing module (placement after the current public
query functions):

```fsharp
module MapQuery =

    // ... existing functions unchanged ...

    /// Scans `grid.ResourceMap` for connected components of non-zero
    /// cells using 8-way connectivity and returns one synthetic metal
    /// spot per cluster, in `(worldX, worldY, worldZ, richness)` tuples
    /// matching the shape of `GameSnapshot.MetalSpots`.
    ///
    /// - `worldX` and `worldZ` are the cluster's elevation-independent
    ///   centroid in elmos (heightmap cells * 8).
    /// - `worldY` is the height at that centroid as read from
    ///   `grid.HeightMap` (nearest-cell lookup).
    /// - `richness` is the cluster's mean cell value normalised to
    ///   `[0, 1]` against the grid's global max resource value.
    ///
    /// Returns an empty array when the grid contains no non-zero
    /// `ResourceMap` cells. Deterministic: identical input grids always
    /// produce identical output arrays (including ordering).
    val metalSpotsFromResourceMap:
        grid: MapGrid
     -> (float32 * float32 * float32 * float32) array
```

### Semantics

- 8-connected flood fill; stable ordering (e.g., order clusters by the
  top-left-most cell position) so determinism is observable in tests.
- Pure: no I/O, no RNG, no hidden caches.

### Post-change acceptance

- `FSBar.Client.MapQuery.metalSpotsFromResourceMap` appears in the
  `FSBar.Client` surface-area baseline.
- `tests/FSBar.Client.Tests/MapQueryMetalSpotsTests.fs` contains at
  least: (a) empty `ResourceMap` → empty array, (b) single cell → single
  spot at `(x*8, height, z*8, richness)`, (c) two 3-cell clusters →
  exactly two spots, (d) determinism assertion (two calls equal).

---

## Surface-area baseline housekeeping

- `tests/FSBar.Client.Tests/baselines/FSBar.Client.baseline` (or the
  equivalent file used in this repo — whichever file the existing
  tests read) must be regenerated after the `.fsi` edits and committed
  in the same PR.
- Same for the `FSBar.Viz` baseline file(s).
- The regeneration is performed by running the existing
  `FSBar.Client.Tests` / `FSBar.Viz.Tests` suites with the
  "update baseline" flag (the mechanism established by feature 007)
  and reviewing the diff before commit.

---

## Negative surface

The following are **explicitly NOT** added to any public surface:

- `renderBaseTerrain` stays private inside `LayerRenderer.fs`.
- `brownLandRamp`, `blueWaterRamp` stay private inside `ColorMaps.fs`.
- `pulsePhase`, `updatePulsePhase`, `buildPulsingMetal` stay private
  inside `SceneBuilder.fs`.
- `MetalCluster` record stays private inside `MapQuery.fs`.
- The cycling state record stays private inside `PreviewSession.fs`.
- Any internal helper for "find repo root" stays private.

All of the above are implementation details. Public surface is exactly
the three `.fsi` additions enumerated above.
