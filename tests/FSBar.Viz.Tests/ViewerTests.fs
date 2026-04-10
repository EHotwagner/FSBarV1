module FSBar.Viz.Tests.ViewerTests

open System
open Xunit
open SkiaSharp
open SkiaViewer
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``scene observable receives scenes when triggered`` () =
    let evt = Event<Scene>()
    let config : ViewerConfig =
        { Title = "Test Viewer"
          Width = 400
          Height = 300
          TargetFps = 60
          ClearColor = SKColors.DarkSlateGray
          PreferredBackend = Some Backend.GL }
    let handle, _inputs = Viewer.run config evt.Publish
    use _ = handle
    // Emit a colorful scene with shapes
    let scene = Scene.create SKColors.DarkSlateGray [
        Scene.rect 20.0f 20.0f 200.0f 100.0f (Scene.fill SKColors.CornflowerBlue)
        Scene.circle 300.0f 150.0f 50.0f (Scene.fill SKColors.Coral)
        Scene.text "SkiaViewer Test" 20.0f 250.0f 20.0f (Scene.fill SKColors.White)
    ]
    evt.Trigger scene
    System.Threading.Thread.Sleep(2000)

[<Fact>]
let ``start stop cycles work 3 times`` () =
    for i in 1..3 do
        let evt = Event<Scene>()
        let config : ViewerConfig =
            { Title = $"Cycle {i}/3"
              Width = 320
              Height = 240
              TargetFps = 60
              ClearColor = SKColors.MidnightBlue
              PreferredBackend = Some Backend.GL }
        let handle, _inputs = Viewer.run config evt.Publish
        let scene = Scene.create SKColors.MidnightBlue [
            Scene.rect 10.0f 10.0f 300.0f 220.0f (Scene.fill (SKColor(40uy, 80uy, 120uy)))
            Scene.text $"Cycle {i}" 120.0f 120.0f 24.0f (Scene.fill SKColors.White)
        ]
        evt.Trigger scene
        System.Threading.Thread.Sleep(800)
        (handle :> IDisposable).Dispose()
        System.Threading.Thread.Sleep(200)

[<Fact>]
let ``screenshot captures to temp directory`` () =
    let evt = Event<Scene>()
    let config : ViewerConfig =
        { Title = "Screenshot Test"
          Width = 400
          Height = 300
          TargetFps = 60
          ClearColor = SKColors.Navy
          PreferredBackend = Some Backend.GL }
    let handle, _inputs = Viewer.run config evt.Publish
    use _ = handle
    let scene = Scene.create SKColors.Navy [
        Scene.rect 0.0f 0.0f 400.0f 300.0f
            (Scene.fill SKColors.Transparent
             |> Scene.withShader (
                 Shader.LinearGradient(
                     SKPoint(0.0f, 0.0f), SKPoint(400.0f, 300.0f),
                     [| SKColors.DeepSkyBlue; SKColors.Purple |],
                     [| 0.0f; 1.0f |], TileMode.Clamp)))
        Scene.circle 200.0f 150.0f 80.0f
            (Scene.fill SKColors.Transparent
             |> Scene.withShader (
                 Shader.RadialGradient(
                     SKPoint(200.0f, 150.0f), 80.0f,
                     [| SKColors.Gold; SKColor(255uy, 215uy, 0uy, 0uy) |],
                     [| 0.0f; 1.0f |], TileMode.Clamp)))
        Scene.text "Screenshot" 140.0f 160.0f 18.0f (Scene.fill SKColors.White)
    ]
    evt.Trigger scene
    System.Threading.Thread.Sleep(1000)
    let tmpDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "viz-test-screenshots")
    System.IO.Directory.CreateDirectory(tmpDir) |> ignore
    let result = handle.Screenshot(tmpDir)
    match result with
    | Ok path ->
        Assert.True(System.IO.File.Exists(path), $"Screenshot file should exist at {path}")
        let fi = System.IO.FileInfo(path)
        Assert.True(fi.Length > 100L, $"Screenshot should have real content, got {fi.Length} bytes")
    | Error msg -> Assert.Fail($"Screenshot failed: {msg}")
