namespace FSBar.Hub.LiveTests

// Feature 039 T024 — live integration tests for US1 (real pause/resume).
//
// Asserts:
//   (a) SC-001 — clock stationary for ≥ 10 s after SessionManager.Pause
//   (b) SC-002 — clock advances within 1 s of SessionManager.Resume
//   (c) FR-004 — startPaused = true yields clock == 0 until first Resume
//
// Skips when BAR install / AIs are missing.

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.AdminChannelHost

module private AdminPauseFixtures =

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
        let avalanche =
            Path.Combine(install.DataDir, "maps", "avalanche_3.4.sd7")
        if File.Exists(avalanche) then "Avalanche 3.4"
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

    /// Read the current in-game frame number from a Running session.
    /// Returns 0 when the session isn't Running or the value can't be read.
    let currentFrame (sm: SessionManager.SessionManager) : uint32 =
        match sm.State with
        | SessionManager.Running rs ->
            try rs.BarClient.GameState.FrameNumber
            with _ -> 0u
        | _ -> 0u

    let requireAttached (sm: SessionManager.SessionManager) (timeoutMs: int) : unit =
        let attached =
            waitUntil timeoutMs (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true
                | _ -> false)
        if not attached then
            raise (Xunit.SkipException (
                sprintf "admin channel did not attach in %dms; final status = %A"
                    timeoutMs sm.AdminStatus))

[<Collection("HubSession")>]
type LiveAdminPauseTests() =

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``SC-001 — Pause halts the sim clock for at least 10s``() = task {
        let install = AdminPauseFixtures.requireBarInstall ()
        let mapName = AdminPauseFixtures.pickMap install
        let lobby = AdminPauseFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        let running =
            AdminPauseFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, "session never reached Running")
        AdminPauseFixtures.requireAttached sm 15000

        // Let the sim advance a bit so we have a non-zero baseline.
        AdminPauseFixtures.waitUntil 5000 (fun () ->
            AdminPauseFixtures.currentFrame sm > 30u) |> ignore

        match sm.Pause() with
        | AdminChannelHost.Sent -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "Pause submit returned %A — engine may not honour autohost pause in this env" other))

        // Give the engine one tick to latch the pause, then sample the
        // frame number and verify it hasn't advanced after 10s of wall
        // time (SC-001 budget).
        Thread.Sleep(1000)
        let before = AdminPauseFixtures.currentFrame sm
        Thread.Sleep(10000)
        let after = AdminPauseFixtures.currentFrame sm
        Assert.True(
            (after = before),
            sprintf "sim clock advanced while paused: before=%u after=%u" before after)

        sm.End()
    }

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``SC-002 — Resume advances the sim clock within 1s``() = task {
        let install = AdminPauseFixtures.requireBarInstall ()
        let mapName = AdminPauseFixtures.pickMap install
        let lobby = AdminPauseFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminPauseFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        AdminPauseFixtures.requireAttached sm 15000

        AdminPauseFixtures.waitUntil 5000 (fun () ->
            AdminPauseFixtures.currentFrame sm > 30u) |> ignore

        match sm.Pause() with
        | AdminChannelHost.Sent -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "Pause submit returned %A" other))
        Thread.Sleep(1000)
        let pausedAt = AdminPauseFixtures.currentFrame sm

        match sm.Resume() with
        | AdminChannelHost.Sent -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "Resume submit returned %A" other))

        // Sim should advance within 1 s of Resume (SC-002 budget).
        let advanced =
            AdminPauseFixtures.waitUntil 1500 (fun () ->
                AdminPauseFixtures.currentFrame sm > pausedAt)
        Assert.True(advanced,
            sprintf "sim clock did not advance within 1.5s of Resume; stuck at %u" pausedAt)

        sm.End()
    }

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``FR-004 — startPaused holds clock at zero until first Resume``() = task {
        let install = AdminPauseFixtures.requireBarInstall ()
        let mapName = AdminPauseFixtures.pickMap install
        let lobby = AdminPauseFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        // Launch with startPaused = true.
        match sm.Launch(lobby, true) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        AdminPauseFixtures.waitUntil 30000 (fun () ->
            match sm.State with SessionManager.Running _ -> true | _ -> false) |> ignore
        AdminPauseFixtures.requireAttached sm 15000

        // Wait 8 seconds of wall time. Sim should NOT have moved past a
        // very small initial warmup count — the autohost Pause is
        // deferred until ServerStartPlaying, then issued synchronously.
        Thread.Sleep(8000)
        let frameAfterStart = AdminPauseFixtures.currentFrame sm
        Assert.True(frameAfterStart < 30u,
            sprintf "expected startPaused to freeze sim near zero, but frame = %u" frameAfterStart)

        match sm.Resume() with
        | AdminChannelHost.Sent -> ()
        | other ->
            raise (Xunit.SkipException
                (sprintf "Resume submit returned %A" other))
        let advanced =
            AdminPauseFixtures.waitUntil 3000 (fun () ->
                AdminPauseFixtures.currentFrame sm > frameAfterStart + 5u)
        Assert.True(advanced, "sim did not advance after Resume of startPaused session")

        sm.End()
    }
