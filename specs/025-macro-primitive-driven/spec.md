# Feature Specification: Macro Bot Primitive-Driven Command Path

**Feature Branch**: `025-macro-primitive-driven`
**Created**: 2026-04-14
**Status**: Draft
**Input**: User description: "Deep integration of 024 tactical primitives into bot_macro.fsx command path: BasePlan drives opening BuildCommand emission, Pathing.findPath drives per-waypoint attack routing per combat unit, defend interrupt filters to combat units only, MapGrid loaded without warmup catch-up OOM, resolvePlan uses real terrain (not synthetic skeleton). Completes the US5 intent of feature 024 that shipped as observability-only."

## Context

Feature `024-tactical-map-primitives` shipped five new Tier-1 FSBar.Client modules (`Pathing`, `SmfParser`, `Chokepoints`, `WallIn`, `BasePlan`) and integrated them into `bots/trainer/bot_macro.fsx` at the observability layer. The macro bot prints `[plan] resolved 5 slots`, `[chokepoint] loaded 3 from cache`, `[attack] findPath skipped (no MapGrid)` at warmup, and routes the defend interrupt to the nearest pinned chokepoint. This demonstrates the primitives work and the offline cache pipeline is correct.

What 024 **did not** land — despite the spec's US5 describing it as "deep single-pass refactor replacing hardcoded logic in one commit" — is the primitives actually driving the bot's command path. The opening phase still issues `BuildCommand`s from the 023 `opening_build.fsx` helper's hardcoded offsets; attack launch still emits a single `MoveCommand` per combat unit to the target position with no route planning; the defend interrupt routes every finished unit (including workers) to the chokepoint; and `BasePlan.resolvePlan` runs against a synthetic MapGrid skeleton because loading a real one at warmup trips the socket-backpressure OOM that blocked the first live-iteration attempts.

This feature closes those gaps. It is a **behaviour-preserving refactor**: the bot must continue to win cleanly on NullAI after the primitives take over the command path. The clean-win commit that ships this feature is the first one where the 024 primitives are load-bearing rather than decorative.

## Clarifications

### Session 2026-04-14

- Q: FR-008 mandates N `MoveCommand`s per combat unit for waypoint traversal, but the existing `FSBar.Client.Commands.MoveCommand` only sets `INTERNAL_ORDER` (bit 3), not the `SHIFT_KEY` queue bit — so back-to-back commands replace rather than queue, and only the last waypoint would be acted on. → A: Extend `FSBar.Client.Commands` with a queued MoveCommand variant (OR'ing in `SHIFT_KEY = 32u`); FR-008 issues the first waypoint unqueued (replaces any existing order) and remaining waypoints queued.
- Q: FR-014 mandates a real MapGrid at warmup, but the "no cache file" Edge Case allows graceful degradation to the 024 synthetic skeleton. Which wins on cache-miss? → A: Hard-fail only if the map under test is in the 025 target set (Avalanche 3.4) AND no cache exists; otherwise log `[cache-miss]` and degrade to synthetic skeleton with 024-partial behaviour. Targeted-map maintainability overrides the edge-case fallback.
- Q: `AttackPathCache` invalidation is specified only on "Attack phase ends or bot picks a new target" — what happens if the cached target dies mid-route while combat units are still traversing waypoints? → A: On target death (target-id absent from `GameState.Units`), invalidate the cache and immediately re-run `findPath` to the next `pickAttackTarget` result in the same tick. One frame of stale waypoints worst-case; reuses existing target-selection logic.
- Q: FR-001 mandates resolvePlan "at the start of each Opening-phase tick" but never pins the cadence of "tick" — is it every game frame, every tactics tick, or event-driven? → A: One resolvePlan call per tactics tick (≈30 game frames / ~1 sim-second), aligned with the existing `bot_macro.fsx` decision cadence. ~60 calls over the full Opening phase, staying well under any realistic budget.
- Q: FR-011 specifies issuing partial waypoints when `findPath` returns `Status = Partial true`, but not whether the bot retries `findPath` with a larger budget on subsequent ticks — what happens after partial? → A: No retry: the partial path is final for the current attack launch. Rely on FR-009a (target death) or the next Attack phase to re-path. Bounds tactics-tick latency, accepts stranded units at the last waypoint as the cost of a simple invariant, and matches 025's "behaviour-preserving refactor" framing (today's bot has no path planning at all).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - BasePlan drives opening command emission (Priority: P1)

