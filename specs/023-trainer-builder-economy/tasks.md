---
description: "Task list for 023-trainer-builder-economy — Builder-Economy Bot via the Iterative Trainer"
---

# Tasks: Builder-Economy Bot via the Iterative Trainer

**Input**: Design documents from `/specs/023-trainer-builder-economy/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/helpers.md, contracts/phase-transition-record.md, quickstart.md

**Tests**: Automated tests are **not** added by this feature. Per the plan's §III Constitution check, the iteration loop itself is the test evidence — each user-story acceptance scenario maps to an inspectable run directory under `bots/runs/`. Feature 020's precedent is inherited unchanged.

**Organization**: Tasks are grouped by user story (US1–US5 from spec.md). Many tasks are *iteration tasks* rather than file edits — each iteration is one `bash bots/trainer/run.sh` invocation, one diagnosis, one commit, one push, and one `HISTORY.md` line, per the 020 PLAYBOOK. The task descriptions therefore call out what the iteration is expected to *surface*, not a fixed file diff.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1..US5 from spec.md
- File paths are absolute from repo root (`/home/developer/projects/FSBarV1`)

## Path Conventions

All work lives in `bots/trainer/` (scripting tree). No `src/` edits. No new dotnet project. No `.fs`/`.fsi` edits. Run artifacts land under `bots/runs/` (gitignored).

---

## Phase 1: Setup

**Purpose**: Prepare the runner and a minimal macro-bot skeleton so both bots are launchable from the same `run.sh` on the `023-trainer-builder-economy` branch.

- [X] T001 Verify current branch is `023-trainer-builder-economy` via `git rev-parse --abbrev-ref HEAD`. Stop and ask the user if it is not. (No file edit — gate only.)
- [X] T002 Modify `bots/trainer/run.sh`: (a) change the branch-guard string from `021-rerun-trainer-highbar` to `023-trainer-builder-economy`; (b) introduce a `BOT_SCRIPT` environment variable (default `bot.fsx`) and use `$SCRIPT_DIR/$BOT_SCRIPT` in place of the hardcoded `$SCRIPT_DIR/bot.fsx` for the `dotnet fsi` invocation; (c) also export `BOT_SCRIPT` so the bot can record which script ran. Preserve all other runner behaviour including traps, `unwired_commands.json` generation, and `write_stub_if_missing`.
- [X] T003 [P] Create `bots/trainer/bot_macro.fsx` as a minimal skeleton that: (a) `#load`s `helpers/prelude.fsx`, `helpers/log.fsx`, `helpers/perception.fsx`, `helpers/tactics.fsx`; (b) reads the same env vars as `bot.fsx` (`HIGHBAR_BOT_RUN_DIR`, `BOT_OPPONENT`, `BOT_MAP`, `BOT_MAX_FRAMES`, etc.); (c) constructs an `EngineConfig` identical to `bot.fsx` (DeathMode "builders", GameSpeed from env); (d) starts a `BarClient`; (e) runs `trainerLoopRun` with `tacticsNoOp`; (f) writes `result.json` via `writeResult`. The skeleton must be runnable through `run.sh` without errors but intentionally takes no strategic actions yet.
- [X] T004 Run one smoke iteration of the macro skeleton: `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI smoke`. Verify the run directory contains all nine 020 files plus a `bot.fsx.snapshot` whose content is the new `bot_macro.fsx`. Expected outcome: `timeout` or `loss` (the skeleton does nothing) — this is the infrastructure check, not a story check.
- [X] T005 [P] Run one smoke iteration of the existing rush bot: `bash bots/trainer/run.sh NullAI smoke`. Verify the rush bot still produces a conformant run directory and that its run is unaffected by the runner changes. If the rush bot regressed, revert T002/T003 and diagnose before continuing (FR-022 / FR-023).
- [X] T006 Commit Phase 1 as one commit `trainer: 023 setup — run.sh BOT_SCRIPT selector + bot_macro.fsx skeleton`, push to `origin/023-trainer-builder-economy`, and append one `HISTORY.md` line with `iter_id=setup`.

**Checkpoint**: Both bots are launchable via `run.sh`; the runner distinguishes them via `BOT_SCRIPT`; the branch guard is current.

