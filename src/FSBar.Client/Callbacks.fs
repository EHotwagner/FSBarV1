namespace FSBar.Client

open System.Net.Sockets
open Highbar

module Callbacks =

    // --- Param helpers ---

    let private intParam (v: int) : CallbackParam =
        { Value = CallbackParam.ValueCase.IntValue v }

    // --- Result extraction helpers ---

    let private getInt (resp: CallbackResponse) : int =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.IntValue v -> v
            | _ -> 0
        | _ -> 0

    let private getFloat (resp: CallbackResponse) : float32 =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.FloatValue v -> v
            | _ -> 0.0f
        | _ -> 0.0f

    let private getString (resp: CallbackResponse) : string =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.StringValue v -> v
            | _ -> ""
        | _ -> ""

    let private getVector3 (resp: CallbackResponse) : float32 * float32 * float32 =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.VectorValue v -> (v.X, v.Y, v.Z)
            | _ -> (0.0f, 0.0f, 0.0f)
        | _ -> (0.0f, 0.0f, 0.0f)

    let private getFloatArray (resp: CallbackResponse) : float32 list =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.FloatArrayValue fa -> fa.Values
            | _ -> []
        | _ -> []

    let private getIntArray (resp: CallbackResponse) : int list =
        match resp.Result with
        | Some r ->
            match r.Value with
            | CallbackResult.ValueCase.IntArrayValue ia -> ia.Values
            | _ -> []
        | _ -> []

    // --- No-param callbacks returning int ---

    let getMyTeam (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGameGetMyTeam) []
        |> getInt

    let getMyAllyTeam (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGameGetMyAllyTeam) []
        |> getInt

    let getMapWidth (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetWidth) []
        |> getInt

    let getMapHeight (stream: NetworkStream) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetHeight) []
        |> getInt

    // --- Map callbacks ---

    let getStartPos (stream: NetworkStream) (teamId: int) : float32 * float32 * float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetStartPos) [ intParam teamId ]
        |> getVector3

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

    let getUnitPos (stream: NetworkStream) (unitId: int) : float32 * float32 * float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetPos) [ intParam unitId ]
        |> getVector3

    let getUnitHealth (stream: NetworkStream) (unitId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetHealth) [ intParam unitId ]
        |> getFloat

    let getUnitMaxHealth (stream: NetworkStream) (unitId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetMaxHealth) [ intParam unitId ]
        |> getFloat

    let getUnitDef (stream: NetworkStream) (unitId: int) : int =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitGetDef) [ intParam unitId ]
        |> getInt

    // --- UnitDef callbacks ---

    let getUnitDefName (stream: NetworkStream) (defId: int) : string =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetName) [ intParam defId ]
        |> getString

    let getBuildOptions (stream: NetworkStream) (defId: int) : int array =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetBuildOptions) [ intParam defId ]
        |> getIntArray
        |> List.toArray

    let getMaxWeaponRange (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetMaxWeaponRange) [ intParam defId ]
        |> getFloat

    let getBuildSpeed (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetBuildSpeed) [ intParam defId ]
        |> getFloat

    let getUnitDefCost (stream: NetworkStream) (defId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackUnitdefGetCost) [ intParam defId ]
        |> getFloat

    // --- Economy callbacks ---

    let getEconomyCurrent (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetCurrent) [ intParam resourceId ]
        |> getFloat

    let getEconomyIncome (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetIncome) [ intParam resourceId ]
        |> getFloat

    let getEconomyUsage (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetUsage) [ intParam resourceId ]
        |> getFloat

    let getEconomyStorage (stream: NetworkStream) (resourceId: int) : float32 =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackEconomyGetStorage) [ intParam resourceId ]
        |> getFloat

    // --- Bulk query ---

    let getUnitDefs (stream: NetworkStream) (maxCount: int) : int array =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackGetUnitDefs) [ intParam maxCount ]
        |> getIntArray
        |> List.toArray

    // --- Map data callbacks ---

    let getHeightMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetHeightMap) []
        |> getFloatArray

    let getCornersHeightMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetCornersHeightMap) []
        |> getFloatArray

    let getSlopeMap (stream: NetworkStream) : float32 list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetSlopeMap) []
        |> getFloatArray

    let getLosMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetLosMap) []
        |> getIntArray

    let getRadarMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetRadarMap) []
        |> getIntArray

    let getResourceMap (stream: NetworkStream) : int list =
        Protocol.sendCallback stream (uint32 CallbackId.CallbackMapGetResourceMap) []
        |> getIntArray
