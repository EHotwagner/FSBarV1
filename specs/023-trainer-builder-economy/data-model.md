# Phase 1 Data Model — 023 Builder-Economy Macro Bot

**Branch**: `023-trainer-builder-economy`
**Date**: 2026-04-13
**Scope**: entities that the macro bot manipulates in memory, the state-machine shape, and the on-disk records it emits. Everything here is F# data defined inside `.fsx` helper modules — no `.fs` / `.fsi` changes, consistent with the plan's Tier 2 classification.

---

## 1. Bot phase enum

The macro bot is a four-phase state machine. The phase is a simple discriminated union held as a `mutable` at the top of `bot_macro.fsx` and passed into the tactics callback on every frame.

```fsharp
type MacroPhase =
    | Opening      // commander-driven opening-build order
    | Production   // factory is producing, economy is growing
    | Upgrade      // upgrade milestone in progress
    | Attack       // committed to the decisive attack
    | Defending    // interrupt state — enemy in base; see §6
```

**State transitions** (all internal-predicate-driven per FR-016a):

| From       | To           | Trigger                                                                 | Helper owning the predicate       |
|------------|--------------|-------------------------------------------------------------------------|-----------------------------------|
| Opening    | Production   | First factory finishes construction (FR-004)                            | `opening_build.fsx` emits the event |
| Production | Upgrade      | Upgrade-gate entry predicate true (economy thresholds reached)           | `upgrade_gate.fsx`                |
| Upgrade    | Attack       | Upgrade milestone reached AND combat-unit count ≥ `CombatUnitThreshold` | `upgrade_gate.fsx` + `attack_launch.fsx` |
| Upgrade    | Attack       | Upgrade deadline exceeded AND combat-unit count ≥ `CombatUnitThreshold` | `attack_launch.fsx` (FR-012 fallback) |
| Upgrade    | (terminal)   | Upgrade deadline exceeded AND combat-unit count < `CombatUnitThreshold` | stall reason recorded; match ends as loss-by-stall (FR-012) |
| *          | Defending    | Any enemy unit inside base radius (FR-016b)                              | `perception.fsx` (`enemyInBase`)  |
| Defending  | (previous)   | No enemy units remaining inside base radius                              | `perception.fsx`                  |

**Constraints**:

- `Attack` is terminal — the bot does not return to production or upgrade once the attack is launched (losing the attack force ends the match as a loss).
- `Defending` is an **interrupt**, not a proper phase; when it ends, the bot resumes the phase it was in before (stored in a second `mutable`, `preDefendPhase`).
- The phase transition `Opening → Production` is unconditional on the factory-completion event even if mex/energy structures are still under construction (per the Session 2026-04-13 Q1 clarification and FR-004).

---

## 2. Opening-build order

```fsharp
type StructureKind =
    | MetalExtractor
    | Energy
    | Factory
    | Other of string

type OpeningBuildItem = {
    Kind: StructureKind
    DefName: string                    // e.g. "cormex", "corsolar", "corlab"
    PreferredPosition: PositionChooser // see below
    MaxRetries: int                    // per-item retry budget before skipping to next
}

and PositionChooser =
    | NearestMetalSpot                 // pick the nearest unused metal spot from getMetalSpots
    | NearCommander of radius:float32  // any buildable tile within `radius` of commander
    | NearBaseCentre of radius:float32 // any buildable tile within `radius` of base centre

type OpeningBuildOrder = {
    Items: OpeningBuildItem list
    TotalItemCount: int                // for progress reporting
}
```

**Default opening** (initial iteration):

```text
1. MetalExtractor cormex NearestMetalSpot MaxRetries=3
2. MetalExtractor cormex NearestMetalSpot MaxRetries=3
3. Energy         corsolar NearCommander(300) MaxRetries=3
4. Energy         corsolar NearCommander(300) MaxRetries=3
5. Factory        corlab   NearBaseCentre(400) MaxRetries=2
```

The list satisfies the FR-001 minimum of 2 mex + 2 energy + 1 factory. Iterations may add items (e.g. a third energy) under the extraction rule.

**Validation rules**:

- `Items.Length ≥ 5` and the list MUST contain at least 2 items with `Kind = MetalExtractor`, 2 with `Kind = Energy`, and 1 with `Kind = Factory`, for the bot to transition through the Opening phase correctly.
- Each `DefName` MUST resolve against `UnitDefCache` at bot start; an unresolved name aborts the match with a clear error (fail-fast).
- `NearestMetalSpot` requires `getMetalSpots` to return at least as many spots as there are `NearestMetalSpot` items; otherwise the helper emits an out-of-scope diagnostic.

---

## 3. Production queue policy

