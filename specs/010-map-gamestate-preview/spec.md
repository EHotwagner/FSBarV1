# Feature Specification: Map & GameState Preview via SkiaViewer

**Feature Branch**: `010-map-gamestate-preview`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "add map/gamestate -> skiaviewer display functionality. use saved mapdata and mock up gamestates to preview/test it before going to live tests. viewer should have fixed 60fps"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Save and Load Map Data for Offline Preview (Priority: P1)

A developer captures map data from a live game session and saves it to disk. Later, without a running game engine, they load the saved map data and display it in the SkiaViewer window at a fixed 60fps. The developer can switch between map layers (height, slope, resource, LOS, radar) and see the terrain rendered with appropriate color schemes.

**Why this priority**: Without saved map data, there is no offline preview capability. This is the foundation for all other stories — saved data enables testing without a live engine.

**Independent Test**: Save map data from a live session, stop the engine, load the saved data, and verify it renders correctly in SkiaViewer showing the terrain.

**Acceptance Scenarios**:

1. **Given** a live game session with map data available, **When** the developer triggers a save, **Then** the full map data (heightmap, slope, resource, LOS, radar, metal spots) is persisted to a file on disk
2. **Given** saved map data on disk, **When** the developer loads it without a running engine, **Then** the data is loaded into a MapGrid structure identical to the original
3. **Given** loaded map data, **When** it is rendered in SkiaViewer, **Then** all map layers display correctly with appropriate color mapping at 60fps

---

### User Story 2 - Mock GameState for Unit and Event Visualization (Priority: P1)

A developer constructs mock game states (units with positions, health, teams; event indicators; economy data) and renders them over the map in SkiaViewer. This allows testing the full visualization pipeline — terrain layers, unit markers, event indicators, economy HUD — without a live game engine.

**Why this priority**: Mock game states decouple visualization testing from engine availability, enabling rapid iteration on rendering logic and catching bugs before live integration tests.

**Independent Test**: Create a mock GameSnapshot with known units, events, and economy values. Render it in SkiaViewer and verify all elements appear at the correct positions with the correct visual properties.

**Acceptance Scenarios**:

1. **Given** a mock GameSnapshot with friendly and enemy units at known positions, **When** rendered in SkiaViewer with the Units overlay enabled, **Then** unit markers appear at the correct map positions with correct team colors (blue for friendly, red for enemy)
2. **Given** a mock GameSnapshot with event indicators (UnitCreated, UnitDestroyed, Combat, EnemySpotted), **When** rendered with the Events overlay enabled, **Then** event indicators display at the correct positions with correct visual styles
3. **Given** a mock GameSnapshot with economy data (metal/energy: current, income, usage, storage), **When** rendered with the EconomyHud overlay enabled, **Then** the HUD panel shows the correct values
4. **Given** a mock GameSnapshot with metal spot positions, **When** rendered with the MetalSpots overlay enabled, **Then** metal spot markers appear at the correct locations

---

### User Story 3 - Animated GameState Sequence Playback (Priority: P2)

A developer creates a sequence of mock game states representing progression over time (units moving, events occurring, economy changing) and plays them back in SkiaViewer at a controlled frame rate. This enables testing animations, event indicator fade-outs, and state transitions without a live game.

**Why this priority**: Animated playback validates the temporal aspects of visualization — event lifecycles, unit movement smoothing, and HUD updates — that static snapshots cannot test.

**Independent Test**: Create a sequence of 10+ mock GameSnapshots with progressively changing unit positions and events. Play them in SkiaViewer and verify smooth animation at 60fps.

**Acceptance Scenarios**:

1. **Given** a sequence of 60 mock GameSnapshots with units moving across the map, **When** played back at 60fps in SkiaViewer, **Then** units appear to move smoothly from start to end positions
2. **Given** a sequence where an event indicator is created and expires, **When** played back, **Then** the indicator appears, persists for its configured duration, and fades out
3. **Given** a sequence with changing economy values, **When** played back, **Then** the HUD updates reflect the changing values in real time

---

### User Story 4 - Interactive Map Navigation in Preview Mode (Priority: P2)

A developer can pan, zoom, and switch layers while previewing saved map data or mock game states. All existing keyboard shortcuts and mouse interactions work identically to live mode.

**Why this priority**: Interactivity is essential for inspecting specific map regions and verifying rendering at different zoom levels, but it builds on top of the core rendering established in US1/US2.

