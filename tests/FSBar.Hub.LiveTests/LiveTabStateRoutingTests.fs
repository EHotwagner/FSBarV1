namespace FSBar.Hub.LiveTests

// Feature 041 T039 / US4 — verify remote gRPC writes appear in
// HubStateStore (the same source the GUI tabs read from on every
// render). Acceptance scenario: SetVizAttribute("overlays.weaponRanges",
// true) → next read of HubStateStore.current().VizConfig has the
// weapon-ranges overlay enabled. Same for SelectUnit, SetHubSettings,
// SavePreset.
//
// This test does not require a launched BAR engine — it exercises the
// generated ServiceBase overrides directly against a HubStateStore +
// ScriptingService composed in-process. Tagged UiParity so it joins
// the SC-001..SC-010 matrix.

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

module private TabRoutingFixtures =

    let defaultDataDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    let requireBarInstall () : BarInstall.BarInstall =
        if not (Directory.Exists(defaultDataDir)) then
            raise (Xunit.SkipException (
                sprintf "BAR data dir not found at %s" defaultDataDir))
        let settings =
            { HubSettings.defaults with BarDataDirOverride = Some defaultDataDir }
        match BarInstall.detect settings with
        | Result.Error e ->
            raise (Xunit.SkipException (
                sprintf "BarInstall.detect failed: %s" (BarInstall.formatError e)))
        | Ok install -> install

    let makeBundled () : BundledProxy.BundledProxyInfo =
        { Version = "test"
          BundleRoot = "/tmp/stub"
          LibSkirmishAiPath = "/tmp/stub/libSkirmishAI.so"
          AiInfoLuaPath = "/tmp/stub/AIInfo.lua"
          AiOptionsLuaPath = "/tmp/stub/AIOptions.lua" }

    let makeService (install: BarInstall.BarInstall) =
        let bus = HubEvents.create ()
        let sm = SessionManager.create install bus.Sink
        let unitDefs () = FSBar.Client.UnitDefCache.empty
        let store =
            HubStateStore.create bus.Sink
                { ActiveTab = FSBar.Hub.HubTab.Setup
                  VizConfig = FSBar.Viz.VizDefaults.defaultConfig
                  Camera = ViewerCamera.defaults
                  Lobby = LobbyConfig.defaults
                  Encyclopedia = { FactionFilter = Set.empty; SelectedDefId = None }
                  PresetList = []
                  Settings = HubSettings.defaults }
        let overlays = OverlayLayerStore.create bus.Sink
        let renderer =
            HeadlessRenderer.create sm store overlays bus.Sink (fun () -> HubSettings.defaults)
        let hubLog = HubLog.create bus.Sink (fun () -> HubSettings.defaults)
        let svc =
            new ScriptingHub.ScriptingService(
                sm, bus.Sink, bus.Events, unitDefs, install, makeBundled (), 5099,
                store, renderer, overlays, hubLog, ScriptingHub.defaults)
        svc, sm, bus, store

    let nullContext : Grpc.Core.ServerCallContext = null

[<Collection("HubSession")>]
type LiveTabStateRoutingTests() =

    [<SkippableFact>]
    [<Trait("Category", "UiParity")>]
    member _.``SetVizAttribute via gRPC reflects in HubStateStore.VizConfig``() = task {
        let install = TabRoutingFixtures.requireBarInstall ()
        let svc, sm, bus, store = TabRoutingFixtures.makeService install
        try
            let req : SetVizAttributeRequest =
                { Key = "overlays.weaponRanges"
                  Value = Some { Value = VizAttributeValue.ValueCase.BoolValue true } }
            let! resp = svc.SetVizAttribute req TabRoutingFixtures.nullContext
            match resp.Result with
            | Some r when r.Outcome = SubmitOutcome.Sent -> ()
            | Some r ->
                Assert.Fail(sprintf "SetVizAttribute outcome=%A reason=%s" r.Outcome r.Reason)
            | None -> Assert.Fail("SetVizAttribute response missing Result")

            // The Configurator + Viewer tabs both read VizConfig from the
            // store on every render — verify the new value lands there.
            let cfg = (HubStateStore.current store).VizConfig
            Assert.Contains(FSBar.Viz.OverlayKind.WeaponRanges, cfg.ActiveOverlays)
        finally
            (svc :> IDisposable).Dispose()
            (sm :> IDisposable).Dispose()
            (bus :> IDisposable).Dispose()
    }

    [<SkippableFact>]
    [<Trait("Category", "UiParity")>]
    member _.``SetHubSettings via gRPC reflects in HubStateStore.Settings``() = task {
        let install = TabRoutingFixtures.requireBarInstall ()
        let svc, sm, bus, store = TabRoutingFixtures.makeService install
        try
            let originalSettings = (HubStateStore.current store).Settings
            let nextStartPaused = not originalSettings.StartPausedDefault
            let req : SetHubSettingsRequest =
                { Settings =
                    Some { GrpcPort = originalSettings.GrpcPort
                           StartPausedDefault = nextStartPaused
                           LaunchGraphicalViewerDefault = originalSettings.LaunchGraphicalViewerDefault
                           MaxRenderFrameSubscribers = originalSettings.MaxRenderFrameSubscribers
                           BarDataDirOverride = originalSettings.BarDataDirOverride |> Option.defaultValue ""
                           EngineVersionOverride = ""
                           SchemaVersion = originalSettings.SchemaVersion } }
            let! resp = svc.SetHubSettings req TabRoutingFixtures.nullContext
            match resp.Result with
            | Some r when r.Outcome = SubmitOutcome.Sent -> ()
            | Some r ->
                Assert.Fail(sprintf "SetHubSettings outcome=%A reason=%s" r.Outcome r.Reason)
            | None -> Assert.Fail("SetHubSettings response missing Result")

            // The Settings tab + Setup tab both read HubSettings from the
            // store on every render via getSettings ().
            let updated = (HubStateStore.current store).Settings
            Assert.Equal(nextStartPaused, updated.StartPausedDefault)
        finally
            (svc :> IDisposable).Dispose()
            (sm :> IDisposable).Dispose()
            (bus :> IDisposable).Dispose()
    }

    [<SkippableFact>]
    [<Trait("Category", "UiParity")>]
    member _.``SelectUnit via gRPC reflects in HubStateStore.Encyclopedia.SelectedDefId``() = task {
        let install = TabRoutingFixtures.requireBarInstall ()
        let svc, sm, bus, store = TabRoutingFixtures.makeService install
        try
            // Pick the first encyclopedia entry — exact name doesn't
            // matter; we're verifying the wiring, not the data.
            let entries = FSBar.Viz.EncyclopediaData.buildFromBarData ()
            Assert.NotEmpty(entries)
            let target = entries |> List.head
            let req : SelectUnitRequest =
                { Selector = SelectUnitRequest.SelectorCase.DefId target.DefId }
            let! resp = svc.SelectUnit req TabRoutingFixtures.nullContext
            match resp.Result with
            | Some r when r.Outcome = SubmitOutcome.Sent -> ()
            | Some r ->
                Assert.Fail(sprintf "SelectUnit outcome=%A reason=%s" r.Outcome r.Reason)
            | None -> Assert.Fail("SelectUnit response missing Result")

            // The Encyclopedia tab's render reads SelectedDefId from
            // (HubStateStore.current store).Encyclopedia per FR-019.
            let encyc = (HubStateStore.current store).Encyclopedia
            Assert.Equal(Some target.DefId, encyc.SelectedDefId)
        finally
            (svc :> IDisposable).Dispose()
            (sm :> IDisposable).Dispose()
            (bus :> IDisposable).Dispose()
    }
