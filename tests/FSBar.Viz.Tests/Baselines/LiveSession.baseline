namespace FSBar.Viz

open System
open FSBar.Client

/// Handle to a running live visualization session.
/// Dispose to stop the engine step loop and close the visualization.
[<Sealed>]
type LiveSessionHandle =
    interface IDisposable
    /// Number of engine frames processed so far.
    member FrameCount: int
    /// Whether the step loop is still running.
    member IsRunning: bool
    /// Last error message, if the session stopped due to an error.
    member LastError: string option

/// Orchestrates a live visualization session: connects a headless BAR engine
/// to the GameViz rendering pipeline via a background step thread.
module LiveSession =
    /// Start a live session that launches an engine, connects a client,
    /// opens the GameViz window, and runs the step loop on a background thread.
    /// Dispose the returned handle to stop everything.
    val start: engineConfig: EngineConfig -> vizConfig: VizConfig option -> LiveSessionHandle

    /// Attach to an already-connected BarClient and start the visualization.
    /// The client lifecycle is NOT managed — caller is responsible for stopping it.
    /// Dispose the returned handle to stop the step loop and viz window.
    val startWithClient: client: BarClient -> vizConfig: VizConfig option -> LiveSessionHandle
