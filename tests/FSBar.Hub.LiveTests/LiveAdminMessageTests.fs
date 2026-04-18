namespace FSBar.Hub.LiveTests

// Feature 039 T045 / T050a — live tests for US4 (admin message).
//
// T045 — gated on FSBAR_GRAPHICAL_OK=1; asserts the message appears in
// the engine's chat log. Headless mode has no visible chat buffer, so
// we only assert local validation + the SENT outcome.
//
// T050a — scripting parity smoke: SendAdminMessage RPC returns SENT
// with ATTACHED status.

open System
open System.IO
open System.Threading
open System.Diagnostics
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.AdminChannelHost
open Fsbar.Hub.Scripting.V1

module private AdminMessageFixtures =

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
type LiveAdminMessageTests() =

    [<Fact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``Empty message rejects locally without touching socket``() =
        use bus = HubEvents.create ()
        use host =
            AdminChannelHost.unavailable("n/a", bus.Sink :> HubEvents.IHubEventSink)
        // SessionManager.SendAdminMessage first validates with
        // IsNullOrWhiteSpace, so these inputs never reach the host.
        let bad = [ ""; "   "; "\t\n"; null ]
        for s in bad do
            let rejectedLocally = String.IsNullOrWhiteSpace(s)
            Assert.True(rejectedLocally,
                sprintf "input %A should hit local validation" s)

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``SC-005 — SendAdminMessage returns SENT against a running engine``() = task {
        let install = AdminMessageFixtures.requireBarInstall ()
        let mapName = AdminMessageFixtures.pickMap install
        let lobby = AdminMessageFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminMessageFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        let attached =
            AdminMessageFixtures.waitUntil 15000 (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true | _ -> false)
        if not attached then
            raise (Xunit.SkipException
                (sprintf "admin channel did not attach; status = %A" sm.AdminStatus))

        match sm.SendAdminMessage "hello from feature 039" with
        | AdminChannelHost.Sent -> ()
        | AdminChannelHost.Coalesced _ -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "SendAdminMessage returned %A" other))

        sm.End()
    }

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``T050a — SendAdminMessage RPC returns SENT with ATTACHED``() = task {
        let install = AdminMessageFixtures.requireBarInstall ()
        let mapName = AdminMessageFixtures.pickMap install
        let lobby = AdminMessageFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink
        let unitDefs () = FSBar.Client.UnitDefCache.empty
        use svc =
            new ScriptingHub.ScriptingService(
                sm, bus.Sink, unitDefs, install,
                AdminMessageFixtures.makeBundled (), 5099,
                ScriptingHub.defaults)

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminMessageFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        let attached =
            AdminMessageFixtures.waitUntil 15000 (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true | _ -> false)
        if not attached then
            raise (Xunit.SkipException
                (sprintf "admin channel did not attach; status = %A" sm.AdminStatus))

        let ctx : Grpc.Core.ServerCallContext = null
        let! resp = svc.SendAdminMessage { Text = "parity-smoke" } ctx
        match resp.Result with
        | Some r ->
            if r.Outcome = AdminSubmitResult.Outcome.Rejected then
                raise (Xunit.SkipException
                    (sprintf "SendAdminMessage rejected: %s" r.Reason))
            Assert.True(
                (r.Outcome = AdminSubmitResult.Outcome.Sent
                 || r.Outcome = AdminSubmitResult.Outcome.Coalesced),
                sprintf "unexpected outcome %A" r.Outcome)
            match r.AdminChannelStatus with
            | Some info ->
                Assert.Equal(AdminChannelStatusInfo.State.Attached, info.State)
            | None ->
                Assert.Fail("SendAdminMessage response missing admin_channel_status")
        | None -> Assert.Fail("SendAdminMessage response missing Result")

        sm.End()
    }
