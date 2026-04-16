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
#load "helpers/viewer.fsx"
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
open Viewer

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

// pickEnemyCommanderPos was extracted into helpers/perception.fsx (021 T032):
// it was being called from two organic sites in this file — the periodic
// progress log and the per-refresh MoveCommand target selection — which is
// the substance bar SC-006 wants. See helpers/perception.fsx for the body.

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

// 021 US4 — Issue 1 AttackCommand getUnitPos probe. Gated by env var
// HIGHBAR_PROBE_ATTACK=1. Runs ONCE before the main trainer loop: captures
// the issuing commander's position, fires one AttackCommand at the nearest
// enemy (or first enemy if distances unavailable), waits 30 frames without
// issuing further commands, re-reads the commander's position, classifies
// the delta, and writes attack_probe.json into the run directory per
// contracts/result-record.delta.md Change 3. Intentionally non-invasive:
// if the probe fails or the env var is unset, the main trainer loop
// continues unchanged.
let probeEnabled =
    match Environment.GetEnvironmentVariable("HIGHBAR_PROBE_ATTACK") with
    | "1" -> true
    | _ -> false

let runAttackProbe (client: BarClient) : unit =
    try
        let cidOpt =
            if Map.isEmpty client.GameState.Units then None
            else Some (client.GameState.Units |> Map.toSeq |> Seq.head |> fst)
        // Prefer the unique-def enemy (usually the enemy commander) as
        // the probe target, so the result is not dominated by Spring
        // refusing to chase a distant critter. Fall back to the first
        // enemy only if no unique-def enemy exists.
        let enemyOpt =
            if Map.isEmpty client.GameState.Enemies then None
            else
                let defCounts =
                    client.GameState.Enemies
                    |> Map.toSeq
                    |> Seq.choose (fun (_, e) -> e.DefId)
                    |> Seq.countBy id
                    |> Map.ofSeq
                let unique =
                    client.GameState.Enemies
                    |> Map.toSeq
                    |> Seq.tryFind (fun (_, e) ->
                        match e.DefId with
                        | Some d -> Map.tryFind d defCounts = Some 1
                        | None -> false)
                match unique with
                | Some pair -> Some pair
                | None -> Some (client.GameState.Enemies |> Map.toSeq |> Seq.head)
        match cidOpt, enemyOpt with
        | Some cid, Some (eid, enemy) ->
            let issuingDefName =
                match client.GameState.Units |> Map.tryFind cid with
                | Some u ->
                    try Callbacks.getUnitDefName client.Stream u.DefId with _ -> "unknown"
                | None -> "unknown"
            let (bx, by, bz) = Callbacks.getUnitPos client.Stream cid
            let frameAtSend = int client.GameState.FrameNumber
            printfn "[probe] frame=%d commander=%d def=%s pos_before=(%.1f,%.1f,%.1f) target=%d"
                frameAtSend cid issuingDefName bx by bz eid
            client.SendCommands [ AttackCommand cid eid ]
            client.WaitFrames 30 (fun _ -> ())
            let frameAtCheck = int client.GameState.FrameNumber
            let destroyed = not (client.GameState.Units |> Map.containsKey cid)
            let (ax, ay, az, outcome) =
                if destroyed then
                    (0.0f, 0.0f, 0.0f, "destroyed")
                else
                    let (ax, ay, az) = Callbacks.getUnitPos client.Stream cid
                    let dx = float (ax - bx)
                    let dy = float (ay - by)
                    let dz = float (az - bz)
                    let dist = sqrt (dx * dx + dy * dy + dz * dz)
                    let outcome = if dist > 5.0 then "moved" else "stationary"
                    (ax, ay, az, outcome)
            printfn "[probe] frame=%d pos_after=(%.1f,%.1f,%.1f) outcome=%s"
                frameAtCheck ax ay az outcome
            use ms = new MemoryStream()
            use writer = new Utf8JsonWriter(ms, JsonWriterOptions(Indented = true))
            writer.WriteStartObject()
            writer.WriteNumber("issuing_unit_id", cid)
            writer.WriteString("issuing_unit_def", issuingDefName)
            writer.WriteNumber("target_unit_id", eid)
            writer.WriteNumber("frame_at_send", frameAtSend)
            writer.WriteStartArray("pos_before")
            writer.WriteNumberValue(bx)
            writer.WriteNumberValue(by)
            writer.WriteNumberValue(bz)
            writer.WriteEndArray()
            writer.WriteNumber("frame_at_check", frameAtCheck)
            writer.WriteStartArray("pos_after")
            writer.WriteNumberValue(ax)
            writer.WriteNumberValue(ay)
            writer.WriteNumberValue(az)
            writer.WriteEndArray()
            writer.WriteString("outcome", outcome)
            writer.WriteEndObject()
            writer.Flush()
            File.WriteAllBytes(Path.Combine(runDir, "attack_probe.json"), ms.ToArray())
            printfn "[probe] wrote attack_probe.json"
        | _ ->
            printfn "[probe] skipped — no commander or no enemy in GameState at probe time"
    with ex ->
        printfn "[probe] ERROR: %s" ex.Message

let mutable clientOpt : BarClient option = None
try
    try
        let client = new BarClient(config)
        clientOpt <- Some client
        printfn "[trainer] BarClient.Start()"
        client.Start()
        printfn "[trainer] BarClient connected"
        if probeEnabled then runAttackProbe client
        // Start viewer AFTER warmup/probe — uses state-based path (no socket reads).
        startViewer None [||] client.GameState.TeamId
        // Wrap tacticsFn to feed each frame to the viewer.
        // Simple bot has no pre-computed MapGrid — viewer uses flat fallback
        let viewerGrid =
            { WidthElmos = 8192; HeightElmos = 8192
              WidthHeightmap = 129; HeightHeightmap = 129
              HeightMap = Array2D.zeroCreate 129 129
              SlopeMap = Array2D.zeroCreate 129 129
              ResourceMap = Array2D.zeroCreate 129 129
              LosMap = Array2D.zeroCreate 129 129
              RadarMap = Array2D.zeroCreate 129 129 }
        let wrappedTactics : TrainerTacticsFn =
            fun client frame cmdOpt ->
                viewerOnFrame client.GameState viewerGrid
                tacticsFn client frame cmdOpt
        let result = trainerLoopRun client logger maxFrames wrappedTactics
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
    stopViewer ()
    match clientOpt with
    | Some c ->
        try c.Stop() with _ -> ()
    | None -> ()
    printfn "[trainer] bot.fsx done"
