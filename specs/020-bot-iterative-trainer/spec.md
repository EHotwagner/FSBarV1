# Feature Specification: Iterative AI Bot Trainer with Helper Library

**Feature Branch**: `020-bot-iterative-trainer`
**Created**: 2026-04-12
**Status**: Draft
**Input**: User description: "create specs according to this plan. change the commit strategy to just commit all and push on the feature branch without pr, also to gh. the main objective is to gain a usefull helper library and a robust infrastructure."

## Overview

The primary objective of this feature is **not** to beat the game's AI for its own sake — it is to **grow a useful helper library and a robust infrastructure** for writing AI bots that play Beyond All Reason through this repository's existing client surface. Bot iterations against progressively harder opponents are the forcing function that surfaces which helpers and which infrastructure pieces are actually worth building.

The trainer is operated by an AI developer working in a single long session. Each iteration plays one match, produces comprehensive logs, feeds back into a diagnose-and-improve loop, and — whenever a reusable pattern appears — causes code to be extracted into a helper module. Over many iterations, the helper library and the run infrastructure accrete into something a future bot author can pick up and use directly.

## Clarifications

### Session 2026-04-12

- Q: What does "win against the no-op opponent" mean in an objective, engine-verifiable way? → A: Kill the enemy commander — match ends via engine-signalled commander death (`deathmode=com`); this is the canonical win condition for every rung, including the first.
- Q: When should the iteration loop halt on a stuck rung, and how is "progress" decided? → A: Halt after 5 consecutive iterations against the same rung where none of the tracked telemetry fields (frames survived, enemy units killed, peak metal, peak energy, units built) improved over the iteration's predecessor. Telemetry-based, objective, reuses FR-004.
- Q: What is the minimum ladder that counts as "feature complete"? → A: Clear the no-op rung **and** at least one competitive rung (lowest competitive difficulty profile counts). Remaining competitive rungs are best-effort: attempted and telemetry-recorded, but not required for feature completion.
- Q: Does the trainer fix one map, pick from a fixed short list, or allow any map via configuration? → A: One fixed map for the whole feature. Every run — across every rung and every iteration — uses the same map. Changing map is an explicit out-of-scope decision requiring a new feature.
- Q: Should two runs with the same bot, rung, and map produce identical game traces, or should the RNG seed vary? → A: Fixed seed for the whole feature. Every run uses the same seed value, set once in trainer configuration. Telemetry deltas across iterations are therefore attributable to code changes (bot, helpers, repo) rather than RNG drift. Seed variation for generalization testing is an explicit future feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Run one full match and capture everything (Priority: P1)

An AI developer writes a minimal bot script, picks an opponent, and runs a single match. The trainer launches a headless engine session, the bot plays until the game ends or a frame limit is reached, and every artifact needed to diagnose what happened afterwards is written to a self-contained run directory.

**Why this priority**: Without a reliable one-shot run that produces full logs, nothing else in the feature can work — no iteration loop, no pattern extraction, no difficulty escalation. This is the infrastructure floor.

**Independent Test**: Invoke the trainer run command against the simplest (no-op) opponent with a stub bot script. Observe that a fresh run directory appears containing match metadata, a per-frame event log, captured engine output, and a terminal result summary. A human reading only the run directory can reconstruct what the bot did, what the engine did, and how the match ended.

**Acceptance Scenarios**:

1. **Given** the trainer is installed and an opponent is selected, **When** the operator starts a single match run, **Then** a new run directory is created, the headless engine launches, the bot connects, frames and events are recorded throughout the match, and on exit the run directory contains the bot script snapshot, metadata, structured frame log, captured engine logs, and a terminal result record.
2. **Given** the engine fails to start, **When** the trainer attempts the run, **Then** the failure is recorded in the run directory's terminal result record with enough detail to classify it (missing engine, socket conflict, script generation error, etc.), and the trainer exits cleanly without leaving orphan processes or sockets.
3. **Given** a match reaches the configured frame limit without a winner, **When** the trainer ends the run, **Then** the result is marked as a timeout, partial telemetry is still written, and all log files are flushed and closed.

