namespace rec Fsbar.Hub.Scripting.V1
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<HubTab>>)>]
type HubTab =
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_SETUP")>] Setup = 1
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_VIEWER")>] Viewer = 2
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_UNITS")>] Units = 3
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_STYLE")>] Style = 4
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_CFG")>] Cfg = 5
| [<FsGrpc.Protobuf.ProtobufName("HUB_TAB_GRPC")>] Grpc = 6

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<ImageFormat>>)>]
type ImageFormat =
| [<FsGrpc.Protobuf.ProtobufName("IMAGE_FORMAT_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("IMAGE_FORMAT_PNG")>] Png = 1
| [<FsGrpc.Protobuf.ProtobufName("IMAGE_FORMAT_JPEG")>] Jpeg = 2

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<SubmitOutcome>>)>]
type SubmitOutcome =
| [<FsGrpc.Protobuf.ProtobufName("SUBMIT_OUTCOME_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("SUBMIT_OUTCOME_SENT")>] Sent = 1
| [<FsGrpc.Protobuf.ProtobufName("SUBMIT_OUTCOME_COALESCED")>] Coalesced = 2
| [<FsGrpc.Protobuf.ProtobufName("SUBMIT_OUTCOME_REJECTED")>] Rejected = 3

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<LobbyMode>>)>]
type LobbyMode =
| [<FsGrpc.Protobuf.ProtobufName("LOBBY_MODE_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("LOBBY_MODE_SKIRMISH")>] Skirmish = 1
| [<FsGrpc.Protobuf.ProtobufName("LOBBY_MODE_FFA")>] Ffa = 2
| [<FsGrpc.Protobuf.ProtobufName("LOBBY_MODE_TEAM")>] Team = 3

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<SeatKind>>)>]
type SeatKind =
| [<FsGrpc.Protobuf.ProtobufName("SEAT_KIND_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("SEAT_KIND_AI")>] Ai = 1
| [<FsGrpc.Protobuf.ProtobufName("SEAT_KIND_HUMAN")>] Human = 2

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<OverlayKey>>)>]
type OverlayKey =
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_UNITS")>] Units = 1
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_EVENTS")>] Events = 2
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_GRID")>] Grid = 3
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_METAL_SPOTS")>] MetalSpots = 4
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_ECONOMY_HUD")>] EconomyHud = 5
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_WEAPON_RANGES")>] WeaponRanges = 6
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_SIGHT_RANGES")>] SightRanges = 7
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_COMMAND_QUEUE")>] CommandQueue = 8
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_KEY_FULL_NAMES")>] FullNames = 9

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<OverlayTargetState>>)>]
type OverlayTargetState =
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_TARGET_STATE_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_TARGET_STATE_TOGGLE")>] Toggle = 1
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_TARGET_STATE_ON")>] On = 2
| [<FsGrpc.Protobuf.ProtobufName("OVERLAY_TARGET_STATE_OFF")>] Off = 3

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<CoordinateSpace>>)>]
type CoordinateSpace =
| [<FsGrpc.Protobuf.ProtobufName("COORDINATE_SPACE_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("COORDINATE_SPACE_WORLD")>] World = 1
| [<FsGrpc.Protobuf.ProtobufName("COORDINATE_SPACE_SCREEN")>] Screen = 2

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<TextAlign>>)>]
type TextAlign =
| [<FsGrpc.Protobuf.ProtobufName("TEXT_ALIGN_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("TEXT_ALIGN_LEFT")>] Left = 1
| [<FsGrpc.Protobuf.ProtobufName("TEXT_ALIGN_CENTER")>] Center = 2
| [<FsGrpc.Protobuf.ProtobufName("TEXT_ALIGN_RIGHT")>] Right = 3

[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<LogSeverity>>)>]
type LogSeverity =
| [<FsGrpc.Protobuf.ProtobufName("LOG_SEVERITY_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("LOG_SEVERITY_DEBUG")>] Debug = 1
| [<FsGrpc.Protobuf.ProtobufName("LOG_SEVERITY_INFO")>] Info = 2
| [<FsGrpc.Protobuf.ProtobufName("LOG_SEVERITY_WARNING")>] Warning = 3
| [<FsGrpc.Protobuf.ProtobufName("LOG_SEVERITY_ERROR")>] Error = 4

/// <summary>
/// Exhaustive enumeration of the Hub subsystems that emit on the log
/// stream today (FR-004). Scope deliberately excludes engine-launcher
/// infolog.txt capture, map-analysis, synthetic-data, and viz-rendering
/// internals per feature 042 Clarifications Q1.
/// </summary>
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<LogCategory>>)>]
type LogCategory =
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_UNSPECIFIED")>] Unspecified = 0
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_SESSION_MANAGER")>] SessionManager = 1
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_ADMIN_CHANNEL")>] AdminChannel = 2
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_SCRIPTING_HUB")>] ScriptingHub = 3
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_PROXY_INSTALL")>] ProxyInstall = 4
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_HEADLESS_RENDERER")>] HeadlessRenderer = 5
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_HUB_STATE_STORE")>] HubStateStore = 6
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_PRESET_PERSISTENCE")>] PresetPersistence = 7
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_LOBBY")>] Lobby = 8
| [<FsGrpc.Protobuf.ProtobufName("LOG_CATEGORY_SETTINGS")>] Settings = 9

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StreamGameFramesRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ClientLabel: string // (1)
            val mutable CloseOnSessionEnd: bool // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ClientLabel <- ValueCodec.String.ReadValue reader
            | 2 -> x.CloseOnSessionEnd <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StreamGameFramesRequest = {
            ClientLabel = x.ClientLabel |> orEmptyString
            CloseOnSessionEnd = x.CloseOnSessionEnd
            }

type private _StreamGameFramesRequest = StreamGameFramesRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StreamGameFramesRequest = {
    // Field Declarations
    /// <summary>Optional client identifier for diagnostics. If empty, hub assigns a uuid.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("clientLabel")>] ClientLabel: string // (1)
    /// <summary>
    /// If true, the hub will close the stream when the current session ends
    /// (instead of keeping it open and waiting for the next session).
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("closeOnSessionEnd")>] CloseOnSessionEnd: bool // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<StreamGameFramesRequest>> =
        lazy
        // Field Definitions
        let ClientLabel = FieldCodec.Primitive ValueCodec.String (1, "clientLabel")
        let CloseOnSessionEnd = FieldCodec.Primitive ValueCodec.Bool (2, "closeOnSessionEnd")
        // Proto Definition Implementation
        { // ProtoDef<StreamGameFramesRequest>
            Name = "StreamGameFramesRequest"
            Empty = {
                ClientLabel = ClientLabel.GetDefault()
                CloseOnSessionEnd = CloseOnSessionEnd.GetDefault()
                }
            Size = fun (m: StreamGameFramesRequest) ->
                0
                + ClientLabel.CalcFieldSize m.ClientLabel
                + CloseOnSessionEnd.CalcFieldSize m.CloseOnSessionEnd
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StreamGameFramesRequest) ->
                ClientLabel.WriteField w m.ClientLabel
                CloseOnSessionEnd.WriteField w m.CloseOnSessionEnd
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StreamGameFramesRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeClientLabel = ClientLabel.WriteJsonField o
                let writeCloseOnSessionEnd = CloseOnSessionEnd.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StreamGameFramesRequest) =
                    writeClientLabel w m.ClientLabel
                    writeCloseOnSessionEnd w m.CloseOnSessionEnd
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StreamGameFramesRequest =
                    match kvPair.Key with
                    | "clientLabel" -> { value with ClientLabel = ClientLabel.ReadJsonField kvPair.Value }
                    | "closeOnSessionEnd" -> { value with CloseOnSessionEnd = CloseOnSessionEnd.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StreamGameFramesRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StreamGameFramesRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GameFrameMessage =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Frame: OptionBuilder<Highbar.Frame> // (1)
            val mutable ClientSequence: uint64 // (2)
            val mutable HubEnqueuedAtUnixMs: int64 // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Frame.Set (ValueCodec.Message<Highbar.Frame>.ReadValue reader)
            | 2 -> x.ClientSequence <- ValueCodec.UInt64.ReadValue reader
            | 3 -> x.HubEnqueuedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GameFrameMessage = {
            Frame = x.Frame.Build
            ClientSequence = x.ClientSequence
            HubEnqueuedAtUnixMs = x.HubEnqueuedAtUnixMs
            }

type private _GameFrameMessage = GameFrameMessage
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GameFrameMessage = {
    // Field Declarations
    /// <summary>
    /// The raw proxy frame envelope as seen by the embedded viewer.
    /// Phase-9 note: may be widened with decoded state projections.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("frame")>] Frame: Highbar.Frame option // (1)
    /// <summary>
    /// Hub-assigned monotonic sequence number for this client. Lets clients
    /// detect dropped frames locally (gaps).
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("clientSequence")>] ClientSequence: uint64 // (2)
    /// <summary>
    /// Hub-side timestamp (unix millis) when the frame was enqueued for this
    /// client. Useful for measuring fan-out latency.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("hubEnqueuedAtUnixMs")>] HubEnqueuedAtUnixMs: int64 // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<GameFrameMessage>> =
        lazy
        // Field Definitions
        let Frame = FieldCodec.Optional ValueCodec.Message<Highbar.Frame> (1, "frame")
        let ClientSequence = FieldCodec.Primitive ValueCodec.UInt64 (2, "clientSequence")
        let HubEnqueuedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (3, "hubEnqueuedAtUnixMs")
        // Proto Definition Implementation
        { // ProtoDef<GameFrameMessage>
            Name = "GameFrameMessage"
            Empty = {
                Frame = Frame.GetDefault()
                ClientSequence = ClientSequence.GetDefault()
                HubEnqueuedAtUnixMs = HubEnqueuedAtUnixMs.GetDefault()
                }
            Size = fun (m: GameFrameMessage) ->
                0
                + Frame.CalcFieldSize m.Frame
                + ClientSequence.CalcFieldSize m.ClientSequence
                + HubEnqueuedAtUnixMs.CalcFieldSize m.HubEnqueuedAtUnixMs
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GameFrameMessage) ->
                Frame.WriteField w m.Frame
                ClientSequence.WriteField w m.ClientSequence
                HubEnqueuedAtUnixMs.WriteField w m.HubEnqueuedAtUnixMs
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GameFrameMessage.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrame = Frame.WriteJsonField o
                let writeClientSequence = ClientSequence.WriteJsonField o
                let writeHubEnqueuedAtUnixMs = HubEnqueuedAtUnixMs.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GameFrameMessage) =
                    writeFrame w m.Frame
                    writeClientSequence w m.ClientSequence
                    writeHubEnqueuedAtUnixMs w m.HubEnqueuedAtUnixMs
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GameFrameMessage =
                    match kvPair.Key with
                    | "frame" -> { value with Frame = Frame.ReadJsonField kvPair.Value }
                    | "clientSequence" -> { value with ClientSequence = ClientSequence.ReadJsonField kvPair.Value }
                    | "hubEnqueuedAtUnixMs" -> { value with HubEnqueuedAtUnixMs = HubEnqueuedAtUnixMs.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GameFrameMessage.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GameFrameMessage.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendCommandRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Command: OptionBuilder<Highbar.AICommand> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Command.Set (ValueCodec.Message<Highbar.AICommand>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SendCommandRequest = {
            Command = x.Command.Build
            }

type private _SendCommandRequest = SendCommandRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendCommandRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("command")>] Command: Highbar.AICommand option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SendCommandRequest>> =
        lazy
        // Field Definitions
        let Command = FieldCodec.Optional ValueCodec.Message<Highbar.AICommand> (1, "command")
        // Proto Definition Implementation
        { // ProtoDef<SendCommandRequest>
            Name = "SendCommandRequest"
            Empty = {
                Command = Command.GetDefault()
                }
            Size = fun (m: SendCommandRequest) ->
                0
                + Command.CalcFieldSize m.Command
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendCommandRequest) ->
                Command.WriteField w m.Command
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SendCommandRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCommand = Command.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendCommandRequest) =
                    writeCommand w m.Command
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendCommandRequest =
                    match kvPair.Key with
                    | "command" -> { value with Command = Command.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendCommandRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SendCommandRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendCommandResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ForwardedAtFrame: int // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ForwardedAtFrame <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SendCommandResponse = {
            ForwardedAtFrame = x.ForwardedAtFrame
            }

type private _SendCommandResponse = SendCommandResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendCommandResponse = {
    // Field Declarations
    /// <summary>
    /// The frame number on which the hub forwarded the command, or 0 if the
    /// command was queued before the next frame request. Diagnostic only.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("forwardedAtFrame")>] ForwardedAtFrame: int // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SendCommandResponse>> =
        lazy
        // Field Definitions
        let ForwardedAtFrame = FieldCodec.Primitive ValueCodec.Int32 (1, "forwardedAtFrame")
        // Proto Definition Implementation
        { // ProtoDef<SendCommandResponse>
            Name = "SendCommandResponse"
            Empty = {
                ForwardedAtFrame = ForwardedAtFrame.GetDefault()
                }
            Size = fun (m: SendCommandResponse) ->
                0
                + ForwardedAtFrame.CalcFieldSize m.ForwardedAtFrame
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendCommandResponse) ->
                ForwardedAtFrame.WriteField w m.ForwardedAtFrame
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SendCommandResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeForwardedAtFrame = ForwardedAtFrame.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendCommandResponse) =
                    writeForwardedAtFrame w m.ForwardedAtFrame
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendCommandResponse =
                    match kvPair.Key with
                    | "forwardedAtFrame" -> { value with ForwardedAtFrame = ForwardedAtFrame.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendCommandResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SendCommandResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetSessionStatusRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = GetSessionStatusRequest.empty

[<StructuralEquality;StructuralComparison>]
type GetSessionStatusRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<GetSessionStatusRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<GetSessionStatusRequest>
            Name = "GetSessionStatusRequest"
            Empty = GetSessionStatusRequest.empty
            Size = fun (m: GetSessionStatusRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetSessionStatusRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                GetSessionStatusRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> GetSessionStatusRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetSessionStatusResponse =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<State>>)>]
    type State =
    | [<FsGrpc.Protobuf.ProtobufName("STATE_UNSPECIFIED")>] Unspecified = 0
    | [<FsGrpc.Protobuf.ProtobufName("IDLE")>] Idle = 1
    | [<FsGrpc.Protobuf.ProtobufName("STARTING")>] Starting = 2
    | [<FsGrpc.Protobuf.ProtobufName("RUNNING")>] Running = 3
    | [<FsGrpc.Protobuf.ProtobufName("ENDING")>] Ending = 4
    | [<FsGrpc.Protobuf.ProtobufName("FAILED")>] Failed = 5

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable State: Fsbar.Hub.Scripting.V1.GetSessionStatusResponse.State // (1)
            val mutable BarDataDir: string // (2)
            val mutable ActiveEngineVersion: string // (3)
            val mutable BundledProxyVersion: string // (4)
            val mutable GrpcPort: int // (5)
            val mutable ActiveSession: OptionBuilder<Fsbar.Hub.Scripting.V1.ActiveSession> // (6)
            val mutable Clients: RepeatedBuilder<Fsbar.Hub.Scripting.V1.ConnectedClient> // (7)
            val mutable Failure: OptionBuilder<Fsbar.Hub.Scripting.V1.FailureInfo> // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.State <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse.State>.ReadValue reader
            | 2 -> x.BarDataDir <- ValueCodec.String.ReadValue reader
            | 3 -> x.ActiveEngineVersion <- ValueCodec.String.ReadValue reader
            | 4 -> x.BundledProxyVersion <- ValueCodec.String.ReadValue reader
            | 5 -> x.GrpcPort <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.ActiveSession.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ActiveSession>.ReadValue reader)
            | 7 -> x.Clients.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ConnectedClient>.ReadValue reader)
            | 8 -> x.Failure.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.FailureInfo>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetSessionStatusResponse = {
            State = x.State
            BarDataDir = x.BarDataDir |> orEmptyString
            ActiveEngineVersion = x.ActiveEngineVersion |> orEmptyString
            BundledProxyVersion = x.BundledProxyVersion |> orEmptyString
            GrpcPort = x.GrpcPort
            ActiveSession = x.ActiveSession.Build
            Clients = x.Clients.Build
            Failure = x.Failure.Build
            }

type private _GetSessionStatusResponse = GetSessionStatusResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetSessionStatusResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("state")>] State: Fsbar.Hub.Scripting.V1.GetSessionStatusResponse.State // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("barDataDir")>] BarDataDir: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("activeEngineVersion")>] ActiveEngineVersion: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("bundledProxyVersion")>] BundledProxyVersion: string // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("grpcPort")>] GrpcPort: int // (5)
    /// <summary>Populated when state = RUNNING.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("activeSession")>] ActiveSession: Fsbar.Hub.Scripting.V1.ActiveSession option // (6)
    /// <summary>Connected scripting clients (including this caller).</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("clients")>] Clients: Fsbar.Hub.Scripting.V1.ConnectedClient list // (7)
    /// <summary>The most recent failure, populated when state = FAILED.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("failure")>] Failure: Fsbar.Hub.Scripting.V1.FailureInfo option // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<GetSessionStatusResponse>> =
        lazy
        // Field Definitions
        let State = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse.State> (1, "state")
        let BarDataDir = FieldCodec.Primitive ValueCodec.String (2, "barDataDir")
        let ActiveEngineVersion = FieldCodec.Primitive ValueCodec.String (3, "activeEngineVersion")
        let BundledProxyVersion = FieldCodec.Primitive ValueCodec.String (4, "bundledProxyVersion")
        let GrpcPort = FieldCodec.Primitive ValueCodec.Int32 (5, "grpcPort")
        let ActiveSession = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.ActiveSession> (6, "activeSession")
        let Clients = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.ConnectedClient> (7, "clients")
        let Failure = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.FailureInfo> (8, "failure")
        // Proto Definition Implementation
        { // ProtoDef<GetSessionStatusResponse>
            Name = "GetSessionStatusResponse"
            Empty = {
                State = State.GetDefault()
                BarDataDir = BarDataDir.GetDefault()
                ActiveEngineVersion = ActiveEngineVersion.GetDefault()
                BundledProxyVersion = BundledProxyVersion.GetDefault()
                GrpcPort = GrpcPort.GetDefault()
                ActiveSession = ActiveSession.GetDefault()
                Clients = Clients.GetDefault()
                Failure = Failure.GetDefault()
                }
            Size = fun (m: GetSessionStatusResponse) ->
                0
                + State.CalcFieldSize m.State
                + BarDataDir.CalcFieldSize m.BarDataDir
                + ActiveEngineVersion.CalcFieldSize m.ActiveEngineVersion
                + BundledProxyVersion.CalcFieldSize m.BundledProxyVersion
                + GrpcPort.CalcFieldSize m.GrpcPort
                + ActiveSession.CalcFieldSize m.ActiveSession
                + Clients.CalcFieldSize m.Clients
                + Failure.CalcFieldSize m.Failure
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetSessionStatusResponse) ->
                State.WriteField w m.State
                BarDataDir.WriteField w m.BarDataDir
                ActiveEngineVersion.WriteField w m.ActiveEngineVersion
                BundledProxyVersion.WriteField w m.BundledProxyVersion
                GrpcPort.WriteField w m.GrpcPort
                ActiveSession.WriteField w m.ActiveSession
                Clients.WriteField w m.Clients
                Failure.WriteField w m.Failure
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetSessionStatusResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeState = State.WriteJsonField o
                let writeBarDataDir = BarDataDir.WriteJsonField o
                let writeActiveEngineVersion = ActiveEngineVersion.WriteJsonField o
                let writeBundledProxyVersion = BundledProxyVersion.WriteJsonField o
                let writeGrpcPort = GrpcPort.WriteJsonField o
                let writeActiveSession = ActiveSession.WriteJsonField o
                let writeClients = Clients.WriteJsonField o
                let writeFailure = Failure.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetSessionStatusResponse) =
                    writeState w m.State
                    writeBarDataDir w m.BarDataDir
                    writeActiveEngineVersion w m.ActiveEngineVersion
                    writeBundledProxyVersion w m.BundledProxyVersion
                    writeGrpcPort w m.GrpcPort
                    writeActiveSession w m.ActiveSession
                    writeClients w m.Clients
                    writeFailure w m.Failure
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetSessionStatusResponse =
                    match kvPair.Key with
                    | "state" -> { value with State = State.ReadJsonField kvPair.Value }
                    | "barDataDir" -> { value with BarDataDir = BarDataDir.ReadJsonField kvPair.Value }
                    | "activeEngineVersion" -> { value with ActiveEngineVersion = ActiveEngineVersion.ReadJsonField kvPair.Value }
                    | "bundledProxyVersion" -> { value with BundledProxyVersion = BundledProxyVersion.ReadJsonField kvPair.Value }
                    | "grpcPort" -> { value with GrpcPort = GrpcPort.ReadJsonField kvPair.Value }
                    | "activeSession" -> { value with ActiveSession = ActiveSession.ReadJsonField kvPair.Value }
                    | "clients" -> { value with Clients = Clients.ReadJsonField kvPair.Value }
                    | "failure" -> { value with Failure = Failure.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetSessionStatusResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetSessionStatusResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ActiveSession =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable SessionId: string // (1)
            val mutable MapName: string // (2)
            val mutable Mode: string // (3)
            val mutable EngineSpeed: float32 // (4)
            val mutable Paused: bool // (5)
            val mutable StartedAtUnixMs: int64 // (6)
            val mutable AdminChannelStatus: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> // (7)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.SessionId <- ValueCodec.String.ReadValue reader
            | 2 -> x.MapName <- ValueCodec.String.ReadValue reader
            | 3 -> x.Mode <- ValueCodec.String.ReadValue reader
            | 4 -> x.EngineSpeed <- ValueCodec.Float.ReadValue reader
            | 5 -> x.Paused <- ValueCodec.Bool.ReadValue reader
            | 6 -> x.StartedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 7 -> x.AdminChannelStatus.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ActiveSession = {
            SessionId = x.SessionId |> orEmptyString
            MapName = x.MapName |> orEmptyString
            Mode = x.Mode |> orEmptyString
            EngineSpeed = x.EngineSpeed
            Paused = x.Paused
            StartedAtUnixMs = x.StartedAtUnixMs
            AdminChannelStatus = x.AdminChannelStatus.Build
            }

type private _ActiveSession = ActiveSession
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ActiveSession = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("sessionId")>] SessionId: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("mapName")>] MapName: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("mode")>] Mode: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("engineSpeed")>] EngineSpeed: float32 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("paused")>] Paused: bool // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("startedAtUnixMs")>] StartedAtUnixMs: int64 // (6)
    /// <summary>Feature 039 — hub-level admin-channel status for the running session.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("adminChannelStatus")>] AdminChannelStatus: Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo option // (7)
    }
    with
    static member Proto : Lazy<ProtoDef<ActiveSession>> =
        lazy
        // Field Definitions
        let SessionId = FieldCodec.Primitive ValueCodec.String (1, "sessionId")
        let MapName = FieldCodec.Primitive ValueCodec.String (2, "mapName")
        let Mode = FieldCodec.Primitive ValueCodec.String (3, "mode")
        let EngineSpeed = FieldCodec.Primitive ValueCodec.Float (4, "engineSpeed")
        let Paused = FieldCodec.Primitive ValueCodec.Bool (5, "paused")
        let StartedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (6, "startedAtUnixMs")
        let AdminChannelStatus = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> (7, "adminChannelStatus")
        // Proto Definition Implementation
        { // ProtoDef<ActiveSession>
            Name = "ActiveSession"
            Empty = {
                SessionId = SessionId.GetDefault()
                MapName = MapName.GetDefault()
                Mode = Mode.GetDefault()
                EngineSpeed = EngineSpeed.GetDefault()
                Paused = Paused.GetDefault()
                StartedAtUnixMs = StartedAtUnixMs.GetDefault()
                AdminChannelStatus = AdminChannelStatus.GetDefault()
                }
            Size = fun (m: ActiveSession) ->
                0
                + SessionId.CalcFieldSize m.SessionId
                + MapName.CalcFieldSize m.MapName
                + Mode.CalcFieldSize m.Mode
                + EngineSpeed.CalcFieldSize m.EngineSpeed
                + Paused.CalcFieldSize m.Paused
                + StartedAtUnixMs.CalcFieldSize m.StartedAtUnixMs
                + AdminChannelStatus.CalcFieldSize m.AdminChannelStatus
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ActiveSession) ->
                SessionId.WriteField w m.SessionId
                MapName.WriteField w m.MapName
                Mode.WriteField w m.Mode
                EngineSpeed.WriteField w m.EngineSpeed
                Paused.WriteField w m.Paused
                StartedAtUnixMs.WriteField w m.StartedAtUnixMs
                AdminChannelStatus.WriteField w m.AdminChannelStatus
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ActiveSession.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSessionId = SessionId.WriteJsonField o
                let writeMapName = MapName.WriteJsonField o
                let writeMode = Mode.WriteJsonField o
                let writeEngineSpeed = EngineSpeed.WriteJsonField o
                let writePaused = Paused.WriteJsonField o
                let writeStartedAtUnixMs = StartedAtUnixMs.WriteJsonField o
                let writeAdminChannelStatus = AdminChannelStatus.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ActiveSession) =
                    writeSessionId w m.SessionId
                    writeMapName w m.MapName
                    writeMode w m.Mode
                    writeEngineSpeed w m.EngineSpeed
                    writePaused w m.Paused
                    writeStartedAtUnixMs w m.StartedAtUnixMs
                    writeAdminChannelStatus w m.AdminChannelStatus
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ActiveSession =
                    match kvPair.Key with
                    | "sessionId" -> { value with SessionId = SessionId.ReadJsonField kvPair.Value }
                    | "mapName" -> { value with MapName = MapName.ReadJsonField kvPair.Value }
                    | "mode" -> { value with Mode = Mode.ReadJsonField kvPair.Value }
                    | "engineSpeed" -> { value with EngineSpeed = EngineSpeed.ReadJsonField kvPair.Value }
                    | "paused" -> { value with Paused = Paused.ReadJsonField kvPair.Value }
                    | "startedAtUnixMs" -> { value with StartedAtUnixMs = StartedAtUnixMs.ReadJsonField kvPair.Value }
                    | "adminChannelStatus" -> { value with AdminChannelStatus = AdminChannelStatus.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ActiveSession.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ActiveSession.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ConnectedClient =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ClientId: string // (1)
            val mutable ClientLabel: string // (2)
            val mutable RemoteEndpoint: string // (3)
            val mutable AttachedAtUnixMs: int64 // (4)
            val mutable CumulativeDroppedFrames: int // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ClientId <- ValueCodec.String.ReadValue reader
            | 2 -> x.ClientLabel <- ValueCodec.String.ReadValue reader
            | 3 -> x.RemoteEndpoint <- ValueCodec.String.ReadValue reader
            | 4 -> x.AttachedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 5 -> x.CumulativeDroppedFrames <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ConnectedClient = {
            ClientId = x.ClientId |> orEmptyString
            ClientLabel = x.ClientLabel |> orEmptyString
            RemoteEndpoint = x.RemoteEndpoint |> orEmptyString
            AttachedAtUnixMs = x.AttachedAtUnixMs
            CumulativeDroppedFrames = x.CumulativeDroppedFrames
            }

type private _ConnectedClient = ConnectedClient
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ConnectedClient = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("clientId")>] ClientId: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("clientLabel")>] ClientLabel: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("remoteEndpoint")>] RemoteEndpoint: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("attachedAtUnixMs")>] AttachedAtUnixMs: int64 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("cumulativeDroppedFrames")>] CumulativeDroppedFrames: int // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<ConnectedClient>> =
        lazy
        // Field Definitions
        let ClientId = FieldCodec.Primitive ValueCodec.String (1, "clientId")
        let ClientLabel = FieldCodec.Primitive ValueCodec.String (2, "clientLabel")
        let RemoteEndpoint = FieldCodec.Primitive ValueCodec.String (3, "remoteEndpoint")
        let AttachedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (4, "attachedAtUnixMs")
        let CumulativeDroppedFrames = FieldCodec.Primitive ValueCodec.Int32 (5, "cumulativeDroppedFrames")
        // Proto Definition Implementation
        { // ProtoDef<ConnectedClient>
            Name = "ConnectedClient"
            Empty = {
                ClientId = ClientId.GetDefault()
                ClientLabel = ClientLabel.GetDefault()
                RemoteEndpoint = RemoteEndpoint.GetDefault()
                AttachedAtUnixMs = AttachedAtUnixMs.GetDefault()
                CumulativeDroppedFrames = CumulativeDroppedFrames.GetDefault()
                }
            Size = fun (m: ConnectedClient) ->
                0
                + ClientId.CalcFieldSize m.ClientId
                + ClientLabel.CalcFieldSize m.ClientLabel
                + RemoteEndpoint.CalcFieldSize m.RemoteEndpoint
                + AttachedAtUnixMs.CalcFieldSize m.AttachedAtUnixMs
                + CumulativeDroppedFrames.CalcFieldSize m.CumulativeDroppedFrames
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ConnectedClient) ->
                ClientId.WriteField w m.ClientId
                ClientLabel.WriteField w m.ClientLabel
                RemoteEndpoint.WriteField w m.RemoteEndpoint
                AttachedAtUnixMs.WriteField w m.AttachedAtUnixMs
                CumulativeDroppedFrames.WriteField w m.CumulativeDroppedFrames
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ConnectedClient.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeClientId = ClientId.WriteJsonField o
                let writeClientLabel = ClientLabel.WriteJsonField o
                let writeRemoteEndpoint = RemoteEndpoint.WriteJsonField o
                let writeAttachedAtUnixMs = AttachedAtUnixMs.WriteJsonField o
                let writeCumulativeDroppedFrames = CumulativeDroppedFrames.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ConnectedClient) =
                    writeClientId w m.ClientId
                    writeClientLabel w m.ClientLabel
                    writeRemoteEndpoint w m.RemoteEndpoint
                    writeAttachedAtUnixMs w m.AttachedAtUnixMs
                    writeCumulativeDroppedFrames w m.CumulativeDroppedFrames
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ConnectedClient =
                    match kvPair.Key with
                    | "clientId" -> { value with ClientId = ClientId.ReadJsonField kvPair.Value }
                    | "clientLabel" -> { value with ClientLabel = ClientLabel.ReadJsonField kvPair.Value }
                    | "remoteEndpoint" -> { value with RemoteEndpoint = RemoteEndpoint.ReadJsonField kvPair.Value }
                    | "attachedAtUnixMs" -> { value with AttachedAtUnixMs = AttachedAtUnixMs.ReadJsonField kvPair.Value }
                    | "cumulativeDroppedFrames" -> { value with CumulativeDroppedFrames = CumulativeDroppedFrames.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ConnectedClient.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ConnectedClient.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FailureInfo =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Reason: string // (1)
            val mutable InfologExcerpt: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Reason <- ValueCodec.String.ReadValue reader
            | 2 -> x.InfologExcerpt <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.FailureInfo = {
            Reason = x.Reason |> orEmptyString
            InfologExcerpt = x.InfologExcerpt |> orEmptyString
            }

type private _FailureInfo = FailureInfo
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type FailureInfo = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("reason")>] Reason: string // (1)
    /// <summary>First N kilobytes of the engine's infolog excerpt at failure time (FR-031).</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("infologExcerpt")>] InfologExcerpt: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<FailureInfo>> =
        lazy
        // Field Definitions
        let Reason = FieldCodec.Primitive ValueCodec.String (1, "reason")
        let InfologExcerpt = FieldCodec.Primitive ValueCodec.String (2, "infologExcerpt")
        // Proto Definition Implementation
        { // ProtoDef<FailureInfo>
            Name = "FailureInfo"
            Empty = {
                Reason = Reason.GetDefault()
                InfologExcerpt = InfologExcerpt.GetDefault()
                }
            Size = fun (m: FailureInfo) ->
                0
                + Reason.CalcFieldSize m.Reason
                + InfologExcerpt.CalcFieldSize m.InfologExcerpt
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: FailureInfo) ->
                Reason.WriteField w m.Reason
                InfologExcerpt.WriteField w m.InfologExcerpt
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.FailureInfo.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeReason = Reason.WriteJsonField o
                let writeInfologExcerpt = InfologExcerpt.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: FailureInfo) =
                    writeReason w m.Reason
                    writeInfologExcerpt w m.InfologExcerpt
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : FailureInfo =
                    match kvPair.Key with
                    | "reason" -> { value with Reason = Reason.ReadJsonField kvPair.Value }
                    | "infologExcerpt" -> { value with InfologExcerpt = InfologExcerpt.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _FailureInfo.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._FailureInfo.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetUnitDefRequest =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<SelectorCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type SelectorCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("defId")>] DefId of int
    | [<System.Text.Json.Serialization.JsonPropertyName("internalName")>] InternalName of string
    with
        static member OneofCodec : Lazy<OneofCodec<SelectorCase>> = 
            lazy
            let DefId = FieldCodec.OneofCase "selector" ValueCodec.Int32 (1, "defId")
            let InternalName = FieldCodec.OneofCase "selector" ValueCodec.String (2, "internalName")
            let Selector = FieldCodec.Oneof "selector" (FSharp.Collections.Map [
                ("defId", fun node -> SelectorCase.DefId (DefId.ReadJsonField node))
                ("internalName", fun node -> SelectorCase.InternalName (InternalName.ReadJsonField node))
                ])
            Selector

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Selector: OptionBuilder<Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Selector.Set (SelectorCase.DefId (ValueCodec.Int32.ReadValue reader))
            | 2 -> x.Selector.Set (SelectorCase.InternalName (ValueCodec.String.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetUnitDefRequest = {
            Selector = x.Selector.Build |> (Option.defaultValue SelectorCase.None)
            }

type private _GetUnitDefRequest = GetUnitDefRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetUnitDefRequest = {
    // Field Declarations
    Selector: Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase
    }
    with
    static member Proto : Lazy<ProtoDef<GetUnitDefRequest>> =
        lazy
        // Field Definitions
        let DefId = FieldCodec.OneofCase "selector" ValueCodec.Int32 (1, "defId")
        let InternalName = FieldCodec.OneofCase "selector" ValueCodec.String (2, "internalName")
        let Selector = FieldCodec.Oneof "selector" (FSharp.Collections.Map [
            ("defId", fun node -> Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId (DefId.ReadJsonField node))
            ("internalName", fun node -> Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName (InternalName.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<GetUnitDefRequest>
            Name = "GetUnitDefRequest"
            Empty = {
                Selector = Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.None
                }
            Size = fun (m: GetUnitDefRequest) ->
                0
                + match m.Selector with
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId v -> DefId.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName v -> InternalName.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetUnitDefRequest) ->
                (match m.Selector with
                | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.None -> ()
                | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId v -> DefId.WriteField w v
                | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName v -> InternalName.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetUnitDefRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSelectorNone = Selector.WriteJsonNoneCase o
                let writeDefId = DefId.WriteJsonField o
                let writeInternalName = InternalName.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetUnitDefRequest) =
                    (match m.Selector with
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.None -> writeSelectorNone w
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId v -> writeDefId w v
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName v -> writeInternalName w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetUnitDefRequest =
                    match kvPair.Key with
                    | "defId" -> { value with Selector = Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId (DefId.ReadJsonField kvPair.Value) }
                    | "internalName" -> { value with Selector = Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName (InternalName.ReadJsonField kvPair.Value) }
                    | "selector" -> { value with Selector = Selector.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetUnitDefRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetUnitDefRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UnitDefInfo =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable DefId: int // (1)
            val mutable InternalName: string // (2)
            val mutable DisplayName: string // (3)
            val mutable MetalCost: int // (4)
            val mutable EnergyCost: int // (5)
            val mutable BuildTime: int // (6)
            val mutable MaxHealth: int // (7)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.DefId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.InternalName <- ValueCodec.String.ReadValue reader
            | 3 -> x.DisplayName <- ValueCodec.String.ReadValue reader
            | 4 -> x.MetalCost <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.EnergyCost <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.BuildTime <- ValueCodec.Int32.ReadValue reader
            | 7 -> x.MaxHealth <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.UnitDefInfo = {
            DefId = x.DefId
            InternalName = x.InternalName |> orEmptyString
            DisplayName = x.DisplayName |> orEmptyString
            MetalCost = x.MetalCost
            EnergyCost = x.EnergyCost
            BuildTime = x.BuildTime
            MaxHealth = x.MaxHealth
            }

/// <summary>
/// UnitDef projection for the scripting wire format. Phase-9 note: may be
/// superseded by a canonical highbar.UnitDef message; kept in hub namespace
/// until that reconciliation happens.
/// </summary>
type private _UnitDefInfo = UnitDefInfo
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type UnitDefInfo = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("defId")>] DefId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("internalName")>] InternalName: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("displayName")>] DisplayName: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("metalCost")>] MetalCost: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("energyCost")>] EnergyCost: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("buildTime")>] BuildTime: int // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("maxHealth")>] MaxHealth: int // (7)
    }
    with
    static member Proto : Lazy<ProtoDef<UnitDefInfo>> =
        lazy
        // Field Definitions
        let DefId = FieldCodec.Primitive ValueCodec.Int32 (1, "defId")
        let InternalName = FieldCodec.Primitive ValueCodec.String (2, "internalName")
        let DisplayName = FieldCodec.Primitive ValueCodec.String (3, "displayName")
        let MetalCost = FieldCodec.Primitive ValueCodec.Int32 (4, "metalCost")
        let EnergyCost = FieldCodec.Primitive ValueCodec.Int32 (5, "energyCost")
        let BuildTime = FieldCodec.Primitive ValueCodec.Int32 (6, "buildTime")
        let MaxHealth = FieldCodec.Primitive ValueCodec.Int32 (7, "maxHealth")
        // Proto Definition Implementation
        { // ProtoDef<UnitDefInfo>
            Name = "UnitDefInfo"
            Empty = {
                DefId = DefId.GetDefault()
                InternalName = InternalName.GetDefault()
                DisplayName = DisplayName.GetDefault()
                MetalCost = MetalCost.GetDefault()
                EnergyCost = EnergyCost.GetDefault()
                BuildTime = BuildTime.GetDefault()
                MaxHealth = MaxHealth.GetDefault()
                }
            Size = fun (m: UnitDefInfo) ->
                0
                + DefId.CalcFieldSize m.DefId
                + InternalName.CalcFieldSize m.InternalName
                + DisplayName.CalcFieldSize m.DisplayName
                + MetalCost.CalcFieldSize m.MetalCost
                + EnergyCost.CalcFieldSize m.EnergyCost
                + BuildTime.CalcFieldSize m.BuildTime
                + MaxHealth.CalcFieldSize m.MaxHealth
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: UnitDefInfo) ->
                DefId.WriteField w m.DefId
                InternalName.WriteField w m.InternalName
                DisplayName.WriteField w m.DisplayName
                MetalCost.WriteField w m.MetalCost
                EnergyCost.WriteField w m.EnergyCost
                BuildTime.WriteField w m.BuildTime
                MaxHealth.WriteField w m.MaxHealth
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.UnitDefInfo.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeDefId = DefId.WriteJsonField o
                let writeInternalName = InternalName.WriteJsonField o
                let writeDisplayName = DisplayName.WriteJsonField o
                let writeMetalCost = MetalCost.WriteJsonField o
                let writeEnergyCost = EnergyCost.WriteJsonField o
                let writeBuildTime = BuildTime.WriteJsonField o
                let writeMaxHealth = MaxHealth.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: UnitDefInfo) =
                    writeDefId w m.DefId
                    writeInternalName w m.InternalName
                    writeDisplayName w m.DisplayName
                    writeMetalCost w m.MetalCost
                    writeEnergyCost w m.EnergyCost
                    writeBuildTime w m.BuildTime
                    writeMaxHealth w m.MaxHealth
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : UnitDefInfo =
                    match kvPair.Key with
                    | "defId" -> { value with DefId = DefId.ReadJsonField kvPair.Value }
                    | "internalName" -> { value with InternalName = InternalName.ReadJsonField kvPair.Value }
                    | "displayName" -> { value with DisplayName = DisplayName.ReadJsonField kvPair.Value }
                    | "metalCost" -> { value with MetalCost = MetalCost.ReadJsonField kvPair.Value }
                    | "energyCost" -> { value with EnergyCost = EnergyCost.ReadJsonField kvPair.Value }
                    | "buildTime" -> { value with BuildTime = BuildTime.ReadJsonField kvPair.Value }
                    | "maxHealth" -> { value with MaxHealth = MaxHealth.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _UnitDefInfo.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._UnitDefInfo.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetUnitDefResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable UnitDef: OptionBuilder<Fsbar.Hub.Scripting.V1.UnitDefInfo> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.UnitDef.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.UnitDefInfo>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetUnitDefResponse = {
            UnitDef = x.UnitDef.Build
            }

type private _GetUnitDefResponse = GetUnitDefResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetUnitDefResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("unitDef")>] UnitDef: Fsbar.Hub.Scripting.V1.UnitDefInfo option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<GetUnitDefResponse>> =
        lazy
        // Field Definitions
        let UnitDef = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.UnitDefInfo> (1, "unitDef")
        // Proto Definition Implementation
        { // ProtoDef<GetUnitDefResponse>
            Name = "GetUnitDefResponse"
            Empty = {
                UnitDef = UnitDef.GetDefault()
                }
            Size = fun (m: GetUnitDefResponse) ->
                0
                + UnitDef.CalcFieldSize m.UnitDef
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetUnitDefResponse) ->
                UnitDef.WriteField w m.UnitDef
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetUnitDefResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeUnitDef = UnitDef.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetUnitDefResponse) =
                    writeUnitDef w m.UnitDef
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetUnitDefResponse =
                    match kvPair.Key with
                    | "unitDef" -> { value with UnitDef = UnitDef.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetUnitDefResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetUnitDefResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AdminChannelStatusInfo =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<State>>)>]
    type State =
    | [<FsGrpc.Protobuf.ProtobufName("STATE_UNSPECIFIED")>] Unspecified = 0
    | [<FsGrpc.Protobuf.ProtobufName("ATTACHED")>] Attached = 1
    | [<FsGrpc.Protobuf.ProtobufName("UNAVAILABLE")>] Unavailable = 2
    | [<FsGrpc.Protobuf.ProtobufName("LOST")>] Lost = 3

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable State: Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo.State // (1)
            val mutable Reason: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.State <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo.State>.ReadValue reader
            | 2 -> x.Reason <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo = {
            State = x.State
            Reason = x.Reason |> orEmptyString
            }

