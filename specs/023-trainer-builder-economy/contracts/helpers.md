# Helper Module Contracts — 023 Builder-Economy Macro Bot

**Branch**: `023-trainer-builder-economy`
**Scope**: F# script-module interfaces for the five new helper modules mandated by FR-021. Each contract lists the module name, the public values it exposes, the `GameFrame`-level shape of each function, and the extraction rule that governs when each helper first lands on-branch.

These are **`.fsx` modules**, not `.fs`/`.fsi` modules — there are no compiler-enforced signature files per the plan's Tier 2 classification. The "contract" here is a human + AI review artifact; the enforcement is that `bot_macro.fsx` and any later bot that reuses these helpers MUST compile (i.e. FSI-load) and MUST pass the feature's user-story acceptance scenarios against the no-op rung.

All helpers follow the same three conventions:

1. **Pure where possible**: each helper exposes a pair `(computeXxx, applyXxx)` — `computeXxx` is pure over the game state inputs and returns a decision record; `applyXxx` is the side-effecting "issue these commands / write this log line" half. This keeps the decision logic diff-able across iterations.
2. **Extraction-rule provenance**: each helper's header comment MUST record the two organic sites from which it was extracted (per feature 021 Clarification Q3, carried forward via 022). Synthetic splits are forbidden.
3. **Bot-agnostic**: every helper takes its inputs explicitly (no hidden globals) so that `bot.fsx` (the existing rush bot) could reuse it without modification. FR-023 makes this a branch invariant — a helper interface change that breaks `bot.fsx` MUST update `bot.fsx` in the same commit.

---

## 1. `opening_build.fsx`

**FR link**: FR-001, FR-002, FR-003, FR-004
**Extraction trigger**: The macro bot's first iteration inlines the opening sequence. The second iteration that would inline the same sequence triggers extraction into this module.

### Public values

```fsharp
/// Resolve the opening-build order's symbolic def names against UnitDefCache.
/// Fails fast if any name is unresolved.
val resolveOpeningBuildOrder :
    cache: UnitDefCache.UnitDefCache ->
    order: OpeningBuildOrder ->
    ResolvedOpeningBuildOrder

/// Compute the next build command the commander should issue, given the
/// current opening state. Returns None when the opening is complete.
val nextOpeningCommand :
    resolved: ResolvedOpeningBuildOrder ->
    progress: OpeningProgress ->
    commanderId: int ->
    commanderPos: (float32 * float32 * float32) ->
    metalSpots: (float32 * float32 * float32 * float32) array ->
    gameState: GameState ->
    OpeningCommandDecision option

/// Record a placement failure (FR-003) against the current item's retry
/// budget and advance to the next item if the budget is exhausted.
val recordPlacementFailure :
    progress: OpeningProgress ->
    reason: string ->
    OpeningProgress

/// Test whether the opening phase is complete (FR-004): the first factory
/// has finished construction. Takes the UnitFinished event stream since the
/// previous call and the resolved factory def-id.
val openingComplete :
    resolved: ResolvedOpeningBuildOrder ->
    newlyFinishedDefIds: int list ->
    bool
```

### Records

```fsharp
type ResolvedOpeningBuildOrder = {
    Items: (int * OpeningBuildItem) list    // (defId, original item)
    FactoryDefId: int                        // short-circuit for openingComplete
}

type OpeningProgress = {
    CurrentIndex: int
    RetryCountThisItem: int
    FailuresByIndex: Map<int, string list>   // diagnostic trail
}

type OpeningCommandDecision = {
    Command: Highbar.AICommand               // the BuildCommand to issue
    Chosen: {| DefId: int; X: float32; Y: float32; Z: float32 |}
    Rationale: string                        // for stdout logging
}
```

### Error handling

- Unresolved def name in `resolveOpeningBuildOrder` → raises `Failure` at bot start (fail-fast per FR-003 spirit).
- `nextOpeningCommand` returns `None` when `CurrentIndex >= Items.Length` — caller transitions to the next phase.
- Placement retry exhausted → `recordPlacementFailure` advances `CurrentIndex` and the operator inspects the run directory to diagnose (FR-003 requires "capture failure in run log with enough context to diagnose").

---

## 2. `production_queue.fsx`

**FR link**: FR-005, FR-006, FR-008
**Extraction trigger**: The macro bot's first iteration after Opening → Production inlines the factory-queue top-up logic. The second iteration that would inline the same top-up logic triggers extraction.

### Public values

