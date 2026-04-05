# Feature Specification: Array2D Map Data Layers

**Feature Branch**: `004-array-map-layers`  
**Created**: 2026-04-05  
**Status**: Draft  
**Input**: User description: "create specs to implement the array based ideas in the report."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load Map Grid Data from Engine at Runtime (Priority: P1)

An AI developer working in an F# REPL session connects to a running BAR game engine and retrieves the full set of map data layers (heightmap, slope, LOS, radar, resource) as structured grid data. The data is immediately available for inspection, querying, and use in AI logic without any manual parsing or transformation.

**Why this priority**: This is the foundation — without runtime map data, no analysis or AI decision-making on terrain is possible. The engine callbacks for these layers are already defined in the protobuf schema but not yet wrapped in the client library.

**Independent Test**: Can be fully tested by connecting to a live BAR engine instance, requesting each map layer, and verifying that the returned grid data has correct dimensions and contains plausible values for the current map.

**Acceptance Scenarios**:

1. **Given** a connected FSBar client session with a game in progress, **When** the user requests the heightmap data, **Then** the system returns a 2D grid of elevation values with dimensions matching `(mapWidth/8 + 1) x (mapHeight/8 + 1)`.
2. **Given** a connected FSBar client session, **When** the user requests the slope map, **Then** the system returns a 2D grid of slope values at the same resolution as the heightmap.
3. **Given** a connected FSBar client session, **When** the user requests the LOS map, **Then** the system returns a 2D grid of integer visibility values for the current game state.
4. **Given** a connected FSBar client session, **When** the user requests the radar map, **Then** the system returns a 2D grid of integer radar coverage values.
5. **Given** a connected FSBar client session, **When** the user requests the resource map, **Then** the system returns a 2D grid of resource distribution values.

---

### User Story 2 - Structured Map Record with All Layers (Priority: P1)

An AI developer loads all available map data into a single structured record that bundles the grid layers together with map metadata (dimensions, name). This record serves as the unified data object passed to all downstream analysis and AI logic.

**Why this priority**: A unified map record is what makes the grid data composable and ergonomic. Without it, users must manage individual arrays manually, which is error-prone and tedious in a REPL.

**Independent Test**: Can be tested by loading a full map record from the engine and verifying that all layers are populated, dimensions are consistent across layers, and the record can be passed to query functions.

**Acceptance Scenarios**:

1. **Given** a connected FSBar client session, **When** the user requests a full map record, **Then** the system returns a structured object containing all grid layers, map dimensions in elmos, and map metadata.
2. **Given** a loaded map record, **When** the user accesses any grid layer, **Then** the layer dimensions are consistent with the map's width and height according to the layer's resolution (8 elmos/cell for heightmap, 16 elmos/cell for metalmap/typemap).
3. **Given** a loaded map record, **When** the user inspects it in the REPL, **Then** meaningful summary information is displayed (dimensions, layer availability) rather than raw array dumps.

---

### User Story 3 - Derive Passability Layer and Cache Computed Data (Priority: P2)

An AI developer derives a passability layer from slope thresholds and terrain types. All expensive computations (including passability per movement type) are cached so they are computed only once per session. The slope layer itself comes from the engine callback (see Clarifications).

**Why this priority**: Passability is essential for pathfinding and terrain classification, but depends on the raw grid data from P1 stories being available first.

**Independent Test**: Can be tested by loading a map, requesting the derived slope layer, verifying values are non-negative and correlate with known terrain features, and confirming that repeated requests return the cached result without recomputation.

**Acceptance Scenarios**:

1. **Given** a loaded map record, **When** the user requests the slope layer, **Then** the system returns the engine-provided slope data as a 2D grid at heightmap resolution.
2. **Given** a loaded map record, **When** the user requests a passability layer for a specific movement type, **Then** the system returns a 2D boolean grid indicating which cells are traversable for that movement type.
3. **Given** a derived layer that has already been computed, **When** the user requests it again, **Then** the cached result is returned without recomputation.

---

### User Story 4 - Point and Region Queries on Grid Data (Priority: P2)

An AI developer queries the map data at specific coordinates or within rectangular regions, receiving terrain classification and feature information. Coordinate conversions between elmos and grid indices are handled transparently.

**Why this priority**: Point and region queries are how AI logic actually consumes map data. They bridge raw grid arrays and meaningful game decisions.

**Independent Test**: Can be tested by querying known locations on a loaded map (e.g., start positions, metal spots) and verifying the returned terrain data matches expected characteristics.

**Acceptance Scenarios**:

1. **Given** a loaded map record, **When** the user queries height at a specific elmo coordinate, **Then** the system converts to the appropriate grid index and returns the height value.
2. **Given** a loaded map record, **When** the user queries terrain classification at a coordinate, **Then** the system returns a categorized terrain type (land, water, cliff, etc.) based on the engine's typemap indices.
3. **Given** a loaded map record, **When** the user extracts a rectangular sub-region, **Then** the system returns the grid data for just that region with correct dimensions.

---

### User Story 5 - Metal Spot Analysis from Grid Data (Priority: P3)

An AI developer identifies and analyzes metal extraction locations using the metal map layer, finding clusters of high-value metal spots and ranking potential expansion sites by metal density.

**Why this priority**: Metal spot analysis is a key AI use case but builds on the grid infrastructure from higher-priority stories.

