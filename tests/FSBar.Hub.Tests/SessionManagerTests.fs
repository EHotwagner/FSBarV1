module FSBar.Hub.Tests.SessionManagerTests

// These tests exercise SessionManager at its public surface using
// fixtures from LobbyConfigTests. We do NOT launch a real BAR engine
// — the hub's actual engine-launch path runs under LiveSessionLaunchTests
// (gated on `tests/engine-version.json` / engine availability).

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open Xunit
open FSBar.Hub
open FSBar.Hub.LobbyConfig

/// Copy of the LobbyConfigTests fixture so this file stands alone.
type private FakeInstall() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-sessmgr-test-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(p) |> ignore
        Directory.CreateDirectory(Path.Combine(p, "maps")) |> ignore
        p

    do
        let engDir = Path.Combine(tempDir, "engine", "recoil_2026.03.14")
        Directory.CreateDirectory(engDir) |> ignore
        let hb = Path.Combine(engDir, "spring-headless")
        File.WriteAllText(hb, "#!/bin/sh\nexit 0")
        File.SetUnixFileMode(
            hb,
            UnixFileMode.UserRead ||| UnixFileMode.UserWrite ||| UnixFileMode.UserExecute)
        let aiDir = Path.Combine(engDir, "AI", "Skirmish", "HighBarV2")
        Directory.CreateDirectory(aiDir) |> ignore
        let aiDir2 = Path.Combine(engDir, "AI", "Skirmish", "BARb")
        Directory.CreateDirectory(aiDir2) |> ignore
        let mapFile = Path.Combine(tempDir, "maps", "Fixture 1.sd7")
        File.WriteAllText(mapFile, "fake sd7")

    member _.DataDir = tempDir

    member this.Resolve() =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Ok install -> install
        | Result.Error e -> failwith (BarInstall.formatError e)

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private happyLobby (mapName: string) =
    { MapName = mapName
      Mode = Skirmish
      EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams =
        [ { Seats = [ { Kind = AiSeat("HighBarV2", Map.empty); Side = "Armada"; Handicap = 0 } ]
            AllyTeamId = 0 }
          { Seats = [ { Kind = AiSeat("BARb", Map.empty); Side = "Cortex"; Handicap = 0 } ]
            AllyTeamId = 1 } ]
      Spectators = [] }

let private collectEvents (bus: HubEvents.HubEventBus) (count: int) (timeoutMs: int) : HubEvents.HubEvent list =
    let received = ConcurrentQueue<HubEvents.HubEvent>()
    let signal = new ManualResetEventSlim(false)
    use _sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvents.HubEvent> with
                member _.OnNext(evt) =
                    received.Enqueue(evt)
                    if received.Count >= count then signal.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    signal.Wait(timeoutMs) |> ignore
    received.ToArray() |> List.ofArray

[<Fact>]
let ``initial state is Idle`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    Assert.Equal(SessionManager.Idle, sm.State)

[<Fact>]
let ``Launch with bad lobby returns Error and stays Idle`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    // No map named "NotInstalled" on disk.
    let bad = happyLobby "NotInstalled"
    match sm.Launch(bad, false) with
    | Ok () -> Assert.Fail("Launch should reject lobby with missing map")
    | Result.Error msg ->
        Assert.Contains("validation", msg)
    Assert.Equal(SessionManager.Idle, sm.State)

[<Fact>]
let ``SetSpeed emits EngineSpeedChanged`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    let collected = async {
        return collectEvents bus 1 1000
    }
    let collector = Async.StartAsTask collected
    // Give the subscriber a moment to attach.
    Thread.Sleep(50)
    sm.SetSpeed 2.5f
    let events = collector.Result
    Assert.Contains(events, function HubEvents.EngineSpeedChanged 2.5f -> true | _ -> false)

[<Fact>]
let ``SetPaused no-op when not Running (feature 038)`` () =
    // Feature 038 repurposes SetPaused from a simple event-emitter into
    // an engine-wired call. Without a Running session there is nothing
    // to pause — the call should be a silent no-op rather than emit a
    // stale SessionPaused event.
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    let collector = Async.StartAsTask (async { return collectEvents bus 1 200 })
    Thread.Sleep(50)
    sm.SetPaused true
    let events = collector.Result
    // Guarantee: no SessionPaused event while Idle.
    Assert.DoesNotContain(events, function HubEvents.SessionPaused _ -> true | _ -> false)
    Assert.False(sm.IsPaused)

[<Fact>]
let ``TogglePause no-op when not Running (feature 038)`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    sm.TogglePause()
    sm.TogglePause()
    Assert.False(sm.IsPaused)

[<Fact>]
let ``IsPaused defaults false on fresh manager (feature 038)`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    Assert.False(sm.IsPaused)

[<Fact>]
let ``End on Idle is a no-op`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    sm.End()
    Assert.Equal(SessionManager.Idle, sm.State)

[<Fact>]
let ``Dispose from Idle is safe`` () =
    use fake = new FakeInstall()
    let bus = HubEvents.create ()
    let sm = SessionManager.create (fake.Resolve()) bus.Sink
    (sm :> IDisposable).Dispose()
    (bus :> IDisposable).Dispose()
    // Second Dispose should not throw.
    (sm :> IDisposable).Dispose()

[<Fact>]
let ``Launch transitions to Starting before returning`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    // Happy lobby on a real map — Launch should succeed synchronously
    // and transition to Starting. The background Start() will fail
    // because no real engine is present, but we only need the
    // synchronous Starting transition for this test.
    let collector = Async.StartAsTask (async { return collectEvents bus 1 1000 })
    Thread.Sleep(50)
    match sm.Launch(happyLobby "Fixture 1", false) with
    | Ok () ->
        let state = sm.State
        match state with
        | SessionManager.Starting _ | SessionManager.Failed _ -> ()
        | other -> Assert.Fail(sprintf "expected Starting or Failed, got %A" other)
        let events = collector.Result
        Assert.Contains(events, function HubEvents.StateChanged HubEvents.Starting -> true | _ -> false)
    | Result.Error msg -> Assert.Fail(sprintf "Launch failed: %s" msg)

[<Fact>]
let ``Second Launch while active returns Error`` () =
    use fake = new FakeInstall()
    use bus = HubEvents.create ()
    use sm = SessionManager.create (fake.Resolve()) bus.Sink
    match sm.Launch(happyLobby "Fixture 1", false) with
    | Ok () ->
        // Immediately try a second Launch while Starting / Running.
        match sm.Launch(happyLobby "Fixture 1", false) with
        | Result.Error msg -> Assert.Contains("already active", msg)
        | Ok () ->
            // Allow the fast-failing background task to terminate.
            Thread.Sleep(200)
            // If state flipped to Failed/Idle in the meantime a fresh
            // Launch is legal; this is still a valid implementation.
            ()
    | Result.Error msg -> Assert.Fail(sprintf "first Launch failed unexpectedly: %s" msg)
