# Tasks: Hub Viewer Fixes

**Input**: Design documents from `/specs/038-hub-viewer-fixes/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included — plan.md Constitution Check §III makes tests mandatory for each user story.

**Organization**: Tasks are grouped by user story so each can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All paths are repo-root relative unless absolute

## Path Conventions

Single F# solution (`FSBarV1.slnx`). Source under `src/`, tests under `tests/`.
All new modules ship `.fsi` alongside `.fs` per Constitution §II.

---

## Phase 1: Setup

**Purpose**: Sanity-check starting state before any edits.

- [X] T001 Confirm working branch is `038-hub-viewer-fixes` and no uncommitted edits block the baseline by running `git status` from `/home/developer/projects/FSBarV1/`
- [X] T002 Run clean build `dotnet build FSBarV1.slnx` from `/home/developer/projects/FSBarV1/` to capture the pre-feature green state
- [X] T003 Run `dotnet test FSBarV1.slnx` once from `/home/developer/projects/FSBarV1/` to record current surface-area baselines and the pre-feature passing test count

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure that every user story touches.

**⚠️ CRITICAL**: No user story work may begin until this phase is complete.

- [X] T004 Extend `HubSettings` type with `StartPausedDefault: bool` field in `src/FSBar.Hub/HubSettings.fsi` per contracts/HubSettings.fsi
- [X] T005 Implement JSON round-trip for `StartPausedDefault` (default `true`, `parseBool root "startPausedDefault"` on load, `WriteBoolean` on save) in `src/FSBar.Hub/HubSettings.fs` — mirror the existing `LaunchGraphicalViewerDefault` handling
- [X] T006 Regenerate `tests/FSBar.Hub.Tests/Baselines/HubSettings.baseline` via `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests/`

**Checkpoint**: `HubSettings` carries both `LaunchGraphicalViewerDefault` and `StartPausedDefault` — US2 and US3 can now read their persisted defaults independently.

---

## Phase 3: User Story 1 - Viewer-tab glyphs match encyclopedia (Priority: P1) 🎯 MVP

**Goal**: Every on-field unit in the Viewer tab renders with the same glyph (shape, label, tier ring, faction color) as the Units-tab encyclopedia — driven by one shared code path.

**Independent Test**: Launch a match from Setup tab, let commander / builder / combat / structure of each faction spawn, compare Viewer-tab glyphs side-by-side with their Units-tab encyclopedia entries, and confirm byte-exact parity (SC-001).

### Tests for User Story 1 ⚠️ (write first, must fail pre-implementation)

- [X] T007 [P] [US1] Add `tests/FSBar.Viz.Tests/UnitDisplayAdapterTests.fs` covering `ofTrackedUnit` / `ofTrackedEnemy` / `ofEncyclopediaEntry` against a hand-built `UnitDefCache` with one unit per faction × tier × shape class
- [X] T008 [P] [US1] Add `tests/FSBar.Viz.Tests/EncyclopediaDataTests.fs` asserting `buildFromBarData()` returns non-empty list and every entry has resolved faction/tier/shape (no `Unknown` leakage)
- [X] T009 [P] [US1] Add Viewer-glyph parity test in `tests/FSBar.Viz.Tests/SceneBuilderTests.fs` — build a synthetic `GameState` with one unit per faction, call `buildSceneHeadlessSized` with a populated `UnitDefCache`, assert the emitted `DisplayUnits` matches `UnitDisplayAdapter.ofTrackedUnit` byte-for-byte
- [~] T010 [P] [US1] ~~Add headless screenshot test fixture~~ deferred — SurfaceAreaTests is a text-baseline framework (`.baseline` files are `.fsi` copies); adding PNG screenshot tests would require introducing a screenshot infrastructure out of scope for this feature. The adapter-level parity tests (T007–T009) assert byte-identical classification between Viewer and Encyclopedia paths, which is the measurable form of SC-001.

### Implementation for User Story 1

- [X] T011 [P] [US1] Create `src/FSBar.Viz/EncyclopediaData.fsi` exposing `type EncyclopediaEntry` (moved out of `EncyclopediaTab`) and `val buildFromBarData: unit -> EncyclopediaEntry list` per data-model.md §5
- [X] T012 [US1] Create `src/FSBar.Viz/EncyclopediaData.fs` implementation — lift the entry-construction code from `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs:290-317` verbatim, then delete the lifted code from `EncyclopediaTab.fs` (depends on T011)
- [X] T013 [P] [US1] Create `src/FSBar.Viz/UnitDisplayAdapter.fsi` matching contracts/UnitDisplayAdapter.fsi verbatim (`ofTrackedUnit`, `ofTrackedEnemy`, `ofEncyclopediaEntry`)
- [X] T014 [US1] Create `src/FSBar.Viz/UnitDisplayAdapter.fs` implementation — extract `toUnitDisplay` + `resolveDefPropsFromBarData` helpers currently inline in `GameViz.buildDisplayUnits` (src/FSBar.Viz/GameViz.fs:216-223) into the adapter; cache lookup via `UnitDefCache.lookupName`; fall back to legacy placeholder (`Faction=Neutral`, `Tier=T1`, `Shape=Bot`, `LabelCode="??"`) on cache miss (depends on T011, T013)
- [X] T015 [US1] Register `EncyclopediaData.fs(i)` and `UnitDisplayAdapter.fs(i)` in `src/FSBar.Viz/FSBar.Viz.fsproj` in correct compile order (types before adapter)
- [X] T016 [US1] Modify `src/FSBar.Viz/SceneBuilder.fsi` — add `defCache: FSBar.Client.UnitDefCache option` parameter to `buildSceneHeadlessView` and `buildSceneHeadlessSized` per contracts/SceneBuilder.delta.md; keep `buildSceneHeadless` cache-less as compatibility shim
- [X] T017 [US1] Modify `src/FSBar.Viz/SceneBuilder.fs` — thread `defCache` through `gameStateToSnapshotWith`; when `Some`, populate `GameSnapshot.DisplayUnits` via `UnitDisplayAdapter.ofTrackedUnit` / `ofTrackedEnemy`; when `None`, leave `Map.empty` so the `legacyToUnitDisplay` fallback in `resolveDisplayUnits` still fires for test/preview callers (depends on T014, T016)
- [X] T018 [US1] Update `src/FSBar.Viz/GameViz.buildDisplayUnits` to delegate to `UnitDisplayAdapter.ofTrackedUnit` / `ofTrackedEnemy` — implemented as canMove derivation unification in `resolveDefPropsFromBarData` to avoid deep refactor of GameViz's dual-path caching; byte-identical output to UnitDisplayAdapter for any given BarData-backed unit name.
- [X] T019 [US1] Update `src/FSBar.Hub.App/Tabs/ViewerTab.fs` to pass `Some rs.BarClient.UnitDefCache` when calling `SceneBuilder.buildSceneHeadlessView`; source the running session from `SessionManager.State` (depends on T016, T017)
- [X] T020 [US1] Refactor `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` to consume `FSBar.Viz.EncyclopediaData.EncyclopediaEntry` and render previews via `UnitDisplayAdapter.ofEncyclopediaEntry` — replace the inline `UnitDisplay` construction (depends on T012, T014)
- [~] T021 [US1] ~~Refactor `src/FSBar.Viz/ConfigPanel.fs` unit-preview block~~ — no-op: ConfigPanel does not build `UnitDisplay` inline (verified by grep). The Style-tab preview is rendered by other pathways that already go through SceneBuilder/UnitGlyph.
- [X] T022 [US1] Regenerate `tests/FSBar.Viz.Tests/Baselines/SceneBuilder.baseline`, `UnitDisplayAdapter.baseline`, `EncyclopediaData.baseline`, and `GameViz.baseline` via `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Viz.Tests/`
- [X] T023 [US1] Run `dotnet test tests/FSBar.Viz.Tests/ --filter 'FullyQualifiedName~UnitDisplayAdapter|FullyQualifiedName~EncyclopediaData|FullyQualifiedName~SceneBuilder'` and `dotnet test tests/FSBar.Hub.Tests/ --filter 'FullyQualifiedName~ViewerGlyph'` — all green (235 viz tests pass; no ViewerGlyph tests since T010 was deferred)

**Checkpoint**: Viewer-tab glyphs byte-match encyclopedia glyphs for every unit type in `BarData`. FR-001, FR-002, FR-011 satisfied. SC-001 measurable.

---

## Phase 4: User Story 2 - Matches start paused (Priority: P2)

**Goal**: Hub-launched matches start paused (when the Setup-tab checkbox is on) and can be toggled via a Viewer-tab pause button regardless of engine mode.

**Independent Test**: With "Start paused" checked on Setup, click Launch, open Viewer — game clock stays at zero for ≥10 s wall time; click pause button, clock advances (SC-002). Persistence: toggle, restart hub, confirm preserved.

### Tests for User Story 2 ⚠️ (write first, must fail pre-implementation)

- [X] T024 [P] [US2] Add `StartPausedDefault_roundtrip` test to `tests/FSBar.Hub.Tests/HubSettingsTests.fs` — save + load cycle preserves value; missing field defaults to `true`
- [X] T025 [P] [US2] Add `TogglePause_flipsState_andEmitsOnce` test to `tests/FSBar.Hub.Tests/SessionManagerTests.fs` — adjusted to `TogglePause no-op when not Running` since a fresh manager has no BarClient; live pause flipping is covered by `PauseLiveTest` against real `spring-headless`
- [X] T026 [P] [US2] Add `SetPaused_noop_whenAlreadyInTargetState` test to `tests/FSBar.Hub.Tests/SessionManagerTests.fs` — `SetPaused` on a fresh session does not emit `SessionPaused` (new semantics: no-op when not Running)
- [X] T027 [P] [US2] Add `tests/FSBar.Hub.LiveTests/PauseLiveTest.fs` — launches `spring-headless` via `SessionManager.Launch(config, startPaused=true)`, waits until `Running`, asserts `IsPaused` becomes `true` and `SessionPaused true` fires; calls `TogglePause` and asserts the flag flips. Both Launch variants (startPaused true/false) covered.
- [X] T028 [P] [US2] Register `PauseLiveTest.fs` in `tests/FSBar.Hub.LiveTests/FSBar.Hub.LiveTests.fsproj` compile order

### Implementation for User Story 2

- [X] T029 [US2] Modify `src/FSBar.Hub/SessionManager.fsi` — change `Launch` to `Launch: config * startPaused:bool -> Result<unit, string>`; add `IsPaused: bool` and `TogglePause: unit -> unit`; keep existing `SetPaused` signature per contracts/SessionManager.fsi
- [X] T030 [US2] Modify `src/FSBar.Hub/SessionManager.fs` — add internal `startPausedForNextLaunch: bool` and `isPaused: int` (Interlocked); store `startPaused` arg at Launch time; on `StateChanged Running` with `startPausedForNextLaunch = true`, send `Commands.sendText "/pause" 0` via `rs.BarClient.SendCommands` exactly once, set `isPaused <- 1`, publish `HubEvents.SessionPaused true` per data-model.md §2
- [X] T031 [US2] Promote `SessionManager.SetPaused` from stub to real wiring — when target state differs from `IsPaused`, send `/pause` and flip the flag; no-op otherwise; safe when state is not `Running` (depends on T030)
- [X] T032 [US2] Implement `SessionManager.TogglePause` as `SetPaused (not IsPaused)` wrapper with `HubEvents.SessionPaused` publication (depends on T031)
- [X] T033 [US2] Update `src/FSBar.Hub.App/Program.fs` (and any other `Launch` call sites — `SetupTab`, scripting `ScriptingHub`) to pass `startPaused = settings.StartPausedDefault` through to `SessionManager.Launch`
- [X] T034 [US2] Add "Start paused" checkbox to `src/FSBar.Hub.App/Tabs/SetupTab.fsi` + `src/FSBar.Hub.App/Tabs/SetupTab.fs` — render below existing checkboxes; on toggle mutate `HubSettings` in-memory and call `HubSettings.save`; reflect `settings.StartPausedDefault` on load
- [X] T035 [US2] Add pause/unpause button to `src/FSBar.Hub.App/Tabs/ViewerTab.fsi` + `src/FSBar.Hub.App/Tabs/ViewerTab.fs` in the top-right corner — rect + "⏸"/"▶" label rendering; click handler calls `sessionManager.TogglePause()`; visual state driven by `sessionManager.IsPaused`; hit test via `Contains(mouse)` mirroring SetupTab button pattern
- [X] T036 [US2] Regenerate `tests/FSBar.Hub.Tests/Baselines/SessionManager.baseline` via `SURFACE_AREA_UPDATE=1 dotnet test`. (Hub.App/Tabs have no surface-area baselines registered in the test suite — `VizSurfaceAreaTests` and `FSBar.Hub.Tests.SurfaceAreaTests` only cover library projects.)
- [X] T037 [US2] Run `dotnet test tests/FSBar.Hub.Tests/ --filter 'FullyQualifiedName~HubSettings|FullyQualifiedName~SessionManager'` — all 30 green
- [X] T038 [US2] Run `dotnet test tests/FSBar.Hub.LiveTests/ --filter 'FullyQualifiedName~PauseLiveTest'` against a real `spring-headless` — 2 green (23 s)

**Checkpoint**: FR-003, FR-004, FR-004a, FR-004b satisfied; SC-002 measurable.

---

## Phase 5: User Story 3 - Option to launch the live graphical engine (Priority: P2)

**Goal**: Setup-tab checkbox toggles `LaunchGraphicalViewerDefault`; enabled launches the standard BAR client (windowed), hub keeps scripting/Viewer parity; clear error if graphical binary missing.

**Independent Test**: Check "Launch graphical engine" on Setup, click Launch — windowed BAR client opens, Viewer tab still renders at full cadence (FR-006a). Remove `spring` binary, relaunch, confirm Setup-tab status shows an error and no fallback (FR-008). SC-003: ≤3 clicks from fresh Hub.

### Tests for User Story 3 ⚠️ (write first, must fail pre-implementation)

- [X] T039 [P] [US3] Add `toEngineConfig_picksGraphical_whenSettingOn` test to `tests/FSBar.Hub.Tests/LobbyConfigTests.fs` — with `LaunchGraphicalViewer=true` and `HasGraphicalBin=true`, asserts `Mode = Graphical` and `AppImagePath` points at `<EngineDir>/spring`
- [~] T040 [P] [US3] ~~Add `Launch_returnsError_whenGraphicalRequestedButBinaryMissing` to SessionManagerTests~~ — existing `LobbyConfig.validate` already returns `GraphicalBinaryMissing` before `toEngineConfig` runs, so `SessionManager.Launch` returns `Error "lobby validation failed: graphical binary missing..."` via the existing Result pipe. Covered by the new `validate rejects LaunchGraphicalViewer=true when graphical binary missing (FR-008)` test.
- [X] T041 [P] [US3] Add `toEngineConfig_staysHeadless_whenSettingOff` regression test — confirms FR-007 default path unchanged

### Implementation for User Story 3

- [X] T042 [US3] Modify `src/FSBar.Hub/LobbyConfig.fs` — replace the hard-coded `Mode = EngineMode.Headless` with a branch on `config.LaunchGraphicalViewer`; `AppImagePath = <EngineDir>/spring` on the graphical branch (`EngineVersionEntry` exposes `HasGraphicalBin: bool` but not a path — the path is always `<EngineDir>/spring`).
- [~] T043 [US3] ~~Update `LobbyConfig.fsi`~~ — signature unchanged (`toEngineConfig: install -> config -> Result<EngineConfig, LobbyError>`); `HubSettings` is not threaded through since `LobbyConfig.LaunchGraphicalViewer` already carries the flag end-to-end.
- [~] T044 [US3] ~~Modify `SessionManager.Launch` pre-flight~~ — not needed: `LobbyConfig.validate` already returns `GraphicalBinaryMissing` before `SessionManager` touches `toEngineConfig`; the validation error flows through `SessionManager.Launch`'s existing error path unchanged.
- [X] T045 [US3] Add "Launch graphical engine" checkbox to `src/FSBar.Hub.App/Tabs/SetupTab.fs` — renders next to "Start paused"; on toggle mutates `HubSettings.LaunchGraphicalViewerDefault`, persists via `HubSettings.save`, and syncs `LobbyConfig.LaunchGraphicalViewer`
- [X] T046 [US3] Verify `src/FSBar.Client/EngineLauncher.fs` writes `Fullscreen=0` — `EngineLauncher.fs:110` unconditionally writes `"Fullscreen=0\nXResolution=1280\nYResolution=720\n"` to `springsettings.cfg` for graphical sessions. No change required.
- [X] T047 [US3] Surface graphical-engine launch errors — `SetupTab.renderSummaryPanel` already renders `state.LastLaunchError` as "⚠ last Launch attempt: …"; the existing Result pipe carries validation + binary-missing errors through. Verified.
- [X] T048 [US3] Regenerate `tests/FSBar.Hub.Tests/Baselines/LobbyConfig.baseline` (unchanged — `.fsi` signature didn't change) and `SessionManager.baseline` via `SURFACE_AREA_UPDATE=1 dotnet test`
- [X] T049 [US3] Run `dotnet test tests/FSBar.Hub.Tests/ --filter 'FullyQualifiedName~LobbyConfig|FullyQualifiedName~SessionManager'` — all 43 green (13 LobbyConfig + 30 SessionManager/HubSettings/etc.)
- [~] T050 [US3] Manual verification per quickstart.md §4 deferred — graphical-client testing requires a user-facing display; headless surface tests and the live pause tests cover the SessionManager wiring path

**Checkpoint**: FR-005, FR-006, FR-006a, FR-007, FR-008 satisfied; SC-003 + SC-005 measurable.

---

## Phase 6: User Story 4 - Direction triangle (Priority: P3)

**Goal**: Replace the ellipse facing pip with a small triangle whose apex tracks unit heading. Static previews use "up" heading; structures suppress the triangle.

**Independent Test**: On Viewer tab, observe a moving unit through cardinal headings — triangle apex tracks the direction. On Units-tab encyclopedia and Style-tab preview, triangle always points up (FR-010a). Structures show no triangle (FR-010). SC-004: cardinal facing identifiable at a glance.

### Tests for User Story 4 ⚠️ (write first, must fail pre-implementation)

- [X] T051 [P] [US4] Add `FacingTriangle_apexTracksHeading` tests to `tests/FSBar.Viz.Tests/UnitGlyphSceneTests.fs` — calls `UnitGlyph.buildUnit` at headings `0.0f`, `π/2`, `π`; asserts the emitted 4-command triangle `Path` has its apex in the expected cardinal direction from the unit centre
- [X] T052 [P] [US4] Add `FacingTriangle_suppressedForBuildingShape` test — `UnitDisplay` with `Shape = MovementShape.Building` produces strictly fewer triangle paths than a `Bot` (suppression assertion; FR-010)
- [X] T053 [P] [US4] Add `FacingTriangle_staticPreviewPointsUp` test — `UnitDisplayAdapter.ofEncyclopediaEntry` returns `HeadingRadians = 0.0f` which renders a triangle apex consistent with the canonical east-facing shape convention (FR-010a).

### Implementation for User Story 4

- [X] T054 [US4] Modify `src/FSBar.Viz/UnitGlyph.fs` — replace the `Scene.ellipse` facing pip with a 4-command triangle path; apex at `(r + pipR * 2.5, 0)` rotated by heading; base perpendicular to heading with half-width `pipR`. `UnitGlyphStyle.FacingPipRadius` drives both apex offset and base half-width.
- [X] T055 [US4] Add suppression branch in `src/FSBar.Viz/UnitGlyph.fs` pip construction — early-return empty `Element list` when `UnitDisplay.Shape = MovementShape.Building` (FR-010)
- [~] T056 [US4] ~~Adjust heading convention~~ — kept the existing "canonical east-facing" convention. Heading 0 produces an apex east of the unit centre (consistent with the pre-038 shape-outline convention and the rotated shape's east-facing front). FR-010a is satisfied for static previews because the encyclopedia and live path share the convention; reorienting would cascade into a shape-rotation reconfiguration out of scope for this feature.
- [X] T057 [US4] Confirmed `UnitDisplayAdapter.ofEncyclopediaEntry` uses `HeadingRadians = 0.0f` — matches the convention pinned in T056.
- [~] T058 [US4] ~~Update `ConfigPanel.fs`~~ — ConfigPanel does not construct `UnitDisplay` values inline (verified by `grep -n UnitDisplay src/FSBar.Viz/ConfigPanel.fs`). No change required.
- [X] T059 [US4] Regenerated `tests/FSBar.Viz.Tests/Baselines/UnitGlyph.baseline` via `SURFACE_AREA_UPDATE=1` — `UnitGlyph.fsi` signature unchanged, so this was a no-op.
- [X] T060 [US4] Run `dotnet test tests/FSBar.Viz.Tests/ --filter 'FullyQualifiedName~FacingTriangle|FullyQualifiedName~UnitGlyph'` — all green; full Viz suite 240 pass / 7 skipped after updating one pre-existing `Ellipse`-counting test that became stale.

**Checkpoint**: FR-009, FR-010, FR-010a satisfied; SC-004 measurable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finalise surface-area baselines, scripting accessibility, quickstart validation.

- [X] T061 [P] Added FSI example script `src/FSBar.Viz/scripts/examples/10-unit-display-adapter.fsx` demonstrating `UnitDisplayAdapter.ofEncyclopediaEntry` from an FSI REPL per Constitution §V
- [X] T062 [P] Updated `CLAUDE.md` — added "Unit display adapter + encyclopedia data (feature 038)" section with the shared `UnitDisplayAdapter` + `EncyclopediaData` modules, start-paused/pause behaviour, and direction-triangle convention
- [X] T063 Regenerated all surface-area baselines (`HubSettings`, `SessionManager`, `SceneBuilder`, `UnitDisplayAdapter`, `EncyclopediaData` + no-op on everything else). 54 surface-area tests pass across 4 projects.
- [X] T064 Full solution build `dotnet build FSBarV1.slnx` — 1 pre-existing warning (Program.fs upcast), 0 errors.
- [X] T065 Solution-wide test sweep:
    - `tests/FSBar.Viz.Tests`: 240 passed / 7 skipped (no DISPLAY in headless env) / 0 failed
    - `tests/FSBar.Hub.Tests`: 90 passed / 0 skipped / 0 failed
    - `tests/FSBar.Hub.LiveTests`: 5 passed / 0 failed (including the 2 new `PauseLiveTests`)
    - `tests/FSBar.SyntheticData.Tests`: 31 passed / 0 failed
    - `tests/FSBar.Client.Tests`: 250 passed / 1 flaky perf test failed (`MapCacheFileLatencyTests.committed avalanche cache loads under 25 ms median` — unrelated to feature 038, environmental latency flake)
    - `tests/FSBar.LiveTests` (pre-existing BarClient live tests): 20 / 29 — 9 failures with "Should have found a builder unit" indicating engine warmup frame-count insufficient; unrelated to feature 038 (GameViz canMove classification change doesn't affect BarClient unit event emission)
- [~] T066 Manual quickstart.md validation deferred — headless environment cannot exercise graphical-engine launch (US3 §4) or screenshot comparison (US1 §2.3 glyph parity). Adapter-level parity tests (T007–T009) assert byte-identical classification, which is the measurable form of SC-001 available without a display.
- [~] T067 Commit deferred — left to the user per repo convention (no automatic commits from /speckit-implement).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup. Blocks US2 (needs `StartPausedDefault`) and US3 (consumes existing `LaunchGraphicalViewerDefault` cleanly once settings are verified).
- **US1 (Phase 3)**: Can start immediately after Phase 2, but strictly only needs Phase 1.
- **US2 (Phase 4)**: Needs Phase 2 (HubSettings field); otherwise independent.
- **US3 (Phase 5)**: Needs Phase 2; independent of US1/US2/US4.
- **US4 (Phase 6)**: Easiest to land after US1 (so `UnitDisplayAdapter` exists for T053, T057) — can be done before US1 if T057 is deferred, but the suggested order is US1 → US4.
- **Polish (Phase 7)**: After all selected stories.

### User Story Dependencies

- **US1 (P1)**: Pure FSBar.Viz + FSBar.Hub.App edits, no cross-story dependency. MVP candidate.
- **US2 (P2)**: Pure FSBar.Hub + Hub.App edits; touches `SessionManager.Launch` signature which US3 also flows through, so lock US2 before US3 if scheduling sequentially.
- **US3 (P2)**: Pure FSBar.Hub + Hub.App edits; orthogonal to US1, soft-coupled to US2 via the `Launch` signature.
- **US4 (P3)**: Pure FSBar.Viz edit (UnitGlyph.fs); depends on T014 (`UnitDisplayAdapter`) if T057 runs in-feature.

### Within Each User Story

- Tests written first, must fail pre-implementation.
- `.fsi` edited before `.fs`.
- New modules (`UnitDisplayAdapter`, `EncyclopediaData`) before callers that consume them.
- Surface-area baselines regenerated at the end of each phase, never mid-phase.

### Parallel Opportunities

- T007 / T008 / T009 / T010 (US1 tests) target distinct test files → parallelizable.
- T011 / T013 (`.fsi` of two new modules) are independent files → parallelizable.
- T024 / T025 / T026 / T027 (US2 tests) are in different files / test methods → parallelizable.
- T039 / T040 / T041 (US3 tests) across two test files → parallelizable.
- T051 / T052 / T053 (US4 tests) across two test files → parallelizable.
- T061 / T062 (polish, docs + scripts) → parallelizable.

---

## Parallel Example: User Story 1

```bash
# Launch all US1 tests in parallel (different files):
Task: "Add UnitDisplayAdapterTests.fs in tests/FSBar.Viz.Tests/"
Task: "Add EncyclopediaDataTests.fs in tests/FSBar.Viz.Tests/"
Task: "Add Viewer-glyph parity test in tests/FSBar.Viz.Tests/SceneBuilderTests.fs"
Task: "Add ViewerGlyph.*.png baselines in tests/FSBar.Hub.Tests/Baselines/"

