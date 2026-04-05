# Tasks: Array2D Map Data Layers

**Input**: Design documents from `/specs/004-array-map-layers/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/public-api.md

**Tests**: Included — constitution requires test evidence for behavior-changing code, and CLAUDE.md mandates live engine integration tests.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Library source**: `src/FSBar.Client/`
- **Proto definitions**: `proto/highbar/` and `src/FSBar.Proto/`
- **Live tests**: `tests/FSBar.LiveTests/`
- **Scripts**: `scripts/` and `scripts/examples/`

---

## Phase 1: Setup

**Purpose**: Add new module files to the project and establish compile order

- [x] T001 Add MapGrid.fsi, MapGrid.fs, MapQuery.fsi, MapQuery.fs, MapCache.fsi, MapCache.fs to compile order in src/FSBar.Client/FSBar.Client.fsproj — insert before BarClient.fsi/BarClient.fs entries, with .fsi before .fs for each pair
- [x] T002 [P] Add MapGridTests.fs to tests/FSBar.LiveTests/FSBar.LiveTests.fsproj
- [x] T003 [P] Add MapQueryTests.fs to tests/FSBar.LiveTests/FSBar.LiveTests.fsproj

---

## Phase 2: Foundational (Callback Wrappers)

**Purpose**: Wrap the five unwrapped engine callbacks — MUST complete before any user story

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T004 Add signatures for getHeightMap, getSlopeMap, getLosMap, getRadarMap, getResourceMap to src/FSBar.Client/Callbacks.fsi — follow existing pattern (stream: NetworkStream -> return type), use float32 list for FloatArray callbacks (52, 53) and int list for IntArray callbacks (54, 55, 56)
- [x] T005 Implement getHeightMap in src/FSBar.Client/Callbacks.fs — call Protocol.sendCallback with CallbackId.CallbackMapGetHeightMap (52), extract via getFloatArray
- [x] T005a [US1] Verify callback 56 (RESOURCE_MAP) return type against live engine — call Protocol.sendCallback with CallbackMapGetResourceMap, inspect whether result is FloatArrayValue or IntArrayValue. If FloatArray: update getResourceMap in Callbacks.fs/.fsi to return float32 list, update MapGrid.ResourceMap to float32[,], and update MapQuery.resourceHotspots threshold type to float32. Document finding in research.md R2
- [x] T006 Implement getSlopeMap in src/FSBar.Client/Callbacks.fs — call with CallbackId.CallbackMapGetSlopeMap (53), extract via getFloatArray
- [x] T007 Implement getLosMap in src/FSBar.Client/Callbacks.fs — call with CallbackId.CallbackMapGetLosMap (54), extract via getIntArray
- [x] T008 Implement getRadarMap in src/FSBar.Client/Callbacks.fs — call with CallbackId.CallbackMapGetRadarMap (55), extract via getIntArray
- [x] T009 Implement getResourceMap in src/FSBar.Client/Callbacks.fs — call with CallbackId.CallbackMapGetResourceMap (56), extract via getIntArray

**Checkpoint**: All five map data callbacks available. Verify with `dotnet build src/FSBar.Client`.

---

## Phase 3: User Story 1 — Load Map Grid Data from Engine (Priority: P1) + User Story 2 — Structured Map Record (Priority: P1) MVP

**Goal**: Load all five map layers from the engine as Array2D grids bundled into a single MapGrid record with REPL-friendly display.

**Independent Test**: Connect to live engine, call loadFromEngine, verify all layers have correct dimensions and plausible values. Inspect record in FSI and confirm summary display.

### Implementation for User Stories 1 & 2

- [x] T010 [US1] Define Terrain DU (Land/Water/Cliff) and MoveType DU (Kbot/Tank/Hover/Ship) with RequireQualifiedAccess in src/FSBar.Client/MapGrid.fsi and src/FSBar.Client/MapGrid.fs — per contracts/public-api.md
- [x] T011 [US1] Define MapGrid record type in src/FSBar.Client/MapGrid.fsi and src/FSBar.Client/MapGrid.fs — fields: WidthElmos, HeightElmos, WidthHeightmap, HeightHeightmap, HeightMap (float32[,]), SlopeMap (float32[,]), ResourceMap (int[,]), LosMap (int[,]), RadarMap (int[,])
- [x] T012 [US1] Implement private toFloat32Array2D and toIntArray2D helper functions in src/FSBar.Client/MapGrid.fs — reshape flat list to Array2D using Array2D.init with row-major indexing per research.md R1. Raise descriptive error if list length does not match expected width*height
- [x] T013 [US1] Implement MapGrid.loadFromEngine in src/FSBar.Client/MapGrid.fs — call Callbacks.getMapWidth/Height for dimensions, then all 5 new callbacks, reshape each to Array2D with correct dimensions per research.md R2 (heightmap/slope: (W+1)x(H+1), LOS/radar/resource: WxH), construct MapGrid record. Raise if any layer returns empty array
- [x] T014 [US2] Override ToString on MapGrid record in src/FSBar.Client/MapGrid.fs — display format: `MapGrid { WxH elmos, WxH heightmap, N layers loaded }` per research.md R6
- [x] T015 [US1] Write MapGrid.loadFromEngine .fsi signature in src/FSBar.Client/MapGrid.fsi — val loadFromEngine: stream: NetworkStream -> MapGrid

### Tests for User Stories 1 & 2

- [x] T016 [US1] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_loadFromEngine_returns_correct_heightmap_dimensions: load MapGrid, verify HeightMap dimensions are (W+1)x(H+1) where W,H from getMapWidth/Height
- [x] T017 [P] [US1] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_loadFromEngine_all_layers_populated: verify HeightMap, SlopeMap, ResourceMap, LosMap, RadarMap all have non-zero dimensions
- [x] T018 [P] [US2] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_mapGrid_toString_shows_summary: load MapGrid, call ToString(), verify output contains "elmos" and "heightmap" and does not exceed 200 characters

**Checkpoint**: MapGrid loads from engine with all 5 layers as Array2D. ToString displays compact summary. All P1 stories functional.

---

## Phase 4: User Story 3 — Derive Passability Layer and Cache Computed Data (Priority: P2)

**Goal**: Compute passability grids per movement type from slope/terrain data, with session-level caching for all derived and static layers.

**Independent Test**: Load map, request passability for each MoveType, verify boolean grids have correct dimensions. Request same MoveType again and confirm cache hit.

### Implementation for User Story 3

- [x] T019 [US3] Implement MapGrid.terrainAt in src/FSBar.Client/MapGrid.fs — classify cell using typemap-equivalent logic: height < 0 → Water(depth), slope > threshold → Cliff(slope), else Land(hardness). Add .fsi signature
- [x] T020 [US3] Implement MapGrid.passability in src/FSBar.Client/MapGrid.fs — compute bool[,] for given MoveType using slope thresholds per research.md R4: Kbot (steep OK, no deep water), Tank (moderate slope, no water), Hover (extreme slope only, crosses water), Ship (water only). Add .fsi signature
- [x] T020a [P] [US3] Implement (|Land|Water|Cliff|) active pattern in src/FSBar.Client/MapGrid.fs — decompose Terrain DU for pattern matching, returning the float32 attribute. Add signature to src/FSBar.Client/MapGrid.fsi per contracts/public-api.md
- [x] T020b [P] [US3] Implement (|Passable|Impassable|) active pattern in src/FSBar.Client/MapGrid.fs — takes MapGrid, MoveType, x, z; returns Passable or Impassable based on passability grid lookup. Add signature to src/FSBar.Client/MapGrid.fsi
- [x] T021 [US3] Implement MapCache module in src/FSBar.Client/MapCache.fsi and src/FSBar.Client/MapCache.fs — ConcurrentDictionary<string, Lazy<obj>> for static grid caching; MapCache.fromEngine caches MapGrid after first call; MapCache.passability caches per MoveType string key; MapCache.clear resets all caches
- [x] T022 [US3] Implement MapGrid.refreshLos and MapGrid.refreshRadar in src/FSBar.Client/MapGrid.fs — re-fetch single dynamic layer from engine, reshape to Array2D, return new MapGrid record with updated layer. Add .fsi signatures

### Tests for User Story 3

- [x] T023 [US3] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_passability_kbot_dimensions_match_heightmap: compute Kbot passability, verify dimensions match (W+1)x(H+1)
- [x] T024 [P] [US3] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_passability_all_four_movetypes: compute passability for Kbot, Tank, Hover, Ship — all return bool[,] with correct dimensions
- [x] T025 [P] [US3] Write live test in tests/FSBar.LiveTests/MapGridTests.fs — test_refreshLos_returns_updated_grid: call refreshLos, verify new MapGrid has LosMap with same dimensions as original

**Checkpoint**: Passability grids computed for all 4 movement types. Caching operational. Dynamic layer refresh works.

---

## Phase 5: User Story 4 — Point and Region Queries on Grid Data (Priority: P2)

**Goal**: Query map data at elmo coordinates with automatic coordinate conversion, terrain classification, and rectangular sub-region extraction.

**Independent Test**: Query known locations (start positions, metal spots), verify returned values match expected terrain. Extract sub-regions and confirm dimensions.

### Implementation for User Story 4

- [x] T026 [US4] Implement MapQuery.elmoToGrid and MapQuery.gridToElmo in src/FSBar.Client/MapQuery.fsi and src/FSBar.Client/MapQuery.fs — integer division by 8 for elmo→grid, multiply by 8 for grid→elmo
- [x] T027 [US4] Implement MapQuery.heightAtElmo and MapQuery.slopeAtElmo in src/FSBar.Client/MapQuery.fs — convert elmo to grid index, bounds-check against WidthHeightmap/HeightHeightmap, return Result<float32, string> with descriptive error for out-of-bounds. Add .fsi signatures
- [x] T028 [US4] Implement MapQuery.terrainAtElmo in src/FSBar.Client/MapQuery.fs — convert to grid, call MapGrid.terrainAt, return Result<Terrain, string>. Add .fsi signature
- [x] T029 [US4] Implement MapQuery.heightSubRegion in src/FSBar.Client/MapQuery.fs — convert elmo bounds to grid indices, clamp to valid range, use Array2D.sub or Array2D.init to extract rectangular sub-grid, return Result<float32[,], string>. Add .fsi signature
- [x] T030 [US4] Implement MapQuery.resourceHotspots in src/FSBar.Client/MapQuery.fs — scan ResourceMap within elmo bounds for cells above threshold, return (x, z, value) list sorted by value descending. Add .fsi signature

### Tests for User Story 4

- [x] T031 [US4] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_heightAtElmo_at_start_position: get start pos via getStartPos, query heightAtElmo at those coordinates, verify Ok result with plausible height value
- [x] T032 [P] [US4] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_heightAtElmo_out_of_bounds: query with coordinates beyond map dimensions, verify Error result with descriptive message
- [x] T033 [P] [US4] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_heightSubRegion_correct_dimensions: extract 1024x1024 elmo region, verify returned Array2D has 128x128 cells
- [x] T034 [P] [US4] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_elmoToGrid_roundtrip: convert elmo→grid→elmo, verify result matches original (within grid alignment)

**Checkpoint**: All point and region queries work with coordinate conversion. Out-of-bounds produces clear errors.

---

## Phase 6: User Story 5 — Metal Spot Analysis from Grid Data (Priority: P3)

**Goal**: Identify high-value metal locations from the resource map and compare with known metal spots.

**Independent Test**: Load map, find resource hotspots, cross-reference with getMetalSpots() results to verify correlation.

### Implementation for User Story 5

- [x] T035 [US5] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_resourceHotspots_correlate_with_metalSpots: load MapGrid, call resourceHotspots for full map with threshold > 0, call getMetalSpots, verify that hotspot coordinates are near known metal spot positions (within 2 grid cells)
- [x] T036 [P] [US5] Write live test in tests/FSBar.LiveTests/MapQueryTests.fs — test_resourceHotspots_empty_for_high_threshold: query with threshold 255, verify returns empty or very short list

**Checkpoint**: Metal spot analysis validates against engine's own metal spot data.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Scripting accessibility, documentation, surface-area baselines

- [x] T037 [P] Update scripts/prelude.fsx — add `open FSBar.Client.MapGrid`, `open FSBar.Client.MapQuery`, `open FSBar.Client.MapCache` after existing opens
- [x] T038 [P] Create scripts/examples/05-map-layers.fsx — example script that loads a MapGrid, queries a few points, shows terrain classification, demonstrates passability, and refreshes LOS. Follow pattern of existing example scripts (load prelude, connect, demonstrate features)
- [ ] T039 [P] Create surface-area baseline tests for MapGrid, MapQuery, MapCache modules in tests/FSBar.LiveTests/ — verify public API surface matches .fsi signatures per constitution principle II (BLOCKED: proxy does not support map data callbacks 52-56)
- [ ] T040 Verify quickstart.md scenarios run correctly against live engine — execute each code snippet from specs/004-array-map-layers/quickstart.md in FSI and confirm expected output (BLOCKED: proxy does not support map data callbacks 52-56)

**Checkpoint**: All scripts load, examples run, surface-area baselines pass.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1+US2 (Phase 3)**: Depends on Phase 2 — the MVP increment
- **US3 (Phase 4)**: Depends on Phase 3 (needs MapGrid record and loaded layers)
- **US4 (Phase 5)**: Depends on Phase 3 (needs MapGrid record). Can run in PARALLEL with Phase 4
- **US5 (Phase 6)**: Depends on Phase 5 (needs MapQuery.resourceHotspots)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1+US2 (P1)**: Depends on Foundational only — no cross-story dependencies
- **US3 (P2)**: Depends on US1+US2 (needs loaded MapGrid with slope data)
- **US4 (P2)**: Depends on US1+US2 (needs loaded MapGrid for queries). Independent of US3
- **US5 (P3)**: Depends on US4 (uses resourceHotspots function)

### Within Each User Story

- Types/signatures before implementation
- Implementation before tests (tests run against live engine, need working code)
- Core logic before error handling
- Story complete before moving to next priority

### Parallel Opportunities

- T002, T003 can run in parallel with T001 (different .fsproj files)
- T005 must come first (establishes callback section pattern), then T006-T009 can run in parallel
- T016, T017, T018 test tasks can run in parallel after implementation
- Phase 4 (US3) and Phase 5 (US4) can run in parallel — they depend on Phase 3 but not on each other
- T037, T038, T039 polish tasks can all run in parallel

---

## Parallel Example: Phase 2 (Foundational)

```text
# First: establish pattern
Task T005: Implement getHeightMap in Callbacks.fs

