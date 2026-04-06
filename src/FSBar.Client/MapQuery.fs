namespace FSBar.Client

module MapQuery =

    let elmoToGrid (x: int) (z: int) : int * int =
        (x / 8, z / 8)

    let gridToElmo (x: int) (z: int) : int * int =
        (x * 8, z * 8)

    let private boundsCheck (grid: MapGrid) (gx: int) (gz: int) (layerName: string) =
        let maxX = Array2D.length1 grid.HeightMap - 1
        let maxZ = Array2D.length2 grid.HeightMap - 1
        if gx < 0 || gx > maxX || gz < 0 || gz > maxZ then
            Error $"Out of bounds: elmo ({gx * 8}, {gz * 8}) → grid ({gx}, {gz}) outside {layerName} range [0..{maxX}, 0..{maxZ}]"
        else
            Ok ()

    let heightAtElmo (grid: MapGrid) (x: int) (z: int) : Result<float32, string> =
        let gx, gz = elmoToGrid x z
        match boundsCheck grid gx gz "HeightMap" with
        | Error e -> Error e
        | Ok () -> Ok grid.HeightMap.[gx, gz]

    let slopeAtElmo (grid: MapGrid) (x: int) (z: int) : Result<float32, string> =
        let sx, sz = x / 16, z / 16
        let maxX = Array2D.length1 grid.SlopeMap - 1
        let maxZ = Array2D.length2 grid.SlopeMap - 1
        if sx < 0 || sx > maxX || sz < 0 || sz > maxZ then
            Error $"Out of bounds: elmo ({x}, {z}) → slope grid ({sx}, {sz}) outside SlopeMap range [0..{maxX}, 0..{maxZ}]"
        else
            Ok grid.SlopeMap.[sx, sz]

    let terrainAtElmo (grid: MapGrid) (x: int) (z: int) : Result<Terrain, string> =
        let gx, gz = elmoToGrid x z
        match boundsCheck grid gx gz "HeightMap" with
        | Error e -> Error e
        | Ok () -> Ok (MapGrid.terrainAt grid gx gz)

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
