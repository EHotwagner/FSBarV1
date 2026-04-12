// bots/trainer/helpers/tactics.fsx — the main trainer match loop
//
// `trainerLoopRun` is inlined on purpose for US1. Extraction into perception/tactics
// sub-helpers happens in later US4 iterations under FR-020.

// perception.fsx must be #loaded before this file. bot.fsx does that in order.

open System
open FSBar.Client
open Log
open Perception

printfn "[trainer] tactics.fsx loaded"

/// Aggregated outcome of a single match. Consumed by bot.fsx to write result.json.
type TrainerMatchResult = {
    Outcome: string
    Frames: int
    Cause: string
    VictorySignal: string option
    ErrorMessage: string option
    Telemetry: TrainerTelemetry
}

/// Classify a GameEvent into a TrainerEventDetail, or None if not notable enough to log.
let private classifyEvent (myCommanderId: int option) (ev: GameEvent) : TrainerEventDetail option =
    let ed t uid aid detail =
        Some { Type = t; UnitId = uid; ActorId = aid; DefId = None; Detail = detail }
    match ev with
    | GameEvent.UnitCreated(uid, bid) ->
        let builder = if bid = 0 then None else Some bid
        ed "UnitCreated" (Some uid) builder (Some (sprintf "our unit %d created (builder %d)" uid bid))
    | GameEvent.UnitFinished uid ->
        ed "UnitFinished" (Some uid) None (Some (sprintf "our unit %d finished" uid))
    | GameEvent.UnitDestroyed(uid, attacker) ->
        let isCommander = (Some uid) = myCommanderId
        let desc =
            if isCommander then
                sprintf "OUR COMMANDER %d DESTROYED (attacker %A)" uid attacker
            else
                sprintf "our unit %d destroyed (attacker %A)" uid attacker
        ed "UnitDestroyed" (Some uid) attacker (Some desc)
    | GameEvent.EnemyEnterLOS eid ->
        ed "EnemyEnterLOS" (Some eid) None (Some (sprintf "enemy %d visible" eid))
    | GameEvent.EnemyDestroyed(eid, attacker) ->
        ed "EnemyDestroyed" (Some eid) attacker (Some (sprintf "ENEMY %d KILLED (by %A)" eid attacker))
    | GameEvent.Shutdown reason ->
        ed "Shutdown" None None (Some (sprintf "engine shutdown: %s" reason))
    | _ -> None

/// A per-frame tactics callback. Runs inside the frame handler after telemetry has been
/// updated. Returns a list of AICommands to queue via client.SendCommands, plus an optional
/// "victory declared" signal: when the bot detects an unambiguous win condition (e.g. the
/// enemy commander has died) but the engine has NOT emitted its own Shutdown event, the
/// callback can return victoryDeclared=true to instruct trainerLoopRun to terminate the
/// loop and classify the match as a win. This is a workaround for BAR's game_end gadget
/// not propagating game-over to AI clients in scripted 1v1 sessions against NullAI — the
/// match IS over (Spring logs `EndGame Graph disabled`) but the proxy never sends Shutdown.
type TrainerTacticsResult = {
    Commands: Highbar.AICommand list
    VictoryDeclared: bool
}

type TrainerTacticsFn = BarClient -> FSBar.Client.GameFrame -> int option -> TrainerTacticsResult

/// Default tactics: pure observation, no commands issued, no victory declared.
let tacticsNoOp : TrainerTacticsFn = fun _ _ _ -> { Commands = []; VictoryDeclared = false }

