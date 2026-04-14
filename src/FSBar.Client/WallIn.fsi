namespace FSBar.Client

/// <summary>Why a proposed placement would wall in the base.</summary>
type WallInReason =
    /// <summary>Proposed placement disconnects the base centre from the listed structures.</summary>
    | DisconnectsStructures of names: string list
    /// <summary>Proposed placement creates an enclosed pocket around the base centre.</summary>
    | EnclosesBase

/// <summary>Result of a <c>wouldWallIn</c> check.</summary>
type WallInResult =
    /// <summary>Placement is safe — base remains connected to every existing structure and a map-edge exit.</summary>
    | Passes
    /// <summary>Placement would wall in; diagnostic payload attached.</summary>
    | Fails of reason: WallInReason

/// <summary>Query parameters controlling <c>wouldWallIn</c> semantics.</summary>
type WallInQuery =
    { /// <summary>Movement type used for connectivity checks (usually Kbot or Hover).</summary>
      MoveType: MoveType
      /// <summary>When <c>true</c>, also require at least one passable route from base centre to the map edge.</summary>
      RequireMapEdgeExit: bool }

/// <summary>
/// Pure connectivity predicate that shares passability rules with <c>Pathing</c>. Used by
/// <c>BasePlan.resolvePlan</c> and directly by bots evaluating ad-hoc placements.
/// </summary>
module WallIn =

    /// <summary>Default query: Kbot move type, <c>RequireMapEdgeExit = true</c>.</summary>
    val defaultWallInQuery: WallInQuery

    /// <summary>
    /// Check whether adding <paramref name="proposed"/> to <paramref name="ownStructures"/>
    /// would disconnect <paramref name="baseCentre"/> from any currently-reachable structure
    /// or map-edge exit. Pure; does not mutate any input.
    /// </summary>
    val wouldWallIn:
        grid: MapGrid ->
        baseCentre: (float32 * float32 * float32) ->
        ownStructures: OwnStructureFootprint list ->
        proposed: OwnStructureFootprint ->
        query: WallInQuery ->
            WallInResult

    /// <summary>
    /// Compute the set of cells reachable from <paramref name="origin"/> given the current
    /// passability overlay masked by <paramref name="ownStructures"/>. Exposed so
    /// <c>BasePlan</c> can reuse the reachability set across multiple slot checks.
    /// </summary>
    val reachableCells:
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
        origin: (float32 * float32 * float32) ->
            bool[,]
