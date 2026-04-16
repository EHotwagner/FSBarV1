# Tasks: Trainer Viewer and Runtime Options

**Input**: Design documents from `/specs/029-trainer-viewer-options/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: No xUnit tests — manual integration testing per plan.md. No test tasks generated.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Build dependencies so .fsx scripts can reference fresh DLLs

- [X] T001 Build FSBar.Viz.Tests to ensure all transitive DLLs (FSBar.Viz, SkiaViewer, SkiaSharp, FSBar.Client) are available in `tests/FSBar.Viz.Tests/bin/Debug/net10.0/` for viewer.fsx `#r` directives

---

## Phase 2: Foundational — CLI Option Parsing

**Purpose**: Extend `run.sh` with the option-parsing loop and env-var exports that ALL user stories depend on. Must complete before any story-specific work.

- [X] T002 Add CLI option parsing loop to `bots/trainer/run.sh` — after shifting the two positional args (`rung_name`, `iter_id`), parse `--viewer`, `--speed`, `--map`, `--bot`, `--opponent`, `--profile` via a `while`/`case` loop over remaining `$@`
- [X] T003 Add validation in `bots/trainer/run.sh` — `--speed` must be 1-5 or "max"; `--bot` file must exist in `bots/trainer/`; other string options must be non-empty. Exit with usage error before engine launch on invalid input (FR-008)
- [X] T004 Export new/overridden environment variables in `bots/trainer/run.sh` — `BOT_VIEWER`, `BOT_GAME_SPEED` (mapped per FR-009: 1→1, 2→5, 3→10, 4→20, 5→50, max→100), `BOT_SPEED_LEVEL` (default `"max"` when no `--speed` and no `--viewer`), `BOT_MAP` override, `BOT_SCRIPT` override, `BOT_OPPONENT` override, `BOT_OPPONENT_OPTIONS` rebuild for `--profile`
- [X] T005 Apply viewer-default speed in `bots/trainer/run.sh` — when `--viewer` is set and `--speed` is not explicitly given, default `BOT_GAME_SPEED=10` / `BOT_SPEED_LEVEL=3` (FR-010)

**Checkpoint**: `run.sh` accepts all new options, validates them, and exports correct env vars. Existing invocations (`bash run.sh NullAI 001`) still work identically (FR-007).

---

## Phase 3: User Story 1 — Run Trainer with FSBar.Viz Viewer (Priority: P1)

**Goal**: Launch a training run with an optional FSBar.Viz viewer window that displays live game state while the engine runs headless.

**Independent Test**: `bash bots/trainer/run.sh NullAI smoke --viewer` opens a SkiaViewer window showing the live game. Without `--viewer`, no window appears.

### Implementation

- [X] T006 [US1] Create `bots/trainer/helpers/viewer.fsx` — check `BOT_VIEWER` env var; when "1", `dlopen` native libraries (libglfw.so.3, libSkiaSharp.so) from `tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native`, then `#r` FSBar.Viz and SkiaViewer DLLs from the test output directory (R5)
- [X] T007 [US1] Implement `Viewer.startViewer` in `bots/trainer/helpers/viewer.fsx` — calls `GameViz.start(None)`, `GameViz.attachToClient(client)`, subscribes to `client.Frames |> Observable.subscribe GameViz.onFrame`, returns a disposable that calls `GameViz.stop()` and disposes the subscription (R1, R2)
- [X] T008 [US1] Implement no-op path and error handling in `bots/trainer/helpers/viewer.fsx` — when `BOT_VIEWER` is unset or not "1", `startViewer` returns a no-op disposable and `stopViewer` is a no-op. When `BOT_VIEWER=1` but `DISPLAY` env var is unset, emit a clear error message and fall back to no-op (spec edge case: no display available). Wrap all viewer operations in try-catch so viewer failures never crash the trainer (R6, FR-013)
- [X] T009 [US1] Integrate viewer into `bots/trainer/bot.fsx` — add `#load "helpers/viewer.fsx"` after existing helper loads; after `client.Start()` call `Viewer.startViewer client`; in `finally` block call `Viewer.stopViewer()` before `client.Stop()`
- [X] T010 [US1] Integrate viewer into `bots/trainer/bot_macro.fsx` — same pattern as T009: `#load`, `startViewer` after Start, `stopViewer` in finally

