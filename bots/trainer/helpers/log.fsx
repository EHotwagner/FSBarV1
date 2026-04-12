// bots/trainer/helpers/log.fsx — TrainerLog module: structured frame log + result writer
//
// Writes frames.jsonl and result.json into the run directory. frames.jsonl conforms to
// specs/020-bot-iterative-trainer/contracts/frame.schema.json. result.json conforms to
// contracts/result.schema.json. stdout is additionally decorated with one human-readable
// line per notable event so the operator can read stdout.log and see what happened
// without re-parsing the JSON.

// prelude must be #loaded before this file. bot.fsx does that in order.

open System
open System.IO
open System.Text.Json
open FSBar.Client

printfn "[trainer] log.fsx loaded"

/// A single event detail captured for a frame log line. One record per notable event.
type TrainerEventDetail = {
    Type: string
    UnitId: int option
    ActorId: int option
    DefId: int option
    Detail: string option
}

/// A logger scoped to a single run directory.
type TrainerLogger = {
    RunDir: string
    FramesPath: string
    ResultPath: string
    StartTime: DateTime
}

/// Create a logger for the given run directory. The directory must already exist.
let createLogger (runDir: string) : TrainerLogger =
    if not (Directory.Exists runDir) then
        Directory.CreateDirectory(runDir) |> ignore
    let framesPath = Path.Combine(runDir, "frames.jsonl")
    File.WriteAllText(framesPath, "")
    {
        RunDir = runDir
        FramesPath = framesPath
        ResultPath = Path.Combine(runDir, "result.json")
        StartTime = DateTime.UtcNow
    }

/// Emit a start banner into stdout (redirected to stdout.log by run.sh).
let logStart (logger: TrainerLogger) (config: EngineConfig) : unit =
    printfn "[trainer] run dir: %s" logger.RunDir
    printfn "[trainer] opponent: %s (options: %s)"
        config.OpponentAI
        (if Map.isEmpty config.OpponentAIOptions then "<none>" else
            config.OpponentAIOptions
            |> Seq.map (fun (KeyValue(k, v)) -> sprintf "%s=%s" k v)
            |> String.concat ",")
    printfn "[trainer] map: %s  death_mode: %s  game_speed: %dx"
        config.MapName config.DeathMode config.GameSpeed
    printfn "[trainer] socket: %s" config.SocketPath
    printfn "[trainer] start: %s" (logger.StartTime.ToString("O"))

let private writeOptional (w: Utf8JsonWriter) (name: string) (value: int option) =
    match value with
    | Some v -> w.WriteNumber(name, v)
    | None -> ()

/// Write one frame log line to frames.jsonl and echo notable events to stdout (SC-002).
let logFrame
    (logger: TrainerLogger)
    (reason: string)
    (frame: uint32)
    (events: (string * int) list)
    (eventDetails: TrainerEventDetail list)
    (unitCount: int)
    (enemyCount: int)
    (metal: float * float)
    (energy: float * float)
    (commandsOut: int)
    : unit =
    use ms = new MemoryStream()
    use writer =
        new Utf8JsonWriter(ms, JsonWriterOptions(Indented = false, SkipValidation = false))
    writer.WriteStartObject()
    writer.WriteNumber("frame", int64 frame)
    writer.WriteString("reason", (if reason = "" then "sampled" else reason))
    if not (List.isEmpty events) then
        writer.WriteStartObject("events")
        for (name, count) in events do
            writer.WriteNumber(name, count)
        writer.WriteEndObject()
    if not (List.isEmpty eventDetails) then
        writer.WriteStartArray("event_details")
        for ed in eventDetails do
            writer.WriteStartObject()
            writer.WriteString("type", ed.Type)
            writeOptional writer "unit_id" ed.UnitId
            writeOptional writer "actor_id" ed.ActorId
            writeOptional writer "def_id" ed.DefId
            match ed.Detail with
            | Some d -> writer.WriteString("detail", d)
            | None -> ()
            writer.WriteEndObject()
        writer.WriteEndArray()
    writer.WriteNumber("units", unitCount)
    writer.WriteNumber("enemies", enemyCount)
    writer.WriteStartObject("metal")
    writer.WriteNumber("current", fst metal)
    writer.WriteNumber("income", snd metal)
    writer.WriteEndObject()
    writer.WriteStartObject("energy")
    writer.WriteNumber("current", fst energy)
    writer.WriteNumber("income", snd energy)
    writer.WriteEndObject()
    if commandsOut > 0 then
        writer.WriteNumber("commands_out", commandsOut)
    writer.WriteEndObject()
    writer.Flush()
    let line = System.Text.Encoding.UTF8.GetString(ms.ToArray())
    File.AppendAllText(logger.FramesPath, line + "\n")
    for ed in eventDetails do
        let idPart =
            match ed.UnitId with
            | Some u -> sprintf " unit=%d" u
            | None -> ""
        let actorPart =
            match ed.ActorId with
            | Some a -> sprintf " actor=%d" a
            | None -> ""
        let detailPart =
            match ed.Detail with
            | Some d -> " " + d
            | None -> ""
        printfn "[frame %d] %s%s%s%s" frame ed.Type idPart actorPart detailPart

