// 09-engine-opponent-options.fsx — Render a start script with OpponentAIOptions + DeathMode
// Usage: dotnet build src/FSBar.Client/FSBar.Client.fsproj -c Debug
//        dotnet fsi scripts/examples/09-engine-opponent-options.fsx
//
// Demonstrates the EngineConfig fields added for the iterative bot trainer:
// - DeathMode controls the [MODOPTIONS] deathmode value (e.g. "com" to end on commander death).
// - OpponentAIOptions becomes the [AI1].[OPTIONS] block (e.g. BARb difficulty profile).
// No engine is launched — this just prints the generated TDF script to stdout.
//
// This example references the locally-built FSBar.Client.dll directly (not the prelude's
// nuget FSBar.Viz chain), because these fields are new API that hasn't been packaged yet.

#r "../../src/FSBar.Client/bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client

let config =
    { EngineConfig.defaultConfig () with
        DeathMode = "com"
        OpponentAI = "BARb"
        OpponentAIOptions = Map.ofList [ "profile", "easy" ] }

printfn "--- Generated start script ---"
printfn "%s" (ScriptGenerator.generate config)
printfn "--- end ---"
printfn ""
printfn "Look for: deathmode=com;  and  [AI1] ... [OPTIONS] { profile=easy; }"
