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
#load "helpers/log.fsx"
#load "helpers/perception.fsx"
#load "helpers/tactics.fsx"
#load "helpers/opening_build.fsx"
#load "helpers/production_queue.fsx"
#load "helpers/constructor_dispatch.fsx"
#load "helpers/upgrade_gate.fsx"
#load "helpers/attack_launch.fsx"

open System
open System.IO
open System.Text.Json
open FSBar.Client
open FSBar.Client.Commands
open Log
open Perception
open Tactics
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
let maxFrames = Int32.Parse(envOrFail "BOT_MAX_FRAMES")
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

/// Assemble a minimal OwnStructureFootprint list from GameState.Units,
/// treating every finished unit with no weapon range and no build options
/// (i.e., static structures) as a blocker for path planning. Lightweight
/// heuristic — real deployments would consult UnitDefCache for the exact
/// footprint shape.
let private ownStructuresFromGameState (gs: GameState) : OwnStructureFootprint seq =
    gs.Units
    |> Map.toSeq
    |> Seq.choose (fun (_, u) ->
        if not u.IsFinished then None
        else
            match UnitDefCache.tryFindById gs.UnitDefs u.DefId with
            | Some info when info.MaxWeaponRange = 0.0f && info.BuildOptions.Length = 0 ->
                Some
                    { Centre = u.Position
                      RadiusElmos = 24.0f
                      Tag = Some info.Name }
            | _ -> None)

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

            if currentPhase = Opening && openingComplete resolvedOpening finishedDefIds then
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
                            let myIds =
                                if Map.isEmpty client.GameState.Units then [ cid ]
                                else
                                    client.GameState.Units
                                    |> Map.toSeq
                                    |> Seq.filter (fun (_, u) -> u.IsFinished)
                                    |> Seq.map fst
                                    |> Seq.toList
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
                elif currentPhase = Opening && not openingProgress.AwaitingCreated then
                    let commanderPos =
                        match Map.tryFind cid client.GameState.Units with
                        | Some u -> u.Position
                        | None -> (0.0f, 100.0f, 0.0f)
                    let centre = baseCentre |> Option.defaultValue commanderPos
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
                    // T030 extraction: attack_launch.launchFreshCombat
                    // picks any combat unit not yet in the launched set
                    // and issues a MoveCommand toward the target.
                    let target = pickAttackTarget client.GameState
                    let (launchCmds, newLaunched) =
                        launchFreshCombat
                            client.GameState
                            client.GameState.UnitDefs
                            target
                            combatUnitsLaunched
                    if not (List.isEmpty launchCmds) && not attackLaunched then
                        attackLaunched <- true
                        let (tx, _, tz) = target
                        printfn "[attack] launching %d combat units at target (%.0f,%.0f)"
                            (List.length launchCmds) tx tz
                        // 024 US5: emit a Pathing.findPath trace for the first
                        // unit in the launch so post-run analysis can see the
                        // route cost + waypoint count. Uses commander pos as
                        // the start since combat units cluster around the base.
                        match mapGrid with
                        | Some grid ->
                            let startPos =
                                match Map.tryFind cid client.GameState.Units with
                                | Some u -> u.Position
                                | None -> (tx, 0.0f, tz)
                            let ownStructures = ownStructuresFromGameState client.GameState
                            let budget : PathBudget =
                                { WallClockMs = 100
                                  MaxExpansions = 100_000
                                  SlopeCost = 2.0f }
                            match Pathing.findPath grid MoveType.Kbot ownStructures startPos target budget with
                            | Result.Ok path ->
                                printfn "[attack] path waypoints=%d cost=%.0f status=%A"
                                    path.Waypoints.Length path.EstimatedCost path.Status
                            | Result.Error e ->
                                printfn "[attack] findPath error: %A" e
                        | None ->
                            printfn "[attack] findPath skipped (no MapGrid)"
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
        try
            let safeName =
                // Mirror scripts/examples/14-cache-map-analysis.fsx's sanitise:
                // lowercase + non-alphanum-or-dot → '_'. Keeping the two in
                // sync is important — a mismatch means the runtime warmup
                // silently falls through to the "no cache" branch.
                mapName.ToLowerInvariant()
                |> Seq.map (fun c ->
                    if Char.IsLetterOrDigit(c) || c = '.' then c else '_')
                |> Seq.toArray
                |> System.String
            let cachePath =
                Path.Combine(Path.GetDirectoryName(botScript), "map-cache", safeName + ".json")
            let fullCachePath =
                let relativeTo = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
                if File.Exists cachePath then cachePath
                else Path.Combine(Directory.GetCurrentDirectory(), "bots", "trainer", "map-cache", safeName + ".json")
            if File.Exists fullCachePath then
                let json = File.ReadAllText(fullCachePath)
                use doc = JsonDocument.Parse(json)
                let root = doc.RootElement
                let cps =
                    root.GetProperty("chokepoints").EnumerateArray()
                    |> Seq.map (fun el ->
                        let pos =
                            (el.GetProperty("position.x").GetSingle(),
                             el.GetProperty("position.y").GetSingle(),
                             el.GetProperty("position.z").GetSingle())
                        let outX = el.GetProperty("outwardDir.x").GetSingle()
                        let outZ = el.GetProperty("outwardDir.z").GetSingle()
                        { Id = ChokepointId (el.GetProperty("id").GetUInt32())
                          Position = pos
                          WidthElmos = el.GetProperty("widthElmos").GetSingle()
                          OutwardDir = (outX, outZ)
                          DistanceFromBase = el.GetProperty("distanceFromBase").GetSingle() })
                    |> Seq.toList
                pinnedChokepoints <- cps
                printfn "[chokepoint] loaded %d chokepoints from cache %s" cps.Length fullCachePath
                for cp in cps |> List.truncate 5 do
                    let (px, _, pz) = cp.Position
                    printfn "[chokepoint] pos=(%.0f,%.0f) width=%.0f id=%A distFromBase=%.0f"
                        px pz cp.WidthElmos cp.Id cp.DistanceFromBase
            else
                printfn "[chokepoint] no cache at %s — run scripts/examples/14-cache-map-analysis.fsx '%s'"
                    fullCachePath mapName
            // BasePlan.resolvePlan is pure CPU, < 1 ms, and needs the live
            // commanderPos + sortedMetalSpots that are only known at warmup,
            // so it runs here rather than offline. Emitted as [plan]
            // telemetry only — command emission stays on the 023
            // opening_build helper until a follow-up iteration validates the
            // full switch in a live NullAI run. The resolver uses a synthetic
            // MapGrid skeleton (not the live engine grid) because we only
            // need the terrain-buildable / off-map checks, which work against
            // any MapGrid whose dimensions cover the real map.
            let planMapGrid : MapGrid =
                // Synthetic skeleton: dimensions pulled from the engine's
                // getMapWidth / getMapHeight (cheap callbacks), heightmap filled
                // with commanderPos.y so the Land check always succeeds. The
                // handful of callbacks here is single-request-response and
                // does not hold the frame-reading path long enough to race
                // the engine.
                let w = Callbacks.getMapWidth client.Stream
                let h = Callbacks.getMapHeight client.Stream
                { WidthElmos = w * 8
                  HeightElmos = h * 8
                  WidthHeightmap = w
                  HeightHeightmap = h
                  HeightMap = Array2D.create (w + 1) (h + 1) cy
                  SlopeMap = Array2D.zeroCreate w h
                  ResourceMap = Array2D.zeroCreate w h
                  LosMap = Array2D.zeroCreate w h
                  RadarMap = Array2D.zeroCreate w h }
            let resolveContext : ResolveContext =
                { Grid = planMapGrid
                  BaseCentre = commanderPos
                  CommanderPos = commanderPos
                  MetalSpotsNearest = sortedMetalSpots
                  Chokepoints = pinnedChokepoints
                  UnitDefs = client.GameState.UnitDefs
                  ExistingStructures = []
                  Progress = BasePlan.emptyPlanProgress }
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
            printfn "[tactical] warmup failed (non-fatal, bot proceeds with 023 flow): %s" ex.Message

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

        let result = trainerLoopRun client logger maxFrames tacticsFn
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
    match clientOpt with
    | Some c ->
        try c.Stop() with _ -> ()
    | None -> ()
    printfn "[trainer] bot_macro.fsx done"
