namespace FSBar.Client

open System.Net.Sockets

/// Friendly unit entry in a <c>GameStateSnapshotResult</c> (spec 045).
type FriendlyUnitSnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    Health: float32
    UnitDefId: int
    Team: int
}

/// LOS-visible enemy entry in a <c>GameStateSnapshotResult</c>. Has a
/// concrete <c>Health</c> because the engine reports it.
type LosEnemySnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    Health: float32
    UnitDefId: int
    Team: int
}

/// Radar-only enemy entry in a <c>GameStateSnapshotResult</c>. Carries
/// NO <c>Health</c> field by design — radar contacts cannot have a
/// concrete health value and callers must never synthesize one.
type RadarOnlyEnemySnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    UnitDefId: int
    Team: int
}

/// Eight-field resource snapshot returned inside a
/// <c>GameStateSnapshotResult</c>.
type EconomyRecordSnapshot = {
    MetalCurrent: float32
    MetalIncome: float32
    MetalUsage: float32
    MetalStorage: float32
    EnergyCurrent: float32
    EnergyIncome: float32
    EnergyUsage: float32
    EnergyStorage: float32
}

/// Per-tick atomic game-state snapshot returned by
/// <c>CALLBACK_GAME_GET_STATE = 15</c>. Replaces the legacy per-unit /
/// per-enemy / per-resource refresh loop in
/// <c>GameState.processEvent</c>'s <c>GameEvent.Update</c> branch.
type GameStateSnapshotResult = {
    Frame: int
    Friendlies: FriendlyUnitSnapshot list
    LosEnemies: LosEnemySnapshot list
    RadarOnlyEnemies: RadarOnlyEnemySnapshot list
    Economy: EconomyRecordSnapshot
}

