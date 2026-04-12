// bots/trainer/bot.fsx — the active trainer bot under iteration
//
// Reads environment variables set by run.sh, constructs an EngineConfig, launches a
// BarClient, runs the match via trainerLoopRun, and writes result.json.
//
// Required env vars:
//   HIGHBAR_BOT_RUN_DIR   absolute path to the pre-created run directory
//   BOT_OPPONENT          opponent AI short name (e.g. NullAI, BARb)
//   BOT_OPPONENT_OPTIONS  JSON object of opponent options (e.g. {"profile":"easy"})
//   BOT_MAP               map name (e.g. "Avalanche 3.4")
//   BOT_SEED              RNG seed (integer) — currently informational; scriptgen hardcodes 1
//   BOT_MAX_FRAMES        frame limit for this rung (integer)

#load "helpers/prelude.fsx"
#load "helpers/log.fsx"
#load "helpers/perception.fsx"
#load "helpers/tactics.fsx"

open System
open System.IO
open System.Text.Json
open FSBar.Client
open FSBar.Client.Commands
open Log
open Perception
open Tactics

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

let opponentOptions = parseOpponentOptions opponentOptionsJson

printfn "[trainer] bot.fsx starting"
printfn "[trainer] run_dir=%s opponent=%s map=%s max_frames=%d"
    runDir opponent mapName maxFrames

let logger = createLogger runDir

// Try "builders": game_team_com_ends.lua treats every builder/resurrector as
// a commander, so killing corcom (the only enemy builder) definitely triggers
// the allyteam-wide death path. Plain "com" / "own_com" didn't end the match
// against NullAI even though corcom died at frame 4195 in iters 013–016.
let config =
    let baseConfig = EngineConfig.defaultConfig ()
    { baseConfig with
        MapName = mapName
        OpponentAI = opponent
        OpponentAIOptions = opponentOptions
        DeathMode = "builders"
        GameSpeed = gameSpeed }

logStart logger config

// ---------------------------------------------------------------------------
// Bot tactics — iter 013: target the def=296 enemy explicitly, not just (3200,3200)
//
// Finding from iter 012: commander walks to (3200, 3200) fine, but no enemies
// are within weapon range there. The 8 NullAI enemies include 7 with def=507
// (identical buildings, scattered) and 1 with def=296 (unique — the enemy
// commander) at roughly (3699, 3601). Walk to *that* enemy's position,
// refreshing the waypoint each time so the path adapts if the enemy moves.
// The commander's default weapons will engage anything in range along the way.
// ---------------------------------------------------------------------------

let moveRefreshInterval = 500

// Identify the enemy commander: pick the enemy whose DefId is unique across all enemies.
// (NullAI's 8 spawns include 7 identical buildings and 1 unique commander.)
let pickEnemyCommanderPos (gs: GameState) : (float32 * float32 * float32) option =
    if Map.isEmpty gs.Enemies then None
    else
        let defCounts =
            gs.Enemies
            |> Map.toSeq
            |> Seq.choose (fun (_, e) -> e.DefId)
            |> Seq.countBy id
            |> Map.ofSeq
        let uniqueEnemy =
            gs.Enemies
            |> Map.toSeq
            |> Seq.tryFind (fun (_, e) ->
                match e.DefId with
                | Some d -> Map.tryFind d defCounts = Some 1
                | None -> false)
        uniqueEnemy |> Option.map (fun (_, e) -> e.Position)

let mutable lastMoveFrame = -10000
let mutable strategyAnnounced = false
let mutable defNamesDumped = false
let mutable enemyCommanderDefId : int option = None
let mutable victoryAnnounced = false