---

### User Story 2 — Iterate the bot through a diagnose-improve loop (Priority: P1)

After a match finishes, the operator reads the run directory, classifies what went wrong or what worked, and changes something — bot logic, a helper module, the shared infrastructure, or even upstream repo code. Every change is committed to the feature branch and pushed. The next iteration uses the improved code. Over many cycles the bot gets better and the helpers and infrastructure grow.

**Why this priority**: This is the mechanism that turns single-shot runs into a learning system and, more importantly, drives the accretion of the helper library. Without it, runs produce logs but nothing compounds.

**Independent Test**: Start from a deliberately weak bot script. Run it, read the logs, edit the bot (or extract a helper), commit and push, run again. After a small bounded number of iterations the run outcome improves measurably (e.g., the bot now achieves its first win, or the same failure does not recur). A reviewer reading the commit history can see a trail of `bot iter N` / `fix: …` / `extract <helper>` commits on the feature branch.

**Acceptance Scenarios**:

1. **Given** a run ended with a loss, **When** the operator classifies the failure as bot logic and edits the bot, **Then** a commit is created on the feature branch, pushed to the remote, and the next run uses the updated script automatically.
2. **Given** two consecutive iterations contain the same ad-hoc perception/tactics snippet, **When** the operator recognises the duplication, **Then** the snippet is extracted into a helper module, the bot is updated to call the helper, and a single commit captures both the extraction and the bot's switch to it.
3. **Given** a run failure is traced to a bug in the shared client library or supporting infrastructure used by the bot, **When** the operator classifies it as an in-scope repository bug, **Then** the fix is made inside the repository's source tree, the existing automated tests are run, and a commit for the repository fix is pushed to the feature branch alongside the bot iterations.
4. **Given** a run failure is traced to an out-of-scope cause (engine crash, opponent AI internal bug, OS-level issue), **When** the operator classifies it as such, **Then** an out-of-scope report is written to the run-history area, the iteration loop halts, and control is returned to the user for a decision.
5. **Given** a run completed successfully, **When** the operator updates the iteration history log, **Then** the log gains a single-line entry identifying the iteration, the opponent, the outcome, the frame count, and the commit hash in effect for that run.

---

### User Story 3 — Escalate through an opponent ladder (Priority: P2)

Once the bot reliably beats a given opponent rung, the trainer advances to the next rung on the ladder. The first rung is a no-op opponent so that the bot's own correctness is validated; subsequent rungs pit the bot against progressively harder configurations of the main competitive AI. A static configuration file defines the ladder.

**Why this priority**: Escalation is what produces interesting iteration signal — every rung forces a new class of helper to emerge. But it only makes sense once US1 and US2 work, hence P2.

**Independent Test**: Populate the ladder configuration with at least three rungs of increasing difficulty. Starting from the first rung, let the iteration loop run until each rung is cleared. Observe that the trainer advances the rung only after a win and never skips ahead, and that the history log shows a monotonically advancing rung column.

**Acceptance Scenarios**:

1. **Given** the bot has just won against the current rung, **When** the next iteration begins, **Then** the trainer reads the next rung from the ladder configuration, starts a run against that opponent, and records the promotion in the history log.
2. **Given** the bot has run 5 consecutive iterations against the same rung with no improvement in any tracked telemetry field, **When** the stall-detection check runs at the end of the fifth iteration, **Then** the iteration loop halts automatically with a stall report identifying the rung, the five iterations, and the telemetry trend, and the rung is not advanced without an explicit operator decision.
3. **Given** the configured ladder has been fully cleared, **When** the trainer completes the final rung, **Then** the iteration loop exits cleanly and a completion summary is produced.

---

### User Story 4 — Grow a reusable helper library and infrastructure (Priority: P1)