/// <summary>
/// Hub-visible admin-channel status. Mirrors
/// FSBar.Hub.AdminChannelHost.AdminChannelStatus.
/// </summary>
type private _AdminChannelStatusInfo = AdminChannelStatusInfo
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AdminChannelStatusInfo = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("state")>] State: Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo.State // (1)
    /// <summary>Populated when state = UNAVAILABLE or LOST. Empty otherwise.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("reason")>] Reason: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<AdminChannelStatusInfo>> =
        lazy
        // Field Definitions
        let State = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo.State> (1, "state")
        let Reason = FieldCodec.Primitive ValueCodec.String (2, "reason")
        // Proto Definition Implementation
        { // ProtoDef<AdminChannelStatusInfo>
            Name = "AdminChannelStatusInfo"
            Empty = {
                State = State.GetDefault()
                Reason = Reason.GetDefault()
                }
            Size = fun (m: AdminChannelStatusInfo) ->
                0
                + State.CalcFieldSize m.State
                + Reason.CalcFieldSize m.Reason
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AdminChannelStatusInfo) ->
                State.WriteField w m.State
                Reason.WriteField w m.Reason
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeState = State.WriteJsonField o
                let writeReason = Reason.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AdminChannelStatusInfo) =
                    writeState w m.State
                    writeReason w m.Reason
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AdminChannelStatusInfo =
                    match kvPair.Key with
                    | "state" -> { value with State = State.ReadJsonField kvPair.Value }
                    | "reason" -> { value with Reason = Reason.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AdminChannelStatusInfo.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._AdminChannelStatusInfo.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AdminSubmitResult =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<Outcome>>)>]
    type Outcome =
    | [<FsGrpc.Protobuf.ProtobufName("OUTCOME_UNSPECIFIED")>] Unspecified = 0
    | [<FsGrpc.Protobuf.ProtobufName("SENT")>] Sent = 1
    | [<FsGrpc.Protobuf.ProtobufName("COALESCED")>] Coalesced = 2
    | [<FsGrpc.Protobuf.ProtobufName("REJECTED")>] Rejected = 3

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Outcome: Fsbar.Hub.Scripting.V1.AdminSubmitResult.Outcome // (1)
            val mutable DroppedCount: int // (2)
            val mutable Reason: string // (3)
            val mutable AdminChannelStatus: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Outcome <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.AdminSubmitResult.Outcome>.ReadValue reader
            | 2 -> x.DroppedCount <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Reason <- ValueCodec.String.ReadValue reader
            | 4 -> x.AdminChannelStatus.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.AdminSubmitResult = {
            Outcome = x.Outcome
            DroppedCount = x.DroppedCount
            Reason = x.Reason |> orEmptyString
            AdminChannelStatus = x.AdminChannelStatus.Build
            }

/// <summary>
/// Common shape for every admin-command response. Matches the three
/// FSBar.Hub.AdminChannelHost.SubmitOutcome cases.
/// </summary>
type private _AdminSubmitResult = AdminSubmitResult
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type AdminSubmitResult = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("outcome")>] Outcome: Fsbar.Hub.Scripting.V1.AdminSubmitResult.Outcome // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("droppedCount")>] DroppedCount: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("reason")>] Reason: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("adminChannelStatus")>] AdminChannelStatus: Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo option // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<AdminSubmitResult>> =
        lazy
        // Field Definitions
        let Outcome = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.AdminSubmitResult.Outcome> (1, "outcome")
        let DroppedCount = FieldCodec.Primitive ValueCodec.Int32 (2, "droppedCount")
        let Reason = FieldCodec.Primitive ValueCodec.String (3, "reason")
        let AdminChannelStatus = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> (4, "adminChannelStatus")
        // Proto Definition Implementation
        { // ProtoDef<AdminSubmitResult>
            Name = "AdminSubmitResult"
            Empty = {
                Outcome = Outcome.GetDefault()
                DroppedCount = DroppedCount.GetDefault()
                Reason = Reason.GetDefault()
                AdminChannelStatus = AdminChannelStatus.GetDefault()
                }
            Size = fun (m: AdminSubmitResult) ->
                0
                + Outcome.CalcFieldSize m.Outcome
                + DroppedCount.CalcFieldSize m.DroppedCount
                + Reason.CalcFieldSize m.Reason
                + AdminChannelStatus.CalcFieldSize m.AdminChannelStatus
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: AdminSubmitResult) ->
                Outcome.WriteField w m.Outcome
                DroppedCount.WriteField w m.DroppedCount
                Reason.WriteField w m.Reason
                AdminChannelStatus.WriteField w m.AdminChannelStatus
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.AdminSubmitResult.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeOutcome = Outcome.WriteJsonField o
                let writeDroppedCount = DroppedCount.WriteJsonField o
                let writeReason = Reason.WriteJsonField o
                let writeAdminChannelStatus = AdminChannelStatus.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: AdminSubmitResult) =
                    writeOutcome w m.Outcome
                    writeDroppedCount w m.DroppedCount
                    writeReason w m.Reason
                    writeAdminChannelStatus w m.AdminChannelStatus
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : AdminSubmitResult =
                    match kvPair.Key with
                    | "outcome" -> { value with Outcome = Outcome.ReadJsonField kvPair.Value }
                    | "droppedCount" -> { value with DroppedCount = DroppedCount.ReadJsonField kvPair.Value }
                    | "reason" -> { value with Reason = Reason.ReadJsonField kvPair.Value }
                    | "adminChannelStatus" -> { value with AdminChannelStatus = AdminChannelStatus.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _AdminSubmitResult.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._AdminSubmitResult.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PauseRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = PauseRequest.empty

[<StructuralEquality;StructuralComparison>]
type PauseRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<PauseRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<PauseRequest>
            Name = "PauseRequest"
            Empty = PauseRequest.empty
            Size = fun (m: PauseRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PauseRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                PauseRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> PauseRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PauseResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminSubmitResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PauseResponse = {
            Result = x.Result.Build
            }

type private _PauseResponse = PauseResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PauseResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.AdminSubmitResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PauseResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<PauseResponse>
            Name = "PauseResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: PauseResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PauseResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PauseResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PauseResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PauseResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PauseResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PauseResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResumeRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ResumeRequest.empty

[<StructuralEquality;StructuralComparison>]
type ResumeRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ResumeRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ResumeRequest>
            Name = "ResumeRequest"
            Empty = ResumeRequest.empty
            Size = fun (m: ResumeRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ResumeRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ResumeRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ResumeRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ResumeResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminSubmitResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ResumeResponse = {
            Result = x.Result.Build
            }

type private _ResumeResponse = ResumeResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ResumeResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.AdminSubmitResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ResumeResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<ResumeResponse>
            Name = "ResumeResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: ResumeResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ResumeResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ResumeResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ResumeResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ResumeResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ResumeResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ResumeResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetEngineSpeedRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Speed: float32 // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Speed <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest = {
            Speed = x.Speed
            }

type private _SetEngineSpeedRequest = SetEngineSpeedRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetEngineSpeedRequest = {
    // Field Declarations
    /// <summary>
    /// Multiplier relative to 1.0x engine-native speed. Must be finite and
    /// positive; Hub-local validation rejects non-positive values.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("speed")>] Speed: float32 // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetEngineSpeedRequest>> =
        lazy
        // Field Definitions
        let Speed = FieldCodec.Primitive ValueCodec.Float (1, "speed")
        // Proto Definition Implementation
        { // ProtoDef<SetEngineSpeedRequest>
            Name = "SetEngineSpeedRequest"
            Empty = {
                Speed = Speed.GetDefault()
                }
            Size = fun (m: SetEngineSpeedRequest) ->
                0
                + Speed.CalcFieldSize m.Speed
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetEngineSpeedRequest) ->
                Speed.WriteField w m.Speed
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSpeed = Speed.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetEngineSpeedRequest) =
                    writeSpeed w m.Speed
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetEngineSpeedRequest =
                    match kvPair.Key with
                    | "speed" -> { value with Speed = Speed.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetEngineSpeedRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetEngineSpeedRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetEngineSpeedResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminSubmitResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse = {
            Result = x.Result.Build
            }

type private _SetEngineSpeedResponse = SetEngineSpeedResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetEngineSpeedResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.AdminSubmitResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetEngineSpeedResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SetEngineSpeedResponse>
            Name = "SetEngineSpeedResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SetEngineSpeedResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetEngineSpeedResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetEngineSpeedResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetEngineSpeedResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetEngineSpeedResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetEngineSpeedResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ForceEndMatchRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ForceEndMatchRequest.empty

[<StructuralEquality;StructuralComparison>]
type ForceEndMatchRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ForceEndMatchRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ForceEndMatchRequest>
            Name = "ForceEndMatchRequest"
            Empty = ForceEndMatchRequest.empty
            Size = fun (m: ForceEndMatchRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ForceEndMatchRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ForceEndMatchRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ForceEndMatchRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ForceEndMatchResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminSubmitResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ForceEndMatchResponse = {
            Result = x.Result.Build
            }

type private _ForceEndMatchResponse = ForceEndMatchResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ForceEndMatchResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.AdminSubmitResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ForceEndMatchResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<ForceEndMatchResponse>
            Name = "ForceEndMatchResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: ForceEndMatchResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ForceEndMatchResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ForceEndMatchResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ForceEndMatchResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ForceEndMatchResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ForceEndMatchResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ForceEndMatchResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendAdminMessageRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Text: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Text <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SendAdminMessageRequest = {
            Text = x.Text |> orEmptyString
            }

type private _SendAdminMessageRequest = SendAdminMessageRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendAdminMessageRequest = {
    // Field Declarations
    /// <summary>Free-form UTF-8. Empty strings are rejected locally.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("text")>] Text: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SendAdminMessageRequest>> =
        lazy
        // Field Definitions
        let Text = FieldCodec.Primitive ValueCodec.String (1, "text")
        // Proto Definition Implementation
        { // ProtoDef<SendAdminMessageRequest>
            Name = "SendAdminMessageRequest"
            Empty = {
                Text = Text.GetDefault()
                }
            Size = fun (m: SendAdminMessageRequest) ->
                0
                + Text.CalcFieldSize m.Text
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendAdminMessageRequest) ->
                Text.WriteField w m.Text
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SendAdminMessageRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeText = Text.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendAdminMessageRequest) =
                    writeText w m.Text
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendAdminMessageRequest =
                    match kvPair.Key with
                    | "text" -> { value with Text = Text.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendAdminMessageRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SendAdminMessageRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SendAdminMessageResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.AdminSubmitResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SendAdminMessageResponse = {
            Result = x.Result.Build
            }

type private _SendAdminMessageResponse = SendAdminMessageResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SendAdminMessageResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.AdminSubmitResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SendAdminMessageResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminSubmitResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SendAdminMessageResponse>
            Name = "SendAdminMessageResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SendAdminMessageResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SendAdminMessageResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SendAdminMessageResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SendAdminMessageResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SendAdminMessageResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SendAdminMessageResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SendAdminMessageResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MutationResult =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Outcome: Fsbar.Hub.Scripting.V1.SubmitOutcome // (1)
            val mutable Reason: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Outcome <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.SubmitOutcome>.ReadValue reader
            | 2 -> x.Reason <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.MutationResult = {
            Outcome = x.Outcome
            Reason = x.Reason |> orEmptyString
            }

/// <summary>Reused by every mutating RPC that doesn't need a richer response.</summary>
type private _MutationResult = MutationResult
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type MutationResult = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("outcome")>] Outcome: Fsbar.Hub.Scripting.V1.SubmitOutcome // (1)
    /// <summary>Populated when outcome = REJECTED.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("reason")>] Reason: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<MutationResult>> =
        lazy
        // Field Definitions
        let Outcome = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.SubmitOutcome> (1, "outcome")
        let Reason = FieldCodec.Primitive ValueCodec.String (2, "reason")
        // Proto Definition Implementation
        { // ProtoDef<MutationResult>
            Name = "MutationResult"
            Empty = {
                Outcome = Outcome.GetDefault()
                Reason = Reason.GetDefault()
                }
            Size = fun (m: MutationResult) ->
                0
                + Outcome.CalcFieldSize m.Outcome
                + Reason.CalcFieldSize m.Reason
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: MutationResult) ->
                Outcome.WriteField w m.Outcome
                Reason.WriteField w m.Reason
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.MutationResult.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeOutcome = Outcome.WriteJsonField o
                let writeReason = Reason.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: MutationResult) =
                    writeOutcome w m.Outcome
                    writeReason w m.Reason
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : MutationResult =
                    match kvPair.Key with
                    | "outcome" -> { value with Outcome = Outcome.ReadJsonField kvPair.Value }
                    | "reason" -> { value with Reason = Reason.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _MutationResult.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._MutationResult.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ConfigureLobbyRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Lobby: OptionBuilder<Fsbar.Hub.Scripting.V1.LobbyConfigWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Lobby.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest = {
            Lobby = x.Lobby.Build
            }

type private _ConfigureLobbyRequest = ConfigureLobbyRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ConfigureLobbyRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("lobby")>] Lobby: Fsbar.Hub.Scripting.V1.LobbyConfigWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ConfigureLobbyRequest>> =
        lazy
        // Field Definitions
        let Lobby = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire> (1, "lobby")
        // Proto Definition Implementation
        { // ProtoDef<ConfigureLobbyRequest>
            Name = "ConfigureLobbyRequest"
            Empty = {
                Lobby = Lobby.GetDefault()
                }
            Size = fun (m: ConfigureLobbyRequest) ->
                0
                + Lobby.CalcFieldSize m.Lobby
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ConfigureLobbyRequest) ->
                Lobby.WriteField w m.Lobby
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeLobby = Lobby.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ConfigureLobbyRequest) =
                    writeLobby w m.Lobby
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ConfigureLobbyRequest =
                    match kvPair.Key with
                    | "lobby" -> { value with Lobby = Lobby.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ConfigureLobbyRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ConfigureLobbyRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ConfigureLobbyResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable ValidationErrors: RepeatedBuilder<string> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.ValidationErrors.Add (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse = {
            Result = x.Result.Build
            ValidationErrors = x.ValidationErrors.Build
            }

type private _ConfigureLobbyResponse = ConfigureLobbyResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ConfigureLobbyResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("validationErrors")>] ValidationErrors: string list // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<ConfigureLobbyResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let ValidationErrors = FieldCodec.Repeated ValueCodec.String (2, "validationErrors")
        // Proto Definition Implementation
        { // ProtoDef<ConfigureLobbyResponse>
            Name = "ConfigureLobbyResponse"
            Empty = {
                Result = Result.GetDefault()
                ValidationErrors = ValidationErrors.GetDefault()
                }
            Size = fun (m: ConfigureLobbyResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + ValidationErrors.CalcFieldSize m.ValidationErrors
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ConfigureLobbyResponse) ->
                Result.WriteField w m.Result
                ValidationErrors.WriteField w m.ValidationErrors
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeValidationErrors = ValidationErrors.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ConfigureLobbyResponse) =
                    writeResult w m.Result
                    writeValidationErrors w m.ValidationErrors
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ConfigureLobbyResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "validationErrors" -> { value with ValidationErrors = ValidationErrors.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ConfigureLobbyResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ConfigureLobbyResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LobbyConfigWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable MapName: string // (1)
            val mutable Mode: Fsbar.Hub.Scripting.V1.LobbyMode // (2)
            val mutable EngineSpeed: float32 // (3)
            val mutable LaunchGraphicalViewer: bool // (4)
            val mutable Teams: RepeatedBuilder<Fsbar.Hub.Scripting.V1.TeamWire> // (5)
            val mutable Spectators: RepeatedBuilder<Fsbar.Hub.Scripting.V1.SpectatorWire> // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.MapName <- ValueCodec.String.ReadValue reader
            | 2 -> x.Mode <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LobbyMode>.ReadValue reader
            | 3 -> x.EngineSpeed <- ValueCodec.Float.ReadValue reader
            | 4 -> x.LaunchGraphicalViewer <- ValueCodec.Bool.ReadValue reader
            | 5 -> x.Teams.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.TeamWire>.ReadValue reader)
            | 6 -> x.Spectators.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.SpectatorWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LobbyConfigWire = {
            MapName = x.MapName |> orEmptyString
            Mode = x.Mode
            EngineSpeed = x.EngineSpeed
            LaunchGraphicalViewer = x.LaunchGraphicalViewer
            Teams = x.Teams.Build
            Spectators = x.Spectators.Build
            }

type private _LobbyConfigWire = LobbyConfigWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LobbyConfigWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("mapName")>] MapName: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("mode")>] Mode: Fsbar.Hub.Scripting.V1.LobbyMode // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("engineSpeed")>] EngineSpeed: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("launchGraphicalViewer")>] LaunchGraphicalViewer: bool // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("teams")>] Teams: Fsbar.Hub.Scripting.V1.TeamWire list // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("spectators")>] Spectators: Fsbar.Hub.Scripting.V1.SpectatorWire list // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<LobbyConfigWire>> =
        lazy
        // Field Definitions
        let MapName = FieldCodec.Primitive ValueCodec.String (1, "mapName")
        let Mode = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LobbyMode> (2, "mode")
        let EngineSpeed = FieldCodec.Primitive ValueCodec.Float (3, "engineSpeed")
        let LaunchGraphicalViewer = FieldCodec.Primitive ValueCodec.Bool (4, "launchGraphicalViewer")
        let Teams = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.TeamWire> (5, "teams")
        let Spectators = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.SpectatorWire> (6, "spectators")
        // Proto Definition Implementation
        { // ProtoDef<LobbyConfigWire>
            Name = "LobbyConfigWire"
            Empty = {
                MapName = MapName.GetDefault()
                Mode = Mode.GetDefault()
                EngineSpeed = EngineSpeed.GetDefault()
                LaunchGraphicalViewer = LaunchGraphicalViewer.GetDefault()
                Teams = Teams.GetDefault()
                Spectators = Spectators.GetDefault()
                }
            Size = fun (m: LobbyConfigWire) ->
                0
                + MapName.CalcFieldSize m.MapName
                + Mode.CalcFieldSize m.Mode
                + EngineSpeed.CalcFieldSize m.EngineSpeed
                + LaunchGraphicalViewer.CalcFieldSize m.LaunchGraphicalViewer
                + Teams.CalcFieldSize m.Teams
                + Spectators.CalcFieldSize m.Spectators
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LobbyConfigWire) ->
                MapName.WriteField w m.MapName
                Mode.WriteField w m.Mode
                EngineSpeed.WriteField w m.EngineSpeed
                LaunchGraphicalViewer.WriteField w m.LaunchGraphicalViewer
                Teams.WriteField w m.Teams
                Spectators.WriteField w m.Spectators
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LobbyConfigWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeMapName = MapName.WriteJsonField o
                let writeMode = Mode.WriteJsonField o
                let writeEngineSpeed = EngineSpeed.WriteJsonField o
                let writeLaunchGraphicalViewer = LaunchGraphicalViewer.WriteJsonField o
                let writeTeams = Teams.WriteJsonField o
                let writeSpectators = Spectators.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LobbyConfigWire) =
                    writeMapName w m.MapName
                    writeMode w m.Mode
                    writeEngineSpeed w m.EngineSpeed
                    writeLaunchGraphicalViewer w m.LaunchGraphicalViewer
                    writeTeams w m.Teams
                    writeSpectators w m.Spectators
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LobbyConfigWire =
                    match kvPair.Key with
                    | "mapName" -> { value with MapName = MapName.ReadJsonField kvPair.Value }
                    | "mode" -> { value with Mode = Mode.ReadJsonField kvPair.Value }
                    | "engineSpeed" -> { value with EngineSpeed = EngineSpeed.ReadJsonField kvPair.Value }
                    | "launchGraphicalViewer" -> { value with LaunchGraphicalViewer = LaunchGraphicalViewer.ReadJsonField kvPair.Value }
                    | "teams" -> { value with Teams = Teams.ReadJsonField kvPair.Value }
                    | "spectators" -> { value with Spectators = Spectators.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LobbyConfigWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LobbyConfigWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TeamWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable AllyTeamId: int // (1)
            val mutable Seats: RepeatedBuilder<Fsbar.Hub.Scripting.V1.SeatWire> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.AllyTeamId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.Seats.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.SeatWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.TeamWire = {
            AllyTeamId = x.AllyTeamId
            Seats = x.Seats.Build
            }

type private _TeamWire = TeamWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type TeamWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("allyTeamId")>] AllyTeamId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("seats")>] Seats: Fsbar.Hub.Scripting.V1.SeatWire list // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<TeamWire>> =
        lazy
        // Field Definitions
        let AllyTeamId = FieldCodec.Primitive ValueCodec.Int32 (1, "allyTeamId")
        let Seats = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.SeatWire> (2, "seats")
        // Proto Definition Implementation
        { // ProtoDef<TeamWire>
            Name = "TeamWire"
            Empty = {
                AllyTeamId = AllyTeamId.GetDefault()
                Seats = Seats.GetDefault()
                }
            Size = fun (m: TeamWire) ->
                0
                + AllyTeamId.CalcFieldSize m.AllyTeamId
                + Seats.CalcFieldSize m.Seats
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: TeamWire) ->
                AllyTeamId.WriteField w m.AllyTeamId
                Seats.WriteField w m.Seats
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.TeamWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeAllyTeamId = AllyTeamId.WriteJsonField o
                let writeSeats = Seats.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: TeamWire) =
                    writeAllyTeamId w m.AllyTeamId
                    writeSeats w m.Seats
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : TeamWire =
                    match kvPair.Key with
                    | "allyTeamId" -> { value with AllyTeamId = AllyTeamId.ReadJsonField kvPair.Value }
                    | "seats" -> { value with Seats = Seats.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _TeamWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._TeamWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SeatWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Kind: Fsbar.Hub.Scripting.V1.SeatKind // (1)
            val mutable Side: string // (2)
            val mutable Handicap: float32 // (3)
            val mutable AiName: string // (4)
            val mutable HumanName: string // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Kind <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.SeatKind>.ReadValue reader
            | 2 -> x.Side <- ValueCodec.String.ReadValue reader
            | 3 -> x.Handicap <- ValueCodec.Float.ReadValue reader
            | 4 -> x.AiName <- ValueCodec.String.ReadValue reader
            | 5 -> x.HumanName <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SeatWire = {
            Kind = x.Kind
            Side = x.Side |> orEmptyString
            Handicap = x.Handicap
            AiName = x.AiName |> orEmptyString
            HumanName = x.HumanName |> orEmptyString
            }

type private _SeatWire = SeatWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SeatWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("kind")>] Kind: Fsbar.Hub.Scripting.V1.SeatKind // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("side")>] Side: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("handicap")>] Handicap: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("aiName")>] AiName: string // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("humanName")>] HumanName: string // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<SeatWire>> =
        lazy
        // Field Definitions
        let Kind = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.SeatKind> (1, "kind")
        let Side = FieldCodec.Primitive ValueCodec.String (2, "side")
        let Handicap = FieldCodec.Primitive ValueCodec.Float (3, "handicap")
        let AiName = FieldCodec.Primitive ValueCodec.String (4, "aiName")
        let HumanName = FieldCodec.Primitive ValueCodec.String (5, "humanName")
        // Proto Definition Implementation
        { // ProtoDef<SeatWire>
            Name = "SeatWire"
            Empty = {
                Kind = Kind.GetDefault()
                Side = Side.GetDefault()
                Handicap = Handicap.GetDefault()
                AiName = AiName.GetDefault()
                HumanName = HumanName.GetDefault()
                }
            Size = fun (m: SeatWire) ->
                0
                + Kind.CalcFieldSize m.Kind
                + Side.CalcFieldSize m.Side
                + Handicap.CalcFieldSize m.Handicap
                + AiName.CalcFieldSize m.AiName
                + HumanName.CalcFieldSize m.HumanName
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SeatWire) ->
                Kind.WriteField w m.Kind
                Side.WriteField w m.Side
                Handicap.WriteField w m.Handicap
                AiName.WriteField w m.AiName
                HumanName.WriteField w m.HumanName
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SeatWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeKind = Kind.WriteJsonField o
                let writeSide = Side.WriteJsonField o
                let writeHandicap = Handicap.WriteJsonField o
                let writeAiName = AiName.WriteJsonField o
                let writeHumanName = HumanName.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SeatWire) =
                    writeKind w m.Kind
                    writeSide w m.Side
                    writeHandicap w m.Handicap
                    writeAiName w m.AiName
                    writeHumanName w m.HumanName
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SeatWire =
                    match kvPair.Key with
                    | "kind" -> { value with Kind = Kind.ReadJsonField kvPair.Value }
                    | "side" -> { value with Side = Side.ReadJsonField kvPair.Value }
                    | "handicap" -> { value with Handicap = Handicap.ReadJsonField kvPair.Value }
                    | "aiName" -> { value with AiName = AiName.ReadJsonField kvPair.Value }
                    | "humanName" -> { value with HumanName = HumanName.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SeatWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SeatWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SpectatorWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SpectatorWire = {
            Name = x.Name |> orEmptyString
            }

type private _SpectatorWire = SpectatorWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SpectatorWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SpectatorWire>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        // Proto Definition Implementation
        { // ProtoDef<SpectatorWire>
            Name = "SpectatorWire"
            Empty = {
                Name = Name.GetDefault()
                }
            Size = fun (m: SpectatorWire) ->
                0
                + Name.CalcFieldSize m.Name
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SpectatorWire) ->
                Name.WriteField w m.Name
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SpectatorWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SpectatorWire) =
                    writeName w m.Name
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SpectatorWire =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SpectatorWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SpectatorWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListMapsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ListMapsRequest.empty

