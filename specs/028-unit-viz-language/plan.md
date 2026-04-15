# Implementation Plan: Unit Visual Representation for SkiaViewer

**Branch**: `028-unit-viz-language` | **Date**: 2026-04-15 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/028-unit-viz-language/spec.md`

## Summary

Replace the current radial-gradient dot renderer in `FSBar.Viz.SceneBuilder` with an information-dense unit visual language that encodes identity and state through orthogonal visual channels. The permanent layer carries movement class (shape), team (fill), faction (stroke color), tier (stroke width), facing (pip), HP (arc), build progress (dashed + alpha), and a unique 2-char label derived at build time from `BarData.AllUnits`. Four sticky-toggle overlays (`W L C N`) compose on top. MVP consumes `FSBar.SyntheticData` snapshots only; live-game wiring is a follow-up. Two new public modules are added to `FSBar.Viz` (`UnitGlyph`, `UnitGlyphPalettes`) with full `.fsi` contracts and surface-area baselines. A build-time generator (`scripts/gen-unit-labels.fsx`) emits a committed `UnitLabels.generated.fs` table.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: Existing in-repo — `FSBar.Viz` (`SceneBuilder`, `VizTypes`, `ColorMaps`), `FSBar.Client` (`GameState`, `UnitDefCache`), `FSBar.SyntheticData` (`Scenes`, `SceneTypes`), `SkiaViewer` 1.1.3-dev (declarative `Scene`), `SkiaSharp` 2.88.6, `BarData` (local nupkg feed), `xUnit 2.9.x`. **No new NuGet dependencies.**
**Storage**: Filesystem. One build-time artifact committed to the repo: `src/FSBar.Viz/UnitLabels.generated.fs` — byte-stable mapping from `BarData` unit internal name to 2- or 3-char code. No runtime storage.
**Testing**: xUnit 2.9.x in `tests/FSBar.Viz.Tests`. Surface-area baseline via existing `SurfaceBaselineTests`. Deterministic synthetic-fixture scenes from `FSBar.SyntheticData.Scenes` drive renderer tests; no live engine.
**Target Platform**: Linux desktop under SkiaViewer (raster `SKSurface` + GL texture upload per CLAUDE.md viz notes). No GPU-backend dependency.
**Project Type**: F# library add-on to the existing `FSBar.Viz` desktop application module.
**Performance Goals**: SC-004 — ≥ 30 fps for a 200-unit scene in the standard SkiaViewer window with permanent layer + up to 3 concurrent overlays active.
**Constraints**:
- Permanent-layer draw call budget: ≤ ~10 Scene primitives per unit (shape + stroke + pip + arc + label + optional damage-shader filter) to meet SC-004 on a 200-unit scene.
- Label-table regeneration must be deterministic and minimize churn across `BarData` versions per SC-006 (≥ 95% preserved).
- No new NuGet packages; all capabilities come from existing deps.
- Must not break existing `SceneBuilder.buildScene` callers in `LiveSession` / `PreviewSession` — old `OverlayKind.Units` legacy path is retained during transition behind a config flag and removed only after the new renderer reaches feature parity.

**Scale/Scope**:
- `BarData.AllUnits` catalog ~953 entries.
- MVP surface area: 2 new public modules in `FSBar.Viz` + 1 generated file + 1 extended type in `VizTypes`.
- MVP tests: ~6 xUnit cases (label-generator determinism, shape-rule dispatch, tier/faction derivation, scene composition smoke, overlay toggle composition, surface-area baseline).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|---|---|---|
| **I. Spec-First Delivery** | ✅ PASS | `spec.md` + this plan; user stories P1–P3 with independent test criteria; FR-001..FR-025 traceable to story acceptance scenarios. Clarifications session recorded. |
| **II. Compiler-Enforced Structural Contracts** | ✅ PASS | Two new public modules (`UnitGlyph`, `UnitGlyphPalettes`) ship with `.fsi` signature files sketched under `contracts/`. Generated `UnitLabels.generated.fs` is internal and accessed only through `UnitGlyph`'s public API. `VizTypes.fsi` is extended (additive) to carry the new `UnitDisplay` record; the surface-area baseline under `tests/FSBar.Viz.Tests/Baselines/` is updated as part of implementation. |
| **III. Test Evidence Is Mandatory** | ✅ PASS | Tests planned for (a) label-generator determinism + uniqueness, (b) shape-rule dispatch per movement class, (c) tier/faction derivation with fallbacks, (d) scene composition smoke across synthetic scenes, (e) overlay toggle independence, (f) surface-area baseline. Each maps to a story acceptance scenario. |
| **IV. Observability and Safe Failure Handling** | ✅ PASS | Per-unit resolution misses (unknown movementClass, missing tier, unknown faction, unknown defId) emit structured one-shot log entries on first occurrence. Renderer never crashes on missing fields — it draws a fallback glyph and continues. Events are explicit and actionable (logs name the unit). |
| **V. Scripting Accessibility** | ✅ PASS | `scripts/prelude.fsx` in `FSBar.Viz` is extended to expose `UnitGlyph.buildSceneForScene` so a synthetic scene can be rendered interactively from FSI with a single `#load`. A new example script `scripts/examples/NN-unit-glyph.fsx` demonstrates shape/color/tier dispatch on fixtures. |
| **Engineering Constraint — packable** | ✅ PASS | `FSBar.Viz.fsproj` remains packable via `dotnet pack`. No new projects. |
| **Engineering Constraint — dependencies minimized** | ✅ PASS | No new NuGet packages. Uses existing `SkiaSharp` path rendering for all new shapes. |

