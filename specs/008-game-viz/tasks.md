# Tasks: Game State Visualization

**Input**: Design documents from `/specs/008-game-viz/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: Included — constitution mandates test evidence for behavior-changing code. Tests run against live game sessions (no mocks).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the FSBar.Viz project, add dependencies, establish build

- [x] T001 Create FSBar.Viz project: `dotnet new classlib -lang F# -o src/FSBar.Viz` targeting net10.0, add PackageReferences for Silk.NET.Windowing 2.22.0, Silk.NET.OpenGL 2.22.0, Silk.NET.Input 2.22.0, SkiaSharp 2.88.6, and ProjectReference to FSBar.Client in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T002 Create FSBar.Viz.Tests project: `dotnet new xunit -lang F# -o tests/FSBar.Viz.Tests` targeting net10.0, add ProjectReference to FSBar.Viz and PackageReferences for xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x in tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
- [x] T003 Verify build: `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj` succeeds with all dependencies resolved

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core types and rendering infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 [P] Create VizTypes.fsi defining public API surface: LayerKind, OverlayKind, ColorScheme, ViewState, VizConfig, UnitState, EventIndicator, EventKind, GameSnapshot, EconomyData, VizCommand discriminated unions and records in src/FSBar.Viz/VizTypes.fsi
- [x] T005 [P] Create VizTypes.fs implementing all types from VizTypes.fsi with default values (defaultConfig, defaultViewState) in src/FSBar.Viz/VizTypes.fs
- [x] T006 [P] Create ColorMaps.fsi defining public API: built-in color schemes (grayscale, terrain, heatMap, binary) and colorSchemeFor default mapping per LayerKind in src/FSBar.Viz/ColorMaps.fsi
- [x] T007 [P] Create ColorMaps.fs implementing color gradient functions: grayscale (black→white), terrain (blue→green→brown→white for height), heatMap (blue→yellow→red for slope/LOS), binary (red/green for passability) in src/FSBar.Viz/ColorMaps.fs
- [x] T008 Create Viewer.fsi defining public API: ViewerConfig record, run (starts Silk.NET window on background thread returning IDisposable), stop in src/FSBar.Viz/Viewer.fsi
- [x] T009 Create Viewer.fs implementing Silk.NET window host on background thread with OpenGL + SkiaSharp GPU surface (following GameVizCurrent Prototype/Library.fs pattern): window creation, GL context, GRContext, SKSurface lifecycle, framebuffer resize handling, render callback that invokes a scene-drawing function, in src/FSBar.Viz/Viewer.fs
- [x] T010 Update FSBar.Viz.fsproj Compile item list to include all .fsi/.fs pairs in correct order: VizTypes, ColorMaps, Viewer in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T011 Verify foundational build: `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj` compiles cleanly with .fsi conformance

**Checkpoint**: Foundation ready — core types defined, viewer window can open and render. User story implementation can now begin.

---

## Phase 3: User Story 1 — View Live Map During Game Session (Priority: P1) 🎯 MVP

**Goal**: Open a visualization window that renders the current map terrain (height/water/cliffs) in real time, updating each game frame. Auto-fits map to window, handles resize.

**Independent Test**: Start a headless game session, open the viz, confirm a recognizable terrain map appears and updates as frames progress.

### Implementation for User Story 1