[<StructuralEquality;StructuralComparison>]
type ListMapsRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ListMapsRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ListMapsRequest>
            Name = "ListMapsRequest"
            Empty = ListMapsRequest.empty
            Size = fun (m: ListMapsRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListMapsRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ListMapsRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ListMapsRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListMapsResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Maps: RepeatedBuilder<Fsbar.Hub.Scripting.V1.MapDescriptor> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Maps.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MapDescriptor>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ListMapsResponse = {
            Maps = x.Maps.Build
            }

type private _ListMapsResponse = ListMapsResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ListMapsResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("maps")>] Maps: Fsbar.Hub.Scripting.V1.MapDescriptor list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ListMapsResponse>> =
        lazy
        // Field Definitions
        let Maps = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.MapDescriptor> (1, "maps")
        // Proto Definition Implementation
        { // ProtoDef<ListMapsResponse>
            Name = "ListMapsResponse"
            Empty = {
                Maps = Maps.GetDefault()
                }
            Size = fun (m: ListMapsResponse) ->
                0
                + Maps.CalcFieldSize m.Maps
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListMapsResponse) ->
                Maps.WriteField w m.Maps
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ListMapsResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeMaps = Maps.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ListMapsResponse) =
                    writeMaps w m.Maps
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ListMapsResponse =
                    match kvPair.Key with
                    | "maps" -> { value with Maps = Maps.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ListMapsResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ListMapsResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module MapDescriptor =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
            val mutable FilePath: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | 2 -> x.FilePath <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.MapDescriptor = {
            Name = x.Name |> orEmptyString
            FilePath = x.FilePath |> orEmptyString
            }

type private _MapDescriptor = MapDescriptor
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type MapDescriptor = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("filePath")>] FilePath: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<MapDescriptor>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        let FilePath = FieldCodec.Primitive ValueCodec.String (2, "filePath")
        // Proto Definition Implementation
        { // ProtoDef<MapDescriptor>
            Name = "MapDescriptor"
            Empty = {
                Name = Name.GetDefault()
                FilePath = FilePath.GetDefault()
                }
            Size = fun (m: MapDescriptor) ->
                0
                + Name.CalcFieldSize m.Name
                + FilePath.CalcFieldSize m.FilePath
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: MapDescriptor) ->
                Name.WriteField w m.Name
                FilePath.WriteField w m.FilePath
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.MapDescriptor.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let writeFilePath = FilePath.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: MapDescriptor) =
                    writeName w m.Name
                    writeFilePath w m.FilePath
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : MapDescriptor =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | "filePath" -> { value with FilePath = FilePath.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _MapDescriptor.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._MapDescriptor.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ValidateLobbyRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Lobby: OptionBuilder<Fsbar.Hub.Scripting.V1.LobbyConfigWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Lobby.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ValidateLobbyRequest = {
            Lobby = x.Lobby.Build
            }

type private _ValidateLobbyRequest = ValidateLobbyRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ValidateLobbyRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("lobby")>] Lobby: Fsbar.Hub.Scripting.V1.LobbyConfigWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ValidateLobbyRequest>> =
        lazy
        // Field Definitions
        let Lobby = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire> (1, "lobby")
        // Proto Definition Implementation
        { // ProtoDef<ValidateLobbyRequest>
            Name = "ValidateLobbyRequest"
            Empty = {
                Lobby = Lobby.GetDefault()
                }
            Size = fun (m: ValidateLobbyRequest) ->
                0
                + Lobby.CalcFieldSize m.Lobby
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ValidateLobbyRequest) ->
                Lobby.WriteField w m.Lobby
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ValidateLobbyRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeLobby = Lobby.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ValidateLobbyRequest) =
                    writeLobby w m.Lobby
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ValidateLobbyRequest =
                    match kvPair.Key with
                    | "lobby" -> { value with Lobby = Lobby.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ValidateLobbyRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ValidateLobbyRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ValidateLobbyResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Errors: RepeatedBuilder<string> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Errors.Add (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ValidateLobbyResponse = {
            Errors = x.Errors.Build
            }

type private _ValidateLobbyResponse = ValidateLobbyResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ValidateLobbyResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("errors")>] Errors: string list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ValidateLobbyResponse>> =
        lazy
        // Field Definitions
        let Errors = FieldCodec.Repeated ValueCodec.String (1, "errors")
        // Proto Definition Implementation
        { // ProtoDef<ValidateLobbyResponse>
            Name = "ValidateLobbyResponse"
            Empty = {
                Errors = Errors.GetDefault()
                }
            Size = fun (m: ValidateLobbyResponse) ->
                0
                + Errors.CalcFieldSize m.Errors
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ValidateLobbyResponse) ->
                Errors.WriteField w m.Errors
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ValidateLobbyResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeErrors = Errors.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ValidateLobbyResponse) =
                    writeErrors w m.Errors
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ValidateLobbyResponse =
                    match kvPair.Key with
                    | "errors" -> { value with Errors = Errors.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ValidateLobbyResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ValidateLobbyResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LaunchSessionRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StartPaused: bool // (1)
            val mutable LaunchGraphicalViewer: bool // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StartPaused <- ValueCodec.Bool.ReadValue reader
            | 2 -> x.LaunchGraphicalViewer <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LaunchSessionRequest = {
            StartPaused = x.StartPaused
            LaunchGraphicalViewer = x.LaunchGraphicalViewer
            }

type private _LaunchSessionRequest = LaunchSessionRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LaunchSessionRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("startPaused")>] StartPaused: bool // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("launchGraphicalViewer")>] LaunchGraphicalViewer: bool // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<LaunchSessionRequest>> =
        lazy
        // Field Definitions
        let StartPaused = FieldCodec.Primitive ValueCodec.Bool (1, "startPaused")
        let LaunchGraphicalViewer = FieldCodec.Primitive ValueCodec.Bool (2, "launchGraphicalViewer")
        // Proto Definition Implementation
        { // ProtoDef<LaunchSessionRequest>
            Name = "LaunchSessionRequest"
            Empty = {
                StartPaused = StartPaused.GetDefault()
                LaunchGraphicalViewer = LaunchGraphicalViewer.GetDefault()
                }
            Size = fun (m: LaunchSessionRequest) ->
                0
                + StartPaused.CalcFieldSize m.StartPaused
                + LaunchGraphicalViewer.CalcFieldSize m.LaunchGraphicalViewer
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LaunchSessionRequest) ->
                StartPaused.WriteField w m.StartPaused
                LaunchGraphicalViewer.WriteField w m.LaunchGraphicalViewer
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LaunchSessionRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStartPaused = StartPaused.WriteJsonField o
                let writeLaunchGraphicalViewer = LaunchGraphicalViewer.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LaunchSessionRequest) =
                    writeStartPaused w m.StartPaused
                    writeLaunchGraphicalViewer w m.LaunchGraphicalViewer
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LaunchSessionRequest =
                    match kvPair.Key with
                    | "startPaused" -> { value with StartPaused = StartPaused.ReadJsonField kvPair.Value }
                    | "launchGraphicalViewer" -> { value with LaunchGraphicalViewer = LaunchGraphicalViewer.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LaunchSessionRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LaunchSessionRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LaunchSessionResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable SessionId: OptionBuilder<string> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.SessionId.Set (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LaunchSessionResponse = {
            Result = x.Result.Build
            SessionId = x.SessionId.Build
            }

type private _LaunchSessionResponse = LaunchSessionResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LaunchSessionResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("sessionId")>] SessionId: string option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<LaunchSessionResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let SessionId = FieldCodec.Optional ValueCodec.String (2, "sessionId")
        // Proto Definition Implementation
        { // ProtoDef<LaunchSessionResponse>
            Name = "LaunchSessionResponse"
            Empty = {
                Result = Result.GetDefault()
                SessionId = SessionId.GetDefault()
                }
            Size = fun (m: LaunchSessionResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + SessionId.CalcFieldSize m.SessionId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LaunchSessionResponse) ->
                Result.WriteField w m.Result
                SessionId.WriteField w m.SessionId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LaunchSessionResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeSessionId = SessionId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LaunchSessionResponse) =
                    writeResult w m.Result
                    writeSessionId w m.SessionId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LaunchSessionResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "sessionId" -> { value with SessionId = SessionId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LaunchSessionResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LaunchSessionResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StopSessionRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = StopSessionRequest.empty

[<StructuralEquality;StructuralComparison>]
type StopSessionRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<StopSessionRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<StopSessionRequest>
            Name = "StopSessionRequest"
            Empty = StopSessionRequest.empty
            Size = fun (m: StopSessionRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StopSessionRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                StopSessionRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> StopSessionRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StopSessionResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StopSessionResponse = {
            Result = x.Result.Build
            }

type private _StopSessionResponse = StopSessionResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StopSessionResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<StopSessionResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<StopSessionResponse>
            Name = "StopSessionResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: StopSessionResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StopSessionResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StopSessionResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StopSessionResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StopSessionResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StopSessionResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StopSessionResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StreamRenderFramesRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ClientLabel: string // (1)
            val mutable TargetHz: int // (2)
            val mutable Format: Fsbar.Hub.Scripting.V1.ImageFormat // (3)
            val mutable ViewportWidth: int // (4)
            val mutable ViewportHeight: int // (5)
            val mutable JpegQuality: int // (6)
            val mutable CloseOnSessionEnd: bool // (7)
            val mutable EmitNoSessionPlaceholder: bool // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ClientLabel <- ValueCodec.String.ReadValue reader
            | 2 -> x.TargetHz <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Format <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat>.ReadValue reader
            | 4 -> x.ViewportWidth <- ValueCodec.Int32.ReadValue reader
            | 5 -> x.ViewportHeight <- ValueCodec.Int32.ReadValue reader
            | 6 -> x.JpegQuality <- ValueCodec.Int32.ReadValue reader
            | 7 -> x.CloseOnSessionEnd <- ValueCodec.Bool.ReadValue reader
            | 8 -> x.EmitNoSessionPlaceholder <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest = {
            ClientLabel = x.ClientLabel |> orEmptyString
            TargetHz = x.TargetHz
            Format = x.Format
            ViewportWidth = x.ViewportWidth
            ViewportHeight = x.ViewportHeight
            JpegQuality = x.JpegQuality
            CloseOnSessionEnd = x.CloseOnSessionEnd
            EmitNoSessionPlaceholder = x.EmitNoSessionPlaceholder
            }

type private _StreamRenderFramesRequest = StreamRenderFramesRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StreamRenderFramesRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("clientLabel")>] ClientLabel: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("targetHz")>] TargetHz: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("format")>] Format: Fsbar.Hub.Scripting.V1.ImageFormat // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportWidth")>] ViewportWidth: int // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportHeight")>] ViewportHeight: int // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("jpegQuality")>] JpegQuality: int // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("closeOnSessionEnd")>] CloseOnSessionEnd: bool // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("emitNoSessionPlaceholder")>] EmitNoSessionPlaceholder: bool // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<StreamRenderFramesRequest>> =
        lazy
        // Field Definitions
        let ClientLabel = FieldCodec.Primitive ValueCodec.String (1, "clientLabel")
        let TargetHz = FieldCodec.Primitive ValueCodec.Int32 (2, "targetHz")
        let Format = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat> (3, "format")
        let ViewportWidth = FieldCodec.Primitive ValueCodec.Int32 (4, "viewportWidth")
        let ViewportHeight = FieldCodec.Primitive ValueCodec.Int32 (5, "viewportHeight")
        let JpegQuality = FieldCodec.Primitive ValueCodec.Int32 (6, "jpegQuality")
        let CloseOnSessionEnd = FieldCodec.Primitive ValueCodec.Bool (7, "closeOnSessionEnd")
        let EmitNoSessionPlaceholder = FieldCodec.Primitive ValueCodec.Bool (8, "emitNoSessionPlaceholder")
        // Proto Definition Implementation
        { // ProtoDef<StreamRenderFramesRequest>
            Name = "StreamRenderFramesRequest"
            Empty = {
                ClientLabel = ClientLabel.GetDefault()
                TargetHz = TargetHz.GetDefault()
                Format = Format.GetDefault()
                ViewportWidth = ViewportWidth.GetDefault()
                ViewportHeight = ViewportHeight.GetDefault()
                JpegQuality = JpegQuality.GetDefault()
                CloseOnSessionEnd = CloseOnSessionEnd.GetDefault()
                EmitNoSessionPlaceholder = EmitNoSessionPlaceholder.GetDefault()
                }
            Size = fun (m: StreamRenderFramesRequest) ->
                0
                + ClientLabel.CalcFieldSize m.ClientLabel
                + TargetHz.CalcFieldSize m.TargetHz
                + Format.CalcFieldSize m.Format
                + ViewportWidth.CalcFieldSize m.ViewportWidth
                + ViewportHeight.CalcFieldSize m.ViewportHeight
                + JpegQuality.CalcFieldSize m.JpegQuality
                + CloseOnSessionEnd.CalcFieldSize m.CloseOnSessionEnd
                + EmitNoSessionPlaceholder.CalcFieldSize m.EmitNoSessionPlaceholder
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StreamRenderFramesRequest) ->
                ClientLabel.WriteField w m.ClientLabel
                TargetHz.WriteField w m.TargetHz
                Format.WriteField w m.Format
                ViewportWidth.WriteField w m.ViewportWidth
                ViewportHeight.WriteField w m.ViewportHeight
                JpegQuality.WriteField w m.JpegQuality
                CloseOnSessionEnd.WriteField w m.CloseOnSessionEnd
                EmitNoSessionPlaceholder.WriteField w m.EmitNoSessionPlaceholder
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeClientLabel = ClientLabel.WriteJsonField o
                let writeTargetHz = TargetHz.WriteJsonField o
                let writeFormat = Format.WriteJsonField o
                let writeViewportWidth = ViewportWidth.WriteJsonField o
                let writeViewportHeight = ViewportHeight.WriteJsonField o
                let writeJpegQuality = JpegQuality.WriteJsonField o
                let writeCloseOnSessionEnd = CloseOnSessionEnd.WriteJsonField o
                let writeEmitNoSessionPlaceholder = EmitNoSessionPlaceholder.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StreamRenderFramesRequest) =
                    writeClientLabel w m.ClientLabel
                    writeTargetHz w m.TargetHz
                    writeFormat w m.Format
                    writeViewportWidth w m.ViewportWidth
                    writeViewportHeight w m.ViewportHeight
                    writeJpegQuality w m.JpegQuality
                    writeCloseOnSessionEnd w m.CloseOnSessionEnd
                    writeEmitNoSessionPlaceholder w m.EmitNoSessionPlaceholder
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StreamRenderFramesRequest =
                    match kvPair.Key with
                    | "clientLabel" -> { value with ClientLabel = ClientLabel.ReadJsonField kvPair.Value }
                    | "targetHz" -> { value with TargetHz = TargetHz.ReadJsonField kvPair.Value }
                    | "format" -> { value with Format = Format.ReadJsonField kvPair.Value }
                    | "viewportWidth" -> { value with ViewportWidth = ViewportWidth.ReadJsonField kvPair.Value }
                    | "viewportHeight" -> { value with ViewportHeight = ViewportHeight.ReadJsonField kvPair.Value }
                    | "jpegQuality" -> { value with JpegQuality = JpegQuality.ReadJsonField kvPair.Value }
                    | "closeOnSessionEnd" -> { value with CloseOnSessionEnd = CloseOnSessionEnd.ReadJsonField kvPair.Value }
                    | "emitNoSessionPlaceholder" -> { value with EmitNoSessionPlaceholder = EmitNoSessionPlaceholder.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StreamRenderFramesRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StreamRenderFramesRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RenderFrameMessage =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ImageBytes: FsGrpc.Bytes // (1)
            val mutable Format: Fsbar.Hub.Scripting.V1.ImageFormat // (2)
            val mutable RenderedAtUnixMs: int64 // (3)
            val mutable EncodedAtUnixMs: int64 // (4)
            val mutable ClientSequence: uint64 // (5)
            val mutable ViewportWidth: int // (6)
            val mutable ViewportHeight: int // (7)
            val mutable Quality: int // (8)
            val mutable IsPlaceholder: bool // (9)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ImageBytes <- ValueCodec.Bytes.ReadValue reader
            | 2 -> x.Format <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat>.ReadValue reader
            | 3 -> x.RenderedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 4 -> x.EncodedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 5 -> x.ClientSequence <- ValueCodec.UInt64.ReadValue reader
            | 6 -> x.ViewportWidth <- ValueCodec.Int32.ReadValue reader
            | 7 -> x.ViewportHeight <- ValueCodec.Int32.ReadValue reader
            | 8 -> x.Quality <- ValueCodec.Int32.ReadValue reader
            | 9 -> x.IsPlaceholder <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.RenderFrameMessage = {
            ImageBytes = x.ImageBytes
            Format = x.Format
            RenderedAtUnixMs = x.RenderedAtUnixMs
            EncodedAtUnixMs = x.EncodedAtUnixMs
            ClientSequence = x.ClientSequence
            ViewportWidth = x.ViewportWidth
            ViewportHeight = x.ViewportHeight
            Quality = x.Quality
            IsPlaceholder = x.IsPlaceholder
            }

type private _RenderFrameMessage = RenderFrameMessage
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RenderFrameMessage = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("imageBytes")>] ImageBytes: FsGrpc.Bytes // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("format")>] Format: Fsbar.Hub.Scripting.V1.ImageFormat // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("renderedAtUnixMs")>] RenderedAtUnixMs: int64 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("encodedAtUnixMs")>] EncodedAtUnixMs: int64 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("clientSequence")>] ClientSequence: uint64 // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportWidth")>] ViewportWidth: int // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportHeight")>] ViewportHeight: int // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("quality")>] Quality: int // (8)
    [<System.Text.Json.Serialization.JsonPropertyName("isPlaceholder")>] IsPlaceholder: bool // (9)
    }
    with
    static member Proto : Lazy<ProtoDef<RenderFrameMessage>> =
        lazy
        // Field Definitions
        let ImageBytes = FieldCodec.Primitive ValueCodec.Bytes (1, "imageBytes")
        let Format = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat> (2, "format")
        let RenderedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (3, "renderedAtUnixMs")
        let EncodedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (4, "encodedAtUnixMs")
        let ClientSequence = FieldCodec.Primitive ValueCodec.UInt64 (5, "clientSequence")
        let ViewportWidth = FieldCodec.Primitive ValueCodec.Int32 (6, "viewportWidth")
        let ViewportHeight = FieldCodec.Primitive ValueCodec.Int32 (7, "viewportHeight")
        let Quality = FieldCodec.Primitive ValueCodec.Int32 (8, "quality")
        let IsPlaceholder = FieldCodec.Primitive ValueCodec.Bool (9, "isPlaceholder")
        // Proto Definition Implementation
        { // ProtoDef<RenderFrameMessage>
            Name = "RenderFrameMessage"
            Empty = {
                ImageBytes = ImageBytes.GetDefault()
                Format = Format.GetDefault()
                RenderedAtUnixMs = RenderedAtUnixMs.GetDefault()
                EncodedAtUnixMs = EncodedAtUnixMs.GetDefault()
                ClientSequence = ClientSequence.GetDefault()
                ViewportWidth = ViewportWidth.GetDefault()
                ViewportHeight = ViewportHeight.GetDefault()
                Quality = Quality.GetDefault()
                IsPlaceholder = IsPlaceholder.GetDefault()
                }
            Size = fun (m: RenderFrameMessage) ->
                0
                + ImageBytes.CalcFieldSize m.ImageBytes
                + Format.CalcFieldSize m.Format
                + RenderedAtUnixMs.CalcFieldSize m.RenderedAtUnixMs
                + EncodedAtUnixMs.CalcFieldSize m.EncodedAtUnixMs
                + ClientSequence.CalcFieldSize m.ClientSequence
                + ViewportWidth.CalcFieldSize m.ViewportWidth
                + ViewportHeight.CalcFieldSize m.ViewportHeight
                + Quality.CalcFieldSize m.Quality
                + IsPlaceholder.CalcFieldSize m.IsPlaceholder
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RenderFrameMessage) ->
                ImageBytes.WriteField w m.ImageBytes
                Format.WriteField w m.Format
                RenderedAtUnixMs.WriteField w m.RenderedAtUnixMs
                EncodedAtUnixMs.WriteField w m.EncodedAtUnixMs
                ClientSequence.WriteField w m.ClientSequence
                ViewportWidth.WriteField w m.ViewportWidth
                ViewportHeight.WriteField w m.ViewportHeight
                Quality.WriteField w m.Quality
                IsPlaceholder.WriteField w m.IsPlaceholder
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.RenderFrameMessage.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeImageBytes = ImageBytes.WriteJsonField o
                let writeFormat = Format.WriteJsonField o
                let writeRenderedAtUnixMs = RenderedAtUnixMs.WriteJsonField o
                let writeEncodedAtUnixMs = EncodedAtUnixMs.WriteJsonField o
                let writeClientSequence = ClientSequence.WriteJsonField o
                let writeViewportWidth = ViewportWidth.WriteJsonField o
                let writeViewportHeight = ViewportHeight.WriteJsonField o
                let writeQuality = Quality.WriteJsonField o
                let writeIsPlaceholder = IsPlaceholder.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RenderFrameMessage) =
                    writeImageBytes w m.ImageBytes
                    writeFormat w m.Format
                    writeRenderedAtUnixMs w m.RenderedAtUnixMs
                    writeEncodedAtUnixMs w m.EncodedAtUnixMs
                    writeClientSequence w m.ClientSequence
                    writeViewportWidth w m.ViewportWidth
                    writeViewportHeight w m.ViewportHeight
                    writeQuality w m.Quality
                    writeIsPlaceholder w m.IsPlaceholder
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RenderFrameMessage =
                    match kvPair.Key with
                    | "imageBytes" -> { value with ImageBytes = ImageBytes.ReadJsonField kvPair.Value }
                    | "format" -> { value with Format = Format.ReadJsonField kvPair.Value }
                    | "renderedAtUnixMs" -> { value with RenderedAtUnixMs = RenderedAtUnixMs.ReadJsonField kvPair.Value }
                    | "encodedAtUnixMs" -> { value with EncodedAtUnixMs = EncodedAtUnixMs.ReadJsonField kvPair.Value }
                    | "clientSequence" -> { value with ClientSequence = ClientSequence.ReadJsonField kvPair.Value }
                    | "viewportWidth" -> { value with ViewportWidth = ViewportWidth.ReadJsonField kvPair.Value }
                    | "viewportHeight" -> { value with ViewportHeight = ViewportHeight.ReadJsonField kvPair.Value }
                    | "quality" -> { value with Quality = Quality.ReadJsonField kvPair.Value }
                    | "isPlaceholder" -> { value with IsPlaceholder = IsPlaceholder.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RenderFrameMessage.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._RenderFrameMessage.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetRenderFrameRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Format: Fsbar.Hub.Scripting.V1.ImageFormat // (1)
            val mutable ViewportWidth: int // (2)
            val mutable ViewportHeight: int // (3)
            val mutable JpegQuality: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Format <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat>.ReadValue reader
            | 2 -> x.ViewportWidth <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.ViewportHeight <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.JpegQuality <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetRenderFrameRequest = {
            Format = x.Format
            ViewportWidth = x.ViewportWidth
            ViewportHeight = x.ViewportHeight
            JpegQuality = x.JpegQuality
            }

type private _GetRenderFrameRequest = GetRenderFrameRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetRenderFrameRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("format")>] Format: Fsbar.Hub.Scripting.V1.ImageFormat // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportWidth")>] ViewportWidth: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("viewportHeight")>] ViewportHeight: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("jpegQuality")>] JpegQuality: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<GetRenderFrameRequest>> =
        lazy
        // Field Definitions
        let Format = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.ImageFormat> (1, "format")
        let ViewportWidth = FieldCodec.Primitive ValueCodec.Int32 (2, "viewportWidth")
        let ViewportHeight = FieldCodec.Primitive ValueCodec.Int32 (3, "viewportHeight")
        let JpegQuality = FieldCodec.Primitive ValueCodec.Int32 (4, "jpegQuality")
        // Proto Definition Implementation
        { // ProtoDef<GetRenderFrameRequest>
            Name = "GetRenderFrameRequest"
            Empty = {
                Format = Format.GetDefault()
                ViewportWidth = ViewportWidth.GetDefault()
                ViewportHeight = ViewportHeight.GetDefault()
                JpegQuality = JpegQuality.GetDefault()
                }
            Size = fun (m: GetRenderFrameRequest) ->
                0
                + Format.CalcFieldSize m.Format
                + ViewportWidth.CalcFieldSize m.ViewportWidth
                + ViewportHeight.CalcFieldSize m.ViewportHeight
                + JpegQuality.CalcFieldSize m.JpegQuality
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetRenderFrameRequest) ->
                Format.WriteField w m.Format
                ViewportWidth.WriteField w m.ViewportWidth
                ViewportHeight.WriteField w m.ViewportHeight
                JpegQuality.WriteField w m.JpegQuality
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetRenderFrameRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFormat = Format.WriteJsonField o
                let writeViewportWidth = ViewportWidth.WriteJsonField o
                let writeViewportHeight = ViewportHeight.WriteJsonField o
                let writeJpegQuality = JpegQuality.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetRenderFrameRequest) =
                    writeFormat w m.Format
                    writeViewportWidth w m.ViewportWidth
                    writeViewportHeight w m.ViewportHeight
                    writeJpegQuality w m.JpegQuality
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetRenderFrameRequest =
                    match kvPair.Key with
                    | "format" -> { value with Format = Format.ReadJsonField kvPair.Value }
                    | "viewportWidth" -> { value with ViewportWidth = ViewportWidth.ReadJsonField kvPair.Value }
                    | "viewportHeight" -> { value with ViewportHeight = ViewportHeight.ReadJsonField kvPair.Value }
                    | "jpegQuality" -> { value with JpegQuality = JpegQuality.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetRenderFrameRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetRenderFrameRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetRenderFrameResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Frame: OptionBuilder<Fsbar.Hub.Scripting.V1.RenderFrameMessage> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Frame.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.RenderFrameMessage>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetRenderFrameResponse = {
            Frame = x.Frame.Build
            }

type private _GetRenderFrameResponse = GetRenderFrameResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetRenderFrameResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("frame")>] Frame: Fsbar.Hub.Scripting.V1.RenderFrameMessage option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<GetRenderFrameResponse>> =
        lazy
        // Field Definitions
        let Frame = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.RenderFrameMessage> (1, "frame")
        // Proto Definition Implementation
        { // ProtoDef<GetRenderFrameResponse>
            Name = "GetRenderFrameResponse"
            Empty = {
                Frame = Frame.GetDefault()
                }
            Size = fun (m: GetRenderFrameResponse) ->
                0
                + Frame.CalcFieldSize m.Frame
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetRenderFrameResponse) ->
                Frame.WriteField w m.Frame
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetRenderFrameResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrame = Frame.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetRenderFrameResponse) =
                    writeFrame w m.Frame
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetRenderFrameResponse =
                    match kvPair.Key with
                    | "frame" -> { value with Frame = Frame.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetRenderFrameResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetRenderFrameResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module VizAttributeValue =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<ValueCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type ValueCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("boolValue")>] BoolValue of bool
    | [<System.Text.Json.Serialization.JsonPropertyName("intValue")>] IntValue of int
    | [<System.Text.Json.Serialization.JsonPropertyName("floatValue")>] FloatValue of double
    | [<System.Text.Json.Serialization.JsonPropertyName("stringValue")>] StringValue of string
    | [<System.Text.Json.Serialization.JsonPropertyName("colorRgba")>] ColorRgba of uint32
    | [<System.Text.Json.Serialization.JsonPropertyName("stringListValue")>] StringListValue of Fsbar.Hub.Scripting.V1.StringList
    with
        static member OneofCodec : Lazy<OneofCodec<ValueCase>> = 
            lazy
            let BoolValue = FieldCodec.OneofCase "value" ValueCodec.Bool (1, "boolValue")
            let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (2, "intValue")
            let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Double (3, "floatValue")
            let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (4, "stringValue")
            let ColorRgba = FieldCodec.OneofCase "value" ValueCodec.UInt32 (5, "colorRgba")
            let StringListValue = FieldCodec.OneofCase "value" ValueCodec.Message<Fsbar.Hub.Scripting.V1.StringList> (6, "stringListValue")
            let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
                ("boolValue", fun node -> ValueCase.BoolValue (BoolValue.ReadJsonField node))
                ("intValue", fun node -> ValueCase.IntValue (IntValue.ReadJsonField node))
                ("floatValue", fun node -> ValueCase.FloatValue (FloatValue.ReadJsonField node))
                ("stringValue", fun node -> ValueCase.StringValue (StringValue.ReadJsonField node))
                ("colorRgba", fun node -> ValueCase.ColorRgba (ColorRgba.ReadJsonField node))
                ("stringListValue", fun node -> ValueCase.StringListValue (StringListValue.ReadJsonField node))
                ])
            Value

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Value: OptionBuilder<Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Value.Set (ValueCase.BoolValue (ValueCodec.Bool.ReadValue reader))
            | 2 -> x.Value.Set (ValueCase.IntValue (ValueCodec.Int32.ReadValue reader))
            | 3 -> x.Value.Set (ValueCase.FloatValue (ValueCodec.Double.ReadValue reader))
            | 4 -> x.Value.Set (ValueCase.StringValue (ValueCodec.String.ReadValue reader))
            | 5 -> x.Value.Set (ValueCase.ColorRgba (ValueCodec.UInt32.ReadValue reader))
            | 6 -> x.Value.Set (ValueCase.StringListValue (ValueCodec.Message<Fsbar.Hub.Scripting.V1.StringList>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.VizAttributeValue = {
            Value = x.Value.Build |> (Option.defaultValue ValueCase.None)
            }

type private _VizAttributeValue = VizAttributeValue
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type VizAttributeValue = {
    // Field Declarations
    Value: Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase
    }
    with
    static member Proto : Lazy<ProtoDef<VizAttributeValue>> =
        lazy
        // Field Definitions
        let BoolValue = FieldCodec.OneofCase "value" ValueCodec.Bool (1, "boolValue")
        let IntValue = FieldCodec.OneofCase "value" ValueCodec.Int32 (2, "intValue")
        let FloatValue = FieldCodec.OneofCase "value" ValueCodec.Double (3, "floatValue")
        let StringValue = FieldCodec.OneofCase "value" ValueCodec.String (4, "stringValue")
        let ColorRgba = FieldCodec.OneofCase "value" ValueCodec.UInt32 (5, "colorRgba")
        let StringListValue = FieldCodec.OneofCase "value" ValueCodec.Message<Fsbar.Hub.Scripting.V1.StringList> (6, "stringListValue")
        let Value = FieldCodec.Oneof "value" (FSharp.Collections.Map [
            ("boolValue", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.BoolValue (BoolValue.ReadJsonField node))
            ("intValue", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.IntValue (IntValue.ReadJsonField node))
            ("floatValue", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.FloatValue (FloatValue.ReadJsonField node))
            ("stringValue", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringValue (StringValue.ReadJsonField node))
            ("colorRgba", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.ColorRgba (ColorRgba.ReadJsonField node))
            ("stringListValue", fun node -> Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringListValue (StringListValue.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<VizAttributeValue>
            Name = "VizAttributeValue"
            Empty = {
                Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.None
                }
            Size = fun (m: VizAttributeValue) ->
                0
                + match m.Value with
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.BoolValue v -> BoolValue.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.IntValue v -> IntValue.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.FloatValue v -> FloatValue.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringValue v -> StringValue.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.ColorRgba v -> ColorRgba.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringListValue v -> StringListValue.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: VizAttributeValue) ->
                (match m.Value with
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.None -> ()
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.BoolValue v -> BoolValue.WriteField w v
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.IntValue v -> IntValue.WriteField w v
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.FloatValue v -> FloatValue.WriteField w v
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringValue v -> StringValue.WriteField w v
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.ColorRgba v -> ColorRgba.WriteField w v
                | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringListValue v -> StringListValue.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.VizAttributeValue.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValueNone = Value.WriteJsonNoneCase o
                let writeBoolValue = BoolValue.WriteJsonField o
                let writeIntValue = IntValue.WriteJsonField o
                let writeFloatValue = FloatValue.WriteJsonField o
                let writeStringValue = StringValue.WriteJsonField o
                let writeColorRgba = ColorRgba.WriteJsonField o
                let writeStringListValue = StringListValue.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: VizAttributeValue) =
                    (match m.Value with
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.None -> writeValueNone w
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.BoolValue v -> writeBoolValue w v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.IntValue v -> writeIntValue w v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.FloatValue v -> writeFloatValue w v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringValue v -> writeStringValue w v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.ColorRgba v -> writeColorRgba w v
                    | Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringListValue v -> writeStringListValue w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : VizAttributeValue =
                    match kvPair.Key with
                    | "boolValue" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.BoolValue (BoolValue.ReadJsonField kvPair.Value) }
                    | "intValue" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.IntValue (IntValue.ReadJsonField kvPair.Value) }
                    | "floatValue" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.FloatValue (FloatValue.ReadJsonField kvPair.Value) }
                    | "stringValue" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringValue (StringValue.ReadJsonField kvPair.Value) }
                    | "colorRgba" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.ColorRgba (ColorRgba.ReadJsonField kvPair.Value) }
                    | "stringListValue" -> { value with Value = Fsbar.Hub.Scripting.V1.VizAttributeValue.ValueCase.StringListValue (StringListValue.ReadJsonField kvPair.Value) }
                    | "value" -> { value with Value = Value.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _VizAttributeValue.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._VizAttributeValue.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StringList =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Values: RepeatedBuilder<string> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Values.Add (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StringList = {
            Values = x.Values.Build
            }

type private _StringList = StringList
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StringList = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("values")>] Values: string list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<StringList>> =
        lazy
        // Field Definitions
        let Values = FieldCodec.Repeated ValueCodec.String (1, "values")
        // Proto Definition Implementation
        { // ProtoDef<StringList>
            Name = "StringList"
            Empty = {
                Values = Values.GetDefault()
                }
            Size = fun (m: StringList) ->
                0
                + Values.CalcFieldSize m.Values
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StringList) ->
                Values.WriteField w m.Values
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StringList.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeValues = Values.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StringList) =
                    writeValues w m.Values
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StringList =
                    match kvPair.Key with
                    | "values" -> { value with Values = Values.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StringList.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StringList.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module VizConfigWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Attributes: MapBuilder<string, Fsbar.Hub.Scripting.V1.VizAttributeValue> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Attributes.Add ((ValueCodec.MapRecord ValueCodec.String ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue>).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.VizConfigWire = {
            Attributes = x.Attributes.Build
            }

type private _VizConfigWire = VizConfigWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type VizConfigWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("attributes")>] Attributes: Map<string, Fsbar.Hub.Scripting.V1.VizAttributeValue> // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<VizConfigWire>> =
        lazy
        // Field Definitions
        let Attributes = FieldCodec.Map ValueCodec.String ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue> (1, "attributes")
        // Proto Definition Implementation
        { // ProtoDef<VizConfigWire>
            Name = "VizConfigWire"
            Empty = {
                Attributes = Attributes.GetDefault()
                }
            Size = fun (m: VizConfigWire) ->
                0
                + Attributes.CalcFieldSize m.Attributes
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: VizConfigWire) ->
                Attributes.WriteField w m.Attributes
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.VizConfigWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeAttributes = Attributes.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: VizConfigWire) =
                    writeAttributes w m.Attributes
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : VizConfigWire =
                    match kvPair.Key with
                    | "attributes" -> { value with Attributes = Attributes.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _VizConfigWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._VizConfigWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetVizConfigRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable VizConfig: OptionBuilder<Fsbar.Hub.Scripting.V1.VizConfigWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.VizConfig.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetVizConfigRequest = {
            VizConfig = x.VizConfig.Build
            }

type private _SetVizConfigRequest = SetVizConfigRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetVizConfigRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("vizConfig")>] VizConfig: Fsbar.Hub.Scripting.V1.VizConfigWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetVizConfigRequest>> =
        lazy
        // Field Definitions
        let VizConfig = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (1, "vizConfig")
        // Proto Definition Implementation
        { // ProtoDef<SetVizConfigRequest>
            Name = "SetVizConfigRequest"
            Empty = {
                VizConfig = VizConfig.GetDefault()
                }
            Size = fun (m: SetVizConfigRequest) ->
                0
                + VizConfig.CalcFieldSize m.VizConfig
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetVizConfigRequest) ->
                VizConfig.WriteField w m.VizConfig
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetVizConfigRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeVizConfig = VizConfig.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetVizConfigRequest) =
                    writeVizConfig w m.VizConfig
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetVizConfigRequest =
                    match kvPair.Key with
                    | "vizConfig" -> { value with VizConfig = VizConfig.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetVizConfigRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetVizConfigRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetVizConfigResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable UnknownKeys: RepeatedBuilder<string> // (2)
            val mutable InvalidValues: RepeatedBuilder<string> // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.UnknownKeys.Add (ValueCodec.String.ReadValue reader)
            | 3 -> x.InvalidValues.Add (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetVizConfigResponse = {
            Result = x.Result.Build
            UnknownKeys = x.UnknownKeys.Build
            InvalidValues = x.InvalidValues.Build
            }

type private _SetVizConfigResponse = SetVizConfigResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetVizConfigResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("unknownKeys")>] UnknownKeys: string list // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("invalidValues")>] InvalidValues: string list // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<SetVizConfigResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let UnknownKeys = FieldCodec.Repeated ValueCodec.String (2, "unknownKeys")
        let InvalidValues = FieldCodec.Repeated ValueCodec.String (3, "invalidValues")
        // Proto Definition Implementation
        { // ProtoDef<SetVizConfigResponse>
            Name = "SetVizConfigResponse"
            Empty = {
                Result = Result.GetDefault()
                UnknownKeys = UnknownKeys.GetDefault()
                InvalidValues = InvalidValues.GetDefault()
                }
            Size = fun (m: SetVizConfigResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + UnknownKeys.CalcFieldSize m.UnknownKeys
                + InvalidValues.CalcFieldSize m.InvalidValues
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetVizConfigResponse) ->
                Result.WriteField w m.Result
                UnknownKeys.WriteField w m.UnknownKeys
                InvalidValues.WriteField w m.InvalidValues
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetVizConfigResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeUnknownKeys = UnknownKeys.WriteJsonField o
                let writeInvalidValues = InvalidValues.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetVizConfigResponse) =
                    writeResult w m.Result
                    writeUnknownKeys w m.UnknownKeys
                    writeInvalidValues w m.InvalidValues
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetVizConfigResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "unknownKeys" -> { value with UnknownKeys = UnknownKeys.ReadJsonField kvPair.Value }
                    | "invalidValues" -> { value with InvalidValues = InvalidValues.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetVizConfigResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetVizConfigResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetVizAttributeRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Key: string // (1)
            val mutable Value: OptionBuilder<Fsbar.Hub.Scripting.V1.VizAttributeValue> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Key <- ValueCodec.String.ReadValue reader
            | 2 -> x.Value.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetVizAttributeRequest = {
            Key = x.Key |> orEmptyString
            Value = x.Value.Build
            }

type private _SetVizAttributeRequest = SetVizAttributeRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetVizAttributeRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("key")>] Key: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("value")>] Value: Fsbar.Hub.Scripting.V1.VizAttributeValue option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SetVizAttributeRequest>> =
        lazy
        // Field Definitions
        let Key = FieldCodec.Primitive ValueCodec.String (1, "key")
        let Value = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue> (2, "value")
        // Proto Definition Implementation
        { // ProtoDef<SetVizAttributeRequest>
            Name = "SetVizAttributeRequest"
            Empty = {
                Key = Key.GetDefault()
                Value = Value.GetDefault()
                }
            Size = fun (m: SetVizAttributeRequest) ->
                0
                + Key.CalcFieldSize m.Key
                + Value.CalcFieldSize m.Value
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetVizAttributeRequest) ->
                Key.WriteField w m.Key
                Value.WriteField w m.Value
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetVizAttributeRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeKey = Key.WriteJsonField o
                let writeValue = Value.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetVizAttributeRequest) =
                    writeKey w m.Key
                    writeValue w m.Value
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetVizAttributeRequest =
                    match kvPair.Key with
                    | "key" -> { value with Key = Key.ReadJsonField kvPair.Value }
                    | "value" -> { value with Value = Value.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetVizAttributeRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetVizAttributeRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetVizAttributeResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetVizAttributeResponse = {
            Result = x.Result.Build
            }

type private _SetVizAttributeResponse = SetVizAttributeResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetVizAttributeResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetVizAttributeResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SetVizAttributeResponse>
            Name = "SetVizAttributeResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SetVizAttributeResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetVizAttributeResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetVizAttributeResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetVizAttributeResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetVizAttributeResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetVizAttributeResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetVizAttributeResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ToggleOverlayRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Overlay: Fsbar.Hub.Scripting.V1.OverlayKey // (1)
            val mutable Target: Fsbar.Hub.Scripting.V1.OverlayTargetState // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Overlay <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.OverlayKey>.ReadValue reader
            | 2 -> x.Target <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.OverlayTargetState>.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ToggleOverlayRequest = {
            Overlay = x.Overlay
            Target = x.Target
            }

type private _ToggleOverlayRequest = ToggleOverlayRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ToggleOverlayRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("overlay")>] Overlay: Fsbar.Hub.Scripting.V1.OverlayKey // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("target")>] Target: Fsbar.Hub.Scripting.V1.OverlayTargetState // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<ToggleOverlayRequest>> =
        lazy
        // Field Definitions
        let Overlay = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.OverlayKey> (1, "overlay")
        let Target = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.OverlayTargetState> (2, "target")
        // Proto Definition Implementation
        { // ProtoDef<ToggleOverlayRequest>
            Name = "ToggleOverlayRequest"
            Empty = {
                Overlay = Overlay.GetDefault()
                Target = Target.GetDefault()
                }
            Size = fun (m: ToggleOverlayRequest) ->
                0
                + Overlay.CalcFieldSize m.Overlay
                + Target.CalcFieldSize m.Target
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ToggleOverlayRequest) ->
                Overlay.WriteField w m.Overlay
                Target.WriteField w m.Target
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ToggleOverlayRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeOverlay = Overlay.WriteJsonField o
                let writeTarget = Target.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ToggleOverlayRequest) =
                    writeOverlay w m.Overlay
                    writeTarget w m.Target
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ToggleOverlayRequest =
                    match kvPair.Key with
                    | "overlay" -> { value with Overlay = Overlay.ReadJsonField kvPair.Value }
                    | "target" -> { value with Target = Target.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ToggleOverlayRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ToggleOverlayRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ToggleOverlayResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable NewState: bool // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.NewState <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ToggleOverlayResponse = {
            Result = x.Result.Build
            NewState = x.NewState
            }

