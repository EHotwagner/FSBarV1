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
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open SkiaSharp
open SkiaViewer
open Silk.NET.Input
open FSBar.Hub
open FSBar.Hub.App
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
    let mutable activeTab =
        // FSBAR_HUB_INITIAL_TAB lets CI screenshots skip past Setup
        // without driving a simulated tab click through the input
        // pipeline.
        match Environment.GetEnvironmentVariable("FSBAR_HUB_INITIAL_TAB") with
        | null | "" -> HubTab.Setup
        | "Viewer" -> HubTab.Viewer
        | "Configurator" -> HubTab.Configurator
        | "Encyclopedia" -> HubTab.Encyclopedia
        | "Settings" -> HubTab.Settings
        | "Grpc" -> HubTab.Grpc
        | _ -> HubTab.Setup
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

    // Live VizConfig shared by ViewerTab + ConfiguratorTab. Starts
    // from VizDefaults.defaultConfig and mutates as the user edits
    // swatches / sliders / toggles in the Configurator tab.
    let mutable vizConfig = FSBar.Viz.VizDefaults.defaultConfig
    let mutable configuratorState = ConfiguratorTab.init ()

    // Settings-tab state: eagerly evaluated when BAR + bundled proxy
    // are both resolvable. When either is missing the tab renders a
    // warning banner instead.
    // Lock guarding mutable tab-state writes from background tasks
    // (currently only the async proxy-install handler).
    let renderSceneLock = obj ()

    // Encyclopedia tab — eagerly computes the ~953 UnitEntry records
    // at startup so tab-switch is instant. Lives independently of
    // session state.
    let mutable encyclopediaState : EncyclopediaTab.EncyclopediaTabState =
        let base0 = EncyclopediaTab.init ()
        // FSBAR_HUB_ENCYCLOPEDIA_SELECT lets CI screenshots land on a
        // specific unit name without driving a simulated click.
        match Environment.GetEnvironmentVariable("FSBAR_HUB_ENCYCLOPEDIA_SELECT") with
        | null | "" -> base0
        | name ->
            match base0.Entries |> List.tryFind (fun e -> e.InternalName = name) with
            | Some e -> { base0 with Selected = Some e.DefId }
            | None -> base0

    let mutable settingsTabState : SettingsTab.SettingsTabState option =
        match barInstall, bundled with
        | Some i, Some b -> Some (SettingsTab.init i b)
        | _ ->
            Some
                { Status = None
                  Health = None
                  LastInstallResult = None
                  InstallInFlight = false }

    // --- gRPC scripting service (T063/T064) -------------------------------
    // Registered only when we have a real BAR install + SessionManager.
    // Hosted on a background Kestrel task so it doesn't block the main
    // UI thread. Bound to 127.0.0.1:<GrpcPort> with HTTP/2 cleartext.
    let grpcEndpointUrl = sprintf "http://127.0.0.1:%d" settings.GrpcPort

    let grpcService : ScriptingHub.ScriptingService option =
        match barInstall, sessions with
        | Some install, Some sm ->
            // Bundled proxy is surfaced on GetSessionStatusResponse
            // but is not required for the service to run. When the
            // resolver hasn't found a complete bundle (e.g. a fresh
            // checkout where refresh-bundled-proxy.sh hasn't been
            // run), substitute a sentinel so the gRPC contract still
            // returns a sensible "unknown" string.
            let bundleInfo =
                bundled
                |> Option.defaultValue
                    { BundledProxy.Version = "unknown"
                      BundledProxy.BundleRoot = ""
                      BundledProxy.LibSkirmishAiPath = ""
                      BundledProxy.AiInfoLuaPath = ""
                      BundledProxy.AiOptionsLuaPath = "" }
            let unitDefsThunk () =
                match sm.State with
                | SessionManager.Running rs ->
                    try rs.BarClient.GameState.UnitDefs
                    with _ -> FSBar.Client.UnitDefCache.empty
                | _ -> FSBar.Client.UnitDefCache.empty
            Some (new ScriptingHub.ScriptingService(
                    sm, bus.Sink, unitDefsThunk, install, bundleInfo,
                    settings.GrpcPort, ScriptingHub.defaults))
        | _ -> None

    let grpcHostTask : Task =
        match grpcService with
        | None -> Task.CompletedTask
        | Some svc ->
            try
                let webBuilder = WebApplication.CreateBuilder()
                // Mute ASP.NET Core's default stdout spam so the hub's
                // own stderr diagnostics stay readable.
                webBuilder.Logging.ClearProviders() |> ignore
                webBuilder.Logging.AddFilter(fun _ -> false) |> ignore
                webBuilder.Services.AddGrpc() |> ignore
                webBuilder.Services.AddSingleton<ScriptingHub.ScriptingService>(svc)
                |> ignore
                webBuilder.WebHost.ConfigureKestrel(fun opts ->
                    opts.ListenLocalhost(settings.GrpcPort, fun lo ->
                        lo.Protocols <- HttpProtocols.Http2))
                |> ignore
                let app = webBuilder.Build()
                app.MapGrpcService<ScriptingHub.ScriptingService>() |> ignore
                eprintfn "[hub] gRPC scripting service listening on %s" grpcEndpointUrl
                app.RunAsync() :> Task
            with ex ->
                eprintfn "[hub] gRPC host failed to start: %s" ex.Message
                bus.Sink.Publish(
                    HubEvents.DiagnosticsLine(
                        HubEvents.Error,
                        sprintf "gRPC host failed to start: %s" ex.Message))
                Task.CompletedTask

    // --- Paints ------------------------------------------------------------
    let contentBgColor = SKColor(0x0cuy, 0x10uy, 0x18uy, 0xffuy)
    let placeholderText = Scene.fill (SKColor(0x70uy, 0x80uy, 0x98uy, 0xffuy))
    let headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
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
            ViewerTab.render sessState vizConfig cx cy cw ch
        | HubTab.Configurator ->
            ConfiguratorTab.render configuratorState vizConfig cx cy cw ch
        | HubTab.Encyclopedia ->
            EncyclopediaTab.render encyclopediaState vizConfig.GlyphStyle cx cy cw ch
        | HubTab.Settings ->
            match settingsTabState with
            | Some st -> SettingsTab.render st barInstall bundled settings cx cy cw ch
            | None ->
                placeholderBlock
                    "Settings — BAR install, proxy, ports"
                    "BAR install detection failed — no settings surface."
        | HubTab.Grpc ->
            GrpcTab.render grpcService grpcEndpointUrl cx cy cw ch

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
                        let (cx, cy, cw, ch) = contentRect ()
                        match activeTab, setupState, barInstall with
                        | HubTab.Setup, Some st, Some install ->
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
                        | HubTab.Configurator, _, _ ->
                            let (ns, action) =
                                ConfiguratorTab.handleInput
                                    configuratorState vizConfig evt cx cy cw ch
                            configuratorState <- ns
                            // Apply ConfigChanged: the panel's handleInput
                            // returns an UpdatedConfig on every slider
                            // drag / color cycle. Here we re-dispatch
                            // the event locally so the panel sees the
                            // updated config on the next frame.
                            match action with
                            | Some (ConfiguratorTab.ConfiguratorTabAction.ConfigChanged nc) ->
                                vizConfig <- nc
                            | Some (ConfiguratorTab.ConfiguratorTabAction.SavePreset name) ->
                                let preset = FSBar.Viz.StylePreset.fromConfig name vizConfig
                                match FSBar.Viz.StylePreset.save preset with
                                | Ok path ->
                                    configuratorState <-
                                        { configuratorState with
                                            PresetNames = FSBar.Viz.StylePreset.listNames ()
                                            ActivePreset = Some name
                                            LastPresetResult = Some (Ok (sprintf "saved %s" path)) }
                                | Result.Error msg ->
                                    configuratorState <-
                                        { configuratorState with LastPresetResult = Some (Result.Error msg) }
                            | Some (ConfiguratorTab.ConfiguratorTabAction.LoadPreset name) ->
                                match FSBar.Viz.StylePreset.load name with
                                | Ok preset ->
                                    vizConfig <- FSBar.Viz.StylePreset.applyToConfig preset vizConfig
                                    configuratorState <-
                                        { configuratorState with
                                            ActivePreset = Some name
                                            LastPresetResult = Some (Ok (sprintf "loaded %s" name)) }
                                | Result.Error msg ->
                                    configuratorState <-
                                        { configuratorState with LastPresetResult = Some (Result.Error msg) }
                            | Some (ConfiguratorTab.ConfiguratorTabAction.DeletePreset name) ->
                                match FSBar.Viz.StylePreset.delete name with
                                | Ok () ->
                                    configuratorState <-
                                        { configuratorState with
                                            PresetNames = FSBar.Viz.StylePreset.listNames ()
                                            ActivePreset =
                                                if configuratorState.ActivePreset = Some name then None
                                                else configuratorState.ActivePreset
                                            LastPresetResult = Some (Ok (sprintf "deleted %s" name)) }
                                | Result.Error msg ->
                                    configuratorState <-
                                        { configuratorState with LastPresetResult = Some (Result.Error msg) }
                            | Some ConfiguratorTab.ConfiguratorTabAction.ResetDefaults ->
                                vizConfig <- FSBar.Viz.VizDefaults.defaultConfig
                                configuratorState <-
                                    { configuratorState with
                                        ActivePreset = None
                                        LastPresetResult = Some (Ok "defaults restored") }
                            | None -> ()
                        | HubTab.Encyclopedia, _, _ ->
                            match EncyclopediaTab.handleMouse encyclopediaState x y cx cy cw ch with
                            | Some (EncyclopediaTab.EncyclopediaTabAction.ToggleFaction f) ->
                                let flt =
                                    if encyclopediaState.FactionFilter.Contains f then
                                        encyclopediaState.FactionFilter.Remove f
                                    else
                                        encyclopediaState.FactionFilter.Add f
                                encyclopediaState <-
                                    { encyclopediaState with
                                        FactionFilter = flt
                                        // Reset scroll so filter changes don't strand the
                                        // view past the end of the (shorter) visible list.
                                        ListScroll = 0.0f }
                            | Some (EncyclopediaTab.EncyclopediaTabAction.SelectUnit defId) ->
                                encyclopediaState <- { encyclopediaState with Selected = Some defId }
                            | Some (EncyclopediaTab.EncyclopediaTabAction.ScrollList off) ->
                                encyclopediaState <- { encyclopediaState with ListScroll = off }
                            | None -> ()
                        | HubTab.Settings, _, _ ->
                            match settingsTabState with
                            | Some st ->
                                match SettingsTab.handleMouse st x y cx cy cw ch with
                                | Some SettingsTab.SettingsTabAction.RefreshStatus ->
                                    match barInstall, bundled with
                                    | Some i, Some b ->
                                        let status = ProxyInstaller.checkStatus i b
                                        settingsTabState <-
                                            Some (SettingsTab.applyStatus st status)
                                    | _ -> ()
                                | Some SettingsTab.SettingsTabAction.InstallProxy
                                | Some SettingsTab.SettingsTabAction.ForceReinstallProxy as action ->
                                    match barInstall, bundled with
                                    | Some i, Some b ->
                                        let force =
                                            action = Some SettingsTab.SettingsTabAction.ForceReinstallProxy
                                        settingsTabState <-
                                            Some { st with InstallInFlight = true }
                                        // Run the install on a
                                        // background task so the UI
                                        // keeps painting during the
                                        // potentially-slow file I/O +
                                        // IGL_data.lua rewrite. Fold
                                        // the result back on completion.
                                        let runTask () =
                                            try
                                                let result =
                                                    ProxyInstaller.install i b bus.Sink force
                                                lock renderSceneLock (fun () ->
                                                    settingsTabState <-
                                                        settingsTabState
                                                        |> Option.map (fun s -> SettingsTab.applyInstallResult s result))
                                            with ex ->
                                                lock renderSceneLock (fun () ->
                                                    settingsTabState <-
                                                        settingsTabState
                                                        |> Option.map (fun s ->
                                                            { s with
                                                                InstallInFlight = false
                                                                LastInstallResult =
                                                                    Some (Result.Error ex.Message) }))
                                        ignore (Task.Run(runTask))
                                    | _ -> ()
                                | None -> ()
                            | None -> ()
                        | _ -> ()
            trigger ()
        | InputEvent.KeyDown key ->
            // FR-017: W/L/C/N toggle the four unit-glyph overlays in
            // the Viewer tab. Keys are processed regardless of active
            // tab so a user on Setup can still pre-arm overlays for
            // the next session.
            let toggle (k: FSBar.Viz.OverlayKind) =
                let current = vizConfig.ActiveOverlays
                let next =
                    if current.Contains k then current.Remove k
                    else current.Add k
                vizConfig <- { vizConfig with ActiveOverlays = next }
            match key with
            | Key.W -> toggle FSBar.Viz.OverlayKind.WeaponRanges
            | Key.L -> toggle FSBar.Viz.OverlayKind.SightRanges
            | Key.C -> toggle FSBar.Viz.OverlayKind.CommandQueue
            | Key.N -> toggle FSBar.Viz.OverlayKind.FullNames
            | _ -> ()
            trigger ()
        | InputEvent.MouseScroll(delta, x, y) ->
            let (cx, cy, cw, ch) = contentRect ()
            match activeTab, setupState with
            | HubTab.Setup, Some st ->
                match SetupTab.handleScroll st delta x y cx cy cw ch with
                | Some (SetupTab.SetupTabAction.ScrollMapList off) ->
                    setupState <- Some { st with MapListScroll = off }
                | _ -> ()
            | HubTab.Configurator, _ ->
                let (ns, _action) =
                    ConfiguratorTab.handleInput
                        configuratorState vizConfig evt cx cy cw ch
                configuratorState <- ns
            | HubTab.Encyclopedia, _ ->
                match EncyclopediaTab.handleScroll encyclopediaState delta x y cx cy cw ch with
                | Some (EncyclopediaTab.EncyclopediaTabAction.ScrollList off) ->
                    encyclopediaState <- { encyclopediaState with ListScroll = off }
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

    // Install POSIX signal handlers so Ctrl-C / SIGTERM / normal
    // process exit run the session-teardown path before the process
    // dies. Belt-and-braces for the hub-crash case is tracked as a
    // follow-up (requires routing through scripts/hub-spawn-engine.sh).
    ProcessLifetime.installSignalHandlers
        (fun () ->
            sessions |> Option.iter (fun sm ->
                try sm.End() with _ -> ()))
        None

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
        // Window closed by the user. In the interactive path we
        // honour `ProcessLifetime.requestClose` — if a session is
        // running, sweep it down with SIGTERM via sweepChildEngines
        // after SessionManager.End() (below) rather than letting the
        // process exit with a dangling engine. The prompt-for-confirm
        // branch is a modal dialog that Phase-4's wizard work will
        // surface; for now we just do the safe cleanup.
        let state =
            sessions
            |> Option.map (fun sm -> sm.State)
            |> Option.defaultValue SessionManager.Idle
        match ProcessLifetime.requestClose state with
        | ProcessLifetime.CloseDecision.AllowClose -> ()
        | ProcessLifetime.CloseDecision.RequireConfirm msg ->
            eprintfn "[hub] %s (proceeding with teardown)" msg
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
    grpcService |> Option.iter (fun svc ->
        try (svc :> IDisposable).Dispose() with _ -> ())
    // Kestrel host task is left to exit on process shutdown — it has
    // no clean-shutdown hook registered and the hub is on its way
    // out.
    ignore grpcHostTask
    sessions |> Option.iter (fun sm ->
        try sm.End() with _ -> ()
        try (sm :> IDisposable).Dispose() with _ -> ())
    (bus :> IDisposable).Dispose()

    0
