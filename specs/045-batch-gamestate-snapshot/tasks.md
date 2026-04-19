---
description: "Task list for feature 045: Batched GameState snapshot + FSBAR_TEST_ENGINE alias"
---

# Tasks: Batched GameState snapshot + FSBAR_TEST_ENGINE alias

**Input**: Design documents from `/specs/045-batch-gamestate-snapshot/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/gamestate-snapshot.md, quickstart.md

**Tests**: REQUESTED — spec FR-012 + Constitution III mandate unit and live integration tests for proto roundtrip, snapshot→GameState mapping, radar-only `Health = None`, frozen-enemy retention, and hard-error on pre-0.1.5 proxy.

**Organization**: Tasks are grouped by user story so each can be implemented and tested independently. US1 (P1) is the MVP; US2 (P3) is an additive env-var alias.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no ordering dependencies)
- **[Story]**: US1 = batched snapshot; US2 = env-var alias
- All paths are repo-relative under `/home/developer/projects/FSBarV1/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No project-level scaffolding needed — existing solution + projects + nupkg feed are reused. Only proxy + proto-regen preflight.

- [X] T001 Confirm locally installed HighBarV2 proxy is `>= 0.1.5` (per quickstart.md §1); upgrade via `upstream-pack` skill if the sibling HighBarV2 checkout is on `032-batch-callback-rpcs` but FSBarV1 is still consuming an older `HighBar.Client` nupkg.
- [X] T002 [P] Verify current build+tests are green on branch `045-batch-gamestate-snapshot` before any edits: `dotnet build FSBarV1.slnx` and `dotnet test FSBarV1.slnx` (baseline so later surface-area diffs are attributable to this feature).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Proto contract + generated F# bindings + the new exception type must land before any client code, tests, or live code can reference the new surface. BLOCKS both user stories (US1 strictly; US2 is independent of these but grouped here for a single build-green checkpoint).

**⚠️ CRITICAL**: US1 cannot start until T005 lands.

- [X] T003 Edit `proto/highbar/callbacks.proto`: add `CALLBACK_GAME_GET_STATE = 15` to the `CallbackId` enum, add the five messages (`FriendlyUnit`, `LosEnemyUnit`, `RadarOnlyEnemyUnit`, `EconomyRecord`, `GameStateSnapshot`) per `specs/045-batch-gamestate-snapshot/data-model.md` §Wire-level, and add `GameStateSnapshot snapshot_value = 8;` to the `CallbackResult.value` oneof. Mirror HighBarV2 `specs/032-batch-callback-rpcs/contracts/` byte-for-byte (field numbers, types, names).
- [X] T004 Regenerate `src/FSBar.Proto/Generated/highbar/callbacks.proto.gen.fs` via the `proto-regen` skill. Commit the regenerated file verbatim.
- [X] T005 Add `exception ProxyVersionMismatchException of message: string * requiredVersion: string` in `src/FSBar.Client/Protocol.fs` (+ export in `src/FSBar.Client/Protocol.fsi`), alongside the existing `EngineDisconnectedException`.

**Checkpoint**: Solution builds with the new proto surface + new exception; no behavior change yet.

---

## Phase 3: User Story 1 — Single-RPC per-tick GameState refresh (Priority: P1) 🎯 MVP

**Goal**: Every `GameEvent.Update` services the refresh with exactly one `CALLBACK_GAME_GET_STATE` RPC; per-unit `refreshUnit` + per-enemy refresh + per-resource `refreshEconomy` code is deleted; pre-0.1.5 proxy fails fast.

**Independent Test**: Live scenario with 200 friendlies + 50 enemies via `cheat-spawn` against a 0.1.5+ proxy — one callback id-15 RPC per tick (wire counter), `GameState` matches engine ground truth within float tolerance, radar-only enemies carry `Health = None`, enemies absent from snapshot retain frozen position. Running against a pre-0.1.5 proxy raises `ProxyVersionMismatchException` on first update.

