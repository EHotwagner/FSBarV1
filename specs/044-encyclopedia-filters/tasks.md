---
description: "Task list for feature 044-encyclopedia-filters"
---

# Tasks: Unit Encyclopedia Filters

**Input**: Design documents from `/specs/044-encyclopedia-filters/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/encyclopedia-filter.md, quickstart.md

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1, US2, US3 from spec.md

---

## Phase 1: Setup

**Purpose**: No new project/dependency scaffolding — this feature extends existing in-repo projects.

- [X] T001 Verify branch `044-encyclopedia-filters` is checked out and `dotnet build FSBarV1.slnx` is green before any edits.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend the Hub state contract and create the pure filter module; every user story depends on these.

**⚠️ CRITICAL**: Complete before starting any user story phase.

- [X] T002 Extend `EncyclopediaSelection` and add `TierFilterKey` + `MobilityFilterKey` DUs in `src/FSBar.Hub/HubUiTypes.fsi` (add new DU cases + three new record fields: `TierFilter`, `MobilityFilter`, `SearchText`).
- [X] T003 Mirror the `HubUiTypes.fsi` changes in `src/FSBar.Hub/HubUiTypes.fs` and supply an `EncyclopediaSelection.defaults` factory value.
- [X] T004 Add `.fsi` for the pure filter module at `src/FSBar.Hub/EncyclopediaFilter.fsi` per `contracts/encyclopedia-filter.md` (`matches`, `apply`, `humanName`, `toTierKey`, `toMobilityKey`, `defaultSelection`).
- [X] T005 Implement `src/FSBar.Hub/EncyclopediaFilter.fs` — pure predicate: AND-across / OR-within / empty=pass-all, case-insensitive search over `InternalName` + glyph label + `humanName`; wire `toTierKey` / `toMobilityKey` from `FSBar.Viz.UnitGlyph` DUs.
- [X] T006 Register `EncyclopediaFilter.fs(i)` in `src/FSBar.Hub/FSBar.Hub.fsproj` ordered after `HubUiTypes` and before `HubStateStore`.
- [X] T007 Extend `HubStateStore.setEncyclopedia` in `src/FSBar.Hub/HubStateStore.fs` to trim `SearchText` and reject lengths > 128 with `SubmitOutcome.Rejected "search text > 128 chars"`; preserve emission of `HubEvent.EncyclopediaSelectionChanged` with the sanitized snapshot.
- [X] T008 Update `HubState` initial seed in `src/FSBar.Hub.App/Program.fs` (or wherever `HubStateStore.create` is called) to use `EncyclopediaSelection.defaults` instead of the previous inline `{ FactionFilter = Set.empty; SelectedDefId = None }`.
- [X] T009 Regenerate the `FSBar.Hub` surface-area baseline (must run after T002–T008 land; parallelizable only with Phase 3+ tasks): `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests/FSBar.Hub.Tests.fsproj` and commit the changed files under `tests/FSBar.Hub.Tests/Baselines/`.

**Checkpoint**: `dotnet build FSBarV1.slnx` green with the extended state record; `EncyclopediaFilter` callable from FSI. User-story phases can now begin.

---

## Phase 3: User Story 1 — Narrow the list by core tags (Priority: P1) 🎯 MVP

**Goal**: Faction / Tier / Mobility chip row filters the list with AND-across, OR-within semantics; Clear-filters resets everything.

**Independent Test**: Launch Hub with `FSBAR_HUB_INITIAL_TAB=Units`, click Arm chip → list narrows; add T2 chip → further narrows; add Cor → Arm OR Cor T2; Clear filters → full list.

### Tests for User Story 1

- [X] T010 [P] [US1] Add `tests/FSBar.Hub.Tests/EncyclopediaFilterTests.fs` with pure-predicate xUnit cases covering: empty-selection passes everything; single faction chip; two-faction OR; faction+tier AND; empty-category-semantics = pass-all. Wire the new test file into `tests/FSBar.Hub.Tests/FSBar.Hub.Tests.fsproj`.

### Implementation for User Story 1

- [X] T011 [US1] Extend `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fsi` to document the new chip-row render contract (render still returns `Element list`; `handleMouse` routes all chip clicks through `HubStateStore.setEncyclopedia`).
- [X] T012 [US1] In `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` render a three-row chip bar above the list (Faction / Tier / Mobility) using the existing chip styling; active chips tint from `HubState.Encyclopedia.*Filter`.
- [X] T013 [US1] In `EncyclopediaTab.fs` replace the current all-entries list call with `EncyclopediaFilter.apply (HubStateStore.current store).Encyclopedia state.Entries` and cache the result per render frame.
- [X] T014 [US1] Add a "Clear filters" button in `EncyclopediaTab.fs` that writes `EncyclopediaFilter.defaultSelection` via `HubStateStore.setEncyclopedia` (preserving `SelectedDefId` reconciliation per FR-011).
- [X] T015 [US1] Implement the hit-testing in `EncyclopediaTab.fs` `handleMouse`: map each chip rect → toggle the corresponding entry in `FactionFilter` / `TierFilter` / `MobilityFilter` and submit the updated `EncyclopediaSelection`; reconcile `SelectedDefId` using the post-filter list before the `setEncyclopedia` call (FR-011).
- [X] T016 [US1] Render the "N of M units shown" count in the tab header area (FR-006) using the pre- and post-filter list lengths.

**Checkpoint**: Chip filtering demo works end-to-end. MVP complete.

---

## Phase 4: User Story 2 — Combine tag filters with free-text search (Priority: P2)

**Goal**: A search input narrows the already-filtered list by case-insensitive substring match on `InternalName` / label / display name.

**Independent Test**: Activate Air mobility, type "bomb" — only air bombers remain; clear the text — only the tag filter remains.

### Tests for User Story 2

- [X] T017 [P] [US2] Extend `tests/FSBar.Hub.Tests/EncyclopediaFilterTests.fs` with: Air + "bomb" intersection test; search alone (no tag filters); case-insensitivity; whitespace-trimmed search text.

### Implementation for User Story 2

- [X] T018 [US2] Add a text-input widget to `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` alongside the chip row; keystrokes route through `HubStateStore.setEncyclopedia` with the updated `SearchText` (relying on T007's trim + 128-char cap).
- [X] T019 [US2] Extend `EncyclopediaTab.fs` `handleInput` / key handling to support focus on the search field, backspace, and Escape-to-clear (clears only the search text, leaves chips alone).
- [X] T020 [US2] Add a `tests/FSBar.Hub.Tests/EncyclopediaStateStoreTests.fs` case verifying `HubStateStore.setEncyclopedia` rejects > 128-char `SearchText` and emits no event on rejection.

**Checkpoint**: Search + chip combinations match spec AS-1/AS-2.

---

## Phase 5: User Story 3 — Remember my filter state (Priority: P3)

**Goal**: Filter state survives tab switches within a single Hub process but resets on relaunch.

**Independent Test**: Set filters on Units tab, switch to Viewer and back — chips + search still active. Close and relaunch Hub — chips cleared, search empty.

### Tests for User Story 3

- [X] T021 [P] [US3] Add `tests/FSBar.Hub.Tests/EncyclopediaSessionPersistenceTests.fs`: after two `setActiveTab` round-trips, `HubStateStore.current` reports the same `EncyclopediaSelection`; a freshly `HubStateStore.create`d store returns `EncyclopediaFilter.defaultSelection`.

### Implementation for User Story 3

- [X] T022 [US3] Confirm in `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` that no per-tab `let mutable` mirror of the filter state remains (feature-041 state-routing convention); audit `EncyclopediaTabState` — only `Entries` + `ListScroll` should survive there.
- [X] T023 [US3] Audit `src/FSBar.Hub/HubSettings.fs(i)` to confirm no new `EncyclopediaSelection` field is added (FR-008 explicitly forbids disk persistence); add a short comment to `HubUiTypes.fsi` documenting session-scope lifetime if unclear.

**Checkpoint**: Tab-switch persistence verified; relaunch reset verified.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T024 [P] Regenerate surface-area baselines covering the new `EncyclopediaTab.fsi` surface (same project as T009): `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests/FSBar.Hub.Tests.fsproj` and commit the updated files under `tests/FSBar.Hub.Tests/Baselines/`.
- [X] T025 Render the empty-state panel in `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` when `EncyclopediaFilter.apply` returns `[]`: label "No units match the active filters" plus an inline "Clear filters" button (FR-007 / SC-004).
- [X] T026 [P] Add a CLAUDE.md entry under `## Hub state-store routing convention (feature 041)` (or a sibling section) summarizing the extended `EncyclopediaSelection` fields and pointing at `EncyclopediaFilter`.
- [X] T027 [P] Run `scripts/examples/` smoke by executing the `quickstart.md` FSI snippet manually (no new example script required) and record the unit-count output in the PR description.
- [X] T028 Run `dotnet test FSBarV1.slnx` from the repo root; confirm all unit tests pass and surface-area baselines are clean.
- [X] T028a [P] Add a perf sanity test in `tests/FSBar.Hub.Tests/EncyclopediaFilterTests.fs`: materialize `EncyclopediaData.buildFromBarData()` once, run `EncyclopediaFilter.apply` 1 000 iterations with a non-trivial selection, assert median < 1 ms (well inside a 16.6 ms frame budget, satisfies SC-002). Skip on `CI=true` if noisy.
- [X] T028b [P] Add an audit test in `tests/FSBar.Hub.Tests/EncyclopediaCoverageTests.fs`: iterate `EncyclopediaData.buildFromBarData()`; for each entry check whether `EncyclopediaFilter.toTierKey entry.Tier`, `toMobilityKey entry.Shape`, and the faction mapping are all classifiable; assert that the ratio of entries where *all three* fall into catch-all buckets is ≤ 5% (SC-003).
- [X] T029 Validate the full `quickstart.md` GUI walkthrough end-to-end on the dev machine.

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup) — no deps.
- Phase 2 (Foundational) — depends on Phase 1; **blocks every user story phase**.
- Phase 3 (US1) — depends on Phase 2.
- Phase 4 (US2) — depends on Phase 2 (chip UI from US1 makes US2 easier to verify but is not a hard code dep).
- Phase 5 (US3) — depends on Phase 2; orthogonal to US1/US2 code.
- Phase 6 (Polish) — depends on all desired user stories.

