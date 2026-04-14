namespace FSBar.Client

open System.Collections.Generic
open System.Diagnostics

type OwnStructureFootprint =
    { Centre: float32 * float32 * float32
      RadiusElmos: float32
      Tag: string option }

type PathStatus =
    | Complete
    | Partial of budgetExhausted: bool

type PathFailure =
    | OutOfBounds
    | EndpointImpassable
    | NoRoute

type Path =
    { Waypoints: (float32 * float32 * float32) array
      EstimatedCost: float32
      Status: PathStatus }

type PathBudget =
    { WallClockMs: int
      MaxExpansions: int
      SlopeCost: float32 }

module Pathing =

    let defaultPathBudget: PathBudget =
        { WallClockMs = 50
          MaxExpansions = 50_000
          SlopeCost = 2.0f }

    // ---------------------------------------------------------------------
    // Coordinate conversion
    // ---------------------------------------------------------------------

    /// Heightmap cell size in elmos (matches Spring engine: 8 elmos per square).
    let private cellSize = 8.0f

    let private worldToCell (worldX: float32) (worldZ: float32) : int * int =
        int (worldX / cellSize), int (worldZ / cellSize)

    let private cellToWorldCentre (cellX: int) (cellZ: int) : float32 * float32 =
        float32 cellX * cellSize + cellSize * 0.5f,
        float32 cellZ * cellSize + cellSize * 0.5f

    // ---------------------------------------------------------------------
    // Footprint rasterisation (FR-002: ownStructures mask)
    // ---------------------------------------------------------------------

    let rasteriseFootprints (grid: MapGrid) (ownStructures: OwnStructureFootprint seq) : bool[,] =
        let w = Array2D.length1 grid.HeightMap
        let h = Array2D.length2 grid.HeightMap
        let blocked = Array2D.zeroCreate<bool> w h
        for fp in ownStructures do
            let (cx, _, cz) = fp.Centre
            let cellXf = cx / cellSize
            let cellZf = cz / cellSize
            let rCells = fp.RadiusElmos / cellSize
            let minX = max 0 (int (cellXf - rCells))
            let maxX = min (w - 1) (int (cellXf + rCells))
            let minZ = max 0 (int (cellZf - rCells))
            let maxZ = min (h - 1) (int (cellZf + rCells))
            let r2 = rCells * rCells
            for x in minX .. maxX do
                for z in minZ .. maxZ do
                    let dxf = float32 x + 0.5f - cellXf
                    let dzf = float32 z + 0.5f - cellZf
                    if dxf * dxf + dzf * dzf <= r2 then
                        blocked.[x, z] <- true
        blocked

    // ---------------------------------------------------------------------
    // A* core
    // ---------------------------------------------------------------------

    let private neighbourOffsets = [|
        struct (-1, -1); struct (0, -1); struct (1, -1)
        struct (-1,  0);                  struct (1,  0)
        struct (-1,  1); struct (0,  1); struct (1,  1)
    |]

    let private isCardinal (dx: int) (dz: int) = abs dx + abs dz = 1
    let private sqrt2 = 1.4142135f

    /// Octile distance heuristic scaled by the minimum edge cost (= 1.0f for flat).
    let private octileHeuristic (ax: int) (az: int) (bx: int) (bz: int) : float32 =
        let dx = float32 (abs (ax - bx))
        let dz = float32 (abs (az - bz))
        if dx > dz then dx + (sqrt2 - 1.0f) * dz
        else dz + (sqrt2 - 1.0f) * dx

    /// Slope lookup that matches MapGrid.passability: clamps `x/2, z/2` into the slope map.
    let private slopeAtCell (grid: MapGrid) (x: int) (z: int) : float32 =
        let sw = Array2D.length1 grid.SlopeMap
        let sh = Array2D.length2 grid.SlopeMap
        if sw = 0 || sh = 0 then 0.0f
        else
            let sx = min (x / 2) (sw - 1)
            let sz = min (z / 2) (sh - 1)
            grid.SlopeMap.[sx, sz]

    /// Straight-line walkability test between two cells (inclusive). Uses Bresenham's
    /// line algorithm and verifies every cell on the line is passable.
    let private lineIsWalkable (passable: bool[,]) (x0: int) (z0: int) (x1: int) (z1: int) : bool =
        let w = Array2D.length1 passable
        let h = Array2D.length2 passable
        let mutable x = x0
        let mutable z = z0
        let dx = abs (x1 - x0)
        let dz = abs (z1 - z0)
        let sx = if x0 < x1 then 1 else -1
        let sz = if z0 < z1 then 1 else -1
        let mutable err = dx - dz
        let mutable ok = true
        let mutable reached = false
        while ok && not reached do
            if x < 0 || x >= w || z < 0 || z >= h || not passable.[x, z] then
                ok <- false
            elif x = x1 && z = z1 then
                reached <- true
            else
                let e2 = 2 * err
                if e2 > -dz then
                    err <- err - dz
                    x <- x + sx
                if e2 < dx then
                    err <- err + dx
                    z <- z + sz
        ok && reached

    /// Collapse a raw cell path into waypoints by greedy line-of-sight coalescing.
    /// Walks forward from `start` and keeps the farthest reachable raw-path cell,
    /// guaranteeing that every consecutive pair of waypoints is line-walkable.
    let private collapseToWaypoints (passable: bool[,]) (cells: (int * int) array) : (int * int) array =
        if cells.Length <= 1 then cells
        else
            let result = ResizeArray<int * int>()
            result.Add(cells.[0])
            let mutable anchor = 0
            while anchor < cells.Length - 1 do
                let mutable farthest = anchor + 1
                let mutable probe = anchor + 2
                let (ax, az) = cells.[anchor]
                while probe < cells.Length do
                    let (px, pz) = cells.[probe]
                    if lineIsWalkable passable ax az px pz then
                        farthest <- probe
                        probe <- probe + 1
                    else
                        probe <- cells.Length
                result.Add(cells.[farthest])
                anchor <- farthest
            result.ToArray()

    /// Recover the full cell-by-cell path from start to `endIdx` using the parent array.
    let private recoverCellPath (parent: int[]) (startIdx: int) (endIdx: int) (width: int) : (int * int) array =
        let cells = ResizeArray<int * int>()
        let mutable cur = endIdx
        let mutable ok = true
        // Guard against accidental infinite loops via a bounded step counter.
        let mutable steps = 0
        let maxSteps = parent.Length + 1
        while ok && cur <> -1 && steps < maxSteps do
            let x = cur % width
            let z = cur / width
            cells.Add((x, z))
            if cur = startIdx then
                ok <- false
            else
                cur <- parent.[cur]
                steps <- steps + 1
        // Reverse to start→goal order.
        let arr = cells.ToArray()
        System.Array.Reverse(arr)
        arr

    let findPath
        (grid: MapGrid)
        (moveType: MoveType)
        (ownStructures: OwnStructureFootprint seq)
        (start: float32 * float32 * float32)
        (goal: float32 * float32 * float32)
        (budget: PathBudget)
        : Result<Path, PathFailure> =

        let (startX, _, startZ) = start
        let (goalX, _, goalZ) = goal

        let w = Array2D.length1 grid.HeightMap
        let h = Array2D.length2 grid.HeightMap

        let sxCell, szCell = worldToCell startX startZ
        let gxCell, gzCell = worldToCell goalX goalZ

        if sxCell < 0 || sxCell >= w || szCell < 0 || szCell >= h then
            Result.Error OutOfBounds
        elif gxCell < 0 || gxCell >= w || gzCell < 0 || gzCell >= h then
            Result.Error OutOfBounds
        else
            let basePassable = MapGrid.passability grid moveType
            let blocked = rasteriseFootprints grid ownStructures
            // Combine into a single passability grid. Small copy; amortised over many expansions.
            let passable = Array2D.init w h (fun x z -> basePassable.[x, z] && not blocked.[x, z])

            if not passable.[sxCell, szCell] || not passable.[gxCell, gzCell] then
                Result.Error EndpointImpassable
            else
                let cellCount = w * h
                let gScore = Array.create cellCount System.Single.PositiveInfinity
                let parent = Array.create cellCount -1
                let closed = Array.create cellCount false

                let linearise (x: int) (z: int) = z * w + x

                let startIdx = linearise sxCell szCell
                let goalIdx = linearise gxCell gzCell
                gScore.[startIdx] <- 0.0f

                // PriorityQueue keyed by (f, linearisedIdx) for deterministic tie-breaking.
                let comparer =
                    { new IComparer<struct (float32 * int)> with
                        member _.Compare(a, b) =
                            let struct (af, ai) = a
                            let struct (bf, bi) = b
                            let c = compare af bf
                            if c <> 0 then c else compare ai bi }
                let openSet = PriorityQueue<int, struct (float32 * int)>(comparer)
                openSet.Enqueue(startIdx, struct (octileHeuristic sxCell szCell gxCell gzCell, startIdx))

                // Partial-path tracking: node popped so far with the smallest heuristic-to-goal.
                let mutable bestHeuristic = System.Single.PositiveInfinity
                let mutable bestIdx = startIdx

                let stopwatch = Stopwatch.StartNew()
                let mutable expansions = 0
                let mutable budgetExhausted = false
                let mutable foundGoal = false

                while not foundGoal && not budgetExhausted && openSet.Count > 0 do
                    let currentIdx = openSet.Dequeue()
                    if closed.[currentIdx] then
                        ()  // Stale entry from a lazy "decrease-key" — skip.
                    else
                        closed.[currentIdx] <- true
                        let cx = currentIdx % w
                        let cz = currentIdx / w

                        // Track best partial (closest to goal by heuristic).
                        let hCur = octileHeuristic cx cz gxCell gzCell
                        if hCur < bestHeuristic then
                            bestHeuristic <- hCur
                            bestIdx <- currentIdx

                        if currentIdx = goalIdx then
                            foundGoal <- true
                        else
                            // Budget check every 256 expansions.
                            expansions <- expansions + 1
                            if expansions % 256 = 0 then
                                if int stopwatch.ElapsedMilliseconds > budget.WallClockMs
                                   || expansions >= budget.MaxExpansions then
                                    budgetExhausted <- true
                            elif expansions >= budget.MaxExpansions then
                                budgetExhausted <- true

                            if not budgetExhausted then
                                let gCur = gScore.[currentIdx]
                                for offset in neighbourOffsets do
                                    let struct (dx, dz) = offset
                                    let nx = cx + dx
                                    let nz = cz + dz
                                    if nx >= 0 && nx < w && nz >= 0 && nz < h && passable.[nx, nz] then
                                        // Disallow diagonal corner-cutting through walls.
                                        let corner1Ok =
                                            if dx <> 0 && dz <> 0 then passable.[cx + dx, cz]
                                            else true
                                        let corner2Ok =
                                            if dx <> 0 && dz <> 0 then passable.[cx, cz + dz]
                                            else true
                                        if corner1Ok && corner2Ok then
                                            let baseD = if isCardinal dx dz then 1.0f else sqrt2
                                            let slope = slopeAtCell grid nx nz
                                            let step = baseD * (1.0f + slope * budget.SlopeCost)
                                            let tentative = gCur + step
                                            let nIdx = linearise nx nz
                                            if tentative < gScore.[nIdx] then
                                                gScore.[nIdx] <- tentative
                                                parent.[nIdx] <- currentIdx
                                                let f = tentative + octileHeuristic nx nz gxCell gzCell
                                                openSet.Enqueue(nIdx, struct (f, nIdx))

                if foundGoal then
                    let rawCells = recoverCellPath parent startIdx goalIdx w
                    let wpCells = collapseToWaypoints passable rawCells
                    let waypoints =
                        wpCells
                        |> Array.map (fun (x, z) ->
                            let (wx, wz) = cellToWorldCentre x z
                            // Y coordinate: pick from heightmap if valid, else 0.
                            let hmW = Array2D.length1 grid.HeightMap
                            let hmH = Array2D.length2 grid.HeightMap
                            let y =
                                if x >= 0 && x < hmW && z >= 0 && z < hmH then
                                    grid.HeightMap.[x, z]
                                else 0.0f
                            (wx, y, wz))
                    let estimatedCost = gScore.[goalIdx]
                    Result.Ok
                        { Waypoints = waypoints
                          EstimatedCost = estimatedCost
                          Status = Complete }
                elif budgetExhausted then
                    // Build a partial path to the best node popped so far.
                    if bestIdx = startIdx then
                        // No progress at all — return an empty partial starting at `start`.
                        let (sx, sy, sz) = start
                        Result.Ok
                            { Waypoints = [| (sx, sy, sz) |]
                              EstimatedCost = 0.0f
                              Status = Partial true }
                    else
                        let rawCells = recoverCellPath parent startIdx bestIdx w
                        let wpCells = collapseToWaypoints passable rawCells
                        let waypoints =
                            wpCells
                            |> Array.map (fun (x, z) ->
                                let (wx, wz) = cellToWorldCentre x z
                                let y = grid.HeightMap.[x, z]
                                (wx, y, wz))
                        let estimatedCost = gScore.[bestIdx]
                        Result.Ok
                            { Waypoints = waypoints
                              EstimatedCost = estimatedCost
                              Status = Partial true }
                else
                    // Open set exhausted without reaching goal → no route exists.
                    Result.Error NoRoute

    // ---------------------------------------------------------------------
    // pathCost — re-score a precomputed path under a different slope weight.
    // ---------------------------------------------------------------------

    let pathCost (grid: MapGrid) (moveType: MoveType) (path: Path) (slopeCost: float32) : float32 =
        ignore moveType
        let wps = path.Waypoints
        if wps.Length < 2 then 0.0f
        else
            let mutable total = 0.0f
            for i in 1 .. wps.Length - 1 do
                let (x0, _, z0) = wps.[i - 1]
                let (x1, _, z1) = wps.[i]
                // Straight-line distance in elmos; sum slope contributions along the Bresenham walk.
                let sxCell, szCell = worldToCell x0 z0
                let gxCell, gzCell = worldToCell x1 z1
                // Walk the line and accumulate cell-local costs.
                let mutable cx = sxCell
                let mutable cz = szCell
                let dx = abs (gxCell - sxCell)
                let dz = abs (gzCell - szCell)
                let sx = if sxCell < gxCell then 1 else -1
                let sz = if szCell < gzCell then 1 else -1
                let mutable err = dx - dz
                let mutable reached = false
                while not reached do
                    if cx = gxCell && cz = gzCell then
                        reached <- true
                    else
                        let prevX = cx
                        let prevZ = cz
                        let e2 = 2 * err
                        if e2 > -dz then
                            err <- err - dz
                            cx <- cx + sx
                        if e2 < dx then
                            err <- err + dx
                            cz <- cz + sz
                        let ddx = cx - prevX
                        let ddz = cz - prevZ
                        let baseD = if abs ddx + abs ddz = 1 then 1.0f else sqrt2
                        let slope = slopeAtCell grid cx cz
                        total <- total + baseD * (1.0f + slope * slopeCost)
            total
