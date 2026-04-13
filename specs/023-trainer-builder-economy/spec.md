# Feature Specification: Builder-Economy Bot via the Iterative Trainer

**Feature Branch**: `023-trainer-builder-economy`
**Created**: 2026-04-13
**Status**: Draft
**Input**: User description: "run the iterative trainer but use the commander to build structures. build units and economy and upgrade before crushing the enemy."

## Clarifications

### Session 2026-04-13

- Q: When is the "opening phase" considered complete (what event marks opening → production transition)? → A: When the **first factory finishes construction**. Mex and energy structures may still be under construction at that point — they finish on their own and do not gate the phase transition. The factory-completion event is the single observable engine signal that unblocks the production phase.
- Q: What is the army-composition threshold that gates the attack-launch decision (initial configured value)? → A: **At least 12 combat units AND the upgrade milestone reached**. 12 is large enough that no realistic rush could meet it accidentally (proves the macro phases ran) and small enough that a working macro bot can plausibly reach it within the inherited frame budget. Pairs with the upgrade gate so an attack at this threshold is unambiguously a "macro win", not a delayed rush.
- Q: Where does the macro bot live relative to the existing trainer bot from features 020/021/022? → A: **Lives alongside as a second bot file in `bots/trainer/`**. Both bots must remain runnable on every commit on this branch. This validates SC-009 (a second operator composing helpers into a new bot) and exercises the FR-023 backwards-compat rule under load — helper changes must keep both bots running.
- Q: How much enemy awareness does the bot need during the macro phases? → A: **Internal-threshold-driven with one override: "enemy in base → defend"**. Phase transitions are otherwise purely internal (no scouting, no wider perception). If any enemy unit is detected inside the bot's base radius, the bot MUST interrupt production to defend until the threat is gone, then resume the macro phase it was in. No scouting, no recon, no enemy-mix-aware reactions — keep the surface area small so the next iteration drives the defense and perception helpers out only when the competitive rung exposes the need.
- Q: What does the bot do when the upgrade-deadline frame budget is exceeded without the milestone reached? → A: **Drop the upgrade requirement and attack if the army threshold (12 units) is met; otherwise record a stall reason and end the run as a loss-by-stall**. Keeps every run ending in a real engine outcome (win/loss/timeout) for stronger iteration signal, while still blocking degenerate rushes — the bot can never attack without either (a) the upgrade reached or (b) the army threshold met after the upgrade deadline expired.

## Overview

This feature is a fresh bot-strategy run of the existing **Iterative Trainer** (the run → diagnose → improve → commit → push cycle established in feature 020 and exercised in 021/022). All match-execution infrastructure, the run-directory layout, the ladder, the playbook, the helper modules, the commit-and-push discipline, and the canonical win condition (kill the enemy commander) are inherited from those features and **reused unchanged** unless an iteration surfaces a defect.

What's new in this feature is the **target bot archetype**. Earlier trainer runs used minimal or rush-style bots whose only goal was getting to a commander kill. This feature drives the trainer toward a "macro" archetype:

1. The commander walks out and builds an opening base (metal extractors, energy, a factory).
2. The factory produces a sustained stream of constructors and combat units while economy compounds.
3. The bot **upgrades** — reaches at least one tier-2 milestone (advanced builder, advanced factory, or advanced units) before committing to a decisive attack.
4. Only after the economy and tech are in place does the bot assemble its forces and crush the enemy commander.

The forcing function is the same as in 020: each match exposes the missing helper, the missing perception query, or the missing production primitive, and those gaps are filled in helper modules so future bots can compose them. The deliverable is again **the helper library and the bot's progression**, not a one-off win.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Commander opens with an economy build (Priority: P1)

The bot's commander walks out from spawn and lays down the canonical opening: at least two metal extractors on nearby metal spots, at least two energy structures, and one bot/vehicle factory near the base. The commander does not idle, does not wander into combat, and does not skip directly to attacking. By the time the opening is finished, the bot has a positive metal and energy income and a factory ready to produce.

**Why this priority**: Without a working opening, none of the later phases can happen — there is no income to sustain production, no factory to produce from, and no tech base to upgrade. This is the foundation every subsequent iteration depends on.

