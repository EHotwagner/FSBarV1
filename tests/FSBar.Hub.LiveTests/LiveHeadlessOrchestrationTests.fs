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
            HeadlessRenderer.create sm store overlays (fun () -> HubSettings.defaults)
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

    [<SkippableFact>]
    [<Trait("Category", "UiParity")>]
    member _.``T021a — full ConfigureLobby → LaunchSession → StopSession smoke on Avalanche``() = task {
        let install = HeadlessOrchestrationFixtures.requireBarInstall ()
        HeadlessOrchestrationFixtures.requireMapInstalled install "avalanche_3.4"

        let svc, sm, bus, _store = HeadlessOrchestrationFixtures.makeService install
        try
            // ListMaps must return at least one entry.
            let maps =
                (svc.ListMaps ListMapsRequest.empty
                    HeadlessOrchestrationFixtures.nullContext).Result
            Assert.NotEmpty(maps.Maps)

            // ConfigureLobby with the canonical happy lobby.
            let wire = HeadlessOrchestrationFixtures.happyLobbyWire "Avalanche 3.4"
            let cfgReq : ConfigureLobbyRequest = { Lobby = Some wire }
            let cfgResp : ConfigureLobbyResponse =
                (svc.ConfigureLobby cfgReq
                    HeadlessOrchestrationFixtures.nullContext).Result
            match cfgResp.Result with
            | Some r ->
                Assert.Equal(SubmitOutcome.Sent, r.Outcome)
            | None ->
                Assert.Fail(sprintf "ConfigureLobby returned no MutationResult — errors: %A" cfgResp.ValidationErrors)

            // LaunchSession(startPaused=true, launchGraphicalViewer=false).
            let launchReq : LaunchSessionRequest = {
                StartPaused = true
                LaunchGraphicalViewer = false
            }
            let launchResp : LaunchSessionResponse =
                (svc.LaunchSession launchReq
                    HeadlessOrchestrationFixtures.nullContext).Result
            match launchResp.Result with
            | Some r ->
                Assert.Equal(SubmitOutcome.Sent, r.Outcome)
            | None ->
                Assert.Fail("LaunchSession returned no MutationResult")

            // Wait for Running within 40s (engine warmup on Avalanche is ~15-25s).
            let running =
                HeadlessOrchestrationFixtures.waitUntil 40000 (fun () ->
                    match sm.State with SessionManager.Running _ -> true | _ -> false)
            Assert.True(running,
                sprintf "session did not reach Running in 40s; final state = %A" sm.State)

            // StopSession returns to Idle.
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
                sprintf "session did not return to Idle after StopSession; state = %A" sm.State)
        finally
            try (svc :> IDisposable).Dispose() with _ -> ()
            try (sm :> IDisposable).Dispose() with _ -> ()
            try (bus :> IDisposable).Dispose() with _ -> ()
    }
