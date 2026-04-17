namespace FSBar.Viz

open SkiaViewer
open FSBar.Client

/// Builds declarative Scene trees from game snapshots.
module SceneBuilder =
    /// Builds a complete Scene from a game snapshot, viz config, and view state.
    val buildScene: snapshot: GameSnapshot -> config: VizConfig -> viewState: ViewState -> Scene

    /// Builds a `Scene` for an embedded / headless viewer (feature
    /// 035-central-gui-hub R8). Converts a live `GameState` + optional
    /// `MapGrid` into a minimal `GameSnapshot` and calls `buildScene`
    /// with `VizDefaults.defaultViewState`. Unlike `GameViz.start`,
    /// this opens no window — the caller owns the `SkiaViewer.Window`
    /// and composes the returned scene into its own frame loop.
    ///
    /// When `map` is `None` a tiny synthetic 16x16 heightmap stand-in
    /// is used so early frames during session start-up render without
    /// crashing; replace once the real `MapGrid` is available.
    ///
    /// This is a stateless, pure-ish function (the pulse-phase clock
    /// remains module-level). Cheap enough to call every frame.
    val buildSceneHeadless: state: GameState -> map: MapGrid option -> config: VizConfig -> Scene

    /// Computes a clamped pulse alpha byte in [60, 220] from elapsed seconds and period.
    val computePulseAlpha: elapsed: float -> periodSeconds: float -> byte

    /// Resets the pulse clock to zero. Call on session start/stop.
    val resetPulsePhase: unit -> unit

    /// Advances the pulse clock by one FrameTick's delta seconds and recomputes
    /// the shared phase used by metal-spot markers. Call once per FrameTick.
    val updatePulsePhase: deltaSeconds: float -> unit
