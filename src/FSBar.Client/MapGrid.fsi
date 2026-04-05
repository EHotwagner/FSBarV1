namespace FSBar.Client

open System.Net.Sockets

/// Terrain classification based on height and slope data.
[<RequireQualifiedAccess>]
type Terrain =
    | Land of hardness: float32
    | Water of depth: float32
    | Cliff of slope: float32

/// Unit movement type for passability computation.
[<RequireQualifiedAccess>]
type MoveType =
    | Kbot
    | Tank
    | Hover
    | Ship

/// Bundled map data layers from the BAR engine.
type MapGrid =
    { WidthElmos: int
      HeightElmos: int
      WidthHeightmap: int
      HeightHeightmap: int
      HeightMap: float32[,]
      SlopeMap: float32[,]
      ResourceMap: int[,]
      LosMap: int[,]
      RadarMap: int[,] }
    override ToString: unit -> string

module MapGrid =

    /// Load all map layers from the engine into a MapGrid record.
    /// Raises on empty arrays or dimension mismatches.
    val loadFromEngine: stream: NetworkStream -> MapGrid

    /// Refresh the LOS map layer from the engine.
    val refreshLos: stream: NetworkStream -> grid: MapGrid -> MapGrid

    /// Refresh the radar map layer from the engine.
    val refreshRadar: stream: NetworkStream -> grid: MapGrid -> MapGrid

    /// Classify terrain at a heightmap grid cell.
    val terrainAt: grid: MapGrid -> x: int -> z: int -> Terrain

    /// Compute a passability grid for the given movement type.
    val passability: grid: MapGrid -> moveType: MoveType -> bool[,]

    /// Classify terrain by type — for use in pattern matching.
    val (|Land|Water|Cliff|) : Terrain -> Choice<float32, float32, float32>

    /// Check if a heightmap cell is passable for a given move type.
    val (|Passable|Impassable|) : grid: MapGrid -> moveType: MoveType -> x: int -> z: int -> Choice<unit, unit>
