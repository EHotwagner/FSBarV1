# Implementation Plan: Live 60fps Map Visualization

**Branch**: `011-live-map-viz` | **Date**: 2026-04-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-live-map-viz/spec.md`

## Summary

Add a `LiveSession` orchestration module to FSBar.Viz that connects a headless BAR engine to the existing GameViz rendering pipeline, running the engine step loop on a background thread decoupled from the 60fps visualization window. The existing GameViz, SceneBuilder, LayerRenderer, and VizTypes modules already implement all rendering, layer switching, unit overlays, and keyboard controls — this feature provides the missing "glue" that makes it work end-to-end with a live engine.

## Technical Context

**Language/Version**: F# / .NET 10.0  
**Primary Dependencies**: FSBar.Client (in-repo), FSBar.Viz (in-repo), SkiaViewer 1.0.0, SkiaSharp 2.88.6, Silk.NET 2.22.0  
**Storage**: N/A (in-memory only, no persistence needed)  
**Testing**: xUnit 2.9.x (unit tests for LiveSession lifecycle; integration tests with live engine)  
**Target Platform**: Linux x86_64 with X11 display  
**Project Type**: Library module within FSBar.Viz  
**Performance Goals**: 60fps visualization rendering, engine steps on separate thread  
**Constraints**: SkiaSharp GPU backend unavailable (raster + GL texture upload); engine stepping blocks during callback processing  
**Scale/Scope**: Single developer tool; one engine instance at a time

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Spec-first delivery | PASS | Spec, clarifications, and plan complete |
| .fsi signature file for new public module | REQUIRED | LiveSession.fsi must be created |
| Surface-area baseline for LiveSession | REQUIRED | Must add baseline test |
| Test evidence | REQUIRED | Unit + integration tests for LiveSession lifecycle |
| Observability | REQUIRED | Structured logging for session state transitions |
| Scripting accessibility | REQUIRED | FSI prelude + example script for live viz |
| No new dependencies | PASS | Uses only existing dependencies |
| dotnet pack | PASS | FSBar.Viz already packable |

### Post-Design Re-Check

| Gate | Status | Notes |
|------|--------|-------|
| .fsi contracts defined | Addressed in Phase 2 task | LiveSession.fsi with start/stop/dispose |
| Surface-area baseline | Addressed in Phase 2 task | SurfaceBaselineTests update |
| Test evidence | Addressed in Phase 2 tasks | Unit tests (mock client) + integration tests (live engine) |
| Scripting accessibility | Addressed in Phase 2 task | Example script NN-live-viz.fsx |

## Project Structure

### Documentation (this feature)

```text
specs/011-live-map-viz/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── LiveSession.fsi          # NEW — Public API contract
├── LiveSession.fs           # NEW — Orchestration module
├── VizTypes.fsi             # EXISTING — may add LiveSessionConfig type
├── VizTypes.fs              # EXISTING — may add LiveSessionConfig type
├── GameViz.fsi              # EXISTING — no changes expected
├── GameViz.fs               # EXISTING — no changes expected
├── SceneBuilder.fsi         # EXISTING — no changes expected
├── LayerRenderer.fsi        # EXISTING — no changes expected
├── FSBar.Viz.fsproj         # EXISTING — add LiveSession to compile order
└── ...                      # All other modules unchanged

tests/FSBar.Viz.Tests/
├── LiveSessionTests.fs      # NEW — Unit tests (mock lifecycle)
├── LiveSessionIntegrationTests.fs  # NEW — Integration tests (live engine)
├── SurfaceBaselineTests.fs  # EXISTING — add LiveSession baseline
└── FSBar.Viz.Tests.fsproj   # EXISTING — add new test files

scripts/examples/
└── NN-live-viz.fsx          # NEW — FSI example script
```

**Structure Decision**: All new code lives within the existing FSBar.Viz project. One new module (LiveSession) handles orchestration. No new projects or dependencies.

## Complexity Tracking

No constitution violations. Single new module in existing project.

## Design Decisions

### 1. LiveSession Architecture

LiveSession is a stateful orchestrator with a simple lifecycle:

```
start(engineConfig, ?vizConfig) → IDisposable
  1. Create BarClient with engineConfig
  2. client.Start() — launches engine, accepts proxy connection
  3. GameViz.start(?vizConfig) — opens 60fps window
  4. GameViz.attachToClient(client) — loads initial MapGrid
  5. Spawn background thread: stepLoop()
  6. Return session (IDisposable)

stepLoop():
  while running:
    frame = client.Step()
    GameViz.onFrame(frame)
    if maxFrames reached → stop

Dispose():
  running ← false
  stepThread.Join()
  GameViz.stop()
  client.Stop()
```

### 2. Thread Model

- **Render thread**: Managed by SkiaViewer, runs at 60fps, calls SceneBuilder.drawFrame()
- **Step thread**: Background thread owned by LiveSession, calls client.Step() + GameViz.onFrame()
- **Synchronization**: GameViz already uses `lock stateLock` for all state mutations — no additional synchronization needed

### 3. Error Handling

- Engine crash / socket disconnect → catch exception in stepLoop → GameViz.setDisconnected() → log error
- Window close → SkiaViewer fires close event → LiveSession.Dispose() → stops step thread + client
- Timeout during client.Start() → propagate exception, do not open viz window

### 4. .fsi Contract (LiveSession.fsi)

```fsharp
module FSBar.Viz.LiveSession

open FSBar.Client

type LiveSessionHandle =
    interface System.IDisposable
    member FrameCount: int
    member IsRunning: bool

val start: engineConfig: EngineConfig -> vizConfig: VizConfig option -> LiveSessionHandle
val startWithClient: client: BarClient -> vizConfig: VizConfig option -> LiveSessionHandle
```

Two entry points:
- `start` — creates and manages its own BarClient (full lifecycle)
- `startWithClient` — attaches to an existing connected client (for testing / REPL use)

## Implementation Phases

### Phase 1: LiveSession Module (P1 — Core Pipeline)

1. Create `LiveSession.fsi` with public API contract
2. Create `LiveSession.fs` implementing:
   - start/startWithClient functions
   - Background step thread with error handling
   - IDisposable cleanup
   - State tracking (frame count, running flag, last error)
3. Add to FSBar.Viz.fsproj compile order (after GameViz)
4. Add LiveSessionConfig to VizTypes if needed

### Phase 2: Tests (P1 — Verification)

1. `LiveSessionTests.fs` — Unit tests:
   - Session lifecycle (start → running → dispose → stopped)
   - startWithClient with mock/offline client
   - Error handling (client throws during step)
2. `LiveSessionIntegrationTests.fs` — Integration tests with live engine:
   - Launch engine, run 100+ frames with visualization
   - Verify all layer types render (heightmap, slope, LOS, radar, resource)
   - Verify unit overlay updates
   - Verify 60fps sustained rendering
3. Update `SurfaceBaselineTests.fs` with LiveSession public API baseline

### Phase 3: Scripting & Documentation (P1 — Accessibility)

1. Create `scripts/examples/NN-live-viz.fsx` FSI script
2. Update prelude if needed for LiveSession access
3. Verify script runs end-to-end with live engine

### Phase 4: Layer Toggling & Polish (P3 — Already Done)

All keyboard layer switching (keys 1-0), overlay toggling (U/E/G/M), mouse pan/zoom, and layer labels are already implemented in GameViz.processKeyDown and SceneBuilder.drawFrame. No additional work needed unless integration testing reveals issues.