As iterations progress, reusable building blocks are extracted out of the bot script and into named helper modules — one for logging, one for perception queries, one for tactical routines. In parallel, infrastructure pieces (run directory layout, result schema, match runner, iteration playbook, out-of-scope reporting) stabilise. At the end of the feature, a future bot author can start a new bot by composing the existing helpers, and the run infrastructure works without modification.

**Why this priority**: This is the *stated main objective* of the feature. Everything else exists to drive it.

**Independent Test**: After the iteration loop has run for long enough to have extracted multiple helpers, ask a second operator (or a fresh session) to write a new throwaway bot that uses the helpers to play a match and produce a complete run directory — without modifying the helpers themselves. If the new bot can be assembled from existing helpers and produces a valid run, the library and infrastructure are usable.

**Acceptance Scenarios**:

1. **Given** two bot iterations contain very similar logic for a perception query or a command sequence, **When** the operator extracts that logic, **Then** the extracted helper lives in a dedicated helper module (perception or tactics), the bot is updated to use it, and both changes go out in the same feature-branch commit.
2. **Given** the match runner has evolved during iterations, **When** a new match is started, **Then** the runner uses the current (not hardcoded) run directory layout, result schema, and logging format, and produces outputs conforming to those formats without special-casing.
3. **Given** the helper library has been used in at least three distinct bot iterations, **When** a helper is modified, **Then** the change is backwards-compatible with all earlier bots or the earlier bots are updated in the same commit — there must not be broken bots left in-tree.
4. **Given** the trainer process writes a run directory, **When** the operator inspects that directory, **Then** the layout matches a single documented schema whose fields, file names, and purposes are described in the trainer documentation.

---

### Edge Cases

- The engine session directory lingers from a previous crashed run: the new run must not collide with it and must capture the stale directory's diagnostic content into the new run's logs when available.
- The bot connects but the opponent AI fails to load (missing files, profile config error): the run must still produce a terminal result record classifying the failure and must not hang indefinitely.
- The bot attempts to send commands to unit IDs that no longer exist (unit destroyed mid-frame): errors are caught and logged without terminating the match.
- Two iterations started back-to-back share a run counter collision: run directory naming must be unique even under fast back-to-back invocations (timestamp + iteration id is sufficient).
- The operator kills the trainer mid-match (Ctrl-C or external signal): the engine process and socket must be cleaned up, the run directory must be marked as interrupted, and partial logs must be flushed.
- A helper module is broken by an extraction: if the next iteration cannot even start due to a load error, the failure is visible in the run directory and is classified as an infrastructure regression, not a bot logic failure.
- The feature branch diverges from master: commits continue to be pushed to the feature branch; no rebase or merge is performed without explicit operator instruction.
- The remote push fails (network, auth): the local commit is preserved, the failure is reported, and iteration may continue locally; the operator decides when to retry pushing.
- An opponent rung is attempted before its required engine-side configuration has been applied (for example, a difficulty profile patch has not been installed): the run must fail fast with a clear classification, not hang or produce misleading telemetry.

## Requirements *(mandatory)*

### Functional Requirements

#### Match execution infrastructure

- **FR-001**: The trainer MUST launch a complete headless match consisting of exactly one bot and exactly one opponent AI, from a single operator command, without requiring any manual editing of engine configuration files at run time. Every match within this feature MUST be played on the **same fixed map**; the map name is set once in the trainer configuration and applies uniformly to every rung and every iteration.
- **FR-002**: Every match MUST produce a self-contained run directory whose location is uniquely determined by a timestamp and an iteration identifier, so no two runs can overwrite each other.
- **FR-003**: Each run directory MUST contain at minimum: the bot script as run (snapshot), match metadata (opponent, opponent options, engine version, source code revision, seed, frame limit), a structured per-frame log, copies of the engine's own log output, and a terminal result record. The RNG **seed is fixed for the entire feature** — every run uses the same seed value set in trainer configuration — so that telemetry changes between iterations are attributable to code changes rather than RNG drift.
- **FR-004**: The terminal result record MUST classify the outcome as one of: win, loss, timeout, error, or interrupted, and MUST include the frame count at termination, the cause of termination in human-readable form, and a telemetry summary (commands issued, units built, units lost, peak economy).
- **FR-005**: The trainer MUST capture engine stdout, stderr, and the engine's own infolog into each run directory, even when the engine crashes or exits abnormally.
- **FR-006**: The trainer MUST guarantee cleanup of the engine process and its socket file on every exit path, including normal termination, operator interrupt, and runaway frame count.
- **FR-007**: The structured per-frame log MUST be sampled so its size stays bounded over long matches, but MUST always include frames on which notable events (unit created/destroyed, enemy entered line-of-sight, economy milestones, commands issued) occurred.