**Independent Test**: Run a single trainer match against the no-op rung. Inspect the run directory's frame log: by some early frame budget the bot must have issued build commands for the expected opening structures, the structures must have completed (or be under construction with sufficient resources), and the telemetry summary must show non-zero metal extractors, energy structures, and at least one factory built.

**Acceptance Scenarios**:

1. **Given** a fresh match starts on the configured map, **When** the bot connects and the commander becomes available, **Then** within an early-game frame budget the bot issues build commands for at least two metal extractors, at least two energy structures, and at least one factory, and the run telemetry records each of these structures as built or under construction at termination.
2. **Given** the commander is partway through the opening build, **When** a structure completes, **Then** the bot reassigns the commander to the next item in the opening order without operator intervention and without leaving the commander idle for an extended idle-frame budget.
3. **Given** the commander cannot place a structure at the planned location (blocked, off-map, no resources), **When** the placement fails, **Then** the failure is captured in the run log with enough context to diagnose it, and the bot attempts the next viable position or the next item in the opening order rather than stalling.

---

### User Story 2 — Sustained production from the factory (Priority: P1)

Once a factory exists, the bot keeps it producing. Idle factories are treated as a defect to be fixed in the next iteration. The production stream includes both constructors (to extend the economy) and combat units (to build the eventual attack force). Economy and army size grow together over the match.

**Why this priority**: A built factory that sits idle is the most common rookie failure in macro RTS bots and is the single biggest signal that a production-loop helper is missing. This story exists to force that helper into existence.

**Independent Test**: Run a trainer match for at least the configured frame budget. Confirm from the per-frame log that the bot's first factory was producing units for the majority of frames after it completed (idle gap thresholds are documented in the run report), and that the terminal telemetry shows both constructor units and combat units built in non-trivial quantities.

**Acceptance Scenarios**:

1. **Given** a factory has finished construction, **When** the next frame is processed, **Then** the bot has an active build queue on that factory and the queue is replenished as items complete throughout the rest of the match.
2. **Given** the bot's metal or energy income is insufficient to sustain the current production queue, **When** the resource shortfall is detected, **Then** the bot prioritises additional economy structures (via constructors or the commander) before adding more combat units to the queue.
3. **Given** a constructor unit has been produced from the factory, **When** it leaves the factory, **Then** it is given productive work (more economy, repair, assist on the next factory or upgrade) rather than left idle.

---

### User Story 3 — Reach a tier-2 / upgrade milestone before the decisive attack (Priority: P2)

Before committing the army to a final attack, the bot reaches at least one upgrade milestone: an advanced constructor, an advanced factory, or an advanced unit type — whichever the helper library most naturally supports. The decision to attack is gated on this milestone being reached (or on a configurable fallback if the upgrade path is blocked for too long).

**Why this priority**: This is what differentiates a "macro" bot from a "build-some-stuff-then-charge" bot. It also forces the trainer to grow tech-tree-aware helpers that didn't exist before.

**Independent Test**: Run an iteration that survives long enough to reach mid-game. Inspect the run telemetry and confirm that at least one tier-2 / upgrade marker is recorded (advanced builder constructed, advanced factory constructed, or first advanced unit produced) before the bot's attack-launch decision frame.

**Acceptance Scenarios**:

1. **Given** the economy is producing the configured upgrade thresholds, **When** the upgrade trigger fires, **Then** the bot constructs the planned advanced structure or unit and records the upgrade event in the run log.
2. **Given** the bot has not reached the upgrade milestone by a configurable frame deadline, **When** the deadline passes, **Then** the bot either drops the upgrade requirement and proceeds to the attack phase with the units it has, or records a stall reason — the choice is documented and consistent across iterations.
3. **Given** the upgrade has been reached, **When** the attack-launch decision runs, **Then** the bot transitions into the attack phase and the run log records the transition (the frame, the army size, the economy state, and the upgrade reached).

---

### User Story 4 — Crush the enemy commander with the assembled force (Priority: P2)

With economy, production, and an upgrade in hand, the bot composes its army into a coordinated attack and drives it onto the enemy commander. The match ends with the engine-signalled commander-death win (the canonical win condition from 020). The bot does not commit early with insufficient force, and it does not refuse to commit after every threshold is met.

**Why this priority**: The commander kill is what closes the iteration loop on this archetype — without it, the macro phases never get tested under win-pressure. It depends on the previous three stories.