# Then parallel:
Task T006: Implement getSlopeMap in Callbacks.fs
Task T007: Implement getLosMap in Callbacks.fs
Task T008: Implement getRadarMap in Callbacks.fs
Task T009: Implement getResourceMap in Callbacks.fs
```

## Parallel Example: Phase 4 + Phase 5

```text
# After Phase 3 completes, launch both in parallel:
Phase 4 (US3): T019 → T020 → T021 → T022 → T023-T025
Phase 5 (US4): T026 → T027-T030 → T031-T034
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (5 callback wrappers)
3. Complete Phase 3: US1+US2 (MapGrid types, loadFromEngine, ToString)
4. **STOP and VALIDATE**: Load MapGrid from live engine, verify all layers, check REPL display
5. This is a usable increment — developers can access all map data as Array2D grids

### Incremental Delivery

1. Setup + Foundational → Callbacks ready
2. US1+US2 → MapGrid loads from engine → **MVP!**
3. US3 → Passability + caching → Strategic terrain analysis enabled
4. US4 → Point/region queries → Ergonomic coordinate-based access
5. US5 → Metal analysis → Cross-validated resource intelligence
6. Polish → Scripts, examples, baselines → Production-ready

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All tests run against live BAR engine — no mocks per CLAUDE.md
- Constitution requires .fsi signatures for all new public modules (MapGrid, MapQuery, MapCache)
- Constitution requires surface-area baseline tests for public API modules
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
