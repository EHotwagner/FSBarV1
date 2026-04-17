module FSBar.Hub.App.Program

// Hub entry point (feature 035-central-gui-hub T018).
//
// Current scope (Phase 3 MVP):
//   * Load HubSettings from XDG path.
//   * Construct HubEventBus + SessionManager.
//   * Detect BarInstall; if detection fails, render an error banner
//     instead of the Setup tab (first-run wizard lands in T039/T041).
//   * Open a SkiaViewer window and run a tab router that paints the
//     chrome + a placeholder tab content area.
//   * Route left-click in the sidebar to a tab switch, in the status
//     bar to SessionManager.SetSpeed / SetPaused / End.
//   * Mirror SessionManager state + live speed/pause into the status
//     bar's render state so the user sees transitions immediately.
//
// Deferred to later phases (placeholder text is shown instead):
//   * Tab content (SetupTab, ViewerTab, Encyclopedia, Configurator,
//     SettingsTab, GrpcTab) — T029+.
//   * Kestrel + ScriptingService registration — T064.
//   * ProcessLifetime + SIGTERM handling — T017.
//   * First-run wizard — T039/T041.

open System
open System.Threading
open SkiaSharp
open SkiaViewer
open Silk.NET.Input
open FSBar.Hub
open FSBar.Hub.App.Chrome