```fsharp
type UnitRole =
    | Constructor
    | Combat

type QueueItem = {
    Role: UnitRole
    DefName: string       // e.g. "corck" (constructor), "corak" (combat)
    Weight: int           // relative share of the queue (higher = more of these)
}

type QueuePolicy = {
    FactoryDefName: string
    Items: QueueItem list
    MinQueueDepth: int    // top up when observed queue drops below this
    TargetConstructorRatio: float32  // 0.0..1.0 — share of Constructors in running production
}

type QueueState = {
    FactoryUnitId: int option
    AskedCounts: Map<int, int>    // defId → count of items we've submitted
    ObservedBuilt: Map<int, int>  // defId → count of UnitFinished events we've seen
    LastRefillFrame: uint32
}
```

**Replenishment rule**: on every frame (or every N frames — configurable at the top of `bot_macro.fsx`), the helper computes current observed queue depth = `Σ(AskedCounts - ObservedBuilt)` and submits new `BuildCommand`s to raise it to `MinQueueDepth`. When selecting *what* to queue, the helper compares `observed Constructor count / observed total` against `TargetConstructorRatio` and picks the role that is furthest below its target. This gives FR-006's "both constructors and combat" behaviour from a single tunable point.

**Resource-shortfall gate (FR-008)**: before queueing a *combat* unit, the helper checks `GameState.Metal.Income > minCombatIncomeThreshold`; if income is below the threshold, the helper prioritises an *Economy* structure via the constructor dispatcher (§4) instead.

---

## 4. Idle-constructor dispatch

```fsharp
type ConstructorJob =
    | BuildStructure of defName:string * positionChooser:PositionChooser
    | AssistUnit of targetUnitId:int
    | Repair of targetUnitId:int
    | Idle                                    // sentinel — should be minimised

type ConstructorAssignment = {
    ConstructorUnitId: int
    Job: ConstructorJob
    AssignedFrame: uint32
}

type DispatchState = {
    Assignments: Map<int, ConstructorAssignment>  // keyed by constructor id
    IdleSinceFrame: Map<int, uint32>               // for idle-defect telemetry (FR-007)
}
```

**Dispatch rule**: on each frame, the helper walks `GameState.Units` for units whose def is a constructor (by name prefix), checks whether each has an entry in `Assignments`, and assigns an `Idle` constructor to the highest-priority available job:

1. Repair any damaged own unit within `repairRadius` (if any).
2. Assist the commander if the commander has a non-empty build queue and no other assistor (reduces build time of the current opening-build item).
3. Build another economy structure (cheap `MetalExtractor` or `Energy` via `NearCommander`) — keeps income compounding.
4. `Idle` only if no job applies; this case must be extremely rare and is recorded in `IdleSinceFrame` for telemetry.

**Defect signal (FR-007)**: if a constructor's idle duration exceeds `idleConstructorThreshold` frames, the helper emits a stdout line tagged `[idle-dispatch-defect]` so `PLAYBOOK.md` §2c diagnosis picks it up.

---

## 5. Upgrade gate

```fsharp
type UpgradePredicateName =
    | AdvancedConstructor
    | AdvancedFactory
    | AdvancedCombatUnit

type UpgradeGateState = {
    Reached: UpgradePredicateName option  // first predicate to fire
    ReachedFrame: uint32 option
    DeadlineFrame: uint32                  // initial value per spec clarification
    EntryPredicatesMet: bool               // when production is "ready" to start upgrading
}
```

**Entry predicate** (production → upgrade): `GameState.Metal.Income ≥ metalIncomeThreshold AND observed factory-built units ≥ initialProductionCount`. Both thresholds live at the top of `bot_macro.fsx` as named constants.

**Exit predicates** (upgrade → attack):

- *Normal path*: `Reached.IsSome AND combatUnitCount ≥ CombatUnitThreshold`
- *Deadline-fallback path* (FR-012): `currentFrame > DeadlineFrame AND combatUnitCount ≥ CombatUnitThreshold` (logs deadline-fallback decision)
- *Stall path* (FR-012): `currentFrame > DeadlineFrame AND combatUnitCount < CombatUnitThreshold` — bot does NOT attack; records stall reason; match runs out the frame clock as loss-by-stall.

**Invariant**: the bot MUST NOT enter `Attack` with both `Reached.IsNone` and `combatUnitCount < CombatUnitThreshold` — this is the "no degenerate rush" rule from the spec Q5 clarification.

---

## 6. Enemy-in-base (defend interrupt)

```fsharp
type BaseAwareness = {
    BaseCentre: float32 * float32 * float32
    BaseRadius: float32                           // tunable, initial 1200.0f
}

type DefendState = {
    EnemiesInBase: Set<int>                       // enemy unit ids inside base radius right now
    LastTransitionFrame: uint32 option
}
```

**Query** (per FR-016b): each frame, walk `GameState.Enemies`, compute 2D distance from each enemy `Position` to `BaseCentre`, and collect those ≤ `BaseRadius`. When the set transitions from empty → non-empty, the bot records a `Defending` phase transition with `preDefendPhase` captured. When the set transitions back to empty, the bot records a `Resuming` phase transition and restores `preDefendPhase`.