/// <summary>
/// Engine callback functions that query live game state via the HighBar V2 proxy.
/// Each function sends a callback request over the Unix domain socket and returns the parsed result.
/// All functions require an active <see cref="T:System.Net.Sockets.NetworkStream"/> connection to the proxy.
/// </summary>
module Callbacks =

    /// <summary>Queries the engine for this AI's team ID.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>The integer team ID assigned to this AI.</returns>
    val getMyTeam: stream: NetworkStream -> int

    /// <summary>Queries the engine for this AI's ally-team ID.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>The integer ally-team ID for this AI's alliance group.</returns>
    val getMyAllyTeam: stream: NetworkStream -> int

    /// <summary>Queries the engine for the map width in heightmap grid squares (1 square = 8 elmos).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Map width in heightmap squares.</returns>
    val getMapWidth: stream: NetworkStream -> int

    /// <summary>Queries the engine for the map height in heightmap grid squares (1 square = 8 elmos).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Map height in heightmap squares.</returns>
    val getMapHeight: stream: NetworkStream -> int

    /// <summary>Queries the engine for the start position of a given team.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="teamId">The team ID to query the start position for.</param>
    /// <returns>A tuple of (x, y, z) world coordinates in elmos.</returns>
    val getStartPos: stream: NetworkStream -> teamId: int -> float32 * float32 * float32

    /// <summary>
    /// Queries the engine for all metal extraction spots on the map.
    /// Each spot is returned as (x, y, z, value) where value indicates metal richness.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>An array of (x, y, z, metalValue) tuples in world coordinates.</returns>
    val getMetalSpots: stream: NetworkStream -> (float32 * float32 * float32 * float32) array

    /// <summary>Queries the engine for the current world position of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>A tuple of (x, y, z) world coordinates in elmos.</returns>
    val getUnitPos: stream: NetworkStream -> unitId: int -> float32 * float32 * float32

    /// <summary>Queries the engine for the current health of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>Current health points as a float.</returns>
    val getUnitHealth: stream: NetworkStream -> unitId: int -> float32

    /// <summary>Queries the engine for the maximum health of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>Maximum health points as a float.</returns>
    val getUnitMaxHealth: stream: NetworkStream -> unitId: int -> float32

    /// <summary>Queries the engine for the unit-definition ID of a unit instance.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>The unit-definition ID that describes this unit's type.</returns>
    val getUnitDef: stream: NetworkStream -> unitId: int -> int

    /// <summary>Queries the engine for the internal name of a unit definition (e.g., "armcom").</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>The string name of the unit definition.</returns>
    val getUnitDefName: stream: NetworkStream -> defId: int -> string

    /// <summary>Queries the engine for the build options of a unit definition (what it can build).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID of the builder.</param>
    /// <returns>An array of unit-definition IDs that this unit can build.</returns>
    val getBuildOptions: stream: NetworkStream -> defId: int -> int array

    /// <summary>Queries the engine for the maximum weapon range of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>Maximum weapon range in elmos.</returns>
    val getMaxWeaponRange: stream: NetworkStream -> defId: int -> float32

    /// <summary>Queries the engine for the build speed of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>Build speed value (higher means faster construction).</returns>
    val getBuildSpeed: stream: NetworkStream -> defId: int -> float32

    /// <summary>Queries the engine for the resource cost of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>The total resource cost of the unit.</returns>
    val getUnitDefCost: stream: NetworkStream -> defId: int -> float32

    /// <summary>Queries the engine for the current stockpile of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Current resource amount.</returns>
    val getEconomyCurrent: stream: NetworkStream -> resourceId: int -> float32

    /// <summary>Queries the engine for the per-frame income rate of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Income rate per frame.</returns>
    val getEconomyIncome: stream: NetworkStream -> resourceId: int -> float32

    /// <summary>Queries the engine for the per-frame usage rate of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Usage rate per frame.</returns>
    val getEconomyUsage: stream: NetworkStream -> resourceId: int -> float32

    /// <summary>Queries the engine for the maximum storage capacity of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Maximum storage capacity.</returns>
    val getEconomyStorage: stream: NetworkStream -> resourceId: int -> float32

    /// <summary>Queries the engine for all available unit-definition IDs.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="maxCount">Maximum number of unit-definition IDs to retrieve.</param>
    /// <returns>An array of unit-definition IDs.</returns>
    val getUnitDefs: stream: NetworkStream -> maxCount: int -> int array

    /// <summary>
    /// Queries the engine for the full heightmap as a flat list in row-major order.
    /// The heightmap has dimensions (mapWidth) x (mapHeight) in heightmap squares.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of height values in row-major order.</returns>
    val getHeightMap: stream: NetworkStream -> float32 list

    /// <summary>
    /// Queries the engine for the corners heightmap as a flat list in row-major order.
    /// Returns (mapWidth+1) x (mapHeight+1) vertex-resolution height values suitable for
    /// constructing a <see cref="T:FSBar.Client.MapGrid"/>.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of corner height values in row-major order.</returns>
    val getCornersHeightMap: stream: NetworkStream -> float32 list

    /// <summary>
    /// Queries the engine for the slope map as a flat list in row-major order.
    /// The slope map has dimensions (mapWidth/2) x (mapHeight/2), at half heightmap resolution.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of slope values (0.0 = flat, 1.0 = vertical) in row-major order.</returns>
    val getSlopeMap: stream: NetworkStream -> float32 list

    /// <summary>
    /// Queries the engine for the line-of-sight map as a flat list in row-major order.
    /// Non-zero values indicate cells currently visible to our team.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of LOS values in row-major order.</returns>
    val getLosMap: stream: NetworkStream -> int list

    /// <summary>
    /// Queries the engine for the radar coverage map as a flat list in row-major order.
    /// Non-zero values indicate cells covered by our team's radar.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of radar coverage values in row-major order.</returns>
    val getRadarMap: stream: NetworkStream -> int list

    /// <summary>
    /// Queries the engine for the resource distribution map as a flat list in row-major order.
    /// Higher values indicate richer metal deposits.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of resource density values in row-major order.</returns>
    val getResourceMap: stream: NetworkStream -> int list

    /// <summary>
    /// Issues one <c>CALLBACK_GAME_GET_STATE = 15</c> RPC and returns a
    /// per-tick atomic snapshot: friendlies + LOS enemies + radar-only
    /// enemies + 8-field economy record (spec 045, HighBarV2 032).
    /// Replaces the legacy per-unit / per-enemy / per-resource refresh
    /// loop — exactly one RPC regardless of army size.
    /// </summary>
    /// <remarks>
    /// <para>Raises <see cref="T:FSBar.Client.ProxyVersionMismatchException"/>
    /// when the proxy rejects callback 15 with "Unknown callback id" —
    /// no legacy fallback.</para>
    /// <para>Raises <see cref="T:System.InvalidOperationException"/> with
    /// a descriptive message on other failures (cap exceeded, proxy
    /// error). The caller's prior <c>GameState</c> is left untouched;
    /// no partial application.</para>
    /// <para>Raises <see cref="T:FSBar.Client.EngineDisconnectedException"/>
    /// on connection loss (unchanged).</para>
    /// </remarks>
    val getGameStateSnapshot: stream: NetworkStream -> GameStateSnapshotResult
