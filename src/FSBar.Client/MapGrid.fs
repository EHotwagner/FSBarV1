namespace FSBar.Client

open System.Net.Sockets

/// <summary>
/// Terrain classification based on height and slope data.
/// Used to categorize map cells for AI decision-making.
/// </summary>
[<RequireQualifiedAccess>]
type Terrain =
    /// <summary>Traversable land with the given slope value (0.0 = flat).</summary>
    | Land of hardness: float32
    /// <summary>Water at the given depth (positive value representing depth below sea level).</summary>
    | Water of depth: float32
    /// <summary>Steep cliff with the given slope value (typically above 0.6).</summary>
    | Cliff of slope: float32

/// <summary>
/// Unit movement type for passability computation.
/// Each type has different slope and water traversal thresholds.
/// </summary>
[<RequireQualifiedAccess>]
type MoveType =
    /// <summary>Bipedal robot. Can handle moderate slopes (up to 0.8) but cannot cross water.</summary>
    | Kbot
    /// <summary>Tracked vehicle. Requires gentle slopes (up to 0.4) and cannot cross water.</summary>
    | Tank
    /// <summary>Hovercraft. Tolerates moderate slopes (up to 0.6) and can cross water.</summary>
    | Hover
    /// <summary>Naval vessel. Can only move through water cells.</summary>
    | Ship

