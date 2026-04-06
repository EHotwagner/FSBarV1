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
            let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
                ("intValue", fun node -> ValueCase.IntValue (IntValue.ReadJsonField node))
                ("floatValue", fun node -> ValueCase.FloatValue (FloatValue.ReadJsonField node))
                ("stringValue", fun node -> ValueCase.StringValue (StringValue.ReadJsonField node))
                ("vectorValue", fun node -> ValueCase.VectorValue (VectorValue.ReadJsonField node))
                ("bytesValue", fun node -> ValueCase.BytesValue (BytesValue.ReadJsonField node))
                ("floatArrayValue", fun node -> ValueCase.FloatArrayValue (FloatArrayValue.ReadJsonField node))
                ("intArrayValue", fun node -> ValueCase.IntArrayValue (IntArrayValue.ReadJsonField node))
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
        let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
            ("intValue", fun node -> Highbar.CallbackResult.ValueCase.IntValue (IntValue.ReadJsonField node))
            ("floatValue", fun node -> Highbar.CallbackResult.ValueCase.FloatValue (FloatValue.ReadJsonField node))
            ("stringValue", fun node -> Highbar.CallbackResult.ValueCase.StringValue (StringValue.ReadJsonField node))
            ("vectorValue", fun node -> Highbar.CallbackResult.ValueCase.VectorValue (VectorValue.ReadJsonField node))
            ("bytesValue", fun node -> Highbar.CallbackResult.ValueCase.BytesValue (BytesValue.ReadJsonField node))
            ("floatArrayValue", fun node -> Highbar.CallbackResult.ValueCase.FloatArrayValue (FloatArrayValue.ReadJsonField node))
            ("intArrayValue", fun node -> Highbar.CallbackResult.ValueCase.IntArrayValue (IntArrayValue.ReadJsonField node))
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
                    | "value" -> { value with Value = Value.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CallbackResult.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._CallbackResult.Proto.Value.Empty

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

