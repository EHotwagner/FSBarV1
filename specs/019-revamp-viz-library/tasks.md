# Tasks: Revamp Viz Library with Declarative SkiaViewer

**Input**: Design documents from `/specs/019-revamp-viz-library/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — spec explicitly requests "create real live visual tests with the synthetic data."

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Delete old FSBar.Viz code and create new project skeleton with correct dependencies and file ordering.

- [x] T001 Delete all existing source files in src/FSBar.Viz/ (all .fs, .fsi files — keep directory)
- [x] T002 Delete all existing test files in tests/FSBar.Viz.Tests/ (all .fs files — keep directory)
- [x] T003 Create new src/FSBar.Viz/FSBar.Viz.fsproj with SkiaViewer (*-*), SkiaSharp 2.88.6, FSBar.Client project reference, FSBar.SyntheticData project reference, and compile order: VizTypes → ColorMaps → LayerRenderer → SceneBuilder → MapData → MockSnapshot → PreviewSession → GameViz → LiveSession (each .fsi before .fs)
- [x] T004 Create new tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj with xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x, FSBar.Viz project reference, FSBar.SyntheticData project reference, and compile order for all test files
- [x] T005 [P] Create src/FSBar.Viz/VizTypes.fsi with all type definitions: LayerKind, OverlayKind, EventKind, ColorScheme, ViewState, VizConfig, UnitState, EventIndicator, EconomyData, GameSnapshot, VizCommand, VizDefaults module — signatures unchanged from contracts/public-api.md
- [x] T006 [P] Create src/FSBar.Viz/VizTypes.fs implementing all types and VizDefaults module with default values

**Checkpoint**: Project compiles with `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj` (VizTypes only)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement utility modules shared across all user stories. These MUST be complete before any story-specific work.

**CRITICAL**: No user story work can begin until this phase is complete.

- [x] T007 [P] Create src/FSBar.Viz/ColorMaps.fsi with signatures: grayscale, terrain, heatMap, binary, colorSchemeFor
- [x] T008 [P] Create src/FSBar.Viz/ColorMaps.fs implementing color schemes with lerp interpolation (terrain: blue→green→brown→white, heatMap: blue→yellow→red, binary: red/green threshold, grayscale: black→white)
- [x] T009 [P] Create src/FSBar.Viz/MapData.fsi with signatures: save, load (binary format with "FSMG" magic bytes)
- [x] T010 [P] Create src/FSBar.Viz/MapData.fs implementing binary save/load with FSMG magic, version 1, float32/int32 arrays, metal spots — identical format to previous version
- [x] T011 [P] Create src/FSBar.Viz/MockSnapshot.fsi with signatures: emptySnapshot, withUnits, withFriendlyAt, withEnemyAt, withEvent, withEconomy, withEnergyEconomy, withMetalSpots, withFrame
- [x] T012 [P] Create src/FSBar.Viz/MockSnapshot.fs implementing builder pattern with composable pipeline functions and auto-incrementing UnitId

### Tests for Foundational Phase

- [x] T013 [P] Create tests/FSBar.Viz.Tests/MapDataTests.fs testing save/load round-trip, magic byte validation, version checking, truncation error handling
- [x] T014 [P] Create tests/FSBar.Viz.Tests/MockSnapshotTests.fs testing builder composition, unit auto-ID, event/economy builders

- [x] T014b [P] Create tests/FSBar.Viz.Tests/ColorMapsTests.fs testing: each scheme (grayscale, terrain, heatMap, binary) maps 0.0f→expected low color and 1.0f→expected high color, colorSchemeFor returns correct scheme per LayerKind, all schemes handle boundary values (0.0f, 0.5f, 1.0f) without exception

**Checkpoint**: Foundation ready — `dotnet build` succeeds, MapData, MockSnapshot, and ColorMaps tests pass with `dotnet test`

---

## Phase 3: User Story 1 — Terrain Map Visualization with Visual Richness (Priority: P1) MVP

**Goal**: Render terrain base layers (height, slope, resource, LOS, radar, passability) using shader-enhanced bitmaps via the declarative Scene API.

**Independent Test**: Generate synthetic map grid, call SceneBuilder.buildScene, verify Scene contains Element.Image with correct dimensions and that each LayerKind produces a distinct bitmap.

### Implementation for User Story 1

- [x] T015 [P] [US1] Create src/FSBar.Viz/LayerRenderer.fsi with signatures: renderLayer, invalidateCache, invalidateAll, cacheStats
- [x] T016 [P] [US1] Create src/FSBar.Viz/LayerRenderer.fs implementing map data → SKBitmap rendering with ConcurrentDictionary caching, GCHandle pixel transfer, normalization pipelines (float→[0..1], int→[0..max], bool→0/1), and per-LayerKind render functions (renderHeightMap, renderFloatArray, renderIntArray, renderBoolArray, renderTerrainClassification). Cache static layers; always re-render LOS/Radar.
- [x] T017 [US1] Create src/FSBar.Viz/SceneBuilder.fsi with signature: val buildScene: GameSnapshot -> VizConfig -> ViewState -> Scene
- [x] T018 [US1] Create src/FSBar.Viz/SceneBuilder.fs implementing base layer scene building: compute viewport Transform from ViewState (translate + scale), render base layer bitmap via LayerRenderer, wrap in Element.Image inside a Group with viewport transform. Handle empty/missing layer data by emitting Element.Text "No data" centered on background. Include placeholder groups for overlays (Units, Events, Grid, MetalSpots, EconomyHud) that return empty element lists when not yet implemented.

### Tests for User Story 1

- [x] T019 [P] [US1] Create tests/FSBar.Viz.Tests/LayerRendererTests.fs testing: HeightMap/SlopeMap/ResourceMap/Passability rendering produces non-null SKBitmap with correct dimensions, color scheme application changes output colors, cache hit/miss counting works, invalidateAll clears cache
- [x] T020 [P] [US1] Create tests/FSBar.Viz.Tests/SceneBuilderTests.fs testing base layer: buildScene with HeightMap returns Scene with Element.Image in viewport Group, switching LayerKind changes the rendered bitmap, empty MapGrid produces "No data" text element, Scene.BackgroundColor matches VizConfig

**Checkpoint**: LayerRenderer produces bitmaps for all layer kinds, SceneBuilder builds Scene with base layer. All US1 tests pass.

---

## Phase 4: User Story 2 — Unit and Event Overlay with Animations (Priority: P1)

**Goal**: Render unit markers with RadialGradient shaders (cyan=friendly, red=enemy) and animated event indicators (expanding rings, fading glow, combat effects).

**Independent Test**: Create snapshot with units and events, call buildScene for consecutive frames, verify Scene contains Ellipse elements with RadialGradient paint for units and that event element properties (radius, opacity) change across frames.

### Implementation for User Story 2

- [x] T021 [US2] Extend src/FSBar.Viz/SceneBuilder.fs to implement unit overlay: for each UnitState in snapshot, emit Element.Ellipse at (posX/8, posZ/8) map coordinates with RadialGradient shader paint — cyan center→transparent edge for friendly (IsEnemy=false), red center→transparent edge for enemy (IsEnemy=true). Size from VizConfig.UnitMarkerSize. Health bar as small Rect below each unit. Apply OverlayOpacity from config. Only render when Units in ActiveOverlays.
- [x] T022 [US2] Extend src/FSBar.Viz/SceneBuilder.fs to implement event overlay: for each EventIndicator, compute animation progress as (currentFrame - FrameCreated) / DurationFrames clamped to [0..1]. UnitCreated: expanding ring (Ellipse with increasing radius, fading opacity). UnitDestroyed: contracting ring with blur (MaskFilter.Blur). EnemySpotted: pulsing diamond shape (Path). Combat: glowing circle with RadialGradient + ImageFilter.Blur. Skip expired indicators (progress >= 1.0). Only render when Events in ActiveOverlays.

### Tests for User Story 2

- [x] T023 [P] [US2] Add tests to tests/FSBar.Viz.Tests/SceneBuilderTests.fs for unit overlay: snapshot with 2 friendly + 1 enemy unit produces 3 Ellipse elements in unit overlay Group, friendly Ellipses have cyan-based RadialGradient paint, enemy Ellipses have red-based RadialGradient, empty units map produces empty Group
- [x] T024 [P] [US2] Add tests to tests/FSBar.Viz.Tests/SceneBuilderTests.fs for event animations: snapshot with UnitCreated event at frame 0 rendered at frames 0, 5, 10 produces Ellipse elements with increasing radius and decreasing opacity; expired event (frame > FrameCreated + DurationFrames) produces no elements; Combat event produces element with ImageFilter.Blur paint

**Checkpoint**: Units render with gradient markers, events animate across frames. All US2 tests pass.

---

## Phase 5: User Story 3 — Economy HUD with Animated Gauges (Priority: P2)

**Goal**: Render economy HUD in screen-space (no viewport transform) with metal and energy bar gauges using LinearGradient fill, Perlin noise background texture, and low-resource warning state.

**Independent Test**: Create snapshots with different economy values, call buildScene, verify HUD Group contains Rect elements with LinearGradient paint and Text elements showing values.

### Implementation for User Story 3

- [x] T025 [US3] Extend src/FSBar.Viz/SceneBuilder.fs to implement economy HUD: add a screen-space Group (no viewport transform) positioned bottom-right. Background Rect with PerlinNoiseFractalNoise shader for texture. Metal bar: Rect with LinearGradient (silver→white) fill, width proportional to Current/Storage. Energy bar: Rect with LinearGradient (yellow→orange) fill, width proportional to Current/Storage. Text labels showing "M: current/storage +income -usage" and "E: current/storage +income -usage". Low-resource warning: when Current/Storage < 0.1, bar fill changes to red gradient and Text color shifts to red. Only render when EconomyHud in ActiveOverlays. Add smooth interpolation: maintain internal mutable previous economy display values; on each frame, lerp current display values toward snapshot values by a smoothing factor (e.g., 0.15f per frame tick). This produces smooth bar transitions instead of jumps between snapshots.

### Tests for User Story 3

- [x] T026 [US3] Add tests to tests/FSBar.Viz.Tests/SceneBuilderTests.fs for economy HUD: snapshot with economy data produces HUD Group with Rect and Text elements, metal bar width scales with Current/Storage ratio, low-resource state (Current near zero) produces red-tinted elements, HUD not rendered when EconomyHud not in ActiveOverlays

**Checkpoint**: Economy HUD renders with gauge bars and labels. All US3 tests pass.

---

## Phase 6: User Story 4 — Interactive Viewer with Declarative Scene Pipeline (Priority: P2)

**Goal**: Wire SceneBuilder output to SkiaViewer via IObservable<Scene>, handle InputEvent for pan/zoom/layer switching, implement PreviewSession and GameViz modules.

**Independent Test**: Launch viewer with synthetic snapshot, programmatically trigger zoom/pan/layer switch, capture screenshots, verify viewport changes.

### Implementation for User Story 4

- [x] T027 [P] [US4] Create src/FSBar.Viz/PreviewSession.fsi with signatures: startWithMap, startWithSnapshot, startPlayback, stop — unchanged from contracts/public-api.md
- [x] T028 [P] [US4] Create src/FSBar.Viz/GameViz.fsi with all signatures: start, stop, attachToClient, seedUnits, onFrame, setDisconnected, resetView, setBaseLayer, toggleOverlay, enableOverlay, disableOverlay, setConfig, updateConfig, setColorScheme, setMarkerSize, setOverlayOpacity, toggleGridLines, pan, zoom, screenshot — unchanged from contracts/public-api.md
- [x] T029 [US4] Create src/FSBar.Viz/PreviewSession.fs implementing offline preview: maintain mutable config/viewState/snapshot state protected by stateLock. Create Event<Scene> and subscribe SkiaViewer via Viewer.run. On each FrameTick from InputEvent observable, rebuild Scene via SceneBuilder.buildScene and trigger event. Handle InputEvent.KeyDown for layer switching (1-0, U/E/G/M/H/Home), InputEvent.MouseScroll for zoom (1.1x/0.9x factor centered on cursor), InputEvent.MouseDown/MouseMove for pan drag. Playback: index into snapshot array using elapsed time × gameFps, modulo length for looping. Compute AutoFit on first frame.
- [x] T030 [US4] Create src/FSBar.Viz/GameViz.fs implementing live REPL API: maintain mutable state (config, viewState, snapshot, viewer, clientRef, mapGridRef, units, indicators) protected by stateLock. On start(): create Event<Scene>, call Viewer.run, subscribe to InputEvent observable for keyboard/mouse handling. On onFrame(): process GameEvents (UnitCreated/Finished/Destroyed, EnemyEnterLOS/LeaveLOS, UnitDamaged→Combat), update units map, refresh LOS/RadarMap, query economy, prune expired indicators, build GameSnapshot, call SceneBuilder.buildScene, trigger sceneEvent. Screenshot delegates to ViewerHandle.Screenshot. All public functions thread-safe via lock stateLock.
- [x] T030b [US4] Add structured diagnostics to src/FSBar.Viz/GameViz.fs and src/FSBar.Viz/PreviewSession.fs: emit eprintfn log lines on viewer start/stop, engine attach/detach, onFrame error, LOS/Radar refresh failure, and setDisconnected. Include context (frame number, error message). Add same to src/FSBar.Viz/LiveSession.fs for stream subscribe/complete/error events.
- [x] T031 [US4] Extend src/FSBar.Viz/SceneBuilder.fs to implement grid overlay: when Grid in ActiveOverlays, emit Line elements at GridLineSpacing intervals in semi-transparent white across the map dimensions. Apply viewport transform via parent Group.
- [x] T032 [US4] Extend src/FSBar.Viz/SceneBuilder.fs to implement metal spots overlay: when MetalSpots in ActiveOverlays, emit Ellipse elements at each metal spot position (x/8, z/8) with RadialGradient shader sized by richness value. Apply OverlayOpacity.
- [x] T033 [US4] Extend src/FSBar.Viz/SceneBuilder.fs to implement disconnected overlay: when GameSnapshot.Connected = false, emit centered Text element "DISCONNECTED" in large red font over a semi-transparent dark overlay covering the full viewport.

### Tests for User Story 4

- [x] T034 [P] [US4] Create tests/FSBar.Viz.Tests/PreviewSessionTests.fs testing: startWithSnapshot opens viewer without exception, startWithMap renders base layer, startPlayback advances frames over time, stop disposes cleanly, keyboard layer switching changes config
- [x] T035 [P] [US4] Create tests/FSBar.Viz.Tests/ViewerTests.fs testing: SkiaViewer Scene observable receives scenes, start/stop cycles (10x), screenshot capture produces file on disk, mouse scroll and drag produce viewport changes
- [x] T036 [P] [US4] Create tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs testing: start without exception, setBaseLayer changes rendered layer, toggleOverlay enables/disables overlay groups, pan/zoom modify ViewState, setColorScheme invalidates cache, screenshot produces file

**Checkpoint**: Interactive viewer opens, responds to input, renders all overlays. All US4 tests pass.

---

## Phase 7: User Story 5 — Synthetic Data Playback with Animated Timeline (Priority: P3)

**Goal**: Play back 300-frame synthetic scenes with full visual rendering, implement LiveSession for engine integration, create comprehensive visual tests with all three SyntheticData scenes.

**Independent Test**: Start playback of SceneA, capture screenshots at frame 0/150/299, verify content differs (animation progresses). Run all 300 frames of all 3 scenes without errors.

### Implementation for User Story 5

- [x] T037 [P] [US5] Create src/FSBar.Viz/LiveSession.fsi with LiveSessionHandle type (IDisposable, FrameCount, IsRunning, LastError) and signatures: start, startWithClient — unchanged from contracts/public-api.md
- [x] T038 [US5] Create src/FSBar.Viz/LiveSession.fs implementing engine orchestration: start() creates BarClient, calls client.Start(), calls GameViz.start() + GameViz.attachToClient(), subscribes to client.Frames observable, on each frame calls GameViz.onFrame() and increments FrameCount. On stream completion/error: sets IsRunning=false, calls GameViz.setDisconnected(). Dispose stops engine, unsubscribes, closes viz.
- [x] T039 [US5] Create tests/FSBar.Viz.Tests/VizEngineFixture.fs with shared test fixture: helper to convert FSBar.Client.GameState (from SyntheticData) to FSBar.Viz.GameSnapshot — maps TrackedUnit→UnitState, TrackedEnemy→UnitState(IsEnemy=true), EconomySnapshot→EconomyData. Helper to generate MapGrid from scene dimensions. Shared setup for viewer-based tests (DISPLAY, XDG_RUNTIME_DIR env vars).

### Visual Tests for User Story 5

- [x] T040 [US5] Create tests/FSBar.Viz.Tests/SyntheticVizTests.fs with live visual tests:
  - Test per scene (SceneA, SceneB, SceneC): generate scene via Scenes.generate, convert all 300 GameState frames to GameSnapshots, start PreviewSession.startPlayback at 30 FPS, wait 2 seconds, capture screenshot, verify file exists and is non-empty, stop cleanly
  - Layer switching test: start preview with SceneA frame 0, switch through all LayerKinds (HeightMap→SlopeMap→...→Passability), capture screenshot after each switch, verify all screenshots are non-empty
  - Animation progression test: render SceneB frames 100-105 (mid-combat) individually via buildScene, verify event overlay elements change between frames (different radius/opacity values)
  - Full playback smoke test: play each scene for 300/30=10 seconds, verify no exceptions, verify FrameCount advances, verify playback loops (FrameCount > 300)
  - Economy HUD test: render SceneA frames 0 and 200, verify HUD Group present with different bar widths reflecting economy changes

### Engine Integration Tests

- [x] T041 [P] [US5] Create tests/FSBar.Viz.Tests/LiveSessionTests.fs testing: LiveSessionHandle creation, FrameCount/IsRunning/LastError properties, Dispose stops cleanly
- [x] T042 [P] [US5] Create tests/FSBar.Viz.Tests/LiveSessionIntegrationTests.fs testing with real engine (skip if engine unavailable): start with EngineConfig, verify FrameCount increments, startWithClient with existing BarClient, verify disconnection handling

**Checkpoint**: All 3 synthetic scenes play back without errors, visual tests pass, LiveSession orchestrates correctly.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: FSI scripting accessibility, surface-area baselines, and documentation.

- [x] T043 [P] Create src/FSBar.Viz/scripts/prelude.fsx with #r references to FSBar.Viz + FSBar.SyntheticData DLLs, dlopen for native libs, helper functions for quick scene preview (convertToSnapshot, previewScene, playScene)
- [x] T044 [P] Create src/FSBar.Viz/scripts/examples/01-basic-scene.fsx demonstrating SceneBuilder.buildScene with a MockSnapshot
- [x] T045 [P] Create src/FSBar.Viz/scripts/examples/02-layer-rendering.fsx demonstrating LayerRenderer with each LayerKind and custom ColorSchemes
- [x] T046 [P] Create src/FSBar.Viz/scripts/examples/03-synthetic-playback.fsx demonstrating PreviewSession.startPlayback with SyntheticData SceneA
- [x] T047 Create tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs with surface-area baseline tests for all 9 public modules: verify each .fsi signature matches expected public API surface via reflection
- [x] T048 Run full test suite with `dotnet test tests/FSBar.Viz.Tests/` and fix any failing tests
- [x] T049 Run `dotnet pack src/FSBar.Viz/FSBar.Viz.fsproj` and verify .nupkg is produced in nupkg/ directory

**Checkpoint**: All tests pass, FSI scripts load without errors, NuGet package builds successfully.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - US1 (Phase 3) and US2 (Phase 4) are both P1 but US2 extends SceneBuilder from US1
  - US3 (Phase 5) extends SceneBuilder, can start after US1
  - US4 (Phase 6) depends on SceneBuilder (US1+US2+US3 ideally complete)
  - US5 (Phase 7) depends on US4 (PreviewSession/GameViz needed for playback tests)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1** (P1): Can start after Foundational — no dependencies on other stories
- **US2** (P1): Depends on US1 (extends SceneBuilder.fs)
- **US3** (P2): Depends on US1 (extends SceneBuilder.fs), independent of US2
- **US4** (P2): Depends on US1 (SceneBuilder needed for viewer), benefits from US2+US3
- **US5** (P3): Depends on US4 (PreviewSession/GameViz needed)

### Within Each User Story

- .fsi files before .fs files (compiler contract first)
- SceneBuilder extensions build on prior extensions
- Tests validate each increment independently

### Parallel Opportunities

- T005/T006 (VizTypes .fsi/.fs) can run in parallel
- T007-T012 (Foundational modules) can all run in parallel (different files)
- T013/T014 (Foundational tests) can run in parallel
- T015/T016 (LayerRenderer .fsi/.fs) can run in parallel with each other
- T027/T028 (PreviewSession/GameViz .fsi files) can run in parallel
- T034/T035/T036 (US4 tests) can run in parallel
- T037 (LiveSession .fsi) can run in parallel with T039 (fixture)
- T041/T042 (LiveSession tests) can run in parallel
- T043-T046 (scripts/examples) can all run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch .fsi and .fs for LayerRenderer in parallel:
Task: "Create LayerRenderer.fsi in src/FSBar.Viz/LayerRenderer.fsi"
Task: "Create LayerRenderer.fs in src/FSBar.Viz/LayerRenderer.fs"

# After SceneBuilder implementation, launch tests in parallel:
Task: "Create LayerRendererTests.fs in tests/FSBar.Viz.Tests/LayerRendererTests.fs"
Task: "Create SceneBuilderTests.fs in tests/FSBar.Viz.Tests/SceneBuilderTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (delete old, create project skeleton + VizTypes)
2. Complete Phase 2: Foundational (ColorMaps, MapData, MockSnapshot)
3. Complete Phase 3: User Story 1 (LayerRenderer + SceneBuilder base layer)
4. **STOP and VALIDATE**: Run LayerRenderer and SceneBuilder tests
5. Base terrain rendering works — foundation for all visual features

### Incremental Delivery

1. Setup + Foundational → Project compiles, utility modules work
2. US1 (terrain layers) → Base map renders with shader-enhanced bitmaps
3. US2 (unit/event overlays) → Animated markers and effects on map
4. US3 (economy HUD) → Complete game information display
5. US4 (interactive viewer) → Full interactive preview/viz experience
6. US5 (playback + live) → Full synthetic playback and engine integration
7. Polish → Scripts, baselines, packaging

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- The spec explicitly requests visual tests — all test tasks are included
- SceneBuilder.fs is extended incrementally across US1→US2→US3→US4 (same file, sequential tasks)