type private _ToggleOverlayResponse = ToggleOverlayResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ToggleOverlayResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("newState")>] NewState: bool // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<ToggleOverlayResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let NewState = FieldCodec.Primitive ValueCodec.Bool (2, "newState")
        // Proto Definition Implementation
        { // ProtoDef<ToggleOverlayResponse>
            Name = "ToggleOverlayResponse"
            Empty = {
                Result = Result.GetDefault()
                NewState = NewState.GetDefault()
                }
            Size = fun (m: ToggleOverlayResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + NewState.CalcFieldSize m.NewState
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ToggleOverlayResponse) ->
                Result.WriteField w m.Result
                NewState.WriteField w m.NewState
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ToggleOverlayResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeNewState = NewState.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ToggleOverlayResponse) =
                    writeResult w m.Result
                    writeNewState w m.NewState
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ToggleOverlayResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "newState" -> { value with NewState = NewState.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ToggleOverlayResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ToggleOverlayResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetCameraRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Camera: OptionBuilder<Fsbar.Hub.Scripting.V1.ViewerCameraWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Camera.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetCameraRequest = {
            Camera = x.Camera.Build
            }

type private _SetCameraRequest = SetCameraRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetCameraRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("camera")>] Camera: Fsbar.Hub.Scripting.V1.ViewerCameraWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetCameraRequest>> =
        lazy
        // Field Definitions
        let Camera = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire> (1, "camera")
        // Proto Definition Implementation
        { // ProtoDef<SetCameraRequest>
            Name = "SetCameraRequest"
            Empty = {
                Camera = Camera.GetDefault()
                }
            Size = fun (m: SetCameraRequest) ->
                0
                + Camera.CalcFieldSize m.Camera
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetCameraRequest) ->
                Camera.WriteField w m.Camera
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetCameraRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCamera = Camera.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetCameraRequest) =
                    writeCamera w m.Camera
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetCameraRequest =
                    match kvPair.Key with
                    | "camera" -> { value with Camera = Camera.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetCameraRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetCameraRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ViewerCameraWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Scale: float32 // (1)
            val mutable OriginX: float32 // (2)
            val mutable OriginY: float32 // (3)
            val mutable AutoFit: bool // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Scale <- ValueCodec.Float.ReadValue reader
            | 2 -> x.OriginX <- ValueCodec.Float.ReadValue reader
            | 3 -> x.OriginY <- ValueCodec.Float.ReadValue reader
            | 4 -> x.AutoFit <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ViewerCameraWire = {
            Scale = x.Scale
            OriginX = x.OriginX
            OriginY = x.OriginY
            AutoFit = x.AutoFit
            }

type private _ViewerCameraWire = ViewerCameraWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ViewerCameraWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("scale")>] Scale: float32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("originX")>] OriginX: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("originY")>] OriginY: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("autoFit")>] AutoFit: bool // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<ViewerCameraWire>> =
        lazy
        // Field Definitions
        let Scale = FieldCodec.Primitive ValueCodec.Float (1, "scale")
        let OriginX = FieldCodec.Primitive ValueCodec.Float (2, "originX")
        let OriginY = FieldCodec.Primitive ValueCodec.Float (3, "originY")
        let AutoFit = FieldCodec.Primitive ValueCodec.Bool (4, "autoFit")
        // Proto Definition Implementation
        { // ProtoDef<ViewerCameraWire>
            Name = "ViewerCameraWire"
            Empty = {
                Scale = Scale.GetDefault()
                OriginX = OriginX.GetDefault()
                OriginY = OriginY.GetDefault()
                AutoFit = AutoFit.GetDefault()
                }
            Size = fun (m: ViewerCameraWire) ->
                0
                + Scale.CalcFieldSize m.Scale
                + OriginX.CalcFieldSize m.OriginX
                + OriginY.CalcFieldSize m.OriginY
                + AutoFit.CalcFieldSize m.AutoFit
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ViewerCameraWire) ->
                Scale.WriteField w m.Scale
                OriginX.WriteField w m.OriginX
                OriginY.WriteField w m.OriginY
                AutoFit.WriteField w m.AutoFit
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ViewerCameraWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeScale = Scale.WriteJsonField o
                let writeOriginX = OriginX.WriteJsonField o
                let writeOriginY = OriginY.WriteJsonField o
                let writeAutoFit = AutoFit.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ViewerCameraWire) =
                    writeScale w m.Scale
                    writeOriginX w m.OriginX
                    writeOriginY w m.OriginY
                    writeAutoFit w m.AutoFit
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ViewerCameraWire =
                    match kvPair.Key with
                    | "scale" -> { value with Scale = Scale.ReadJsonField kvPair.Value }
                    | "originX" -> { value with OriginX = OriginX.ReadJsonField kvPair.Value }
                    | "originY" -> { value with OriginY = OriginY.ReadJsonField kvPair.Value }
                    | "autoFit" -> { value with AutoFit = AutoFit.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ViewerCameraWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ViewerCameraWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetCameraResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetCameraResponse = {
            Result = x.Result.Build
            }

type private _SetCameraResponse = SetCameraResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetCameraResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetCameraResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SetCameraResponse>
            Name = "SetCameraResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SetCameraResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetCameraResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetCameraResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetCameraResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetCameraResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetCameraResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetCameraResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetActiveTabRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Tab: Fsbar.Hub.Scripting.V1.HubTab // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Tab <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab>.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetActiveTabRequest = {
            Tab = x.Tab
            }

type private _SetActiveTabRequest = SetActiveTabRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetActiveTabRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("tab")>] Tab: Fsbar.Hub.Scripting.V1.HubTab // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetActiveTabRequest>> =
        lazy
        // Field Definitions
        let Tab = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab> (1, "tab")
        // Proto Definition Implementation
        { // ProtoDef<SetActiveTabRequest>
            Name = "SetActiveTabRequest"
            Empty = {
                Tab = Tab.GetDefault()
                }
            Size = fun (m: SetActiveTabRequest) ->
                0
                + Tab.CalcFieldSize m.Tab
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetActiveTabRequest) ->
                Tab.WriteField w m.Tab
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetActiveTabRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeTab = Tab.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetActiveTabRequest) =
                    writeTab w m.Tab
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetActiveTabRequest =
                    match kvPair.Key with
                    | "tab" -> { value with Tab = Tab.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetActiveTabRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetActiveTabRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetActiveTabResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetActiveTabResponse = {
            Result = x.Result.Build
            }

type private _SetActiveTabResponse = SetActiveTabResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetActiveTabResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetActiveTabResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SetActiveTabResponse>
            Name = "SetActiveTabResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SetActiveTabResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetActiveTabResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetActiveTabResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetActiveTabResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetActiveTabResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetActiveTabResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetActiveTabResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListPresetsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ListPresetsRequest.empty

[<StructuralEquality;StructuralComparison>]
type ListPresetsRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ListPresetsRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ListPresetsRequest>
            Name = "ListPresetsRequest"
            Empty = ListPresetsRequest.empty
            Size = fun (m: ListPresetsRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListPresetsRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ListPresetsRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ListPresetsRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListPresetsResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Presets: RepeatedBuilder<Fsbar.Hub.Scripting.V1.PresetDescriptor> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Presets.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetDescriptor>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ListPresetsResponse = {
            Presets = x.Presets.Build
            }

type private _ListPresetsResponse = ListPresetsResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ListPresetsResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("presets")>] Presets: Fsbar.Hub.Scripting.V1.PresetDescriptor list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ListPresetsResponse>> =
        lazy
        // Field Definitions
        let Presets = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetDescriptor> (1, "presets")
        // Proto Definition Implementation
        { // ProtoDef<ListPresetsResponse>
            Name = "ListPresetsResponse"
            Empty = {
                Presets = Presets.GetDefault()
                }
            Size = fun (m: ListPresetsResponse) ->
                0
                + Presets.CalcFieldSize m.Presets
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListPresetsResponse) ->
                Presets.WriteField w m.Presets
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ListPresetsResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePresets = Presets.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ListPresetsResponse) =
                    writePresets w m.Presets
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ListPresetsResponse =
                    match kvPair.Key with
                    | "presets" -> { value with Presets = Presets.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ListPresetsResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ListPresetsResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PresetDescriptor =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
            val mutable ModifiedAtUnixMs: int64 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | 2 -> x.ModifiedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PresetDescriptor = {
            Name = x.Name |> orEmptyString
            ModifiedAtUnixMs = x.ModifiedAtUnixMs
            }

type private _PresetDescriptor = PresetDescriptor
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PresetDescriptor = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("modifiedAtUnixMs")>] ModifiedAtUnixMs: int64 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<PresetDescriptor>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        let ModifiedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (2, "modifiedAtUnixMs")
        // Proto Definition Implementation
        { // ProtoDef<PresetDescriptor>
            Name = "PresetDescriptor"
            Empty = {
                Name = Name.GetDefault()
                ModifiedAtUnixMs = ModifiedAtUnixMs.GetDefault()
                }
            Size = fun (m: PresetDescriptor) ->
                0
                + Name.CalcFieldSize m.Name
                + ModifiedAtUnixMs.CalcFieldSize m.ModifiedAtUnixMs
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PresetDescriptor) ->
                Name.WriteField w m.Name
                ModifiedAtUnixMs.WriteField w m.ModifiedAtUnixMs
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PresetDescriptor.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let writeModifiedAtUnixMs = ModifiedAtUnixMs.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PresetDescriptor) =
                    writeName w m.Name
                    writeModifiedAtUnixMs w m.ModifiedAtUnixMs
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PresetDescriptor =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | "modifiedAtUnixMs" -> { value with ModifiedAtUnixMs = ModifiedAtUnixMs.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PresetDescriptor.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PresetDescriptor.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SavePresetRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
            val mutable VizConfig: OptionBuilder<Fsbar.Hub.Scripting.V1.VizConfigWire> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | 2 -> x.VizConfig.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SavePresetRequest = {
            Name = x.Name |> orEmptyString
            VizConfig = x.VizConfig.Build
            }

type private _SavePresetRequest = SavePresetRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SavePresetRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("vizConfig")>] VizConfig: Fsbar.Hub.Scripting.V1.VizConfigWire option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SavePresetRequest>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        let VizConfig = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (2, "vizConfig")
        // Proto Definition Implementation
        { // ProtoDef<SavePresetRequest>
            Name = "SavePresetRequest"
            Empty = {
                Name = Name.GetDefault()
                VizConfig = VizConfig.GetDefault()
                }
            Size = fun (m: SavePresetRequest) ->
                0
                + Name.CalcFieldSize m.Name
                + VizConfig.CalcFieldSize m.VizConfig
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SavePresetRequest) ->
                Name.WriteField w m.Name
                VizConfig.WriteField w m.VizConfig
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SavePresetRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let writeVizConfig = VizConfig.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SavePresetRequest) =
                    writeName w m.Name
                    writeVizConfig w m.VizConfig
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SavePresetRequest =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | "vizConfig" -> { value with VizConfig = VizConfig.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SavePresetRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SavePresetRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SavePresetResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SavePresetResponse = {
            Result = x.Result.Build
            }

type private _SavePresetResponse = SavePresetResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SavePresetResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SavePresetResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SavePresetResponse>
            Name = "SavePresetResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SavePresetResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SavePresetResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SavePresetResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SavePresetResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SavePresetResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SavePresetResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SavePresetResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadPresetRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LoadPresetRequest = {
            Name = x.Name |> orEmptyString
            }

type private _LoadPresetRequest = LoadPresetRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadPresetRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadPresetRequest>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        // Proto Definition Implementation
        { // ProtoDef<LoadPresetRequest>
            Name = "LoadPresetRequest"
            Empty = {
                Name = Name.GetDefault()
                }
            Size = fun (m: LoadPresetRequest) ->
                0
                + Name.CalcFieldSize m.Name
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadPresetRequest) ->
                Name.WriteField w m.Name
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LoadPresetRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadPresetRequest) =
                    writeName w m.Name
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadPresetRequest =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadPresetRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LoadPresetRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadPresetResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable VizConfig: OptionBuilder<Fsbar.Hub.Scripting.V1.VizConfigWire> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.VizConfig.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LoadPresetResponse = {
            Result = x.Result.Build
            VizConfig = x.VizConfig.Build
            }

type private _LoadPresetResponse = LoadPresetResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LoadPresetResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("vizConfig")>] VizConfig: Fsbar.Hub.Scripting.V1.VizConfigWire option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<LoadPresetResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let VizConfig = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (2, "vizConfig")
        // Proto Definition Implementation
        { // ProtoDef<LoadPresetResponse>
            Name = "LoadPresetResponse"
            Empty = {
                Result = Result.GetDefault()
                VizConfig = VizConfig.GetDefault()
                }
            Size = fun (m: LoadPresetResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + VizConfig.CalcFieldSize m.VizConfig
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LoadPresetResponse) ->
                Result.WriteField w m.Result
                VizConfig.WriteField w m.VizConfig
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LoadPresetResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeVizConfig = VizConfig.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LoadPresetResponse) =
                    writeResult w m.Result
                    writeVizConfig w m.VizConfig
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LoadPresetResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "vizConfig" -> { value with VizConfig = VizConfig.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LoadPresetResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LoadPresetResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DeletePresetRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.DeletePresetRequest = {
            Name = x.Name |> orEmptyString
            }

type private _DeletePresetRequest = DeletePresetRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DeletePresetRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<DeletePresetRequest>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        // Proto Definition Implementation
        { // ProtoDef<DeletePresetRequest>
            Name = "DeletePresetRequest"
            Empty = {
                Name = Name.GetDefault()
                }
            Size = fun (m: DeletePresetRequest) ->
                0
                + Name.CalcFieldSize m.Name
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DeletePresetRequest) ->
                Name.WriteField w m.Name
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.DeletePresetRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DeletePresetRequest) =
                    writeName w m.Name
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DeletePresetRequest =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DeletePresetRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._DeletePresetRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DeletePresetResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.DeletePresetResponse = {
            Result = x.Result.Build
            }

type private _DeletePresetResponse = DeletePresetResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DeletePresetResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<DeletePresetResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<DeletePresetResponse>
            Name = "DeletePresetResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: DeletePresetResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DeletePresetResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.DeletePresetResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DeletePresetResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DeletePresetResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DeletePresetResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._DeletePresetResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListUnitsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FactionFilter: RepeatedBuilder<string> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FactionFilter.Add (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ListUnitsRequest = {
            FactionFilter = x.FactionFilter.Build
            }

type private _ListUnitsRequest = ListUnitsRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ListUnitsRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("factionFilter")>] FactionFilter: string list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ListUnitsRequest>> =
        lazy
        // Field Definitions
        let FactionFilter = FieldCodec.Repeated ValueCodec.String (1, "factionFilter")
        // Proto Definition Implementation
        { // ProtoDef<ListUnitsRequest>
            Name = "ListUnitsRequest"
            Empty = {
                FactionFilter = FactionFilter.GetDefault()
                }
            Size = fun (m: ListUnitsRequest) ->
                0
                + FactionFilter.CalcFieldSize m.FactionFilter
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListUnitsRequest) ->
                FactionFilter.WriteField w m.FactionFilter
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ListUnitsRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFactionFilter = FactionFilter.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ListUnitsRequest) =
                    writeFactionFilter w m.FactionFilter
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ListUnitsRequest =
                    match kvPair.Key with
                    | "factionFilter" -> { value with FactionFilter = FactionFilter.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ListUnitsRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ListUnitsRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListUnitsResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Entries: RepeatedBuilder<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Entries.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ListUnitsResponse = {
            Entries = x.Entries.Build
            }

type private _ListUnitsResponse = ListUnitsResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ListUnitsResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("entries")>] Entries: Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ListUnitsResponse>> =
        lazy
        // Field Definitions
        let Entries = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire> (1, "entries")
        // Proto Definition Implementation
        { // ProtoDef<ListUnitsResponse>
            Name = "ListUnitsResponse"
            Empty = {
                Entries = Entries.GetDefault()
                }
            Size = fun (m: ListUnitsResponse) ->
                0
                + Entries.CalcFieldSize m.Entries
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListUnitsResponse) ->
                Entries.WriteField w m.Entries
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ListUnitsResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeEntries = Entries.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ListUnitsResponse) =
                    writeEntries w m.Entries
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ListUnitsResponse =
                    match kvPair.Key with
                    | "entries" -> { value with Entries = Entries.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ListUnitsResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ListUnitsResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EncyclopediaEntryWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable DefId: int // (1)
            val mutable InternalName: string // (2)
            val mutable Subfolder: string // (3)
            val mutable Faction: string // (4)
            val mutable Tier: string // (5)
            val mutable Shape: string // (6)
            val mutable MetalCost: int // (7)
            val mutable EnergyCost: int // (8)
            val mutable BuildTime: int // (9)
            val mutable MaxHealth: int // (10)
            val mutable FootprintX: int // (11)
            val mutable FootprintZ: int // (12)
            val mutable SightRangeElmo: float32 // (13)
            val mutable WeaponRangesElmo: RepeatedBuilder<float32> // (14)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.DefId <- ValueCodec.Int32.ReadValue reader
            | 2 -> x.InternalName <- ValueCodec.String.ReadValue reader
            | 3 -> x.Subfolder <- ValueCodec.String.ReadValue reader
            | 4 -> x.Faction <- ValueCodec.String.ReadValue reader
            | 5 -> x.Tier <- ValueCodec.String.ReadValue reader
            | 6 -> x.Shape <- ValueCodec.String.ReadValue reader
            | 7 -> x.MetalCost <- ValueCodec.Int32.ReadValue reader
            | 8 -> x.EnergyCost <- ValueCodec.Int32.ReadValue reader
            | 9 -> x.BuildTime <- ValueCodec.Int32.ReadValue reader
            | 10 -> x.MaxHealth <- ValueCodec.Int32.ReadValue reader
            | 11 -> x.FootprintX <- ValueCodec.Int32.ReadValue reader
            | 12 -> x.FootprintZ <- ValueCodec.Int32.ReadValue reader
            | 13 -> x.SightRangeElmo <- ValueCodec.Float.ReadValue reader
            | 14 -> x.WeaponRangesElmo.AddRange ((ValueCodec.Packed ValueCodec.Float).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire = {
            DefId = x.DefId
            InternalName = x.InternalName |> orEmptyString
            Subfolder = x.Subfolder |> orEmptyString
            Faction = x.Faction |> orEmptyString
            Tier = x.Tier |> orEmptyString
            Shape = x.Shape |> orEmptyString
            MetalCost = x.MetalCost
            EnergyCost = x.EnergyCost
            BuildTime = x.BuildTime
            MaxHealth = x.MaxHealth
            FootprintX = x.FootprintX
            FootprintZ = x.FootprintZ
            SightRangeElmo = x.SightRangeElmo
            WeaponRangesElmo = x.WeaponRangesElmo.Build
            }

type private _EncyclopediaEntryWire = EncyclopediaEntryWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EncyclopediaEntryWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("defId")>] DefId: int // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("internalName")>] InternalName: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("subfolder")>] Subfolder: string // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("faction")>] Faction: string // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("tier")>] Tier: string // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("shape")>] Shape: string // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("metalCost")>] MetalCost: int // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("energyCost")>] EnergyCost: int // (8)
    [<System.Text.Json.Serialization.JsonPropertyName("buildTime")>] BuildTime: int // (9)
    [<System.Text.Json.Serialization.JsonPropertyName("maxHealth")>] MaxHealth: int // (10)
    [<System.Text.Json.Serialization.JsonPropertyName("footprintX")>] FootprintX: int // (11)
    [<System.Text.Json.Serialization.JsonPropertyName("footprintZ")>] FootprintZ: int // (12)
    [<System.Text.Json.Serialization.JsonPropertyName("sightRangeElmo")>] SightRangeElmo: float32 // (13)
    [<System.Text.Json.Serialization.JsonPropertyName("weaponRangesElmo")>] WeaponRangesElmo: float32 list // (14)
    }
    with
    static member Proto : Lazy<ProtoDef<EncyclopediaEntryWire>> =
        lazy
        // Field Definitions
        let DefId = FieldCodec.Primitive ValueCodec.Int32 (1, "defId")
        let InternalName = FieldCodec.Primitive ValueCodec.String (2, "internalName")
        let Subfolder = FieldCodec.Primitive ValueCodec.String (3, "subfolder")
        let Faction = FieldCodec.Primitive ValueCodec.String (4, "faction")
        let Tier = FieldCodec.Primitive ValueCodec.String (5, "tier")
        let Shape = FieldCodec.Primitive ValueCodec.String (6, "shape")
        let MetalCost = FieldCodec.Primitive ValueCodec.Int32 (7, "metalCost")
        let EnergyCost = FieldCodec.Primitive ValueCodec.Int32 (8, "energyCost")
        let BuildTime = FieldCodec.Primitive ValueCodec.Int32 (9, "buildTime")
        let MaxHealth = FieldCodec.Primitive ValueCodec.Int32 (10, "maxHealth")
        let FootprintX = FieldCodec.Primitive ValueCodec.Int32 (11, "footprintX")
        let FootprintZ = FieldCodec.Primitive ValueCodec.Int32 (12, "footprintZ")
        let SightRangeElmo = FieldCodec.Primitive ValueCodec.Float (13, "sightRangeElmo")
        let WeaponRangesElmo = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Float) (14, "weaponRangesElmo")
        // Proto Definition Implementation
        { // ProtoDef<EncyclopediaEntryWire>
            Name = "EncyclopediaEntryWire"
            Empty = {
                DefId = DefId.GetDefault()
                InternalName = InternalName.GetDefault()
                Subfolder = Subfolder.GetDefault()
                Faction = Faction.GetDefault()
                Tier = Tier.GetDefault()
                Shape = Shape.GetDefault()
                MetalCost = MetalCost.GetDefault()
                EnergyCost = EnergyCost.GetDefault()
                BuildTime = BuildTime.GetDefault()
                MaxHealth = MaxHealth.GetDefault()
                FootprintX = FootprintX.GetDefault()
                FootprintZ = FootprintZ.GetDefault()
                SightRangeElmo = SightRangeElmo.GetDefault()
                WeaponRangesElmo = WeaponRangesElmo.GetDefault()
                }
            Size = fun (m: EncyclopediaEntryWire) ->
                0
                + DefId.CalcFieldSize m.DefId
                + InternalName.CalcFieldSize m.InternalName
                + Subfolder.CalcFieldSize m.Subfolder
                + Faction.CalcFieldSize m.Faction
                + Tier.CalcFieldSize m.Tier
                + Shape.CalcFieldSize m.Shape
                + MetalCost.CalcFieldSize m.MetalCost
                + EnergyCost.CalcFieldSize m.EnergyCost
                + BuildTime.CalcFieldSize m.BuildTime
                + MaxHealth.CalcFieldSize m.MaxHealth
                + FootprintX.CalcFieldSize m.FootprintX
                + FootprintZ.CalcFieldSize m.FootprintZ
                + SightRangeElmo.CalcFieldSize m.SightRangeElmo
                + WeaponRangesElmo.CalcFieldSize m.WeaponRangesElmo
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EncyclopediaEntryWire) ->
                DefId.WriteField w m.DefId
                InternalName.WriteField w m.InternalName
                Subfolder.WriteField w m.Subfolder
                Faction.WriteField w m.Faction
                Tier.WriteField w m.Tier
                Shape.WriteField w m.Shape
                MetalCost.WriteField w m.MetalCost
                EnergyCost.WriteField w m.EnergyCost
                BuildTime.WriteField w m.BuildTime
                MaxHealth.WriteField w m.MaxHealth
                FootprintX.WriteField w m.FootprintX
                FootprintZ.WriteField w m.FootprintZ
                SightRangeElmo.WriteField w m.SightRangeElmo
                WeaponRangesElmo.WriteField w m.WeaponRangesElmo
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeDefId = DefId.WriteJsonField o
                let writeInternalName = InternalName.WriteJsonField o
                let writeSubfolder = Subfolder.WriteJsonField o
                let writeFaction = Faction.WriteJsonField o
                let writeTier = Tier.WriteJsonField o
                let writeShape = Shape.WriteJsonField o
                let writeMetalCost = MetalCost.WriteJsonField o
                let writeEnergyCost = EnergyCost.WriteJsonField o
                let writeBuildTime = BuildTime.WriteJsonField o
                let writeMaxHealth = MaxHealth.WriteJsonField o
                let writeFootprintX = FootprintX.WriteJsonField o
                let writeFootprintZ = FootprintZ.WriteJsonField o
                let writeSightRangeElmo = SightRangeElmo.WriteJsonField o
                let writeWeaponRangesElmo = WeaponRangesElmo.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EncyclopediaEntryWire) =
                    writeDefId w m.DefId
                    writeInternalName w m.InternalName
                    writeSubfolder w m.Subfolder
                    writeFaction w m.Faction
                    writeTier w m.Tier
                    writeShape w m.Shape
                    writeMetalCost w m.MetalCost
                    writeEnergyCost w m.EnergyCost
                    writeBuildTime w m.BuildTime
                    writeMaxHealth w m.MaxHealth
                    writeFootprintX w m.FootprintX
                    writeFootprintZ w m.FootprintZ
                    writeSightRangeElmo w m.SightRangeElmo
                    writeWeaponRangesElmo w m.WeaponRangesElmo
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EncyclopediaEntryWire =
                    match kvPair.Key with
                    | "defId" -> { value with DefId = DefId.ReadJsonField kvPair.Value }
                    | "internalName" -> { value with InternalName = InternalName.ReadJsonField kvPair.Value }
                    | "subfolder" -> { value with Subfolder = Subfolder.ReadJsonField kvPair.Value }
                    | "faction" -> { value with Faction = Faction.ReadJsonField kvPair.Value }
                    | "tier" -> { value with Tier = Tier.ReadJsonField kvPair.Value }
                    | "shape" -> { value with Shape = Shape.ReadJsonField kvPair.Value }
                    | "metalCost" -> { value with MetalCost = MetalCost.ReadJsonField kvPair.Value }
                    | "energyCost" -> { value with EnergyCost = EnergyCost.ReadJsonField kvPair.Value }
                    | "buildTime" -> { value with BuildTime = BuildTime.ReadJsonField kvPair.Value }
                    | "maxHealth" -> { value with MaxHealth = MaxHealth.ReadJsonField kvPair.Value }
                    | "footprintX" -> { value with FootprintX = FootprintX.ReadJsonField kvPair.Value }
                    | "footprintZ" -> { value with FootprintZ = FootprintZ.ReadJsonField kvPair.Value }
                    | "sightRangeElmo" -> { value with SightRangeElmo = SightRangeElmo.ReadJsonField kvPair.Value }
                    | "weaponRangesElmo" -> { value with WeaponRangesElmo = WeaponRangesElmo.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EncyclopediaEntryWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._EncyclopediaEntryWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SelectUnitRequest =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<SelectorCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type SelectorCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("defId")>] DefId of int
    | [<System.Text.Json.Serialization.JsonPropertyName("internalName")>] InternalName of string
    with
        static member OneofCodec : Lazy<OneofCodec<SelectorCase>> = 
            lazy
            let DefId = FieldCodec.OneofCase "selector" ValueCodec.Int32 (1, "defId")
            let InternalName = FieldCodec.OneofCase "selector" ValueCodec.String (2, "internalName")
            let Selector = FieldCodec.Oneof "selector" (FSharp.Collections.Map [
                ("defId", fun node -> SelectorCase.DefId (DefId.ReadJsonField node))
                ("internalName", fun node -> SelectorCase.InternalName (InternalName.ReadJsonField node))
                ])
            Selector

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Selector: OptionBuilder<Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Selector.Set (SelectorCase.DefId (ValueCodec.Int32.ReadValue reader))
            | 2 -> x.Selector.Set (SelectorCase.InternalName (ValueCodec.String.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SelectUnitRequest = {
            Selector = x.Selector.Build |> (Option.defaultValue SelectorCase.None)
            }

type private _SelectUnitRequest = SelectUnitRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SelectUnitRequest = {
    // Field Declarations
    Selector: Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase
    }
    with
    static member Proto : Lazy<ProtoDef<SelectUnitRequest>> =
        lazy
        // Field Definitions
        let DefId = FieldCodec.OneofCase "selector" ValueCodec.Int32 (1, "defId")
        let InternalName = FieldCodec.OneofCase "selector" ValueCodec.String (2, "internalName")
        let Selector = FieldCodec.Oneof "selector" (FSharp.Collections.Map [
            ("defId", fun node -> Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.DefId (DefId.ReadJsonField node))
            ("internalName", fun node -> Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.InternalName (InternalName.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<SelectUnitRequest>
            Name = "SelectUnitRequest"
            Empty = {
                Selector = Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.None
                }
            Size = fun (m: SelectUnitRequest) ->
                0
                + match m.Selector with
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.DefId v -> DefId.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.InternalName v -> InternalName.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SelectUnitRequest) ->
                (match m.Selector with
                | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.None -> ()
                | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.DefId v -> DefId.WriteField w v
                | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.InternalName v -> InternalName.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SelectUnitRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSelectorNone = Selector.WriteJsonNoneCase o
                let writeDefId = DefId.WriteJsonField o
                let writeInternalName = InternalName.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SelectUnitRequest) =
                    (match m.Selector with
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.None -> writeSelectorNone w
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.DefId v -> writeDefId w v
                    | Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.InternalName v -> writeInternalName w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SelectUnitRequest =
                    match kvPair.Key with
                    | "defId" -> { value with Selector = Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.DefId (DefId.ReadJsonField kvPair.Value) }
                    | "internalName" -> { value with Selector = Fsbar.Hub.Scripting.V1.SelectUnitRequest.SelectorCase.InternalName (InternalName.ReadJsonField kvPair.Value) }
                    | "selector" -> { value with Selector = Selector.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SelectUnitRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SelectUnitRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SelectUnitResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable Entry: OptionBuilder<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.Entry.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SelectUnitResponse = {
            Result = x.Result.Build
            Entry = x.Entry.Build
            }

type private _SelectUnitResponse = SelectUnitResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SelectUnitResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("entry")>] Entry: Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<SelectUnitResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let Entry = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaEntryWire> (2, "entry")
        // Proto Definition Implementation
        { // ProtoDef<SelectUnitResponse>
            Name = "SelectUnitResponse"
            Empty = {
                Result = Result.GetDefault()
                Entry = Entry.GetDefault()
                }
            Size = fun (m: SelectUnitResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + Entry.CalcFieldSize m.Entry
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SelectUnitResponse) ->
                Result.WriteField w m.Result
                Entry.WriteField w m.Entry
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SelectUnitResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeEntry = Entry.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SelectUnitResponse) =
                    writeResult w m.Result
                    writeEntry w m.Entry
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SelectUnitResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "entry" -> { value with Entry = Entry.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SelectUnitResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SelectUnitResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetHubSettingsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = GetHubSettingsRequest.empty

[<StructuralEquality;StructuralComparison>]
type GetHubSettingsRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<GetHubSettingsRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<GetHubSettingsRequest>
            Name = "GetHubSettingsRequest"
            Empty = GetHubSettingsRequest.empty
            Size = fun (m: GetHubSettingsRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetHubSettingsRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                GetHubSettingsRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> GetHubSettingsRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetHubSettingsResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Settings: OptionBuilder<Fsbar.Hub.Scripting.V1.HubSettingsWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Settings.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.GetHubSettingsResponse = {
            Settings = x.Settings.Build
            }

type private _GetHubSettingsResponse = GetHubSettingsResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type GetHubSettingsResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("settings")>] Settings: Fsbar.Hub.Scripting.V1.HubSettingsWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<GetHubSettingsResponse>> =
        lazy
        // Field Definitions
        let Settings = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire> (1, "settings")
        // Proto Definition Implementation
        { // ProtoDef<GetHubSettingsResponse>
            Name = "GetHubSettingsResponse"
            Empty = {
                Settings = Settings.GetDefault()
                }
            Size = fun (m: GetHubSettingsResponse) ->
                0
                + Settings.CalcFieldSize m.Settings
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetHubSettingsResponse) ->
                Settings.WriteField w m.Settings
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.GetHubSettingsResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSettings = Settings.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: GetHubSettingsResponse) =
                    writeSettings w m.Settings
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : GetHubSettingsResponse =
                    match kvPair.Key with
                    | "settings" -> { value with Settings = Settings.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _GetHubSettingsResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._GetHubSettingsResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module HubSettingsWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable BarDataDirOverride: string // (1)
            val mutable EngineVersionOverride: string // (2)
            val mutable GrpcPort: int // (3)
            val mutable LaunchGraphicalViewerDefault: bool // (4)
            val mutable StartPausedDefault: bool // (5)
            val mutable MaxRenderFrameSubscribers: int // (6)
            val mutable SchemaVersion: int // (7)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.BarDataDirOverride <- ValueCodec.String.ReadValue reader
            | 2 -> x.EngineVersionOverride <- ValueCodec.String.ReadValue reader
            | 3 -> x.GrpcPort <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.LaunchGraphicalViewerDefault <- ValueCodec.Bool.ReadValue reader
            | 5 -> x.StartPausedDefault <- ValueCodec.Bool.ReadValue reader
            | 6 -> x.MaxRenderFrameSubscribers <- ValueCodec.Int32.ReadValue reader
            | 7 -> x.SchemaVersion <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.HubSettingsWire = {
            BarDataDirOverride = x.BarDataDirOverride |> orEmptyString
            EngineVersionOverride = x.EngineVersionOverride |> orEmptyString
            GrpcPort = x.GrpcPort
            LaunchGraphicalViewerDefault = x.LaunchGraphicalViewerDefault
            StartPausedDefault = x.StartPausedDefault
            MaxRenderFrameSubscribers = x.MaxRenderFrameSubscribers
            SchemaVersion = x.SchemaVersion
            }

type private _HubSettingsWire = HubSettingsWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type HubSettingsWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("barDataDirOverride")>] BarDataDirOverride: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("engineVersionOverride")>] EngineVersionOverride: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("grpcPort")>] GrpcPort: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("launchGraphicalViewerDefault")>] LaunchGraphicalViewerDefault: bool // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("startPausedDefault")>] StartPausedDefault: bool // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("maxRenderFrameSubscribers")>] MaxRenderFrameSubscribers: int // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("schemaVersion")>] SchemaVersion: int // (7)
    }
    with
    static member Proto : Lazy<ProtoDef<HubSettingsWire>> =
        lazy
        // Field Definitions
        let BarDataDirOverride = FieldCodec.Primitive ValueCodec.String (1, "barDataDirOverride")
        let EngineVersionOverride = FieldCodec.Primitive ValueCodec.String (2, "engineVersionOverride")
        let GrpcPort = FieldCodec.Primitive ValueCodec.Int32 (3, "grpcPort")
        let LaunchGraphicalViewerDefault = FieldCodec.Primitive ValueCodec.Bool (4, "launchGraphicalViewerDefault")
        let StartPausedDefault = FieldCodec.Primitive ValueCodec.Bool (5, "startPausedDefault")
        let MaxRenderFrameSubscribers = FieldCodec.Primitive ValueCodec.Int32 (6, "maxRenderFrameSubscribers")
        let SchemaVersion = FieldCodec.Primitive ValueCodec.Int32 (7, "schemaVersion")
        // Proto Definition Implementation
        { // ProtoDef<HubSettingsWire>
            Name = "HubSettingsWire"
            Empty = {
                BarDataDirOverride = BarDataDirOverride.GetDefault()
                EngineVersionOverride = EngineVersionOverride.GetDefault()
                GrpcPort = GrpcPort.GetDefault()
                LaunchGraphicalViewerDefault = LaunchGraphicalViewerDefault.GetDefault()
                StartPausedDefault = StartPausedDefault.GetDefault()
                MaxRenderFrameSubscribers = MaxRenderFrameSubscribers.GetDefault()
                SchemaVersion = SchemaVersion.GetDefault()
                }
            Size = fun (m: HubSettingsWire) ->
                0
                + BarDataDirOverride.CalcFieldSize m.BarDataDirOverride
                + EngineVersionOverride.CalcFieldSize m.EngineVersionOverride
                + GrpcPort.CalcFieldSize m.GrpcPort
                + LaunchGraphicalViewerDefault.CalcFieldSize m.LaunchGraphicalViewerDefault
                + StartPausedDefault.CalcFieldSize m.StartPausedDefault
                + MaxRenderFrameSubscribers.CalcFieldSize m.MaxRenderFrameSubscribers
                + SchemaVersion.CalcFieldSize m.SchemaVersion
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: HubSettingsWire) ->
                BarDataDirOverride.WriteField w m.BarDataDirOverride
                EngineVersionOverride.WriteField w m.EngineVersionOverride
                GrpcPort.WriteField w m.GrpcPort
                LaunchGraphicalViewerDefault.WriteField w m.LaunchGraphicalViewerDefault
                StartPausedDefault.WriteField w m.StartPausedDefault
                MaxRenderFrameSubscribers.WriteField w m.MaxRenderFrameSubscribers
                SchemaVersion.WriteField w m.SchemaVersion
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.HubSettingsWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeBarDataDirOverride = BarDataDirOverride.WriteJsonField o
                let writeEngineVersionOverride = EngineVersionOverride.WriteJsonField o
                let writeGrpcPort = GrpcPort.WriteJsonField o
                let writeLaunchGraphicalViewerDefault = LaunchGraphicalViewerDefault.WriteJsonField o
                let writeStartPausedDefault = StartPausedDefault.WriteJsonField o
                let writeMaxRenderFrameSubscribers = MaxRenderFrameSubscribers.WriteJsonField o
                let writeSchemaVersion = SchemaVersion.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: HubSettingsWire) =
                    writeBarDataDirOverride w m.BarDataDirOverride
                    writeEngineVersionOverride w m.EngineVersionOverride
                    writeGrpcPort w m.GrpcPort
                    writeLaunchGraphicalViewerDefault w m.LaunchGraphicalViewerDefault
                    writeStartPausedDefault w m.StartPausedDefault
                    writeMaxRenderFrameSubscribers w m.MaxRenderFrameSubscribers
                    writeSchemaVersion w m.SchemaVersion
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : HubSettingsWire =
                    match kvPair.Key with
                    | "barDataDirOverride" -> { value with BarDataDirOverride = BarDataDirOverride.ReadJsonField kvPair.Value }
                    | "engineVersionOverride" -> { value with EngineVersionOverride = EngineVersionOverride.ReadJsonField kvPair.Value }
                    | "grpcPort" -> { value with GrpcPort = GrpcPort.ReadJsonField kvPair.Value }
                    | "launchGraphicalViewerDefault" -> { value with LaunchGraphicalViewerDefault = LaunchGraphicalViewerDefault.ReadJsonField kvPair.Value }
                    | "startPausedDefault" -> { value with StartPausedDefault = StartPausedDefault.ReadJsonField kvPair.Value }
                    | "maxRenderFrameSubscribers" -> { value with MaxRenderFrameSubscribers = MaxRenderFrameSubscribers.ReadJsonField kvPair.Value }
                    | "schemaVersion" -> { value with SchemaVersion = SchemaVersion.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _HubSettingsWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._HubSettingsWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetHubSettingsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Settings: OptionBuilder<Fsbar.Hub.Scripting.V1.HubSettingsWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Settings.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetHubSettingsRequest = {
            Settings = x.Settings.Build
            }

type private _SetHubSettingsRequest = SetHubSettingsRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetHubSettingsRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("settings")>] Settings: Fsbar.Hub.Scripting.V1.HubSettingsWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetHubSettingsRequest>> =
        lazy
        // Field Definitions
        let Settings = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire> (1, "settings")
        // Proto Definition Implementation
        { // ProtoDef<SetHubSettingsRequest>
            Name = "SetHubSettingsRequest"
            Empty = {
                Settings = Settings.GetDefault()
                }
            Size = fun (m: SetHubSettingsRequest) ->
                0
                + Settings.CalcFieldSize m.Settings
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetHubSettingsRequest) ->
                Settings.WriteField w m.Settings
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetHubSettingsRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSettings = Settings.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetHubSettingsRequest) =
                    writeSettings w m.Settings
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetHubSettingsRequest =
                    match kvPair.Key with
                    | "settings" -> { value with Settings = Settings.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetHubSettingsRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetHubSettingsRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module SetHubSettingsResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.SetHubSettingsResponse = {
            Result = x.Result.Build
            }

type private _SetHubSettingsResponse = SetHubSettingsResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type SetHubSettingsResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<SetHubSettingsResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<SetHubSettingsResponse>
            Name = "SetHubSettingsResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: SetHubSettingsResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: SetHubSettingsResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.SetHubSettingsResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: SetHubSettingsResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : SetHubSettingsResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _SetHubSettingsResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._SetHubSettingsResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module InstallProxyRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ForceReinstall: bool // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ForceReinstall <- ValueCodec.Bool.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.InstallProxyRequest = {
            ForceReinstall = x.ForceReinstall
            }

