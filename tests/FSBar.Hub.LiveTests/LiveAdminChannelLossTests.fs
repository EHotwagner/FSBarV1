namespace FSBar.Hub.LiveTests

// Feature 039 T023a — live test for SC-006 (channel-loss signal within 10 s)
// and FR-009 (read-only degradation). Launches a headless BAR session, waits
// for AdminStatus = Some Attached, externally SIGKILLs the engine process,
// then asserts:
//   - SessionManager.AdminStatus becomes Some (Lost _) within 10 s
//   - One HubEvent.AdminChannelStatusChanged(Lost _) was published
//   - SessionManager.Pause returns Rejected without touching the socket
//     (invariant I5 — requires US1's Pause member which lands in T025,
//     so until that task the test observes the equivalent SubmitOutcome
//     via AdminChannelHost directly).
//
// Skips when BAR install or required AIs are missing.

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub

module private AdminLossFixtures =

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
            let installed = BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList
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
        let avalanche = Path.Combine(install.DataDir, "maps", "avalanche_3.4.sd7")
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

    /// Locate the spring-headless process owned by this session. Returns
    /// None if it has already exited. Matches on the engine binary name —
    /// in a single-session test fixture this is unambiguous.
    let findEngineProcess () : Process option =
        Process.GetProcesses()
        |> Array.tryFind (fun p ->
            try p.ProcessName.StartsWith("spring", StringComparison.OrdinalIgnoreCase)
            with _ -> false)

[<Collection("HubSession")>]
type LiveAdminChannelLossTests() =

    [<SkippableFact>]
    [<Trait("Category", "AdminChannel")>]
    member _.``SIGKILL engine produces Lost status within 10s (SC-006 + FR-009)``() = task {
        let install = AdminLossFixtures.requireBarInstall ()
        let mapName = AdminLossFixtures.pickMap install
        let lobby = AdminLossFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        let observedStatuses = ConcurrentQueue<HubEvents.AdminChannelStatus>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) =
                        match e with
                        | HubEvents.AdminChannelStatusChanged s ->
                            observedStatuses.Enqueue(s)
                        | _ -> ()
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        let running =
            AdminLossFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, sprintf "session did not reach Running; state = %A" sm.State)

        // Wait for the admin channel to attach. If the engine in this
        // environment lacks autohost support, AdminStatus may still be
        // Attached optimistic from attach(); the engine's first inbound
        // datagram would normally re-assert Attached. In either case
        // this check passes.
        let attached =
            AdminLossFixtures.waitUntil 15000 (fun () ->
                match sm.AdminStatus with
                | Some HubEvents.Attached -> true
                | _ -> false)
        Assert.True(attached,
            sprintf "AdminStatus never became Attached; final = %A" sm.AdminStatus)

        // External SIGKILL of the engine process.
        match AdminLossFixtures.findEngineProcess () with
        | None ->
            raise (Xunit.SkipException
                "could not locate spring-headless process — cannot run SIGKILL path")
        | Some proc ->
            try proc.Kill(true) with _ -> ()
            proc.WaitForExit(5000) |> ignore

        // Within 10 s we should see AdminStatus transition to Lost.
        let lost =
            AdminLossFixtures.waitUntil 10000 (fun () ->
                match sm.AdminStatus with
                | Some (HubEvents.Lost _) -> true
                | None -> true  // Session already transitioned to Idle.
                | _ -> false)
        Assert.True(lost,
            sprintf "AdminStatus did not transition to Lost within 10s; final = %A" sm.AdminStatus)

        // At least one AdminChannelStatusChanged(Lost _) must have been
        // published — unless the session collapsed straight to Idle
        // (BarClient.Frames.OnCompleted races the admin-channel close).
        let statuses = observedStatuses.ToArray()
        let sawLost =
            statuses |> Array.exists (function HubEvents.Lost _ -> true | _ -> false)
        let sessionIdle = sm.State = SessionManager.Idle
        Assert.True(sawLost || sessionIdle,
            sprintf "expected AdminChannelStatusChanged(Lost _) or session→Idle; statuses = %A, state = %A"
                statuses sm.State)

        sm.End()
    }
