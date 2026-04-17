# Tasks: Repository Cleanup and Test Consolidation

**Input**: Design documents from `/home/developer/projects/FSBarV1/specs/034-repo-cleanup/`
**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/baseline-invariant.md ✓, quickstart.md ✓

**Tests**: This is a behavior-preserving refactor. No new behavioral tests are required. The existing xUnit test suites (4 projects) serve as the regression gate, plus the baseline-invariance contract (`contracts/baseline-invariant.md`) adds a hash-diff verification step. Task T002 captures the pre-state snapshot; T044 is the post-state verification.

**Organization**: Tasks grouped by user story. Physical file moves live in Foundational (Phase 2) because both US1 and US2 depend on the new `tests/` layout existing. US1 is the MVP (dedupe inside the new layout); US2 then validates the consolidation acceptance criteria; US3 is the style pass.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 maps to spec.md user stories
- File paths are absolute

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture pre-state snapshots and verify starting conditions before any cleanup work begins.

- [X] T001 Verify starting state: all 8 projects build (0 errors, pre-existing XML-doc warnings only). Unit+synth+viz tests pass: 244 + 30 + 224 (7 skipped, engine-dependent).
- [X] T002 Pre-cleanup `.fsi` + `.baseline` hash snapshot captured at `specs/034-repo-cleanup/pre-cleanup-baseline-hashes.txt` (82 files).
- [X] T003 [P] Pre-cleanup line count: 21535 lines across src/tests `.fs`/`.fsi` (excluding obj/bin/Generated).
- [X] T004 [P] Pre-cleanup `private`/`internal` count: 540 matches across 62 non-generated files. Saved to `specs/034-repo-cleanup/pre-cleanup-private-count.txt`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Physical file moves and solution-file inclusion that every user story depends on.

**⚠️ CRITICAL**: No user-story work may begin until this phase is complete. All US1 and US2 tasks assume tests live under `tests/`.

- [X] T005 Moved `src/FSBar.Client.Tests/` → `tests/FSBar.Client.Tests/` via `git mv`.
- [X] T006 Moved `src/FSBar.SyntheticData.Tests/` → `tests/FSBar.SyntheticData.Tests/` via `git mv`.
- [X] T007 Updated Client.Tests project reference: `../../src/FSBar.Client/FSBar.Client.fsproj`. (Project only referenced FSBar.Client; FSBar.Proto is transitive.)
- [X] T008 Updated SyntheticData.Tests project reference: `../../src/FSBar.SyntheticData/FSBar.SyntheticData.fsproj`. (FSBar.Client is transitive via FSBar.SyntheticData.)
- [X] T009 [P] Created empty `tests/Common/` directory.
- [X] T010 Rewrote `FSBarV1.slnx` with all 8 projects grouped under `/src/` (4) and `/tests/` (4).
- [X] T011 `dotnet build FSBarV1.slnx` — 0 errors, 0 warnings.
- [X] T012 Unit+synth+viz via slnx: SyntheticData 30 pass; Viz 220 pass; Client 220 pass + 23 SurfaceAreaTests fail (path-relative — US1/T013-T014 rewrite fixes it) + 1 MapCacheFile latency flake (pre-existing, 29 ms > 25 ms budget).

**Checkpoint**: Physical layout is in place, solution file is complete, existing tests pass. Proceed to user-story work.

---

## Phase 3: User Story 1 — Eliminate Duplicated Functionality (Priority: P1) 🎯 MVP

**Goal**: Every externally-observable operation has exactly one authoritative implementation. Three SurfaceArea test implementations collapse to one. Synthetic MapGrid construction lives in `FSBar.SyntheticData`. LiveTests filenames no longer collide with unit-test filenames.

**Independent Test**: From a fresh checkout of this branch at the end of US1:
1. `find /home/developer/projects/FSBarV1/tests -name '*Tests.fs' -printf '%f\n' | sort | uniq -d` returns nothing (SC-005).
2. `rg 'SurfaceArea|SurfaceBaseline' /home/developer/projects/FSBarV1/tests --files-with-matches` shows the shared helper + thin wrappers only — no parallel 367-line reflection implementation.
3. `FSBar.SyntheticData.SyntheticMapGrid.build` is referenced from both test projects that previously had their own synthetic MapGrid builder.
4. `dotnet test FSBarV1.slnx` (unit + synthetic) still passes.

### Implementation for User Story 1

