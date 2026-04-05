namespace FSBar.Client

open System
open System.Collections.Concurrent
open System.Net.Sockets

module MapCache =

    let private gridCache = ConcurrentDictionary<string, Lazy<MapGrid>>()
    let private passabilityCache = ConcurrentDictionary<string, Lazy<bool[,]>>()

    let fromEngine (stream: NetworkStream) : MapGrid =
        gridCache.GetOrAdd("__engine__", Lazy<MapGrid>(fun () ->
            MapGrid.loadFromEngine stream)).Value

    let passability (grid: MapGrid) (moveType: MoveType) : bool[,] =
        let key = string moveType
        passabilityCache.GetOrAdd(key, Lazy<bool[,]>(fun () ->
            MapGrid.passability grid moveType)).Value

    let clear () =
        gridCache.Clear()
        passabilityCache.Clear()
