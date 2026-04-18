module FSBar.Hub.Tests.ScriptingHubTests

// In-process tests for ScriptingHub (feature 035-central-gui-hub T060).
// Drives the fan-out pump through the service's `PushTestFrame` /
// `AttachTestClient` internal helpers so we don't need a running gRPC
// host or a live BarClient. Live-wire coverage lands in
// FSBar.Hub.LiveTests (gated on engine availability).

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Channels
open Xunit
open FSBar.Hub
open Fsbar.Hub.Scripting.V1

/// Minimal BAR fixture — enough for SessionManager.create / BarInstall.detect.
type private BarFixture() =
    let tempDir =
        let p = Path.Combine(Path.GetTempPath(), "fsbar-scripthub-test-" + Guid.NewGuid().ToString("N"))
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

    member _.DataDir = tempDir

    member this.Resolve() =
        let settings = { HubSettings.defaults with BarDataDirOverride = Some this.DataDir }
        match BarInstall.detect settings with
        | Ok install -> install
        | Result.Error e -> failwith (BarInstall.formatError e)

    interface IDisposable with
        member _.Dispose() =
            try Directory.Delete(tempDir, recursive = true) with _ -> ()

let private makeBundled () : BundledProxy.BundledProxyInfo =
    { Version = "0.1.17"
      BundleRoot = "/tmp/stub"
      LibSkirmishAiPath = "/tmp/stub/libSkirmishAI.so"
      AiInfoLuaPath = "/tmp/stub/AIInfo.lua"
      AiOptionsLuaPath = "/tmp/stub/AIOptions.lua" }

/// Convenience builder that wires a SessionManager + empty UnitDefCache
/// thunk + empty BarInstall into a ScriptingService.
let private makeService (opts: ScriptingHub.ScriptingHubOptions) =
    let fixture = new BarFixture()
    let install = fixture.Resolve()
    let bus = HubEvents.create ()
    let sessions = SessionManager.create install bus.Sink
    let unitDefs () = FSBar.Client.UnitDefCache.empty
    let initialState : HubState =
        { ActiveTab = FSBar.Hub.HubTab.Setup
          VizConfig = FSBar.Viz.VizDefaults.defaultConfig
          Camera = ViewerCamera.defaults
          Lobby = LobbyConfig.defaults
          Encyclopedia =
              { FactionFilter = Set.empty; SelectedDefId = None }
          PresetList = []
          Settings = HubSettings.defaults }
    let store = HubStateStore.create bus.Sink initialState
    let overlays = OverlayLayerStore.create bus.Sink
    let renderer =
        HeadlessRenderer.create sessions store overlays (fun () -> HubSettings.defaults)
    let service =
        new ScriptingHub.ScriptingService(
            sessions, bus.Sink, bus.Events, unitDefs, install, makeBundled (), 5021,
            store, renderer, overlays, opts)
    service, bus, sessions, fixture

let private drain (reader: ChannelReader<GameFrameMessage>) (expected: int) (timeoutMs: int) =
    let received = ResizeArray<GameFrameMessage>()
    let sw = System.Diagnostics.Stopwatch.StartNew()
    while received.Count < expected && sw.ElapsedMilliseconds < int64 timeoutMs do
        let mutable msg = Unchecked.defaultof<GameFrameMessage>
        if reader.TryRead(&msg) then received.Add(msg)
        else Thread.Sleep(5)
    received |> List.ofSeq

[<Fact>]
let ``defaults are 16 buffer capacity and 32 max drops`` () =
    Assert.Equal(16, ScriptingHub.defaults.FrameBufferCapacity)
    Assert.Equal(32, ScriptingHub.defaults.MaxCumulativeDrops)

[<Fact>]
let ``initial Clients roster is empty`` () =
    let service, bus, sessions, fixture = makeService ScriptingHub.defaults
    try
        Assert.Empty(service.Clients)
        Assert.Equal(0, service.OverflowDetachCount)
    finally
        (service :> IDisposable).Dispose()
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()

[<Fact>]
let ``AttachTestClient publishes a ScriptingClientConnected event`` () =
    let service, bus, sessions, fixture = makeService ScriptingHub.defaults
    try
        let observed = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observed.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })
        let _id, _reader = service.AttachTestClient("alpha")
        Thread.Sleep(100)
        let events = observed.ToArray()
        Assert.Contains(events, function
            | HubEvents.ScriptingClientConnected(_, _) -> true
            | _ -> false)
        Assert.Single(service.Clients) |> ignore
    finally
        (service :> IDisposable).Dispose()
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()

