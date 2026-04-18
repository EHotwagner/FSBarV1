module FSBar.Hub.Tests.HeadlessRendererTests

// Feature 040 T032 — unit tests for HeadlessRenderer (US2).
// Feature 041 T003/T004/T005 — overlay composite tests (US1).
//
// These tests exercise the off-screen render pipeline with a no-session
// SessionManager so the "placeholder frame" path is what we assert against.
// Live rendering against a real BAR engine lives in LiveRenderFrameStreamTests.

open System
open System.IO
open System.Threading
open SkiaSharp
open Xunit
open FSBar.Hub

/// Fixture: minimal tempdir BAR install — enough for BarInstall.detect
/// + SessionManager.create. We never Launch a session so the renderer
/// sees State = Idle throughout.
type private Fixture() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-render-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        p
    do
        let engDir = Path.Combine(tempDir, "engine", "recoil_2026.03.14")
        Directory.CreateDirectory(engDir) |> ignore
        let hb = Path.Combine(engDir, "spring-headless")
        File.WriteAllText(hb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            hb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)

    member _.DataDir = tempDir

    member this.Resolve() =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Ok i -> i
        | Result.Error e -> failwith (BarInstall.formatError e)

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private makeRenderer (settings: HubSettings.HubSettings) =
    let fx = new Fixture()
    let install = fx.Resolve()
    let bus = HubEvents.create ()
    let sm = SessionManager.create install bus.Sink
    let initial : HubState =
        { ActiveTab = FSBar.Hub.HubTab.Setup
          VizConfig = FSBar.Viz.VizDefaults.defaultConfig
          Camera = ViewerCamera.defaults
          Lobby = LobbyConfig.defaults
          Encyclopedia = { FactionFilter = Set.empty; SelectedDefId = None }
          PresetList = []
          Settings = settings }
    let store = HubStateStore.create bus.Sink initial
    let overlays = OverlayLayerStore.create bus.Sink
    let renderer = HeadlessRenderer.create sm store overlays bus.Sink (fun () -> settings)
    renderer, bus, sm, fx, store, overlays

let private disposeAll
        (bus: HubEvents.HubEventBus)
        (sm: SessionManager.SessionManager)
        (fx: Fixture) =
    (sm :> IDisposable).Dispose()
    (bus :> IDisposable).Dispose()
    (fx :> IDisposable).Dispose()

