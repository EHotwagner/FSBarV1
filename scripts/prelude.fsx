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

printfn "FSBar.Client + FSBar.Viz loaded. Use BarClient.startHeadless() to begin."