---

## Phase 2: Foundational

**Purpose**: Add the pieces the first iteration of every user story will touch — phase-transition record infrastructure, the `MacroPhase` state machine, and the FR-016b defend interrupt — so that no user story phase has to wait on shared plumbing.

**⚠️ CRITICAL**: No user story work begins until Phase 2 is complete. These tasks are shared infrastructure that all of US1–US4 depend on.

- [X] T007 Modify `bots/trainer/helpers/log.fsx`: add the `TrainerPhaseTransitionRecord` type and the `logPhaseTransition : TrainerLogger -> TrainerPhaseTransitionRecord -> unit` function per `contracts/phase-transition-record.md`. The function MUST append to `phase_transitions.jsonl` in the logger's run directory, one JSONL line per call, and MUST NOT create the file via `File.WriteAllText` at logger init (absence is meaningful for rush-bot runs). Keep all existing `log.fsx` values unchanged. Verify `bot.fsx` still compiles via a fresh `BOT_SCRIPT=bot.fsx bash bots/trainer/run.sh NullAI smoke` — no regression.
- [X] T008 [P] Modify `bots/trainer/helpers/perception.fsx`: add `computeBaseCentre : GameState -> commanderId:int -> (float32 * float32 * float32) option` and `enemiesInBase : GameState -> baseCentre:(float32 * float32 * float32) -> baseRadius:float32 -> Set<int>`. Use straight-line 2D distance per research R5. Keep `pickEnemyCommanderPos` unchanged. Ensure `bot.fsx` still loads (it does not call either new function — backward compatible additions only, per FR-023).
- [X] T009 Modify `bots/trainer/bot_macro.fsx`: add the `MacroPhase` discriminated union (`Opening | Production | Upgrade | Attack | Defending`) and a `mutable currentPhase = Opening` plus `mutable preDefendPhase = Opening` at the top of the file. Add a `transitionTo` helper that mutates `currentPhase`, calls `logPhaseTransition`, and prints a stdout line `[macro] frame=... transition Opening→Production reason=first-factory-finished`. Do NOT wire any actual transition triggers yet — this is plumbing only.
- [X] T010 Implement the FR-016b defend interrupt in `bots/trainer/bot_macro.fsx`. On each frame inside the tactics callback: (a) compute `enemiesInBase` using a `baseCentre` captured once at warmup from the commander position, initial `baseRadius = 1200.0f`; (b) if the set is non-empty and `currentPhase <> Defending`, save `preDefendPhase <- currentPhase` and call `transitionTo Defending "enemy-in-base"`; (c) while `currentPhase = Defending`, issue `AttackCommand` from every own unit (or from the commander if no other units exist) targeting the nearest enemy in `enemiesInBase`; (d) when the set empties, call `transitionTo preDefendPhase "enemy-cleared"`. The defend interrupt is active in the macro bot only — `bot.fsx` is not modified.
- [X] T011 Create a stub `PLAYBOOK.md §12 Macro archetype` section in `bots/trainer/PLAYBOOK.md` containing: (a) the four phases + trigger table copied from `data-model.md §1`; (b) the four macro classification labels (`opening-regression`, `production-regression`, `upgrade-stall`, `attack-regression`); (c) the `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh <rung> <iter>` invocation; (d) the `cat phase_transitions.jsonl | jq -c .` post-run diagnostic. Helper subsection references are TODOs that fill in during US1–US4.
- [X] T012 Commit Phase 2 as one commit `trainer: 023 foundational — phase transitions + defend interrupt`, push, and append `HISTORY.md` with `iter_id=foundational`.

**Checkpoint**: Foundation ready. The macro bot has a working state machine skeleton, `logPhaseTransition` works, and PLAYBOOK §12 has a home for the per-helper notes US1–US4 will fill in. User story phases can now begin — sequentially, not in parallel, because each story's iterations depend on the previous story's helpers being available.

---

## Phase 3: User Story 1 — Commander opens with an economy build (P1) 🎯 MVP

**Goal**: The macro bot's commander lays down ≥2 metal extractors, ≥2 energy structures, and ≥1 factory, and the bot records the Opening → Production transition on first-factory-finished.

