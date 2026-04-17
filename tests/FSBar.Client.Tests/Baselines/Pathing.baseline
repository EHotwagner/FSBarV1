namespace FSBar.Client

/// <summary>
/// Footprint of a placed own structure used as an impassable mask overlay
/// when pathing. Consumed by both <c>Pathing</c> and <c>WallIn</c>.
/// </summary>
type OwnStructureFootprint =
    { /// <summary>World-coordinate centre of the structure.</summary>
      Centre: float32 * float32 * float32
      /// <summary>Footprint radius in elmos (circular approximation for non-square structures).</summary>
      RadiusElmos: float32
      /// <summary>Optional diagnostic tag (def name, slot label).</summary>
      Tag: string option }

/// <summary>Completion status of a <c>findPath</c> call.</summary>
type PathStatus =
    /// <summary>The goal was reached within budget.</summary>
    | Complete
    /// <summary>
    /// The search hit the wall-clock or node-count budget before finding the goal.
    /// Waypoints contain the best partial path toward the lowest-f-score frontier node.
    /// </summary>
    | Partial of budgetExhausted: bool

/// <summary>Reasons <c>findPath</c> can return <c>Error</c>.</summary>
type PathFailure =
    /// <summary>Start or goal cell is outside the map bounds.</summary>
    | OutOfBounds
    /// <summary>Start or goal is on a cell that the given move type cannot occupy.</summary>
    | EndpointImpassable
    /// <summary>A search completed within budget but no route exists.</summary>
    | NoRoute

/// <summary>A resolved path from start to goal (or a partial path under budget exhaustion).</summary>
type Path =
    { /// <summary>
      /// Ordered world-coordinate waypoints, start → goal. Any two consecutive waypoints
      /// are connected by a straight line that is passable for the move type used to find
      /// this path.
      /// </summary>
      Waypoints: (float32 * float32 * float32) array
      /// <summary>Total estimated traversal cost (sum of edge weights).</summary>
      EstimatedCost: float32
      /// <summary>Completion status.</summary>
      Status: PathStatus }

/// <summary>Search budget limiting wall-clock time, node expansions, and slope weighting.</summary>
type PathBudget =
    { /// <summary>Maximum wall-clock time before the search aborts with <c>Partial</c>.</summary>
      WallClockMs: int
      /// <summary>Maximum cell expansions before the search aborts with <c>Partial</c>.</summary>
      MaxExpansions: int
      /// <summary>Slope cost multiplier: <c>edgeCost = distance × (1 + slope × SlopeCost)</c>.</summary>
      SlopeCost: float32 }

/// <summary>
/// Slope-aware A* pathing over a <see cref="T:FSBar.Client.MapGrid"/> with friendly-structure
/// masking. Pure over inputs; never blocks beyond the configured budget.
/// </summary>
module Pathing =

    /// <summary>
    /// Default budget: 50 ms wall clock, 50 000 cell expansions, slope cost multiplier 2.0.
    /// </summary>
    val defaultPathBudget: PathBudget

    /// <summary>
    /// Find a path from <paramref name="start"/> to <paramref name="goal"/> over
    /// <paramref name="grid"/> for <paramref name="moveType"/>, treating every cell covered
    /// by an <paramref name="ownStructures"/> footprint as impassable.
    /// </summary>
    /// <param name="grid">Map data providing passability and slope information.</param>
    /// <param name="moveType">Movement type governing passability thresholds.</param>
    /// <param name="ownStructures">Friendly structure footprints to mask out of the passability grid.</param>
    /// <param name="start">Start position in world coordinates (elmos).</param>
    /// <param name="goal">Goal position in world coordinates (elmos).</param>
    /// <param name="budget">Wall-clock, expansion, and slope-weighting limits.</param>
    /// <returns>
    /// <c>Ok</c> with a <see cref="T:FSBar.Client.Path"/> on success, or <c>Error</c> with a
    /// <see cref="T:FSBar.Client.PathFailure"/> on invalid endpoints or no route.
    /// </returns>
    val findPath:
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
        start: (float32 * float32 * float32) ->
        goal: (float32 * float32 * float32) ->
        budget: PathBudget ->
            Result<Path, PathFailure>

    /// <summary>
    /// Re-sum the edge weights of a precomputed path under a caller-supplied slope cost.
    /// Used to re-score an existing <see cref="T:FSBar.Client.Path"/> against a different
    /// <c>SlopeCost</c> without re-running the full search.
    /// </summary>
    val pathCost: grid: MapGrid -> moveType: MoveType -> path: Path -> slopeCost: float32 -> float32

    /// <summary>
    /// Rasterise a collection of structure footprints into a boolean grid overlay matching
    /// the shape of <c>MapGrid.passability</c>. Callers can reuse the overlay across
    /// multiple <c>findPath</c> calls for the same structure set.
    /// </summary>
    val rasteriseFootprints: grid: MapGrid -> ownStructures: OwnStructureFootprint seq -> bool[,]
