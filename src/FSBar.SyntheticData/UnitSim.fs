namespace FSBar.SyntheticData

open FSBar.Client

module UnitSim =

    type MovingUnit = {
        Unit: TrackedUnit
        TargetX: float32
        TargetZ: float32
        Speed: float32
    }

    let private pseudoRandom (seed: int) (range: float32) : float32 =
        let s = abs ((seed * 1103515245 + 12345) % 2147483647)
        (float32 s / 2147483647.0f) * range

    let create (unit: TrackedUnit) (speed: float32) (mapWidth: float32) (mapHeight: float32) (seed: int) : MovingUnit =
        let (x, _, _) = unit.Position
        let tx = pseudoRandom (seed + int x) mapWidth
        let tz = pseudoRandom (seed + int x + 7) mapHeight
        { Unit = unit; TargetX = tx; TargetZ = tz; Speed = speed }

    let step (mu: MovingUnit) (mapWidth: float32) (mapHeight: float32) (frame: int) : MovingUnit =
        if mu.Speed <= 0.0f then
            // Buildings don't move
            mu
        else
            let (x, y, z) = mu.Unit.Position
            let dx = mu.TargetX - x
            let dz = mu.TargetZ - z
            let dist = sqrt (dx * dx + dz * dz)
            if dist < mu.Speed then
                // Reached target — pick new one
                let newTx = pseudoRandom (frame * 31 + mu.Unit.UnitId * 17) mapWidth
                let newTz = pseudoRandom (frame * 37 + mu.Unit.UnitId * 13) mapHeight
                let updatedUnit = { mu.Unit with Position = (mu.TargetX, y, mu.TargetZ); IsIdle = true }
                { mu with Unit = updatedUnit; TargetX = newTx; TargetZ = newTz }
            else
                // Move toward target
                let scale = mu.Speed / dist
                let nx = x + dx * scale
                let nz = z + dz * scale
                // Clamp to map bounds
                let cx = max 0.0f (min nx mapWidth)
                let cz = max 0.0f (min nz mapHeight)
                let updatedUnit = { mu.Unit with Position = (cx, y, cz); IsIdle = false }
                { mu with Unit = updatedUnit }