### Tests for User Story 1 (write first; MUST FAIL before implementation lands) ⚠️

- [X] T006 [P] [US1] Add proto roundtrip tests for `GameStateSnapshot` + all five new messages in `tests/FSBar.Client.Tests/CallbacksSnapshotTests.fs` (new file): encode → bytes → decode equivalence, including empty-list edge case and the `RadarOnlyEnemyUnit`-has-no-health structural check.
- [X] T007 [P] [US1] Add mapper unit tests in `tests/FSBar.Client.Tests/CallbacksSnapshotTests.fs` covering: (a) friendlies new/updated, (b) LOS enemy sets `InLOS=true` + `Health = Some _`, (c) radar-only sets `InRadar=true` + `Health = None` even when prior state had `Some _`, (d) enemy absent from snapshot retains prior `Position` with `InLOS=false`, `InRadar=false`, `Health=None`, (e) economy fully replaced, (f) snapshot failure leaves prior `GameState` untouched.
- [X] T008 [P] [US1] Add `tests/FSBar.LiveTests/GameStateSnapshotLiveTests.fs` (new file): spawns a small mixed army, asserts exactly one callback id-15 RPC per `GameEvent.Update` via a wire-level counter, verifies correctness against engine ground truth over ≥300 ticks (SC-002 / SC-003 / SC-004), and asserts `ProxyVersionMismatchException` is raised by `BarClient.connect` itself (preflight, T010a) on a forced pre-0.1.5 proxy path — not on the first `Update` (skip if no legacy binary available, per testing policy).

### Implementation for User Story 1

- [X] T009 [P] [US1] Add snapshot record types (`FriendlyUnitSnapshot`, `LosEnemySnapshot`, `RadarOnlyEnemySnapshot`, `EconomyRecordSnapshot`, `GameStateSnapshotResult`) to `src/FSBar.Client/Callbacks.fsi` and `src/FSBar.Client/Callbacks.fs` per `data-model.md` §Client-level.
- [X] T010 [US1] Implement `val getGameStateSnapshot: stream: NetworkStream -> GameStateSnapshotResult` in `src/FSBar.Client/Callbacks.fs` (+ signature in `.fsi`): build `CallbackRequest { callback_id = 15; params = [] }`, await response via existing Protocol replay-buffered round-trip, decode the `snapshot_value` oneof into the F# record, raise `ProxyVersionMismatchException` on `error_message` prefix `"Unknown callback id"`, raise the existing descriptive error type on `"Snapshot unit count exceeds HIGHBAR_SNAPSHOT_MAX_UNITS"` / other, raise `EngineDisconnectedException` on disconnect (unchanged).
- [X] T010a [US1] Add a connect-time preflight snapshot in `src/FSBar.Client/BarClient.fs` (per research R4): immediately after the first `Init` event is processed during `BarClient.connect`, issue exactly one `Callbacks.getGameStateSnapshot` call and discard the result. Any raised `ProxyVersionMismatchException` propagates out of `connect`, so a pre-0.1.5 proxy fails at warmup rather than on the first `GameEvent.Update` (FR-006 + SC-005).
- [X] T011 [US1] Rewrite `GameEvent.Update` branch in `src/FSBar.Client/GameState.fs` `processEvent`: call `Callbacks.getGameStateSnapshot`, then build new `Units'` / `Enemies'` / `Metal` / `Energy` exactly per `data-model.md` §Mapping. On snapshot failure (any raised exception other than `EngineDisconnectedException`) leave prior state unchanged and rethrow.
- [X] T012 [US1] Delete the now-unused legacy refresh code in `src/FSBar.Client/GameState.fs`: `refreshUnit`, per-enemy `Unit_getPos`/`Unit_getHealth` pair, and the eight `Economy_*` calls / `refreshEconomy` helper. Remove any resulting unused `open`s and private helpers. No public-shape change in `GameState.fsi`.
- [X] T013 [US1] Regenerate surface-area baselines: `SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx` — commit the updated `tests/FSBar.Client.Tests/Baselines/*.baseline` (new snapshot types + `getGameStateSnapshot` + `ProxyVersionMismatchException` appear; no unintended diffs elsewhere).
- [X] T014 [US1] Add `scripts/examples/23-gamestate-snapshot.fsx` (new FSI walkthrough) matching `quickstart.md` §5: `#load prelude`, `BarClient.connect`, one `Callbacks.getGameStateSnapshot`, pretty-print frame/friendlies/los/radar/economy counts.
- [X] T015 [US1] Run live suite: `./tests/run-all.sh` — `GameStateSnapshotLiveTests` passes against the 0.1.5+ proxy; record the measured per-tick wall-clock vs SC-001 (< 10 ms at 200 + 50) in the PR description (sanity ceiling only, not a regression gate).

