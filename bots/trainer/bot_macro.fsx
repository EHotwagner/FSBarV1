// bots/trainer/bot_macro.fsx — the macro builder-economy bot for feature 023.
//
// Lives alongside bot.fsx (the rush bot). Selected at the runner via
//   BOT_SCRIPT=bot_macro.fsx bash bots/trainer/run.sh <rung> <iter>
//
// This is intentionally a thin skeleton at T003 — no strategic actions yet.
// Iterations on user stories US1..US4 will grow the opening, production,
// upgrade, and attack phases inline and then extract them into helpers
// under the 020 FR-020 two-site / two-iteration extraction rule.
//
// Required env vars are identical to bot.fsx:
//   HIGHBAR_BOT_RUN_DIR   absolute path to the pre-created run directory
//   BOT_OPPONENT          opponent AI short name
//   BOT_OPPONENT_OPTIONS  JSON object of opponent options
//   BOT_MAP               map name
//   BOT_SEED              RNG seed (informational)
//   BOT_MAX_FRAMES        frame limit for this rung
//   BOT_GAME_SPEED        optional, defaults to 100
//   BOT_SCRIPT            set by run.sh, informational only

#load "helpers/prelude.fsx"
#load "helpers/viewer.fsx"
#load "helpers/log.fsx"
#load "helpers/perception.fsx"
#load "helpers/tactics.fsx"
#load "helpers/opening_build.fsx"
#load "helpers/production_queue.fsx"
#load "helpers/constructor_dispatch.fsx"
#load "helpers/upgrade_gate.fsx"
#load "helpers/attack_launch.fsx"

open System
open System.Diagnostics
open System.IO
open System.IO.Compression
open System.Text.Json
open FSBar.Client
open FSBar.Client.Commands
open Log
open Perception
open Tactics
open Viewer
open Opening_build
open Production_queue
open Constructor_dispatch
open Upgrade_gate
open Attack_launch

let envOrFail (name: string) : string =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> failwithf "required environment variable %s is unset" name
    | v -> v

let envOr (name: string) (defaultValue: string) : string =
    match Environment.GetEnvironmentVariable(name) with
    | null | "" -> defaultValue
    | v -> v

let parseOpponentOptions (json: string) : Map<string, string> =
    if String.IsNullOrWhiteSpace(json) || json = "{}" then Map.empty
    else
        try
            use doc = JsonDocument.Parse(json)
            doc.RootElement.EnumerateObject()
            |> Seq.map (fun p -> p.Name, p.Value.GetString())
            |> Map.ofSeq
        with ex ->
            printfn "[trainer] WARNING: failed to parse BOT_OPPONENT_OPTIONS: %s" ex.Message
            Map.empty

let runDir = envOrFail "HIGHBAR_BOT_RUN_DIR"
let opponent = envOrFail "BOT_OPPONENT"
let opponentOptionsJson = envOr "BOT_OPPONENT_OPTIONS" "{}"
let mapName = envOrFail "BOT_MAP"
let maxFrames =
    let fullViz =
        match Environment.GetEnvironmentVariable("BOT_FULL_VIZ") with
        | "1" -> true
        | _ -> false
    if fullViz then Int32.MaxValue
    else Int32.Parse(envOrFail "BOT_MAX_FRAMES")
let _seed = Int32.Parse(envOr "BOT_SEED" "1")
let gameSpeed = Int32.Parse(envOr "BOT_GAME_SPEED" "100")
let botScript = envOr "BOT_SCRIPT" "bot_macro.fsx"

let opponentOptions = parseOpponentOptions opponentOptionsJson

printfn "[trainer] bot_macro.fsx starting (BOT_SCRIPT=%s)" botScript
printfn "[trainer] run_dir=%s opponent=%s map=%s max_frames=%d"
    runDir opponent mapName maxFrames

let logger = createLogger runDir

// 023 T009: MacroPhase state machine — the macro archetype's four
// phases plus the FR-016b defend interrupt. currentPhase is mutated
// via transitionTo, which records the transition via logPhaseTransition
// so post-run diagnosis can cat phase_transitions.jsonl | jq -c .
// preDefendPhase stores the phase to resume after a Defending interrupt.
type MacroPhase =
    | Opening
    | Production
    | Upgrade
    | Attack
    | Defending

let phaseName (p: MacroPhase) : string =
    match p with
    | Opening -> "Opening"
    | Production -> "Production"
    | Upgrade -> "Upgrade"
    | Attack -> "Attack"
    | Defending -> "Defending"

let mutable currentPhase : MacroPhase = Opening
let mutable preDefendPhase : MacroPhase = Opening

/// Mutate currentPhase and emit a phase_transitions.jsonl line. Reason
/// is a short slug matching the allowlist in
/// contracts/phase-transition-record.md §"Allowed reason slugs".
let transitionTo (frame: uint32) (next: MacroPhase) (reason: string) : unit =
    let fromName = phaseName currentPhase
    let toName = phaseName next
    currentPhase <- next
    logPhaseTransition logger {
        Frame = frame
        From = fromName
        To = toName
        Reason = reason
        Telemetry = None
        Notes = None
    }
    printfn "[macro] frame=%d transition %s→%s reason=%s" frame fromName toName reason

// DeathMode "builders" matches bot.fsx — the only way to reliably end a
// match in BAR without the game_end gadget being present is to kill the
// sole enemy builder (the commander), which triggers the allyteam-wide
// death path via game_team_com_ends.lua.
let config =
    let baseConfig = EngineConfig.defaultConfig ()
    { baseConfig with
        MapName = mapName
        OpponentAI = opponent
        OpponentAIOptions = opponentOptions
        DeathMode = "builders"
        GameSpeed = gameSpeed }

logStart logger config

// ===========================================================================
// 023 T010 (FR-016b): defend-interrupt helpers — unchanged from Phase 2.
// ===========================================================================
let baseRadius : float32 = 1200.0f

let mutable baseCentre : (float32 * float32 * float32) option = None

let private nearestEnemyId
    (gs: GameState)
    (origin: float32 * float32 * float32)
    (candidates: Set<int>)
    : int option =
    if Set.isEmpty candidates then None
    else
        let (ox, _, oz) = origin
        candidates
        |> Set.toSeq
        |> Seq.choose (fun eid ->
            match Map.tryFind eid gs.Enemies with
            | Some e ->
                let (ex, _, ez) = e.Position
                let dx = ex - ox
                let dz = ez - oz
                Some (eid, dx * dx + dz * dz)
            | None -> None)
        |> Seq.sortBy snd
        |> Seq.tryHead
        |> Option.map fst

// ===========================================================================
// 023 T015: opening-build state now lives in helpers/opening_build.fsx.
// Everything below is the macro bot's local wiring: one ResolvedOpeningBuild-
// Order, one mutable OpeningProgress, a critter-filter for the defend
// interrupt, and the sortedMetalSpots array that the helper consumes.
// ===========================================================================
let commanderIdleThreshold : uint32 = 300u

let mutable resolvedOpening : ResolvedOpeningBuildOrder =
    { Items = [||]; FactoryDefId = -1 }