**Result**: All gates pass. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/028-unit-viz-language/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── UnitGlyph.fsi           # Public renderer API contract
│   ├── UnitGlyphPalettes.fsi   # Faction/team palette contract
│   └── VizTypes.delta.fsi      # Additive delta to existing VizTypes.fsi
├── checklists/
│   └── requirements.md  # From /speckit.specify
└── tasks.md             # From /speckit.tasks (NOT created here)
```

### Source Code (repository root)

```text
src/
├── FSBar.Viz/
│   ├── UnitGlyph.fsi            # NEW — public renderer API
│   ├── UnitGlyph.fs             # NEW — shape dispatch, stroke, pip, HP arc, label placement
│   ├── UnitGlyphPalettes.fsi    # NEW — faction + team color tables
│   ├── UnitGlyphPalettes.fs     # NEW
│   ├── UnitLabels.generated.fs  # NEW — committed generator output
│   ├── VizTypes.fsi             # EXTENDED — add UnitDisplay, MovementShape, Tier, FactionId, LabelCode
│   ├── VizTypes.fs              # EXTENDED — mirror
│   ├── SceneBuilder.fsi         # UNCHANGED public surface (new path added behind extended config flag)
│   ├── SceneBuilder.fs          # MODIFIED — new `buildUnitsGlyph` replaces legacy `buildUnits` when flag set
│   └── scripts/
│       ├── prelude.fsx          # EXTENDED — load new module
│       ├── gen-unit-labels.fsx  # NEW — build-time label-table generator
│       └── examples/
│           └── NN-unit-glyph.fsx # NEW — FSI demo
├── FSBar.SyntheticData/
│   ├── SceneTypes.fsi           # UNCHANGED — existing scenes already expose Units with DefId + Health
│   └── UnitSim.fs               # EXTENDED (optional) — add synthetic heading + buildProgress to scene-generated units so the new renderer has data to display
└── FSBar.Client/
    └── GameState.fsi            # UNCHANGED for MVP (live-game wiring is a follow-up feature)

tests/
└── FSBar.Viz.Tests/
    ├── UnitGlyphTests.fs              # NEW — shape-rule dispatch, tier, faction, label uniqueness
    ├── UnitGlyphSceneTests.fs         # NEW — scene composition on synthetic scenes + overlay composition
    ├── UnitLabelsGeneratorTests.fs    # NEW — determinism, fallback to 3 chars, stability across re-runs
    └── Baselines/
        └── FSBar.Viz.baseline         # UPDATED — reflects UnitGlyph/UnitGlyphPalettes additive surface
```

**Structure Decision**: Extend the existing `FSBar.Viz` project in place. No new project. Two new public modules (`UnitGlyph`, `UnitGlyphPalettes`) with `.fsi` contracts; one generated internal file (`UnitLabels.generated.fs`) consumed only through `UnitGlyph`. `VizTypes` gains an additive `UnitDisplay` record so the renderer decouples from `FSBar.Client.TrackedUnit` (which lacks heading/buildProgress) and lets `FSBar.SyntheticData` populate the extra fields for MVP. The legacy `OverlayKind.Units` path in `SceneBuilder.fs:128` is retained behind a feature flag in `VizConfig` and removed in a follow-up once the new renderer is the default on every consumer.

## Complexity Tracking

No Constitution Check violations — this section intentionally empty.