#### Opponent configuration

- **FR-008**: The trainer MUST support running the bot against a no-op opponent as the first rung, without modifying any engine-installed files.
- **FR-009**: The trainer MUST support running the bot against the main competitive opponent AI with an optional difficulty profile and optional tuning knobs (for example: global-sight toggle, disabled unit list).
- **FR-010**: Opponent difficulty profiles that require patching engine-installed configuration files MUST be applied by a dedicated installer script stored inside the repository, with a source copy of the patched file kept alongside it, so the patch can be reapplied after an engine reinstall.
- **FR-011**: The canonical win condition is **enemy commander killed**. The trainer MUST configure the engine so that the opponent's commander death causes an engine-signalled match termination (commander-death death mode), and this condition applies uniformly to every ladder rung (including the no-op first rung). The bot MUST NOT fabricate an alternative win condition on the side; all match outcomes flow from the engine's termination signal.
- **FR-012**: The trainer MUST read the opponent ladder from a single configuration file, and MUST advance rungs strictly in the configured order, one rung per win. The ladder MUST contain at minimum a no-op first rung and at least one competitive rung. Clearing the no-op rung and the first (easiest) competitive rung is the minimum that satisfies feature completion (see SC-011); any further competitive rungs in the ladder are attempted best-effort and produce telemetry but are not required to be won.

#### Iteration loop

- **FR-013**: The iteration loop MUST be operable by a single human (or AI developer acting as one) in a single long session, walking a documented playbook stored in the repository.
- **FR-014**: After each match, the operator MUST classify the outcome into one of: bot-logic issue, repo-source issue, helper-extraction opportunity, out-of-scope external issue, or clean win requiring rung advancement.
- **FR-015**: For bot-logic issues, repo-source issues, and helper extractions, the operator MUST make the change in the source tree, produce a focused commit with a descriptive message, and push that commit to the feature branch's remote before starting the next iteration.
- **FR-016**: For out-of-scope external issues, the operator MUST write an out-of-scope report naming the iteration, the symptom, and the evidence, and MUST halt the iteration loop pending operator decision.
- **FR-017**: The trainer MUST maintain a single iteration history log recording every iteration's rung, outcome, frame count, and the source code revision in effect at that iteration.
- **FR-018**: The iteration loop MUST detect stalls and halt automatically. A stall is defined as **5 consecutive iterations against the same rung in which none of the telemetry fields of FR-004 (frames survived, enemy units killed, peak metal, peak energy, units built) improved over the previous iteration**. When a stall is detected, the loop MUST halt and record a stall report naming the rung, the five iterations involved, and the telemetry trend.

#### Helper library and infrastructure (primary objective)

- **FR-019**: The bot script MUST load its utilities from a dedicated set of helper modules organised by concern (at least: logging, perception, tactics); it MUST NOT embed long-lived infrastructure logic inline.
- **FR-020**: A helper module's *contents* MUST NOT be created preemptively; meaningful helper code MUST arise from an explicit extraction when the same logic has appeared in at least two iterations. Exceptions (bootstrapped up front because US1 depends on them):
  1. The **logging helper** (`log.fsx`) is fully implemented on day one.
  2. The **match loop skeleton** (`TrainerLoop.run`) is bootstrapped up front inside `tactics.fsx`; its body initially inlines all per-match logic and is progressively thinned as perception/tactics extractions move code out of it.
  3. Empty **placeholder modules** (`perception.fsx` containing only `module Trainer.Perception`, and — if needed — other concern-named placeholders) may exist as files on day one so that `bot.fsx` can `#load` them unconditionally; placeholders do not count as "helper modules" for the purposes of this requirement until they receive extracted code.