- [x] T012 [P] [US1] Create LayerRenderer.fsi defining public API: renderLayer (MapGrid → LayerKind → ColorScheme → SKBitmap), invalidateCache, cacheStats in src/FSBar.Viz/LayerRenderer.fsi
- [x] T013 [P] [US1] Create SceneBuilder.fsi defining public API: drawFrame (SKCanvas → GameSnapshot → VizConfig → ViewState → unit) that composites base layer bitmap onto canvas with scale/origin transform in src/FSBar.Viz/SceneBuilder.fsi
- [x] T014 [P] [US1] Create GameViz.fsi defining public API for US1 scope: start (VizConfig option → unit), stop (unit → unit), attachToClient (BarClient → unit), onFrame (GameFrame → unit), setDisconnected (unit → unit), resetView (unit → unit) in src/FSBar.Viz/GameViz.fsi
- [x] T015 [US1] Create LayerRenderer.fs implementing Array2D → SKBitmap conversion: for each grid cell, normalize value to [0..1], apply ColorScheme to get SKColor, write pixels via SKBitmap.SetPixel or LockPixels + Marshal.Copy for performance. Cache bitmaps keyed by (LayerKind, data hash). Implement HeightMap layer first (height values → terrain color scheme) in src/FSBar.Viz/LayerRenderer.fs
- [x] T016 [US1] Create SceneBuilder.fs implementing drawFrame: compute auto-fit scale (windowWidth / gridWidth), translate canvas by origin, draw base layer SKBitmap scaled to fill window, draw "Disconnected" overlay text when snapshot.Connected = false in src/FSBar.Viz/SceneBuilder.fs
- [x] T017 [US1] Create GameViz.fs implementing thread-safe state management (lock-guarded mutable VizConfig, ViewState, GameSnapshot), start function that launches Viewer.run with a render callback that calls SceneBuilder.drawFrame, stop function, attachToClient (stores BarClient reference and NetworkStream for callbacks), onFrame (builds GameSnapshot internally: refreshes MapGrid LOS/radar, queries unit positions/economy via Callbacks, processes GameFrame events), auto-fit ViewState computation on window resize in src/FSBar.Viz/GameViz.fs
- [x] T018 [US1] Update FSBar.Viz.fsproj Compile list to add LayerRenderer, SceneBuilder, GameViz (.fsi/.fs pairs) in correct dependency order in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T019 [US1] Create surface area baseline file for FSBar.Viz public modules (VizTypes, ColorMaps, LayerRenderer, SceneBuilder, GameViz, Viewer) in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs
- [x] T020 [US1] Create integration test: start headless session via BarClient, call GameViz.start, call GameViz.attachToClient, step a few frames calling GameViz.onFrame for each, wait 2 seconds, call stop — assert no exceptions in tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs
- [x] T021 [US1] Create LayerRenderer test: load MapGrid from live session, call renderLayer for HeightMap, assert returned SKBitmap dimensions match grid dimensions, assert pixel colors are non-uniform (terrain varies) in tests/FSBar.Viz.Tests/LayerRendererTests.fs

**Checkpoint**: At this point, `GameViz.start` opens a window showing a live terrain map that updates each frame. This is the MVP.

---

## Phase 4: User Story 2 — Switch Between Map Layer Representations (Priority: P2)

**Goal**: Support 7+ base layer views (height, slope, resource, LOS, radar, terrain classification, passability per MoveType). User can switch base layer via keyboard or REPL.

**Independent Test**: With viz running, switch to SlopeMap and verify display changes to show slope data. Switch to Passability(Tank) and verify binary red/green rendering.

### Implementation for User Story 2

- [x] T022 [P] [US2] Create InputHandler.fsi defining public API: processKeyDown (Key → VizCommand option), processMouseWheel (float32 → float32 → float32 → VizCommand option), processMouseDrag (float32 → float32 → VizCommand option) in src/FSBar.Viz/InputHandler.fsi
- [x] T023 [P] [US2] Create InputHandler.fs implementing keyboard → VizCommand mapping: keys 1-0 → SetBaseLayer for each LayerKind, Home → ResetView in src/FSBar.Viz/InputHandler.fs
- [x] T024 [US2] Extend LayerRenderer.fs to handle all LayerKind variants: SlopeMap (slope values → heatMap scheme), ResourceMap (metal values → heatMap), LosMap (visibility → binary bright/dim), RadarMap (coverage → binary), TerrainClassification (Land→green, Water→blue, Cliff→brown via MapGrid.terrainAt), Passability per MoveType (passable→green, impassable→red via MapGrid.passability) in src/FSBar.Viz/LayerRenderer.fs
- [x] T025 [US2] Wire InputHandler into Viewer: register Silk.NET.Input keyboard event handler in Viewer.fs that calls InputHandler.processKeyDown, dispatch resulting VizCommand to update VizConfig.BaseLayer in GameViz.fs state in src/FSBar.Viz/Viewer.fs and src/FSBar.Viz/GameViz.fs
- [x] T026 [US2] Add REPL API functions to GameViz.fsi and GameViz.fs: setBaseLayer (LayerKind → unit) in src/FSBar.Viz/GameViz.fsi and src/FSBar.Viz/GameViz.fs
- [x] T027 [US2] Update FSBar.Viz.fsproj Compile list to add InputHandler (.fsi/.fs) before Viewer in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T028 [US2] Create LayerRenderer test: render each LayerKind variant from a live MapGrid, assert each produces a non-null SKBitmap with expected dimensions, assert HeightMap and SlopeMap produce visually different bitmaps (compare pixel samples) in tests/FSBar.Viz.Tests/LayerRendererTests.fs
- [x] T029 [US2] Update surface area baselines for new public symbols (InputHandler, updated GameViz) in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

**Checkpoint**: All 7+ base layers render correctly. Keyboard 1-0 and `GameViz.setBaseLayer` both switch layers live.

