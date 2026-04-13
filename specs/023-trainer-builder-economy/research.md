# Phase 0 Research — 023 Builder-Economy Macro Bot

**Branch**: `023-trainer-builder-economy`
**Date**: 2026-04-13
**Purpose**: resolve unknowns that block Phase 1 design for the macro bot. Each entry states the Decision, Rationale, and Alternatives Considered, per the `/speckit.plan` research format. No `NEEDS CLARIFICATION` markers were introduced in the plan's Technical Context, so this file documents the *technical* unknowns the plan deferred to research rather than unclarified spec items (which were resolved in the spec's Clarifications section).

---

## R1 — Commander build-command primitive availability

**Unknown**: Can the bot issue `BuildCommand` for a structure at a concrete world position from `.fsx` using the existing `FSBar.Client` surface, or is a new command / callback required?

**Decision**: Reuse the existing `FSBar.Client.Commands.BuildCommand (unitId, toBuildUnitDefId, x, y, z, facing)`. No new command primitive is required in Phase 0. If mid-iteration a placement refuses for reasons the `.fsx` cannot diagnose (e.g. the proxy never renders the command to the engine), reclassify the iteration as `repo-bug` per the PLAYBOOK and fix the `FSBar.Client` side in the same commit, per 020 FR-015.

**Rationale**: `src/FSBar.Client/Commands.fs:24` and `Commands.fsi:9` already declare `BuildCommand` with the exact signature the macro bot needs. The existing rush bot (`bot.fsx`) proves the command round-trips to the engine by virtue of driving the match to commander-death wins on the no-op rung on this branch's starting commit — the command plumbing is live. Adding a parallel F# primitive would be pure duplication.

**Alternatives considered**:
- *Add a new `BuildStructureCommand` wrapper with default facing* — rejected; a default facing value is a helper-level concern (belongs in `opening_build.fsx`), not a new public primitive.
- *Use `GiveMeNewUnitCommand` to skip the build sequence entirely* — rejected; that is a cheat primitive and would violate the spirit of the iterative trainer, which exists to force the build-order helper into existence.

---

## R2 — UnitDef discovery for "metal extractor", "energy structure", "factory", and "advanced" variants

**Unknown**: How does the bot resolve a symbolic structure name (e.g. "cormex" / "corsolar" / "corlab" / "coralab") to a `unitDefId` at runtime, and how does it discover the tier-2 / advanced variants for the upgrade phase?

**Decision**: Use `FSBar.Client.UnitDefCache` (built from `Callbacks.getUnitDefs` + `getUnitDefName` + `getBuildOptions` + `getUnitDefCost`) to walk the commander's build options by name. The opening-build helper (`opening_build.fsx`) accepts a list of symbolic structure names and looks them up once at bot start against the cache, producing a list of `(defId, friendlyName, cost)` tuples. Advanced / tier-2 variants are discovered by walking `BuildOptions` of a tier-1 factory (t2 labs appear as build options of an advanced constructor) rather than hardcoding def-ids.

**Rationale**: `src/FSBar.Client/UnitDefCache.fsi:12` already exposes `BuildOptions: int array` per def, `Callbacks.fsi:80` exposes `getBuildOptions`, and `Callbacks.fsi:68`/`:74` expose `getUnitDef` / `getUnitDefName`. The rush bot's `bot.fsx` already uses `getUnitDefName` to disambiguate the enemy commander (see `bot.fsx:125`), so the callback is known to work on this branch. Hardcoded def-ids are a known-bad pattern because BAR content updates renumber defs; name-based resolution keeps the helper stable across engine versions.

**Alternatives considered**:
- *Hardcode numeric def-ids for Avalanche 3.4 + current engine* — rejected; brittle across engine bumps and hostile to SC-009 (a second operator writing an alternative bot using the same helpers).
- *Query `BarData` NuGet for canonical def-ids offline* — rejected for Phase 0; `BarData` is already referenced through the existing prelude but the extra indirection buys nothing over the live `UnitDefCache` the bot already loads. Revisit if an iteration surfaces a reason.

---

## R3 — Metal-spot discovery for opening-build placement

**Unknown**: How does the bot decide **where** on the map to place the two opening metal extractors?

**Decision**: Call `FSBar.Client.Callbacks.getMetalSpots` at bot start to obtain all candidate spots, filter to spots within a configurable radius of the commander's spawn position, and sort by distance. The opening-build helper iterates the sorted list, placing extractors at the first N reachable spots. If `getMetalSpots` returns an empty array (no metal map loaded) the iteration is classified as `out-of-scope` per the PLAYBOOK and the operator is asked whether to change maps (FR-018 forbids changing the map without a separate feature).

**Rationale**: `Callbacks.fsi:44` declares `getMetalSpots: stream: NetworkStream -> (float32 * float32 * float32 * float32) array` — the exact (x,y,z,metalAmount) tuple the helper needs. This is already live on the branch; no new callback is required.