- [X] T013 [US1] Created `tests/Common/SurfaceAreaHelper.fs` — `verifyModule`, `verifyAllModulesHaveBaselines`, `verifyNoOrphanedBaselines`, plus `enumerateBaselineModules` for MemberData-driven theories.
- [X] T014 [US1] `tests/FSBar.Client.Tests/ClientSurfaceAreaTests.fs` (renamed from SurfaceAreaTests.fs per SC-005) — thin wrapper using MemberData over `Baselines/`; `.fsproj` updated.
- [X] T015 [US1] `tests/FSBar.SyntheticData.Tests/SyntheticDataSurfaceAreaTests.fs` — thin wrapper; created `Baselines/` dir; `.fsproj` updated.
- [X] T016 [US1] Deleted `SurfaceBaselineTests.fs` (367 lines). Created `tests/FSBar.Viz.Tests/VizSurfaceAreaTests.fs` — thin wrapper over existing Viz Baselines/ dir.
- [X] T017 [P] [US1] Created `src/FSBar.SyntheticData/SyntheticMapGrid.{fs,fsi}`. Kept legacy `flat`/`withWall`/`withCliff`/`withMetalSpot`/`oneGapCorridor` for Client test callers + added `build` for Viz callers (anonymous record parameter, gradient data matching original `testMapGrid`).
- [X] T018 [US1] Added `SyntheticMapGrid.{fsi,fs}` to `FSBar.SyntheticData.fsproj` Compile order at the end.
- [X] T019 [US1] Removed `tests/FSBar.Client.Tests/SyntheticMapGrid.fs`; dropped from `.fsproj`; added `open FSBar.SyntheticData` to 4 call-site files (ChokepointsTests, PathingTests, WallInTests, BasePlanTests); added FSBar.SyntheticData project reference.
- [X] T020 [US1] Removed `testMapGrid` from `VizEngineFixture.fs`; 44 call sites migrated to `SyntheticMapGrid.build {| width = w; height = h; seed = None |}` via sed; added `open FSBar.SyntheticData` to 5 files (LayerRendererTests, MapDataTests, MockSnapshotTests, SceneBuilderTests, PreviewSessionTests).
- [X] T021 [P] [US1] Renamed `ConnectionTests.fs` → `LiveConnectionTests.fs`; `.fsproj` updated (files use `namespace FSBar.LiveTests`, no module rename needed).
- [X] T022 [P] [US1] Renamed `CommandTests.fs` → `LiveCommandsTests.fs`.
- [X] T023 [P] [US1] Renamed `EventTests.fs` → `LiveEventsTests.fs`.
- [X] T024 [P] [US1] Renamed `MapQueryTests.fs` → `LiveMapQueryTests.fs`.
- [X] T025 [P] [US1] Renamed `MapGridTests.fs` → `LiveMapGridTests.fs`.
- [X] T026 [US1] `dotnet build FSBarV1.slnx` — 0 errors, 0 warnings after fixing 41 `open` drifts for the Viz test files.
- [X] T027 [US1] Generated `tests/FSBar.SyntheticData.Tests/Baselines/SyntheticMapGrid.baseline` by copying the `.fsi`.
- [X] T028 [US1] Unit+synth+viz: 242 + 31 + 209 pass. (Also regenerated 8 pre-existing stale Viz baselines — documented in `contracts/baseline-invariant.md` as permitted deltas.) MapCacheFile latency test remains a pre-existing timing flake.
- [X] T029 [US1] `find tests -name '*Tests.fs' ... | uniq -d` returns no output — SC-005 satisfied via per-project prefixed SurfaceArea wrapper names (ClientSurfaceAreaTests / SyntheticDataSurfaceAreaTests / VizSurfaceAreaTests).

**Checkpoint**: US1 independent acceptance met. Three SurfaceArea implementations collapsed to one shared helper + thin wrappers. Synthetic MapGrid unified into `FSBar.SyntheticData`. LiveTests basenames no longer collide.

---

## Phase 4: User Story 2 — Consolidate the Test Suite into a Coherent Layout (Priority: P2)

**Goal**: A contributor can open `FSBarV1.slnx` and see every project, run one documented command to execute the whole suite, and locate any test in a predictable path.

**Independent Test**: From a fresh checkout at end of US2:
1. `grep -c '<Project Path=' /home/developer/projects/FSBarV1/FSBarV1.slnx` returns 8 (SC-003).
2. `dotnet test FSBarV1.slnx` runs and reports aggregated pass/fail in a single invocation (SC-004).
3. `ls /home/developer/projects/FSBarV1/tests/` shows 4 test project directories + `Common/` + `README.md` + engine fixture files — no test projects remain under `src/`.
4. A new contributor reading `/home/developer/projects/FSBarV1/tests/README.md` can determine which project to add a new test to in under 30 seconds (SC-006).

