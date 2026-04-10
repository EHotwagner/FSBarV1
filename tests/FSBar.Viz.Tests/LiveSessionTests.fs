module FSBar.Viz.Tests.LiveSessionTests

open System
open Xunit
open FSBar.Client
open FSBar.Viz

[<Fact>]
let ``LiveSessionHandle type has expected members`` () =
    // Verify that LiveSessionHandle implements IDisposable and has the expected properties
    let t = typeof<LiveSessionHandle>
    Assert.True(typeof<IDisposable>.IsAssignableFrom(t), "LiveSessionHandle should implement IDisposable")
    let frameCountProp = t.GetProperty("FrameCount")
    Assert.NotNull(frameCountProp)
    Assert.Equal(typeof<int>, frameCountProp.PropertyType)
    let isRunningProp = t.GetProperty("IsRunning")
    Assert.NotNull(isRunningProp)
    Assert.Equal(typeof<bool>, isRunningProp.PropertyType)
    let lastErrorProp = t.GetProperty("LastError")
    Assert.NotNull(lastErrorProp)
    Assert.Equal(typeof<string option>, lastErrorProp.PropertyType)

[<Fact(Skip = "Requires running BAR engine and DISPLAY")>]
let ``start creates a running session`` () =
    // This test would require an actual engine
    ()

[<Fact(Skip = "Requires running BAR engine and DISPLAY")>]
let ``Dispose sets IsRunning to false`` () =
    // This test would require an actual engine
    ()