[<Fact>]
let ``T032a — renderOnce produces PNG bytes of the requested viewport`` () =
    let renderer, bus, sm, fx, _store, _overlays = makeRenderer HubSettings.defaults
    try
        let msg =
            HeadlessRenderer.renderOnce
                renderer FSBar.Hub.Png 512 384 100
        Assert.NotEmpty(msg.ImageBytes)
        // PNG magic: 89 50 4E 47 0D 0A 1A 0A
        Assert.Equal(0x89uy, msg.ImageBytes.[0])
        Assert.Equal(0x50uy, msg.ImageBytes.[1])
        Assert.Equal(0x4Euy, msg.ImageBytes.[2])
        Assert.Equal(0x47uy, msg.ImageBytes.[3])
        Assert.Equal(512, msg.ViewportWidth)
        Assert.Equal(384, msg.ViewportHeight)
        Assert.Equal(FSBar.Hub.Png, msg.Format)
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T032b — renderOnce returns placeholder when no session is active`` () =
    let renderer, bus, sm, fx, _store, _overlays = makeRenderer HubSettings.defaults
    try
        let msg =
            HeadlessRenderer.renderOnce
                renderer FSBar.Hub.Png 256 256 100
        Assert.True(msg.IsPlaceholder, "expected IsPlaceholder = true when no session running")
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T032c — renderOnce encodes JPEG when requested`` () =
    let renderer, bus, sm, fx, _store, _overlays = makeRenderer HubSettings.defaults
    try
        let msg =
            HeadlessRenderer.renderOnce
                renderer FSBar.Hub.Jpeg 320 240 80
        Assert.NotEmpty(msg.ImageBytes)
        // JPEG magic: FF D8 FF
        Assert.Equal(0xFFuy, msg.ImageBytes.[0])
        Assert.Equal(0xD8uy, msg.ImageBytes.[1])
        Assert.Equal(0xFFuy, msg.ImageBytes.[2])
        Assert.Equal(FSBar.Hub.Jpeg, msg.Format)
        Assert.Equal(80, msg.Quality)
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T032d — subscribe respects MaxRenderFrameSubscribers cap`` () =
    // Cap to 2 so we can exercise the reject path with low fuss.
    let tight =
        match HubSettings.updateMaxRenderFrameSubscribers HubSettings.defaults 2 with
        | Ok s -> s
        | Result.Error e -> failwith e
    let renderer, bus, sm, fx, _store, _overlays = makeRenderer tight
    try
        let req : RenderSubscriptionRequest = {
            ClientLabel = ""
            TargetHz = 5
            Format = FSBar.Hub.Png
            ViewportWidth = 64
            ViewportHeight = 64
            JpegQuality = 100
            CloseOnSessionEnd = false
            EmitNoSessionPlaceholder = true
        }
        let a = HeadlessRenderer.subscribe renderer req
        let b = HeadlessRenderer.subscribe renderer req
        let c = HeadlessRenderer.subscribe renderer req
        match a, b with
        | Subscribed _, Subscribed _ -> ()
        | _ -> Assert.Fail("first two subscriptions should succeed")
        match c with
        | SubscribeRejected reason ->
            Assert.Contains("max subscribers", reason)
        | Subscribed _ -> Assert.Fail("third subscription should be rejected at cap")
        // Clean up the subscribers.
        (match a with | Subscribed s -> s.Dispose() | _ -> ())
        (match b with | Subscribed s -> s.Dispose() | _ -> ())
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T032e — subscriber count goes up and down with subscribe / Dispose`` () =
    let renderer, bus, sm, fx, _store, _overlays = makeRenderer HubSettings.defaults
    try
        Assert.Equal(0, HeadlessRenderer.subscriberCount renderer)
        let req : RenderSubscriptionRequest = {
            ClientLabel = "probe"
            TargetHz = 5
            Format = FSBar.Hub.Png
            ViewportWidth = 64
            ViewportHeight = 64
            JpegQuality = 100
            CloseOnSessionEnd = false
            EmitNoSessionPlaceholder = true
        }
        match HeadlessRenderer.subscribe renderer req with
        | Subscribed sub ->
            Assert.Equal(1, HeadlessRenderer.subscriberCount renderer)
            sub.Dispose()
            // Dispose is best-effort async — poll briefly.
            let sw = System.Diagnostics.Stopwatch.StartNew()
            while HeadlessRenderer.subscriberCount renderer > 0 && sw.ElapsedMilliseconds < 1000L do
                Thread.Sleep(20)
            Assert.Equal(0, HeadlessRenderer.subscriberCount renderer)
        | SubscribeRejected r ->
            Assert.Fail(sprintf "subscribe unexpectedly rejected: %s" r)
    finally
        disposeAll bus sm fx

// -----------------------------------------------------------------------------
// Feature 041 US1 — overlay composite tests (T003 / T004 / T005)
// -----------------------------------------------------------------------------

let private decodePixel (pngBytes: byte[]) (x: int) (y: int) : SKColor =
    use data = SKData.CreateCopy(pngBytes)
    use bmp = SKBitmap.Decode(data)
    bmp.GetPixel(x, y)

let private colorClose (a: SKColor) (b: SKColor) (tol: int) : bool =
    let d (x: byte) (y: byte) = abs (int x - int y)
    d a.Red b.Red <= tol && d a.Green b.Green <= tol && d a.Blue b.Blue <= tol

let private brightStyle (rgba: uint32) : OverlayStyle =
    { StrokeColorRgba = rgba
      StrokeWidth = 2.0f
      FillColorRgba = Some rgba
      Opacity = 1.0f
      Dash = None }

let private layerWith (name: string) (z: int) (prims: OverlayPrimitive list) : OverlayLayer =
    { Name = name
      ZHint = z
      UploadedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
      Primitives = prims }