**Checkpoint**: Viewer window opens with `--viewer`, renders live game state at ~60fps, closes gracefully. Without `--viewer`, behavior is unchanged.

---

## Phase 4: User Story 2 — Set Game Speed (Priority: P1)

**Goal**: Control game simulation speed via `--speed <1-5|max>` option.

**Independent Test**: `bash bots/trainer/run.sh NullAI smoke --speed 1` runs at real-time; `--speed max` runs at 100x; no `--speed` defaults to max (or 3 with `--viewer`).

### Implementation

- [X] T011 [US2] Verify speed env var consumption in bot scripts — confirm `BOT_GAME_SPEED` is already read and applied to the engine config in `bots/trainer/bot.fsx` and `bots/trainer/bot_macro.fsx`. If not, add `EngineConfig` speed override using `BOT_GAME_SPEED` env var
- [X] T012 [US2] Verify `EngineConfig.GameSpeed` flows to `MinSpeed`/`MaxSpeed` in the startscript via `src/FSBar.Client/ScriptGenerator.fs` — confirm the speed multiplier from `BOT_GAME_SPEED` propagates through `EngineConfig` into the generated startscript (already wired at ScriptGenerator.fs:L50-51)

**Checkpoint**: Speed levels 1-5 and max produce visibly different game paces. Default (no option) remains max speed.

---

## Phase 5: User Story 3 — Select Map (Priority: P2)

**Goal**: Override the ladder map via `--map <name>`.

**Independent Test**: `bash bots/trainer/run.sh NullAI smoke --map "Red Comet Remake 1.8"` loads on that map instead of the ladder default.

### Implementation

- [X] T013 [US3] Verify map override flow — confirm that the `BOT_MAP` env var override (set in T004) propagates through `bots/trainer/run.sh` to the engine config in `bots/trainer/bot.fsx` and `bots/trainer/bot_macro.fsx`. The env var is already exported; verify the bot scripts use it for `EngineConfig.map`

**Checkpoint**: Specifying `--map` loads the requested map; omitting it uses the ladder default.

---

## Phase 6: User Story 4 — Select Self AI Bot Script (Priority: P2)

**Goal**: Choose which bot script runs via `--bot <script>`.

**Independent Test**: `bash bots/trainer/run.sh NullAI smoke --bot bot_macro.fsx` runs the macro bot; `--bot bot.fsx` runs the rush bot.

### Implementation

- [X] T014 [US4] Verify bot script override flow — confirm that `BOT_SCRIPT` override from T004 causes `run.sh` to set `BOT_FSX` to the correct script path and that `dotnet fsi "$BOT_FSX"` launches the right script. The existing `BOT_SCRIPT` env var mechanism (line 36-37 of run.sh) should already handle this; verify the CLI `--bot` option feeds into it correctly

**Checkpoint**: `--bot bot_macro.fsx` launches the macro bot; `--bot nonexistent.fsx` errors before engine launch.

---

## Phase 7: User Story 5 — Select Opponent AI (Priority: P2)

**Goal**: Override opponent AI and profile via `--opponent <name>` and `--profile <name>`.

**Independent Test**: `bash bots/trainer/run.sh NullAI smoke --opponent BARb --profile hard` launches against BARb at hard difficulty.

### Implementation

- [X] T015 [US5] Verify opponent override flow — confirm `BOT_OPPONENT` override from T004 propagates to the engine config in bot scripts. Check that `bots/trainer/bot.fsx` and `bots/trainer/bot_macro.fsx` use the env var for opponent AI name in the startscript
- [X] T016 [US5] Verify profile override flow — confirm `BOT_OPPONENT_OPTIONS` rebuild from T004 (wrapping `--profile` value as `{"profile":"<val>"}`) is consumed by bot scripts for opponent AI options in the startscript

