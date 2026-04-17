namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer

/// The six hub tabs, ordered top-to-bottom on the sidebar.
/// Values are serialised via their tag names in `HubSettings`
/// (Phase 3 extension) so renaming these later is a schema change.
[<RequireQualifiedAccess>]
type HubTab =
    | Setup
    | Viewer
    | Encyclopedia
    | Configurator
    | Settings
    | Grpc

/// Persistent sidebar chrome (feature 035-central-gui-hub T015).
///
/// A 56-pixel-wide column on the left edge of the hub window that
/// lists every tab, highlights the currently-active one, and routes
/// click events to a tab-switch action. Purely rendering-side; owns
/// no state beyond the public `HubTab` enum.
module TabBar =

    /// Width in pixels — the content area starts at `x = Width`.
    val Width: float32

    /// Pixel Y range for a given tab index (0..5). Used by both
    /// `render` for drawing and `handleMouse` for hit-testing.
    val tabBounds: tabIndex: int -> float32 * float32

    /// Maps a tab to a human-readable label shown in the sidebar.
    val label: tab: HubTab -> string

    /// Produces the `Scene.Element` list that paints the sidebar
    /// against the given window height.
    val render:
        active: HubTab ->
        windowHeight: int ->
            Element list

    /// Hit-tests a mouse click at `(x, y)` against the tab strip.
    /// Returns `Some tab` when the click lands on a tab, else `None`
    /// (either because the click was outside the sidebar column or
    /// between rows in dead space).
    val handleMouse:
        x: float32 ->
        y: float32 ->
            HubTab option
