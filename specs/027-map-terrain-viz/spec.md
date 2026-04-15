# Feature Specification: Map Terrain Visualization Rework

**Feature Branch**: `027-map-terrain-viz`  
**Created**: 2026-04-15  
**Status**: Draft  
**Input**: User description: "lets rework the visualization of maps. lets create a base viz of the cached permanent content. i want land go from lower height deep brown to lighter brown when higher elevation. same water from deep blue to light blue in the shallows. have the metal pulse animated."

## Clarifications

### Session 2026-04-15

- Q: Integration scope of the new base viz relative to the existing `FSBar.Viz` architecture? → A: Add a new `BaseTerrain` base layer, make it the default for both `PreviewSession` and `LiveSession`, keep the existing raw `HeightMap` layer available as a debug/alternate option.
- Q: How does the user select which committed cached map the preview path loads? → A: `.fsx` entry script takes the map name as an argument; in-viewer next/prev keypress cycles through `MapCacheFile.supportedMaps` without restarting the viewer.
- Q: Should the base viz be user-interactive (pan/zoom) or a fixed auto-fit image? → A: Interactive — reuse the existing `VizCommand.Pan`/`Zoom`/`ResetView` path; start the view with `AutoFit = true` so a fresh map fits the window, and user interaction drops auto-fit.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Elevation-shaded terrain base layer (Priority: P1)

A developer or analyst opens the map visualizer for any supported map and sees the static terrain of that map rendered as a base image: land areas are colored using a brown gradient where low-elevation land is deep/dark brown and high-elevation land is a lighter/pale brown, while areas below the water line are colored using a blue gradient where deep water is dark blue and shallow water (near the shoreline) is light blue. The shoreline is clearly legible because the brown and blue palettes meet at the water level. No live game is required — the visualization is produced entirely from the permanent cached map data (heightmap + resource map) already committed to the repository.

**Why this priority**: This is the foundation of every other map visualization in the project. Without a readable base layer, no overlay (units, build sites, chokepoints, plans) is meaningful. It also validates that the permanent map cache feature (026) is sufficient as an input for end-to-end visualization, which is the whole reason that cache exists.

**Independent Test**: Can be fully tested by loading any supported map from the permanent cache and visually confirming that (a) land is rendered with a brown gradient that tracks elevation, (b) water is rendered with a blue gradient that tracks depth, (c) the shoreline is clearly delineated, and (d) the same map always renders identically given the same cache file (deterministic). No live game connection is used in this test.

**Acceptance Scenarios**:

1. **Given** a supported map with its cache file present, **When** the user opens the base map visualization for that map, **Then** land pixels are colored on a continuous dark-brown → light-brown gradient that monotonically tracks terrain height above the water line.
2. **Given** the same map, **When** the user views the base visualization, **Then** water pixels are colored on a continuous dark-blue → light-blue gradient where deeper water is darker and water near the shoreline is lighter.
3. **Given** any two supported maps of differing size and shape, **When** each is rendered in turn, **Then** both fit the viewer at an aspect-correct scale and the color ramps are applied consistently so the viewer can tell at a glance which parts of each map are high, low, shallow, or deep.
4. **Given** a cache file that is missing or fails validation, **When** the user tries to open that map, **Then** the viewer shows a clear, human-readable error identifying which map and which cache field was bad, rather than a blank screen or a crash.

---

### User Story 2 - Animated metal-spot highlights (Priority: P2)

On top of the elevation-shaded base layer, the viewer continuously highlights every metal extraction spot on the map with a pulsing marker. The pulse is a smooth, steady animation (brightness or size, or both, rising and falling at a constant rhythm) that draws the eye to metal locations without obscuring the terrain underneath. The set of metal spots is taken directly from the cached resource map — no live game is required.

**Why this priority**: Metal spots are the single most decision-relevant static feature of a BAR map for strategy analysis. Making them pop visually is the main reason a human would want a "base map viz" rather than just staring at the cached heightmap. It is still P2 rather than P1 because the elevation base layer is meaningful on its own, while metal pulses without a base layer are not.

**Independent Test**: Load a map that has a known non-zero number of metal spots in its cache, confirm that exactly that many markers appear at the expected locations, and watch the viewer for long enough to see each marker visibly pulse (brighten/dim or grow/shrink) on a repeating cadence. Freeze-frame the viewer and confirm the markers never fully vanish (they remain at least faintly visible at every phase of the pulse).

**Acceptance Scenarios**:

1. **Given** a cached map containing N metal spots, **When** the base viz is displayed, **Then** exactly N pulsing markers are drawn, each anchored at its metal spot's map position.
2. **Given** the viewer is open and idle, **When** at least one full pulse period elapses, **Then** every metal marker has visibly brightened and dimmed (or grown and shrunk) at least once in that period.
3. **Given** the viewer is running, **When** the user looks at a pulsing metal marker, **Then** the underlying terrain color at that spot remains at least partially visible (the pulse does not fully occlude the base layer).
4. **Given** a map with zero metal spots in its cache, **When** the base viz is displayed, **Then** the base terrain renders normally and no metal markers appear (and no error is raised).