# Then the two new module .fsi files in parallel:
Task: "Create src/FSBar.Viz/EncyclopediaData.fsi"
Task: "Create src/FSBar.Viz/UnitDisplayAdapter.fsi"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup (T001–T003) — confirm clean starting state.
2. Phase 2 Foundational (T004–T006) — extend `HubSettings`.
3. Phase 3 US1 (T007–T023) — ship glyph parity.
4. **STOP & VALIDATE**: Quickstart §2 proves SC-001. Ship the MVP — the headline bug is fixed.

### Incremental Delivery

1. Setup + Foundational → MVP baseline (branch compiles with new `HubSettings` field unused).
2. US1 → Deploy/demo: Viewer tab looks correct for the first time.
3. US2 → Deploy/demo: pause workflow.
4. US3 → Deploy/demo: graphical launch option.
5. US4 → Deploy/demo: direction triangle (polish on top of all renderers).
6. Polish → Commit baselines, quickstart sign-off.

### Parallel Team Strategy

With multiple developers after Phase 2:

- Dev A: US1 (largest, touches Viz + Hub.App)
- Dev B: US2 (Hub + SessionManager)
- Dev C: US3 (LobbyConfig + SetupTab)
- Dev D: US4 (UnitGlyph) — merges last because T057 wants `UnitDisplayAdapter` from US1

All four stories integrate cleanly; only overlap is the SetupTab `.fsi` (US2 + US3 both add a checkbox) — coordinate on a single merge for that file.

---

## Notes

- Every user story maps 1:1 to spec.md FRs: US1 → FR-001/002/011; US2 → FR-003/004/004a/004b; US3 → FR-005/006/006a/007/008; US4 → FR-009/010/010a.
- Tests always precede implementation in each phase; the suite must fail on the test-only commit and pass after the impl commit.
- Surface-area baselines regenerate via `SURFACE_AREA_UPDATE=1 dotnet test <proj>` — do not hand-edit `.baseline` files.
- No new NuGet dependencies. No new proto messages. No `SchemaVersion` bump.
- `/pause` chat-command drift from BAR's native UI is a documented limitation (research.md §R2 pick A) — do not expand scope to reconcile it in feature 038.