- **FR-021**: Each extraction MUST land as a single commit containing both the new or enlarged helper and the bot's switch to using it; the bot in-tree MUST always be runnable after any single commit.
- **FR-022**: The run directory layout, the metadata schema, the frame log schema, and the terminal result record schema MUST be documented in the repository, and every run MUST conform to the documented schemas.
- **FR-023**: Any change to a shared schema or a helper's interface MUST be backwards-compatible across bots in-tree, or all affected bots MUST be updated in the same commit.
- **FR-024**: The trainer documentation MUST include an operator-facing description of how to start a new bot by composing existing helpers, so the accumulated library is discoverable by a future bot author.

#### Commit, branch, and push discipline

- **FR-025**: All work for this feature MUST happen on the feature branch `020-bot-iterative-trainer`; no work is committed directly to the main branch.
- **FR-026**: Every meaningful change (bot edit, repo fix, helper extraction, infrastructure change, documentation update) MUST be committed individually on the feature branch with a descriptive message.
- **FR-027**: Every commit on the feature branch MUST be pushed to the remote (`origin`, GitHub) as soon as it is made — the trainer follows a "commit and push on every change" policy.
- **FR-028**: The feature branch MUST NOT be merged back to the main branch through a pull request as part of this feature's workflow; pushing to the feature branch on the remote is the end of the workflow. Merging (if any) is a separate decision outside this feature's scope.
- **FR-029**: When a push fails (network, auth, hooks), the local commit MUST be preserved, the failure MUST be surfaced to the operator, and iteration MAY continue locally until the push can be retried.
- **FR-030**: Engine-installed file patches (for example, opponent AI profile config) MUST NOT be committed as direct modifications to the engine install; only the in-repo source copy and the installer script are committed.

### Key Entities

