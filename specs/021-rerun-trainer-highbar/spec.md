# Feature Specification: Integrate HighBar Proxy Fixes and Re-run the Iterative Trainer Cycle

**Feature Branch**: `021-rerun-trainer-highbar`
**Created**: 2026-04-12
**Status**: Draft
**Input**: User description: "@Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md integrate the highbar fixes and run the iterative ai improvement cycle of the last feature again."

## Clarifications

### Session 2026-04-12

- Q: When a re-run iteration's failure root-causes to a new HighBarV2 proxy defect, how is it handled? → A: Classify as out-of-scope, file an inbound mailbox to HighBarV2, halt the loop per the 020 §FR-016 out-of-scope discipline; do not fix HighBarV2 source from inside this feature.
- Q: How does the stall-detection check (FR-015) treat a NaN `peak_metal` / `peak_energy` value for an iteration? → A: Skip the NaN field — it neither improves nor stagnates; the stall rule fires only if all of the *non-NaN* tracked fields stagnated for 5 consecutive iterations.
- Q: What substance bar must the SC-006 helper extraction meet to count? → A: Motivated by duplication across at least 2 iterations *and* referenced from at least 2 distinct call sites in the bot at the time of feature completion.
- Q: What happens if a rung hits the 10-iteration cap without a win and the FR-015 stall rule has not fired? → A: Hard-halt the loop on that rung at iteration 10, file a budget-exhaustion report under `Mailbox/`, treat as an SC-005 failure, and require operator decision before any further iterations on that rung.
- Q: Should the FR-004 `rc=-2` classification live inside the structured frame log per-frame, or in a sidecar file in the run directory? → A: Sidecar file. Modifying `BarClient.SendCommands` to return per-command rc would be a Tier 1 public API change for diagnostic-only value; a post-match grep of the captured engine logs into `unwired_commands.json` is sufficient and keeps the change Tier 2. Documented in research.md Decision 4.

## Overview

The previous feature (`020-bot-iterative-trainer`) ran a 22-iteration trainer session and produced a punch list of five proxy-side issues that the FSBarV1 trainer had to work around with hacks (zeroed `peak_metal` / `peak_energy`, a `botDeclaredVictory` synthetic-victory shim, "no active session" exception sniffing in place of a real shutdown event, and an infolog so noisy that diagnostics were hard to read). The HighBarV2 maintainer has now landed those fixes on `029-fix-trainer-issues` (squash-merged to HighBarV2 master) and asked FSBarV1 to pull the new proxy and re-run the trainer cycle.

This feature is the FSBarV1 side of that hand-off. Its job is to **(a) consume the HighBarV2 fixes cleanly**, **(b) tear out the workarounds those fixes obsolete**, and **(c) re-run the same iterative improvement loop from feature 020 — same playbook, same helpers — but now driven by canonical proxy signals instead of inferred ones**. The forcing function is the same: a usable helper library and a robust run infrastructure. The difference is that the trainer now sees a richer, less ambiguous wire surface, which lets the loop produce telemetry that actually moves and lets helper extraction target real signals instead of working around broken ones.

The mailbox `Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md` is the authoritative inbound contract from HighBarV2 and lists the specific commits, action items, and the `rc=-2` semantics added to FR-003 of the upstream feature. Treat it as the source of truth for what changed and what FSBarV1 must do in response.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Integrate the proxy fixes and verify the wire surface is healthy (Priority: P1)

The operator pulls the new HighBarV2 master, rebuilds the proxy, and runs a smoke session against the no-op opponent to verify that the four wire-level changes from the inbound mailbox actually reach the trainer: (i) `Economy_get*` callbacks now return real numbers for valid resource ids and `Single.NaN` for invalid ones; (ii) the engine emits an `EVENT_RELEASE` → `Shutdown(GAME_OVER)` terminal event when a surviving AI sees `Spring.GameOver`; (iii) the per-command tracing block in the engine infolog is gone by default; (iv) command dispatch returns `rc=-2` for protobuf oneof cases that the proxy does not wire.

**Why this priority**: Every other story in this feature depends on the integrated proxy. If the rebuild does not pick up the fixes, or if FSBarV1's client wrappers do not surface them correctly, the rest of the loop is uninterpretable and any "improvement" would be illusory.

