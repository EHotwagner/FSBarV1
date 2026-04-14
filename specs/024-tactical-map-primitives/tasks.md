---
description: "Task list for 024-tactical-map-primitives — slope-aware pathing, chokepoint analysis, building plans, anti wall-in checks, SMF parser, and macro-bot integration"
---

# Tasks: Tactical Map Primitives

**Input**: Design documents from `/specs/024-tactical-map-primitives/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/pathing.md, contracts/smf-parser.md, contracts/chokepoints.md, contracts/base-plan.md, contracts/wall-in.md, quickstart.md

**Tests**: Tests are MANDATORY for this feature. Constitution §III (Test Evidence Is Mandatory) plus Tier 1 obligations plus SC-001..SC-010 all require explicit test tasks. The three-layer test strategy from plan.md Technical Context governs test ordering: synthetic `MapGrid` unit tests (Layer 1), SMF-parsed real-map integration tests (Layer 2), live bot iteration tests (Layer 3).

**Organization**: Tasks are grouped by the spec's five user stories plus the shared SMF parser (which lands in Phase 2 Foundational because every user-story test needs it for Layer-2 coverage).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1..US5 from spec.md
- File paths are absolute from repo root (`/home/developer/projects/FSBarV1`)

## Path Conventions

Tier 1 library extension to `FSBar.Client` (per clarification Q1). All new library code lives in `src/FSBar.Client/`; all new tests live in `src/FSBar.Client.Tests/` (NOT under `tests/` — the existing test project is `src/FSBar.Client.Tests`). New FSI example scripts live in `scripts/examples/`.

**Technical dependency ordering note**: the spec's user story priorities are US1 (pathing, P1), US2 (chokepoints, P1), US3 (building plans, P1), US4 (wall-in, P2), US5 (integration, P2). The implementation order below **swaps US3 and US4** because `BasePlan.resolvePlan` depends on `WallIn.wouldWallIn` per FR-023 — US3 cannot be completed without US4 in place. Each story is still independently testable once its prerequisites are met.

---

## Phase 1: Setup

**Purpose**: Verify branch + inventory existing state + create shared test fixture helper before any module work begins.

- [X] T001 Verify current branch is `024-tactical-map-primitives` via `git rev-parse --abbrev-ref HEAD`. Stop and ask the operator if it is not. (No file edit — gate only.)
- [X] T002 Inventory `src/FSBar.Client/FSBar.Client.fsproj` — confirm existing compile order and identify the insertion point for the five new `.fs`/`.fsi` pairs. The order is fixed by F# forward-declaration rules: `Pathing` before `SmfParser` before `Chokepoints` before `WallIn` before `BasePlan`. Record the chosen `<Compile Include>` position in a scratch note under `specs/024-tactical-map-primitives/` (operator memory only; not a deliverable).
- [X] T003 [P] Confirm `bsdtar` is present: `which bsdtar` must return a non-empty path. If absent, halt and escalate to the operator with a plan-phase research question (per spec Assumption on `.sd7` extraction tooling).
- [X] T004 [P] Verify BAR installation has Avalanche 3.4 at `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7`. If missing, Layer-2 integration tests will be skipped — operator decides whether to install or proceed with Layer-1 only.

**Checkpoint**: Branch is current, fsproj insertion point known, extraction tool + test-map availability confirmed.

---

## Phase 2: Foundational

**Purpose**: Land all five `.fsi` contracts + stub `.fs` files so the project builds with the new surface area; implement the **full** `SmfParser` because every user story's Layer-2 integration tests consume it; create the synthetic `MapGrid` fixture helper that all Layer-1 unit tests share; wire up the surface-area baseline harness for the new modules.

**⚠️ CRITICAL**: No user story work begins until Phase 2 is complete. These tasks are the shared foundation that all of US1–US5 depend on. Within Phase 2, the `.fsi` + stub block must land first (so the project compiles), then `SmfParser` full implementation, then the test fixture helper + baseline harness.

### 2a — `.fsi` contracts and stub `.fs` files (compile-order)

- [X] T005 Create `src/FSBar.Client/Pathing.fsi` with the exact public surface from `contracts/pathing.md` §"Public API surface": `OwnStructureFootprint`, `PathStatus`, `PathFailure`, `Path`, `PathBudget`, `module Pathing` with `defaultPathBudget`, `findPath`, `pathCost`, `rasteriseFootprints`.
- [X] T006 Create `src/FSBar.Client/Pathing.fs` as a compile-only stub: every `val` in the `.fsi` has a matching `let` with body `failwith "not implemented — T023"`. Records and DUs fully implemented (they're just types). This lets the project compile while implementation is pending.
- [X] T007 Create `src/FSBar.Client/SmfParser.fsi` with the exact public surface from `contracts/smf-parser.md` §"Public API surface": `SmfMap`, `SmfParseError`, `module SmfParser` with `parseSd7`, `parseBytes`, `toMapGrid`, `listInstalledMaps`.
- [X] T008 Create `src/FSBar.Client/SmfParser.fs` as a compile-only stub (same pattern as T006).
- [X] T009 Create `src/FSBar.Client/Chokepoints.fsi` with the exact public surface from `contracts/chokepoints.md`: `ChokepointId`, `Chokepoint`, `ChokepointQuery`, `module Chokepoints` with `defaultChokepointQuery`, `findChokepoints`, `chokepointIdOf`, `computeDistanceTransform`.
- [X] T010 Create `src/FSBar.Client/Chokepoints.fs` as a compile-only stub.
- [X] T011 Create `src/FSBar.Client/WallIn.fsi` with the exact public surface from `contracts/wall-in.md`: `WallInReason`, `WallInResult`, `WallInQuery`, `module WallIn` with `defaultWallInQuery`, `wouldWallIn`, `reachableCells`.
- [X] T012 Create `src/FSBar.Client/WallIn.fs` as a compile-only stub.
- [X] T013 Create `src/FSBar.Client/BasePlan.fsi` with the exact public surface from `contracts/base-plan.md`: `PositionChooser`, `PlanSlot`, `BasePlan`, `SlotFailure`, `ResolvedSlot`, `PlanProgress`, `ResolveContext`, `module BasePlan` with `defaultArmadaOpening`, `emptyPlanProgress`, `resolvePlan`, `markConsumed`, `markInFlight`, `markUnfulfillable`.
- [X] T014 Create `src/FSBar.Client/BasePlan.fs` as a compile-only stub.
- [X] T015 Update `src/FSBar.Client/FSBar.Client.fsproj`: add the ten new `<Compile Include>` entries in the exact dependency order `Pathing.fsi → Pathing.fs → SmfParser.fsi → SmfParser.fs → Chokepoints.fsi → Chokepoints.fs → WallIn.fsi → WallIn.fs → BasePlan.fsi → BasePlan.fs`, inserted after the existing `BarClient.fs` entry. Run `dotnet build src/FSBar.Client/FSBar.Client.fsproj -c Debug --nologo` and confirm a warning-only (no-error) build.

### 2b — Shared test fixture helper

- [X] T016 Create `src/FSBar.Client.Tests/SyntheticMapGrid.fs` exposing a `module SyntheticMapGrid` with factories: `flat : width:int -> height:int -> MapGrid` (all cells Land, slope 0, no metal, LOS/radar zero), `withWall : grid:MapGrid -> x1:int -> z1:int -> x2:int -> z2:int -> MapGrid` (stamps impassable cells along a line), `withCliff : grid:MapGrid -> centreX:int -> centreZ:int -> radius:int -> MapGrid` (stamps a circular high-slope zone), `withMetalSpot : grid:MapGrid -> x:int -> z:int -> value:int -> MapGrid`, `oneGapCorridor : width:int -> height:int -> gapCells:int -> MapGrid` (a `width × height` grid with a single N-S impassable wall containing a gap of `gapCells` open cells at the vertical midpoint). Add to `FSBar.Client.Tests.fsproj` as `<Compile Include>` before any `*Tests.fs` file.

### 2c — SMF parser full implementation (Layer-2 enabler for every user story)

- [X] T017 Implement `src/FSBar.Client/SmfParser.fs` (replace the T008 stub): `parseBytes` validates magic `spring map file\0`, reads SMF v1 header (version + width + height + tile index + heightmap offset + metal map offset + type map offset), decodes int16 heightmap to float32 world heights, decodes uint8 metal + type maps, computes slope map locally via the Spring formula from research R3, assembles `SmfMap`. `parseSd7` shells out to `bsdtar -xf <sd7> -C <temp> 'maps/*.smf'`, reads the extracted `.smf` bytes, cleans up the temp dir, delegates to `parseBytes`. `toMapGrid` copies layers into a `MapGrid` record with zero-initialised LOS/radar. `listInstalledMaps` scans `~/.local/state/Beyond All Reason/maps/*.sd7`. All failure modes return `Result.Error` with the appropriate `SmfParseError` variant (no exceptions for bounded failures).
- [X] T018 Create `src/FSBar.Client.Tests/SmfParserTests.fs` with Layer-1 unit tests: `parseBytes` with a hand-crafted minimal valid 8×8 SMF blob → `Ok SmfMap` with correct dimensions; bad magic → `Error InvalidMagic`; version=2 → `Error UnsupportedVersion`; truncated heightmap → `Error Truncated`; `toMapGrid` round-trip preserves heightmap dimensions. Add to `FSBar.Client.Tests.fsproj`.
- [X] T019 [P] Add Layer-2 integration tests to `src/FSBar.Client.Tests/SmfParserTests.fs`: `parseSd7` on `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7` → `Ok SmfMap` with `WidthHeightmap=512, HeightHeightmap=512`; heightmap min/max within ±1 elmo of the live-engine-captured reference values (130.0 / 700.0 per the 2026-04-06 HighBarV2 mailbox). Tests use `Skip` when the `.sd7` file is absent. Satisfies **SC-010**.
- [X] T020 [P] Create `src/FSBar.Client.Tests/Baselines/SmfParser.baseline` by invoking the existing `BaselineSerializer` against `SmfParser.fsi`. Add a baseline assertion for `SmfParser` in `src/FSBar.Client.Tests/SurfaceAreaTests.fs`.

### 2d — Baseline harness extension for the remaining four modules (stub baselines now, regenerated per story)

- [X] T021 Update `src/FSBar.Client.Tests/SurfaceAreaTests.fs`: add baseline assertions for `Pathing`, `Chokepoints`, `WallIn`, `BasePlan`. Generate initial empty / stub baseline files (`Pathing.baseline`, `Chokepoints.baseline`, `WallIn.baseline`, `BasePlan.baseline` under `Baselines/`) so the test harness compiles and runs. These baselines will be regenerated as each user story lands its `.fs` implementation.

### 2e — FSI example script for SmfParser

- [X] T022 [P] Create `scripts/examples/10-smf.fsx` matching the quickstart.md §1 script shape: loads `prelude.fsx`, parses Avalanche 3.4, prints dimensions + heightmap range. Verify it runs via `dotnet fsi scripts/examples/10-smf.fsx` without errors when BAR is installed.

**Checkpoint**: Foundation ready. `FSBar.Client` compiles with five new `.fsi` + stub `.fs` pairs. `SmfParser` is **fully implemented** with unit + integration test coverage and a passing surface-area baseline. `SyntheticMapGrid` factory is available to all downstream tests. The four remaining module baselines are stubs ready for regeneration as each story's implementation lands. User story phases can begin.

---

## Phase 3: User Story 1 — Slope-aware pathing (P1) 🎯 MVP

**Goal**: `Pathing.findPath` produces deterministic slope-weighted A\* paths over `MapGrid` + `OwnStructureFootprint` mask, satisfying FR-001..FR-006a and SC-001/SC-002.

**Independent Test**: `PathingTests.fs` Layer-1 unit tests pass against synthetic `MapGrid` fixtures (straight-line flat, detour around cliff, no-route for impassable terrain, budget exhaustion, determinism). Layer-2 integration tests pass against Avalanche 3.4 (cross-map Kbot path from base to enemy commander). The `Pathing.baseline` surface-area assertion passes.

- [X] T023 [US1] Create `src/FSBar.Client.Tests/PathingTests.fs` with failing Layer-1 unit tests: straight-line path on a `SyntheticMapGrid.flat 64 64` → waypoints trace the line, cost = straight-line distance; path around a central cliff via `withCliff` → returned waypoints detour, every waypoint is Kbot-passable; no route for `MoveType.Tank` through a terrain with a cliff-gap only Kbot can climb → `Error NoRoute`; `start` off-map → `Error OutOfBounds`; `start` on impassable cell → `Error EndpointImpassable`; `ownStructures` mask blocks a clear route → detour returned; budget exhaustion via small `WallClockMs` → `Status = Partial true` with non-empty partial path; determinism (two consecutive `findPath` calls identical). Add to `FSBar.Client.Tests.fsproj`.
- [X] T024 [US1] Implement the A\* algorithm in `src/FSBar.Client/Pathing.fs` (replacing the T006 stub): `rasteriseFootprints` overlays a passability mask; `findPath` uses a binary-heap priority queue keyed by `(f, linearisedIdx)` for deterministic tie-breaking; octile 8-neighbour model; edge cost = `distance × (1 + slope × budget.SlopeCost)`; octile heuristic (admissible). Cover FR-001 (function signature), FR-002 (cost function + ownStructures mask), FR-003 (determinism).
- [X] T025 [US1] Implement budget enforcement in `src/FSBar.Client/Pathing.fs`: `Stopwatch` started at entry, expansion counter, every 256 expansions check `WallClockMs` + `MaxExpansions`; on threshold crossing, reconstruct the best partial path from the lowest-f-score node with a valid parent trail back to `start` and return `Status = Partial true`. Cover FR-005.
- [X] T026 [US1] Implement waypoint post-processing in `src/FSBar.Client/Pathing.fs`: after raw grid-cell path recovery, collapse collinear consecutive cells into waypoints; verify each consecutive-waypoint pair connects via a straight-line Bresenham walk over passable cells (re-insert intermediate cells if the straight-line invariant fails). Cover FR-004.
- [X] T027 [US1] Implement `pathCost` in `src/FSBar.Client/Pathing.fs`: re-sum edge weights of a supplied `Path` under a caller-provided `slopeCost` value, returning a new float. Cover FR-006.
- [X] T028 [US1] Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --filter "Pathing"`. All Layer-1 tests MUST pass. Confirm determinism test explicitly passes (two `findPath` calls with identical inputs produce byte-identical `Path` records).
- [X] T029 [US1] Add Layer-2 integration tests to `src/FSBar.Client.Tests/PathingTests.fs` (skipped when `.sd7` absent): SMF-parse `avalanche_3.4.sd7`, convert to `MapGrid` via `SmfParser.toMapGrid`, run `findPath` from `(500, 350, 397)` to `(3699, 344, 3601)` for `MoveType.Kbot` with `Seq.empty` ownStructures and `defaultPathBudget` → `Ok path` with `Status = Complete` and `Waypoints.Length ≥ 2`. Satisfies **SC-001** (path query completes within budget) and **SC-002** (map with a known ridge returns a detour).
- [X] T030 [US1] Regenerate `src/FSBar.Client.Tests/Baselines/Pathing.baseline` from the now-real `Pathing.fsi` surface. Re-run `dotnet test` — `SurfaceAreaTests.Pathing` MUST pass against the regenerated baseline.
- [X] T031 [P] [US1] Create `scripts/examples/11-pathing.fsx` per quickstart.md §2: SMF-parse Avalanche 3.4, run `findPath` for Kbot, print waypoints + cost + status. Runs via `dotnet fsi scripts/examples/11-pathing.fsx` successfully.
- [X] T032 [US1] Commit Phase 3 as one commit `tactical: US1 pathing — A* with slope-weighted edges + ownStructures mask`, push to `origin/024-tactical-map-primitives`.

**Checkpoint (SC-001, SC-002)**: `Pathing` module fully landed with Layer-1 + Layer-2 test coverage and a passing surface-area baseline. `bot_macro.fsx` is NOT yet consuming it (that's US5). Independent test: run `dotnet test --filter "Pathing"` + `dotnet fsi scripts/examples/11-pathing.fsx`.

---

## Phase 4: User Story 2 — Chokepoint detection (P1)

**Goal**: `Chokepoints.findChokepoints` returns stable-ID width-annotated chokepoint descriptors, satisfying FR-007..FR-011 and SC-003.

**Independent Test**: `ChokepointsTests.fs` Layer-1 tests pass on synthetic one-gap-wall grids, multi-corridor grids, open terrain (returns `[]`), and determinism. Layer-2 SC-003 test passes against Avalanche 3.4 (top-1 chokepoint within ±150 elmos of the human-recognised canyon entrance leading to the NullAI spawn). `Chokepoints.baseline` passes.

- [X] T033 [US2] Create `src/FSBar.Client.Tests/ChokepointsTests.fs` with failing Layer-1 unit tests: single-gap corridor via `SyntheticMapGrid.oneGapCorridor 32 32 3` → exactly one chokepoint at the gap with `WidthElmos ≈ 24.0f`; two parallel corridors → two chokepoints ordered by distance from base centre; fully open grid → `[]` (FR-009 verification); two consecutive calls with identical inputs → identical result lists (determinism); `chokepointIdOf` returns identical IDs for the same ridge cell across two calls (FR-011 stability). Add to `FSBar.Client.Tests.fsproj`.
- [X] T034 [US2] Implement the two-pass Felzenszwalb-Huttenlocher distance transform in `src/FSBar.Client/Chokepoints.fs` (`computeDistanceTransform`): squared-Euclidean distance-to-nearest-impassable over the passability grid with an optional `ownStructures` overlay. Output dimensions match `MapGrid.passability`.
- [X] T035 [US2] Implement ridge detection + radius/width filter in `src/FSBar.Client/Chokepoints.fs`: 3×3 local-maximum filter over the distance transform → candidate ridge cells; filter by `query.SearchRadiusElmos` around `baseCentre`; filter by `2 × dt[cell] × 8 < query.MaxWidthElmos`.
- [X] T036 [US2] Implement the primary-route filter in `src/FSBar.Client/Chokepoints.fs` (FR-010): for each candidate ridge, temporarily mark the cell impassable, flood-fill from `baseCentre`, compare reachable region sizes; drop ridges whose removal doesn't shrink the reachable region by ≥10%. Surviving candidates are the chokepoints.
- [X] T037 [US2] Implement `findChokepoints` assembly in `src/FSBar.Client/Chokepoints.fs`: compute `OutwardDir` as the normalised vector from `baseCentre` to the ridge cell; assemble `Chokepoint` records with stable IDs derived from `(widthHeightmap, heightHeightmap, linearisedRidgeCellIdx)`; sort by `DistanceFromBase` ascending and return.
- [X] T038 [US2] Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --filter "Chokepoints"`. All Layer-1 tests MUST pass.
- [X] T039 [US2] Add SC-003 integration test to `src/FSBar.Client.Tests/ChokepointsTests.fs` (skipped when `.sd7` absent): SMF-parse Avalanche 3.4, call `findChokepoints` from base `(500, 397)` with `defaultChokepointQuery MoveType.Kbot` → top-1 result's `Position` coincides (within ±150 elmos, verified by operator-captured reference constant) with the canyon entrance to the NullAI spawn area. The reference constant is captured by running `scripts/examples/12-chokepoints.fsx` (T041) once and inspecting the output.
- [X] T040 [US2] Regenerate `src/FSBar.Client.Tests/Baselines/Chokepoints.baseline` from the now-real `Chokepoints.fsi` surface. Re-run `dotnet test` — `SurfaceAreaTests.Chokepoints` MUST pass.
- [X] T041 [P] [US2] Create `scripts/examples/12-chokepoints.fsx` per quickstart.md §3. Run it once against Avalanche 3.4 and capture the top-1 chokepoint `Position` as the SC-003 reference constant in T039. Verify the script runs successfully.
- [X] T042 [US2] Commit Phase 4 as one commit `tactical: US2 chokepoints — distance-transform ridges + primary-route filter`, push.

**Checkpoint (SC-003)**: `Chokepoints` module fully landed. Independent test: `dotnet test --filter "Chokepoints"` + `dotnet fsi scripts/examples/12-chokepoints.fsx`. `bot_macro.fsx` still not consuming (US5 deferred).

---

## Phase 5: User Story 4 — Anti wall-in check (P2)

**Goal**: `WallIn.wouldWallIn` is a pure connectivity predicate sharing passability rules with `Pathing`, satisfying FR-019..FR-023 and SC-005.

**Independent Test**: `WallInTests.fs` Layer-1 tests pass on synthetic one-corridor bases (reject placement in corridor, accept placement to the side, accept loop-closing placements with surviving paths). Cross-primitive invariant test passes (shared passability with `Pathing`). `WallIn.baseline` passes.

**Why US4 before US3**: US3 (`BasePlan.resolvePlan`) integrates `WallIn.wouldWallIn` via FR-023, so WallIn MUST land before BasePlan. Spec priority ordering (US3 P1, US4 P2) reflects **value** priority, not implementation dependency.

- [X] T043 [US4] Create `src/FSBar.Client.Tests/WallInTests.fs` with failing Layer-1 unit tests: staged one-corridor base with a proposed placement in the corridor → `Fails (DisconnectsStructures [...])` with the factory name in the list; same base with proposed placement to the side → `Passes`; proposed placement that closes a loop but leaves alternate paths → `Passes`; placement that isolates base from map edge (with `RequireMapEdgeExit = true`) → `Fails EnclosesBase`; purity (two calls → identical result, `ownStructures` list unchanged); shared-passability with `Pathing` (a placement that passes `wouldWallIn` produces a non-`NoRoute` `findPath` for all reachable pairs). Add to `FSBar.Client.Tests.fsproj`.
- [X] T044 [US4] Implement `reachableCells` flood fill in `src/FSBar.Client/WallIn.fs`: BFS from origin cell over `(MapGrid.passability grid moveType)` masked by `ownStructures` footprints (same rasterise as `Pathing.rasteriseFootprints`). Returns `bool[,]` the same shape as `passability`. Cover FR-020 passability sharing.
- [X] T045 [US4] Implement `wouldWallIn` in `src/FSBar.Client/WallIn.fs`: compute pre-reachability from `baseCentre`, verify each existing structure's centre is in the pre-set, compute post-reachability with `proposed` added to the mask, diff the two sets to identify disconnected structures; when `RequireMapEdgeExit` is set, also verify the post-set contains at least one map-edge cell. Returns `Passes | Fails`. Cover FR-019, FR-021 (diagnostic payload), FR-022 (purity).
- [X] T046 [US4] Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --filter "WallIn"`. All Layer-1 tests MUST pass, including the **cross-primitive invariant** test (FR-020): construct a scenario where `wouldWallIn` says `Passes` and verify `Pathing.findPath` can actually cross the disputed cells. Satisfies **SC-005**.
- [X] T047 [US4] Regenerate `src/FSBar.Client.Tests/Baselines/WallIn.baseline`. Re-run `dotnet test` — `SurfaceAreaTests.WallIn` MUST pass.
- [X] T048 [US4] Commit Phase 5 as one commit `tactical: US4 wall-in — connectivity predicate with shared passability`, push.

**Checkpoint (SC-005, partial)**: `WallIn` module fully landed. Independent test: `dotnet test --filter "WallIn"`. Ready for US3 integration.

---

## Phase 6: User Story 3 — Declarative building plans (P1)

**Goal**: `BasePlan.resolvePlan` turns named slot lists into placement decisions that honour terrain, clearance (FR-013 / Q4 additive margin), builder reach, wall-in (FR-023 via `WallIn.wouldWallIn`), and plan consumption semantics. `defaultArmadaOpening` matches the 023 iter 026 opening. Satisfies FR-012..FR-018, FR-023, and SC-004.

**Independent Test**: `BasePlanTests.fs` Layer-1 tests pass on synthetic grids (resolve `defaultArmadaOpening` → 5 non-failure slots; clearance collision; wall-in rejection via `WallIn.wouldWallIn`; off-map; out-of-reach; `NoMetalSpot` when index exceeds available spots; `PlanProgress` round-trips). Layer-2 integration test passes against Avalanche 3.4. `BasePlan.baseline` passes.

**Depends on**: US1 (`Pathing` for reach checks), US4 (`WallIn.wouldWallIn` for FR-023), `UnitDefCache` (existing).

- [X] T049 [US3] Create `src/FSBar.Client.Tests/BasePlanTests.fs` with failing Layer-1 unit tests: `resolvePlan defaultArmadaOpening` on a synthetic flat 64×64 grid with 2 metal spots → 5 non-failure `ResolvedSlot`s with deterministic positions (two calls produce identical output); slot with `NearestMetalSpot 2` on a 2-spot grid → `Failure = NoMetalSpot 2`; two slots targeting the same `NearBaseCentre` offset → second returns `Failure = ClearanceCollision "first slot name"`; synthetic one-corridor base whose factory slot would close the corridor → `Failure = WouldWallIn [...]`; `markConsumed` / `markInFlight` / `markUnfulfillable` round-trip through `PlanProgress` and subsequent `resolvePlan` calls correctly skip / retry / permanently reject; off-map slot via `NearBaseCentre(-10000, 0)` → `Failure = OffMap`. Add to `FSBar.Client.Tests.fsproj`.
- [X] T050 [US3] Implement types + `PlanProgress` helpers in `src/FSBar.Client/BasePlan.fs` (replacing the T014 stub): `emptyPlanProgress`, `markConsumed`, `markInFlight`, `markUnfulfillable` — pure record updates. Cover FR-018.
- [X] T051 [US3] Implement `resolvePlan` position-chooser dispatch in `src/FSBar.Client/BasePlan.fs`: for each slot, resolve `PositionChooser` against `ResolveContext` (`NearestMetalSpot n` → indexed metal spot with `Failure = NoMetalSpot` on out-of-range; `NearCommander`/`NearBaseCentre` → offset arithmetic; `AtChokepointHead k` → indexed chokepoint with `Failure = UnresolvedDependency` on out-of-range; `AtLiteralPosition` → pass-through). Cover FR-013 (chooser enum), FR-014 (function signature).
- [X] T052 [US3] Implement the `resolvePlan` validation pipeline in `src/FSBar.Client/BasePlan.fs`: bounds check → `OffMap`; terrain check via `MapQuery.terrainAtElmo` → `TerrainNotBuildable` for non-Land cells; **edge-to-edge clearance margin check per Q4** → `ClearanceCollision` against existing + previously-resolved footprints; builder reach check via **straight-line 2D distance** from the builder's current position to the proposed centre (per `contracts/base-plan.md` §5 — path-distance reach is out of scope for 024 and can be added in a follow-up feature if the gap matters in practice) → `OutOfBuilderReach`. Cover FR-015.
- [X] T053 [US3] Integrate `WallIn.wouldWallIn` into `resolvePlan` in `src/FSBar.Client/BasePlan.fs` per FR-023: for each candidate placement, call `WallIn.wouldWallIn` with the proposed footprint added to `ResolveContext.ExistingStructures`; on `Fails`, set `Failure = WouldWallIn unreachableStructureNames`.
- [X] T054 [US3] Implement `defaultArmadaOpening` in `src/FSBar.Client/BasePlan.fs` per FR-016 and contracts/base-plan.md §`defaultArmadaOpening`: 2 `armmex NearestMetalSpot 0/1`, 2 `armsolar NearBaseCentre ±200, 0`, 1 `armlab NearBaseCentre 0, 350`, all with `armcom` as builder, `ClearanceMargin = 16.0f` (32.0f for the factory), `MaxRetries = 3` (2 for the factory). Matches 023 iter 026 layout exactly.
- [X] T055 [US3] Run `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug --filter "BasePlan"`. All Layer-1 tests MUST pass.
- [X] T056 [US3] Add Layer-2 integration test to `src/FSBar.Client.Tests/BasePlanTests.fs` (skipped when `.sd7` absent): SMF-parse Avalanche 3.4, construct a `ResolveContext` with metal spots from the SMF metal map, call `resolvePlan defaultArmadaOpening` → 5 non-failure `ResolvedSlot` records. Satisfies **SC-004**.
- [X] T057 [US3] Regenerate `src/FSBar.Client.Tests/Baselines/BasePlan.baseline`. Re-run `dotnet test` — `SurfaceAreaTests.BasePlan` MUST pass.
- [X] T058 [P] [US3] Create `scripts/examples/13-plan.fsx` per quickstart.md §4: SMF-parse Avalanche 3.4, build a `ResolveContext`, call `resolvePlan defaultArmadaOpening`, print slot resolutions with `[ok]` / `[fail]` prefixes.
- [X] T059 [US3] Commit Phase 6 as one commit `tactical: US3 base plan — resolvePlan with clearance, reach, and wall-in integration`, push.

**Checkpoint (SC-004)**: All four module primitives landed (`Pathing`, `Chokepoints`, `WallIn`, `BasePlan`) plus `SmfParser`. Independent test: `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` — all module tests + all surface-area baselines MUST pass. Ready for US5 deep bot integration.

---

## Phase 7: User Story 5 — Deep macro bot integration (P2)

**Goal**: `bot_macro.fsx` is refactored in a **single atomic commit** (per clarification Q5) to replace its opening logic with `resolvePlan`, its attack launch with `findPath`, and its defend interrupt with `findChokepoints`. The refactored bot wins cleanly on NullAI with `cause = "commander-death-win-after-upgrade"`. Rush bot (`bot.fsx`) remains unchanged and still wins. Satisfies FR-029..FR-031 and SC-006/SC-007.

**Independent Test**: One iteration of `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 024-smoke` produces `result.json.cause = "commander-death-win-after-upgrade"`, `phase_transitions.jsonl` shows Opening→Production→Upgrade→Attack, and stdout contains `[plan] resolved 5 slots`, `[attack] path waypoints=N`, `[defend] chokepoint pos=(X,Y) width=W` observability traces. Rush bot smoke still wins.

**Critical discipline** (Q5 / clarification): if the deep refactor surfaces a regression on its first run, subsequent iterations MUST fix exactly one issue at a time per the 023 PLAYBOOK §2c "one fix per iter" rule. The "fix everything in one more commit" temptation is explicitly out of scope.

- [X] T060 [US5] Modify `bots/trainer/bot_macro.fsx` — US5 **deep integration commit** (atomic): (a) add `open FSBar.Client` references for `SmfParser`, `Pathing`, `Chokepoints`, `BasePlan`, `WallIn`; (b) at warmup, load the current map via `Callbacks.getMetalSpots` + a new `MapGrid.loadFromEngine`-equivalent path (the existing helper already produces a `MapGrid` for live engine consumption), resolve `BasePlan.defaultArmadaOpening` against a `ResolveContext` built from the live `GameState`, pin the list of `Chokepoint`s from `findChokepoints`; (c) replace the 023 inline opening-build command emission with a loop over `ResolvedSlot` records, calling `BasePlan.markInFlight` / `markConsumed` as each slot's structure progresses; (d) replace the 023 attack-launch `MoveCommand` with a `Pathing.findPath` call whose `ownStructures` comes from the current `GameState.Units` filtered to own structures — emit one `MoveCommand` per waypoint per combat unit; (e) replace the 023 defend interrupt's `nearestEnemyId` intercept logic with `MoveCommand` to the nearest `Chokepoint.Position` from the pinned list; (f) retain all 023 fallbacks (critter filter, peewee auto-fire behaviour) as defensive regression guards.
- [X] T061 [US5] Add stdout observability traces to `bots/trainer/bot_macro.fsx`: `[plan] resolved <N> slots: <names>` at warmup; `[plan] slot <name> resolved @ (<x>,<z>)` or `[plan] slot <name> failed: <reason>` per `ResolvedSlot`; `[attack] path waypoints=<N> cost=<C> status=<S>` at each Attack-phase launch; `[defend] chokepoint pos=(<x>,<z>) width=<w> id=<id>` at each defend interrupt entry; `[wall-in-defect] proposed=<tag> cuts off <names>` if any `resolvePlan` slot returns `WouldWallIn`. Matches the 023 telemetry discipline.
- [X] T062 [US5] Run `bash bots/trainer/run.sh NullAI 024-rush-smoke` — rush bot `bot.fsx` MUST still win cleanly. If it regresses, **revert T060/T061 immediately** and diagnose (FR-030 is a hard invariant). Satisfies **SC-007**.
- [X] T063 [US5] Run `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 024-macro-smoke`. Verify: `phase_transitions.jsonl` shows Opening→Production→Upgrade→Attack (matches 023 iter 026 shape); `result.json.cause = "commander-death-win-after-upgrade"`; `result.json.victory_signal = "engine-shutdown-gameover"`; stdout contains at least one `[plan] resolved 5 slots` line, at least one `[attack] path waypoints=N` line. Append a `HISTORY.md` line with `iter_id=024-macro-smoke` and label `[us5-deep-integration]`. Satisfies **SC-006** first iteration.
- [X] T064 [US5] If T063 regressed (no clean win), iterate per 023 PLAYBOOK §2c: one fix per iter, one commit per iter, one HISTORY line per iter. Continue until `cause = commander-death-win-after-upgrade` is achieved. Per clarification Q5, the "fix everything in one more commit" shortcut is out of scope. Budget: up to 3 iterations per SC-006. If 3 iters don't clear, file a budget-exhaustion mailbox per PLAYBOOK §10 and halt.
- [X] T065 [P] [US5] Run a best-effort `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh BARb/dev 024-barb-probe` — the refactored bot's defend interrupt should now use chokepoints against BARb raiders. Not required to win; record the dominant behaviour (chokepoint activations, defend oscillation count, final `result.json.cause`) in HISTORY with a `[barb-probe]` label.
- [X] T066 [P] [US5] Update `bots/trainer/PLAYBOOK.md` — add §13 "Tactical primitives integration" covering: how the refactored `bot_macro.fsx` consumes each primitive, what the new stdout traces mean, how to diagnose a `[wall-in-defect]` line, how to add new `BasePlan` entries.
- [X] T067 [P] [US5] Update `bots/trainer/README.md` — one paragraph documenting that `bot_macro.fsx` now consumes `FSBar.Client.{Pathing, Chokepoints, BasePlan, WallIn, SmfParser}`, and that the 023 helpers are still in-tree but no longer drive macro behaviour.
- [X] T068 [US5] Final US5 commit `tactical: US5 bot_macro deep integration — path + plan + chokepoint defend`, push. Verify `git log origin/024-tactical-map-primitives..HEAD` is empty (everything pushed).

**Checkpoint (SC-006, SC-007)**: `bot_macro.fsx` is now running on `FSBar.Client` tactical primitives end-to-end. Rush bot unchanged and still wins. Independent test: run both bots' smoke iterations and inspect the run directory for the observability traces.

---

## Phase 8: Polish & Cross-Cutting

**Purpose**: SC-008 second-operator exercise, SC-009 infrastructure-regression rate check, fsdoc refresh (constitution §Workflow §7), and the feature-complete commit.

- [X] T069 [P] Run `fsdoc` for the Tier 1 public API change (constitution §Workflow §7): updates generated docs for `FSBar.Client` reflecting the five new modules. Output lands in `docs/` under the existing convention.
- [X] T070 [P] Walk `specs/024-tactical-map-primitives/quickstart.md` end-to-end as the operator. Every step MUST produce the expected output. Fix any drift between the quickstart and the actual module behaviour (the quickstart is a living document).
- [X] T071 SC-008 second-operator exercise: spawn a fresh Claude Code session (or a subagent) with instructions to read ONLY `PLAYBOOK.md §13` + `src/FSBar.Client/{Pathing,Chokepoints,BasePlan,WallIn,SmfParser}.fsi` + `data-model.md`, and produce a written sketch of a minimal alternative bot that reuses ≥3 of the 4 primitives without modification. Record the outcome as a `SC-008:` prefixed line in `bots/trainer/HISTORY.md` (pass or fail). Same discipline as 023 SC-009.
- [X] T072 SC-009 infrastructure-regression rate check: count iterations on the 024 branch classified as `infrastructure-regression` (rush bot regressions, failing surface-area baselines, compile breaks). Rate MUST be ≤10% of total iterations. Record the rate in HISTORY.
- [X] T073 Full test suite pass: `dotnet test src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug`. ALL tests across all modules + all five new surface-area baselines MUST pass. Zero skips attributable to feature 024 (SMF Layer-2 skips are allowed if BAR is not installed — note the skip count for the completion commit).
- [X] T074 Final feature-complete commit `tactical: 024 complete — pathing + chokepoints + plans + wall-in + smf + macro integration`. Append a `COMPLETE:` prefixed HISTORY line summarising: five new modules shipped, `bot_macro.fsx` refactored, all 10 SCs verified, second-operator exercise passed, fsdoc refreshed. Push.

**Checkpoint (SC-008, SC-009, SC-010)**: Feature 024 complete. The repo has:
- Five new Tier 1 compiled modules in `FSBar.Client` with curated `.fsi` + surface-area baselines
- Full test coverage across unit + integration + live-bot layers
- A refactored macro bot that wins on NullAI via the new primitives
- Rush bot preserved throughout
- Second-operator exercise passed
- Documentation (PLAYBOOK §13, README, fsdoc) reflects the new reality
- Ready for `/speckit.mergeBranches`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1. BLOCKS every user story because `.fsi` files must exist for the project to compile, `SmfParser` must work for Layer-2 tests, and `SyntheticMapGrid` must exist for Layer-1 tests.
- **Phase 3 US1 Pathing**: Depends on Phase 2. Independent of US2/US3/US4.
- **Phase 4 US2 Chokepoints**: Depends on Phase 2. Independent of US1/US3/US4 (uses `MapGrid` directly, not `Pathing`).
- **Phase 5 US4 WallIn**: Depends on Phase 2 AND Phase 3 US1 (shares passability with `Pathing.rasteriseFootprints`).
- **Phase 6 US3 BasePlan**: Depends on Phase 2 + US1 (reach via `Pathing`) + US4 (`wouldWallIn` per FR-023). This is why implementation order swaps US3 and US4.
- **Phase 7 US5 Integration**: Depends on US1 + US2 + US3 + US4 all being landed (uses all four primitives from `bot_macro.fsx`).
- **Phase 8 Polish**: Depends on all user stories.

### User Story Dependencies (this feature deviates from the template)

Unlike a typical multi-story feature, **US3 cannot start until US4 is done** because `resolvePlan` integrates `wouldWallIn` per FR-023. The spec's priority ordering (US3 P1, US4 P2) reflects **user value** priority, not **technical** dependency. Tasks below respect the technical dependency (US4 before US3) with explicit notes in Phase 5's header.

US1 (Pathing) and US2 (Chokepoints) are **independent of each other** — they can be parallelised across multiple operators after Phase 2 is complete.

### Within Each User Story

- Layer-1 unit tests (synthetic `MapGrid`) MUST fail before the `.fs` implementation lands.
- `.fsi` stubs from Phase 2 mean the project always compiles, so failing tests are test-level (missing functionality), not build-level.
- Each story's surface-area baseline is regenerated once the `.fs` implementation lands and Layer-1 tests pass.
- Layer-2 integration tests (SMF-parsed real maps) come last in each story.
- Example FSI scripts (parallel task per story) land after Layer-1 passes.

### Parallel Opportunities

- **Phase 1 Setup**: T003 and T004 are independent checks; can run in parallel with T001/T002.
- **Phase 2 Foundational**: T019 (SMF Layer-2) and T020 (SMF baseline) can run in parallel with each other; T022 (example script) can run in parallel with both.
- **US1 (Pathing)**: T031 (example script) is [P] with the rest of the story's impl tasks.
- **US2 (Chokepoints)**: T041 (example script) is [P].
- **US3 (BasePlan)**: T058 (example script) is [P].
- **US5 (Integration)**: T065 (BARb/dev probe), T066 (PLAYBOOK), T067 (README) are [P] after T063/T064 produce a clean NullAI win.
- **US1 vs US2**: once Phase 2 is complete, US1 and US2 are fully independent and can be implemented by two operators in parallel (or by one operator sequentially with no coupling).
- **Within an iteration of US5 (T064)**: no parallelism — each iter is a single `run.sh` invocation followed by a single diagnosis and a single commit, per 023 PLAYBOOK §2c.

---

## Parallel Example: Phase 2 Foundational

```bash
# After T005-T015 (sequential — they edit the same fsproj and depend on each other),
# these three run in parallel:

Task T019: "Add Layer-2 SMF integration tests to src/FSBar.Client.Tests/SmfParserTests.fs"
Task T020: "Regenerate src/FSBar.Client.Tests/Baselines/SmfParser.baseline"
Task T022: "Create scripts/examples/10-smf.fsx"
```

## Parallel Example: US1 + US2 with two operators

```bash
# After Phase 2 completes:

Operator A: runs T023-T032 (full Phase 3 US1 Pathing)
Operator B: runs T033-T042 (full Phase 4 US2 Chokepoints)

# Both complete independently; neither blocks the other.
# Then a single operator continues with Phase 5 US4 WallIn (depends on US1).
```

---

## Implementation Strategy

### MVP scope (suggested for a first demo)

1. **Phase 1 Setup + Phase 2 Foundational** — ~22 tasks, ends with `SmfParser` fully working.
2. **Phase 3 US1 Pathing** — ends with `dotnet fsi scripts/examples/11-pathing.fsx` printing a cross-map detour path on Avalanche 3.4.
3. **STOP and VALIDATE**: the four modules that `bot_macro.fsx` doesn't yet consume (Chokepoints, WallIn, BasePlan) are still stubs. SC-001 and SC-002 are verified by `dotnet test`. `findPath` is demoable to a second operator via the example script.

This is a legitimate pause point if iteration budget is tight. The remaining user stories extend the primitive set without breaking what's landed.

### Incremental delivery

1. MVP above → demo (Pathing + SmfParser)
2. Add US2 Chokepoints → demo (`[chokepoint]` on a real map)
3. Add US4 WallIn → demo (`[wall-in-defect]` on a synthetic scenario)
4. Add US3 BasePlan → demo (`resolvePlan defaultArmadaOpening` against Avalanche 3.4)
5. Add US5 Integration → demo (refactored `bot_macro.fsx` winning on NullAI via primitives)
6. Polish + SC verification

Each increment is independently usable by a subsequent operator reading `PLAYBOOK.md §13`.

### Single-operator strategy (default)

One operator, sequential phases in the order listed. Technical dependency forces US4 before US3; everything else follows the phase numbering. Commits per task, push after every commit (inherits 020/023 discipline).

---

## Notes

- **Tests are mandatory** — this is a Tier 1 change and the constitution §III requires behavior-changing code to include automated tests that fail before the fix and pass after.
- **`[P]` tasks** — different files, no dependencies on incomplete work in the same phase. Parallel opportunity is limited because module work mostly targets the same `FSBar.Client.fsproj`; once a file is created it's owned by one task at a time.
- **Integration task ordering**: per clarification Q5, US5 is a **single atomic refactor commit**. T060 and T061 are logically one change split across two tasks only so the task list reads cleanly; they commit together. T062/T063/T064 are separate commits because each live iteration is a separate `run.sh` invocation.
- **File paths** in task descriptions are absolute from the repo root.
- **Example scripts** (10-smf.fsx, 11-pathing.fsx, 12-chokepoints.fsx, 13-plan.fsx) each land inside the story that produces the module they demonstrate, plus SmfParser's in Phase 2 Foundational because SmfParser lands there.
- **Never modify `bot.fsx`** in this feature. If a test regression suggests the rush bot would benefit from the new primitives, file a follow-up feature — do not touch it inside 024.
- **Never commit on `master`**. Every commit lands on `024-tactical-map-primitives` and is pushed before the next task begins. Inherited from 020/023 discipline.