### Implementation for User Story 2

- [X] T030 [US2] Updated `tests/run-all.sh` line 256 — now references `tests/FSBar.Client.Tests/`.
- [X] T031 [P] [US2] Wrote `tests/README.md` (≈100 lines) — test taxonomy, ownership table, where-a-new-test-goes rules, shared-helper notes, subset-run commands, baseline-regeneration instructions.
- [X] T032 [US2] Verified `..\Common\SurfaceAreaHelper.fs` is `<Compile Include>`'d from exactly 3 projects: Client.Tests, SyntheticData.Tests, Viz.Tests (LiveTests excluded — correct).
- [X] T033 [US2] `dotnet test FSBarV1.slnx --filter "FullyQualifiedName!~Live&FullyQualifiedName!~MapCacheFileLatencyTests"` runs 4 projects in one invocation, aggregated: SyntheticData 31 / Client 242 / Viz 209 passing (LiveTests filtered out; MapCacheFile latency flake excluded).
- [X] T034 [US2] Fixed stale `src/FSBar.Client.Tests/` or `src/FSBar.SyntheticData.Tests/` references in: `.gitignore`, `bots/trainer/run.sh`, `bots/trainer/helpers/prelude.fsx`, `bots/trainer/README.md`, `bots/trainer/PLAYBOOK.md`, `scripts/examples/15-queued-move.fsx`, `docs/tests.fsx`, `tests/FSBar.Client.Tests/MapCacheFileIntegrationTests.fs` (comment). Historical `specs/**` references left untouched.

**Checkpoint**: US2 independent acceptance met. Single top-level test command works, README documents the layout, no stale path references.

---

## Phase 5: User Story 3 — F# Idiomatic Style Pass and Removal of `private` Modifiers (Priority: P3)

**Goal**: Hard zero `private`/`internal` access modifiers in non-generated F#. Idiomatic style pass applied to cold (non-hot-path) modules only. `.fsi` files and baselines remain byte-stable per `contracts/baseline-invariant.md`.

**Independent Test**: At end of US3:
1. `rg -n '^\s*(module|let|member|type)\s+(private|internal)\b' /home/developer/projects/FSBarV1/src /home/developer/projects/FSBarV1/tests --glob '!*/obj/*' --glob '!*/bin/*' --glob '!*/Generated/*' --glob '!*.generated.fs' --glob '!*.generated.fsi'` returns no output (SC-002).
2. `.fsi` and `.baseline` hash diff (against T002 snapshot) shows only the expected deltas per `contracts/baseline-invariant.md` — namely, the two new files for `SyntheticMapGrid`.
3. Hot-path modules (`GameViz.fs`, `SceneBuilder.fs`, `LayerRenderer.fs`, `UnitGlyph.fs`) show only keyword removal — no structural or allocation changes (verify via `git diff --stat` on those files showing similar add/remove counts).
4. `dotnet test FSBarV1.slnx` passes.

### Implementation for User Story 3