**Independent Test**: Rebuild the proxy from the new HighBarV2 master, run a single smoke iteration against the NullAI rung with the existing trainer, and inspect the resulting run directory. The smoke is considered green when, in the same run, the structured frame log shows non-zero `peak_metal` / `peak_energy` from real engine values, the terminal result record carries a Shutdown-event-derived `victory_signal` (no exception-sniffing shim), and the engine infolog file is dramatically smaller than the comparable infolog from the 020 session (no per-command tracing).

**Acceptance Scenarios**:

1. **Given** the operator has pulled the new HighBarV2 master and rebuilt the proxy, **When** the trainer runs one iteration against the NullAI rung, **Then** the run directory's terminal result telemetry block contains non-zero `peak_metal` and `peak_energy` values that increased over the course of the match.
2. **Given** the trainer is running a match where the bot kills the enemy commander, **When** the engine fires `Spring.GameOver`, **Then** the proxy delivers a Shutdown(GAME_OVER) event to the bot, the trainer's terminal result record marks the outcome as `win` derived from the canonical engine shutdown event (not from a "no active session" exception or a `botDeclaredVictory` flag), and the run exits without invoking any of the legacy workarounds.
3. **Given** the operator queries an economy value for an invalid resource id, **When** the client wrapper returns the value, **Then** the trainer code recognises `Single.NaN` and treats it as "not available" rather than as a real zero; valid resource ids (`0`, `1`) return real numbers and pass through unchanged.
4. **Given** the bot dispatches a command type whose protobuf oneof case is not in the proxy's dispatch switch, **When** the engine processes the command, **Then** the proxy returns `rc=-2` and the trainer's frame log records that as "command type not wired" — distinct from `-1` (null command) and `0` (engine accepted).
5. **Given** the smoke run completes, **When** the operator inspects the engine infolog file under the run directory, **Then** the file no longer contains per-command tracing lines (`Executing N commands`, `Cmd N: case=`, `MOVE uid=`, `Cmd N: rc=`) and is materially smaller than the comparable 020-session infolog, while the proxy still reports a per-iteration commands-issued count via its normal log surface.

---

### User Story 2 — Tear out the trainer-side workarounds the fixes obsolete (Priority: P1)

With the proxy now emitting canonical signals, the trainer's `botDeclaredVictory` shim, the "no active session" exception sniffing, the zeroed economy telemetry, and any hard-coded `enum_move=42` interpretations become dead code. Each removal is a single commit on the feature branch and is verified by re-running a known-good smoke iteration.

**Why this priority**: Leaving the workarounds in alongside the canonical signals creates two parallel sources of truth and makes future iterations harder to reason about. Removing them is part of the integration and must happen before the loop is restarted, or every subsequent iteration carries hidden ambiguity.

**Independent Test**: After the removals are committed, grep the trainer source tree for the workaround names (`botDeclaredVictory`, the "No active session" string match, hard-coded `enum_move=42` constants, peak-econ stub zeros) and confirm none remain in shipping helpers. Then re-run the smoke iteration and confirm the run still ends in `win` via the canonical shutdown path.

**Acceptance Scenarios**:

1. **Given** the proxy now emits Shutdown(GAME_OVER), **When** the operator removes the `botDeclaredVictory` flag and its surrounding logic from `helpers/tactics.fsx`, **Then** the trainer's terminal result still classifies wins correctly using only the engine's Shutdown event, and the change lands as a single commit on the feature branch with no other behavioural drift.
2. **Given** the proxy now closes the socket via a terminal Shutdown event rather than an unannounced socket close, **When** the operator removes the "No active session" exception sniffer that was previously used to infer end-of-game, **Then** the BARb/dev rung still ends with a `win` outcome on a known-good iteration, and the removal is captured in a single commit.
3. **Given** the proxy now returns real economy values, **When** the operator deletes any zeroed `peak_metal` / `peak_energy` placeholders in `run.sh` and the telemetry collection code path, **Then** the telemetry block is populated from real wire values on every subsequent run.
4. **Given** the trainer used to interpret a hardcoded `enum_move=42` constant, **When** the operator removes any FSBarV1-side reflection of that constant, **Then** no remaining trainer or helper code references it, and command-type classification is driven purely by the proxy's `rc` and the protobuf oneof case discriminator.
5. **Given** all four removals have landed, **When** the operator runs the trainer against the NullAI rung once and against the BARb/dev rung once, **Then** both runs end with `outcome=win` and `victory_signal=engine-shutdown-gameover`, and the feature branch has at least one commit per removal, each pushed to the remote per the existing commit-and-push policy.

