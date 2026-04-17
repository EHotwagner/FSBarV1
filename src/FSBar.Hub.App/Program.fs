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
open FSBar.Hub.App.Tabs

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

    // Setup-tab state: only meaningful when BAR was detected. When we
    // have no install the Setup pane falls back to the placeholder
    // diagnostic block.
    let mutable setupState : SetupTab.SetupTabState option =
        barInstall |> Option.map SetupTab.init

    // --- Paints ------------------------------------------------------------
    let contentBgColor = SKColor(0x0cuy, 0x10uy, 0x18uy, 0xffuy)
    let placeholderText = Scene.fill (SKColor(0x70uy, 0x80uy, 0x98uy, 0xffuy))
    let headingText = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let bannerText = Scene.fill (SKColor(0xffuy, 0xa5uy, 0x50uy, 0xffuy))

    let contentRect () =
        let x = TabBar.Width + 16.0f
        let y = 16.0f
        let w = float32 windowWidth - x - 16.0f
        let h = float32 windowHeight - StatusBar.Height - y - 8.0f
        x, y, w, h

    // --- Tab content renderer -----------------------------------------
    let renderTabContent (tab: HubTab) : Element list =
        let (cx, cy, cw, ch) = contentRect ()
        let sessState =
            match sessions with
            | Some sm -> sm.State
            | None -> SessionManager.Idle
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
        let placeholderBlock (heading: string) (subline: string) =
            [ yield Scene.text heading cx (cy + 18.0f) 18.0f headingText
              yield Scene.text subline cx (cy + 44.0f) 13.0f placeholderText
              match statusBanner with
              | Some msg ->
                  yield Scene.text (sprintf "⚠ %s" msg) cx (cy + 68.0f) 12.0f bannerText
              | None -> ()
              let baseY = cy + (match statusBanner with Some _ -> 92.0f | None -> 76.0f)
              for i in 0 .. diagLines.Length - 1 do
                  yield Scene.text diagLines.[i] cx (baseY + float32 i * 18.0f) 12.0f placeholderText ]
        match tab with
        | HubTab.Setup ->
            match setupState with
            | Some st -> SetupTab.render st cx cy cw ch
            | None ->
                placeholderBlock
                    "Setup — configure your next session"
                    "BAR install not detected — no lobby builder surface."
        | HubTab.Viewer ->
            ViewerTab.render sessState FSBar.Viz.VizDefaults.defaultConfig cx cy cw ch
        | HubTab.Encyclopedia ->
            placeholderBlock "Units — BarData encyclopedia" "Unit catalog renderer lands in T056."
        | HubTab.Configurator ->
            placeholderBlock "Style — visualisation configurator" "ConfigPanel embed lands in T058."
        | HubTab.Settings ->
            placeholderBlock "Settings — BAR install, proxy, ports" "Settings rows land in T040."
        | HubTab.Grpc ->
            placeholderBlock "gRPC — scripting clients + endpoint" "gRPC tab + scripting service register in T064/T065."

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
                    | None ->
                        // Route to the active tab.
                        match activeTab, setupState, barInstall with
                        | HubTab.Setup, Some st, Some install ->
                            let (cx, cy, cw, ch) = contentRect ()
                            match SetupTab.handleMouse st x y cx cy cw ch with
                            | Some (SetupTab.SetupTabAction.SelectMap name) ->
                                let lobby = { st.Lobby with MapName = name }
                                setupState <- Some (SetupTab.validate install { st with Lobby = lobby })
                            | Some (SetupTab.SetupTabAction.ScrollMapList off) ->
                                setupState <- Some { st with MapListScroll = off }
                            | Some SetupTab.SetupTabAction.Launch ->
                                match sessions with
                                | None ->
                                    setupState <-
                                        Some { st with LastLaunchError = Some "no BAR install detected" }
                                | Some sm ->
                                    match sm.Launch st.Lobby with
                                    | Ok () ->
                                        setupState <- Some { st with LastLaunchError = None }
                                        activeTab <- HubTab.Viewer
                                    | Result.Error msg ->
                                        setupState <- Some { st with LastLaunchError = Some msg }
                            | None -> ()
                        | _ -> ()
            trigger ()
        | InputEvent.MouseScroll(delta, x, y) ->
            match activeTab, setupState with
            | HubTab.Setup, Some st ->
                let (cx, cy, cw, ch) = contentRect ()
                match SetupTab.handleScroll st delta x y cx cy cw ch with
                | Some (SetupTab.SetupTabAction.ScrollMapList off) ->
                    setupState <- Some { st with MapListScroll = off }
                | _ -> ()
            | _ -> ()
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

    // --- Test hook: auto-launch + snapshot + exit -------------------------
    // FSBAR_HUB_AUTO_LAUNCH=1 triggers SetupTab.Launch programmatically
    // and waits for the session to reach Running before the screenshot
    // (total ~20s with engine warmup). FSBAR_HUB_SCREENSHOT_DIR tells
    // the hub where to save the PNG; without it the hub runs
    // interactively.
    let autoLaunch =
        match Environment.GetEnvironmentVariable("FSBAR_HUB_AUTO_LAUNCH") with
        | null | "" | "0" -> false
        | _ -> true
    match Environment.GetEnvironmentVariable("FSBAR_HUB_SCREENSHOT_DIR") with
    | null | "" ->
        closedSignal.Wait()
    | dir ->
        Thread.Sleep(800)
        if autoLaunch then
            match setupState, sessions with
            | Some st, Some sm when st.Errors.IsEmpty ->
                eprintfn "[hub] auto-launch: attempting Launch"
                match sm.Launch st.Lobby with
                | Ok () ->
                    activeTab <- HubTab.Viewer
                    trigger ()
                    // Wait up to 40s for Running.
                    let sw = System.Diagnostics.Stopwatch.StartNew()
                    let mutable reached = false
                    while not reached && sw.ElapsedMilliseconds < 40000L do
                        match sm.State with
                        | SessionManager.Running _ -> reached <- true
                        | _ -> Thread.Sleep(200)
                    if reached then
                        eprintfn "[hub] auto-launch: Running reached in %dms" sw.ElapsedMilliseconds
                        // Let HighBarV2 build some units before snapshot.
                        // ~15s of game time at 1.0x speed gives the
                        // commander + a metal extractor + a few solars,
                        // plus BARb's opening — enough to show units
                        // distributed across the map.
                        let extraWaitMs =
                            match Environment.GetEnvironmentVariable("FSBAR_HUB_SCREENSHOT_WAIT_MS") with
                            | null | "" -> 2000
                            | v ->
                                match System.Int32.TryParse(v) with
                                | true, n -> n
                                | _ -> 2000
                        Thread.Sleep(extraWaitMs)
                        trigger ()
                        Thread.Sleep(300)
                    else
                        eprintfn "[hub] auto-launch: timeout waiting for Running; state=%A" sm.State
                | Result.Error msg ->
                    eprintfn "[hub] auto-launch failed: %s" msg
            | _ -> eprintfn "[hub] auto-launch skipped (no session manager or lobby invalid)"
        else
            trigger ()
            Thread.Sleep(200)
        try
            match viewer.Screenshot(dir) with
            | r -> eprintfn "[hub] screenshot: %A" r
        with ex ->
            eprintfn "[hub] screenshot failed: %s" ex.Message
        // Clean session teardown before disposing the viewer.
        sessions |> Option.iter (fun sm ->
            try sm.End() with _ -> ())

    try (viewer :> IDisposable).Dispose() with _ -> ()

    // --- Teardown --------------------------------------------------------
    sessions |> Option.iter (fun sm ->
        try sm.End() with _ -> ()
        try (sm :> IDisposable).Dispose() with _ -> ())
    (bus :> IDisposable).Dispose()

    0
