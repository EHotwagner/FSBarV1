# Contract — `bot_macro.fsx` primitive-driven integration shape

**Feature**: 025-macro-primitive-driven
**Target file**: `bots/trainer/bot_macro.fsx`
**Tier**: N/A (FSI script, not a packable library; verified via live iteration SC-001..SC-005 and the FR-008a unit test)
**Maps to**: FR-001..FR-013, FR-018..FR-020; all five user stories US1..US5

## Rationale

Feature 024 left `bot_macro.fsx` in an observability-only state: the 024 primitives are loaded, resolved, and traced, but the bot's actual command emission still comes from the 023 hardcoded helpers. Feature 025 is **the one commit** that moves command emission to the primitives, per the spec framing "deep single-pass refactor replacing hardcoded logic in one commit." This contract enumerates the call-site edits inside `bot_macro.fsx` with enough precision that the task breakdown can order and verify them.

## Module-mutable state additions

Alongside the existing 024 mutables (`mapGrid`, `pinnedChokepoints`, `planResolvedAtWarmup`), 025 adds:

```fsharp
// 025 US2: one findPath result per attack launch, reused by all combat units
//          in the launch. Invalidated per clarification Q3 (target death).
let mutable attackPathCache : AttackPathCache option = None

// 025 US1: PlanProgress now persists across Opening-phase ticks instead of
//          being re-created fresh in each resolvePlan call. Carries
//          ConsumedSlots / InFlight / Unfulfillable across ticks.
let mutable planProgress : BasePlan.PlanProgress = BasePlan.emptyPlanProgress
```

Both mutables are declared near the top of the script next to the existing 024 mutables (~line 273 region). No new module-level types; `AttackPathCache` is a local record defined once above the mutable declaration.

## Warmup path edits (US4 / FR-014, FR-015)

**Location**: the warmup block around lines 820–905. The 024 partial builds a synthetic `MapGrid` skeleton inline (lines 874–891).

**Edit**:

1. Replace the synthetic `planMapGrid` construction with a real-`MapGrid` load from the extended `MapGridCache` (per R1 cache-extension decision):
   ```fsharp
   let planMapGrid : MapGrid =
       match MapGridCache.loadFromJson fullCachePath with
       | Some grid -> grid  // US4 real grid path
       | None when MapTargetSet.contains mapName ->
           failwithf "[warmup] no MapGrid in cache at %s — run scripts/examples/14-cache-map-analysis.fsx '%s'" fullCachePath mapName
       | None ->
           printfn "[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx"
           // fall through to 024 synthetic skeleton
           { WidthElmos = w * 8 ; HeightElmos = h * 8; WidthHeightmap = w; HeightHeightmap = h
             HeightMap = Array2D.create (w + 1) (h + 1) cy
             SlopeMap = Array2D.zeroCreate w h
             ResourceMap = Array2D.zeroCreate w h
             LosMap = Array2D.zeroCreate w h
             RadarMap = Array2D.zeroCreate w h }
   mapGrid <- Some planMapGrid
   ```
2. `MapGridCache.loadFromJson` is a local helper declared in `bot_macro.fsx` (not a new module in `FSBar.Client`) that reads the JSON cache file, base64-decodes the gzipped payload, decompresses, and reconstructs the `Array2D<float32>` fields. Implementation fits in ~40 LOC inside the bot script.
3. `MapTargetSet.contains` is a local helper that returns `true` for map names in the 025 target set. Initial implementation is a single-element list `[ "Avalanche 3.4" ]`; expanding the target set is a one-line edit per clarification Q2.
4. The warmup block is wrapped in a `Stopwatch` measurement to enforce FR-015 (< 100 ms total CPU). If the budget is blown, the warmup emits a `[warmup] WARN: CPU budget exceeded: <ms> ms > 100 ms` trace — non-fatal, but a signal for iteration analysis.

## Opening-phase command emission edits (US1 / FR-001..FR-005)

**Location**: the opening-phase tactics tick handler (currently delegates to `helpers/opening_build.nextOpeningCommand`).

**Edit**:

1. At the start of every Opening-phase tactics tick (clarification Q4 cadence: ~30 game frames / ~1 sim-second):
   ```fsharp
   let existingStructures = currentExistingStructures ()  // live GameState.Units filtered to finished structures
   let resolveContext =
       { Grid = mapGrid |> Option.defaultValue synthSkeleton
         BaseCentre = commanderPos
         CommanderPos = commanderPos
         MetalSpotsNearest = sortedMetalSpots
         Chokepoints = pinnedChokepoints
         UnitDefs = client.GameState.UnitDefs
         ExistingStructures = existingStructures
         Progress = planProgress }
   let resolved =
       try Some (BasePlan.resolvePlan BasePlan.defaultArmadaOpening resolveContext)
       with ex ->
           printfn "[plan] resolvePlan exception — falling back to 023 helper: %s" ex.Message
           None
   ```