**Independent Test**: After enough iterations to make the previous stories work, run the trainer and observe an iteration whose terminal result record shows `outcome=win` with `cause` referencing engine-signalled commander death, on at least the no-op rung. The run's telemetry shows the army-vs-economy ratio at the moment of the attack-launch decision and confirms the attack was launched after the upgrade and economy thresholds were met.

**Acceptance Scenarios**:

1. **Given** the army has reached the configured composition and threshold, **When** the attack decision fires, **Then** the bot issues movement and engagement commands toward the enemy commander's last known location and the run log records the attack composition and the target.
2. **Given** the attack is in progress and the enemy commander dies, **When** the engine signals match termination, **Then** the trainer's terminal result record marks the outcome as a win with `cause` referencing commander death, conforming to the FR-011 contract from feature 020.
3. **Given** the attack fails (army wiped, commander not reached), **When** the match ends, **Then** the loss is classified by the operator into one of: insufficient army composition, attack mistimed, pathing failure, or out-of-scope, and the next iteration addresses the highest-impact category.

---

### User Story 5 — Drive new helpers out of the macro archetype (Priority: P1)

As the iterations work through stories 1–4, recurring patterns are extracted into helper modules — exactly per the rule from feature 020 (FR-019 through FR-024). The new archetype is expected to surface helpers the earlier rush-style trainer never needed: an opening-build order, a production-queue keeper, an idle-constructor dispatcher, a tech-upgrade gate, and an army-composition / attack-launch helper. Each helper goes in via an explicit extraction commit on this branch and is documented in the operator playbook.

**Why this priority**: The helper library is the **stated primary objective** of every iterative-trainer feature (per the 020 overview and SC-004). This story exists so the spec's success criteria explicitly include *which* helpers this feature is expected to surface, not just *how many*.

**Independent Test**: At the end of the feature, the helper modules contain at least the new helpers listed below, each with at least one bot consumer in-tree, each introduced in a single extraction commit on this branch, and each referenced by a section in the operator playbook.

**Acceptance Scenarios**:

1. **Given** two iterations have repeated the same opening-build sequence inline, **When** the second occurrence is recognised, **Then** the opening-build order is extracted into a helper, the bot is updated to call it, and both changes ship in the same commit.
2. **Given** the production-loop logic has appeared in two iterations, **When** it is extracted, **Then** it lives in a helper module whose interface accepts a factory identity and a queue policy, and the bot consumes it without inlining queue-management logic.
3. **Given** the upgrade-gate logic has appeared in two iterations, **When** it is extracted, **Then** it lives in a helper that exposes a single boolean predicate (or a small number of named predicates) the bot consults before transitioning phases.

---

### Edge Cases

- **Commander dies in early game before the economy is up**: The match ends in a loss with `cause=commander died`. The terminal result record must capture which opening-build step the commander was on, so the next iteration can decide whether to delay the commander's exposure or split the build differently.
- **No metal spots reachable from spawn**: The opening-build helper must surface the failure (no candidate positions within a configurable radius) rather than silently looping forever; the iteration is classified as either a map-knowledge gap (in-scope, fix the helper) or out-of-scope.
- **Factory completes but no resources to start production**: The bot must wait on resources rather than spamming failed orders, and the run log must show the wait period as a wait state, not as idle.
- **Upgrade structure begins construction but is destroyed before completion**: The bot must restart the upgrade if resources allow, or record the upgrade as "deferred" with the reason in the run log; it must not pretend the upgrade succeeded.
- **Army gathers but the chosen attack route is blocked or unsafe**: The attack helper must either choose an alternate approach or record the path failure for the next iteration; it must not commit units to an obviously failing pathfind.
- **No-op rung is cleared but the first competitive rung kills the commander before the upgrade phase**: This is the expected forcing function for the army-defense / map-awareness helpers, not a bug. The iteration loop classifies it normally and the next iteration addresses it.
- **An iteration regresses an earlier story** (e.g., refactoring the production helper breaks the opening-build): the run directory must surface the regression (an earlier-story acceptance scenario fails) and the operator must fix the regression in the same iteration before advancing.
- **Stall on the upgrade phase**: The 5-iteration stall detector inherited from feature 020 (FR-018) applies unchanged — five iterations on the same rung with no telemetry improvement halts the loop.

## Requirements *(mandatory)*

