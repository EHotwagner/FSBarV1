# Implementation Plan: Map & GameState Preview via SkiaViewer

**Branch**: `010-map-gamestate-preview` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-map-gamestate-preview/spec.md`

## Summary

Add offline map preview and mock game state rendering to FSBar.Viz. Implement binary serialization for MapGrid data (save/load to disk), mock GameSnapshot builder helpers, and a PreviewSession module that renders saved map data and mock/animated game states via SkiaViewer at a fixed 60fps — all without requiring a live game engine.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: SkiaViewer 1.0.0, SkiaSharp 2.88.6, FSBar.Client (in-repo), FSBar.Viz (in-repo)  
**Storage**: Binary files on disk for MapGrid serialization  
**Testing**: xUnit 2.9.3 + Microsoft.NET.Test.Sdk 17.14.1 (existing FSBar.Viz.Tests project)  
**Target Platform**: Linux x86_64, X11 (DISPLAY=:0)  
**Project Type**: Library (visualization component)  
**Performance Goals**: 60fps render, <1s save/load for 512x512 maps  
**Constraints**: No game engine required for preview tests; existing rendering pipeline (SceneBuilder, LayerRenderer, ColorMaps) must work identically with saved/mock data  
**Scale/Scope**: Maps up to 512x512 heightmap resolution (~2MB height data)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| I. Spec-First Delivery | PASS | Spec exists with user stories and acceptance criteria |
| II. Compiler-Enforced Structural Contracts | PASS | New public modules will need `.fsi` files and baselines |
| III. Test Evidence Is Mandatory | PASS | Tests will use saved map data and mock snapshots — no engine required |
| IV. Observability and Safe Failure Handling | PASS | Load errors will be explicit (file not found, corrupt data) |
| V. Scripting Accessibility | PASS | MapData save/load and MockSnapshot builders will be FSI-friendly |

**Engineering Constraints**:
- F# exclusive: PASS
- `.fsi` files: New modules (MapData, MockSnapshot, PreviewSession) will need `.fsi` files
- Surface baselines: Will be added for new modules
- Dependencies: No new external dependencies (binary serialization uses System.IO)

## Project Structure

### Documentation (this feature)

```text
specs/010-map-gamestate-preview/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── MapData.fsi          # NEW: MapGrid save/load public API
├── MapData.fs           # NEW: Binary serialization for MapGrid + metal spots
├── MockSnapshot.fsi     # NEW: GameSnapshot builder helpers
├── MockSnapshot.fs      # NEW: Programmatic snapshot construction
├── PreviewSession.fsi   # NEW: Offline preview session management
├── PreviewSession.fs    # NEW: SkiaViewer-based preview with playback
├── VizTypes.fsi/.fs     # Existing (no changes)
├── ColorMaps.fsi/.fs    # Existing (no changes)
├── LayerRenderer.fsi/.fs # Existing (no changes)
├── SceneBuilder.fsi/.fs  # Existing (no changes)
├── GameViz.fsi/.fs      # Existing (no changes)
└── FSBar.Viz.fsproj     # Updated: add new compile entries

tests/FSBar.Viz.Tests/
├── MapDataTests.fs      # NEW: Save/load round-trip tests
├── MockSnapshotTests.fs # NEW: Builder and rendering tests
├── PreviewSessionTests.fs # NEW: Preview session lifecycle tests
├── ViewerTests.fs       # Existing (no changes)
├── SurfaceBaselineTests.fs # Updated: add new module baselines
└── Baselines/
    ├── MapData.baseline    # NEW
    ├── MockSnapshot.baseline # NEW
    └── PreviewSession.baseline # NEW
```

**Structure Decision**: Three new modules in FSBar.Viz. MapData handles serialization, MockSnapshot provides builder helpers, PreviewSession orchestrates the viewer. No new projects needed.

## Implementation Approach

### MapData Module (FR-001, FR-002, FR-009)

Save and load MapGrid + metal spots using `BinaryWriter`/`BinaryReader` with a simple format:

```
[magic: 4 bytes "FSMG"]
[version: int32]
[widthHeightmap: int32]
[heightHeightmap: int32]
[heightMap: float32[] flattened row-major, (w+1)*(h+1) elements]
[slopeMap: float32[] flattened, (w/2)*(h/2) elements]
[resourceMap: int32[] flattened, w*h elements]
[losMap: int32[] flattened, w*h elements]
[radarMap: int32[] flattened, w*h elements]
[metalSpotCount: int32]
[metalSpots: (float32*float32*float32*float32)[] x count]
```

- Magic bytes for format identification
- Version field for future compatibility
- WidthElmos/HeightElmos derived from WidthHeightmap * 8 (not stored)
- Validation on load: dimension checks, magic byte verification

### MockSnapshot Module (FR-004)

Builder functions for constructing GameSnapshot records:

- `emptySnapshot: MapGrid -> GameSnapshot` — creates a snapshot with the given map and empty units/events
- `withUnits: UnitState list -> GameSnapshot -> GameSnapshot` — adds units
- `withEnemyAt: float32 * float32 * float32 -> GameSnapshot -> GameSnapshot` — adds an enemy unit
- `withFriendlyAt: float32 * float32 * float32 -> GameSnapshot -> GameSnapshot` — adds a friendly unit
- `withEvent: EventKind -> float32 * float32 * float32 -> int -> GameSnapshot -> GameSnapshot` — adds an event indicator
- `withEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot` — sets metal economy
- `withEnergyEconomy: float32 -> float32 -> float32 -> float32 -> GameSnapshot -> GameSnapshot` — sets energy economy
- `withMetalSpots: (float32 * float32 * float32 * float32) array -> GameSnapshot -> GameSnapshot` — sets metal spots
- `withFrame: int -> GameSnapshot -> GameSnapshot` — sets frame number

### PreviewSession Module (FR-003, FR-005, FR-006, FR-007, FR-008)

Orchestrates preview rendering via SkiaViewer:

- `startWithMap: MapGrid -> IDisposable` — starts a viewer showing a static map
- `startWithSnapshot: GameSnapshot -> IDisposable` — starts a viewer with a full snapshot
- `startPlayback: GameSnapshot seq -> int -> IDisposable` — plays back a sequence at specified game-fps (viewer always renders at 60fps, advancing game state at the configured rate)
- Reuses existing VizConfig, ViewState, SceneBuilder.drawFrame pipeline
- Handles keyboard/mouse input for pan, zoom, layer switching, overlay toggling (same as GameViz)

## Complexity Tracking

No constitution violations. All work stays within existing project structure using existing dependencies.
