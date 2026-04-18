namespace FSBar.Hub.GrpcTests

open System
open System.Threading.Tasks
open Grpc.Core
open Fsbar.Hub.Scripting.V1
open Xunit
open FSBar.Hub.GrpcTests.HubTestFixture

[<Collection("HubGrpc")>]
type CorrelationIdTests(hub: HubTestFixture) =

    let stub () = hub.Stub
    let headerName = "x-fsbar-correlation-id"

    interface IClassFixture<HubTestFixture>

    [<Fact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-018 — CorrelationId_EchoedInResponseTrailer``() = task {
        let correlationId = Guid.NewGuid().ToString("N")
        let headers = Metadata()
        headers.Add(headerName, correlationId)
        let callOpts = CallOptions(
            deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)),
            headers = headers)
        let call = stub().GetHubStateAsync(callOpts) GetHubStateRequest.empty
        let! _ = call
        let trailers = call.GetTrailers()
        let echoed =
            trailers
            |> Seq.tryFind (fun e -> String.Equals(e.Key, headerName, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun e -> e.Value)
        Assert.Equal(Some correlationId, echoed)
    }

    [<Fact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-018 — CorrelationId_GeneratedWhenAbsent_PresentInTrailer``() = task {
        let callOpts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))
        let call = stub().GetHubStateAsync(callOpts) GetHubStateRequest.empty
        let! _ = call
        let trailers = call.GetTrailers()
        let echoed =
            trailers
            |> Seq.tryFind (fun e -> String.Equals(e.Key, headerName, StringComparison.OrdinalIgnoreCase))
            |> Option.map (fun e -> e.Value)
        Assert.True(echoed.IsSome, "expected correlation ID in response trailer even when not provided in request")
        Assert.False(String.IsNullOrEmpty(echoed.Value), "correlation ID in trailer should be non-empty")
    }

    [<Fact>]
    [<Trait("Category", "GrpcAdmin")>]
    member _.``FR-017 — SendAdminMessage_EmptyString_RejectedBeforeEngine``() = task {
        let stub = stub()
        let opts = CallOptions(deadline = Nullable(DateTime.UtcNow.AddSeconds(10.0)))

        let logCall = stub.StreamHubLogAsync(opts)
        let filter = { LogFilterWire.empty with
                          Categories = [ LogCategory.AdminChannel ]
                          MinSeverity = LogSeverity.Debug }
        do! logCall.RequestStream.WriteAsync({ StreamHubLogRequest.empty with Filter = Some filter })
        use harness = new LogStreamHarness.LogStreamHarness(logCall.ResponseStream)

        let! resp = stub.SendAdminMessageAsync(opts) { SendAdminMessageRequest.empty with Text = "" }
        match resp.Result with
        | Some r when r.Outcome = AdminSubmitResult.Outcome.Rejected ->
            Assert.False(String.IsNullOrEmpty(r.Reason), "expected non-empty rejection reason")
        | other -> Assert.Fail(sprintf "expected Rejected result for empty message, got: %A" other)

        do! harness.AssertNoUnexpected(
                (fun e -> e.Category = LogCategory.AdminChannel && e.Message.Contains("SAYMESSAGE")),
                1000)
        logCall.Dispose()
    }
