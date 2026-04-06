namespace FSBar.Viz

open SkiaSharp
open FSBar.Client

/// Renders MapGrid data layers to SKBitmap images with color mapping.
module LayerRenderer =
    /// Render a map layer to a bitmap using the given color scheme.
    /// Results are cached; call invalidateCache to force re-render.
    val renderLayer: grid: MapGrid -> layer: LayerKind -> scheme: ColorScheme -> SKBitmap

    /// Invalidate cached bitmap for a specific layer kind.
    val invalidateCache: layer: LayerKind -> unit

    /// Invalidate all cached bitmaps.
    val invalidateAll: unit -> unit

    /// Returns (cacheHits, cacheMisses) since last reset.
    val cacheStats: unit -> int * int
