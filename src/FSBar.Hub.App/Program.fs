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
    // --- Engine-launch wrapper ---------------------------------------------
    // Route engine spawns through scripts/hub-spawn-engine.sh by default so
    // the kernel kills child engines when the hub crashes (SIGKILL / OOM /
    // SIGSEGV — anything that bypasses our SIGTERM handler). Users on
    // non-Linux systems or with unusual setups can opt out by setting the
    // env var to an empty string before launch.
    if Environment.GetEnvironmentVariable("FSBAR_ENGINE_WRAPPER") = null then
        let defaultWrapper =
            System.IO.Path.GetFullPath(
                System.IO.Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..", "scripts", "hub-spawn-engine.sh"))
        if System.IO.File.Exists(defaultWrapper) then
            Environment.SetEnvironmentVariable("FSBAR_ENGINE_WRAPPER", defaultWrapper)

    // --- Load settings + environment detection -----------------------------
    // Boot-time read of HubSettings; after `hubState` is constructed
    // every read of settings goes through `getSettings ()` and every
    // write through `HubStateStore.setSettings` (feature 041 R6 / FR-022).
    let initialSettings = HubSettings.load ()
    let bus = HubEvents.create ()

    let barInstall, initialBanner =
        match BarInstall.detect initialSettings with
        | Ok install -> Some install, None
        | Result.Error err -> None, Some (BarInstall.formatError err)

    let bundled = BundledProxy.resolve () |> Result.toOption

    let sessions =
        match barInstall with
        | Some install -> Some (SessionManager.create install bus.Sink)
        | None -> None

    // Feature 040: central state store for Hub UI. Tabs + gRPC handlers
    // both read/write through this so the local GUI and remote clients
    // never drift.
    //
    // FSBAR_HUB_INITIAL_TAB lets CI screenshots skip past Setup
    // without driving a simulated tab click through the input
    // pipeline; seed it into the store so the first paint reflects
    // the env preference.
    let initialActiveTab =
        match Environment.GetEnvironmentVariable("FSBAR_HUB_INITIAL_TAB") with
        | null | "" -> FSBar.Hub.HubTab.Setup
        | "Viewer" -> FSBar.Hub.HubTab.Viewer
        | "Configurator" | "Style" -> FSBar.Hub.HubTab.Style
        | "Encyclopedia" | "Units" -> FSBar.Hub.HubTab.Units
        | "Settings" | "Cfg" -> FSBar.Hub.HubTab.Cfg
        | "Grpc" -> FSBar.Hub.HubTab.Grpc
        | _ -> FSBar.Hub.HubTab.Setup

    let initialEncyclopedia : EncyclopediaSelection =
        match Environment.GetEnvironmentVariable("FSBAR_HUB_ENCYCLOPEDIA_SELECT") with
        | null | "" -> { FactionFilter = Set.empty; SelectedDefId = None }
        | name ->
            let entries = FSBar.Viz.EncyclopediaData.buildFromBarData ()
            match entries |> List.tryFind (fun (e: FSBar.Viz.EncyclopediaData.EncyclopediaEntry) -> e.InternalName = name) with
            | Some e -> { FactionFilter = Set.empty; SelectedDefId = Some e.DefId }
            | None -> { FactionFilter = Set.empty; SelectedDefId = None }

    let hubState =
        let initial : HubState =
            { ActiveTab = initialActiveTab
              VizConfig = FSBar.Viz.VizDefaults.defaultConfig
              Camera = ViewerCamera.defaults
              Lobby = LobbyConfig.defaults
              Encyclopedia = initialEncyclopedia
              PresetList = []
              Settings = initialSettings }
        HubStateStore.create bus.Sink initial

    // Live reader for HubSettings; tabs and gRPC handlers both go
    // through this so a remote `SetHubSettings` is reflected in the
    // GUI within one render frame (FR-021).
    let getSettings () = (HubStateStore.current hubState).Settings

    // Feature 040 US6: per-client overlay layer store. US2 renders the
    // base scene only; US6 will query this store to composite primitives.
    let overlayStore = OverlayLayerStore.create bus.Sink
    // Wire disconnect cleanup so stream clients' layers drop when their
    // gRPC channel closes (SC-010 cleanup).
    OverlayLayerStore.wireDisconnectCleanup overlayStore bus.Events

    // Feature 040 US2: off-screen render-frame pipeline. Shared by
    // GetRenderFrame (single-shot) and StreamRenderFrames (per-client
    // bounded channel fanout).
    let headlessRenderer =
        match sessions with
        | Some sm ->
            Some (HeadlessRenderer.create sm hubState overlayStore bus.Sink getSettings)
        | None -> None

    // --- Render state (mutable; updated by input handler) ------------------
    // Feature 041 R6: ActiveTab lives authoritatively in HubStateStore.
    // The chrome-side HubTab DU still distinguishes Configurator /
    // Encyclopedia / Settings whereas the store uses Style / Units / Cfg
    // for those three (wire-aligned naming). Local helpers translate
    // both directions so the Program.fs render + input plumbing stays
    // chrome-side.
    let storeTabToChrome (t: FSBar.Hub.HubTab) : HubTab =
        match t with
        | FSBar.Hub.HubTab.Setup -> HubTab.Setup
        | FSBar.Hub.HubTab.Viewer -> HubTab.Viewer
        | FSBar.Hub.HubTab.Units -> HubTab.Encyclopedia
        | FSBar.Hub.HubTab.Style -> HubTab.Configurator
        | FSBar.Hub.HubTab.Cfg -> HubTab.Settings
        | FSBar.Hub.HubTab.Grpc -> HubTab.Grpc

    let chromeTabToStore (t: HubTab) : FSBar.Hub.HubTab =
        match t with
        | HubTab.Setup -> FSBar.Hub.HubTab.Setup
        | HubTab.Viewer -> FSBar.Hub.HubTab.Viewer
        | HubTab.Encyclopedia -> FSBar.Hub.HubTab.Units
        | HubTab.Configurator -> FSBar.Hub.HubTab.Style
        | HubTab.Settings -> FSBar.Hub.HubTab.Cfg
        | HubTab.Grpc -> FSBar.Hub.HubTab.Grpc

    let getActiveTab () = storeTabToChrome (HubStateStore.current hubState).ActiveTab
    let setActiveTab (t: HubTab) =
        HubStateStore.setActiveTab hubState (chromeTabToStore t) |> ignore
    let getVizConfig () = (HubStateStore.current hubState).VizConfig

    let mutable windowWidth = 1280
    let mutable windowHeight = 800
    let mutable currentSpeed = (getSettings ()).LaunchGraphicalViewerDefault |> ignore; 1.0f
    let mutable currentPaused = false
    let mutable statusBanner : string option = initialBanner

    // Setup-tab state: only meaningful when BAR was detected. When we
    // have no install the Setup pane falls back to the placeholder
    // diagnostic block. Seed the lobby's `LaunchGraphicalViewer`
    // from `HubSettings.LaunchGraphicalViewerDefault` so persisted
    // preference (feature 038 FR-005) takes effect on every startup.
    //
    // Feature 040 T028: Lobby is now owned by HubStateStore. The tab
    // keeps a SetupTabState shell (for Maps list / scroll / errors), but
    // Lobby is pulled from the store on every render via Program.fs
    // wiring below. Tab actions mutate the store; the HubEvent.LobbyChanged
    // subscription then fires a re-render.
    let mutable setupState : SetupTab.SetupTabState option =
        barInstall
        |> Option.map (fun install ->
            let initial = SetupTab.init install
            let seeded =
                { initial with
                    Lobby =
                        { initial.Lobby with
                            LaunchGraphicalViewer = (getSettings ()).LaunchGraphicalViewerDefault } }
                |> SetupTab.validate install
            // Seed the store with the validated lobby so the gRPC
            // ConfigureLobby handler / GetHubState snapshot see the same
            // starting point as the GUI.
            HubStateStore.setLobby hubState seeded.Lobby |> ignore
            seeded)

    // Feature 041 R6: VizConfig is owned by HubStateStore. Tabs that
    // need it call `getVizConfig ()` (a single read of the cell) so the
    // local GUI and remote gRPC writes converge per FR-017/FR-018.
    let mutable configuratorState = ConfiguratorTab.init ()

    // Viewer-tab camera. `AutoFit = true` makes ViewerTab.render
    // letterbox the map into the content rect each frame; scroll-
    // wheel + left-drag flip it to manual (AutoFit=false) with the
    // user's Scale/Origin preserved across frames. `R` resets.
    //
    // Feature 040 T041: the ref is the GUI-local mirror; Program.fs
    // writes every pan/zoom mutation through HubStateStore.setCamera
    // so the HeadlessRenderer + gRPC clients see the same camera.
    let viewerViewState : FSBar.Viz.ViewState ref =
        ref { FSBar.Viz.VizDefaults.defaultViewState with AutoFit = true }
    let pushCameraToStore () =
        let vs = viewerViewState.Value
        let cam : ViewerCamera = {
            Scale = vs.Scale
            OriginX = vs.OriginX
            OriginY = vs.OriginY
            AutoFit = vs.AutoFit
        }
        HubStateStore.setCamera hubState cam |> ignore
    let mutable viewerDragStart : (float32 * float32) option = None
    let mutable viewerDragOrigin : (float32 * float32) option = None

    // Settings-tab state: eagerly evaluated when BAR + bundled proxy
    // are both resolvable. When either is missing the tab renders a
    // warning banner instead.
    // Lock guarding mutable tab-state writes from background tasks
    // (currently only the async proxy-install handler).
    let renderSceneLock = obj ()

    // Encyclopedia tab — eagerly computes the ~953 UnitEntry records
    // at startup so tab-switch is instant. Lives independently of
    // session state. FactionFilter + Selected live in HubStateStore
    // (feature 041 FR-019); the env-var seed for FSBAR_HUB_ENCYCLOPEDIA_SELECT
    // ran above when the store was constructed.
    let mutable encyclopediaState : EncyclopediaTab.EncyclopediaTabState =
        EncyclopediaTab.init ()

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
    let grpcEndpointUrl = sprintf "http://127.0.0.1:%d" (getSettings ()).GrpcPort

    let grpcService : ScriptingHub.ScriptingService option =
        match barInstall, sessions, headlessRenderer with
        | Some install, Some sm, Some hr ->
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
                    sm, bus.Sink, bus.Events, unitDefsThunk, install, bundleInfo,
                    (getSettings ()).GrpcPort, hubState, hr, overlayStore, ScriptingHub.defaults))
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
                    opts.ListenLocalhost((getSettings ()).GrpcPort, fun lo ->
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
              yield sprintf "gRPC port    : %d (scripting service not yet started)" (getSettings ()).GrpcPort ]
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
            | Some st -> SetupTab.render st (getSettings ()) cx cy cw ch
            | None ->
                placeholderBlock
                    "Setup — configure your next session"
                    "BAR install not detected — no lobby builder surface."
        | HubTab.Viewer ->
            let paused =
                sessions
                |> Option.map (fun sm -> sm.IsPaused)
                |> Option.defaultValue false
            let adminStatus =
                sessions
                |> Option.bind (fun sm -> sm.AdminStatus)
            ViewerTab.render sessState (getVizConfig ()) viewerViewState paused adminStatus cx cy cw ch
        | HubTab.Configurator ->
            ConfiguratorTab.render configuratorState hubState cx cy cw ch
        | HubTab.Encyclopedia ->
            EncyclopediaTab.render encyclopediaState hubState cx cy cw ch
        | HubTab.Settings ->
            match settingsTabState with
            | Some st -> SettingsTab.render st barInstall bundled hubState cx cy cw ch
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
            (TabBar.render (getActiveTab ()) windowHeight)
            @ (StatusBar.render statusState windowWidth windowHeight)
        let tabElements = renderTabContent (getActiveTab ())
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
                | Some tab -> setActiveTab tab
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
                        sessions |> Option.iter (fun sm ->
                            sm.SetEngineSpeed s |> ignore)
                    | Some StatusBar.StatusBarAction.TogglePause ->
                        sessions |> Option.iter (fun sm -> sm.TogglePause())
                        currentPaused <-
                            sessions
                            |> Option.map (fun sm -> sm.IsPaused)
                            |> Option.defaultValue (not currentPaused)
                    | Some StatusBar.StatusBarAction.EndSession ->
                        sessions |> Option.iter (fun sm -> sm.End())
                    | None ->
                        // Route to the active tab.
                        let (cx, cy, cw, ch) = contentRect ()
                        match getActiveTab (), setupState, barInstall with
                        | HubTab.Setup, Some st, Some install ->
                            match SetupTab.handleMouse st (getSettings ()) x y cx cy cw ch with
                            | Some (SetupTab.SetupTabAction.SelectMap name) ->
                                // Feature 040: route lobby mutation through
                                // HubStateStore. The HubEvent.LobbyChanged
                                // subscription below reconciles setupState.
                                let lobby = { st.Lobby with MapName = name }
                                HubStateStore.setLobby hubState lobby |> ignore
                                setupState <- Some (SetupTab.validate install { st with Lobby = lobby })
                            | Some (SetupTab.SetupTabAction.ScrollMapList off) ->
                                setupState <- Some { st with MapListScroll = off }
                            | Some SetupTab.SetupTabAction.Launch ->
                                match sessions with
                                | None ->
                                    setupState <-
                                        Some { st with LastLaunchError = Some "no BAR install detected" }
                                | Some sm ->
                                    match sm.Launch(st.Lobby, (getSettings ()).StartPausedDefault) with
                                    | Ok () ->
                                        setupState <- Some { st with LastLaunchError = None }
                                        setActiveTab HubTab.Viewer
                                    | Result.Error msg ->
                                        setupState <- Some { st with LastLaunchError = Some msg }
                            | Some (SetupTab.SetupTabAction.ToggleStartPaused v) ->
                                let next = { getSettings () with StartPausedDefault = v }
                                HubSettings.save next |> ignore
                                HubStateStore.setSettings hubState next |> ignore
                            | Some (SetupTab.SetupTabAction.ToggleGraphicalEngine v) ->
                                let next = { getSettings () with LaunchGraphicalViewerDefault = v }
                                HubSettings.save next |> ignore
                                HubStateStore.setSettings hubState next |> ignore
                                let updated =
                                    { st with
                                        Lobby = { st.Lobby with LaunchGraphicalViewer = v } }
                                HubStateStore.setLobby hubState updated.Lobby |> ignore
                                setupState <- Some (SetupTab.validate install updated)
                            | None -> ()
                        | HubTab.Configurator, _, _ ->
                            let (ns, action) =
                                ConfiguratorTab.handleInput
                                    configuratorState hubState evt cx cy cw ch
                            configuratorState <- ns
                            // Whole-config mutations were already applied via
                            // HubStateStore.setVizConfig inside the tab; here
                            // we only handle the file-system side-effects.
                            match action with
                            | Some (ConfiguratorTab.ConfiguratorTabAction.SavePreset name) ->
                                let preset =
                                    FSBar.Viz.StylePreset.fromConfig name (getVizConfig ())
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
                                    let next =
                                        FSBar.Viz.StylePreset.applyToConfig preset (getVizConfig ())
                                    HubStateStore.setVizConfig hubState next |> ignore
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
                                HubStateStore.setVizConfig hubState FSBar.Viz.VizDefaults.defaultConfig |> ignore
                                configuratorState <-
                                    { configuratorState with
                                        ActivePreset = None
                                        LastPresetResult = Some (Ok "defaults restored") }
                            | None -> ()
                        | HubTab.Encyclopedia, _, _ ->
                            match EncyclopediaTab.handleMouse encyclopediaState hubState x y cx cy cw ch with
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
                        | HubTab.Viewer, _, _ ->
                            // Feature 039: admin toolbar dispatch takes
                            // precedence over pan-drag. handleMouse
                            // internally routes pause / force-end / speed
                            // presets / message-submit to SessionManager.
                            let handled =
                                ViewerTab.handleMouse sessions "" x y cx cy cw ch
                            if not handled then
                                if x >= cx && x < cx + cw && y >= cy && y < cy + ch then
                                    // Left-click inside the Viewer content rect
                                    // starts a pan drag. Subsequent MouseMove
                                    // events update OriginX/Y; MouseUp ends it.
                                    viewerDragStart <- Some (x, y)
                                    viewerDragOrigin <-
                                        Some (viewerViewState.Value.OriginX, viewerViewState.Value.OriginY)
                        | _ -> ()
            trigger ()
        | InputEvent.MouseMove(x, y) ->
            match viewerDragStart, viewerDragOrigin with
            | Some (sx, sy), Some (ox, oy) when getActiveTab () = HubTab.Viewer ->
                let vs = viewerViewState.Value
                let scale = max 0.0001f vs.Scale
                let dx = (x - sx) / scale
                let dy = (y - sy) / scale
                viewerViewState.Value <-
                    { vs with OriginX = ox - dx; OriginY = oy - dy; AutoFit = false }
                pushCameraToStore ()
                trigger ()
            | _ -> ()
        | InputEvent.MouseUp(_btn, _x, _y) ->
            if viewerDragStart.IsSome then
                viewerDragStart <- None
                viewerDragOrigin <- None
        | InputEvent.KeyDown key ->
            // FR-017: W/L/C/N toggle the four unit-glyph overlays in
            // the Viewer tab. Keys are processed regardless of active
            // tab so a user on Setup can still pre-arm overlays for
            // the next session.
            //
            // Feature 040 T053: route through HubStateStore.toggleOverlay
            // so gRPC clients observe the event + the renderer picks up
            // the same VizConfig snapshot the GUI sees.
            let toggle (k: FSBar.Viz.OverlayKind) =
                HubStateStore.toggleOverlay hubState k FSBar.Hub.ToggleTarget.Toggle
                |> ignore
            match key with
            | Key.W -> toggle FSBar.Viz.OverlayKind.WeaponRanges
            | Key.L -> toggle FSBar.Viz.OverlayKind.SightRanges
            | Key.C -> toggle FSBar.Viz.OverlayKind.CommandQueue
            | Key.N -> toggle FSBar.Viz.OverlayKind.FullNames
            | Key.R when getActiveTab () = HubTab.Viewer ->
                // Reset the Viewer camera to auto-fit. ViewerTab.render
                // recomputes Scale + zero Origin on the next frame.
                viewerViewState.Value <-
                    { viewerViewState.Value with AutoFit = true }
                pushCameraToStore ()
            | _ -> ()
            trigger ()
        | InputEvent.MouseScroll(delta, x, y) ->
            let (cx, cy, cw, ch) = contentRect ()
            match getActiveTab (), setupState with
            | HubTab.Setup, Some st ->
                match SetupTab.handleScroll st delta x y cx cy cw ch with
                | Some (SetupTab.SetupTabAction.ScrollMapList off) ->
                    setupState <- Some { st with MapListScroll = off }
                | _ -> ()
            | HubTab.Configurator, _ ->
                let (ns, _action) =
                    ConfiguratorTab.handleInput
                        configuratorState hubState evt cx cy cw ch
                configuratorState <- ns
            | HubTab.Encyclopedia, _ ->
                match EncyclopediaTab.handleScroll encyclopediaState hubState delta x y cx cy cw ch with
                | Some (EncyclopediaTab.EncyclopediaTabAction.ScrollList off) ->
                    encyclopediaState <- { encyclopediaState with ListScroll = off }
                | _ -> ()
            | HubTab.Viewer, _ when x >= cx && x < cx + cw && y >= cy && y < cy + ch ->
                // Cursor-anchored zoom — keep the map point under the
                // cursor fixed while scaling. Mirrors the GameViz /
                // PreviewSession convention (1.1× per tick).
                let vs = viewerViewState.Value
                let scale = max 0.0001f vs.Scale
                let factor = if delta > 0.0f then 1.1f else 1.0f / 1.1f
                // Mouse position in content-local coordinates — the
                // ViewerTab wraps the scene in a Translate(cx,cy) group
                // before handing it to Skia, so the map's (0,0) lives
                // at (cx,cy). Undo that offset before converting to
                // map space.
                let lx = x - cx
                let ly = y - cy
                let mapX = lx / scale + vs.OriginX
                let mapY = ly / scale + vs.OriginY
                let newScale = scale * factor
                viewerViewState.Value <-
                    { vs with
                        Scale = newScale
                        OriginX = mapX - lx / newScale
                        OriginY = mapY - ly / newScale
                        AutoFit = false }
                pushCameraToStore ()
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
                    | HubEvents.LobbyChanged lobby ->
                        // Feature 040 T028: reconcile SetupTab state with
                        // authoritative store writes (e.g. gRPC ConfigureLobby).
                        match setupState, barInstall with
                        | Some st, Some install when not (obj.ReferenceEquals(st.Lobby, lobby)) ->
                            setupState <-
                                Some (SetupTab.validate install { st with Lobby = lobby })
                            trigger ()
                        | _ -> ()
                    | HubEvents.VizConfigChanged _
                    | HubEvents.VizAttributeChanged _ ->
                        // VizConfig is read directly from the store on
                        // every tab render (feature 041 FR-017); just
                        // poke a redraw so the new value is on screen.
                        trigger ()
                    | HubEvents.ActiveTabChanged _ ->
                        // ActiveTab is read directly from the store on
                        // every render via getActiveTab; just trigger
                        // a redraw so the chrome reflects the new tab.
                        trigger ()
                    | HubEvents.EncyclopediaSelectionChanged _
                    | HubEvents.HubSettingsChanged _ ->
                        trigger ()
                    | HubEvents.CameraChanged cam ->
                        // Feature 040 T041: reflect remote gRPC SetCamera
                        // writes in the GUI. Guard against the drag/zoom
                        // echo (we wrote the same values ourselves via
                        // pushCameraToStore).
                        let vs = viewerViewState.Value
                        let sameAsLocal =
                            vs.Scale = cam.Scale
                            && vs.OriginX = cam.OriginX
                            && vs.OriginY = cam.OriginY
                            && vs.AutoFit = cam.AutoFit
                        if not sameAsLocal then
                            viewerViewState.Value <-
                                { vs with
                                    Scale = cam.Scale
                                    OriginX = cam.OriginX
                                    OriginY = cam.OriginY
                                    AutoFit = cam.AutoFit }
                            trigger ()
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
                match sm.Launch(st.Lobby, (getSettings ()).StartPausedDefault) with
                | Ok () ->
                    setActiveTab HubTab.Viewer
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
