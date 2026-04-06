namespace FSBar.Client

open System
open System.Collections.Concurrent
open System.Net.Sockets

/// <summary>
/// Thread-safe caching layer for <see cref="T:FSBar.Client.MapGrid"/> and passability data.
/// Avoids redundant engine callbacks by caching results using <see cref="T:System.Lazy`1"/> for safe concurrent access.
/// </summary>
module MapCache =

    let private gridCache = ConcurrentDictionary<string, Lazy<MapGrid>>()
    let private passabilityCache = ConcurrentDictionary<string, Lazy<bool[,]>>()

    /// <summary>
    /// Loads a <see cref="T:FSBar.Client.MapGrid"/> from the engine, caching the result.
    /// Subsequent calls return the cached grid without additional engine callbacks.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>The cached or newly loaded <see cref="T:FSBar.Client.MapGrid"/>.</returns>
    let fromEngine (stream: NetworkStream) : MapGrid =
        gridCache.GetOrAdd("__engine__", Lazy<MapGrid>(fun () ->
            MapGrid.loadFromEngine stream)).Value

    /// <summary>
    /// Gets or computes a passability grid for the given movement type, caching the result.
    /// Each <see cref="T:FSBar.Client.MoveType"/> is cached independently.
    /// </summary>
    /// <param name="grid">The map grid to compute passability from.</param>
    /// <param name="moveType">The unit movement type to compute passability for.</param>
    /// <returns>A cached 2D boolean array where <c>true</c> indicates a passable cell.</returns>
    let passability (grid: MapGrid) (moveType: MoveType) : bool[,] =
        let key = string moveType
        passabilityCache.GetOrAdd(key, Lazy<bool[,]>(fun () ->
            MapGrid.passability grid moveType)).Value

    /// <summary>
    /// Clears all cached map grid and passability data.
    /// Call this when starting a new game session to avoid stale data.
    /// </summary>
    let clear () =
        gridCache.Clear()
        passabilityCache.Clear()
