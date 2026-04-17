namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer
open FSBar.Hub

/// Persistent bottom-edge bar (feature 035-central-gui-hub T016).
///
/// Shows the current session state as text on the left and three
/// controls on the right: speed slider, pause toggle, end-session
/// button. Height is 24 px; spans the full window width minus the
/// TabBar column.
///
/// Phase-3 status: controls render and hit-test; wiring to
/// `SessionManager.SetSpeed` / `SetPaused` / `End` lands in T031
/// (SetupTab wiring) so the hub already has a flow to drive them.
module StatusBar =

    /// Height in pixels.
    val Height: float32

    /// Click actions the status-bar hit-test can surface.
    [<RequireQualifiedAccess>]
    type StatusBarAction =
        | SetSpeed of speed: float32
        | TogglePause
        | EndSession

    /// Render state a chrome draw depends on.
    type StatusBarState = {
        SessionState: SessionManager.SessionState
        Paused: bool
        Speed: float32
    }

    /// Render the status bar. Caller places this on top of the main
    /// tab content layer.
    val render:
        state: StatusBarState ->
        windowWidth: int ->
        windowHeight: int ->
            Element list

    /// Hit-test a mouse click at `(x, y)` against the controls on the
    /// right side of the bar. Returns `None` when the click lands
    /// outside the bar or on dead space.
    val handleMouse:
        state: StatusBarState ->
        x: float32 ->
        y: float32 ->
        windowWidth: int ->
        windowHeight: int ->
            StatusBarAction option