### Within Each Story

- Tests listed before implementation can be written first (optional TDD) or alongside.
- All chip-rendering edits inside `EncyclopediaTab.fs` are sequential (same file).
- The `EncyclopediaFilter.fs` predicate (T005) gates every rendering task.

### Parallel Opportunities

- T009 (baseline regen) parallelizable with any US1 start once T002–T008 land.
- T010, T017, T021 test authoring: all [P] against one another (distinct test files).
- T024, T026, T027 in Phase 6 are independent.

---

## Parallel Example: User Story 1

```bash
# After Phase 2 completes:
Task: "T010 [US1] Write EncyclopediaFilterTests.fs"   # test author
Task: "T011 [US1] Update EncyclopediaTab.fsi"          # signature file
# Sequential (same .fs file): T012 → T013 → T014 → T015 → T016
```

---

## Implementation Strategy

### MVP First

1. Phase 1 + Phase 2 → Foundation.
2. Phase 3 (US1) → demo tag-chip filtering. Ship.

### Incremental

1. Add Phase 4 (US2) → search combines with chips.
2. Add Phase 5 (US3) → session persistence audit (mostly verification; no net-new code if state-store routing already holds).
3. Phase 6 → baselines + CLAUDE.md + docs.

---

## Notes

- Everything runs on existing in-repo deps; no new NuGet package introduced.
- Every mutation of `EncyclopediaSelection` goes through `HubStateStore.setEncyclopedia` — no per-tab mutable mirror (feature 041 convention).
- Selection stickiness (FR-011) is a `EncyclopediaTab` responsibility, not a store responsibility; reconcile `SelectedDefId` in the same `setEncyclopedia` submit that changes the filter.
- `.fsi` + surface-area baseline updates are mandatory per Constitution §II.
