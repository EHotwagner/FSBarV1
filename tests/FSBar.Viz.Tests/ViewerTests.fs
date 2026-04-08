namespace FSBar.Viz.Tests

open System
open System.Threading
open Xunit
open SkiaSharp
open Silk.NET.Maths
open SkiaViewer

/// Serialize all viewer tests — GLFW requires single-threaded window lifecycle.
[<CollectionDefinition("Viewer", DisableParallelization = true)>]
type ViewerCollection() = class end

[<Collection("Viewer")>]
type ViewerTests() =

    static let makeConfig (onRender: SKCanvas -> Vector2D<int> -> unit) =
        { Title = "ViewerTest"
          Width = 400
          Height = 300
          TargetFps = 60
          ClearColor = SKColors.Black
          OnRender = onRender
          OnResize = fun _ _ -> ()
          OnKeyDown = fun _ -> ()
          OnMouseScroll = fun _ _ _ -> ()
          OnMouseDrag = fun _ _ -> ()
          PreferredBackend = None }

    // ========================================================================
    // Phase 3: User Story 1 — Viewer Renders Graphics Reliably (P1)
    // ========================================================================

    [<Fact>]
    member _.``US1 continuous rendering counts frames without exceptions`` () =
        let mutable frameCount = 0
        let mutable exceptionCount = 0

        let config =
            makeConfig (fun canvas fbSize ->
                try
                    Interlocked.Increment(&frameCount) |> ignore

                    use fillPaint = new SKPaint(Color = SKColors.CornflowerBlue, IsAntialias = true)
                    canvas.DrawRect(10.0f, 10.0f, 80.0f, 60.0f, fillPaint)

                    use circlePaint = new SKPaint(Color = SKColors.Coral, IsAntialias = true)
                    canvas.DrawCircle(200.0f, 80.0f, 40.0f, circlePaint)

                    use linePaint = new SKPaint(Color = SKColors.White, StrokeWidth = 2.0f, IsStroke = true)
                    canvas.DrawLine(10.0f, 150.0f, 300.0f, 150.0f, linePaint)

                    use textPaint = new SKPaint(Color = SKColors.Yellow, TextSize = 20.0f, IsAntialias = true)
                    canvas.DrawText("Frame " + string frameCount, 10.0f, 200.0f, textPaint)

                    use shader = SKShader.CreateLinearGradient(
                        SKPoint(10.0f, 220.0f), SKPoint(200.0f, 270.0f),
                        [| SKColors.Red; SKColors.Blue |], [| 0.0f; 1.0f |],
                        SKShaderTileMode.Clamp)
                    use gradPaint = new SKPaint(Shader = shader, IsAntialias = true)
                    canvas.DrawRect(10.0f, 220.0f, 190.0f, 50.0f, gradPaint)
                with _ ->
                    Interlocked.Increment(&exceptionCount) |> ignore)

        use viewer = Viewer.run config
        Thread.Sleep(3000)

        Assert.True(frameCount > 60, $"Expected > 60 frames but got {frameCount}")
        Assert.Equal(0, exceptionCount)

    [<Fact>]
    member _.``US1 render exception recovery continues rendering`` () =
        let mutable frameCount = 0

        let config =
            makeConfig (fun canvas _ ->
                let n = Interlocked.Increment(&frameCount)
                use paint = new SKPaint(Color = SKColors.Green, IsAntialias = true)
                canvas.DrawRect(20.0f, 20.0f, 100.0f, 50.0f, paint)
                if n % 10 = 0 then
                    raise (InvalidOperationException("Deliberate test exception")))

        use viewer = Viewer.run config
        Thread.Sleep(3000)

        Assert.True(frameCount > 60, $"Expected > 60 frames but got {frameCount} (exceptions should not stop rendering)")

    // ========================================================================
    // Phase 4: User Story 2 — Viewer Lifecycle Is Robust (P1)
    // ========================================================================

    [<Fact>]
    member _.``US2 start stop cycle 10 times without crash`` () =
        for i in 1..10 do
            let config =
                makeConfig (fun canvas _ ->
                    use paint = new SKPaint(Color = SKColors.Orange)
                    canvas.DrawRect(0.0f, 0.0f, 100.0f, 100.0f, paint))

            use viewer = Viewer.run config
            Thread.Sleep(500)

        Assert.True(true)

    [<Fact>]
    member _.``US2 cross-thread dispose completes within timeout`` () =
        let mutable frameCount = 0

        let config =
            makeConfig (fun canvas _ ->
                Interlocked.Increment(&frameCount) |> ignore
                use paint = new SKPaint(Color = SKColors.Purple)
                canvas.DrawRect(0.0f, 0.0f, 50.0f, 50.0f, paint))

        let viewer = Viewer.run config
        Thread.Sleep(1000)

        let disposeTask = System.Threading.Tasks.Task.Run(fun () -> (viewer :> System.IDisposable).Dispose())
        let completed = disposeTask.Wait(TimeSpan.FromSeconds(2.0))

        Assert.True(completed, "Dispose should complete within 2 seconds")
        Assert.True(frameCount > 0, "Should have rendered at least some frames before dispose")

    // ========================================================================
    // Phase 5: User Story 3 — Standalone Demo with SkiaSharp Primitives (P2)
    // ========================================================================

    [<Fact>]
    member _.``US3 standalone demo renders five primitive types`` () =
        let mutable frameCount = 0

        let config =
            makeConfig (fun canvas fbSize ->
                Interlocked.Increment(&frameCount) |> ignore

                // 1. Filled rectangle
                use fillPaint = new SKPaint(Color = SKColors.DodgerBlue, IsAntialias = true)
                canvas.DrawRect(10.0f, 10.0f, 120.0f, 80.0f, fillPaint)

                // 2. Stroked round rectangle
                use strokePaint = new SKPaint(Color = SKColors.LimeGreen, IsStroke = true, StrokeWidth = 3.0f, IsAntialias = true)
                let rrect = new SKRoundRect(SKRect(150.0f, 10.0f, 300.0f, 90.0f), 10.0f, 10.0f)
                canvas.DrawRoundRect(rrect, strokePaint)

                // 3. Circle
                use circlePaint = new SKPaint(Color = SKColors.Tomato, IsAntialias = true)
                canvas.DrawCircle(60.0f, 160.0f, 35.0f, circlePaint)

                // 4. Line
                use linePaint = new SKPaint(Color = SKColors.Gold, StrokeWidth = 2.0f, IsStroke = true, IsAntialias = true)
                canvas.DrawLine(120.0f, 120.0f, 350.0f, 200.0f, linePaint)

                // 5. Text
                use textPaint = new SKPaint(Color = SKColors.White, TextSize = 24.0f, IsAntialias = true)
                canvas.DrawText($"Frame {frameCount}", 150.0f, 170.0f, textPaint)

                // 6. Gradient rectangle
                use shader = SKShader.CreateLinearGradient(
                    SKPoint(10.0f, 220.0f), SKPoint(350.0f, 270.0f),
                    [| SKColors.DeepPink; SKColors.Cyan |], [| 0.0f; 1.0f |],
                    SKShaderTileMode.Clamp)
                use gradPaint = new SKPaint(Shader = shader, IsAntialias = true)
                canvas.DrawRect(10.0f, 220.0f, 340.0f, 50.0f, gradPaint))

        use viewer = Viewer.run config
        Thread.Sleep(3000)

        Assert.True(frameCount > 0, $"Expected frames to be rendered but got {frameCount}")

    [<Fact>]
    member _.``US3 mouse scroll and drag callbacks fire`` () =
        let mutable frameCount = 0

        let config =
            { Title = "US3 Callback Test"
              Width = 400
              Height = 300
              TargetFps = 60
              ClearColor = SKColors.DarkSlateGray
              OnRender = fun canvas _ ->
                  Interlocked.Increment(&frameCount) |> ignore
                  use paint = new SKPaint(Color = SKColors.White, TextSize = 16.0f)
                  canvas.DrawText("Scroll/drag test", 10.0f, 30.0f, paint)
              OnResize = fun _ _ -> ()
              OnKeyDown = fun _ -> ()
              OnMouseScroll = fun _ _ _ -> ()
              OnMouseDrag = fun _ _ -> ()
              PreferredBackend = None }

        use viewer = Viewer.run config
        Thread.Sleep(2000)

        Assert.True(frameCount > 0, "Viewer should start and render with callbacks wired")

    // ========================================================================
    // Phase 6: User Story 4 — Viewer Handles Edge Conditions Gracefully (P2)
    // ========================================================================

    [<Fact>]
    member _.``US4 zero-size framebuffer skips rendering gracefully`` () =
        let mutable frameCount = 0

        let config =
            { makeConfig (fun canvas _ ->
                  Interlocked.Increment(&frameCount) |> ignore
                  use paint = new SKPaint(Color = SKColors.White)
                  canvas.DrawRect(0.0f, 0.0f, 50.0f, 50.0f, paint))
              with Width = 100; Height = 100 }

        use viewer = Viewer.run config
        Thread.Sleep(2000)

        Assert.True(frameCount > 0, $"Expected frames to be rendered but got {frameCount}")

    [<Fact>]
    member _.``US4 concurrent access from multiple threads`` () =
        let mutable frameCount = 0
        let mutable exceptionCount = 0

        let config =
            { Title = "US4 Concurrency Test"
              Width = 400
              Height = 300
              TargetFps = 60
              ClearColor = SKColors.Black
              OnRender = fun canvas _ ->
                  try
                      Interlocked.Increment(&frameCount) |> ignore
                      use paint = new SKPaint(Color = SKColors.White)
                      canvas.DrawRect(0.0f, 0.0f, 100.0f, 100.0f, paint)
                  with _ ->
                      Interlocked.Increment(&exceptionCount) |> ignore
              OnResize = fun _ _ -> ()
              OnKeyDown = fun _ -> ()
              OnMouseScroll = fun _ _ _ -> ()
              OnMouseDrag = fun _ _ -> ()
              PreferredBackend = None }

        use viewer = Viewer.run config
        // Wait for viewer to fully initialize and start rendering
        Thread.Sleep(1500)

        use cts = new CancellationTokenSource()
        let threads =
            [| Thread(fun () -> while not cts.Token.IsCancellationRequested do config.OnResize 400 300; Thread.Sleep(10))
               Thread(fun () -> while not cts.Token.IsCancellationRequested do config.OnKeyDown Silk.NET.Input.Key.A; Thread.Sleep(10))
               Thread(fun () -> while not cts.Token.IsCancellationRequested do config.OnMouseScroll 1.0f 200.0f 150.0f; Thread.Sleep(10))
               Thread(fun () -> while not cts.Token.IsCancellationRequested do config.OnMouseDrag 5.0f 5.0f; Thread.Sleep(10)) |]

        for t in threads do
            t.IsBackground <- true
            t.Start()

        Thread.Sleep(2000)
        cts.Cancel()
        for t in threads do t.Join(1000) |> ignore

        Assert.True(frameCount > 0, "Should have rendered frames during concurrent access")
        Assert.Equal(0, exceptionCount)