/// Telemetry record passed to writeResult. Keys match contracts/result.schema.json.
type TrainerTelemetry = {
    CommandsTotal: int
    UnitsBuilt: int
    UnitsLost: int
    EnemyUnitsKilled: int
    PeakMetal: float
    PeakEnergy: float
    FramesSurvived: int
}

/// Write result.json for a clean termination. victorySignal required when outcome="win".
let writeResult
    (logger: TrainerLogger)
    (outcome: string)
    (frames: int)
    (cause: string)
    (victorySignal: string option)
    (errorMessage: string option)
    (telemetry: TrainerTelemetry)
    : unit =
    use ms = new MemoryStream()
    use writer =
        new Utf8JsonWriter(ms, JsonWriterOptions(Indented = true, SkipValidation = false))
    writer.WriteStartObject()
    writer.WriteString("outcome", outcome)
    writer.WriteNumber("frames", frames)
    writer.WriteString("cause", cause)
    (match victorySignal with
     | Some v -> writer.WriteString("victory_signal", v)
     | None ->
         if outcome <> "win" then writer.WriteNull("victory_signal"))
    (match errorMessage with
     | Some e -> writer.WriteString("error_message", e)
     | None ->
         if outcome <> "error" then writer.WriteNull("error_message"))
    writer.WriteStartObject("telemetry")
    writer.WriteNumber("commands_total", telemetry.CommandsTotal)
    writer.WriteNumber("units_built", telemetry.UnitsBuilt)
    writer.WriteNumber("units_lost", telemetry.UnitsLost)
    writer.WriteNumber("enemy_units_killed", telemetry.EnemyUnitsKilled)
    writer.WriteNumber("peak_metal", telemetry.PeakMetal)
    writer.WriteNumber("peak_energy", telemetry.PeakEnergy)
    writer.WriteNumber("frames_survived", telemetry.FramesSurvived)
    writer.WriteEndObject()
    writer.WriteEndObject()
    writer.Flush()
    File.WriteAllBytes(logger.ResultPath, ms.ToArray())
    printfn "[trainer] result written: outcome=%s frames=%d cause=%s" outcome frames cause

/// Write a stub result.json for an unhandled exception.
let writeError (logger: TrainerLogger) (ex: exn) : unit =
    let stubTelemetry = {
        CommandsTotal = 0
        UnitsBuilt = 0
        UnitsLost = 0
        EnemyUnitsKilled = 0
        PeakMetal = 0.0
        PeakEnergy = 0.0
        FramesSurvived = 0
    }
    writeResult
        logger
        "error"
        0
        (sprintf "unhandled-exception: %s" (ex.GetType().Name))
        None
        (Some (sprintf "%s: %s" (ex.GetType().FullName) ex.Message))
        stubTelemetry
    printfn "[trainer] ERROR: %s" ex.Message
    printfn "[trainer] stack:\n%s" ex.StackTrace
