# Feature Specification: Game State Visualization

**Feature Branch**: `008-game-viz`  
**Created**: 2026-04-06  
**Status**: Draft  
**Input**: User description: "create a running visualization of the map/gamestate. use https://github.com/EHotwagner/GameVizCurrent as a template. have customizations, different representations of the gamestate."

## Clarifications

### Session 2026-04-06

- Q: What is the application form factor? → A: In-process visualization embedded within FSBar.Client, following the GameVizCurrent template structure (Silk.NET windowing + SkiaSharp GPU rendering on a background thread, REPL-friendly thread-safe API with `setBodies`/`updateBodies`/`setConfig`/`updateConfig` pattern).
- Q: Can multiple map layers be displayed simultaneously? → A: Composited — one base layer (e.g., height map) with optional overlays stacked on top (units, LOS, radar, grid).
- Q: How should users switch layers and toggle overlays? → A: Both keyboard shortcuts (for quick manual control during live viewing) and REPL API commands (for scripted/programmatic control).
- Q: Should the map support pan/zoom or always auto-fit? → A: Auto-fit by default, with optional manual pan/zoom (mouse wheel, click-drag, reset shortcut). All UI interactions (pan, zoom, reset, layer switching) must also be available as REPL/scripting commands.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Live Map During Game Session (Priority: P1)

A user starts an AI game session and wants to see the current state of the map rendered visually in real time. The visualization updates each game frame, showing terrain (height, slopes, cliffs, water), unit positions, and resource locations on a 2D top-down view. The user can observe the game unfolding without relying solely on the in-engine graphical client.

**Why this priority**: The core value proposition — without a live map view, there is no visualization tool. This enables all other features.

**Independent Test**: Can be tested by launching a game session and confirming the visualization window opens and renders a recognizable map with terrain features that update as the game progresses.

**Acceptance Scenarios**:

1. **Given** a running game session, **When** the visualization is started, **Then** a window displays a 2D top-down rendering of the current map terrain (height, water, cliffs) within 2 seconds.
2. **Given** the visualization is running, **When** a new game frame arrives, **Then** the display refreshes to reflect the latest game state including unit positions and visibility changes.
3. **Given** the visualization is running, **When** the user resizes the window, **Then** the map scales proportionally to fit the available space.

---

### User Story 2 - Switch Between Map Layer Representations (Priority: P2)

A user wants to view different aspects of the game state by switching between distinct map representations. Available layers include: height map, slope map, resource (metal) distribution, line-of-sight coverage, radar coverage, terrain classification (land/water/cliff), and passability overlays for each movement type (kbot, tank, hover, ship).

**Why this priority**: Multiple representations are the key differentiator — they let the user understand different strategic dimensions of the game at a glance.

**Independent Test**: Can be tested by switching between at least three different layer views and confirming each shows visually distinct data that corresponds to the underlying map data.

**Acceptance Scenarios**:

1. **Given** the visualization is running, **When** the user selects the "Height Map" layer, **Then** the display shows a color-gradient rendering where elevation is represented by color intensity.
2. **Given** the visualization is running, **When** the user selects the "Line of Sight" layer, **Then** visible areas are highlighted and fog-of-war regions are dimmed, updating each frame.
3. **Given** the visualization is running, **When** the user selects the "Passability (Tank)" layer, **Then** passable cells are shown in green and impassable cells in red, matching the computed passability grid.

---

### User Story 3 - Overlay Units and Game Events on the Map (Priority: P2)

A user wants to see friendly and enemy units plotted on the map, color-coded by team allegiance, with visual indicators for key game events (unit destroyed, unit created, combat). This gives a strategic overview of the battlefield.

**Why this priority**: Unit positions on top of terrain complete the tactical picture and make the visualization actionable for AI debugging and strategy review.

**Independent Test**: Can be tested by running a game with units and confirming that unit markers appear at correct positions, change when units move, and disappear when units are destroyed.

**Acceptance Scenarios**:

1. **Given** the visualization is running and units exist, **When** a frame is received, **Then** each known unit is displayed as a marker at its map position, colored by team.
2. **Given** units are displayed, **When** a unit is destroyed, **Then** the marker is removed and a brief destruction indicator is shown.
3. **Given** an enemy enters line of sight, **When** the `EnemyEnterLOS` event fires, **Then** the enemy unit marker appears on the map.

---

### User Story 4 - Customize Visualization Appearance (Priority: P3)

A user wants to adjust visual settings such as color schemes for map layers, unit marker size, overlay opacity, and whether to show grid lines or coordinate labels. These preferences persist for the duration of the session.

**Why this priority**: Customization improves usability for different use cases (presentations, debugging, analysis) but is not required for core functionality.

**Independent Test**: Can be tested by changing a color scheme setting and confirming the display updates immediately without restarting.

**Acceptance Scenarios**:

1. **Given** the visualization is running, **When** the user changes the height map color scheme, **Then** the map re-renders with the new colors within 1 second.
2. **Given** the visualization is running, **When** the user toggles grid lines on, **Then** a grid overlay appears aligned with the heightmap grid.
3. **Given** the user has customized settings, **When** they switch layers and switch back, **Then** their customizations are preserved.

---

### User Story 5 - View Economic and Resource Overlay (Priority: P3)

A user wants to see an economic overview showing metal spot locations, resource income/usage rates, and storage levels as an overlay or sidebar panel alongside the map.

**Why this priority**: Economic data adds strategic depth but is supplementary to the core map and unit visualization.

**Independent Test**: Can be tested by confirming metal spots are plotted on the map at correct positions and economy values update each frame.

**Acceptance Scenarios**:

1. **Given** the visualization is running, **When** the user enables the resource overlay, **Then** metal spot locations are marked on the map with indicators proportional to their richness.
2. **Given** the resource overlay is active, **When** a frame updates, **Then** current metal/energy income, usage, and storage values are displayed.

---

### Edge Cases

- What happens when the game session disconnects mid-visualization? The visualization should display a "disconnected" indicator and freeze on the last known state.
- What happens when the map is very large (e.g., 32x32 km)? The visualization should still render within acceptable frame times by downsampling if needed.
- What happens when no units exist on the map (e.g., before game start)? The terrain and layer views should render normally with no unit markers.
- What happens when the user switches layers during a frame update? The switch should complete cleanly without visual artifacts.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST connect to a running FSBar game session and receive game frame data in real time.
- **FR-002**: System MUST render a 2D top-down view of the map terrain based on the MapGrid height data.
- **FR-003**: System MUST support at least seven map layers: height map, slope map, resource map, LOS map, radar map, terrain classification, and passability (per movement type). These are rendered as a composited stack: one selectable base layer with optional overlay layers (units, LOS, radar, grid) toggled independently on top.
- **FR-004**: Users MUST be able to switch the base layer and toggle overlays during a live session without restarting, via both keyboard shortcuts (for quick manual control) and REPL API commands (for scripted/programmatic control).
- **FR-005**: System MUST display unit positions on the map, updated each game frame, color-coded by team allegiance.
- **FR-006**: System MUST visually indicate game events (unit created, destroyed, enemy spotted) on the map.
- **FR-007**: Users MUST be able to customize visual settings including color scheme, marker size, overlay opacity, and grid line visibility.
- **FR-008**: System MUST display current economic data (metal/energy income, usage, storage) when the resource overlay is enabled.
- **FR-009**: System MUST auto-fit the full map to the window by default, scaling proportionally on resize. Users MAY manually pan (click-drag) and zoom (mouse wheel), with a shortcut to reset to full-map view.
- **FR-010**: System MUST gracefully handle session disconnection by freezing on the last known state and showing a status indicator.
- **FR-011**: System MUST run in-process within FSBar.Client, rendering on a background thread using the Silk.NET + SkiaSharp pattern from GameVizCurrent, with a thread-safe REPL-friendly API for state mutation.
- **FR-012**: All UI interactions (pan, zoom, reset view, layer switching, overlay toggling) MUST be available as REPL/scripting API commands in addition to mouse/keyboard controls.

### Key Entities

- **MapView**: A visual representation of a specific map data layer (height, slope, LOS, etc.) with associated color mapping and rendering rules.
- **UnitMarker**: A visual element representing a game unit on the map, with position, team color, health indicator, and unit type.
- **LayerConfiguration**: User-defined visual settings for a particular map layer, including color scheme, opacity, and display toggles.
- **GameSnapshot**: The aggregated state of the game at a given frame — map layers, unit positions, events, and economic data — used to render a single visualization frame.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view a live map visualization within 3 seconds of starting a game session.
- **SC-002**: The visualization updates at least 10 times per second during normal gameplay, keeping pace with game frame delivery.
- **SC-003**: Users can switch between all available layer views in under 1 second per switch.
- **SC-004**: Unit positions displayed on the visualization match their actual in-game positions with no more than 1 grid cell of deviation.
- **SC-005**: Users can customize at least 3 visual properties (color scheme, marker size, overlay opacity) and see changes reflected immediately.
- **SC-006**: The visualization remains responsive (no freezes or crashes) during a 30-minute continuous game session.

## Assumptions

- The visualization runs in-process within FSBar.Client, so it has direct access to MapGrid, GameFrame, Callbacks, and all other client modules — no network or IPC layer is needed.
- The user's system supports OpenGL rendering (required by Silk.NET + SkiaSharp GPU surface).
- The GameVizCurrent template (github.com/EHotwagner/GameVizCurrent) provides the architectural pattern: Silk.NET windowing on a background thread, SkiaSharp GPU rendering, thread-safe state mutation via lock-guarded mutable state, and a REPL-friendly API.
- The existing MapGrid, Callbacks, and Protocol modules provide all necessary game data — no engine-side changes are required.