**Independent Test**: Can be tested by loading a map known to have metal spots, querying the metal layer, and verifying that identified high-value locations correspond to known metal spot positions from the existing `getMetalSpots()` callback.

**Acceptance Scenarios**:

1. **Given** a loaded map record with a metal layer, **When** the user queries metal density at known metal spot coordinates, **Then** the values are significantly higher than surrounding terrain.
2. **Given** a loaded map record, **When** the user scans a region for cells above a metal density threshold, **Then** the system returns a list of coordinates meeting the criteria, sorted by density.

---

### Edge Cases

- What happens when the engine returns an empty or zero-length array for a map layer? The system should report a clear error indicating the layer is unavailable rather than creating a zero-dimension grid.
- What happens when the user queries coordinates outside the map bounds? The system should return a clear out-of-bounds indication rather than an index exception.
- How does the system handle maps of different sizes? Grid dimensions must be dynamically computed from the actual map width and height, not hardcoded.
- What happens if the engine connection is lost between individual layer requests during map loading? Partial map records should not be silently returned — the user should be informed which layers failed to load.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST expose callback wrappers for all five unwrapped map data layers: heightmap, slope map, LOS map, radar map, and resource map.
- **FR-002**: Each callback wrapper MUST return grid data as a native 2D array with dimensions derived from the current map's width and height.
- **FR-003**: System MUST provide a unified map record type that bundles all grid layers together with map dimensions and metadata.
- **FR-004**: System MUST support loading a complete map record from the engine in a single call.
- **FR-005**: System MUST retrieve slope data from the engine's slope map callback (not computed client-side).
- **FR-006**: System MUST compute passability grids from slope and terrain type data for all four movement types: tank, kbot, hover, and ship.
- **FR-007**: System MUST cache static layers (heightmap, slope, metal, type) and derived layers (passability) so that repeated requests return the cached result. Dynamic layers (LOS, radar) MUST support individual re-fetching without reloading the entire map record.
- **FR-008**: System MUST provide point query functions that accept elmo coordinates and return values from the appropriate grid cell.
- **FR-009**: System MUST provide region extraction functions that return sub-grids for rectangular areas.
- **FR-010**: System MUST validate coordinates against map bounds and report clear errors for out-of-bounds queries.
- **FR-011**: System MUST provide terrain classification that categorizes grid cells into terrain types (land, water, cliff) based on height and slope data (height < 0 = water, slope > threshold = cliff, else land). Typemap indices are not available via engine callbacks; this heuristic serves as the initial classification method.
- **FR-012**: System MUST provide REPL-friendly display of map records showing summary information (dimensions, available layers) rather than raw data dumps.

### Key Entities

- **MapGrid**: The core data structure bundling all 2D grid layers for a single map, along with map dimensions and resolution metadata.
- **Grid Layer**: A single 2D array representing one data dimension of the map (height, slope, metal, terrain type, visibility, radar coverage, resource distribution, passability).
- **Terrain Type**: A classification of map cells into strategic categories (land, water, cliff) derived from raw grid data.
- **Movement Type**: A unit mobility class (e.g., tank, kbot, hover, ship) that determines which cells are passable — used to generate per-type passability layers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All five previously-unwrapped map data layers are retrievable from a live engine session with correct dimensions within 5 seconds per layer.
- **SC-002**: A complete map record can be loaded from the engine in a single operation, with all layers populated and dimension-consistent.
- **SC-003**: Derived layers (slope, passability) are computed once and subsequent requests return instantly from cache.
- **SC-004**: Point queries by elmo coordinate return correct grid values, verified against known map features (metal spots, start positions) with 100% accuracy.
- **SC-005**: Out-of-bounds coordinate queries produce clear, descriptive error messages rather than unhandled exceptions.
- **SC-006**: Map records display meaningful summaries in the REPL (dimensions, layer count) rather than overwhelming raw data output.

## Clarifications

### Session 2026-04-05

- Q: Should slope data be computed client-side from the heightmap or retrieved from the engine's slope callback? → A: Use engine slope callback as primary source; no client-side derivation.
- Q: Should the map record support refreshing dynamic layers (LOS, radar) mid-session, or is it a one-time snapshot? → A: Refreshable — dynamic layers can be re-fetched individually; static layers stay cached.
- Q: What determines terrain classification — height-based heuristics, typemap indices, or a combination? → A: Height+slope heuristic — typemap indices are not exposed via engine callbacks (IDs 52-56). Use height < 0 for water, slope > threshold for cliff, else land. Revisit if a typemap callback becomes available.
- Q: How many movement types should passability layers support? → A: All four — tank, kbot, hover, and ship.

## Assumptions

- The BAR game engine is running and accessible via the existing gRPC bridge during testing and usage.
- The five unwrapped callback IDs (52-56) in `callbacks.proto` are correctly defined and the engine responds to them — the protobuf schema exists but the F# wrappers do not.
- Map dimensions do not change during a game session, so cached grid data remains valid for the session's duration.
- LOS and radar maps change during gameplay as fog of war updates; these dynamic layers support individual refresh while static layers remain cached.
- The existing `Callbacks` module patterns (for `getMapWidth`, `getMapHeight`, `getStartPos`, `getMetalSpots`) serve as the model for implementing the new callback wrappers.
- Standard BAR map sizes (up to 32x32 SMU) are the target; memory consumption for multiple grid layers at these sizes is acceptable (tens of MB total).
