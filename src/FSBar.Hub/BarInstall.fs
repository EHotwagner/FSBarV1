namespace FSBar.Hub

// Note: `FSBar.Client.SessionState` defines its own `Error of string`
// case. We deliberately do not `open FSBar.Client` here so that bare
// `Error` resolves to `Result.Error` as expected; `DiscoveredEngine`
// and `EngineDiscovery` are referenced fully-qualified.
open System
open System.IO

module BarInstall =

    type EngineVersionEntry = {
        Version: string
        EngineDir: string
        HasHeadlessBin: bool
        HasGraphicalBin: bool
        AiSkirmishDir: string
    }

    type BarInstall = {
        DataDir: string
        Engines: EngineVersionEntry list
        ActiveEngine: EngineVersionEntry
        DataDirIsDefault: bool
    }

    type BarInstallError =
        | DataDirNotFound of path: string
        | EngineSubdirMissing of path: string
        | NoEngineVersions of path: string
        | OverriddenEngineNotFound of version: string

    let private standardDataDir () =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    let resolveDataDir (settings: HubSettings.HubSettings) =
        match settings.BarDataDirOverride with
        | Some path when not (String.IsNullOrWhiteSpace(path)) -> path
        | _ -> standardDataDir ()

    let formatError (err: BarInstallError) =
        match err with
        | DataDirNotFound p -> sprintf "BAR data directory not found: %s" p
        | EngineSubdirMissing p -> sprintf "BAR engine/ subdirectory missing under: %s" p
        | NoEngineVersions p -> sprintf "no recoil_* engine versions installed under: %s" p
        | OverriddenEngineNotFound v -> sprintf "engine version %s (from settings override) is not installed" v

    let private toEntry (discovered: FSBar.Client.DiscoveredEngine) : EngineVersionEntry = {
        Version = discovered.VersionString
        EngineDir = discovered.VersionDir
        HasHeadlessBin = Option.isSome discovered.HeadlessBin
        HasGraphicalBin = Option.isSome discovered.GraphicalBin
        AiSkirmishDir = Path.Combine(discovered.VersionDir, "AI", "Skirmish")
    }

    let detect (settings: HubSettings.HubSettings) : Result<BarInstall, BarInstallError> =
        let dataDir = resolveDataDir settings
        if not (Directory.Exists(dataDir)) then
            Error (DataDirNotFound dataDir)
        else
            let engineSubdir = Path.Combine(dataDir, "engine")
            if not (Directory.Exists(engineSubdir)) then
                Error (EngineSubdirMissing engineSubdir)
            else
                let entries =
                    FSBar.Client.EngineDiscovery.discoverEngines dataDir
                    |> List.map toEntry
                match entries with
                | [] -> Error (NoEngineVersions engineSubdir)
                | _ ->
                    let active =
                        match settings.EngineVersionOverride with
                        | Some requested when not (String.IsNullOrWhiteSpace(requested)) ->
                            entries |> List.tryFind (fun e -> e.Version = requested)
                        | _ ->
                            entries |> List.tryHead
                    match active with
                    | None ->
                        match settings.EngineVersionOverride with
                        | Some v -> Error (OverriddenEngineNotFound v)
                        | None -> Error (NoEngineVersions engineSubdir)
                    | Some active ->
                        let dataDirIsDefault = (dataDir = standardDataDir ())
                        Ok { DataDir = dataDir
                             Engines = entries
                             ActiveEngine = active
                             DataDirIsDefault = dataDirIsDefault }

    let listSkirmishAis (engine: EngineVersionEntry) : string list =
        if not (Directory.Exists(engine.AiSkirmishDir)) then []
        else
            Directory.GetDirectories(engine.AiSkirmishDir)
            |> Array.map Path.GetFileName
            |> Array.sort
            |> Array.toList
