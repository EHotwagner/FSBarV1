# Tasks: GameViz State-Based Rendering API

**Input**: Design documents from `/specs/030-gameviz-state-api/`  
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — constitution requires test evidence for behavior-changing code (§III).

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: No new project setup needed — all changes are within existing `FSBar.Viz` and `bots/trainer/`. This phase ensures the build is clean.

- [X] T001 Verify clean build of `src/FSBar.Viz/` and `tests/FSBar.Viz.Tests/` with `dotnet build`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Private helper functions in GameViz that both the state-based path (US1) and the viewer script update (US2) depend on.

**⚠️ CRITICAL**: US1 and US2 cannot begin until these helpers exist.

- [X] T002 Add private `ensureDefPropsFromCache` helper to `src/FSBar.Viz/GameViz.fs` — resolves DefId → name via `UnitDefCache.tryFindById` then calls existing `resolveDefPropsFromBarData`, populating `defPropsCache` without socket access. Falls back to `sprintf "def%d" defId` for unknown DefIds.
- [X] T003 [P] Add private `trackedUnitToUnitState` helper to `src/FSBar.Viz/GameViz.fs` — converts `TrackedUnit` → `UnitState` (position tuple → flat fields, `IsEnemy = false`, `TeamId = myTeamId`).
- [X] T004 [P] Add private `trackedEnemyToUnitState` helper to `src/FSBar.Viz/GameViz.fs` — converts `TrackedEnemy` → `UnitState` (position tuple → flat fields, `IsEnemy = true`, `TeamId` derived from non-myTeamId, `DefId` from option with 0 default, `Health` from option with 100.0f default).
- [X] T005 [P] Add private `economyFromSnapshot` helper to `src/FSBar.Viz/GameViz.fs` — converts `FSBar.Client.EconomySnapshot` → `FSBar.Viz.EconomyData` (field-by-field copy: `Current`, `Income`, `Usage`, `Storage`).

**Checkpoint**: All private helpers compile. Existing socket-based path is unaffected.

---

## Phase 3: User Story 1 — Trainer Bot Drives Visualizer Without Socket Contention (Priority: P1) 🎯 MVP

**Goal**: Add `attachWithState` and `onFrameWithState` to `GameViz` so the trainer bot can render game state without any engine socket reads.

**Independent Test**: Construct a `GameState` with known units and a `MapGrid`, call `onFrameWithState`, verify the module's internal snapshot contains the expected `DisplayUnits`, economy data, and event indicators — all without a `BarClient` or socket.

### Implementation for User Story 1

- [X] T006 [US1] Add `val attachWithState: mapGrid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> teamId: int -> unit` to `src/FSBar.Viz/GameViz.fsi`
- [X] T007 [US1] Add `val onFrameWithState: gameState: GameState -> mapGrid: MapGrid -> unit` to `src/FSBar.Viz/GameViz.fsi`
- [X] T008 [US1] Implement `attachWithState` in `src/FSBar.Viz/GameViz.fs` — acquires `stateLock`, sets `mapGridRef`, `metalSpots`, `myTeamId` from parameters, calls `computeAutoFit`, emits `[GameViz] Attached via state` diagnostic. Does NOT set `clientRef`.
- [X] T009 [US1] Implement `onFrameWithState` in `src/FSBar.Viz/GameViz.fs`:
  - Acquire `stateLock`
  - Process `gameState.Events` for indicators: `UnitCreated` → creation indicator at unit position from `gameState.Units`; `UnitDestroyed` → destruction indicator at last known position from current `units` map; `UnitDamaged` → combat indicator; `EnemyEnterLOS` → enemy spotted indicator at position from `gameState.Enemies`; `EnemyLeaveLOS`/`EnemyDestroyed` → destruction indicator from `units` map
  - Rebuild `units` from `gameState.Units` (via `trackedUnitToUnitState`) + `gameState.Enemies` where `InLOS = true` (via `trackedEnemyToUnitState`)
  - For each unique DefId in rebuilt `units`, call `ensureDefPropsFromCache` with `gameState.UnitDefs`
  - Set `unfinishedUnits` from `TrackedUnit` entries where `IsFinished = false`
  - Prune expired indicators
  - Derive economy via `economyFromSnapshot gameState.Metal` and `economyFromSnapshot gameState.Energy`
  - Update `mapGridRef` with provided `mapGrid`
  - Call `buildSnapshot` and store in `snapshot`
