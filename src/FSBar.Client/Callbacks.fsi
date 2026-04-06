namespace FSBar.Client

open System.Net.Sockets

module Callbacks =

    /// Get the team ID for this AI.
    val getMyTeam: stream: NetworkStream -> int

    /// Get the ally-team ID for this AI.
    val getMyAllyTeam: stream: NetworkStream -> int

    /// Get the map width in heightmap squares.
    val getMapWidth: stream: NetworkStream -> int

    /// Get the map height in heightmap squares.
    val getMapHeight: stream: NetworkStream -> int

    /// Get the start position for a given team.
    val getStartPos: stream: NetworkStream -> teamId: int -> float32 * float32 * float32

    /// Get all metal spots as (x, y, z, value) tuples.
    val getMetalSpots: stream: NetworkStream -> (float32 * float32 * float32 * float32) array

    /// Get the position of a unit.
    val getUnitPos: stream: NetworkStream -> unitId: int -> float32 * float32 * float32

    /// Get the current health of a unit.
    val getUnitHealth: stream: NetworkStream -> unitId: int -> float32

    /// Get the maximum health of a unit.
    val getUnitMaxHealth: stream: NetworkStream -> unitId: int -> float32

    /// Get the unit-definition ID for a unit.
    val getUnitDef: stream: NetworkStream -> unitId: int -> int

    /// Get the name of a unit definition.
    val getUnitDefName: stream: NetworkStream -> defId: int -> string

    /// Get the build options (unit-def IDs) for a unit definition.
    val getBuildOptions: stream: NetworkStream -> defId: int -> int array

    /// Get the maximum weapon range for a unit definition.
    val getMaxWeaponRange: stream: NetworkStream -> defId: int -> float32

    /// Get the build speed for a unit definition.
    val getBuildSpeed: stream: NetworkStream -> defId: int -> float32

    /// Get the cost for a unit definition.
    val getUnitDefCost: stream: NetworkStream -> defId: int -> float32

    /// Get the current amount of a resource.
    val getEconomyCurrent: stream: NetworkStream -> resourceId: int -> float32

    /// Get the income rate of a resource.
    val getEconomyIncome: stream: NetworkStream -> resourceId: int -> float32

    /// Get the usage rate of a resource.
    val getEconomyUsage: stream: NetworkStream -> resourceId: int -> float32

    /// Get the storage capacity of a resource.
    val getEconomyStorage: stream: NetworkStream -> resourceId: int -> float32

    /// Get all available unit-definition IDs up to maxCount.
    val getUnitDefs: stream: NetworkStream -> maxCount: int -> int array

    /// Get the full heightmap as a flat float32 list (row-major order).
    val getHeightMap: stream: NetworkStream -> float32 list

    /// Get the corners heightmap as a flat float32 list (row-major order).
    /// Returns (mapWidth+1)*(mapHeight+1) vertex-resolution height values.
    val getCornersHeightMap: stream: NetworkStream -> float32 list

    /// Get the full slope map as a flat float32 list (row-major order).
    val getSlopeMap: stream: NetworkStream -> float32 list

    /// Get the line-of-sight map as a flat int list (row-major order).
    val getLosMap: stream: NetworkStream -> int list

    /// Get the radar coverage map as a flat int list (row-major order).
    val getRadarMap: stream: NetworkStream -> int list

    /// Get the resource distribution map as a flat int list (row-major order).
    val getResourceMap: stream: NetworkStream -> int list
