module FSBar.Hub.Tests.HeadlessRendererTests

// Feature 040 T032 — unit tests for HeadlessRenderer (US2).
//
// These tests exercise the off-screen render pipeline with a no-session
// SessionManager so the "placeholder frame" path is what we assert against.
// Live rendering against a real BAR engine lives in LiveRenderFrameStreamTests.

open System
open System.IO
open System.Threading
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
    let renderer = HeadlessRenderer.create sm store overlays (fun () -> settings)
    renderer, bus, sm, fx

let private disposeAll
        (bus: HubEvents.HubEventBus)
        (sm: SessionManager.SessionManager)
        (fx: Fixture) =
    (sm :> IDisposable).Dispose()
    (bus :> IDisposable).Dispose()
    (fx :> IDisposable).Dispose()

[<Fact>]
let ``T032a — renderOnce produces PNG bytes of the requested viewport`` () =
    let renderer, bus, sm, fx = makeRenderer HubSettings.defaults
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
    let renderer, bus, sm, fx = makeRenderer HubSettings.defaults
    try
        let msg =
            HeadlessRenderer.renderOnce
                renderer FSBar.Hub.Png 256 256 100
        Assert.True(msg.IsPlaceholder, "expected IsPlaceholder = true when no session running")
    finally
        disposeAll bus sm fx

[<Fact>]
let ``T032c — renderOnce encodes JPEG when requested`` () =
    let renderer, bus, sm, fx = makeRenderer HubSettings.defaults
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
    let renderer, bus, sm, fx = makeRenderer tight
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
    let renderer, bus, sm, fx = makeRenderer HubSettings.defaults
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
