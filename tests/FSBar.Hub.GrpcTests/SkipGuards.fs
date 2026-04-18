module FSBar.Hub.GrpcTests.SkipGuards

open System
open System.IO
open FSBar.Hub

let private defaultDataDir =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".local/state/Beyond All Reason")

let requireBarInstall () : unit =
    if not (Directory.Exists(defaultDataDir)) then
        raise (Xunit.SkipException(sprintf "BAR data dir not found at %s" defaultDataDir))
    let settings = { HubSettings.defaults with BarDataDirOverride = Some defaultDataDir }
    match BarInstall.detect settings with
    | Result.Error e ->
        raise (Xunit.SkipException(sprintf "BarInstall.detect failed: %s" (BarInstall.formatError e)))
    | Ok _ -> ()

let requireEngineInstalled () : unit =
    if not (Directory.Exists(defaultDataDir)) then
        raise (Xunit.SkipException(sprintf "BAR data dir not found at %s" defaultDataDir))
    let settings = { HubSettings.defaults with BarDataDirOverride = Some defaultDataDir }
    match BarInstall.detect settings with
    | Result.Error e ->
        raise (Xunit.SkipException(sprintf "BarInstall.detect failed: %s" (BarInstall.formatError e)))
    | Ok install ->
        if not install.ActiveEngine.HasHeadlessBin then
            raise (Xunit.SkipException(sprintf "spring-headless not found under %s" install.ActiveEngine.EngineDir))
        let required = [ "HighBarV2"; "BARb" ]
        let installed = BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList
        let missing = required |> List.filter (installed.Contains >> not)
        if not (List.isEmpty missing) then
            raise (Xunit.SkipException(sprintf "required skirmish AIs not installed: %s" (String.concat ", " missing)))