---

## Phase 5: User Story 3 — Overlay Units and Game Events on the Map (Priority: P2)

**Goal**: Display unit markers (colored by team) and event indicators (created, destroyed, spotted) as overlays on top of the base layer.

**Independent Test**: Run a game with units, enable Units overlay, verify colored circles appear at unit positions. Destroy a unit and verify the marker disappears with a brief flash indicator.

### Implementation for User Story 3

- [x] T030 [P] [US3] Create UnitRenderer.fsi defining public API: drawUnits (SKCanvas → Map<int,UnitState> → ViewState → VizConfig → unit), drawEvents (SKCanvas → EventIndicator list → int → ViewState → VizConfig → unit) in src/FSBar.Viz/UnitRenderer.fsi
- [x] T031 [US3] Create UnitRenderer.fs: for each UnitState, convert elmo position to screen coordinates via ViewState (elmoToGrid then grid*scale+origin), draw filled circle with team color (friendly=blue, enemy=red) at UnitMarkerSize radius, optionally draw health bar. For EventIndicators, draw expanding ring or flash at position, skip if frameNumber > frameCreated + durationFrames in src/FSBar.Viz/UnitRenderer.fs
- [x] T032 [US3] Extend SceneBuilder.fs drawFrame to call UnitRenderer.drawUnits and drawEvents when OverlayKind.Units and OverlayKind.Events are in VizConfig.ActiveOverlays, applying OverlayOpacity in src/FSBar.Viz/SceneBuilder.fs
- [x] T033 [US3] Extend GameViz.fs onFrame to track unit state: on UnitCreated/EnemyCreated events, query position/health/team via Callbacks and add UnitState to snapshot.Units. On Update frames, refresh known unit positions. On UnitDestroyed/EnemyDestroyed, remove from Units and add EventIndicator. On EnemyEnterLOS, add enemy UnitState in src/FSBar.Viz/GameViz.fs
- [x] T034 [US3] Add keyboard toggle for overlays in InputHandler.fs: U → ToggleOverlay Units, E → ToggleOverlay Events in src/FSBar.Viz/InputHandler.fs
- [x] T035 [US3] Add REPL API to GameViz.fsi and GameViz.fs: toggleOverlay (OverlayKind → unit), enableOverlay (OverlayKind → unit), disableOverlay (OverlayKind → unit) in src/FSBar.Viz/GameViz.fsi and src/FSBar.Viz/GameViz.fs
- [x] T036 [US3] Update FSBar.Viz.fsproj Compile list to add UnitRenderer (.fsi/.fs) between LayerRenderer and SceneBuilder in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T037 [US3] Create integration test: start game session, step until units exist, call GameViz.start with Units overlay enabled, updateSnapshot with unit data, verify no exceptions. Assert snapshot.Units is non-empty after stepping in tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs
- [x] T038 [US3] Update surface area baselines for UnitRenderer and updated GameViz in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

**Checkpoint**: Unit circles and event flashes appear on the map overlaid on the terrain. Keyboard U/E and REPL API toggle them.

---

## Phase 6: User Story 4 — Customize Visualization Appearance (Priority: P3)

**Goal**: Allow users to change color schemes, marker size, overlay opacity, and grid lines. Changes apply immediately and persist for the session.

**Independent Test**: Change height map color scheme via REPL, verify display updates. Toggle grid lines, verify grid overlay appears.

### Implementation for User Story 4

- [x] T039 [US4] Extend SceneBuilder.fs drawFrame to render grid lines when VizConfig.ShowGridLines is true: draw horizontal/vertical lines at GridLineSpacing intervals in grid coordinates, transformed to screen space via ViewState in src/FSBar.Viz/SceneBuilder.fs
- [x] T040 [US4] Add REPL API to GameViz.fsi and GameViz.fs: setConfig (VizConfig → unit), updateConfig ((VizConfig → VizConfig) → unit), setColorScheme (LayerKind → ColorScheme → unit), setMarkerSize (float32 → unit), setOverlayOpacity (float32 → unit), toggleGridLines (unit → unit) in src/FSBar.Viz/GameViz.fsi and src/FSBar.Viz/GameViz.fs
- [x] T041 [US4] Add keyboard toggle for grid in InputHandler.fs: G → ToggleOverlay Grid. Process VizCommand.SetColorScheme, SetMarkerSize, SetOverlayOpacity, ToggleGridLines in GameViz.fs command handler in src/FSBar.Viz/InputHandler.fs and src/FSBar.Viz/GameViz.fs
- [x] T042 [US4] Invalidate LayerRenderer cache when color scheme changes for a layer (detect scheme change in GameViz.fs, call LayerRenderer.invalidateCache for affected LayerKind) in src/FSBar.Viz/GameViz.fs
- [x] T043 [US4] Create customization test: start live session with viz, call setColorScheme to change HeightMap scheme, assert LayerRenderer cache is invalidated (cacheStats shows miss on next render). Call toggleGridLines, assert VizConfig.ShowGridLines flipped. Call setMarkerSize and setOverlayOpacity, assert VizConfig values updated in tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs
- [x] T044 [US4] Update surface area baselines for updated GameViz API in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