type private _InstallProxyRequest = InstallProxyRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type InstallProxyRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("forceReinstall")>] ForceReinstall: bool // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<InstallProxyRequest>> =
        lazy
        // Field Definitions
        let ForceReinstall = FieldCodec.Primitive ValueCodec.Bool (1, "forceReinstall")
        // Proto Definition Implementation
        { // ProtoDef<InstallProxyRequest>
            Name = "InstallProxyRequest"
            Empty = {
                ForceReinstall = ForceReinstall.GetDefault()
                }
            Size = fun (m: InstallProxyRequest) ->
                0
                + ForceReinstall.CalcFieldSize m.ForceReinstall
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: InstallProxyRequest) ->
                ForceReinstall.WriteField w m.ForceReinstall
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.InstallProxyRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeForceReinstall = ForceReinstall.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: InstallProxyRequest) =
                    writeForceReinstall w m.ForceReinstall
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : InstallProxyRequest =
                    match kvPair.Key with
                    | "forceReinstall" -> { value with ForceReinstall = ForceReinstall.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _InstallProxyRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._InstallProxyRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module InstallProxyResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable InstalledVersion: OptionBuilder<string> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.InstalledVersion.Set (ValueCodec.String.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.InstallProxyResponse = {
            Result = x.Result.Build
            InstalledVersion = x.InstalledVersion.Build
            }

type private _InstallProxyResponse = InstallProxyResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type InstallProxyResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("installedVersion")>] InstalledVersion: string option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<InstallProxyResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let InstalledVersion = FieldCodec.Optional ValueCodec.String (2, "installedVersion")
        // Proto Definition Implementation
        { // ProtoDef<InstallProxyResponse>
            Name = "InstallProxyResponse"
            Empty = {
                Result = Result.GetDefault()
                InstalledVersion = InstalledVersion.GetDefault()
                }
            Size = fun (m: InstallProxyResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + InstalledVersion.CalcFieldSize m.InstalledVersion
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: InstallProxyResponse) ->
                Result.WriteField w m.Result
                InstalledVersion.WriteField w m.InstalledVersion
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.InstallProxyResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeInstalledVersion = InstalledVersion.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: InstallProxyResponse) =
                    writeResult w m.Result
                    writeInstalledVersion w m.InstalledVersion
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : InstallProxyResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "installedVersion" -> { value with InstalledVersion = InstalledVersion.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _InstallProxyResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._InstallProxyResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RefreshProxyStatusRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = RefreshProxyStatusRequest.empty

[<StructuralEquality;StructuralComparison>]
type RefreshProxyStatusRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<RefreshProxyStatusRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<RefreshProxyStatusRequest>
            Name = "RefreshProxyStatusRequest"
            Empty = RefreshProxyStatusRequest.empty
            Size = fun (m: RefreshProxyStatusRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RefreshProxyStatusRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                RefreshProxyStatusRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> RefreshProxyStatusRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RefreshProxyStatusResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable InstalledVersion: string // (1)
            val mutable InstallPath: string // (2)
            val mutable Health: string // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.InstalledVersion <- ValueCodec.String.ReadValue reader
            | 2 -> x.InstallPath <- ValueCodec.String.ReadValue reader
            | 3 -> x.Health <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse = {
            InstalledVersion = x.InstalledVersion |> orEmptyString
            InstallPath = x.InstallPath |> orEmptyString
            Health = x.Health |> orEmptyString
            }

type private _RefreshProxyStatusResponse = RefreshProxyStatusResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RefreshProxyStatusResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("installedVersion")>] InstalledVersion: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("installPath")>] InstallPath: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("health")>] Health: string // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<RefreshProxyStatusResponse>> =
        lazy
        // Field Definitions
        let InstalledVersion = FieldCodec.Primitive ValueCodec.String (1, "installedVersion")
        let InstallPath = FieldCodec.Primitive ValueCodec.String (2, "installPath")
        let Health = FieldCodec.Primitive ValueCodec.String (3, "health")
        // Proto Definition Implementation
        { // ProtoDef<RefreshProxyStatusResponse>
            Name = "RefreshProxyStatusResponse"
            Empty = {
                InstalledVersion = InstalledVersion.GetDefault()
                InstallPath = InstallPath.GetDefault()
                Health = Health.GetDefault()
                }
            Size = fun (m: RefreshProxyStatusResponse) ->
                0
                + InstalledVersion.CalcFieldSize m.InstalledVersion
                + InstallPath.CalcFieldSize m.InstallPath
                + Health.CalcFieldSize m.Health
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RefreshProxyStatusResponse) ->
                InstalledVersion.WriteField w m.InstalledVersion
                InstallPath.WriteField w m.InstallPath
                Health.WriteField w m.Health
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeInstalledVersion = InstalledVersion.WriteJsonField o
                let writeInstallPath = InstallPath.WriteJsonField o
                let writeHealth = Health.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RefreshProxyStatusResponse) =
                    writeInstalledVersion w m.InstalledVersion
                    writeInstallPath w m.InstallPath
                    writeHealth w m.Health
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RefreshProxyStatusResponse =
                    match kvPair.Key with
                    | "installedVersion" -> { value with InstalledVersion = InstalledVersion.ReadJsonField kvPair.Value }
                    | "installPath" -> { value with InstallPath = InstallPath.ReadJsonField kvPair.Value }
                    | "health" -> { value with Health = Health.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RefreshProxyStatusResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._RefreshProxyStatusResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module GetHubStateRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = GetHubStateRequest.empty

[<StructuralEquality;StructuralComparison>]
type GetHubStateRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<GetHubStateRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<GetHubStateRequest>
            Name = "GetHubStateRequest"
            Empty = GetHubStateRequest.empty
            Size = fun (m: GetHubStateRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: GetHubStateRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                GetHubStateRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> GetHubStateRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module HubStateSnapshot =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ActiveTab: Fsbar.Hub.Scripting.V1.HubTab // (1)
            val mutable VizConfig: OptionBuilder<Fsbar.Hub.Scripting.V1.VizConfigWire> // (2)
            val mutable Camera: OptionBuilder<Fsbar.Hub.Scripting.V1.ViewerCameraWire> // (3)
            val mutable Lobby: OptionBuilder<Fsbar.Hub.Scripting.V1.LobbyConfigWire> // (4)
            val mutable Encyclopedia: OptionBuilder<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire> // (5)
            val mutable Presets: RepeatedBuilder<Fsbar.Hub.Scripting.V1.PresetDescriptor> // (6)
            val mutable SessionStatus: OptionBuilder<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse> // (7)
            val mutable HubSettings: OptionBuilder<Fsbar.Hub.Scripting.V1.HubSettingsWire> // (8)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ActiveTab <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab>.ReadValue reader
            | 2 -> x.VizConfig.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire>.ReadValue reader)
            | 3 -> x.Camera.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire>.ReadValue reader)
            | 4 -> x.Lobby.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire>.ReadValue reader)
            | 5 -> x.Encyclopedia.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire>.ReadValue reader)
            | 6 -> x.Presets.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetDescriptor>.ReadValue reader)
            | 7 -> x.SessionStatus.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>.ReadValue reader)
            | 8 -> x.HubSettings.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.HubStateSnapshot = {
            ActiveTab = x.ActiveTab
            VizConfig = x.VizConfig.Build
            Camera = x.Camera.Build
            Lobby = x.Lobby.Build
            Encyclopedia = x.Encyclopedia.Build
            Presets = x.Presets.Build
            SessionStatus = x.SessionStatus.Build
            HubSettings = x.HubSettings.Build
            }

type private _HubStateSnapshot = HubStateSnapshot
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type HubStateSnapshot = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("activeTab")>] ActiveTab: Fsbar.Hub.Scripting.V1.HubTab // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("vizConfig")>] VizConfig: Fsbar.Hub.Scripting.V1.VizConfigWire option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("camera")>] Camera: Fsbar.Hub.Scripting.V1.ViewerCameraWire option // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("lobby")>] Lobby: Fsbar.Hub.Scripting.V1.LobbyConfigWire option // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("encyclopedia")>] Encyclopedia: Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire option // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("presets")>] Presets: Fsbar.Hub.Scripting.V1.PresetDescriptor list // (6)
    [<System.Text.Json.Serialization.JsonPropertyName("sessionStatus")>] SessionStatus: Fsbar.Hub.Scripting.V1.GetSessionStatusResponse option // (7)
    [<System.Text.Json.Serialization.JsonPropertyName("hubSettings")>] HubSettings: Fsbar.Hub.Scripting.V1.HubSettingsWire option // (8)
    }
    with
    static member Proto : Lazy<ProtoDef<HubStateSnapshot>> =
        lazy
        // Field Definitions
        let ActiveTab = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab> (1, "activeTab")
        let VizConfig = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (2, "vizConfig")
        let Camera = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire> (3, "camera")
        let Lobby = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire> (4, "lobby")
        let Encyclopedia = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire> (5, "encyclopedia")
        let Presets = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetDescriptor> (6, "presets")
        let SessionStatus = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse> (7, "sessionStatus")
        let HubSettings = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire> (8, "hubSettings")
        // Proto Definition Implementation
        { // ProtoDef<HubStateSnapshot>
            Name = "HubStateSnapshot"
            Empty = {
                ActiveTab = ActiveTab.GetDefault()
                VizConfig = VizConfig.GetDefault()
                Camera = Camera.GetDefault()
                Lobby = Lobby.GetDefault()
                Encyclopedia = Encyclopedia.GetDefault()
                Presets = Presets.GetDefault()
                SessionStatus = SessionStatus.GetDefault()
                HubSettings = HubSettings.GetDefault()
                }
            Size = fun (m: HubStateSnapshot) ->
                0
                + ActiveTab.CalcFieldSize m.ActiveTab
                + VizConfig.CalcFieldSize m.VizConfig
                + Camera.CalcFieldSize m.Camera
                + Lobby.CalcFieldSize m.Lobby
                + Encyclopedia.CalcFieldSize m.Encyclopedia
                + Presets.CalcFieldSize m.Presets
                + SessionStatus.CalcFieldSize m.SessionStatus
                + HubSettings.CalcFieldSize m.HubSettings
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: HubStateSnapshot) ->
                ActiveTab.WriteField w m.ActiveTab
                VizConfig.WriteField w m.VizConfig
                Camera.WriteField w m.Camera
                Lobby.WriteField w m.Lobby
                Encyclopedia.WriteField w m.Encyclopedia
                Presets.WriteField w m.Presets
                SessionStatus.WriteField w m.SessionStatus
                HubSettings.WriteField w m.HubSettings
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.HubStateSnapshot.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeActiveTab = ActiveTab.WriteJsonField o
                let writeVizConfig = VizConfig.WriteJsonField o
                let writeCamera = Camera.WriteJsonField o
                let writeLobby = Lobby.WriteJsonField o
                let writeEncyclopedia = Encyclopedia.WriteJsonField o
                let writePresets = Presets.WriteJsonField o
                let writeSessionStatus = SessionStatus.WriteJsonField o
                let writeHubSettings = HubSettings.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: HubStateSnapshot) =
                    writeActiveTab w m.ActiveTab
                    writeVizConfig w m.VizConfig
                    writeCamera w m.Camera
                    writeLobby w m.Lobby
                    writeEncyclopedia w m.Encyclopedia
                    writePresets w m.Presets
                    writeSessionStatus w m.SessionStatus
                    writeHubSettings w m.HubSettings
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : HubStateSnapshot =
                    match kvPair.Key with
                    | "activeTab" -> { value with ActiveTab = ActiveTab.ReadJsonField kvPair.Value }
                    | "vizConfig" -> { value with VizConfig = VizConfig.ReadJsonField kvPair.Value }
                    | "camera" -> { value with Camera = Camera.ReadJsonField kvPair.Value }
                    | "lobby" -> { value with Lobby = Lobby.ReadJsonField kvPair.Value }
                    | "encyclopedia" -> { value with Encyclopedia = Encyclopedia.ReadJsonField kvPair.Value }
                    | "presets" -> { value with Presets = Presets.ReadJsonField kvPair.Value }
                    | "sessionStatus" -> { value with SessionStatus = SessionStatus.ReadJsonField kvPair.Value }
                    | "hubSettings" -> { value with HubSettings = HubSettings.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _HubStateSnapshot.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._HubStateSnapshot.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module EncyclopediaSelectionWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable FactionFilter: RepeatedBuilder<string> // (1)
            val mutable SelectedDefId: OptionBuilder<int> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.FactionFilter.Add (ValueCodec.String.ReadValue reader)
            | 2 -> x.SelectedDefId.Set (ValueCodec.Int32.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire = {
            FactionFilter = x.FactionFilter.Build
            SelectedDefId = x.SelectedDefId.Build
            }

type private _EncyclopediaSelectionWire = EncyclopediaSelectionWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type EncyclopediaSelectionWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("factionFilter")>] FactionFilter: string list // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("selectedDefId")>] SelectedDefId: int option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<EncyclopediaSelectionWire>> =
        lazy
        // Field Definitions
        let FactionFilter = FieldCodec.Repeated ValueCodec.String (1, "factionFilter")
        let SelectedDefId = FieldCodec.Optional ValueCodec.Int32 (2, "selectedDefId")
        // Proto Definition Implementation
        { // ProtoDef<EncyclopediaSelectionWire>
            Name = "EncyclopediaSelectionWire"
            Empty = {
                FactionFilter = FactionFilter.GetDefault()
                SelectedDefId = SelectedDefId.GetDefault()
                }
            Size = fun (m: EncyclopediaSelectionWire) ->
                0
                + FactionFilter.CalcFieldSize m.FactionFilter
                + SelectedDefId.CalcFieldSize m.SelectedDefId
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: EncyclopediaSelectionWire) ->
                FactionFilter.WriteField w m.FactionFilter
                SelectedDefId.WriteField w m.SelectedDefId
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFactionFilter = FactionFilter.WriteJsonField o
                let writeSelectedDefId = SelectedDefId.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: EncyclopediaSelectionWire) =
                    writeFactionFilter w m.FactionFilter
                    writeSelectedDefId w m.SelectedDefId
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : EncyclopediaSelectionWire =
                    match kvPair.Key with
                    | "factionFilter" -> { value with FactionFilter = FactionFilter.ReadJsonField kvPair.Value }
                    | "selectedDefId" -> { value with SelectedDefId = SelectedDefId.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _EncyclopediaSelectionWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._EncyclopediaSelectionWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StreamHubStateEventsRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ClientLabel: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ClientLabel <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest = {
            ClientLabel = x.ClientLabel |> orEmptyString
            }

type private _StreamHubStateEventsRequest = StreamHubStateEventsRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StreamHubStateEventsRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("clientLabel")>] ClientLabel: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<StreamHubStateEventsRequest>> =
        lazy
        // Field Definitions
        let ClientLabel = FieldCodec.Primitive ValueCodec.String (1, "clientLabel")
        // Proto Definition Implementation
        { // ProtoDef<StreamHubStateEventsRequest>
            Name = "StreamHubStateEventsRequest"
            Empty = {
                ClientLabel = ClientLabel.GetDefault()
                }
            Size = fun (m: StreamHubStateEventsRequest) ->
                0
                + ClientLabel.CalcFieldSize m.ClientLabel
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StreamHubStateEventsRequest) ->
                ClientLabel.WriteField w m.ClientLabel
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeClientLabel = ClientLabel.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StreamHubStateEventsRequest) =
                    writeClientLabel w m.ClientLabel
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StreamHubStateEventsRequest =
                    match kvPair.Key with
                    | "clientLabel" -> { value with ClientLabel = ClientLabel.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StreamHubStateEventsRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StreamHubStateEventsRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module HubStateEvent =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<ChangeCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type ChangeCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("activeTab")>] ActiveTab of Fsbar.Hub.Scripting.V1.HubTab
    | [<System.Text.Json.Serialization.JsonPropertyName("vizConfig")>] VizConfig of Fsbar.Hub.Scripting.V1.VizConfigWire
    | [<System.Text.Json.Serialization.JsonPropertyName("vizAttribute")>] VizAttribute of Fsbar.Hub.Scripting.V1.VizAttributeChange
    | [<System.Text.Json.Serialization.JsonPropertyName("camera")>] Camera of Fsbar.Hub.Scripting.V1.ViewerCameraWire
    | [<System.Text.Json.Serialization.JsonPropertyName("lobby")>] Lobby of Fsbar.Hub.Scripting.V1.LobbyConfigWire
    | [<System.Text.Json.Serialization.JsonPropertyName("encyclopedia")>] Encyclopedia of Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire
    | [<System.Text.Json.Serialization.JsonPropertyName("preset")>] Preset of Fsbar.Hub.Scripting.V1.PresetChange
    | [<System.Text.Json.Serialization.JsonPropertyName("sessionStatus")>] SessionStatus of Fsbar.Hub.Scripting.V1.GetSessionStatusResponse
    | [<System.Text.Json.Serialization.JsonPropertyName("adminChannelStatus")>] AdminChannelStatus of Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo
    | [<System.Text.Json.Serialization.JsonPropertyName("hubSettings")>] HubSettings of Fsbar.Hub.Scripting.V1.HubSettingsWire
    | [<System.Text.Json.Serialization.JsonPropertyName("proxyInstallProgress")>] ProxyInstallProgress of Fsbar.Hub.Scripting.V1.ProxyInstallProgress
    with
        static member OneofCodec : Lazy<OneofCodec<ChangeCase>> = 
            lazy
            let ActiveTab = FieldCodec.OneofCase "change" ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab> (1, "activeTab")
            let VizConfig = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (2, "vizConfig")
            let VizAttribute = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeChange> (3, "vizAttribute")
            let Camera = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire> (4, "camera")
            let Lobby = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire> (5, "lobby")
            let Encyclopedia = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire> (6, "encyclopedia")
            let Preset = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetChange> (7, "preset")
            let SessionStatus = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse> (8, "sessionStatus")
            let AdminChannelStatus = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> (9, "adminChannelStatus")
            let HubSettings = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire> (10, "hubSettings")
            let ProxyInstallProgress = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ProxyInstallProgress> (11, "proxyInstallProgress")
            let Change = FieldCodec.Oneof "change" (FSharp.Collections.Map [
                ("activeTab", fun node -> ChangeCase.ActiveTab (ActiveTab.ReadJsonField node))
                ("vizConfig", fun node -> ChangeCase.VizConfig (VizConfig.ReadJsonField node))
                ("vizAttribute", fun node -> ChangeCase.VizAttribute (VizAttribute.ReadJsonField node))
                ("camera", fun node -> ChangeCase.Camera (Camera.ReadJsonField node))
                ("lobby", fun node -> ChangeCase.Lobby (Lobby.ReadJsonField node))
                ("encyclopedia", fun node -> ChangeCase.Encyclopedia (Encyclopedia.ReadJsonField node))
                ("preset", fun node -> ChangeCase.Preset (Preset.ReadJsonField node))
                ("sessionStatus", fun node -> ChangeCase.SessionStatus (SessionStatus.ReadJsonField node))
                ("adminChannelStatus", fun node -> ChangeCase.AdminChannelStatus (AdminChannelStatus.ReadJsonField node))
                ("hubSettings", fun node -> ChangeCase.HubSettings (HubSettings.ReadJsonField node))
                ("proxyInstallProgress", fun node -> ChangeCase.ProxyInstallProgress (ProxyInstallProgress.ReadJsonField node))
                ])
            Change

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Change: OptionBuilder<Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase>
            val mutable EmittedAtUnixMs: int64 // (12)
            val mutable Source: string // (13)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Change.Set (ChangeCase.ActiveTab (ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab>.ReadValue reader))
            | 2 -> x.Change.Set (ChangeCase.VizConfig (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire>.ReadValue reader))
            | 3 -> x.Change.Set (ChangeCase.VizAttribute (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeChange>.ReadValue reader))
            | 4 -> x.Change.Set (ChangeCase.Camera (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire>.ReadValue reader))
            | 5 -> x.Change.Set (ChangeCase.Lobby (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire>.ReadValue reader))
            | 6 -> x.Change.Set (ChangeCase.Encyclopedia (ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire>.ReadValue reader))
            | 7 -> x.Change.Set (ChangeCase.Preset (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetChange>.ReadValue reader))
            | 8 -> x.Change.Set (ChangeCase.SessionStatus (ValueCodec.Message<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>.ReadValue reader))
            | 9 -> x.Change.Set (ChangeCase.AdminChannelStatus (ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo>.ReadValue reader))
            | 10 -> x.Change.Set (ChangeCase.HubSettings (ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire>.ReadValue reader))
            | 11 -> x.Change.Set (ChangeCase.ProxyInstallProgress (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ProxyInstallProgress>.ReadValue reader))
            | 12 -> x.EmittedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 13 -> x.Source <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.HubStateEvent = {
            Change = x.Change.Build |> (Option.defaultValue ChangeCase.None)
            EmittedAtUnixMs = x.EmittedAtUnixMs
            Source = x.Source |> orEmptyString
            }

type private _HubStateEvent = HubStateEvent
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type HubStateEvent = {
    // Field Declarations
    Change: Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase
    [<System.Text.Json.Serialization.JsonPropertyName("emittedAtUnixMs")>] EmittedAtUnixMs: int64 // (12)
    [<System.Text.Json.Serialization.JsonPropertyName("source")>] Source: string // (13)
    }
    with
    static member Proto : Lazy<ProtoDef<HubStateEvent>> =
        lazy
        // Field Definitions
        let ActiveTab = FieldCodec.OneofCase "change" ValueCodec.Enum<Fsbar.Hub.Scripting.V1.HubTab> (1, "activeTab")
        let VizConfig = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizConfigWire> (2, "vizConfig")
        let VizAttribute = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeChange> (3, "vizAttribute")
        let Camera = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ViewerCameraWire> (4, "camera")
        let Lobby = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.LobbyConfigWire> (5, "lobby")
        let Encyclopedia = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.EncyclopediaSelectionWire> (6, "encyclopedia")
        let Preset = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PresetChange> (7, "preset")
        let SessionStatus = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse> (8, "sessionStatus")
        let AdminChannelStatus = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.AdminChannelStatusInfo> (9, "adminChannelStatus")
        let HubSettings = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.HubSettingsWire> (10, "hubSettings")
        let ProxyInstallProgress = FieldCodec.OneofCase "change" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ProxyInstallProgress> (11, "proxyInstallProgress")
        let EmittedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (12, "emittedAtUnixMs")
        let Source = FieldCodec.Primitive ValueCodec.String (13, "source")
        let Change = FieldCodec.Oneof "change" (FSharp.Collections.Map [
            ("activeTab", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ActiveTab (ActiveTab.ReadJsonField node))
            ("vizConfig", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizConfig (VizConfig.ReadJsonField node))
            ("vizAttribute", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizAttribute (VizAttribute.ReadJsonField node))
            ("camera", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Camera (Camera.ReadJsonField node))
            ("lobby", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Lobby (Lobby.ReadJsonField node))
            ("encyclopedia", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Encyclopedia (Encyclopedia.ReadJsonField node))
            ("preset", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Preset (Preset.ReadJsonField node))
            ("sessionStatus", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.SessionStatus (SessionStatus.ReadJsonField node))
            ("adminChannelStatus", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.AdminChannelStatus (AdminChannelStatus.ReadJsonField node))
            ("hubSettings", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.HubSettings (HubSettings.ReadJsonField node))
            ("proxyInstallProgress", fun node -> Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ProxyInstallProgress (ProxyInstallProgress.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<HubStateEvent>
            Name = "HubStateEvent"
            Empty = {
                Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.None
                EmittedAtUnixMs = EmittedAtUnixMs.GetDefault()
                Source = Source.GetDefault()
                }
            Size = fun (m: HubStateEvent) ->
                0
                + match m.Change with
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ActiveTab v -> ActiveTab.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizConfig v -> VizConfig.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizAttribute v -> VizAttribute.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Camera v -> Camera.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Lobby v -> Lobby.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Encyclopedia v -> Encyclopedia.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Preset v -> Preset.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.SessionStatus v -> SessionStatus.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.AdminChannelStatus v -> AdminChannelStatus.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.HubSettings v -> HubSettings.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ProxyInstallProgress v -> ProxyInstallProgress.CalcFieldSize v
                + EmittedAtUnixMs.CalcFieldSize m.EmittedAtUnixMs
                + Source.CalcFieldSize m.Source
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: HubStateEvent) ->
                (match m.Change with
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.None -> ()
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ActiveTab v -> ActiveTab.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizConfig v -> VizConfig.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizAttribute v -> VizAttribute.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Camera v -> Camera.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Lobby v -> Lobby.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Encyclopedia v -> Encyclopedia.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Preset v -> Preset.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.SessionStatus v -> SessionStatus.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.AdminChannelStatus v -> AdminChannelStatus.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.HubSettings v -> HubSettings.WriteField w v
                | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ProxyInstallProgress v -> ProxyInstallProgress.WriteField w v
                )
                EmittedAtUnixMs.WriteField w m.EmittedAtUnixMs
                Source.WriteField w m.Source
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.HubStateEvent.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeChangeNone = Change.WriteJsonNoneCase o
                let writeActiveTab = ActiveTab.WriteJsonField o
                let writeVizConfig = VizConfig.WriteJsonField o
                let writeVizAttribute = VizAttribute.WriteJsonField o
                let writeCamera = Camera.WriteJsonField o
                let writeLobby = Lobby.WriteJsonField o
                let writeEncyclopedia = Encyclopedia.WriteJsonField o
                let writePreset = Preset.WriteJsonField o
                let writeSessionStatus = SessionStatus.WriteJsonField o
                let writeAdminChannelStatus = AdminChannelStatus.WriteJsonField o
                let writeHubSettings = HubSettings.WriteJsonField o
                let writeProxyInstallProgress = ProxyInstallProgress.WriteJsonField o
                let writeEmittedAtUnixMs = EmittedAtUnixMs.WriteJsonField o
                let writeSource = Source.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: HubStateEvent) =
                    (match m.Change with
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.None -> writeChangeNone w
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ActiveTab v -> writeActiveTab w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizConfig v -> writeVizConfig w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizAttribute v -> writeVizAttribute w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Camera v -> writeCamera w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Lobby v -> writeLobby w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Encyclopedia v -> writeEncyclopedia w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Preset v -> writePreset w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.SessionStatus v -> writeSessionStatus w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.AdminChannelStatus v -> writeAdminChannelStatus w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.HubSettings v -> writeHubSettings w v
                    | Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ProxyInstallProgress v -> writeProxyInstallProgress w v
                    )
                    writeEmittedAtUnixMs w m.EmittedAtUnixMs
                    writeSource w m.Source
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : HubStateEvent =
                    match kvPair.Key with
                    | "activeTab" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ActiveTab (ActiveTab.ReadJsonField kvPair.Value) }
                    | "vizConfig" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizConfig (VizConfig.ReadJsonField kvPair.Value) }
                    | "vizAttribute" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.VizAttribute (VizAttribute.ReadJsonField kvPair.Value) }
                    | "camera" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Camera (Camera.ReadJsonField kvPair.Value) }
                    | "lobby" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Lobby (Lobby.ReadJsonField kvPair.Value) }
                    | "encyclopedia" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Encyclopedia (Encyclopedia.ReadJsonField kvPair.Value) }
                    | "preset" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.Preset (Preset.ReadJsonField kvPair.Value) }
                    | "sessionStatus" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.SessionStatus (SessionStatus.ReadJsonField kvPair.Value) }
                    | "adminChannelStatus" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.AdminChannelStatus (AdminChannelStatus.ReadJsonField kvPair.Value) }
                    | "hubSettings" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.HubSettings (HubSettings.ReadJsonField kvPair.Value) }
                    | "proxyInstallProgress" -> { value with Change = Fsbar.Hub.Scripting.V1.HubStateEvent.ChangeCase.ProxyInstallProgress (ProxyInstallProgress.ReadJsonField kvPair.Value) }
                    | "change" -> { value with Change = Change.ReadJsonField kvPair.Value }
                    | "emittedAtUnixMs" -> { value with EmittedAtUnixMs = EmittedAtUnixMs.ReadJsonField kvPair.Value }
                    | "source" -> { value with Source = Source.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _HubStateEvent.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._HubStateEvent.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module VizAttributeChange =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Key: string // (1)
            val mutable OldValue: OptionBuilder<Fsbar.Hub.Scripting.V1.VizAttributeValue> // (2)
            val mutable NewValue: OptionBuilder<Fsbar.Hub.Scripting.V1.VizAttributeValue> // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Key <- ValueCodec.String.ReadValue reader
            | 2 -> x.OldValue.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue>.ReadValue reader)
            | 3 -> x.NewValue.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.VizAttributeChange = {
            Key = x.Key |> orEmptyString
            OldValue = x.OldValue.Build
            NewValue = x.NewValue.Build
            }

type private _VizAttributeChange = VizAttributeChange
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type VizAttributeChange = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("key")>] Key: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("oldValue")>] OldValue: Fsbar.Hub.Scripting.V1.VizAttributeValue option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("newValue")>] NewValue: Fsbar.Hub.Scripting.V1.VizAttributeValue option // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<VizAttributeChange>> =
        lazy
        // Field Definitions
        let Key = FieldCodec.Primitive ValueCodec.String (1, "key")
        let OldValue = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue> (2, "oldValue")
        let NewValue = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.VizAttributeValue> (3, "newValue")
        // Proto Definition Implementation
        { // ProtoDef<VizAttributeChange>
            Name = "VizAttributeChange"
            Empty = {
                Key = Key.GetDefault()
                OldValue = OldValue.GetDefault()
                NewValue = NewValue.GetDefault()
                }
            Size = fun (m: VizAttributeChange) ->
                0
                + Key.CalcFieldSize m.Key
                + OldValue.CalcFieldSize m.OldValue
                + NewValue.CalcFieldSize m.NewValue
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: VizAttributeChange) ->
                Key.WriteField w m.Key
                OldValue.WriteField w m.OldValue
                NewValue.WriteField w m.NewValue
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.VizAttributeChange.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeKey = Key.WriteJsonField o
                let writeOldValue = OldValue.WriteJsonField o
                let writeNewValue = NewValue.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: VizAttributeChange) =
                    writeKey w m.Key
                    writeOldValue w m.OldValue
                    writeNewValue w m.NewValue
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : VizAttributeChange =
                    match kvPair.Key with
                    | "key" -> { value with Key = Key.ReadJsonField kvPair.Value }
                    | "oldValue" -> { value with OldValue = OldValue.ReadJsonField kvPair.Value }
                    | "newValue" -> { value with NewValue = NewValue.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _VizAttributeChange.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._VizAttributeChange.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PresetChange =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.EnumConverter<Kind>>)>]
    type Kind =
    | [<FsGrpc.Protobuf.ProtobufName("KIND_UNSPECIFIED")>] Unspecified = 0
    | [<FsGrpc.Protobuf.ProtobufName("KIND_SAVED")>] Saved = 1
    | [<FsGrpc.Protobuf.ProtobufName("KIND_DELETED")>] Deleted = 2
    | [<FsGrpc.Protobuf.ProtobufName("KIND_LOADED")>] Loaded = 3

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Kind: Fsbar.Hub.Scripting.V1.PresetChange.Kind // (1)
            val mutable Name: string // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Kind <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.PresetChange.Kind>.ReadValue reader
            | 2 -> x.Name <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PresetChange = {
            Kind = x.Kind
            Name = x.Name |> orEmptyString
            }

type private _PresetChange = PresetChange
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PresetChange = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("kind")>] Kind: Fsbar.Hub.Scripting.V1.PresetChange.Kind // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<PresetChange>> =
        lazy
        // Field Definitions
        let Kind = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.PresetChange.Kind> (1, "kind")
        let Name = FieldCodec.Primitive ValueCodec.String (2, "name")
        // Proto Definition Implementation
        { // ProtoDef<PresetChange>
            Name = "PresetChange"
            Empty = {
                Kind = Kind.GetDefault()
                Name = Name.GetDefault()
                }
            Size = fun (m: PresetChange) ->
                0
                + Kind.CalcFieldSize m.Kind
                + Name.CalcFieldSize m.Name
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PresetChange) ->
                Kind.WriteField w m.Kind
                Name.WriteField w m.Name
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PresetChange.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeKind = Kind.WriteJsonField o
                let writeName = Name.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PresetChange) =
                    writeKind w m.Kind
                    writeName w m.Name
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PresetChange =
                    match kvPair.Key with
                    | "kind" -> { value with Kind = Kind.ReadJsonField kvPair.Value }
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PresetChange.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PresetChange.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ProxyInstallProgress =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Stage: string // (1)
            val mutable Percent: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Stage <- ValueCodec.String.ReadValue reader
            | 2 -> x.Percent <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ProxyInstallProgress = {
            Stage = x.Stage |> orEmptyString
            Percent = x.Percent
            }

