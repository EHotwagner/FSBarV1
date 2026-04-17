module FSBar.Hub.Tests.CloseWhileRunningTests

// T026a per specs/035-central-gui-hub/tasks.md — Edge-case coverage:
// the hub should prompt before tearing down a live session.
//
// `ProcessLifetime.requestClose` is a pure predicate over
// `SessionManager.SessionState`. Tests construct each state variant
// directly (no real session needed) and assert the decision.

open System
open System.IO
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.App

let private dummyLobby : LobbyConfig.LobbyConfig =
    { MapName = "fixture"
      Mode = LobbyConfig.Skirmish
      EngineSpeed = 1.0f
      LaunchGraphicalViewer = false
      Teams =
        [ { Seats =
              [ { Kind = LobbyConfig.AiSeat("HighBarV2", Map.empty)
                  Side = "Armada"
                  Handicap = 0 } ]
            AllyTeamId = 0 }
          { Seats =
              [ { Kind = LobbyConfig.AiSeat("BARb", Map.empty)
                  Side = "Cortex"
                  Handicap = 0 } ]
            AllyTeamId = 1 } ]
      Spectators = [] }

/// Builds a RunningSession stub. The fields the `requestClose`
/// predicate branches on are the DU case only — the inner record is
/// treated as an opaque value.
let private stubRunningSession () : SessionManager.RunningSession =
    // Build a BarClient with the lightest-weight EngineConfig possible.
    // `requestClose` never calls into the client so Start() need not
    // run; the record just needs a valid reference.
    let cfg = EngineConfig.defaultConfig ()
    let client = new BarClient(cfg)
    { Id = Guid.NewGuid()
      Config = dummyLobby
      EngineConfig = cfg
      BarClient = client
      GraphicalEngineProcess = None
      StartedAt = DateTimeOffset.UtcNow
      MapGrid = None
      MetalSpots = [||] }

let private dummyEngineConfig () : EngineConfig =
    EngineConfig.defaultConfig ()

[<Fact>]
let ``Idle allows close immediately`` () =
    let decision = ProcessLifetime.requestClose SessionManager.Idle
    Assert.Equal(ProcessLifetime.CloseDecision.AllowClose, decision)

[<Fact>]
let ``Failed state allows close`` () =
    let state = SessionManager.Failed(dummyLobby, "engine crashed", None)
    Assert.Equal(ProcessLifetime.CloseDecision.AllowClose, ProcessLifetime.requestClose state)

[<Fact>]
let ``Ending state allows close`` () =
    let rs = stubRunningSession ()
    Assert.Equal(ProcessLifetime.CloseDecision.AllowClose,
        ProcessLifetime.requestClose (SessionManager.Ending rs))

[<Fact>]
let ``Running state requires confirmation`` () =
    let rs = stubRunningSession ()
    match ProcessLifetime.requestClose (SessionManager.Running rs) with
    | ProcessLifetime.CloseDecision.RequireConfirm msg ->
        Assert.Contains("session is running", msg.ToLowerInvariant())
        Assert.Contains("close anyway?", msg.ToLowerInvariant())
    | other -> Assert.Fail(sprintf "expected RequireConfirm, got %A" other)

[<Fact>]
let ``Starting state requires confirmation`` () =
    let state = SessionManager.Starting dummyLobby
    match ProcessLifetime.requestClose state with
    | ProcessLifetime.CloseDecision.RequireConfirm msg ->
        Assert.Contains("warmup", msg.ToLowerInvariant())
    | other -> Assert.Fail(sprintf "expected RequireConfirm, got %A" other)

[<Fact>]
let ``PID registry round-trips`` () =
    // Fresh registry (tests don't share state since the module is a
    // singleton — unregister after each add to keep the invariant).
    let seed = [ 42; 100; 9999 ]
    for p in seed do ProcessLifetime.register p
    let tracked = ProcessLifetime.tracked ()
    for p in seed do Assert.Contains(p, tracked)
    for p in seed do ProcessLifetime.unregister p
    let after = ProcessLifetime.tracked ()
    for p in seed do Assert.DoesNotContain(p, after)

[<Fact>]
let ``Register ignores non-positive PIDs`` () =
    ProcessLifetime.register 0
    ProcessLifetime.register -1
    let tracked = ProcessLifetime.tracked ()
    Assert.DoesNotContain(0, tracked)
    Assert.DoesNotContain(-1, tracked)
