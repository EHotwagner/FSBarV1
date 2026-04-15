# Phase 1 — Data Model

Feature: 027-map-terrain-viz

This feature is almost entirely a *rendering* change; the only new data
types are the layer-variant tag and a derived metal-spot tuple. Everything
else is an extension of existing records (`VizConfig`, `ViewState`) or
pure computed artifacts (`SKBitmap` outputs).

---

## Entity: `LayerKind.BaseTerrain` (new DU variant)

**Location**: `src/FSBar.Viz/VizTypes.fsi` — `LayerKind` discriminated union.

**Shape**: `LayerKind.BaseTerrain` (nullary; no payload).

**Relationships**:
- Consumed by `LayerRenderer.renderLayer` as one more match case.
- Consumed by `ColorMaps.colorSchemeFor` (returns an identity scheme —
  ignored by the `BaseTerrain` renderer path but required to keep the
  `renderLayer` signature stable).
- Consumed by `SceneBuilder.buildBaseLayer` (no code change — the shader
  blit is layer-agnostic).
- Stored in `VizConfig.BaseLayer`; becomes the default value of that
  field in `VizDefaults.defaultConfig` (FR-015).

**Validation rules**:
- Rendering of `BaseTerrain` requires `grid.WidthHeightmap > 0 &&
  grid.HeightHeightmap > 0`. Empty grids already fall back to a "No data"
  banner in `SceneBuilder.buildBaseLayer`; the new variant inherits that
  behavior.

**State transitions**: none (DU variant is purely declarative).

---

## Entity: `MetalCluster` (internal, transient)

**Location**: private inside
`src/FSBar.Client/MapQuery.fs::metalSpotsFromResourceMap`.

**Shape** (logical):
```
type private MetalCluster = {
    CellZs: int list    // z indices in ResourceMap
    CellXs: int list    // x indices in ResourceMap
    SumValue: int       // sum of ResourceMap cell values in cluster
    CellCount: int
}
```

**Relationships**:
- Built by 8-connected flood fill over `grid.ResourceMap`.
- Projected into the public tuple shape
  `(float32 * float32 * float32 * float32)` = `(worldX, worldY, worldZ,
  richnessNormalized)` so it can be consumed by
  `SceneBuilder.buildPulsingMetal`.

**Validation rules**:
- Non-zero cells are cluster members; cells with value `0` are gaps.
- Connectivity is 8-way.
- A single cluster produces exactly one output tuple.
- Coordinate conversion: heightmap-cell `(x, z)` → elmos `(x * 8.0f,
  grid.HeightMap[z, x], z * 8.0f)`. Rationale: `MapGrid.fsi:39-42`
  establishes the elmo/heightmap ratio and the live `SceneBuilder` path
  already converts with `/ 8.0f` (`SceneBuilder.fs:17-18`), so going the
  other direction with `* 8.0f` is the correct inverse.
- `richnessNormalized = min 1.0f (float32 SumValue / float32 CellCount /
  float32 globalMax)` where `globalMax` is the maximum `ResourceMap` cell
  value in the whole grid. If `globalMax = 0`, the function returns an
  empty array (FR-006 "every metal spot listed" — zero spots → zero output,
  not an error; US2 acceptance scenario 4).

**State transitions**: purely functional; no in-place mutation beyond the
local flood-fill worklist.

---

## Entity: Preview cycling state (extension of `PreviewSession` internals)

**Location**: private module state inside
`src/FSBar.Viz/PreviewSession.fs`. Not part of the public `.fsi` surface;
exposed only via `startWithCachedMaps` (see contracts).

**Shape** (logical):
```
type private CyclingState = {
    SupportedMaps: MapCacheFile.SupportedMap list
    CurrentIndex: int
    CurrentMapName: string
    RepoRoot: string
}
```

**Relationships**:
- Created by `PreviewSession.startWithCachedMaps`.
- Mutated by the new `[`/`]` key handlers in `processKey`.
- Drives calls to `MapCacheFile.read` followed by `currentSnapshot <-
  Some newSnap` followed by `LayerRenderer.invalidateAll ()` and
  `autoFitDone <- false` (or the post-refactor equivalent: set
  `viewState.AutoFit <- true`).