type private _ProxyInstallProgress = ProxyInstallProgress
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ProxyInstallProgress = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("stage")>] Stage: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("percent")>] Percent: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<ProxyInstallProgress>> =
        lazy
        // Field Definitions
        let Stage = FieldCodec.Primitive ValueCodec.String (1, "stage")
        let Percent = FieldCodec.Primitive ValueCodec.Int32 (2, "percent")
        // Proto Definition Implementation
        { // ProtoDef<ProxyInstallProgress>
            Name = "ProxyInstallProgress"
            Empty = {
                Stage = Stage.GetDefault()
                Percent = Percent.GetDefault()
                }
            Size = fun (m: ProxyInstallProgress) ->
                0
                + Stage.CalcFieldSize m.Stage
                + Percent.CalcFieldSize m.Percent
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ProxyInstallProgress) ->
                Stage.WriteField w m.Stage
                Percent.WriteField w m.Percent
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ProxyInstallProgress.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStage = Stage.WriteJsonField o
                let writePercent = Percent.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ProxyInstallProgress) =
                    writeStage w m.Stage
                    writePercent w m.Percent
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ProxyInstallProgress =
                    match kvPair.Key with
                    | "stage" -> { value with Stage = Stage.ReadJsonField kvPair.Value }
                    | "percent" -> { value with Percent = Percent.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ProxyInstallProgress.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ProxyInstallProgress.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OverlayPoint =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable X: float32 // (1)
            val mutable Y: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.X <- ValueCodec.Float.ReadValue reader
            | 2 -> x.Y <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.OverlayPoint = {
            X = x.X
            Y = x.Y
            }

type private _OverlayPoint = OverlayPoint
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type OverlayPoint = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("x")>] X: float32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("y")>] Y: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<OverlayPoint>> =
        lazy
        // Field Definitions
        let X = FieldCodec.Primitive ValueCodec.Float (1, "x")
        let Y = FieldCodec.Primitive ValueCodec.Float (2, "y")
        // Proto Definition Implementation
        { // ProtoDef<OverlayPoint>
            Name = "OverlayPoint"
            Empty = {
                X = X.GetDefault()
                Y = Y.GetDefault()
                }
            Size = fun (m: OverlayPoint) ->
                0
                + X.CalcFieldSize m.X
                + Y.CalcFieldSize m.Y
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: OverlayPoint) ->
                X.WriteField w m.X
                Y.WriteField w m.Y
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.OverlayPoint.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeX = X.WriteJsonField o
                let writeY = Y.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: OverlayPoint) =
                    writeX w m.X
                    writeY w m.Y
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : OverlayPoint =
                    match kvPair.Key with
                    | "x" -> { value with X = X.ReadJsonField kvPair.Value }
                    | "y" -> { value with Y = Y.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _OverlayPoint.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._OverlayPoint.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OverlayStyle =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable StrokeColorRgba: uint32 // (1)
            val mutable StrokeWidth: float32 // (2)
            val mutable HasFill: bool // (3)
            val mutable FillColorRgba: uint32 // (4)
            val mutable Opacity: float32 // (5)
            val mutable Dash: RepeatedBuilder<float32> // (6)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.StrokeColorRgba <- ValueCodec.UInt32.ReadValue reader
            | 2 -> x.StrokeWidth <- ValueCodec.Float.ReadValue reader
            | 3 -> x.HasFill <- ValueCodec.Bool.ReadValue reader
            | 4 -> x.FillColorRgba <- ValueCodec.UInt32.ReadValue reader
            | 5 -> x.Opacity <- ValueCodec.Float.ReadValue reader
            | 6 -> x.Dash.AddRange ((ValueCodec.Packed ValueCodec.Float).ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.OverlayStyle = {
            StrokeColorRgba = x.StrokeColorRgba
            StrokeWidth = x.StrokeWidth
            HasFill = x.HasFill
            FillColorRgba = x.FillColorRgba
            Opacity = x.Opacity
            Dash = x.Dash.Build
            }

type private _OverlayStyle = OverlayStyle
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type OverlayStyle = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("strokeColorRgba")>] StrokeColorRgba: uint32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("strokeWidth")>] StrokeWidth: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("hasFill")>] HasFill: bool // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("fillColorRgba")>] FillColorRgba: uint32 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("opacity")>] Opacity: float32 // (5)
    [<System.Text.Json.Serialization.JsonPropertyName("dash")>] Dash: float32 list // (6)
    }
    with
    static member Proto : Lazy<ProtoDef<OverlayStyle>> =
        lazy
        // Field Definitions
        let StrokeColorRgba = FieldCodec.Primitive ValueCodec.UInt32 (1, "strokeColorRgba")
        let StrokeWidth = FieldCodec.Primitive ValueCodec.Float (2, "strokeWidth")
        let HasFill = FieldCodec.Primitive ValueCodec.Bool (3, "hasFill")
        let FillColorRgba = FieldCodec.Primitive ValueCodec.UInt32 (4, "fillColorRgba")
        let Opacity = FieldCodec.Primitive ValueCodec.Float (5, "opacity")
        let Dash = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Float) (6, "dash")
        // Proto Definition Implementation
        { // ProtoDef<OverlayStyle>
            Name = "OverlayStyle"
            Empty = {
                StrokeColorRgba = StrokeColorRgba.GetDefault()
                StrokeWidth = StrokeWidth.GetDefault()
                HasFill = HasFill.GetDefault()
                FillColorRgba = FillColorRgba.GetDefault()
                Opacity = Opacity.GetDefault()
                Dash = Dash.GetDefault()
                }
            Size = fun (m: OverlayStyle) ->
                0
                + StrokeColorRgba.CalcFieldSize m.StrokeColorRgba
                + StrokeWidth.CalcFieldSize m.StrokeWidth
                + HasFill.CalcFieldSize m.HasFill
                + FillColorRgba.CalcFieldSize m.FillColorRgba
                + Opacity.CalcFieldSize m.Opacity
                + Dash.CalcFieldSize m.Dash
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: OverlayStyle) ->
                StrokeColorRgba.WriteField w m.StrokeColorRgba
                StrokeWidth.WriteField w m.StrokeWidth
                HasFill.WriteField w m.HasFill
                FillColorRgba.WriteField w m.FillColorRgba
                Opacity.WriteField w m.Opacity
                Dash.WriteField w m.Dash
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.OverlayStyle.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeStrokeColorRgba = StrokeColorRgba.WriteJsonField o
                let writeStrokeWidth = StrokeWidth.WriteJsonField o
                let writeHasFill = HasFill.WriteJsonField o
                let writeFillColorRgba = FillColorRgba.WriteJsonField o
                let writeOpacity = Opacity.WriteJsonField o
                let writeDash = Dash.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: OverlayStyle) =
                    writeStrokeColorRgba w m.StrokeColorRgba
                    writeStrokeWidth w m.StrokeWidth
                    writeHasFill w m.HasFill
                    writeFillColorRgba w m.FillColorRgba
                    writeOpacity w m.Opacity
                    writeDash w m.Dash
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : OverlayStyle =
                    match kvPair.Key with
                    | "strokeColorRgba" -> { value with StrokeColorRgba = StrokeColorRgba.ReadJsonField kvPair.Value }
                    | "strokeWidth" -> { value with StrokeWidth = StrokeWidth.ReadJsonField kvPair.Value }
                    | "hasFill" -> { value with HasFill = HasFill.ReadJsonField kvPair.Value }
                    | "fillColorRgba" -> { value with FillColorRgba = FillColorRgba.ReadJsonField kvPair.Value }
                    | "opacity" -> { value with Opacity = Opacity.ReadJsonField kvPair.Value }
                    | "dash" -> { value with Dash = Dash.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _OverlayStyle.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._OverlayStyle.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PathVerb =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<VerbCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type VerbCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("moveTo")>] MoveTo of Fsbar.Hub.Scripting.V1.OverlayPoint
    | [<System.Text.Json.Serialization.JsonPropertyName("lineTo")>] LineTo of Fsbar.Hub.Scripting.V1.OverlayPoint
    | [<System.Text.Json.Serialization.JsonPropertyName("cubicTo")>] CubicTo of Fsbar.Hub.Scripting.V1.CubicTo
    | [<System.Text.Json.Serialization.JsonPropertyName("close")>] Close of Fsbar.Hub.Scripting.V1.Close
    with
        static member OneofCodec : Lazy<OneofCodec<VerbCase>> = 
            lazy
            let MoveTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "moveTo")
            let LineTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (2, "lineTo")
            let CubicTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.CubicTo> (3, "cubicTo")
            let Close = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.Close> (4, "close")
            let Verb = FieldCodec.Oneof "verb" (FSharp.Collections.Map [
                ("moveTo", fun node -> VerbCase.MoveTo (MoveTo.ReadJsonField node))
                ("lineTo", fun node -> VerbCase.LineTo (LineTo.ReadJsonField node))
                ("cubicTo", fun node -> VerbCase.CubicTo (CubicTo.ReadJsonField node))
                ("close", fun node -> VerbCase.Close (Close.ReadJsonField node))
                ])
            Verb

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Verb: OptionBuilder<Fsbar.Hub.Scripting.V1.PathVerb.VerbCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Verb.Set (VerbCase.MoveTo (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader))
            | 2 -> x.Verb.Set (VerbCase.LineTo (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader))
            | 3 -> x.Verb.Set (VerbCase.CubicTo (ValueCodec.Message<Fsbar.Hub.Scripting.V1.CubicTo>.ReadValue reader))
            | 4 -> x.Verb.Set (VerbCase.Close (ValueCodec.Message<Fsbar.Hub.Scripting.V1.Close>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PathVerb = {
            Verb = x.Verb.Build |> (Option.defaultValue VerbCase.None)
            }

type private _PathVerb = PathVerb
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PathVerb = {
    // Field Declarations
    Verb: Fsbar.Hub.Scripting.V1.PathVerb.VerbCase
    }
    with
    static member Proto : Lazy<ProtoDef<PathVerb>> =
        lazy
        // Field Definitions
        let MoveTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "moveTo")
        let LineTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (2, "lineTo")
        let CubicTo = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.CubicTo> (3, "cubicTo")
        let Close = FieldCodec.OneofCase "verb" ValueCodec.Message<Fsbar.Hub.Scripting.V1.Close> (4, "close")
        let Verb = FieldCodec.Oneof "verb" (FSharp.Collections.Map [
            ("moveTo", fun node -> Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.MoveTo (MoveTo.ReadJsonField node))
            ("lineTo", fun node -> Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.LineTo (LineTo.ReadJsonField node))
            ("cubicTo", fun node -> Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.CubicTo (CubicTo.ReadJsonField node))
            ("close", fun node -> Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.Close (Close.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<PathVerb>
            Name = "PathVerb"
            Empty = {
                Verb = Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.None
                }
            Size = fun (m: PathVerb) ->
                0
                + match m.Verb with
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.MoveTo v -> MoveTo.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.LineTo v -> LineTo.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.CubicTo v -> CubicTo.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.Close v -> Close.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PathVerb) ->
                (match m.Verb with
                | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.None -> ()
                | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.MoveTo v -> MoveTo.WriteField w v
                | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.LineTo v -> LineTo.WriteField w v
                | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.CubicTo v -> CubicTo.WriteField w v
                | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.Close v -> Close.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PathVerb.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeVerbNone = Verb.WriteJsonNoneCase o
                let writeMoveTo = MoveTo.WriteJsonField o
                let writeLineTo = LineTo.WriteJsonField o
                let writeCubicTo = CubicTo.WriteJsonField o
                let writeClose = Close.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PathVerb) =
                    (match m.Verb with
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.None -> writeVerbNone w
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.MoveTo v -> writeMoveTo w v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.LineTo v -> writeLineTo w v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.CubicTo v -> writeCubicTo w v
                    | Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.Close v -> writeClose w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PathVerb =
                    match kvPair.Key with
                    | "moveTo" -> { value with Verb = Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.MoveTo (MoveTo.ReadJsonField kvPair.Value) }
                    | "lineTo" -> { value with Verb = Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.LineTo (LineTo.ReadJsonField kvPair.Value) }
                    | "cubicTo" -> { value with Verb = Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.CubicTo (CubicTo.ReadJsonField kvPair.Value) }
                    | "close" -> { value with Verb = Fsbar.Hub.Scripting.V1.PathVerb.VerbCase.Close (Close.ReadJsonField kvPair.Value) }
                    | "verb" -> { value with Verb = Verb.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PathVerb.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PathVerb.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CubicTo =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable C1: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
            val mutable C2: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (2)
            val mutable P: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.C1.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 2 -> x.C2.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 3 -> x.P.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.CubicTo = {
            C1 = x.C1.Build
            C2 = x.C2.Build
            P = x.P.Build
            }

type private _CubicTo = CubicTo
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CubicTo = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("c1")>] C1: Fsbar.Hub.Scripting.V1.OverlayPoint option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("c2")>] C2: Fsbar.Hub.Scripting.V1.OverlayPoint option // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("p")>] P: Fsbar.Hub.Scripting.V1.OverlayPoint option // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<CubicTo>> =
        lazy
        // Field Definitions
        let C1 = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "c1")
        let C2 = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (2, "c2")
        let P = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (3, "p")
        // Proto Definition Implementation
        { // ProtoDef<CubicTo>
            Name = "CubicTo"
            Empty = {
                C1 = C1.GetDefault()
                C2 = C2.GetDefault()
                P = P.GetDefault()
                }
            Size = fun (m: CubicTo) ->
                0
                + C1.CalcFieldSize m.C1
                + C2.CalcFieldSize m.C2
                + P.CalcFieldSize m.P
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CubicTo) ->
                C1.WriteField w m.C1
                C2.WriteField w m.C2
                P.WriteField w m.P
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.CubicTo.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeC1 = C1.WriteJsonField o
                let writeC2 = C2.WriteJsonField o
                let writeP = P.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CubicTo) =
                    writeC1 w m.C1
                    writeC2 w m.C2
                    writeP w m.P
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CubicTo =
                    match kvPair.Key with
                    | "c1" -> { value with C1 = C1.ReadJsonField kvPair.Value }
                    | "c2" -> { value with C2 = C2.ReadJsonField kvPair.Value }
                    | "p" -> { value with P = P.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CubicTo.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._CubicTo.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Close =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = Close.empty

[<StructuralEquality;StructuralComparison>]
type Close = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<Close>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<Close>
            Name = "Close"
            Empty = Close.empty
            Size = fun (m: Close) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: Close) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                Close.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> Close.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LinePrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable From: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
            val mutable To: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.From.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 2 -> x.To.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LinePrimitive = {
            From = x.From.Build
            To = x.To.Build
            }

type private _LinePrimitive = LinePrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LinePrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("from")>] From: Fsbar.Hub.Scripting.V1.OverlayPoint option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("to")>] To: Fsbar.Hub.Scripting.V1.OverlayPoint option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<LinePrimitive>> =
        lazy
        // Field Definitions
        let From = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "from")
        let To = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (2, "to")
        // Proto Definition Implementation
        { // ProtoDef<LinePrimitive>
            Name = "LinePrimitive"
            Empty = {
                From = From.GetDefault()
                To = To.GetDefault()
                }
            Size = fun (m: LinePrimitive) ->
                0
                + From.CalcFieldSize m.From
                + To.CalcFieldSize m.To
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LinePrimitive) ->
                From.WriteField w m.From
                To.WriteField w m.To
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LinePrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeFrom = From.WriteJsonField o
                let writeTo = To.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LinePrimitive) =
                    writeFrom w m.From
                    writeTo w m.To
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LinePrimitive =
                    match kvPair.Key with
                    | "from" -> { value with From = From.ReadJsonField kvPair.Value }
                    | "to" -> { value with To = To.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LinePrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LinePrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PolylinePrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Points: RepeatedBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Points.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PolylinePrimitive = {
            Points = x.Points.Build
            }

type private _PolylinePrimitive = PolylinePrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PolylinePrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("points")>] Points: Fsbar.Hub.Scripting.V1.OverlayPoint list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PolylinePrimitive>> =
        lazy
        // Field Definitions
        let Points = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "points")
        // Proto Definition Implementation
        { // ProtoDef<PolylinePrimitive>
            Name = "PolylinePrimitive"
            Empty = {
                Points = Points.GetDefault()
                }
            Size = fun (m: PolylinePrimitive) ->
                0
                + Points.CalcFieldSize m.Points
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PolylinePrimitive) ->
                Points.WriteField w m.Points
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PolylinePrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePoints = Points.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PolylinePrimitive) =
                    writePoints w m.Points
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PolylinePrimitive =
                    match kvPair.Key with
                    | "points" -> { value with Points = Points.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PolylinePrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PolylinePrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PolygonPrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Points: RepeatedBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Points.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PolygonPrimitive = {
            Points = x.Points.Build
            }

type private _PolygonPrimitive = PolygonPrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PolygonPrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("points")>] Points: Fsbar.Hub.Scripting.V1.OverlayPoint list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PolygonPrimitive>> =
        lazy
        // Field Definitions
        let Points = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "points")
        // Proto Definition Implementation
        { // ProtoDef<PolygonPrimitive>
            Name = "PolygonPrimitive"
            Empty = {
                Points = Points.GetDefault()
                }
            Size = fun (m: PolygonPrimitive) ->
                0
                + Points.CalcFieldSize m.Points
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PolygonPrimitive) ->
                Points.WriteField w m.Points
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PolygonPrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writePoints = Points.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PolygonPrimitive) =
                    writePoints w m.Points
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PolygonPrimitive =
                    match kvPair.Key with
                    | "points" -> { value with Points = Points.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PolygonPrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PolygonPrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module RectanglePrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable X: float32 // (1)
            val mutable Y: float32 // (2)
            val mutable Width: float32 // (3)
            val mutable Height: float32 // (4)
            val mutable CornerRadius: float32 // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.X <- ValueCodec.Float.ReadValue reader
            | 2 -> x.Y <- ValueCodec.Float.ReadValue reader
            | 3 -> x.Width <- ValueCodec.Float.ReadValue reader
            | 4 -> x.Height <- ValueCodec.Float.ReadValue reader
            | 5 -> x.CornerRadius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.RectanglePrimitive = {
            X = x.X
            Y = x.Y
            Width = x.Width
            Height = x.Height
            CornerRadius = x.CornerRadius
            }

type private _RectanglePrimitive = RectanglePrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type RectanglePrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("x")>] X: float32 // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("y")>] Y: float32 // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("width")>] Width: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("height")>] Height: float32 // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("cornerRadius")>] CornerRadius: float32 // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<RectanglePrimitive>> =
        lazy
        // Field Definitions
        let X = FieldCodec.Primitive ValueCodec.Float (1, "x")
        let Y = FieldCodec.Primitive ValueCodec.Float (2, "y")
        let Width = FieldCodec.Primitive ValueCodec.Float (3, "width")
        let Height = FieldCodec.Primitive ValueCodec.Float (4, "height")
        let CornerRadius = FieldCodec.Primitive ValueCodec.Float (5, "cornerRadius")
        // Proto Definition Implementation
        { // ProtoDef<RectanglePrimitive>
            Name = "RectanglePrimitive"
            Empty = {
                X = X.GetDefault()
                Y = Y.GetDefault()
                Width = Width.GetDefault()
                Height = Height.GetDefault()
                CornerRadius = CornerRadius.GetDefault()
                }
            Size = fun (m: RectanglePrimitive) ->
                0
                + X.CalcFieldSize m.X
                + Y.CalcFieldSize m.Y
                + Width.CalcFieldSize m.Width
                + Height.CalcFieldSize m.Height
                + CornerRadius.CalcFieldSize m.CornerRadius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: RectanglePrimitive) ->
                X.WriteField w m.X
                Y.WriteField w m.Y
                Width.WriteField w m.Width
                Height.WriteField w m.Height
                CornerRadius.WriteField w m.CornerRadius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.RectanglePrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeX = X.WriteJsonField o
                let writeY = Y.WriteJsonField o
                let writeWidth = Width.WriteJsonField o
                let writeHeight = Height.WriteJsonField o
                let writeCornerRadius = CornerRadius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: RectanglePrimitive) =
                    writeX w m.X
                    writeY w m.Y
                    writeWidth w m.Width
                    writeHeight w m.Height
                    writeCornerRadius w m.CornerRadius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : RectanglePrimitive =
                    match kvPair.Key with
                    | "x" -> { value with X = X.ReadJsonField kvPair.Value }
                    | "y" -> { value with Y = Y.ReadJsonField kvPair.Value }
                    | "width" -> { value with Width = Width.ReadJsonField kvPair.Value }
                    | "height" -> { value with Height = Height.ReadJsonField kvPair.Value }
                    | "cornerRadius" -> { value with CornerRadius = CornerRadius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _RectanglePrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._RectanglePrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module CirclePrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Center: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
            val mutable Radius: float32 // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Center.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 2 -> x.Radius <- ValueCodec.Float.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.CirclePrimitive = {
            Center = x.Center.Build
            Radius = x.Radius
            }

type private _CirclePrimitive = CirclePrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type CirclePrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("center")>] Center: Fsbar.Hub.Scripting.V1.OverlayPoint option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("radius")>] Radius: float32 // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<CirclePrimitive>> =
        lazy
        // Field Definitions
        let Center = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "center")
        let Radius = FieldCodec.Primitive ValueCodec.Float (2, "radius")
        // Proto Definition Implementation
        { // ProtoDef<CirclePrimitive>
            Name = "CirclePrimitive"
            Empty = {
                Center = Center.GetDefault()
                Radius = Radius.GetDefault()
                }
            Size = fun (m: CirclePrimitive) ->
                0
                + Center.CalcFieldSize m.Center
                + Radius.CalcFieldSize m.Radius
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: CirclePrimitive) ->
                Center.WriteField w m.Center
                Radius.WriteField w m.Radius
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.CirclePrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCenter = Center.WriteJsonField o
                let writeRadius = Radius.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: CirclePrimitive) =
                    writeCenter w m.Center
                    writeRadius w m.Radius
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : CirclePrimitive =
                    match kvPair.Key with
                    | "center" -> { value with Center = Center.ReadJsonField kvPair.Value }
                    | "radius" -> { value with Radius = Radius.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _CirclePrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._CirclePrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PathPrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Verbs: RepeatedBuilder<Fsbar.Hub.Scripting.V1.PathVerb> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Verbs.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PathVerb>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PathPrimitive = {
            Verbs = x.Verbs.Build
            }

type private _PathPrimitive = PathPrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PathPrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("verbs")>] Verbs: Fsbar.Hub.Scripting.V1.PathVerb list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PathPrimitive>> =
        lazy
        // Field Definitions
        let Verbs = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.PathVerb> (1, "verbs")
        // Proto Definition Implementation
        { // ProtoDef<PathPrimitive>
            Name = "PathPrimitive"
            Empty = {
                Verbs = Verbs.GetDefault()
                }
            Size = fun (m: PathPrimitive) ->
                0
                + Verbs.CalcFieldSize m.Verbs
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PathPrimitive) ->
                Verbs.WriteField w m.Verbs
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PathPrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeVerbs = Verbs.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PathPrimitive) =
                    writeVerbs w m.Verbs
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PathPrimitive =
                    match kvPair.Key with
                    | "verbs" -> { value with Verbs = Verbs.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PathPrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PathPrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TextPrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Anchor: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
            val mutable Text: string // (2)
            val mutable FontSize: float32 // (3)
            val mutable FontFamily: string // (4)
            val mutable Align: Fsbar.Hub.Scripting.V1.TextAlign // (5)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Anchor.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 2 -> x.Text <- ValueCodec.String.ReadValue reader
            | 3 -> x.FontSize <- ValueCodec.Float.ReadValue reader
            | 4 -> x.FontFamily <- ValueCodec.String.ReadValue reader
            | 5 -> x.Align <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.TextAlign>.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.TextPrimitive = {
            Anchor = x.Anchor.Build
            Text = x.Text |> orEmptyString
            FontSize = x.FontSize
            FontFamily = x.FontFamily |> orEmptyString
            Align = x.Align
            }

type private _TextPrimitive = TextPrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type TextPrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("anchor")>] Anchor: Fsbar.Hub.Scripting.V1.OverlayPoint option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("text")>] Text: string // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("fontSize")>] FontSize: float32 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("fontFamily")>] FontFamily: string // (4)
    [<System.Text.Json.Serialization.JsonPropertyName("align")>] Align: Fsbar.Hub.Scripting.V1.TextAlign // (5)
    }
    with
    static member Proto : Lazy<ProtoDef<TextPrimitive>> =
        lazy
        // Field Definitions
        let Anchor = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "anchor")
        let Text = FieldCodec.Primitive ValueCodec.String (2, "text")
        let FontSize = FieldCodec.Primitive ValueCodec.Float (3, "fontSize")
        let FontFamily = FieldCodec.Primitive ValueCodec.String (4, "fontFamily")
        let Align = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.TextAlign> (5, "align")
        // Proto Definition Implementation
        { // ProtoDef<TextPrimitive>
            Name = "TextPrimitive"
            Empty = {
                Anchor = Anchor.GetDefault()
                Text = Text.GetDefault()
                FontSize = FontSize.GetDefault()
                FontFamily = FontFamily.GetDefault()
                Align = Align.GetDefault()
                }
            Size = fun (m: TextPrimitive) ->
                0
                + Anchor.CalcFieldSize m.Anchor
                + Text.CalcFieldSize m.Text
                + FontSize.CalcFieldSize m.FontSize
                + FontFamily.CalcFieldSize m.FontFamily
                + Align.CalcFieldSize m.Align
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: TextPrimitive) ->
                Anchor.WriteField w m.Anchor
                Text.WriteField w m.Text
                FontSize.WriteField w m.FontSize
                FontFamily.WriteField w m.FontFamily
                Align.WriteField w m.Align
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.TextPrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeAnchor = Anchor.WriteJsonField o
                let writeText = Text.WriteJsonField o
                let writeFontSize = FontSize.WriteJsonField o
                let writeFontFamily = FontFamily.WriteJsonField o
                let writeAlign = Align.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: TextPrimitive) =
                    writeAnchor w m.Anchor
                    writeText w m.Text
                    writeFontSize w m.FontSize
                    writeFontFamily w m.FontFamily
                    writeAlign w m.Align
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : TextPrimitive =
                    match kvPair.Key with
                    | "anchor" -> { value with Anchor = Anchor.ReadJsonField kvPair.Value }
                    | "text" -> { value with Text = Text.ReadJsonField kvPair.Value }
                    | "fontSize" -> { value with FontSize = FontSize.ReadJsonField kvPair.Value }
                    | "fontFamily" -> { value with FontFamily = FontFamily.ReadJsonField kvPair.Value }
                    | "align" -> { value with Align = Align.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _TextPrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._TextPrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ImagePrimitive =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Anchor: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPoint> // (1)
            val mutable Width: int // (2)
            val mutable Height: int // (3)
            val mutable Bytes: FsGrpc.Bytes // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Anchor.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint>.ReadValue reader)
            | 2 -> x.Width <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Height <- ValueCodec.Int32.ReadValue reader
            | 4 -> x.Bytes <- ValueCodec.Bytes.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ImagePrimitive = {
            Anchor = x.Anchor.Build
            Width = x.Width
            Height = x.Height
            Bytes = x.Bytes
            }

type private _ImagePrimitive = ImagePrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ImagePrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("anchor")>] Anchor: Fsbar.Hub.Scripting.V1.OverlayPoint option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("width")>] Width: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("height")>] Height: int // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("bytes")>] Bytes: FsGrpc.Bytes // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<ImagePrimitive>> =
        lazy
        // Field Definitions
        let Anchor = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPoint> (1, "anchor")
        let Width = FieldCodec.Primitive ValueCodec.Int32 (2, "width")
        let Height = FieldCodec.Primitive ValueCodec.Int32 (3, "height")
        let Bytes = FieldCodec.Primitive ValueCodec.Bytes (4, "bytes")
        // Proto Definition Implementation
        { // ProtoDef<ImagePrimitive>
            Name = "ImagePrimitive"
            Empty = {
                Anchor = Anchor.GetDefault()
                Width = Width.GetDefault()
                Height = Height.GetDefault()
                Bytes = Bytes.GetDefault()
                }
            Size = fun (m: ImagePrimitive) ->
                0
                + Anchor.CalcFieldSize m.Anchor
                + Width.CalcFieldSize m.Width
                + Height.CalcFieldSize m.Height
                + Bytes.CalcFieldSize m.Bytes
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ImagePrimitive) ->
                Anchor.WriteField w m.Anchor
                Width.WriteField w m.Width
                Height.WriteField w m.Height
                Bytes.WriteField w m.Bytes
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ImagePrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeAnchor = Anchor.WriteJsonField o
                let writeWidth = Width.WriteJsonField o
                let writeHeight = Height.WriteJsonField o
                let writeBytes = Bytes.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ImagePrimitive) =
                    writeAnchor w m.Anchor
                    writeWidth w m.Width
                    writeHeight w m.Height
                    writeBytes w m.Bytes
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ImagePrimitive =
                    match kvPair.Key with
                    | "anchor" -> { value with Anchor = Anchor.ReadJsonField kvPair.Value }
                    | "width" -> { value with Width = Width.ReadJsonField kvPair.Value }
                    | "height" -> { value with Height = Height.ReadJsonField kvPair.Value }
                    | "bytes" -> { value with Bytes = Bytes.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ImagePrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ImagePrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OverlayPrimitive =

    [<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.OneofConverter<PrimitiveCase>>)>]
    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    [<StructuralEquality;StructuralComparison>]
    [<RequireQualifiedAccess>]
    type PrimitiveCase =
    | None
    | [<System.Text.Json.Serialization.JsonPropertyName("line")>] Line of Fsbar.Hub.Scripting.V1.LinePrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("polyline")>] Polyline of Fsbar.Hub.Scripting.V1.PolylinePrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("polygon")>] Polygon of Fsbar.Hub.Scripting.V1.PolygonPrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("rectangle")>] Rectangle of Fsbar.Hub.Scripting.V1.RectanglePrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("circle")>] Circle of Fsbar.Hub.Scripting.V1.CirclePrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("path")>] Path of Fsbar.Hub.Scripting.V1.PathPrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("text")>] Text of Fsbar.Hub.Scripting.V1.TextPrimitive
    | [<System.Text.Json.Serialization.JsonPropertyName("image")>] Image of Fsbar.Hub.Scripting.V1.ImagePrimitive
    with
        static member OneofCodec : Lazy<OneofCodec<PrimitiveCase>> = 
            lazy
            let Line = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.LinePrimitive> (3, "line")
            let Polyline = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolylinePrimitive> (4, "polyline")
            let Polygon = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolygonPrimitive> (5, "polygon")
            let Rectangle = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.RectanglePrimitive> (6, "rectangle")
            let Circle = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.CirclePrimitive> (7, "circle")
            let Path = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PathPrimitive> (8, "path")
            let Text = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.TextPrimitive> (9, "text")
            let Image = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ImagePrimitive> (10, "image")
            let Primitive = FieldCodec.Oneof "primitive" (FSharp.Collections.Map [
                ("line", fun node -> PrimitiveCase.Line (Line.ReadJsonField node))
                ("polyline", fun node -> PrimitiveCase.Polyline (Polyline.ReadJsonField node))
                ("polygon", fun node -> PrimitiveCase.Polygon (Polygon.ReadJsonField node))
                ("rectangle", fun node -> PrimitiveCase.Rectangle (Rectangle.ReadJsonField node))
                ("circle", fun node -> PrimitiveCase.Circle (Circle.ReadJsonField node))
                ("path", fun node -> PrimitiveCase.Path (Path.ReadJsonField node))
                ("text", fun node -> PrimitiveCase.Text (Text.ReadJsonField node))
                ("image", fun node -> PrimitiveCase.Image (Image.ReadJsonField node))
                ])
            Primitive

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Space: Fsbar.Hub.Scripting.V1.CoordinateSpace // (1)
            val mutable Style: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayStyle> // (2)
            val mutable Primitive: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase>
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Space <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.CoordinateSpace>.ReadValue reader
            | 2 -> x.Style.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayStyle>.ReadValue reader)
            | 3 -> x.Primitive.Set (PrimitiveCase.Line (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LinePrimitive>.ReadValue reader))
            | 4 -> x.Primitive.Set (PrimitiveCase.Polyline (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolylinePrimitive>.ReadValue reader))
            | 5 -> x.Primitive.Set (PrimitiveCase.Polygon (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolygonPrimitive>.ReadValue reader))
            | 6 -> x.Primitive.Set (PrimitiveCase.Rectangle (ValueCodec.Message<Fsbar.Hub.Scripting.V1.RectanglePrimitive>.ReadValue reader))
            | 7 -> x.Primitive.Set (PrimitiveCase.Circle (ValueCodec.Message<Fsbar.Hub.Scripting.V1.CirclePrimitive>.ReadValue reader))
            | 8 -> x.Primitive.Set (PrimitiveCase.Path (ValueCodec.Message<Fsbar.Hub.Scripting.V1.PathPrimitive>.ReadValue reader))
            | 9 -> x.Primitive.Set (PrimitiveCase.Text (ValueCodec.Message<Fsbar.Hub.Scripting.V1.TextPrimitive>.ReadValue reader))
            | 10 -> x.Primitive.Set (PrimitiveCase.Image (ValueCodec.Message<Fsbar.Hub.Scripting.V1.ImagePrimitive>.ReadValue reader))
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.OverlayPrimitive = {
            Space = x.Space
            Style = x.Style.Build
            Primitive = x.Primitive.Build |> (Option.defaultValue PrimitiveCase.None)
            }

type private _OverlayPrimitive = OverlayPrimitive
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type OverlayPrimitive = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("space")>] Space: Fsbar.Hub.Scripting.V1.CoordinateSpace // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("style")>] Style: Fsbar.Hub.Scripting.V1.OverlayStyle option // (2)
    Primitive: Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase
    }
    with
    static member Proto : Lazy<ProtoDef<OverlayPrimitive>> =
        lazy
        // Field Definitions
        let Space = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.CoordinateSpace> (1, "space")
        let Style = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayStyle> (2, "style")
        let Line = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.LinePrimitive> (3, "line")
        let Polyline = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolylinePrimitive> (4, "polyline")
        let Polygon = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PolygonPrimitive> (5, "polygon")
        let Rectangle = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.RectanglePrimitive> (6, "rectangle")
        let Circle = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.CirclePrimitive> (7, "circle")
        let Path = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.PathPrimitive> (8, "path")
        let Text = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.TextPrimitive> (9, "text")
        let Image = FieldCodec.OneofCase "primitive" ValueCodec.Message<Fsbar.Hub.Scripting.V1.ImagePrimitive> (10, "image")
        let Primitive = FieldCodec.Oneof "primitive" (FSharp.Collections.Map [
            ("line", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Line (Line.ReadJsonField node))
            ("polyline", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polyline (Polyline.ReadJsonField node))
            ("polygon", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polygon (Polygon.ReadJsonField node))
            ("rectangle", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Rectangle (Rectangle.ReadJsonField node))
            ("circle", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Circle (Circle.ReadJsonField node))
            ("path", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Path (Path.ReadJsonField node))
            ("text", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Text (Text.ReadJsonField node))
            ("image", fun node -> Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Image (Image.ReadJsonField node))
            ])
        // Proto Definition Implementation
        { // ProtoDef<OverlayPrimitive>
            Name = "OverlayPrimitive"
            Empty = {
                Space = Space.GetDefault()
                Style = Style.GetDefault()
                Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.None
                }
            Size = fun (m: OverlayPrimitive) ->
                0
                + Space.CalcFieldSize m.Space
                + Style.CalcFieldSize m.Style
                + match m.Primitive with
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.None -> 0
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Line v -> Line.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polyline v -> Polyline.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polygon v -> Polygon.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Rectangle v -> Rectangle.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Circle v -> Circle.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Path v -> Path.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Text v -> Text.CalcFieldSize v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Image v -> Image.CalcFieldSize v
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: OverlayPrimitive) ->
                Space.WriteField w m.Space
                Style.WriteField w m.Style
                (match m.Primitive with
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.None -> ()
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Line v -> Line.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polyline v -> Polyline.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polygon v -> Polygon.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Rectangle v -> Rectangle.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Circle v -> Circle.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Path v -> Path.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Text v -> Text.WriteField w v
                | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Image v -> Image.WriteField w v
                )
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.OverlayPrimitive.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeSpace = Space.WriteJsonField o
                let writeStyle = Style.WriteJsonField o
                let writePrimitiveNone = Primitive.WriteJsonNoneCase o
                let writeLine = Line.WriteJsonField o
                let writePolyline = Polyline.WriteJsonField o
                let writePolygon = Polygon.WriteJsonField o
                let writeRectangle = Rectangle.WriteJsonField o
                let writeCircle = Circle.WriteJsonField o
                let writePath = Path.WriteJsonField o
                let writeText = Text.WriteJsonField o
                let writeImage = Image.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: OverlayPrimitive) =
                    writeSpace w m.Space
                    writeStyle w m.Style
                    (match m.Primitive with
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.None -> writePrimitiveNone w
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Line v -> writeLine w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polyline v -> writePolyline w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polygon v -> writePolygon w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Rectangle v -> writeRectangle w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Circle v -> writeCircle w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Path v -> writePath w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Text v -> writeText w v
                    | Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Image v -> writeImage w v
                    )
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : OverlayPrimitive =
                    match kvPair.Key with
                    | "space" -> { value with Space = Space.ReadJsonField kvPair.Value }
                    | "style" -> { value with Style = Style.ReadJsonField kvPair.Value }
                    | "line" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Line (Line.ReadJsonField kvPair.Value) }
                    | "polyline" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polyline (Polyline.ReadJsonField kvPair.Value) }
                    | "polygon" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Polygon (Polygon.ReadJsonField kvPair.Value) }
                    | "rectangle" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Rectangle (Rectangle.ReadJsonField kvPair.Value) }
                    | "circle" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Circle (Circle.ReadJsonField kvPair.Value) }
                    | "path" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Path (Path.ReadJsonField kvPair.Value) }
                    | "text" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Text (Text.ReadJsonField kvPair.Value) }
                    | "image" -> { value with Primitive = Fsbar.Hub.Scripting.V1.OverlayPrimitive.PrimitiveCase.Image (Image.ReadJsonField kvPair.Value) }
                    | "primitive" -> { value with Primitive = Primitive.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _OverlayPrimitive.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._OverlayPrimitive.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OverlayLayerWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
            val mutable ZHint: int // (2)
            val mutable Primitives: RepeatedBuilder<Fsbar.Hub.Scripting.V1.OverlayPrimitive> // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | 2 -> x.ZHint <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.Primitives.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPrimitive>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.OverlayLayerWire = {
            Name = x.Name |> orEmptyString
            ZHint = x.ZHint
            Primitives = x.Primitives.Build
            }