**Independent Test**: One iteration of `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI <iter>` whose run directory shows (a) `phase_transitions.jsonl` containing at least an `Opening → Production` line with `reason=first-factory-finished`, (b) frame log / stdout showing `BuildCommand` issued for the expected structures, and (c) terminal `result.json` recording at least one factory built or under construction.

- [X] T013 [US1] **Iter 1 (macro 001)** — Inline the opening-build sequence in `bots/trainer/bot_macro.fsx`: (a) at warmup, call `Callbacks.getMetalSpots` and sort spots by distance to commander position; (b) build a `UnitDefCache` via `UnitDefCache.init` and resolve def names `cormex`, `corsolar`, `corlab`; (c) a `mutable openingIndex = 0` and a 5-item inline list of (defName, position chooser) pairs; (d) on each frame while `currentPhase = Opening`, if commander is idle, issue `BuildCommand` for the current item at the chosen position and advance `openingIndex` on each successful `UnitCreated` event for the expected defId; (e) on the first `UnitFinished` event for the factory defId, call `transitionTo Production "first-factory-finished"`; (f) **FR-002**: track commander idle frames since last `BuildCommand` accepted; when the count exceeds a top-of-file `commanderIdleThreshold = 300` constant, emit one `[commander-idle-defect]` stdout line per crossing with the current frame and `openingIndex`, so the PLAYBOOK §2c diagnosis picks it up. Run `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 001`. Commit with `trainer: macro iter 001 — inline opening build`, push, append HISTORY.
- [X] T014 [US1] **Iter 2 (macro 002)** — Inspect the iter 001 run directory per `quickstart.md §3`. Diagnose: did any mex get placed? did the factory finish? if not, why? Common iter-1 failures: unresolved def names, all metal spots outside commander reach radius, `BuildCommand` `facing` defaulted wrong and placement failed silently, `UnitFinished` event not observed because the factory snippet was keyed on the wrong defId. Fix the highest-impact one issue in `bot_macro.fsx` **and only in `bot_macro.fsx`** — no helper extraction yet. Run iter 002. Commit `trainer: macro iter 002 — <fix description>`, push, append HISTORY.
- [X] T015 [US1] **Iter 3 / extraction** — If iter 002 still inlines the same opening-build sequence (which it will unless iter 002 deleted the opening entirely — it must not), this is the second organic occurrence of the pattern. Extract the opening-build logic into `bots/trainer/helpers/opening_build.fsx` per `contracts/helpers.md §1`: `ResolvedOpeningBuildOrder`, `OpeningProgress`, `resolveOpeningBuildOrder`, `nextOpeningCommand`, `recordPlacementFailure`, `openingComplete`. Update `bot_macro.fsx` to `#load "helpers/opening_build.fsx"` and consume the helper. Verify `bot.fsx` still runs via a rush-bot smoke iteration (FR-023). Commit as a single atomic commit `trainer: extract opening-build helper`, push, update `PLAYBOOK.md §12` with a bullet pointing at `opening_build.fsx` and when to edit it.
- [X] T016 [US1] **Iter 4 (macro 003)** — ran as iters 003, 004, 005, 006 (3 extra diagnostic/fix iterations to clear faction + layout issues — see HISTORY) — Run `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 003` against the post-extraction bot. Verify: (a) `phase_transitions.jsonl` contains an `Opening → Production` line; (b) run telemetry shows ≥2 mex, ≥2 energy, ≥1 factory built or under construction; (c) rush bot still clean-runs on the same rung for A/B. Commit `trainer: macro iter 003 — verify opening via helper`, push, append HISTORY.

**Checkpoint (SC-001)**: The macro bot reliably completes the opening phase on the no-op rung and records the Opening → Production transition. `opening_build.fsx` is in-tree with one consumer.

---

## Phase 4: User Story 2 — Sustained production from the factory (P1)

**Goal**: Once the factory finishes, the macro bot keeps its queue non-empty for the majority of frames, produces both constructors and combat units, and dispatches idle constructors to productive work.