[<Fact>]
let ``five concurrent clients receive every frame in order`` () =
    let service, bus, sessions, fixture = makeService ScriptingHub.defaults
    try
        let readers =
            [ for i in 1 .. 5 ->
                let _id, r = service.AttachTestClient(sprintf "client-%d" i)
                r ]
        // Push 10 frames. With a 16-capacity buffer and 5 attached
        // readers, nothing should be dropped.
        for frameN in 1 .. 10 do
            service.PushTestFrame(frameN, 0)
        // Each reader should have all 10 frames in their channel.
        for r in readers do
            let got = drain r 10 2000
            Assert.Equal(10, got.Length)
            // ClientSequence values are strictly increasing and cover
            // 1..10 (Interlocked.Increment starts at 1 for a fresh
            // reg with Sequence=0).
            let seqs = got |> List.map (fun m -> int m.ClientSequence)
            Assert.Equal<int list>([ 1..10 ], seqs)
            // Each message carries the correct frame number.
            let frameNums =
                got |> List.map (fun m ->
                    match m.Frame with
                    | Some f -> int f.FrameNumber
                    | None -> -1)
            Assert.Equal<int list>([ 1..10 ], frameNums)
    finally
        (service :> IDisposable).Dispose()
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()

[<Fact>]
let ``slow client is detached when cumulative drops exceed MaxCumulativeDrops`` () =
    // Tiny buffer + low threshold so the test triggers quickly.
    let opts = { ScriptingHub.FrameBufferCapacity = 2; ScriptingHub.MaxCumulativeDrops = 5 }
    let service, bus, sessions, fixture = makeService opts
    try
        let observed = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observed.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })
        let slowId, slowReader = service.AttachTestClient("slow")
        let fastId, fastReader = service.AttachTestClient("fast")
        // Slow client never reads.
        // Fast client drains after every frame.
        for frameN in 1 .. 50 do
            service.PushTestFrame(frameN, 0)
            // Drain fast reader so it keeps up.
            let mutable msg = Unchecked.defaultof<GameFrameMessage>
            while fastReader.TryRead(&msg) do ()
        Thread.Sleep(150)
        // Slow client should be gone; fast client should still be present.
        let roster = service.Clients |> List.map (fun c -> c.ClientLabel) |> Set.ofList
        Assert.DoesNotContain("slow", roster)
        Assert.Contains("fast", roster)
        Assert.Equal(1, service.OverflowDetachCount)
        // The event bus should have seen an OverflowDropLimit detach.
        let events = observed.ToArray()
        Assert.Contains(events, function
            | HubEvents.ScriptingClientDetached(cid, HubEvents.OverflowDropLimit) when cid = slowId -> true
            | _ -> false)
    finally
        (service :> IDisposable).Dispose()
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()

[<Fact>]
let ``DetachTestClient publishes ScriptingClientDetached(ClientDisconnected)`` () =
    let service, bus, sessions, fixture = makeService ScriptingHub.defaults
    try
        let observed = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observed.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })
        let id, _reader = service.AttachTestClient("leaving")
        Thread.Sleep(50)
        service.DetachTestClient(id)
        Thread.Sleep(100)
        let events = observed.ToArray()
        Assert.Contains(events, function
            | HubEvents.ScriptingClientDetached(cid, HubEvents.ClientDisconnected) when cid = id -> true
            | _ -> false)
        Assert.Empty(service.Clients)
    finally
        (service :> IDisposable).Dispose()
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()

[<Fact>]
let ``Dispose publishes ServerShutdown detach for every active client`` () =
    let service, bus, sessions, fixture = makeService ScriptingHub.defaults
    try
        let observed = ConcurrentQueue<HubEvents.HubEvent>()
        use _sub =
            bus.Events.Subscribe(
                { new IObserver<HubEvents.HubEvent> with
                    member _.OnNext(e) = observed.Enqueue(e)
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })
        let id1, _ = service.AttachTestClient("a")
        let id2, _ = service.AttachTestClient("b")
        Thread.Sleep(50)
        (service :> IDisposable).Dispose()
        Thread.Sleep(100)
        let events = observed.ToArray()
        Assert.Contains(events, function
            | HubEvents.ScriptingClientDetached(c, HubEvents.ServerShutdown) when c = id1 -> true
            | _ -> false)
        Assert.Contains(events, function
            | HubEvents.ScriptingClientDetached(c, HubEvents.ServerShutdown) when c = id2 -> true
            | _ -> false)
    finally
        (sessions :> IDisposable).Dispose()
        (bus :> IDisposable).Dispose()
        (fixture :> IDisposable).Dispose()
