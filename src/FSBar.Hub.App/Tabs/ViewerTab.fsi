namespace FSBar.Hub.App.Tabs

open SkiaViewer
open FSBar.Viz
open FSBar.Hub

/// Viewer tab (feature 035-central-gui-hub T030) — composes the embedded
/// live-game scene into the hub window when a session is `Running`.
/// Delegates entirely to `FSBar.Viz.SceneBuilder.buildSceneHeadless`; no
/// new glyph / layer logic here.
module ViewerTab =

    /// Renders the viewer content area.
    ///
    /// When `sessionState = Running`, composes `buildSceneHeadless` over
    /// the current `GameState` + `MapGrid` + `VizConfig`, offset into the
    /// content rectangle. Otherwise renders a placeholder panel.
    val render:
        sessionState: SessionManager.SessionState ->
        vizConfig: VizConfig ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list
