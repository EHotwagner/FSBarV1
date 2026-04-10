namespace FSBar.Viz

open SkiaViewer

/// Builds declarative Scene trees from game snapshots.
module SceneBuilder =
    /// Builds a complete Scene from a game snapshot, viz config, and view state.
    val buildScene: snapshot: GameSnapshot -> config: VizConfig -> viewState: ViewState -> Scene