2. For the first `ResolvedSlot` where `BuildableNow = true` and `Slot.Name` not already in `planProgress.ConsumedSlots`:
   ```fsharp
   match r.Position with
   | Some (px, py, pz) ->
       printfn "[plan] issuing BuildCommand %s @ (%.0f,%.0f) from resolvePlan" r.Slot.DefName px pz
       let cmd = Commands.BuildCommand builderId r.Slot.DefId px py pz 0
       planProgress <- BasePlan.markInFlight planProgress r.Slot
       Some cmd
   | None -> None
   ```
3. On `UnitFinished` events matching a slot's `DefName`:
   ```fsharp
   planProgress <- BasePlan.markConsumed planProgress slot
   ```
4. Exception fallback path: if `resolvePlan` throws (not `Failure` — an actual exception), log the `[plan] resolvePlan exception` trace and call `opening_build.nextOpeningCommand` once as a belt-and-suspenders recovery (FR-005). The helper stays in-tree on this path only (FR-006 / FR-020).

**FR check**:

- FR-001: resolvePlan fires every Opening tactics tick + warmup. ✓
- FR-002: `BuildCommand` at exact `Position`. ✓
- FR-003: `markInFlight` on issue. ✓
- FR-004: `markConsumed` on `UnitFinished`. ✓
- FR-005: exception fallback with explicit trace. ✓

## Attack-phase command emission edits (US2 / FR-007..FR-011)

**Location**: the attack launcher around `bot_macro.fsx:623` where `helpers/attack_launch.fsx` is invoked.

**Edit**:

1. On the first Attack-phase tick (or on a cache miss per Q3):
   ```fsharp
   let target = pickAttackTarget client.GameState
   match target, mapGrid with
   | Some (tid, tpos), Some grid ->
       let combat = client.GameState.Units |> Seq.filter (fun (_, u) -> Attack_launch.isCombatDef client.GameState.UnitDefs u.DefId && u.IsFinished) |> Seq.toList
       let centre = centreOfMass (combat |> List.map (fun (_, u) -> u.Position))
       let ownStructures = currentOwnStructureFootprints ()
       let budget = 50  // ms — Pathing.findPath default
       match Pathing.findPath grid MoveType.Kbot ownStructures centre tpos budget with
       | Result.Ok path ->
           let statusStr = match path.Status with | Complete -> "Complete" | Partial true -> "Partial budget-exhausted" | _ -> "Partial"
           printfn "[attack] path waypoints=%d cost=%.1f status=%s" path.Waypoints.Length path.Cost statusStr
           attackPathCache <- Some { TargetUnitId = tid; TargetPosition = tpos; Path = path; LaunchTick = currentFrame }
           emitWaypointCommands combat path.Waypoints
       | Result.Error NoRoute ->
           printfn "[attack] findPath NoRoute — falling back to direct move"
           attackPathCache <- None
           combat |> List.iter (fun (uid, _) -> emit (Commands.MoveCommand uid (fst3 tpos) 100.0f (trd3 tpos)))
   | _ -> ...  // existing fallback
   ```
2. Where `emitWaypointCommands` is:
   ```fsharp
   let emitWaypointCommands (units: (int * Unit) list) (waypoints: Waypoint list) =
       for (uid, _) in units do
           match waypoints with
           | [] -> ()
           | first :: rest ->
               // first waypoint: unqueued — replaces any existing order
               emit (Commands.MoveCommand uid first.X first.Y first.Z)
               // remaining waypoints: queued — append to order queue
               for w in rest do
                   emit (Commands.MoveCommandQueued uid w.X w.Y w.Z)
   ```
3. On subsequent Attack-phase ticks, if `attackPathCache` is `Some` and the target is still in `GameState.Units`, reuse the cached path for any NEW combat units that weren't in the previous tick's combat list (FR-009 "joining on later ticks"). Do NOT re-run `findPath`.
4. On the target-death check (clarification Q3 / FR-009a): at the start of each Attack tactics tick, before reusing the cache:
   ```fsharp
   match attackPathCache with
   | Some cache when not (client.GameState.Units |> Seq.exists (fun (id, _) -> id = cache.TargetUnitId)) ->
       printfn "[attack] target %d absent from GameState — re-pathing" cache.TargetUnitId
       attackPathCache <- None
       // fall through to the first-tick branch above, which re-runs pickAttackTarget + findPath in THIS tick
   | _ -> ()
   ```