---

### User Story 3 - Browse any cached map without a live game (Priority: P3)

A user can point the viewer at any map whose analysis is committed under the permanent map cache and see the full base viz (elevation-shaded terrain + pulsing metal) without starting the game engine, without connecting to a running AI, and without any network activity. Switching from one map to another is a fast, low-friction action that does not require restarting the viewer.

**Why this priority**: This is what turns the feature from "a demo of the rendering pipeline" into a usable tool for inspecting committed map analysis. It depends on P1 and P2 being in place, so it ships after them. It is not itself a rendering feature — it is about access and navigation.

**Independent Test**: With the game engine and any live AI services stopped, open the viewer, pick a cached map from the list of committed cache files, confirm the base viz renders, then switch to a different cached map and confirm the new viz renders without restarting the viewer.

**Acceptance Scenarios**:

1. **Given** no live game session and no AI connection, **When** the user opens the viewer and selects a committed cached map, **Then** the base viz renders successfully.
2. **Given** the viewer is already displaying map A, **When** the user selects map B, **Then** the viewer replaces the view with B's base viz within a few seconds, without being relaunched.
3. **Given** the viewer's list of available maps, **When** the user inspects it, **Then** the list matches the set of committed cache files (no ghosts, no omissions).

---

### Edge Cases

- **Maps that are entirely land or entirely water**: the color ramp must still look sensible — all land, all water, no broken palette.
- **Flat maps** (very small elevation range): the brown gradient should still make the small variation visible rather than collapsing to a single near-uniform color.
- **Very tall maps** (extreme elevation range): the brightest/darkest ends of the ramp must remain readable, not clip to pure white or pure black.
- **Metal spots that sit exactly on the waterline**: the marker must still be visible and must clearly read as a metal marker rather than blending into the shoreline.
- **Metal spots clustered extremely close together**: pulsing markers must not merge into an unreadable blob; individual spots should remain distinguishable.
- **Rapid switching between maps**: starting a new map while the previous map's pulse animation is in flight must not leave stale markers or animation artifacts on screen.
- **Stale or schema-mismatched cache file**: must be reported as a clear, named error (per the permanent-map-cache contract), not silently rendered with wrong colors.
- **Window resize**: the base viz must rescale to the new window size while preserving map aspect ratio and gradient continuity.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The viewer MUST render a static base image of a map using only data available in that map's permanent cache file (heightmap and resource/metal positions on the cached `MapGrid`), without requiring a live game, AI, or network connection. The waterline is the implicit BAR sea-level convention (elevation `0`): cells with elevation below `0` are water, cells at or above `0` are land.
- **FR-002**: Land areas (cells at elevation ≥ 0) MUST be colored on a continuous deep-brown to light-brown gradient that monotonically increases in lightness with elevation, so that the viewer can read relative height at a glance.
- **FR-003**: Water areas (cells at elevation < 0) MUST be colored on a continuous deep-blue to light-blue gradient where deeper water (more negative elevation) is darker and shallower water (closer to 0) is lighter.
- **FR-004**: The shoreline (the transition between the brown and blue palettes) MUST be clearly visible to a human viewer and MUST coincide with elevation 0.
- **FR-005**: The color ramps for land and for water MUST be scaled to each map's actual min/max elevation (and min/max depth) so that flat maps and extreme maps both use the full visible range of their respective gradient.
- **FR-006**: Every metal spot listed in the cached resource map MUST be drawn as a marker at its cached map position.
- **FR-007**: Each metal marker MUST be animated with a smooth, continuously repeating pulse (brightness and/or size) at a steady human-readable cadence.
- **FR-008**: Metal markers at every phase of the pulse MUST remain at least faintly visible, and MUST NOT fully occlude the underlying terrain pixel at the marker's center.
- **FR-009**: The viewer MUST preserve each map's true aspect ratio when sizing the base image to the available viewport.
- **FR-009a**: The base viz MUST be interactive via the existing `VizCommand.Pan`, `VizCommand.Zoom`, and `VizCommand.ResetView` commands. A freshly opened map MUST start with `AutoFit = true` so the full map fits the window; any user pan or zoom MUST switch `AutoFit` off, and `ResetView` MUST return to auto-fit. Window resize MUST re-run auto-fit while it is still enabled, and MUST preserve the current Scale/OriginX/OriginY otherwise.
- **FR-010**: If a map's cache file is missing, schema-mismatched, or fails validation, the viewer MUST display a clear, human-readable error naming the map and the nature of the failure, and MUST NOT crash or present a blank view.
- **FR-011**: The user MUST be able to switch from one cached map to another inside the same viewer session, without restarting the viewer. The mechanism is a next/previous keybinding inside the running viewer that cycles through `MapCacheFile.supportedMaps` in a stable, deterministic order.
- **FR-011a**: The cached-preview entry point MUST be an `.fsx` script (consistent with the existing `src/FSBar.Viz/scripts/examples/*.fsx` idiom) that accepts the initial map name as a script argument and opens that map in the viewer; the user then uses the next/previous keybinding from FR-011 to browse other cached maps.
- **FR-012**: The same cache file MUST always produce a visually identical base image (deterministic rendering given the same cache input and window size); only the metal pulse phase is allowed to vary across redraws.
- **FR-013**: The base viz MUST work for every map whose cache file is committed under the permanent map cache, with no per-map special casing.
- **FR-014**: The pulse animation MUST run continuously while the viewer is open and the user is not interacting; pausing/resuming behavior beyond that (e.g., explicit freeze control) is out of scope for this feature.
- **FR-015**: The new base viz MUST be exposed as a new base layer option (a "BaseTerrain" layer) in the existing viz layer-selection model, and MUST become the default base layer for both the cached-content preview path (`PreviewSession`) and the live in-game viz path (`LiveSession`).
- **FR-016**: The existing raw `HeightMap` layer (and any other pre-existing base layers) MUST remain selectable as an alternate/debug view; this feature MUST NOT remove them. Only the *default* base layer changes.
- **FR-017**: When the new base layer is rendered from live in-game data (`LiveSession`), it MUST use the same land-brown / water-blue ramp rules (FR-002 – FR-005) and the same pulsing metal marker rules (FR-006 – FR-008) as the cached-content path, so the viewer looks consistent whether the source is a committed cache file or a live game.

