namespace FSBar.Viz

open FSBar.Client

/// Binary save/load for MapGrid data and metal spots.
/// File format uses magic bytes "FSMG" with version header for forward compatibility.
module MapData =
    /// Save a MapGrid and its associated metal spots to a binary file.
    val save: path: string -> grid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> unit

    /// Load a MapGrid and metal spots from a binary file.
    /// Throws if the file has invalid magic bytes, unsupported version, or corrupted dimensions.
    val load: path: string -> MapGrid * (float32 * float32 * float32 * float32) array
