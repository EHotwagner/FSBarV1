namespace FSBar.Viz

open FSBar.Client

/// Manages the lifecycle of a live visualization session connected to the BAR engine.
[<Sealed>]
type LiveSessionHandle =
    interface System.IDisposable
    member FrameCount: int
    member IsRunning: bool
    member LastError: string option

/// Engine-to-GameViz orchestration.
module LiveSession =
    val start: engineConfig: EngineConfig -> vizConfig: VizConfig option -> LiveSessionHandle
    val startWithClient: client: BarClient -> vizConfig: VizConfig option -> LiveSessionHandle