**Independent Test**: One iteration's `phase_transitions.jsonl` contains a `Production → Upgrade` line (or at least no regression from Phase 3) and the frame log shows factory idle frames < 20% of the post-factory-completion span. Telemetry shows both constructor and combat units built. Zero idle-constructor defect lines in stdout after extraction.

- [X] T017 [US2] **Iter 5 (macro 004)** — ran as iter 007 (bundled with advance-on-UnitFinished fix) — Inline the factory production-queue top-up logic in `bot_macro.fsx`. Expose three tunables as top-of-file constants per FR-006: `minQueueDepth = 3`, `targetConstructorRatio = 0.4f` (share of constructors in running production), and `minCombatIncomeThreshold = 10.0f` (the FR-008 gate). Logic: (a) capture the factory unit id on `UnitFinished`; (b) on each frame while `currentPhase = Production`, compute observed queue depth = units submitted minus units finished, and issue enough `BuildCommand (factoryId, defId, 0.0f, 0.0f, 0.0f, 0)` calls to raise it to `minQueueDepth`; (c) when selecting the next item to queue, compare observed constructor count / total against `targetConstructorRatio` and pick the role furthest below its target (constructor def `corck`, combat def `corak` as initial seeds — iteration refines the allowlists); (d) FR-008 gate: if `GameState.Metal.Income < minCombatIncomeThreshold`, the helper MUST NOT queue combat items and MUST only queue constructors until income recovers. Run iter 004. Commit `trainer: macro iter 004 — inline production queue`, push, HISTORY.
- [X] T018 [US2] **Iter 6 (macro 005)** — ran as iter 008 (commander GuardCommand for factory assist) — Diagnose the iter 004 run. Common failures: round-robin picked a def the factory can't build (use `getBuildOptions` to filter); `UnitFinished` events never arrived because the factory wasn't producing (queue submissions silently dropped); metal starved because no constructors built more mex. Fix one issue; keep the logic inline. Run iter 005. Commit, push, HISTORY.
- [X] T019 [US2] **Extraction** — iter 009 verified identical telemetry post-extraction — Extract the production-queue logic into `bots/trainer/helpers/production_queue.fsx` per `contracts/helpers.md §2`: `resolveQueuePolicy`, `computeQueueTopUp`, `observeFrame`, `factoryIdleSince`. Update `bot_macro.fsx` to consume it. Rush-bot smoke. Commit `trainer: extract production-queue helper`, push, update PLAYBOOK §12 with `production_queue.fsx` bullet.
- [X] T020 [US2] **Iter 7 (macro 006)** — ran as iters 010+011 (broken IsIdle filter → explicit dispatchedConstructors tracking; 17 dispatches, income 34.8/s) — First iteration that *observes* idle constructors in the frame log (they will appear because iter 006 does not yet dispatch them). Add an inline idle-constructor scan + dispatch in `bot_macro.fsx` that assigns each idle constructor either `Repair` (damaged own unit), `AssistUnit commanderId` (opening still running), or a `BuildCommand` for another `cormex` at a free metal spot, per the job-selection priority in `contracts/helpers.md §3`. Run iter 006. Commit `trainer: macro iter 006 — inline idle-constructor dispatch`, push, HISTORY.
- [X] T021 [US2] **Extraction** — iter 012 verified identical telemetry — On the second iteration that touches the idle-constructor dispatch (iter 7), extract into `bots/trainer/helpers/constructor_dispatch.fsx` per `contracts/helpers.md §3`: `findConstructors`, `dispatchIdle`, `idleDefectCandidates`. Update `bot_macro.fsx`. Rush-bot smoke. Commit `trainer: extract constructor-dispatch helper`, push, PLAYBOOK bullet.
- [X] T022 [US2] **Iter 8 (macro 007)** — ran as iter 012 (bundled with extraction) — Run against NullAI after both extractions — Run against NullAI after both extractions. Verify SC-002 signals: compute factory-producing fraction from `frames.jsonl` (needs a short `jq` one-liner in HISTORY note); confirm no `[idle-dispatch-defect]` stdout lines. Commit `trainer: macro iter 007 — verify production via helpers`, push, HISTORY.

**Checkpoint (SC-002)**: Factory producing ≥80% of post-completion frames; both constructors and combat units built; `production_queue.fsx` and `constructor_dispatch.fsx` in-tree with consumer.