- **Iteration**: One full cycle of run → diagnose → improve → commit → push. Identified by a monotonically increasing iteration id and a timestamp. Linked to one run directory, one rung, one outcome, and one source revision.
- **Rung**: One level of the opponent ladder. Has a short name, an opponent AI identifier, an options map (e.g., `{ "profile": "easy" }`), and a frame limit. Rungs are ordered; progression is strictly sequential.
- **Run directory**: The self-contained artifact of a single match. Holds the bot snapshot, metadata, frame log, engine logs, and terminal result record. The unit of post-hoc analysis.
- **Terminal result record**: The structured summary of how a match ended. Has an outcome (win/loss/timeout/error/interrupted), a frame count, a cause, and a telemetry block.
- **Helper module**: A named unit of extracted, reusable bot-facing code in a specific concern area (logging, perception, tactics, …). Grows by extraction, never preemptively.
- **Playbook**: The documented step-by-step procedure the operator follows every iteration. Encodes the diagnose/classify/improve/commit/push/advance decision tree.
- **Iteration history log**: A single append-only file recording one line per iteration — rung, outcome, frame count, source revision. The primary backwards-looking view of trainer progress.
- **Out-of-scope report**: A document created when the operator classifies a failure as external. Halts iteration and requests operator intervention.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A single operator command starts one complete match against the simplest opponent and produces a full run directory (all required files present and non-empty) in under two minutes of wall-clock time on a development machine.
- **SC-002**: An operator reading only a run directory (no access to the live terminal) can determine the outcome, the cause of termination, and at least five specific in-game events that occurred during the match, without reading source code.
- **SC-003**: Starting from a deliberately weak bot that fails to kill the no-op opponent's commander, the iteration loop produces a bot that kills the no-op opponent's commander (engine-signalled win) in no more than ten iterations.
- **SC-004**: By the time the bot has cleared the first competitive-AI rung, the helper library contains at least three extracted helper modules (logging plus at least two of: perception, tactics, map analysis, build orders), each used by the then-current bot and introduced via an explicit extraction commit.
- **SC-005**: Every iteration has a matching commit (or commit group) on the feature branch identifying the iteration, and every such commit has been successfully pushed to the remote — no orphaned iterations, no unpushed local commits at iteration boundaries.
- **SC-006**: A second operator, given only the trainer documentation and the helper library, can write a new minimal bot that connects, plays a match against the no-op opponent, and produces a conformant run directory without modifying the helpers or the runner. Time budget: under thirty minutes.
- **SC-007**: Across the full feature's iterations, no more than 10% of runs are lost to infrastructure regressions (a run that fails to produce a conformant run directory for reasons unrelated to the bot's own decisions). Infrastructure regressions are a signal that the runner or helpers need hardening, and each one must produce at least one fix commit.
- **SC-008**: At the end of the feature, every piece of documentation referenced by the playbook (run directory schema, result schema, ladder configuration schema, helper catalogue, operator playbook) exists in the repository and is consistent with the code actually shipped on the feature branch.
- **SC-009**: The iteration history log correctly reflects 100% of executed iterations, with every line pointing to a commit hash and a run directory that both exist on disk and on the remote.
- **SC-010**: Out-of-scope failures, when they occur, are reported within the same iteration in which they are detected, and the loop halts — no out-of-scope failure is retried more than once without operator intervention.
- **SC-011**: The feature is considered complete when the bot has killed the commander on **both** the no-op rung **and** the first (easiest) competitive rung within the same feature branch's work. Beating additional competitive rungs is a bonus and does not gate completion; stalling on any rung beyond the first competitive one does not block completion either, provided the earlier rungs have been cleared.

## Assumptions

- The operator is an experienced developer (or AI developer) comfortable reading F# code, using git, and running shell commands; the trainer does not need a friendly interactive UI.
- A development machine has Beyond All Reason installed under its standard per-user data directory, including at least one engine version and the competitive opponent AI.
- The engine, its headless launch path, and the repository's existing client library for controlling a team are functional and trusted at the start of this feature; if they are not, the first iterations will be spent hardening them, which is explicitly considered in-scope work.
- Opponent AI difficulty profiles installed by the engine may require small patches to their configuration files (exposing hidden difficulty levels); patching engine-installed files via an in-repo installer is acceptable, and the patch is tracked in the repository source tree.
- The feature branch `020-bot-iterative-trainer` is the unit of delivery; all commits for this feature live on it and are pushed to GitHub. No pull request is opened as part of this workflow — final merge to the main branch is a separate, out-of-scope decision.
- Iteration is driven by a human or AI operator walking a documented playbook in a single long session; no autonomous scheduler is required.
- Run directories under the trainer's runs area are not committed to version control; only in-repo source, helpers, schemas, and documentation are committed.
- The helper library is the primary deliverable of the feature; bot wins against specific opponents are the forcing function that drives helper extraction, not the goal in themselves.
- Two or more iterations producing similar ad-hoc logic is sufficient justification to create a helper; there is no requirement to wait for three or more occurrences.
- The competitive opponent AI is treated as a black box — its internals are out of scope, and bugs inside it are out-of-scope failures.
- Map selection is fixed for the entire feature. Introducing a second map, varying the map per rung, or making map a runtime parameter are explicit out-of-scope extensions left to a future feature; consequently, the map-aware helpers developed during this feature may start concrete (grounded in the chosen map) and only later be generalised.
- The RNG seed is fixed for the entire feature. Introducing seed variation to test bot generalization (handling unseen game states, stochastic opponent behaviour) is an explicit out-of-scope extension left to a future feature. Bot iterations during this feature are therefore diagnostic rather than generalization-oriented: a loss is a bot or infrastructure problem, not bad luck.