**Checkpoint**: Color schemes, marker size, opacity, and grid lines are all configurable via REPL and keyboard.

---

## Phase 7: User Story 5 — View Economic and Resource Overlay (Priority: P3)

**Goal**: Display metal spot locations on the map and show metal/energy income, usage, and storage as a HUD overlay.

**Independent Test**: Enable MetalSpots overlay, verify colored dots appear at metal spot positions. Enable EconomyHud, verify resource numbers display.

### Implementation for User Story 5

- [x] T045 [US5] Extend UnitRenderer.fs (or SceneBuilder.fs) to draw metal spots: for each spot in GameSnapshot.MetalSpots, draw filled circle at spot position proportional to richness value, using resource color scheme. Extend drawFrame to render when OverlayKind.MetalSpots is active in src/FSBar.Viz/UnitRenderer.fs and src/FSBar.Viz/SceneBuilder.fs
- [x] T046 [US5] Extend SceneBuilder.fs drawFrame to render economy HUD when OverlayKind.EconomyHud is active: draw text panel in corner showing Metal (current/storage, income, usage) and Energy (current/storage, income, usage) from GameSnapshot.EconomyMetal and EconomyEnergy in src/FSBar.Viz/SceneBuilder.fs
- [x] T047 [US5] Extend GameViz.fs onFrame to query economy data via Callbacks: getEconomyCurrent/Income/Usage/Storage for resourceId 0 (metal) and 1 (energy), and getMetalSpots, populate GameSnapshot fields in src/FSBar.Viz/GameViz.fs
- [x] T048 [US5] Add keyboard toggles in InputHandler.fs: M → ToggleOverlay MetalSpots, $ → ToggleOverlay EconomyHud in src/FSBar.Viz/InputHandler.fs
- [x] T049 [US5] Create integration test: start game session, step a few frames calling GameViz.onFrame, assert EconomyMetal.Income > 0 (metal income exists in a started game) in tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs
- [x] T050 [US5] Update surface area baselines in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

**Checkpoint**: Metal spots and economy HUD render correctly as toggleable overlays.

---

## Phase 8: Pan/Zoom & Input Completion (Cross-cutting, supports FR-009/FR-012)

**Purpose**: Complete mouse input (pan, zoom) and ensure all UI actions have REPL equivalents.

- [x] T051 Extend InputHandler.fs to handle mouse events: mouse wheel → Zoom VizCommand (factor based on scroll delta, centered on cursor), click+drag → Pan VizCommand (pixel delta), in src/FSBar.Viz/InputHandler.fs
- [x] T052 Wire mouse events in Viewer.fs: register Silk.NET.Input mouse scroll and mouse button/move handlers, dispatch to InputHandler, apply resulting VizCommands to ViewState in GameViz.fs in src/FSBar.Viz/Viewer.fs and src/FSBar.Viz/GameViz.fs
- [x] T053 Implement ViewState update logic in GameViz.fs: Pan adjusts origin, Zoom adjusts scale around cursor center and shifts origin, ResetView sets AutoFit=true and recomputes from window/map dimensions in src/FSBar.Viz/GameViz.fs
- [x] T054 Add REPL API to GameViz.fsi and GameViz.fs: pan (float32 → float32 → unit), zoom (float32 → float32 → float32 → unit) — complete the contract from contracts/GameViz-api.md in src/FSBar.Viz/GameViz.fsi and src/FSBar.Viz/GameViz.fs
- [x] T055 Update surface area baselines for final GameViz public API in tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs

**Checkpoint**: Full mouse interaction works. All UI actions available via REPL.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Scripting accessibility, documentation, final validation

