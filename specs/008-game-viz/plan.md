# Implementation Plan: Game State Visualization

**Branch**: `008-game-viz` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-game-viz/spec.md`

## Summary

Build an in-process, real-time 2D map visualization for FSBar game sessions. The visualization renders composited map layers (height, slope, LOS, radar, resources, terrain, passability) with unit and event overlays using Silk.NET windowing + SkiaSharp GPU rendering on a background thread. Follows the GameVizCurrent viewer and thread-safe state pattern (Silk.NET window on background thread, lock-guarded mutable state, REPL-friendly API). All controls available via both keyboard shortcuts and REPL/scripting API.

## Technical Context

**Language/Version**: F# / .NET 10.0
**Primary Dependencies**: Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Proto (in-repo)
**Storage**: N/A (in-memory only)
**Testing**: xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x (live game sessions per CLAUDE.md — no mocks)
**Target Platform**: Linux (cross-platform via Silk.NET/SkiaSharp)
**Project Type**: Library (in-process visualization, REPL-drivable)
**Performance Goals**: 10+ fps visualization updates, <1s layer switch, <3s startup
**Constraints**: Must run on background thread to keep REPL responsive; thread-safe state mutation
**Scale/Scope**: Single game session, maps up to ~32x32 km (heightmap grids up to 4096x4096)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec complete with clarifications, plan in progress |
| II. Compiler-Enforced Structural Contracts | PASS | All public modules will have `.fsi` files; surface baselines will be created |
| III. Test Evidence Is Mandatory | PASS | Live tests against running game session; each story has independent test criteria |
| IV. Observability and Safe Failure | PASS | Structured diagnostics for frame metrics (from GameVizCurrent pattern); explicit disconnection handling |
| V. Scripting Accessibility | PASS | REPL API is a core requirement; prelude.fsx update + example scripts planned |
| F# exclusive stack | PASS | All F# |
| .fsi for every public module | PASS | Planned for all modules |
| Surface area baselines | PASS | Planned |
| dotnet pack | PASS | FSBar.Viz will be packable |
| Dependencies justified | PASS | See research.md — Silk.NET + SkiaSharp are the proven stack from GameVizCurrent |

## Project Structure

### Documentation (this feature)

```text
specs/008-game-viz/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (via /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── FSBar.Proto/                    # Existing — protobuf types
├── FSBar.Client/                   # Existing — game client library
│   ├── MapGrid.fsi / .fs           # Existing — map data layers
│   ├── Events.fsi / .fs            # Existing — game events
│   ├── Callbacks.fsi / .fs         # Existing — engine queries
│   └── BarClient.fsi / .fs         # Existing — session orchestration
├── FSBar.Client.Tests/             # Existing — unit tests
└── FSBar.Viz/                      # NEW — visualization library
    ├── FSBar.Viz.fsproj
    ├── VizTypes.fsi / .fs          # Core types: VizConfig, LayerKind, ViewState, UnitState, GameSnapshot
    ├── ColorMaps.fsi / .fs         # Color gradient functions for map layers
    ├── LayerRenderer.fsi / .fs     # Converts MapGrid layers → SKBitmap (one per layer kind)
    ├── UnitRenderer.fsi / .fs      # Renders unit markers and event indicators onto canvas
    ├── SceneBuilder.fsi / .fs      # Assembles composited scene: base layer + overlays + HUD
    ├── InputHandler.fsi / .fs      # Keyboard/mouse → VizCommand (layer switch, pan, zoom, toggle)
    ├── Viewer.fsi / .fs            # Silk.NET window host on background thread (from GameVizCurrent)
    └── GameViz.fsi / .fs           # Public REPL API: run/stop, setBaseLayer, toggleOverlay, pan, zoom, etc.

tests/
├── FSBar.LiveTests/                # Existing — live integration tests
└── FSBar.Viz.Tests/                # NEW — visualization tests (live session)
    ├── FSBar.Viz.Tests.fsproj
    ├── LayerRendererTests.fs       # Verify layer bitmaps match expected data patterns
    ├── SceneBuilderTests.fs        # Verify scene composition logic
    └── GameVizIntegrationTests.fs  # End-to-end: start session → open viz → verify rendering

scripts/
├── prelude.fsx                     # UPDATED — add FSBar.Viz #r references
└── examples/
    ├── 06-game-viz-basic.fsx       # NEW — start viz during a game session
    └── 07-game-viz-layers.fsx      # NEW — demonstrate layer switching and customization
```

**Structure Decision**: New `FSBar.Viz` project alongside existing `FSBar.Client`. Separate project because it introduces GPU rendering dependencies (Silk.NET, SkiaSharp) that should not be required by consumers of the core client library. References `FSBar.Client` for game data access.

## Complexity Tracking

No constitution violations to justify.

## Design Decisions

### D1: Scene Graph vs Direct Canvas Drawing

**Decision**: Use direct SkiaSharp canvas drawing (not the v2 scene graph DSL).

**Rationale**: The v2 scene graph (Core.fs) is a general-purpose retained-mode rendering API. For game map visualization, we need to render large 2D array data as pixel bitmaps — this is fundamentally a rasterization task, not a shape-graph task. Using `SKBitmap` for base layers (rendered from Array2D data) and direct `SKCanvas` calls for overlays (unit circles, text, lines) is simpler and more performant than wrapping everything in scene graph nodes.

**What we keep from GameVizCurrent**: The Viewer.fs pattern (Silk.NET window on background thread, OpenGL + SkiaSharp GPU surface, frame loop), the thread-safe state mutation pattern, and the REPL-friendly API design. We skip the scene DSL and render module.

### D2: Layer Rendering Strategy

**Decision**: Pre-render base layers to `SKBitmap` on data change, blit to canvas each frame. Overlays (units, grid, events) drawn directly each frame.

**Rationale**: Map layers like heightmap and slope change rarely (only on initial load). LOS/radar change per-frame but are low-resolution grids that can be rendered to bitmap quickly. Pre-rendering avoids per-pixel computation each frame. Overlays are small-count draw calls (unit circles, text labels) that are cheap to render directly.

### D3: Coordinate System

**Decision**: The visualization uses heightmap grid coordinates internally. The `ViewState` tracks scale (pixels per grid cell) and origin (pixel offset for panning). Auto-fit computes scale from `window_size / grid_dimensions`.

**Rationale**: All MapGrid layers are in grid coordinates. Elmo coordinates are only used for unit positions (converted via `elmoToGrid`). This avoids constant coordinate conversion during rendering.

### D4: Input Handling

**Decision**: Silk.NET.Input for keyboard/mouse events, dispatched as `VizCommand` discriminated union values that the viewer processes. Same commands issued by the REPL API.

**Rationale**: Unified command model means keyboard and REPL trigger identical code paths. No separate logic for each input source.

### D5: Cross-Platform Consideration

**Decision**: Target `net10.0` (not `net10.0-windows` like GameVizCurrent v2). Skip the Win32 `timeBeginPeriod` high-resolution timer call.

**Rationale**: FSBarV1 targets Linux. Silk.NET and SkiaSharp are cross-platform. The high-res timer is Windows-only and not needed for 10fps target.
