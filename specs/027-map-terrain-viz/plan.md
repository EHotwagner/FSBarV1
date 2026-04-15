# Implementation Plan: Map Terrain Visualization Rework

**Branch**: `027-map-terrain-viz` | **Date**: 2026-04-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/027-map-terrain-viz/spec.md`

## Summary

Introduce a new `LayerKind.BaseTerrain` in `FSBar.Viz` that renders land on a
deep→light brown gradient and water on a deep→light blue gradient straight
from `MapGrid.HeightMap`, then animates every non-zero cluster in
`MapGrid.ResourceMap` (plus `GameSnapshot.MetalSpots` when present) as a
pulsing marker on top. Make `BaseTerrain` the default base layer for both
`PreviewSession` and `LiveSession`, while keeping the existing raw `HeightMap`
(and all other layers) selectable as debug/alternate views. Add an `.fsx`
entry script that loads a cached map by name via `MapCacheFile.read` and
wires a keybind inside the running `PreviewSession` to cycle through
`MapCacheFile.supportedMaps` in place. Honor `VizCommand.Pan`/`Zoom`/
`ResetView` with `AutoFit` re-running on window resize while still enabled.

**Technical approach**: Extend the existing discriminated-union layer dispatch
in `LayerRenderer.renderLayer` with a new `renderBaseTerrain` case that emits
a per-map deterministic `SKBitmap`. Keep the existing `SceneBuilder` shader
blit unchanged. Move metal-marker drawing out of the `OverlayKind.MetalSpots`
path for preview-from-cache and into a `SceneBuilder.buildPulsingMetal` that
consumes `InputEvent.FrameTick`'s elapsedSeconds to drive a shared pulse
phase, and sources spot positions from both `GameSnapshot.MetalSpots` (live)
and a new `MapQuery.metalSpotsFromResourceMap` helper (cached). Fix the
known `WindowResize` autofit gap in both `PreviewSession.handleInput` and
`GameViz.handleInput`. Ship as a new `FSBar.Client.MapQuery` helper plus
`FSBar.Viz` changes plus one new `.fsx` example.

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: Existing in-repo only — `FSBar.Client` (`MapCacheFile`, `MapGrid`, `MapQuery`), `FSBar.Viz` (`LayerRenderer`, `SceneBuilder`, `PreviewSession`, `GameViz`, `ColorMaps`, `VizTypes`), `SkiaViewer` 1.1.3-dev (`InputEvent.FrameTick`, `Scene`, `Shader.Image`), `SkiaSharp` 2.88.6, `xUnit 2.9.x`. **No new NuGet dependencies.**
**Storage**: Filesystem read-only — reads cached `bots/trainer/map-cache/<map>.json` files via `MapCacheFile.read`. No new on-disk formats.
**Testing**: `xUnit 2.9.x` unit tests in `tests/FSBar.Viz.Tests` for deterministic `renderBaseTerrain` output; `tests/FSBar.Client.Tests` for `metalSpotsFromResourceMap`. Live-viewer visual acceptance via the new `.fsx` quickstart is the UI check (Constitution III — automated tests for the logic, manual viz for the image).
**Target Platform**: Linux x64 (Arch dev container); graphical (windowed) SkiaViewer session for the `.fsx` entry script; headless unit-test rendering via `SKBitmap`.
**Project Type**: F# library + example scripts inside an existing multi-project solution.
**Performance Goals**: Base viz fully rendered within 3 s of selecting a map on a developer workstation (SC-001); map-to-map switch inside a running viewer within 3 s (SC-005); pulse runs at SkiaViewer's target 60 fps without changing existing viz throughput.
**Constraints**: Deterministic terrain output — same cache input + same window size must produce pixel-identical terrain bitmaps (SC-006). No live game engine, AI process, or network connection may be required to render the cached base viz (SC-007). Public API surface changes in `FSBar.Viz` and `FSBar.Client` must ship with matching `.fsi` updates and surface baseline refresh (Constitution II).
**Scale/Scope**: One new layer variant; one new public helper in `FSBar.Client.MapQuery`; one new cycling API in `PreviewSession`; one new `.fsx` entry script; ~3–4 new unit tests; no protocol or contract changes. Currently one supported cached map (`Avalanche 3.4`); the new `.fsx` must cycle cleanly through the `supportedMaps` list even when it is length 1.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Spec-First Delivery

- **Pass.** This plan maps 1:1 to `specs/027-map-terrain-viz/spec.md` — User Stories US1 (P1, base terrain), US2 (P2, pulsing metal), US3 (P3, cached map browsing) → Phase 1 deliverables. FR-001…FR-017 are covered explicitly in the data model and contracts below. Three open questions were resolved via Clarifications 2026-04-15 inside the spec (layer integration, map selection mechanism, interactivity/autofit).
- **Tier classification**: Tier 1 — adds a public `LayerKind` variant (`BaseTerrain`), a new public `MapQuery.metalSpotsFromResourceMap` function, a new public `PreviewSession` cycling entry point, and changes the default `BaseLayer` for both sessions (observable behavior already specified by FR-015). Full artifact chain required.

### II. Compiler-Enforced Structural Contracts

- **Pass with obligations.** Every touched public module is `.fsi`-gated. This plan inventories the exact `.fsi` files to update and requires corresponding surface-area baseline refreshes:
  - `src/FSBar.Viz/VizTypes.fsi` — add `LayerKind.BaseTerrain`.
  - `src/FSBar.Viz/PreviewSession.fsi` — add the cached-map cycling entry point.
  - `src/FSBar.Client/MapQuery.fsi` — add `metalSpotsFromResourceMap`.
  - `src/FSBar.Viz/LayerRenderer.fsi` — **no signature change** (`renderLayer` stays the only public entry point) but implementation adds a case; call out in PR body so reviewers do not miss the behavior extension.
  - `src/FSBar.Viz/ColorMaps.fsi` — may expose new scheme(s) if the brown/blue ramps end up public; otherwise kept internal.
  - Surface-area baselines: `tests/FSBar.Client.Tests/baselines/*.baseline` and `tests/FSBar.Viz.Tests/baselines/*.baseline` (if present) must be regenerated and committed in the same PR.

### III. Test Evidence Is Mandatory

- **Pass.** Each user story has an automated verification path:
  - US1 (P1 terrain): `tests/FSBar.Viz.Tests/BaseTerrainRenderingTests.fs` — feed synthetic `MapGrid` values (all-land, all-water, mixed, flat, extreme), call `LayerRenderer.renderLayer grid LayerKind.BaseTerrain scheme`, assert determinism (two calls same hash), assert pixel color bands (e.g. elevation 0 is blue/brown boundary, lowest cell is darkest blue, highest cell is lightest brown) and per-map ramp scaling.
  - US2 (P2 pulsing metal): `tests/FSBar.Client.Tests/MapQueryMetalSpotsTests.fs` — feed synthetic `ResourceMap` int grids, assert exact spot count and centroid coordinates; zero-metal case returns empty. Pulse animation is verified by a headless scene-build test that calls `SceneBuilder.build` with two different `FrameTick` elapsed values and asserts the metal marker opacity/radius differs between frames.
  - US3 (P3 map cycling): a `PreviewSession` unit test that drives the cycling API with a stub of `MapCacheFile.read` results (constructed `MapGrid` in memory) and asserts that the active snapshot's map name updates and `autoFitDone` resets. Acceptance scenarios 3.1–3.3 end-to-end are verified by the `.fsx` quickstart being runnable by a human (documented as a manual-verification step in the PR because the viewer is graphical).

### IV. Observability and Safe Failure Handling

- **Pass.** All failures use the already-structured `MapCacheFile.LoadError` pipeline + `MapCacheFile.formatLoadError`. The new `.fsx` and the new cycling entry point MUST call `formatLoadError` and surface the result in a visible text element inside the viewer (an `Scene.text` banner over black) instead of crashing or showing a blank window. This satisfies FR-010. Additionally, `eprintfn` diagnostics (matching the existing `[PreviewSession] ...` style) must announce map load, map switch, and load failures.

### V. Scripting Accessibility

- **Pass.** The feature ships a new numbered example under `src/FSBar.Viz/scripts/examples/NN-base-terrain-cache.fsx` that (a) loads via `#load "../prelude.fsx"`, (b) accepts an initial map name from `fsi.CommandLineArgs`, (c) opens the cached map in `PreviewSession`, (d) documents the next/prev keybinding inside the script's header comment so it doubles as living documentation. The existing `prelude.fsx` is already adequate for this; no changes required.

### Gate Result

- **PASS** — all five principles satisfied; no complexity justifications required. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/027-map-terrain-viz/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── viz-api.md       # Phase 1 output — .fsi surface changes + semantics
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── FSBar.Client/
│   ├── MapQuery.fsi                     # + metalSpotsFromResourceMap
│   └── MapQuery.fs                      # + implementation
├── FSBar.Viz/
│   ├── VizTypes.fsi                     # + LayerKind.BaseTerrain
│   ├── VizTypes.fs                      # + LayerKind.BaseTerrain, default BaseLayer := BaseTerrain
│   ├── LayerRenderer.fs                 # + renderBaseTerrain case in renderLayer
│   ├── ColorMaps.fs                     # + internal brown & blue ramps
│   ├── SceneBuilder.fs                  # + pulse phase from FrameTick, cached-path metal source
│   ├── PreviewSession.fsi               # + startWithCachedMap cycling entry
│   ├── PreviewSession.fs                # + cycling state, next/prev keys, AutoFit-on-resize fix
│   ├── GameViz.fsi                      # (no signature change)
│   ├── GameViz.fs                       # + AutoFit-on-resize fix, pulse phase wiring
│   └── scripts/
│       └── examples/
│           └── 04-base-terrain-cache.fsx  # new entry script (numbering TBD at task time)
tests/
├── FSBar.Client.Tests/
│   └── MapQueryMetalSpotsTests.fs       # + new file
└── FSBar.Viz.Tests/
    ├── BaseTerrainRenderingTests.fs     # + new file
    └── PreviewSessionCyclingTests.fs    # + new file
```

**Structure Decision**: Extend the existing three-project layout in place
(`FSBar.Client`, `FSBar.Viz`, corresponding `tests/*Tests` projects). No new
project. No new directories. The `.fsx` entry script lives alongside the
existing `scripts/examples/*.fsx` per Constitution V.

## Complexity Tracking

No constitutional violations; this table is intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| _(none)_  | _(n/a)_    | _(n/a)_                             |