let tacticsFn : TrainerTacticsFn =
    fun client frame commanderIdOpt ->
        match commanderIdOpt with
        | None -> { Commands = []; VictoryDeclared = false }
        | Some cid ->
            let fnum = int frame.FrameNumber

            if not strategyAnnounced then
                strategyAnnounced <- true
                printfn "[bot] strategy: MoveCommand → unique-def enemy; refresh every %d frames"
                    moveRefreshInterval

            // One-shot def name dump so we can identify the commander by name. Also
            // record the commander's DefId so we can detect its destruction precisely.
            if not defNamesDumped && client.GameState.Enemies.Count > 0 then
                defNamesDumped <- true
                printfn "[bot-defs] enemies with def names:"
                for (KeyValue(eid, e)) in client.GameState.Enemies do
                    match e.DefId with
                    | Some d ->
                        try
                            let name = Callbacks.getUnitDefName client.Stream d
                            let (px, py, pz) = e.Position
                            printfn "[bot-defs]   enemy %d def=%d name=%s pos=(%.0f,%.0f,%.0f)" eid d name px py pz
                            // Cortex commander def is "corcom"; Armada commander is "armcom".
                            // Any name containing "com" that isn't "corcom_..." sub-unit.
                            if name = "corcom" || name = "armcom" then
                                enemyCommanderDefId <- Some d
                                printfn "[bot] identified enemy commander: def=%d name=%s" d name
                        with ex ->
                            printfn "[bot-defs]   enemy %d def=%d <name lookup failed: %s>" eid d ex.Message
                    | None ->
                        printfn "[bot-defs]   enemy %d def=<none>" eid

            // Victory detection: if the enemy commander is no longer present in GameState.Enemies,
            // it has been destroyed (GameState.processEvent removes destroyed enemies).
            let enemyComStillAlive =
                match enemyCommanderDefId with
                | None -> true  // Haven't identified it yet
                | Some cdef ->
                    client.GameState.Enemies
                    |> Map.exists (fun _ e -> e.DefId = Some cdef)
            let victoryNow =
                not enemyComStillAlive && enemyCommanderDefId.IsSome && not victoryAnnounced
            if victoryNow then
                victoryAnnounced <- true
                printfn "[bot] 🏆 enemy commander is no longer in GameState.Enemies — victory!"

            // Periodic progress log every 1500 frames
            if fnum > 0 && fnum % 1500 = 0 then
                try
                    let (cx, cy, cz) = Callbacks.getUnitPos client.Stream cid
                    let hp = Callbacks.getUnitHealth client.Stream cid
                    let (tx, _, tz) =
                        pickEnemyCommanderPos client.GameState
                        |> Option.defaultValue (3200.0f, 100.0f, 3200.0f)
                    let dx = tx - cx
                    let dz = tz - cz
                    let dist = sqrt (dx * dx + dz * dz)
                    printfn "[bot] f=%d commander=(%.0f,%.0f,%.0f) hp=%.0f → target=(%.0f,%.0f) dist=%.0f enemies=%d"
                        fnum cx cy cz hp tx tz dist client.GameState.Enemies.Count
                with _ -> ()

            let cmds =
                if fnum - lastMoveFrame >= moveRefreshInterval then
                    lastMoveFrame <- fnum
                    let target =
                        pickEnemyCommanderPos client.GameState
                        |> Option.defaultValue (3200.0f, 100.0f, 3200.0f)
                    let (tx, _, tz) = target
                    [ MoveCommand cid tx 100.0f tz ]
                else
                    []
            { Commands = cmds; VictoryDeclared = victoryNow }

let mutable clientOpt : BarClient option = None
try
    try
        let client = new BarClient(config)
        clientOpt <- Some client
        printfn "[trainer] BarClient.Start()"
        client.Start()
        printfn "[trainer] BarClient connected"
        let result = trainerLoopRun client logger maxFrames tacticsFn
        writeResult
            logger
            result.Outcome
            result.Frames
            result.Cause
            result.VictorySignal
            result.ErrorMessage
            result.Telemetry
    with ex ->
        writeError logger ex
finally
    match clientOpt with
    | Some c ->
        try c.Stop() with _ -> ()
    | None -> ()
    printfn "[trainer] bot.fsx done"
