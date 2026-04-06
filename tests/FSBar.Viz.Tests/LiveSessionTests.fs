module FSBar.Viz.Tests.LiveSessionTests

open Xunit
open Xunit.Abstractions
open FSBar.Client
open FSBar.Viz

/// LiveSession tests using the shared VizEngine fixture (live engine required).
[<Collection("VizEngine")>]
type LiveSessionTests(engine: VizEngineFixture, output: ITestOutputHelper) =

    [<Fact>]
    member _.``startWithClient creates running session with incrementing FrameCount`` () =
        use session = LiveSession.startWithClient engine.Client None
        Assert.True(session.IsRunning, "Session should be running after start")

        // Let the step loop run a few frames
        System.Threading.Thread.Sleep(2000)
        let count = session.FrameCount
        output.WriteLine($"FrameCount after 2s: {count}")
        Assert.True(count > 0, "FrameCount should increment")

    [<Fact>]
    member _.``Dispose stops the session cleanly`` () =
        let session = LiveSession.startWithClient engine.Client None
        System.Threading.Thread.Sleep(1000)
        Assert.True(session.IsRunning, "Should be running before dispose")

        (session :> System.IDisposable).Dispose()
        Assert.False(session.IsRunning, "Should not be running after dispose")
        output.WriteLine($"Final FrameCount: {session.FrameCount}")
