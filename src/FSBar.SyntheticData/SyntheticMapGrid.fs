module FSBar.SyntheticData.SyntheticMapGrid

open FSBar.Client

let flat (width: int) (height: int) : MapGrid =
    let heightMap = Array2D.create (width + 1) (height + 1) 10.0f
    let slopeMap = Array2D.create width height 0.0f
    let resourceMap = Array2D.create width height 0
    let losMap = Array2D.create width height 0
    let radarMap = Array2D.create width height 0
    { WidthElmos = width * 8
      HeightElmos = height * 8
      WidthHeightmap = width
      HeightHeightmap = height
      HeightMap = heightMap
      SlopeMap = slopeMap
      ResourceMap = resourceMap
      LosMap = losMap
      RadarMap = radarMap }

let withWall (grid: MapGrid) (x1: int) (z1: int) (x2: int) (z2: int) : MapGrid =
    let slopeW = Array2D.length1 grid.SlopeMap
    let slopeH = Array2D.length2 grid.SlopeMap
    let slope = Array2D.copy grid.SlopeMap
    let dx = abs (x2 - x1)
    let dz = abs (z2 - z1)
    let sx = if x1 < x2 then 1 else -1
    let sz = if z1 < z2 then 1 else -1
    let mutable err = dx - dz
    let mutable x = x1
    let mutable z = z1
    let stamp (cx: int) (cz: int) =
        let sxIdx = cx / 2
        let szIdx = cz / 2
        if sxIdx >= 0 && sxIdx < slopeW && szIdx >= 0 && szIdx < slopeH then
            slope.[sxIdx, szIdx] <- 1.0f
    stamp x z
    while x <> x2 || z <> z2 do
        let e2 = 2 * err
        if e2 > -dz then
            err <- err - dz
            x <- x + sx
        if e2 < dx then
            err <- err + dx
            z <- z + sz
        stamp x z
    { grid with SlopeMap = slope }

let withCliff (grid: MapGrid) (centreX: int) (centreZ: int) (radius: int) : MapGrid =
    let slopeW = Array2D.length1 grid.SlopeMap
    let slopeH = Array2D.length2 grid.SlopeMap
    let slope = Array2D.copy grid.SlopeMap
    let r2 = radius * radius
    let cxSlope = centreX / 2
    let czSlope = centreZ / 2
    let rSlope = max 1 (radius / 2)
    for dx in -rSlope .. rSlope do
        for dz in -rSlope .. rSlope do
            if dx * dx + dz * dz <= r2 then
                let x = cxSlope + dx
                let z = czSlope + dz
                if x >= 0 && x < slopeW && z >= 0 && z < slopeH then
                    slope.[x, z] <- 0.7f
    { grid with SlopeMap = slope }

let withMetalSpot (grid: MapGrid) (x: int) (z: int) (value: int) : MapGrid =
    let resW = Array2D.length1 grid.ResourceMap
    let resH = Array2D.length2 grid.ResourceMap
    let res = Array2D.copy grid.ResourceMap
    if x >= 0 && x < resW && z >= 0 && z < resH then
        res.[x, z] <- value
    { grid with ResourceMap = res }

let oneGapCorridor (width: int) (height: int) (gapCells: int) : MapGrid =
    let grid = flat width height
    let slope = Array2D.copy grid.SlopeMap
    let slopeH = Array2D.length2 slope
    let wallSlopeX = width / 4
    let gapSlopeCentre = height / 4
    let gapSlopeHalf = max 0 (gapCells / 2)
    let gapSlopeLo =
        if gapCells = 0 then 1
        else gapSlopeCentre - gapSlopeHalf
    let gapSlopeHi =
        if gapCells = 0 then 0
        else gapSlopeLo + max 1 (gapCells / 2 + gapCells % 2) - 1
    for z in 0 .. slopeH - 1 do
        if z < gapSlopeLo || z > gapSlopeHi then
            slope.[wallSlopeX, z] <- 1.0f
    { grid with SlopeMap = slope }

let build (parameters: {| width: int; height: int; seed: int option |}) : MapGrid =
    let w = parameters.width
    let h = parameters.height
    let offset = parameters.seed |> Option.defaultValue 0 |> float32
    let heightMap = Array2D.init (w + 1) (h + 1) (fun x z ->
        float32 x * 0.1f + float32 z * 0.05f + offset)
    let slopeMap = Array2D.init (max 1 (w / 2)) (max 1 (h / 2)) (fun x z ->
        float32 x * 0.01f + float32 z * 0.02f)
    let resourceMap = Array2D.init w h (fun x z -> (x + z) % 10)
    let losMap = Array2D.init w h (fun x z -> if (x + z) % 3 = 0 then 1 else 0)
    let radarMap = Array2D.init w h (fun x z -> if (x + z) % 5 = 0 then 1 else 0)
    { WidthElmos = w * 8
      HeightElmos = h * 8
      WidthHeightmap = w
      HeightHeightmap = h
      HeightMap = heightMap
      SlopeMap = slopeMap
      ResourceMap = resourceMap
      LosMap = losMap
      RadarMap = radarMap }
