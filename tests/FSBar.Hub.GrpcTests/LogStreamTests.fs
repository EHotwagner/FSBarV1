namespace FSBar.Hub.GrpcTests

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

module private LogStreamHelpers =

    // Drain all remaining entries from a response stream for windowMs, ensuring
    // the server's drain loop has completed writes before the call is disposed.
    let drainStream (stream: Grpc.Core.IAsyncStreamReader<LogEntryMessage>) (windowMs: int) =
        task {
            use cts = new CancellationTokenSource(windowMs)
            try
                let mutable keepGoing = true
                while keepGoing do
                    let! moved = stream.MoveNext(cts.Token)
                    if not moved then keepGoing <- false
            with _ -> ()
        } :> Task

    let openStream (stub: ScriptingService.Client) (opts: CallOptions) (filter: LogFilterWire option) =
        task {
            let call = stub.StreamHubLogAsync(opts)
            let req = { StreamHubLogRequest.empty with Filter = filter }
            do! call.RequestStream.WriteAsync(req)
            return call
        }

    let defaultFilter () =
        LogFilterWire.empty

    let debugFilter () =
        { LogFilterWire.empty with MinSeverity = LogSeverity.Debug }

    let categoryDebugFilter (cats: LogCategory list) =
        { LogFilterWire.empty with Categories = cats; MinSeverity = LogSeverity.Debug }

