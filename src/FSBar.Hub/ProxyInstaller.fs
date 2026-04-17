namespace FSBar.Hub

// Do not `open FSBar.Client` — `FSBar.Client.SessionState.Error`
// shadows `Result.Error` when imported at namespace scope.
open System
open System.IO
open System.Text
open System.Text.RegularExpressions

module ProxyInstaller =

    type ProxyInstallStatus = {
        EngineVersion: string
        InstalledAtPath: string
        InstalledVersion: string option
        AiFilesPresent: bool
        DevModeFilePresent: bool
        SimpleAiListDisabled: bool
        MatchesBundled: bool
    }

    type ProxyHealth =
        | UpToDate
        | NotInstalled
        | StaleVersion of installed: string * bundled: string
        | StaleEngine of forEngine: string * activeEngine: string
        | ConfigIncomplete of reasons: string list

    let formatHealth (h: ProxyHealth) =
        match h with
        | UpToDate -> "up to date"
        | NotInstalled -> "proxy is not installed"
        | StaleVersion(installed, bundled) ->
            sprintf "stale version: installed %s, bundled %s" installed bundled
        | StaleEngine(forEngine, activeEngine) ->
            sprintf "stale engine: installed under %s, active is %s" forEngine activeEngine
        | ConfigIncomplete reasons -> sprintf "config incomplete: %s" (String.concat "; " reasons)

    // --- rewriteSimpleAiList (research.md R5) ------------------------------

    /// Regex per R5 — anchored multiline, group 4 is the bool token.
    let private simpleAiListRegex =
        Regex(
            @"^(\s*)simpleAiList(\s*)=(\s*)(true|false)(\s*,?\s*)$",
            RegexOptions.Multiline ||| RegexOptions.Compiled)

    let rewriteSimpleAiList (contents: string) : string option =
        let m = simpleAiListRegex.Match(contents)
        if not m.Success then
            // Phase-2 simplification: key absent → no-op. Chobby
            // creates the key on first launch; a second `install` run
            // after the user opens Chobby once picks it up.
            None
        else
            let current = m.Groups.[4].Value
            if current = "false" then None
            else
                let sb = StringBuilder()
                sb.Append(contents.Substring(0, m.Groups.[4].Index)) |> ignore
                sb.Append("false") |> ignore
                sb.Append(contents.Substring(m.Groups.[4].Index + m.Groups.[4].Length)) |> ignore
                Some (sb.ToString())

    // --- Path helpers ------------------------------------------------------

    let private proxyInstallDir (engine: BarInstall.EngineVersionEntry) (version: string) =
        Path.Combine(engine.AiSkirmishDir, "HighBarV2", version)

    /// Refuses target paths that live under packages/ or pool/ (FR-010).
    let private isSafeWritePath (path: string) =
        let full = Path.GetFullPath(path)
        let segments =
            full.Split([| Path.DirectorySeparatorChar; Path.AltDirectorySeparatorChar |])
            |> Array.map (fun s -> s.ToLowerInvariant())
        not (Array.contains "packages" segments)
        && not (Array.contains "pool" segments)

    // --- checkStatus -------------------------------------------------------

    /// Scans the AI/Skirmish/HighBarV2/ directory under `engine` for
    /// any installed version subdirectory. Returns the version
    /// directory name for the first one whose three required files
    /// exist. Defensive against the multi-version case by preferring
    /// an exact match with `bundled.Version` and falling back to any
    /// complete install.
    let private findInstalledVersion
            (engine: BarInstall.EngineVersionEntry)
            (bundled: BundledProxy.BundledProxyInfo)
            : (string * string) option =
        let rootDir = Path.Combine(engine.AiSkirmishDir, "HighBarV2")
        if not (Directory.Exists(rootDir)) then None
        else
            let isComplete (dir: string) =
                [ "libSkirmishAI.so"; "AIInfo.lua"; "AIOptions.lua" ]
                |> List.forall (fun f -> File.Exists(Path.Combine(dir, f)))
            let subdirs =
                Directory.GetDirectories(rootDir)
                |> Array.map (fun d -> Path.GetFileName(d), d)
            // Prefer exact version match.
            match subdirs |> Array.tryFind (fun (name, _) -> name = bundled.Version) with
            | Some (name, path) when isComplete path -> Some (name, path)
            | _ ->
                subdirs
                |> Array.tryFind (fun (_, path) -> isComplete path)

    let checkStatus
            (install: BarInstall.BarInstall)
            (bundled: BundledProxy.BundledProxyInfo)
            : ProxyInstallStatus =
        let engine = install.ActiveEngine
        let installedAt = proxyInstallDir engine bundled.Version

        let installedEntry = findInstalledVersion engine bundled
        let installedVersion = installedEntry |> Option.map fst
        let aiFilesPresent =
            match installedEntry with
            | Some (v, path) when v = bundled.Version ->
                [ "libSkirmishAI.so"; "AIInfo.lua"; "AIOptions.lua" ]
                |> List.forall (fun f -> File.Exists(Path.Combine(path, f)))
            | _ -> false

        let devModePath = Path.Combine(install.DataDir, "devmode.txt")
        let devModePresent = File.Exists(devModePath)

        let iglPath = Path.Combine(install.DataDir, "LuaMenu", "Config", "IGL_data.lua")
        let simpleAiListDisabled =
            if not (File.Exists(iglPath)) then false
            else
                let contents = File.ReadAllText(iglPath)
                // Key absent → report as "not disabled" so Settings
                // surfaces ConfigIncomplete; Chobby creates it on
                // first run.
                let m = simpleAiListRegex.Match(contents)
                m.Success && m.Groups.[4].Value = "false"

        { EngineVersion = engine.Version
          InstalledAtPath = installedAt
          InstalledVersion = installedVersion
          AiFilesPresent = aiFilesPresent
          DevModeFilePresent = devModePresent
          SimpleAiListDisabled = simpleAiListDisabled
          MatchesBundled = installedVersion = Some bundled.Version && aiFilesPresent }

    let health (status: ProxyInstallStatus) : ProxyHealth =
        match status.InstalledVersion with
        | None -> NotInstalled
        | Some installed ->
            if not status.MatchesBundled then
                let bundledName =
                    // We don't have the bundled version on the record
                    // itself — `health` only receives status — so we
                    // surface `installed` on both sides; the caller
                    // that has access to the bundle can refine the
                    // message. This matches the fsi-sketch contract.
                    installed
                StaleVersion(installed, bundledName)
            else
                let reasons =
                    [ if not status.DevModeFilePresent then
                          yield sprintf "devmode.txt missing at <dataDir>"
                      if not status.SimpleAiListDisabled then
                          yield "simpleAiList is not set to false in IGL_data.lua" ]
                if List.isEmpty reasons then UpToDate
                else ConfigIncomplete reasons

    // --- install -----------------------------------------------------------

    let private contentsMatch (srcPath: string) (dstPath: string) =
        if not (File.Exists(dstPath)) then false
        else
            let srcInfo = FileInfo(srcPath)
            let dstInfo = FileInfo(dstPath)
            if srcInfo.Length <> dstInfo.Length then false
            else
                use a = File.OpenRead(srcPath)
                use b = File.OpenRead(dstPath)
                let aBuf = Array.zeroCreate 4096
                let bBuf = Array.zeroCreate 4096
                let mutable equal = true
                let mutable keepGoing = true
                while keepGoing do
                    let aRead = a.Read(aBuf, 0, aBuf.Length)
                    let bRead = b.Read(bBuf, 0, bBuf.Length)
                    if aRead <> bRead then
                        equal <- false
                        keepGoing <- false
                    elif aRead = 0 then
                        keepGoing <- false
                    else
                        let mutable i = 0
                        while i < aRead && equal do
                            if aBuf.[i] <> bBuf.[i] then equal <- false
                            i <- i + 1
                        if not equal then keepGoing <- false
                equal

    let private copyFileIfDifferent (srcPath: string) (dstPath: string) : Result<bool, string> =
        // Returns Ok true when the file was written, Ok false when
        // skipped because identical, Error when the copy failed.
        try
            if not (isSafeWritePath dstPath) then
                Result.Error (sprintf "refusing to write under packages/ or pool/: %s" dstPath)
            elif contentsMatch srcPath dstPath then Ok false
            else
                let dir = Path.GetDirectoryName(dstPath)
                if not (String.IsNullOrEmpty(dir)) then
                    Directory.CreateDirectory(dir) |> ignore
                File.Copy(srcPath, dstPath, overwrite = true)
                Ok true
        with ex -> Result.Error (sprintf "copy failed: %s" ex.Message)

    let private copyAiFiles
            (bundled: BundledProxy.BundledProxyInfo)
            (engine: BarInstall.EngineVersionEntry)
            (events: HubEvents.IHubEventSink)
            (force: bool)
            : Result<string list, string> =
        let destDir = proxyInstallDir engine bundled.Version
        let dirResult =
            try Directory.CreateDirectory(destDir) |> ignore; Ok ()
            with ex -> Result.Error (sprintf "could not create %s: %s" destDir ex.Message)
        match dirResult with
        | Result.Error e ->
            events.Publish(
                HubEvents.ProxyInstallProgress(
                    HubEvents.CopyAiFiles, HubEvents.StepFailed e))
            Result.Error e
        | Ok () ->

        // Mtime guard for libSkirmishAI.so (spec.md Edge Cases).
        let libSrc = bundled.LibSkirmishAiPath
        let libDst = Path.Combine(destDir, "libSkirmishAI.so")
        let localIsNewer =
            File.Exists(libDst)
            && (FileInfo(libDst).LastWriteTimeUtc > FileInfo(libSrc).LastWriteTimeUtc)
            && not (contentsMatch libSrc libDst)
        if localIsNewer && not force then
            let msg =
                sprintf
                    "on-disk libSkirmishAI.so is newer than bundled — skipping; pass force=true to overwrite (on-disk: %s, bundled: %s)"
                    (FileInfo(libDst).LastWriteTimeUtc.ToString("o"))
                    (FileInfo(libSrc).LastWriteTimeUtc.ToString("o"))
            events.Publish(
                HubEvents.ProxyInstallProgress(
                    HubEvents.CopyAiFiles,
                    HubEvents.StepFailed msg))
            Result.Error msg
        else
            let files =
                [ bundled.LibSkirmishAiPath, libDst
                  bundled.AiInfoLuaPath, Path.Combine(destDir, "AIInfo.lua")
                  bundled.AiOptionsLuaPath, Path.Combine(destDir, "AIOptions.lua") ]
            let results =
                files |> List.map (fun (src, dst) -> src, dst, copyFileIfDifferent src dst)
            let failures =
                results |> List.choose (fun (_, _, r) ->
                    match r with Result.Error e -> Some e | _ -> None)
            let wrote =
                results |> List.choose (fun (_, dst, r) ->
                    match r with Ok true -> Some dst | _ -> None)
            match failures with
            | [] ->
                if List.isEmpty wrote then
                    events.Publish(
                        HubEvents.ProxyInstallProgress(HubEvents.CopyAiFiles, HubEvents.Skipped))
                else
                    events.Publish(
                        HubEvents.ProxyInstallProgress(HubEvents.CopyAiFiles, HubEvents.Performed))
                Ok wrote
            | reasons ->
                let combined = String.concat "; " reasons
                events.Publish(
                    HubEvents.ProxyInstallProgress(
                        HubEvents.CopyAiFiles,
                        HubEvents.StepFailed combined))
                Result.Error combined

    let private touchDevMode
            (install: BarInstall.BarInstall)
            (events: HubEvents.IHubEventSink)
            : Result<bool, string> =
        let path = Path.Combine(install.DataDir, "devmode.txt")
        try
            if not (isSafeWritePath path) then
                let msg = sprintf "refusing to write under packages/ or pool/: %s" path
                events.Publish(
                    HubEvents.ProxyInstallProgress(
                        HubEvents.TouchDevMode, HubEvents.StepFailed msg))
                Result.Error msg
            elif File.Exists(path) then
                events.Publish(
                    HubEvents.ProxyInstallProgress(HubEvents.TouchDevMode, HubEvents.Skipped))
                Ok false
            else
                let dir = Path.GetDirectoryName(path)
                if not (String.IsNullOrEmpty(dir)) then
                    Directory.CreateDirectory(dir) |> ignore
                File.WriteAllText(path, "")
                events.Publish(
                    HubEvents.ProxyInstallProgress(HubEvents.TouchDevMode, HubEvents.Performed))
                Ok true
        with ex ->
            let msg = sprintf "touch devmode.txt failed: %s" ex.Message
            events.Publish(
                HubEvents.ProxyInstallProgress(
                    HubEvents.TouchDevMode, HubEvents.StepFailed msg))
            Result.Error msg

    let private toggleSimpleAiList
            (install: BarInstall.BarInstall)
            (events: HubEvents.IHubEventSink)
            : Result<bool, string> =
        let path = Path.Combine(install.DataDir, "LuaMenu", "Config", "IGL_data.lua")
        try
            if not (isSafeWritePath path) then
                let msg = sprintf "refusing to write under packages/ or pool/: %s" path
                events.Publish(
                    HubEvents.ProxyInstallProgress(
                        HubEvents.ToggleSimpleAiList, HubEvents.StepFailed msg))
                Result.Error msg
            elif not (File.Exists(path)) then
                // Chobby has not yet written IGL_data.lua. Not an
                // error — the user just hasn't run Chobby yet. Skip
                // and let a later `install` call pick it up after
                // Chobby creates the file.
                events.Publish(
                    HubEvents.ProxyInstallProgress(HubEvents.ToggleSimpleAiList, HubEvents.Skipped))
                Ok false
            else
                let contents = File.ReadAllText(path)
                match rewriteSimpleAiList contents with
                | None ->
                    events.Publish(
                        HubEvents.ProxyInstallProgress(HubEvents.ToggleSimpleAiList, HubEvents.Skipped))
                    Ok false
                | Some updated ->
                    // Atomic write: tmp then rename, so Chobby's
                    // settings-watcher never sees a torn file.
                    let tmp = path + ".tmp"
                    File.WriteAllText(tmp, updated)
                    File.Move(tmp, path, overwrite = true)
                    events.Publish(
                        HubEvents.ProxyInstallProgress(HubEvents.ToggleSimpleAiList, HubEvents.Performed))
                    Ok true
        with ex ->
            let msg = sprintf "edit IGL_data.lua failed: %s" ex.Message
            events.Publish(
                HubEvents.ProxyInstallProgress(
                    HubEvents.ToggleSimpleAiList, HubEvents.StepFailed msg))
            Result.Error msg

    let install
            (install: BarInstall.BarInstall)
            (bundled: BundledProxy.BundledProxyInfo)
            (events: HubEvents.IHubEventSink)
            (force: bool)
            : Result<ProxyInstallStatus, string list> =
        let failures = ResizeArray<string>()
        match copyAiFiles bundled install.ActiveEngine events force with
        | Ok _ -> ()
        | Result.Error e -> failures.Add(sprintf "CopyAiFiles: %s" e)
        match touchDevMode install events with
        | Ok _ -> ()
        | Result.Error e -> failures.Add(sprintf "TouchDevMode: %s" e)
        match toggleSimpleAiList install events with
        | Ok _ -> ()
        | Result.Error e -> failures.Add(sprintf "ToggleSimpleAiList: %s" e)

        if failures.Count = 0 then
            Ok (checkStatus install bundled)
        else
            Result.Error (failures |> List.ofSeq)
