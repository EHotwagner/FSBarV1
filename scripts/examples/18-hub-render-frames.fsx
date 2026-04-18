// 18-hub-render-frames.fsx — Feature 040 US2 walkthrough.
//
// Demonstrates the rendered-viewer-frame stream: connects to a running
// FSBar.Hub.App, subscribes to StreamRenderFrames at 10 Hz, saves the
// first 10 frames to /tmp/fsbar-hub-frames/, and measures the per-frame
// render+encode latency vs. wall-clock receipt.
//
// The hub does not need a Running session — with
// EmitNoSessionPlaceholder=true it produces a placeholder PNG every
// tick so this script runs even on a fresh hub.
//
// Run:
//   dotnet fsi scripts/examples/18-hub-render-frames.fsx

#r "nuget: Grpc.Net.Client, 2.*"
#r "nuget: FSBar.Hub, *-*"

open System
open System.IO
open Grpc.Net.Client
open Fsbar.Hub.Scripting.V1

let endpoint = "http://127.0.0.1:5021"
let outDir = "/tmp/fsbar-hub-frames"
Directory.CreateDirectory outDir |> ignore

printfn "Opening gRPC channel to %s …" endpoint
use channel = GrpcChannel.ForAddress(endpoint)
let client = ScriptingService.ScriptingServiceClient(channel)

// One-shot: GetRenderFrame returns a single rendered PNG.
let shot =
    client.GetRenderFrame(
        { Format = ImageFormat.Png
          ViewportWidth = 1024
          ViewportHeight = 768
          JpegQuality = 0 } : GetRenderFrameRequest)
match shot.Frame with
| Some f ->
    let path = Path.Combine(outDir, "single-shot.png")
    File.WriteAllBytes(path, f.ImageBytes.Data.ToArray())
    printfn "GetRenderFrame: %d bytes → %s (placeholder=%b)"
        f.ImageBytes.Length path f.IsPlaceholder
| None ->
    printfn "GetRenderFrame returned no frame"

// Streaming: subscribe at 10 Hz, grab the first 10 frames.
let streamReq : StreamRenderFramesRequest = {
    ClientLabel = "18-hub-render-frames"
    TargetHz = 10
    Format = ImageFormat.Png
    ViewportWidth = 800
    ViewportHeight = 600
    JpegQuality = 0
    CloseOnSessionEnd = false
    EmitNoSessionPlaceholder = true
}

printfn ""
printfn "Subscribing to StreamRenderFrames at %d Hz …" streamReq.TargetHz
use call = client.StreamRenderFrames(streamReq)

let reader = call.ResponseStream
let latencies = ResizeArray<int64>()

let pump =
    task {
        let mutable n = 0
        while n < 10 do
            let! more = reader.MoveNext(System.Threading.CancellationToken.None)
            if not more then n <- 10
            else
                let f = reader.Current
                let recvMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                let latency = recvMs - f.EncodedAtUnixMs
                latencies.Add latency
                let path = Path.Combine(outDir, sprintf "frame-%03d.png" n)
                File.WriteAllBytes(path, f.ImageBytes.Data.ToArray())
                printfn "  #%03d seq=%d size=%d bytes encode-to-recv=%d ms"
                    n f.ClientSequence f.ImageBytes.Length latency
                n <- n + 1
    }

pump.GetAwaiter().GetResult()

let avg =
    if latencies.Count = 0 then 0L
    else latencies |> Seq.sum |> fun s -> s / int64 latencies.Count
printfn ""
printfn "Captured %d frames → %s  (avg encode→recv latency ≈ %dms)"
    latencies.Count outDir avg
