namespace FSBar.Viz

open FSBar.Client

module MockSnapshot =
    val emptySnapshot: grid: MapGrid -> GameSnapshot
    val withUnits: units: UnitState list -> snapshot: GameSnapshot -> GameSnapshot
    val withFriendlyAt: pos: (float32 * float32 * float32) -> snapshot: GameSnapshot -> GameSnapshot
    val withEnemyAt: pos: (float32 * float32 * float32) -> snapshot: GameSnapshot -> GameSnapshot
    val withEvent: kind: EventKind -> pos: (float32 * float32 * float32) -> frame: int -> snapshot: GameSnapshot -> GameSnapshot
    val withEconomy: current: float32 -> income: float32 -> usage: float32 -> storage: float32 -> snapshot: GameSnapshot -> GameSnapshot
    val withEnergyEconomy: current: float32 -> income: float32 -> usage: float32 -> storage: float32 -> snapshot: GameSnapshot -> GameSnapshot
    val withMetalSpots: spots: (float32 * float32 * float32 * float32) array -> snapshot: GameSnapshot -> GameSnapshot
    val withFrame: frame: int -> snapshot: GameSnapshot -> GameSnapshot