### Functional Requirements

#### Bot archetype — opening and economy

- **FR-001**: The bot MUST use the commander as its primary early-game builder and MUST issue an opening sequence of build commands that includes at least: two metal extractors, two energy structures, and one factory, before the bot transitions out of the opening phase.
- **FR-002**: The bot MUST keep the commander productive during the opening: idle frames for the commander above a configurable threshold MUST be recorded in the run log as a defect signal so the next iteration can address it.
- **FR-003**: The bot MUST detect placement failures (blocked terrain, out-of-resources, off-map, unit dead) for any commander build command and MUST either retry at an alternative position or move on to the next item in the opening order — it MUST NOT loop on a failing placement indefinitely.
- **FR-004**: The bot MUST treat the opening phase as complete on the frame the **first factory finishes construction**, and MUST record the opening → production transition in the run log on that frame. Other opening structures (metal extractors, energy structures) MAY still be under construction at the transition point — they finish on their own and do not gate the phase transition.

#### Bot archetype — production loop

- **FR-005**: Once a factory exists, the bot MUST maintain a non-empty production queue on that factory for the remainder of the match, except during explicit waits (resource shortfall, upgrade transition).
- **FR-006**: The bot's production policy MUST produce both constructor units (for further economy and assist) and combat units (for the eventual attack), and the ratio MUST be tunable from a single place in the bot script or helper module so iterations can adjust it without touching multiple call sites.
- **FR-007**: Constructor units leaving the factory MUST be assigned productive work (build economy, assist commander, assist factory, repair) rather than left idle. Idle constructor frames above a configurable threshold MUST be recorded in the run log as a defect signal.
- **FR-008**: When metal or energy income is insufficient to sustain the queue, the bot MUST prioritise additional economy structures over additional combat units, and MUST NOT spam build orders the economy cannot fulfil.

#### Bot archetype — upgrade phase

- **FR-009**: The bot MUST reach at least one tier-2 / upgrade milestone (advanced constructor, advanced factory, or advanced unit) before launching its decisive attack, unless the configured upgrade-deadline frame budget is exceeded.
- **FR-010**: The upgrade trigger MUST be expressed as a small set of testable predicates (e.g., metal income ≥ X, total economy structures ≥ Y, no enemy in base) and MUST live in a helper module after the second iteration in which it appears, per the extraction rule from feature 020.
- **FR-011**: The bot MUST record the upgrade-reached event in the run log, including the frame, the upgrade type, and the economy state at that frame.
- **FR-012**: If the upgrade-deadline frame budget is exceeded without the milestone being reached, the bot MUST evaluate the army-composition threshold (FR-013, initial value 12 combat units): (a) if the army threshold is met, the bot MUST drop the upgrade requirement and launch the attack phase, recording the deadline-fallback decision in the run log; (b) if the army threshold is **not** met, the bot MUST record a stall reason in the run log and end the run as a loss-by-stall (the engine match terminates on its own at the inherited frame limit). The bot MUST NOT attack with neither the upgrade reached nor the army threshold met.

#### Bot archetype — decisive attack

- **FR-013**: The bot MUST gate the attack-launch decision on the upgrade milestone (or the documented fallback) AND on an army-composition threshold of **at least 12 combat units**. The threshold MUST be tunable from a single place in the bot script or helper module so iterations can adjust it, but the initial configured value for this feature is 12. The army-composition mix (specific unit types) is left to operator iteration.
- **FR-014**: When the attack launches, the bot MUST issue coordinated movement and engagement commands toward the enemy commander's last known position, and MUST record the attack composition, the target, and the launch frame in the run log.
- **FR-015**: The match termination MUST flow from the engine-signalled commander-death win (inheriting FR-011 from feature 020) — the bot MUST NOT fabricate an alternative win condition.
- **FR-016**: If the attack fails (army wiped, commander unreachable), the loss MUST be classified by the operator into one of: insufficient army composition, attack mistimed, pathing failure, upgrade still missing, or out-of-scope, and the classification MUST be captured in the iteration history log so the trainer's history shows the dominant failure mode over time.

#### Bot archetype — enemy awareness