The macro bot's Opening phase issues `BuildCommand`s derived from `BasePlan.resolvePlan`'s `ResolvedSlot` positions, not from the 023 `opening_build.fsx` helper's hardcoded offset list. If `BasePlan.defaultArmadaOpening` is edited — adding a slot, changing a clearance margin, switching a position chooser — the bot's opening behaviour changes accordingly on the next iteration, without any edit to `bot_macro.fsx` or `opening_build.fsx`.

**Why this priority**: This is the core spec-intent of 024's US5. Without it, the primitives are documentation-only and any future plan change has to be made in two places. P1 because every other US here depends on the same resolver loop being present in the command path.

**Independent Test**: Edit `defaultArmadaOpening` to add a sixth slot (e.g., `NearBaseCentre(0, -200)` for an `armllt` defensive turret). Run the macro smoke on NullAI. Post-run `frames.jsonl` should show a `UnitFinished armllt` event and the result should still be a clean win. No edit to `bot_macro.fsx` should be required.

**Acceptance Scenarios**:

1. **Given** the macro bot enters the Opening phase with `defaultArmadaOpening` as the active plan, **When** it decides the next structure to place, **Then** the `BuildCommand` it issues references the exact `(x, z)` from the corresponding `ResolvedSlot.Position`, not the 023 helper's offset table.
2. **Given** a plan slot's `Failure` is `Some (WouldWallIn names)` at warmup resolution, **When** the bot would otherwise issue that slot's `BuildCommand`, **Then** the slot is skipped and a `[wall-in-defect] proposed=<slot> cuts off <names>` trace fires.
3. **Given** a slot resolves successfully at warmup but a mid-game event (commander moved, another structure built in the collision radius) would invalidate the clearance check, **When** the bot re-resolves the plan before issuing the next `BuildCommand`, **Then** the updated resolution reflects the current `ResolveContext.ExistingStructures`.

---

### User Story 2 - Pathing.findPath drives attack routing (Priority: P1)

When the macro bot enters the Attack phase, each combat unit receives a sequence of `MoveCommand`s corresponding to the waypoints of a `Pathing.findPath` result from its current position to the attack target. A single unit approaching an enemy commander across a ridge map receives multiple move commands that trace around the ridge; a unit on open terrain receives the same small number of commands the existing attack launcher produces today (one or two).

**Why this priority**: FR-029 of 024 mandated "one `MoveCommand` per waypoint per combat unit." The current implementation issues one `MoveCommand` to the target with no route, so any ridge or impassable terrain between the attack group and the enemy wastes units that would otherwise reach the commander. On Avalanche from Player-1 start, `findPath` returns 3 waypoints for the canonical `(500, 397)` → `(3699, 3601)` route — meaning a direct-move approach wastes a non-trivial chunk of the unit pool to terrain pathfinding failures handled engine-side.

**Independent Test**: Run the macro smoke on NullAI. Confirm the bot wins cleanly (FR-030 invariant) and that `grep "MoveCommand" frames.jsonl | wc -l` is greater than the current baseline (currently 12, one per combat unit). The new count should be approximately `12 × waypoint_count` for the attack launch, observable in the run's `unwired_commands.json` as a burst of MoveCommand entries all tagged with distinct (x, z) coordinates matching the waypoint list.

**Acceptance Scenarios**:

1. **Given** the attack launcher fires with 12 combat units and a `findPath` result of 3 waypoints, **When** the bot emits the per-unit command sequence, **Then** 36 `MoveCommand`s land in the frame's command list (12 units × 3 waypoints) with distinct (x, z) targets per waypoint.
2. **Given** `findPath` returns `Result.Error NoRoute` because the target cell is unreachable, **When** the attack launcher would otherwise fire, **Then** the bot falls back to a single `MoveCommand` to the target (current behaviour) and logs `[attack] findPath NoRoute — falling back to direct move`.
3. **Given** `findPath` returns `Ok { Status = Partial true }` because the wall-clock budget expired, **When** the bot issues commands, **Then** it issues the partial waypoints anyway and logs the status so post-run analysis can tell budget-exhausted routes apart from complete ones.

---

### User Story 3 - Defend interrupt filters to combat units (Priority: P2)

