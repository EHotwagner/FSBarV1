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
    let nearestMetalSpot (spots: (float32 * float32 * float32 * float32) array) (position: float32 * float32 * float32) : (float32 * float32 * float32 * float32) option =
        if spots.Length = 0 then None
        else
            let px, _py, pz = position
            let mutable bestDist = System.Single.MaxValue
            let mutable bestIdx = 0
            for i in 0 .. spots.Length - 1 do
                let (sx, _sy, sz, _v) = spots.[i]
                let dx = sx - px
                let dz = sz - pz
                let dist = dx * dx + dz * dz
                if dist < bestDist then
                    bestDist <- dist
                    bestIdx <- i
            Some spots.[bestIdx]

    let metalSpotsFromResourceMap (grid: MapGrid) : (float32 * float32 * float32 * float32) array =
        let resource = grid.ResourceMap
        let heights = grid.HeightMap
        let rh = Array2D.length1 resource
        let rw = Array2D.length2 resource
        if rh = 0 || rw = 0 then [||]
        else
            let mutable globalMax = 0
            for z in 0 .. rh - 1 do
                for x in 0 .. rw - 1 do
                    let v = resource.[z, x]
                    if v > globalMax then globalMax <- v
            if globalMax <= 0 then [||]
            else
                let hh = Array2D.length1 heights
                let hw = Array2D.length2 heights
                let visited = Array2D.init rh rw (fun _ _ -> false)
                let clusters = System.Collections.Generic.List<int * int * int * int * int64 * int>()
                // tuple: (minZ, minX, sumZ, sumX, sumValue, cellCount)
                let queue = System.Collections.Generic.Queue<int * int>()
                let offsets =
                    [| (-1, -1); (-1, 0); (-1, 1)
                       ( 0, -1);            ( 0, 1)
                       ( 1, -1); ( 1, 0); ( 1, 1) |]
                for z0 in 0 .. rh - 1 do
                    for x0 in 0 .. rw - 1 do
                        if not visited.[z0, x0] && resource.[z0, x0] > 0 then
                            visited.[z0, x0] <- true
                            queue.Clear()
                            queue.Enqueue((z0, x0))
                            let mutable minZ = z0
                            let mutable minX = x0
                            let mutable sumZ = 0
                            let mutable sumX = 0
                            let mutable sumValue = 0L
                            let mutable cellCount = 0
                            while queue.Count > 0 do
                                let (cz, cx) = queue.Dequeue()
                                let v = resource.[cz, cx]
                                sumZ <- sumZ + cz
                                sumX <- sumX + cx
                                sumValue <- sumValue + int64 v
                                cellCount <- cellCount + 1
                                if cz < minZ then minZ <- cz
                                if cx < minX then minX <- cx
                                for (dz, dx) in offsets do
                                    let nz = cz + dz
                                    let nx = cx + dx
                                    if nz >= 0 && nz < rh && nx >= 0 && nx < rw
                                       && not visited.[nz, nx]
                                       && resource.[nz, nx] > 0 then
                                        visited.[nz, nx] <- true
                                        queue.Enqueue((nz, nx))
                            clusters.Add((minZ, minX, sumZ, sumX, sumValue, cellCount))
                clusters
                |> Seq.sortBy (fun (mz, mx, _, _, _, _) -> (mz, mx))
                |> Seq.map (fun (_, _, sumZ, sumX, sumValue, cellCount) ->
                    let cz = float32 sumZ / float32 cellCount
                    let cx = float32 sumX / float32 cellCount
                    let nearestZ = min (hh - 1) (max 0 (int (cz + 0.5f)))
                    let nearestX = min (hw - 1) (max 0 (int (cx + 0.5f)))
                    let worldY =
                        if hh > 0 && hw > 0 then heights.[nearestZ, nearestX]
                        else 0.0f
                    let worldX = cx * 8.0f
                    let worldZ = cz * 8.0f
                    let mean = float32 sumValue / float32 cellCount
                    let richness =
                        let r = mean / float32 globalMax
                        if r < 0.0f then 0.0f
                        elif r > 1.0f then 1.0f
                        else r
                    (worldX, worldY, worldZ, richness))
                |> Seq.toArray

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
