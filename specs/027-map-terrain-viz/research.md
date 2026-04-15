# Phase 0 — Research & Decisions

Feature: 027-map-terrain-viz
Spec: [spec.md](./spec.md)
Plan: [plan.md](./plan.md)

This document records the decisions that resolve open design questions
before implementation. The spec's own Clarifications section handled user-
facing ambiguity (layer integration, cycling mechanism, interactivity).
Everything below is implementation-level.

---

## R1 — Where does `BaseTerrain` live in the layer system?

**Decision**: Add `BaseTerrain` as a new variant of the existing
`LayerKind` discriminated union in `src/FSBar.Viz/VizTypes.fsi`, and handle
it in `LayerRenderer.renderLayer` via a new private `renderBaseTerrain`
function that returns an `SKBitmap` exactly like the existing
`renderFloatArray`/`renderTerrainClassification` cases.

**Rationale**:
- The layer system is *not* a registry; it's DU dispatch in
  `LayerRenderer.renderLayer` (`src/FSBar.Viz/LayerRenderer.fs:137`). A new
  variant is the minimum-churn idiom.
- Existing cacheability logic already hinges on `isDynamic`
  (`LayerRenderer.fs:29`). `BaseTerrain` is static given a `MapGrid`, so it
  naturally joins the cached branch and satisfies SC-006 determinism for
  free.
- `SceneBuilder.buildBaseLayer` is layer-agnostic — it calls
  `LayerRenderer.renderLayer` and blits whatever bitmap comes back as an
  `Shader.Image`. No changes needed there for the base image itself.

**Alternatives considered**:
- *Separate `BaseTerrainRenderer` module outside `LayerRenderer`.* Rejected:
  would require changing `SceneBuilder.buildBaseLayer` to special-case the
  new layer, fragmenting the single dispatch point.
- *Build the base image as two stacked layers (heightmap + brown filter +
  heightmap + blue filter).* Rejected: can't express the per-cell land/water
  split through `ColorScheme: float32 -> SKColor`, which is a single scalar
  input.

---

## R2 — How does the brown/blue dual ramp fit the existing `ColorScheme` model?

**Decision**: The new `renderBaseTerrain` function **ignores** the
`ColorScheme` argument passed to `renderLayer` and uses two internal ramps
hardcoded in `ColorMaps.fs` (`brownLandRamp`, `blueWaterRamp`, each a
`float32 -> SKColor` scaled to `[0,1]`). `ColorMaps.colorSchemeFor
LayerKind.BaseTerrain` returns an identity/placeholder scheme for API
uniformity only. Per-map min/max elevation scaling happens inside
`renderBaseTerrain`, not in the scheme.

**Rationale**:
- `ColorScheme` is fundamentally `float32 -> SKColor`, a single-input
  mapping. The brown/blue split needs *which ramp* (a sign decision) before
  any ramp normalization. Trying to overload `ColorScheme` for this would
  either change its type (ripple across all layers) or smuggle the sign
  bit into the scalar (ugly, breaks other schemes).
- FR-005 requires per-map rescaling of both ramps to each map's actual
  min/max. That requires a two-pass walk over `HeightMap` (min/max → color
  lookup) which the existing `renderFloatArray` already does for its single
  ramp. Duplicating that pattern for two sub-ranges (land and water) is
  trivial and keeps the rendering loop local.
- Tuning the palette later is a single-file change in `ColorMaps.fs`.

**Alternatives considered**:
- *Extend `ColorScheme` to a discriminated union with a "dual ramp with
  split point" variant.* Rejected: invasive and adds a polymorphic branch to
  every render loop for zero win elsewhere.
- *Let the scheme carry the ramps as two closures in a new record field.*
  Rejected: same invasiveness, and still doesn't solve min/max-per-sub-range
  rescaling inside the scheme function signature.

---

## R3 — How are metal spots derived in the cached (no-live-game) path?

**Decision**: Add a new public helper
`FSBar.Client.MapQuery.metalSpotsFromResourceMap: MapGrid -> (float32 * float32 * float32 * float32) array`
that scans `grid.ResourceMap` for connected components of non-zero cells
using 8-connected flood fill, and emits one spot per component. The spot's
position is the elevation-aware centroid of the component's cells in
**world/elmo units** so it matches the `GameSnapshot.MetalSpots` tuple
shape already rendered by `SceneBuilder` (`(x, y, z, richness)`). The `y`
value is looked up from `HeightMap` at the centroid. `richness` is the
average cell value of the cluster normalized to `[0,1]` by the grid's max
cell value (defaulting to `0.5f` if the grid has zero metal — unused case).

**Rationale**:
- The cache does **not** store discrete metal spot positions as a
  separate field. It stores `MapGrid.ResourceMap: int[,]` at heightmap
  resolution (`MapGrid.fsi:52`). Metal spots in live play come from the
  `Callbacks.getMetalSpots` call (`GameViz.fs:164`), which is unavailable
  in the preview path.
