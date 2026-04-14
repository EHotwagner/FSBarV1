# Phase 1 — Data Model: Macro Bot Primitive-Driven Command Path

**Feature**: 025-macro-primitive-driven
**Date**: 2026-04-14

This feature introduces no new F# types on the public API surface. All new entities live inside `bots/trainer/bot_macro.fsx` as module-mutable state (following the 023/024 pattern established by `mapGrid`, `pinnedChokepoints`, `planResolvedAtWarmup`). The one public-API shape change is a single `Commands` function variant — covered in [contracts/commands-queued-move.md](./contracts/commands-queued-move.md).

## 1. `AttackPathCache` (NEW — bot-local state)

**Location**: `bots/trainer/bot_macro.fsx` module-mutable state, alongside the existing `mapGrid` / `pinnedChokepoints` / `planResolvedAtWarmup` mutables.

**Shape**:

```fsharp
type AttackPathCache =
    { TargetUnitId: int           // The unit id of the enemy the path is chasing
      TargetPosition: Position    // (x, y, z) tuple — same shape as Chokepoint.Position
      Path: Pathing.Path          // The findPath result including Waypoints, Status, Cost
      LaunchTick: int             // Game-frame number at which the path was computed (for diagnostics / stale-detection telemetry)
    }

let mutable attackPathCache : AttackPathCache option = None
```

**Validation rules**:

- `TargetUnitId` MUST be present in `client.GameState.Units` at construction time. If absent at tick-start, the cache is invalidated and a new `pickAttackTarget` + `findPath` runs in the same tick (FR-009a).
- `Path.Waypoints` length MUST be ≥ 1. An empty-waypoint path is treated as `Result.Error NoRoute` and falls through to the FR-010 direct-move path (single `MoveCommand` to `TargetPosition`).
- `LaunchTick` is diagnostic-only; no invariant depends on its value.

**Lifecycle / state transitions**:

| Event | Cache state before | Cache state after | Trace |
|---|---|---|---|
| Attack phase entered for the first time | `None` | `Some { ... }` computed by a fresh `findPath` | `[attack] path waypoints=N cost=C status=<Complete|Partial budget-exhausted>` |
| Subsequent tick, cache still valid | `Some { ... }` | unchanged | — (combat units following cached waypoints via queued `MoveCommand`s, see FR-009) |
| Target unit id no longer present in `GameState.Units` | `Some { TargetUnitId = id; ... }` (id absent) | `Some { ... }` computed by a fresh `findPath` to the next `pickAttackTarget` result in the same tick | `[attack] target <id> absent from GameState — re-pathing` |
| Bot explicitly picks a new target (e.g., closer-priority target appears) | `Some { ... }` (stale target) | `Some { ... }` with new target + new path | `[attack] retargeted to <newId>` |
| `findPath` returns `Result.Ok { Status = Partial true }` | any | `Some { ... Path.Status = Partial true }` | `[attack] path waypoints=N cost=C status=Partial budget-exhausted` — NO retry per clarification Q5 / FR-011 |
| `findPath` returns `Result.Error NoRoute` | any | `None` (cache not populated; direct-move fallback engages per FR-010) | `[attack] findPath NoRoute — falling back to direct move` |
| Attack phase ends / new Attack phase begins | `Some { ... }` | `None` | — |

**Relationship to other entities**:

- **Input**: `mapGrid` (real `MapGrid` from cache per R1), combat group centre-of-mass from `GameState.Units`, `pickAttackTarget` result, `ownStructures` from live `GameState.Units` finished-structures filter.
- **Output**: a sequence of queued `MoveCommand`s per combat unit, one unqueued + (N-1) queued per unit per launch (FR-008).
- **Depends on**: Q3 target-death rule (FR-009a), Q5 no-retry rule (FR-011), FR-007 one-call-per-launch rule.

## 2. `ResolveContext.ExistingStructures` (EXTENDED — derivation rule)

**Location**: existing type from 024 at `src/FSBar.Client/BasePlan.fsi`. No shape change. **Derivation rule changes**.

**Change**: 024's bot constructed `ExistingStructures = []` at warmup and left it empty for the lifetime of the bot. 025 derives `ExistingStructures` **per resolvePlan call** from live `GameState.Units` filtered to finished structures.

