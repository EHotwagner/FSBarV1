module FSBar.Hub.Tests.HubLogTests

open System
open System.Text
open System.Threading
open System.Threading.Channels
open Xunit
open FSBar.Hub

// --- Shared helpers ----------------------------------------------------

let private nullSink =
    { new HubEvents.IHubEventSink with
        member _.Publish(_) = () }

let private settingsWith (cap: int) =
    fun () -> { HubSettings.defaults with MaxLogStreamSubscribers = cap }

let private drainOne (reader: ChannelReader<HubLog.LogEntry>) (timeoutMs: int) : HubLog.LogEntry option =
    let sw = System.Diagnostics.Stopwatch.StartNew()
    let mutable result = None
    while result.IsNone && sw.ElapsedMilliseconds < int64 timeoutMs do
        let mutable entry = Unchecked.defaultof<HubLog.LogEntry>
        if reader.TryRead(&entry) then result <- Some entry
        else Thread.Sleep(2)
    result

// --- Emit + filter + truncation --------------------------------------

[<Fact>]
let ``stream receives emitted entries`` () =
    use bus = HubLog.create nullSink (settingsWith 4)
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t1" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected reason -> Assert.Fail(sprintf "attach rejected: %s" reason)
    | HubLog.Attached sub ->
        HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "hello world")
        match drainOne sub.Reader 500 with
        | None -> Assert.Fail("no entry received within 500 ms")
        | Some entry ->
            Assert.Equal(HubLog.SessionManager, entry.Category)
            Assert.Equal(HubLog.Info, entry.Severity)
            Assert.Equal("hello world", entry.Message)
        sub.Dispose()

[<Fact>]
let ``truncateUtf8 does not exceed 8 KiB on pathological inputs`` () =
    let ascii10k = String.replicate 10240 "x"
    let truncated = HubLog.truncateUtf8 ascii10k
    let byteCount = Encoding.UTF8.GetByteCount(truncated)
    Assert.True(byteCount <= 8192, sprintf "ascii truncated to %d bytes (limit 8192)" byteCount)
    Assert.Contains("…[truncated ", truncated)

    // Mixed multi-byte + ASCII tail (each 日 is 3 bytes UTF-8).
    let jpPrefix = String.replicate 1500 "日本語"  // 13500 bytes
    let mixed = jpPrefix + String.replicate 2000 "A"
    let truncatedMixed = HubLog.truncateUtf8 mixed
    let mixedBytes = Encoding.UTF8.GetByteCount(truncatedMixed)
    Assert.True(mixedBytes <= 8192, sprintf "mixed truncated to %d bytes (limit 8192)" mixedBytes)
    Assert.Contains("…[truncated ", truncatedMixed)
    // Re-decoding as UTF-8 must succeed without replacement characters
    // — evidence the cut fell on a UTF-8 lead-byte boundary.
    let roundTrip =
        let bytes = Encoding.UTF8.GetBytes(truncatedMixed)
        Encoding.UTF8.GetString(bytes)
    Assert.Equal(truncatedMixed, roundTrip)

[<Fact>]
let ``no subscriber means thunk not invoked`` () =
    use bus = HubLog.create nullSink (settingsWith 4)
    let mutable invocations = 0
    for _ in 1 .. 1000 do
        HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () ->
            Interlocked.Increment(&invocations) |> ignore
            "should never be built")
    Assert.Equal(0, invocations)

[<Fact>]
let ``emit picks up AsyncLocal correlation id`` () =
    use bus = HubLog.create nullSink (settingsWith 4)
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t2" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected reason -> Assert.Fail(sprintf "attach rejected: %s" reason)
    | HubLog.Attached sub ->
        let cid = CorrelationId.CorrelationId "cid-under-scope"
        do
            use _scope = CorrelationId.withScope (Some cid)
            HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "with-scope")
        match drainOne sub.Reader 500 with
        | None -> Assert.Fail("no scoped entry received")
        | Some entry ->
            Assert.Equal<CorrelationId.CorrelationId option>(Some cid, entry.CorrelationId)

        // Second emit outside any scope — correlation should be None.
        HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "no-scope")
        match drainOne sub.Reader 500 with
        | None -> Assert.Fail("no bare entry received")
        | Some entry ->
            Assert.Equal<CorrelationId.CorrelationId option>(None, entry.CorrelationId)
        sub.Dispose()

// --- US5 preset tests (T057) ----------------------------------------

[<Fact>]
let ``preset bundles categories and floor`` () =
    match HubLog.resolveFilter [] None (Some "session-lifecycle") with
    | Result.Error e -> Assert.Fail(sprintf "resolve failed: %s" e)
    | Ok filter ->
        let expected =
            Set.ofList [
                HubLog.SessionManager
                HubLog.AdminChannel
                HubLog.ProxyInstall ]
        Assert.Equal<Set<HubLog.LogCategory>>(expected, filter.Categories.Value)
        Assert.Equal(HubLog.Info, filter.MinSeverity)
        Assert.Equal<string option>(Some "session-lifecycle", filter.PresetName)

[<Fact>]
let ``explicit categories override preset`` () =
    match HubLog.resolveFilter [ HubLog.SessionManager ] None (Some "admin-channel") with
    | Result.Error e -> Assert.Fail(sprintf "resolve failed: %s" e)
    | Ok filter ->
        let expected = Set.singleton HubLog.SessionManager
        Assert.Equal<Set<HubLog.LogCategory>>(expected, filter.Categories.Value)
        // Preset severity floor still wins when no explicit floor supplied.
        Assert.Equal(HubLog.Debug, filter.MinSeverity)
        // PresetName preserved for diagnostics (data-model §4).
        Assert.Equal<string option>(Some "admin-channel", filter.PresetName)

[<Fact>]
let ``preset name lookup is case insensitive`` () =
    for name in [ "Session-Lifecycle"; "SESSION-LIFECYCLE"; "session-lifecycle" ] do
        match HubLog.resolveFilter [] None (Some name) with
        | Result.Error e -> Assert.Fail(sprintf "%s failed: %s" name e)
        | Ok filter ->
            Assert.Equal<string option>(Some "session-lifecycle", filter.PresetName)

[<Fact>]
let ``unknown preset name rejected`` () =
    match HubLog.resolveFilter [] None (Some "verbose") with
    | Ok _ -> Assert.Fail("expected error for unknown preset")
    | Result.Error msg ->
        Assert.Contains("verbose", msg)