**Checkpoint**: `--opponent BARb --profile hard` produces a game against BARb with hard difficulty. Default uses ladder config.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Metadata recording and final validation

- [X] T017 [P] Extend meta.json in `bots/trainer/run.sh` — add fields: `viewer` (bool from `BOT_VIEWER`), `speed_level` (string from `BOT_SPEED_LEVEL`), `map_override` (string or null), `bot_script` (string from `BOT_SCRIPT`), `opponent_override` (string or null), `opponent_profile` (string or null) per FR-011
- [X] T018 [P] Update branch check warning in `bots/trainer/run.sh` — change the hardcoded branch name from `023-trainer-builder-economy` (line 55) to `029-trainer-viewer-options` or remove the check entirely
- [X] T019 [P] Update usage message in `bots/trainer/run.sh` — update the header comment and the usage line (line 14) to include `[OPTIONS]` and list available options
- [X] T020 Run quickstart.md validation — execute each example from `specs/029-trainer-viewer-options/quickstart.md` and verify expected behavior

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1 Viewer (Phase 3)**: Depends on Phase 2 — core feature, should be implemented first
- **US2 Speed (Phase 4)**: Depends on Phase 2 — can run in parallel with US1
- **US3 Map (Phase 5)**: Depends on Phase 2 — can run in parallel with US1/US2
- **US4 Bot Script (Phase 6)**: Depends on Phase 2 — can run in parallel with US1/US2/US3
- **US5 Opponent (Phase 7)**: Depends on Phase 2 — can run in parallel with US1/US2/US3/US4
- **Polish (Phase 8)**: Depends on Phase 2 (for meta.json), can start as soon as run.sh option parsing is complete

### User Story Dependencies

- **US1 (Viewer)**: Depends only on Phase 2. Independent of all other stories.
- **US2 (Speed)**: Depends only on Phase 2. Independent of all other stories.
- **US3 (Map)**: Depends only on Phase 2. Independent — mostly verification that env var flows correctly.
- **US4 (Bot Script)**: Depends only on Phase 2. Independent — existing `BOT_SCRIPT` mechanism does most of the work.
- **US5 (Opponent)**: Depends only on Phase 2. Independent — verification of env var propagation.

### Parallel Opportunities

- T001 can run in parallel with reading/planning
- T002, T003, T004, T005 are sequential (all modify `run.sh`)
- After Phase 2: US1 (T006-T010), US2 (T011-T012), US3 (T013), US4 (T014), US5 (T015-T016) can all start in parallel
- T009 and T010 (bot.fsx and bot_macro.fsx integration) can run in parallel with each other
- T017, T018, T019 can all run in parallel (different sections of run.sh or independent concerns)

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Build dependencies
2. Complete Phase 2: CLI option parsing in run.sh (CRITICAL — blocks all stories)
3. Complete Phase 3: Viewer (US1) — the primary feature request
4. Complete Phase 4: Speed (US2) — essential companion to viewer
5. **STOP and VALIDATE**: Test `--viewer --speed 3` end-to-end
6. Demo: developer can watch a training game at comfortable speed

### Incremental Delivery

1. Phase 1 + Phase 2 → CLI options work, backward compatibility preserved
2. Add US1 (Viewer) → Visual observation available
3. Add US2 (Speed) → Comfortable viewing speed
4. Add US3-US5 (Map, Bot, Opponent) → Full flexibility
5. Phase 8 (Polish) → Metadata and documentation complete

---

## Notes

- All changes are in .fsx scripts and bash — no compiled F# modules, no .fsi files, no xUnit tests
- US3, US4, US5 are largely verification tasks — the env var mechanism from Phase 2 does the heavy lifting
- The viewer helper (viewer.fsx) is the only new file; everything else is modification of existing files
- Native library loading (dlopen) must happen before managed DLL references per CLAUDE.md
- Viewer must never crash the trainer — all viewer code wrapped in try-catch