/// <summary>
/// Bundled map data layers from the BAR engine, stored as 2D arrays.
/// Contains height, slope, resource, line-of-sight, and radar data.
/// Dimensions are in heightmap grid squares unless otherwise noted.
/// </summary>
type MapGrid =
    { /// <summary>Map width in elmos (world units). Equal to WidthHeightmap * 8.</summary>
      WidthElmos: int
      /// <summary>Map height in elmos (world units). Equal to HeightHeightmap * 8.</summary>
      HeightElmos: int
      /// <summary>Map width in heightmap grid squares.</summary>
      WidthHeightmap: int
      /// <summary>Map height in heightmap grid squares.</summary>
      HeightHeightmap: int
      /// <summary>Corners heightmap of size (WidthHeightmap+1) x (HeightHeightmap+1). Values are world-space heights.</summary>
      HeightMap: float32[,]
      /// <summary>Slope map at half heightmap resolution: (WidthHeightmap/2) x (HeightHeightmap/2). Values range from 0.0 (flat) to 1.0 (vertical).</summary>
      SlopeMap: float32[,]
      /// <summary>Resource density map at heightmap resolution. Higher values indicate richer metal deposits.</summary>
      ResourceMap: int[,]
      /// <summary>Line-of-sight map at heightmap resolution. Non-zero values indicate visible cells.</summary>
      LosMap: int[,]
      /// <summary>Radar coverage map at heightmap resolution. Non-zero values indicate radar-covered cells.</summary>
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

/// <summary>
/// Functions for loading, refreshing, and querying <see cref="T:FSBar.Client.MapGrid"/> data.
/// Provides terrain classification, passability analysis, and active patterns for pattern matching.
/// </summary>
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

    /// <summary>
    /// Loads all map layers (height, slope, resource, LOS, radar) from the engine and assembles them
    /// into a <see cref="T:FSBar.Client.MapGrid"/> record with properly shaped 2D arrays.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>A fully populated <see cref="T:FSBar.Client.MapGrid"/>.</returns>
    /// <exception cref="T:System.Exception">Thrown if any layer returns empty data or has a dimension mismatch.</exception>
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

        // LOS and Radar may be at a lower resolution (e.g., 1/8th of heightmap)
        let losRaw = Callbacks.getLosMap stream |> List.toArray
        let losMap =
            let losSize = losRaw.Length
            if losSize = w * h then
                Array2D.init w h (fun x z -> losRaw.[z * w + x])
            else
                // Compute actual dimensions from element count (square or proportional grid)
                let losSide = int (sqrt (float losSize))
                if losSide * losSide = losSize then
                    Array2D.init losSide losSide (fun x z -> losRaw.[z * losSide + x])
                else
                    Array2D.zeroCreate 1 1

        let radarRaw = Callbacks.getRadarMap stream |> List.toArray
        let radarMap =
            let radarSize = radarRaw.Length
            if radarSize = w * h then
                Array2D.init w h (fun x z -> radarRaw.[z * w + x])
            else
                let radarSide = int (sqrt (float radarSize))
                if radarSide * radarSide = radarSize then
                    Array2D.init radarSide radarSide (fun x z -> radarRaw.[z * radarSide + x])
                else
                    Array2D.zeroCreate 1 1

        let resourceRaw = Callbacks.getResourceMap stream |> List.toArray
        let resourceMap =
            let resSize = resourceRaw.Length
            if resSize = w * h then
                Array2D.init w h (fun x z -> resourceRaw.[z * w + x])
            else
                let resSide = int (sqrt (float resSize))
                if resSide * resSide = resSize then
                    Array2D.init resSide resSide (fun x z -> resourceRaw.[z * resSide + x])
                else
                    Array2D.zeroCreate 1 1

        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = heightMap
          SlopeMap = slopeMap
          ResourceMap = resourceMap
          LosMap = losMap
          RadarMap = radarMap }

    /// <summary>
    /// Refreshes only the line-of-sight layer from the engine, returning an updated grid.
    /// Call this each frame to get current visibility data without reloading static layers.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="grid">The existing map grid to update.</param>
    /// <returns>A new <see cref="T:FSBar.Client.MapGrid"/> with the LOS layer refreshed.</returns>
    let refreshLos (stream: NetworkStream) (grid: MapGrid) : MapGrid =
        let raw = Callbacks.getLosMap stream |> List.toArray
        let currentW = Array2D.length1 grid.LosMap
        let currentH = Array2D.length2 grid.LosMap
        let losMap =
            if raw.Length = currentW * currentH then
                Array2D.init currentW currentH (fun x z -> raw.[z * currentW + x])
            else
                let side = int (sqrt (float raw.Length))
                if side * side = raw.Length then
                    Array2D.init side side (fun x z -> raw.[z * side + x])
                else
                    grid.LosMap
        { grid with LosMap = losMap }

    /// <summary>
    /// Refreshes only the radar layer from the engine, returning an updated grid.
    /// Call this each frame to get current radar coverage without reloading static layers.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="grid">The existing map grid to update.</param>
    /// <returns>A new <see cref="T:FSBar.Client.MapGrid"/> with the radar layer refreshed.</returns>
    let refreshRadar (stream: NetworkStream) (grid: MapGrid) : MapGrid =
        let raw = Callbacks.getRadarMap stream |> List.toArray
        let currentW = Array2D.length1 grid.RadarMap
        let currentH = Array2D.length2 grid.RadarMap
        let radarMap =
            if raw.Length = currentW * currentH then
                Array2D.init currentW currentH (fun x z -> raw.[z * currentW + x])
            else
                let side = int (sqrt (float raw.Length))
                if side * side = raw.Length then
                    Array2D.init side side (fun x z -> raw.[z * side + x])
                else
                    grid.RadarMap
        { grid with RadarMap = radarMap }

    /// <summary>
    /// Classifies the terrain at a heightmap grid cell based on height and slope.
    /// Cells below sea level are Water, cells with slope above 0.6 are Cliff, otherwise Land.
    /// </summary>
    /// <param name="grid">The map grid containing height and slope data.</param>
    /// <param name="x">Heightmap grid X coordinate.</param>
    /// <param name="z">Heightmap grid Z coordinate.</param>
    /// <returns>The <see cref="T:FSBar.Client.Terrain"/> classification for the cell.</returns>
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

    /// <summary>
    /// Computes a boolean passability grid for the given movement type.
    /// Each cell is <c>true</c> if the movement type can traverse it, based on slope thresholds
    /// and water traversal rules: Kbot (slope &lt; 0.8, no water), Tank (slope &lt; 0.4, no water),
    /// Hover (slope &lt; 0.6, can cross water), Ship (water only).
    /// </summary>
    /// <param name="grid">The map grid containing height and slope data.</param>
    /// <param name="moveType">The unit movement type to compute passability for.</param>
    /// <returns>A 2D boolean array where <c>true</c> indicates a passable cell.</returns>
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

    /// <summary>Active pattern that decomposes a <see cref="T:FSBar.Client.Terrain"/> value into Land, Water, or Cliff.</summary>
    /// <param name="terrain">The terrain value to decompose.</param>
    /// <returns>Land with slope, Water with depth, or Cliff with slope value.</returns>
    let (|Land|Water|Cliff|) (terrain: Terrain) =
        match terrain with
        | Terrain.Land h -> Choice1Of3 h
        | Terrain.Water d -> Choice2Of3 d
        | Terrain.Cliff s -> Choice3Of3 s

    /// <summary>
    /// Active pattern that checks whether a heightmap cell is passable for a given movement type.
    /// Returns Passable if the cell is within bounds and traversable, Impassable otherwise.
    /// </summary>
    /// <param name="grid">The map grid containing height and slope data.</param>
    /// <param name="moveType">The unit movement type to check passability for.</param>
    /// <param name="x">Heightmap grid X coordinate.</param>
    /// <param name="z">Heightmap grid Z coordinate.</param>
    /// <returns>Passable or Impassable.</returns>
    let (|Passable|Impassable|) (grid: MapGrid) (moveType: MoveType) (x: int) (z: int) =
        let pass = passability grid moveType
        if x >= 0 && x < Array2D.length1 pass && z >= 0 && z < Array2D.length2 pass && pass.[x, z] then
            Passable
        else
            Impassable
