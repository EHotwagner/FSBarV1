namespace FSBar.Hub

// `FSBar.Client.SessionState` exposes an `Error of string` DU case at
// the namespace root. We import specific types rather than opening the
// namespace to keep bare `Error` resolving to `Result.Error`.
open System
open System.IO
open FSBar.Client

module LobbyConfig =

    type GameMode =
        | Skirmish
        | FFA
        | Team

    type SeatKind =
        | AiSeat of aiName: string * options: Map<string, string>
        | HumanSeat of playerName: string

    type Seat = {
        Kind: SeatKind
        Side: string
        Handicap: int
    }

    type Team = {
        Seats: Seat list
        AllyTeamId: int
    }

    type Spectator = {
        PlayerName: string
    }

    type LobbyConfig = {
        MapName: string
        Mode: GameMode
        EngineSpeed: float32
        LaunchGraphicalViewer: bool
        Teams: Team list
        Spectators: Spectator list
    }

    type LobbyError =
        | NotEnoughTeams
        | TeamHasNoActiveSeats of teamIndex: int
        | HandicapOutOfRange of teamIndex: int * seatIndex: int * value: int
        | MapNotInstalled of mapName: string
        | UnknownAi of teamIndex: int * seatIndex: int * aiName: string
        | FfaTeamHasMultipleSeats of teamIndex: int
        | FfaTooFewTeams
        | EngineSpeedOutOfRange of value: float32
        | GraphicalBinaryMissing of engineVersion: string
        | AdapterUnsupportedShape of reason: string

    let defaults: LobbyConfig =
        let armadaAi =
            { Kind = AiSeat("HighBarV2", Map.empty)
              Side = "Armada"
              Handicap = 0 }
        let cortexAi =
            { Kind = AiSeat("BARb", Map.empty)
              Side = "Cortex"
              Handicap = 0 }
        { MapName = ""
          Mode = Skirmish
          EngineSpeed = 1.0f
          LaunchGraphicalViewer = false
          Teams =
            [ { Seats = [ armadaAi ]; AllyTeamId = 0 }
              { Seats = [ cortexAi ]; AllyTeamId = 1 } ]
          Spectators = [] }

    let formatError (err: LobbyError) =
        match err with
        | NotEnoughTeams -> "at least two teams are required"
        | TeamHasNoActiveSeats i -> sprintf "team %d has no playing seats" i
        | HandicapOutOfRange(t, s, v) ->
            sprintf "team %d seat %d handicap %d is outside [-100, 100]" t s v
        | MapNotInstalled m -> sprintf "map %s is not installed under <dataDir>/maps/" m
        | UnknownAi(t, s, ai) ->
            sprintf "team %d seat %d AI %s is not installed under the active engine's AI/Skirmish/" t s ai
        | FfaTeamHasMultipleSeats t ->
            sprintf "FFA mode requires one seat per team; team %d has multiple" t
        | FfaTooFewTeams -> "FFA mode requires at least three teams"
        | EngineSpeedOutOfRange v -> sprintf "engine speed %f is outside [0.1, 100.0]" v
        | GraphicalBinaryMissing ver ->
            sprintf "graphical `spring` binary is not installed under engine %s" ver
        | AdapterUnsupportedShape reason ->
            sprintf "current script generator cannot emit this lobby: %s" reason

    let private isAiSeat (seat: Seat) =
        match seat.Kind with AiSeat _ -> true | _ -> false

    let private aiName (seat: Seat) =
        match seat.Kind with
        | AiSeat(name, _) -> Some name
        | HumanSeat _ -> None

    let private mapInstalled (install: BarInstall.BarInstall) (mapName: string) =
        if String.IsNullOrWhiteSpace(mapName) then false
        else
            let mapsDir = Path.Combine(install.DataDir, "maps")
            let candidate = Path.Combine(mapsDir, mapName + ".sd7")
            File.Exists(candidate)

    let private installedAiSet (install: BarInstall.BarInstall) : Set<string> =
        BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList

    let validate
            (install: BarInstall.BarInstall)
            (config: LobbyConfig)
            : Result<LobbyConfig, LobbyError list> =
        let errs = ResizeArray<LobbyError>()

        if config.EngineSpeed < 0.1f || config.EngineSpeed > 100.0f then
            errs.Add(EngineSpeedOutOfRange config.EngineSpeed)

        if not (mapInstalled install config.MapName) then
            errs.Add(MapNotInstalled config.MapName)

        if config.LaunchGraphicalViewer && not install.ActiveEngine.HasGraphicalBin then
            errs.Add(GraphicalBinaryMissing install.ActiveEngine.Version)

        if config.Teams.Length < 2 then
            errs.Add(NotEnoughTeams)

        let aiSet = installedAiSet install

        config.Teams
        |> List.iteri (fun ti team ->
            let activeSeats = team.Seats |> List.filter (fun s -> isAiSeat s || (match s.Kind with HumanSeat _ -> true | _ -> false))
            if activeSeats.IsEmpty then
                errs.Add(TeamHasNoActiveSeats ti)
            team.Seats
            |> List.iteri (fun si seat ->
                if seat.Handicap < -100 || seat.Handicap > 100 then
                    errs.Add(HandicapOutOfRange(ti, si, seat.Handicap))
                match aiName seat with
                | Some name when not (aiSet.Contains name) ->
                    errs.Add(UnknownAi(ti, si, name))
                | _ -> ()))

        match config.Mode with
        | FFA ->
            if config.Teams.Length < 3 then
                errs.Add(FfaTooFewTeams)
            config.Teams
            |> List.iteri (fun ti team ->
                if team.Seats.Length > 1 then
                    errs.Add(FfaTeamHasMultipleSeats ti))
        | Skirmish
        | Team -> ()

        if errs.Count = 0 then Ok config
        else Result.Error (errs |> List.ofSeq)

    /// Computes a unique socket path for a fresh session.
    let private generateSocketPath () =
        let id = Guid.NewGuid().ToString("N").Substring(0, 8)
        sprintf "/tmp/highbar-v2-%s.sock" id

    let private singleAi (team: Team) =
        match team.Seats with
        | [ seat ] ->
            match seat.Kind with
            | AiSeat(name, opts) -> Some (name, opts, seat.Side)
            | HumanSeat _ -> None
        | _ -> None

    let toEngineConfig
            (install: BarInstall.BarInstall)
            (config: LobbyConfig)
            : Result<EngineConfig, LobbyError> =
        // Phase-3 adapter constraint — the existing ScriptGenerator
        // emits a hardcoded 2-team AI-vs-AI skirmish. Refuse anything
        // more elaborate; US4 / US-Team / US-FFA widen the generator.
        if config.Mode <> Skirmish then
            Result.Error (AdapterUnsupportedShape "only Skirmish mode is currently mapped to the engine script")
        elif config.Teams.Length <> 2 then
            Result.Error (AdapterUnsupportedShape "only 2-team lobbies are currently mapped to the engine script")
        elif not config.Spectators.IsEmpty then
            Result.Error (AdapterUnsupportedShape "spectators are not yet mapped to the engine script")
        else
            let team0 = config.Teams.[0]
            let team1 = config.Teams.[1]
            match singleAi team0, singleAi team1 with
            | None, _ ->
                Result.Error (AdapterUnsupportedShape "team 0 must have exactly one AI seat")
            | _, None ->
                Result.Error (AdapterUnsupportedShape "team 1 must have exactly one AI seat")
            | Some (ourAi, _ourOpts, ourSide), Some (oppAi, oppOpts, oppSide) ->
                // The current script pins our AI to "HighBarV2" — if the
                // user's first team picks a different AI we reject since
                // the generator would emit the wrong name.
                if ourAi <> "HighBarV2" then
                    Result.Error (AdapterUnsupportedShape
                        (sprintf "team 0 must use HighBarV2 (got %s); the hub's script generator is wired to that proxy" ourAi))
                else
                    let engineBin =
                        if install.ActiveEngine.HasHeadlessBin then
                            Path.Combine(install.ActiveEngine.EngineDir, "spring-headless")
                        else
                            "spring-headless"
                    let ec: EngineConfig = {
                        Mode = EngineMode.Headless
                        SocketPath = generateSocketPath ()
                        MapName = config.MapName
                        GameType = "Beyond All Reason $latest"
                        OpponentAI = oppAi
                        OpponentSide = oppSide
                        OurSide = ourSide
                        TimeoutMs = 30000
                        EngineBin = engineBin
                        AppImagePath = ""
                        SpringDataDir = Some install.DataDir
                        GameSpeed = int (config.EngineSpeed |> max 1.0f |> min 100.0f)
                        ReadTimeoutMs = None
                        OpponentAIOptions = oppOpts
                        DeathMode = "com"
                    }
                    Ok ec
