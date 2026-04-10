namespace FSBar.SyntheticData

open FSBar.Client

/// Unit movement simulation.
module UnitSim =
    /// Internal state for a moving unit.
    type MovingUnit = {
        Unit: TrackedUnit
        TargetX: float32
        TargetZ: float32
        Speed: float32
    }

    /// Create a MovingUnit with a random target within map bounds.
    val create: unit: TrackedUnit -> speed: float32 -> mapWidth: float32 -> mapHeight: float32 -> seed: int -> MovingUnit

    /// Advance one frame. Moves toward target; picks new target when reached.
    /// Returns updated MovingUnit.
    val step: mu: MovingUnit -> mapWidth: float32 -> mapHeight: float32 -> frame: int -> MovingUnit