When the defend interrupt triggers (enemy in base radius), the bot routes only combat-classified units (`isCombatDef` returns `true`) to the nearest chokepoint's `Position`. Workers, constructors, and the commander itself continue whatever they were doing — building, guarding, repairing — instead of abandoning their tasks to run to a canyon they cannot defend.

**Why this priority**: The 024 US5 partial commit (5ed9ca6) introduced the chokepoint routing but used `client.GameState.Units |> Seq.filter (fun (_, u) -> u.IsFinished)` as the target-unit filter — every finished unit, not every combat unit. It was flagged in the feature-complete hand-off as a latent bug and never fixed. The 024 clean-win run did not exercise defend, so the bug has never fired in anger. P2 because the live risk is real but only on rungs with aggressive raiders (BARb/dev), not the current NullAI smoke.

**Independent Test**: Set up a synthetic defend scenario — spawn a BARb raid early via a test config — and verify that the `[defend] routing` trace lists only unit ids that pass `Attack_launch.isCombatDef`. Workers and the commander MUST NOT appear in that list.

**Acceptance Scenarios**:

1. **Given** the defend interrupt fires with 5 combat units and 3 workers finished, **When** the bot issues the chokepoint-intercept `MoveCommand`s, **Then** exactly 5 commands fire — one per combat unit — and no worker id appears in the command list.
2. **Given** the bot has zero combat units when the defend interrupt fires, **When** the bot would otherwise issue chokepoint routes, **Then** the bot falls back to the 023 nearest-enemy `AttackCommand` with the commander as the sole defender (current fallback) and logs `[defend] no combat units available — commander fallback`.

---

### User Story 4 - Real MapGrid at runtime without warmup catch-up OOM (Priority: P2)

The macro bot's `BasePlan.resolvePlan` call uses a real `MapGrid` with correct heightmap, slope map, and resource map, not the synthetic skeleton (all-zero slope, constant-y heightmap) the 024 partial shipped. Warmup still completes fast enough that the engine does not race ahead of the bot's frame-reading path and trip `Socket not writable, dropping frame` → Lua OOM.

**Why this priority**: US1 and US2 above depend on this. `BasePlan.resolvePlan` on the synthetic skeleton cannot detect a slot placed on a cliff or in water because the slope map is all zeros — every slot passes the terrain check. `Pathing.findPath` for attack routing needs a real passability grid or it cannot compute a meaningful route. Both US1 and US2 would silently degrade without this. P2 because US1/US2 are still observable (the wrong route / the wrong slot position) without this, they just fail more gracefully.

**Independent Test**: Warmup completes in under 100 ms of CPU work on the bot side (measured via a `Stopwatch` around the US5 block) and the first `WaitFrames 1` tick in the trainer main loop reads a frame whose number is within 1000 game frames of the last warmup tick. Bot-frame-count vs engine-frame-count divergence stays bounded.

**Acceptance Scenarios**:

1. **Given** the bot enters warmup with a cached map analysis file present, **When** it loads the pinned chokepoints and prepares the `ResolveContext`, **Then** the `Grid` field of the context is a real `MapGrid` whose `SlopeMap` contains the live map's slope values (loaded from a cache that includes the grid, OR loaded live with frame-reading interleaved to prevent backpressure).
2. **Given** the bot finishes warmup and enters the main loop, **When** the first live frame arrives, **Then** its frame number is within 1000 of the last warmup-tick frame number, and `engine.infolog` contains no `Socket not writable, dropping frame` lines during the warmup window.
3. **Given** a plan slot positioned deliberately on a water cell (commander-relative offset that lands in Avalanche's central lake), **When** `resolvePlan` runs at warmup, **Then** the slot resolves to `Failure = Some (TerrainNotBuildable "water depth X.Y")`, not to a successful position.

---

### User Story 5 - Macro bot preserves its clean win on NullAI (Priority: P1 invariant)

After US1, US2, US3, and US4 land in a single atomic commit (per 023 PLAYBOOK §2c "one fix per iter" discipline — this whole feature is one fix), `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-smoke` must still produce `result.json.cause = "commander-death-win-after-upgrade"` on the first iteration.

**Why this priority**: This is the spec's FR-030 invariant carried forward from 024. The point of the whole refactor is that the bot keeps doing what it does today — the primitives are a cleaner implementation of the same behaviour, not new behaviour. If the refactor regresses the clean win, the refactor is wrong.

**Independent Test**: Single `run.sh` invocation. Pass/fail is binary.

**Acceptance Scenarios**:

1. **Given** the 025 branch at HEAD after all four user stories are integrated, **When** `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-smoke` completes, **Then** `result.json.cause = "commander-death-win-after-upgrade"` and `result.json.victory_signal = "engine-shutdown-gameover"`.
2. **Given** the 025 branch at HEAD, **When** `bash bots/trainer/run.sh NullAI 025-rush-smoke` runs the unchanged rush bot, **Then** `result.json.outcome = "win"` at approximately f=12500 (matching the 024 rush smoke baseline within 5%). FR-030 preserved.
3. **Given** the 025 iteration window for SC-007 fires, **When** the first macro iteration regresses (no clean win), **Then** iterations follow the 023 PLAYBOOK §2c "one fix per iter" rule. Budget: 3 iterations. If 3 iters don't clear, halt and file a mailbox per PLAYBOOK §10.

---

### Edge Cases

- **Mid-game plan re-resolution when a structure is destroyed by enemy fire.** The bot re-computes `ResolveContext.ExistingStructures` from live `GameState.Units` before each tick's resolvePlan call, so a destroyed solar panel's slot can be re-issued as a replacement. Today's synthetic context never updates existing structures.
- **`findPath` during Attack phase takes longer than the bot's tactics-tick budget.** Current `defaultPathBudget` is 50 ms; if 12 units each call `findPath` that's 600 ms of contention. Mitigation: cache the path once per target per tick, have all combat units follow the cached waypoint list. Or bump the per-call budget for the first attack-launch tick only.
- **Enemy structures under LOS become new `ownStructures` from the pathing perspective.** Not in scope for 025 — enemy structures are not masked into the passability grid. `ownStructures` stays what its name says.
- **The cached MapGrid is stale relative to the current map content version.** `scripts/examples/14-cache-map-analysis.fsx` writes `generatedAtUtc` and the `SourceArchive`; the bot logs these at load time so post-run analysis can spot a stale cache. No automatic rejection.
- **The bot is assigned to a map that has no cache file.** If the map is in the 025 target set (currently Avalanche 3.4), warmup aborts with a clear error instructing the operator to run `scripts/examples/14-cache-map-analysis.fsx`. If the map is outside the target set, the bot falls back to the 024 partial's behaviour: empty `pinnedChokepoints`, synthetic `MapGrid` skeleton for `resolvePlan`, synthetic `findPath` with a pathless direct-move fallback in Attack. The `[cache-miss]` trace is loud so the operator sees it in the run stdout.
- **Defend interrupt fires during Upgrade phase when combat units exist but are factory-guarding.** Combat units are still combat-classified; they abandon the guard and head to the chokepoint. That is the intended behaviour — defense trumps production queue stability.

## Requirements *(mandatory)*

### Functional Requirements

**Opening-phase command emission (US1)**:

- **FR-001**: The macro bot MUST call `BasePlan.resolvePlan BasePlan.defaultArmadaOpening context` at warmup and at the start of each Opening-phase **tactics tick** (aligned with the existing `bot_macro.fsx` decision cadence: approximately once per 30 game frames / ~1 sim-second), not just at warmup for observability. Event-driven re-resolution (e.g., on `UnitDestroyed`) is not required — the tactics-tick cadence catches structure changes within one sim-second.
- **FR-002**: For each `ResolvedSlot` where `BuildableNow = true` and whose `Slot.Name` is not yet in `progress.ConsumedSlots`, the bot MUST issue a `BuildCommand` whose `(x, y, z)` matches the slot's `Position` exactly.
- **FR-003**: When a slot's `BuildCommand` is issued, the bot MUST call `BasePlan.markInFlight` on the current `PlanProgress` and persist the updated `PlanProgress` for the next tick.
- **FR-004**: When a `UnitFinished` event matches a slot's `DefName`, the bot MUST call `BasePlan.markConsumed` on the current `PlanProgress`.
- **FR-005**: The bot MUST NOT fall back to the 023 `opening_build.nextOpeningCommand` code path when `BasePlan.resolvePlan` is available. If resolvePlan itself fails (exception, not a `Failure` result), the bot MAY fall back once and log `[plan] resolvePlan exception — falling back to 023 helper: <msg>`.
- **FR-006**: The `bots/trainer/helpers/opening_build.fsx` helper MUST remain in-tree and continue to compile. The rush bot (`bot.fsx`) does not consume it today; the macro bot consumes it only on the exception-fallback path. The file is not deleted.

**Attack-phase command emission (US2)**:

- **FR-007**: On the first Attack-phase tick the bot MUST call `Pathing.findPath` once with a `ResolveContext`-style tuple (the live `MapGrid`, the combat group's current centre-of-mass as start, the attack target as goal, `ownStructures = <own finished structures as footprints>`) and cache the resulting `Path.Waypoints`.
- **FR-008**: For each combat unit in the launch set, the bot MUST issue `Path.Waypoints.Length` `MoveCommand`s, one per waypoint, in order. Per unit, commands are issued back-to-back in a single frame's `FrameResponse`. The first waypoint's `MoveCommand` MUST be issued unqueued (replaces any existing order); each subsequent waypoint MUST be issued with the engine's `SHIFT_KEY` queue bit (`32u`) set so the waypoints append to the unit's order queue rather than overwrite.
- **FR-008a**: `FSBar.Client.Commands` MUST expose a queued MoveCommand variant (e.g., `MoveCommandQueued` or `MoveCommand` with a `queue:bool` parameter) that OR's `SHIFT_KEY = 32u` into `Options` alongside `INTERNAL_ORDER`. This is a pre-024 module, so editing it does not violate FR-021.
- **FR-009**: Combat units joining the attack on subsequent ticks (production queue spawning new peewees into an already-launched attack) MUST receive the same cached waypoint list — no new `findPath` call per unit.
- **FR-009a**: When the cached attack target's unit id is no longer present in `GameState.Units` (target died or out of LOS and despawned), the bot MUST invalidate `AttackPathCache`, run `pickAttackTarget` to pick a new target, and immediately re-run `Pathing.findPath` to that new target within the same tactics tick. Per-unit waypoint emission uses the new cached path starting that tick.
- **FR-010**: If `findPath` returns `Result.Error NoRoute`, the bot MUST log `[attack] findPath NoRoute — falling back to direct move` and emit a single `MoveCommand` to the target for each combat unit (the 024 partial's behaviour).
- **FR-011**: If `findPath` returns `Result.Ok { Status = Partial true }`, the bot MUST issue the partial waypoints as-is and log `[attack] path waypoints=N cost=C status=Partial budget-exhausted`. The partial path is final for the current attack launch — the bot MUST NOT retry `findPath` with a larger budget on subsequent ticks. Re-pathing happens only via FR-009a (target death / despawn) or when the Attack phase ends and a new one begins. Stranded units at the last waypoint are an accepted cost; this invariant keeps tactics-tick latency bounded.

**Defend-interrupt filtering (US3)**:

- **FR-012**: When the defend interrupt routes combat units to a chokepoint, the bot MUST filter the target-unit set through `Attack_launch.isCombatDef` (the same classifier used by the attack launcher). Units for which `isCombatDef` returns `false` MUST NOT appear in the command list.
- **FR-013**: If the filtered combat-unit set is empty, the bot MUST log `[defend] no combat units available — commander fallback` and fall through to the 023 nearest-enemy `AttackCommand` with the commander as defender.

**Real MapGrid at runtime (US4)**:

- **FR-014**: When the active map is in the 025 target set (currently: Avalanche 3.4), the macro bot MUST have a real `MapGrid` available at warmup — not the synthetic skeleton. The `MapGrid.SlopeMap` MUST contain non-zero slope values where the real map has slope; the `MapGrid.HeightMap` MUST reflect the real heightmap. If the target map has no cache file AND inline re-parse is not enabled, warmup MUST abort with a clear error instructing the operator to run `scripts/examples/14-cache-map-analysis.fsx`. For maps outside the 025 target set, the bot MUST log a loud `[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx` trace and degrade to the synthetic skeleton (preserving 024 partial behaviour, not failing warmup).
- **FR-015**: The warmup total CPU budget (measured via a `Stopwatch` around the US5 block and the `MapGrid` load) MUST be under 100 ms. The offline cache script MAY be extended to serialise a compressed `MapGrid` alongside the chokepoint list, or the bot MAY re-parse the `.sd7` via `SmfParser` at warmup if measurement shows that is fast enough.
- **FR-016**: During warmup, the engine frame counter in `engine.infolog` MUST NOT advance by more than 1000 game frames between the first `[trainer] BarClient connected` line and the first `[trainer] entering main frame loop` line. Exceeding this threshold is a regression on US4.
- **FR-017**: `engine.infolog` MUST contain zero `Socket not writable, dropping frame` lines during the warmup window (from socket-open to first main-loop tick).

**Invariants (US5)**:

- **FR-018**: The macro bot MUST produce `result.json.cause = "commander-death-win-after-upgrade"` and `result.json.victory_signal = "engine-shutdown-gameover"` on NullAI on the first integration iteration (or within the 3-iter SC-007 budget).
- **FR-019**: The rush bot (`bot.fsx`) MUST remain runnable at every commit on the 025 branch. After every commit, `bash bots/trainer/run.sh NullAI <iter>` MUST produce `outcome=win` with `cause` containing `"engine shutdown"`.
- **FR-020**: The 023 `helpers/opening_build.fsx` helper MUST still exist and compile. It is on the exception-fallback path only.
- **FR-021**: The 024 modules (`Pathing`, `SmfParser`, `Chokepoints`, `WallIn`, `BasePlan`) MUST NOT be edited by this feature. 025 lives entirely in `bot_macro.fsx`, possibly with small additions to a new `helpers/primitive_driver.fsx` if two consumer call sites emerge organically per 020 FR-020 two-site extraction rule.

### Key Entities *(include if feature involves data)*

- **`PlanProgress` (existing, from 024)**: Persisted across ticks by the bot. Carries `ConsumedSlots`, `InFlight`, `Unfulfillable`. The 025 bot holds one instance at module-mutable scope and updates it via `BasePlan.markConsumed` / `markInFlight` / `markUnfulfillable`.
- **`ResolveContext` (existing, from 024)**: Built fresh per Opening tick from live `GameState`. Key update from 024's observability-only use: `ExistingStructures` MUST be derived from live `GameState.Units` filtered to finished structures, not left empty.
- **`AttackPathCache` (new)**: A per-attack-launch cached `(targetUnitId: int, targetPosition: Vec3, path: Path)` triple so combat units joining on later ticks reuse the same waypoint list. Invalidated when: (a) the Attack phase ends, (b) the bot explicitly picks a new target, or (c) `targetUnitId` is absent from `GameState.Units` at tick start (target death / despawn) — in case (c), `pickAttackTarget` runs and `findPath` is re-issued in the same tick.
- **`MapGridCache` (extended)**: The existing `bots/trainer/map-cache/<map>.json` is extended with a compressed serialisation of the real `MapGrid` (heightmap + slope map + resource map + dimensions), or the bot moves to re-parsing the `.sd7` via `SmfParser` at warmup if measurement shows that is under the 100 ms FR-015 budget. The choice is a plan-phase decision.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The first iteration of `BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh NullAI 025-smoke` after all four user stories are integrated produces `result.json.cause = "commander-death-win-after-upgrade"` and `result.json.victory_signal = "engine-shutdown-gameover"`.
- **SC-002**: In the same run, `stdout.log` contains at least one `[plan] issuing BuildCommand <def> @ (x,z) from resolvePlan` line (proving the resolvePlan path is driving emission) for each of the 5 opening slots, AND contains zero `[opening] idx=N issuing BuildCommand` lines (the 023 helper's emission signature). The two traces are mutually exclusive.
- **SC-003**: In the same run, `stdout.log` contains `[attack] path waypoints=N cost=C status=Complete` for the first attack launch, AND the bot's run-dir `unwired_commands.json` contains at least `12 × N` `MoveCommand` entries in the attack-launch frame range (where 12 is the combat-unit threshold and N is the waypoint count from the logged path — typically 3 on Avalanche).
- **SC-004**: Rush bot smoke `bash bots/trainer/run.sh NullAI 025-rush-smoke` produces `outcome=win` with `frames ≤ 13000` (matching the 024 baseline of 12390 within 5 %). FR-030 / FR-019 preserved.
- **SC-005**: During warmup, `engine.infolog | grep "Socket not writable"` returns zero lines, and the engine frame counter at the `[trainer] entering main frame loop` timestamp is within 1000 game frames of the frame at the first `[trainer] BarClient connected` timestamp (FR-016 + FR-017 compliance).
- **SC-006**: A deliberately-invalid plan slot (added as an experiment, e.g. `NearBaseCentre(-2500, 0)` on Avalanche which lands off-map) produces `Failure = Some OffMap` on the real MapGrid — not `BuildableNow = true` (which the 024 synthetic skeleton would have produced).
- **SC-007**: Integration lands in ≤3 live-iteration attempts per the 023 PLAYBOOK §2c "one fix per iter" rule. If >3 iters, the feature is halted and the operator files a budget-exhaustion mailbox.

## Assumptions

- **The offline map-cache pipeline (from 024) is the right place to stash an extended MapGrid blob.** Alternative: the bot re-parses the `.sd7` at warmup via `SmfParser.parseSd7` every iteration. Research task: measure both; the observed 1.2 s parse budget on Avalanche is 12 × the 100 ms warmup budget, so cache-extension is the most likely outcome but not mandated by this spec.
- **`Attack_launch.isCombatDef` is the correct combat-vs-worker classifier for the defend filter.** The same classifier is already used by the attack launcher; reusing it keeps the two code paths consistent. If it turns out to be wrong (e.g., misclassifies `armsam` anti-air as non-combat), the fix lands in the helper and both consumers benefit.
- **Combat units joining a launched attack on subsequent ticks should follow the same cached waypoint list.** This is the simplest thing that is coherent with "one `findPath` per attack launch." Alternatives (per-unit `findPath`, retargeting mid-attack) are out of scope.
- **Mid-game plan re-resolution runs on every Opening tactics tick** (~30 game frames / ~1 sim-second cadence, aligned with the existing `bot_macro.fsx` decision loop), not only on structure events. This is cheap (< 1 ms per call × ~60 calls over a full Opening phase) and catches edge cases the event-driven invalidation would miss. If profiling shows this is too slow in practice, the plan-phase tasks can add a dirty bit.
- **The 023 `opening_build.fsx` helper stays in-tree on the exception-fallback path.** Deleting it would be scope creep — this feature is the resolver taking over, not a cleanup pass.
- **SC-006's "deliberately-invalid slot test"** is a one-off check done by the operator during integration, not a committed test case. Committing it would poison the clean-win HISTORY with a slot that fails on purpose.
- **Feature 024's cross-repo `Protocol.replayBufferEnabled` contract is load-bearing for this feature**. US4's catch-up-OOM mitigation depends on the 024 replay buffer working correctly during the MapGrid callbacks at warmup. If HighBarV2 ever breaks the 031-contract, this feature's US4 fails first.

## Dependencies

- **Feature 024** (`024-tactical-map-primitives`) — merged into master at `c8888ad`. This feature edits `bot_macro.fsx` but not the 024 primitive modules.
- **HighBarV2 feature 031** (`031-fix-callback-event-drop`) — merged on the HighBarV2 side. The FSBarV1-side mirror landed with 024 at `c8888ad`.
- **BAR install with Avalanche 3.4** — required for live iteration. Maps are cached offline via `scripts/examples/14-cache-map-analysis.fsx` before the first iteration.
- **`bots/trainer/map-cache/avalanche_3.4.json`** — the chokepoint cache. Extended by this feature to include a compressed MapGrid, or replaced by a direct `.sd7` re-parse at warmup (plan-phase decision).

## Out of Scope

- Alternative faction plans (`defaultCoreOpening` etc.). Only `defaultArmadaOpening` is wired this feature.
- Map cache coverage for maps other than Avalanche 3.4. The offline cache script already handles any map; operator runs it per map as needed.
- BARb/dev clean wins. The best-effort BARb probe from 024 T065 is still out of scope here — 025's success criterion is NullAI only.
- Chokepoint minImpact tuning / dynamic thresholds. The 024 hardcoded `50` floor continues; if a future map needs a different value, that is a 024-scope follow-up.
- Replay-buffer thread safety (`Protocol.replayBufferEnabled` as module mutable). Single-BarClient-per-process is the only use case today.
- Filename sanitiser deduplication between the offline cache script and the bot. Trivial extract-method, not worth specifying.
- FSDOC_AGENT full refresh (XML doc audit, `tests.fsx` regen, known-issues regen, link validation). That is a separate feature if desired — the 024 partial manual docs refresh is the current state and is acceptable.
- Multi-commander / multi-base scenarios. The 024 primitives support one base centre; this feature inherits that.
- Live `MapGrid` refresh mid-game (e.g., on `FeatureRemoved` events for a crater). The warmup-loaded grid is static for the match.
