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

    /// Opens a viewer on the first map in <c>supportedMaps</c> (or
    /// <c>initialMapName</c> when provided and found in the list) loaded via
    /// <c>MapCacheFile.read</c>. Installs in-viewer next/prev keybindings
    /// (<c>]</c>/<c>.</c> to advance, <c>[</c>/<c>,</c> to retreat) that cycle
    /// through <c>supportedMaps</c> in the order supplied, wrapping at the ends.
    ///
    /// On a <c>MapCacheFile.LoadError</c> for any map, the viewer displays a
    /// formatted error banner (via <c>MapCacheFile.formatLoadError</c>) over
    /// the last successfully-loaded scene and does not advance the index;
    /// it never crashes or shows a blank window. <c>supportedMaps</c> must be
    /// non-empty — otherwise raises <c>System.ArgumentException</c>.
    val startWithCachedMaps:
        supportedMaps: MapCacheFile.SupportedMap list
        -> initialMapName: string option
        -> System.IDisposable

    /// Pure helper exposed for unit testing: advance a cycling index by
    /// <c>direction</c> (+1 forward, -1 backward), wrapping at the ends of
    /// an <c>n</c>-element list. <c>current</c> must satisfy
    /// <c>0 &lt;= current &lt; n</c>.
    val advanceCycleIndex: n: int -> direction: int -> current: int -> int

    /// Stops the current preview session.
    val stop: unit -> unit
