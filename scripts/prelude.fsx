// FSBar.Client Prelude — load this once to get started:
//   ./pack-dev.sh
//   #load "scripts/prelude.fsx"
//
// Then use:
//   let client = BarClient.startHeadless()
//   use sub = client.Frames.Subscribe(fun frame -> printfn "%A" frame)
//   client.Stop()

#r "nuget: FSBar.Viz, *-*"

open FSBar.Client
open FSBar.Client.Commands
open FSBar.Client.MapQuery
open FSBar.Viz
open BarData

/// Discover installed engine versions in the standard BAR data directory.
let engines () =
    match EngineDiscovery.defaultDataDir () with
    | Some dir -> EngineDiscovery.discoverEngines dir
    | None -> printfn "BAR data directory not found"; []

/// Resolve the engine to use (env var → config → auto-detect).
let resolveEngine () = EngineDiscovery.resolveEngine None

printfn "FSBar.Client + FSBar.Viz loaded. Use BarClient.startHeadless() to begin."