let mutable openingProgress : OpeningProgress = emptyProgress
let mutable sortedMetalSpots : (float32 * float32 * float32 * float32) array = [||]

// Iter 002 fix: filter NullAI's critter_penguin units out of the defend
// interrupt so harmless static wildlife doesn't trap the bot in Defending.
let mutable critterDefIds : Set<int> = Set.empty

let resolveCritterDefIds (cache: UnitDefCache) : Set<int> =
    UnitDefCache.all cache
    |> Seq.choose (fun info ->
        if info.Name.StartsWith("critter") then Some info.DefId else None)
    |> Set.ofSeq

// ===========================================================================
// 023 T019: production queue state now lives in helpers/production_queue.fsx.
// bot_macro.fsx keeps a ResolvedQueuePolicy + mutable QueueState plus a
// one-shot guard-issued flag for the T018 commander-assists-factory fix.
// ===========================================================================
let mutable resolvedQueuePolicy : ResolvedQueuePolicy =
    { FactoryDefId = -1
      ResolvedItems = [||]
      MinQueueDepth = 3
      TargetConstructorRatio = 0.4f
      MinCombatIncomeThreshold = 10.0f }
let mutable queueState : QueueState = emptyQueueState
let mutable commanderGuardIssued : bool = false        // T018: one-shot guard

// ===========================================================================
// 023 T020 (US2 iter 010, FR-007): idle-constructor dispatch. Finds
// armck constructors that are finished + idle and assigns each one a
// BuildCommand for armmex at the next free metal spot (sortedMetalSpots
// beyond the opening's first two). FR-007 defect signal fires when a
// constructor's idle duration exceeds idleConstructorThreshold.
// ===========================================================================
let idleConstructorThreshold : uint32 = 300u
let mutable dispatchState : DispatchState = emptyDispatchState 2  // opening uses 0, 1
let mutable armmexDefId : int = -1
let mutable armckDefId : int = -1

// ===========================================================================
// 023 T023 (US3 iter 013, FR-009..FR-012): inline upgrade gate. Entry
// predicate fires when metal income and factory-built count cross the
// thresholds; during Upgrade, an idle armck is redirected to build an
// advanced kbot lab (armalab). On UnitFinished for armalab → transition
// to Attack with reason `upgrade-reached-normal`. FR-012 stall path:
// if frame > upgradeDeadlineFrame and upgrade not reached, record
// `upgrade-stall-no-army` and do NOT enter Attack.
// ===========================================================================
// Upgrade thresholds — the helper consumes these via a plain record.
// upgradeDeadlineFrame is kept as a top-level mutable so T027 stall
// verification can lower/restore it without rebuilding the thresholds.
let upgradeEntryMetalIncome : float32 = 20.0f
let upgradeEntryProductionCount : int = 6
// Main-path deadline. T027 temporarily lowered this to 1800u to trip
// FR-012 and verified `decideUpgradeExit` returned StallAndLose
// against the contract invariants. Restored to 16000 here.
let mutable upgradeDeadlineFrame : uint32 = 16000u
let combatUnitThreshold : int = 12

let buildUpgradeThresholds () : UpgradeThresholds = {
    MetalIncome = upgradeEntryMetalIncome
    InitialProductionCount = upgradeEntryProductionCount
    DeadlineFrame = upgradeDeadlineFrame
    CombatUnitThreshold = combatUnitThreshold
}

let mutable armalabDefId : int = -1
let mutable upgradeGateState : UpgradeGateState = emptyUpgradeGateState
let mutable advancedLabRequested : bool = false       // one-shot dispatch
let mutable advancedLabUnitId : int option = None

// ===========================================================================
// 023 T030 (US4): attack_launch state. Classifier + count live in the
// helper; bot keeps a `launched` set for incremental launch so new
// factory-produced combat units join the attack each frame.
// ===========================================================================
let mutable attackLaunched : bool = false
let mutable combatUnitsLaunched : Set<int> = Set.empty

// ===========================================================================
// 024 US5: tactical primitives integration. Warmup pins:
//   - mapGrid: MapGrid.loadFromEngine-loaded grid (static for the match)
//   - pinnedChokepoints: the Chokepoints.findChokepoints result ordered by
//     distance from base — used by the defend interrupt to route interceptors
//     to the nearest canyon entrance rather than chasing individual enemies.
//   - planResolvedAtWarmup: BasePlan.resolvePlan output for the default
//     Armada opening. Emitted as [plan] traces only in this commit; the
//     023 opening_build helper still drives command emission. A follow-up
//     iteration will switch the command path to consume ResolvedSlots
//     directly (per US5 spec §T060 c-d), deferred here because live game
//     iteration validation is out of scope for this session.
//   - attackPathByUnitId: per-combat-unit path cache so [attack] traces
//     show waypoint count / cost / status on each launch without
//     re-running findPath every frame.
// ===========================================================================
let mutable mapGrid : MapGrid option = None
let mutable pinnedChokepoints : Chokepoint list = []
let mutable planResolvedAtWarmup : ResolvedSlot list = []
let mutable attackPathReported : Set<int> = Set.empty

// ===========================================================================
// 025 US1/US2: primitive-driven command path state.
// ===========================================================================

/// 025 US2 cache: one findPath result per attack launch, reused by all
/// combat units in the launch (FR-007/FR-009). Invalidated per clarification
/// Q3 (target death) via the start-of-attack-tick check.
type AttackPathCache =
    { TargetUnitId: int option
      TargetPosition: float32 * float32 * float32
      Path: Path
      LaunchTick: uint32 }

let mutable attackPathCache : AttackPathCache option = None

/// 025 US1: PlanProgress persisted across Opening tactics ticks so
/// `markInFlight`/`markConsumed` state carries forward instead of being
/// reset on every resolvePlan call (FR-003/FR-004).
let mutable planProgress : PlanProgress = BasePlan.emptyPlanProgress

/// 025 US1 FR-015: warmup CPU budget stopwatch — elapsed milliseconds
/// from first 024 warmup step (chokepoint cache load) through
/// resolvePlan. Enforced as a non-fatal [warmup] WARN trace.
let mutable warmupCpuElapsedMs : int64 = 0L

/// 025 US4 target set: maps for which missing MapGrid cache is a hard
/// warmup failure (clarification Q2). Single-element list for now;
/// expanding is a one-line edit.
let mapTargetSet : Set<string> = Set.ofList [ "Avalanche 3.4" ]

/// 025 data-model §2: authoritative structure classifier. Any DefName
/// in the active plan is a structure. Auto-updates when
/// defaultArmadaOpening is edited.
let structureDefNames : Set<string> =
    BasePlan.defaultArmadaOpening.Slots
    |> List.map (fun s -> s.DefName)
    |> Set.ofList

let private isStructureDef (cache: UnitDefCache) (defId: int) : bool =
    match UnitDefCache.tryFindById cache defId with
    | Some info -> Set.contains info.Name structureDefNames
    | None -> false