[<EntryPoint>]
let main _argv =
    // --- Load settings + environment detection -----------------------------
    let settings = HubSettings.load ()
    let bus = HubEvents.create ()

    let barInstall, initialBanner =
        match BarInstall.detect settings with
        | Ok install -> Some install, None
        | Result.Error err -> None, Some (BarInstall.formatError err)

    let bundled = BundledProxy.resolve () |> Result.toOption

    let sessions =
        match barInstall with
        | Some install -> Some (SessionManager.create install bus.Sink)
        | None -> None

    // --- Render state (mutable; updated by input handler) ------------------
    let mutable activeTab = HubTab.Setup
    let mutable windowWidth = 1280
    let mutable windowHeight = 800
    let mutable currentSpeed = settings.LaunchGraphicalViewerDefault |> ignore; 1.0f
    let mutable currentPaused = false
    let mutable statusBanner : string option = initialBanner

    // --- Paints ------------------------------------------------------------
    let contentBgColor = SKColor(0x0cuy, 0x10uy, 0x18uy, 0xffuy)
    let placeholderText = Scene.fill (SKColor(0x70uy, 0x80uy, 0x98uy, 0xffuy))
    let headingText = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let bannerText = Scene.fill (SKColor(0xffuy, 0xa5uy, 0x50uy, 0xffuy))

    // --- Tab content placeholder -----------------------------------------
    let renderTabContent (tab: HubTab) : Element list =
        let contentX = TabBar.Width + 16.0f
        let contentY = 24.0f
        let sessState =
            match sessions with
            | Some sm -> sm.State
            | None -> SessionManager.Idle
        let heading =
            match tab with
            | HubTab.Setup -> "Setup — configure your next session"
            | HubTab.Viewer -> "Viewer — embedded live game view"
            | HubTab.Encyclopedia -> "Units — BarData encyclopedia"
            | HubTab.Configurator -> "Style — visualisation configurator"
            | HubTab.Settings -> "Settings — BAR install, proxy, ports"
            | HubTab.Grpc -> "gRPC — scripting clients + endpoint"
        let subline =
            match tab with
            | HubTab.Setup -> "Lobby builder lands in T029."
            | HubTab.Viewer ->
                match sessState with
                | SessionManager.Running _ -> "Session running. Viewer tab wiring lands in T030."
                | _ -> "No session active. Switch to Setup to configure one."
            | HubTab.Encyclopedia -> "Unit catalog renderer lands in T056."
            | HubTab.Configurator -> "ConfigPanel embed lands in T058."
            | HubTab.Settings -> "Settings rows land in T040."
            | HubTab.Grpc -> "gRPC tab + scripting service register in T064/T065."
        let diagLines =
            [ match barInstall with
              | Some i ->
                  yield sprintf "data dir    : %s" i.DataDir
                  yield sprintf "active engine: %s" i.ActiveEngine.Version
                  let aiCount = BarInstall.listSkirmishAis i.ActiveEngine |> List.length
                  yield sprintf "skirmish AIs : %d installed" aiCount
              | None -> yield "BAR install could not be detected (see banner above)."
              match bundled with
              | Some b -> yield sprintf "bundled proxy: %s" b.Version
              | None -> yield "bundled proxy: not resolved"
              yield sprintf "gRPC port    : %d (scripting service not yet started)" settings.GrpcPort ]
        [ yield Scene.text heading contentX (contentY + 18.0f) 18.0f headingText
          yield Scene.text subline contentX (contentY + 44.0f) 13.0f placeholderText
          match statusBanner with
          | Some msg ->
              yield Scene.text (sprintf "⚠ %s" msg) contentX (contentY + 68.0f) 12.0f bannerText
          | None -> ()
          let baseY = contentY + (match statusBanner with Some _ -> 92.0f | None -> 76.0f)
          for i in 0 .. diagLines.Length - 1 do
              yield Scene.text diagLines.[i] contentX (baseY + float32 i * 18.0f) 12.0f placeholderText ]

    let renderScene () : Scene =
        let sessState =
            match sessions with
            | Some sm -> sm.State
            | None -> SessionManager.Idle
        let statusState : StatusBar.StatusBarState =
            { SessionState = sessState
              Paused = currentPaused
              Speed = currentSpeed }
        let chromeElements =
            (TabBar.render activeTab windowHeight)
            @ (StatusBar.render statusState windowWidth windowHeight)
        let tabElements = renderTabContent activeTab
        Scene.create contentBgColor (tabElements @ chromeElements)

    // --- Viewer wiring ----------------------------------------------------
    let sceneEvent = Event<Scene>()
    let viewerConfig : ViewerConfig = {
        Title = sprintf "FSBar Hub — %s" (match barInstall with Some i -> i.ActiveEngine.Version | None -> "no engine")
        Width = windowWidth
        Height = windowHeight
        TargetFps = 60
        ClearColor = SKColor(0x08uy, 0x0cuy, 0x12uy, 0xffuy)
        PreferredBackend = None
    }
    let viewer, inputs = Viewer.run viewerConfig sceneEvent.Publish

    let trigger () = sceneEvent.Trigger(renderScene ())

    let handleInput (evt: InputEvent) =
        match evt with
        | InputEvent.FrameTick _ ->
            // Re-render each frame. Cheap for the hub's static chrome;
            // becomes meaningful once the Viewer tab composes live
            // scenes (T030).
            trigger ()
        | InputEvent.WindowResize(w, h) ->
            windowWidth <- w
            windowHeight <- h
            trigger ()
        | InputEvent.MouseDown(btn, x, y) ->
            if btn = MouseButton.Left then
                // TabBar owns the left 56 px.
                match TabBar.handleMouse x y with
                | Some tab -> activeTab <- tab
                | None ->
                    // StatusBar owns the bottom 24 px.
                    let sessState =
                        match sessions with
                        | Some sm -> sm.State
                        | None -> SessionManager.Idle
                    let status : StatusBar.StatusBarState =
                        { SessionState = sessState
                          Paused = currentPaused
                          Speed = currentSpeed }
                    match StatusBar.handleMouse status x y windowWidth windowHeight with
                    | Some (StatusBar.StatusBarAction.SetSpeed s) ->
                        currentSpeed <- s
                        sessions |> Option.iter (fun sm -> sm.SetSpeed s)
                    | Some StatusBar.StatusBarAction.TogglePause ->
                        currentPaused <- not currentPaused
                        sessions |> Option.iter (fun sm -> sm.SetPaused currentPaused)
                    | Some StatusBar.StatusBarAction.EndSession ->
                        sessions |> Option.iter (fun sm -> sm.End())
                    | None -> ()
            trigger ()
        | _ -> ()

    // The inputs observable completes when the user closes the
    // window; block the main thread on that signal.
    let closedSignal = new ManualResetEventSlim(false)
    use _inputsSub =
        inputs.Subscribe(
            { new IObserver<InputEvent> with
                member _.OnNext(evt) = handleInput evt
                member _.OnError(_) = closedSignal.Set()
                member _.OnCompleted() = closedSignal.Set() })

    // Forward hub diagnostics to stderr so the operator sees them
    // even before the Diagnostics tab is wired (T065).
    use _diagSub =
        bus.Events.Subscribe(
            { new IObserver<HubEvents.HubEvent> with
                member _.OnNext(e) =
                    match e with
                    | HubEvents.DiagnosticsLine(sev, msg) ->
                        eprintfn "[hub diag %A] %s" sev msg
                    | HubEvents.StateChanged tag ->
                        eprintfn "[hub state] %A" tag
                    | _ -> ()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })

    // Publish an initial frame before the first FrameTick so the
    // window shows something immediately on open.
    trigger ()

    // --- Test hook: snapshot + exit ---------------------------------------
    // When FSBAR_HUB_SCREENSHOT_DIR is set (CI / smoke test), the hub
    // takes a screenshot after a brief settle delay, then closes the
    // window programmatically. Normal interactive runs ignore this
    // branch and block until the user closes the window.
    match Environment.GetEnvironmentVariable("FSBAR_HUB_SCREENSHOT_DIR") with
    | null | "" ->
        closedSignal.Wait()
    | dir ->
        // Give the render thread a few frames to settle.
        Thread.Sleep(800)
        trigger ()
        Thread.Sleep(200)
        try
            match viewer.Screenshot(dir) with
            | r -> eprintfn "[hub] screenshot: %A" r
        with ex ->
            eprintfn "[hub] screenshot failed: %s" ex.Message

    try (viewer :> IDisposable).Dispose() with _ -> ()

    // --- Teardown --------------------------------------------------------
    sessions |> Option.iter (fun sm ->
        try sm.End() with _ -> ()
        try (sm :> IDisposable).Dispose() with _ -> ())
    (bus :> IDisposable).Dispose()

    0