**Alternatives considered**:
- *Sample a grid of candidate positions around the commander and ask the engine to validate each* — rejected; higher command volume, more latency, and it bypasses the engine's authoritative metal-spot list.
- *Precompute metal spots per map into a static JSON file* — rejected; adds a new artifact outside the feature's single-map scope and defeats SC-009 portability.

---

## R4 — Factory production-queue submission primitive

**Unknown**: Is there a "queue N of unit X on factory F" primitive on `FSBar.Client`, or does the bot submit repeated `BuildCommand` calls targeting the factory's unitId?

**Decision**: Submit one `BuildCommand (factoryUnitId, unitDefIdToBuild, 0.0f, 0.0f, 0.0f, 0)` per queued item. In BAR, `BuildCommand` issued to a factory with unit-def-id = the target unit enqueues one build on the factory's queue; the position and facing arguments are ignored. The production-queue helper keeps an internal model of "how many we asked for" vs. "how many `UnitFinished` events we observed" and tops up the queue whenever the gap drops below a configured threshold. **Do not** use a `CustomCommand` wrapper; the base `BuildCommand` is sufficient and is what the AI Interface expects.

**Rationale**: This is the standard AI Interface idiom for factory production in Spring/Recoil. `BuildCommand` in `Commands.fs:24` already passes `ToBuildUnitDefId` through; the command case is `BuildUnit` regardless of whether the issuing unit is a constructor (placing a structure) or a factory (enqueuing a unit). The rush bot never exercised this path, so the macro bot will be the first consumer and the first iteration that uses it should verify the `unwired_commands.json` (from 022) does not list it as unwired.

**Alternatives considered**:
- *New `FactoryProduceCommand` wrapper in `Commands.fs`* — rejected; identical wire encoding, pure duplication.
- *One large batched `CustomCommand` with a list of defIds* — rejected; no upstream engine support, and loses the per-item UnitFinished tracking the helper needs.

---

## R5 — Enemy-in-base detection surface (FR-016b)

**Unknown**: What query does the bot use to decide "enemy unit is inside my base radius" given it has no scouting?

**Decision**: Walk `client.GameState.Enemies` every frame and compute the 2D distance from each enemy's `Position` to the bot's configured base centre (derived once at bot start as the commander spawn position). If any distance is ≤ the configured base radius (initial value: 1200 game-units, exposed as a single constant in `bot_macro.fsx`), the enemy-in-base condition is true. This uses only the enemies already in `GameState.Enemies` — the set the proxy has surfaced via LOS / radar events — not any scouting query. The query deliberately does NOT call `getMetalSpots` or `MapQuery` beyond the initial spawn lookup; keeping the surface area small is the point of FR-016b.

**Rationale**: `GameState.Enemies` is already populated by `GameState.processEvent` (see `src/FSBar.Client/GameState.fs` — the rush bot depends on this). Distance-to-centre is one float32 sqrt per enemy per frame; at a realistic enemy count (< 100) this is free.

**Alternatives considered**:
- *Axis-aligned bounding box instead of radius* — rejected; radius is simpler and the "base" is visually round anyway.
- *Convex hull of bot's own buildings* — rejected; over-engineered for a feature whose entire enemy-awareness budget is one threshold.
- *Use pathing distance instead of straight-line distance* — rejected; pathing queries require a callback we would have to validate first, and the cheap Euclidean check is adequate for "is something in my face right now".

---

## R6 — Tier-2 milestone detection (FR-009 / FR-011)

**Unknown**: How does the bot *observe* that a tier-2 / upgrade milestone has been reached?

**Decision**: The upgrade-gate helper (`upgrade_gate.fsx`) exposes three named predicates that close over the `UnitDefCache` + `GameState.Units`:
1. `hasAdvancedConstructor gs cache` — true when any own unit's defId has a `unitDefName` prefix indicating an advanced / tier-2 constructor (name-based, via the cache).
2. `hasAdvancedFactory gs cache` — true when any own unit's defId resolves to an advanced factory (name-based).
3. `hasAdvancedCombatUnit gs cache` — true when any own unit's defId resolves to a tier-2 combat unit (name-based).

The bot declares the upgrade milestone reached when ANY of the three predicates is true for the first time in the match. The event is written to `phase_transitions.jsonl` with the predicate name that tripped, so the iteration log shows which path the bot actually took.

**Rationale**: Name-based detection sidesteps hardcoded def-ids (per R2). Three predicates instead of one gives the bot script freedom to choose whichever upgrade path the opening economy naturally supports; the iteration loop will reveal which one is most reliable on the current map, and later iterations may collapse to the most-common path.

**Alternatives considered**:
- *A single "any unit whose BuildOptions is a strict superset of the tier-1 factory's BuildOptions" heuristic* — rejected; clever, but opaque when it misfires and hard to diagnose from a run log.
- *A config file listing tier-2 names per faction* — rejected; adds an artifact we do not need yet and nothing else reads it. Revisit if the helper needs to support more than one faction.