5. Partial path (FR-011 / clarification Q5): the `Partial budget-exhausted` branch of the `Result.Ok` match is terminal. No retry, no bookkeeping beyond the standard `attackPathCache <- Some { ... }` assignment. Re-pathing only via target-death (step 4) or attack-phase exit.

**FR check**:

- FR-007: one `findPath` per launch. ✓
- FR-008: N `MoveCommand`s per unit, first unqueued + rest queued. ✓ (uses the new `Commands.MoveCommandQueued`)
- FR-009: cached reuse for joiners. ✓
- FR-009a: target-death invalidation. ✓
- FR-010: NoRoute fallback to direct `MoveCommand`. ✓
- FR-011: Partial waypoints issued as-is, no retry. ✓

## Defend-interrupt edits (US3 / FR-012, FR-013)

**Location**: `bot_macro.fsx:461` — the `nearestChokepointTo centre pinnedChokepoints` branch where the 024 partial built `myIds` from `|> Seq.filter (fun (_, u) -> u.IsFinished)`.

**Edit**:

```fsharp
// Before (024 partial, current buggy behaviour):
let myIds =
    client.GameState.Units
    |> Seq.filter (fun (_, u) -> u.IsFinished)
    |> Seq.map fst
    |> Seq.toList

// After (025 US3):
let myIds =
    client.GameState.Units
    |> Seq.filter (fun (_, u) -> u.IsFinished && Attack_launch.isCombatDef client.GameState.UnitDefs u.DefId)
    |> Seq.map fst
    |> Seq.toList
printfn "[defend] routing combat units only n=%d" myIds.Length
let cmds =
    if List.isEmpty myIds then
        printfn "[defend] no combat units available — commander fallback"
        match nearestEnemyId client.GameState centre intruders with
        | Some eid -> [ Commands.AttackCommand commanderId eid ]
        | None -> []
    else
        [ for uid in myIds -> Commands.MoveCommand uid cpx 0.0f cpz ]
```

**FR check**:

- FR-012: `isCombatDef` filter. ✓
- FR-013: commander fallback when no combat units. ✓

## Invariant preservation (US5 / FR-018, FR-019, FR-020, FR-021)

- **FR-018**: live NullAI run produces `result.json.cause = "commander-death-win-after-upgrade"`. Validated by SC-001 live iteration.
- **FR-019**: after every commit on the 025 branch, `bash bots/trainer/run.sh NullAI <iter>` with the rush bot (`bot.fsx`) MUST still produce `outcome=win`. Validated by SC-004 rush smoke per iteration.
- **FR-020**: `helpers/opening_build.fsx` file stays in-tree and compiles. Consumed only on FR-005 exception fallback. Check: `ls bots/trainer/helpers/opening_build.fsx` is present at feature end.
- **FR-021**: 024 primitive modules (`Pathing.fs`, `SmfParser.fs`, `Chokepoints.fs`, `WallIn.fs`, `BasePlan.fs` and their `.fsi` siblings) are NOT edited by 025. Check: `git diff 024-head..025-head -- src/FSBar.Client/{Pathing,SmfParser,Chokepoints,WallIn,BasePlan}.{fs,fsi}` is empty.

## Commit discipline

Per the spec framing "one atomic commit" and 023 PLAYBOOK §2c "one fix per iter," the US1+US2+US3+US4 integration lands in **one commit** after the `.fsi`/`.fs` Tier 1 delta and its unit test have landed in a prior commit. Commit order on the 025 branch:

1. `Commands.fsi` + `Commands.fs` + baseline refresh + unit test — Tier 1 behaviour-non-changing plumbing commit. Rush bot unaffected; macro bot unaffected.
2. `scripts/examples/14-cache-map-analysis.fsx` extension + cache regeneration for Avalanche 3.4. Rush bot unaffected; macro bot unaffected.
3. `bot_macro.fsx` US1+US2+US3+US4 integration + MapGridCache.loadFromJson helper. Rush bot unaffected; macro bot now primitive-driven. First live iteration runs at this commit.
4. If iteration 1 fails: one fix-per-iter commit per 023 PLAYBOOK §2c, up to 3 iters (SC-007). Halt at 3 and file mailbox per PLAYBOOK §10.