- 8-connected clustering matches BAR's standard "metal patch" geometry
  (extractor blobs are always contiguous groups of 1–5 cells, occasionally
  bigger).
- Matching the live tuple shape means `SceneBuilder.buildPulsingMetal` can
  consume the same `(float32 * float32 * float32 * float32) array` from
  both sources with one code path. Satisfies FR-017 visual consistency for
  free.
- `MapQuery` already holds this family of primitive queries (it is referenced
  by `CLAUDE.md`'s "Map analysis caching" note), so the helper belongs
  there rather than inside `FSBar.Viz`, keeping the cache-analysis primitives
  decoupled from rendering.

**Alternatives considered**:
- *Hardcode metal spot coordinates per-map in the cache file.* Rejected:
  requires re-running `MapCacheFile.write` and bumping `codeVersion`; out of
  scope for this feature (the cache already has what's needed).
- *Render one marker per non-zero `ResourceMap` cell.* Rejected: metal
  patches span multiple adjacent cells; this would produce dozens of
  overlapping pulses per real spot, violating SC-004 (the viewer's visible
  count must match the real spot count).
- *Use `FSBar.Client.Chokepoints` or `BasePlan` helpers.* Rejected: those
  solve different problems (navigation and build placement); neither emits
  metal-spot centroids.

---

## R4 — How is the pulse animation driven?

**Decision**: Thread `InputEvent.FrameTick.elapsedSeconds` (a `float`
already emitted by SkiaViewer — verified at
`SkiaViewer/src/SkiaViewer/Scene.fsi:229`) into scene assembly through a
shared mutable `pulsePhase: float32` stored in `SceneBuilder` (or passed
through `buildScene`). Compute one sinusoidal phase per frame:
`phase = 0.5f + 0.5f * sin(elapsed * 2π / period)`. The period defaults to
`1.5f` seconds (tweakable; per spec assumption 1–2 s is acceptable). The
phase drives both the marker's alpha (`base + phase * delta`) and radius
(`baseR + phase * deltaR`) so the pulse is visible as both brightness and
size. Alpha floor is non-zero (FR-008): markers never fully occlude the
underlying pixel and also never fully vanish.

**Rationale**:
- `PreviewSession.handleInput` and `GameViz.handleInput` already hit the
  `InputEvent.FrameTick _` arm on every frame to emit a scene. Reading
  `elapsedSeconds` there and passing it into the scene build is a
  one-parameter plumbing change, no new timers or observables.
- Using a sinusoid yields a continuously smooth curve that meets FR-007
  (steady cadence, visibly rising and falling) without extra easing code.
- Marker opacity + radius double-encoding gives strong readability even
  for colorblind users and for the "metal on the shoreline" edge case
  where hue contrast is lowest.

**Alternatives considered**:
- *Use wall-clock `DateTime.UtcNow` in `SceneBuilder` directly.* Rejected:
  breaks determinism of headless scene tests (we want to assert that two
  different elapsed values produce different marker alphas).
- *Use an external `Timer`.* Rejected: duplicates the FrameTick plumbing
  already in place.

---

## R5 — How does the `.fsx` entry script cycle maps inside a running viewer?

**Decision**: Add a new public `PreviewSession.startWithCachedMaps`:

```
val startWithCachedMaps:
    supportedMaps: MapCacheFile.SupportedMap list
 -> initialMapName: string option
 -> IDisposable
```

This function owns the cycling state. Internally it:
1. Resolves the initial map via `MapCacheFile.tryFindSupportedMap` (falling
   back to `List.head supportedMaps` when the name is `None`).
2. Calls `MapCacheFile.read` using `MapCacheFile.cachePathFor repoRoot`
   and surfaces any `LoadError` via `formatLoadError` into an on-screen
   error banner instead of crashing (FR-010, Constitution IV).
3. Stores the current index, the ordered supportedMaps list, and a
   "load-by-index" closure in `PreviewSession`'s private state.
4. Adds two new arms to `processKey`:
   `Key.Right` / `Key.Period` → advance index mod N, reload, reset
   `autoFitDone` to `false` so the next frame refits the new map.
   `Key.Left` / `Key.Comma` → retreat index mod N, same reload path.

The `.fsx` script becomes a ~15-line wrapper:

```fsharp
#load "../prelude.fsx"
open FSBar.Client
open FSBar.Viz
let args = fsi.CommandLineArgs
let initial = if args.Length > 1 then Some args.[1] else None
use _ = PreviewSession.startWithCachedMaps MapCacheFile.supportedMaps initial
System.Console.ReadLine() |> ignore
```

**Rationale**:
- `PreviewSession` already owns the key-dispatch table in `processKey`
  (`PreviewSession.fs:52`), the AutoFit state, and the Scene emitter. Adding
  `[`/`]` handlers there keeps input handling in one place.
- Constitution V asks for a minimal, ergonomic `.fsx` shell. Fifteen lines
  with one explicit call is about as minimal as this can get.
- Putting `supportedMaps` as a parameter (rather than hard-wiring
  `MapCacheFile.supportedMaps` inside `PreviewSession`) keeps the layer
  decoupled and makes cycling unit-testable with synthetic lists.

**Alternatives considered**:
- *Let the `.fsx` subscribe to `Viewer.run`'s input observable directly and
  call `PreviewSession.swapGrid` for key presses.* Rejected: requires
  exposing the viewer's input stream publicly, fragmenting the key-handling
  contract. Every other key still routes through `PreviewSession`, so
  adding these two arms there is the consistent move.
- *A separate `PreviewBrowser` module that wraps `PreviewSession`.*
  Rejected: the wrapping would essentially duplicate the `handleInput`
  dispatch just to add two arms. Not worth the module boundary.
- *Relaunch the viewer on each map switch.* Rejected: violates FR-011 and
  SC-005 (sub-3 s switch, no restart).

---

## R6 — Why does WindowResize currently not re-run AutoFit, and how is that fixed?

**Observation (not a decision yet)**: `PreviewSession.fs:113-115` and
`GameViz.fs:107-108` both just update `WindowWidth`/`WindowHeight` on
`InputEvent.WindowResize` without re-evaluating `computeAutoFit`. The
current `autoFitDone` flag is set to `true` after the first autofit and
never reset, so even the "still in autofit mode" case (`viewState.AutoFit =
true`) wouldn't re-trigger.

**Decision**: In both files, change the `WindowResize(w, h)` arm to:
1. Update `WindowWidth`/`WindowHeight` (unchanged).
2. If `viewState.AutoFit = true`, call `computeAutoFit` on the current
   `snap.MapGrid`. This re-fits so the full map still fills the resized
   window.
3. If `viewState.AutoFit = false`, leave `Scale`/`OriginX`/`OriginY`
   untouched — the user explicitly panned/zoomed and should keep their view.

**Rationale**:
- FR-009a is explicit: window resize must re-run auto-fit while still
  enabled, and must preserve `Scale`/`OriginX`/`OriginY` otherwise.
- `autoFitDone` the flag is redundant with `viewState.AutoFit`. Consolidate
  on `viewState.AutoFit` as the authoritative predicate. `autoFitDone` can
  be removed in the same pass (internal cleanup, no public API change).
- `computeAutoFit` already sets `AutoFit = false` at the end of its current
  implementation (`PreviewSession.fs:32`) — this is actually a bug for
  FR-009a (the re-fit path should keep it `true`). Fix in the same pass:
  `computeAutoFit` now takes an explicit `keepAutoFit: bool` argument; the
  first-frame path passes `true`, the `ResetView` path passes `true`, and
  user Pan/Zoom paths continue to force `false` *before* calling
  `computeAutoFit` (they don't call it at all).

**Alternatives considered**:
- *Leave `autoFitDone` in place for the first-frame path and only fix
  `WindowResize`.* Rejected: keeps two sources of truth (`autoFitDone` and
  `viewState.AutoFit`), which is how this bug existed in the first place.
  Simplifying is cheap and within scope.

---

## R7 — Cache invalidation when the user switches maps

**Decision**: On map switch (the `[`/`]` handlers), call
`LayerRenderer.invalidateAll ()` (already public) before replacing the
current snapshot. The current implementation keys its `SKBitmap` cache by
layer kind only (`LayerRenderer.fs:14`) — it does **not** key by map name.
Without invalidation, the viewer would show Map A's terrain under Map B's
marker overlay after a switch.

**Rationale**:
- `invalidateAll` is already called on `PreviewSession.stop`
  (`PreviewSession.fs:159`), which proves the API and pattern are intended
  for exactly this kind of whole-session flush.
- Alternative (keying bitmaps by grid identity) would be a larger change
  to `LayerRenderer` and is out of scope.
- The reset takes ~milliseconds for the supported map sizes, so it comfort-
  ably fits inside SC-005's 3 s map-switch budget.

**Alternatives considered**:
- *Per-grid cache keys.* Rejected: larger refactor; this feature doesn't
  need it.

---

## R8 — Determinism guarantees for the terrain bitmap

**Decision**: `renderBaseTerrain` is pure over `(MapGrid, window size)`
inputs — no wall clock, no RNG, no global state — which gives SC-006 for
free. The test plan verifies this by calling `renderBaseTerrain` twice on
the same synthetic grid and asserting byte-for-byte equality of the
resulting `SKBitmap` pixel buffers (the same technique the existing
`LayerRenderer` tests use for the heightmap layer).

**Rationale**:
- The SC-006 contract ("same cache file ⇒ visually identical terrain,
  only pulse phase varies") matches exactly how static layers already
  behave in this codebase. Keeping `renderBaseTerrain` pure is the
  smallest design choice that honors the guarantee.

**Alternatives considered**: none — this is the standard pattern.

---

## Summary of all NEEDS CLARIFICATION

There are **zero** unresolved `NEEDS CLARIFICATION` entries after this
research pass. The spec's open questions were resolved in its own
Clarifications section on 2026-04-15. All implementation-level uncertainties
are decided above.