### Key Entities

- **Cached Map**: A single supported map represented by its committed permanent cache file. Carries the heightmap (elevation grid), the resource map (metal spot positions), and identity metadata (map name, schema version, code version). The waterline is the implicit BAR sea-level constant (elevation `0`) — not a field of the cache. This feature treats the cache as a read-only input.
- **Elevation Color Ramp**: A conceptual mapping from an elevation value to a color. Two ramps exist: one from deep brown to light brown for land (elevation `0` → max elevation), and one from deep blue to light blue for water (min elevation → elevation `0`). Each ramp is rescaled per-map so both flat and extreme maps render legibly.
- **Metal Marker**: A visible, animated marker rendered on top of the base image at a metal spot's map coordinates. Its only state is the shared pulse phase; its position is fixed by the cache.
- **Base Map Viz**: The composed output — elevation-shaded terrain underneath, pulsing metal markers on top — shown to the user for a given Cached Map inside the viewer.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Opening a cached map in the viewer produces a fully rendered base viz (terrain + metal markers visibly pulsing at least once) within 3 seconds of the user selecting the map, on a developer workstation.
- **SC-002**: 100% of maps with committed permanent cache files render their base viz without error, with no per-map configuration.
- **SC-003**: A viewer looking at the base viz of an unfamiliar map can correctly identify, without any extra labels or legend, which regions are land vs. water and which parts of the land are high vs. low (verified by showing the viz to someone who has not seen that map before and asking them to point out the highest and lowest land areas and the shoreline).
- **SC-004**: A viewer can count the metal spots on a map just by looking at the pulsing markers, and the count matches the number of metal spots in the cache file exactly.
- **SC-005**: Switching from one cached map to another inside a running viewer session completes within 3 seconds and leaves no stale visuals from the previous map.
- **SC-006**: Re-rendering the same map twice in a row produces visually identical terrain output (pixel-for-pixel at the same window size), with only the metal pulse phase differing.
- **SC-007**: No live game engine, AI process, or network connection is required at any point during the base viz — verified by running the viewer with those systems stopped and confirming all of the above still works.

## Assumptions

- The permanent map cache (feature 026) is the sole input for the cached-content base viz. It already contains a usable heightmap and resource/metal map for every supported map (via the serialised `MapGrid`); the waterline is the implicit BAR sea-level constant (elevation `0`), not a cache field. If future maps are added, their cache files will be regenerated via the existing refresh workflow.
- "Deep brown", "light brown", "deep blue", "light blue" mean the obvious thing — a human reader, shown the result, should agree that land looks brown and water looks blue, with higher/shallower being visibly lighter. Exact palette values are a tuning decision left to implementation and will be adjusted based on how the first render looks.
- The pulse animation cadence is a tuning decision; a steady period in the rough neighborhood of one pulse every one-to-two seconds is assumed unless implementation reveals a clearly better default. The user can always ask for it to be faster or slower after seeing it.
- The existing viewer shell is the intended host for this base viz; no new standalone viewer application is in scope.
- This feature is about the *base terrain layer* (elevation-shaded land/water + pulsing metal) and nothing else. Live dynamic overlays — units, commands, fog of war, LOS, radar, transient events, economy HUD — are explicitly out of scope and will continue to be drawn by their existing overlay code unchanged. The new base layer MUST render correctly underneath those existing overlays in `LiveSession`, but the overlays themselves are untouched by this feature.
- Slope information is present in the cache but is not required by this feature's acceptance criteria. Implementation MAY use slope for subtle shading (e.g., hillshading) if that improves legibility, but that is an implementation choice, not a requirement.
- Chokepoints and base-plan data committed in the cache are out of scope for this feature — they belong to follow-up tactical overlays, not the base viz.
- The viewer continues to run in a graphical (windowed) environment, consistent with the project's existing viewer conventions; headless screenshot-only rendering is not a requirement of this feature.
