# Tasks: Integrate HighBar Proxy Fixes and Re-run the Iterative Trainer Cycle

**Input**: Design documents from `/specs/021-rerun-trainer-highbar/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/result-record.delta.md, quickstart.md

**Tests**: One pure-F# unit test file is included for the NaN-aware stall comparison helper, per Constitution §III. The remaining behavioural verification is via live-engine smoke iterations and operator walk of the existing PLAYBOOK, consistent with feature 020.

**Organization**: Tasks are grouped by user story (US1–US4 from spec.md). Setup and Foundational phases gate all stories. Workarounds in US2 are removed as **separate commits** per FR-010.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US4)
- All paths are absolute or relative to repo root `/home/developer/projects/FSBarV1`

## Path Conventions

- In-repo trainer tree: `bots/trainer/`
- F# test project (only one in this repo): `tests/FSBar.Client.Tests/`
- HighBarV2 sibling checkout: `../HighBarV2/`
- Mailbox: `Mailbox/`
- 020 contracts (modified by this feature via additive relaxation): `specs/020-bot-iterative-trainer/contracts/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Pull and install the rebuilt HighBarV2 proxy, restart FSI so any cached DLL is dropped, and update the runner branch guard.

- [ ] T001 Verify the working tree is on `021-rerun-trainer-highbar`, clean (no unstaged changes), and ahead-of `master` only by the spec/plan/tasks files. Run `git status && git rev-parse --abbrev-ref HEAD` from `/home/developer/projects/FSBarV1`.
- [ ] T002 Rebuild and install the HighBarV2 proxy from the sibling checkout. Run `cd ../HighBarV2 && git pull && cmake --build build && cmake --install build && cd -`. Confirm `~/.local/state/Beyond All Reason/engine/recoil_*/AI/Skirmish/HighBarV2/0.1/libSkirmishAI.so` mtime matches the build by running `stat -c '%y %n' ~/.local/state/Beyond\ All\ Reason/engine/recoil_*/AI/Skirmish/HighBarV2/0.1/libSkirmishAI.so`.
- [ ] T003 Restart the FSI MCP server (or any local FSI session) so previously-loaded FSBar.Client DLLs are dropped, per the `CLAUDE.md` "DLL references are locked" guidance. If using the MCP server, invoke the `restart_fsi` tool. If running FSI standalone, exit and re-launch.
- [ ] T004 [P] Update `bots/trainer/run.sh` line ~46 — change the feature-branch guard from `020-bot-iterative-trainer` to `021-rerun-trainer-highbar` so the runner stops warning on this feature's branch. Commit as `chore(trainer): bump branch guard to 021-rerun-trainer-highbar` and push.

**Checkpoint**: Rebuild verified by file mtime, FSI cache dropped, runner branch guard updated. Ready to proceed to Foundational tasks.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land the contract relaxation and the post-match `rc=-2` grep that the user-story verification steps depend on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T005 [P] Apply the backwards-compatible relaxation from `specs/021-rerun-trainer-highbar/contracts/result-record.delta.md` Change 1 to `specs/020-bot-iterative-trainer/contracts/result.schema.json`. Edit `peak_metal` and `peak_energy` in the `telemetry` object: change `"type": "number"` to `"type": ["number", "null"]` and update each description to mention the FR-003 NaN sentinel. Verify the file is still valid JSON by running `jq . specs/020-bot-iterative-trainer/contracts/result.schema.json > /dev/null`. Commit as `feat(contracts): relax result.schema.json peak_metal/peak_energy to nullable per 021 FR-003` and push.
- [ ] T006 [P] Add the post-match `rc=-2` grep step to `bots/trainer/run.sh`. After the `wait $bot_pid` block (around line 180–185, before the engine session-log copy block) and before the result.json stub fallback, insert a step that:
  1. Greps `$run_dir/engine.infolog`, `$run_dir/engine.stdout`, and `$run_dir/engine.stderr` (whichever exist) for the substring `rc=-2`, extracts each line's `case=` value with `awk` or `sed`, counts occurrences, and writes a `$run_dir/unwired_commands.json` file conforming to the schema in `contracts/result-record.delta.md` Change 2.
  2. Always writes the file even when the count is zero — `{ "rc_minus_2_count": 0, "by_case": {} }`.
  3. Uses `jq -n` to construct the JSON for safety; never builds JSON by string concatenation.
  Commit as `feat(trainer): write unwired_commands.json post-match per FR-004` and push.

**Checkpoint**: Contract is relaxed, runner emits `unwired_commands.json` on every run. User story phases can now begin.

---

## Phase 3: User Story 1 — Integrate the proxy fixes and verify the wire surface (Priority: P1) 🎯 MVP