type private _OverlayLayerWire = OverlayLayerWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type OverlayLayerWire = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("zHint")>] ZHint: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("primitives")>] Primitives: Fsbar.Hub.Scripting.V1.OverlayPrimitive list // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<OverlayLayerWire>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        let ZHint = FieldCodec.Primitive ValueCodec.Int32 (2, "zHint")
        let Primitives = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayPrimitive> (3, "primitives")
        // Proto Definition Implementation
        { // ProtoDef<OverlayLayerWire>
            Name = "OverlayLayerWire"
            Empty = {
                Name = Name.GetDefault()
                ZHint = ZHint.GetDefault()
                Primitives = Primitives.GetDefault()
                }
            Size = fun (m: OverlayLayerWire) ->
                0
                + Name.CalcFieldSize m.Name
                + ZHint.CalcFieldSize m.ZHint
                + Primitives.CalcFieldSize m.Primitives
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: OverlayLayerWire) ->
                Name.WriteField w m.Name
                ZHint.WriteField w m.ZHint
                Primitives.WriteField w m.Primitives
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.OverlayLayerWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let writeZHint = ZHint.WriteJsonField o
                let writePrimitives = Primitives.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: OverlayLayerWire) =
                    writeName w m.Name
                    writeZHint w m.ZHint
                    writePrimitives w m.Primitives
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : OverlayLayerWire =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | "zHint" -> { value with ZHint = ZHint.ReadJsonField kvPair.Value }
                    | "primitives" -> { value with Primitives = Primitives.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _OverlayLayerWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._OverlayLayerWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module OverlayLayerDescriptor =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
            val mutable ZHint: int // (2)
            val mutable UploadedAtUnixMs: int64 // (3)
            val mutable PrimitiveCount: int // (4)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | 2 -> x.ZHint <- ValueCodec.Int32.ReadValue reader
            | 3 -> x.UploadedAtUnixMs <- ValueCodec.Int64.ReadValue reader
            | 4 -> x.PrimitiveCount <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor = {
            Name = x.Name |> orEmptyString
            ZHint = x.ZHint
            UploadedAtUnixMs = x.UploadedAtUnixMs
            PrimitiveCount = x.PrimitiveCount
            }

type private _OverlayLayerDescriptor = OverlayLayerDescriptor
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type OverlayLayerDescriptor = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("zHint")>] ZHint: int // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("uploadedAtUnixMs")>] UploadedAtUnixMs: int64 // (3)
    [<System.Text.Json.Serialization.JsonPropertyName("primitiveCount")>] PrimitiveCount: int // (4)
    }
    with
    static member Proto : Lazy<ProtoDef<OverlayLayerDescriptor>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        let ZHint = FieldCodec.Primitive ValueCodec.Int32 (2, "zHint")
        let UploadedAtUnixMs = FieldCodec.Primitive ValueCodec.Int64 (3, "uploadedAtUnixMs")
        let PrimitiveCount = FieldCodec.Primitive ValueCodec.Int32 (4, "primitiveCount")
        // Proto Definition Implementation
        { // ProtoDef<OverlayLayerDescriptor>
            Name = "OverlayLayerDescriptor"
            Empty = {
                Name = Name.GetDefault()
                ZHint = ZHint.GetDefault()
                UploadedAtUnixMs = UploadedAtUnixMs.GetDefault()
                PrimitiveCount = PrimitiveCount.GetDefault()
                }
            Size = fun (m: OverlayLayerDescriptor) ->
                0
                + Name.CalcFieldSize m.Name
                + ZHint.CalcFieldSize m.ZHint
                + UploadedAtUnixMs.CalcFieldSize m.UploadedAtUnixMs
                + PrimitiveCount.CalcFieldSize m.PrimitiveCount
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: OverlayLayerDescriptor) ->
                Name.WriteField w m.Name
                ZHint.WriteField w m.ZHint
                UploadedAtUnixMs.WriteField w m.UploadedAtUnixMs
                PrimitiveCount.WriteField w m.PrimitiveCount
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let writeZHint = ZHint.WriteJsonField o
                let writeUploadedAtUnixMs = UploadedAtUnixMs.WriteJsonField o
                let writePrimitiveCount = PrimitiveCount.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: OverlayLayerDescriptor) =
                    writeName w m.Name
                    writeZHint w m.ZHint
                    writeUploadedAtUnixMs w m.UploadedAtUnixMs
                    writePrimitiveCount w m.PrimitiveCount
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : OverlayLayerDescriptor =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | "zHint" -> { value with ZHint = ZHint.ReadJsonField kvPair.Value }
                    | "uploadedAtUnixMs" -> { value with UploadedAtUnixMs = UploadedAtUnixMs.ReadJsonField kvPair.Value }
                    | "primitiveCount" -> { value with PrimitiveCount = PrimitiveCount.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _OverlayLayerDescriptor.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._OverlayLayerDescriptor.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PutLayerRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Layer: OptionBuilder<Fsbar.Hub.Scripting.V1.OverlayLayerWire> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Layer.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayLayerWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PutLayerRequest = {
            Layer = x.Layer.Build
            }

type private _PutLayerRequest = PutLayerRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PutLayerRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("layer")>] Layer: Fsbar.Hub.Scripting.V1.OverlayLayerWire option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<PutLayerRequest>> =
        lazy
        // Field Definitions
        let Layer = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayLayerWire> (1, "layer")
        // Proto Definition Implementation
        { // ProtoDef<PutLayerRequest>
            Name = "PutLayerRequest"
            Empty = {
                Layer = Layer.GetDefault()
                }
            Size = fun (m: PutLayerRequest) ->
                0
                + Layer.CalcFieldSize m.Layer
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PutLayerRequest) ->
                Layer.WriteField w m.Layer
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PutLayerRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeLayer = Layer.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PutLayerRequest) =
                    writeLayer w m.Layer
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PutLayerRequest =
                    match kvPair.Key with
                    | "layer" -> { value with Layer = Layer.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PutLayerRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PutLayerRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module PutLayerResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable ValidationErrors: RepeatedBuilder<string> // (2)
            val mutable ExceededCap: string // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.ValidationErrors.Add (ValueCodec.String.ReadValue reader)
            | 3 -> x.ExceededCap <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.PutLayerResponse = {
            Result = x.Result.Build
            ValidationErrors = x.ValidationErrors.Build
            ExceededCap = x.ExceededCap |> orEmptyString
            }

type private _PutLayerResponse = PutLayerResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type PutLayerResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("validationErrors")>] ValidationErrors: string list // (2)
    [<System.Text.Json.Serialization.JsonPropertyName("exceededCap")>] ExceededCap: string // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<PutLayerResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let ValidationErrors = FieldCodec.Repeated ValueCodec.String (2, "validationErrors")
        let ExceededCap = FieldCodec.Primitive ValueCodec.String (3, "exceededCap")
        // Proto Definition Implementation
        { // ProtoDef<PutLayerResponse>
            Name = "PutLayerResponse"
            Empty = {
                Result = Result.GetDefault()
                ValidationErrors = ValidationErrors.GetDefault()
                ExceededCap = ExceededCap.GetDefault()
                }
            Size = fun (m: PutLayerResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + ValidationErrors.CalcFieldSize m.ValidationErrors
                + ExceededCap.CalcFieldSize m.ExceededCap
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: PutLayerResponse) ->
                Result.WriteField w m.Result
                ValidationErrors.WriteField w m.ValidationErrors
                ExceededCap.WriteField w m.ExceededCap
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.PutLayerResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeValidationErrors = ValidationErrors.WriteJsonField o
                let writeExceededCap = ExceededCap.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: PutLayerResponse) =
                    writeResult w m.Result
                    writeValidationErrors w m.ValidationErrors
                    writeExceededCap w m.ExceededCap
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : PutLayerResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "validationErrors" -> { value with ValidationErrors = ValidationErrors.ReadJsonField kvPair.Value }
                    | "exceededCap" -> { value with ExceededCap = ExceededCap.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _PutLayerResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._PutLayerResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DeleteLayerRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Name: string // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Name <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.DeleteLayerRequest = {
            Name = x.Name |> orEmptyString
            }

type private _DeleteLayerRequest = DeleteLayerRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DeleteLayerRequest = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("name")>] Name: string // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<DeleteLayerRequest>> =
        lazy
        // Field Definitions
        let Name = FieldCodec.Primitive ValueCodec.String (1, "name")
        // Proto Definition Implementation
        { // ProtoDef<DeleteLayerRequest>
            Name = "DeleteLayerRequest"
            Empty = {
                Name = Name.GetDefault()
                }
            Size = fun (m: DeleteLayerRequest) ->
                0
                + Name.CalcFieldSize m.Name
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DeleteLayerRequest) ->
                Name.WriteField w m.Name
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.DeleteLayerRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeName = Name.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DeleteLayerRequest) =
                    writeName w m.Name
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DeleteLayerRequest =
                    match kvPair.Key with
                    | "name" -> { value with Name = Name.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DeleteLayerRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._DeleteLayerRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module DeleteLayerResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.DeleteLayerResponse = {
            Result = x.Result.Build
            }

type private _DeleteLayerResponse = DeleteLayerResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type DeleteLayerResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<DeleteLayerResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        // Proto Definition Implementation
        { // ProtoDef<DeleteLayerResponse>
            Name = "DeleteLayerResponse"
            Empty = {
                Result = Result.GetDefault()
                }
            Size = fun (m: DeleteLayerResponse) ->
                0
                + Result.CalcFieldSize m.Result
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: DeleteLayerResponse) ->
                Result.WriteField w m.Result
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.DeleteLayerResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: DeleteLayerResponse) =
                    writeResult w m.Result
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : DeleteLayerResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _DeleteLayerResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._DeleteLayerResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListLayersRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ListLayersRequest.empty

[<StructuralEquality;StructuralComparison>]
type ListLayersRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ListLayersRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ListLayersRequest>
            Name = "ListLayersRequest"
            Empty = ListLayersRequest.empty
            Size = fun (m: ListLayersRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListLayersRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ListLayersRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ListLayersRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ListLayersResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Layers: RepeatedBuilder<Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor> // (1)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Layers.Add (ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ListLayersResponse = {
            Layers = x.Layers.Build
            }

type private _ListLayersResponse = ListLayersResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ListLayersResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("layers")>] Layers: Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor list // (1)
    }
    with
    static member Proto : Lazy<ProtoDef<ListLayersResponse>> =
        lazy
        // Field Definitions
        let Layers = FieldCodec.Repeated ValueCodec.Message<Fsbar.Hub.Scripting.V1.OverlayLayerDescriptor> (1, "layers")
        // Proto Definition Implementation
        { // ProtoDef<ListLayersResponse>
            Name = "ListLayersResponse"
            Empty = {
                Layers = Layers.GetDefault()
                }
            Size = fun (m: ListLayersResponse) ->
                0
                + Layers.CalcFieldSize m.Layers
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ListLayersResponse) ->
                Layers.WriteField w m.Layers
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ListLayersResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeLayers = Layers.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ListLayersResponse) =
                    writeLayers w m.Layers
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ListLayersResponse =
                    match kvPair.Key with
                    | "layers" -> { value with Layers = Layers.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ListLayersResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ListLayersResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ClearLayersRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | _ -> reader.SkipLastField()
        member x.Build = ClearLayersRequest.empty

[<StructuralEquality;StructuralComparison>]
type ClearLayersRequest = | Unused
    with
    static member empty = Unused 
    static member Proto : Lazy<ProtoDef<ClearLayersRequest>> =
        lazy
        // Proto Definition Implementation
        { // ProtoDef<ClearLayersRequest>
            Name = "ClearLayersRequest"
            Empty = ClearLayersRequest.empty
            Size = fun (m: ClearLayersRequest) ->
                0
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ClearLayersRequest) ->
                ()
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable tag = 0
                while read r &tag do
                    r.SkipLastField()
                ClearLayersRequest.empty
            EncodeJson = fun _ _ _ -> ()
            DecodeJson = fun _ -> ClearLayersRequest.empty
        }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ClearLayersResponse =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Result: OptionBuilder<Fsbar.Hub.Scripting.V1.MutationResult> // (1)
            val mutable ClearedCount: int // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Result.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult>.ReadValue reader)
            | 2 -> x.ClearedCount <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ClearLayersResponse = {
            Result = x.Result.Build
            ClearedCount = x.ClearedCount
            }

type private _ClearLayersResponse = ClearLayersResponse
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type ClearLayersResponse = {
    // Field Declarations
    [<System.Text.Json.Serialization.JsonPropertyName("result")>] Result: Fsbar.Hub.Scripting.V1.MutationResult option // (1)
    [<System.Text.Json.Serialization.JsonPropertyName("clearedCount")>] ClearedCount: int // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<ClearLayersResponse>> =
        lazy
        // Field Definitions
        let Result = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.MutationResult> (1, "result")
        let ClearedCount = FieldCodec.Primitive ValueCodec.Int32 (2, "clearedCount")
        // Proto Definition Implementation
        { // ProtoDef<ClearLayersResponse>
            Name = "ClearLayersResponse"
            Empty = {
                Result = Result.GetDefault()
                ClearedCount = ClearedCount.GetDefault()
                }
            Size = fun (m: ClearLayersResponse) ->
                0
                + Result.CalcFieldSize m.Result
                + ClearedCount.CalcFieldSize m.ClearedCount
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ClearLayersResponse) ->
                Result.WriteField w m.Result
                ClearedCount.WriteField w m.ClearedCount
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.ClearLayersResponse.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeResult = Result.WriteJsonField o
                let writeClearedCount = ClearedCount.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ClearLayersResponse) =
                    writeResult w m.Result
                    writeClearedCount w m.ClearedCount
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : ClearLayersResponse =
                    match kvPair.Key with
                    | "result" -> { value with Result = Result.ReadJsonField kvPair.Value }
                    | "clearedCount" -> { value with ClearedCount = ClearedCount.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _ClearLayersResponse.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._ClearLayersResponse.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LogFilterWire =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable Categories: RepeatedBuilder<Fsbar.Hub.Scripting.V1.LogCategory> // (1)
            val mutable MinSeverity: Fsbar.Hub.Scripting.V1.LogSeverity // (2)
            val mutable PresetName: string // (3)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.Categories.AddRange ((ValueCodec.Packed ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogCategory>).ReadValue reader)
            | 2 -> x.MinSeverity <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogSeverity>.ReadValue reader
            | 3 -> x.PresetName <- ValueCodec.String.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LogFilterWire = {
            Categories = x.Categories.Build
            MinSeverity = x.MinSeverity
            PresetName = x.PresetName |> orEmptyString
            }

/// <summary>
/// Client-side filter request. Sent as the first message (initial
/// subscription) and on every subsequent update.
/// </summary>
type private _LogFilterWire = LogFilterWire
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LogFilterWire = {
    // Field Declarations
    /// <summary>
    /// Category whitelist. Empty list = "all categories" per the default
    /// filter (FR-005a). Presence of LOG_CATEGORY_UNSPECIFIED →
    /// INVALID_ARGUMENT (FR-007).
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("categories")>] Categories: Fsbar.Hub.Scripting.V1.LogCategory list // (1)
    /// <summary>Severity floor. LOG_SEVERITY_UNSPECIFIED = server default = INFO.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("minSeverity")>] MinSeverity: Fsbar.Hub.Scripting.V1.LogSeverity // (2)
    /// <summary>
    /// Optional hub-shipped preset name (e.g. "admin-channel",
    /// "session-lifecycle", "scripting-wire"). Empty string = no preset.
    /// When both preset_name and an explicit categories list are supplied,
    /// the explicit list overrides the preset (US5 AS2).
    /// Unknown preset name → INVALID_ARGUMENT.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("presetName")>] PresetName: string // (3)
    }
    with
    static member Proto : Lazy<ProtoDef<LogFilterWire>> =
        lazy
        // Field Definitions
        let Categories = FieldCodec.Primitive (ValueCodec.Packed ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogCategory>) (1, "categories")
        let MinSeverity = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogSeverity> (2, "minSeverity")
        let PresetName = FieldCodec.Primitive ValueCodec.String (3, "presetName")
        // Proto Definition Implementation
        { // ProtoDef<LogFilterWire>
            Name = "LogFilterWire"
            Empty = {
                Categories = Categories.GetDefault()
                MinSeverity = MinSeverity.GetDefault()
                PresetName = PresetName.GetDefault()
                }
            Size = fun (m: LogFilterWire) ->
                0
                + Categories.CalcFieldSize m.Categories
                + MinSeverity.CalcFieldSize m.MinSeverity
                + PresetName.CalcFieldSize m.PresetName
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LogFilterWire) ->
                Categories.WriteField w m.Categories
                MinSeverity.WriteField w m.MinSeverity
                PresetName.WriteField w m.PresetName
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LogFilterWire.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeCategories = Categories.WriteJsonField o
                let writeMinSeverity = MinSeverity.WriteJsonField o
                let writePresetName = PresetName.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LogFilterWire) =
                    writeCategories w m.Categories
                    writeMinSeverity w m.MinSeverity
                    writePresetName w m.PresetName
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LogFilterWire =
                    match kvPair.Key with
                    | "categories" -> { value with Categories = Categories.ReadJsonField kvPair.Value }
                    | "minSeverity" -> { value with MinSeverity = MinSeverity.ReadJsonField kvPair.Value }
                    | "presetName" -> { value with PresetName = PresetName.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LogFilterWire.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LogFilterWire.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module StreamHubLogRequest =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable ClientLabel: string // (1)
            val mutable Filter: OptionBuilder<Fsbar.Hub.Scripting.V1.LogFilterWire> // (2)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.ClientLabel <- ValueCodec.String.ReadValue reader
            | 2 -> x.Filter.Set (ValueCodec.Message<Fsbar.Hub.Scripting.V1.LogFilterWire>.ReadValue reader)
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.StreamHubLogRequest = {
            ClientLabel = x.ClientLabel |> orEmptyString
            Filter = x.Filter.Build
            }

type private _StreamHubLogRequest = StreamHubLogRequest
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type StreamHubLogRequest = {
    // Field Declarations
    /// <summary>
    /// Optional human-readable label for the connected client; surfaces in
    /// Hub-side diagnostics. Ignored on filter-update messages after the
    /// first request.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("clientLabel")>] ClientLabel: string // (1)
    /// <summary>
    /// Effective filter. On the first message, the filter becomes the
    /// subscription's active policy. On subsequent messages, it replaces
    /// the policy atomically.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("filter")>] Filter: Fsbar.Hub.Scripting.V1.LogFilterWire option // (2)
    }
    with
    static member Proto : Lazy<ProtoDef<StreamHubLogRequest>> =
        lazy
        // Field Definitions
        let ClientLabel = FieldCodec.Primitive ValueCodec.String (1, "clientLabel")
        let Filter = FieldCodec.Optional ValueCodec.Message<Fsbar.Hub.Scripting.V1.LogFilterWire> (2, "filter")
        // Proto Definition Implementation
        { // ProtoDef<StreamHubLogRequest>
            Name = "StreamHubLogRequest"
            Empty = {
                ClientLabel = ClientLabel.GetDefault()
                Filter = Filter.GetDefault()
                }
            Size = fun (m: StreamHubLogRequest) ->
                0
                + ClientLabel.CalcFieldSize m.ClientLabel
                + Filter.CalcFieldSize m.Filter
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: StreamHubLogRequest) ->
                ClientLabel.WriteField w m.ClientLabel
                Filter.WriteField w m.Filter
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.StreamHubLogRequest.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeClientLabel = ClientLabel.WriteJsonField o
                let writeFilter = Filter.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: StreamHubLogRequest) =
                    writeClientLabel w m.ClientLabel
                    writeFilter w m.Filter
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : StreamHubLogRequest =
                    match kvPair.Key with
                    | "clientLabel" -> { value with ClientLabel = ClientLabel.ReadJsonField kvPair.Value }
                    | "filter" -> { value with Filter = Filter.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _StreamHubLogRequest.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._StreamHubLogRequest.Proto.Value.Empty

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LogEntryMessage =

    [<System.Runtime.CompilerServices.IsByRefLike>]
    type Builder =
        struct
            val mutable TimestampUnixMs: int64 // (1)
            val mutable Severity: Fsbar.Hub.Scripting.V1.LogSeverity // (2)
            val mutable Category: Fsbar.Hub.Scripting.V1.LogCategory // (3)
            val mutable Message: string // (4)
            val mutable CorrelationId: string // (5)
            val mutable SessionId: string // (6)
            val mutable ScriptingClientId: string // (7)
            val mutable Sequence: uint64 // (8)
            val mutable DroppedSinceLast: int // (9)
        end
        with
        member x.Put ((tag, reader): int * Reader) =
            match tag with
            | 1 -> x.TimestampUnixMs <- ValueCodec.Int64.ReadValue reader
            | 2 -> x.Severity <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogSeverity>.ReadValue reader
            | 3 -> x.Category <- ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogCategory>.ReadValue reader
            | 4 -> x.Message <- ValueCodec.String.ReadValue reader
            | 5 -> x.CorrelationId <- ValueCodec.String.ReadValue reader
            | 6 -> x.SessionId <- ValueCodec.String.ReadValue reader
            | 7 -> x.ScriptingClientId <- ValueCodec.String.ReadValue reader
            | 8 -> x.Sequence <- ValueCodec.UInt64.ReadValue reader
            | 9 -> x.DroppedSinceLast <- ValueCodec.Int32.ReadValue reader
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.LogEntryMessage = {
            TimestampUnixMs = x.TimestampUnixMs
            Severity = x.Severity
            Category = x.Category
            Message = x.Message |> orEmptyString
            CorrelationId = x.CorrelationId |> orEmptyString
            SessionId = x.SessionId |> orEmptyString
            ScriptingClientId = x.ScriptingClientId |> orEmptyString
            Sequence = x.Sequence
            DroppedSinceLast = x.DroppedSinceLast
            }

type private _LogEntryMessage = LogEntryMessage
[<System.Text.Json.Serialization.JsonConverter(typeof<FsGrpc.Json.MessageConverter>)>]
[<FsGrpc.Protobuf.Message>]
[<StructuralEquality;StructuralComparison>]
type LogEntryMessage = {
    // Field Declarations
    /// <summary>Hub-local UTC timestamp, Unix ms.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("timestampUnixMs")>] TimestampUnixMs: int64 // (1)
    /// <summary>
    /// Severity. DEBUG only reaches clients that explicitly lowered the
    /// severity floor via filter (FR-005a).
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("severity")>] Severity: Fsbar.Hub.Scripting.V1.LogSeverity // (2)
    /// <summary>Source subsystem.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("category")>] Category: Fsbar.Hub.Scripting.V1.LogCategory // (3)
    /// <summary>
    /// Human-readable message. UTF-8, ≤ 8 KiB including a trailing
    /// " …[truncated N bytes]" marker on messages that were truncated
    /// at the Hub (FR-012a).
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("message")>] Message: string // (4)
    /// <summary>
    /// Correlation ID of the RPC that owns this entry (FR-009/009a).
    /// Empty string when the entry is not tied to any RPC.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("correlationId")>] CorrelationId: string // (5)
    /// <summary>
    /// Session ID when the entry is about a specific RunningSession
    /// (FR-010). Empty string otherwise.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("sessionId")>] SessionId: string // (6)
    /// <summary>
    /// Scripting-client ID when the entry is about a specific connected
    /// client (FR-010). Empty string otherwise.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("scriptingClientId")>] ScriptingClientId: string // (7)
    /// <summary>Per-subscriber monotonic sequence number, starting at 1.</summary>
    [<System.Text.Json.Serialization.JsonPropertyName("sequence")>] Sequence: uint64 // (8)
    /// <summary>
    /// Count of entries the server dropped to this subscriber since the
    /// last successful delivery (FR-012). Reset to 0 on every delivery.
    /// </summary>
    [<System.Text.Json.Serialization.JsonPropertyName("droppedSinceLast")>] DroppedSinceLast: int // (9)
    }
    with
    static member Proto : Lazy<ProtoDef<LogEntryMessage>> =
        lazy
        // Field Definitions
        let TimestampUnixMs = FieldCodec.Primitive ValueCodec.Int64 (1, "timestampUnixMs")
        let Severity = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogSeverity> (2, "severity")
        let Category = FieldCodec.Primitive ValueCodec.Enum<Fsbar.Hub.Scripting.V1.LogCategory> (3, "category")
        let Message = FieldCodec.Primitive ValueCodec.String (4, "message")
        let CorrelationId = FieldCodec.Primitive ValueCodec.String (5, "correlationId")
        let SessionId = FieldCodec.Primitive ValueCodec.String (6, "sessionId")
        let ScriptingClientId = FieldCodec.Primitive ValueCodec.String (7, "scriptingClientId")
        let Sequence = FieldCodec.Primitive ValueCodec.UInt64 (8, "sequence")
        let DroppedSinceLast = FieldCodec.Primitive ValueCodec.Int32 (9, "droppedSinceLast")
        // Proto Definition Implementation
        { // ProtoDef<LogEntryMessage>
            Name = "LogEntryMessage"
            Empty = {
                TimestampUnixMs = TimestampUnixMs.GetDefault()
                Severity = Severity.GetDefault()
                Category = Category.GetDefault()
                Message = Message.GetDefault()
                CorrelationId = CorrelationId.GetDefault()
                SessionId = SessionId.GetDefault()
                ScriptingClientId = ScriptingClientId.GetDefault()
                Sequence = Sequence.GetDefault()
                DroppedSinceLast = DroppedSinceLast.GetDefault()
                }
            Size = fun (m: LogEntryMessage) ->
                0
                + TimestampUnixMs.CalcFieldSize m.TimestampUnixMs
                + Severity.CalcFieldSize m.Severity
                + Category.CalcFieldSize m.Category
                + Message.CalcFieldSize m.Message
                + CorrelationId.CalcFieldSize m.CorrelationId
                + SessionId.CalcFieldSize m.SessionId
                + ScriptingClientId.CalcFieldSize m.ScriptingClientId
                + Sequence.CalcFieldSize m.Sequence
                + DroppedSinceLast.CalcFieldSize m.DroppedSinceLast
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: LogEntryMessage) ->
                TimestampUnixMs.WriteField w m.TimestampUnixMs
                Severity.WriteField w m.Severity
                Category.WriteField w m.Category
                Message.WriteField w m.Message
                CorrelationId.WriteField w m.CorrelationId
                SessionId.WriteField w m.SessionId
                ScriptingClientId.WriteField w m.ScriptingClientId
                Sequence.WriteField w m.Sequence
                DroppedSinceLast.WriteField w m.DroppedSinceLast
            Decode = fun (r: Google.Protobuf.CodedInputStream) ->
                let mutable builder = new Fsbar.Hub.Scripting.V1.LogEntryMessage.Builder()
                let mutable tag = 0
                while read r &tag do
                    builder.Put (tag, r)
                builder.Build
            EncodeJson = fun (o: JsonOptions) ->
                let writeTimestampUnixMs = TimestampUnixMs.WriteJsonField o
                let writeSeverity = Severity.WriteJsonField o
                let writeCategory = Category.WriteJsonField o
                let writeMessage = Message.WriteJsonField o
                let writeCorrelationId = CorrelationId.WriteJsonField o
                let writeSessionId = SessionId.WriteJsonField o
                let writeScriptingClientId = ScriptingClientId.WriteJsonField o
                let writeSequence = Sequence.WriteJsonField o
                let writeDroppedSinceLast = DroppedSinceLast.WriteJsonField o
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: LogEntryMessage) =
                    writeTimestampUnixMs w m.TimestampUnixMs
                    writeSeverity w m.Severity
                    writeCategory w m.Category
                    writeMessage w m.Message
                    writeCorrelationId w m.CorrelationId
                    writeSessionId w m.SessionId
                    writeScriptingClientId w m.ScriptingClientId
                    writeSequence w m.Sequence
                    writeDroppedSinceLast w m.DroppedSinceLast
                encode
            DecodeJson = fun (node: System.Text.Json.Nodes.JsonNode) ->
                let update value (kvPair: System.Collections.Generic.KeyValuePair<string,System.Text.Json.Nodes.JsonNode>) : LogEntryMessage =
                    match kvPair.Key with
                    | "timestampUnixMs" -> { value with TimestampUnixMs = TimestampUnixMs.ReadJsonField kvPair.Value }
                    | "severity" -> { value with Severity = Severity.ReadJsonField kvPair.Value }
                    | "category" -> { value with Category = Category.ReadJsonField kvPair.Value }
                    | "message" -> { value with Message = Message.ReadJsonField kvPair.Value }
                    | "correlationId" -> { value with CorrelationId = CorrelationId.ReadJsonField kvPair.Value }
                    | "sessionId" -> { value with SessionId = SessionId.ReadJsonField kvPair.Value }
                    | "scriptingClientId" -> { value with ScriptingClientId = ScriptingClientId.ReadJsonField kvPair.Value }
                    | "sequence" -> { value with Sequence = Sequence.ReadJsonField kvPair.Value }
                    | "droppedSinceLast" -> { value with DroppedSinceLast = DroppedSinceLast.ReadJsonField kvPair.Value }
                    | _ -> value
                Seq.fold update _LogEntryMessage.empty (node.AsObject ())
        }
    static member empty
        with get() = Fsbar.Hub.Scripting.V1._LogEntryMessage.Proto.Value.Empty

