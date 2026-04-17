namespace FSBar.Viz

open SkiaViewer

/// Builds declarative Scene trees from game snapshots.
module SceneBuilder =
    /// Builds a complete Scene from a game snapshot, viz config, and view state.
    val buildScene: snapshot: GameSnapshot -> config: VizConfig -> viewState: ViewState -> Scene

    /// Computes a clamped pulse alpha byte in [60, 220] from elapsed seconds and period.
    val computePulseAlpha: elapsed: float -> periodSeconds: float -> byte

    /// Resets the pulse clock to zero. Call on session start/stop.
    val resetPulsePhase: unit -> unit

    /// Advances the pulse clock by one FrameTick's delta seconds and recomputes
    /// the shared phase used by metal-spot markers. Call once per FrameTick.
    val updatePulsePhase: deltaSeconds: float -> unit