**Validation rules**:
- `SupportedMaps.Length >= 1` (guard clause in `startWithCachedMaps`;
  otherwise raise an `ArgumentException` with a clear message —
  Constitution IV, explicit failure).
- `0 ≤ CurrentIndex < SupportedMaps.Length`; index arithmetic uses
  modular `(i + 1) % n` / `(i - 1 + n) % n`.
- On a `LoadError`, `CurrentIndex` is **not** advanced; the previous map
  remains displayed and an error banner is shown over the scene.

**State transitions**:
- Initial: `CurrentIndex = findByName initialMapName |> Option.defaultValue 0`.
- On `Key.Right`/`Key.Period`: `CurrentIndex <- (CurrentIndex + 1) % n`;
  reload.
- On `Key.Left`/`Key.Comma`: `CurrentIndex <- (CurrentIndex - 1 + n) % n`;
  reload.
- On `LoadError`: no change to `CurrentIndex`; error banner posted.

---

## Entity: Shared pulse phase (extension of `SceneBuilder` internals)

**Location**: `src/FSBar.Viz/SceneBuilder.fs` — new private
`mutable pulsePhase: float32` (already mutable conventions exist for
the HUD interpolation state at `SceneBuilder.fs:10-12`, so this follows
the existing idiom).

**Shape**: `float32` in range `[0, 1]`.

**Relationships**:
- Written by the FrameTick arm of `handleInput` in both `PreviewSession`
  and `GameViz` — they call a new
  `SceneBuilder.updatePulsePhase elapsedSeconds` before `emitScene ()`.
- Read by `SceneBuilder.buildPulsingMetal` when building each metal
  marker's `alpha` and `radius` scale.

**Validation rules**:
- Must never fall to `0` when applied to alpha — FR-008 requires markers
  remain at least faintly visible at all phases. Implementation clamps
  the effective alpha to a minimum floor (e.g., `60/255`).
- Must never reach `1` when applied to the alpha of the *opaque center
  pixel* — FR-008 also requires the base layer remains at least
  partially visible underneath. Implementation bounds the alpha ceiling
  (e.g., `220/255`) and uses a radial gradient so the center is already
  non-opaque.

**State transitions**: `pulsePhase = 0.5 + 0.5 * sin(elapsedSeconds *
2π / periodSeconds)`. `periodSeconds` defaults to `1.5f`.

---

## Changes to existing entities

### `VizConfig` (`src/FSBar.Viz/VizTypes.fs`)

- No field added or removed.
- `VizDefaults.defaultConfig` changes: `BaseLayer = LayerKind.BaseTerrain`
  (was `LayerKind.HeightMap`).
- `VizDefaults.defaultConfig.ActiveOverlays` gains `OverlayKind.MetalSpots`
  by default so the pulsing markers appear without requiring the user to
  press `M` (FR-007; the user can still toggle off).
- These changes are a public-behavior change covered by FR-015 / FR-016.
  The existing `HeightMap` and other layers remain selectable via
  `Key.Number1…Number0` exactly as today; only the starting value of
  `BaseLayer` and the starting `ActiveOverlays` set move.

### `ViewState`

- No field change.
- Behavioral change: `AutoFit` is now authoritative over `autoFitDone` (see
  research R6). `computeAutoFit` no longer forces `AutoFit = false`; the
  user-Pan/Zoom paths already set `AutoFit = false` *before* updating
  view, which is the new single source of truth.

### `GameSnapshot`

- No field change. `MetalSpots` remains the live-path source; the cached
  preview path still produces a `GameSnapshot` record but fills
  `MetalSpots = [||]` so the `SceneBuilder.buildPulsingMetal` path
  consults `MapQuery.metalSpotsFromResourceMap snap.MapGrid` as a
  fallback when `MetalSpots` is empty. This preserves FR-017 visual
  parity between live and cached rendering without splitting the scene
  build into two code paths.

### `LoadedMap` (unchanged)

- `FSBar.Client.MapCacheFile.LoadedMap` already contains the `MapGrid`
  needed for the cached-preview path. No shape change.
