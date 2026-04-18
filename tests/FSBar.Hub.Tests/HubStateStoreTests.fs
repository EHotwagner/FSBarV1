module FSBar.Hub.Tests.HubStateStoreTests

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Tasks
open Xunit
open FSBar.Hub
open FSBar.Hub.HubEvents
open FSBar.Viz

// --- Fixtures ---------------------------------------------------------------

let private collectingSink (received: ConcurrentQueue<HubEvent>) : IHubEventSink =
    { new IHubEventSink with
        member _.Publish(evt) = received.Enqueue(evt) }

let private freshInitialState () : HubState =
    { ActiveTab = HubTab.Setup
      VizConfig = VizDefaults.defaultConfig
      Camera = ViewerCamera.defaults
      Lobby = LobbyConfig.defaults
      Encyclopedia =
        { FactionFilter = Set.empty
          SelectedDefId = None }
      PresetList = []
      Settings = HubSettings.defaults }

// --- Atomic LWW / event emission -------------------------------------------

[<Fact>]
let ``setVizConfig on two racing threads converges with exactly two events`` () =
    let received = ConcurrentQueue<HubEvent>()
    let store = HubStateStore.create (collectingSink received) (freshInitialState ())
    let configA = { VizDefaults.defaultConfig with UnitMarkerSize = 7.0f }
    let configB = { VizDefaults.defaultConfig with UnitMarkerSize = 11.0f }
    let barrier = new Barrier(2)
    let t1 =
        Task.Run(fun () ->
            barrier.SignalAndWait()
            HubStateStore.setVizConfig store configA)
    let t2 =
        Task.Run(fun () ->
            barrier.SignalAndWait()
            HubStateStore.setVizConfig store configB)
    Task.WaitAll([| t1 :> Task; t2 :> Task |])
    // Both writers succeeded (LWW under contention retries up to 3 times;
    // two writers cannot exhaust that bound against a non-thrashing test).
    Assert.Equal(SubmitOutcome.Sent, t1.Result)
    Assert.Equal(SubmitOutcome.Sent, t2.Result)
    let vizEvents =
        received.ToArray()
        |> Array.choose (function
            | VizConfigChanged c -> Some c
            | _ -> None)
    Assert.Equal(2, vizEvents.Length)
    // Final state is whichever writer won the CAS race last.
    let finalRadius = (HubStateStore.current store).VizConfig.UnitMarkerSize
    Assert.True(finalRadius = 7.0f || finalRadius = 11.0f)

[<Fact>]
let ``each mutator emits exactly one event per successful call`` () =
    let received = ConcurrentQueue<HubEvent>()
    let store = HubStateStore.create (collectingSink received) (freshInitialState ())
    Assert.Equal(SubmitOutcome.Sent, HubStateStore.setActiveTab store HubTab.Viewer)
    Assert.Equal(SubmitOutcome.Sent, HubStateStore.setCamera store ViewerCamera.defaults)
    Assert.Equal(SubmitOutcome.Sent, HubStateStore.setLobby store LobbyConfig.defaults)
    Assert.Equal(SubmitOutcome.Sent, HubStateStore.setVizConfig store VizDefaults.defaultConfig)
    HubStateStore.updatePresetList store [ "a"; "b" ]  // no event
    let events = received.ToArray() |> List.ofArray
    let byKind =
        events
        |> List.map (function
            | ActiveTabChanged _ -> "ActiveTab"
            | CameraChanged _ -> "Camera"
            | LobbyChanged _ -> "Lobby"
            | VizConfigChanged _ -> "VizConfig"
            | other -> sprintf "UNEXPECTED-%A" other)
    Assert.Equal<string list>([ "ActiveTab"; "Camera"; "Lobby"; "VizConfig" ], byKind)

// --- ViewerCamera.validate --------------------------------------------------

[<Theory>]
[<InlineData(System.Single.NaN, 0.0f, 0.0f)>]
[<InlineData(0.0f, System.Single.NaN, 0.0f)>]
[<InlineData(0.0f, 0.0f, System.Single.PositiveInfinity)>]
[<InlineData(0.04f, 0.0f, 0.0f)>]        // below min
[<InlineData(100.5f, 0.0f, 0.0f)>]       // above max
let ``ViewerCamera.validate rejects invalid components`` (scale, originX, originY) =
    let cam =
        { Scale = scale
          OriginX = originX
          OriginY = originY
          AutoFit = false }
    match ViewerCamera.validate cam with
    | Ok _ -> Assert.Fail("expected rejection")
    | Result.Error _ -> ()

[<Fact>]
let ``ViewerCamera.validate accepts defaults`` () =
    match ViewerCamera.validate ViewerCamera.defaults with
    | Ok _ -> ()
    | Result.Error reason -> Assert.Fail(sprintf "unexpected rejection: %s" reason)

[<Fact>]
let ``setCamera rejects invalid camera and emits a DiagnosticsLine Warning`` () =
    // Feature 041 FR-023a / R7 — every Rejected mutator outcome publishes
    // exactly one DiagnosticsLine Warning whose message names the mutator.
    let received = ConcurrentQueue<HubEvent>()
    let store = HubStateStore.create (collectingSink received) (freshInitialState ())
    let bad =
        { Scale = System.Single.NaN
          OriginX = 0.0f
          OriginY = 0.0f
          AutoFit = false }
    match HubStateStore.setCamera store bad with
    | SubmitOutcome.Rejected _ -> ()
    | SubmitOutcome.Sent ->
        Assert.Fail("expected rejection for NaN scale")
    let events = received.ToArray()
    let diagnostics =
        events
        |> Array.choose (function
            | DiagnosticsLine (sev, msg) -> Some (sev, msg)
            | _ -> None)
    Assert.Equal(1, diagnostics.Length)
    let sev, msg = diagnostics.[0]
    Assert.Equal(Warning, sev)
    Assert.StartsWith("HubStateStore.setCamera rejected:", msg)
    // No success-path events fired.
    let nonDiagnostic =
        events
        |> Array.filter (function | DiagnosticsLine _ -> false | _ -> true)
    Assert.Empty(nonDiagnostic)

[<Fact>]
let ``T029 — setCamera out-of-range scale emits HubStateStore.setCamera rejected warning`` () =
    let received = ConcurrentQueue<HubEvent>()
    let store = HubStateStore.create (collectingSink received) (freshInitialState ())
    let outOfRange =
        { Scale = 200.0f
          OriginX = 0.0f
          OriginY = 0.0f
          AutoFit = false }
    let outcome = HubStateStore.setCamera store outOfRange
    match outcome with
    | SubmitOutcome.Rejected _ -> ()
    | SubmitOutcome.Sent ->
        Assert.Fail("expected rejection for scale 200")
    let warnings =
        received.ToArray()
        |> Array.choose (function
            | DiagnosticsLine (Warning, msg)
              when msg.StartsWith("HubStateStore.setCamera rejected:") -> Some msg
            | _ -> None)
    Assert.Equal(1, warnings.Length)
