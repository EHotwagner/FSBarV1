namespace FSBar.Client

module MapQuery =

    /// Get the height at an elmo coordinate. Returns Error for out-of-bounds.
    val heightAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// Get the slope at an elmo coordinate. Returns Error for out-of-bounds.
    val slopeAtElmo: grid: MapGrid -> x: int -> z: int -> Result<float32, string>

    /// Get the terrain classification at an elmo coordinate.
    val terrainAtElmo: grid: MapGrid -> x: int -> z: int -> Result<Terrain, string>

    /// Extract a rectangular sub-region of the heightmap by elmo bounds.
    val heightSubRegion: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> Result<float32[,], string>

    /// Find all cells in a region where resource value exceeds threshold, sorted descending.
    val resourceHotspots: grid: MapGrid -> x1: int -> z1: int -> x2: int -> z2: int -> threshold: int -> (int * int * int) list

    /// Convert elmo coordinates to heightmap grid indices.
    val elmoToGrid: x: int -> z: int -> int * int

    /// Convert heightmap grid indices to elmo coordinates.
    val gridToElmo: x: int -> z: int -> int * int