- [x] T056 [P] Update scripts/prelude.fsx to add #r references for FSBar.Viz.dll and its dependencies (Silk.NET, SkiaSharp) in scripts/prelude.fsx
- [x] T057 [P] Create example script 06-game-viz-basic.fsx: start headless session, open viz, run game loop with GameViz.onFrame, demonstrate basic usage in scripts/examples/06-game-viz-basic.fsx
- [x] T058 [P] Create example script 07-game-viz-layers.fsx: demonstrate layer switching, overlay toggling, customization, pan/zoom via REPL API in scripts/examples/07-game-viz-layers.fsx
- [x] T059 Configure dotnet pack for FSBar.Viz: add Version, PackageId, Description to fsproj, verify `dotnet pack src/FSBar.Viz/FSBar.Viz.fsproj` produces .nupkg in src/FSBar.Viz/FSBar.Viz.fsproj
- [x] T060 Add structured diagnostics: log viewer startup/shutdown, frame metrics (optional DiagnosticsConfig), disconnection events via Console or structured logger in src/FSBar.Viz/Viewer.fs and src/FSBar.Viz/GameViz.fs
- [x] T061 Run all tests: `dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj` — all pass
- [x] T062 Validate quickstart.md steps execute successfully end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational — first implementable story
- **User Story 2 (Phase 4)**: Depends on Foundational + US1 (needs base rendering infrastructure)
- **User Story 3 (Phase 5)**: Depends on Foundational + US1 (needs SceneBuilder/Viewer)
- **User Story 4 (Phase 6)**: Depends on US1 + US2 (customizes existing layer rendering)
- **User Story 5 (Phase 7)**: Depends on US1 + US3 (adds overlays to existing SceneBuilder)
- **Pan/Zoom (Phase 8)**: Depends on US2 (InputHandler exists)
- **Polish (Phase 9)**: Depends on all prior phases

### User Story Dependencies

- **US1 (P1)**: After Foundational → standalone MVP
- **US2 (P2)**: After US1 → adds layer switching to existing viewer
- **US3 (P2)**: After US1 → adds unit overlay to existing SceneBuilder (can parallel with US2)
- **US4 (P3)**: After US1 + US2 → customizes layer rendering
- **US5 (P3)**: After US1 + US3 → adds economy overlay (can parallel with US4)

### Within Each User Story

- .fsi files before .fs implementations
- LayerRenderer/UnitRenderer before SceneBuilder
- SceneBuilder before GameViz integration
- Implementation before tests
- Update surface baselines after API changes

### Parallel Opportunities

- T004, T005, T006, T007 (foundational types + color maps) can all run in parallel
- T012, T013, T014 (US1 .fsi files) can all run in parallel
- T022, T023 (US2 InputHandler .fsi/.fs) can parallel with T024 (extending LayerRenderer)
- T030 (US3 UnitRenderer .fsi) can parallel with T034 (keyboard toggles)
- US3 and US2 can execute in parallel after US1 completes
- US4 and US5 can execute in parallel after their respective dependencies
- T055, T056, T057 (polish scripts) can all run in parallel

---

## Parallel Example: User Story 1

```text
# After Foundational completes, launch .fsi definitions in parallel:
Task T012: Create LayerRenderer.fsi in src/FSBar.Viz/LayerRenderer.fsi
Task T013: Create SceneBuilder.fsi in src/FSBar.Viz/SceneBuilder.fsi
Task T014: Create GameViz.fsi in src/FSBar.Viz/GameViz.fsi

# Then implement sequentially (dependencies):
Task T015: LayerRenderer.fs (needs T012)
Task T016: SceneBuilder.fs (needs T013, T015)
Task T017: GameViz.fs (needs T014, T016)

# Then tests in parallel:
Task T020: Integration test in tests/FSBar.Viz.Tests/GameVizIntegrationTests.fs
Task T021: LayerRenderer test in tests/FSBar.Viz.Tests/LayerRendererTests.fs
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T011)
3. Complete Phase 3: User Story 1 (T012–T021)
4. **STOP and VALIDATE**: Open viz during a live game, confirm terrain renders
5. Deploy/demo if ready — this is a functional visualization

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 → Live terrain map (MVP!)
3. US2 + US3 (parallel) → Layer switching + unit markers
4. US4 + US5 (parallel) → Customization + economy
5. Phase 8 → Mouse pan/zoom
6. Phase 9 → Scripts, packaging, polish

### Single Developer Strategy

1. Phases 1–3 sequentially → MVP in one sprint
2. US2 then US3 → Full tactical visualization
3. US4, US5, Phase 8 → Completeness
4. Phase 9 → Ship it

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All .fs modules MUST have corresponding .fsi signature files (constitution II)
- Surface area baselines MUST be updated after each phase (constitution II)
- Tests run against live BAR engine sessions — no mocks (CLAUDE.md)
- Example scripts must remain runnable (constitution V)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
