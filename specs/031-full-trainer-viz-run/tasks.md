# Tasks: Full Trainer Game with Complete Visualization

**Input**: Design documents from `/specs/031-full-trainer-viz-run/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: No automated test tasks — this feature modifies scripts only (bash + .fsx). Verification is manual (launch game, observe viewer).

**Organization**: Tasks grouped by user story for independent implementation.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: No setup needed — all target files already exist. This feature modifies 3 existing files in-place.

*(No tasks in this phase)*

---

## Phase 2: Foundational (CLI Flag Parsing)

**Purpose**: Add `--full-viz` flag to `run.sh` and export the env var. This MUST complete before user story work in .fsx files.

- [x] T001 Add `--full-viz` flag parsing to the getopts loop in `bots/trainer/run.sh` — set `opt_full_viz=1` and `opt_viewer=1` (full-viz implies viewer)
- [x] T002 Add speed-default logic for full-viz in `bots/trainer/run.sh` — when `opt_full_viz` is set and no explicit `--speed`, default to speed level 2 (game speed 5) instead of viewer's default level 3
- [x] T003 Export `BOT_FULL_VIZ=1` env var in `bots/trainer/run.sh` when `opt_full_viz` is set
- [x] T004 Add `full_viz` and `initial_overlays` fields to the `meta.json` writer in `bots/trainer/run.sh` when full-viz mode is active

**Checkpoint**: `run.sh --full-viz NullAI 001` should parse without error, export `BOT_FULL_VIZ=1`, default to speed 5, and record full_viz in meta.json.

---

## Phase 3: User Story 1 — All Overlays Active from First Frame (Priority: P1) 🎯 MVP

**Goal**: When `BOT_FULL_VIZ=1`, the viewer opens with all 8 gameplay overlays enabled from the first rendered frame.

**Independent Test**: Run `bots/trainer/run.sh NullAI 001 --full-viz` and verify the viewer window shows Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, and FullNames overlays immediately.

### Implementation for User Story 1

- [x] T005 [US1] Read `BOT_FULL_VIZ` env var in `bots/trainer/helpers/viewer.fsx` (alongside existing `BOT_VIEWER` check)
- [x] T006 [US1] When `BOT_FULL_VIZ = "1"`, set `ActiveOverlays` to all 8 gameplay overlays (Units, Events, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, FullNames) in `bots/trainer/helpers/viewer.fsx`
- [ ] T007 [US1] Verify existing keyboard toggles (W/L/C/N/U/E/M/H) still work to turn individual overlays on/off during a full-viz game

**Checkpoint**: Full-viz game launches with all 8 overlays visible from frame 1. Keyboard toggles still work.

---

## Phase 4: User Story 2 — Run to Natural Completion (Priority: P2)

**Goal**: When `BOT_FULL_VIZ=1`, the game runs until a natural win/loss instead of stopping at max_frames.

**Independent Test**: Run a full-viz game against NullAI and verify it continues past the rung's default max_frames until the enemy commander is destroyed.

### Implementation for User Story 2

- [x] T008 [US2] Read `BOT_FULL_VIZ` env var in `bots/trainer/bot.fsx` and set `maxFrames = Int32.MaxValue` when active, overriding the ladder rung's `BOT_MAX_FRAMES` value
- [ ] T009 [US2] Verify graceful exit when viewer window is closed mid-game (existing `shutdownSeen` mechanism in `trainerLoopRun` at `bots/trainer/helpers/tactics.fsx:240` — no code change expected, just verify)

**Checkpoint**: Full-viz game runs past the normal frame limit. Closing the viewer window stops the game gracefully with partial results written.

---

## Phase 5: User Story 3 — Base Terrain Layer Active (Priority: P3)

**Goal**: When `BOT_FULL_VIZ=1`, the base terrain layer renders beneath all overlays from game start.

**Independent Test**: Run a full-viz game and verify the textured terrain map is visible as the backdrop (not the grayscale height map).

### Implementation for User Story 3

- [x] T010 [US3] When `BOT_FULL_VIZ = "1"`, set `BaseLayer = LayerKind.BaseTerrain` in the VizConfig built by `bots/trainer/helpers/viewer.fsx` (overriding `VizDefaults.defaultConfig`'s `HeightMap`)
- [ ] T011 [US3] Verify `B` key toggle still works to turn base terrain on/off during a full-viz game

**Checkpoint**: Full-viz game shows textured terrain as background. `B` key toggles it normally.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T012 Run a full end-to-end test: `bots/trainer/run.sh NullAI 001 --full-viz` — verify all overlays, terrain, speed 2, and game runs to completion
- [ ] T013 Run with speed override: `bots/trainer/run.sh NullAI 001 --full-viz --speed 3` — verify speed 3 takes precedence
- [ ] T014 Run with map override: `bots/trainer/run.sh NullAI 001 --full-viz --map "Avalanche 3.4"` — verify compatibility
- [ ] T015 Verify `meta.json` in run output directory contains `full_viz: true` and `initial_overlays` array
- [ ] T016 Verify existing `--viewer` (without `--full-viz`) still works with original 4-overlay default and speed 3

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: No dependencies — start immediately
- **User Story 1 (Phase 3)**: Depends on Phase 2 (needs `BOT_FULL_VIZ` env var exported)
- **User Story 2 (Phase 4)**: Depends on Phase 2 only (reads `BOT_FULL_VIZ` in bot.fsx, independent of viewer.fsx)
- **User Story 3 (Phase 5)**: Depends on Phase 2 only (reads `BOT_FULL_VIZ` in viewer.fsx, same file as US1 but independent section)
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **User Story 1 (P1)**: Depends on Phase 2 only — no cross-story dependencies
- **User Story 2 (P2)**: Depends on Phase 2 only — modifies `bot.fsx`, independent of `viewer.fsx` changes
- **User Story 3 (P3)**: Depends on Phase 2 only — modifies `viewer.fsx` (same file as US1 but different config field)

### Parallel Opportunities

- T005+T006 (US1, viewer.fsx) and T008 (US2, bot.fsx) touch different files and can run in parallel after Phase 2
- T010 (US3) modifies the same function in viewer.fsx as T006 (US1), so should run sequentially after US1
- All Polish tasks (T012-T016) are independent manual verifications and can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 2: Add `--full-viz` to run.sh (T001-T004)
2. Complete Phase 3: Expand overlays in viewer.fsx (T005-T007)
3. **STOP and VALIDATE**: Run `--full-viz` and confirm all 8 overlays appear
4. This alone delivers the core value — single-command full observability

### Incremental Delivery

1. Phase 2 → CLI flag ready
2. Phase 3 (US1) → All overlays from first frame (MVP!)
3. Phase 4 (US2) → Unlimited game duration
4. Phase 5 (US3) → Terrain backdrop
5. Phase 6 → End-to-end validation + regression check

---

## Notes

- All changes are in scripts (bash + .fsx) — no compiled F# library changes, no `.fsi` updates, no baseline updates needed
- The `viewer.fsx` changes (US1 + US3) should be combined into a single edit of the `startViewer` function for cleanliness
- `bot.fsx` change (US2) is a single 2-line addition near the existing `maxFrames` parse
- Total scope: ~30 lines of code across 3 files