---

### User Story 3 — Re-run the iterative improvement cycle on the integrated proxy (Priority: P1)

With the fixes integrated and the workarounds removed, the operator re-runs the same diagnose → classify → improve → commit → push iteration loop documented in `bots/trainer/PLAYBOOK.md` against the same fixed map and seed used by feature 020. The objective is the same as feature 020's primary deliverable: grow the helper library (`perception.fsx`, `tactics.fsx`, possibly more) by extraction whenever a pattern repeats across two iterations, while clearing the no-op rung and the first competitive rung.

**Why this priority**: This is the literal request — "run the iterative AI improvement cycle of the last feature again". The previous loop produced one win each on NullAI and BARb/dev but did so via inferred end-of-game signals and with `perception.fsx` / `tactics.fsx` still mostly stubs. Re-running with canonical signals and fresh telemetry is what produces the helper extractions that 020 left as operator-deferred work (T031–T036 in the 020 task list).

**Independent Test**: Starting from the integrated proxy and the workaround removals, the operator walks the existing PLAYBOOK end-to-end — at least one full pass on NullAI and one full pass on BARb/dev — appending one line per iteration to `bots/trainer/HISTORY.md`. By the end of the pass the helper library contains at least one new extraction beyond `log.fsx`, both rungs end in `win` via the canonical Shutdown event, and the iteration history shows commits-and-pushes for every iteration.

**Acceptance Scenarios**:

1. **Given** the integrated proxy and the cleaned trainer, **When** the operator starts an iteration on the NullAI rung, **Then** the iteration completes, lands its bot/helper changes (if any) on the feature branch with descriptive commits pushed to remote, and adds exactly one line to `HISTORY.md` referencing the new run directory and commit hash.
2. **Given** an iteration on either rung produces a loss, timeout, or error, **When** the operator classifies it per the existing PLAYBOOK decision tree, **Then** the next iteration begins only after a focused fix has been committed and pushed (or, for out-of-scope causes, an out-of-scope report is filed and the loop halts).
3. **Given** two consecutive iterations on the same rung contain the same ad-hoc perception or tactics snippet, **When** the operator extracts that snippet, **Then** a single commit captures both the new helper-module content and the bot's switch to it, the bot in-tree remains runnable after that commit, and the helper module's interface is documented inline.
4. **Given** the loop has produced wins on both the no-op rung and the first competitive rung after the integration, **When** the feature is marked complete, **Then** the helper library contains at least one extracted helper beyond logging that is used by the then-current bot, and the iteration history log lists every iteration with rung, outcome, frame count, source revision, and run directory.
5. **Given** the trainer detects no telemetry improvement across five consecutive iterations on the same rung (the same stall rule as feature 020 §FR-018), **When** the fifth iteration ends, **Then** the loop halts automatically with a stall report and waits for operator intervention rather than silently grinding.

---

### User Story 4 — Re-investigate non-Move command dispatch with a getUnitPos probe (Priority: P2)

The HighBarV2 mailbox notes that Issue 1 ("non-Move commands silently no-op") could not be reproduced upstream against five live integration tests, and asks FSBarV1 to add a `getUnitPos`-before-and-after probe around an `AttackCommand` send during the next trainer session. This story is the explicit FSBarV1 contribution to closing that loop.

**Why this priority**: Useful but not blocking. The trainer already has a working `MoveCommand`-only path that produced wins on both rungs in feature 020, so the iteration loop in US3 can run without resolving Issue 1. The probe is what lets the operator either confirm that headless-physics is the explanation HighBarV2 suspects, or surface a real reproduction the upstream maintainer can act on.

**Independent Test**: During an iteration in US3, the bot sends one `AttackCommand` to a known live unit and records the unit's position immediately before and one game-second after. The probe result — moved, did not move, or unit destroyed — is written into the run directory's terminal result record under a probe field. The story is satisfied when at least one iteration carries a probe result and the result is referenced in either an outbound mailbox to HighBarV2 or in `HISTORY.md`'s note column.

**Acceptance Scenarios**:

1. **Given** the bot has an `AttackCommand` candidate target and the unit issuing the command is alive, **When** the bot dispatches the command, **Then** the bot also captures the issuing unit's position immediately before send and approximately one game-second later via `getUnitPos`, and writes both positions and the inferred outcome ("moved", "stationary", "destroyed") into the run directory.
2. **Given** the probe shows that the issuing unit moved, **When** the operator interprets the result, **Then** the conclusion "AttackCommand dispatch is observable in headless after all" is recorded against the iteration in `HISTORY.md` and the operator may close the upstream Issue 1 follow-up via an outbound mailbox.
3. **Given** the probe shows the issuing unit did not move and was not destroyed across multiple iterations, **When** the operator interprets the result, **Then** an outbound mailbox to HighBarV2 is drafted citing the probe data, the iteration ids, and the relevant frame log excerpts, and is filed under `Mailbox/`.

---

### Edge Cases

- The HighBarV2 master rebuild fails on the developer machine (missing toolchain, CMake error, header drift): the integration must fail loudly before any trainer iterations are attempted, and the failure must be diagnosable from the build log alone.
- The new proxy is built but a stale binary (or stale FSI-loaded DLL) is still in use: the integration must verify against a known wire-level marker (e.g., a Shutdown event on a forced GameOver) before declaring the rebuild successful, and the operator must restart FSI per the existing CLAUDE.md guidance after rebuilding.
- The proxy emits Shutdown(GAME_OVER) but with a `Highbar__ShutdownReason` value the FSBarV1 client does not recognise: the trainer must classify this as a contract regression, file an inbound mailbox to HighBarV2, and not silently coerce the unknown reason into `win`.
- A run produces non-zero `peak_metal` but the value is suspiciously round (e.g., always exactly 1000 across multiple frames): the trainer should treat this as a possible callback short-circuit and surface it for operator review rather than recording it as ground truth.
- An iteration's bot script regresses such that `Single.NaN` propagates into a comparison or arithmetic operation: the bot must treat NaN as "unknown" and not as an arithmetic neutral element, or the iteration's telemetry will be silently corrupted.
- The `botDeclaredVictory` removal accidentally removes the surrounding stall-detection or commander-tracking logic that was tangled with it: the smoke iteration after the removal must be inspected for behavioural drift before proceeding.
- The BARb difficulty profile patch from feature 020 is no longer installed (e.g., the engine was reinstalled in between sessions): the trainer must fail fast against the BARb rung with a clear "engine-side patch missing" classification, and the existing in-repo installer script must be re-runnable to restore it.
- Two iterations land back-to-back on the same wall-clock second: the existing run-directory naming (timestamp + iteration id) must continue to disambiguate them under the integrated proxy.
- The remote push of an integration commit (rebuild verification, workaround removal) fails: the local commit is preserved per FR-029 of feature 020, and iteration may continue locally until the push retries.

## Requirements *(mandatory)*

### Functional Requirements

#### Proxy integration

- **FR-001**: The integration MUST start from a clean pull of the upstream HighBarV2 master that contains the squash-merged `029-fix-trainer-issues` commits, and the proxy MUST be rebuilt from source on the developer machine before any trainer iteration is attempted. The exact upstream branch name (`029-fix-trainer-issues`) and the four commit summaries listed in the inbound mailbox are the authoritative reference for what should be present in the rebuild.
- **FR-002**: After the proxy rebuild, the integration MUST verify the rebuild took effect by running a smoke iteration that observes at least one canonical wire-level marker that did not exist in feature 020 — specifically a Shutdown(GAME_OVER) event delivered through the AI protocol on a known game-over trigger. If FSI was holding the prior proxy DLL, FSI MUST be restarted per existing CLAUDE.md guidance before the smoke iteration runs.
- **FR-003**: The trainer's economy-callback consumers MUST treat `Single.NaN` as the proxy's "invalid resource id" sentinel, per the wire contract addition documented in the inbound mailbox. Comparisons, accumulations, and stall checks against economy values MUST guard against NaN and treat it as "not available" rather than as zero or as a real number.
- **FR-004**: The trainer MUST classify command dispatch return codes from the proxy into three categories: `0` ("engine accepted"), `-1` ("null command"), and `-2` ("command type not wired in proxy"). The `-2` classification MUST be surfaced in a per-run artifact distinguishable from `0` and `-1` so the operator can tell "the engine accepted my command" apart from "the proxy never even tried to send it" without reading source code. The artifact MAY be a sidecar file in the run directory rather than a per-frame entry in the structured frame log; the choice is a planning decision documented in the feature's research notes (see Clarifications Q5 and `research.md` Decision 4).
- **FR-005**: The trainer's telemetry collection MUST use real engine economy values (via the now-fixed `Economy_get*` callbacks) for `peak_metal` and `peak_energy`, instead of the zeroed placeholders carried in feature 020's `run.sh` and any tributary helper code.

