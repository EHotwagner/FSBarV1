module FSBar.Viz.Tests.PreviewSessionCyclingTests

open System
open Xunit
open FSBar.Viz

[<Fact>]
let ``advance wraps past end to zero`` () =
    Assert.Equal(0, PreviewSession.advanceCycleIndex 3 1 2)

[<Fact>]
let ``retreat wraps from zero to last`` () =
    Assert.Equal(2, PreviewSession.advanceCycleIndex 3 -1 0)

[<Fact>]
let ``single-map list stays on the same index in both directions`` () =
    Assert.Equal(0, PreviewSession.advanceCycleIndex 1 1 0)
    Assert.Equal(0, PreviewSession.advanceCycleIndex 1 -1 0)

[<Fact>]
let ``start with empty supportedMaps throws ArgumentException`` () =
    Assert.Throws<ArgumentException>(fun () ->
        PreviewSession.startWithCachedMaps [] None |> ignore) |> ignore