**Checkpoint**: MVP complete. Single-RPC refresh is sole supported path; `git grep -n 'refreshUnit\|refreshEconomy\|Unit_getPos\|Unit_getHealth\|Economy_'` in `src/FSBar.Client/` returns only the new callback plumbing (or nothing). `FSBar.Client` surface-area diff is exactly the documented additions.

---

## Phase 4: User Story 2 — `FSBAR_TEST_ENGINE` env-var alias (Priority: P3)

**Goal**: `EngineDiscovery` prefers `FSBAR_TEST_ENGINE`, accepts `HIGHBAR_TEST_ENGINE` as legacy fallback, warns once on conflict; all existing call sites route through the shared helper.

**Independent Test**: With `FSBAR_TEST_ENGINE` alone set, `./tests/check-prerequisites.sh` resolves and the live suite runs. With `HIGHBAR_TEST_ENGINE` alone set, same behavior. With both set to different values, `FSBAR_TEST_ENGINE` wins and a single diagnostic warning is emitted naming both values.

### Tests for User Story 2 ⚠️

- [X] T016 [P] [US2] Extend `tests/FSBar.Client.Tests/EngineDiscoveryTests.fs` with three cases per FR-009: (a) only `FSBAR_TEST_ENGINE` set → used; (b) only `HIGHBAR_TEST_ENGINE` set → used unchanged; (c) both set to different values → `FSBAR_TEST_ENGINE` wins + warning emitted exactly once. Use env-var set/unset scoped to the test (xUnit `IDisposable` pattern already used in file).

### Implementation for User Story 2

- [X] T017 [US2] Add `val resolveOverrideEnvVar: unit -> {| Value: string option; Conflict: (string * string) option |}` (per `data-model.md` §Engine discovery) in `src/FSBar.Client/EngineDiscovery.fsi` + `.fs`. Update `resolveFromEnvVar` to route through it; emit conflict warning via the existing discovery diagnostic surface (same channel `resolveFromEnvVar` already uses); update `ResolutionSource.OverrideEnvVar` label to the winning variable name.
- [X] T018 [P] [US2] Update `tests/check-prerequisites.sh`: prefer `FSBAR_TEST_ENGINE`, fall back to `HIGHBAR_TEST_ENGINE`, emit the same single warning on conflict. Keep the rest of the script behavior intact.
- [X] T019 [P] [US2] Update docs: `CLAUDE.md` §Engine paths (list `FSBAR_TEST_ENGINE` as primary, `HIGHBAR_TEST_ENGINE` as legacy alias) and `tests/ENGINE-VERSION.md` (same). No content beyond the alias change.
- [X] T020 [US2] Regenerate `FSBar.Client` surface-area baselines (`SURFACE_AREA_UPDATE=1 dotnet test …/FSBar.Client.Tests`) to pick up `resolveOverrideEnvVar`.