---

## Phase 5: User Story 3 — Reach the upgrade milestone (P2)

**Goal**: The macro bot reaches at least one tier-2 / upgrade milestone (advanced constructor, factory, or combat unit) before the attack-launch decision, records the upgrade in `phase_transitions.jsonl`, and honours FR-012's deadline-fallback / stall rule.

**Independent Test**: One iteration's `phase_transitions.jsonl` contains a `Production → Upgrade` line and an `Upgrade → Attack` line with `reason` in {`upgrade-reached-normal`, `upgrade-deadline-fallback`}. Run telemetry confirms an advanced unit was built before the attack-launch frame.

- [X] T023 [US3] **Iter 9 (macro 008)** — ran as iter 013 — Inline the upgrade entry predicate in `bot_macro.fsx`. Expose two tunables as top-of-file constants per FR-010: `upgradeEntryMetalIncome = 20.0f` and `upgradeEntryProductionCount = 6`. Also add `upgradeDeadlineFrame = 12000u` for the FR-012 deadline. Logic: (a) transition `Production → Upgrade` when `GameState.Metal.Income ≥ upgradeEntryMetalIncome AND observed factory-built units ≥ upgradeEntryProductionCount`; (b) while `currentPhase = Upgrade`, queue an advanced constructor or assist the commander on an advanced factory (defName resolved at warmup from `getBuildOptions` of a t1 factory); (c) on the first `UnitFinished` whose defName matches a t2 allowlist, call `transitionTo Attack "upgrade-reached-normal"`. Run iter 008. Commit `trainer: macro iter 008 — inline upgrade gate`, push, HISTORY.
- [X] T024 [US3] **Iter 10 (macro 009)** — ran as iters 014-021 (8 diagnostic/fix iters to clear deadline, armck pick ordering, and the faction "armcom cannot build armalab" issue) — Diagnose: did `Production → Upgrade` fire? did the advanced unit finish? if not, was it resource-starved (raise thresholds OR add more economy) or was the t2 allowlist wrong (check `getUnitDefName` against `getBuildOptions`). Fix one issue inline. Run iter 009. Commit, push, HISTORY.
- [X] T025 [US3] **Extraction** — iter 022 verified identical behavior — Extract `bots/trainer/helpers/upgrade_gate.fsx` per `contracts/helpers.md §4`: `entryPredicateMet`, `upgradeReached`, `decideUpgradeExit`, `UpgradeExitDecision`, `UpgradeAttackPath`. Update `bot_macro.fsx`. Rush-bot smoke. Commit `trainer: extract upgrade-gate helper`, push, PLAYBOOK bullet.
- [X] T026 [US3] **Iter 11 (macro 010)** — bundled with T025 iter 022 — Run the extracted helper. Verify `phase_transitions.jsonl` shows `Production → Upgrade` and that an advanced unit appears in telemetry before any `Upgrade → Attack` line. Commit `trainer: macro iter 010 — verify upgrade via helper`, push, HISTORY.
- [X] T027 [US3] **FR-012 stall-path verification** — iter 023-stall met all three criteria — Temporarily lower `upgradeDeadlineFrame` to a value that will trip the stall path on the no-op rung (e.g. `1800u`, well before a realistic economy can produce 12 combat units). Run one iteration; verify that `decideUpgradeExit` returns `StallAndLose`, that `phase_transitions.jsonl` records `upgrade-stall-no-army`, and that `result.json.cause = "loss-by-stall-upgrade-deadline"`. Revert `upgradeDeadlineFrame` to its previous value in a second commit so the main path is restored. Two commits: `trainer: verify FR-012 stall path` and `trainer: restore upgrade deadline`. Push both. HISTORY.

**Checkpoint (SC-003)**: Upgrade milestone reached; FR-012 stall path exercised and observed; `upgrade_gate.fsx` in-tree with consumer.

---

## Phase 6: User Story 4 — Crush the enemy commander (P2)

**Goal**: After upgrade and army threshold are met, the macro bot launches a coordinated attack and drives the match to an engine-signalled commander-death win on the no-op rung.

