namespace FSBar.Client

open System.Collections.Generic

type WallInReason =
    | DisconnectsStructures of names: string list
    | EnclosesBase

type WallInResult =
    | Passes
    | Fails of reason: WallInReason

type WallInQuery =
    { MoveType: MoveType
      RequireMapEdgeExit: bool }

module WallIn =

    let cellSize = 8.0f

    let defaultWallInQuery : WallInQuery =
        { MoveType = MoveType.Kbot
          RequireMapEdgeExit = true }

    /// Combined "base passability + ownStructures footprint mask" used by both
    /// `reachableCells` and `Pathing.findPath` so a placement rejected by one is
    /// consistent with the other (FR-020).
    let buildPassable
        (grid: MapGrid)
        (moveType: MoveType)
        (ownStructures: OwnStructureFootprint seq)
        : bool[,] =
        let basePassable = MapGrid.passability grid moveType
        let blocked = Pathing.rasteriseFootprints grid ownStructures
        let w = Array2D.length1 basePassable
        let h = Array2D.length2 basePassable
        Array2D.init w h (fun x z -> basePassable.[x, z] && not blocked.[x, z])

    let reachableCells
        (grid: MapGrid)
        (moveType: MoveType)
        (ownStructures: OwnStructureFootprint seq)
        (origin: float32 * float32 * float32)
        : bool[,] =
        let passable = buildPassable grid moveType ownStructures
        let w = Array2D.length1 passable
        let h = Array2D.length2 passable
        let visited = Array2D.zeroCreate<bool> w h
        let (ox, _, oz) = origin
        let sx = int (ox / cellSize)
        let sz = int (oz / cellSize)
        if sx < 0 || sx >= w || sz < 0 || sz >= h || not passable.[sx, sz] then
            visited
        else
            let queue = Queue<struct (int * int)>()
            queue.Enqueue(struct (sx, sz))
            visited.[sx, sz] <- true
            while queue.Count > 0 do
                let struct (cx, cz) = queue.Dequeue()
                // 8-connectivity with diagonal corner-cut prevention — matches Pathing.findPath.
                for dz in -1 .. 1 do
                    for dx in -1 .. 1 do
                        if dx <> 0 || dz <> 0 then
                            let nx = cx + dx
                            let nz = cz + dz
                            if nx >= 0 && nx < w && nz >= 0 && nz < h
                               && passable.[nx, nz] && not visited.[nx, nz] then
                                let corner1Ok =
                                    if dx <> 0 && dz <> 0 then passable.[cx + dx, cz]
                                    else true
                                let corner2Ok =
                                    if dx <> 0 && dz <> 0 then passable.[cx, cz + dz]
                                    else true
                                if corner1Ok && corner2Ok then
                                    visited.[nx, nz] <- true
                                    queue.Enqueue(struct (nx, nz))
            visited

    /// True when the reachable set contains at least one map-edge cell.
    let hasMapEdgeExit (reachable: bool[,]) : bool =
        let w = Array2D.length1 reachable
        let h = Array2D.length2 reachable
        let mutable found = false
        let mutable i = 0
        while not found && i < w do
            if reachable.[i, 0] || reachable.[i, h - 1] then found <- true
            i <- i + 1
        let mutable j = 0
        while not found && j < h do
            if reachable.[0, j] || reachable.[w - 1, j] then found <- true
            j <- j + 1
        found

    /// A structure is "reachable" iff some passable cell inside a square of
    /// side `2 × (radiusCells + 1) + 1` around its centre is in the reach set.
    /// Structure footprints make their own centre impassable, so the literal
    /// centre cell is not a reliable indicator — callers care whether a builder
    /// can approach the structure from any adjacent passable cell.
    let structureReachable
        (reach: bool[,])
        (cellX: int)
        (cellZ: int)
        (radiusCells: int)
        : bool =
        let w = Array2D.length1 reach
        let h = Array2D.length2 reach
        let margin = radiusCells + 1
        let mutable found = false
        let minX = max 0 (cellX - margin)
        let maxX = min (w - 1) (cellX + margin)
        let minZ = max 0 (cellZ - margin)
        let maxZ = min (h - 1) (cellZ + margin)
        let mutable x = minX
        while not found && x <= maxX do
            let mutable z = minZ
            while not found && z <= maxZ do
                if reach.[x, z] then found <- true
                z <- z + 1
            x <- x + 1
        found

    let wouldWallIn
        (grid: MapGrid)
        (baseCentre: float32 * float32 * float32)
        (ownStructures: OwnStructureFootprint list)
        (proposed: OwnStructureFootprint)
        (query: WallInQuery)
        : WallInResult =
        let preReach = reachableCells grid query.MoveType ownStructures baseCentre
        let postStructures = proposed :: ownStructures
        let postReach = reachableCells grid query.MoveType postStructures baseCentre

        let disconnected =
            ownStructures
            |> List.filter (fun s ->
                let (cx, _, cz) = s.Centre
                let x = int (cx / cellSize)
                let z = int (cz / cellSize)
                let r = int (ceil (s.RadiusElmos / cellSize))
                let wasReachable = structureReachable preReach x z r
                let stillReachable = structureReachable postReach x z r
                wasReachable && not stillReachable)
            |> List.map (fun s -> s.Tag |> Option.defaultValue "?unnamed")

        if not disconnected.IsEmpty then
            Fails(DisconnectsStructures disconnected)
        elif query.RequireMapEdgeExit && not (hasMapEdgeExit postReach) then
            Fails EnclosesBase
        else
            Passes