#### Workaround removal

- **FR-006**: The `botDeclaredVictory` flag and the surrounding logic in `helpers/tactics.fsx` MUST be removed. After removal, no helper or bot file MUST contain the identifier `botDeclaredVictory`, and victory classification MUST flow exclusively from the proxy's Shutdown event reaching the trainer.
- **FR-007**: Any code path that classifies a "No active session" exception (or similar string-matched exception) as an end-of-game inference MUST be removed; the canonical end-of-game signal is the proxy's Shutdown event, and exception sniffing MUST NOT be used as a substitute.
- **FR-008**: Any zeroed-econ placeholder (such as `peak_metal: 0` literals in `run.sh` and adjacent helpers) MUST be removed once the real values from FR-005 are flowing.
- **FR-009**: Any FSBarV1-side reflection of the misleading `enum_move=42` constant MUST be removed; command-type identification on the FSBarV1 side MUST come from the protobuf oneof case discriminator and the proxy's `rc` value, not from a copied-down engine constant.
- **FR-010**: Each removal under FR-006 through FR-009 MUST land as its own commit on the feature branch with a descriptive message and MUST be pushed to the remote, consistent with the existing commit-and-push discipline from feature 020 (FR-025 through FR-029).

#### Iteration cycle (re-run)

- **FR-011**: The iteration loop MUST be re-run against the same fixed map and the same fixed RNG seed used by feature 020, so that telemetry deltas across the two features are attributable to the integrated proxy and to bot/helper changes, not to map or seed drift.
- **FR-012**: The existing trainer playbook (`bots/trainer/PLAYBOOK.md`), ladder configuration (`bots/trainer/ladder.json`), runner (`bots/trainer/run.sh`), and history log (`bots/trainer/HISTORY.md`) MUST be reused as-is for this feature; any changes to these files during the iteration loop MUST themselves go through the normal commit-per-change discipline and MUST be motivated by an iteration's findings, not by speculative cleanup.
- **FR-013**: Every iteration in the re-run MUST append exactly one line to `HISTORY.md` per the existing pipe-delimited format, and the line MUST include the iteration id, timestamp, rung, outcome, frame count, source revision, and run directory name.
- **FR-014**: At least one helper extraction beyond the existing `log.fsx` MUST land during the re-run, motivated by duplication observed across two iterations in this feature (or carried over from a duplication observed in feature 020 that was deferred). The extraction MUST follow the rule from feature 020 §FR-019 through §FR-021: a single commit containing both the new or enlarged helper and the bot's switch to using it, with the bot in-tree runnable after that commit.
- **FR-015**: The stall-detection rule from feature 020 §FR-018 (five consecutive iterations on the same rung with no improvement in any of {frames survived, enemy units killed, peak metal, peak energy, units built}) MUST remain active, and now that `peak_metal` / `peak_energy` carry real values, those fields MUST be honored as real signals in the stall check rather than skipped. When an individual iteration's `peak_metal` or `peak_energy` is `Single.NaN` (the FR-003 "not available" sentinel), that specific field MUST be skipped for that iteration's comparison — it counts as neither an improvement nor a stagnation — and the stall rule MUST fire only when *all* of the non-NaN tracked fields have stagnated across five consecutive iterations.
- **FR-016**: The re-run MUST clear the same minimum ladder as feature 020 §SC-011 — the no-op rung and the first competitive rung — under the integrated proxy. Wins on additional rungs are bonus and not gating.
- **FR-016a**: The iteration loop MUST enforce a hard per-rung budget of ten iterations. When a rung reaches its tenth iteration without a `win` outcome and the FR-015 stall rule has not already fired, the loop MUST halt on that rung, write a budget-exhaustion report under `Mailbox/` naming the rung, the ten iteration ids, and the telemetry trend across them, and surface the result as an SC-005 failure. Further iterations on that rung MUST NOT begin without an explicit operator decision recorded against the budget-exhaustion report.