- **FR-016a**: The bot's phase transitions (opening → production → upgrade → attack) MUST be driven by **internal predicates only** (own economy, own army, own structures). The bot MUST NOT scout the enemy, MUST NOT track enemy unit composition, and MUST NOT alter its production mix or upgrade timing based on what it sees of the enemy.
- **FR-016b**: The bot MUST react to one and only one enemy-aware condition during the macro phases: **enemy unit detected inside the bot's base radius**. When this condition is true, the bot MUST interrupt production to defend (engage detected enemy units with whatever forces are available, including the commander if no other units exist) until no enemy units remain inside the base radius, then resume the macro phase it was in. The base radius MUST be tunable from a single place. No other reactive behavior is in scope for this feature; broader perception/defense logic is the explicit forcing function for the *next* feature, not this one.

#### Trainer infrastructure (inherited, reused unchanged)

- **FR-017**: This feature MUST reuse the existing trainer match-execution infrastructure from features 020/021/022 — the run directory layout, the metadata schema, the frame log schema, the terminal result record schema, the engine launch path, the cleanup guarantees, the iteration history log, the stall detector, the ladder, the playbook, the commit-and-push discipline. It MUST NOT introduce a parallel trainer or a divergent run schema.
- **FR-018**: This feature MUST reuse the same fixed map and the same fixed RNG seed established in feature 020 (FR-001 and FR-003 of that feature). Changing either is explicitly out of scope and would require a separate feature.
- **FR-019**: This feature MUST reuse the existing ladder rungs (no-op rung plus the existing competitive rungs) without reordering or removing them. Adding rungs is permitted only if an iteration produces evidence that a new rung is needed, and any added rung MUST be added through a single commit that updates the ladder configuration and the playbook.
- **FR-020**: All work for this feature MUST happen on the feature branch `023-trainer-builder-economy`; every meaningful change (bot edit, helper extraction, infrastructure fix, documentation update) MUST be committed individually and pushed to the remote (`origin`) per the discipline established in feature 020 (FR-025 through FR-029).

#### Helper library growth (primary objective)

- **FR-021**: The helper library MUST gain at least the following new helpers over the course of this feature, each introduced via an explicit extraction commit after the second iteration in which the pattern appears: an **opening-build order** helper, a **production-queue keeper** helper, an **idle-constructor dispatcher** helper, an **upgrade-gate** helper, and an **army-composition / attack-launch** helper. The exact module names and module organisation are left to the operator's judgement in iteration, but each helper MUST have at least one bot consumer in-tree at the end of the feature.
- **FR-022**: The bot script MUST consume helpers rather than inlining their logic; helpers from earlier features (logging, perception, tactics) MUST be reused where applicable rather than duplicated. The macro bot MUST live as a **second bot file alongside the existing trainer bot in `bots/trainer/`** (the existing bot is not replaced or moved). Both bots MUST remain runnable on every commit on this feature branch — any helper-interface change that would break the existing bot MUST update the existing bot in the same commit, per FR-023.
- **FR-023**: Any change to a helper interface introduced in this feature MUST be backwards-compatible across all bots in-tree, or all affected bots MUST be updated in the same commit (inherited from feature 020 FR-023).
- **FR-024**: The operator playbook MUST be updated to describe the macro archetype's phases (opening → production → upgrade → attack) and to point a future bot author at the new helpers, so the accumulated library is discoverable.

### Key Entities

- **Macro archetype bot**: A trainer bot whose decision logic is organised as four explicit phases — opening, production, upgrade, attack — each gated on testable predicates expressed via helper modules. The bot for this feature.
- **Opening-build order**: A configured sequence of structure types and target positions the commander walks through at game start. Surfaced as a helper module after the second iteration in which it appears.
- **Production queue policy**: The rule that decides what the factory builds next, given current economy, current army composition, and current phase. Surfaced as a helper module under the same extraction rule.
- **Upgrade gate**: A small set of named predicates the bot consults to decide whether the upgrade milestone has been reached and whether the attack phase is unlocked. Surfaced as a helper module under the same extraction rule.
- **Attack-launch decision**: The frame at which the bot transitions from production+upgrade into the decisive attack phase, recorded in the run log along with the army composition, economy state, and target.
- **Phase transition record**: A run-log entry marking the bot's transition between phases (opening → production → upgrade → attack), captured for every iteration so the iteration history shows when each phase started and how long it lasted.