**Behaviour while defending**: the bot overrides its current production/upgrade/attack logic and issues `AttackCommand`s from all available combat units (and the commander if no combat units exist) at the enemies in the set, preferring the closest one to the base centre. When the set empties, production logic resumes on the next frame.

---

## 7. Attack-launch decision

```fsharp
type AttackLaunchState = {
    Launched: bool
    LaunchFrame: uint32 option
    ArmySnapshot: int                             // combat unit count at launch
    TargetPosition: (float32 * float32 * float32) option  // enemy commander position (from perception)
    Composition: Map<string, int>                 // defName → count, for the run log
}
```

**Predicate (FR-013)**: `MacroPhase = Attack AND Launched = false`. The bot enters `Attack` only via the upgrade gate; once entered, the attack-launch helper fires `AttackCommand` or `FightCommand` at the enemy commander position (pulled from `perception.fsx.pickEnemyCommanderPos`) for every combat unit, sets `Launched = true`, and records the launch frame + composition.

**Retargeting**: if the perceived enemy commander position changes by more than `retargetThreshold` game-units, the helper re-issues movement / engagement commands for units that are still alive. No retargeting occurs if `commanderAlive` (own side) is false — at that point the match is already lost.

---

## 8. On-disk records

### 8.1 `phase_transitions.jsonl`

One JSONL file in the run directory, one object per line. Written by the macro bot only (the rush bot does not write this file). Full schema in `contracts/phase-transition-record.md`.

```jsonc
{
  "frame": 1234,
  "from": "Opening",
  "to": "Production",
  "reason": "first-factory-finished",
  "telemetry": {
    "units": 1,
    "metal_income": 12.3,
    "energy_income": 35.0,
    "combat_units": 0,
    "structures_built": {"cormex": 2, "corsolar": 2, "corlab": 1}
  },
  "notes": "factory unit_id=42"
}
```

Required fields: `frame`, `from`, `to`, `reason`. Optional: `telemetry`, `notes`.

### 8.2 `result.json` additions

The existing `result.json` schema from 020/021 is **not extended** in this feature — the macro bot reuses it verbatim. The `cause` string distinguishes macro-specific terminal states:

- `"commander-death-win-after-upgrade"` — clean macro win (SC-004, SC-010)
- `"commander-death-win-deadline-fallback"` — FR-012 fallback path
- `"loss-by-stall-upgrade-deadline"` — FR-012 stall path
- All existing causes (`commander destroyed before engine shutdown`, `frame limit reached`, etc.) remain unchanged.

### 8.3 `bot.fsx.snapshot`

Existing file, contents now reflect whichever bot was launched (`bot.fsx` OR `bot_macro.fsx`). Filename preserved for schema compatibility with 020 contracts. The first line of the snapshot is a comment identifying which bot was launched, so the operator can tell them apart without `diff`.

---

## 9. Entity lifetimes and state ownership

All state is in-memory, per-match, non-persistent. Each entity is owned by exactly one helper module:

| Entity                    | Owner (`.fsx` module)       | Lifetime                                      |
|---------------------------|-----------------------------|-----------------------------------------------|
| `MacroPhase`, `preDefendPhase` | `bot_macro.fsx` (mutables) | single match                                |
| `OpeningBuildOrder`, progress | `opening_build.fsx`         | single match (immutable order + mutable idx) |
| `QueueState`              | `production_queue.fsx`      | single match                                  |
| `DispatchState`           | `constructor_dispatch.fsx`  | single match                                  |
| `UpgradeGateState`        | `upgrade_gate.fsx`          | single match                                  |
| `DefendState`             | `perception.fsx` (or a new small module) | single match                    |
| `AttackLaunchState`       | `attack_launch.fsx`         | single match                                  |
| Phase transition log file | `log.fsx`                   | single match (file handle closed on bot exit) |

No cross-match persistence. Cross-match learning happens entirely through the operator reading run directories and editing helpers, which is the whole point of the iterative trainer.

---

## 10. What is NOT modelled here (explicit non-goals)

- **Unit type / DefId tables**: live in `UnitDefCache` (existing), not duplicated.
- **Map knowledge beyond metal spots and spawn position**: out of scope. No heightmap sampling, no path checks, no defensible-position heuristics.
- **Enemy model**: limited to "is an enemy inside my base radius" per FR-016b. No enemy army composition, no enemy economy, no enemy upgrade detection.
- **Multi-match state**: rejected by the trainer's "one run directory per match" invariant from 020.
- **A `MacroBotConfig` record type**: the clarifications spec thresholds as "tunable from a single place" — that single place is a handful of top-of-file `let` bindings in `bot_macro.fsx`. No config file, no JSON schema. Keeps iteration fast.

All non-goals are deliberate; each one is a forcing function for a *future* feature if an iteration proves the gap is blocking.
