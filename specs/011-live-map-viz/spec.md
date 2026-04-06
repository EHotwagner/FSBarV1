# Feature Specification: Live 60fps Map Visualization

**Feature Branch**: `011-live-map-viz`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "create working live 60fps map visualization with a headless engine"

## Clarifications

### Session 2026-04-06

- Q: Does the visualization launch the engine or connect to an already-running one? → A: Built on existing FSBar.Viz infrastructure, which manages engine connectivity. Visualization connects to an already-running engine instance.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live Heightmap Rendering During Game (Priority: P1)

A developer launches a headless BAR engine game and sees a real-time 2D map visualization window that renders the terrain heightmap at a smooth 60fps. The visualization updates live as the game runs, showing the terrain as a color-coded elevation map. This is the core proof that the live engine-to-visualization pipeline works end-to-end.

**Why this priority**: Without a working real-time rendering loop connected to a live engine, nothing else in this feature has value. This is the foundational pipeline.

**Independent Test**: Can be fully tested by launching a headless engine game and observing a window that displays the heightmap with smooth animation. Delivers immediate visual confirmation that the data pipeline works.

**Acceptance Scenarios**:

1. **Given** a headless BAR engine game is running, **When** the visualization is started, **Then** a window opens displaying a color-coded heightmap of the current map
2. **Given** the visualization window is open, **When** the game is running, **Then** the display updates at approximately 60 frames per second without visible stutter or lag
3. **Given** a game on any supported map, **When** the visualization renders the heightmap, **Then** terrain features (mountains, valleys, plateaus) are visually distinguishable through color gradation

---

### User Story 2 - Live Unit and Game State Overlay (Priority: P2)

While watching the live map visualization, the developer sees unit positions overlaid on the terrain map. As units move during the game, their positions update in real time on the visualization. This transforms the tool from a static terrain viewer into a live game state monitor.

**Why this priority**: Unit position overlay is the primary value-add over a static map export. It enables real-time debugging and observation of AI behavior during gameplay.

**Independent Test**: Can be tested by running a game with units and verifying that unit markers appear at correct map positions and move as the game progresses.

**Acceptance Scenarios**:

1. **Given** a running game with units on the map, **When** the visualization is active, **Then** unit positions are displayed as markers on the terrain overlay
2. **Given** units are moving during gameplay, **When** the visualization renders the next frame, **Then** unit markers reflect their updated positions
3. **Given** units belonging to different teams, **When** displayed on the visualization, **Then** units are visually distinguishable by team (e.g., different colors)

---

### User Story 3 - Dynamic Map Layer Toggling (Priority: P3)

The developer can toggle between different map data layers while the game is running — switching between heightmap, slope map, LOS (line-of-sight) map, and radar map views. This allows focused analysis of specific terrain or game state properties during live gameplay.

**Why this priority**: Layer toggling enhances the diagnostic value of the visualization but is not required for the core live rendering to be useful. Each layer provides different analytical insight.

**Independent Test**: Can be tested by pressing keyboard keys to switch between map layers and verifying each layer renders correctly with appropriate visual encoding.

**Acceptance Scenarios**:

1. **Given** the visualization is running, **When** the user presses a designated key, **Then** the display switches to a different map data layer (e.g., heightmap to slope map)
2. **Given** the LOS map layer is selected, **When** the game state changes (units move, gain/lose vision), **Then** the LOS overlay updates in real time
3. **Given** any map layer is selected, **When** the visualization renders, **Then** a legend or label indicates which layer is currently displayed

---

### Edge Cases

- What happens when the engine has not yet provided map data (first few frames before data is available)? The visualization displays a loading indicator and retries each frame until data arrives.
- How does the visualization behave when the engine process terminates unexpectedly mid-game? The visualization freezes on the last known state and displays a disconnection indicator.
- What happens when the game runs at very high speed (e.g., 100x)? The visualization continues rendering at 60fps, sampling the latest available game state regardless of simulation speed.
- How does the system handle maps of significantly different sizes? The visualization scales the map to fit the window while preserving aspect ratio.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST render map data from a live headless BAR engine instance within the existing FSBar.Viz visualization framework, connecting to an already-running engine
- **FR-002**: System MUST render at a target of 60 frames per second, decoupled from engine game speed
- **FR-003**: System MUST display the heightmap as a color-coded 2D terrain visualization
- **FR-004**: System MUST connect to the engine through the existing proxy-to-client pipeline (Unix domain socket, protobuf framing)
- **FR-005**: System MUST display unit positions overlaid on the map, updating each frame
- **FR-006**: System MUST visually distinguish units by team affiliation
- **FR-007**: System MUST support switching between map layers (heightmap, slope, LOS, radar, metal) via keyboard input
- **FR-008**: System MUST indicate which map layer is currently active
- **FR-009**: System MUST handle graceful startup when map data is not yet available (retry until data arrives, show loading state)
- **FR-010**: System MUST continue rendering smoothly regardless of engine game speed (rendering is independent of simulation tick rate)

### Key Entities

- **MapFrame**: A snapshot of map data (heightmap, slope, LOS, radar) at a given point in time, received from the engine
- **UnitOverlay**: Collection of unit positions and team affiliations for overlay rendering on the map
- **MapLayer**: An enumeration of available visualization modes (heightmap, slope, LOS, radar, metal)
- **RenderState**: The current visualization state including selected layer, window dimensions, and frame timing

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The visualization window displays a recognizable terrain map within 5 seconds of game start
- **SC-002**: Frame rate remains at or above 55fps (sustained) during normal game operation
- **SC-003**: Unit positions on the visualization correspond to their actual game positions with less than 1 second of latency
- **SC-004**: Switching between map layers completes within a single frame (no visible delay)
- **SC-005**: The visualization runs a complete game session (5+ minutes) without crashes or memory growth
- **SC-006**: All five map layer types (heightmap, slope, LOS, radar, metal) render with visually correct data

## Assumptions

- The existing proxy-to-client pipeline (C proxy -> Unix domain socket -> F# client) is functional and available, as proven by the map data extraction report
- The headless engine binary is installed and accessible at the configured path
- The raster rendering + texture upload approach (from existing visualization infrastructure) will be reused, since the GPU backend is not available in this environment
- Map data callbacks (heightmap, slope map, LOS map, radar map, resource map, unit positions) are queryable each frame through the existing callback infrastructure
- The visualization targets a desktop Linux environment with display support
- FSBar.Viz is the target project for this feature — it already provides windowing, SkiaSharp rendering, and engine connectivity infrastructure from features 008/009/010
- "60fps" refers to the visualization rendering rate, not the engine simulation rate — the two are decoupled
