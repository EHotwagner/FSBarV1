module FSBar.Hub.Tests.CorrelationIdInterceptorTests

open System
open System.Threading
open System.Threading.Tasks
open Xunit
open FSBar.Hub

[<Fact>]
let ``auto assigns id when header absent`` () =
    let before = CorrelationId.current ()
    Assert.Equal<CorrelationId.CorrelationId option>(None, before)
    let cid = CorrelationId.generate ()
    do
        use _scope = CorrelationId.withScope (Some cid)
        let inside = CorrelationId.current ()
        Assert.Equal<CorrelationId.CorrelationId option>(Some cid, inside)
    let after = CorrelationId.current ()
    Assert.Equal<CorrelationId.CorrelationId option>(None, after)

[<Fact>]
let ``generate produces 32 hex chars`` () =
    let (CorrelationId.CorrelationId raw) = CorrelationId.generate ()
    Assert.Equal(32, raw.Length)
    Assert.All(raw, fun c ->
        Assert.True(Char.IsDigit(c) || (c >= 'a' && c <= 'f')))

[<Fact>]
let ``too long client header rejected`` () =
    let raw = String.replicate 65 "x"
    match CorrelationId.tryParseClientHeader raw with
    | Ok _ -> Assert.Fail("expected Error for 65-byte header")
    | Result.Error reason -> Assert.Contains("exceeds", reason)

[<Fact>]
let ``empty client header rejected`` () =
    match CorrelationId.tryParseClientHeader "" with
    | Ok _ -> Assert.Fail("expected Error for empty header")
    | Result.Error reason -> Assert.Contains("empty", reason)

[<Fact>]
let ``valid client header parsed`` () =
    match CorrelationId.tryParseClientHeader "my-id-42" with
    | Result.Error e -> Assert.Fail(sprintf "expected Ok: %s" e)
    | Ok (CorrelationId.CorrelationId raw) ->
        Assert.Equal("my-id-42", raw)

[<Fact>]
let ``background task inherits via explicit scope`` () =
    // AsyncLocal does NOT automatically flow through Task.Run if the
    // parent has already exited its scope — but within the scope,
    // `Task.Run` does capture the AsyncLocal value at time of capture.
    let cid = CorrelationId.generate ()
    let mutable observed : CorrelationId.CorrelationId option = None
    do
        use _scope = CorrelationId.withScope (Some cid)
        let task =
            Task.Run(fun () ->
                // Inside the scope, AsyncLocal flows.
                observed <- CorrelationId.current ())
        task.Wait()
    Assert.Equal<CorrelationId.CorrelationId option>(Some cid, observed)

[<Fact>]
let ``explicit scope restores prior on dispose`` () =
    let outer = CorrelationId.generate ()
    let inner = CorrelationId.generate ()
    do
        use _scopeOuter = CorrelationId.withScope (Some outer)
        Assert.Equal<CorrelationId.CorrelationId option>(Some outer, CorrelationId.current ())
        do
            use _scopeInner = CorrelationId.withScope (Some inner)
            Assert.Equal<CorrelationId.CorrelationId option>(Some inner, CorrelationId.current ())
        // After inner scope disposes, outer should be back.
        Assert.Equal<CorrelationId.CorrelationId option>(Some outer, CorrelationId.current ())
    Assert.Equal<CorrelationId.CorrelationId option>(None, CorrelationId.current ())

[<Fact>]
let ``header name is standard lower-case`` () =
    Assert.Equal("x-fsbar-correlation-id", CorrelationId.HeaderName)

[<Fact>]
let ``max client supplied bytes is 64`` () =
    Assert.Equal(64, CorrelationId.MaxClientSuppliedBytes)
