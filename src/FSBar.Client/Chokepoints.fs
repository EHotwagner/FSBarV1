namespace FSBar.Client

open System.Collections.Generic

type ChokepointId = ChokepointId of uint32

type Chokepoint =
    { Id: ChokepointId
      Position: float32 * float32 * float32
      WidthElmos: float32
      OutwardDir: float32 * float32
      DistanceFromBase: float32 }

type ChokepointQuery =
    { MaxWidthElmos: float32
      SearchRadiusElmos: float32
      MoveType: MoveType }

module Chokepoints =

    let private cellSize = 8.0f
    let private sqrt2 = 1.4142135f

    let defaultChokepointQuery (moveType: MoveType) : ChokepointQuery =
        { MaxWidthElmos = 40.0f
          SearchRadiusElmos = 2500.0f
          MoveType = moveType }

    /// Chamfer (1, sqrt(2)) distance transform over the grid's passability layer.
    /// Out-of-bounds cells and any cell covered by an `ownStructures` footprint are
    /// treated as impassable. Output is a fresh float32[,] at passability resolution.
    let computeDistanceTransform
        (grid: MapGrid)
        (moveType: MoveType)
        (ownStructures: OwnStructureFootprint seq)
        : float32[,] =
        let basePassable = MapGrid.passability grid moveType
        let blocked = Pathing.rasteriseFootprints grid ownStructures
        let w = Array2D.length1 basePassable
        let h = Array2D.length2 basePassable
        let infinity = 1.0e9f
        let dt = Array2D.init w h (fun x z ->
            if basePassable.[x, z] && not blocked.[x, z] then infinity
            else 0.0f)

        // Forward pass: top-left → bottom-right. Out-of-bounds neighbours count as dt=0.
        let getForward (x: int) (z: int) : float32 =
            if x < 0 || x >= w || z < 0 || z >= h then 0.0f
            else dt.[x, z]
        for z in 0 .. h - 1 do
            for x in 0 .. w - 1 do
                if dt.[x, z] > 0.0f then
                    let mutable best = dt.[x, z]
                    let cardWest = getForward (x - 1) z + 1.0f
                    if cardWest < best then best <- cardWest
                    let cardNorth = getForward x (z - 1) + 1.0f
                    if cardNorth < best then best <- cardNorth
                    let diagNW = getForward (x - 1) (z - 1) + sqrt2
                    if diagNW < best then best <- diagNW
                    let diagNE = getForward (x + 1) (z - 1) + sqrt2
                    if diagNE < best then best <- diagNE
                    dt.[x, z] <- best

        // Backward pass.
        let getBackward (x: int) (z: int) : float32 =
            if x < 0 || x >= w || z < 0 || z >= h then 0.0f
            else dt.[x, z]
        for z in h - 1 .. -1 .. 0 do
            for x in w - 1 .. -1 .. 0 do
                if dt.[x, z] > 0.0f then
                    let mutable best = dt.[x, z]
                    let cardEast = getBackward (x + 1) z + 1.0f
                    if cardEast < best then best <- cardEast
                    let cardSouth = getBackward x (z + 1) + 1.0f
                    if cardSouth < best then best <- cardSouth
                    let diagSE = getBackward (x + 1) (z + 1) + sqrt2
                    if diagSE < best then best <- diagSE
                    let diagSW = getBackward (x - 1) (z + 1) + sqrt2
                    if diagSW < best then best <- diagSW
                    dt.[x, z] <- best
        dt

    let chokepointIdOf (grid: MapGrid) (ridgeCellX: int) (ridgeCellZ: int) : ChokepointId =
        // Include grid dimensions so two maps with different shapes can't collide.
        let w = uint32 grid.WidthHeightmap
        let h = uint32 grid.HeightHeightmap
        let idx = uint32 ridgeCellZ * (w + 1u) + uint32 ridgeCellX
        // Simple 3-field mix — deterministic and stable.
        let mutable hash = 2166136261u  // FNV-1a offset basis
        let fnv (v: uint32) =
            hash <- hash ^^^ (v &&& 0xFFu)
            hash <- hash * 16777619u
            hash <- hash ^^^ ((v >>> 8) &&& 0xFFu)
            hash <- hash * 16777619u
            hash <- hash ^^^ ((v >>> 16) &&& 0xFFu)
            hash <- hash * 16777619u
            hash <- hash ^^^ ((v >>> 24) &&& 0xFFu)
            hash <- hash * 16777619u
        fnv w
        fnv h
        fnv idx
        ChokepointId hash

    /// Chokepoint detection via reverse-order union-find bridge discovery.
    ///
    /// Key insight: process passable cells in decreasing dt order (widest-first).
    /// A cell is a "bridge" if adding it causes base's connected component to
    /// absorb a non-trivial previously-disconnected region. The dt value at the
    /// bridge is the corridor's half-width at the narrowest crossing point.
    ///
    /// Total cost: O(N log N) for the sort + O(N α(N)) for union-find, no
    /// per-candidate flood-fill. Runs comfortably on a 512 × 512 heightmap
    /// (Avalanche 3.4) in under a second.
    let findChokepoints
        (grid: MapGrid)
        (baseCentre: float32 * float32 * float32)
        (query: ChokepointQuery)
        : Chokepoint list =
        let (bcx, _, bcz) = baseCentre
        let basePassable = MapGrid.passability grid query.MoveType
        let dt = computeDistanceTransform grid query.MoveType Seq.empty
        let w = Array2D.length1 basePassable
        let h = Array2D.length2 basePassable

        let baseCellX = int (bcx / cellSize)
        let baseCellZ = int (bcz / cellSize)
        if baseCellX < 0 || baseCellX >= w || baseCellZ < 0 || baseCellZ >= h
           || not basePassable.[baseCellX, baseCellZ] then
            []
        else
            let n = w * h
            let baseIdx = baseCellZ * w + baseCellX
            let maxDtForWidth = query.MaxWidthElmos / (2.0f * cellSize)
            let searchRadiusCells = query.SearchRadiusElmos / cellSize
            let searchRadius2 = searchRadiusCells * searchRadiusCells

            // Collect passable cells with their dt; sort descending by dt.
            // Tie-break by linearised index so processing is deterministic.
            let passableCells = ResizeArray<struct (int * float32)>()
            for z in 0 .. h - 1 do
                for x in 0 .. w - 1 do
                    if basePassable.[x, z] then
                        passableCells.Add(struct (z * w + x, dt.[x, z]))
            passableCells.Sort(
                { new System.Collections.Generic.IComparer<struct (int * float32)> with
                    member _.Compare(a, b) =
                        let struct (ai, av) = a
                        let struct (bi, bv) = b
                        let c = compare bv av  // descending by dt
                        if c <> 0 then c else compare ai bi })

            // Union-find with path compression + size-balanced union.
            let parent = Array.init n id
            let compSize = Array.create n 1
            let rec findRoot i =
                if parent.[i] = i then i
                else
                    let r = findRoot parent.[i]
                    parent.[i] <- r
                    r
            let unionCells a b =
                let ra = findRoot a
                let rb = findRoot b
                if ra = rb then false
                else
                    if compSize.[ra] < compSize.[rb] then
                        parent.[ra] <- rb
                        compSize.[rb] <- compSize.[rb] + compSize.[ra]
                    else
                        parent.[rb] <- ra
                        compSize.[ra] <- compSize.[ra] + compSize.[rb]
                    true

            let active = Array.create n false
            // Pre-activate the base cell so it's always "in its own component".
            // Cells processed later that are adjacent to base (directly or
            // transitively) join base's component. Base itself is skipped during
            // the sorted iteration so it never registers as a bridge discovery.
            active.[baseIdx] <- true

            // Process cells widest-first. Record each cell c as a "bridge" iff:
            //   - After adding c and unioning with its active neighbours, c is in
            //     base's component, AND
            //   - base's component size grew by MORE than 1 (i.e., c linked at
            //     least one previously-disconnected component into base).
            // The growth counts the size of the merged-in region, which is the
            // chokepoint's "impact" (how many cells are isolated behind it).
            let bridges = ResizeArray<int * float32 * int>()  // (idx, dt, gained)
            let neighbourOffsets = [|
                struct (-1, -1); struct (0, -1); struct (1, -1)
                struct (-1,  0);                  struct (1,  0)
                struct (-1,  1); struct (0,  1); struct (1,  1)
            |]

            for struct (idx, v) in passableCells do
                if idx <> baseIdx then
                    active.[idx] <- true
                    let cx = idx % w
                    let cz = idx / w
                    let baseRootBefore = findRoot baseIdx
                    let baseSizeBefore = compSize.[baseRootBefore]
                    for offset in neighbourOffsets do
                        let struct (dx, dz) = offset
                        let nx = cx + dx
                        let nz = cz + dz
                        if nx >= 0 && nx < w && nz >= 0 && nz < h then
                            let nIdx = nz * w + nx
                            if active.[nIdx] then
                                unionCells idx nIdx |> ignore
                    let baseRootAfter = findRoot baseIdx
                    let baseSizeAfter = compSize.[baseRootAfter]
                    if baseSizeAfter > baseSizeBefore && findRoot idx = baseRootAfter then
                        let gained = baseSizeAfter - baseSizeBefore
                        if gained > 1 then
                            bridges.Add((idx, v, gained))

            // Bridges are discovered in dt-descending order. Filter: within search
            // radius of base, dt ≤ maxDtForWidth, and gained ≥ minImpact (50 cells
            // — large enough to reject micro-pockets but small enough to catch
            // canyon entrances that isolate a single island). The floor is
            // deliberately independent of total map size; a 50-cell pocket is
            // ~400 elmos² which is already big enough to care about.
            let minImpact = 50

            let isWithinRadius (idx: int) : bool =
                let cx = idx % w
                let cz = idx / w
                let dxc = float32 (cx - baseCellX)
                let dzc = float32 (cz - baseCellZ)
                dxc * dxc + dzc * dzc <= searchRadius2
            let survivors =
                bridges
                |> Seq.filter (fun (idx, v, gained) ->
                    gained >= minImpact
                    && v > 0.0f
                    && v <= maxDtForWidth
                    && isWithinRadius idx)
                |> Seq.toArray

            // Build Chokepoint records sorted by distance from base. No further
            // deduplication is needed — the union-find approach naturally yields
            // one bridge per connected-component merge, not many along a corridor.
            survivors
            |> Seq.map (fun (idx, v, _gained) ->
                let cx = idx % w
                let cz = idx / w
                let wx = float32 cx * cellSize + cellSize * 0.5f
                let wz = float32 cz * cellSize + cellSize * 0.5f
                let dxr = wx - bcx
                let dzr = wz - bcz
                let dist = sqrt (dxr * dxr + dzr * dzr)
                let outward =
                    if dist > 0.0f then (dxr / dist, dzr / dist)
                    else (1.0f, 0.0f)
                let y =
                    if cx >= 0 && cx < Array2D.length1 grid.HeightMap
                       && cz >= 0 && cz < Array2D.length2 grid.HeightMap
                    then grid.HeightMap.[cx, cz]
                    else 0.0f
                { Id = chokepointIdOf grid cx cz
                  Position = (wx, y, wz)
                  WidthElmos = 2.0f * v * cellSize
                  OutwardDir = outward
                  DistanceFromBase = dist })
            |> Seq.sortBy (fun cp -> cp.DistanceFromBase)
            |> Seq.toList
