namespace FSBar.Client

open System.Net.Sockets
open Highbar

/// <summary>
/// Engine callback functions that query live game state via the HighBar V2 proxy.
/// Each function sends a callback request over the Unix domain socket and returns the parsed result.
/// All functions require an active <see cref="T:System.Net.Sockets.NetworkStream"/> connection to the proxy.
/// </summary>
module Callbacks =

    // --- Param helpers ---

    let intParam (v: int) : CallbackParam =
        { Value = CallbackParam.ValueCase.IntValue v }

    // --- Result extraction helpers ---

    let getInt (resp: CallbackResponse) : int =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.IntValue v -> v
            | _ -> 0
        | _ -> 0

    let getFloat (resp: CallbackResponse) : float32 =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.FloatValue v -> v
            | _ -> 0.0f
        | _ -> 0.0f

    let getString (resp: CallbackResponse) : string =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.StringValue v -> v
            | _ -> ""
        | _ -> ""

    let getVector3 (resp: CallbackResponse) : float32 * float32 * float32 =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.VectorValue v -> (v.X, v.Y, v.Z)
            | _ -> (0.0f, 0.0f, 0.0f)
        | _ -> (0.0f, 0.0f, 0.0f)

    let getFloatArray (resp: CallbackResponse) : float32 list =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.FloatArrayValue fa -> fa.Values
            | _ -> []
        | _ -> []

    let getIntArray (resp: CallbackResponse) : int list =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.IntArrayValue ia -> ia.Values
            | _ -> []
        | _ -> []

    // --- No-param callbacks returning int ---

    /// <summary>Queries the engine for this AI's team ID.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>The integer team ID assigned to this AI.</returns>
    let getMyTeam (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGameGetMyTeam) []
        |> getInt

    /// <summary>Queries the engine for this AI's ally-team ID.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>The integer ally-team ID for this AI's alliance group.</returns>
    let getMyAllyTeam (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGameGetMyAllyTeam) []
        |> getInt

    /// <summary>Queries the engine for the map width in heightmap grid squares (1 square = 8 elmos).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Map width in heightmap squares.</returns>
    let getMapWidth (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetWidth) []
        |> getInt

    /// <summary>Queries the engine for the map height in heightmap grid squares (1 square = 8 elmos).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Map height in heightmap squares.</returns>
    let getMapHeight (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetHeight) []
        |> getInt

    // --- Map callbacks ---

    /// <summary>Queries the engine for the start position of a given team.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="teamId">The team ID to query the start position for.</param>
    /// <returns>A tuple of (x, y, z) world coordinates in elmos.</returns>
    let getStartPos (stream: NetworkStream) (teamId: int) : float32 * float32 * float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetStartPos) [ intParam teamId ]
        |> getVector3

    /// <summary>
    /// Queries the engine for all metal extraction spots on the map.
    /// Each spot is returned as (x, y, z, value) where value indicates metal richness.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>An array of (x, y, z, metalValue) tuples in world coordinates.</returns>
    let getMetalSpots (stream: NetworkStream) : (float32 * float32 * float32 * float32) array =
        let resp =
            Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetMetalSpots) []
        let values = getFloatArray resp
        values
        |> List.chunkBySize 4
        |> List.choose (fun chunk ->
            match chunk with
            | [ x; y; z; v ] -> Some (x, y, z, v)
            | _ -> None)
        |> List.toArray

    // --- Unit callbacks ---

    /// <summary>Queries the engine for the current world position of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>A tuple of (x, y, z) world coordinates in elmos.</returns>
    let getUnitPos (stream: NetworkStream) (unitId: int) : float32 * float32 * float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetPos) [ intParam unitId ]
        |> getVector3

    /// <summary>Queries the engine for the current health of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>Current health points as a float.</returns>
    let getUnitHealth (stream: NetworkStream) (unitId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetHealth) [ intParam unitId ]
        |> getFloat

    /// <summary>Queries the engine for the maximum health of a unit.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>Maximum health points as a float.</returns>
    let getUnitMaxHealth (stream: NetworkStream) (unitId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetMaxHealth) [ intParam unitId ]
        |> getFloat

    /// <summary>Queries the engine for the unit-definition ID of a unit instance.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="unitId">The engine-assigned unit ID.</param>
    /// <returns>The unit-definition ID that describes this unit's type.</returns>
    let getUnitDef (stream: NetworkStream) (unitId: int) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetDef) [ intParam unitId ]
        |> getInt

    // --- UnitDef callbacks ---

    /// <summary>Queries the engine for the internal name of a unit definition (e.g., "armcom").</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>The string name of the unit definition.</returns>
    let getUnitDefName (stream: NetworkStream) (defId: int) : string =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetName) [ intParam defId ]
        |> getString

    /// <summary>Queries the engine for the build options of a unit definition (what it can build).</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID of the builder.</param>
    /// <returns>An array of unit-definition IDs that this unit can build.</returns>
    let getBuildOptions (stream: NetworkStream) (defId: int) : int array =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetBuildOptions) [ intParam defId ]
        |> getIntArray
        |> List.toArray

    /// <summary>Queries the engine for the maximum weapon range of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>Maximum weapon range in elmos.</returns>
    let getMaxWeaponRange (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetMaxWeaponRange) [ intParam defId ]
        |> getFloat

    /// <summary>Queries the engine for the build speed of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>Build speed value (higher means faster construction).</returns>
    let getBuildSpeed (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetBuildSpeed) [ intParam defId ]
        |> getFloat

    /// <summary>Queries the engine for the resource cost of a unit definition.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="defId">The unit-definition ID.</param>
    /// <returns>The total resource cost of the unit.</returns>
    let getUnitDefCost (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetCost) [ intParam defId ]
        |> getFloat

    // --- Economy callbacks ---

    /// <summary>Queries the engine for the current stockpile of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Current resource amount.</returns>
    let getEconomyCurrent (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetCurrent) [ intParam resourceId ]
        |> getFloat

    /// <summary>Queries the engine for the per-frame income rate of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Income rate per frame.</returns>
    let getEconomyIncome (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetIncome) [ intParam resourceId ]
        |> getFloat

    /// <summary>Queries the engine for the per-frame usage rate of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Usage rate per frame.</returns>
    let getEconomyUsage (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetUsage) [ intParam resourceId ]
        |> getFloat

    /// <summary>Queries the engine for the maximum storage capacity of a resource.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="resourceId">Resource index (0 = metal, 1 = energy).</param>
    /// <returns>Maximum storage capacity.</returns>
    let getEconomyStorage (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetStorage) [ intParam resourceId ]
        |> getFloat

    // --- Bulk query ---

    /// <summary>Queries the engine for all available unit-definition IDs.</summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <param name="maxCount">Maximum number of unit-definition IDs to retrieve.</param>
    /// <returns>An array of unit-definition IDs.</returns>
    let getUnitDefs (stream: NetworkStream) (maxCount: int) : int array =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGetUnitDefs) [ intParam maxCount ]
        |> getIntArray
        |> List.toArray

    // --- Map data callbacks ---

    /// <summary>
    /// Queries the engine for the full heightmap as a flat list in row-major order.
    /// The heightmap has dimensions (mapWidth) x (mapHeight) in heightmap squares.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of height values in row-major order.</returns>
    let getHeightMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetHeightMap) []
        |> getFloatArray

    /// <summary>
    /// Queries the engine for the corners heightmap as a flat list in row-major order.
    /// Returns (mapWidth+1) x (mapHeight+1) vertex-resolution height values suitable for
    /// constructing an <see cref="T:FSBar.Client.MapGrid"/>.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of corner height values in row-major order.</returns>
    let getCornersHeightMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetCornersHeightMap) []
        |> getFloatArray

    /// <summary>
    /// Queries the engine for the slope map as a flat list in row-major order.
    /// The slope map has dimensions (mapWidth/2) x (mapHeight/2), at half heightmap resolution.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of slope values (0.0 = flat, 1.0 = vertical) in row-major order.</returns>
    let getSlopeMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetSlopeMap) []
        |> getFloatArray

    /// <summary>
    /// Queries the engine for the line-of-sight map as a flat list in row-major order.
    /// Non-zero values indicate cells currently visible to our team.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of LOS values in row-major order.</returns>
    let getLosMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetLosMap) []
        |> getIntArray

    /// <summary>
    /// Queries the engine for the radar coverage map as a flat list in row-major order.
    /// Non-zero values indicate cells covered by our team's radar.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of radar coverage values in row-major order.</returns>
    let getRadarMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetRadarMap) []
        |> getIntArray

    /// <summary>
    /// Queries the engine for the resource distribution map as a flat list in row-major order.
    /// Higher values indicate richer metal deposits.
    /// </summary>
    /// <param name="stream">Active network stream to the HighBar V2 proxy.</param>
    /// <returns>Flat list of resource density values in row-major order.</returns>
    let getResourceMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetResourceMap) []
        |> getIntArray
