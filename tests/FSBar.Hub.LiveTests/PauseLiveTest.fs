namespace FSBar.Hub.LiveTests

// Feature 038 US2 — live `/pause` round-trip through SessionManager.
// Exercises the start-paused arming + TogglePause path against a real
// spring-headless session; skips when BAR isn't installed, matching
// LiveSessionLaunchTests.

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.LobbyConfig

module private PauseFixtures =

    let defaultDataDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    let requireBarInstall () : BarInstall.BarInstall =
        if not (Directory.Exists(defaultDataDir)) then
            raise (Xunit.SkipException (
                sprintf "BAR data dir not found at %s — live tests skipped" defaultDataDir))
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
                raise (Xunit.SkipException (
                    sprintf "spring-headless not found under %s" install.ActiveEngine.EngineDir))
            install

    let pickMap (install: BarInstall.BarInstall) : string =
        let mapsDir = Path.Combine(install.DataDir, "maps")
        let avalanche = Path.Combine(mapsDir, "avalanche_3.4.sd7")
        if File.Exists(avalanche) then "Avalanche 3.4"
        else raise (Xunit.SkipException "avalanche_3.4.sd7 not installed — live test needs a known map")

    let happyLobby (mapName: string) : LobbyConfig =
        { MapName = mapName
          Mode = Skirmish
          EngineSpeed = 1.0f
          LaunchGraphicalViewer = false
          Teams =
            [ { AllyTeamId = 0
                Seats =
                  [ { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 } ] }
              { AllyTeamId = 1
                Seats =
                  [ { Kind = AiSeat("BARb", Map.empty); Side = "Cortex"; Handicap = 0 } ] } ]
          Spectators = [] }

    let waitUntil (timeoutMs: int) (predicate: unit -> bool) : bool =
        let sw = System.Diagnostics.Stopwatch.StartNew()
        let mutable reached = false
        while not reached && sw.ElapsedMilliseconds < int64 timeoutMs do
            if predicate () then reached <- true
            else Thread.Sleep(100)
        reached

[<Collection("HubSession")>]
type PauseLiveTests() =

    [<SkippableFact>]
    [<Trait("Category", "LiveSession")>]
    member _.``Launch(startPaused=true) flips IsPaused and emits SessionPaused true (feature 038 FR-003)``() = task {
        let install = PauseFixtures.requireBarInstall ()
        let mapName = PauseFixtures.pickMap install
        let lobby = PauseFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        let observed = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observed.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        match sm.Launch(lobby, true) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        let running =
            PauseFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, "session did not reach Running state")

        Thread.Sleep(500)

        Assert.True(sm.IsPaused, "IsPaused must be true after start-paused launch")
        let events = observed.ToArray()
        Assert.Contains(events, function HubEvents.SessionPaused true -> true | _ -> false)

        sm.TogglePause()
        Assert.False(sm.IsPaused, "TogglePause should have flipped IsPaused false")

        sm.End()
    }

    [<SkippableFact>]
    [<Trait("Category", "LiveSession")>]
    member _.``Launch(startPaused=false) leaves IsPaused false; TogglePause flips (feature 038 FR-004b)``() = task {
        let install = PauseFixtures.requireBarInstall ()
        let mapName = PauseFixtures.pickMap install
        let lobby = PauseFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        let running =
            PauseFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, "session did not reach Running state")

        Assert.False(sm.IsPaused)
        sm.TogglePause()
        Assert.True(sm.IsPaused)
        sm.TogglePause()
        Assert.False(sm.IsPaused)

        sm.End()
    }
