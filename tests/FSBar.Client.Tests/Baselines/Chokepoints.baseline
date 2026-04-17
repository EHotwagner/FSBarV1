namespace FSBar.Client

/// <summary>
/// Stable identifier for a chokepoint, derived deterministically from the
/// grid dimensions and ridge cell index. Two calls to <c>findChokepoints</c> with the same
/// inputs produce the same <c>Id</c> values.
/// </summary>
type ChokepointId = ChokepointId of uint32

/// <summary>A detected chokepoint descriptor.</summary>
type Chokepoint =
    { /// <summary>Stable identifier across successive queries on the same grid.</summary>
      Id: ChokepointId
      /// <summary>Representative world position at the narrowest point of the corridor.</summary>
      Position: float32 * float32 * float32
      /// <summary>Estimated width of the corridor at the narrowest point, in elmos.</summary>
      WidthElmos: float32
      /// <summary>Unit vector pointing outward from the base centre through the chokepoint.</summary>
      OutwardDir: float32 * float32
      /// <summary>Straight-line distance from the base centre to <c>Position</c>.</summary>
      DistanceFromBase: float32 }

/// <summary>Query parameters bounding a chokepoint search.</summary>
type ChokepointQuery =
    { /// <summary>Maximum width (elmos) for a passage to qualify as a chokepoint.</summary>
      MaxWidthElmos: float32
      /// <summary>Search radius in elmos from the base centre.</summary>
      SearchRadiusElmos: float32
      /// <summary>Movement type used to evaluate passability.</summary>
      MoveType: MoveType }

/// <summary>
/// Distance-transform-based chokepoint detection. Returns stable-ID, width-annotated
/// descriptors for corridors that approach a specified base centre.
/// </summary>
module Chokepoints =

    /// <summary>
    /// Default query for a given move type: <c>MaxWidthElmos = 40.0</c>,
    /// <c>SearchRadiusElmos = 2500.0</c>.
    /// </summary>
    val defaultChokepointQuery: MoveType -> ChokepointQuery

    /// <summary>
    /// Find all chokepoints within the query's search radius around <paramref name="baseCentre"/>,
    /// sorted by distance from base (closest first). Returns an empty list when no passage
    /// satisfies the width threshold and primary-route predicate.
    /// </summary>
    val findChokepoints:
        grid: MapGrid ->
        baseCentre: (float32 * float32 * float32) ->
        query: ChokepointQuery ->
            Chokepoint list

    /// <summary>
    /// Deterministically compute the stable ID for a ridge cell. Exposed so callers can
    /// reference a chokepoint by its <c>Id</c> across successive queries without re-running
    /// the full detection.
    /// </summary>
    val chokepointIdOf: grid: MapGrid -> ridgeCellX: int -> ridgeCellZ: int -> ChokepointId

    /// <summary>
    /// Return the distance-transform raw values for diagnostic or visualisation use.
    /// Dimensions match <c>MapGrid.passability</c> for the given move type. Pure.
    /// </summary>
    val computeDistanceTransform:
        grid: MapGrid ->
        moveType: MoveType ->
        ownStructures: OwnStructureFootprint seq ->
            float32[,]
