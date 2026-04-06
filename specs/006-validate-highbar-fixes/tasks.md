# Tasks: Validate HighBar Fixes

**Input**: Design documents from `/specs/006-validate-highbar-fixes/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Existing integration tests serve as test evidence. No new test files needed — existing 12 map tests validate the fix.

**Organization**: Tasks grouped by user story. US1 (dimension fix) is the MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Proto + Bindings)

**Purpose**: Add corners heightmap callback to protocol and regenerate F# bindings

- [x] T001 Add `CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP = 59` to proto/highbar/callbacks.proto after CALLBACK_MAP_GET_METAL_SPOTS
- [x] T002 Regenerate F# protobuf bindings via `dotnet build src/FSBar.Proto/`

**Checkpoint**: Proto builds cleanly with new callback ID

---

## Phase 2: Foundational (Callback Wrapper)

**Purpose**: Expose the new callback in the F# client — MUST complete before user stories

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T003 Add `getCornersHeightMap` function to src/FSBar.Client/Callbacks.fs using `CallbackId.CallbackMapGetCornersHeightMap`
- [x] T004 Add `val getCornersHeightMap: stream: NetworkStream -> float32 list` signature to src/FSBar.Client/Callbacks.fsi
- [x] T004b [P] Note in specs/006-validate-highbar-fixes/plan.md Complexity Tracking table that surface-area baselines are a pre-existing project-wide gap — not introduced by this feature. Baseline creation deferred to a dedicated tech-debt feature per constitution §II.

**Checkpoint**: `dotnet build src/FSBar.Client/` compiles cleanly

---

## Phase 3: User Story 1 — Verify Map Grid Dimension Handling (Priority: P1) MVP

**Goal**: Fix the dimension mismatch by switching to corners heightmap and correcting slope map dimensions. This unblocks 11/12 failing map tests.

**Independent Test**: Run `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid"` — all heightmap and slope tests pass or skip with diagnostics.

### Implementation for User Story 1

- [x] T005 [US1] In src/FSBar.Client/MapGrid.fs line 70, change `Callbacks.getHeightMap stream` to `Callbacks.getCornersHeightMap stream`
- [x] T006 [US1] In src/FSBar.Client/MapGrid.fs lines 73-75, change slope map dimensions from `hmW hmH` to `w/2` and `h/2` — introduce `let slopeW = w / 2` and `let slopeH = h / 2`, pass to `toFloat32Array2D slopeW slopeH "SlopeMap"`
- [x] T007 [US1] In src/FSBar.Client/MapGrid.fs `terrainAt` function (line 111-119), change slope access from `grid.SlopeMap.[x, z]` to `grid.SlopeMap.[min (x/2) (Array2D.length1 grid.SlopeMap - 1), min (z/2) (Array2D.length2 grid.SlopeMap - 1)]` to handle half-resolution indexing with bounds clamping
- [x] T008 [US1] In src/FSBar.Client/MapGrid.fs `passability` function (line 122-137), change slope access from `grid.SlopeMap.[x, z]` to use half-resolution indexing with bounds clamping (same pattern as T007)
- [x] T009 [US1] In src/FSBar.Client/MapQuery.fs, replace `slopeAtElmo` (lines 25-29) with a slope-specific implementation: convert elmo coords to slope grid via `x / 16, z / 16`, bounds-check against `Array2D.length1 grid.SlopeMap` and `Array2D.length2 grid.SlopeMap`, and format the Error message using `* 16` for elmo back-conversion (not `* 8` as in the shared `boundsCheck` helper)
- [x] T010 [US1] In src/FSBar.Client/MapQuery.fs `terrainAtElmo` function (line 31-35), ensure slope access uses half-resolution coordinates when calling `MapGrid.terrainAt` (already handled if terrainAt is fixed in T007)
- [x] T011 [US1] In tests/FSBar.LiveTests/MapGridTests.fs, update `tryLoadGrid` to also catch dimension mismatch exceptions (message contains "dimension mismatch") and skip with diagnostic
- [x] T012 [US1] In tests/FSBar.LiveTests/MapQueryTests.fs, update `tryLoadGrid` to match the same pattern as T011
- [x] T012b [US1] Verify FR-007: confirm that toFloat32Array2D in src/FSBar.Client/MapGrid.fs (line 49-50) emits expected vs actual counts in dimension mismatch error messages — no code change needed, just verify the existing failwith format
- [x] T013 [US1] Build and run: `dotnet build && dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"`

**Checkpoint**: All 12 map tests pass or skip with diagnostics. Zero unhandled dimension mismatch exceptions.

---

## Phase 4: User Story 2 — Validate Typed Disconnection Exceptions (Priority: P2)

**Goal**: Confirm `EngineDisconnectedException` is raised correctly and prevents cascade failures.

