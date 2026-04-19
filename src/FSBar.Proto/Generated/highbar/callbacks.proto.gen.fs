namespace rec Highbar
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


/// <summary>Callback ID enum for common engine callbacks</summary>
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<CallbackId>>)>]
type CallbackId =
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNKNOWN")>] CallbackUnknown = 0
/// <summary>Engine</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ENGINE_HANDLE_COMMAND")>] CallbackEngineHandleCommand = 1
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ENGINE_VERSION_MAJOR")>] CallbackEngineVersionMajor = 2
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ENGINE_VERSION_MINOR")>] CallbackEngineVersionMinor = 3
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ENGINE_VERSION_PATCH")>] CallbackEngineVersionPatch = 4
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ENGINE_VERSION_STRING")>] CallbackEngineVersionString = 5
/// <summary>Game</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_MY_TEAM")>] CallbackGameGetMyTeam = 10
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_MY_ALLY_TEAM")>] CallbackGameGetMyAllyTeam = 11
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_TEAM_COUNT")>] CallbackGameGetTeamCount = 12
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_ALLY_TEAM_COUNT")>] CallbackGameGetAllyTeamCount = 13
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_PLAYER_COUNT")>] CallbackGameGetPlayerCount = 14
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GAME_GET_STATE")>] CallbackGameGetState = 15
/// <summary>Unit</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_POS")>] CallbackUnitGetPos = 20
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_HEALTH")>] CallbackUnitGetHealth = 21
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_MAX_HEALTH")>] CallbackUnitGetMaxHealth = 22
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_DEF")>] CallbackUnitGetDef = 23
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_SPEED")>] CallbackUnitGetSpeed = 24
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_POWER")>] CallbackUnitGetPower = 25
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_TEAM")>] CallbackUnitGetTeam = 26
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_ALLY_TEAM")>] CallbackUnitGetAllyTeam = 27
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_MAX_SPEED")>] CallbackUnitGetMaxSpeed = 28
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_EXPERIENCE")>] CallbackUnitGetExperience = 29
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_GROUP")>] CallbackUnitGetGroup = 30
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNIT_GET_CURRENT_COMMANDS")>] CallbackUnitGetCurrentCommands = 31
/// <summary>UnitDef</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_NAME")>] CallbackUnitdefGetName = 40
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_HUMAN_NAME")>] CallbackUnitdefGetHumanName = 41
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_BUILD_OPTIONS")>] CallbackUnitdefGetBuildOptions = 42
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_MAX_WEAPON_RANGE")>] CallbackUnitdefGetMaxWeaponRange = 43
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_COST")>] CallbackUnitdefGetCost = 44
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_BUILD_TIME")>] CallbackUnitdefGetBuildTime = 45
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_UNITDEF_GET_BUILD_SPEED")>] CallbackUnitdefGetBuildSpeed = 46
/// <summary>Map</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_WIDTH")>] CallbackMapGetWidth = 50
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_HEIGHT")>] CallbackMapGetHeight = 51
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_HEIGHT_MAP")>] CallbackMapGetHeightMap = 52
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_SLOPE_MAP")>] CallbackMapGetSlopeMap = 53
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_LOS_MAP")>] CallbackMapGetLosMap = 54
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_RADAR_MAP")>] CallbackMapGetRadarMap = 55
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_RESOURCE_MAP")>] CallbackMapGetResourceMap = 56
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_START_POS")>] CallbackMapGetStartPos = 57
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_METAL_SPOTS")>] CallbackMapGetMetalSpots = 58
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MAP_GET_CORNERS_HEIGHT_MAP")>] CallbackMapGetCornersHeightMap = 59
/// <summary>Resource / Economy</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_RESOURCE_GET_NAME")>] CallbackResourceGetName = 60
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ECONOMY_GET_CURRENT")>] CallbackEconomyGetCurrent = 61
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ECONOMY_GET_INCOME")>] CallbackEconomyGetIncome = 62
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ECONOMY_GET_USAGE")>] CallbackEconomyGetUsage = 63
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_ECONOMY_GET_STORAGE")>] CallbackEconomyGetStorage = 64
/// <summary>Team</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_TEAM_GET_ALLY_TEAM")>] CallbackTeamGetAllyTeam = 70
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_TEAM_IS_ALLY")>] CallbackTeamIsAlly = 71
/// <summary>Mod</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MOD_GET_SHORT_NAME")>] CallbackModGetShortName = 80
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_MOD_GET_VERSION")>] CallbackModGetVersion = 81
/// <summary>UnitDefs (bulk query)</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_GET_UNIT_DEFS")>] CallbackGetUnitDefs = 47
/// <summary>Cheats</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_CHEATS_IS_ENABLED")>] CallbackCheatsIsEnabled = 90
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_CHEATS_SET_ENABLED")>] CallbackCheatsSetEnabled = 91
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_CHEATS_SET_EVENTS_ENABLED")>] CallbackCheatsSetEventsEnabled = 92
/// <summary>DataDirs</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_DATADIRS_GET_CONFIG_DIR")>] CallbackDatadirsGetConfigDir = 100
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_DATADIRS_GET_WRITABLE_DIR")>] CallbackDatadirsGetWritableDir = 101
/// <summary>Info (used for config loading)</summary>
| [<FsGrpc.Protobuf.ProtobufName("CALLBACK_INFO_GET_VALUE_BY_KEY")>] CallbackInfoGetValueByKey = 110

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallbackRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable RequestId: uint32 // (1)
            val mutable CallbackId: uint32 // (2)
            val mutable Params: RepeatedBuilder<Highbar.CallbackParam> // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.RequestId <- ValueCodec.UInt32.ReadValue reader
            | 2 -> x.CallbackId <- ValueCodec.UInt32.ReadValue reader
            | 3 -> x.Params.Add (ValueCodec.Message<Highbar.CallbackParam>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallbackRequest = {
            RequestId = x.RequestId
            CallbackId = x.CallbackId
            Params = x.Params.Build
            }

/// <summary>Request to invoke an engine callback function</summary>
type private _CallbackRequest = CallbackRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallbackRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("requestId")>] RequestId: uint32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("callbackId")>] CallbackId: uint32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("params")>] Params: Highbar.CallbackParam list // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<CallbackRequest>> =
        lazy
        // Field Definitions
        let RequestId = FieldCodec.Primitive ValueCodec.UInt32 (1, "requestId")
        let CallbackId = FieldCodec.Primitive ValueCodec.UInt32 (2, "callbackId")
        let Params = FieldCodec.Repeated ValueCodec.Message<Highbar.CallbackParam> (3, "params")
        // Proto Definition Implementation
        { // ProtoDef<CallbackRequest>
            Name = "CallbackRequest"
            Empty = {
                RequestId = RequestId.GetDefault()
                CallbackId = CallbackId.GetDefault()
                Params = Params.GetDefault()
                }
            Size = fun (m: CallbackRequest) ->
                0
                + RequestId.CalcFieldSize m.RequestId
                + CallbackId.CalcFieldSize m.CallbackId
                + Params.CalcFieldSize m.Params
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallbackRequest) ->
                RequestId.WriteField w m.RequestId
                CallbackId.WriteField w m.CallbackId
                Params.WriteField w m.Params
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallbackRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeRequestId = RequestId.WriteJsonField o
                let writeCallbackId = CallbackId.WriteJsonField o
                let writeParams = Params.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallbackRequest) =
                    writeRequestId w m.RequestId
                    writeCallbackId w m.CallbackId
                    writeParams w m.Params
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallbackRequest =
                    match kvPair.Key with
                    | "requestId" -> { value with RequestId = RequestId.ReadJsonField kvPair.Value }
                    | "callbackId" -> { value with CallbackId = CallbackId.ReadJsonField kvPair.Value }
                    | "params" -> { value with Params = Params.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallbackRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallbackRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallbackResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable RequestId: uint32 // (1)
            val mutable Success: bool // (2)
            val mutable Result: OptionBuilder<Highbar.CallbackResult> // (3)
            val mutable ErrorMessage: string // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.RequestId <- ValueCodec.UInt32.ReadValue reader
            | 2 -> x.Success <- ValueCodec.Bool.ReadValue reader
            | 3 -> x.Result.Set (ValueCodec.Message<Highbar.CallbackResult>.ReadValue reader)
            | 4 -> x.ErrorMessage <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallbackResponse = {
            RequestId = x.RequestId
            Success = x.Success
            Result = x.Result.Build
            ErrorMessage = x.ErrorMessage |> orEmptyString
            }

/// <summary>Response from engine callback invocation</summary>
type private _CallbackResponse = CallbackResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallbackResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("requestId")>] RequestId: uint32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("success")>] Success: bool // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Highbar.CallbackResult option // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("errorMessage")>] ErrorMessage: string // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<CallbackResponse>> =
        lazy
        // Field Definitions
        let RequestId = FieldCodec.Primitive ValueCodec.UInt32 (1, "requestId")
        let Success = FieldCodec.Primitive ValueCodec.Bool (2, "success")
        let Result = FieldCodec.Optional ValueCodec.Message<Highbar.CallbackResult> (3, "result")
        let ErrorMessage = FieldCodec.Primitive ValueCodec.String (4, "errorMessage")
        // Proto Definition Implementation
        { // ProtoDef<CallbackResponse>
            Name = "CallbackResponse"
            Empty = {
                RequestId = RequestId.GetDefault()
                Success = Success.GetDefault()
                Result = Result.GetDefault()
                ErrorMessage = ErrorMessage.GetDefault()
                }
            Size = fun (m: CallbackResponse) ->
                0
                + RequestId.CalcFieldSize m.RequestId
                + Success.CalcFieldSize m.Success
                + Result.CalcFieldSize m.Result
                + ErrorMessage.CalcFieldSize m.ErrorMessage
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallbackResponse) ->
                RequestId.WriteField w m.RequestId
                Success.WriteField w m.Success
                Result.WriteField w m.Result
                ErrorMessage.WriteField w m.ErrorMessage
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallbackResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeRequestId = RequestId.WriteJsonField o
                let writeSuccess = Success.WriteJsonField o
                let writeResult = Result.WriteJsonField o
                let writeErrorMessage = ErrorMessage.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallbackResponse) =
                    writeRequestId w m.RequestId
                    writeSuccess w m.Success
                    writeResult w m.Result
                    writeErrorMessage w m.ErrorMessage
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallbackResponse =
                    match kvPair.Key with
                    | "requestId" -> { value with RequestId = RequestId.ReadJsonField kvPair.Value }
                    | "success" -> { value with Success = Success.ReadJsonField kvPair.Value }
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "errorMessage" -> { value with ErrorMessage = ErrorMessage.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallbackResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallbackResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallbackParam =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<ValueCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type ValueCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("intValue")>] IntValue of int
    | [<System.Text.Json.Serialization.JsonPropertyName("floatValue")>] FloatValue of float32
    | [<System.Text.Json.Serialization.JsonPropertyName("stringValue")>] StringValue of string
    | [<System.Text.Json.Serialization.JsonPropertyName("vectorValue")>] VectorValue of Highbar.Vector3
    with
        static member OneofCodec : Lazy<OneofCodec<ValueCase>> = 
            lazy
            let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (1, "intValue")
            let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Float (2, "floatValue")
            let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (3, "stringValue")
            let VectorValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.Vector3> (4, "vectorValue")
            let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
                ("intValue", fun node -> ValueCase.IntValue (IntValue.ReadJsonField node))
                ("floatValue", fun node -> ValueCase.FloatValue (FloatValue.ReadJsonField node))
                ("stringValue", fun node -> ValueCase.StringValue (StringValue.ReadJsonField node))
                ("vectorValue", fun node -> ValueCase.VectorValue (VectorValue.ReadJsonField node))
                ])
            Value

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Value: OptionBuilder<Highbar.CallbackParam.ValueCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Value.Set (ValueCase.IntValue (ValueCodec.Int32.ReadValue reader))
            | 2 -> x.Value.Set (ValueCase.FloatValue (ValueCodec.Float.ReadValue reader))
            | 3 -> x.Value.Set (ValueCase.StringValue (ValueCodec.String.ReadValue reader))
            | 4 -> x.Value.Set (ValueCase.VectorValue (ValueCodec.Message<Highbar.Vector3>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallbackParam = {
            Value = x.Value.Build |> (Option.defaultValue ValueCase.None)
            }

/// <summary>Typed parameter for callback invocation</summary>
type private _CallbackParam = CallbackParam
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallbackParam = {
    // Field Declarations
    Value: Highbar.CallbackParam.ValueCase
    }
    with
    static member Proto : Lazy<ProtoDef<CallbackParam>> =
        lazy
        // Field Definitions
        let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (1, "intValue")
        let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Float (2, "floatValue")
        let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (3, "stringValue")
        let VectorValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.Vector3> (4, "vectorValue")
        let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
            ("intValue", fun node -> Highbar.CallbackParam.ValueCase.IntValue (IntValue.ReadJsonField node))
            ("floatValue", fun node -> Highbar.CallbackParam.ValueCase.FloatValue (FloatValue.ReadJsonField node))
            ("stringValue", fun node -> Highbar.CallbackParam.ValueCase.StringValue (StringValue.ReadJsonField node))
            ("vectorValue", fun node -> Highbar.CallbackParam.ValueCase.VectorValue (VectorValue.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<CallbackParam>
            Name = "CallbackParam"
            Empty = {
                Value = Highbar.CallbackParam.ValueCase.None
                }
            Size = fun (m: CallbackParam) ->
                0
                + match m.Value with
                    | Highbar.CallbackParam.ValueCase.None -> 0
                    | Highbar.CallbackParam.ValueCase.IntValue v -> IntValue.CalcFieldSize v
                    | Highbar.CallbackParam.ValueCase.FloatValue v -> FloatValue.CalcFieldSize v
                    | Highbar.CallbackParam.ValueCase.StringValue v -> StringValue.CalcFieldSize v
                    | Highbar.CallbackParam.ValueCase.VectorValue v -> VectorValue.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallbackParam) ->
                (match m.Value with
                | Highbar.CallbackParam.ValueCase.None -> ()
                | Highbar.CallbackParam.ValueCase.IntValue v -> IntValue.WriteField w v
                | Highbar.CallbackParam.ValueCase.FloatValue v -> FloatValue.WriteField w v
                | Highbar.CallbackParam.ValueCase.StringValue v -> StringValue.WriteField w v
                | Highbar.CallbackParam.ValueCase.VectorValue v -> VectorValue.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallbackParam.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValueNone = Value.WriteJsonNoneCase o
                let writeIntValue = IntValue.WriteJsonField o
                let writeFloatValue = FloatValue.WriteJsonField o
                let writeStringValue = StringValue.WriteJsonField o
                let writeVectorValue = VectorValue.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallbackParam) =
                    (match m.Value with
                    | Highbar.CallbackParam.ValueCase.None -> writeValueNone w
                    | Highbar.CallbackParam.ValueCase.IntValue v -> writeIntValue w v
                    | Highbar.CallbackParam.ValueCase.FloatValue v -> writeFloatValue w v
                    | Highbar.CallbackParam.ValueCase.StringValue v -> writeStringValue w v
                    | Highbar.CallbackParam.ValueCase.VectorValue v -> writeVectorValue w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallbackParam =
                    match kvPair.Key with
                    | "intValue" -> { value with Value = Highbar.CallbackParam.ValueCase.IntValue (IntValue.ReadJsonField kvPair.Value) }
                    | "floatValue" -> { value with Value = Highbar.CallbackParam.ValueCase.FloatValue (FloatValue.ReadJsonField kvPair.Value) }
                    | "stringValue" -> { value with Value = Highbar.CallbackParam.ValueCase.StringValue (StringValue.ReadJsonField kvPair.Value) }
                    | "vectorValue" -> { value with Value = Highbar.CallbackParam.ValueCase.VectorValue (VectorValue.ReadJsonField kvPair.Value) }
                    | "value" -> { value with Value = Value.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallbackParam.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallbackParam.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CallbackResult =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<ValueCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type ValueCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("intValue")>] IntValue of int
    | [<System.Text.Json.Serialization.JsonPropertyName("floatValue")>] FloatValue of float32
    | [<System.Text.Json.Serialization.JsonPropertyName("stringValue")>] StringValue of string
    | [<System.Text.Json.Serialization.JsonPropertyName("vectorValue")>] VectorValue of Highbar.Vector3
    | [<System.Text.Json.Serialization.JsonPropertyName("bytesValue")>] BytesValue of FsGrpc.Bytes
    | [<System.Text.Json.Serialization.JsonPropertyName("floatArrayValue")>] FloatArrayValue of Highbar.FloatArray
    | [<System.Text.Json.Serialization.JsonPropertyName("intArrayValue")>] IntArrayValue of Highbar.IntArray
    | [<System.Text.Json.Serialization.JsonPropertyName("snapshotValue")>] SnapshotValue of Highbar.GameStateSnapshot
    with
        static member OneofCodec : Lazy<OneofCodec<ValueCase>> = 
            lazy
            let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (1, "intValue")
            let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Float (2, "floatValue")
            let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (3, "stringValue")
            let VectorValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.Vector3> (4, "vectorValue")
            let BytesValue = FieldCodec.OneofCase "value" ValueCodec.Bytes (5, "bytesValue")
            let FloatArrayValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.FloatArray> (6, "floatArrayValue")
            let IntArrayValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.IntArray> (7, "intArrayValue")
            let SnapshotValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.GameStateSnapshot> (8, "snapshotValue")
            let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
                ("intValue", fun node -> ValueCase.IntValue (IntValue.ReadJsonField node))
                ("floatValue", fun node -> ValueCase.FloatValue (FloatValue.ReadJsonField node))
                ("stringValue", fun node -> ValueCase.StringValue (StringValue.ReadJsonField node))
                ("vectorValue", fun node -> ValueCase.VectorValue (VectorValue.ReadJsonField node))
                ("bytesValue", fun node -> ValueCase.BytesValue (BytesValue.ReadJsonField node))
                ("floatArrayValue", fun node -> ValueCase.FloatArrayValue (FloatArrayValue.ReadJsonField node))
                ("intArrayValue", fun node -> ValueCase.IntArrayValue (IntArrayValue.ReadJsonField node))
                ("snapshotValue", fun node -> ValueCase.SnapshotValue (SnapshotValue.ReadJsonField node))
                ])
            Value

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Value: OptionBuilder<Highbar.CallbackResult.ValueCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Value.Set (ValueCase.IntValue (ValueCodec.Int32.ReadValue reader))
            | 2 -> x.Value.Set (ValueCase.FloatValue (ValueCodec.Float.ReadValue reader))
            | 3 -> x.Value.Set (ValueCase.StringValue (ValueCodec.String.ReadValue reader))
            | 4 -> x.Value.Set (ValueCase.VectorValue (ValueCodec.Message<Highbar.Vector3>.ReadValue reader))
            | 5 -> x.Value.Set (ValueCase.BytesValue (ValueCodec.Bytes.ReadValue reader))
            | 6 -> x.Value.Set (ValueCase.FloatArrayValue (ValueCodec.Message<Highbar.FloatArray>.ReadValue reader))
            | 7 -> x.Value.Set (ValueCase.IntArrayValue (ValueCodec.Message<Highbar.IntArray>.ReadValue reader))
            | 8 -> x.Value.Set (ValueCase.SnapshotValue (ValueCodec.Message<Highbar.GameStateSnapshot>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.CallbackResult = {
            Value = x.Value.Build |> (Option.defaultValue ValueCase.None)
            }

/// <summary>Typed result from callback invocation</summary>
type private _CallbackResult = CallbackResult
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CallbackResult = {
    // Field Declarations
    Value: Highbar.CallbackResult.ValueCase
    }
    with
    static member Proto : Lazy<ProtoDef<CallbackResult>> =
        lazy
        // Field Definitions
        let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (1, "intValue")
        let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Float (2, "floatValue")
        let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (3, "stringValue")
        let VectorValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.Vector3> (4, "vectorValue")
        let BytesValue = FieldCodec.OneofCase "value" ValueCodec.Bytes (5, "bytesValue")
        let FloatArrayValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.FloatArray> (6, "floatArrayValue")
        let IntArrayValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.IntArray> (7, "intArrayValue")
        let SnapshotValue = FieldCodec.OneofCase "value" ValueCodec.Message<Highbar.GameStateSnapshot> (8, "snapshotValue")
        let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
            ("intValue", fun node -> Highbar.CallbackResult.ValueCase.IntValue (IntValue.ReadJsonField node))
            ("floatValue", fun node -> Highbar.CallbackResult.ValueCase.FloatValue (FloatValue.ReadJsonField node))
            ("stringValue", fun node -> Highbar.CallbackResult.ValueCase.StringValue (StringValue.ReadJsonField node))
            ("vectorValue", fun node -> Highbar.CallbackResult.ValueCase.VectorValue (VectorValue.ReadJsonField node))
            ("bytesValue", fun node -> Highbar.CallbackResult.ValueCase.BytesValue (BytesValue.ReadJsonField node))
            ("floatArrayValue", fun node -> Highbar.CallbackResult.ValueCase.FloatArrayValue (FloatArrayValue.ReadJsonField node))
            ("intArrayValue", fun node -> Highbar.CallbackResult.ValueCase.IntArrayValue (IntArrayValue.ReadJsonField node))
            ("snapshotValue", fun node -> Highbar.CallbackResult.ValueCase.SnapshotValue (SnapshotValue.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<CallbackResult>
            Name = "CallbackResult"
            Empty = {
                Value = Highbar.CallbackResult.ValueCase.None
                }
            Size = fun (m: CallbackResult) ->
                0
                + match m.Value with
                    | Highbar.CallbackResult.ValueCase.None -> 0
                    | Highbar.CallbackResult.ValueCase.IntValue v -> IntValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.FloatValue v -> FloatValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.StringValue v -> StringValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.VectorValue v -> VectorValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.BytesValue v -> BytesValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.FloatArrayValue v -> FloatArrayValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.IntArrayValue v -> IntArrayValue.CalcFieldSize v
                    | Highbar.CallbackResult.ValueCase.SnapshotValue v -> SnapshotValue.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CallbackResult) ->
                (match m.Value with
                | Highbar.CallbackResult.ValueCase.None -> ()
                | Highbar.CallbackResult.ValueCase.IntValue v -> IntValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.FloatValue v -> FloatValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.StringValue v -> StringValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.VectorValue v -> VectorValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.BytesValue v -> BytesValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.FloatArrayValue v -> FloatArrayValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.IntArrayValue v -> IntArrayValue.WriteField w v
                | Highbar.CallbackResult.ValueCase.SnapshotValue v -> SnapshotValue.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.CallbackResult.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValueNone = Value.WriteJsonNoneCase o
                let writeIntValue = IntValue.WriteJsonField o
                let writeFloatValue = FloatValue.WriteJsonField o
                let writeStringValue = StringValue.WriteJsonField o
                let writeVectorValue = VectorValue.WriteJsonField o
                let writeBytesValue = BytesValue.WriteJsonField o
                let writeFloatArrayValue = FloatArrayValue.WriteJsonField o
                let writeIntArrayValue = IntArrayValue.WriteJsonField o
                let writeSnapshotValue = SnapshotValue.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CallbackResult) =
                    (match m.Value with
                    | Highbar.CallbackResult.ValueCase.None -> writeValueNone w
                    | Highbar.CallbackResult.ValueCase.IntValue v -> writeIntValue w v
                    | Highbar.CallbackResult.ValueCase.FloatValue v -> writeFloatValue w v
                    | Highbar.CallbackResult.ValueCase.StringValue v -> writeStringValue w v
                    | Highbar.CallbackResult.ValueCase.VectorValue v -> writeVectorValue w v
                    | Highbar.CallbackResult.ValueCase.BytesValue v -> writeBytesValue w v
                    | Highbar.CallbackResult.ValueCase.FloatArrayValue v -> writeFloatArrayValue w v
                    | Highbar.CallbackResult.ValueCase.IntArrayValue v -> writeIntArrayValue w v
                    | Highbar.CallbackResult.ValueCase.SnapshotValue v -> writeSnapshotValue w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CallbackResult =
                    match kvPair.Key with
                    | "intValue" -> { value with Value = Highbar.CallbackResult.ValueCase.IntValue (IntValue.ReadJsonField kvPair.Value) }
                    | "floatValue" -> { value with Value = Highbar.CallbackResult.ValueCase.FloatValue (FloatValue.ReadJsonField kvPair.Value) }
                    | "stringValue" -> { value with Value = Highbar.CallbackResult.ValueCase.StringValue (StringValue.ReadJsonField kvPair.Value) }
                    | "vectorValue" -> { value with Value = Highbar.CallbackResult.ValueCase.VectorValue (VectorValue.ReadJsonField kvPair.Value) }
                    | "bytesValue" -> { value with Value = Highbar.CallbackResult.ValueCase.BytesValue (BytesValue.ReadJsonField kvPair.Value) }
                    | "floatArrayValue" -> { value with Value = Highbar.CallbackResult.ValueCase.FloatArrayValue (FloatArrayValue.ReadJsonField kvPair.Value) }
                    | "intArrayValue" -> { value with Value = Highbar.CallbackResult.ValueCase.IntArrayValue (IntArrayValue.ReadJsonField kvPair.Value) }
                    | "snapshotValue" -> { value with Value = Highbar.CallbackResult.ValueCase.SnapshotValue (SnapshotValue.ReadJsonField kvPair.Value) }
                    | "value" -> { value with Value = Value.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallbackResult.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallbackResult.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FriendlyUnit =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
            val mutable Health: float32 // (3)
            val mutable UnitDefId: int // (4)
            val mutable Team: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.Health <- ValueCodec.Float.ReadValue reader
            | 4 -> x.UnitDefId <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Team <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.FriendlyUnit = {
            UnitId = x.UnitId
            Position = x.Position.Build
            Health = x.Health
            UnitDefId = x.UnitDefId
            Team = x.Team
            }

/// <summary>
/// Per-tick batched GameState snapshot (spec 045 / HighBarV2 032).
/// Returned by CALLBACK_GAME_GET_STATE=15 in a single RPC round-trip,
/// collapsing per-unit refreshUnit + per-resource Economy_* calls.
/// </summary>
type private _FriendlyUnit = FriendlyUnit
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FriendlyUnit = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("health")>] Health: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("unitDefId")>] UnitDefId: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("team")>] Team: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<FriendlyUnit>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        let Health = FieldCodec.Primitive ValueCodec.Float (3, "health")
        let UnitDefId = FieldCodec.Primitive ValueCodec.Int32 (4, "unitDefId")
        let Team = FieldCodec.Primitive ValueCodec.Int32 (5, "team")
        // Proto Definition Implementation
        { // ProtoDef<FriendlyUnit>
            Name = "FriendlyUnit"
            Empty = {
                UnitId = UnitId.GetDefault()
                Position = Position.GetDefault()
                Health = Health.GetDefault()
                UnitDefId = UnitDefId.GetDefault()
                Team = Team.GetDefault()
                }
            Size = fun (m: FriendlyUnit) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + Position.CalcFieldSize m.Position
                + Health.CalcFieldSize m.Health
                + UnitDefId.CalcFieldSize m.UnitDefId
                + Team.CalcFieldSize m.Team
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FriendlyUnit) ->
                UnitId.WriteField w m.UnitId
                Position.WriteField w m.Position
                Health.WriteField w m.Health
                UnitDefId.WriteField w m.UnitDefId
                Team.WriteField w m.Team
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.FriendlyUnit.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeHealth = Health.WriteJsonField o
                let writeUnitDefId = UnitDefId.WriteJsonField o
                let writeTeam = Team.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FriendlyUnit) =
                    writeUnitId w m.UnitId
                    writePosition w m.Position
                    writeHealth w m.Health
                    writeUnitDefId w m.UnitDefId
                    writeTeam w m.Team
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FriendlyUnit =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "health" -> { value with Health = Health.ReadJsonField kvPair.Value }
                    | "unitDefId" -> { value with UnitDefId = UnitDefId.ReadJsonField kvPair.Value }
                    | "team" -> { value with Team = Team.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FriendlyUnit.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._FriendlyUnit.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LosEnemyUnit =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
            val mutable Health: float32 // (3)
            val mutable UnitDefId: int // (4)
            val mutable Team: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.Health <- ValueCodec.Float.ReadValue reader
            | 4 -> x.UnitDefId <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.Team <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LosEnemyUnit = {
            UnitId = x.UnitId
            Position = x.Position.Build
            Health = x.Health
            UnitDefId = x.UnitDefId
            Team = x.Team
            }

type private _LosEnemyUnit = LosEnemyUnit
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LosEnemyUnit = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("health")>] Health: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("unitDefId")>] UnitDefId: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("team")>] Team: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<LosEnemyUnit>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        let Health = FieldCodec.Primitive ValueCodec.Float (3, "health")
        let UnitDefId = FieldCodec.Primitive ValueCodec.Int32 (4, "unitDefId")
        let Team = FieldCodec.Primitive ValueCodec.Int32 (5, "team")
        // Proto Definition Implementation
        { // ProtoDef<LosEnemyUnit>
            Name = "LosEnemyUnit"
            Empty = {
                UnitId = UnitId.GetDefault()
                Position = Position.GetDefault()
                Health = Health.GetDefault()
                UnitDefId = UnitDefId.GetDefault()
                Team = Team.GetDefault()
                }
            Size = fun (m: LosEnemyUnit) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + Position.CalcFieldSize m.Position
                + Health.CalcFieldSize m.Health
                + UnitDefId.CalcFieldSize m.UnitDefId
                + Team.CalcFieldSize m.Team
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LosEnemyUnit) ->
                UnitId.WriteField w m.UnitId
                Position.WriteField w m.Position
                Health.WriteField w m.Health
                UnitDefId.WriteField w m.UnitDefId
                Team.WriteField w m.Team
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LosEnemyUnit.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeHealth = Health.WriteJsonField o
                let writeUnitDefId = UnitDefId.WriteJsonField o
                let writeTeam = Team.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LosEnemyUnit) =
                    writeUnitId w m.UnitId
                    writePosition w m.Position
                    writeHealth w m.Health
                    writeUnitDefId w m.UnitDefId
                    writeTeam w m.Team
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LosEnemyUnit =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "health" -> { value with Health = Health.ReadJsonField kvPair.Value }
                    | "unitDefId" -> { value with UnitDefId = UnitDefId.ReadJsonField kvPair.Value }
                    | "team" -> { value with Team = Team.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LosEnemyUnit.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LosEnemyUnit.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RadarOnlyEnemyUnit =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitId: int // (1)
            val mutable Position: OptionBuilder<Highbar.Vector3> // (2)
            val mutable UnitDefId: int // (3)
            val mutable Team: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Position.Set (ValueCodec.Message<Highbar.Vector3>.ReadValue reader)
            | 3 -> x.UnitDefId <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.Team <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.RadarOnlyEnemyUnit = {
            UnitId = x.UnitId
            Position = x.Position.Build
            UnitDefId = x.UnitDefId
            Team = x.Team
            }

/// <summary>
/// NOTE: no health field by design — radar-only contacts cannot have a
/// concrete health value and the client must never synthesize one.
/// </summary>
type private _RadarOnlyEnemyUnit = RadarOnlyEnemyUnit
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RadarOnlyEnemyUnit = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitId")>] UnitId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("position")>] Position: Highbar.Vector3 option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("unitDefId")>] UnitDefId: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("team")>] Team: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<RadarOnlyEnemyUnit>> =
        lazy
        // Field Definitions
        let UnitId = FieldCodec.Primitive ValueCodec.Int32 (1, "unitId")
        let Position = FieldCodec.Optional ValueCodec.Message<Highbar.Vector3> (2, "position")
        let UnitDefId = FieldCodec.Primitive ValueCodec.Int32 (3, "unitDefId")
        let Team = FieldCodec.Primitive ValueCodec.Int32 (4, "team")
        // Proto Definition Implementation
        { // ProtoDef<RadarOnlyEnemyUnit>
            Name = "RadarOnlyEnemyUnit"
            Empty = {
                UnitId = UnitId.GetDefault()
                Position = Position.GetDefault()
                UnitDefId = UnitDefId.GetDefault()
                Team = Team.GetDefault()
                }
            Size = fun (m: RadarOnlyEnemyUnit) ->
                0
                + UnitId.CalcFieldSize m.UnitId
                + Position.CalcFieldSize m.Position
                + UnitDefId.CalcFieldSize m.UnitDefId
                + Team.CalcFieldSize m.Team
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RadarOnlyEnemyUnit) ->
                UnitId.WriteField w m.UnitId
                Position.WriteField w m.Position
                UnitDefId.WriteField w m.UnitDefId
                Team.WriteField w m.Team
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.RadarOnlyEnemyUnit.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitId = UnitId.WriteJsonField o
                let writePosition = Position.WriteJsonField o
                let writeUnitDefId = UnitDefId.WriteJsonField o
                let writeTeam = Team.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RadarOnlyEnemyUnit) =
                    writeUnitId w m.UnitId
                    writePosition w m.Position
                    writeUnitDefId w m.UnitDefId
                    writeTeam w m.Team
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RadarOnlyEnemyUnit =
                    match kvPair.Key with
                    | "unitId" -> { value with UnitId = UnitId.ReadJsonField kvPair.Value }
                    | "position" -> { value with Position = Position.ReadJsonField kvPair.Value }
                    | "unitDefId" -> { value with UnitDefId = UnitDefId.ReadJsonField kvPair.Value }
                    | "team" -> { value with Team = Team.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RadarOnlyEnemyUnit.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._RadarOnlyEnemyUnit.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EconomyRecord =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable MetalCurrent: float32 // (1)
            val mutable MetalIncome: float32 // (2)
            val mutable MetalUsage: float32 // (3)
            val mutable MetalStorage: float32 // (4)
            val mutable EnergyCurrent: float32 // (5)
            val mutable EnergyIncome: float32 // (6)
            val mutable EnergyUsage: float32 // (7)
            val mutable EnergyStorage: float32 // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.MetalCurrent <- ValueCodec.Float.ReadValue reader
            | 2 -> x.MetalIncome <- ValueCodec.Float.ReadValue reader
            | 3 -> x.MetalUsage <- ValueCodec.Float.ReadValue reader
            | 4 -> x.MetalStorage <- ValueCodec.Float.ReadValue reader
            | 5 -> x.EnergyCurrent <- ValueCodec.Float.ReadValue reader
            | 6 -> x.EnergyIncome <- ValueCodec.Float.ReadValue reader
            | 7 -> x.EnergyUsage <- ValueCodec.Float.ReadValue reader
            | 8 -> x.EnergyStorage <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.EconomyRecord = {
            MetalCurrent = x.MetalCurrent
            MetalIncome = x.MetalIncome
            MetalUsage = x.MetalUsage
            MetalStorage = x.MetalStorage
            EnergyCurrent = x.EnergyCurrent
            EnergyIncome = x.EnergyIncome
            EnergyUsage = x.EnergyUsage
            EnergyStorage = x.EnergyStorage
            }

type private _EconomyRecord = EconomyRecord
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EconomyRecord = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("metalCurrent")>] MetalCurrent: float32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("metalIncome")>] MetalIncome: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("metalUsage")>] MetalUsage: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("metalStorage")>] MetalStorage: float32 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("energyCurrent")>] EnergyCurrent: float32 // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("energyIncome")>] EnergyIncome: float32 // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("energyUsage")>] EnergyUsage: float32 // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("energyStorage")>] EnergyStorage: float32 // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<EconomyRecord>> =
        lazy
        // Field Definitions
        let MetalCurrent = FieldCodec.Primitive ValueCodec.Float (1, "metalCurrent")
        let MetalIncome = FieldCodec.Primitive ValueCodec.Float (2, "metalIncome")
        let MetalUsage = FieldCodec.Primitive ValueCodec.Float (3, "metalUsage")
        let MetalStorage = FieldCodec.Primitive ValueCodec.Float (4, "metalStorage")
        let EnergyCurrent = FieldCodec.Primitive ValueCodec.Float (5, "energyCurrent")
        let EnergyIncome = FieldCodec.Primitive ValueCodec.Float (6, "energyIncome")
        let EnergyUsage = FieldCodec.Primitive ValueCodec.Float (7, "energyUsage")
        let EnergyStorage = FieldCodec.Primitive ValueCodec.Float (8, "energyStorage")
        // Proto Definition Implementation
        { // ProtoDef<EconomyRecord>
            Name = "EconomyRecord"
            Empty = {
                MetalCurrent = MetalCurrent.GetDefault()
                MetalIncome = MetalIncome.GetDefault()
                MetalUsage = MetalUsage.GetDefault()
                MetalStorage = MetalStorage.GetDefault()
                EnergyCurrent = EnergyCurrent.GetDefault()
                EnergyIncome = EnergyIncome.GetDefault()
                EnergyUsage = EnergyUsage.GetDefault()
                EnergyStorage = EnergyStorage.GetDefault()
                }
            Size = fun (m: EconomyRecord) ->
                0
                + MetalCurrent.CalcFieldSize m.MetalCurrent
                + MetalIncome.CalcFieldSize m.MetalIncome
                + MetalUsage.CalcFieldSize m.MetalUsage
                + MetalStorage.CalcFieldSize m.MetalStorage
                + EnergyCurrent.CalcFieldSize m.EnergyCurrent
                + EnergyIncome.CalcFieldSize m.EnergyIncome
                + EnergyUsage.CalcFieldSize m.EnergyUsage
                + EnergyStorage.CalcFieldSize m.EnergyStorage
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EconomyRecord) ->
                MetalCurrent.WriteField w m.MetalCurrent
                MetalIncome.WriteField w m.MetalIncome
                MetalUsage.WriteField w m.MetalUsage
                MetalStorage.WriteField w m.MetalStorage
                EnergyCurrent.WriteField w m.EnergyCurrent
                EnergyIncome.WriteField w m.EnergyIncome
                EnergyUsage.WriteField w m.EnergyUsage
                EnergyStorage.WriteField w m.EnergyStorage
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.EconomyRecord.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeMetalCurrent = MetalCurrent.WriteJsonField o
                let writeMetalIncome = MetalIncome.WriteJsonField o
                let writeMetalUsage = MetalUsage.WriteJsonField o
                let writeMetalStorage = MetalStorage.WriteJsonField o
                let writeEnergyCurrent = EnergyCurrent.WriteJsonField o
                let writeEnergyIncome = EnergyIncome.WriteJsonField o
                let writeEnergyUsage = EnergyUsage.WriteJsonField o
                let writeEnergyStorage = EnergyStorage.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EconomyRecord) =
                    writeMetalCurrent w m.MetalCurrent
                    writeMetalIncome w m.MetalIncome
                    writeMetalUsage w m.MetalUsage
                    writeMetalStorage w m.MetalStorage
                    writeEnergyCurrent w m.EnergyCurrent
                    writeEnergyIncome w m.EnergyIncome
                    writeEnergyUsage w m.EnergyUsage
                    writeEnergyStorage w m.EnergyStorage
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EconomyRecord =
                    match kvPair.Key with
                    | "metalCurrent" -> { value with MetalCurrent = MetalCurrent.ReadJsonField kvPair.Value }
                    | "metalIncome" -> { value with MetalIncome = MetalIncome.ReadJsonField kvPair.Value }
                    | "metalUsage" -> { value with MetalUsage = MetalUsage.ReadJsonField kvPair.Value }
                    | "metalStorage" -> { value with MetalStorage = MetalStorage.ReadJsonField kvPair.Value }
                    | "energyCurrent" -> { value with EnergyCurrent = EnergyCurrent.ReadJsonField kvPair.Value }
                    | "energyIncome" -> { value with EnergyIncome = EnergyIncome.ReadJsonField kvPair.Value }
                    | "energyUsage" -> { value with EnergyUsage = EnergyUsage.ReadJsonField kvPair.Value }
                    | "energyStorage" -> { value with EnergyStorage = EnergyStorage.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EconomyRecord.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._EconomyRecord.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GameStateSnapshot =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Frame: int // (1)
            val mutable Friendlies: RepeatedBuilder<Highbar.FriendlyUnit> // (2)
            val mutable LosEnemies: RepeatedBuilder<Highbar.LosEnemyUnit> // (3)
            val mutable RadarOnlyEnemies: RepeatedBuilder<Highbar.RadarOnlyEnemyUnit> // (4)
            val mutable Economy: OptionBuilder<Highbar.EconomyRecord> // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Frame <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Friendlies.Add (ValueCodec.Message<Highbar.FriendlyUnit>.ReadValue reader)
            | 3 -> x.LosEnemies.Add (ValueCodec.Message<Highbar.LosEnemyUnit>.ReadValue reader)
            | 4 -> x.RadarOnlyEnemies.Add (ValueCodec.Message<Highbar.RadarOnlyEnemyUnit>.ReadValue reader)
            | 5 -> x.Economy.Set (ValueCodec.Message<Highbar.EconomyRecord>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.GameStateSnapshot = {
            Frame = x.Frame
            Friendlies = x.Friendlies.Build
            LosEnemies = x.LosEnemies.Build
            RadarOnlyEnemies = x.RadarOnlyEnemies.Build
            Economy = x.Economy.Build
            }

type private _GameStateSnapshot = GameStateSnapshot
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GameStateSnapshot = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("frame")>] Frame: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("friendlies")>] Friendlies: Highbar.FriendlyUnit list // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("losEnemies")>] LosEnemies: Highbar.LosEnemyUnit list // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("radarOnlyEnemies")>] RadarOnlyEnemies: Highbar.RadarOnlyEnemyUnit list // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("economy")>] Economy: Highbar.EconomyRecord option // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<GameStateSnapshot>> =
        lazy
        // Field Definitions
        let Frame = FieldCodec.Primitive ValueCodec.Int32 (1, "frame")
        let Friendlies = FieldCodec.Repeated ValueCodec.Message<Highbar.FriendlyUnit> (2, "friendlies")
        let LosEnemies = FieldCodec.Repeated ValueCodec.Message<Highbar.LosEnemyUnit> (3, "losEnemies")
        let RadarOnlyEnemies = FieldCodec.Repeated ValueCodec.Message<Highbar.RadarOnlyEnemyUnit> (4, "radarOnlyEnemies")
        let Economy = FieldCodec.Optional ValueCodec.Message<Highbar.EconomyRecord> (5, "economy")
        // Proto Definition Implementation
        { // ProtoDef<GameStateSnapshot>
            Name = "GameStateSnapshot"
            Empty = {
                Frame = Frame.GetDefault()
                Friendlies = Friendlies.GetDefault()
                LosEnemies = LosEnemies.GetDefault()
                RadarOnlyEnemies = RadarOnlyEnemies.GetDefault()
                Economy = Economy.GetDefault()
                }
            Size = fun (m: GameStateSnapshot) ->
                0
                + Frame.CalcFieldSize m.Frame
                + Friendlies.CalcFieldSize m.Friendlies
                + LosEnemies.CalcFieldSize m.LosEnemies
                + RadarOnlyEnemies.CalcFieldSize m.RadarOnlyEnemies
                + Economy.CalcFieldSize m.Economy
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GameStateSnapshot) ->
                Frame.WriteField w m.Frame
                Friendlies.WriteField w m.Friendlies
                LosEnemies.WriteField w m.LosEnemies
                RadarOnlyEnemies.WriteField w m.RadarOnlyEnemies
                Economy.WriteField w m.Economy
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.GameStateSnapshot.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrame = Frame.WriteJsonField o
                let writeFriendlies = Friendlies.WriteJsonField o
                let writeLosEnemies = LosEnemies.WriteJsonField o
                let writeRadarOnlyEnemies = RadarOnlyEnemies.WriteJsonField o
                let writeEconomy = Economy.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GameStateSnapshot) =
                    writeFrame w m.Frame
                    writeFriendlies w m.Friendlies
                    writeLosEnemies w m.LosEnemies
                    writeRadarOnlyEnemies w m.RadarOnlyEnemies
                    writeEconomy w m.Economy
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GameStateSnapshot =
                    match kvPair.Key with
                    | "frame" -> { value with Frame = Frame.ReadJsonField kvPair.Value }
                    | "friendlies" -> { value with Friendlies = Friendlies.ReadJsonField kvPair.Value }
                    | "losEnemies" -> { value with LosEnemies = LosEnemies.ReadJsonField kvPair.Value }
                    | "radarOnlyEnemies" -> { value with RadarOnlyEnemies = RadarOnlyEnemies.ReadJsonField kvPair.Value }
                    | "economy" -> { value with Economy = Economy.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GameStateSnapshot.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._GameStateSnapshot.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FloatArray =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Values: RepeatedBuilder<float32> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Values.AddRange ((ValueCodec.Packed ValueCodec.Float).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.FloatArray = {
            Values = x.Values.Build
            }

/// <summary>Array types for map data and bulk queries</summary>
type private _FloatArray = FloatArray
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FloatArray = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("values")>] Values: float32 list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<FloatArray>> =
        lazy
        // Field Definitions
        let Values = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Float) (1, "values")
        // Proto Definition Implementation
        { // ProtoDef<FloatArray>
            Name = "FloatArray"
            Empty = {
                Values = Values.GetDefault()
                }
            Size = fun (m: FloatArray) ->
                0
                + Values.CalcFieldSize m.Values
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FloatArray) ->
                Values.WriteField w m.Values
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.FloatArray.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValues = Values.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FloatArray) =
                    writeValues w m.Values
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FloatArray =
                    match kvPair.Key with
                    | "values" -> { value with Values = Values.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FloatArray.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._FloatArray.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module IntArray =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Values: RepeatedBuilder<int> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Values.AddRange ((ValueCodec.Packed ValueCodec.Int32).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.IntArray = {
            Values = x.Values.Build
            }

type private _IntArray = IntArray
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type IntArray = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("values")>] Values: int list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<IntArray>> =
        lazy
        // Field Definitions
        let Values = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Int32) (1, "values")
        // Proto Definition Implementation
        { // ProtoDef<IntArray>
            Name = "IntArray"
            Empty = {
                Values = Values.GetDefault()
                }
            Size = fun (m: IntArray) ->
                0
                + Values.CalcFieldSize m.Values
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: IntArray) ->
                Values.WriteField w m.Values
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.IntArray.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValues = Values.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: IntArray) =
                    writeValues w m.Values
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : IntArray =
                    match kvPair.Key with
                    | "values" -> { value with Values = Values.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _IntArray.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._IntArray.Proto.Value.Empty

