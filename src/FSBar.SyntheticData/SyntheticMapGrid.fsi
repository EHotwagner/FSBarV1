module FSBar.SyntheticData.SyntheticMapGrid

open FSBar.Client

/// Build a flat MapGrid with all cells Land (slope 0, height 10), zero metal/LOS/radar.
/// `width` and `height` are heightmap grid squares. HeightMap is (width+1) × (height+1),
/// SlopeMap/ResourceMap/LosMap/RadarMap are at heightmap resolution.
val flat : width: int -> height: int -> MapGrid

/// Stamp a line of impassable cells along a Bresenham line between (x1,z1) and (x2,z2).
/// Impassability is encoded as slope 1.0 (rejected by every MoveType).
val withWall : grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> MapGrid

/// Stamp a circular high-slope zone of radius `radius` cells at (centreX, centreZ).
val withCliff : grid: MapGrid -> centreX: int -> centreZ: int -> radius: int -> MapGrid

/// Stamp a metal spot value at heightmap cell (x, z).
val withMetalSpot : grid: MapGrid -> x: int -> z: int -> value: int -> MapGrid

/// A width×height flat grid with a single N-S impassable wall near the middle
/// of the heightmap and a `gapCells`-tall open gap centred at heightmap-corner
/// z = height/2. Pass `gapCells = 0` for a fully sealed wall.
val oneGapCorridor : width: int -> height: int -> gapCells: int -> MapGrid

/// Build a MapGrid suitable for rendering tests. Parameters:
/// - `width` / `height`: heightmap grid squares.
/// - `seed`: `None` → deterministic gradient values (height/slope/resource gradients);
///           `Some s` → gradient seeded with `s` (same deterministic formula per seed).
val build : parameters: {| width: int; height: int; seed: int option |} -> MapGrid
