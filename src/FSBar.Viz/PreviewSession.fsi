namespace FSBar.Viz

open FSBar.Client

/// Offline preview and playback via SkiaViewer.
module PreviewSession =
    /// Opens a viewer showing the given map grid with the default layer.
    val startWithMap: grid: MapGrid -> System.IDisposable
    /// Opens a viewer showing a single static snapshot.
    val startWithSnapshot: snapshot: GameSnapshot -> System.IDisposable
    /// Opens a viewer playing back a sequence of snapshots at the given game FPS, looping.
    val startPlayback: frames: GameSnapshot seq -> gameFps: int -> System.IDisposable
    /// Stops the current preview session.
    val stop: unit -> unit