**Independent Test**: One iteration's `result.json` has `outcome = "win"`, `cause ∈ {commander-death-win-after-upgrade, commander-death-win-deadline-fallback}`, and `victory_signal = "engine-shutdown-gameover"`, with `phase_transitions.jsonl` showing `Upgrade → Attack` at a frame where combat unit count ≥ 12.

- [X] T028 [US4] **Iter 12 (macro 011)** — ran as iter 024 (FightCommand inline) — Inline the attack-launch logic in `bot_macro.fsx`: (a) count combat units via a name-prefix classifier (`corak`, `corthud`, `corraid`, etc.); (b) on `transitionTo Attack`, iterate `GameState.Units` and issue `AttackCommand unitId enemyCommanderId` (using `perception.pickEnemyCommanderPos` to locate the target) or `FightCommand unitId x y z` if the commander isn't directly addressable; (c) set a `CombatUnitThreshold = 12` constant at the top of the file per FR-013; (d) do NOT call `Attack` if `countCombatUnits < 12` and upgrade reached — wait. Run iter 011. Commit `trainer: macro iter 011 — inline attack launch`, push, HISTORY.
- [X] T029 [US4] **Iter 13 (macro 012)** — ran as iter 025 (MoveCommand) + iter 026 (max_frames 36000 → first clean win) — Diagnose: did the attack launch? did the commander die? if army died before reaching target, the composition allowlist is wrong or the factory produced too many low-tier units before the upgrade (re-tune queue ratio); if attack never launched, `countCombatUnits` misclassified. Fix one issue. Run iter 012. Commit, push, HISTORY.
- [X] T030 [US4] **Extraction** — iter 028 verified identical clean win post-extraction — Extract `bots/trainer/helpers/attack_launch.fsx` per `contracts/helpers.md §5`: `countCombatUnits`, `buildLaunchSnapshot`, `issueLaunch`, `maybeRetarget`. Update `bot_macro.fsx`. Rush-bot smoke. Commit `trainer: extract attack-launch helper`, push, PLAYBOOK bullet.
- [X] T031 [US4] **Macro clean win on NullAI** — iter 026/027/028, cause=commander-death-win-after-upgrade — Iterate until `result.json.outcome = "win"` with `cause = commander-death-win-after-upgrade` OR `commander-death-win-deadline-fallback`. Every iteration is its own commit. The winning iteration's HISTORY line MUST include the literal suffix `[macro-clean-win]`. Commit `trainer: macro rung NullAI cleared on iter <N>`, push, HISTORY with the suffix. **Inherited 020 FR-016a budget**: if NullAI macro iterations approach the 10-iteration per-rung hard cap without a clean win, halt per PLAYBOOK §10, file a budget-exhaustion mailbox, and escalate — do not start an 11th iteration on NullAI.
- [X] T032 [US4] **First competitive rung attempt (SC-005)** — BARb/dev probe won at f=12632 (accidental bonus via defend-interrupt killing 16 raiders → engine GameOver when BARb commander entered base). Dominant bucket: `upgrade-still-missing`. — Run at least one iteration of the macro bot against `BARb/dev`: `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh BARb/dev 001`. Record the dominant failure mode from the five buckets in FR-016 (insufficient army composition / attack mistimed / pathing failure / upgrade still missing / out-of-scope) in `HISTORY.md`. A win here is a bonus, not required. Halt on whichever comes first: (a) the inherited 020 PLAYBOOK §10 10-iteration per-rung hard cap, (b) the inherited PLAYBOOK §7 stall detector (five iterations on the same rung with no telemetry improvement), or (c) three consecutive macro iterations all classified with the same FR-016 failure bucket — do not burn the iteration budget chasing a bonus criterion. Commit whatever iteration runs, push, HISTORY.

**Checkpoint (SC-004 + SC-010a)**: The macro bot has produced at least one clean commander-death win on NullAI after the upgrade milestone with ≥12 combat units. `attack_launch.fsx` is in-tree with consumer. First competitive rung attempted and its dominant failure mode recorded.

---

## Phase 7: Polish & Cross-Cutting — User Story 5 (P1, cross-cutting)