```fsharp
/// Resolve the queue policy's symbolic def names against UnitDefCache.
val resolveQueuePolicy :
    cache: UnitDefCache.UnitDefCache ->
    policy: QueuePolicy ->
    ResolvedQueuePolicy

/// Compute the next batch of BuildCommands to queue on the factory.
/// Respects MinQueueDepth, TargetConstructorRatio, and the FR-008
/// resource-shortfall gate. Returns an empty list when the queue is full
/// or the factory is not yet built.
val computeQueueTopUp :
    resolved: ResolvedQueuePolicy ->
    state: QueueState ->
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    (Highbar.AICommand list * QueueState)

/// Update queue state from a frame's events. Counts UnitFinished per
/// defId to update ObservedBuilt; counts UnitCreated emitted by the
/// factory to update the 'in-flight' view.
val observeFrame :
    state: QueueState ->
    events: GameEvent list ->
    factoryUnitId: int ->
    QueueState

/// Return the observed idle-frame count for the factory (frame at which
/// factory became idle, or None if producing). For defect telemetry.
val factoryIdleSince :
    state: QueueState ->
    currentFrame: uint32 ->
    uint32 option
```

### Records

See `data-model.md §3` for `QueuePolicy`, `QueueItem`, `QueueState`, `ResolvedQueuePolicy`.

### Invariants

- `computeQueueTopUp` never issues a command when `FactoryUnitId = None`.
- When the resource-shortfall gate fires (metal or energy income below threshold), `computeQueueTopUp` returns at most constructor items; it must NOT add combat items in that state (FR-008).
- `observeFrame` is the only mutator; `state` is otherwise immutable from the bot's point of view.

---

## 3. `constructor_dispatch.fsx`

**FR link**: FR-007
**Extraction trigger**: The macro bot's first iteration does not dispatch idle constructors. The second iteration notices idle constructors in the run log and adds per-constructor dispatch logic. A third iteration that would repeat that logic triggers extraction.

### Public values

```fsharp
/// Find all own units whose def is a constructor (by name prefix against the cache).
val findConstructors :
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    int list

/// Dispatch all idle constructors to highest-priority available jobs.
/// Returns the commands to issue and the updated DispatchState.
val dispatchIdle :
    state: DispatchState ->
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    currentFrame: uint32 ->
    (Highbar.AICommand list * DispatchState)

/// Return constructor ids whose idle duration exceeds the threshold
/// (for defect telemetry per FR-007).
val idleDefectCandidates :
    state: DispatchState ->
    currentFrame: uint32 ->
    threshold: uint32 ->
    int list
```

### Job-selection rule

Priority order (first match wins):

1. Repair — any own unit with `healthRatio < 0.9` within `repairRadius`
2. Assist commander — if commander has a non-empty opening-build queue and no other assistor is already attached
3. Build economy — `MetalExtractor` on a free metal spot, else `Energy` via `NearCommander`
4. `Idle` — recorded in `IdleSinceFrame`

---

## 4. `upgrade_gate.fsx`

**FR link**: FR-009, FR-010, FR-011, FR-012
**Extraction trigger**: The macro bot's first iteration hardcodes the upgrade entry/exit predicates inline. The second iteration that touches the same predicates (e.g. to raise a threshold) triggers extraction.

### Public values

```fsharp
/// Entry predicate: is the economy "ready" to start upgrading?
val entryPredicateMet :
    gameState: GameState ->
    thresholds: {| MetalIncome: float32; InitialProductionCount: int |} ->
    bool

/// First-wins check for the three advanced predicates.
/// Returns Some(predicateName) on first true, None otherwise.
val upgradeReached :
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    UpgradePredicateName option

/// Given the current upgrade state, the current frame, and the helper's
/// deadline, return the exit decision:
///  - AttackNow     → enter Attack (upgrade reached OR deadline+threshold)
///  - StallAndLose  → record stall reason; no attack, run out the clock (FR-012)
///  - WaitLonger    → stay in Upgrade
val decideUpgradeExit :
    state: UpgradeGateState ->
    currentFrame: uint32 ->
    combatUnitCount: int ->
    combatUnitThreshold: int ->
    UpgradeExitDecision

type UpgradeExitDecision =
    | AttackNow of path:UpgradeAttackPath   // Normal | DeadlineFallback
    | StallAndLose of reason:string
    | WaitLonger

type UpgradeAttackPath = Normal | DeadlineFallback
```

### Invariants (enforced in tests or by inspection of run log)

- `decideUpgradeExit` MUST return `StallAndLose` when the deadline is exceeded AND combat-unit count is below threshold (FR-012).
- `decideUpgradeExit` MUST NOT return `AttackNow` when both `state.Reached.IsNone` and combat-unit count is below threshold. This is the "no degenerate rush" rule.

---

## 5. `attack_launch.fsx`

**FR link**: FR-013, FR-014, FR-015
**Extraction trigger**: The macro bot's first attack iteration inlines the army count + launch logic. The second such iteration triggers extraction.