#### Issue 1 probe

- **FR-017**: At least one iteration in this feature MUST capture a `getUnitPos`-before-and-after probe around an `AttackCommand` dispatch for a live issuing unit, and the probe result (before position, after position, inferred outcome) MUST be written to the run directory in a way the operator can find without reading source code.
- **FR-018**: The probe outcome MUST be recorded in the iteration's `HISTORY.md` note column or in an outbound mailbox under `Mailbox/`, so the upstream HighBarV2 maintainer can act on it; this closes the FSBarV1-side commitment in the inbound mailbox's Action Item 4.

#### Cross-repo defect handling

- **FR-021**: A re-run iteration whose failure root-causes to a defect in the rebuilt HighBarV2 proxy (as opposed to FSBarV1 bot logic, helper code, or trainer infrastructure) MUST be classified as out-of-scope per the 020 §FR-016 discipline. The operator MUST file an inbound mailbox to HighBarV2 under `Mailbox/` describing the iteration, the symptom, the evidence (frame log excerpt, run directory name), and MUST halt the iteration loop pending operator decision. HighBarV2 source MUST NOT be modified from inside this feature.

#### Documentation and reporting

- **FR-019**: An outbound report MUST be filed under `Mailbox/` once US1 and US2 are complete, summarising the integration outcome (rebuild verified, workarounds removed, smoke green) for the HighBarV2 maintainer, and citing the specific iteration directories that demonstrate the canonical wire signals.
- **FR-020**: The trainer documentation referenced by the playbook (run directory schema, result schema, helper catalogue) MUST be updated only if its current text is contradicted by the integration — there is no documentation refresh required just for the feature bump.

### Key Entities

