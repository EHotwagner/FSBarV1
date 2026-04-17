namespace rec Fsbar.Hub.Scripting.V1
open FsGrpc.Protobuf
open Google.Protobuf
#nowarn "40"
#nowarn "1182"


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
            | _ -> reader.SkipLastField()
        member x.Build : Fsbar.Hub.Scripting.V1.ActiveSession = {
            SessionId = x.SessionId |> orEmptyString
            MapName = x.MapName |> orEmptyString
            Mode = x.Mode |> orEmptyString
            EngineSpeed = x.EngineSpeed
            Paused = x.Paused
            StartedAtUnixMs = x.StartedAtUnixMs
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
                }
            Size = fun (m: ActiveSession) ->
                0
                + SessionId.CalcFieldSize m.SessionId
                + MapName.CalcFieldSize m.MapName
                + Mode.CalcFieldSize m.Mode
                + EngineSpeed.CalcFieldSize m.EngineSpeed
                + Paused.CalcFieldSize m.Paused
                + StartedAtUnixMs.CalcFieldSize m.StartedAtUnixMs
            Encode = fun (w: Google.Protobuf.CodedOutputStream) (m: ActiveSession) ->
                SessionId.WriteField w m.SessionId
                MapName.WriteField w m.MapName
                Mode.WriteField w m.Mode
                EngineSpeed.WriteField w m.EngineSpeed
                Paused.WriteField w m.Paused
                StartedAtUnixMs.WriteField w m.StartedAtUnixMs
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
                let encode (w: System.Text.Json.Utf8JsonWriter) (m: ActiveSession) =
                    writeSessionId w m.SessionId
                    writeMapName w m.MapName
                    writeMode w m.Mode
                    writeEngineSpeed w m.EngineSpeed
                    writePaused w m.Paused
                    writeStartedAtUnixMs w m.StartedAtUnixMs
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
    [<AbstractClass>]
    [<Grpc.Core.BindServiceMethod(typeof<ServiceBase>, "BindService")>]
    type ServiceBase() = 
        abstract member StreamGameFrames : Fsbar.Hub.Scripting.V1.StreamGameFramesRequest -> Grpc.Core.IServerStreamWriter<Fsbar.Hub.Scripting.V1.GameFrameMessage> -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task
        abstract member SendCommand : Fsbar.Hub.Scripting.V1.SendCommandRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.SendCommandResponse>
        abstract member GetSessionStatus : Fsbar.Hub.Scripting.V1.GetSessionStatusRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetSessionStatusResponse>
        abstract member GetUnitDef : Fsbar.Hub.Scripting.V1.GetUnitDefRequest -> Grpc.Core.ServerCallContext -> System.Threading.Tasks.Task<Fsbar.Hub.Scripting.V1.GetUnitDefResponse>
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
