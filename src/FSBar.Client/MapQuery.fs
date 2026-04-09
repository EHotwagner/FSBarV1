namespace FSBar.Client

/// <summary>
/// Elmo-coordinate query functions for <see cref="T:FSBar.Client.MapGrid"/> data.
/// All functions accept world-space elmo coordinates and handle the conversion to grid indices internally.
/// </summary>
module MapQuery =

    /// <summary>Converts elmo (world) coordinates to heightmap grid indices by dividing by 8.</summary>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns>A tuple (gridX, gridZ) of heightmap grid indices.</returns>
    let elmoToGrid (x: int) (z: int) : int * int =
        (x / 8, z / 8)

    /// <summary>Converts heightmap grid indices to elmo (world) coordinates by multiplying by 8.</summary>
    /// <param name="x">X grid index.</param>
    /// <param name="z">Z grid index.</param>
    /// <returns>A tuple (elmoX, elmoZ) of world coordinates.</returns>
    let gridToElmo (x: int) (z: int) : int * int =
        (x * 8, z * 8)

    let boundsCheck (grid: MapGrid) (gx: int) (gz: int) (layerName: string) =
        let maxX = Array2D.length1 grid.HeightMap - 1
        let maxZ = Array2D.length2 grid.HeightMap - 1
        if gx < 0 || gx > maxX || gz < 0 || gz > maxZ then
            Error $"Out of bounds: elmo ({gx * 8}, {gz * 8}) → grid ({gx}, {gz}) outside {layerName} range [0..{maxX}, 0..{maxZ}]"
        else
            Ok ()

    /// <summary>Queries the terrain height at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing height data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok height</c> if within bounds, or <c>Error message</c> if out of bounds.</returns>
    let heightAtElmo (grid: MapGrid) (x: int) (z: int) : Result<float32, string> =
        let gx, gz = elmoToGrid x z
        match boundsCheck grid gx gz "HeightMap" with
        | Error e -> Error e
        | Ok () -> Ok grid.HeightMap.[gx, gz]

    /// <summary>Queries the terrain slope at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing slope data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok slope</c> (0.0 = flat, 1.0 = vertical) if within bounds, or <c>Error message</c> if out of bounds.</returns>
    let slopeAtElmo (grid: MapGrid) (x: int) (z: int) : Result<float32, string> =
        let sx, sz = x / 16, z / 16
        let maxX = Array2D.length1 grid.SlopeMap - 1
        let maxZ = Array2D.length2 grid.SlopeMap - 1
        if sx < 0 || sx > maxX || sz < 0 || sz > maxZ then
            Error $"Out of bounds: elmo ({x}, {z}) → slope grid ({sx}, {sz}) outside SlopeMap range [0..{maxX}, 0..{maxZ}]"
        else
            Ok grid.SlopeMap.[sx, sz]

    /// <summary>Queries the terrain classification at the given elmo coordinates.</summary>
    /// <param name="grid">The map grid containing height and slope data.</param>
    /// <param name="x">X coordinate in elmos.</param>
    /// <param name="z">Z coordinate in elmos.</param>
    /// <returns><c>Ok terrain</c> with the <see cref="T:FSBar.Client.Terrain"/> classification, or <c>Error message</c> if out of bounds.</returns>
    let terrainAtElmo (grid: MapGrid) (x: int) (z: int) : Result<Terrain, string> =
        let gx, gz = elmoToGrid x z
        match boundsCheck grid gx gz "HeightMap" with
        | Error e -> Error e
        | Ok () -> Ok (MapGrid.terrainAt grid gx gz)

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
    let heightSubRegion (grid: MapGrid) (x1: int) (z1: int) (x2: int) (z2: int) : Result<float32[,], string> =
        let gx1, gz1 = elmoToGrid x1 z1
        let gx2, gz2 = elmoToGrid x2 z2
        let maxX = Array2D.length1 grid.HeightMap - 1
        let maxZ = Array2D.length2 grid.HeightMap - 1
        let cx1 = max 0 (min gx1 maxX)
        let cz1 = max 0 (min gz1 maxZ)
        let cx2 = max 0 (min gx2 maxX)
        let cz2 = max 0 (min gz2 maxZ)
        let w = cx2 - cx1
        let h = cz2 - cz1
        if w <= 0 || h <= 0 then
            Error $"Invalid sub-region: elmo ({x1},{z1})-({x2},{z2}) → grid ({cx1},{cz1})-({cx2},{cz2}) has zero or negative size"
        else
            Ok (Array2D.init w h (fun x z -> grid.HeightMap.[cx1 + x, cz1 + z]))

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
    let resourceHotspots (grid: MapGrid) (x1: int) (z1: int) (x2: int) (z2: int) (threshold: int) : (int * int * int) list =
        let rw = Array2D.length1 grid.ResourceMap
        let rh = Array2D.length2 grid.ResourceMap
        let gx1, gz1 = elmoToGrid x1 z1
        let gx2, gz2 = elmoToGrid x2 z2
        let cx1 = max 0 (min gx1 (rw - 1))
        let cz1 = max 0 (min gz1 (rh - 1))
        let cx2 = max 0 (min gx2 (rw - 1))
        let cz2 = max 0 (min gz2 (rh - 1))
        [ for x in cx1 .. cx2 do
            for z in cz1 .. cz2 do
                let v = grid.ResourceMap.[x, z]
                if v > threshold then
                    yield (x, z, v) ]
        |> List.sortByDescending (fun (_, _, v) -> v)
