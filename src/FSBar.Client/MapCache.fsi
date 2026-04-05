namespace FSBar.Client

open System.Net.Sockets

module MapCache =

    /// Load a MapGrid from the engine, caching the result.
    /// Subsequent calls return the cached grid.
    val fromEngine: stream: NetworkStream -> MapGrid

    /// Get or compute a passability grid for the given movement type.
    /// Cached per MoveType after first computation.
    val passability: grid: MapGrid -> moveType: MoveType -> bool[,]

    /// Clear all cached data (useful for new game sessions).
    val clear: unit -> unit