**Independent Test**: Load saved map data, use mouse scroll to zoom, mouse drag to pan, and keyboard shortcuts to switch layers. Verify all interactions work at 60fps.

**Acceptance Scenarios**:

1. **Given** a preview is running with saved map data, **When** the developer scrolls the mouse wheel, **Then** the map zooms in/out centered on the cursor position
2. **Given** a preview is running, **When** the developer drags with the left mouse button, **Then** the map pans following the drag direction
3. **Given** a preview is running, **When** the developer presses layer switch keys (1-0), **Then** the displayed layer changes accordingly
4. **Given** a preview is running, **When** the developer presses overlay toggle keys (U, E, G, M), **Then** the corresponding overlays toggle on/off

### Edge Cases

- What happens when saved map data is from a different map version or has corrupted dimensions?
- What happens when a mock GameSnapshot references unit positions outside the map bounds?
- What happens when the saved file format is unreadable or truncated?
- What happens when the mock GameSnapshot has zero units, zero events, or zero metal spots?
- What happens when the viewer window is resized during animated playback?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a way to capture and save the current MapGrid data (heightmap, slopemap, resourcemap, LOS, radar, metal spots) to a file on disk
- **FR-002**: The system MUST provide a way to load saved MapGrid data from disk without requiring a running game engine
- **FR-003**: The system MUST render loaded map data in SkiaViewer using the existing rendering pipeline (LayerRenderer, SceneBuilder, ColorMaps) at a fixed 60fps
- **FR-004**: The system MUST support constructing mock GameSnapshot records with arbitrary unit positions, events, economy data, and metal spots
- **FR-005**: The system MUST render mock GameSnapshots in SkiaViewer identically to live game snapshots — same layer rendering, unit markers, event indicators, economy HUD, and metal spots
- **FR-006**: The system MUST support playing back a sequence of GameSnapshots at a configurable frame rate for animation preview
- **FR-007**: The system MUST support all existing interactive controls (pan, zoom, layer switching, overlay toggling) during preview mode
- **FR-008**: The viewer MUST maintain a fixed 60fps render rate regardless of the game state playback rate
- **FR-009**: The system MUST validate loaded map data dimensions and report errors for corrupted or incompatible files
- **FR-010**: The system MUST handle edge cases gracefully: empty unit maps, out-of-bounds positions, zero-dimension maps

### Key Entities

- **SavedMapData**: A persisted representation of MapGrid including all layer arrays and metal spot positions. Stored as a file on disk.
- **MockGameSnapshot**: A GameSnapshot record constructed programmatically for testing, with builder helpers for setting unit positions, events, economy, and metal spots.
- **PreviewSession**: A running SkiaViewer instance displaying saved map data and/or mock game states, with interactive controls.
- **PlaybackSequence**: An ordered list of GameSnapshots replayed over time to simulate game progression.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Saved map data round-trips correctly — loaded data produces pixel-identical layer renders compared to the original live capture (verified by frame callback counting, consistent with project testing approach)
- **SC-002**: Mock GameSnapshots with 100+ units render at 60fps without frame drops
- **SC-003**: A 60-frame animated sequence plays back smoothly at 60fps with correct timing
- **SC-004**: All 5 map layers (height, slope, resource, LOS, radar) render correctly from saved data
- **SC-005**: All 5 overlays (units, events, grid, metal spots, economy HUD) render correctly from mock data
- **SC-006**: All interactive controls (pan, zoom, layer switch, overlay toggle) respond within one frame during preview mode
- **SC-007**: Map data save and load each complete in under 1 second for maps up to 512x512 heightmap resolution
- **SC-008**: All preview tests pass without a running game engine (fully offline)

## Assumptions

- The saved map data format is internal to this project; interoperability with external tools is not required
- Map data sizes are consistent with Beyond All Reason maps (typically 64x64 to 512x512 heightmap resolution)
- Mock GameSnapshots use the existing GameSnapshot record type from VizTypes — no new data types for game state
- The existing SceneBuilder.drawFrame and LayerRenderer.renderLayer functions work correctly with any valid MapGrid/GameSnapshot regardless of data source (live or loaded)
- Preview mode reuses the existing SkiaViewer integration via the same ViewerConfig callback pattern
- The SkiaViewer package (extracted in 009) provides the windowing and rendering infrastructure
- Metal spots are saved alongside map data since they are map-specific, not game-state-specific
