// src/FSBar.Hub/scripts/examples/01-detect-bar-install.fsx
//
// Resolves the user's BAR install from the hub's point of view: data
// dir, active engine version, installed skirmish AIs. Prints the
// result. Pure file-system I/O — does not launch the hub or an
// engine.
//
// Usage:
//     dotnet fsi src/FSBar.Hub/scripts/examples/01-detect-bar-install.fsx
//
// Requires: `dotnet build src/FSBar.Hub.App/FSBar.Hub.App.fsproj`
// so the prelude can pick up transitive dependencies.

#load "../prelude.fsx"

open FSBar.Hub

let settings = HubSettings.load ()
printfn "Settings: barDataDirOverride=%A engineVersionOverride=%A gRPC=%d"
    settings.BarDataDirOverride settings.EngineVersionOverride settings.GrpcPort

match BarInstall.detect settings with
| Result.Error err ->
    eprintfn "✗ %s" (BarInstall.formatError err)
    exit 1
| Ok install ->
    printfn ""
    printfn "BAR install:"
    printfn "  data dir    : %s" install.DataDir
    printfn "  active engine: %s" install.ActiveEngine.Version
    printfn "  engines     :"
    for e in install.Engines do
        let flag =
            if e.Version = install.ActiveEngine.Version then "→" else " "
        printfn "    %s %s%s%s"
            flag e.Version
            (if e.HasHeadlessBin then " [spring-headless]" else "")
            (if e.HasGraphicalBin then " [spring]" else "")
    printfn "  AIs installed: %s"
        (BarInstall.listSkirmishAis install.ActiveEngine |> String.concat ", ")