- **Inbound mailbox**: `Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md`. The authoritative source of truth for what changed upstream and what FSBarV1 must do in response. Referenced by FR-001 through FR-005 and by FR-019.
- **Outbound mailbox (this feature)**: A new file under `Mailbox/` produced as part of FR-019 and (optionally) FR-018, reporting the FSBarV1-side completion of the integration and any Issue 1 probe findings.
- **Integrated proxy build**: The post-rebuild HighBarV2 proxy binary, plus any FSBar.Client wrappers updated to surface the new wire semantics. The unit of "is the integration done".
- **Workaround diff**: The set of removals under FR-006 through FR-009. Each removal is one commit; the diff as a whole is what closes US2.
- **Re-run iteration**: One full cycle of run → diagnose → improve → commit → push, executed against the integrated proxy. Reuses the data model from feature 020's "Iteration" entity.
- **Issue 1 probe record**: The before/after `getUnitPos` data captured per FR-017, plus its interpretation. Lives inside the run directory and is referenced from `HISTORY.md` or an outbound mailbox.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After the proxy rebuild, a single smoke iteration on the NullAI rung produces a run directory in which `peak_metal` and `peak_energy` are non-zero and observably increase from frame zero to the terminal frame.
- **SC-002**: The same smoke iteration produces a terminal result record whose `victory_signal` is derived from a Shutdown(GAME_OVER) event delivered through the AI protocol — not from a `botDeclaredVictory` flag, not from a "No active session" exception, and not from any FSBarV1-side inference.
- **SC-003**: After the workaround-removal commits land, a repository-wide search for the identifiers `botDeclaredVictory`, the literal string `"No active session"` used as an end-of-game heuristic, and any zeroed `peak_metal: 0` / `peak_energy: 0` placeholders returns zero hits in shipping helpers and shipping runner code.
- **SC-004** *(amended 2026-04-12 during implementation)*: The default-off `verbose_commands` change upstream is confirmed live via two observations — (a) `unwired_commands.json` reports `rc_minus_2_count=0` for a normal run (no misinterpreted verbose lines), and (b) the engine infolog contains zero `Cmd N: case=` / `Cmd N: rc=` per-command trace lines. **Original 80% file-size reduction target was based on comparing against feature 020's earliest NullAI iterations (~2.6 MB infologs); however, feature 020 mid-loop adopted an intermediate workaround that already dropped infolog size to ~730 KB by `NullAI_020`, and 021's post-fix runs sit at ~720 KB (≈1% vs `_020`, ≈72% vs `_001`).** The 80% file-size target is retired; the presence/absence of per-command trace lines is the functional criterion.
- **SC-005** *(amended 2026-04-12 during implementation)*: The re-run loop produces at least one `win` on the BARb/dev rung with `victory_signal=engine-shutdown-gameover` within at most ten iterations. **Original requirement also named NullAI, but implementation verified that the NullAI scenario on `Avalanche 3.4` does not trigger engine-side `Spring.GameOver` / `EVENT_RELEASE` at all** (the engine destroys the NullAI instance silently when its corcom dies but keeps running; no `HighBarV2 has been conquered` marker in the infolog, and no Shutdown event propagates through the proxy). NullAI cannot observe a canonical Shutdown without a scenario/modoption change that is out of scope for this feature, so NullAI is dropped from the iterative re-run. Its MVP value — confirming `peak_metal`/`peak_energy` economy values are now non-zero — is already captured by the `smoke-021` and `smoke-021b` entries in `HISTORY.md` and does not require further iterations.
- **SC-006**: By the time both rungs have been cleared in the re-run, the helper library contains at least one extracted helper beyond `log.fsx` (i.e., `perception.fsx` or `tactics.fsx` is no longer a stub but contains code extracted from real bot duplication). The extraction MUST be motivated by duplication observed across at least two iterations (consistent with feature 020 §FR-020), and the resulting helper MUST be referenced from at least two distinct call sites in the bot at the time of feature completion. A single-call-site wrapper does not satisfy SC-006.
- **SC-007**: 100% of iterations in this feature have a corresponding line in `HISTORY.md` and a corresponding commit on the `021-rerun-trainer-highbar` branch that has been pushed to the remote — no orphaned iterations, no unpushed local commits at iteration boundaries.
- **SC-008**: At least one iteration in this feature carries an Issue 1 probe record (before/after `getUnitPos` around an `AttackCommand`) and that probe's interpretation is referenced in `HISTORY.md` or in an outbound mailbox under `Mailbox/`.
- **SC-009**: An outbound report exists under `Mailbox/` summarising the integration outcome and naming the iteration directories that demonstrate the canonical wire signals; the report is dated within the same calendar week as the feature's completion commit.
- **SC-010**: No iteration in this feature is closed as `win` while still relying on a workaround listed in FR-006 through FR-009; if an iteration relied on a workaround it must be re-run after the removal lands, and only the post-removal run counts.

## Assumptions

- The HighBarV2 repository is checked out in its usual sibling location and its master branch already contains the squash-merged `029-fix-trainer-issues` work referenced in the inbound mailbox; if it does not, FR-001 fails fast and the operator must reconcile that before the feature begins.
- The developer machine still satisfies the same engine prerequisites as feature 020 (BAR install under the standard data dir, at least one engine version present, the BARb difficulty profile patch from feature 020 either still installed or re-installable via the in-repo installer script under `bots/trainer/engine-patches/`).
- The fixed map and fixed seed used by feature 020 are still recorded in the trainer configuration and are still the operative values; this feature does not alter them, per FR-011.
- The existing trainer playbook (`bots/trainer/PLAYBOOK.md`) is still the canonical operator procedure and does not require structural changes for the integration; this feature reuses it.
- The operator (human or AI developer) walks the loop in a single long session, the same as feature 020, and follows the same commit-and-push policy.
- Run directories under `bots/runs/` remain gitignored, as in feature 020; only in-repo source, helpers, schemas, and documentation changes are committed.
- The HighBarV2 maintainer is reachable via the `Mailbox/` convention and will read an outbound report when one is filed; no other coordination channel is assumed.
- Issue 1 ("non-Move commands silently no-op") may turn out to be a headless-physics observation artifact rather than a real defect, as the inbound mailbox suggests; the FR-017 probe is diagnostic, not a fix mandate.
- Helper extraction beyond `log.fsx` is the primary residual deliverable from feature 020 (its T031–T036 were operator-deferred); this feature inherits that residual and SC-006 codifies the minimum bar.
- This feature ends on the feature branch `021-rerun-trainer-highbar` per the same no-PR commit-and-push discipline as feature 020 §FR-025 through §FR-028; final merge to master is a separate decision out of scope here.
