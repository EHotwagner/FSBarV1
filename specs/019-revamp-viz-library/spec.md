# Feature Specification: Revamp Viz Library with Declarative SkiaViewer

**Feature Branch**: `019-revamp-viz-library`  
**Created**: 2026-04-10  
**Status**: Draft  
**Input**: User description: "delete fsbar.viz and create a new library to visualize the bar data with the revamped skiaviewer. research the improved skiaviewes. make it visually impressive and use advanced skia functions like shaders/animations.... create real live visual tests with the synthetic data of the last feature. run those tests and fix any bugs."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Terrain Map Visualization with Visual Richness (Priority: P1)

A developer opens a game map preview and sees a visually impressive terrain rendering. Height maps use smooth gradient shaders instead of flat pixel colors. Slope and resource layers use animated noise effects or heat-map gradients that convey data density at a glance. The base layer rendering is the foundation of the entire visualization and must look polished.

**Why this priority**: The base map layer is the canvas on which all other overlays sit. Without a visually compelling base, no other visualization feature has impact.

**Independent Test**: Can be tested by generating a synthetic map grid, rendering a single frame to a screenshot, and verifying the output contains expected gradient/shader characteristics and correct dimensions.

**Acceptance Scenarios**:

1. **Given** a synthetic map grid with height data, **When** the terrain layer is rendered, **Then** the output uses smooth gradient shaders that produce continuous color transitions rather than flat per-pixel colors.
2. **Given** any supported layer kind (height, slope, resource, LOS, radar, passability), **When** the layer is rendered, **Then** each layer type has a distinct, visually appropriate color scheme applied via shaders.
3. **Given** a rendered base layer, **When** the user switches between layer kinds, **Then** the transition is immediate and the new layer renders correctly.

---

### User Story 2 - Unit and Event Overlay with Animations (Priority: P1)

A developer viewing a live or replayed game session sees friendly and enemy units rendered as visually distinct markers with animated effects. Unit creation events pulse outward, destruction events fade with a dissolve, and combat zones glow. The overlay conveys game activity through motion and visual emphasis rather than static icons.

**Why this priority**: Animated overlays are central to the "visually impressive" goal and differentiate this library from the previous flat-circle approach.

**Independent Test**: Can be tested by creating a synthetic snapshot with units and events, rendering multiple sequential frames, and verifying that event indicators change over time (animation progresses across frames).

**Acceptance Scenarios**:

1. **Given** a snapshot with friendly and enemy units, **When** a frame is rendered, **Then** friendly units are visually distinct from enemy units through color, shape, or shader effects.
2. **Given** a snapshot with a UnitCreated event, **When** multiple consecutive frames are rendered, **Then** the event indicator animates (e.g., expanding ring, pulsing glow) and eventually disappears after its duration.
3. **Given** a snapshot with a Combat event, **When** the frame is rendered, **Then** the combat zone shows a visually prominent animated effect (e.g., glow, particle-like shader).

---

### User Story 3 - Economy HUD with Animated Gauges (Priority: P2)

A developer viewing the game visualization sees an economy heads-up display that shows metal and energy status using animated bar gauges or radial indicators. The HUD updates smoothly as resource values change between frames, with fill levels that interpolate rather than jump.

**Why this priority**: The economy HUD provides critical gameplay context but is secondary to the map and unit visualization which form the core visual experience.

**Independent Test**: Can be tested by creating two sequential synthetic snapshots with different economy values, rendering both, and verifying the HUD elements reflect the changed values with visual interpolation.

**Acceptance Scenarios**:

1. **Given** a snapshot with economy data, **When** a frame is rendered, **Then** the HUD displays metal and energy indicators showing current level relative to storage capacity.
2. **Given** two consecutive frames with changing economy values, **When** both frames are rendered, **Then** the HUD indicators show smooth visual transitions reflecting the change.
3. **Given** a snapshot where current resources are near zero, **When** the frame is rendered, **Then** the HUD visually emphasizes the low-resource state (e.g., color shift to red, warning animation).

---

### User Story 4 - Interactive Viewer with Declarative Scene Pipeline (Priority: P2)

A developer can launch an interactive viewer window that responds to keyboard and mouse input. Panning, zooming, and layer switching work fluidly. The underlying rendering uses the declarative scene model from the revamped SkiaViewer, emitting scene trees via observables rather than imperative canvas drawing.

**Why this priority**: Interactivity is essential for practical use but the scene pipeline is an architectural requirement that enables all visual features.

**Independent Test**: Can be tested by launching the viewer with a synthetic snapshot, programmatically sending input events (zoom, pan, layer switch), capturing screenshots before and after, and verifying the view changed appropriately.

**Acceptance Scenarios**:

1. **Given** a viewer displaying a map, **When** the user scrolls the mouse wheel, **Then** the view zooms in or out centered on the cursor position.
2. **Given** a viewer displaying a map, **When** the user drags with the mouse, **Then** the view pans to follow the drag.
3. **Given** a viewer displaying a height map layer, **When** the user presses a layer-switching key, **Then** the base layer changes to the selected layer type.