- [X] T010 [US1] Update surface-area baseline in `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` to include `attachWithState` and `onFrameWithState` signatures
- [X] T011 [US1] Add behavioral xUnit test in `tests/FSBar.Viz.Tests/GameVizStateTests.fs`: construct a `GameState` with 3+ known `TrackedUnit` entries (varied DefIds), a `UnitDestroyed` event, and non-zero `Metal`/`Energy`; construct a minimal `MapGrid`; call `GameViz.start`, `attachWithState`, then `onFrameWithState`; assert the resulting `GameSnapshot` contains expected `DisplayUnits` count, economy values matching input, and a destruction `EventIndicator`. Include a sub-case where `GameState.Units` is empty (first frame / no units) to verify the snapshot still builds without error. Call `GameViz.stop` in teardown.
- [X] T012 [US1] Verify `dotnet build src/FSBar.Viz/` compiles cleanly with new `.fsi`/`.fs` additions
- [X] T013 [US1] Verify existing socket-based path (`attachToClient` + `onFrame`) still compiles and existing tests pass with `dotnet test tests/FSBar.Viz.Tests/`

**Checkpoint**: `attachWithState` + `onFrameWithState` compile, pass surface-area baseline, and pass behavioral test. Existing tests pass (FR-007, SC-005).

---

## Phase 4: User Story 2 — Visualizer Initialization Without Socket Handshake (Priority: P2)

**Goal**: Update the trainer bot viewer helper script to use `attachWithState` + `onFrameWithState`, eliminating all socket reads from the visualization path.

**Independent Test**: Run the macro trainer bot with `--viewer` flag, confirm the visualization window renders units, health, economy, and terrain throughout a full game with zero socket errors.

### Implementation for User Story 2

- [X] T014 [US2] Update `startViewer` in `bots/trainer/helpers/viewer.fsx` — accept `mapGrid: MapGrid option`, `metalSpots: (float32 * float32 * float32 * float32) array`, and `teamId: int` parameters. When `mapGrid` is `Some grid`, call `GameViz.attachWithState grid metalSpots teamId` immediately after `GameViz.start` (no deferred attach needed). When `None`, defer to US3 fallback logic.
- [X] T015 [US2] Update `viewerOnFrame` in `bots/trainer/helpers/viewer.fsx` — change signature to accept `gameState: GameState` and `mapGrid: MapGrid`. Call `GameViz.onFrameWithState gameState mapGrid` instead of `GameViz.onFrame frame`.
- [X] T016 [US2] Remove deferred-attach machinery from `bots/trainer/helpers/viewer.fsx` — delete `pendingClient`, `clientAttached` mutable state and the deferred `attachToClient`/`seedUnits` block in `viewerOnFrame`.
- [X] T017 [US2] Update macro bot callsites in both `bots/trainer/bot.fsx` (lines 291, 296) and `bots/trainer/bot_macro.fsx` (lines 1222, 1227) to pass `Some mapGrid`, `metalSpots`, `teamId`, `client.GameState` instead of `client`/`frame`. The macro bot already has `MapGrid` from its warmup phase. For `metalSpots`, store the raw `allSpots` array from the existing `Callbacks.getMetalSpots client.Stream` call (`bot_macro.fsx:1060`) and pass it to `startViewer` (the viewer needs the unsorted array, not `sortedMetalSpots`). For `bot.fsx` (simpler bot), pass `None` for mapGrid and `[||]` for metalSpots.
- [X] T018 [US2] Verify `dotnet fsi bots/trainer/helpers/viewer.fsx` loads without errors (syntax check)

**Checkpoint**: Viewer helper uses state-based path. Socket-free visualization ready for macro bot.

---

## Phase 5: User Story 3 — Non-Macro Bot Visualization Support (Priority: P3)

**Goal**: Ensure the simpler bot script can also use the visualizer, falling back to cached map data or a flat MapGrid when full map analysis is unavailable.

**Independent Test**: Run the simpler bot with `--viewer` flag, confirm visualization window opens and renders unit positions and economy data on a simplified map.

