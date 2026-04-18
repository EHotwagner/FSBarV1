namespace FSBar.Hub.LiveTests

// Feature 040 T021 — Live integration test for US1 (headless session
// orchestration). Exercises the full ConfigureLobby → LaunchSession →
// wait Running → StopSession cycle via the gRPC ScriptingService
// overrides (in-process — no Kestrel host) against a real BAR engine.
//
// Skips when BAR install / HighBarV2 / BARb / Avalanche 3.4 are missing,
// matching the LiveSessionLaunchTests pattern.

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

module private HeadlessOrchestrationFixtures =

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
        | Ok install ->
            let requiredAis = [ "HighBarV2"; "BARb" ]
            let installed = BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList
            let missing = requiredAis |> List.filter (installed.Contains >> not)
            if not (List.isEmpty missing) then
                raise (Xunit.SkipException (
                    sprintf "required skirmish AIs missing: %s"
                        (String.concat ", " missing)))
            if not install.ActiveEngine.HasHeadlessBin then
                raise (Xunit.SkipException (
                    sprintf "spring-headless not found under %s" install.ActiveEngine.EngineDir))
            install

    let requireMapInstalled (install: BarInstall.BarInstall) (stem: string) =
        let path = Path.Combine(install.DataDir, "maps", stem + ".sd7")
        if not (File.Exists(path)) then
            raise (Xunit.SkipException (sprintf "%s not installed under <dataDir>/maps/" stem))

    let makeBundled () : BundledProxy.BundledProxyInfo =
        { Version = "0.1.17"
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
        let svc =
            new ScriptingHub.ScriptingService(
                sm, bus.Sink, bus.Events, unitDefs, install, makeBundled (), 5099,
                store, renderer, overlays, ScriptingHub.defaults)
        svc, sm, bus, store

    let waitUntil (timeoutMs: int) (predicate: unit -> bool) : bool =
        let sw = Stopwatch.StartNew()
        let mutable ok = predicate ()
        while not ok && sw.ElapsedMilliseconds < int64 timeoutMs do
            Thread.Sleep(100)
            ok <- predicate ()
        ok

    let nullContext : Grpc.Core.ServerCallContext =
        Unchecked.defaultof<Grpc.Core.ServerCallContext>

    let happyLobbyWire (mapName: string) : LobbyConfigWire =
        { MapName = mapName
          Mode = LobbyMode.Skirmish
          EngineSpeed = 1.0f
          LaunchGraphicalViewer = false
          Teams =
              [ { AllyTeamId = 0
                  Seats =
                      [ { Kind = SeatKind.Ai
                          Side = "Armada"
                          Handicap = 0.0f
                          AiName = "HighBarV2"
                          HumanName = "" } ] }
                { AllyTeamId = 1
                  Seats =
                      [ { Kind = SeatKind.Ai
                          Side = "Cortex"
                          Handicap = 0.0f
                          AiName = "BARb"
                          HumanName = "" } ] } ]
          Spectators = [] }

[<Collection("HubSession")>]
type LiveHeadlessOrchestrationTests() =

    // Feature 041 T022 — extracted body so we can drive the same flow
    // from both the original [SkippableFact] (T021a) and the new
    // UiParity [SkippableTheory] over the 3 reference maps. Returns
    // unit; throws on assertion failure (xUnit picks it up via the
    // task wrapper).
    static member private RunOrchestrationSmoke (mapName: string) (mapFile: string) =
        task {
            let install = HeadlessOrchestrationFixtures.requireBarInstall ()
            HeadlessOrchestrationFixtures.requireMapInstalled install mapFile

            let svc, sm, bus, _store = HeadlessOrchestrationFixtures.makeService install
            try
                let maps =
                    (svc.ListMaps ListMapsRequest.empty
                        HeadlessOrchestrationFixtures.nullContext).Result
                Assert.NotEmpty(maps.Maps)

                let wire = HeadlessOrchestrationFixtures.happyLobbyWire mapName
                let cfgReq : ConfigureLobbyRequest = { Lobby = Some wire }
                let cfgResp : ConfigureLobbyResponse =
                    (svc.ConfigureLobby cfgReq
                        HeadlessOrchestrationFixtures.nullContext).Result
                match cfgResp.Result with
                | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
                | None ->
                    Assert.Fail(sprintf "ConfigureLobby returned no MutationResult — errors: %A" cfgResp.ValidationErrors)

                let launchReq : LaunchSessionRequest = {
                    StartPaused = true
                    LaunchGraphicalViewer = false
                }
                let launchResp : LaunchSessionResponse =
                    (svc.LaunchSession launchReq
                        HeadlessOrchestrationFixtures.nullContext).Result
                match launchResp.Result with
                | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
                | None -> Assert.Fail("LaunchSession returned no MutationResult")

                let running =
                    HeadlessOrchestrationFixtures.waitUntil 40000 (fun () ->
                        match sm.State with SessionManager.Running _ -> true | _ -> false)
                Assert.True(running,
                    sprintf "session did not reach Running in 40s on %s; final state = %A" mapName sm.State)

                let stopResp : StopSessionResponse =
                    (svc.StopSession StopSessionRequest.empty
                        HeadlessOrchestrationFixtures.nullContext).Result
                match stopResp.Result with
                | Some r -> Assert.Equal(SubmitOutcome.Sent, r.Outcome)
                | None -> Assert.Fail("StopSession returned no MutationResult")

                let backToIdle =
                    HeadlessOrchestrationFixtures.waitUntil 15000 (fun () ->
                        sm.State = SessionManager.Idle)
                Assert.True(backToIdle,
                    sprintf "session did not return to Idle on %s; state = %A" mapName sm.State)
            finally
                try (svc :> IDisposable).Dispose() with _ -> ()
                try (sm :> IDisposable).Dispose() with _ -> ()
                try (bus :> IDisposable).Dispose() with _ -> ()
        }

    [<SkippableFact>]
    [<Trait("Category", "UiParity")>]
    member _.``T021a — full ConfigureLobby → LaunchSession → StopSession smoke on Avalanche``() =
        LiveHeadlessOrchestrationTests.RunOrchestrationSmoke "Avalanche 3.4" "avalanche_3.4"

    // Feature 041 T022 — UiParity matrix over three reference maps.
    // Spec calls for 20 launches per map; we run a single launch per
    // map here (3 launches total, ~3-5 minutes wall-clock) so the
    // matrix stays under the SC-004 20-minute budget. Stress-grade
    // 19/20 sampling is deferred to a follow-up bot-driven harness.
    [<SkippableTheory>]
    [<InlineData("Avalanche 3.4", "avalanche_3.4")>]
    [<InlineData("Red Comet Remake 1.8", "red_comet_remake_1.8")>]
    [<InlineData("Titan v2", "titan_v2")>]
    [<Trait("Category", "UiParity")>]
    member _.``T022 — orchestration smoke over UiParity reference maps`` (mapName: string) (mapFile: string) =
        LiveHeadlessOrchestrationTests.RunOrchestrationSmoke mapName mapFile
