namespace FSBar.Hub.LiveTests

// Live integration test for feature 035-central-gui-hub T025 — launch a
// real BAR session through FSBar.Hub.SessionManager, observe frames,
// then tear down cleanly. Requires the BAR engine + HighBarV2 + BARb
// installed on the dev machine (per tests/engine-version.json).
//
// Test skips (rather than fails) when those prerequisites are missing.

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.LobbyConfig

module private LiveFixtures =

    /// Default BAR data dir per CLAUDE.md §Engine paths.
    let defaultDataDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".local/state/Beyond All Reason")

    /// Guard: skip (as xUnit SkipException) when BAR isn't installed
    /// here. Matches the tests/README.md convention used by
    /// FSBar.LiveTests.
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
            let requiredAis = [ "HighBarV2"; "BARb" ]
            let installed = BarInstall.listSkirmishAis install.ActiveEngine |> Set.ofList
            let missing = requiredAis |> List.filter (installed.Contains >> not)
            if not (List.isEmpty missing) then
                raise (Xunit.SkipException (
                    sprintf "required skirmish AIs not installed: %s"
                        (String.concat ", " missing)))
            if not install.ActiveEngine.HasHeadlessBin then
                raise (Xunit.SkipException (
                    sprintf "spring-headless not found under %s" install.ActiveEngine.EngineDir))
            install

    /// Pick a map that exists under <dataDir>/maps/. Prefer Avalanche 3.4
    /// (the canonical US1 demo map per quickstart.md); fall back to
    /// anything installed.
    let pickMap (install: BarInstall.BarInstall) : string =
        let mapsDir = Path.Combine(install.DataDir, "maps")
        // Archive filename is lowercased-underscored; the engine
        // indexes the archive by the display name stored in mapinfo.lua.
        let avalanche = Path.Combine(mapsDir, "avalanche_3.4.sd7")
        if File.Exists(avalanche) then "Avalanche 3.4"
        else
            raise (Xunit.SkipException (
                sprintf "avalanche_3.4.sd7 not installed under %s — live test needs a known map with a predictable display name" mapsDir))

    let happyLobby (mapName: string) : LobbyConfig =
        { MapName = mapName
          Mode = Skirmish
          EngineSpeed = 1.0f
          LaunchGraphicalViewer = false
          Teams =
            [ { Seats =
                  [ { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 } ]
                AllyTeamId = 0 }
              { Seats =
                  [ { Kind = AiSeat("BARb", Map.empty); Side = "Cortex"; Handicap = 0 } ]
                AllyTeamId = 1 } ]
          Spectators = [] }

    let waitUntil (timeoutMs: int) (predicate: unit -> bool) : bool =
        let sw = Stopwatch.StartNew()
        let mutable ok = predicate ()
        while not ok && sw.ElapsedMilliseconds < int64 timeoutMs do
            Thread.Sleep(100)
            ok <- predicate ()
        ok

[<Collection("HubSession")>]
type LiveSessionLaunchTests() =

    [<SkippableFact>]
    [<Trait("Category", "LiveSession")>]
    member _.``Launch reaches Running within 30s and produces frames``() = task {
        let install = LiveFixtures.requireBarInstall ()
        let mapName = LiveFixtures.pickMap install
        let lobby = LiveFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        let observedStates = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observedStates.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        let frameCount = ref 0
        use _frameSub =
            sm.Frames.Subscribe(
                { new IObserver<GameFrame> with
                    member _.OnNext(_) = Interlocked.Increment(frameCount) |> ignore
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        match sm.Launch(lobby, false) with
        | Result.Error msg ->
            Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        // Reach Running within 30 s (SC-002).
        let running =
            LiveFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running,
            sprintf "session did not reach Running in 30s; final state = %A" sm.State)

        // At least one frame within another 30 s (engine warmup).
        let sawFrame = LiveFixtures.waitUntil 30000 (fun () -> !frameCount > 0)
        Assert.True(sawFrame, "no frames received after Running transition")

        // Confirm both StateChanged Starting and StateChanged Running fired.
        let tags =
            observedStates.ToArray()
            |> Array.choose (function
                | HubEvents.StateChanged t -> Some t
                | _ -> None)
        Assert.Contains(HubEvents.Starting, tags)
        Assert.Contains(HubEvents.Running, tags)

        // Clean teardown.
        sm.End()
        let backToIdle =
            LiveFixtures.waitUntil 10000 (fun () -> sm.State = SessionManager.Idle)
        Assert.True(backToIdle,
            sprintf "session did not return to Idle after End(); state = %A" sm.State)
    }

    [<SkippableFact>]
    [<Trait("Category", "LiveSession")>]
    member _.``SetSpeed and SetPaused emit HubEvents while session is running``() = task {
        let install = LiveFixtures.requireBarInstall ()
        let mapName = LiveFixtures.pickMap install
        let lobby = LiveFixtures.happyLobby mapName

        use bus = HubEvents.create ()
        use sm = SessionManager.create install bus.Sink

        let observedCtrl = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observedCtrl.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        match sm.Launch(lobby, false) with
        | Result.Error msg -> Assert.Fail(sprintf "Launch rejected: %s" msg)
        | Ok () -> ()

        let running =
            LiveFixtures.waitUntil 30000 (fun () ->
                match sm.State with SessionManager.Running _ -> true | _ -> false)
        Assert.True(running, "session did not reach Running state")

        sm.SetSpeed 3.0f
        sm.SetPaused true
        sm.SetPaused false
        Thread.Sleep(200)

        let events = observedCtrl.ToArray()
        Assert.Contains(events, function HubEvents.EngineSpeedChanged 3.0f -> true | _ -> false)
        Assert.Contains(events, function HubEvents.SessionPaused true -> true | _ -> false)
        Assert.Contains(events, function HubEvents.SessionPaused false -> true | _ -> false)

        sm.End()
    }


[<CollectionDefinition("HubSession")>]
type HubSessionCollection() =
    // Serialises live-session tests so concurrent BAR engine spawns
    // don't trample each other's socket paths / session dirs.
    class end
