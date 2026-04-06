namespace FSBar.Viz

open System
open FSBar.Client

/// Offline preview session: renders saved map data and mock game states via SkiaViewer.
/// All interactive controls (pan, zoom, layer switching, overlay toggling) work during preview.
module PreviewSession =
    /// Start a viewer showing a static map with default overlays.
    val startWithMap: grid: MapGrid -> IDisposable

    /// Start a viewer showing a full game snapshot with all overlays.
    val startWithSnapshot: snapshot: GameSnapshot -> IDisposable

    /// Play back a sequence of snapshots at the specified game-fps. Viewer renders at 60fps.
    val startPlayback: frames: GameSnapshot seq -> gameFps': int -> IDisposable

    /// Stop any running preview session.
    val stop: unit -> unit