/// Pick the nearest chokepoint to a world position. Returns None when the
/// pinned list is empty (e.g., findChokepoints returned [] or warmup failed).
let private nearestChokepointTo
    (origin: float32 * float32 * float32)
    (chokepoints: Chokepoint list)
    : Chokepoint option =
    if List.isEmpty chokepoints then None
    else
        let (ox, _, oz) = origin
        chokepoints
        |> List.sortBy (fun cp ->
            let (cx, _, cz) = cp.Position
            let dx = cx - ox
            let dz = cz - oz
            dx * dx + dz * dz)
        |> List.tryHead

/// Live derivation of own-structure footprints from GameState.Units per
/// the 025 data-model §2 structure classifier (plan-DefName set). Used
/// both as `ResolveContext.ExistingStructures` in the Opening phase and
/// as `ownStructures` in the Attack phase's Pathing.findPath call.
/// Single source of truth — do not reintroduce a parallel classifier.
let private currentOwnStructureFootprints (gs: GameState) : OwnStructureFootprint list =
    gs.Units
    |> Map.toSeq
    |> Seq.choose (fun (_, u) ->
        if u.IsFinished && isStructureDef gs.UnitDefs u.DefId then
            let tag =
                match UnitDefCache.tryFindById gs.UnitDefs u.DefId with
                | Some info -> Some info.Name
                | None -> None
            Some
                { Centre = u.Position
                  RadiusElmos = 24.0f
                  Tag = tag }
        else None)
    |> Seq.toList

/// 025 US2 FR-009a: identify the enemy commander unit id + position via
/// the unique-DefId heuristic (same as perception.pickEnemyCommanderPos
/// but also returns the unit id so AttackPathCache can invalidate on
/// target death). Returns None when the Enemies map is empty or no
/// enemy has a unique DefId.
let private pickEnemyCommanderUnitId (gs: GameState) : (int * (float32 * float32 * float32)) option =
    if Map.isEmpty gs.Enemies then None
    else
        let defCounts =
            gs.Enemies
            |> Map.toSeq
            |> Seq.choose (fun (_, e) -> e.DefId)
            |> Seq.countBy id
            |> Map.ofSeq
        gs.Enemies
        |> Map.toSeq
        |> Seq.tryFind (fun (_, e) ->
            match e.DefId with
            | Some d -> Map.tryFind d defCounts = Some 1
            | None -> false)
        |> Option.map (fun (eid, e) -> eid, e.Position)

/// 025 US2 FR-008: emit one `MoveCommand` (unqueued, replaces current
/// order) for the first waypoint plus `MoveCommandQueued` (SHIFT_KEY
/// appended) for every subsequent waypoint so each combat unit traces
/// the path in order. On a Partial-budget-exhausted path, the pathfinder
/// may stop well short of the real target — append the real target
/// position as one final queued move so units continue past the last
/// waypoint and engage whatever is in range at the goal (clarification Q5
/// says the bot does not retry findPath for Partial, so this post-path
/// target ensures forward progress without a second findPath call).
let private emitWaypointCommands
    (units: int list)
    (waypoints: (float32 * float32 * float32) array)
    (targetPos: float32 * float32 * float32)
    : Highbar.AICommand list =
    let (tx, ty, tz) = targetPos
    if Array.isEmpty waypoints then
        [ for uid in units -> MoveCommand uid tx ty tz ]
    else
        let (fx, fy, fz) = waypoints.[0]
        [ for uid in units do
            yield MoveCommand uid fx fy fz
            for i in 1 .. waypoints.Length - 1 do
                let (wx, wy, wz) = waypoints.[i]
                yield MoveCommandQueued uid wx wy wz
            yield MoveCommandQueued uid tx ty tz ]

