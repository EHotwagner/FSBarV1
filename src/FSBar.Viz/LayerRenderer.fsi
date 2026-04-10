namespace FSBar.Viz

open SkiaSharp
open FSBar.Client

/// Renders map data layers to SKBitmap with caching.
module LayerRenderer =
    /// Renders a map layer to a bitmap using the given color scheme.
    val renderLayer: grid: MapGrid -> layer: LayerKind -> scheme: ColorScheme -> SKBitmap
    /// Invalidates the cached bitmap for a specific layer.
    val invalidateCache: layer: LayerKind -> unit
    /// Invalidates all cached bitmaps.
    val invalidateAll: unit -> unit
    /// Returns (hits, misses) cache statistics.
    val cacheStats: unit -> int * int
