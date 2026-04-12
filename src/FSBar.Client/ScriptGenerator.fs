namespace FSBar.Client

open System.Text

/// <summary>Generates Spring engine startup scripts from an <see cref="T:FSBar.Client.EngineConfig"/>.</summary>
module ScriptGenerator =
    /// <summary>
    /// Generates a Spring engine TDF-format startup script that configures a two-team game
    /// with the HighBar V2 AI proxy on team 0 and the specified opponent AI on team 1.
    /// The script includes mod options for faction selection, game speed, and debug commands.
    /// </summary>
    /// <param name="config">The engine configuration specifying map, factions, socket path, and opponent AI.</param>
    /// <returns>A complete TDF-format script string ready to be written to disk for engine launch.</returns>
    let generate (config: EngineConfig) : string =
        let ourFaction = config.OurSide.ToLowerInvariant()
        let opponentFaction = config.OpponentSide.ToLowerInvariant()
        let sb = StringBuilder()
        let ln (s: string) = sb.AppendLine(s) |> ignore
        let speed = config.GameSpeed

        ln "[GAME]"
        ln "{"
        ln $"\tGameType={config.GameType};"
        ln $"\tMapName={config.MapName};"
        ln "\tModHash=1;"
        ln "\tMapHash=1;"
        ln "\tIsHost=1;"
        ln "\tHostIP=127.0.0.1;"
        ln "\tHostPort=0;"
        ln "\tMyPlayerName=FSBarClient;"
        ln "\tNoHelperAIs=0;"
        ln "\tStartPosType=0;"
        ln "\tGameStartDelay=0;"
        ln "\tFixedRNGSeed=1;"
        ln ""
        // BAR's game_end.lua gadget reads the `gamemode` modoption (not `deathmode`)
        // to decide when to end the match. Map EngineConfig.DeathMode to the BAR
        // gamemode value: "com" → 0 (game ends when a side's last commander dies,
        // which is what the trainer needs), anything else → 3 (never end).
        let barGameMode =
            match config.DeathMode with
            | "com" -> 0
            | _ -> 3
        ln "\t[MODOPTIONS]"
        ln "\t{"
        ln $"\t\tGameMode={barGameMode};"
        ln "\t\tdraft_mode=disabled;"
        ln $"\t\tteamfaction_0={ourFaction};"
        ln $"\t\tteamfaction_1={opponentFaction};"
        ln $"\t\tMinSpeed={speed};"
        ln $"\t\tMaxSpeed={speed};"
        ln $"\t\tdeathmode={config.DeathMode};"
        ln "\t\tdebugcommands=1:cheat|3:globallos;"
        ln "\t}"
        ln ""
        ln "\t[MAPOPTIONS]"
        ln "\t{"
        ln "\t}"
        ln ""
        ln "\t[PLAYER0]"
        ln "\t{"
        ln "\t\tName=FSBarClient;"
        ln "\t\tTeam=0;"
        ln "\t\tIsFromDemo=0;"
        ln "\t\tSpectator=1;"
        ln "\t}"
        ln ""
        ln "\t[AI0]"
        ln "\t{"
        ln "\t\tName=HighBarV2;"
        ln "\t\tTeam=0;"
        ln "\t\tShortName=HighBarV2;"
        ln "\t\tVersion=0.1;"
        ln "\t\tHost=0;"
        ln "\t\t[OPTIONS]"
        ln "\t\t{"
        ln $"\t\t\tsocket_path={config.SocketPath};"
        ln "\t\t}"
        ln "\t}"
        ln ""
        ln "\t[AI1]"
        ln "\t{"
        ln $"\t\tName={config.OpponentAI};"
        ln "\t\tTeam=1;"
        ln $"\t\tShortName={config.OpponentAI};"
        ln "\t\tHost=0;"
        if not (Map.isEmpty config.OpponentAIOptions) then
            ln "\t\t[OPTIONS]"
            ln "\t\t{"
            for KeyValue(key, value) in config.OpponentAIOptions do
                ln $"\t\t\t{key}={value};"
            ln "\t\t}"
        ln "\t}"
        ln ""
        ln "\t[TEAM0]"
        ln "\t{"
        ln "\t\tTeamLeader=0;"
        ln "\t\tAllyTeam=0;"
        ln $"\t\tSide={config.OurSide};"
        ln "\t\tStartPosX=800;"
        ln "\t\tStartPosZ=800;"
        ln "\t}"
        ln ""
        ln "\t[TEAM1]"
        ln "\t{"
        ln "\t\tTeamLeader=0;"
        ln "\t\tAllyTeam=1;"
        ln $"\t\tSide={config.OpponentSide};"
        ln "\t\tStartPosX=3200;"
        ln "\t\tStartPosZ=3200;"
        ln "\t}"
        ln ""
        ln "\t[ALLYTEAM0]"
        ln "\t{"
        ln "\t\tNumAllies=0;"
        ln "\t}"
        ln ""
        ln "\t[ALLYTEAM1]"
        ln "\t{"
        ln "\t\tNumAllies=0;"
        ln "\t}"
        ln ""
        ln "\tNumTeams=2;"
        ln "\tNumAllyTeams=2;"
        ln "\tNumPlayers=1;"
        ln "}"

        sb.ToString()