---

### User Story 5 - Synthetic Data Playback with Animated Timeline (Priority: P3)

A developer can play back a full 300-frame synthetic game session as an animated sequence. The playback shows units moving, events triggering, and economy fluctuating over time. The playback loops continuously and runs at a configurable frame rate.

**Why this priority**: Playback is the ultimate showcase of all visual features working together but depends on all prior stories being complete.

**Independent Test**: Can be tested by starting a playback of a synthetic scene, letting it run for a few seconds, capturing multiple screenshots at intervals, and verifying that content differs between captures (animation is progressing).

**Acceptance Scenarios**:

1. **Given** a generated synthetic scene with 300 frames, **When** playback is started, **Then** the viewer shows units moving across the map over time.
2. **Given** a playback in progress, **When** the last frame is reached, **Then** playback loops back to frame 1 and continues.
3. **Given** a playback running, **When** the developer stops it programmatically, **Then** the viewer closes cleanly and all resources are released.

---

### Edge Cases

- When the map grid has zero-dimension or empty layer data, the viewer displays a solid background with a centered "No data" text label until valid data is available.
- How does the system handle a snapshot with no units and no events (empty overlay)?
- What happens when economy values are exactly zero or exceed storage capacity?
- How does the viewer behave when the window is resized to very small dimensions?
- What happens when playback is stopped mid-frame during an animation?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The old FSBar.Viz library MUST be deleted from the project before the new library is created.
- **FR-002**: The new library MUST use the revamped SkiaViewer's declarative scene API (observable-based scene emission) instead of imperative canvas callbacks.
- **FR-003**: The library MUST render terrain base layers (height, slope, resource, LOS, radar, passability) using shader-based rendering for smooth visual output.
- **FR-004**: The library MUST render unit overlays with visually distinct markers for friendly vs. enemy units, using advanced rendering techniques (gradients, glows, or shaped markers).
- **FR-005**: The library MUST render animated event indicators that change over multiple frames (pulsing, expanding, fading effects for unit creation, destruction, combat, and enemy spotted events).
- **FR-006**: The library MUST render an economy HUD displaying metal and energy status with gauge-style indicators.
- **FR-007**: The library MUST support interactive input: pan (mouse drag), zoom (mouse scroll), and layer switching (keyboard).
- **FR-008**: The library MUST support synthetic data playback with looping and configurable frame rate.
- **FR-009**: The library MUST provide a screenshot capture capability.
- **FR-010**: The library MUST maintain a public API surface comparable to the old FSBar.Viz (start/stop viewer, attach to game client, layer control, overlay control, preview/playback).
- **FR-011**: Visual tests MUST use synthetic data from the FSBar.SyntheticData library (all three scenes) to validate rendering correctness.
- **FR-012**: The library MUST use advanced SkiaSharp features including at least: gradient shaders, image filters (blur/glow), and animated effects that evolve across frames.

### Key Entities

- **Scene**: A declarative tree of visual elements emitted each frame to the SkiaViewer.
- **GameSnapshot**: The complete game state for one frame (units, events, economy, map data) used to build each scene.
- **LayerKind / OverlayKind**: Selectors for which map data and overlay features are currently active.
- **ViewState**: Camera position, zoom level, and viewport dimensions controlling the current view.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All three synthetic data scenes (SceneA, SceneB, SceneC) render without errors through a full 300-frame playback cycle.
- **SC-002**: Visual tests pass, confirming that rendered frames contain expected visual elements (units, events, HUD, base layers) for each synthetic scene.
- **SC-003**: The viewer starts, displays content, and shuts down cleanly within 5 seconds of the stop command.
- **SC-004**: Layer switching between all supported layer kinds completes within one frame refresh.
- **SC-005**: Animated event indicators visually change across at least 3 consecutive frames, confirming animation progression.
- **SC-006**: The library compiles and all visual tests pass in the project's test suite.

## Clarifications

### Session 2026-04-10

- Q: Should the new library keep the same project/package name (`FSBar.Viz`) or use a new name? → A: Same name (`FSBar.Viz`) — delete old code, create new project in same slot. Consumer references (test projects, solution file) remain unchanged.
- Q: Should the new library replicate all old module boundaries or focus on viz-only? → A: Full parity — reimplement all modules (GameViz, LiveSession, PreviewSession, MockSnapshot, MapData) with the new declarative scene API.
- Q: What should the viewer display when map grid has empty/missing layer data? → A: Placeholder — show background color with a centered "No data" text label.

## Assumptions

- The revamped SkiaViewer with declarative scene API and observable-based input is available from the local NuGet feed.
- The FSBar.SyntheticData library is available and produces valid 300-frame scenes with units, events, and economy data.
- The rendering environment supports either Vulkan or OpenGL backends as provided by SkiaViewer.
- The old FSBar.Viz library can be fully deleted without breaking other project components that depend on it — or those dependencies will be updated as part of this work.
- The new library will reside in the same project slot (src/) and be consumable by the existing test infrastructure.
- The GL (CPU raster) backend will be used in the container/CI environment where GPU access may be limited.