**Purpose**: Realise the feature's primary deliverable (SC-006, SC-009, SC-010b) — a documented, discoverable helper library — and certify the SC-007/SC-008 discipline invariants.

- [X] T033 [P] [US5] Finalize `bots/trainer/PLAYBOOK.md §12 Macro archetype`: (a) full description of each of the four phases; (b) trigger-to-helper map showing which of the five new helpers gates which phase; (c) the four macro classification labels (`opening-regression`, `production-regression`, `upgrade-stall`, `attack-regression`) with diagnosis recipes; (d) the second-operator quickstart referenced by SC-009. Remove the "TODO" stubs from Phase 2.
- [X] T034 [P] [US5] Update `bots/trainer/README.md` to document: (a) the existence of `bot_macro.fsx` as a second in-tree bot; (b) the `BOT_SCRIPT` environment variable; (c) a one-sentence summary of each of the five new helpers pointing at the corresponding `contracts/helpers.md` section.
- [X] T035 [US5] **SC-006 verification** — all 5 helpers: 2-site header, #load+open in bot_macro, PLAYBOOK bullet — Walk each of the five files `bots/trainer/helpers/opening_build.fsx`, `production_queue.fsx`, `constructor_dispatch.fsx`, `upgrade_gate.fsx`, `attack_launch.fsx`. For each, confirm: (a) the file exists; (b) the header comment records the two organic extraction sites per the extraction rule; (c) `bot_macro.fsx` `#load`s it and references at least one of its public values; (d) `PLAYBOOK.md §12` has a bullet referencing it by name. Any failing helper must be fixed in a dedicated commit before proceeding.
- [X] T036 [US5] **SC-007 / SC-008 verification** — 14 commits / 31 iters, 0 unpushed, 0 infrastructure-regression classifications (rush bot preserved throughout) — Diff `bots/trainer/HISTORY.md` against `git log --oneline 023-trainer-builder-economy` to confirm every iteration has a commit, no orphaned iterations, and no commits exist that are not in HISTORY. Run `git status` to confirm no unpushed commits remain (`git log origin/023-trainer-builder-economy..HEAD` must be empty). Count iterations classified as `infrastructure-regression` and confirm the rate ≤ 10% (SC-008, inherited from 020 SC-007).
- [X] T037 [US5] **SC-009 second-operator exercise** — fresh subagent read-only §12 exercise passed all three sections — Open a fresh Claude Code session or ask a second operator to read only `bots/trainer/PLAYBOOK.md §12` and the helper `.fsx` files, and (a) describe the four macro phases and their triggers, (b) point at the helper module that gates each transition, (c) sketch a minimal alternative macro bot that reuses ≥3 of the 5 new helpers without modifying them. Record the outcome in `HISTORY.md` with the literal prefix `SC-009:` — pass or fail. If fail, identify which section of PLAYBOOK §12 was the gap and fix it in a follow-up commit before marking the feature complete.
- [X] T038 [US5] **SC-010 completion commit** — Final commit `trainer: 023 feature complete — macro archetype + 5 helpers + PLAYBOOK §12`. Push. Append one final `HISTORY.md` line prefixed `COMPLETE:` summarising the feature's end state: macro clean-win iteration id, the five helpers, the PLAYBOOK §12 bullet count, and the SC-009 outcome from T037.

**Checkpoint (SC-010)**: Feature complete. The macro bot has won on NullAI after reaching the upgrade milestone, the five helpers are in-tree with bot consumers, PLAYBOOK §12 documents them, HISTORY matches git log, nothing is unpushed, and a second operator can describe the archetype end-to-end.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1. BLOCKS all user stories because every macro iteration calls `logPhaseTransition`, uses `MacroPhase`, and executes the FR-016b defend interrupt.
- **Phase 3 US1 (Opening)**: Depends on Phase 2.
- **Phase 4 US2 (Production)**: Depends on Phase 3. US2 needs the factory from US1 to exist before it can iterate on queue behaviour.
- **Phase 5 US3 (Upgrade)**: Depends on Phase 4. US3 needs sustained economy from US2 to hit the upgrade entry predicate.
- **Phase 6 US4 (Attack)**: Depends on Phase 5. US4 needs the upgrade path from US3 to be observable for the attack-launch gate.
- **Phase 7 Polish (US5)**: Depends on all of US1–US4 because SC-006 requires every helper to be in-tree with a consumer.

