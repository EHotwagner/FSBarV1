# Research: 011-live-map-viz

## Key Finding: FSBar.Viz Already Implements Most Functionality

The existing FSBar.Viz project already contains a near-complete live visualization system:

### Existing Capabilities (No Changes Needed)

| Capability | Module | Status |
|------------|--------|--------|
| 60fps windowed rendering | GameViz.start() via SkiaViewer | Working |
| Heightmap color rendering | LayerRenderer.renderHeightMap | Working |
| Slope map rendering | LayerRenderer.renderFloatArray | Working |
| LOS/Radar map rendering | LayerRenderer.renderIntArray | Working |
| Resource map rendering | LayerRenderer.renderIntArray | Working |
| Terrain classification | LayerRenderer.renderTerrainClassification | Working |
| Passability layers (4 types) | LayerRenderer.renderBoolArray | Working |
| Unit position overlay | SceneBuilder (blue=friendly, red=enemy) | Working |
| Event indicators | SceneBuilder (colored expanding rings) | Working |
| Metal spot overlay | SceneBuilder (gray circles) | Working |
| Economy HUD | SceneBuilder (top-right panel) | Working |
| Keyboard layer switching | GameViz.processKeyDown (keys 1-0) | Working |
| Overlay toggling | GameViz.processKeyDown (U/E/G/M) | Working |
| Mouse pan/zoom | GameViz (OnMouseScroll/OnMouseDrag) | Working |
| Bitmap caching | LayerRenderer (ConcurrentDictionary) | Working |
| Dynamic layer refresh | GameViz.onFrame (LOS/Radar each frame) | Working |
| Thread-safe state | GameViz (stateLock) | Working |
| Loading indicator / disconnect | SceneBuilder (DISCONNECTED overlay) | Working |
| MapGrid loading from engine | MapGrid.loadFromEngine | Working |

### What's Missing: Orchestration and End-to-End Proof

The existing GameViz module provides all rendering and data processing, but there is no **runnable orchestration** that:

1. Launches a headless engine
2. Connects a BarClient
3. Runs a game loop that feeds frames to GameViz at the correct cadence
4. Decouples the render loop (60fps) from the engine step rate
5. Handles graceful shutdown

The `attachToClient()` and `onFrame()` API exists, but the **game loop** that calls `client.Step()` in a background thread while the viz renders at 60fps needs to be built.

### Decision: LiveSession Module

- **Decision**: Create a `LiveSession` module in FSBar.Viz that orchestrates the engine→client→viz pipeline
- **Rationale**: GameViz handles rendering; LiveSession handles the game loop. This keeps separation of concerns and allows GameViz to remain usable with mock/offline data.
- **Alternatives considered**:
  - Putting the game loop inside GameViz → rejected, would couple rendering to engine lifecycle
  - Making it a standalone executable → rejected, user wants it in FSBar.Viz for scripting access

### Decision: Background Thread Game Loop

- **Decision**: LiveSession runs engine stepping on a background thread, decoupled from the 60fps render loop
- **Rationale**: The SkiaViewer already manages the render thread at 60fps. Engine steps may take variable time. A dedicated stepping thread ensures neither blocks the other.
- **Alternatives considered**:
  - Stepping inside the render callback → rejected, would cause frame drops during slow engine frames
  - Async/Task-based → acceptable but thread is simpler for a tight game loop

### Decision: FSI Script as Primary Entry Point

- **Decision**: Provide an FSI example script (`scripts/examples/NN-live-viz.fsx`) as the primary way to launch live visualization
- **Rationale**: Constitution requires scripting accessibility. An FSI script is the most interactive and flexible way for developers to launch and control the viz.
- **Alternatives considered**:
  - Standalone CLI executable → not required, FSI script covers the use case
  - xUnit test only → insufficient for interactive use