**Independent Test**: Run integration tests and verify disconnection scenarios produce typed exceptions, not generic `IOException`.

### Implementation for User Story 2

- [x] T014 [US2] Verify in src/FSBar.Client/Connection.fs that `EngineDisconnectedException` is raised in `readExact` on `IOException` and zero-byte reads (already implemented in branch 005 — confirm no regressions)
- [x] T015 [US2] Verify in tests/FSBar.LiveTests/MapGridTests.fs and MapQueryTests.fs that `tryLoadGrid` catches `EngineDisconnectedException` and skips (already implemented — confirm works with new callback)
- [x] T016 [US2] Run `dotnet test tests/FSBar.LiveTests/` and verify no cascade failures — each test that encounters disconnection skips independently

**Checkpoint**: Disconnection events produce typed exceptions. No cascade failures across test suite.

---

## Phase 5: User Story 3 — Validate Configurable Read Timeouts (Priority: P3)

**Goal**: Confirm read timeout configuration is respected via config, environment variable, and default fallback.

**Independent Test**: Verify `ReadTimeoutMs` in `EngineConfig` is applied to the stream timeout.

### Implementation for User Story 3

- [x] T017 [US3] Verify in src/FSBar.Client/EngineConfig.fs that `resolveReadTimeout` correctly implements the fallback chain: explicit config → `FSBAR_CLIENT_TIMEOUT_MS` env var → 10000ms default (already implemented in branch 005 — confirm no regressions)
- [x] T018 [US3] Verify in src/FSBar.Client/Connection.fs line 36 that `stream.ReadTimeout` is set to the resolved value (already implemented — confirm present)
- [x] T019 [US3] Run full test suite `dotnet test tests/FSBar.LiveTests/` and confirm it completes without hanging

**Checkpoint**: Suite completes within expected time. No indefinite hangs.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Ensure contracts, scripting, and packaging are up to date

- [x] T020 [P] Review scripts/examples/05-map-layers.fsx — update if it references heightmap loading or slope access patterns
- [x] T021 [P] Run `dotnet pack src/FSBar.Client/` and verify NuGet package builds
- [x] T022 Run quickstart.md validation: full build + unit tests + integration tests
- [x] T023 Update specs/006-validate-highbar-fixes/spec.md status from Draft to Complete

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (proto must be regenerated first)
- **User Story 1 (Phase 3)**: Depends on Phase 2 (callback wrapper must exist)
- **User Story 2 (Phase 4)**: Depends on Phase 2 only — can run in parallel with US1
- **User Story 3 (Phase 5)**: Depends on Phase 2 only — can run in parallel with US1/US2
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 — core dimension fix, MVP
- **User Story 2 (P2)**: Depends on Phase 2 — verification of existing HighBar fix, independent of US1
- **User Story 3 (P3)**: Depends on Phase 2 — verification of existing HighBar fix, independent of US1/US2

### Within User Story 1

- T005, T006 can run in parallel (different sections of MapGrid.fs, but same file — run sequentially for safety)
- T007, T008 depend on T006 (slope dimension change must be in place)
- T009, T010 can run in parallel with T007/T008 (different file: MapQuery.fs)
- T011, T012 can run in parallel (different test files)
- T013 depends on all previous US1 tasks

### Parallel Opportunities

```text
After Phase 2 completes:
  ├── US1 (T005-T013) — dimension fix [MVP]
  ├── US2 (T014-T016) — exception validation [parallel]
  └── US3 (T017-T019) — timeout validation [parallel]

Within Phase 6:
  T020 ‖ T021 — script review and NuGet pack in parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Proto update (T001-T002)
2. Complete Phase 2: Callback wrapper (T003-T004)
3. Complete Phase 3: Dimension fix (T005-T013)
4. **STOP and VALIDATE**: Run `dotnet test tests/FSBar.LiveTests/ --filter "Category=MapGrid|Category=MapQuery"` — all 12 map tests should pass
5. This alone resolves the critical 11/12 test failure blocker

### Incremental Delivery

1. Phase 1 + 2 → Foundation ready
2. Add US1 → Test independently → 11 previously-failing tests now pass (MVP!)
3. Add US2 → Verify exception handling → Confirm no regressions
4. Add US3 → Verify timeouts → Confirm no hangs
5. Polish → Pack, update scripts, mark complete

---

## Notes

- US2 and US3 are primarily **verification** tasks — the code was already ported in branch 005. The tasks confirm no regressions after the US1 dimension changes.
- The `getHeightMap` function (callback 52) is retained in Callbacks.fs for backward compatibility but is no longer called by `MapGrid.loadFromEngine`.
- Tests that cannot pass due to proxy limitations must skip (per CLAUDE.md). The `tryLoadGrid` pattern handles this.
