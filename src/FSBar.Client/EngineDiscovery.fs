namespace FSBar.Client

open System
open System.IO
open System.IO.Compression

type DiscoveredEngine = {
    VersionString: string
    VersionDir: string
    HeadlessBin: string option
    GraphicalBin: string option
    DataDir: string
}

type DiscoveredGame = {
    Tag: string
    Name: string
    Hash: string
}

type ResolutionSource =
    | OverrideEnvVar
    | ConfigFile
    | AutoDetected

type EngineResolution = {
    Source: ResolutionSource
    Engine: DiscoveredEngine
    Game: DiscoveredGame
}

module EngineDiscovery =

    let standardDataDir () =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    let defaultDataDir () =
        let candidate = standardDataDir ()
        if Directory.Exists(Path.Combine(candidate, "maps"))
           && Directory.Exists(Path.Combine(candidate, "packages")) then
            Some candidate
        else
            None

    let isExecutable (path: string) =
        if not (File.Exists(path)) then false
        else
            try
                let info = UnixFileMode.UserExecute
                let mode = File.GetUnixFileMode(path)
                mode.HasFlag(info)
            with
            | _ -> File.Exists(path)

    let tryBinary (versionDir: string) (name: string) =
        let path = Path.Combine(versionDir, name)
        if isExecutable path then Some path else None

    let discoverEngines (dataDir: string) =
        let engineDir = Path.Combine(dataDir, "engine")
        if not (Directory.Exists(engineDir)) then []
        else
            Directory.GetDirectories(engineDir)
            |> Array.choose (fun dir ->
                let dirName = Path.GetFileName(dir)
                if dirName.StartsWith("recoil_") then
                    let version = dirName.Substring(7)
                    Some {
                        VersionString = version
                        VersionDir = dir
                        HeadlessBin = tryBinary dir "spring-headless"
                        GraphicalBin = tryBinary dir "spring"
                        DataDir = dataDir
                    }
                else
                    None)
            |> Array.sortByDescending (fun e -> e.VersionString)
            |> Array.toList

    let discoverGameVersion (dataDir: string) (tag: string) =
        let versionsPath =
            Path.Combine(dataDir, "rapid/repos-cdn.beyondallreason.dev/byar/versions.gz")
        if not (File.Exists(versionsPath)) then
            // Try alternative rapid location
            let altPath =
                Path.Combine(dataDir, "rapid/repos.springrts.com/byar/versions.gz")
            let pathToUse =
                if File.Exists(altPath) then Some altPath else None
            match pathToUse with
            | None -> None
            | Some p ->
                use fs = File.OpenRead(p)
                use gz = new GZipStream(fs, CompressionMode.Decompress)
                use reader = new StreamReader(gz)
                let content = reader.ReadToEnd()
                let prefix = tag + ","
                content.Split('\n')
                |> Array.tryFindBack (fun line -> line.StartsWith(prefix))
                |> Option.map (fun line ->
                    let parts = line.Split(',')
                    { Tag = tag; Hash = parts.[1]; Name = parts.[3] })
        else
            use fs = File.OpenRead(versionsPath)
            use gz = new GZipStream(fs, CompressionMode.Decompress)
            use reader = new StreamReader(gz)
            let content = reader.ReadToEnd()
            let prefix = tag + ","
            content.Split('\n')
            |> Array.tryFindBack (fun line -> line.StartsWith(prefix))
            |> Option.map (fun line ->
                let parts = line.Split(',')
                { Tag = tag; Hash = parts.[1]; Name = parts.[3] })

    let validateEngine (binaryPath: string) (versionString: string) =
        if not (File.Exists(binaryPath)) then
            failwithf
                "Engine version %s is corrupted: binary not found at %s"
                versionString binaryPath
        if not (isExecutable binaryPath) then
            failwithf
                "Engine version %s is corrupted: binary at %s is not executable"
                versionString binaryPath

    let resolveFromEnvVar () =
        match Environment.GetEnvironmentVariable("HIGHBAR_TEST_ENGINE") with
        | null | "" -> None
        | path ->
            validateEngine path "env:HIGHBAR_TEST_ENGINE"
            // Derive version and datadir from the binary path
            let binDir = Path.GetDirectoryName(path)
            let dirName = Path.GetFileName(binDir)
            let version =
                if dirName.StartsWith("recoil_") then dirName.Substring(7)
                else "unknown"
            let dataDir =
                let parent = Directory.GetParent(binDir)
                if not (isNull parent) && not (isNull parent.Parent) then
                    parent.Parent.FullName
                else
                    standardDataDir ()
            let engine = {
                VersionString = version
                VersionDir = binDir
                HeadlessBin =
                    let hp = Path.Combine(binDir, "spring-headless")
                    if path = hp || isExecutable hp then Some hp else Some path
                GraphicalBin =
                    let gp = Path.Combine(binDir, "spring")
                    if isExecutable gp then Some gp else None
                DataDir = dataDir
            }
            Some engine

    let resolveFromConfigFile (configPath: string) =
        if not (File.Exists(configPath)) then None
        else
            let json = File.ReadAllText(configPath)
            // Simple JSON parsing without external dependencies
            let tryExtract (key: string) =
                let pattern = sprintf "\"%s\"" key
                let idx = json.IndexOf(pattern)
                if idx < 0 then None
                else
                    let colonIdx = json.IndexOf(':', idx + pattern.Length)
                    if colonIdx < 0 then None
                    else
                        let startQuote = json.IndexOf('"', colonIdx + 1)
                        if startQuote < 0 then None
                        else
                            let endQuote = json.IndexOf('"', startQuote + 1)
                            if endQuote < 0 then None
                            else Some (json.Substring(startQuote + 1, endQuote - startQuote - 1))

            let version = tryExtract "version"
            let binary = tryExtract "binary" |> Option.defaultValue "spring-headless"

            match version with
            | None -> None
            | Some ver ->
                match defaultDataDir () with
                | None -> None
                | Some dataDir ->
                    let versionDir = Path.Combine(dataDir, "engine", sprintf "recoil_%s" ver)
                    if not (Directory.Exists(versionDir)) then
                        failwithf
                            "Engine version %s specified in %s not found. Expected directory: %s"
                            ver configPath versionDir
                    let binPath = Path.Combine(versionDir, binary)
                    validateEngine binPath ver
                    Some {
                        VersionString = ver
                        VersionDir = versionDir
                        HeadlessBin =
                            let hp = Path.Combine(versionDir, "spring-headless")
                            if isExecutable hp then Some hp else None
                        GraphicalBin =
                            let gp = Path.Combine(versionDir, "spring")
                            if isExecutable gp then Some gp else None
                        DataDir = dataDir
                    }

    let sourceLabel = function
        | OverrideEnvVar -> "env:HIGHBAR_TEST_ENGINE"
        | ConfigFile -> "engine-version.json"
        | AutoDetected -> "auto-detected"

    let resolveEngine (configPath: string option) =
        // Try env var first
        match resolveFromEnvVar () with
        | Some engine ->
            let game =
                discoverGameVersion engine.DataDir "byar:test"
                |> Option.defaultValue { Tag = "byar:test"; Name = "Beyond All Reason"; Hash = "" }
            let resolution = { Source = OverrideEnvVar; Engine = engine; Game = game }
            printfn "[EngineDiscovery] Resolved engine %s from %s" engine.VersionString (sourceLabel resolution.Source)
            printfn "[EngineDiscovery]   Headless: %s" (engine.HeadlessBin |> Option.defaultValue "N/A")
            printfn "[EngineDiscovery]   Graphical: %s" (engine.GraphicalBin |> Option.defaultValue "N/A")
            printfn "[EngineDiscovery]   Game: %s" resolution.Game.Name
            resolution
        | None ->
        // Try config file
        match configPath |> Option.bind resolveFromConfigFile with
        | Some engine ->
            let game =
                discoverGameVersion engine.DataDir "byar:test"
                |> Option.defaultValue { Tag = "byar:test"; Name = "Beyond All Reason"; Hash = "" }
            let resolution = { Source = ConfigFile; Engine = engine; Game = game }
            printfn "[EngineDiscovery] Resolved engine %s from %s" engine.VersionString (sourceLabel resolution.Source)
            printfn "[EngineDiscovery]   Headless: %s" (engine.HeadlessBin |> Option.defaultValue "N/A")
            printfn "[EngineDiscovery]   Graphical: %s" (engine.GraphicalBin |> Option.defaultValue "N/A")
            printfn "[EngineDiscovery]   Game: %s" resolution.Game.Name
            resolution
        | None ->
        // Auto-detect
        match defaultDataDir () with
        | None ->
            let searched = standardDataDir ()
            failwithf
                "No BAR engine found. Searched locations:\n  - HIGHBAR_TEST_ENGINE (not set)\n  - %s (not found or missing maps/packages)\nInstall Beyond All Reason or set HIGHBAR_TEST_ENGINE."
                searched
        | Some dataDir ->
            let engines = discoverEngines dataDir
            match engines with
            | [] ->
                let engineDir = Path.Combine(dataDir, "engine")
                let configNote =
                    match configPath with
                    | Some p -> sprintf "\n  - %s (not found)" p
                    | None -> ""
                failwithf
                    "No BAR engine found. Searched locations:\n  - HIGHBAR_TEST_ENGINE (not set)%s\n  - %s (no recoil_* directories)\nInstall Beyond All Reason or set HIGHBAR_TEST_ENGINE."
                    configNote engineDir
            | latest :: _ ->
                // Validate that at least one binary exists
                match latest.HeadlessBin, latest.GraphicalBin with
                | None, None ->
                    failwithf
                        "Engine version %s is corrupted: neither spring-headless nor spring binary found in %s"
                        latest.VersionString latest.VersionDir
                | _ -> ()
                let game =
                    discoverGameVersion dataDir "byar:test"
                    |> Option.defaultValue { Tag = "byar:test"; Name = "Beyond All Reason"; Hash = "" }
                let resolution = { Source = AutoDetected; Engine = latest; Game = game }
                printfn "[EngineDiscovery] Resolved engine %s from %s" latest.VersionString (sourceLabel resolution.Source)
                printfn "[EngineDiscovery]   Headless: %s" (latest.HeadlessBin |> Option.defaultValue "N/A")
                printfn "[EngineDiscovery]   Graphical: %s" (latest.GraphicalBin |> Option.defaultValue "N/A")
                printfn "[EngineDiscovery]   Game: %s" resolution.Game.Name
                resolution