### Public values

```fsharp
/// Count combat units in the bot's army using name-based classification.
/// The initial allowlist lives here; iterations grow it under the
/// extraction rule. Returns the integer count.
val countCombatUnits :
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    int

/// Build the launch snapshot: per-def counts, total, target position.
val buildLaunchSnapshot :
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    targetPos: (float32 * float32 * float32) ->
    AttackLaunchState

/// Issue initial attack / fight commands for every combat unit in the
/// army. Caller invokes this once per phase-entry into Attack.
val issueLaunch :
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    target: (float32 * float32 * float32) ->
    Highbar.AICommand list

/// Re-issue commands for still-alive combat units if the target position
/// has moved by more than retargetThreshold.
val maybeRetarget :
    state: AttackLaunchState ->
    gameState: GameState ->
    cache: UnitDefCache.UnitDefCache ->
    newTarget: (float32 * float32 * float32) ->
    retargetThreshold: float32 ->
    Highbar.AICommand list
```

### Observations

- Combat-unit classification is name-prefix based (see data-model §9 / research R9); the initial allowlist is intentionally small to let iteration grow it.
- `issueLaunch` emits one `AttackCommand` per combat unit targeting the closest visible enemy, falling back to `FightCommand` at the target position if no enemy unit is visible near the target.
- `maybeRetarget` is a pure function that returns commands; the caller (bot) decides whether to apply them.

---

## 6. Extensions to existing helpers

These are not new modules — they are additions to modules that already exist in-tree. They are listed here because the contract consumers are the macro bot's new helper modules above, and the additions MUST preserve backward compatibility with `bot.fsx` (FR-023).

### 6.1 `log.fsx` — phase-transition record writer

New public value, additive:

```fsharp
type PhaseTransitionRecord = {
    Frame: uint32
    From: string
    To: string
    Reason: string
    Telemetry: Map<string, obj> option
    Notes: string option
}

/// Append one phase-transition record to phase_transitions.jsonl in the
/// run directory. Opens/appends; caller is responsible for match-lifetime
/// file ownership.
val logPhaseTransition :
    logger: TrainerLogger ->
    record: PhaseTransitionRecord ->
    unit
```

Full schema in `phase-transition-record.md`. Existing `logFrame`, `logStart`, `writeResult`, `writeError` are unchanged.

### 6.2 `perception.fsx` — base radius / enemy-in-base query

New public values, additive:

```fsharp
/// Compute a base centre from the commander's starting position at
/// bot warmup. Caller pins this value for the whole match.
val computeBaseCentre :
    gameState: GameState ->
    commanderId: int ->
    (float32 * float32 * float32) option

/// Return the set of enemy unit ids inside base radius (FR-016b).
val enemiesInBase :
    gameState: GameState ->
    baseCentre: (float32 * float32 * float32) ->
    baseRadius: float32 ->
    Set<int>
```

Existing `pickEnemyCommanderPos` is unchanged.

### 6.3 `tactics.fsx` — no changes

`trainerLoopRun` stays untouched. The macro bot drives its phase logic via a new `TrainerTacticsFn` it constructs inline at the top of `bot_macro.fsx`; no new kernel-level machinery is added. This keeps FR-017 intact (reuse the existing trainer infrastructure).

---

## 7. Summary of the extraction schedule

This table is the contract that `/speckit.tasks` will translate into story-grouped tasks. Each row says *when* the helper is expected to land on-branch during iteration.

| Helper                         | Earliest iteration | Gated by               | Commit style      |
|--------------------------------|--------------------|------------------------|-------------------|
| `opening_build.fsx`            | iter 2-3           | 2 iterations of opening-build duplication | `trainer: extract opening-build helper` |
| `log.fsx` phase-transition add | iter 1             | first phase transition | `trainer: add phase-transition record` (atomic with iter 1) |
| `production_queue.fsx`         | iter 3-4           | 2 iterations with factory producing       | `trainer: extract production-queue helper` |
| `upgrade_gate.fsx`             | iter 4-5           | 2 iterations touching upgrade thresholds  | `trainer: extract upgrade-gate helper`     |
| `constructor_dispatch.fsx`     | iter 5-6           | 2 iterations with idle-constructor defects| `trainer: extract constructor-dispatch helper` |
| `attack_launch.fsx`            | iter 6-7           | 2 iterations launching attacks            | `trainer: extract attack-launch helper`    |

The earliest-iteration numbers are estimates for plan calibration only — actual landing is driven by the extraction rule (two organic call sites), not by a schedule. SC-006 is the final gate: all five helpers in-tree, each with a consumer, each referenced by the playbook, at feature end.