(All other entities — Iteration, Rung, Run directory, Terminal result record, Helper module, Playbook, Iteration history log, Out-of-scope report — are inherited unchanged from feature 020 and are not redefined here.)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A single trainer iteration of the macro bot against the no-op rung produces a run directory whose telemetry shows at least two metal extractors, at least two energy structures, and at least one factory built or under construction at termination, on **all** iterations after the opening-build order helper has been extracted.
- **SC-002**: After the production-loop helper has been extracted, the bot's first factory is producing for at least 80% of the frames between its completion and either the bot's loss or the bot's attack-launch decision, measured from the per-frame log.
- **SC-003**: At least one trainer iteration in this feature produces a run telemetry entry showing a tier-2 / upgrade milestone reached (advanced constructor built, advanced factory built, or first advanced unit produced) before the attack-launch decision frame.
- **SC-004**: At least one trainer iteration in this feature produces an engine-signalled commander-death win against the **no-op rung**, with the attack-launch decision having fired only after the upgrade milestone was reached AND the bot had produced at least 12 combat units (i.e. not a degenerate rush).
- **SC-005**: The first competitive rung is attempted under the macro archetype, and the iteration history records the dominant failure mode for it (insufficient army, attack mistimed, pathing, upgrade missing, out-of-scope). A win on the first competitive rung is a bonus but is **not** required for feature completion.
- **SC-006**: At the end of the feature, the helper library contains the five new helpers listed in FR-021, each with at least one bot consumer in-tree, each introduced via a dedicated extraction commit on this feature branch, and each referenced from the updated operator playbook.
- **SC-007**: Every iteration of this feature has a corresponding commit (or commit group) on the feature branch, every such commit has been pushed to `origin`, and the iteration history log on disk matches the commit history with no orphaned iterations and no unpushed commits at iteration boundaries.
- **SC-008**: No more than 10% of iterations in this feature are lost to infrastructure regressions (a run that fails to produce a conformant run directory for reasons unrelated to the bot's own decisions), inheriting the SC-007 budget from feature 020.
- **SC-009**: A second operator (or a fresh session), reading only the updated playbook and the helper library, can describe the macro archetype's four phases, point at the helper that gates each phase transition, and write a minimal alternative bot that reuses at least three of the five new helpers without modifying them. Time budget: under one hour.
- **SC-010**: The feature is considered complete when (a) the macro bot has produced an engine-signalled commander-death win against the no-op rung after reaching the upgrade milestone, **and** (b) the five helpers from FR-021 are in-tree, used by the bot, and documented in the playbook. Wins on competitive rungs are a bonus and do not gate completion.

## Assumptions

- The trainer infrastructure from features 020/021/022 is in working order on this branch's starting commit. If an early iteration uncovers an infrastructure regression, fixing it is in-scope work for this feature, exactly per the spirit of feature 020.
- The fixed map and fixed RNG seed configured in feature 020 are still appropriate for a macro bot — i.e. the map has reachable metal spots near the spawn, enough buildable terrain for an opening base, and a path the eventual attack can take to the enemy commander. If the map turns out to be hostile to the macro archetype (e.g. no metal spots, no buildable terrain), changing it is out of scope and the feature halts pending operator decision.
- The existing competitive opponent AI on the first competitive rung does not rush so aggressively that the macro bot has no chance to reach the upgrade phase. If it does, this is a forcing function for a defense helper, not a reason to lower the upgrade requirement.
- The engine and the in-repo client library expose enough information for the bot to issue commander build commands, factory production commands, advanced-unit production commands, and movement/attack commands. If a needed primitive is missing on the client side, fixing it is in-scope (per feature 020 FR-015) and the fix lands as a commit on this branch.
- "Upgrade" is interpreted as a tier-2 / advanced-tech milestone in the existing game's tech tree — the exact unit/structure chosen is left to the operator's iteration, since the helper library does not yet encode tech-tree knowledge and growing it is part of the work.
- "Crushing the enemy" is interpreted as the canonical commander-death win (per feature 020 FR-011); the bot does not invent alternative win conditions.
- This feature does not attempt to harden the trainer infrastructure beyond what iterations naturally surface; broader infrastructure work is left to dedicated features.
- This feature is a continuation of the **Iterative Trainer** named in project memory; "iterative trainer" is the established name for the underlying run → diagnose → improve → commit → push cycle and is referenced by that name in commits and reports for this feature.
