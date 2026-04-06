namespace FSBar.Viz

open FSBar.Client

/// Pipeline builder functions for constructing GameSnapshot records for testing.
/// All functions are composable via the |> operator.
module MockSnapshot =
    /// Create a snapshot with the given map grid, frame 0, and empty units/events/economy.
    val emptySnapshot: grid: MapGrid -> GameSnapshot

    /// Replace the Units map with the given list (keyed by UnitId).
    val withUnits: units: UnitState list -> snapshot: GameSnapshot -> GameSnapshot

    /// Add a friendly unit at the given (x, y, z) position with auto-generated UnitId.
    val withFriendlyAt: pos: (float32 * float32 * float32) -> snapshot: GameSnapshot -> GameSnapshot

    /// Add an enemy unit at the given (x, y, z) position with auto-generated UnitId.
    val withEnemyAt: pos: (float32 * float32 * float32) -> snapshot: GameSnapshot -> GameSnapshot

    /// Add an event indicator of the given kind at the position, created at the given frame.
    val withEvent: kind: EventKind -> pos: (float32 * float32 * float32) -> frame: int -> snapshot: GameSnapshot -> GameSnapshot

    /// Set the metal economy values (current, income, usage, storage).
    val withEconomy: current: float32 -> income: float32 -> usage: float32 -> storage: float32 -> snapshot: GameSnapshot -> GameSnapshot

    /// Set the energy economy values (current, income, usage, storage).
    val withEnergyEconomy: current: float32 -> income: float32 -> usage: float32 -> storage: float32 -> snapshot: GameSnapshot -> GameSnapshot

    /// Set the metal spot positions.
    val withMetalSpots: spots: (float32 * float32 * float32 * float32) array -> snapshot: GameSnapshot -> GameSnapshot

    /// Set the frame number.
    val withFrame: frame: int -> snapshot: GameSnapshot -> GameSnapshot
