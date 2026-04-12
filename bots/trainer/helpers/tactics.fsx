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
    let mutable commandsTotal = 0
    let mutable unitsBuilt = 0
    let mutable unitsLost = 0
    let mutable enemyKilled = 0
    let mutable peakMetal = 0.0
    let mutable peakEnergy = 0.0
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
    let mutable botDeclaredVictory = false
    let mutable botVictoryFrame = 0

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
                if float m.Current > peakMetal then peakMetal <- float m.Current
                if float e.Current > peakEnergy then peakEnergy <- float e.Current

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
                if tacticsResult.VictoryDeclared && not botDeclaredVictory then
                    botDeclaredVictory <- true
                    botVictoryFrame <- int f
                    printfn "[trainer] bot declared VICTORY at frame %d" f

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
            // A "No active session" exception means BarClient went to Stopped state
            // (Protocol.receiveFrame returned None because the engine closed the socket
            // on its own game-over). Treat as engine shutdown, not a bot bug.
            if ex.Message.Contains "No active session" || client.State = Stopped then
                if not shutdownSeen then
                    shutdownSeen <- true
                    shutdownReason <- "engine-socket-closed"
                    printfn "[trainer] engine socket closed at frame %d — treating as shutdown"
                        lastFrameNumber
                stepping <- false
            else
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
        elif botDeclaredVictory && lastFrameNumber - botVictoryFrame >= 60 then
            // Bot detected an unambiguous win; give the engine a few frames to emit
            // any straggler events then stop. Classification below will produce a win.
            stepping <- false

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
        if botDeclaredVictory && commanderAlive then
            // Workaround: BAR's game_end.lua fires Spring.GameOver (engine log shows
            // `EndGame Graph disabled`) but the HighBar V2 proxy doesn't forward that to
            // our AI client as a Shutdown protocol event in scripted 1v1 sessions. The
            // bot tactics callback detected the win condition (enemy commander gone from
            // GameState.Enemies, ours alive) and declared victory. Report as a win with
            // the engine-shutdown-gameover signal per contracts/result.schema.json.
            {
                Outcome = "win"
                Frames = lastFrameNumber
                Cause =
                    sprintf "bot declared victory at frame %d (enemy commander killed, ours alive)"
                        botVictoryFrame
                VictorySignal = Some "engine-shutdown-gameover"
                ErrorMessage = None
                Telemetry = telemetry
            }
        elif shutdownSeen && commanderAlive then
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
