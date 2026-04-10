namespace FSBar.Viz

open FSBar.Client

module MapData =
    val save: path: string -> grid: MapGrid -> metalSpots: (float32 * float32 * float32 * float32) array -> unit
    val load: path: string -> MapGrid * (float32 * float32 * float32 * float32) array
