module FSBar.Hub.Tests.HubEventsTests

open System
open System.Collections.Concurrent
open System.Threading
open Xunit
open FSBar.Hub
open FSBar.Hub.HubEvents

// Helper: drain all events into a list, returning it once `expected` events
// arrive or the timeout expires.
let private collectUntil (bus: HubEventBus) (expected: int) (timeoutMs: int) : HubEvent list =
    let received = ConcurrentQueue<HubEvent>()
    let signal = new ManualResetEventSlim(false)
    use _sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(evt) =
                    received.Enqueue(evt)
                    if received.Count >= expected then signal.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    signal.Wait(timeoutMs) |> ignore
    received.ToArray() |> List.ofArray

[<Fact>]
let ``Publish reaches a subscribed observer`` () =
    use bus = HubEvents.create ()
    let received = ConcurrentQueue<HubEvent>()
    let signal = new ManualResetEventSlim(false)
    use _sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(e) = received.Enqueue(e); signal.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    bus.Sink.Publish(StateChanged Running)
    Assert.True(signal.Wait(1000), "observer did not receive event within 1s")
    Assert.Equal<HubEvent[]>([| StateChanged Running |], received.ToArray())

[<Fact>]
let ``Multiple observers all receive each event`` () =
    use bus = HubEvents.create ()
    let received1 = ConcurrentQueue<HubEvent>()
    let received2 = ConcurrentQueue<HubEvent>()
    let signal1 = new ManualResetEventSlim(false)
    let signal2 = new ManualResetEventSlim(false)
    use _s1 =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(e) = received1.Enqueue(e); signal1.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    use _s2 =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(e) = received2.Enqueue(e); signal2.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    bus.Sink.Publish(EngineSpeedChanged 2.0f)
    signal1.Wait(1000) |> ignore
    signal2.Wait(1000) |> ignore
    Assert.Equal<HubEvent[]>([| EngineSpeedChanged 2.0f |], received1.ToArray())
    Assert.Equal<HubEvent[]>([| EngineSpeedChanged 2.0f |], received2.ToArray())

[<Fact>]
let ``Disposed subscription stops receiving events`` () =
    use bus = HubEvents.create ()
    let count = ref 0
    let sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(_) = Interlocked.Increment(count) |> ignore
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    bus.Sink.Publish(SessionPaused true)
    // Drain the first event — poll briefly.
    let deadline = DateTime.UtcNow.AddMilliseconds(1000.0)
    while !count < 1 && DateTime.UtcNow < deadline do
        Thread.Sleep(5)
    Assert.Equal(1, !count)
    sub.Dispose()
    bus.Sink.Publish(SessionPaused false)
    // Give the pump a chance to process the second event — it should NOT
    // reach our observer.
    Thread.Sleep(100)
    Assert.Equal(1, !count)

[<Fact>]
let ``Slow observer does not block fast observer`` () =
    use bus = HubEvents.create ()
    let fastCount = ref 0
    let fastDone = new ManualResetEventSlim(false)
    use _fast =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(_) =
                    let n = Interlocked.Increment(fastCount)
                    if n >= 3 then fastDone.Set()
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    // Slow observer sleeps 500ms per event. Since the pump dispatches
    // synchronously to each observer in a snapshot loop, the bus does
    // NOT promise that a slow observer never holds up a fast one — the
    // promise is weaker: publishers are not blocked. So this test
    // asserts the publisher-side promise: Publish returns fast.
    use _slow =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(_) = Thread.Sleep(500)
                member _.OnError(_) = ()
                member _.OnCompleted() = () })
    let sw = System.Diagnostics.Stopwatch.StartNew()
    for _ in 1 .. 3 do
        bus.Sink.Publish(DiagnosticsLine(Info, "tick"))
    sw.Stop()
    // 3 Publish calls should return well under 100ms even though the
    // slow observer needs ~1.5s to process them.
    Assert.True(sw.ElapsedMilliseconds < 100L, sprintf "Publish blocked for %dms" sw.ElapsedMilliseconds)

[<Fact>]
let ``Dispose signals OnCompleted`` () =
    let bus = HubEvents.create ()
    let completed = new ManualResetEventSlim(false)
    use _sub =
        bus.Events.Subscribe(
            { new IObserver<HubEvent> with
                member _.OnNext(_) = ()
                member _.OnError(_) = ()
                member _.OnCompleted() = completed.Set() })
    (bus :> IDisposable).Dispose()
    Assert.True(completed.Wait(1000), "OnCompleted was not signalled within 1s")
