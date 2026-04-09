namespace FSBar.Client

/// <summary>
/// Elmo-coordinate query functions for <see cref="T:FSBar.Client.MapGrid"/> data.
/// All functions accept world-space elmo coordinates and handle the conversion to grid indices internally.
/// </summary>
module MapQuery =

    /// <summary>Queries the terrain height at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing height data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok height</c> if within bounds, or <c>Error message</c> if out of bounds.</returns>
    val heightAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// <summary>Queries the terrain slope at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing slope data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok slope</c> (0.0 = flat, 1.0 = vertical) if within bounds, or <c>Error message</c> if out of bounds.</returns>
    val slopeAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// <summary>Queries the terrain classification at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing height and slope data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok terrain</c> with the <see cref="T:FSBar.Client.Terrain"/> classification, or <c>Error message</c> if out of bounds.</returns>
    val terrainAtElmo: grid: MapGrid -> x: int -> z: int -> Result<Terrain, string>

    /// <summary>
    /// Extracts a rectangular sub-region of the heightmap defined by elmo coordinate bounds.
    /// Coordinates are clamped to the valid grid range.
    /// </summary>
    /// <param name="grid">The map grid containing height data.</param>
    /// <param name="x1">Left edge X coordinate in elmos.</param>
    /// <param name="z1">Top edge Z coordinate in elmos.</param>
    /// <param name="x2">Right edge X coordinate in elmos.</param>
    /// <param name="z2">Bottom edge Z coordinate in elmos.</param>
    /// <returns><c>Ok subRegion</c> as a 2D float32 array, or <c>Error message</c> if the region has zero or negative size.</returns>
    val heightSubRegion: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> Result<float32[,], string>

    /// <summary>
    /// Finds all cells in a rectangular region where the resource value exceeds the given threshold.
    /// Results are sorted by resource value in descending order (richest first).
    /// </summary>
    /// <param name="grid">The map grid containing resource data.</param>
    /// <param name="x1">Left edge X coordinate in elmos.</param>
    /// <param name="z1">Top edge Z coordinate in elmos.</param>
    /// <param name="x2">Right edge X coordinate in elmos.</param>
    /// <param name="z2">Bottom edge Z coordinate in elmos.</param>
    /// <param name="threshold">Minimum resource value (exclusive) to include in results.</param>
    /// <returns>A list of (gridX, gridZ, resourceValue) tuples sorted by value descending.</returns>
    /// Finds the nearest metal spot to the given world position.
    /// Returns None if the spots array is empty.
    val nearestMetalSpot: spots: (float32 * float32 * float32 * float32) array -> position: float32 * float32 * float32 -> (float32 * float32 * float32 * float32) option

    val resourceHotspots: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> threshold: int -> (int * int * int) list

    /// <summary>Converts elmo (world) coordinates to heightmap grid indices by dividing by 8.</summary>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns>A tuple (gridX, gridZ) of heightmap grid indices.</returns>
    val elmoToGrid: x: int -> z: int -> int * int

    /// <summary>Converts heightmap grid indices to elmo (world) coordinates by multiplying by 8.</summary>
    /// <param name="x">X grid index.</param>
    /// <param name="z">Z grid index.</param>
    /// <returns>A tuple (elmoX, elmoZ) of world coordinates.</returns>
    val gridToElmo: x: int -> z: int -> int * int