/// Run one match. Terminates on Shutdown event, commander death, frame limit, or 3 consecutive
/// same-frame exceptions. Calls `tactics` once per received frame to collect commands to
/// issue on the next step.
let trainerLoopRun
    (client: BarClient)
    (logger: TrainerLogger)
    (maxFrames: int)
    (tactics: TrainerTacticsFn)
    : TrainerMatchResult =
    // Per 021 FR-003: peakMetal/peakEnergy use float option so a match where
    // every frame reads Single.NaN (proxy returned NaN for an invalid resource
    // id) serializes to null rather than 0.0 — preserves the distinction
    // between "real zero accumulation" and "callback unavailable" for the
    // stall check.
    let nanSafeUpdate (acc: float option) (v: float32) =
        if Single.IsNaN v then acc
        else
            match acc with
            | None -> Some (float v)
            | Some prev -> Some (max prev (float v))

    let mutable commandsTotal = 0
    let mutable unitsBuilt = 0
    let mutable unitsLost = 0
    let mutable enemyKilled = 0
    let mutable peakMetal : float option = None
    let mutable peakEnergy : float option = None
    let mutable framesSurvived = 0
    let mutable myCommanderId : int option = None
    let mutable commanderAlive = true
    let mutable shutdownSeen = false
    let mutable shutdownReason = ""
    let mutable lastFrameNumber = 0
    let mutable consecutiveExceptions = 0
    let mutable lastExceptionFrame = 0u
    let mutable lastExceptionType = ""
    let mutable terminalError : (string * string) option = None
    let mutable stepping = true

    // BarClient.Start already does a 60-frame internal warmup that captures the commander
    // into client.GameState.Units before UnitCreated events reach us. Check the unit map
    // first; fall back to watching UnitCreated events during our own warmup for edge cases.
    if not (Map.isEmpty client.GameState.Units) then
        let (firstId, _) = client.GameState.Units |> Map.toSeq |> Seq.head
        myCommanderId <- Some firstId
        printfn "[trainer] captured commander id = %d from GameState.Units (size=%d) at frame %d"
            firstId client.GameState.Units.Count client.GameState.FrameNumber

    printfn "[trainer] warmup: reading up to 60 frames to confirm / capture late spawns"
    try
        client.WaitFrames 60 (fun frame ->
            for ev in frame.Events do
                match ev with
                | GameEvent.UnitCreated(uid, _) when myCommanderId.IsNone ->
                    myCommanderId <- Some uid
                    printfn "[trainer] captured commander id = %d (via UnitCreated at frame %d)" uid frame.FrameNumber
                | _ -> ())
    with ex ->
        printfn "[trainer] warmup exception: %s" ex.Message

    printfn "[trainer] entering main frame loop (max_frames=%d)" maxFrames

    while stepping do
        try
            client.WaitFrames 1 (fun frame ->
                let f = frame.FrameNumber
                lastFrameNumber <- int f
                framesSurvived <- if commanderAlive then int f else framesSurvived

                let eventNameCounts =
                    frame.Events
                    |> List.countBy (fun ev ->
                        match ev with
                        | GameEvent.UnitCreated _ -> "UnitCreated"
                        | GameEvent.UnitFinished _ -> "UnitFinished"
                        | GameEvent.UnitDestroyed _ -> "UnitDestroyed"
                        | GameEvent.EnemyCreated _ -> "EnemyCreated"
                        | GameEvent.EnemyFinished _ -> "EnemyFinished"
                        | GameEvent.EnemyEnterLOS _ -> "EnemyEnterLOS"
                        | GameEvent.EnemyDestroyed _ -> "EnemyDestroyed"
                        | GameEvent.UnitDamaged _ -> "UnitDamaged"
                        | GameEvent.EnemyDamaged _ -> "EnemyDamaged"
                        | GameEvent.Update _ -> "Update"
                        | GameEvent.Shutdown _ -> "Shutdown"
                        | _ -> "Other")
                    |> List.filter (fun (name, _) -> name <> "Update" && name <> "Other")

                let detailedEvents =
                    frame.Events
                    |> List.choose (classifyEvent myCommanderId)

                for ev in frame.Events do
                    match ev with
                    | GameEvent.UnitFinished _ -> unitsBuilt <- unitsBuilt + 1
                    | GameEvent.UnitDestroyed(uid, _) ->
                        unitsLost <- unitsLost + 1
                        if Some uid = myCommanderId then
                            commanderAlive <- false
                            printfn "[trainer] commander destroyed at frame %d" f
                    | GameEvent.EnemyDestroyed _ -> enemyKilled <- enemyKilled + 1
                    | GameEvent.Shutdown reason ->
                        shutdownSeen <- true
                        shutdownReason <- reason
                        printfn "[trainer] Shutdown received at frame %d reason=%s" f reason
                    | _ -> ()

                let m = client.GameState.Metal
                let e = client.GameState.Energy
                peakMetal <- nanSafeUpdate peakMetal m.Current
                peakEnergy <- nanSafeUpdate peakEnergy e.Current

                // Let bot tactics decide commands for next frame
                let tacticsResult =
                    try tactics client frame myCommanderId
                    with ex ->
                        printfn "[trainer] tactics callback exception at frame %d: %s" f ex.Message
                        { Commands = []; VictoryDeclared = false }
                let cmds = tacticsResult.Commands
                if not (List.isEmpty cmds) then
                    try
                        client.SendCommands cmds
                        commandsTotal <- commandsTotal + List.length cmds
                    with ex ->
                        printfn "[trainer] SendCommands exception at frame %d: %s" f ex.Message
                // tacticsResult.VictoryDeclared is no longer consumed: per 021
                // US2 the canonical victory path is Shutdown(GAME_OVER) from
                // the proxy (surfaced as GameEvent.Shutdown via the
                // Protocol.fs synthesis patch). The field survives in the
                // TrainerTacticsResult record only because bot.fsx still
                // produces it — kept as a no-op to avoid churning bot.fsx
                // within the same feature.

                let hasEvents = not (List.isEmpty detailedEvents)
                let sampledFrame = int f % 30 = 0
                if hasEvents || sampledFrame then
                    let reason = if hasEvents then "event" else "sampled"
                    logFrame
                        logger
                        reason
                        f
                        eventNameCounts
                        detailedEvents
                        client.GameState.Units.Count
                        client.GameState.Enemies.Count
                        (float m.Current, float m.Income)
                        (float e.Current, float e.Income)
                        (List.length cmds))
            consecutiveExceptions <- 0
        with ex ->
            // Canonical end-of-game now flows through GameEvent.Shutdown in
            // the frame event stream (see Protocol.fs receiveFrame synthesis
            // patch). Any exception from WaitFrames at this point is a real
            // error — increment the repeat-counter and classify as
            // terminal-error after 3 consecutive same-type repeats. The
            // The pre-021 exception sniffer that treated a socket close as
            // the canonical shutdown has been removed per FR-007.
            let fnum = uint32 lastFrameNumber
            let etype = ex.GetType().Name
            if fnum = lastExceptionFrame && etype = lastExceptionType then
                consecutiveExceptions <- consecutiveExceptions + 1
            else
                consecutiveExceptions <- 1
                lastExceptionFrame <- fnum
                lastExceptionType <- etype
            printfn "[trainer] frame loop exception (count=%d) at frame %d: %s: %s"
                consecutiveExceptions lastFrameNumber etype ex.Message
            if consecutiveExceptions >= 3 then
                terminalError <-
                    Some ("error", sprintf "repeated-frame-exception: %s" etype)
                stepping <- false

        if shutdownSeen then stepping <- false
        elif not commanderAlive then stepping <- false
        elif lastFrameNumber >= maxFrames then stepping <- false

    let telemetry = {
        CommandsTotal = commandsTotal
        UnitsBuilt = unitsBuilt
        UnitsLost = unitsLost
        EnemyUnitsKilled = enemyKilled
        PeakMetal = peakMetal
        PeakEnergy = peakEnergy
        FramesSurvived = framesSurvived
    }

    match terminalError with
    | Some (outcome, cause) ->
        {
            Outcome = outcome
            Frames = lastFrameNumber
            Cause = cause
            VictorySignal = None
            ErrorMessage = Some cause
            Telemetry = telemetry
        }
    | None ->
        if shutdownSeen && commanderAlive then
            {
                Outcome = "win"
                Frames = lastFrameNumber
                Cause = sprintf "engine shutdown (reason=%s), commander alive" shutdownReason
                VictorySignal = Some "engine-shutdown-gameover"
                ErrorMessage = None
                Telemetry = telemetry
            }
        elif shutdownSeen then
            {
                Outcome = "loss"
                Frames = lastFrameNumber
                Cause = sprintf "engine shutdown (reason=%s), commander dead" shutdownReason
                VictorySignal = None
                ErrorMessage = None
                Telemetry = telemetry
            }
        elif not commanderAlive then
            {
                Outcome = "loss"
                Frames = lastFrameNumber
                Cause = "commander destroyed before engine shutdown"
                VictorySignal = None
                ErrorMessage = None
                Telemetry = telemetry
            }
        else
            {
                Outcome = "timeout"
                Frames = lastFrameNumber
                Cause = sprintf "frame limit reached (max_frames=%d)" maxFrames
                VictorySignal = None
                ErrorMessage = None
                Telemetry = telemetry
            }