**Goal**: Confirm the rebuilt HighBarV2 proxy actually delivers the four canonical wire signals (real economy, Shutdown(GAME_OVER), quiet infolog, `rc=-2`) to the unmodified trainer.

**Independent Test**: Run one smoke iteration on the NullAI rung against the integrated proxy and confirm all five US1 smoke checks (quickstart §3) pass: real or null `peak_metal`/`peak_energy`, `[trainer] Shutdown received` line in `stdout.log`, dramatically smaller `engine.infolog` than feature 020's comparable run, and `unwired_commands.json` present with an integer count.

### Implementation for User Story 1

- [ ] T007 [US1] Run the integration smoke iteration: `bash bots/trainer/run.sh NullAI smoke-021` from repo root. Capture the resulting run directory path (newest entry under `bots/runs/`) for the verification tasks below.
- [ ] T008 [P] [US1] Verify the smoke iteration's `result.json` `telemetry.peak_metal` and `telemetry.peak_energy` are either non-zero numbers or `null` (NaN sentinel). Run `jq '.telemetry.peak_metal, .telemetry.peak_energy' <run_dir>/result.json`. A real engine match on Avalanche 3.4 with the trainer commander accumulating metal should produce non-zero values. Record the values in a scratch note for the outbound mailbox.
- [ ] T009 [P] [US1] Verify the smoke iteration's `stdout.log` contains the line `[trainer] Shutdown received at frame ...` (this is the unmodified `printf` from `tactics.fsx` line 162, fired only when the proxy delivers a Shutdown event through the AI protocol). Run `grep -n "Shutdown received" <run_dir>/stdout.log`. The presence of this line is the canonical FR-002 wire-level marker. **Note**: at this point the bot may still classify the win via the `botDeclaredVictory` shim — that is acceptable for US1; US2 removes the shim.
- [ ] T010 [P] [US1] Verify the smoke iteration's `engine.infolog` is materially smaller than the comparable feature 020 infolog. Pick a 020 NullAI iteration of similar frame budget (`ls -la bots/runs/*NullAI* | head`) and compare sizes with `wc -c <020_run>/engine.infolog <run_dir_021>/engine.infolog`. SC-004 expects ≥80% reduction; record the actual ratio for the outbound mailbox.
- [ ] T011 [P] [US1] Verify `<run_dir>/unwired_commands.json` exists and parses as a valid object with `rc_minus_2_count` integer and `by_case` object. Run `jq 'has("rc_minus_2_count") and has("by_case")' <run_dir>/unwired_commands.json` — must print `true`.
- [ ] T012 [US1] Append the smoke iteration to `bots/trainer/HISTORY.md` per the existing pipe-delimited format. Suggested note: `US1 smoke against integrated 029-fix-trainer-issues proxy: shutdown_seen=yes, peak_metal=<value>, infolog_ratio=<value>`. Commit + push as `chore(trainer): record US1 integration smoke in HISTORY`.

**Checkpoint**: US1 verified — the five wire-level markers from the integrated proxy are observable in a single live run directory. The MVP slice of the feature is complete.

---

## Phase 4: User Story 2 — Tear out the trainer-side workarounds (Priority: P1)

**Goal**: With the proxy now emitting canonical signals, delete the four workarounds (NaN-poisoning peak accumulators, `botDeclaredVictory` synthetic-victory shim, `"No active session"` exception sniffer, any `enum_move=42` constant) and re-verify with smoke iterations on both rungs.

**Independent Test**: After all removals are committed and pushed, `git grep` returns zero hits for the workaround identifiers in shipping helpers and runner code, and one fresh smoke iteration on **each** rung (NullAI and BARb/dev) ends in `outcome=win` with `victory_signal=engine-shutdown-gameover` derived purely from the canonical Shutdown event path.

### Implementation for User Story 2