**Structure classifier**: `UnitDefInfo` in `src/FSBar.Client/UnitDefCache.fs:5–13` has no `IsBuilding` / `SpeedMax` / `CanMove` field — the F#-side cache carries only `DefId/Name/Cost/BuildSpeed/MaxWeaponRange/BuildOptions`. The inverse of `Attack_launch.isCombatDef` (which is `MaxWeaponRange > 0 && BuildOptions.Length = 0`) would misclassify defensive turrets like `armllt` as combat and miss them as structures. The cleanest classifier for this feature is to derive the structure set directly from the active plan's `DefName` list — this is exactly the set of structures `BasePlan.resolvePlan` cares about for clearance checks, and it auto-updates whenever `defaultArmadaOpening` is edited.

```fsharp
// Authoritative structure classifier for feature 025: any DefName
// that appears in the active plan is a structure. Automatically
// tracks plan edits; zero ambiguity vs. mobile units.
let private structureDefNames : Set<string> =
    BasePlan.defaultArmadaOpening.Slots
    |> List.map (fun s -> s.DefName)
    |> Set.ofList

let private isStructureDef (cache: UnitDefCache) (defId: int) : bool =
    match UnitDefCache.tryFindById cache defId with
    | Some info -> Set.contains info.Name structureDefNames
    | None -> false

let currentExistingStructures () : OwnStructureFootprint list =
    client.GameState.Units
    |> Map.toSeq
    |> Seq.choose (fun (id, u) ->
        if u.IsFinished && isStructureDef client.GameState.UnitDefs u.DefId then
            // OwnStructureFootprint field names per src/FSBar.Client/Pathing.fsi:7-14.
            // Verify exact field shape at T018 time; likely (Centre, Xsize, Zsize).
            // Footprint dimensions come from the matching PlanSlot metadata:
            // walk defaultArmadaOpening.Slots for the slot whose DefName
            // matches info.Name and read its footprint fields.
            Some { Centre = u.Position
                   Xsize = (* slot.Xsize *) 0
                   Zsize = (* slot.Zsize *) 0 }
        else None)
    |> Seq.toList
```

**Validation rules**:

- `DefId` MUST resolve in `client.GameState.UnitDefs` (the `UnitDefCache`). If not, the structure is silently skipped — it can't be clearance-checked against something whose footprint is unknown.
- The matching `PlanSlot` (walked by `DefName`) MUST exist for every structure that passes `isStructureDef`. This is guaranteed by construction because `structureDefNames` is derived from the plan's slots.
- When `defaultArmadaOpening` is edited to add a new slot, the classifier auto-includes the new def with zero feature-25 code changes. This is the invariant that keeps US1's "edit the plan, behaviour changes" acceptance scenario (spec.md US1 §"Independent Test") working.

**Cadence**: recomputed once per Opening tactics tick (clarification Q4 / FR-001), immediately before the `BasePlan.resolvePlan` call that consumes it. Not cached across ticks — structures can finish, die, or be replaced between ticks.

## 3. `MapGridCache` (EXTENDED — cache file schema)

**Location**: `bots/trainer/map-cache/<map>.json`. Current schema carries `chokepoints[]` + a handful of metadata fields. Extended schema per R1 decision.

**Schema delta** (new top-level fields, existing fields unchanged):

```json
{
  "mapName": "...",
  "sd7Path": "...",
  "widthHeightmap": 512,
  "heightHeightmap": 512,
  "widthElmos": 4096,
  "heightElmos": 4096,
  "baseCentre.x": 500,
  "baseCentre.y": 0,
  "baseCentre.z": 397,
  "query.maxWidthElmos": 240,
  "query.searchRadiusElmos": 5500,
  "chokepoints": [ ... existing ... ],

  // NEW in 025:
  "mapGrid": {
    "widthElmos": 4096,
    "heightElmos": 4096,
    "widthHeightmap": 512,
    "heightHeightmap": 512,
    "heightMap.gzip.b64": "<base64 of gzip of float32[513*513] row-major little-endian>",
    "slopeMap.gzip.b64": "<base64 of gzip of float32[256*256] row-major little-endian>",
    "resourceMap.gzip.b64": "<base64 of gzip of float32[256*256] row-major little-endian>",
    "schemaVersion": 1
  },

  "generatedAtUtc": "2026-04-14T...",
  "sourceArchive": "avalanche_3.4.sd7"
}
```

**Validation rules**:

- If `mapGrid` is absent and the map is in the 025 target set (currently `["Avalanche 3.4"]`), the bot MUST hard-fail warmup with the FR-014 message instructing the operator to re-run `scripts/examples/14-cache-map-analysis.fsx`.
- If `mapGrid` is absent and the map is outside the target set, the bot MUST log `[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx` and fall back to the 024 synthetic skeleton.
- `mapGrid.schemaVersion` MUST equal 1 for this feature. Future features that extend the schema MAY increment the version; the bot rejects unknown versions with a clear error rather than silently mis-parsing.
- Array dimensions MUST match the declared `widthHeightmap` / `heightHeightmap`. Mismatch is a hard-fail at cache load.

**Relationship to other entities**:

- **Written by**: `scripts/examples/14-cache-map-analysis.fsx` after parsing the `.sd7` via `SmfParser.parseSd7` (the offline-only parse, NOT repeated at warmup).
- **Read by**: `bots/trainer/bot_macro.fsx` at warmup, reconstructing a full `MapGrid` value to store in the existing `mapGrid` mutable.

## 4. `PlanProgress` (EXISTING — persisted across ticks)

**Location**: existing type from 024 at `src/FSBar.Client/BasePlan.fsi`. No shape change. **Lifetime changes**.

**Change**: 024's bot constructed `BasePlan.emptyPlanProgress` fresh on every call because the bot wasn't actually consuming the resolver output. 025 stores one instance as `bot_macro.fsx` module-mutable state and updates it across ticks via `BasePlan.markInFlight` / `BasePlan.markConsumed`:

```fsharp
let mutable planProgress : BasePlan.PlanProgress = BasePlan.emptyPlanProgress
```

**State transitions**:

| Event | Call site | Effect on PlanProgress |
|---|---|---|
| Bot issues `BuildCommand` for a resolved slot | At the per-tick command-emission path in Opening phase | `planProgress <- BasePlan.markInFlight planProgress slot` (FR-003) |
| `UnitFinished` event fires with a `DefName` matching a slot | In the bot's event handler | `planProgress <- BasePlan.markConsumed planProgress slot` (FR-004) |
| A slot is repeatedly resolved with `Failure = Some _` across multiple ticks | Opening tactics tick | `planProgress <- BasePlan.markUnfulfillable planProgress slot` (per the BasePlan API, if applicable — mechanism TBD during implementation) |
| Opening phase ends | Phase transition | `planProgress` is **not** reset — it carries into the Production phase as a record-of-what-built, but is not consumed there |

**Relationship to other entities**:

- **Input**: per-call `ResolveContext.Progress` field copy.
- **Mutation**: updated only via the exported `BasePlan.markInFlight` / `markConsumed` / `markUnfulfillable` functions (no ad-hoc record update — keeps all invariants in the 024 module).
- **Scope**: one instance per bot process. The rush bot (`bot.fsx`) does not use this entity.

## 5. Combat unit filter (EXISTING — reuse site)

**Location**: `bots/trainer/helpers/attack_launch.fsx:48` — `isCombatDef : UnitDefCache -> int -> bool`.

No shape change. The defend interrupt at `bot_macro.fsx:461` currently filters with `u.IsFinished` only; 025 adds the `isCombatDef` gate:

```fsharp
// Before (024 partial):
let targets =
    client.GameState.Units
    |> Seq.filter (fun (_, u) -> u.IsFinished)
    |> Seq.map fst
    |> Seq.toList

// After (025):
let targets =
    client.GameState.Units
    |> Seq.filter (fun (_, u) -> u.IsFinished && isCombatDef client.GameState.UnitDefs u.DefId)
    |> Seq.map fst
    |> Seq.toList
```

**Fallback**: when the filtered list is empty, log `[defend] no combat units available — commander fallback` and fall through to the 023 nearest-enemy `AttackCommand` with the commander as defender (FR-013).

## Entity summary

| Entity | Location | New/Extended | Storage |
|---|---|---|---|
| `AttackPathCache` | `bot_macro.fsx` mutable | New | In-memory, per-bot-process |
| `ResolveContext.ExistingStructures` | derivation rule in `bot_macro.fsx` | Extended derivation | In-memory, recomputed per tactics tick |
| `MapGridCache` (JSON schema) | `bots/trainer/map-cache/<map>.json` | Extended schema (v1 → add `mapGrid` block) | Filesystem, per-map |
| `PlanProgress` | `bot_macro.fsx` mutable | Existing, now lifetime-extended | In-memory, per-bot-process |
| Combat filter reuse | `helpers/attack_launch.isCombatDef` | Existing, new consumer | N/A |
