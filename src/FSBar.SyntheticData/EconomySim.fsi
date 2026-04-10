namespace FSBar.SyntheticData

open FSBar.Client

/// Pure economy simulation stepping.
module EconomySim =
    /// Advance economy by one frame. Clamps Current to [0, Storage].
    val step: snapshot: EconomySnapshot -> EconomySnapshot