- [ ] T013 [US2] Convert `peakMetal` and `peakEnergy` in `bots/trainer/helpers/tactics.fsx` (lines ~82–83 of `TrainerLoop.run`) from `mutable float` initialised to `0.0` to `mutable float option` initialised to `None`. Add a local helper at the top of the function: `let nanSafeUpdate (acc: float option) (v: float32) = if Single.IsNaN v then acc else match acc with | None -> Some (float v) | Some prev -> Some (max prev (float v))`. Replace the per-frame reads at lines ~167–168 with `peakMetal <- nanSafeUpdate peakMetal m.Current` and `peakEnergy <- nanSafeUpdate peakEnergy e.Current`. Update the `Telemetry` record assignment at lines ~244–245 to pass `peakMetal` and `peakEnergy` directly (the `Telemetry` record's PeakMetal/PeakEnergy fields will need to become `float option` too — see T014). Commit as `fix(trainer): NaN-safe peak economy accumulators per FR-003`.
- [ ] T014 [US2] Update the `Telemetry` record definition in `bots/trainer/helpers/tactics.fsx` (around lines 73–79 — find via `grep -n "type Telemetry" bots/trainer/helpers/tactics.fsx`) to change `PeakMetal: float` and `PeakEnergy: float` to `PeakMetal: float option` and `PeakEnergy: float option`. Commit as part of the same fix as T013 (folded into one commit).
- [ ] T015 [US2] Update the result writer in `bots/trainer/helpers/log.fsx` (around lines 176–177 — find via `grep -n "peak_metal" bots/trainer/helpers/log.fsx`) to serialize `Telemetry.PeakMetal` and `Telemetry.PeakEnergy` as either `WriteNumber` for `Some v` or `WriteNull` for `None`. Use `match` expressions; the Utf8JsonWriter API has `writer.WriteNull("peak_metal")` and `writer.WriteNumber("peak_metal", v)`. Commit as `fix(trainer): emit JSON null for unavailable peak economy fields per FR-003 + contracts delta` and push.
- [ ] T016 [US2] Remove the `botDeclaredVictory` shim from `bots/trainer/helpers/tactics.fsx`. Delete the `mutable botDeclaredVictory = false` line (~95), the `mutable botVictoryFrame = 0` line (~96), the `if tacticsResult.VictoryDeclared && not botDeclaredVictory then ...` block (~183–186), the `elif botDeclaredVictory && lastFrameNumber - botVictoryFrame >= 60 then ...` branch (~234–237) in the loop-exit conditions, and the synthetic-victory branch in the result-classification block (~260–276) which checks `if botDeclaredVictory && commanderAlive then`. The result classification then falls through to the existing `elif shutdownSeen && commanderAlive then` branch at line ~277, which is the canonical victory path. Also delete the `VictoryDeclared` field from the `TrainerTacticsFn` result record if it becomes unused (check with `grep -rn "VictoryDeclared" bots/trainer/`); if `bot.fsx` still produces it, leave it as a no-op field for now. Commit as `fix(trainer): remove botDeclaredVictory shim — Shutdown(GAME_OVER) is canonical per FR-006` and push.
- [ ] T017 [US2] Remove the `"No active session"` exception sniffer from `bots/trainer/helpers/tactics.fsx` (around lines 205–214). Replace the entire `if ex.Message.Contains "No active session" || client.State = Stopped then ... stepping <- false` branch with a simple re-raise: leave only the existing else-branch that increments `consecutiveExceptions` and classifies as `terminal-error` after 3 repeats. The canonical end-of-game now flows through `Shutdown reason -> shutdownSeen <- true` at line ~159. Commit as `fix(trainer): remove No-active-session exception sniffer per FR-007` and push.
- [ ] T018 [US2] Search for any `enum_move=42`, `enumMove=42`, or `Command_Move = 42` constant anywhere under `bots/trainer/` and `src/FSBar.Client/`. Run `git grep -nE 'enum_move\s*=\s*42|enumMove\s*=\s*42|Command_Move\s*=\s*42'`. If any hit appears in shipping code (not in feature 020 spec/research/data-model files), delete it and commit as `fix(trainer): remove enum_move=42 constant per FR-009`. If no hits exist in shipping code, write a one-line note `bots/trainer/.no-enum-move-42` containing `verified clean on 2026-04-12` for traceability and commit it; alternatively skip the commit and document the verification in the T026 outbound mailbox.
- [ ] T019 [US2] Verify `peak_metal: 0` / `peak_energy: 0` literals exist only in `write_stub_if_missing` and `write_interrupted_stub` paths in `bots/trainer/run.sh`, per research.md Decision 7. Run `git grep -nE 'peak_metal\s*[:=]\s*0|peak_energy\s*[:=]\s*0' bots/trainer/ src/FSBar.Client/`. The expected hits are exactly four (lines ~129, ~130, ~150, ~151 in `run.sh`). If any other hit is found, remove it and commit as `fix(trainer): remove real-path peak econ zero placeholder per FR-008`. Document the expected-hits-only result in the T026 outbound mailbox.
- [ ] T020 [US2] Run a post-removal smoke iteration on the NullAI rung: `bash bots/trainer/run.sh NullAI smoke-021-postclean`. Capture the run directory path.
- [ ] T021 [US2] Verify the post-removal NullAI smoke ends in `outcome=win` with `victory_signal=engine-shutdown-gameover`. Run `jq '{outcome, victory_signal}' <run_dir>/result.json`. Both fields must be present and exact. Also `grep -c "botDeclaredVictory\|No active session" <run_dir>/stdout.log` must return 0 (the deleted code paths are no longer producing log lines). Append the iteration to `HISTORY.md` with a note like `US2 post-clean NullAI smoke: canonical Shutdown path verified`.
- [ ] T022 [US2] Run a post-removal smoke iteration on the BARb/dev rung: `bash bots/trainer/run.sh BARb/dev smoke-021-postclean`. The BARb difficulty profile patch must be installed first — if not, run `bash bots/trainer/engine-patches/install-barb-profiles.sh` per the existing 020 procedure.
- [ ] T023 [US2] Verify the post-removal BARb/dev smoke ends in `outcome=win` with `victory_signal=engine-shutdown-gameover`. Same checks as T021. Append the iteration to `HISTORY.md` with a note like `US2 post-clean BARb/dev smoke: canonical Shutdown path verified`. Commit + push the HISTORY entries from T021 and T023 as `chore(trainer): record US2 post-clean smokes in HISTORY`.
- [ ] T024 [US2] Verify SC-003: run `git grep -nE 'botDeclaredVictory|No active session|enum_move\s*=\s*42'` from repo root. Expected: zero hits in `bots/trainer/helpers/`, `bots/trainer/bot.fsx`, `bots/trainer/run.sh`, and `src/FSBar.Client/`. Hits in `specs/020-*` documentation files are acceptable (historical record). If shipping code hits remain, return to T016/T017/T018.
- [ ] T025 [US2] Verify SC-004: compare `<US2_postclean_run_dir>/engine.infolog` size against the feature 020 NullAI infolog size from a comparable iteration (same rung, same `max_frames`). Run `wc -c <020_run>/engine.infolog <021_run>/engine.infolog` and compute the ratio. Expected: ≥80% size reduction. Record the ratio for the outbound mailbox.
- [ ] T026 [US2] File the integration outbound mailbox per FR-019 at `Mailbox/2026-04-XX_from_FSBarV1_integration_complete.md` (replace `XX` with the current day-of-month). Include: (a) reference to the inbound `Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md`, (b) the four removal commit hashes from T013–T018, (c) the smoke iteration run directories from T007 and T020/T022, (d) the SC-001 (peak econ values) and SC-004 (infolog reduction ratio) measurements from T008 and T025, (e) confirmation that the rebuilt proxy mtime from T002 matches the feature branch HEAD. Commit + push as `docs(mailbox): outbound integration-complete report to HighBarV2`.

**Checkpoint**: All four workarounds removed, both rungs end in canonical `win` via the Shutdown(GAME_OVER) path, outbound mailbox filed. SC-001/002/003/004 verified.

---

## Phase 5: User Story 3 — Re-run the iterative improvement cycle on the integrated proxy (Priority: P1)

**Goal**: Walk the existing PLAYBOOK loop on both rungs under the integrated proxy, with the NaN-aware stall rule and the 10-iteration per-rung budget enforced, producing at least one substantive helper extraction (SC-006) along the way.

**Independent Test**: At the end of the loop, `HISTORY.md` contains at least one `win` line per rung in this feature's session, every iteration line maps to a pushed commit on `021-rerun-trainer-highbar`, the `bots/trainer/helpers/perception.fsx` or `tactics.fsx` file contains substantive extracted code referenced from at least two distinct call sites in `bot.fsx`, and the `TrainerStallTests.fs` xUnit suite passes with all six enumerated cases green.

### Implementation for User Story 3

- [ ] T027 [P] [US3] Add a new compiled F# file `tests/FSBar.Client.Tests/TrainerStallHelper.fs` containing the `StallTelemetry` record and the `improvedOverPrior : prior:StallTelemetry -> current:StallTelemetry -> bool` function, exactly matching `data-model.md` §StallTelemetry: int fields are `int`, peak fields are `float option`, the function returns `true` on any improvement (including a `Some` after a prior `None`) and returns `false` only when every non-None field stagnated. The helper lives **inside the test project** (not under `src/`) so it does NOT enter `FSBar.Client.dll`'s public API surface — Tier 2 classification is preserved (no `.fsi` file, no surface-area baseline change). Add `<Compile Include="TrainerStallHelper.fs" />` to `tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` **before** `TrainerStallTests.fs` (F# top-down compile order). The trainer's runtime stall check at iteration time is operator-driven via PLAYBOOK §10 (added in T029) and does not need to call this helper from `tactics.fsx` — the helper exists primarily to give the test a real compiled target, and the operator may transcribe its logic into PLAYBOOK §10 prose. Commit as `feat(trainer): add NaN-aware stall comparison helper (test-project-scoped) per FR-015 + Decision 3`.
- [ ] T028a [P] [US3] Add `tests/FSBar.Client.Tests/TrainerStallTests.fs` with six `[<Fact>]` test methods, one per row of the table in `data-model.md` §`improvedOverPrior` Behaviour. The test file MUST consume the production helper directly via `open` (not via inline-redefinition), so a regression in the helper actually fails the test per Constitution §III. T027 lands the helper as a compiled `.fs` module in the same test project, which makes direct consumption straightforward.
- [ ] T028b [P] [US3] Edit `tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj` to add `<Compile Include="TrainerStallTests.fs" />` to the appropriate `<ItemGroup>` block. F# requires top-down compile order — place the entry **after** `TrainerStallHelper.fs` (added in T027) and **before** any `Program.fs` / `Main` entry if one exists. Run `dotnet test tests/FSBar.Client.Tests/` and confirm all six tests in `TrainerStallTests` pass and the existing test count is unchanged-or-greater (no regressions). Commit T028a + T028b together as `test(trainer): NaN-aware stall comparison cases per Constitution §III`.
- [ ] T029 [US3] Update `bots/trainer/PLAYBOOK.md`: (a) extend §3 (the diagnose/classify decision tree) with two new branches matching `data-model.md` §"State transitions" — one for `cross-repo HighBarV2 defect → halt + inbound mailbox`, one for `budget exhausted → halt + budget mailbox`; (b) add a new §10 documenting the FR-016a 10-iteration per-rung hard cap, including the budget-exhaustion mailbox file naming convention `Mailbox/<YYYY-MM-DD>_from_FSBarV1_budget_exhausted_<rung-slug>.md` where `<rung-slug>` is the rung name with `/` replaced by `-` and lowercased (e.g. `BARb/dev` → `barb-dev`, `NullAI` → `nullai`), and the format of its contents (rung name, 10 iter ids, outcomes, telemetry trend, hypothesis); (c) add a §11 documenting the FR-021 cross-repo defect rule with the inbound-mailbox file naming convention `Mailbox/<YYYY-MM-DD>_to_HighBarV2_<short-symptom-slug>.md` where `<short-symptom-slug>` follows the same `/`→`-` lowercase rule. Commit as `docs(trainer): PLAYBOOK §10 budget cap + §11 cross-repo defect routing per FR-016a + FR-021`.
- [ ] T030 [US3] Walk PLAYBOOK §1–§9 on the **NullAI rung** for this feature's session. Each iteration: run `bash bots/trainer/run.sh NullAI <iter_id>`, classify outcome per PLAYBOOK §3, fix/improve/extract as appropriate, commit + push per PLAYBOOK §6, append one line to `HISTORY.md` per PLAYBOOK §5. Continue until: (a) one `win` outcome with `victory_signal=engine-shutdown-gameover` is recorded for this feature's branch, OR (b) the FR-015 stall rule fires (5 consecutive iterations with no `improvedOverPrior` improvement on the rung), OR (c) the FR-016a 10-iteration budget is exhausted. The post-clean smoke from T020 may count as the first iteration if its outcome was `win`.
- [ ] T031 [US3] Walk PLAYBOOK §1–§9 on the **BARb/dev rung** for this feature's session, with the same termination conditions as T030. The post-clean smoke from T022 may count as the first iteration if its outcome was `win`.
- [ ] T032 [US3] During T030 and T031: when the operator notices an ad-hoc perception or tactics snippet appearing in `bot.fsx` for the **second time** across iterations, extract the snippet into the appropriate helper module (`bots/trainer/helpers/perception.fsx` or `bots/trainer/helpers/tactics.fsx`) following PLAYBOOK §7. The extraction commit must contain both the new helper content **and** `bot.fsx`'s switch to call the helper. After extraction, ensure the helper is referenced from **at least two distinct call sites** in `bot.fsx` (per SC-006 substance bar from clarification Q3). If the second organic call site has not materialised by the time the rung's `win` outcome is recorded, the operator MUST run **additional iterations on the cleared rung** (still under the FR-016a 10-iteration budget) specifically to surface a second call site, OR the operator MUST formally relax SC-006 in a new `/speckit.clarify` session **before** marking the feature complete. Synthetic call sites added solely to satisfy the count are forbidden by Q3 and do not count. Commit per PLAYBOOK §6. Repeat for as many extraction opportunities as arise.
- [ ] T032a [US3] FR-014 satisfaction gate: before the polish phase begins, verify that at least one helper extraction beyond `log.fsx` has landed on the feature branch with both the 2-iteration motivation (visible in HISTORY note + diff history) and the 2-call-site usage (verified per T036). If neither condition is met after T030 + T031 + any extra iterations from T032, halt and re-open clarification — the feature cannot complete with an unsatisfied FR-014.
- [ ] T033 [US3] If a rung hits the FR-016a 10-iteration cap without a win and stall has not fired, file a budget-exhaustion mailbox at `Mailbox/<YYYY-MM-DD>_from_FSBarV1_budget_exhausted_<rung-slug>.md` per PLAYBOOK §10 (added in T029) using the slug rule from T029 (`/`→`-`, lowercase). Halt iteration on that rung. Suffix the last `HISTORY.md` line for that rung with `[budget-exhausted]`. Do not start an 11th iteration without an explicit operator decision.
- [ ] T034 [US3] If a re-run iteration's failure root-causes to a HighBarV2 proxy defect, file an inbound mailbox to HighBarV2 at `Mailbox/<YYYY-MM-DD>_to_HighBarV2_<short-symptom-slug>.md` per PLAYBOOK §11 (added in T029) using the slug rule from T029. Halt the loop. Do not edit HighBarV2 source from inside this feature.
- [ ] T035 [US3] Verify SC-005: `git -C . log 021-rerun-trainer-highbar --grep='HISTORY' --oneline | wc -l` plus a manual scan of `HISTORY.md` for at least one `win | engine-shutdown-gameover` per rung in this feature's session, both within ≤10 iterations. Record the iteration counts in a scratch note for the polish phase.
- [ ] T036 [US3] Verify SC-006: confirm `bots/trainer/helpers/perception.fsx` or `bots/trainer/helpers/tactics.fsx` contains substantive extracted code (motivated by 2-iteration duplication, used from 2+ distinct call sites in `bot.fsx`). Run `wc -l bots/trainer/helpers/perception.fsx bots/trainer/helpers/tactics.fsx` to confirm growth from the 020 baseline; cross-check call sites with `git grep -c "<helper-function-name>" bots/trainer/bot.fsx`. The grep count must be ≥2.
- [ ] T037 [US3] Verify SC-007: every iteration line in `HISTORY.md` for this feature has a corresponding pushed commit on `021-rerun-trainer-highbar`. Run `git log 021-rerun-trainer-highbar --oneline | grep -c 'iter\|US[1-4]\|smoke'` and cross-reference against the new HISTORY lines added in this feature. There must be no orphaned local commits (`git status` clean, `git rev-list HEAD ^origin/021-rerun-trainer-highbar` empty).

**Checkpoint**: Iteration loop walked on both rungs, at least one helper extracted with the substance bar met, stall + budget rules tested in practice (or correctly skipped), every iteration committed and pushed.

---

## Phase 6: User Story 4 — Re-investigate non-Move command dispatch with a getUnitPos probe (Priority: P2)

**Goal**: Capture one `getUnitPos`-before-and-after probe around an `AttackCommand` send during a NullAI iteration, write the result into the run directory as `attack_probe.json`, and reference it from `HISTORY.md` or an outbound mailbox so the upstream HighBarV2 maintainer can act on it.

**Independent Test**: Exactly one run directory under `bots/runs/` for this feature contains a valid `attack_probe.json` conforming to the schema in `contracts/result-record.delta.md` Change 3, and the `HISTORY.md` line for that iteration carries an `Issue 1 probe: outcome=...` note.

### Implementation for User Story 4

- [ ] T038 [US4] Wire the AttackCommand probe into `bots/trainer/bot.fsx`'s tactics callback for **one** iteration on the NullAI rung. The probe must follow Decision 5 in `research.md`: capture the issuing unit's `Pos` immediately before `client.SendCommands [AttackCommand …]`, `client.WaitFrames 30` (one game-second) without sending further commands, capture `Pos` again (or note the unit is missing from `client.GameState.Units`), classify `moved` (Euclidean distance > 5.0 game units) / `stationary` / `destroyed`, and write `<run_dir>/attack_probe.json` per the contracts delta schema using `System.Text.Json.Utf8JsonWriter`. The run directory path is available to the bot via the `HIGHBAR_BOT_RUN_DIR` env var that `run.sh` sets at line 168.
- [ ] T039 [US4] Run the probe iteration: `bash bots/trainer/run.sh NullAI probe-021`. Verify `<run_dir>/attack_probe.json` was written and is schema-valid: `jq 'has("issuing_unit_id") and has("frame_at_send") and has("pos_before") and has("pos_after") and has("outcome")' <run_dir>/attack_probe.json` must print `true`. Confirm `outcome` is one of `moved`, `stationary`, `destroyed`.
- [ ] T040 [US4] Append the probe iteration to `HISTORY.md` with a note like `Issue 1 probe: outcome=moved (issuing unit moved from (x1,y1,z1) to (x2,y2,z2)) — see attack_probe.json`. This satisfies SC-008. Commit + push as `chore(trainer): record Issue 1 probe iteration in HISTORY`.
- [ ] T041 [US4] If two consecutive probe iterations (T039 plus a follow-up if needed) classify as `stationary`, file an outbound mailbox to HighBarV2 at `Mailbox/<YYYY-MM-DD>_to_HighBarV2_attack-command-stationary.md` (no slashes in the symptom slug, per the T029 slug rule) containing the probe JSONs, the iteration ids, and the relevant `frames.jsonl` excerpts around the `frame_at_send` and `frame_at_check` frames. This satisfies FR-018. If the first probe iteration classifies as `moved` or `destroyed`, the upstream Issue 1 follow-up is closed by the HISTORY note alone — no outbound mailbox needed.
- [ ] T042 [US4] Remove the probe instrumentation from `bot.fsx` after the probe iteration if it was a one-off addition that should not persist into production iterations. Alternatively, keep it gated behind an `HIGHBAR_PROBE_ATTACK=1` env var and document the toggle in `bot.fsx` comments. Commit either the removal or the env-var gating as `chore(trainer): retire one-off Issue 1 probe instrumentation`.

**Checkpoint**: Issue 1 probe captured, classified, and reported. The FSBarV1-side commitment in the inbound mailbox's Action Item 4 is closed.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final SC verification, README/documentation touch-ups, and the outbound integration mailbox if it was deferred.

- [ ] T043 [P] Walk the SC-001 through SC-010 checklist from `quickstart.md` Step 10 end-to-end. Mark each checkbox in a scratch document or directly in this `tasks.md` (append a `## Final SC Verification` section). For any unchecked SC, return to the corresponding US phase. SC-009 specifically: confirm `Mailbox/2026-04-XX_from_FSBarV1_integration_complete.md` from T026 is dated within the same calendar week as the feature's completion commit.
- [ ] T044 [P] Update `bots/trainer/README.md` with a one-paragraph note at the bottom referencing this feature's outbound mailbox (T026) and the integrated HighBarV2 commit range. Do not rewrite the README — the existing operator-facing intro from feature 020 is still correct. Commit as `docs(trainer): note 021 integration in README` and push.
- [ ] T045 [P] Run the F# test project once more from a clean state to verify nothing regressed: `dotnet test tests/FSBar.Client.Tests/`. The new `TrainerStallTests.fs` from T028 must pass with six green tests, and the existing 020 tests must still pass.
- [ ] T046 Run `git status` and `git log 021-rerun-trainer-highbar..HEAD --oneline` to confirm the working tree is clean and every iteration's commits are on the remote. Run `git push origin 021-rerun-trainer-highbar` one final time to be safe (it should be a no-op if every iteration pushed correctly, per FR-027).
- [ ] T047 Final SC-010 audit: walk every iteration line in `HISTORY.md` for this feature in chronological order. Confirm no `win` outcome was recorded *before* the corresponding workaround removal commit landed (T013–T018). The post-clean smokes from T020/T022 are the earliest legal `win` outcomes; any earlier `win` must be re-iterated under the post-clean code or struck from the success criteria. Document the audit in the outbound mailbox or in a final HISTORY note.

**Feature complete** when T043 shows all 10 SCs checked and T046/T047 are clean.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 → T002 → T003 (sequential — can't restart FSI before rebuild lands). T004 [P] independent of T001/T002/T003.
- **Foundational (Phase 2)**: requires Setup (specifically T002 — the rebuilt proxy must exist before any smoke iteration). T005 [P] and T006 [P] independent of each other.
- **US1 (Phase 3)**: requires Foundational. T007 must precede T008–T012 (they verify T007's run directory).
- **US2 (Phase 4)**: requires US1 verification (T008–T011). T013–T015 (NaN handling) are one logical commit; T016–T018 (workaround removals) are independent commits in any order. T020–T023 (post-removal smokes) require T013–T018 to be complete. T024–T026 require all removals + smokes.
- **US3 (Phase 5)**: requires US2 (the loop runs against the cleaned trainer). T027, T028a, T028b are independent of T029 (different files). T030 and T031 require T027 + T028a + T028b + T029 (the compiled helper, its tests, the .fsproj wiring, and the playbook update). T032 happens organically inside T030/T031. T032a runs at the end of US3, after T030 + T031. T035–T037 require T030 + T031 to terminate.
- **US4 (Phase 6)**: requires US3 (the probe runs against an iterating bot). Can interleave with T030 — the probe iteration is one of the iterations in the loop.
- **Polish (Phase 7)**: requires every prior phase. T043, T044, T045 [P] independent of each other.

### User Story Dependencies

- **US1 (P1)**: depends only on Phase 1 + Phase 2. The MVP slice — completing US1 alone delivers a verified integrated proxy.
- **US2 (P1)**: depends on US1 (verification path). Removes workarounds the proxy made obsolete.
- **US3 (P1)**: depends on US2 (loop runs against the cleaned trainer; without removals, every iteration would carry hidden ambiguity per spec rationale).
- **US4 (P2)**: depends on US3 (probe runs as one iteration inside the loop). Could in principle run as a standalone iteration after US2, but practically interleaves with US3.

### Within Each User Story

- Tests (T028a + T028b) MUST be written, wired into the .fsproj, and passing before the helper they cover (T027) is relied on for the iteration loop (T030/T031).
- Workaround removals (T016/T017/T018) MUST be separate commits (FR-010).
- Each PLAYBOOK iteration in T030/T031 produces its own commit + push + HISTORY line (FR-013, FR-027).
- Helper extraction (T032) MUST land as a single commit containing both the helper content and the bot's switch (FR-014 / 020 §FR-021).

### Parallel Opportunities

- **Within Setup**: T004 can run in parallel with T002/T003.
- **Within Foundational**: T005 and T006 can run in parallel.
- **Within US1 verification**: T008, T009, T010, T011 can all run in parallel (they read different parts of the same run directory).
- **Within US2**: the three workaround removals (T016, T017, T018) touch different parts of `tactics.fsx` and could in theory be parallelized, but per FR-010 each lands as its own commit so they should be sequenced by an operator working alone.
- **Within US3**: T027 (helper), T028a (tests), and T028b (.fsproj wiring) can be drafted in parallel; the playbook update (T029) is independent of all three.
- **US4 in parallel with US3**: the probe iteration is one iteration inside the US3 loop; T038 wires it, T039 runs it, T040 records it.

---

## Parallel Example: User Story 1 verification

```bash
# T007 first (sequential — produces the run directory the others read):
bash bots/trainer/run.sh NullAI smoke-021

# Then T008/T009/T010/T011 in parallel (different files, same run dir):
jq '.telemetry.peak_metal, .telemetry.peak_energy' <run_dir>/result.json   # T008
grep -n "Shutdown received" <run_dir>/stdout.log                            # T009
wc -c <020_run>/engine.infolog <run_dir>/engine.infolog                     # T010
jq 'has("rc_minus_2_count") and has("by_case")' <run_dir>/unwired_commands.json  # T011
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 Setup (T001–T004): pull HighBarV2, rebuild, install, restart FSI, branch guard.
2. Phase 2 Foundational (T005–T006): schema relaxation + post-match grep.
3. Phase 3 US1 (T007–T012): smoke iteration + five checks + HISTORY.
4. **STOP and VALIDATE**: integrated proxy verified end-to-end. The MVP delivers "the trainer can see the canonical signals" as a discrete deliverable.
5. The user can decide at this point whether to continue with US2 (which is gated on US1 anyway).

### Incremental Delivery

1. Setup + Foundational → environment ready.
2. + US1 → integrated proxy verified. (MVP)
3. + US2 → workarounds removed; canonical Shutdown(GAME_OVER) is the only victory path. Both rungs win again.
4. + US3 → iteration loop walked; at least one substantive helper extracted; SC-005/006/007 verified.
5. + US4 → Issue 1 probe captured and reported; SC-008 verified.
6. + Polish → final SC checklist; outbound mailbox confirmed; clean working tree.

Each increment adds value without breaking previous increments. Stopping after any phase still leaves the branch in a consistent state per the commit-and-push discipline (FR-010 + FR-027).

### Solo-Operator Strategy

This feature is designed for one operator (human or AI developer) walking the playbook in a single long session. The "parallel" markers above are about *task-graph independence*, not about staffing — a single operator just runs them in any order within a phase.

---

## Notes

- **Tier 2 change classification** (per `plan.md` Constitution Check): no FSBarV1 public API changes, no `.fsi` updates, no surface-area baseline regeneration, no `fsdoc` agent run.
- **Test scope**: only one new compiled test file (`TrainerStallTests.fs`). All other behavioural verification is via live-engine smoke iterations and operator-walked playbook, consistent with feature 020.
- **Commit discipline**: every meaningful change is its own commit pushed immediately (FR-010 + 020 §FR-027). No batched commits.
- **No PR**: the feature ends on the `021-rerun-trainer-highbar` branch on the remote. Merging to `master` is out of scope (FR-028 inherited from 020).
- **Cross-repo edits**: forbidden inside this feature (FR-021). Defects in HighBarV2 route via inbound mailbox + halt.
- **Avoid**: editing HighBarV2 source from inside this feature; rewriting the 020 schemas non-additively; classifying a `win` outcome via a workaround that has not yet been removed (SC-010).
