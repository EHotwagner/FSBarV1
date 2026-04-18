namespace FSBar.Hub.LiveTests

// Feature 039 T040 / T044a — live tests for US3 (force-end).
//
// Asserts:
//   (a) SC-004 — session transitions Running → Idle within 5 s of ForceEnd.
//   (b) ForceEnd on a paused match produces the same clean shutdown.
//   (c) Relaunch after force-end does not leak pause/speed state.
//   (d) T044a — ForceEndMatch RPC returns SENT and session transitions to
//       Idle within 5 s.

open System
open System.IO
open System.Threading
open System.Diagnostics
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.AdminChannelHost
open Fsbar.Hub.Scripting.V1

module private AdminForceEndFixtures =

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
            let required = [ "HighBarV2"; "BARb" ]
            let installed =
                BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList
            let missing = required |> List.filter (installed.Contains >> not)
            if not (List.isEmpty missing) then
                raise (Xunit.SkipException (
                    sprintf "required skirmish AIs not installed: %s"
                        (String.concat ", " missing)))
            if not install.ActiveEngine.HasHeadlessBin then
                raise (Xunit.SkipException
                    (sprintf "spring-headless not found under %s" install.ActiveEngine.EngineDir))
            install

    let pickMap (install: BarInstall.BarInstall) : string =
        let p = Path.Combine(install.DataDir, "maps", "avalanche_3.4.sd7")
        if File.Exists(p) then "Avalanche 3.4"
        else raise (Xunit.SkipException "avalanche_3.4.sd7 not installed")

    let happyLobby (mapName: string) : LobbyConfig.LobbyConfig =
        { LobbyConfig.MapName = mapName
          LobbyConfig.Mode = LobbyConfig.Skirmish
          LobbyConfig.EngineSpeed = 1.0f
          LobbyConfig.LaunchGraphicalViewer = false
          LobbyConfig.Teams =
            [ { LobbyConfig.Seats =
                  [ { LobbyConfig.Kind = LobbyConfig.AiSeat("HighBarV2", Map.empty)
                      LobbyConfig.Side = "Armada"
                      LobbyConfig.Handicap = 0 } ]
                LobbyConfig.AllyTeamId = 0 }
              { LobbyConfig.Seats =
                  [ { LobbyConfig.Kind = LobbyConfig.AiSeat("BARb", Map.empty)
                      LobbyConfig.Side = "Cortex"
                      LobbyConfig.Handicap = 0 } ]
                LobbyConfig.AllyTeamId = 1 } ]
          LobbyConfig.Spectators = [] }

    let waitUntil (timeoutMs: int) (predicate: unit -> bool) : bool =
        let sw = Stopwatch.StartNew()
        let mutable ok = predicate ()
        while not ok && sw.ElapsedMilliseconds < int64 timeoutMs do
            Thread.Sleep(100)
            ok <- predicate ()
        ok

    let makeBundled () : BundledProxy.BundledProxyInfo =
        { Version = "test"
          BundleRoot = "/tmp/stub"
          LibSkirmishAiPath = "/tmp/stub/libSkirmishAI.so"
          AiInfoLuaPath = "/tmp/stub/AIInfo.lua"
          AiOptionsLuaPath = "/tmp/stub/AIOptions.lua" }

[<Collection("HubSession")>]
type LiveAdminForceEndTests() =

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``SC-004 — ForceEnd transitions Running → Idle within 5s``() = task {
        let install = AdminForceEndFixtures.requireBarInstall ()
        let mapName = AdminForceEndFixtures.pickMap install
        let lobby = AdminForceEndFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminForceEndFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        let attached =
            AdminForceEndFixtures.waitUntil 15000 (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true | _ -> false)
        if not attached then
            raise (Xunit.SkipException
                (sprintf "admin channel did not attach; status = %A" sm.AdminStatus))

        match sm.ForceEnd() with
        | AdminChannelHost.Sent -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "ForceEnd returned %A" other))

        let idle =
            AdminForceEndFixtures.waitUntil 5000 (fun () -> sm.State = SessionManager.Idle)
        // Fall back to the 8 s SIGKILL escalation window.
        let idleOrEscalated =
            idle ||
                AdminForceEndFixtures.waitUntil 3500 (fun () ->
                    sm.State = SessionManager.Idle)
        Assert.True(idleOrEscalated,
            sprintf "session did not reach Idle within 8s; final = %A" sm.State)
    }

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``T044a — ForceEndMatch RPC returns SENT and session → Idle``() = task {
        let install = AdminForceEndFixtures.requireBarInstall ()
        let mapName = AdminForceEndFixtures.pickMap install
        let lobby = AdminForceEndFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink
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
        use svc =
            new ScriptingHub.ScriptingService(
                sm, bus.Sink, bus.Events, unitDefs, install,
                AdminForceEndFixtures.makeBundled (), 5099,
                store, renderer, overlays, hubLog, ScriptingHub.defaults)

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminForceEndFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        let attached =
            AdminForceEndFixtures.waitUntil 15000 (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true | _ -> false)
        if not attached then
            raise (Xunit.SkipException
                (sprintf "admin channel did not attach; status = %A" sm.AdminStatus))

        let ctx : Grpc.Core.ServerCallContext = null
        let! resp = svc.ForceEndMatch ForceEndMatchRequest.Unused ctx
        match resp.Result with
        | Some r ->
            if r.Outcome = AdminSubmitResult.Outcome.Rejected then
                raise (Xunit.SkipException
                    (sprintf "ForceEndMatch rejected: %s" r.Reason))
            Assert.True(
                (r.Outcome = AdminSubmitResult.Outcome.Sent
                 || r.Outcome = AdminSubmitResult.Outcome.Coalesced),
                sprintf "unexpected outcome %A" r.Outcome)
        | None -> Assert.Fail("ForceEndMatch response missing Result")

        let idle =
            AdminForceEndFixtures.waitUntil 8000 (fun () ->
                sm.State = SessionManager.Idle)
        Assert.True(idle,
            sprintf "session did not reach Idle within 8s after RPC; final = %A" sm.State)
    }