---

## R7 — Phase-transition record format and write path

**Unknown**: Where does the phase-transition record live in the run directory, and who writes it?

**Decision**: One new file `phase_transitions.jsonl` in the run directory, one JSON object per line, written by the macro bot via a new `logPhaseTransition` function added to `bots/trainer/helpers/log.fsx`. The rush bot (`bot.fsx`) does NOT write this file — absence is the indicator that a given run came from the rush bot. The exact schema is defined in `contracts/phase-transition-record.md`. The file is optional in the run-directory conformance check (run.sh does not stub it) — the PLAYBOOK §12 addition notes that its absence for a macro-bot iteration is a bot-logic bug, not an infrastructure regression.

**Rationale**: Keeping phase transitions in a *separate* JSONL from `frames.jsonl` means the operator can diff two iterations' phase timelines by `diff`-ing a ~4-line file instead of grep-searching the 30fps frame log. It also keeps `log.fsx`'s existing `logFrame` API unchanged (Tier 2 discipline — no unnecessary surface-area churn on the existing rush bot).

**Alternatives considered**:
- *Add a `phase_transition` reason to existing `frames.jsonl`* — rejected; couples the macro bot's phase machinery to the rush bot's log schema, and makes the stall detector in PLAYBOOK §7 harder to reason about.
- *Write phase transitions into `result.json` as an array field* — rejected; `result.json` is written once at match end and could be lost if the bot crashes mid-match, losing the phase-transition record too.
- *Inline phase transitions into `stdout.log` only* — rejected; stdout is human-readable but not structured, and the PLAYBOOK already relies on JSONL for diffable telemetry.

---

## R8 — Run.sh selector for which bot to launch

**Unknown**: How does the runner pick between `bot.fsx` and `bot_macro.fsx` without duplicating the runner?

**Decision**: Introduce a `BOT_SCRIPT` environment variable (default: `bot.fsx`) read at the top of `run.sh`. Operators override it for macro-bot runs via `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh <rung> <iter>`. The runner snapshots whichever script it launched into `bot.fsx.snapshot` in the run directory (filename preserved as `bot.fsx.snapshot` for schema compatibility with 020 contracts; the snapshot's contents identify the bot). The branch guard is updated from `021-rerun-trainer-highbar` to `023-trainer-builder-economy`.

**Rationale**: One-line env-var switch keeps the runner minimal (the spirit of 020's "bash for orchestration only"). Both bots live in the same directory tree so the relative `#r` paths in `helpers/prelude.fsx` continue to work unchanged.

**Alternatives considered**:
- *A positional third argument to `run.sh`* — rejected; breaks existing invocations documented in PLAYBOOK §1.
- *Two parallel runners (`run.sh` + `run_macro.sh`)* — rejected; duplicates every trap/cleanup/log-copy step and violates FR-017's "no parallel trainer" rule.
- *Auto-detect based on rung name* — rejected; coupling bot choice to rung choice is arbitrary and prevents running both bots against the same rung for A/B comparison.

---

## R9 — Army-threshold counting (FR-013)

**Unknown**: How does the bot count "at least 12 combat units"?

**Decision**: The attack-launch helper (`attack_launch.fsx`) walks `GameState.Units`, resolves each unit's defId via `UnitDefCache`, classifies each as `combat` or `non-combat` by name prefix / category (e.g. excluding commanders, constructors, and structures), and returns the count. The threshold is exposed as `CombatUnitThreshold = 12` in one place at the top of `bot_macro.fsx`, satisfying FR-013's "tunable from a single place". Combat-unit classification belongs in the helper (not the bot) so the same function can be reused by later bots under SC-009.

**Rationale**: Name-based classification is consistent with R2/R6 and avoids hardcoded def-ids. The helper's return is a simple `int`, which the bot compares against the threshold; no helper-level boolean is returned so iterations can add more nuanced thresholds later (e.g. "12 combat units AND at least 2 advanced") without breaking callers.

**Alternatives considered**:
- *Store combat/non-combat classification on `TrackedUnit` at `GameState` level* — rejected; would require a `.fs` change and is a Tier 1 impact for a Tier 2 feature.
- *Use a `Set<string>` of combat-unit names hardcoded in the helper* — partially accepted; the helper starts with a small hardcoded allowlist of the generic combat-unit name stems it encounters on iteration 1, then grows from there under the extraction rule. This is consistent with the spec's guidance that the mix is left to operator iteration.

---

## Summary

All Phase 0 unknowns are resolved using existing `FSBar.Client` surface area. **No new `.fs`/`.fsi` changes are required up-front.** Any primitive gap that surfaces mid-iteration is handled as a `repo-bug` sub-change in the same commit, per the PLAYBOOK and 020 FR-015. Phase 1 design can proceed.
