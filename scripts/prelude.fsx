// FSBar.Client Prelude — load this once to get started:
//   #load "scripts/prelude.fsx"
//
// Then use:
//   let client = BarClient.startHeadless()
//   let frame = client.Step()
//   client.Stop()

#r "../src/FSBar.Proto/bin/Debug/net10.0/FsGrpc.dll"
#r "../src/FSBar.Proto/bin/Debug/net10.0/Google.Protobuf.dll"
#r "../src/FSBar.Proto/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../src/FSBar.Client/bin/Debug/net10.0/FSBar.Client.dll"
#r "../src/FSBar.Client/bin/Debug/net10.0/BarData.dll"
#r "../src/FSBar.Viz/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../src/FSBar.Viz/bin/Debug/net10.0/SkiaSharp.dll"

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