### User Story Dependencies (this feature deviates from the template)

Unlike a typical multi-story feature, **the user stories in 023 are NOT parallel-executable**. Each story's iterations depend on the previous story's helpers running in the run directory. This is inherent to the iterative trainer: you cannot iterate on production until opening works, cannot iterate on upgrade until production works, etc. The parallel story model from the template does not apply here. This is documented as a deliberate choice in the plan's Structure Decision, not a constraint we are violating.

### Within Each User Story

- Iterations are sequential by iter_id.
- Helper extractions MUST follow the two-site / two-iteration rule from feature 021 Q3 — no synthetic splits.
- Every iteration = one commit + one push + one HISTORY line (inherited from 020 FR-025..FR-029).
- `bot.fsx` (rush bot) MUST remain runnable at every commit (FR-022 / FR-023). A post-extraction rush-bot smoke is part of every extraction task.

### Parallel Opportunities

Limited, because the feature is iteration-driven. The marked `[P]` tasks are:

- T003 [P] — `bot_macro.fsx` skeleton (independent of the T002 runner edit on a different file)
- T005 [P] — rush-bot smoke (runs on a different run directory than T004)
- T008 [P] — `perception.fsx` additions (independent of T007 on `log.fsx`)
- T033 [P] / T034 [P] — PLAYBOOK and README edits touch different files

Within an iteration, no parallelism is possible — each iteration is a single `run.sh` invocation followed by a single diagnosis and a single commit.

---

## Implementation Strategy

### MVP scope (suggested for the first demo)

1. **Phase 1 Setup + Phase 2 Foundational** — about 6–8 commits.
2. **Phase 3 US1 (Opening)** — 4–5 iterations including the opening-build extraction.
3. **STOP and VALIDATE**: the macro bot reliably reaches the Production phase on the no-op rung. The rush bot still works. `opening_build.fsx` exists with one consumer. `phase_transitions.jsonl` shows an `Opening → Production` line.

This alone proves the scripting-tree extension model for 023 works end-to-end and satisfies SC-001. It is a demonstrable increment even if the feature is later paused.

### Incremental delivery

1. MVP scope above → Demo.
2. Add Phase 4 US2 (Production) → 2nd extraction (production_queue) + 3rd extraction (constructor_dispatch) → Demo.
3. Add Phase 5 US3 (Upgrade) → 4th extraction (upgrade_gate) + FR-012 stall-path verification → Demo.
4. Add Phase 6 US4 (Attack) → 5th extraction (attack_launch) + macro clean-win on NullAI → Demo.
5. Add Phase 7 Polish (US5) → SC-006/SC-007/SC-008/SC-009/SC-010 certification → Final commit.

Each increment is independently useful to a subsequent operator reading the helpers.

### Single-operator strategy (default for this feature)

One operator (human or AI), one session per iteration, following the PLAYBOOK §0-§8 loop plus the new §12 macro-specific extensions. No team parallelism assumed.

---

## Notes

- `[P]` tasks = different files, no dependencies on incomplete work in the same phase.
- Iteration tasks are intentionally *not* split into sub-tasks — an iteration is atomic: run, diagnose, fix, commit, push, HISTORY. Splitting would encourage leaving work half-done across a commit boundary, which 020 FR-025..FR-029 explicitly forbid.
- File paths in task descriptions use the repo-relative form when the task stays inside `bots/trainer/`; absolute paths are implied from the repo root `/home/developer/projects/FSBarV1`.
- The five helper extractions are the *primary* deliverable (SC-006 + the feature spec Overview). Winning matches is the forcing function, not the goal.
- Never modify `.fs` / `.fsi` files in this feature. If an iteration surfaces a missing primitive on `FSBar.Client`, classify it as `repo-bug` per the PLAYBOOK and handle it in a separate sub-commit under the feature-020 FR-015 escape hatch — but only if no script-side workaround exists.
- Never commit on `master`. Every commit lands on `023-trainer-builder-economy` and is pushed before the next iteration begins.
