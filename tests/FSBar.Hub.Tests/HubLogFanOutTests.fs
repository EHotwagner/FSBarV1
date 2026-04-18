module FSBar.Hub.Tests.HubLogFanOutTests

open System
open System.Threading
open System.Threading.Channels
open Xunit
open FSBar.Hub

let private nullSink =
    { new HubEvents.IHubEventSink with
        member _.Publish(_) = () }

let private settings () =
    { HubSettings.defaults with MaxLogStreamSubscribers = 8 }

let private drainOne (reader: ChannelReader<HubLog.LogEntry>) (timeoutMs: int) : HubLog.LogEntry option =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let mutable result = None
    while result.IsNone && sw.ElapsedMilliseconds < int64 timeoutMs do
        let mutable entry = Unchecked.defaultof<HubLog.LogEntry>
        if reader.TryRead(&entry) then result <- Some entry
        else Thread.Sleep(2)
    result

// T025 — three subscribers should each see every emitted entry with
// byte-identical fields, and each subscriber's wire-level sequence is
// independent from the others.
[<Fact>]
let ``multi subscriber sees identical entries`` () =
    use bus = HubLog.create nullSink settings
    let cts = new CancellationTokenSource()
    let attachOne label =
        match HubLog.attach bus label HubLog.defaultFilter cts.Token with
        | HubLog.Rejected reason -> failwithf "attach rejected: %s" reason
        | HubLog.Attached sub -> sub
    let a = attachOne "a"
    let b = attachOne "b"
    let c = attachOne "c"
    try
        for i in 1 .. 5 do
            HubLog.emit bus HubLog.SessionManager HubLog.Info None None
                (fun () -> sprintf "entry-%d" i)
        // Drain each reader and assert byte-identical content across subs.
        let drainAll (sub: HubLog.Subscription) =
            [ for _ in 1 .. 5 ->
                match drainOne sub.Reader 500 with
                | None -> failwith "timeout"
                | Some e -> e ]
        let entriesA = drainAll a
        let entriesB = drainAll b
        let entriesC = drainAll c
        Assert.Equal(5, entriesA.Length)
        for i in 0 .. 4 do
            Assert.Equal(entriesA.[i].Message, entriesB.[i].Message)
            Assert.Equal(entriesA.[i].Message, entriesC.[i].Message)
            Assert.Equal(entriesA.[i].Category, entriesB.[i].Category)
            Assert.Equal(entriesA.[i].Severity, entriesB.[i].Severity)
            Assert.Equal(entriesA.[i].TimestampUnixMs, entriesB.[i].TimestampUnixMs)
        // Per-subscriber sequence counters advance independently.
        for sub in [ a; b; c ] do
            for expectedSeq in 1UL .. 5UL do
                let seq = HubLog.nextSequenceFor bus sub.Id
                Assert.Equal(expectedSeq, seq)
    finally
        a.Dispose(); b.Dispose(); c.Dispose()

// T052 — slow subscriber drops oldest, reports the drop count on the
// next delivered entry.
[<Fact>]
let ``slow subscriber drops oldest and reports count`` () =
    use bus = HubLog.create nullSink settings
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "slow" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected reason -> Assert.Fail(sprintf "attach rejected: %s" reason)
    | HubLog.Attached sub ->
        try
            // Flood past the capacity of 256 entries.
            for i in 1 .. 1024 do
                HubLog.emit bus HubLog.SessionManager HubLog.Info None None
                    (fun () -> sprintf "burst-%d" i)
            // DropOldest is best-effort: the subscriber channel capacity
            // is 256, so any live count ≥ 1 is acceptable. We check that
            // the first-emitted entry is NOT the first one remaining.
            let mutable entry = Unchecked.defaultof<HubLog.LogEntry>
            Assert.True(sub.Reader.TryRead(&entry))
            Assert.NotEqual<string>("burst-1", entry.Message)
            // After draining one, the drop counter should now be non-zero.
            let dropped = HubLog.exchangeDroppedSinceLast bus sub.Id
            Assert.True(dropped > 0, sprintf "expected non-zero drop count, got %d" dropped)
        finally
            sub.Dispose()

// T052 — dispose releases per-subscriber state within 1 s.
[<Fact>]
let ``dispose releases per subscriber state within 1s`` () =
    use bus = HubLog.create nullSink settings
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "x" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached sub ->
        Assert.Equal(1, HubLog.subscriberCount bus)
        let sw = System.Diagnostics.Stopwatch.StartNew()
        sub.Dispose()
        while HubLog.subscriberCount bus > 0 && sw.ElapsedMilliseconds < 1000L do
            Thread.Sleep(5)
        sw.Stop()
        Assert.Equal(0, HubLog.subscriberCount bus)
        Assert.True(sw.ElapsedMilliseconds < 1000L,
            sprintf "detach took %d ms" sw.ElapsedMilliseconds)

[<Fact>]
let ``cancellation token from grpc releases subscriber`` () =
    use bus = HubLog.create nullSink settings
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "x" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached _ ->
        Assert.Equal(1, HubLog.subscriberCount bus)
        cts.Cancel()
        let sw = System.Diagnostics.Stopwatch.StartNew()
        while HubLog.subscriberCount bus > 0 && sw.ElapsedMilliseconds < 1000L do
            Thread.Sleep(5)
        Assert.Equal(0, HubLog.subscriberCount bus)

[<Fact>]
let ``attach rejected when cap reached`` () =
    let cap = 2
    use bus =
        HubLog.create nullSink (fun () ->
            { HubSettings.defaults with MaxLogStreamSubscribers = cap })
    let cts = new CancellationTokenSource()
    let subs = ResizeArray<HubLog.Subscription>()
    try
        for i in 1 .. cap do
            match HubLog.attach bus (sprintf "sub-%d" i) HubLog.defaultFilter cts.Token with
            | HubLog.Attached s -> subs.Add(s)
            | HubLog.Rejected r -> Assert.Fail(sprintf "unexpected rejection at %d: %s" i r)
        match HubLog.attach bus "over" HubLog.defaultFilter cts.Token with
        | HubLog.Attached _ -> Assert.Fail("attach over cap should reject")
        | HubLog.Rejected reason ->
            Assert.Contains("max log-stream subscribers", reason)
    finally
        for s in subs do s.Dispose()