let tacticsFn : TrainerTacticsFn =
    fun client frame commanderIdOpt ->
        match commanderIdOpt with
        | None -> { Commands = []; VictoryDeclared = false }
        | Some cid ->
            let fnum = frame.FrameNumber

            if baseCentre.IsNone then
                baseCentre <- computeBaseCentre client.GameState cid

            // ---- Event processing ----
            // Opening: advance progress on UnitFinished of current item's def.
            // Production: delegate to production_queue.observeFrame.
            let mutable finishedDefIds : int list = []
            for ev in frame.Events do
                match ev with
                | GameEvent.UnitFinished uid ->
                    match Map.tryFind uid client.GameState.Units with
                    | Some u ->
                        finishedDefIds <- u.DefId :: finishedDefIds
                        if openingProgress.AwaitingCreated
                           && openingProgress.CurrentIndex < resolvedOpening.Items.Length then
                            let (expectedDefId, item) =
                                resolvedOpening.Items.[openingProgress.CurrentIndex]
                            if u.DefId = expectedDefId then
                                printfn "[opening] idx=%d %s finished (unit %d) at frame %d"
                                    openingProgress.CurrentIndex item.DefName uid fnum
                                openingProgress <- advanceOnFinished openingProgress fnum
                        // 025 FR-004: mark the first in-flight plan slot with
                        // a matching DefName consumed. Using `InFlight` as the
                        // matcher avoids consuming two mex slots on a single
                        // mex completion (both mex#1 and mex#2 share the
                        // "armmex" DefName).
                        match UnitDefCache.tryFindById client.GameState.UnitDefs u.DefId with
                        | Some info ->
                            let matching =
                                BasePlan.defaultArmadaOpening.Slots
                                |> List.tryFind (fun slot ->
                                    slot.DefName = info.Name
                                    && Set.contains slot.Name planProgress.InFlight)
                            match matching with
                            | Some slot ->
                                planProgress <- BasePlan.markConsumed planProgress slot.Name
                            | None -> ()
                        | None -> ()
                    | None -> ()
                | _ -> ()

            // Production queue observation (captures factory id + counts).
            let prevBuilt = queueState.ObservedBuilt
            queueState <-
                observeFrame resolvedQueuePolicy queueState frame.Events client.GameState
            if queueState.ObservedBuilt <> prevBuilt then
                printfn "[production] queue built update: {asked=%d,built=%d}"
                    (queueState.AskedCounts |> Map.toSeq |> Seq.sumBy snd)
                    (queueState.ObservedBuilt |> Map.toSeq |> Seq.sumBy snd)

            // Upgrade tracking: capture armalab unit id on UnitCreated;
            // on UnitFinished matching armalab defId, mark reached via
            // the helper (first-wins) so decideUpgradeExit sees it.
            for ev in frame.Events do
                match ev with
                | GameEvent.UnitCreated(uid, _) ->
                    match Map.tryFind uid client.GameState.Units with
                    | Some u when u.DefId = armalabDefId && advancedLabUnitId.IsNone ->
                        advancedLabUnitId <- Some uid
                        printfn "[upgrade] armalab unit=%d started" uid
                    | _ -> ()
                | GameEvent.UnitFinished uid ->
                    match Map.tryFind uid client.GameState.Units with
                    | Some u when u.DefId = armalabDefId ->
                        upgradeGateState <-
                            markReached upgradeGateState AdvancedFactory fnum
                        printfn "[upgrade] armalab finished → markReached"
                    | _ -> ()
                | _ -> ()

            // 025 US1: Opening→Production transitions when the factory slot
            // completes. The 023 openingComplete check relied on openingProgress
            // staying in sync with resolvedOpening.Items, which the 025 plan-
            // based issuance path bypasses. Use planProgress.ConsumedSlots as
            // the source of truth for the transition.
            if currentPhase = Opening && Set.contains "factory" planProgress.ConsumedSlots then
                transitionTo fnum Production "first-factory-finished"

            // Production → Upgrade via helper entryPredicateMet (FR-009)
            if currentPhase = Production then
                let totalProduction =
                    queueState.ObservedBuilt |> Map.toSeq |> Seq.sumBy snd
                if entryPredicateMet client.GameState totalProduction (buildUpgradeThresholds()) then
                    transitionTo fnum Upgrade "upgrade-entry-predicate-met"

            // T028: Upgrade exit via decideUpgradeExit with real combat
            // count. "No degenerate rush" invariant is enforced by the
            // helper — Attack only fires when Reached.IsSome AND combat
            // ≥ threshold, or when deadline passed AND combat ≥
            // threshold (DeadlineFallback). StallAndLose when deadline
            // exceeded with no army.
            if currentPhase = Upgrade then
                let combatCount = countCombatUnits client.GameState client.GameState.UnitDefs
                let decision =
                    decideUpgradeExit
                        upgradeGateState
                        fnum
                        combatCount
                        (buildUpgradeThresholds())
                match decision with
                | AttackNow Normal ->
                    transitionTo fnum Attack "upgrade-reached-normal"
                | AttackNow DeadlineFallback ->
                    transitionTo fnum Attack "upgrade-deadline-fallback"
                | StallAndLose reason when not upgradeGateState.StallRecorded ->
                    upgradeGateState <- { upgradeGateState with StallRecorded = true }
                    logPhaseTransition logger {
                        Frame = fnum
                        From = "Upgrade"
                        To = "Upgrade"
                        Reason = reason
                        Telemetry = None
                        Notes = Some (sprintf "deadline=%d combat=%d" upgradeDeadlineFrame combatCount)
                    }
                    printfn "[upgrade] FR-012 stall: frame %d > deadline %d, combat=%d < %d"
                        fnum upgradeDeadlineFrame combatCount combatUnitThreshold
                | _ -> ()  // WaitLonger or already-stalled

            // ---- Defend interrupt (FR-016b) with critter filter ----
            let intruders =
                match baseCentre with
                | Some c ->
                    enemiesInBase client.GameState c baseRadius
                    |> Set.filter (fun eid ->
                        match Map.tryFind eid client.GameState.Enemies with
                        | Some e ->
                            match e.DefId with
                            | Some d -> not (Set.contains d critterDefIds)
                            | None -> true
                        | None -> false)
                | None -> Set.empty
            let intruderCount = Set.count intruders
            if intruderCount > 0 && currentPhase <> Defending then
                preDefendPhase <- currentPhase
                transitionTo fnum Defending "enemy-in-base"
            if intruderCount = 0 && currentPhase = Defending then
                transitionTo fnum preDefendPhase "enemy-cleared"

            // ---- Command generation ----
            let cmds =
                if currentPhase = Defending then
                    match baseCentre with
                    | None -> []
                    | Some centre ->
                        // 024 US5: prefer routing interceptors to the nearest
                        // chokepoint's approach side rather than chasing the
                        // nearest raider individually. Falls back to the 023
                        // nearest-enemy AttackCommand when no chokepoints were
                        // detected at warmup (open terrain / detection gap).
                        match nearestChokepointTo centre pinnedChokepoints with
                        | Some cp ->
                            let (cpx, _, cpz) = cp.Position
                            if not (Set.contains -1 combatUnitsLaunched) then
                                printfn "[defend] chokepoint pos=(%.0f,%.0f) width=%.0f id=%A"
                                    cpx cpz cp.WidthElmos cp.Id
                                combatUnitsLaunched <- Set.add -1 combatUnitsLaunched
                            // 025 FR-012: route only combat units (workers,
                            // constructors, commander continue their tasks).
                            let myIds =
                                client.GameState.Units
                                |> Map.toSeq
                                |> Seq.filter (fun (_, u) ->
                                    u.IsFinished
                                    && isCombatDef client.GameState.UnitDefs u.DefId)
                                |> Seq.map fst
                                |> Seq.toList
                            printfn "[defend] routing combat units only n=%d" myIds.Length
                            if List.isEmpty myIds then
                                // 025 FR-013: no combat units available — fall
                                // through to the 023 nearest-enemy commander
                                // fallback.
                                printfn "[defend] no combat units available — commander fallback"
                                match nearestEnemyId client.GameState centre intruders with
                                | Some eid -> [ AttackCommand cid eid ]
                                | None -> []
                            else
                                [ for uid in myIds -> MoveCommand uid cpx 0.0f cpz ]
                        | None ->
                            match nearestEnemyId client.GameState centre intruders with
                            | None -> []
                            | Some targetEid ->
                                let myIds =
                                    if Map.isEmpty client.GameState.Units then [ cid ]
                                    else
                                        client.GameState.Units
                                        |> Map.toSeq
                                        |> Seq.map fst
                                        |> Seq.toList
                                [ for uid in myIds -> AttackCommand uid targetEid ]
                elif currentPhase = Opening && Set.isEmpty planProgress.InFlight then
                    let commanderPos =
                        match Map.tryFind cid client.GameState.Units with
                        | Some u -> u.Position
                        | None -> (0.0f, 100.0f, 0.0f)
                    let centre = baseCentre |> Option.defaultValue commanderPos
                    // 025 US1 FR-001/FR-002: drive command emission from
                    // BasePlan.resolvePlan with live-derived structure
                    // footprints and persistent planProgress. Q4 cadence is
                    // naturally throttled by the opening-progress cycle
                    // (one command per structure completion, tens of seconds
                    // between emissions), so we can call resolvePlan on every
                    // "ready to issue" transition without a separate tick gate.
                    let resolveGrid =
                        match mapGrid with
                        | Some g -> g
                        | None ->
                            // Degraded fallback — only reached on a non-target-set
                            // cache-miss (warmup still succeeded with a synthetic
                            // skeleton in that case).
                            failwith "[plan] mapGrid missing at tactics tick (warmup invariant violated)"
                    let existingStructures = currentOwnStructureFootprints client.GameState
                    let resolveContext : ResolveContext =
                        { Grid = resolveGrid
                          BaseCentre = centre
                          CommanderPos = commanderPos
                          MetalSpotsNearest = sortedMetalSpots
                          Chokepoints = pinnedChokepoints
                          UnitDefs = client.GameState.UnitDefs
                          ExistingStructures = existingStructures
                          Progress = planProgress }
                    let resolvedOpt =
                        try Some (BasePlan.resolvePlan BasePlan.defaultArmadaOpening resolveContext)
                        with ex ->
                            // 025 FR-005/FR-006: exception fallback to the 023
                            // opening_build helper as belt-and-suspenders. Note
                            // that Failure cases (WouldWallIn, TerrainNotBuildable,
                            // etc.) return via ResolvedSlot.Failure and do NOT
                            // throw — only genuine exceptions land here.
                            printfn "[plan] resolvePlan exception — falling back to 023 helper: %s" ex.Message
                            None
                    match resolvedOpt with
                    | None ->
                        // FR-006: 023 helper recovery path.
                        match nextOpeningCommand
                                  resolvedOpening
                                  openingProgress
                                  cid
                                  commanderPos
                                  centre
                                  sortedMetalSpots with
                        | Some decision ->
                            let (tx, ty, tz) = decision.ChosenPosition
                            printfn "[opening] idx=%d issuing BuildCommand %s defId=%d @ (%.0f,%.0f,%.0f)"
                                openingProgress.CurrentIndex decision.ChosenDefName decision.ChosenDefId tx ty tz
                            openingProgress <- markIssued openingProgress fnum
                            [ decision.Command ]
                        | None -> []
                    | Some resolved ->
                        // FR-002: pick the first buildable-now slot whose
                        // Name is not already consumed. Failures are traced
                        // but not fatal — the bot waits for structures to
                        // land and the next tick retries.
                        let pickNext () =
                            resolved
                            |> List.tryFind (fun r ->
                                r.BuildableNow
                                && r.Position.IsSome
                                && not (Set.contains r.Slot.Name planProgress.ConsumedSlots)
                                && not (Set.contains r.Slot.Name planProgress.InFlight))
                        match pickNext () with
                        | Some r ->
                            let (px, py, pz) = r.Position.Value
                            // Resolve the slot's DefId via the UnitDefCache —
                            // PlanSlot carries only the DefName, not a pinned id.
                            match UnitDefCache.tryFindByName client.GameState.UnitDefs r.Slot.DefName with
                            | Some info ->
                                printfn "[plan] issuing BuildCommand %s @ (%.0f,%.0f) from resolvePlan"
                                    r.Slot.DefName px pz
                                planProgress <- BasePlan.markInFlight planProgress r.Slot.Name
                                openingProgress <- markIssued openingProgress fnum
                                [ BuildCommand cid info.DefId px py pz 0 ]
                            | None ->
                                printfn "[plan] slot %s DefName=%s unresolved in cache — skipping"
                                    r.Slot.Name r.Slot.DefName
                                []
                        | None ->
                            // No buildable-now slot this tick. Trace any Failure
                            // reasons for diagnostic visibility (not fatal).
                            for r in resolved do
                                match r.Failure with
                                | Some f ->
                                    printfn "[plan] slot %s (%s) failure %A"
                                        r.Slot.Name r.Slot.DefName f
                                | None -> ()
                            []
                elif currentPhase = Production then
                    match queueState.FactoryUnitId with
                    | None -> []
                    | Some fid ->
                        let guardCmd =
                            if not commanderGuardIssued then
                                commanderGuardIssued <- true
                                printfn "[production] commander guarding factory (unit %d)" fid
                                [ GuardCommand cid fid ]
                            else []
                        let (topUp, newState) =
                            computeQueueTopUp
                                resolvedQueuePolicy
                                queueState
                                client.GameState
                                fnum
                        if not (List.isEmpty topUp) then
                            printfn "[production] top-up submitted %d command(s) depth→%d income=%.1f"
                                (List.length topUp) resolvedQueuePolicy.MinQueueDepth
                                client.GameState.Metal.Income
                        queueState <- newState

                        // ---- T021 idle-constructor dispatch via helper ----
                        let (decisions, newDispatch) =
                            dispatchIdle
                                dispatchState
                                client.GameState
                                armckDefId
                                armmexDefId
                                sortedMetalSpots
                                fnum
                        for d in decisions do
                            let (mx, _, mz) = d.ChosenPosition
                            printfn "[dispatch] con %d → armmex spot[%d] (%.0f,%.0f)"
                                d.ConstructorId d.ChosenSpotIdx mx mz
                        dispatchState <- newDispatch
                        // FR-007 defect telemetry
                        let defectIds =
                            idleDefectCandidates dispatchState fnum idleConstructorThreshold
                        for uid in defectIds do
                            let sinceFrame =
                                Map.find uid dispatchState.IdleSinceFrame
                            printfn "[idle-dispatch-defect] constructor=%d elapsed=%d"
                                uid (fnum - sinceFrame)
                        dispatchState <- markDefectReported dispatchState defectIds
                        let dispatchCmds = decisions |> List.map (fun d -> d.Command)
                        guardCmd @ topUp @ dispatchCmds
                elif currentPhase = Upgrade then
                    // T023 Upgrade: keep the production queue running.
                    // ORDER MATTERS: pick the armalab builder BEFORE
                    // dispatchIdle runs — otherwise every fresh constructor
                    // gets claimed for a mex site on its first frame and
                    // advancedLabRequested never finds a fresh candidate
                    // (iter 015 symptom).
                    match queueState.FactoryUnitId with
                    | None -> []
                    | Some fid ->
                        let (topUp, newState) =
                            computeQueueTopUp
                                resolvedQueuePolicy
                                queueState
                                client.GameState
                                fnum
                        queueState <- newState

                        // Iter 019 confirmed armcom cannot build armalab
                        // directly (not in its BuildOptions); only armck
                        // can. So: fresh armck builds armalab, and once
                        // the armalab UnitCreated fires we send the
                        // commander to GuardCommand it — the commander's
                        // build speed (~300) + the armck's (~100) = ~400
                        // combined, finishing ~1700 metal in ~130s in-game.
                        let upgradeCmds =
                            if not advancedLabRequested then
                                let armckCandidates =
                                    client.GameState.Units
                                    |> Map.toSeq
                                    |> Seq.filter (fun (uid, u) ->
                                        u.IsFinished
                                        && u.DefId = armckDefId
                                        && not (Set.contains uid dispatchState.Dispatched))
                                    |> Seq.map fst
                                    |> Seq.toList
                                match armckCandidates with
                                | [] -> []
                                | builderId :: _ ->
                                    advancedLabRequested <- true
                                    let (bx, by, bz) =
                                        baseCentre |> Option.defaultValue (0.0f, 100.0f, 0.0f)
                                    // Place armalab adjacent to the existing
                                    // armlab factory (known-buildable area)
                                    // but offset so it doesn't overlap. The
                                    // opening built armlab via
                                    // NearBaseCentre(0, 350) = (bx, bz+350).
                                    // Use (bx+300, bz+350) for armalab.
                                    let (lx, ly, lz) =
                                        (bx + 300.0f, by, bz + 350.0f)
                                    printfn "[upgrade] armck %d → armalab @ (%.0f,%.0f,%.0f); commander %d guarding armck"
                                        builderId lx ly lz cid
                                    dispatchState <-
                                        { dispatchState with
                                            Dispatched =
                                                Set.add builderId dispatchState.Dispatched }
                                    // StopCommand commander first so its
                                    // current GuardCommand(factory) doesn't
                                    // linger, then GuardCommand(armck).
                                    [ BuildCommand builderId armalabDefId lx ly lz 0
                                      StopCommand cid
                                      GuardCommand cid builderId ]
                            else []

                        let (decisions, newDispatch) =
                            dispatchIdle
                                dispatchState
                                client.GameState
                                armckDefId
                                armmexDefId
                                sortedMetalSpots
                                fnum
                        dispatchState <- newDispatch
                        let dispatchCmds = decisions |> List.map (fun d -> d.Command)

                        topUp @ upgradeCmds @ dispatchCmds
                elif currentPhase = Attack then
                    // 025 US2: drive attack routing from Pathing.findPath.
                    // One findPath per launch, cached in attackPathCache and
                    // reused by every combat unit in the launch. Joiners on
                    // subsequent ticks consume the same cached waypoints.
                    // Invalidation: target death (FR-009a, Q3) — on mismatch
                    // we clear the cache, re-run pickAttackTarget + findPath
                    // in the same tick and emit waypoints to the active set.
                    let targetIdOpt = pickEnemyCommanderUnitId client.GameState
                    let fallbackPos = (3200.0f, 100.0f, 3200.0f)
                    let targetPos =
                        targetIdOpt
                        |> Option.map snd
                        |> Option.defaultValue fallbackPos

                    // Q3 target-death invalidation
                    match attackPathCache with
                    | Some cache ->
                        let stillAlive =
                            match cache.TargetUnitId with
                            | Some tid -> Map.containsKey tid client.GameState.Enemies
                            | None -> true  // positional target has no death signal
                        if not stillAlive then
                            printfn "[attack] target %A absent from GameState — re-pathing"
                                cache.TargetUnitId
                            attackPathCache <- None
                    | None -> ()

                    // Combat unit roster
                    let combatIds =
                        client.GameState.Units
                        |> Map.toSeq
                        |> Seq.filter (fun (_, u) ->
                            u.IsFinished
                            && isCombatDef client.GameState.UnitDefs u.DefId)
                        |> Seq.map fst
                        |> Seq.toList

                    let (launchCmds, newLaunched) =
                        match combatIds, mapGrid with
                        | [], _ -> [], combatUnitsLaunched
                        | _, None ->
                            // No grid — fall back to 024-partial direct-move
                            printfn "[attack] findPath skipped (no MapGrid) — direct move fallback"
                            launchFreshCombat
                                client.GameState
                                client.GameState.UnitDefs
                                targetPos
                                combatUnitsLaunched
                        | _, Some grid ->
                            // Emit waypoints to any combat unit not yet launched
                            let freshIds =
                                combatIds
                                |> List.filter (fun uid ->
                                    not (Set.contains uid combatUnitsLaunched))
                            if List.isEmpty freshIds then
                                [], combatUnitsLaunched
                            else
                                // Compute path if cache miss
                                let ensurePath () =
                                    match attackPathCache with
                                    | Some c -> Some c
                                    | None ->
                                        // FR-007 one findPath per launch — 50ms default budget
                                        let ownStructures =
                                            currentOwnStructureFootprints client.GameState
                                        let startPos =
                                            match Map.tryFind cid client.GameState.Units with
                                            | Some u -> u.Position
                                            | None -> targetPos
                                        // 100 ms / 100k expansions tracks the 024-era budget that
                                        // reached Complete on Avalanche 3.4 in the 024 [attack]
                                        // trace (default 50 ms was Partial-exhausted in iter 4).
                                        let budget : PathBudget =
                                            { WallClockMs = 100
                                              MaxExpansions = 100_000
                                              SlopeCost = 2.0f }
                                        match Pathing.findPath grid MoveType.Kbot ownStructures startPos targetPos budget with
                                        | Result.Ok path ->
                                            let statusStr =
                                                match path.Status with
                                                | Complete -> "Complete"
                                                | Partial true -> "Partial budget-exhausted"
                                                | Partial false -> "Partial"
                                            printfn "[attack] path waypoints=%d cost=%.1f status=%s"
                                                path.Waypoints.Length path.EstimatedCost statusStr
                                            let c =
                                                { TargetUnitId = Option.map fst targetIdOpt
                                                  TargetPosition = targetPos
                                                  Path = path
                                                  LaunchTick = fnum }
                                            attackPathCache <- Some c
                                            Some c
                                        | Result.Error NoRoute ->
                                            printfn "[attack] findPath NoRoute — falling back to direct move"
                                            attackPathCache <- None
                                            None
                                        | Result.Error err ->
                                            printfn "[attack] findPath error %A — falling back to direct move" err
                                            attackPathCache <- None
                                            None
                                match ensurePath () with
                                | Some cache ->
                                    let cmds = emitWaypointCommands freshIds cache.Path.Waypoints cache.TargetPosition
                                    let newSet =
                                        freshIds
                                        |> List.fold (fun acc uid -> Set.add uid acc) combatUnitsLaunched
                                    cmds, newSet
                                | None ->
                                    // FR-010 NoRoute / error fallback: direct-move via
                                    // the 023 helper (marks launched set in step).
                                    launchFreshCombat
                                        client.GameState
                                        client.GameState.UnitDefs
                                        targetPos
                                        combatUnitsLaunched

                    if not (List.isEmpty launchCmds) && not attackLaunched then
                        attackLaunched <- true
                        let (tx, _, tz) = targetPos
                        printfn "[attack] launching %d combat units at target (%.0f,%.0f)"
                            (List.length launchCmds) tx tz
                    combatUnitsLaunched <- newLaunched
                    let (topUp, newQueueState) =
                        computeQueueTopUp
                            resolvedQueuePolicy
                            queueState
                            client.GameState
                            fnum
                    queueState <- newQueueState
                    launchCmds @ topUp
                else
                    []

            // ---- FR-002 commander-idle defect detector ----
            if currentPhase = Opening
               && openingProgress.CurrentIndex < resolvedOpening.Items.Length
               && openingProgress.AwaitingCreated
               && fnum > openingProgress.LastCommandFrame then
                let elapsed = fnum - openingProgress.LastCommandFrame
                if elapsed > commanderIdleThreshold && not openingProgress.IdleDefectEmitted then
                    openingProgress <- { openingProgress with IdleDefectEmitted = true }
                    let (_, item) = resolvedOpening.Items.[openingProgress.CurrentIndex]
                    printfn "[commander-idle-defect] frame=%d openingIndex=%d elapsed=%d item=%s"
                        fnum openingProgress.CurrentIndex elapsed item.DefName
                    // T016b: on first defect emission, probe the commander's
                    // live position via getUnitPos so we can see whether it
                    // ever actually moved after receiving the BuildCommand.
                    try
                        let (lx, ly, lz) = Callbacks.getUnitPos client.Stream cid
                        let cached =
                            match Map.tryFind cid client.GameState.Units with
                            | Some u ->
                                let (cx, cy, cz) = u.Position
                                sprintf "(%.0f,%.0f,%.0f)" cx cy cz
                            | None -> "?"
                        printfn "[probe-idle] commander %d livePos=(%.0f,%.0f,%.0f) cachedPos=%s"
                            cid lx ly lz cached
                    with ex ->
                        printfn "[probe-idle] getUnitPos failed: %s" ex.Message

            // T016b: periodic commander position trace every 600 frames
            // while in Opening phase so we can see movement (or lack of).
            if currentPhase = Opening && fnum > 0u && fnum % 600u = 0u then
                try
                    let (lx, ly, lz) = Callbacks.getUnitPos client.Stream cid
                    let intruderDbg = Set.count intruders
                    printfn "[probe-periodic] frame=%d livePos=(%.0f,%.0f,%.0f) openingIdx=%d awaiting=%b intruders=%d"
                        fnum lx ly lz openingProgress.CurrentIndex openingProgress.AwaitingCreated intruderDbg
                with _ -> ()

            { Commands = cmds; VictoryDeclared = false }

