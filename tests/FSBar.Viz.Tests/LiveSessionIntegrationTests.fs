module FSBar.Viz.Tests.LiveSessionIntegrationTests

open System
open Xunit
open FSBar.Client
open FSBar.Viz

[<Fact(Skip = "Requires running BAR engine")>]
let ``start with EngineConfig launches engine and opens viz`` () =
    // Would need: valid EngineConfig, BAR engine installed, DISPLAY available
    // let config : EngineConfig = { ... }
    // use handle = LiveSession.start config None
    // Assert.True(handle.IsRunning)
    // System.Threading.Thread.Sleep(2000)
    // Assert.True(handle.FrameCount > 0)
    ()

[<Fact(Skip = "Requires running BAR engine")>]
let ``startWithClient attaches to existing client`` () =
    // Would need: a connected BarClient, DISPLAY available
    // use handle = LiveSession.startWithClient client None
    // Assert.True(handle.IsRunning)
    ()

[<Fact(Skip = "Requires running BAR engine")>]
let ``Dispose stops engine and closes viz`` () =
    // Would need: valid EngineConfig, BAR engine installed, DISPLAY available
    // let config : EngineConfig = { ... }
    // let handle = LiveSession.start config None
    // (handle :> IDisposable).Dispose()
    // Assert.False(handle.IsRunning)
    ()

[<Fact(Skip = "Requires running BAR engine")>]
let ``FrameCount increments during gameplay`` () =
    // Would need: valid EngineConfig, BAR engine installed, DISPLAY available
    // let config : EngineConfig = { ... }
    // use handle = LiveSession.start config None
    // System.Threading.Thread.Sleep(5000)
    // Assert.True(handle.FrameCount > 0, $"Expected frames > 0, got {handle.FrameCount}")
    ()

[<Fact(Skip = "Requires running BAR engine")>]
let ``LastError is None during normal operation`` () =
    // Would need: valid EngineConfig, BAR engine installed, DISPLAY available
    // use handle = LiveSession.start config None
    // System.Threading.Thread.Sleep(2000)
    // Assert.True(handle.LastError.IsNone)
    ()
