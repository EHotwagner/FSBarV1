namespace FSBar.Client

open System.Net.Sockets

[<RequireQualifiedAccess>]
type Terrain =
    | Land of hardness: float32
    | Water of depth: float32
    | Cliff of slope: float32

[<RequireQualifiedAccess>]
type MoveType =
    | Kbot
    | Tank
    | Hover
    | Ship

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
    override this.ToString() =
        let layers =
            [ if Array2D.length1 this.HeightMap > 0 then "Height"
              if Array2D.length1 this.SlopeMap > 0 then "Slope"
              if Array2D.length1 this.ResourceMap > 0 then "Resource"
              if Array2D.length1 this.LosMap > 0 then "LOS"
              if Array2D.length1 this.RadarMap > 0 then "Radar" ]
        sprintf "MapGrid { %dx%d elmos, %dx%d heightmap, %d layers loaded }"
            this.WidthElmos this.HeightElmos
            this.WidthHeightmap this.HeightHeightmap
            layers.Length

module MapGrid =

    // --- Private helpers: reshape flat lists to Array2D ---

    let private toFloat32Array2D (width: int) (height: int) (layerName: string) (values: float32 list) : float32[,] =
        let expected = width * height
        let arr = values |> List.toArray
        if arr.Length = 0 then
            failwith $"MapGrid: {layerName} returned empty array from engine — proxy may not support this callback"
        if arr.Length <> expected then
            failwith $"MapGrid: {layerName} dimension mismatch — expected {expected} ({width}x{height}), got {arr.Length}"
        Array2D.init width height (fun x z -> arr.[z * width + x])

    let private toIntArray2D (width: int) (height: int) (layerName: string) (values: int list) : int[,] =
        let expected = width * height
        let arr = values |> List.toArray
        if arr.Length = 0 then
            failwith $"MapGrid: {layerName} returned empty array from engine — proxy may not support this callback"
        if arr.Length <> expected then
            failwith $"MapGrid: {layerName} dimension mismatch — expected {expected} ({width}x{height}), got {arr.Length}"
        Array2D.init width height (fun x z -> arr.[z * width + x])

    let loadFromEngine (stream: NetworkStream) : MapGrid =
        let w = Callbacks.getMapWidth stream
        let h = Callbacks.getMapHeight stream

        let hmW = w + 1
        let hmH = h + 1

        let heightMap =
            Callbacks.getCornersHeightMap stream
            |> toFloat32Array2D hmW hmH "HeightMap"

        let slopeW = w / 2
        let slopeH = h / 2

        let slopeMap =
            Callbacks.getSlopeMap stream
            |> toFloat32Array2D slopeW slopeH "SlopeMap"

        let losMap =
            Callbacks.getLosMap stream
            |> toIntArray2D w h "LosMap"

        let radarMap =
            Callbacks.getRadarMap stream
            |> toIntArray2D w h "RadarMap"

        let resourceMap =
            Callbacks.getResourceMap stream
            |> toIntArray2D w h "ResourceMap"

        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = heightMap
          SlopeMap = slopeMap
          ResourceMap = resourceMap
          LosMap = losMap
          RadarMap = radarMap }

    let refreshLos (stream: NetworkStream) (grid: MapGrid) : MapGrid =
        let losMap =
            Callbacks.getLosMap stream
            |> toIntArray2D grid.WidthHeightmap grid.HeightHeightmap "LosMap"
        { grid with LosMap = losMap }

    let refreshRadar (stream: NetworkStream) (grid: MapGrid) : MapGrid =
        let radarMap =
            Callbacks.getRadarMap stream
            |> toIntArray2D grid.WidthHeightmap grid.HeightHeightmap "RadarMap"
        { grid with RadarMap = radarMap }

    let terrainAt (grid: MapGrid) (x: int) (z: int) : Terrain =
        let h = grid.HeightMap.[x, z]
        let sx = min (x / 2) (Array2D.length1 grid.SlopeMap - 1)
        let sz = min (z / 2) (Array2D.length2 grid.SlopeMap - 1)
        let s = grid.SlopeMap.[sx, sz]
        if h < 0.0f then
            Terrain.Water -h
        elif s > 0.6f then
            Terrain.Cliff s
        else
            Terrain.Land s

    /// Slope thresholds per movement type
    let passability (grid: MapGrid) (moveType: MoveType) : bool[,] =
        let w = Array2D.length1 grid.HeightMap
        let h = Array2D.length2 grid.HeightMap
        Array2D.init w h (fun x z ->
            let height = grid.HeightMap.[x, z]
            let sx = min (x / 2) (Array2D.length1 grid.SlopeMap - 1)
            let sz = min (z / 2) (Array2D.length2 grid.SlopeMap - 1)
            let slope = grid.SlopeMap.[sx, sz]
            let isWater = height < 0.0f
            match moveType with
            | MoveType.Kbot ->
                slope < 0.8f && not isWater
            | MoveType.Tank ->
                slope < 0.4f && not isWater
            | MoveType.Hover ->
                slope < 0.6f  // can cross water
            | MoveType.Ship ->
                isWater)

    let (|Land|Water|Cliff|) (terrain: Terrain) =
        match terrain with
        | Terrain.Land h -> Choice1Of3 h
        | Terrain.Water d -> Choice2Of3 d
        | Terrain.Cliff s -> Choice3Of3 s

    let (|Passable|Impassable|) (grid: MapGrid) (moveType: MoveType) (x: int) (z: int) =
        let pass = passability grid moveType
        if x >= 0 && x < Array2D.length1 pass && z >= 0 && z < Array2D.length2 pass && pass.[x, z] then
            Passable
        else
            Impassable
