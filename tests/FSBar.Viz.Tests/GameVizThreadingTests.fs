module FSBar.Viz.Tests.GameVizThreadingTests

open System
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Viz

let minimalMapGrid =
    { WidthElmos = 8192; HeightElmos = 8192; WidthHeightmap = 129; HeightHeightmap = 129
      HeightMap = Array2D.zeroCreate 129 129; SlopeMap = Array2D.zeroCreate 129 129
      ResourceMap = Array2D.zeroCreate 129 129; LosMap = Array2D.zeroCreate 129 129
      RadarMap = Array2D.zeroCreate 129 129 }

let defaultEcon : EconomySnapshot =
    { Current = 0.0f; Income = 0.0f; Usage = 0.0f; Storage = 0.0f }

let makeGameState frameNum =
    let unit1 : TrackedUnit =
        { UnitId = 1; DefId = 100; Position = (float32 frameNum, 50.0f, 200.0f)
          Health = 1000.0f; MaxHealth = 1000.0f; IsFinished = true; IsIdle = false }
    { FrameNumber = uint32 frameNum
      TeamId = 0
      Units = Map.ofList [ 1, unit1 ]
      Enemies = Map.empty
      Metal = defaultEcon
      Energy = defaultEcon
      UnitDefs = UnitDefCache.empty
      Events = [] }

[<Fact>]
let ``concurrent onFrameWithState and config changes do not deadlock`` () =
    // Spawn two threads: one calling onFrameWithState in a loop (simulating bot thread),
    // one calling config mutations (simulating render-thread API calls).
    // Assert no deadlock within 2 seconds and no exceptions.
    let exceptions = Collections.Concurrent.ConcurrentBag<exn>()
    let cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.0))

    let botThread = Thread(fun () ->
        try
            let mutable frame = 1
            while not cts.Token.IsCancellationRequested do
                GameViz.onFrameWithState (makeGameState frame) minimalMapGrid
                frame <- frame + 1
        with ex ->
            exceptions.Add(ex))

    let configThread = Thread(fun () ->
        try
            while not cts.Token.IsCancellationRequested do
                GameViz.toggleOverlay OverlayKind.Units
                GameViz.setBaseLayer LayerKind.HeightMap
                GameViz.pan 1.0f 1.0f
                GameViz.zoom 1.1f 100.0f 100.0f
                Thread.Sleep(1)
        with ex ->
            exceptions.Add(ex))

    botThread.IsBackground <- true
    configThread.IsBackground <- true
    botThread.Start()
    configThread.Start()

    // Wait for the timeout (2 seconds)
    botThread.Join(TimeSpan.FromSeconds(3.0)) |> ignore
    configThread.Join(TimeSpan.FromSeconds(3.0)) |> ignore

    Assert.True(not botThread.IsAlive, "Bot thread should have completed (possible deadlock)")
    Assert.True(not configThread.IsAlive, "Config thread should have completed (possible deadlock)")
    Assert.Empty(exceptions)
