# Implementation Plan: Revamp Viz Library with Declarative SkiaViewer

**Branch**: `019-revamp-viz-library` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/019-revamp-viz-library/spec.md`

## Summary

Delete the existing FSBar.Viz library and rebuild it from scratch using the revamped SkiaViewer's declarative Scene API (`IObservable<Scene>` + `IObservable<InputEvent>`). The new library replaces imperative `OnRender: SKCanvas -> ...` callbacks with declarative scene tree emission, adds visually rich rendering using SkiaSharp shaders (linear/radial gradients, Perlin noise, blur/glow image filters), animated event indicators, and gauge-style economy HUD. All modules are reimplemented with full API parity: VizTypes, ColorMaps, LayerRenderer, SceneBuilder, MapData, MockSnapshot, PreviewSession, GameViz, LiveSession. Visual tests validate rendering using all three FSBar.SyntheticData scenes (SceneA/B/C, 300 frames each).

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: SkiaViewer (latest prerelease, declarative Scene API), SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.SyntheticData (in-repo)  
**Storage**: Filesystem (MapData binary format for save/load, screenshots)  
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x  
**Target Platform**: Linux (container/desktop), GL backend (CPU raster)  
**Project Type**: Library  
**Performance Goals**: 60 FPS viewer rendering, layer switching within one frame  
**Constraints**: GL backend only (no Vulkan GPU in container); SkiaSharp GPU backend (GRContext) segfaults — use raster SKSurface + GL texture upload  
**Scale/Scope**: Single library (FSBar.Viz), single test project (FSBar.Viz.Tests), ~18 files (9 modules × .fsi + .fs)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Evidence |
|------|--------|----------|
| I. Spec-First Delivery | PASS | Spec at specs/019-revamp-viz-library/spec.md with clarifications complete |
| II. Compiler-Enforced Structural Contracts | PASS | All 9 modules will have .fsi signature files; surface-area baselines will be created |
| III. Test Evidence Is Mandatory | PASS | Visual tests planned using all 3 synthetic data scenes; each user story has independent test criteria |
| IV. Observability and Safe Failure Handling | PASS | Viewer lifecycle events (start/stop/error) will emit structured diagnostics; explicit failure on missing data (placeholder display) |
| V. Scripting Accessibility | PASS | FSI prelude + numbered examples will be provided for the new Scene-based API |
| F# exclusive stack | PASS | F# on .NET 10.0 only |
| .fsi for every public module | PASS | 9 .fsi files planned |
| Surface-area baselines | PASS | Baseline tests planned in test project |
| dotnet pack | PASS | Existing FSBar.Viz.fsproj already packable |
| Dependencies minimized | PASS | Same dependency set as before (SkiaViewer, SkiaSharp, FSBar.Client) — no new dependencies |

## Project Structure

### Documentation (this feature)

```text
specs/019-revamp-viz-library/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── public-api.md    # Module signatures overview
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── FSBar.Viz.fsproj         # Project file (same name, new content)
├── VizTypes.fsi             # Type definitions (LayerKind, OverlayKind, GameSnapshot, etc.)
├── VizTypes.fs
├── ColorMaps.fsi            # Color scheme definitions
├── ColorMaps.fs
├── LayerRenderer.fsi        # Map layer → SKBitmap rendering with caching
├── LayerRenderer.fs
├── SceneBuilder.fsi         # GameSnapshot → Scene tree composition
├── SceneBuilder.fs
├── MapData.fsi              # Binary save/load for MapGrid + metal spots
├── MapData.fs
├── MockSnapshot.fsi         # Test snapshot builders (pipeline style)
├── MockSnapshot.fs
├── PreviewSession.fsi       # Offline preview/playback via SkiaViewer
├── PreviewSession.fs
├── GameViz.fsi              # Live REPL API (thread-safe, single viewer)
├── GameViz.fs
├── LiveSession.fsi          # Engine → GameViz orchestration
├── LiveSession.fs
└── scripts/
    ├── prelude.fsx          # FSI prelude for interactive use
    └── examples/
        ├── 01-basic-scene.fsx
        ├── 02-layer-rendering.fsx
        └── 03-synthetic-playback.fsx

tests/FSBar.Viz.Tests/
├── FSBar.Viz.Tests.fsproj   # Test project (updated dependencies)
├── SurfaceBaselineTests.fs  # Surface-area baseline validation
├── VizEngineFixture.fs      # Shared test fixtures
├── LayerRendererTests.fs    # Layer bitmap rendering tests
├── MapDataTests.fs          # Binary serialization round-trip tests
├── MockSnapshotTests.fs     # Builder pattern composition tests
├── SceneBuilderTests.fs     # Scene tree construction tests
├── PreviewSessionTests.fs   # Offline preview tests
├── ViewerTests.fs           # SkiaViewer integration tests
├── GameVizIntegrationTests.fs  # Live viz integration tests
├── LiveSessionTests.fs      # Engine session tests
├── LiveSessionIntegrationTests.fs  # End-to-end live tests
└── SyntheticVizTests.fs     # NEW: Visual tests using SyntheticData scenes
```

**Structure Decision**: Same project slot (`src/FSBar.Viz`), same test project (`tests/FSBar.Viz.Tests`). Old code is deleted and replaced. File ordering in .fsproj preserved: VizTypes → ColorMaps → LayerRenderer → SceneBuilder → MapData → MockSnapshot → PreviewSession → GameViz → LiveSession.

## Complexity Tracking

No constitution violations to justify.