**Checkpoint**: US2 complete and independent of US1 — can ship in the same PR or separately.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T021 [P] Search for stray `HIGHBAR_TEST_ENGINE` references outside the aliasing helper + legacy shim + docs (`git grep -n HIGHBAR_TEST_ENGINE`) — each remaining site MUST route through `EngineDiscovery.resolveOverrideEnvVar` per FR-010. Fix any that still read the env var directly.
- [X] T022 Run full `./tests/run-all.sh` end-to-end as the release gate; SC-002/SC-003/SC-004 are primarily validated by T008, this task confirms no regression in the broader live suite and re-verifies SC-005 hard-error on pre-0.1.5.
- [X] T023 Execute `specs/045-batch-gamestate-snapshot/quickstart.md` end-to-end as the final gate (sections 2–7); any step that fails is a release blocker.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup; T004 depends on T003; T005 independent of T003/T004 but lands in the same checkpoint. BLOCKS US1 (US2 is technically independent but grouped for a single build-green gate).
- **US1 (Phase 3)**: depends on Phase 2 complete. Within US1, tests T006–T008 precede implementation T009–T012; T013 after T009–T012; T014/T015 after T013.
- **US2 (Phase 4)**: depends only on Phase 1 (and a green tree). Can ship before or after US1.
- **Polish (Phase 5)**: depends on both US1 and US2 (for T021/T022) or US1 alone (for T023) if US2 is deferred.

### User Story Dependencies

- **US1**: depends on Phase 2 (proto + exception).
- **US2**: independent of US1 — touches only `EngineDiscovery`, `check-prerequisites.sh`, and docs.

### Within Each User Story

- Tests MUST be written and FAIL before implementation (per constitution III).
- Proto types (T009) before callback impl (T010) before GameState rewrite (T011) before legacy deletion (T012) before baseline regen (T013).
- Live example (T014) and live run (T015) after unit-test green.

### Parallel Opportunities

- T002 can run while T001 is being decided (read-only baseline check).
- T006, T007, T008 all new-file or independent and marked [P].
- T009 is [P] vs. T006/T007/T008 (different files from the tests).
- US2 tasks T016, T018, T019 are [P] across `EngineDiscoveryTests.fs`, the shell script, and the two doc files.

---

## Parallel Example: User Story 1 tests

```bash
# Launch all three US1 test files in parallel:
Task: "Proto roundtrip + structural tests in tests/FSBar.Client.Tests/CallbacksSnapshotTests.fs (new file)"
Task: "Mapper unit tests in tests/FSBar.Client.Tests/CallbacksSnapshotTests.fs (same file — merge before commit)"
Task: "Live integration test in tests/FSBar.LiveTests/GameStateSnapshotLiveTests.fs (new file)"
```

(T006 + T007 share one file — land them in one commit even if drafted in parallel; T008 is genuinely parallel.)

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 (T001–T002).
2. Phase 2 (T003–T005) — proto + exception land together.
3. Phase 3 (T006–T015) — tests first, implementation, delete legacy path, regenerate baselines, live-validate.
4. **STOP and VALIDATE**: `./tests/run-all.sh` green on a 0.1.5+ proxy; per-tick wall-clock recorded.
5. Ship MVP (US1 alone is the substantive upgrade).

### Incremental Delivery

1. MVP (US1) ships.
2. US2 (T016–T020) as a follow-up — purely additive, zero behavior change in default environments.
3. Polish (T021–T023) before merge to master.

### Parallel Team Strategy

- One developer on Phase 2 + US1 (hot path, same files).
- A second developer can take US2 in parallel as soon as Phase 1 is green — no shared files with US1.

---

## Notes

- [P] = different files, no ordering dependency.
- FR-002 + clarification 2026-04-19 mandate deletion of the legacy refresh path — T012 is not optional.
- Live hard-error test (T008 pre-0.1.5 case) may be skipped if no legacy proxy binary is archived locally (testing policy: skip rather than mark-pass-while-failing).
- `SURFACE_AREA_UPDATE=1` only runs when public surface actually changed (T013, T020).
- Commit after each logical group; the legacy-deletion commit (T012) should be separable from the new-path commit (T009–T011) for easy revert.