[<Fact>]
let ``T003 — World-space circle is drawn at the camera-transformed pixel`` () =
    let renderer, bus, sm, fx, _store, overlays = makeRenderer HubSettings.defaults
    try
        // Camera defaults: Scale=1, Origin=(0,0), so pixel = world.
        // Bright red filled circle at world (100, 80), radius 12.
        let red = 0xFF0000FFu
        let circle =
            Circle(
                center = { X = 100.0f; Y = 80.0f },
                radius = 12.0f,
                style = brightStyle red,
                space = World)
        let clientId = Guid.NewGuid()
        let layer = layerWith "us1-world" 0 [ circle ]
        match OverlayLayerStore.putLayer overlays clientId layer with
        | Ok () -> ()
        | Error e -> Assert.Fail(sprintf "putLayer failed: %A" e)

        let msg = HeadlessRenderer.renderOnce renderer FSBar.Hub.Png 256 192 100
        let pixel = decodePixel msg.ImageBytes 100 80
        let target = SKColor(0xFFuy, 0x00uy, 0x00uy, 0xFFuy)
        Assert.True(
            colorClose pixel target 8,
            sprintf "pixel at (100,80) was R=%d G=%d B=%d, expected ~red"
                (int pixel.Red) (int pixel.Green) (int pixel.Blue))
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T004 — Screen-space primitive is unchanged when the camera moves`` () =
    let renderer, bus, sm, fx, store, overlays = makeRenderer HubSettings.defaults
    try
        // Bright green filled circle anchored at screen pixel (60, 40).
        let green = 0x00FF00FFu
        let circle =
            Circle(
                center = { X = 60.0f; Y = 40.0f },
                radius = 10.0f,
                style = brightStyle green,
                space = Screen)
        let clientId = Guid.NewGuid()
        let layer = layerWith "us1-screen" 0 [ circle ]
        match OverlayLayerStore.putLayer overlays clientId layer with
        | Ok () -> ()
        | Error e -> Assert.Fail(sprintf "putLayer failed: %A" e)

        let frameA = HeadlessRenderer.renderOnce renderer FSBar.Hub.Png 256 192 100
        let pixelA = decodePixel frameA.ImageBytes 60 40

        // Pan camera to a different origin.
        let camB =
            { ViewerCamera.defaults with
                OriginX = 75.0f
                OriginY = -42.0f
                AutoFit = false }
        match HubStateStore.setCamera store camB with
        | Sent -> ()
        | Rejected r -> Assert.Fail(sprintf "setCamera rejected: %s" r)

        let frameB = HeadlessRenderer.renderOnce renderer FSBar.Hub.Png 256 192 100
        let pixelB = decodePixel frameB.ImageBytes 60 40

        let target = SKColor(0x00uy, 0xFFuy, 0x00uy, 0xFFuy)
        Assert.True(
            colorClose pixelA target 8,
            sprintf "frame A (60,40) was R=%d G=%d B=%d, expected ~green"
                (int pixelA.Red) (int pixelA.Green) (int pixelA.Blue))
        Assert.True(
            colorClose pixelB target 8,
            sprintf "frame B (60,40) was R=%d G=%d B=%d, expected ~green (Screen-space anchor must not move)"
                (int pixelB.Red) (int pixelB.Green) (int pixelB.Blue))
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T005 — Overlapping primitives composite in (ownerId, zHint, uploadedAt) order`` () =
    let renderer, bus, sm, fx, _store, overlays = makeRenderer HubSettings.defaults
    try
        let clientLow = Guid.NewGuid()
        let clientHigh = Guid.NewGuid()
        // Same screen pixel; different fill colors.
        let red = 0xFF0000FFu
        let blue = 0x0000FFFFu
        let mkCircle rgba =
            Circle(
                center = { X = 80.0f; Y = 60.0f },
                radius = 16.0f,
                style = brightStyle rgba,
                space = Screen)
        let unixNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        let layerLow =
            { Name = "us1-low"
              ZHint = 0
              UploadedAtUnixMs = unixNow
              Primitives = [ mkCircle red ] }
        let layerHigh =
            { Name = "us1-high"
              ZHint = 0
              UploadedAtUnixMs = unixNow + 10L
              Primitives = [ mkCircle blue ] }
        match OverlayLayerStore.putLayer overlays clientLow layerLow with
        | Ok () -> () | Error e -> Assert.Fail(sprintf "putLayer low: %A" e)
        match OverlayLayerStore.putLayer overlays clientHigh layerHigh with
        | Ok () -> () | Error e -> Assert.Fail(sprintf "putLayer high: %A" e)

        // Snapshot determines who is on top: the LAST entry in
        // (ownerId, zHint, uploadedAt) order draws last → its color wins.
        let snap = OverlayLayerStore.snapshot overlays
        let (lastOwner, _) = snap.Entries.[snap.Entries.Length - 1]
        let expectedRgba =
            if lastOwner = clientLow then red else blue
        let expected =
            SKColor(
                byte ((expectedRgba >>> 24) &&& 0xFFu),
                byte ((expectedRgba >>> 16) &&& 0xFFu),
                byte ((expectedRgba >>> 8) &&& 0xFFu),
                0xFFuy)

        let msg = HeadlessRenderer.renderOnce renderer FSBar.Hub.Png 256 192 100
        let pixel = decodePixel msg.ImageBytes 80 60
        Assert.True(
            colorClose pixel expected 8,
            sprintf "pixel at (80,60) was R=%d G=%d B=%d, expected the higher-sorted layer's color (R=%d G=%d B=%d)"
                (int pixel.Red) (int pixel.Green) (int pixel.Blue)
                (int expected.Red) (int expected.Green) (int expected.Blue))
    finally
        disposeAll bus sm fx