### Implementation for User Story 3

- [X] T019 [US3] Add fallback MapGrid logic to `startViewer` in `bots/trainer/helpers/viewer.fsx` — when `mapGrid` is `None` (already accepted as `MapGrid option` from T014), attempt `MapCacheFile.read` for the current map; if that fails, construct a flat `MapGrid` via a private `flatMapGrid` function.
- [X] T020 [US3] Implement private `flatMapGrid` function in `bots/trainer/helpers/viewer.fsx` — builds a `MapGrid` with `HeightMap = Array2D.zeroCreate`, empty `SlopeMap`/`ResourceMap`/`LosMap`/`RadarMap`, and correct dimensions. Map dimensions (`WidthElmos`, `HeightElmos`) are passed as parameters by the bot (obtained from `Callbacks.getMapWidth`/`getMapHeight` during the bot's own warmup inside WaitFrames, not by the viewer).
- [X] T021 [US3] Verify simpler bot (`bots/trainer/bot.fsx`) already passes `None` for mapGrid (done in T017). Confirm visualization renders unit positions and economy on the flat map (manual verification with `--viewer` flag).

**Checkpoint**: Both macro and simpler bots support visualization via state-based path.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and documentation.

- [X] T022 Run full test suite with `dotnet test` across all test projects to confirm no regressions
- [ ] T023 Verify SC-001: run trainer bot with `--viewer` for 1000+ frames with zero socket contention errors. Also observe that the bot loop does not stall at 5x+ game speed (US1-AS3).
- [X] T024 Verify SC-002: confirm `onFrameWithState` performs zero `NetworkStream` reads (code review of final implementation)
- [ ] T025 Verify SC-003: confirm displayed unit positions and economy match bot's `GameState` (visual inspection during live run)
- [X] T026 Update CLAUDE.md Recent Changes section with feature 030 entry

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — core API implementation
- **US2 (Phase 4)**: Depends on US1 (needs `attachWithState`/`onFrameWithState` to exist)
- **US3 (Phase 5)**: Depends on US2 (extends viewer helper with optional MapGrid)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Blocked only by Foundational phase. Self-contained in `FSBar.Viz`.
- **US2 (P2)**: Depends on US1 being complete (calls the new API from viewer.fsx).
- **US3 (P3)**: Depends on US2 being complete (extends the viewer.fsx changes from US2).

### Within Each User Story

- `.fsi` signature before `.fs` implementation (T006→T007→T008→T009)
- Implementation before baseline + behavioral test (T009→T010→T011)
- Tests before build verification (T011→T012→T013)

### Parallel Opportunities

- T003 + T004 + T005 can run in parallel (independent private helpers)
- T006 + T007 can run in parallel (independent `.fsi` additions)
- T014 + T015 + T016 can be done as a single editing pass on `viewer.fsx`

---

## Parallel Example: Foundational Phase

```bash
# Launch independent helper implementations together:
Task: "Add trackedUnitToUnitState helper in src/FSBar.Viz/GameViz.fs"
Task: "Add trackedEnemyToUnitState helper in src/FSBar.Viz/GameViz.fs"
Task: "Add economyFromSnapshot helper in src/FSBar.Viz/GameViz.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (verify build)
2. Complete Phase 2: Foundational (private helpers)
3. Complete Phase 3: User Story 1 (core API)
4. **STOP and VALIDATE**: Build compiles, baseline passes, existing tests pass
5. State-based rendering API is usable from FSI scripts

### Incremental Delivery

1. Setup + Foundational → Helpers ready
2. US1 → API exists, testable via FSI → Core deliverable
3. US2 → Trainer bot wired up → Full trainer visualization works
4. US3 → Simpler bot supported → Feature complete
5. Polish → All SC criteria verified → Ship-ready

---

## Notes

- No new NuGet dependencies
- No new F# projects or directories
- All changes confined to: `GameViz.fsi`, `GameViz.fs`, surface-area baseline test, `viewer.fsx`, bot callsites
- The `.fsi` must be updated before the `.fs` to satisfy the compiler
- Event indicator processing order matters: process events before rebuilding `units` map so destruction indicators can read the previous frame's positions