let mutable clientOpt : BarClient option = None
try
    try
        let client = new BarClient(config)
        clientOpt <- Some client
        printfn "[trainer] BarClient.Start()"
        client.Start()
        printfn "[trainer] BarClient connected"

        // ---- T015 warmup: delegate to opening_build helper ----
        resolvedOpening <- resolveOpeningBuildOrder client.GameState.UnitDefs defaultOpening
        printfn "[opening] resolved %d items; factoryDefId=%d"
            resolvedOpening.Items.Length resolvedOpening.FactoryDefId
        critterDefIds <- resolveCritterDefIds client.GameState.UnitDefs
        printfn "[opening] critterDefIds count=%d" (Set.count critterDefIds)
        // T019: resolve production queue via helper
        resolvedQueuePolicy <-
            resolveQueuePolicy client.GameState.UnitDefs defaultArmadaKbotPolicy
        printfn "[production] policy: factory=%d items=%d minDepth=%d ratio=%.2f gate=%.1f"
            resolvedQueuePolicy.FactoryDefId
            resolvedQueuePolicy.ResolvedItems.Length
            resolvedQueuePolicy.MinQueueDepth
            resolvedQueuePolicy.TargetConstructorRatio
            resolvedQueuePolicy.MinCombatIncomeThreshold

        // T020: resolve armmex + armck defIds for idle-constructor dispatch
        armmexDefId <-
            match UnitDefCache.tryFindByName client.GameState.UnitDefs "armmex" with
            | Some info -> info.DefId
            | None -> failwith "[dispatch] could not resolve armmex"
        armckDefId <-
            match UnitDefCache.tryFindByName client.GameState.UnitDefs "armck" with
            | Some info -> info.DefId
            | None -> failwith "[dispatch] could not resolve armck"
        printfn "[dispatch] armmex=%d armck=%d" armmexDefId armckDefId

        // T023 US3: resolve the advanced kbot lab for the upgrade gate.
        armalabDefId <-
            match UnitDefCache.tryFindByName client.GameState.UnitDefs "armalab" with
            | Some info -> info.DefId
            | None -> failwith "[upgrade] could not resolve armalab"
        printfn "[upgrade] armalab=%d deadline=%d entryIncome=%.1f entryProd=%d"
            armalabDefId upgradeDeadlineFrame upgradeEntryMetalIncome upgradeEntryProductionCount

        // iter 019 diagnostic: dump armcom + armck BuildOptions to see
        // whether armalab is directly buildable by either.
        let dumpBuildOptions name =
            match UnitDefCache.tryFindByName client.GameState.UnitDefs name with
            | Some info ->
                let hasArmalab = Array.contains armalabDefId info.BuildOptions
                let names =
                    info.BuildOptions
                    |> Array.choose (fun d ->
                        UnitDefCache.tryFindById client.GameState.UnitDefs d
                        |> Option.map (fun i -> i.Name))
                printfn "[upgrade] %s buildOptions=%d (hasArmalab=%b) firstFew=%A"
                    name info.BuildOptions.Length hasArmalab
                    (names |> Array.truncate 10)
            | None -> printfn "[upgrade] %s NOT FOUND" name
        dumpBuildOptions "armcom"
        dumpBuildOptions "armck"
        let allSpots = Callbacks.getMetalSpots client.Stream
        printfn "[opening] getMetalSpots returned %d spots" allSpots.Length
        // T016b diagnostic: dump every unit present at warmup so we can
        // see whether GameState.Units only contains the commander and
        // what its true position / def are. Also compare GameState's
        // cached pos against a live getUnitPos query.
        printfn "[warmup] GameState.Units.Count=%d" client.GameState.Units.Count
        for (KeyValue(uid, u)) in client.GameState.Units do
            let (ux, uy, uz) = u.Position
            let name =
                try Callbacks.getUnitDefName client.Stream u.DefId with _ -> "?"
            let livePos =
                try
                    let (lx, ly, lz) = Callbacks.getUnitPos client.Stream uid
                    sprintf "(%.0f,%.0f,%.0f)" lx ly lz
                with _ -> "?"
            printfn "[warmup] unit %d def=%d name=%s cached=(%.0f,%.0f,%.0f) live=%s"
                uid u.DefId name ux uy uz livePos
        let commanderPos =
            client.GameState.Units
            |> Map.toSeq
            |> Seq.tryHead
            |> Option.map (fun (_, u) -> u.Position)
            |> Option.defaultValue (0.0f, 100.0f, 0.0f)
        let (cx, cy, cz) = commanderPos
        sortedMetalSpots <- sortMetalSpotsByDistance cx cz allSpots
        printfn "[opening] commanderPos=(%.0f,%.0f,%.0f) sortedMetalSpots top5:" cx cy cz
        for i in 0 .. min 4 (sortedMetalSpots.Length - 1) do
            let (x, y, z, v) = sortedMetalSpots.[i]
            let dx = x - cx
            let dz = z - cz
            let dist = sqrt (dx * dx + dz * dz)
            printfn "[opening]   [%d] (%.0f,%.0f,%.0f) v=%.2f dist=%.0f" i x y z v dist

        // ---- 024 US5: load pre-computed map analysis from disk cache ----
        //
        // Maps are static so any analysis that depends ONLY on terrain is
        // a fixed function of the map file. Running it at bot warmup is
        // waste: a 250 ms findChokepoints call on Avalanche holds the
        // frame-reading path long enough for the engine to race 1500+ game
        // frames ahead, blow the proxy's socket write buffer, and trip
        // "Socket not writable, dropping frame" → Lua OOM. All of that work
        // must happen offline.
        //
        // The offline pipeline is `scripts/examples/14-cache-map-analysis.fsx`.
        // It parses the .sd7, runs findChokepoints with the default macro-bot
        // query, and writes `bots/trainer/map-cache/<safe-name>.json`. Run
        // it once per map before the first trainer iteration. The runtime
        // warmup here is a plain File.ReadAllText + JsonSerializer.Deserialize
        // that completes in < 10 ms.
        // 025 FR-015: wrap all warmup tactical work in a CPU budget
        // stopwatch — chokepoint load, MapGrid load, resolvePlan. Non-fatal
        // trace at > 100 ms; hard invariants live elsewhere.
        let warmupSw = Stopwatch.StartNew()
        try
            // 026: permanent committed cache path — look up the SupportedMap,
            // resolve the canonical cachePathFor, delegate parsing to
            // FSBar.Client.MapCacheFile. Hard-abort with formatLoadError on
            // any validation failure (FR-006).
            let repoRoot =
                // Prefer walking up from botScript; fall back to cwd if that
                // path isn't a descendant of the repo.
                let start =
                    try Path.GetFullPath(Path.GetDirectoryName(botScript))
                    with _ -> Directory.GetCurrentDirectory()
                let rec climb (d: string) =
                    if isNull d then Directory.GetCurrentDirectory()
                    elif File.Exists(Path.Combine(d, "pack-dev.sh")) then d
                    else climb (Path.GetDirectoryName d)
                climb start
            let planMapGrid : MapGrid =
                match MapCacheFile.tryFindSupportedMap mapName with
                | Some supported ->
                    let path = MapCacheFile.cachePathFor repoRoot supported
                    let sw = Stopwatch.StartNew()
                    match MapCacheFile.read supported path with
                    | Result.Ok loaded ->
                        sw.Stop()
                        printfn "[mapcache] loaded %s in %dms (codeVersion=%d)"
                            path sw.ElapsedMilliseconds MapCacheFile.codeVersion
                        pinnedChokepoints <- loaded.Chokepoints
                        printfn "[chokepoint] loaded %d chokepoints from cache %s"
                            loaded.Chokepoints.Length path
                        for cp in loaded.Chokepoints |> List.truncate 5 do
                            let (px, _, pz) = cp.Position
                            printfn "[chokepoint] pos=(%.0f,%.0f) width=%.0f id=%A distFromBase=%.0f"
                                px pz cp.WidthElmos cp.Id cp.DistanceFromBase
                        printfn "[mapgrid] loaded from cache %dx%d heightMap slopeMap resourceMap"
                            loaded.Grid.WidthHeightmap loaded.Grid.HeightHeightmap
                        loaded.Grid
                    | Result.Error err ->
                        failwith (MapCacheFile.formatLoadError err)
                | None when Set.contains mapName mapTargetSet ->
                    failwithf "[warmup] map \"%s\" is in mapTargetSet but not in MapCacheFile.supportedMaps"
                        mapName
                | None ->
                    printfn "[cache-miss] WARN: map \"%s\" not in MapCacheFile.supportedMaps — synthesising skeleton"
                        mapName
                    let w = Callbacks.getMapWidth client.Stream
                    let h = Callbacks.getMapHeight client.Stream
                    { WidthElmos = w * 8
                      HeightElmos = h * 8
                      WidthHeightmap = w
                      HeightHeightmap = h
                      HeightMap = Array2D.create (w + 1) (h + 1) cy
                      SlopeMap = Array2D.zeroCreate (w / 2) (h / 2)
                      ResourceMap = Array2D.zeroCreate w h
                      LosMap = Array2D.zeroCreate w h
                      RadarMap = Array2D.zeroCreate w h }
            mapGrid <- Some planMapGrid
            // Reset planProgress at warmup — fresh start each match.
            planProgress <- BasePlan.emptyPlanProgress
            let resolveContext : ResolveContext =
                { Grid = planMapGrid
                  BaseCentre = commanderPos
                  CommanderPos = commanderPos
                  MetalSpotsNearest = sortedMetalSpots
                  Chokepoints = pinnedChokepoints
                  UnitDefs = client.GameState.UnitDefs
                  ExistingStructures = currentOwnStructureFootprints client.GameState
                  Progress = planProgress }
            let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening resolveContext
            planResolvedAtWarmup <- resolved
            let okCount =
                resolved |> List.filter (fun r -> r.BuildableNow) |> List.length
            printfn "[plan] resolved %d slots (%d buildable now)" resolved.Length okCount
            for r in resolved do
                match r.Position, r.Failure with
                | Some(px, _, pz), None ->
                    printfn "[plan] slot %s (%s) resolved @ (%.0f,%.0f)"
                        r.Slot.Name r.Slot.DefName px pz
                | _, Some f ->
                    printfn "[plan] slot %s (%s) failed: %A" r.Slot.Name r.Slot.DefName f
                    if (match f with WouldWallIn _ -> true | _ -> false) then
                        printfn "[wall-in-defect] proposed=%s %A" r.Slot.Name f
                | None, None ->
                    printfn "[plan] slot %s (%s) skipped (consumed)" r.Slot.Name r.Slot.DefName
        with ex ->
            printfn "[tactical] warmup failed: %s" ex.Message
            // Re-raise so target-set hard-fail bubbles up to the top-level
            // error handler rather than silently proceeding with a null grid.
            reraise ()
        warmupSw.Stop()
        warmupCpuElapsedMs <- warmupSw.ElapsedMilliseconds
        printfn "[warmup] CPU budget %d ms (limit 100 ms)" warmupCpuElapsedMs
        if warmupCpuElapsedMs > 100L then
            printfn "[warmup] WARN: CPU budget exceeded: %d ms > 100 ms" warmupCpuElapsedMs

        // 024 US5 / HighBarV2 031 callback-frame-interleaving fix: all warmup
        // batch loads (UnitDefCache, MapGrid, getMetalSpots, findChokepoints,
        // resolvePlan) are complete by this point. Flip
        // Protocol.replayBufferEnabled ON so mid-game callback round-trips
        // (e.g. [probe-idle]'s Callbacks.getUnitPos in the commander-idle
        // defect detector) preserve the UnitFinished / UnitDestroyed events
        // the bot's phase state machine depends on. See HighBarV2
        // specs/031-fix-callback-event-drop/contracts/callback-frame-interleaving.md
        // for the normative contract.
        FSBar.Client.Protocol.replayBufferEnabled <- true
        printfn "[tactical] Protocol.replayBufferEnabled = true (entering main loop)"

        // Start viewer AFTER warmup — uses state-based path (no socket reads).
        startViewer mapGrid allSpots client.GameState.TeamId
        // Push to viewer on every frame. onFrameWithState is lock-free
        // (atomic reference swap only) so it no longer blocks the bot thread.
        let wrappedTactics : TrainerTacticsFn =
            fun client frame cmdOpt ->
                match mapGrid with
                | Some grid -> viewerOnFrame client.GameState grid
                | None -> ()
                tacticsFn client frame cmdOpt

        let result = trainerLoopRun client logger maxFrames wrappedTactics
        // Bot-side outcome/cause override per SC-010 and T027:
        // - FR-012 stall: outcome→loss, cause→loss-by-stall-upgrade-deadline
        // - Macro clean win: when trainerLoopRun reports a win AND we
        //   reached Attack phase via upgrade, override cause to
        //   commander-death-win-after-upgrade (SC-004 / T031 language).
        //   DeadlineFallback path uses commander-death-win-deadline-fallback.
        let finalOutcome, finalCause, finalVictory =
            if upgradeGateState.StallRecorded then
                "loss", "loss-by-stall-upgrade-deadline", None
            elif result.Outcome = "win" && attackLaunched then
                let cause =
                    match upgradeGateState.Reached with
                    | Some _ -> "commander-death-win-after-upgrade"
                    | None -> "commander-death-win-deadline-fallback"
                "win", cause, result.VictorySignal
            else
                result.Outcome, result.Cause, result.VictorySignal
        writeResult
            logger
            finalOutcome
            result.Frames
            finalCause
            finalVictory
            result.ErrorMessage
            result.Telemetry
    with ex ->
        writeError logger ex
finally
    stopViewer ()
    match clientOpt with
    | Some c ->
        try c.Stop() with _ -> ()
    | None -> ()
    printfn "[trainer] bot_macro.fsx done"
