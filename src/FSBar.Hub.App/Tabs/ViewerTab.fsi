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
    /// When `sessionState = Running`, composes `buildSceneHeadlessView`
    /// over the current `GameState` + `MapGrid` + `VizConfig` using the
    /// caller-owned `ViewState` (`viewStateRef`). If
    /// `viewStateRef.Value.AutoFit = true` the tab recomputes `Scale`
    /// to letterbox the map into the content rectangle and writes the
    /// result back into `viewStateRef` so subsequent scroll/drag events
    /// see the fit baseline. Otherwise placeholder panels render.
    ///
    /// The caller (hub `Program.fs`) mutates `viewStateRef` on mouse
    /// scroll (cursor-anchored zoom), left-drag (pan), and `R` keypress
    /// (reset to AutoFit).
    val render:
        sessionState: SessionManager.SessionState ->
        vizConfig: VizConfig ->
        viewStateRef: ViewState ref ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list
