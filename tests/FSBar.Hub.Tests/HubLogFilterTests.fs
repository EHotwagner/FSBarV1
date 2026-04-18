module FSBar.Hub.Tests.HubLogFilterTests

open System
open System.Threading
open System.Threading.Channels
open Xunit
open FSBar.Hub

let private nullSink =
    { new HubEvents.IHubEventSink with
        member _.Publish(_) = () }

let private settings () = HubSettings.defaults

let private drainUpTo (reader: ChannelReader<HubLog.LogEntry>) (expected: int) (timeoutMs: int) =
    let collected = ResizeArray<HubLog.LogEntry>()
    let sw = System.Diagnostics.Stopwatch.StartNew()
    while collected.Count < expected && sw.ElapsedMilliseconds < int64 timeoutMs do
        let mutable entry = Unchecked.defaultof<HubLog.LogEntry>
        if reader.TryRead(&entry) then collected.Add(entry)
        else Thread.Sleep(2)
    collected |> List.ofSeq

// T036 — category whitelist excludes off-list categories.
[<Fact>]
let ``category whitelist excludes others`` () =
    use bus = HubLog.create nullSink settings
    let filter =
        { HubLog.defaultFilter with
            Categories = Some (Set.singleton HubLog.AdminChannel) }
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t" filter cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached sub ->
        try
            HubLog.emit bus HubLog.AdminChannel HubLog.Info None None (fun () -> "admin")
            HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "session")
            let entries = drainUpTo sub.Reader 2 300
            Assert.Equal(1, entries.Length)
            Assert.Equal(HubLog.AdminChannel, entries.[0].Category)
            Assert.Equal<string>("admin", entries.[0].Message)
        finally
            sub.Dispose()

[<Fact>]
let ``severity floor drops lower`` () =
    use bus = HubLog.create nullSink settings
    let filter = { HubLog.defaultFilter with MinSeverity = HubLog.Warning }
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t" filter cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached sub ->
        try
            HubLog.emit bus HubLog.AdminChannel HubLog.Debug None None (fun () -> "debug")
            HubLog.emit bus HubLog.AdminChannel HubLog.Info None None (fun () -> "info")
            HubLog.emit bus HubLog.AdminChannel HubLog.Warning None None (fun () -> "warn")
            HubLog.emit bus HubLog.AdminChannel HubLog.Error None None (fun () -> "err")
            let entries = drainUpTo sub.Reader 4 300
            Assert.Equal(2, entries.Length)
            Assert.Equal(HubLog.Warning, entries.[0].Severity)
            Assert.Equal(HubLog.Error, entries.[1].Severity)
        finally
            sub.Dispose()

[<Fact>]
let ``filter mutation applies on next entry`` () =
    use bus = HubLog.create nullSink settings
    let initial =
        { HubLog.defaultFilter with
            Categories = Some (Set.singleton HubLog.AdminChannel) }
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t" initial cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached sub ->
        try
            // Under initial filter, only AdminChannel entries are delivered.
            HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "session-1")
            HubLog.emit bus HubLog.AdminChannel HubLog.Info None None (fun () -> "admin-1")
            let firstWave = drainUpTo sub.Reader 1 300
            Assert.Equal(1, firstWave.Length)
            Assert.Equal<string>("admin-1", firstWave.[0].Message)

            // Expand the filter to both categories.
            let updated =
                { HubLog.defaultFilter with
                    Categories =
                        Some (Set.ofList [ HubLog.AdminChannel; HubLog.SessionManager ])
                    MinSeverity = HubLog.Debug }
            match HubLog.updateFilter bus sub.Id updated with
            | Result.Error e -> Assert.Fail(sprintf "updateFilter failed: %s" e)
            | Ok () -> ()

            HubLog.emit bus HubLog.SessionManager HubLog.Info None None (fun () -> "session-2")
            HubLog.emit bus HubLog.AdminChannel HubLog.Info None None (fun () -> "admin-2")
            let secondWave = drainUpTo sub.Reader 2 300
            Assert.Equal(2, secondWave.Length)
            Assert.Contains(secondWave, fun e -> e.Message = "session-2")
            Assert.Contains(secondWave, fun e -> e.Message = "admin-2")
        finally
            sub.Dispose()

[<Fact>]
let ``update filter rejects detached subscriber`` () =
    use bus = HubLog.create nullSink settings
    let cts = new CancellationTokenSource()
    match HubLog.attach bus "t" HubLog.defaultFilter cts.Token with
    | HubLog.Rejected _ -> Assert.Fail("attach rejected")
    | HubLog.Attached sub ->
        sub.Dispose()
        match HubLog.updateFilter bus sub.Id HubLog.defaultFilter with
        | Ok _ -> Assert.Fail("expected error after detach")
        | Result.Error msg -> Assert.Contains("detached", msg)