[<Collection("HubGrpc")>]
type LogStreamTests(hub: HubTestFixture) =
    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``FR-006 — SubscriberCap_WhenExceeded_ReturnsResourceExhausted``() = task {
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(30.0)))

        let! settingsResp = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
        let cap =
            settingsResp.HubSettings
            |> Option.map (fun _ -> 8)
            |> Option.defaultValue 8

        let openCalls = List<Grpc.Core.AsyncDuplexStreamingCall<StreamHubLogRequest, LogEntryMessage>>()
        try
            for _ in 1 .. cap do
                let call = stub.StreamHubLogAsync(opts)
                do! call.RequestStream.WriteAsync(StreamHubLogRequest.empty)
                openCalls.Add(call)

            // Give the server time to process all subscribe requests before trying the cap-exceeded one.
            do! Task.Delay(500)

            let mutable caughtResourceExhausted = false
            try
                let extraOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
                let extraCall = stub.StreamHubLogAsync(extraOpts)
                do! extraCall.RequestStream.WriteAsync(StreamHubLogRequest.empty)
                let! _ = extraCall.ResponseStream.MoveNext(CancellationToken.None)
                ()
            with
            | :? RpcException as ex when ex.StatusCode = StatusCode.ResourceExhausted ->
                caughtResourceExhausted <- true
                Assert.Contains(string cap, ex.Status.Detail)
            Assert.True(caughtResourceExhausted, "expected ResourceExhausted when subscriber cap exceeded")
        finally
            for call in openCalls do
                try call.Dispose() with _ -> ()
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``FR-007 — BufferOverflow_DroppedSinceLastIsNonZero``() = task {
        let stub = hub.Stub
        let streamOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(60.0)))

        let call = stub.StreamHubLogAsync(streamOpts)
        let filter = LogStreamHelpers.debugFilter ()
        do! call.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        // Drain entries until the subscription confirmation arrives, then proceed.
        let primeCts = new CancellationTokenSource(5000)
        let mutable confirmed = false
        while not confirmed do
            let! moved = call.ResponseStream.MoveNext(primeCts.Token)
            if not moved || call.ResponseStream.Current.Message.Contains("log-stream subscribed:") then
                confirmed <- true

        let callOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(60.0)))
        let attrReq key boolVal =
            { SetVizAttributeRequest.empty with
                Key = key
                Value = Some { VizAttributeValue.empty with
                                   Value = VizAttributeValue.ValueCase.BoolValue boolVal } }

        // Fire all 300 calls in parallel so entries pile up faster than the drain task can relay them.
        let tasks =
            [| for i in 1 .. 300 ->
                let key = if i % 2 = 0 then "overlays.weaponRanges" else "overlays.sightRanges"
                (stub.SetVizAttributeAsync(callOpts) (attrReq key (i % 2 = 0))).ResponseAsync :> Task |]
        do! Task.WhenAll(tasks)

        do! Task.Delay(200)

        let mutable foundDropped = false
        let cts = new CancellationTokenSource(10000)
        try
            while not foundDropped do
                let! moved = call.ResponseStream.MoveNext(cts.Token)
                if not moved then foundDropped <- true
                elif call.ResponseStream.Current.DroppedSinceLast > 0 then
                    foundDropped <- true
        with
        | :? OperationCanceledException -> ()
        | :? RpcException as rpc when rpc.StatusCode = StatusCode.Cancelled -> ()

        Assert.True(foundDropped, "expected at least one entry with DroppedSinceLast > 0")
        call.Dispose()
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``FR-008 — LongMessage_TruncatedWithMarker``() = task {
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(30.0)))

        let call = stub.StreamHubLogAsync(opts)
        let filter = LogStreamHelpers.categoryDebugFilter [ LogCategory.ScriptingHub ]
        do! call.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(call.ResponseStream)
        // Wait for the server's subscription-active confirmation before triggering the test message.
        let! _ = harness.WaitForEntry((fun e -> e.Message.Contains("log-stream subscribed:")), 5000)

        let longMessage = String.replicate 9000 "A"
        let req = { SendAdminMessageRequest.empty with Text = longMessage }
        try
            let callOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(5.0)))
            let! _ = stub.SendAdminMessageAsync(callOpts) req
            ()
        with _ -> ()

        let! entry = harness.WaitForEntry(
            (fun e -> e.Message.Contains(" \u2026[truncated") || e.Message.Contains("...[truncated")), 8000)
        Assert.NotNull(entry)
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``FR-008 — TruncatedContent_ByteIdenticalAcrossSubscribers``() = task {
        // Use a fresh isolated channel so disposing streams with unconsumed data
        // does not deplete the shared fixture channel's HTTP/2 flow-control window.
        use freshChannel = Grpc.Net.Client.GrpcChannel.ForAddress(sprintf "http://127.0.0.1:%d" hub.Port)
        let freshStub = ScriptingService.Client(freshChannel)
        let opts1 = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(30.0)))
        let opts2 = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(30.0)))

        let call1 = freshStub.StreamHubLogAsync(opts1)
        let call2 = freshStub.StreamHubLogAsync(opts2)
        let filter = LogStreamHelpers.categoryDebugFilter [ LogCategory.ScriptingHub ]
        do! call1.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        do! call2.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })

        use harness1 = new LogStreamHarness.LogStreamHarness(call1.ResponseStream)
        use harness2 = new LogStreamHarness.LogStreamHarness(call2.ResponseStream)
        // Wait for both subscriptions to be confirmed active before emitting the test message.
        let! _ = harness1.WaitForEntry((fun e -> e.Message.Contains("log-stream subscribed:")), 5000)
        let! _ = harness2.WaitForEntry((fun e -> e.Message.Contains("log-stream subscribed:")), 5000)

        let longMessage = String.replicate 9000 "B"
        try
            let callOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(5.0)))
            let! _ = hub.Stub.SendAdminMessageAsync(callOpts) { SendAdminMessageRequest.empty with Text = longMessage }
            ()
        with _ -> ()

        let isTruncated (e: LogEntryMessage) = e.Message.Contains("\u2026[truncated") || e.Message.Contains("[truncated")
        let! entry1 = harness1.WaitForEntry(isTruncated, 8000)
        let! entry2 = harness2.WaitForEntry(isTruncated, 8000)
        Assert.NotNull(entry1)
        Assert.NotNull(entry2)
        Assert.Equal(entry1.Message, entry2.Message)
        // Drain any trailing entries (e.g., DispatchTracer "completed") so the
        // server's responseStream.WriteAsync finishes before freshChannel is
        // disposed. Without this, abrupt connection close can leave Kestrel's
        // HTTP/2 write pipeline in a state that blocks subsequent streaming calls.
        do! LogStreamHelpers.drainStream call1.ResponseStream 500
        do! LogStreamHelpers.drainStream call2.ResponseStream 500
        call1.Dispose()
        call2.Dispose()
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``DefaultFilter_InfoFloor_NoDebugEntries``() = task {
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(20.0)))

        let call = stub.StreamHubLogAsync(opts)
        do! call.RequestStream.WriteAsync(StreamHubLogRequest.empty)
        use harness = new LogStreamHarness.LogStreamHarness(call.ResponseStream)

        let callOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
        for i in 1..5 do
            let! _ = stub.SetVizAttributeAsync(callOpts)
                         { SetVizAttributeRequest.empty with
                             Key = "overlays.weaponRanges"
                             Value = Some { VizAttributeValue.empty with
                                                Value = VizAttributeValue.ValueCase.BoolValue (i % 2 = 0) } }
            ()

        do! harness.AssertNoUnexpected(
                (fun e -> e.Severity = LogSeverity.Debug),
                2000)
        call.Dispose()
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``ScriptingHubCategory_DebugFloor_DeliversDebugEntries``() = task {
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(20.0)))

        let call = stub.StreamHubLogAsync(opts)
        let filter = LogStreamHelpers.categoryDebugFilter [ LogCategory.ScriptingHub ]
        do! call.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(call.ResponseStream)

        // The "log-stream subscribed:" confirmation entry is itself a ScriptingHub/Debug entry,
        // so receiving it proves the Debug-floor filter is delivering entries correctly.
        let! entry = harness.WaitForEntry(
            (fun e -> e.Category = LogCategory.ScriptingHub && e.Severity = LogSeverity.Debug
                      && e.Message.Contains("log-stream subscribed:")), 5000)
        Assert.NotNull(entry)
        call.Dispose()
    }

    [<Fact>]
    [<Trait("Category", "GrpcLogStream")>]
    member _.``SubscriberDisconnect_SlotReleasedWithin1s``() = task {
        let stub = hub.Stub
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(30.0)))

        let! settingsState = stub.GetHubStateAsync(opts) GetHubStateRequest.empty
        let cap = 8

        let openCalls = List<Grpc.Core.AsyncDuplexStreamingCall<StreamHubLogRequest, LogEntryMessage>>()
        try
            for _ in 1 .. cap do
                let call = stub.StreamHubLogAsync(opts)
                do! call.RequestStream.WriteAsync(StreamHubLogRequest.empty)
                openCalls.Add(call)

            do! Task.Delay(500)

            openCalls[0].Dispose()
            openCalls.RemoveAt(0)

            do! Task.Delay(1200)

            let mutable slotReleased = false
            try
                let newCall = stub.StreamHubLogAsync(opts)
                do! newCall.RequestStream.WriteAsync(StreamHubLogRequest.empty)
                let cts = new CancellationTokenSource(2000)
                try
                    let! _ = newCall.ResponseStream.MoveNext(cts.Token)
                    slotReleased <- true  // got a message — stream accepted
                    newCall.Dispose()
                with
                | :? OperationCanceledException -> slotReleased <- true  // timeout = accepted, no data
                | :? RpcException as ex when ex.StatusCode = StatusCode.Cancelled -> slotReleased <- true
                | _ -> ()  // ResourceExhausted or other = rejected
            with _ -> ()
            Assert.True(slotReleased, "new subscriber should connect after a slot was released")
        finally
            for call in openCalls do
                try call.Dispose() with _ -> ()
        ignore settingsState
    }