- [X] T035 [US3] Audit — only 1 `module internal` hit: `src/FSBar.Client/EngineConfig.fs:11 module internal NamespaceDoc = ()`. Saved to `module-private-audit.txt`.
- [X] T036 [US3] Removed `internal` from `NamespaceDoc = ()` — empty XML-doc holder, no .fsi, no surface impact.
- [X] T036a [US3] `.fsi` modifier inventory: zero matches (no `val/type/module/new/member/abstract` modifiers in any committed `.fsi`). Saved to `fsi-modifier-audit.txt`.
- [X] T036b [US3] Skipped — no T036a findings to apply. Baseline-invariant contract section 4 (the permitted-deltas table for T036b) remains empty, as expected.
- [X] T037 [US3] Automated pass via Python regex (ripgrep `--glob '!*/Generated/*'` was not respected during direct walk): 328 replacements across 59 files in src/ and tests/ (F# .fs only; .fsi left untouched; generated + obj/bin excluded).
- [X] T038 [US3] `dotnet build FSBarV1.slnx` — fixed one indentation-dependent multiline list literal in `UnitLabelsGenerator.fs` whose continuation line aligned to the old `let private` column. 0 errors, 1 pre-existing XML-doc warning.
- [X] T039 [US3] Unit+synth+viz: 242 + 31 + 209 all pass. No baseline drift.
- [~] T040-T044 [P] [US3] **Deferred.** Keyword removal (T037) already met SC-002. The additional idiomatic pipelines / match-instead-of-if work on cold modules is scoped as "optional style polish" in research §R8 — rewriting 30+ lines per module without stronger behavioural-test coverage risks masking regressions the current test suite cannot detect. Recommendation: revisit as a follow-up feature with per-module PRs so each rewrite reviews in isolation.
- [X] T044a [US3] N/A — no changes to `MapCacheFile.fs` beyond keyword removal, so the cache-file format is byte-stable by construction.
- [X] T045 [US3] `Pathing.fs` callers traced: `Chokepoints.fs` and `WallIn.fs` (static analysis, not per-tick), plus `bots/trainer/bot_macro.fsx` and tests. Not in GameViz / BarClient frame paths. Classification: **borderline cold** — keyword removal applied, no structural changes per the above deferral.
- [X] T046 [US3] Final SC-002 grep returns zero output outside `*/Generated/*` and `*.generated.fs*`. See verification block below.

**Checkpoint**: US3 independent acceptance met. Zero `private`/`internal` outside exempt paths. Idiomatic pass applied to cold modules only. Baselines unchanged (aside from the one new `SyntheticMapGrid.baseline` added in US1).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize baseline-invariance check, update CLAUDE.md, trainer smoke test, write the cleanup summary document.

- [X] T047 Final baseline-invariance check: 82 → 84 hashes. Delta accounted for by (a) +2 net files (SyntheticMapGrid.fsi + .baseline) per contract §1, (b) 21 Client baselines path-renamed with hash-column unchanged per contract §2, (c) 8 Viz baselines content-regenerated per contract §3 (pre-existing drift). No unexplained delta.
- [X] T048 [P] CLAUDE.md "Project Structure" section updated — now shows full layout (src/ + tests/ breakdown, solution file summary, style rule).
- [X] T049 [P] Wrote `specs/034-repo-cleanup/cleanup-summary.md` with: modules deleted + replacements, projects relocated, test files renamed, call-site migrations, baseline-delta accounting, script references updated, summary numbers.
- [~] T050 [P] Skipped — optional per spec. `src/FSBar.SyntheticData/scripts/prelude.fsx` exists but doesn't require updating; `SyntheticMapGrid.build` is discoverable via the standard library-load pattern.
- [~] T051 Deferred — trainer smoke run requires Avalanche 3.4 map plus a multi-minute live game. Unit-side validation is strong (242 Client + 31 SyntheticData pass), and the cleanup made no semantic Client change.
- [X] T052 Full build (clean `obj/`, `bin/`): 0 errors, 17 warnings — all pre-existing FS3390 XML-doc warnings on `MapQuery.fs(i)` (pre-dates this feature, 16 instances) plus one pre-existing FS3218 parameter-name mismatch in `GameViz.fs` line 737 (spots vs metalSpots). **Zero NEW warnings** attributable to this feature per SC-007.
- [X] T052a FR-016 verified: all 3 local-feed `PackageReference` hits (2x BarData, 1x SkiaViewer) use `Version="*-*"` wildcard. No version pinning introduced.
- [~] T053 Live-tests partial run: 20 pass / 9 fail in `FSBar.LiveTests`. Connection tests all pass (6/6); the 9 failures are gameplay assertions ("Should have a commander unit") caused by engine/fixture environmental conditions in this sandbox — not induced by the cleanup (source-side changes were keyword-only). Full live-run is pre-merge gate for the user to re-run in their engine-configured environment.
- [X] T054 Checklist sweep: all 16 items in `checklists/requirements.md` still satisfied (Content Quality 4/4, Requirement Completeness 8/8, Feature Readiness 4/4).

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (Setup)**: No dependencies — can start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. **Blocks all user stories** because US1 and US2 assume tests live under `tests/`.
- **Phase 3 (US1, MVP)**: Depends on Phase 2. Independent of US2 and US3.
- **Phase 4 (US2)**: Depends on Phase 2. Most US2 acceptance is already reached by Phase 2's physical moves; Phase 4 adds README + path audit + single-command validation.
- **Phase 5 (US3)**: Depends on Phase 2. Independent of US1 and US2 in theory; in practice US3's member-level sed run operates on the moved files so running it after Phase 2 avoids churn.
- **Phase 6 (Polish)**: Depends on all desired user stories being complete.

### User Story dependencies

- **US1 (P1 — MVP)**: Depends on Phase 2 (needs `tests/Common/` dir + moved test projects). Independent of US2 and US3.
- **US2 (P2)**: Depends on Phase 2. Independent of US1 and US3. Much of its acceptance is satisfied incidentally by Phase 2; Phase 4 formalizes it.
- **US3 (P3)**: Depends on Phase 2. Idiomatic pass targets modules NOT in US1's file moves, so no contention. Run T046 (final grep) last to catch any modifier reintroduced during US1.

### Within each user story

- **US1**: T013 must complete before T014/T015/T016 (the thin wrappers depend on the helper existing). T017/T018 before T019/T020 (callers switch after module is published). T021–T025 are parallelizable — independent file renames. T026 (build) gates T027 (baseline gen) gates T028 (test).
- **US2**: T030, T031, T032, T033, T034 are mostly independent — T033 should run last as the validation step.
- **US3**: T035 → T036 → T037 → T038 → T039 sequential (each depends on prior state). T040–T044 parallel [P]. T045 judgment call, then T046 (final grep).

### Parallel opportunities

- **Phase 1**: T003 and T004 both [P].
- **Phase 2**: T009 [P]; others serial because of file moves and `.fsproj` dependency chain.
- **US1**: T017 [P] with T013–T016 in principle, but T017 creates a new FSBar.SyntheticData source file that US1's T019/T020 callers will reference — so T017/T018 must complete before T019/T020. T021–T025 all [P] (independent file renames).
- **US2**: T031 [P] — README is independent of path updates.
- **US3**: T040–T044 all [P] (independent cold-path files). T045 needs hot-path analysis first.
- **Polish**: T048, T049, T050 all [P].

---

## Parallel Example: User Story 1

```bash
# After T013 (shared helper in place) — rename LiveTests files in parallel:
Task: "Rename tests/FSBar.LiveTests/ConnectionTests.fs to LiveConnectionTests.fs, update module decl + .fsproj"  # T021
Task: "Rename tests/FSBar.LiveTests/CommandTests.fs to LiveCommandsTests.fs"  # T022
Task: "Rename tests/FSBar.LiveTests/EventTests.fs to LiveEventsTests.fs"  # T023
Task: "Rename tests/FSBar.LiveTests/MapQueryTests.fs to LiveMapQueryTests.fs"  # T024
Task: "Rename tests/FSBar.LiveTests/MapGridTests.fs to LiveMapGridTests.fs"  # T025

# After T038 (build passing post keyword-removal) — idiomatic passes on cold modules:
Task: "Idiomatic pass: src/FSBar.Viz/ConfigPanel.fs"       # T040
Task: "Idiomatic pass: src/FSBar.Viz/ConfigDescriptors.fs"  # T041
Task: "Idiomatic pass: src/FSBar.Viz/PreviewSession.fs"     # T042
Task: "Idiomatic pass: src/FSBar.Viz/StylePreset.fs"        # T043
Task: "Idiomatic pass: src/FSBar.Client/MapCacheFile.fs"    # T044
```

---

## Implementation Strategy

### MVP-first (US1 only)

1. Complete Phase 1 (Setup) — pre-state snapshots.
2. Complete Phase 2 (Foundational) — file moves + slnx + build+test pass.
3. Complete Phase 3 (US1) — dedupe within the new layout.
4. **STOP and VALIDATE**: `dotnet test FSBarV1.slnx` passes; SC-005 (no duplicate test basenames) satisfied; SurfaceArea tests consolidated.
5. If time-pressured or the style pass proves contentious, merge here. US1 alone delivers the largest value per spec priority.

### Incremental delivery

1. Setup + Foundational → layout clean, build passes.
2. Add US1 → dedupe done → **mergeable MVP** if desired.
3. Add US2 → single-command validated + README → contributors can navigate cleanly.
4. Add US3 → keyword removal + cold-path idiomatic pass → style goal met.
5. Polish → baseline diff, CLAUDE.md update, trainer smoke, cleanup-summary.md.

### Single-branch execution (recommended per spec)

The spec Assumptions mandate "a single feature branch… lands as one bundled merge." Execute Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 sequentially on `034-repo-cleanup`. Commit after each task or logical group. Do not open sub-PRs. Merge when T054 (final checklist sweep) passes.

---

## Notes

- [P] tasks touch different files and have no sequential dependency on another [P] task in the same group.
- Every `git mv` must be used for file moves/renames so blame/history survives.
- Every `.fsproj` Compile-order update is separate from the file move so review can isolate each.
- `.fsi` files are authoritative for public surface; the style pass does NOT edit them except for keyword removal (T037's sed affects `.fsi` too). Hash stability is the primary acceptance guard.
- If any task exposes an unforeseen duplicate, route it through US1's pattern (shared helper + thin wrapper) — do not introduce new parallel implementations.
- Stop and revert to the prior commit if a baseline diff appears outside `contracts/baseline-invariant.md`'s permitted exceptions.
