namespace rec Highbar
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<ShutdownReason>>)>]
type ShutdownReason =
| [<FsGrpc.Protobuf.ProtobufName("SHUTDOWN_REASON_UNKNOWN")>] Unknown = 0
| [<FsGrpc.Protobuf.ProtobufName("SHUTDOWN_REASON_GAME_OVER")>] GameOver = 1
| [<FsGrpc.Protobuf.ProtobufName("SHUTDOWN_REASON_DISCONNECT")>] Disconnect = 2
| [<FsGrpc.Protobuf.ProtobufName("SHUTDOWN_REASON_ERROR")>] Error = 3

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ProxyMessage =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<MessageCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type MessageCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("handshake")>] Handshake of Highbar.Handshake
    | [<System.Text.Json.Serialization.JsonPropertyName("frame")>] Frame of Highbar.Frame
    | [<System.Text.Json.Serialization.JsonPropertyName("callbackResponse")>] CallbackResponse of Highbar.CallbackResponse
    | [<System.Text.Json.Serialization.JsonPropertyName("saveRequest")>] SaveRequest of Highbar.SaveRequest
    | [<System.Text.Json.Serialization.JsonPropertyName("loadRequest")>] LoadRequest of Highbar.LoadRequest
    | [<System.Text.Json.Serialization.JsonPropertyName("shutdown")>] Shutdown of Highbar.Shutdown
    with
        static member OneofCodec : Lazy<OneofCodec<MessageCase>> = 
            lazy
            let Handshake = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Handshake> (1, "handshake")
            let Frame = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Frame> (2, "frame")
            let CallbackResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.CallbackResponse> (3, "callbackResponse")
            let SaveRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.SaveRequest> (4, "saveRequest")
            let LoadRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.LoadRequest> (5, "loadRequest")
            let Shutdown = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Shutdown> (6, "shutdown")
            let Message = FieldCodec.Oneof "message" (FSharp.Collections.Map [
                ("handshake", fun node -> MessageCase.Handshake (Handshake.ReadJsonField node))
                ("frame", fun node -> MessageCase.Frame (Frame.ReadJsonField node))
                ("callbackResponse", fun node -> MessageCase.CallbackResponse (CallbackResponse.ReadJsonField node))
                ("saveRequest", fun node -> MessageCase.SaveRequest (SaveRequest.ReadJsonField node))
                ("loadRequest", fun node -> MessageCase.LoadRequest (LoadRequest.ReadJsonField node))
                ("shutdown", fun node -> MessageCase.Shutdown (Shutdown.ReadJsonField node))
                ])
            Message

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Message: OptionBuilder<Highbar.ProxyMessage.MessageCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Message.Set (MessageCase.Handshake (ValueCodec.Message<Highbar.Handshake>.ReadValue reader))
            | 2 -> x.Message.Set (MessageCase.Frame (ValueCodec.Message<Highbar.Frame>.ReadValue reader))
            | 3 -> x.Message.Set (MessageCase.CallbackResponse (ValueCodec.Message<Highbar.CallbackResponse>.ReadValue reader))
            | 4 -> x.Message.Set (MessageCase.SaveRequest (ValueCodec.Message<Highbar.SaveRequest>.ReadValue reader))
            | 5 -> x.Message.Set (MessageCase.LoadRequest (ValueCodec.Message<Highbar.LoadRequest>.ReadValue reader))
            | 6 -> x.Message.Set (MessageCase.Shutdown (ValueCodec.Message<Highbar.Shutdown>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.ProxyMessage = {
            Message = x.Message.Build |> (Option.defaultValue MessageCase.None)
            }

/// <summary>Top-level envelope for proxy → AI messages</summary>
type private _ProxyMessage = ProxyMessage
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ProxyMessage = {
    // Field Declarations
    Message: Highbar.ProxyMessage.MessageCase
    }
    with
    static member Proto : Lazy<ProtoDef<ProxyMessage>> =
        lazy
        // Field Definitions
        let Handshake = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Handshake> (1, "handshake")
        let Frame = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Frame> (2, "frame")
        let CallbackResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.CallbackResponse> (3, "callbackResponse")
        let SaveRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.SaveRequest> (4, "saveRequest")
        let LoadRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.LoadRequest> (5, "loadRequest")
        let Shutdown = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.Shutdown> (6, "shutdown")
        let Message = FieldCodec.Oneof "message" (FSharp.Collections.Map [
            ("handshake", fun node -> Highbar.ProxyMessage.MessageCase.Handshake (Handshake.ReadJsonField node))
            ("frame", fun node -> Highbar.ProxyMessage.MessageCase.Frame (Frame.ReadJsonField node))
            ("callbackResponse", fun node -> Highbar.ProxyMessage.MessageCase.CallbackResponse (CallbackResponse.ReadJsonField node))
            ("saveRequest", fun node -> Highbar.ProxyMessage.MessageCase.SaveRequest (SaveRequest.ReadJsonField node))
            ("loadRequest", fun node -> Highbar.ProxyMessage.MessageCase.LoadRequest (LoadRequest.ReadJsonField node))
            ("shutdown", fun node -> Highbar.ProxyMessage.MessageCase.Shutdown (Shutdown.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<ProxyMessage>
            Name = "ProxyMessage"
            Empty = {
                Message = Highbar.ProxyMessage.MessageCase.None
                }
            Size = fun (m: ProxyMessage) ->
                0
                + match m.Message with
                    | Highbar.ProxyMessage.MessageCase.None -> 0
                    | Highbar.ProxyMessage.MessageCase.Handshake v -> Handshake.CalcFieldSize v
                    | Highbar.ProxyMessage.MessageCase.Frame v -> Frame.CalcFieldSize v
                    | Highbar.ProxyMessage.MessageCase.CallbackResponse v -> CallbackResponse.CalcFieldSize v
                    | Highbar.ProxyMessage.MessageCase.SaveRequest v -> SaveRequest.CalcFieldSize v
                    | Highbar.ProxyMessage.MessageCase.LoadRequest v -> LoadRequest.CalcFieldSize v
                    | Highbar.ProxyMessage.MessageCase.Shutdown v -> Shutdown.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ProxyMessage) ->
                (match m.Message with
                | Highbar.ProxyMessage.MessageCase.None -> ()
                | Highbar.ProxyMessage.MessageCase.Handshake v -> Handshake.WriteField w v
                | Highbar.ProxyMessage.MessageCase.Frame v -> Frame.WriteField w v
                | Highbar.ProxyMessage.MessageCase.CallbackResponse v -> CallbackResponse.WriteField w v
                | Highbar.ProxyMessage.MessageCase.SaveRequest v -> SaveRequest.WriteField w v
                | Highbar.ProxyMessage.MessageCase.LoadRequest v -> LoadRequest.WriteField w v
                | Highbar.ProxyMessage.MessageCase.Shutdown v -> Shutdown.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.ProxyMessage.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeMessageNone = Message.WriteJsonNoneCase o
                let writeHandshake = Handshake.WriteJsonField o
                let writeFrame = Frame.WriteJsonField o
                let writeCallbackResponse = CallbackResponse.WriteJsonField o
                let writeSaveRequest = SaveRequest.WriteJsonField o
                let writeLoadRequest = LoadRequest.WriteJsonField o
                let writeShutdown = Shutdown.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ProxyMessage) =
                    (match m.Message with
                    | Highbar.ProxyMessage.MessageCase.None -> writeMessageNone w
                    | Highbar.ProxyMessage.MessageCase.Handshake v -> writeHandshake w v
                    | Highbar.ProxyMessage.MessageCase.Frame v -> writeFrame w v
                    | Highbar.ProxyMessage.MessageCase.CallbackResponse v -> writeCallbackResponse w v
                    | Highbar.ProxyMessage.MessageCase.SaveRequest v -> writeSaveRequest w v
                    | Highbar.ProxyMessage.MessageCase.LoadRequest v -> writeLoadRequest w v
                    | Highbar.ProxyMessage.MessageCase.Shutdown v -> writeShutdown w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ProxyMessage =
                    match kvPair.Key with
                    | "handshake" -> { value with Message = Highbar.ProxyMessage.MessageCase.Handshake (Handshake.ReadJsonField kvPair.Value) }
                    | "frame" -> { value with Message = Highbar.ProxyMessage.MessageCase.Frame (Frame.ReadJsonField kvPair.Value) }
                    | "callbackResponse" -> { value with Message = Highbar.ProxyMessage.MessageCase.CallbackResponse (CallbackResponse.ReadJsonField kvPair.Value) }
                    | "saveRequest" -> { value with Message = Highbar.ProxyMessage.MessageCase.SaveRequest (SaveRequest.ReadJsonField kvPair.Value) }
                    | "loadRequest" -> { value with Message = Highbar.ProxyMessage.MessageCase.LoadRequest (LoadRequest.ReadJsonField kvPair.Value) }
                    | "shutdown" -> { value with Message = Highbar.ProxyMessage.MessageCase.Shutdown (Shutdown.ReadJsonField kvPair.Value) }
                    | "message" -> { value with Message = Message.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ProxyMessage.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._ProxyMessage.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AIMessage =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<MessageCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type MessageCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("handshakeResponse")>] HandshakeResponse of Highbar.HandshakeResponse
    | [<System.Text.Json.Serialization.JsonPropertyName("frameResponse")>] FrameResponse of Highbar.FrameResponse
    | [<System.Text.Json.Serialization.JsonPropertyName("callbackRequest")>] CallbackRequest of Highbar.CallbackRequest
    | [<System.Text.Json.Serialization.JsonPropertyName("saveResponse")>] SaveResponse of Highbar.SaveResponse
    with
        static member OneofCodec : Lazy<OneofCodec<MessageCase>> = 
            lazy
            let HandshakeResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.HandshakeResponse> (1, "handshakeResponse")
            let FrameResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.FrameResponse> (2, "frameResponse")
            let CallbackRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.CallbackRequest> (3, "callbackRequest")
            let SaveResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.SaveResponse> (4, "saveResponse")
            let Message = FieldCodec.Oneof "message" (FSharp.Collections.Map [
                ("handshakeResponse", fun node -> MessageCase.HandshakeResponse (HandshakeResponse.ReadJsonField node))
                ("frameResponse", fun node -> MessageCase.FrameResponse (FrameResponse.ReadJsonField node))
                ("callbackRequest", fun node -> MessageCase.CallbackRequest (CallbackRequest.ReadJsonField node))
                ("saveResponse", fun node -> MessageCase.SaveResponse (SaveResponse.ReadJsonField node))
                ])
            Message

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Message: OptionBuilder<Highbar.AIMessage.MessageCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Message.Set (MessageCase.HandshakeResponse (ValueCodec.Message<Highbar.HandshakeResponse>.ReadValue reader))
            | 2 -> x.Message.Set (MessageCase.FrameResponse (ValueCodec.Message<Highbar.FrameResponse>.ReadValue reader))
            | 3 -> x.Message.Set (MessageCase.CallbackRequest (ValueCodec.Message<Highbar.CallbackRequest>.ReadValue reader))
            | 4 -> x.Message.Set (MessageCase.SaveResponse (ValueCodec.Message<Highbar.SaveResponse>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.AIMessage = {
            Message = x.Message.Build |> (Option.defaultValue MessageCase.None)
            }

/// <summary>Top-level envelope for AI → proxy messages</summary>
type private _AIMessage = AIMessage
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AIMessage = {
    // Field Declarations
    Message: Highbar.AIMessage.MessageCase
    }
    with
    static member Proto : Lazy<ProtoDef<AIMessage>> =
        lazy
        // Field Definitions
        let HandshakeResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.HandshakeResponse> (1, "handshakeResponse")
        let FrameResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.FrameResponse> (2, "frameResponse")
        let CallbackRequest = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.CallbackRequest> (3, "callbackRequest")
        let SaveResponse = FieldCodec.OneofCase "message" ValueCodec.Message<Highbar.SaveResponse> (4, "saveResponse")
        let Message = FieldCodec.Oneof "message" (FSharp.Collections.Map [
            ("handshakeResponse", fun node -> Highbar.AIMessage.MessageCase.HandshakeResponse (HandshakeResponse.ReadJsonField node))
            ("frameResponse", fun node -> Highbar.AIMessage.MessageCase.FrameResponse (FrameResponse.ReadJsonField node))
            ("callbackRequest", fun node -> Highbar.AIMessage.MessageCase.CallbackRequest (CallbackRequest.ReadJsonField node))
            ("saveResponse", fun node -> Highbar.AIMessage.MessageCase.SaveResponse (SaveResponse.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<AIMessage>
            Name = "AIMessage"
            Empty = {
                Message = Highbar.AIMessage.MessageCase.None
                }
            Size = fun (m: AIMessage) ->
                0
                + match m.Message with
                    | Highbar.AIMessage.MessageCase.None -> 0
                    | Highbar.AIMessage.MessageCase.HandshakeResponse v -> HandshakeResponse.CalcFieldSize v
                    | Highbar.AIMessage.MessageCase.FrameResponse v -> FrameResponse.CalcFieldSize v
                    | Highbar.AIMessage.MessageCase.CallbackRequest v -> CallbackRequest.CalcFieldSize v
                    | Highbar.AIMessage.MessageCase.SaveResponse v -> SaveResponse.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AIMessage) ->
                (match m.Message with
                | Highbar.AIMessage.MessageCase.None -> ()
                | Highbar.AIMessage.MessageCase.HandshakeResponse v -> HandshakeResponse.WriteField w v
                | Highbar.AIMessage.MessageCase.FrameResponse v -> FrameResponse.WriteField w v
                | Highbar.AIMessage.MessageCase.CallbackRequest v -> CallbackRequest.WriteField w v
                | Highbar.AIMessage.MessageCase.SaveResponse v -> SaveResponse.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.AIMessage.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeMessageNone = Message.WriteJsonNoneCase o
                let writeHandshakeResponse = HandshakeResponse.WriteJsonField o
                let writeFrameResponse = FrameResponse.WriteJsonField o
                let writeCallbackRequest = CallbackRequest.WriteJsonField o
                let writeSaveResponse = SaveResponse.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AIMessage) =
                    (match m.Message with
                    | Highbar.AIMessage.MessageCase.None -> writeMessageNone w
                    | Highbar.AIMessage.MessageCase.HandshakeResponse v -> writeHandshakeResponse w v
                    | Highbar.AIMessage.MessageCase.FrameResponse v -> writeFrameResponse w v
                    | Highbar.AIMessage.MessageCase.CallbackRequest v -> writeCallbackRequest w v
                    | Highbar.AIMessage.MessageCase.SaveResponse v -> writeSaveResponse w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AIMessage =
                    match kvPair.Key with
                    | "handshakeResponse" -> { value with Message = Highbar.AIMessage.MessageCase.HandshakeResponse (HandshakeResponse.ReadJsonField kvPair.Value) }
                    | "frameResponse" -> { value with Message = Highbar.AIMessage.MessageCase.FrameResponse (FrameResponse.ReadJsonField kvPair.Value) }
                    | "callbackRequest" -> { value with Message = Highbar.AIMessage.MessageCase.CallbackRequest (CallbackRequest.ReadJsonField kvPair.Value) }
                    | "saveResponse" -> { value with Message = Highbar.AIMessage.MessageCase.SaveResponse (SaveResponse.ReadJsonField kvPair.Value) }
                    | "message" -> { value with Message = Message.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AIMessage.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._AIMessage.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Handshake =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ProtocolVersion: uint32 // (1)
            val mutable EngineVersion: string // (2)
            val mutable GameId: string // (3)
            val mutable MapName: string // (4)
            val mutable ModName: string // (5)
            val mutable TeamId: int // (6)
            val mutable AllyTeamId: int // (7)
            val mutable PlayerCount: int // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ProtocolVersion <- ValueCodec.UInt32.ReadValue reader
            | 2 -> x.EngineVersion <- ValueCodec.String.ReadValue reader
            | 3 -> x.GameId <- ValueCodec.String.ReadValue reader
            | 4 -> x.MapName <- ValueCodec.String.ReadValue reader
            | 5 -> x.ModName <- ValueCodec.String.ReadValue reader
            | 6 -> x.TeamId <- ValueCodec.Int32.ReadValue reader
            | 7 -> x.AllyTeamId <- ValueCodec.Int32.ReadValue reader
            | 8 -> x.PlayerCount <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.Handshake = {
            ProtocolVersion = x.ProtocolVersion
            EngineVersion = x.EngineVersion |> orEmptyString
            GameId = x.GameId |> orEmptyString
            MapName = x.MapName |> orEmptyString
            ModName = x.ModName |> orEmptyString
            TeamId = x.TeamId
            AllyTeamId = x.AllyTeamId
            PlayerCount = x.PlayerCount
            }

/// <summary>Initial handshake sent by proxy on connection</summary>
type private _Handshake = Handshake
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type Handshake = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("protocolVersion")>] ProtocolVersion: uint32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("engineVersion")>] EngineVersion: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("gameId")>] GameId: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("mapName")>] MapName: string // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("modName")>] ModName: string // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("teamId")>] TeamId: int // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("allyTeamId")>] AllyTeamId: int // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("playerCount")>] PlayerCount: int // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<Handshake>> =
        lazy
        // Field Definitions
        let ProtocolVersion = FieldCodec.Primitive ValueCodec.UInt32 (1, "protocolVersion")
        let EngineVersion = FieldCodec.Primitive ValueCodec.String (2, "engineVersion")
        let GameId = FieldCodec.Primitive ValueCodec.String (3, "gameId")
        let MapName = FieldCodec.Primitive ValueCodec.String (4, "mapName")
        let ModName = FieldCodec.Primitive ValueCodec.String (5, "modName")
        let TeamId = FieldCodec.Primitive ValueCodec.Int32 (6, "teamId")
        let AllyTeamId = FieldCodec.Primitive ValueCodec.Int32 (7, "allyTeamId")
        let PlayerCount = FieldCodec.Primitive ValueCodec.Int32 (8, "playerCount")
        // Proto Definition Implementation
        { // ProtoDef<Handshake>
            Name = "Handshake"
            Empty = {
                ProtocolVersion = ProtocolVersion.GetDefault()
                EngineVersion = EngineVersion.GetDefault()
                GameId = GameId.GetDefault()
                MapName = MapName.GetDefault()
                ModName = ModName.GetDefault()
                TeamId = TeamId.GetDefault()
                AllyTeamId = AllyTeamId.GetDefault()
                PlayerCount = PlayerCount.GetDefault()
                }
            Size = fun (m: Handshake) ->
                0
                + ProtocolVersion.CalcFieldSize m.ProtocolVersion
                + EngineVersion.CalcFieldSize m.EngineVersion
                + GameId.CalcFieldSize m.GameId
                + MapName.CalcFieldSize m.MapName
                + ModName.CalcFieldSize m.ModName
                + TeamId.CalcFieldSize m.TeamId
                + AllyTeamId.CalcFieldSize m.AllyTeamId
                + PlayerCount.CalcFieldSize m.PlayerCount
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: Handshake) ->
                ProtocolVersion.WriteField w m.ProtocolVersion
                EngineVersion.WriteField w m.EngineVersion
                GameId.WriteField w m.GameId
                MapName.WriteField w m.MapName
                ModName.WriteField w m.ModName
                TeamId.WriteField w m.TeamId
                AllyTeamId.WriteField w m.AllyTeamId
                PlayerCount.WriteField w m.PlayerCount
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.Handshake.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeProtocolVersion = ProtocolVersion.WriteJsonField o
                let writeEngineVersion = EngineVersion.WriteJsonField o
                let writeGameId = GameId.WriteJsonField o
                let writeMapName = MapName.WriteJsonField o
                let writeModName = ModName.WriteJsonField o
                let writeTeamId = TeamId.WriteJsonField o
                let writeAllyTeamId = AllyTeamId.WriteJsonField o
                let writePlayerCount = PlayerCount.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: Handshake) =
                    writeProtocolVersion w m.ProtocolVersion
                    writeEngineVersion w m.EngineVersion
                    writeGameId w m.GameId
                    writeMapName w m.MapName
                    writeModName w m.ModName
                    writeTeamId w m.TeamId
                    writeAllyTeamId w m.AllyTeamId
                    writePlayerCount w m.PlayerCount
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : Handshake =
                    match kvPair.Key with
                    | "protocolVersion" -> { value with ProtocolVersion = ProtocolVersion.ReadJsonField kvPair.Value }
                    | "engineVersion" -> { value with EngineVersion = EngineVersion.ReadJsonField kvPair.Value }
                    | "gameId" -> { value with GameId = GameId.ReadJsonField kvPair.Value }
                    | "mapName" -> { value with MapName = MapName.ReadJsonField kvPair.Value }
                    | "modName" -> { value with ModName = ModName.ReadJsonField kvPair.Value }
                    | "teamId" -> { value with TeamId = TeamId.ReadJsonField kvPair.Value }
                    | "allyTeamId" -> { value with AllyTeamId = AllyTeamId.ReadJsonField kvPair.Value }
                    | "playerCount" -> { value with PlayerCount = PlayerCount.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _Handshake.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._Handshake.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module HandshakeResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Accepted: bool // (1)
            val mutable ProtocolVersion: uint32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Accepted <- ValueCodec.Bool.ReadValue reader
            | 2 -> x.ProtocolVersion <- ValueCodec.UInt32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.HandshakeResponse = {
            Accepted = x.Accepted
            ProtocolVersion = x.ProtocolVersion
            }

/// <summary>AI response to handshake</summary>
type private _HandshakeResponse = HandshakeResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type HandshakeResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("accepted")>] Accepted: bool // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("protocolVersion")>] ProtocolVersion: uint32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<HandshakeResponse>> =
        lazy
        // Field Definitions
        let Accepted = FieldCodec.Primitive ValueCodec.Bool (1, "accepted")
        let ProtocolVersion = FieldCodec.Primitive ValueCodec.UInt32 (2, "protocolVersion")
        // Proto Definition Implementation
        { // ProtoDef<HandshakeResponse>
            Name = "HandshakeResponse"
            Empty = {
                Accepted = Accepted.GetDefault()
                ProtocolVersion = ProtocolVersion.GetDefault()
                }
            Size = fun (m: HandshakeResponse) ->
                0
                + Accepted.CalcFieldSize m.Accepted
                + ProtocolVersion.CalcFieldSize m.ProtocolVersion
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: HandshakeResponse) ->
                Accepted.WriteField w m.Accepted
                ProtocolVersion.WriteField w m.ProtocolVersion
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.HandshakeResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeAccepted = Accepted.WriteJsonField o
                let writeProtocolVersion = ProtocolVersion.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: HandshakeResponse) =
                    writeAccepted w m.Accepted
                    writeProtocolVersion w m.ProtocolVersion
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : HandshakeResponse =
                    match kvPair.Key with
                    | "accepted" -> { value with Accepted = Accepted.ReadJsonField kvPair.Value }
                    | "protocolVersion" -> { value with ProtocolVersion = ProtocolVersion.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _HandshakeResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._HandshakeResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Frame =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FrameNumber: uint32 // (1)
            val mutable Events: RepeatedBuilder<Highbar.EngineEvent> // (2)
            val mutable TeamId: int // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FrameNumber <- ValueCodec.UInt32.ReadValue reader
            | 2 -> x.Events.Add (ValueCodec.Message<Highbar.EngineEvent>.ReadValue reader)
            | 3 -> x.TeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.Frame = {
            FrameNumber = x.FrameNumber
            Events = x.Events.Build
            TeamId = x.TeamId
            }

/// <summary>Batched events for a single game frame</summary>
type private _Frame = Frame
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type Frame = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("frameNumber")>] FrameNumber: uint32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("events")>] Events: Highbar.EngineEvent list // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("teamId")>] TeamId: int // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<Frame>> =
        lazy
        // Field Definitions
        let FrameNumber = FieldCodec.Primitive ValueCodec.UInt32 (1, "frameNumber")
        let Events = FieldCodec.Repeated ValueCodec.Message<Highbar.EngineEvent> (2, "events")
        let TeamId = FieldCodec.Primitive ValueCodec.Int32 (3, "teamId")
        // Proto Definition Implementation
        { // ProtoDef<Frame>
            Name = "Frame"
            Empty = {
                FrameNumber = FrameNumber.GetDefault()
                Events = Events.GetDefault()
                TeamId = TeamId.GetDefault()
                }
            Size = fun (m: Frame) ->
                0
                + FrameNumber.CalcFieldSize m.FrameNumber
                + Events.CalcFieldSize m.Events
                + TeamId.CalcFieldSize m.TeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: Frame) ->
                FrameNumber.WriteField w m.FrameNumber
                Events.WriteField w m.Events
                TeamId.WriteField w m.TeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.Frame.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrameNumber = FrameNumber.WriteJsonField o
                let writeEvents = Events.WriteJsonField o
                let writeTeamId = TeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: Frame) =
                    writeFrameNumber w m.FrameNumber
                    writeEvents w m.Events
                    writeTeamId w m.TeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : Frame =
                    match kvPair.Key with
                    | "frameNumber" -> { value with FrameNumber = FrameNumber.ReadJsonField kvPair.Value }
                    | "events" -> { value with Events = Events.ReadJsonField kvPair.Value }
                    | "teamId" -> { value with TeamId = TeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _Frame.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._Frame.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FrameResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Commands: RepeatedBuilder<Highbar.AICommand> // (1)
            val mutable TeamId: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Commands.Add (ValueCodec.Message<Highbar.AICommand>.ReadValue reader)
            | 2 -> x.TeamId <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.FrameResponse = {
            Commands = x.Commands.Build
            TeamId = x.TeamId
            }

/// <summary>AI response to a frame: batch of commands to execute</summary>
type private _FrameResponse = FrameResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FrameResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("commands")>] Commands: Highbar.AICommand list // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("teamId")>] TeamId: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<FrameResponse>> =
        lazy
        // Field Definitions
        let Commands = FieldCodec.Repeated ValueCodec.Message<Highbar.AICommand> (1, "commands")
        let TeamId = FieldCodec.Primitive ValueCodec.Int32 (2, "teamId")
        // Proto Definition Implementation
        { // ProtoDef<FrameResponse>
            Name = "FrameResponse"
            Empty = {
                Commands = Commands.GetDefault()
                TeamId = TeamId.GetDefault()
                }
            Size = fun (m: FrameResponse) ->
                0
                + Commands.CalcFieldSize m.Commands
                + TeamId.CalcFieldSize m.TeamId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FrameResponse) ->
                Commands.WriteField w m.Commands
                TeamId.WriteField w m.TeamId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.FrameResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCommands = Commands.WriteJsonField o
                let writeTeamId = TeamId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FrameResponse) =
                    writeCommands w m.Commands
                    writeTeamId w m.TeamId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FrameResponse =
                    match kvPair.Key with
                    | "commands" -> { value with Commands = Commands.ReadJsonField kvPair.Value }
                    | "teamId" -> { value with TeamId = TeamId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FrameResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._FrameResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SaveRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = SaveRequest.empty

[<StructuralEquality;StructuralComparison>]
type SaveRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<SaveRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<SaveRequest>
            Name = "SaveRequest"
            Empty = SaveRequest.empty
            Size = fun (m: SaveRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SaveRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                SaveRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> SaveRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SaveResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StateData: FsGrpc.Bytes // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StateData <- ValueCodec.Bytes.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.SaveResponse = {
            StateData = x.StateData
            }

/// <summary>AI provides serialized state</summary>
type private _SaveResponse = SaveResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SaveResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("stateData")>] StateData: FsGrpc.Bytes // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SaveResponse>> =
        lazy
        // Field Definitions
        let StateData = FieldCodec.Primitive ValueCodec.Bytes (1, "stateData")
        // Proto Definition Implementation
        { // ProtoDef<SaveResponse>
            Name = "SaveResponse"
            Empty = {
                StateData = StateData.GetDefault()
                }
            Size = fun (m: SaveResponse) ->
                0
                + StateData.CalcFieldSize m.StateData
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SaveResponse) ->
                StateData.WriteField w m.StateData
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.SaveResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStateData = StateData.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SaveResponse) =
                    writeStateData w m.StateData
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SaveResponse =
                    match kvPair.Key with
                    | "stateData" -> { value with StateData = StateData.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SaveResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._SaveResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StateData: FsGrpc.Bytes // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StateData <- ValueCodec.Bytes.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.LoadRequest = {
            StateData = x.StateData
            }

/// <summary>Engine provides previously saved state for AI to restore</summary>
type private _LoadRequest = LoadRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("stateData")>] StateData: FsGrpc.Bytes // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadRequest>> =
        lazy
        // Field Definitions
        let StateData = FieldCodec.Primitive ValueCodec.Bytes (1, "stateData")
        // Proto Definition Implementation
        { // ProtoDef<LoadRequest>
            Name = "LoadRequest"
            Empty = {
                StateData = StateData.GetDefault()
                }
            Size = fun (m: LoadRequest) ->
                0
                + StateData.CalcFieldSize m.StateData
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadRequest) ->
                StateData.WriteField w m.StateData
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.LoadRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStateData = StateData.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadRequest) =
                    writeStateData w m.StateData
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadRequest =
                    match kvPair.Key with
                    | "stateData" -> { value with StateData = StateData.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._LoadRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Shutdown =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Reason: Highbar.ShutdownReason // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Reason <- ValueCodec.Enum<Highbar.ShutdownReason>.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Highbar.Shutdown = {
            Reason = x.Reason
            }

/// <summary>Notification that game is ending</summary>
type private _Shutdown = Shutdown
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type Shutdown = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("reason")>] Reason: Highbar.ShutdownReason // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<Shutdown>> =
        lazy
        // Field Definitions
        let Reason = FieldCodec.Primitive ValueCodec.Enum<Highbar.ShutdownReason> (1, "reason")
        // Proto Definition Implementation
        { // ProtoDef<Shutdown>
            Name = "Shutdown"
            Empty = {
                Reason = Reason.GetDefault()
                }
            Size = fun (m: Shutdown) ->
                0
                + Reason.CalcFieldSize m.Reason
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: Shutdown) ->
                Reason.WriteField w m.Reason
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Highbar.Shutdown.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeReason = Reason.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: Shutdown) =
                    writeReason w m.Reason
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : Shutdown =
                    match kvPair.Key with
                    | "reason" -> { value with Reason = Reason.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _Shutdown.empty (node.AsObject ())
        }
    static member empty
        with get() = Highbar._Shutdown.Proto.Value.Empty