module ScriptingService =
    let private __Marshaller__fsbar_hub_scripting_v1_GameFrameMessage = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GameFrameMessage) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SendCommandResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SendCommandResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetSessionStatusResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetSessionStatusResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetUnitDefResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetUnitDefResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_PauseResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.PauseResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ResumeResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ResumeResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetEngineSpeedResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ForceEndMatchResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ForceEndMatchResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SendAdminMessageResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SendAdminMessageResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ConfigureLobbyResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListMapsResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListMapsResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ValidateLobbyResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ValidateLobbyResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_LaunchSessionResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.LaunchSessionResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StopSessionResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StopSessionResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_RenderFrameMessage = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.RenderFrameMessage) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetRenderFrameResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetRenderFrameResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetVizConfigResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetVizConfigResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetVizAttributeResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetVizAttributeResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ToggleOverlayResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ToggleOverlayResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetCameraResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetCameraResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetActiveTabResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetActiveTabResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListPresetsResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListPresetsResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SavePresetResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SavePresetResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_LoadPresetResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.LoadPresetResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_DeletePresetResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.DeletePresetResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListUnitsResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListUnitsResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SelectUnitResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SelectUnitResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetHubSettingsResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetHubSettingsResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetHubSettingsResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetHubSettingsResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_InstallProxyResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.InstallProxyResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_RefreshProxyStatusResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_HubStateSnapshot = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.HubStateSnapshot) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_HubStateEvent = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.HubStateEvent) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_PutLayerResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.PutLayerResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_DeleteLayerResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.DeleteLayerResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListLayersResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListLayersResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ClearLayersResponse = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ClearLayersResponse) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_LogEntryMessage = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.LogEntryMessage) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StreamGameFramesRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StreamGameFramesRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SendCommandRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SendCommandRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetSessionStatusRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetSessionStatusRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetUnitDefRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetUnitDefRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_PauseRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.PauseRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ResumeRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ResumeRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetEngineSpeedRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ForceEndMatchRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ForceEndMatchRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SendAdminMessageRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SendAdminMessageRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ConfigureLobbyRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListMapsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListMapsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ValidateLobbyRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ValidateLobbyRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_LaunchSessionRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.LaunchSessionRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StopSessionRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StopSessionRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StreamRenderFramesRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetRenderFrameRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetRenderFrameRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetVizConfigRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetVizConfigRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetVizAttributeRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetVizAttributeRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ToggleOverlayRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ToggleOverlayRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetCameraRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetCameraRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetActiveTabRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetActiveTabRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListPresetsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListPresetsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SavePresetRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SavePresetRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_LoadPresetRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.LoadPresetRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_DeletePresetRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.DeletePresetRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListUnitsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListUnitsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SelectUnitRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SelectUnitRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetHubSettingsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetHubSettingsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_SetHubSettingsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.SetHubSettingsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_InstallProxyRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.InstallProxyRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_RefreshProxyStatusRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_GetHubStateRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.GetHubStateRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StreamHubStateEventsRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_PutLayerRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.PutLayerRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_DeleteLayerRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.DeleteLayerRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ListLayersRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ListLayersRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_ClearLayersRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.ClearLayersRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Marshaller__fsbar_hub_scripting_v1_StreamHubLogRequest = Grpc.Core.Marshallers.Create(
        (fun (x: Fsbar.Hub.Scripting.V1.StreamHubLogRequest) -> FsGrpc.Protobuf.encode x),
        (fun (arr: byte array) -> FsGrpc.Protobuf.decode arr)
    )
    let private __Method_StreamGameFrames =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.StreamGameFramesRequest,Fsbar.Hub.Scripting.V1.GameFrameMessage>(
            Grpc.Core.MethodType.ServerStreaming,
            "fsbar.hub.scripting.v1.ScriptingService",
            "StreamGameFrames",
            __Marshaller__fsbar_hub_scripting_v1_StreamGameFramesRequest,
            __Marshaller__fsbar_hub_scripting_v1_GameFrameMessage
        )
    let private __Method_SendCommand =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SendCommandRequest,Fsbar.Hub.Scripting.V1.SendCommandResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SendCommand",
            __Marshaller__fsbar_hub_scripting_v1_SendCommandRequest,
            __Marshaller__fsbar_hub_scripting_v1_SendCommandResponse
        )
    let private __Method_GetSessionStatus =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.GetSessionStatusRequest,Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "GetSessionStatus",
            __Marshaller__fsbar_hub_scripting_v1_GetSessionStatusRequest,
            __Marshaller__fsbar_hub_scripting_v1_GetSessionStatusResponse
        )
    let private __Method_GetUnitDef =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.GetUnitDefRequest,Fsbar.Hub.Scripting.V1.GetUnitDefResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "GetUnitDef",
            __Marshaller__fsbar_hub_scripting_v1_GetUnitDefRequest,
            __Marshaller__fsbar_hub_scripting_v1_GetUnitDefResponse
        )
    let private __Method_Pause =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.PauseRequest,Fsbar.Hub.Scripting.V1.PauseResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "Pause",
            __Marshaller__fsbar_hub_scripting_v1_PauseRequest,
            __Marshaller__fsbar_hub_scripting_v1_PauseResponse
        )
    let private __Method_Resume =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ResumeRequest,Fsbar.Hub.Scripting.V1.ResumeResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "Resume",
            __Marshaller__fsbar_hub_scripting_v1_ResumeRequest,
            __Marshaller__fsbar_hub_scripting_v1_ResumeResponse
        )
    let private __Method_SetEngineSpeed =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest,Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetEngineSpeed",
            __Marshaller__fsbar_hub_scripting_v1_SetEngineSpeedRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetEngineSpeedResponse
        )
    let private __Method_ForceEndMatch =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ForceEndMatchRequest,Fsbar.Hub.Scripting.V1.ForceEndMatchResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ForceEndMatch",
            __Marshaller__fsbar_hub_scripting_v1_ForceEndMatchRequest,
            __Marshaller__fsbar_hub_scripting_v1_ForceEndMatchResponse
        )
    let private __Method_SendAdminMessage =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SendAdminMessageRequest,Fsbar.Hub.Scripting.V1.SendAdminMessageResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SendAdminMessage",
            __Marshaller__fsbar_hub_scripting_v1_SendAdminMessageRequest,
            __Marshaller__fsbar_hub_scripting_v1_SendAdminMessageResponse
        )
    let private __Method_ConfigureLobby =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest,Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ConfigureLobby",
            __Marshaller__fsbar_hub_scripting_v1_ConfigureLobbyRequest,
            __Marshaller__fsbar_hub_scripting_v1_ConfigureLobbyResponse
        )
    let private __Method_ListMaps =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ListMapsRequest,Fsbar.Hub.Scripting.V1.ListMapsResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ListMaps",
            __Marshaller__fsbar_hub_scripting_v1_ListMapsRequest,
            __Marshaller__fsbar_hub_scripting_v1_ListMapsResponse
        )
    let private __Method_ValidateLobby =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ValidateLobbyRequest,Fsbar.Hub.Scripting.V1.ValidateLobbyResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ValidateLobby",
            __Marshaller__fsbar_hub_scripting_v1_ValidateLobbyRequest,
            __Marshaller__fsbar_hub_scripting_v1_ValidateLobbyResponse
        )
    let private __Method_LaunchSession =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.LaunchSessionRequest,Fsbar.Hub.Scripting.V1.LaunchSessionResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "LaunchSession",
            __Marshaller__fsbar_hub_scripting_v1_LaunchSessionRequest,
            __Marshaller__fsbar_hub_scripting_v1_LaunchSessionResponse
        )
    let private __Method_StopSession =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.StopSessionRequest,Fsbar.Hub.Scripting.V1.StopSessionResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "StopSession",
            __Marshaller__fsbar_hub_scripting_v1_StopSessionRequest,
            __Marshaller__fsbar_hub_scripting_v1_StopSessionResponse
        )
    let private __Method_StreamRenderFrames =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest,Fsbar.Hub.Scripting.V1.RenderFrameMessage>(
            Grpc.Core.MethodType.ServerStreaming,
            "fsbar.hub.scripting.v1.ScriptingService",
            "StreamRenderFrames",
            __Marshaller__fsbar_hub_scripting_v1_StreamRenderFramesRequest,
            __Marshaller__fsbar_hub_scripting_v1_RenderFrameMessage
        )
    let private __Method_GetRenderFrame =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.GetRenderFrameRequest,Fsbar.Hub.Scripting.V1.GetRenderFrameResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "GetRenderFrame",
            __Marshaller__fsbar_hub_scripting_v1_GetRenderFrameRequest,
            __Marshaller__fsbar_hub_scripting_v1_GetRenderFrameResponse
        )
    let private __Method_SetVizConfig =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetVizConfigRequest,Fsbar.Hub.Scripting.V1.SetVizConfigResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetVizConfig",
            __Marshaller__fsbar_hub_scripting_v1_SetVizConfigRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetVizConfigResponse
        )
    let private __Method_SetVizAttribute =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetVizAttributeRequest,Fsbar.Hub.Scripting.V1.SetVizAttributeResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetVizAttribute",
            __Marshaller__fsbar_hub_scripting_v1_SetVizAttributeRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetVizAttributeResponse
        )
    let private __Method_ToggleOverlay =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ToggleOverlayRequest,Fsbar.Hub.Scripting.V1.ToggleOverlayResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ToggleOverlay",
            __Marshaller__fsbar_hub_scripting_v1_ToggleOverlayRequest,
            __Marshaller__fsbar_hub_scripting_v1_ToggleOverlayResponse
        )
    let private __Method_SetCamera =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetCameraRequest,Fsbar.Hub.Scripting.V1.SetCameraResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetCamera",
            __Marshaller__fsbar_hub_scripting_v1_SetCameraRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetCameraResponse
        )
    let private __Method_SetActiveTab =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetActiveTabRequest,Fsbar.Hub.Scripting.V1.SetActiveTabResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetActiveTab",
            __Marshaller__fsbar_hub_scripting_v1_SetActiveTabRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetActiveTabResponse
        )
    let private __Method_ListPresets =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ListPresetsRequest,Fsbar.Hub.Scripting.V1.ListPresetsResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ListPresets",
            __Marshaller__fsbar_hub_scripting_v1_ListPresetsRequest,
            __Marshaller__fsbar_hub_scripting_v1_ListPresetsResponse
        )
    let private __Method_SavePreset =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SavePresetRequest,Fsbar.Hub.Scripting.V1.SavePresetResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SavePreset",
            __Marshaller__fsbar_hub_scripting_v1_SavePresetRequest,
            __Marshaller__fsbar_hub_scripting_v1_SavePresetResponse
        )
    let private __Method_LoadPreset =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.LoadPresetRequest,Fsbar.Hub.Scripting.V1.LoadPresetResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "LoadPreset",
            __Marshaller__fsbar_hub_scripting_v1_LoadPresetRequest,
            __Marshaller__fsbar_hub_scripting_v1_LoadPresetResponse
        )
    let private __Method_DeletePreset =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.DeletePresetRequest,Fsbar.Hub.Scripting.V1.DeletePresetResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "DeletePreset",
            __Marshaller__fsbar_hub_scripting_v1_DeletePresetRequest,
            __Marshaller__fsbar_hub_scripting_v1_DeletePresetResponse
        )
    let private __Method_ListUnits =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ListUnitsRequest,Fsbar.Hub.Scripting.V1.ListUnitsResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ListUnits",
            __Marshaller__fsbar_hub_scripting_v1_ListUnitsRequest,
            __Marshaller__fsbar_hub_scripting_v1_ListUnitsResponse
        )
    let private __Method_SelectUnit =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SelectUnitRequest,Fsbar.Hub.Scripting.V1.SelectUnitResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SelectUnit",
            __Marshaller__fsbar_hub_scripting_v1_SelectUnitRequest,
            __Marshaller__fsbar_hub_scripting_v1_SelectUnitResponse
        )
    let private __Method_GetHubSettings =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.GetHubSettingsRequest,Fsbar.Hub.Scripting.V1.GetHubSettingsResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "GetHubSettings",
            __Marshaller__fsbar_hub_scripting_v1_GetHubSettingsRequest,
            __Marshaller__fsbar_hub_scripting_v1_GetHubSettingsResponse
        )
    let private __Method_SetHubSettings =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.SetHubSettingsRequest,Fsbar.Hub.Scripting.V1.SetHubSettingsResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "SetHubSettings",
            __Marshaller__fsbar_hub_scripting_v1_SetHubSettingsRequest,
            __Marshaller__fsbar_hub_scripting_v1_SetHubSettingsResponse
        )
    let private __Method_InstallProxy =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.InstallProxyRequest,Fsbar.Hub.Scripting.V1.InstallProxyResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "InstallProxy",
            __Marshaller__fsbar_hub_scripting_v1_InstallProxyRequest,
            __Marshaller__fsbar_hub_scripting_v1_InstallProxyResponse
        )
    let private __Method_RefreshProxyStatus =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest,Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "RefreshProxyStatus",
            __Marshaller__fsbar_hub_scripting_v1_RefreshProxyStatusRequest,
            __Marshaller__fsbar_hub_scripting_v1_RefreshProxyStatusResponse
        )
    let private __Method_GetHubState =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.GetHubStateRequest,Fsbar.Hub.Scripting.V1.HubStateSnapshot>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "GetHubState",
            __Marshaller__fsbar_hub_scripting_v1_GetHubStateRequest,
            __Marshaller__fsbar_hub_scripting_v1_HubStateSnapshot
        )
    let private __Method_StreamHubStateEvents =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest,Fsbar.Hub.Scripting.V1.HubStateEvent>(
            Grpc.Core.MethodType.ServerStreaming,
            "fsbar.hub.scripting.v1.ScriptingService",
            "StreamHubStateEvents",
            __Marshaller__fsbar_hub_scripting_v1_StreamHubStateEventsRequest,
            __Marshaller__fsbar_hub_scripting_v1_HubStateEvent
        )
    let private __Method_PutLayer =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.PutLayerRequest,Fsbar.Hub.Scripting.V1.PutLayerResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "PutLayer",
            __Marshaller__fsbar_hub_scripting_v1_PutLayerRequest,
            __Marshaller__fsbar_hub_scripting_v1_PutLayerResponse
        )
    let private __Method_DeleteLayer =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.DeleteLayerRequest,Fsbar.Hub.Scripting.V1.DeleteLayerResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "DeleteLayer",
            __Marshaller__fsbar_hub_scripting_v1_DeleteLayerRequest,
            __Marshaller__fsbar_hub_scripting_v1_DeleteLayerResponse
        )
    let private __Method_ListLayers =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ListLayersRequest,Fsbar.Hub.Scripting.V1.ListLayersResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ListLayers",
            __Marshaller__fsbar_hub_scripting_v1_ListLayersRequest,
            __Marshaller__fsbar_hub_scripting_v1_ListLayersResponse
        )
    let private __Method_ClearLayers =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.ClearLayersRequest,Fsbar.Hub.Scripting.V1.ClearLayersResponse>(
            Grpc.Core.MethodType.Unary,
            "fsbar.hub.scripting.v1.ScriptingService",
            "ClearLayers",
            __Marshaller__fsbar_hub_scripting_v1_ClearLayersRequest,
            __Marshaller__fsbar_hub_scripting_v1_ClearLayersResponse
        )
    let private __Method_StreamHubLog =
        Grpc.Core.Method<Fsbar.Hub.Scripting.V1.StreamHubLogRequest,Fsbar.Hub.Scripting.V1.LogEntryMessage>(
            Grpc.Core.MethodType.DuplexStreaming,
            "fsbar.hub.scripting.v1.ScriptingService",
            "StreamHubLog",
            __Marshaller__fsbar_hub_scripting_v1_StreamHubLogRequest,
            __Marshaller__fsbar_hub_scripting_v1_LogEntryMessage
        )
    [<AbstractClass>]
    [<Grpc.Core.BindServiceMethod(typeof<ServiceBase>, "BindService")>]
    type ServiceBase() = 
        abstract member StreamGameFrames : Fsbar.Hub.Scripting.V1.StreamGameFramesRequest -> Grpc.Core.IServerStreamWriter<Fsbar.Hub.Scripting.V1.GameFrameMessage> -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task
        abstract member SendCommand : Fsbar.Hub.Scripting.V1.SendCommandRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SendCommandResponse>
        abstract member GetSessionStatus : Fsbar.Hub.Scripting.V1.GetSessionStatusRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>
        abstract member GetUnitDef : Fsbar.Hub.Scripting.V1.GetUnitDefRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetUnitDefResponse>
        abstract member Pause : Fsbar.Hub.Scripting.V1.PauseRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.PauseResponse>
        abstract member Resume : Fsbar.Hub.Scripting.V1.ResumeRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ResumeResponse>
        abstract member SetEngineSpeed : Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse>
        abstract member ForceEndMatch : Fsbar.Hub.Scripting.V1.ForceEndMatchRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ForceEndMatchResponse>
        abstract member SendAdminMessage : Fsbar.Hub.Scripting.V1.SendAdminMessageRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SendAdminMessageResponse>
        abstract member ConfigureLobby : Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse>
        abstract member ListMaps : Fsbar.Hub.Scripting.V1.ListMapsRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ListMapsResponse>
        abstract member ValidateLobby : Fsbar.Hub.Scripting.V1.ValidateLobbyRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ValidateLobbyResponse>
        abstract member LaunchSession : Fsbar.Hub.Scripting.V1.LaunchSessionRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.LaunchSessionResponse>
        abstract member StopSession : Fsbar.Hub.Scripting.V1.StopSessionRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.StopSessionResponse>
        abstract member StreamRenderFrames : Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest -> Grpc.Core.IServerStreamWriter<Fsbar.Hub.Scripting.V1.RenderFrameMessage> -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task
        abstract member GetRenderFrame : Fsbar.Hub.Scripting.V1.GetRenderFrameRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetRenderFrameResponse>
        abstract member SetVizConfig : Fsbar.Hub.Scripting.V1.SetVizConfigRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetVizConfigResponse>
        abstract member SetVizAttribute : Fsbar.Hub.Scripting.V1.SetVizAttributeRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetVizAttributeResponse>
        abstract member ToggleOverlay : Fsbar.Hub.Scripting.V1.ToggleOverlayRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ToggleOverlayResponse>
        abstract member SetCamera : Fsbar.Hub.Scripting.V1.SetCameraRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetCameraResponse>
        abstract member SetActiveTab : Fsbar.Hub.Scripting.V1.SetActiveTabRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetActiveTabResponse>
        abstract member ListPresets : Fsbar.Hub.Scripting.V1.ListPresetsRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ListPresetsResponse>
        abstract member SavePreset : Fsbar.Hub.Scripting.V1.SavePresetRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SavePresetResponse>
        abstract member LoadPreset : Fsbar.Hub.Scripting.V1.LoadPresetRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.LoadPresetResponse>
        abstract member DeletePreset : Fsbar.Hub.Scripting.V1.DeletePresetRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.DeletePresetResponse>
        abstract member ListUnits : Fsbar.Hub.Scripting.V1.ListUnitsRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ListUnitsResponse>
        abstract member SelectUnit : Fsbar.Hub.Scripting.V1.SelectUnitRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SelectUnitResponse>
        abstract member GetHubSettings : Fsbar.Hub.Scripting.V1.GetHubSettingsRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetHubSettingsResponse>
        abstract member SetHubSettings : Fsbar.Hub.Scripting.V1.SetHubSettingsRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SetHubSettingsResponse>
        abstract member InstallProxy : Fsbar.Hub.Scripting.V1.InstallProxyRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.InstallProxyResponse>
        abstract member RefreshProxyStatus : Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse>
        abstract member GetHubState : Fsbar.Hub.Scripting.V1.GetHubStateRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.HubStateSnapshot>
        abstract member StreamHubStateEvents : Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest -> Grpc.Core.IServerStreamWriter<Fsbar.Hub.Scripting.V1.HubStateEvent> -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task
        abstract member PutLayer : Fsbar.Hub.Scripting.V1.PutLayerRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.PutLayerResponse>
        abstract member DeleteLayer : Fsbar.Hub.Scripting.V1.DeleteLayerRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.DeleteLayerResponse>
        abstract member ListLayers : Fsbar.Hub.Scripting.V1.ListLayersRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ListLayersResponse>
        abstract member ClearLayers : Fsbar.Hub.Scripting.V1.ClearLayersRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.ClearLayersResponse>
        abstract member StreamHubLog : Grpc.Core.IAsyncStreamReader<Fsbar.Hub.Scripting.V1.StreamHubLogRequest> -> Grpc.Core.IServerStreamWriter<Fsbar.Hub.Scripting.V1.LogEntryMessage> -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task
        static member BindService (serviceBinder: Grpc.Core.ServiceBinderBase) (serviceImpl: ServiceBase) =
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamGameFramesRequest,Fsbar.Hub.Scripting.V1.GameFrameMessage>>
                | _ -> Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamGameFramesRequest,Fsbar.Hub.Scripting.V1.GameFrameMessage>(serviceImpl.StreamGameFrames)
            serviceBinder.AddMethod(__Method_StreamGameFrames, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SendCommandRequest,Fsbar.Hub.Scripting.V1.SendCommandResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SendCommandRequest,Fsbar.Hub.Scripting.V1.SendCommandResponse>(serviceImpl.SendCommand)
            serviceBinder.AddMethod(__Method_SendCommand, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetSessionStatusRequest,Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetSessionStatusRequest,Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>(serviceImpl.GetSessionStatus)
            serviceBinder.AddMethod(__Method_GetSessionStatus, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetUnitDefRequest,Fsbar.Hub.Scripting.V1.GetUnitDefResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetUnitDefRequest,Fsbar.Hub.Scripting.V1.GetUnitDefResponse>(serviceImpl.GetUnitDef)
            serviceBinder.AddMethod(__Method_GetUnitDef, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.PauseRequest,Fsbar.Hub.Scripting.V1.PauseResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.PauseRequest,Fsbar.Hub.Scripting.V1.PauseResponse>(serviceImpl.Pause)
            serviceBinder.AddMethod(__Method_Pause, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ResumeRequest,Fsbar.Hub.Scripting.V1.ResumeResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ResumeRequest,Fsbar.Hub.Scripting.V1.ResumeResponse>(serviceImpl.Resume)
            serviceBinder.AddMethod(__Method_Resume, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest,Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest,Fsbar.Hub.Scripting.V1.SetEngineSpeedResponse>(serviceImpl.SetEngineSpeed)
            serviceBinder.AddMethod(__Method_SetEngineSpeed, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ForceEndMatchRequest,Fsbar.Hub.Scripting.V1.ForceEndMatchResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ForceEndMatchRequest,Fsbar.Hub.Scripting.V1.ForceEndMatchResponse>(serviceImpl.ForceEndMatch)
            serviceBinder.AddMethod(__Method_ForceEndMatch, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SendAdminMessageRequest,Fsbar.Hub.Scripting.V1.SendAdminMessageResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SendAdminMessageRequest,Fsbar.Hub.Scripting.V1.SendAdminMessageResponse>(serviceImpl.SendAdminMessage)
            serviceBinder.AddMethod(__Method_SendAdminMessage, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest,Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest,Fsbar.Hub.Scripting.V1.ConfigureLobbyResponse>(serviceImpl.ConfigureLobby)
            serviceBinder.AddMethod(__Method_ConfigureLobby, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListMapsRequest,Fsbar.Hub.Scripting.V1.ListMapsResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListMapsRequest,Fsbar.Hub.Scripting.V1.ListMapsResponse>(serviceImpl.ListMaps)
            serviceBinder.AddMethod(__Method_ListMaps, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ValidateLobbyRequest,Fsbar.Hub.Scripting.V1.ValidateLobbyResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ValidateLobbyRequest,Fsbar.Hub.Scripting.V1.ValidateLobbyResponse>(serviceImpl.ValidateLobby)
            serviceBinder.AddMethod(__Method_ValidateLobby, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.LaunchSessionRequest,Fsbar.Hub.Scripting.V1.LaunchSessionResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.LaunchSessionRequest,Fsbar.Hub.Scripting.V1.LaunchSessionResponse>(serviceImpl.LaunchSession)
            serviceBinder.AddMethod(__Method_LaunchSession, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.StopSessionRequest,Fsbar.Hub.Scripting.V1.StopSessionResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.StopSessionRequest,Fsbar.Hub.Scripting.V1.StopSessionResponse>(serviceImpl.StopSession)
            serviceBinder.AddMethod(__Method_StopSession, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest,Fsbar.Hub.Scripting.V1.RenderFrameMessage>>
                | _ -> Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest,Fsbar.Hub.Scripting.V1.RenderFrameMessage>(serviceImpl.StreamRenderFrames)
            serviceBinder.AddMethod(__Method_StreamRenderFrames, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetRenderFrameRequest,Fsbar.Hub.Scripting.V1.GetRenderFrameResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetRenderFrameRequest,Fsbar.Hub.Scripting.V1.GetRenderFrameResponse>(serviceImpl.GetRenderFrame)
            serviceBinder.AddMethod(__Method_GetRenderFrame, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetVizConfigRequest,Fsbar.Hub.Scripting.V1.SetVizConfigResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetVizConfigRequest,Fsbar.Hub.Scripting.V1.SetVizConfigResponse>(serviceImpl.SetVizConfig)
            serviceBinder.AddMethod(__Method_SetVizConfig, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetVizAttributeRequest,Fsbar.Hub.Scripting.V1.SetVizAttributeResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetVizAttributeRequest,Fsbar.Hub.Scripting.V1.SetVizAttributeResponse>(serviceImpl.SetVizAttribute)
            serviceBinder.AddMethod(__Method_SetVizAttribute, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ToggleOverlayRequest,Fsbar.Hub.Scripting.V1.ToggleOverlayResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ToggleOverlayRequest,Fsbar.Hub.Scripting.V1.ToggleOverlayResponse>(serviceImpl.ToggleOverlay)
            serviceBinder.AddMethod(__Method_ToggleOverlay, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetCameraRequest,Fsbar.Hub.Scripting.V1.SetCameraResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetCameraRequest,Fsbar.Hub.Scripting.V1.SetCameraResponse>(serviceImpl.SetCamera)
            serviceBinder.AddMethod(__Method_SetCamera, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetActiveTabRequest,Fsbar.Hub.Scripting.V1.SetActiveTabResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetActiveTabRequest,Fsbar.Hub.Scripting.V1.SetActiveTabResponse>(serviceImpl.SetActiveTab)
            serviceBinder.AddMethod(__Method_SetActiveTab, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListPresetsRequest,Fsbar.Hub.Scripting.V1.ListPresetsResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListPresetsRequest,Fsbar.Hub.Scripting.V1.ListPresetsResponse>(serviceImpl.ListPresets)
            serviceBinder.AddMethod(__Method_ListPresets, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SavePresetRequest,Fsbar.Hub.Scripting.V1.SavePresetResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SavePresetRequest,Fsbar.Hub.Scripting.V1.SavePresetResponse>(serviceImpl.SavePreset)
            serviceBinder.AddMethod(__Method_SavePreset, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.LoadPresetRequest,Fsbar.Hub.Scripting.V1.LoadPresetResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.LoadPresetRequest,Fsbar.Hub.Scripting.V1.LoadPresetResponse>(serviceImpl.LoadPreset)
            serviceBinder.AddMethod(__Method_LoadPreset, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.DeletePresetRequest,Fsbar.Hub.Scripting.V1.DeletePresetResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.DeletePresetRequest,Fsbar.Hub.Scripting.V1.DeletePresetResponse>(serviceImpl.DeletePreset)
            serviceBinder.AddMethod(__Method_DeletePreset, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListUnitsRequest,Fsbar.Hub.Scripting.V1.ListUnitsResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListUnitsRequest,Fsbar.Hub.Scripting.V1.ListUnitsResponse>(serviceImpl.ListUnits)
            serviceBinder.AddMethod(__Method_ListUnits, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SelectUnitRequest,Fsbar.Hub.Scripting.V1.SelectUnitResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SelectUnitRequest,Fsbar.Hub.Scripting.V1.SelectUnitResponse>(serviceImpl.SelectUnit)
            serviceBinder.AddMethod(__Method_SelectUnit, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetHubSettingsRequest,Fsbar.Hub.Scripting.V1.GetHubSettingsResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetHubSettingsRequest,Fsbar.Hub.Scripting.V1.GetHubSettingsResponse>(serviceImpl.GetHubSettings)
            serviceBinder.AddMethod(__Method_GetHubSettings, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetHubSettingsRequest,Fsbar.Hub.Scripting.V1.SetHubSettingsResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.SetHubSettingsRequest,Fsbar.Hub.Scripting.V1.SetHubSettingsResponse>(serviceImpl.SetHubSettings)
            serviceBinder.AddMethod(__Method_SetHubSettings, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.InstallProxyRequest,Fsbar.Hub.Scripting.V1.InstallProxyResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.InstallProxyRequest,Fsbar.Hub.Scripting.V1.InstallProxyResponse>(serviceImpl.InstallProxy)
            serviceBinder.AddMethod(__Method_InstallProxy, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest,Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest,Fsbar.Hub.Scripting.V1.RefreshProxyStatusResponse>(serviceImpl.RefreshProxyStatus)
            serviceBinder.AddMethod(__Method_RefreshProxyStatus, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetHubStateRequest,Fsbar.Hub.Scripting.V1.HubStateSnapshot>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.GetHubStateRequest,Fsbar.Hub.Scripting.V1.HubStateSnapshot>(serviceImpl.GetHubState)
            serviceBinder.AddMethod(__Method_GetHubState, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest,Fsbar.Hub.Scripting.V1.HubStateEvent>>
                | _ -> Grpc.Core.ServerStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest,Fsbar.Hub.Scripting.V1.HubStateEvent>(serviceImpl.StreamHubStateEvents)
            serviceBinder.AddMethod(__Method_StreamHubStateEvents, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.PutLayerRequest,Fsbar.Hub.Scripting.V1.PutLayerResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.PutLayerRequest,Fsbar.Hub.Scripting.V1.PutLayerResponse>(serviceImpl.PutLayer)
            serviceBinder.AddMethod(__Method_PutLayer, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.DeleteLayerRequest,Fsbar.Hub.Scripting.V1.DeleteLayerResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.DeleteLayerRequest,Fsbar.Hub.Scripting.V1.DeleteLayerResponse>(serviceImpl.DeleteLayer)
            serviceBinder.AddMethod(__Method_DeleteLayer, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListLayersRequest,Fsbar.Hub.Scripting.V1.ListLayersResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ListLayersRequest,Fsbar.Hub.Scripting.V1.ListLayersResponse>(serviceImpl.ListLayers)
            serviceBinder.AddMethod(__Method_ListLayers, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ClearLayersRequest,Fsbar.Hub.Scripting.V1.ClearLayersResponse>>
                | _ -> Grpc.Core.UnaryServerMethod<Fsbar.Hub.Scripting.V1.ClearLayersRequest,Fsbar.Hub.Scripting.V1.ClearLayersResponse>(serviceImpl.ClearLayers)
            serviceBinder.AddMethod(__Method_ClearLayers, serviceMethodOrNull) |> ignore
            let serviceMethodOrNull =
                match box serviceImpl with
                | null -> Unchecked.defaultof<Grpc.Core.DuplexStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamHubLogRequest,Fsbar.Hub.Scripting.V1.LogEntryMessage>>
                | _ -> Grpc.Core.DuplexStreamingServerMethod<Fsbar.Hub.Scripting.V1.StreamHubLogRequest,Fsbar.Hub.Scripting.V1.LogEntryMessage>(serviceImpl.StreamHubLog)
            serviceBinder.AddMethod(__Method_StreamHubLog, serviceMethodOrNull) |> ignore
    type Client = 
        inherit Grpc.Core.ClientBase<Client>
        new () = { inherit Grpc.Core.ClientBase<Client>() }
        new (channel: Grpc.Core.ChannelBase) = { inherit Grpc.Core.ClientBase<Client>(channel) }
        new (callInvoker: Grpc.Core.CallInvoker) = { inherit Grpc.Core.ClientBase<Client>(callInvoker) }
        new (configuration: Grpc.Core.ClientBase.ClientBaseConfiguration) = { inherit Grpc.Core.ClientBase<Client>(configuration) }
        override this.NewInstance (configuration: Grpc.Core.ClientBase.ClientBaseConfiguration) = Client(configuration)
        member this.StreamGameFramesAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.StreamGameFramesRequest) =
            this.CallInvoker.AsyncServerStreamingCall(__Method_StreamGameFrames, Unchecked.defaultof<string>, callOptions, request)
        member this.SendCommand (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SendCommandRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SendCommand, Unchecked.defaultof<string>, callOptions, request)
        member this.SendCommandAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SendCommandRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SendCommand, Unchecked.defaultof<string>, callOptions, request)
        member this.GetSessionStatus (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetSessionStatusRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_GetSessionStatus, Unchecked.defaultof<string>, callOptions, request)
        member this.GetSessionStatusAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetSessionStatusRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_GetSessionStatus, Unchecked.defaultof<string>, callOptions, request)
        member this.GetUnitDef (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetUnitDefRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_GetUnitDef, Unchecked.defaultof<string>, callOptions, request)
        member this.GetUnitDefAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetUnitDefRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_GetUnitDef, Unchecked.defaultof<string>, callOptions, request)
        member this.Pause (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.PauseRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_Pause, Unchecked.defaultof<string>, callOptions, request)
        member this.PauseAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.PauseRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_Pause, Unchecked.defaultof<string>, callOptions, request)
        member this.Resume (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ResumeRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_Resume, Unchecked.defaultof<string>, callOptions, request)
        member this.ResumeAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ResumeRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_Resume, Unchecked.defaultof<string>, callOptions, request)
        member this.SetEngineSpeed (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetEngineSpeed, Unchecked.defaultof<string>, callOptions, request)
        member this.SetEngineSpeedAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetEngineSpeedRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetEngineSpeed, Unchecked.defaultof<string>, callOptions, request)
        member this.ForceEndMatch (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ForceEndMatchRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ForceEndMatch, Unchecked.defaultof<string>, callOptions, request)
        member this.ForceEndMatchAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ForceEndMatchRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ForceEndMatch, Unchecked.defaultof<string>, callOptions, request)
        member this.SendAdminMessage (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SendAdminMessageRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SendAdminMessage, Unchecked.defaultof<string>, callOptions, request)
        member this.SendAdminMessageAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SendAdminMessageRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SendAdminMessage, Unchecked.defaultof<string>, callOptions, request)
        member this.ConfigureLobby (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ConfigureLobby, Unchecked.defaultof<string>, callOptions, request)
        member this.ConfigureLobbyAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ConfigureLobbyRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ConfigureLobby, Unchecked.defaultof<string>, callOptions, request)
        member this.ListMaps (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListMapsRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ListMaps, Unchecked.defaultof<string>, callOptions, request)
        member this.ListMapsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListMapsRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ListMaps, Unchecked.defaultof<string>, callOptions, request)
        member this.ValidateLobby (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ValidateLobbyRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ValidateLobby, Unchecked.defaultof<string>, callOptions, request)
        member this.ValidateLobbyAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ValidateLobbyRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ValidateLobby, Unchecked.defaultof<string>, callOptions, request)
        member this.LaunchSession (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.LaunchSessionRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_LaunchSession, Unchecked.defaultof<string>, callOptions, request)
        member this.LaunchSessionAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.LaunchSessionRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_LaunchSession, Unchecked.defaultof<string>, callOptions, request)
        member this.StopSession (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.StopSessionRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_StopSession, Unchecked.defaultof<string>, callOptions, request)
        member this.StopSessionAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.StopSessionRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_StopSession, Unchecked.defaultof<string>, callOptions, request)
        member this.StreamRenderFramesAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.StreamRenderFramesRequest) =
            this.CallInvoker.AsyncServerStreamingCall(__Method_StreamRenderFrames, Unchecked.defaultof<string>, callOptions, request)
        member this.GetRenderFrame (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetRenderFrameRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_GetRenderFrame, Unchecked.defaultof<string>, callOptions, request)
        member this.GetRenderFrameAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetRenderFrameRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_GetRenderFrame, Unchecked.defaultof<string>, callOptions, request)
        member this.SetVizConfig (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetVizConfigRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetVizConfig, Unchecked.defaultof<string>, callOptions, request)
        member this.SetVizConfigAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetVizConfigRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetVizConfig, Unchecked.defaultof<string>, callOptions, request)
        member this.SetVizAttribute (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetVizAttributeRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetVizAttribute, Unchecked.defaultof<string>, callOptions, request)
        member this.SetVizAttributeAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetVizAttributeRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetVizAttribute, Unchecked.defaultof<string>, callOptions, request)
        member this.ToggleOverlay (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ToggleOverlayRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ToggleOverlay, Unchecked.defaultof<string>, callOptions, request)
        member this.ToggleOverlayAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ToggleOverlayRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ToggleOverlay, Unchecked.defaultof<string>, callOptions, request)
        member this.SetCamera (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetCameraRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetCamera, Unchecked.defaultof<string>, callOptions, request)
        member this.SetCameraAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetCameraRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetCamera, Unchecked.defaultof<string>, callOptions, request)
        member this.SetActiveTab (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetActiveTabRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetActiveTab, Unchecked.defaultof<string>, callOptions, request)
        member this.SetActiveTabAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetActiveTabRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetActiveTab, Unchecked.defaultof<string>, callOptions, request)
        member this.ListPresets (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListPresetsRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ListPresets, Unchecked.defaultof<string>, callOptions, request)
        member this.ListPresetsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListPresetsRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ListPresets, Unchecked.defaultof<string>, callOptions, request)
        member this.SavePreset (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SavePresetRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SavePreset, Unchecked.defaultof<string>, callOptions, request)
        member this.SavePresetAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SavePresetRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SavePreset, Unchecked.defaultof<string>, callOptions, request)
        member this.LoadPreset (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.LoadPresetRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_LoadPreset, Unchecked.defaultof<string>, callOptions, request)
        member this.LoadPresetAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.LoadPresetRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_LoadPreset, Unchecked.defaultof<string>, callOptions, request)
        member this.DeletePreset (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.DeletePresetRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_DeletePreset, Unchecked.defaultof<string>, callOptions, request)
        member this.DeletePresetAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.DeletePresetRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_DeletePreset, Unchecked.defaultof<string>, callOptions, request)
        member this.ListUnits (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListUnitsRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ListUnits, Unchecked.defaultof<string>, callOptions, request)
        member this.ListUnitsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListUnitsRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ListUnits, Unchecked.defaultof<string>, callOptions, request)
        member this.SelectUnit (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SelectUnitRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SelectUnit, Unchecked.defaultof<string>, callOptions, request)
        member this.SelectUnitAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SelectUnitRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SelectUnit, Unchecked.defaultof<string>, callOptions, request)
        member this.GetHubSettings (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetHubSettingsRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_GetHubSettings, Unchecked.defaultof<string>, callOptions, request)
        member this.GetHubSettingsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetHubSettingsRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_GetHubSettings, Unchecked.defaultof<string>, callOptions, request)
        member this.SetHubSettings (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetHubSettingsRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_SetHubSettings, Unchecked.defaultof<string>, callOptions, request)
        member this.SetHubSettingsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.SetHubSettingsRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_SetHubSettings, Unchecked.defaultof<string>, callOptions, request)
        member this.InstallProxy (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.InstallProxyRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_InstallProxy, Unchecked.defaultof<string>, callOptions, request)
        member this.InstallProxyAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.InstallProxyRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_InstallProxy, Unchecked.defaultof<string>, callOptions, request)
        member this.RefreshProxyStatus (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_RefreshProxyStatus, Unchecked.defaultof<string>, callOptions, request)
        member this.RefreshProxyStatusAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.RefreshProxyStatusRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_RefreshProxyStatus, Unchecked.defaultof<string>, callOptions, request)
        member this.GetHubState (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetHubStateRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_GetHubState, Unchecked.defaultof<string>, callOptions, request)
        member this.GetHubStateAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.GetHubStateRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_GetHubState, Unchecked.defaultof<string>, callOptions, request)
        member this.StreamHubStateEventsAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.StreamHubStateEventsRequest) =
            this.CallInvoker.AsyncServerStreamingCall(__Method_StreamHubStateEvents, Unchecked.defaultof<string>, callOptions, request)
        member this.PutLayer (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.PutLayerRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_PutLayer, Unchecked.defaultof<string>, callOptions, request)
        member this.PutLayerAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.PutLayerRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_PutLayer, Unchecked.defaultof<string>, callOptions, request)
        member this.DeleteLayer (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.DeleteLayerRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_DeleteLayer, Unchecked.defaultof<string>, callOptions, request)
        member this.DeleteLayerAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.DeleteLayerRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_DeleteLayer, Unchecked.defaultof<string>, callOptions, request)
        member this.ListLayers (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListLayersRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ListLayers, Unchecked.defaultof<string>, callOptions, request)
        member this.ListLayersAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ListLayersRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ListLayers, Unchecked.defaultof<string>, callOptions, request)
        member this.ClearLayers (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ClearLayersRequest) =
            this.CallInvoker.BlockingUnaryCall(__Method_ClearLayers, Unchecked.defaultof<string>, callOptions, request)
        member this.ClearLayersAsync (callOptions: Grpc.Core.CallOptions) (request: Fsbar.Hub.Scripting.V1.ClearLayersRequest) =
            this.CallInvoker.AsyncUnaryCall(__Method_ClearLayers, Unchecked.defaultof<string>, callOptions, request)
        member this.StreamHubLogAsync (callOptions: Grpc.Core.CallOptions) =
            this.CallInvoker.AsyncDuplexStreamingCall(__Method_StreamHubLog, Unchecked.defaultof<string>, callOptions)
