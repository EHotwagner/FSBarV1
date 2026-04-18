namespace FSBar.Hub

// `FSBar.Client.SessionState.Error` collides with `Result.Error` at
// namespace scope, so we import only what we need and reference
// FSBar.Client types fully-qualified below.
open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open Fsbar.Hub.Scripting.V1

module ScriptingHub =

    type ScriptingHubOptions = {
        FrameBufferCapacity: int
        MaxCumulativeDrops: int
    }

    let defaults: ScriptingHubOptions = {
        FrameBufferCapacity = 16
        MaxCumulativeDrops = 32
    }

    type ConnectedClientInfo = {
        ClientId: Guid
        ClientLabel: string
        RemoteEndpoint: string
        AttachedAtUnixMs: int64
        CumulativeDroppedFrames: int
    }

    /// One connected client's server-side state.
    type private ClientRegistration = {
        Id: Guid
        Label: string
        RemoteEndpoint: string
        Channel: Channel<GameFrameMessage>
        mutable DropCount: int
        AttachedAtUnixMs: int64
        mutable Sequence: uint64
        Cancellation: CancellationTokenSource
    }

    let private unixMillis () =
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    /// Convert the F# admin-channel status DU into its gRPC wire form.
    let private toStatusInfo
            (status: HubEvents.AdminChannelStatus option)
            : AdminChannelStatusInfo option =
        match status with
        | None -> None
        | Some HubEvents.Attached ->
            Some { State = AdminChannelStatusInfo.State.Attached; Reason = "" }
        | Some (HubEvents.Unavailable reason) ->
            Some { State = AdminChannelStatusInfo.State.Unavailable; Reason = reason }
        | Some (HubEvents.Lost reason) ->
            Some { State = AdminChannelStatusInfo.State.Lost; Reason = reason }

    /// Convert an AdminChannelHost SubmitOutcome into the AdminSubmitResult wire form.
    let private toSubmitResult
            (outcome: AdminChannelHost.SubmitOutcome)
            (status: HubEvents.AdminChannelStatus option)
            : AdminSubmitResult =
        let wireStatus = toStatusInfo status
        match outcome with
        | AdminChannelHost.Sent ->
            { Outcome = AdminSubmitResult.Outcome.Sent
              DroppedCount = 0
              Reason = ""
              AdminChannelStatus = wireStatus }
        | AdminChannelHost.Coalesced dropped ->
            { Outcome = AdminSubmitResult.Outcome.Coalesced
              DroppedCount = dropped
              Reason = ""
              AdminChannelStatus = wireStatus }
        | AdminChannelHost.Rejected reason ->
            { Outcome = AdminSubmitResult.Outcome.Rejected
              DroppedCount = 0
              Reason = reason
              AdminChannelStatus = wireStatus }

    /// Construct a REJECTED AdminSubmitResult from a local-validation reason —
    /// used when the RPC caller bypasses the SessionManager entirely (e.g. no session).
    let private rejectedResult (reason: string) (status: HubEvents.AdminChannelStatus option) : AdminSubmitResult =
        { Outcome = AdminSubmitResult.Outcome.Rejected
          DroppedCount = 0
          Reason = reason
          AdminChannelStatus = toStatusInfo status }

    // --- Feature 040 US1 — lobby wire ↔ F# mappers ----------------------

    let private wireToLobbyMode (m: LobbyMode) : LobbyConfig.GameMode =
        match m with
        | LobbyMode.Skirmish -> LobbyConfig.Skirmish
        | LobbyMode.Ffa -> LobbyConfig.FFA
        | LobbyMode.Team -> LobbyConfig.Team
        | LobbyMode.Unspecified | _ -> LobbyConfig.Skirmish

    let private lobbyModeToWire (m: LobbyConfig.GameMode) : LobbyMode =
        match m with
        | LobbyConfig.Skirmish -> LobbyMode.Skirmish
        | LobbyConfig.FFA -> LobbyMode.Ffa
        | LobbyConfig.Team -> LobbyMode.Team

    let private wireToSeat (seat: SeatWire) : LobbyConfig.Seat =
        let kind =
            match seat.Kind with
            | SeatKind.Human ->
                LobbyConfig.HumanSeat(seat.HumanName)
            | SeatKind.Ai
            | SeatKind.Unspecified
            | _ ->
                LobbyConfig.AiSeat(seat.AiName, Map.empty)
        { Kind = kind
          Side = seat.Side
          Handicap = int (round seat.Handicap) }

    let private seatToWire (seat: LobbyConfig.Seat) : SeatWire =
        match seat.Kind with
        | LobbyConfig.AiSeat(name, _) ->
            { Kind = SeatKind.Ai
              Side = seat.Side
              Handicap = float32 seat.Handicap
              AiName = name
              HumanName = "" }
        | LobbyConfig.HumanSeat name ->
            { Kind = SeatKind.Human
              Side = seat.Side
              Handicap = float32 seat.Handicap
              AiName = ""
              HumanName = name }

    let private wireToTeam (t: TeamWire) : LobbyConfig.Team =
        { AllyTeamId = t.AllyTeamId
          Seats = t.Seats |> List.map wireToSeat }

    let private teamToWire (t: LobbyConfig.Team) : TeamWire =
        { AllyTeamId = t.AllyTeamId
          Seats = t.Seats |> List.map seatToWire }

    let private wireToLobby (w: LobbyConfigWire) : LobbyConfig.LobbyConfig =
        { MapName = w.MapName
          Mode = wireToLobbyMode w.Mode
          EngineSpeed = w.EngineSpeed
          LaunchGraphicalViewer = w.LaunchGraphicalViewer
          Teams = w.Teams |> List.map wireToTeam
          Spectators =
              w.Spectators
              |> List.map (fun s -> { LobbyConfig.Spectator.PlayerName = s.Name }) }

    let private lobbyToWire (lobby: LobbyConfig.LobbyConfig) : LobbyConfigWire =
        { MapName = lobby.MapName
          Mode = lobbyModeToWire lobby.Mode
          EngineSpeed = lobby.EngineSpeed
          LaunchGraphicalViewer = lobby.LaunchGraphicalViewer
          Teams = lobby.Teams |> List.map teamToWire
          Spectators =
              lobby.Spectators
              |> List.map (fun s -> ({ Name = s.PlayerName } : SpectatorWire)) }

    let private sentResult () : MutationResult =
        { Outcome = SubmitOutcome.Sent; Reason = "" }

    let private rejectedMutation (reason: string) : MutationResult =
        { Outcome = SubmitOutcome.Rejected; Reason = reason }

    // --- US5 — HubState snapshot + event projection -------------------

    let private hubTabToWire (t: FSBar.Hub.HubTab) : HubTab =
        match t with
        | FSBar.Hub.HubTab.Setup -> HubTab.Setup
        | FSBar.Hub.HubTab.Viewer -> HubTab.Viewer
        | FSBar.Hub.HubTab.Units -> HubTab.Units
        | FSBar.Hub.HubTab.Style -> HubTab.Style
        | FSBar.Hub.HubTab.Cfg -> HubTab.Cfg
        | FSBar.Hub.HubTab.Grpc -> HubTab.Grpc

    let private viewerCameraToWire (c: ViewerCamera) : ViewerCameraWire =
        { Scale = c.Scale
          OriginX = c.OriginX
          OriginY = c.OriginY
          AutoFit = c.AutoFit }

    let private attributeValueToWire
            (v: FSBar.Viz.AttributeValue) : VizAttributeValue =
        let vc =
            match v with
            | FSBar.Viz.BoolValue b -> VizAttributeValue.ValueCase.BoolValue b
            | FSBar.Viz.IntValue i -> VizAttributeValue.ValueCase.IntValue i
            | FSBar.Viz.FloatValue f -> VizAttributeValue.ValueCase.FloatValue f
            | FSBar.Viz.StringValue s -> VizAttributeValue.ValueCase.StringValue s
            | FSBar.Viz.ColorRgbaValue c -> VizAttributeValue.ValueCase.ColorRgba c
            | FSBar.Viz.StringListValue xs ->
                VizAttributeValue.ValueCase.StringListValue
                    ({ Values = xs } : StringList)
        { Value = vc }

    let private objToAttributeValueWire (value: obj) : FSBar.Viz.AttributeValue =
        match value with
        | :? bool as b -> FSBar.Viz.BoolValue b
        | :? int as i -> FSBar.Viz.IntValue i
        | :? int64 as i -> FSBar.Viz.IntValue (int i)
        | :? float32 as f -> FSBar.Viz.FloatValue (float f)
        | :? float as f -> FSBar.Viz.FloatValue f
        | :? string as s -> FSBar.Viz.StringValue s
        | :? uint32 as c -> FSBar.Viz.ColorRgbaValue c
        | :? SkiaSharp.SKColor as sk ->
            let rgba =
                ((uint32 sk.Red) <<< 24)
                ||| ((uint32 sk.Green) <<< 16)
                ||| ((uint32 sk.Blue) <<< 8)
                ||| (uint32 sk.Alpha)
            FSBar.Viz.ColorRgbaValue rgba
        | :? (string array) as xs -> FSBar.Viz.StringListValue (List.ofArray xs)
        | _ -> FSBar.Viz.StringValue (value.GetType().FullName)

    let private vizConfigToWire (config: FSBar.Viz.VizConfig) : VizConfigWire =
        let attrs =
            FSBar.Viz.ConfigDescriptors.all
            |> List.map (fun desc ->
                let v = desc.Get config
                desc.Key, attributeValueToWire (objToAttributeValueWire v))
            |> Map.ofList
        { Attributes = attrs }

    let private factionKeyToString (k: FSBar.Hub.FactionFilterKey) : string =
        match k with
        | FSBar.Hub.Armada -> "Armada"
        | FSBar.Hub.Cortex -> "Cortex"
        | FSBar.Hub.Legion -> "Legion"
        | FSBar.Hub.Raptors -> "Raptors"
        | FSBar.Hub.Scavengers -> "Scavengers"
        | FSBar.Hub.Neutral -> "Neutral"

    let private encyclopediaToWire (s: EncyclopediaSelection) : EncyclopediaSelectionWire =
        { FactionFilter = s.FactionFilter |> Set.toList |> List.map factionKeyToString
          SelectedDefId = s.SelectedDefId }

    // --- US6 — overlay wire ↔ F# mapping ------------------------------

    /// Derive a stable client id from the gRPC peer string. `null` /
    /// empty peer falls back to a fresh Guid (no cross-RPC coherence).
    let private clientIdFromPeer (peer: string) : Guid =
        if String.IsNullOrEmpty(peer) then Guid.NewGuid()
        else
            let bytes =
                System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes(peer))
            Guid(bytes)

    let private wireToOverlayPoint (p: OverlayPoint option) : FSBar.Hub.OverlayPoint =
        match p with
        | Some op -> { X = op.X; Y = op.Y }
        | None -> { X = 0.0f; Y = 0.0f }

    let private wireToCoordinateSpace (c: CoordinateSpace) : FSBar.Hub.CoordinateSpace =
        match c with
        | CoordinateSpace.Screen -> FSBar.Hub.Screen
        | CoordinateSpace.World
        | CoordinateSpace.Unspecified
        | _ -> FSBar.Hub.World

    let private wireToTextAlign (a: TextAlign) : FSBar.Hub.TextAlign =
        match a with
        | TextAlign.Center -> FSBar.Hub.Center
        | TextAlign.Right -> FSBar.Hub.Right
        | TextAlign.Left
        | TextAlign.Unspecified
        | _ -> FSBar.Hub.Left

    let private wireToOverlayStyle (s: OverlayStyle option) : FSBar.Hub.OverlayStyle =
        match s with
        | Some w ->
            { StrokeColorRgba = w.StrokeColorRgba
              StrokeWidth = w.StrokeWidth
              FillColorRgba = if w.HasFill then Some w.FillColorRgba else None
              Opacity = if w.Opacity = 0.0f then 1.0f else w.Opacity
              Dash = if List.isEmpty w.Dash then None else Some (Array.ofList w.Dash) }
        | None ->
            { StrokeColorRgba = 0xFFFFFFFFu
              StrokeWidth = 1.0f
              FillColorRgba = None
              Opacity = 1.0f
              Dash = None }

    let private wireToPathVerb (v: PathVerb) : FSBar.Hub.PathVerb option =
        match v.Verb with
        | PathVerb.VerbCase.MoveTo p -> Some (FSBar.Hub.MoveTo (wireToOverlayPoint (Some p)))
        | PathVerb.VerbCase.LineTo p -> Some (FSBar.Hub.LineTo (wireToOverlayPoint (Some p)))
        | PathVerb.VerbCase.CubicTo c ->
            Some (FSBar.Hub.CubicTo(
                wireToOverlayPoint c.C1,
                wireToOverlayPoint c.C2,
                wireToOverlayPoint c.P))
        | PathVerb.VerbCase.Close _ -> Some FSBar.Hub.Close
        | PathVerb.VerbCase.None -> None

    let private wireToOverlayPrimitive
            (p: OverlayPrimitive)
            : Result<FSBar.Hub.OverlayPrimitive, string> =
        let space = wireToCoordinateSpace p.Space
        let style = wireToOverlayStyle p.Style
        match p.Primitive with
        | OverlayPrimitive.PrimitiveCase.Line lp ->
            Ok (FSBar.Hub.Line(
                wireToOverlayPoint lp.From,
                wireToOverlayPoint lp.To,
                style, space))
        | OverlayPrimitive.PrimitiveCase.Polyline pp ->
            Ok (FSBar.Hub.Polyline(
                pp.Points |> List.map (fun pt -> wireToOverlayPoint (Some pt)),
                style, space))
        | OverlayPrimitive.PrimitiveCase.Polygon pp ->
            Ok (FSBar.Hub.Polygon(
                pp.Points |> List.map (fun pt -> wireToOverlayPoint (Some pt)),
                style, space))
        | OverlayPrimitive.PrimitiveCase.Rectangle rp ->
            Ok (FSBar.Hub.Rectangle(
                rp.X, rp.Y, rp.Width, rp.Height, rp.CornerRadius, style, space))
        | OverlayPrimitive.PrimitiveCase.Circle cp ->
            Ok (FSBar.Hub.Circle(
                wireToOverlayPoint cp.Center, cp.Radius, style, space))
        | OverlayPrimitive.PrimitiveCase.Path pp ->
            let verbs = pp.Verbs |> List.choose wireToPathVerb
            Ok (FSBar.Hub.Path(verbs, style, space))
        | OverlayPrimitive.PrimitiveCase.Text tp ->
            Ok (FSBar.Hub.Text(
                wireToOverlayPoint tp.Anchor,
                tp.Text,
                tp.FontSize,
                tp.FontFamily,
                wireToTextAlign tp.Align,
                style, space))
        | OverlayPrimitive.PrimitiveCase.Image ip ->
            Ok (FSBar.Hub.Image(
                wireToOverlayPoint ip.Anchor,
                ip.Width, ip.Height,
                ip.Bytes.Data.ToArray(),
                space))
        | OverlayPrimitive.PrimitiveCase.None ->
            Error "primitive oneof not set"

    let private capKindToWire (c: FSBar.Hub.CapKind) : string =
        match c with
        | FSBar.Hub.LayersPerClient -> "layers_per_client"
        | FSBar.Hub.PrimitivesPerLayer -> "primitives_per_layer"
        | FSBar.Hub.BytesPerPush -> "bytes_per_push"
        | FSBar.Hub.ImageBytes -> "image_bytes"
        | FSBar.Hub.ImageDimensions -> "image_dimensions"

    // --- US3 — VizAttributeValue wire ↔ F# mapping --------------------

    let private wireToAttributeValue (v: VizAttributeValue) : FSBar.Viz.AttributeValue option =
        match v.Value with
        | VizAttributeValue.ValueCase.BoolValue b -> Some (FSBar.Viz.BoolValue b)
        | VizAttributeValue.ValueCase.IntValue i -> Some (FSBar.Viz.IntValue i)
        | VizAttributeValue.ValueCase.FloatValue f -> Some (FSBar.Viz.FloatValue f)
        | VizAttributeValue.ValueCase.StringValue s -> Some (FSBar.Viz.StringValue s)
        | VizAttributeValue.ValueCase.ColorRgba c -> Some (FSBar.Viz.ColorRgbaValue c)
        | VizAttributeValue.ValueCase.StringListValue sl ->
            Some (FSBar.Viz.StringListValue sl.Values)
        | VizAttributeValue.ValueCase.None -> None

    let private wireToOverlayKind (k: OverlayKey) : FSBar.Viz.OverlayKind option =
        match k with
        | OverlayKey.Units -> Some FSBar.Viz.OverlayKind.Units
        | OverlayKey.Events -> Some FSBar.Viz.OverlayKind.Events
        | OverlayKey.Grid -> Some FSBar.Viz.OverlayKind.Grid
        | OverlayKey.MetalSpots -> Some FSBar.Viz.OverlayKind.MetalSpots
        | OverlayKey.EconomyHud -> Some FSBar.Viz.OverlayKind.EconomyHud
        | OverlayKey.WeaponRanges -> Some FSBar.Viz.OverlayKind.WeaponRanges
        | OverlayKey.SightRanges -> Some FSBar.Viz.OverlayKind.SightRanges
        | OverlayKey.CommandQueue -> Some FSBar.Viz.OverlayKind.CommandQueue
        | OverlayKey.FullNames -> Some FSBar.Viz.OverlayKind.FullNames
        | OverlayKey.Unspecified | _ -> None

    let private wireToToggleTarget (t: OverlayTargetState) : FSBar.Hub.ToggleTarget =
        match t with
        | OverlayTargetState.On -> FSBar.Hub.On
        | OverlayTargetState.Off -> FSBar.Hub.Off
        | OverlayTargetState.Toggle
        | OverlayTargetState.Unspecified
        | _ -> FSBar.Hub.Toggle

    let private wireToHubTab (t: HubTab) : FSBar.Hub.HubTab option =
        match t with
        | HubTab.Setup -> Some FSBar.Hub.HubTab.Setup
        | HubTab.Viewer -> Some FSBar.Hub.HubTab.Viewer
        | HubTab.Units -> Some FSBar.Hub.HubTab.Units
        | HubTab.Style -> Some FSBar.Hub.HubTab.Style
        | HubTab.Cfg -> Some FSBar.Hub.HubTab.Cfg
        | HubTab.Grpc -> Some FSBar.Hub.HubTab.Grpc
        | HubTab.Unspecified | _ -> None

    // --- US2 — render-frame wire ↔ F# ImageFormat mapping --------------

    let private wireToImageFormat (f: ImageFormat) : FSBar.Hub.ImageFormat =
        match f with
        | ImageFormat.Jpeg -> FSBar.Hub.Jpeg
        | ImageFormat.Png
        | ImageFormat.Unspecified
        | _ -> FSBar.Hub.Png

    let private imageFormatToWire (f: FSBar.Hub.ImageFormat) : ImageFormat =
        match f with
        | FSBar.Hub.Png -> ImageFormat.Png
        | FSBar.Hub.Jpeg -> ImageFormat.Jpeg

    let private toRenderFrameMessageWire (msg: FSBar.Hub.RenderFrameMessage)
            : RenderFrameMessage =
        { ImageBytes = FsGrpc.Bytes.CopyFrom(msg.ImageBytes)
          Format = imageFormatToWire msg.Format
          RenderedAtUnixMs = msg.RenderedAtUnixMs
          EncodedAtUnixMs = msg.EncodedAtUnixMs
          ClientSequence = msg.ClientSequence
          ViewportWidth = msg.ViewportWidth
          ViewportHeight = msg.ViewportHeight
          Quality = msg.Quality
          IsPlaceholder = msg.IsPlaceholder }

    // --- Feature 042 — log stream wire ↔ F# mapping ------------------

    let private wireToLogSeverity (s: LogSeverity) : HubLog.LogSeverity option =
        match s with
        | LogSeverity.Debug -> Some HubLog.Debug
        | LogSeverity.Info -> Some HubLog.Info
        | LogSeverity.Warning -> Some HubLog.Warning
        | LogSeverity.Error -> Some HubLog.Error
        | LogSeverity.Unspecified | _ -> None

    let private wireToLogCategory (c: LogCategory) : Result<HubLog.LogCategory, string> =
        match c with
        | LogCategory.SessionManager -> Ok HubLog.SessionManager
        | LogCategory.AdminChannel -> Ok HubLog.AdminChannel
        | LogCategory.ScriptingHub -> Ok HubLog.ScriptingHub
        | LogCategory.ProxyInstall -> Ok HubLog.ProxyInstall
        | LogCategory.HeadlessRenderer -> Ok HubLog.HeadlessRenderer
        | LogCategory.HubStateStore -> Ok HubLog.HubStateStore
        | LogCategory.PresetPersistence -> Ok HubLog.PresetPersistence
        | LogCategory.Lobby -> Ok HubLog.Lobby
        | LogCategory.Settings -> Ok HubLog.Settings
        | LogCategory.Unspecified -> Result.Error "LogCategory.Unspecified is not a valid filter category"
        | other -> Result.Error (sprintf "unknown log category: %A" other)

    let private logSeverityToWire (s: HubLog.LogSeverity) : LogSeverity =
        match s with
        | HubLog.Debug -> LogSeverity.Debug
        | HubLog.Info -> LogSeverity.Info
        | HubLog.Warning -> LogSeverity.Warning
        | HubLog.Error -> LogSeverity.Error

    let private logCategoryToWire (c: HubLog.LogCategory) : LogCategory =
        match c with
        | HubLog.SessionManager -> LogCategory.SessionManager
        | HubLog.AdminChannel -> LogCategory.AdminChannel
        | HubLog.ScriptingHub -> LogCategory.ScriptingHub
        | HubLog.ProxyInstall -> LogCategory.ProxyInstall
        | HubLog.HeadlessRenderer -> LogCategory.HeadlessRenderer
        | HubLog.HubStateStore -> LogCategory.HubStateStore
        | HubLog.PresetPersistence -> LogCategory.PresetPersistence
        | HubLog.Lobby -> LogCategory.Lobby
        | HubLog.Settings -> LogCategory.Settings

    let private resolveFilterFromWire (wire: LogFilterWire option) : Result<HubLog.LogFilter, string> =
        match wire with
        | None ->
            Ok HubLog.defaultFilter
        | Some w ->
            let categoriesResult =
                w.Categories
                |> List.fold (fun acc c ->
                    match acc with
                    | Result.Error _ as e -> e
                    | Ok xs ->
                        match wireToLogCategory c with
                        | Ok mapped -> Ok (mapped :: xs)
                        | Result.Error e -> Result.Error e) (Ok [])
            match categoriesResult with
            | Result.Error e -> Result.Error e
            | Ok categoriesRev ->
                let categories = List.rev categoriesRev
                let severity = wireToLogSeverity w.MinSeverity
                let preset =
                    if String.IsNullOrEmpty(w.PresetName) then None else Some w.PresetName
                HubLog.resolveFilter categories severity preset

    [<Sealed>]
    type ScriptingService(
            sessions: SessionManager.SessionManager,
            events: HubEvents.IHubEventSink,
            busEvents: IObservable<HubEvents.HubEvent>,
            unitDefs: unit -> FSBar.Client.UnitDefCache,
            install: BarInstall.BarInstall,
            bundled: BundledProxy.BundledProxyInfo,
            port: int,
            state: HubStateStore.T,
            renderer: HeadlessRenderer.T,
            overlays: OverlayLayerStore.T,
            log: HubLog.T,
            opts: ScriptingHubOptions) =
        inherit ScriptingService.ServiceBase()

        let clients = ConcurrentDictionary<Guid, ClientRegistration>()
        let mutable overflowDetachCount = 0
        let mutable disposed = 0

        let channelOpts =
            BoundedChannelOptions(opts.FrameBufferCapacity,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false)

        let publishFrame (highbarFrame: Highbar.Frame) =
            let snapshot = clients.Values |> Seq.toArray
            for client in snapshot do
                // DropOldest never fails TryWrite — peek at reader
                // count before writing to detect an impending drop.
                // Racy, but the counter is informational and drift is
                // ≤ capacity under load.
                if client.Channel.Reader.Count >= opts.FrameBufferCapacity then
                    Interlocked.Increment(&client.DropCount) |> ignore
                let seq = Interlocked.Increment(&client.Sequence) |> uint64
                let wire: GameFrameMessage = {
                    Frame = Some highbarFrame
                    ClientSequence = seq
                    HubEnqueuedAtUnixMs = unixMillis ()
                }
                client.Channel.Writer.TryWrite(wire) |> ignore

                if client.DropCount >= opts.MaxCumulativeDrops then
                    match clients.TryRemove(client.Id) with
                    | true, _ ->
                        Interlocked.Increment(&overflowDetachCount) |> ignore
                        events.Publish(
                            HubEvents.ScriptingClientDetached(
                                client.Id,
                                HubEvents.OverflowDropLimit))
                        client.Channel.Writer.TryComplete() |> ignore
                        client.Cancellation.Cancel()
                    | false, _ -> ()

        let frameSubscription =
            sessions.Frames.Subscribe(
                { new IObserver<FSBar.Client.GameFrame> with
                    member _.OnNext(frame) =
                        // Phase-9 note (see proto comment): the hub
                        // surfaces only the engine frame number +
                        // team id pulled from SessionManager's live
                        // GameState. The F# GameFrame carries typed
                        // events that do not yet have a wire form.
                        let teamId =
                            match sessions.State with
                            | SessionManager.Running rs ->
                                try rs.BarClient.GameState.TeamId
                                with _ -> 0
                            | _ -> 0
                        let wireFrame: Highbar.Frame = {
                            FrameNumber = frame.FrameNumber
                            Events = []
                            TeamId = teamId
                        }
                        publishFrame wireFrame
                    member _.OnError(_) = ()
                    member _.OnCompleted() = () })

        let detachOnDisconnect (id: Guid) =
            match clients.TryRemove(id) with
            | true, client ->
                events.Publish(
                    HubEvents.ScriptingClientDetached(
                        id,
                        HubEvents.ClientDisconnected))
                client.Channel.Writer.TryComplete() |> ignore
            | _ -> ()

        // --- Test-accessible internal helpers ---

        member internal _.PushTestFrame(frameNumber: int, teamId: int) =
            let wire: Highbar.Frame = {
                FrameNumber = uint32 frameNumber
                Events = []
                TeamId = teamId
            }
            publishFrame wire

        member internal this.AttachTestClient(label: string) =
            let id = Guid.NewGuid()
            let client = {
                Id = id
                Label = (if String.IsNullOrEmpty(label) then id.ToString("N").Substring(0, 8) else label)
                RemoteEndpoint = "in-process-test"
                Channel = Channel.CreateBounded<GameFrameMessage>(channelOpts)
                DropCount = 0
                AttachedAtUnixMs = unixMillis ()
                Sequence = 0UL
                Cancellation = new CancellationTokenSource()
            }
            clients.[id] <- client
            events.Publish(HubEvents.ScriptingClientConnected(id, client.RemoteEndpoint))
            id, client.Channel.Reader

        member internal _.DropCountFor(id: Guid) =
            match clients.TryGetValue(id) with
            | true, c -> c.DropCount
            | _ -> -1

        member internal _.DetachTestClient(id: Guid) =
            detachOnDisconnect id

        // --- Public API ---

        member _.Clients : ConnectedClientInfo list =
            clients.Values
            |> Seq.map (fun c ->
                { ClientId = c.Id
                  ClientLabel = c.Label
                  RemoteEndpoint = c.RemoteEndpoint
                  AttachedAtUnixMs = c.AttachedAtUnixMs
                  CumulativeDroppedFrames = c.DropCount })
            |> List.ofSeq

        member _.OverflowDetachCount = Volatile.Read(&overflowDetachCount)

        // --- gRPC service overrides (curried signatures) ---

        override _.StreamGameFrames request responseStream context =
            task {
                let id = Guid.NewGuid()
                let client = {
                    Id = id
                    Label = (if String.IsNullOrEmpty(request.ClientLabel) then id.ToString("N").Substring(0, 8) else request.ClientLabel)
                    RemoteEndpoint =
                        match box context with
                        | null -> "unknown"
                        | _ ->
                            try context.Peer
                            with _ -> "unknown"
                    Channel = Channel.CreateBounded<GameFrameMessage>(channelOpts)
                    DropCount = 0
                    AttachedAtUnixMs = unixMillis ()
                    Sequence = 0UL
                    Cancellation = new CancellationTokenSource()
                }
                clients.[id] <- client
                events.Publish(HubEvents.ScriptingClientConnected(id, client.RemoteEndpoint))

                try
                    use linked =
                        CancellationTokenSource.CreateLinkedTokenSource(
                            context.CancellationToken,
                            client.Cancellation.Token)
                    let reader = client.Channel.Reader
                    let mutable keepGoing = true
                    while keepGoing do
                        let! ok =
                            try reader.WaitToReadAsync(linked.Token).AsTask()
                            with :? OperationCanceledException -> Task.FromResult(false)
                        if not ok then keepGoing <- false
                        else
                            let mutable msg = Unchecked.defaultof<GameFrameMessage>
                            while reader.TryRead(&msg) do
                                do! responseStream.WriteAsync(msg)
                with _ -> ()

                detachOnDisconnect id
            } :> Task

        override _.SendCommand request _context =
            task {
                match sessions.State, request.Command with
                | SessionManager.Running rs, Some cmd ->
                    rs.BarClient.SendCommands([ cmd ])
                    let frameNum =
                        try int rs.BarClient.GameState.FrameNumber
                        with _ -> 0
                    return ({ ForwardedAtFrame = frameNum } : SendCommandResponse)
                | _, None ->
                    return raise (Grpc.Core.RpcException(
                        Grpc.Core.Status(
                            Grpc.Core.StatusCode.InvalidArgument,
                            "SendCommandRequest.command is required")))
                | _, Some _ ->
                    return raise (Grpc.Core.RpcException(
                        Grpc.Core.Status(
                            Grpc.Core.StatusCode.NotFound,
                            "no session is currently running")))
            }

        override this.GetSessionStatus _request _context =
            task {
                let stateEnum =
                    match sessions.State with
                    | SessionManager.Idle -> GetSessionStatusResponse.State.Idle
                    | SessionManager.Starting _ -> GetSessionStatusResponse.State.Starting
                    | SessionManager.Running _ -> GetSessionStatusResponse.State.Running
                    | SessionManager.Ending _ -> GetSessionStatusResponse.State.Ending
                    | SessionManager.Failed _ -> GetSessionStatusResponse.State.Failed
                let activeSession : ActiveSession option =
                    match sessions.State with
                    | SessionManager.Running rs ->
                        let modeStr =
                            match rs.Config.Mode with
                            | LobbyConfig.Skirmish -> "Skirmish"
                            | LobbyConfig.FFA -> "FFA"
                            | LobbyConfig.Team -> "Team"
                        Some {
                            SessionId = rs.Id.ToString()
                            MapName = rs.Config.MapName
                            Mode = modeStr
                            EngineSpeed = rs.Config.EngineSpeed
                            Paused = false
                            StartedAtUnixMs = rs.StartedAt.ToUnixTimeMilliseconds()
                            AdminChannelStatus = toStatusInfo sessions.AdminStatus
                        }
                    | _ -> None
                let failure : FailureInfo option =
                    match sessions.State with
                    | SessionManager.Failed(_, reason, excerpt) ->
                        Some {
                            Reason = reason
                            InfologExcerpt = excerpt |> Option.defaultValue ""
                        }
                    | _ -> None
                let wireClients : ConnectedClient list =
                    this.Clients
                    |> List.map (fun c ->
                        {
                            ClientId = c.ClientId.ToString()
                            ClientLabel = c.ClientLabel
                            RemoteEndpoint = c.RemoteEndpoint
                            AttachedAtUnixMs = c.AttachedAtUnixMs
                            CumulativeDroppedFrames = c.CumulativeDroppedFrames
                        })
                return
                    {
                        State = stateEnum
                        BarDataDir = install.DataDir
                        ActiveEngineVersion = install.ActiveEngine.Version
                        BundledProxyVersion = bundled.Version
                        GrpcPort = port
                        ActiveSession = activeSession
                        Clients = wireClients
                        Failure = failure
                    }
            }

        override _.GetUnitDef request _context =
            task {
                let cache = unitDefs ()
                let info =
                    match request.Selector with
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.DefId defId ->
                        FSBar.Client.UnitDefCache.tryFindById cache defId
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.InternalName name ->
                        FSBar.Client.UnitDefCache.tryFindByName cache name
                    | Fsbar.Hub.Scripting.V1.GetUnitDefRequest.SelectorCase.None -> None
                let resp : GetUnitDefResponse =
                    match info with
                    | Some ud ->
                        {
                            UnitDef = Some {
                                DefId = ud.DefId
                                InternalName = ud.Name
                                DisplayName = ud.Name
                                MetalCost = int ud.Cost
                                EnergyCost = int ud.Cost
                                BuildTime = int ud.BuildSpeed
                                MaxHealth = 0
                            }
                        }
                    | None -> { UnitDef = None }
                return resp
            }

        // --- Feature 039 admin-channel RPCs ---
        // Phase-2E scaffolding: every RPC rejects with a "not yet implemented"
        // reason until its per-US phase lands (US1 wires Pause/Resume, US2
        // SetEngineSpeed, US3 ForceEndMatch, US4 SendAdminMessage).

        override _.Pause _request _context =
            task {
                let outcome = sessions.Pause()
                let result = toSubmitResult outcome sessions.AdminStatus
                return ({ Result = Some result } : PauseResponse)
            }

        override _.Resume _request _context =
            task {
                let outcome = sessions.Resume()
                let result = toSubmitResult outcome sessions.AdminStatus
                return ({ Result = Some result } : ResumeResponse)
            }

        override _.SetEngineSpeed request _context =
            task {
                let outcome = sessions.SetEngineSpeed request.Speed
                let result = toSubmitResult outcome sessions.AdminStatus
                return ({ Result = Some result } : SetEngineSpeedResponse)
            }

        override _.ForceEndMatch _request _context =
            task {
                let outcome = sessions.ForceEnd()
                let result = toSubmitResult outcome sessions.AdminStatus
                return ({ Result = Some result } : ForceEndMatchResponse)
            }

        override _.SendAdminMessage request _context =
            task {
                HubLog.emitSimple log HubLog.ScriptingHub HubLog.Debug (fun () ->
                    sprintf "SendAdminMessage text: %s" request.Text)
                let outcome = sessions.SendAdminMessage request.Text
                let result = toSubmitResult outcome sessions.AdminStatus
                return ({ Result = Some result } : SendAdminMessageResponse)
            }

        // -------------------------------------------------------------------
        // Feature 040 — unimplemented stubs.
        //
        // Every RPC below is surface-only at the Phase-1 pre-flight gate —
        // the real implementation lands in its per-US phase (US1 → session
        // orchestration, US2 → render frames, US3 → viz/camera, US4 →
        // preset/encyclopedia/settings/proxy, US5 → hub-state sync, US6 →
        // client overlays). Each stub throws UNIMPLEMENTED so the build
        // passes without pretending to fulfil the contract.
        // -------------------------------------------------------------------

        static member private Unimplemented(name: string) : 'T =
            raise (Grpc.Core.RpcException(
                Grpc.Core.Status(
                    Grpc.Core.StatusCode.Unimplemented,
                    name + " is not yet implemented")))

        // US1 — Session orchestration (feature 040)

        override _.ConfigureLobby request _context =
            task {
                match request.Lobby with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "lobby is required")
                           ValidationErrors = [] } : ConfigureLobbyResponse)
                | Some wire ->
                    if not (sessions.IsLobbyEditable()) then
                        events.Publish(
                            HubEvents.DiagnosticsLine(
                                HubEvents.Warning,
                                "ConfigureLobby rejected: session active"))
                        return
                            ({ Result = Some (rejectedMutation "session is active; lobby is not editable")
                               ValidationErrors = [] } : ConfigureLobbyResponse)
                    else
                        let lobby = wireToLobby wire
                        match LobbyConfig.validate install lobby with
                        | Ok validated ->
                            match HubStateStore.setLobby state validated with
                            | Sent ->
                                return
                                    ({ Result = Some (sentResult ())
                                       ValidationErrors = [] } : ConfigureLobbyResponse)
                            | Rejected reason ->
                                return
                                    ({ Result = Some (rejectedMutation reason)
                                       ValidationErrors = [] } : ConfigureLobbyResponse)
                        | Result.Error errs ->
                            let errStrings = errs |> List.map LobbyConfig.formatError
                            events.Publish(
                                HubEvents.DiagnosticsLine(
                                    HubEvents.Warning,
                                    sprintf "ConfigureLobby validation failed: %s"
                                        (String.concat "; " errStrings)))
                            return
                                ({ Result = Some (rejectedMutation "validation failed")
                                   ValidationErrors = errStrings } : ConfigureLobbyResponse)
            }

        override _.ListMaps _request _context =
            task {
                let mapsDir = System.IO.Path.Combine(install.DataDir, "maps")
                let descriptors =
                    if not (System.IO.Directory.Exists(mapsDir)) then []
                    else
                        let stemToEngineName =
                            FSBar.Client.ArchiveCache.loadMapsForDataDir install.DataDir
                            |> List.map (fun e -> e.FileStem, e.EngineName)
                            |> Map.ofList
                        System.IO.Directory.GetFiles(mapsDir, "*.sd7")
                        |> Array.sort
                        |> Array.map (fun path ->
                            let stem = System.IO.Path.GetFileNameWithoutExtension(path)
                            let engineName =
                                Map.tryFind stem stemToEngineName
                                |> Option.defaultValue stem
                            ({ Name = engineName; FilePath = path } : MapDescriptor))
                        |> List.ofArray
                return ({ Maps = descriptors } : ListMapsResponse)
            }

        override _.ValidateLobby request _context =
            task {
                match request.Lobby with
                | None ->
                    return ({ Errors = [ "lobby is required" ] } : ValidateLobbyResponse)
                | Some wire ->
                    let lobby = wireToLobby wire
                    match LobbyConfig.validate install lobby with
                    | Ok _ -> return ({ Errors = [] } : ValidateLobbyResponse)
                    | Result.Error errs ->
                        let errStrings = errs |> List.map LobbyConfig.formatError
                        return ({ Errors = errStrings } : ValidateLobbyResponse)
            }

        override _.LaunchSession request _context =
            task {
                if not (sessions.IsLobbyEditable()) then
                    return
                        ({ Result = Some (rejectedMutation "session already active")
                           SessionId = None } : LaunchSessionResponse)
                else
                    let current = HubStateStore.current state
                    let lobby =
                        { current.Lobby with
                            LaunchGraphicalViewer = request.LaunchGraphicalViewer }
                    match sessions.Launch(lobby, request.StartPaused) with
                    | Ok () ->
                        // Poll briefly for the Running session so we can
                        // return the Id; if the launch is still Starting
                        // by the timeout, return Sent with no Id (caller
                        // can poll GetSessionStatus).
                        let sw = System.Diagnostics.Stopwatch.StartNew()
                        let mutable sessionId : string option = None
                        while sessionId.IsNone && sw.ElapsedMilliseconds < 50L do
                            match sessions.State with
                            | SessionManager.Running rs ->
                                sessionId <- Some (rs.Id.ToString())
                            | SessionManager.Starting _ ->
                                do! Task.Delay(5)
                            | _ -> sw.Stop()
                        return
                            ({ Result = Some (sentResult ())
                               SessionId = sessionId } : LaunchSessionResponse)
                    | Result.Error msg ->
                        return
                            ({ Result = Some (rejectedMutation msg)
                               SessionId = None } : LaunchSessionResponse)
            }

        override _.StopSession _request _context =
            task {
                let outcome = sessions.Stop()
                let result =
                    match outcome with
                    | Sent -> sentResult ()
                    | Rejected reason -> rejectedMutation reason
                return ({ Result = Some result } : StopSessionResponse)
            }

        // US2 — Rendered viewer frames (feature 040)

        override _.StreamRenderFrames request responseStream context =
            task {
                let req : FSBar.Hub.RenderSubscriptionRequest = {
                    ClientLabel = request.ClientLabel
                    TargetHz = request.TargetHz
                    Format = wireToImageFormat request.Format
                    ViewportWidth = request.ViewportWidth
                    ViewportHeight = request.ViewportHeight
                    JpegQuality = request.JpegQuality
                    CloseOnSessionEnd = request.CloseOnSessionEnd
                    EmitNoSessionPlaceholder = request.EmitNoSessionPlaceholder
                }
                match HeadlessRenderer.subscribe renderer req with
                | FSBar.Hub.SubscribeRejected reason ->
                    events.Publish(
                        HubEvents.DiagnosticsLine(
                            HubEvents.Warning,
                            sprintf "StreamRenderFrames rejected: %s" reason))
                    return raise (Grpc.Core.RpcException(
                        Grpc.Core.Status(
                            Grpc.Core.StatusCode.ResourceExhausted, reason)))
                | FSBar.Hub.Subscribed sub ->
                    try
                        let reader = sub.Channel
                        let mutable keepGoing = true
                        while keepGoing do
                            let! ok =
                                try reader.WaitToReadAsync(context.CancellationToken).AsTask()
                                with :? OperationCanceledException -> Task.FromResult(false)
                            if not ok then keepGoing <- false
                            else
                                let mutable msg = Unchecked.defaultof<FSBar.Hub.RenderFrameMessage>
                                while reader.TryRead(&msg) do
                                    let wireMsg = toRenderFrameMessageWire msg
                                    do! responseStream.WriteAsync(wireMsg)
                    with _ -> ()
                    sub.Dispose()
            } :> Task

        override _.GetRenderFrame request _context =
            task {
                let format = wireToImageFormat request.Format
                let vw = if request.ViewportWidth <= 0 then 1024 else request.ViewportWidth
                let vh = if request.ViewportHeight <= 0 then 768 else request.ViewportHeight
                let q = if request.JpegQuality <= 0 then 85 else request.JpegQuality
                let msg = HeadlessRenderer.renderOnce renderer format vw vh q
                return ({ Frame = Some (toRenderFrameMessageWire msg) } : GetRenderFrameResponse)
            }

        // US3 — Viz + camera control (feature 040)

        override _.SetVizConfig request _context =
            task {
                let attrs =
                    match request.VizConfig with
                    | Some cfg -> cfg.Attributes
                    | None -> Map.empty
                let unknownKeys = ResizeArray<string>()
                let invalidValues = ResizeArray<string>()
                let mutable errorEncountered = false
                for kv in attrs do
                    match FSBar.Viz.ConfigDescriptors.tryFind kv.Key with
                    | None -> unknownKeys.Add kv.Key
                    | Some _ ->
                        match wireToAttributeValue kv.Value with
                        | None ->
                            invalidValues.Add kv.Key
                        | Some attrVal ->
                            match HubStateStore.setVizAttribute state kv.Key attrVal with
                            | Sent -> ()
                            | Rejected reason ->
                                invalidValues.Add (sprintf "%s: %s" kv.Key reason)
                                errorEncountered <- true
                let anyError =
                    errorEncountered
                    || unknownKeys.Count > 0
                    || invalidValues.Count > 0
                let mutationResult =
                    if anyError then
                        let reason =
                            if unknownKeys.Count > 0 && invalidValues.Count > 0 then
                                "unknown keys + invalid values"
                            elif unknownKeys.Count > 0 then "unknown keys"
                            else "invalid values"
                        events.Publish(
                            HubEvents.DiagnosticsLine(
                                HubEvents.Warning,
                                sprintf "SetVizConfig: %s (unknown=%A, invalid=%A)"
                                    reason (List.ofSeq unknownKeys) (List.ofSeq invalidValues)))
                        rejectedMutation reason
                    else
                        sentResult ()
                return
                    ({ Result = Some mutationResult
                       UnknownKeys = List.ofSeq unknownKeys
                       InvalidValues = List.ofSeq invalidValues } : SetVizConfigResponse)
            }

        override _.SetVizAttribute request _context =
            task {
                match request.Value with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "value is required") } : SetVizAttributeResponse)
                | Some wireVal ->
                    match wireToAttributeValue wireVal with
                    | None ->
                        return
                            ({ Result = Some (rejectedMutation "value is required") } : SetVizAttributeResponse)
                    | Some attrVal ->
                        let outcome = HubStateStore.setVizAttribute state request.Key attrVal
                        let result =
                            match outcome with
                            | Sent -> sentResult ()
                            | Rejected reason ->
                                events.Publish(
                                    HubEvents.DiagnosticsLine(
                                        HubEvents.Warning,
                                        sprintf "SetVizAttribute(%s) rejected: %s"
                                            request.Key reason))
                                rejectedMutation reason
                        return ({ Result = Some result } : SetVizAttributeResponse)
            }

        override _.ToggleOverlay request _context =
            task {
                match wireToOverlayKind request.Overlay with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "unknown overlay key")
                           NewState = false } : ToggleOverlayResponse)
                | Some kind ->
                    let target = wireToToggleTarget request.Target
                    let outcome, newState =
                        HubStateStore.toggleOverlay state kind target
                    let result =
                        match outcome with
                        | Sent -> sentResult ()
                        | Rejected reason -> rejectedMutation reason
                    return
                        ({ Result = Some result
                           NewState = newState } : ToggleOverlayResponse)
            }

        override _.SetCamera request _context =
            task {
                match request.Camera with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "camera is required") } : SetCameraResponse)
                | Some wire ->
                    let cam : ViewerCamera = {
                        Scale = wire.Scale
                        OriginX = wire.OriginX
                        OriginY = wire.OriginY
                        AutoFit = wire.AutoFit
                    }
                    let outcome = HubStateStore.setCamera state cam
                    let result =
                        match outcome with
                        | Sent -> sentResult ()
                        | Rejected reason ->
                            events.Publish(
                                HubEvents.DiagnosticsLine(
                                    HubEvents.Warning,
                                    sprintf "SetCamera rejected: %s" reason))
                            rejectedMutation reason
                    return ({ Result = Some result } : SetCameraResponse)
            }

        override _.SetActiveTab request _context =
            task {
                match wireToHubTab request.Tab with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "unknown tab") } : SetActiveTabResponse)
                | Some tab ->
                    let outcome = HubStateStore.setActiveTab state tab
                    let result =
                        match outcome with
                        | Sent -> sentResult ()
                        | Rejected reason -> rejectedMutation reason
                    return ({ Result = Some result } : SetActiveTabResponse)
            }

        // US4 — Preset / encyclopedia / settings / proxy (feature 040)

        override _.ListPresets _request _context =
            task {
                let dir = FSBar.Viz.StylePreset.presetDirectory
                let names = FSBar.Viz.StylePreset.listNames ()
                let descriptors =
                    names
                    |> List.map (fun name ->
                        let path = System.IO.Path.Combine(dir, name + ".json")
                        let modifiedAt =
                            if System.IO.File.Exists(path) then
                                System.IO.File.GetLastWriteTimeUtc(path)
                                |> (fun t -> DateTimeOffset(t).ToUnixTimeMilliseconds())
                            else 0L
                        ({ Name = name; ModifiedAtUnixMs = modifiedAt } : PresetDescriptor))
                return ({ Presets = descriptors } : ListPresetsResponse)
            }

        override _.SavePreset request _context =
            task {
                if not (FSBar.Viz.StylePreset.isValidName request.Name) then
                    events.Publish(
                        HubEvents.DiagnosticsLine(
                            HubEvents.Warning,
                            sprintf "SavePreset invalid name: %s" request.Name))
                    return
                        ({ Result = Some (rejectedMutation "invalid preset name") } : SavePresetResponse)
                else
                    // Snapshot current VizConfig and save under the given name.
                    let current = (HubStateStore.current state).VizConfig
                    let preset = FSBar.Viz.StylePreset.fromConfig request.Name current
                    match FSBar.Viz.StylePreset.save preset with
                    | Ok _ ->
                        events.Publish(HubEvents.PresetSaved request.Name)
                        return
                            ({ Result = Some (sentResult ()) } : SavePresetResponse)
                    | Result.Error reason ->
                        events.Publish(
                            HubEvents.DiagnosticsLine(
                                HubEvents.Error,
                                sprintf "SavePreset failed: %s" reason))
                        return
                            ({ Result = Some (rejectedMutation reason) } : SavePresetResponse)
            }

        override _.LoadPreset request _context =
            task {
                match FSBar.Viz.StylePreset.load request.Name with
                | Ok preset ->
                    let current = (HubStateStore.current state).VizConfig
                    let applied = FSBar.Viz.StylePreset.applyToConfig preset current
                    HubStateStore.setVizConfig state applied |> ignore
                    events.Publish(HubEvents.PresetLoaded request.Name)
                    return
                        ({ Result = Some (sentResult ())
                           VizConfig = None } : LoadPresetResponse)
                | Result.Error reason ->
                    return
                        ({ Result = Some (rejectedMutation reason)
                           VizConfig = None } : LoadPresetResponse)
            }

        override _.DeletePreset request _context =
            task {
                match FSBar.Viz.StylePreset.delete request.Name with
                | Ok () ->
                    events.Publish(HubEvents.PresetDeleted request.Name)
                    return ({ Result = Some (sentResult ()) } : DeletePresetResponse)
                | Result.Error reason ->
                    return ({ Result = Some (rejectedMutation reason) } : DeletePresetResponse)
            }

        // Lazy-materialise the encyclopedia once per process.
        static member val private cachedEntries
            : Lazy<FSBar.Viz.EncyclopediaData.EncyclopediaEntry list> =
                lazy (FSBar.Viz.EncyclopediaData.buildFromBarData ())

        override _.ListUnits request _context =
            task {
                let entries = ScriptingService.cachedEntries.Value
                // Filter by faction labels (case-insensitive string match on Faction).
                let wantedFactions =
                    request.FactionFilter
                    |> List.map (fun s -> s.ToLowerInvariant())
                    |> Set.ofList
                let filtered =
                    if Set.isEmpty wantedFactions then entries
                    else
                        entries
                        |> List.filter (fun e ->
                            wantedFactions.Contains(
                                (sprintf "%A" e.Faction).ToLowerInvariant()))
                let wireEntries =
                    filtered
                    |> List.map (fun e ->
                        ({ DefId = e.DefId
                           InternalName = e.InternalName
                           Subfolder = e.Subfolder
                           Faction = sprintf "%A" e.Faction
                           Tier = sprintf "%A" e.Tier
                           Shape = sprintf "%A" e.Shape
                           MetalCost = e.MetalCost
                           EnergyCost = e.EnergyCost
                           BuildTime = e.BuildTime
                           MaxHealth = e.Health
                           FootprintX = e.FootprintX
                           FootprintZ = e.FootprintZ
                           SightRangeElmo = e.SightRangeElmo
                           WeaponRangesElmo = e.WeaponRangesElmo }
                         : EncyclopediaEntryWire))
                return ({ Entries = wireEntries } : ListUnitsResponse)
            }

        override _.SelectUnit request _context =
            task {
                let entries = ScriptingService.cachedEntries.Value
                let entry =
                    match request.Selector with
                    | SelectUnitRequest.SelectorCase.DefId defId ->
                        entries |> List.tryFind (fun e -> e.DefId = defId)
                    | SelectUnitRequest.SelectorCase.InternalName name ->
                        entries |> List.tryFind (fun e -> e.InternalName = name)
                    | SelectUnitRequest.SelectorCase.None -> None
                match entry with
                | Some e ->
                    // Update the HubStateStore encyclopedia selection.
                    let current = (HubStateStore.current state).Encyclopedia
                    let updated = { current with SelectedDefId = Some e.DefId }
                    HubStateStore.setEncyclopedia state updated |> ignore
                    let wire : EncyclopediaEntryWire =
                        { DefId = e.DefId
                          InternalName = e.InternalName
                          Subfolder = e.Subfolder
                          Faction = sprintf "%A" e.Faction
                          Tier = sprintf "%A" e.Tier
                          Shape = sprintf "%A" e.Shape
                          MetalCost = e.MetalCost
                          EnergyCost = e.EnergyCost
                          BuildTime = e.BuildTime
                          MaxHealth = e.Health
                          FootprintX = e.FootprintX
                          FootprintZ = e.FootprintZ
                          SightRangeElmo = e.SightRangeElmo
                          WeaponRangesElmo = e.WeaponRangesElmo }
                    return
                        ({ Result = Some (sentResult ())
                           Entry = Some wire } : SelectUnitResponse)
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "unit not found")
                           Entry = None } : SelectUnitResponse)
            }

        override _.GetHubSettings _request _context =
            task {
                let s = (HubStateStore.current state).Settings
                let wire : HubSettingsWire = {
                    BarDataDirOverride = Option.defaultValue "" s.BarDataDirOverride
                    EngineVersionOverride = Option.defaultValue "" s.EngineVersionOverride
                    GrpcPort = s.GrpcPort
                    LaunchGraphicalViewerDefault = s.LaunchGraphicalViewerDefault
                    StartPausedDefault = s.StartPausedDefault
                    MaxRenderFrameSubscribers = s.MaxRenderFrameSubscribers
                    SchemaVersion = s.SchemaVersion
                }
                return ({ Settings = Some wire } : GetHubSettingsResponse)
            }

        override _.SetHubSettings request _context =
            task {
                match request.Settings with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "settings is required") } : SetHubSettingsResponse)
                | Some wire ->
                    // Only persist fields that are user-editable via the
                    // setup/settings tabs: StartPausedDefault, LaunchGraphicalViewerDefault,
                    // MaxRenderFrameSubscribers. Others remain at their current values.
                    let currentSettings = (HubStateStore.current state).Settings
                    let step1 =
                        HubSettings.updateStartPausedDefault
                            currentSettings wire.StartPausedDefault
                    let step2 =
                        HubSettings.updateLaunchGraphicalViewerDefault
                            step1 wire.LaunchGraphicalViewerDefault
                    match HubSettings.updateMaxRenderFrameSubscribers
                            step2 wire.MaxRenderFrameSubscribers with
                    | Result.Error reason ->
                        return
                            ({ Result = Some (rejectedMutation reason) } : SetHubSettingsResponse)
                    | Ok updated ->
                        match HubSettings.save updated with
                        | Result.Error reason ->
                            return
                                ({ Result = Some (rejectedMutation reason) } : SetHubSettingsResponse)
                        | Ok () ->
                            HubStateStore.setSettings state updated |> ignore
                            return
                                ({ Result = Some (sentResult ()) } : SetHubSettingsResponse)
            }

        override _.InstallProxy request _context =
            task {
                let result = ProxyInstaller.install install bundled events request.ForceReinstall
                match result with
                | Ok status ->
                    let installedVersion =
                        status.InstalledVersion |> Option.defaultValue ""
                    return
                        ({ Result = Some (sentResult ())
                           InstalledVersion = Some installedVersion } : InstallProxyResponse)
                | Result.Error reasons ->
                    let combined = String.concat "; " reasons
                    return
                        ({ Result = Some (rejectedMutation combined)
                           InstalledVersion = None } : InstallProxyResponse)
            }

        override _.RefreshProxyStatus _request _context =
            task {
                let status = ProxyInstaller.checkStatus install bundled
                let health = ProxyInstaller.health status
                return
                    ({ InstalledVersion =
                           status.InstalledVersion |> Option.defaultValue ""
                       InstallPath = status.InstalledAtPath
                       Health = ProxyInstaller.formatHealth health }
                     : RefreshProxyStatusResponse)
            }

        // US5 — Hub-wide state sync (feature 040)

        override this.GetHubState _request context =
            task {
                let s = HubStateStore.current state
                // Build session-status inline from SessionManager rather
                // than re-calling GetSessionStatus (avoids recursion and
                // keeps the snapshot instant-consistent).
                let! sessionStatus = this.GetSessionStatus GetSessionStatusRequest.empty context
                let dir = FSBar.Viz.StylePreset.presetDirectory
                let presets =
                    FSBar.Viz.StylePreset.listNames ()
                    |> List.map (fun name ->
                        let path = System.IO.Path.Combine(dir, name + ".json")
                        let modifiedAt =
                            if System.IO.File.Exists(path) then
                                System.IO.File.GetLastWriteTimeUtc(path)
                                |> (fun t -> DateTimeOffset(t).ToUnixTimeMilliseconds())
                            else 0L
                        ({ Name = name; ModifiedAtUnixMs = modifiedAt } : PresetDescriptor))
                let hubSettingsWire : HubSettingsWire = {
                    BarDataDirOverride = Option.defaultValue "" s.Settings.BarDataDirOverride
                    EngineVersionOverride = Option.defaultValue "" s.Settings.EngineVersionOverride
                    GrpcPort = s.Settings.GrpcPort
                    LaunchGraphicalViewerDefault = s.Settings.LaunchGraphicalViewerDefault
                    StartPausedDefault = s.Settings.StartPausedDefault
                    MaxRenderFrameSubscribers = s.Settings.MaxRenderFrameSubscribers
                    SchemaVersion = s.Settings.SchemaVersion
                }
                return
                    ({ ActiveTab = hubTabToWire s.ActiveTab
                       VizConfig = Some (vizConfigToWire s.VizConfig)
                       Camera = Some (viewerCameraToWire s.Camera)
                       Lobby = Some (lobbyToWire s.Lobby)
                       Encyclopedia = Some (encyclopediaToWire s.Encyclopedia)
                       Presets = presets
                       SessionStatus = Some sessionStatus
                       HubSettings = Some hubSettingsWire } : HubStateSnapshot)
            }

        override _.StreamHubStateEvents request responseStream context =
            task {
                let clientLabel =
                    if String.IsNullOrEmpty(request.ClientLabel) then "hub-state-observer"
                    else request.ClientLabel
                // Per-subscriber bounded channel; fanout with DropOldest.
                let ch =
                    Channel.CreateBounded<HubStateEvent>(
                        BoundedChannelOptions(16,
                            FullMode = BoundedChannelFullMode.DropOldest,
                            SingleReader = true,
                            SingleWriter = true))
                let stampEvent (change: HubStateEvent.ChangeCase) : HubStateEvent =
                    { Change = change
                      EmittedAtUnixMs = unixMillis ()
                      Source = "hub" }
                let observer =
                    { new IObserver<HubEvents.HubEvent> with
                        member _.OnNext(e) =
                            let wireCase =
                                match e with
                                | HubEvents.ActiveTabChanged t ->
                                    Some (HubStateEvent.ChangeCase.ActiveTab
                                              (hubTabToWire t))
                                | HubEvents.VizConfigChanged c ->
                                    Some (HubStateEvent.ChangeCase.VizConfig
                                              (vizConfigToWire c))
                                | HubEvents.VizAttributeChanged(k, oldV, newV) ->
                                    Some (HubStateEvent.ChangeCase.VizAttribute
                                              ({ Key = k
                                                 OldValue = Some (attributeValueToWire oldV)
                                                 NewValue = Some (attributeValueToWire newV) }
                                               : VizAttributeChange))
                                | HubEvents.CameraChanged c ->
                                    Some (HubStateEvent.ChangeCase.Camera
                                              (viewerCameraToWire c))
                                | HubEvents.LobbyChanged l ->
                                    Some (HubStateEvent.ChangeCase.Lobby (lobbyToWire l))
                                | HubEvents.EncyclopediaSelectionChanged s ->
                                    Some (HubStateEvent.ChangeCase.Encyclopedia
                                              (encyclopediaToWire s))
                                | HubEvents.PresetSaved name ->
                                    Some (HubStateEvent.ChangeCase.Preset
                                              ({ Kind = PresetChange.Kind.Saved; Name = name }
                                               : PresetChange))
                                | HubEvents.PresetDeleted name ->
                                    Some (HubStateEvent.ChangeCase.Preset
                                              ({ Kind = PresetChange.Kind.Deleted; Name = name }
                                               : PresetChange))
                                | HubEvents.PresetLoaded name ->
                                    Some (HubStateEvent.ChangeCase.Preset
                                              ({ Kind = PresetChange.Kind.Loaded; Name = name }
                                               : PresetChange))
                                | HubEvents.HubSettingsChanged hs ->
                                    let wire : HubSettingsWire = {
                                        BarDataDirOverride =
                                            Option.defaultValue "" hs.BarDataDirOverride
                                        EngineVersionOverride =
                                            Option.defaultValue "" hs.EngineVersionOverride
                                        GrpcPort = hs.GrpcPort
                                        LaunchGraphicalViewerDefault = hs.LaunchGraphicalViewerDefault
                                        StartPausedDefault = hs.StartPausedDefault
                                        MaxRenderFrameSubscribers = hs.MaxRenderFrameSubscribers
                                        SchemaVersion = hs.SchemaVersion
                                    }
                                    Some (HubStateEvent.ChangeCase.HubSettings wire)
                                | HubEvents.AdminChannelStatusChanged st ->
                                    Some (HubStateEvent.ChangeCase.AdminChannelStatus
                                              (toStatusInfo (Some st)
                                               |> Option.defaultValue
                                                   { State = AdminChannelStatusInfo.State.Unspecified
                                                     Reason = "" }))
                                | HubEvents.ProxyInstallProgress(step, outcome) ->
                                    let percentGuess =
                                        match step with
                                        | HubEvents.CopyAiFiles -> 33
                                        | HubEvents.TouchDevMode -> 66
                                        | HubEvents.ToggleSimpleAiList -> 100
                                    let stageStr =
                                        sprintf "%A:%A" step outcome
                                    Some (HubStateEvent.ChangeCase.ProxyInstallProgress
                                              ({ Stage = stageStr
                                                 Percent = percentGuess }
                                               : ProxyInstallProgress))
                                | _ -> None
                            match wireCase with
                            | Some c -> ch.Writer.TryWrite(stampEvent c) |> ignore
                            | None -> ()
                        member _.OnError(_) = ()
                        member _.OnCompleted() = ch.Writer.TryComplete() |> ignore }
                // Subscribe to the hub event bus so every HubEvent is
                // projected onto the wire and fanned out to this client.
                use _sub = busEvents.Subscribe(observer)
                ignore clientLabel
                try
                    let reader = ch.Reader
                    let mutable keepGoing = true
                    while keepGoing do
                        let! ok =
                            try reader.WaitToReadAsync(context.CancellationToken).AsTask()
                            with :? OperationCanceledException -> Task.FromResult(false)
                        if not ok then keepGoing <- false
                        else
                            let mutable msg = Unchecked.defaultof<HubStateEvent>
                            while reader.TryRead(&msg) do
                                do! responseStream.WriteAsync(msg)
                with _ -> ()
                ch.Writer.TryComplete() |> ignore
            } :> Task

        // US6 — Client-authored overlays (feature 040)

        override _.PutLayer request context =
            task {
                match request.Layer with
                | None ->
                    return
                        ({ Result = Some (rejectedMutation "layer is required")
                           ValidationErrors = []
                           ExceededCap = "" } : PutLayerResponse)
                | Some wire ->
                    let clientId =
                        let peer = if isNull (box context) then "" else context.Peer
                        clientIdFromPeer peer
                    let primResults =
                        wire.Primitives |> List.map wireToOverlayPrimitive
                    let oneofErrors =
                        primResults
                        |> List.choose (function Error e -> Some e | _ -> None)
                    if not (List.isEmpty oneofErrors) then
                        return
                            ({ Result = Some (rejectedMutation "invalid primitive oneof")
                               ValidationErrors = oneofErrors
                               ExceededCap = "" } : PutLayerResponse)
                    else
                        let primitives =
                            primResults |> List.choose (function Ok p -> Some p | _ -> None)
                        let layer : FSBar.Hub.OverlayLayer = {
                            Name = wire.Name
                            ZHint = wire.ZHint
                            UploadedAtUnixMs = unixMillis ()
                            Primitives = primitives
                        }
                        match OverlayLayerStore.putLayer overlays clientId layer with
                        | Ok () ->
                            return
                                ({ Result = Some (sentResult ())
                                   ValidationErrors = []
                                   ExceededCap = "" } : PutLayerResponse)
                        | Error (FSBar.Hub.InvalidName reason) ->
                            events.Publish(
                                HubEvents.DiagnosticsLine(
                                    HubEvents.Warning,
                                    sprintf "PutLayer invalid name: %s" reason))
                            return
                                ({ Result = Some (rejectedMutation (sprintf "invalid name: %s" reason))
                                   ValidationErrors = []
                                   ExceededCap = "" } : PutLayerResponse)
                        | Error (FSBar.Hub.ValidationFailed errs) ->
                            events.Publish(
                                HubEvents.DiagnosticsLine(
                                    HubEvents.Warning,
                                    sprintf "PutLayer validation failed: %A" errs))
                            return
                                ({ Result = Some (rejectedMutation "validation failed")
                                   ValidationErrors = errs
                                   ExceededCap = "" } : PutLayerResponse)
                        | Error (FSBar.Hub.CapExceeded cap) ->
                            let capStr = capKindToWire cap
                            events.Publish(
                                HubEvents.DiagnosticsLine(
                                    HubEvents.Warning,
                                    sprintf "PutLayer cap exceeded: %s" capStr))
                            return
                                ({ Result = Some (rejectedMutation (sprintf "cap exceeded: %s" capStr))
                                   ValidationErrors = []
                                   ExceededCap = capStr } : PutLayerResponse)
            }

        override _.DeleteLayer request context =
            task {
                let clientId =
                    let peer = if isNull (box context) then "" else context.Peer
                    clientIdFromPeer peer
                let outcome = OverlayLayerStore.deleteLayer overlays clientId request.Name
                let result =
                    match outcome with
                    | Sent -> sentResult ()
                    | Rejected reason -> rejectedMutation reason
                return ({ Result = Some result } : DeleteLayerResponse)
            }

        override _.ListLayers _request context =
            task {
                let clientId =
                    let peer = if isNull (box context) then "" else context.Peer
                    clientIdFromPeer peer
                let descriptors = OverlayLayerStore.listLayers overlays clientId
                let wireDescriptors =
                    descriptors
                    |> List.map (fun d ->
                        ({ Name = d.Name
                           ZHint = d.ZHint
                           UploadedAtUnixMs = d.UploadedAtUnixMs
                           PrimitiveCount = d.PrimitiveCount } : OverlayLayerDescriptor))
                return ({ Layers = wireDescriptors } : ListLayersResponse)
            }

        override _.ClearLayers _request context =
            task {
                let clientId =
                    let peer = if isNull (box context) then "" else context.Peer
                    clientIdFromPeer peer
                let cleared = OverlayLayerStore.clearLayers overlays clientId
                return
                    ({ Result = Some (sentResult ())
                       ClearedCount = cleared } : ClearLayersResponse)
            }

        // --- Feature 042 — StreamHubLog (US1) ---

        override _.StreamHubLog requestStream responseStream context =
            task {
                // Wait for initial filter request.
                let! hasFirst = requestStream.MoveNext(context.CancellationToken)
                if not hasFirst then
                    return ()
                else
                let firstReq = requestStream.Current
                let firstFilterWire =
                    firstReq.Filter
                let filter =
                    match resolveFilterFromWire firstFilterWire with
                    | Ok f -> f
                    | Result.Error reason ->
                        raise (Grpc.Core.RpcException(
                            Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, reason)))
                let label =
                    if String.IsNullOrEmpty(firstReq.ClientLabel) then "anonymous"
                    else firstReq.ClientLabel
                match HubLog.attach log label filter context.CancellationToken with
                | HubLog.Rejected reason ->
                    raise (Grpc.Core.RpcException(
                        Grpc.Core.Status(Grpc.Core.StatusCode.ResourceExhausted, reason)))
                | HubLog.Attached sub ->
                    HubLog.emitSimple log HubLog.ScriptingHub HubLog.Debug (fun () ->
                        sprintf "log-stream subscribed: label=%s id=%s" label (sub.Id.ToString("N")))
                    try
                        // Start a background filter-update loop reading
                        // subsequent request messages.
                        let filterUpdateTask =
                            task {
                                try
                                    let mutable keepGoing = true
                                    while keepGoing do
                                        let! nextOk =
                                            try requestStream.MoveNext(context.CancellationToken)
                                            with :? OperationCanceledException -> Task.FromResult(false)
                                        if not nextOk then keepGoing <- false
                                        else
                                            let req = requestStream.Current
                                            match resolveFilterFromWire req.Filter with
                                            | Result.Error reason ->
                                                raise (Grpc.Core.RpcException(
                                                    Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, reason)))
                                            | Ok newFilter ->
                                                match HubLog.updateFilter log sub.Id newFilter with
                                                | Ok () ->
                                                    let catLabel =
                                                        match newFilter.Categories with
                                                        | None -> "all"
                                                        | Some xs ->
                                                            xs
                                                            |> Seq.map (fun c -> sprintf "%A" c)
                                                            |> String.concat ","
                                                    HubLog.emitSimple log HubLog.ScriptingHub HubLog.Debug (fun () ->
                                                        sprintf "log-stream filter updated: categories=[%s], minSeverity=%A"
                                                            catLabel newFilter.MinSeverity)
                                                | Result.Error reason ->
                                                    raise (Grpc.Core.RpcException(
                                                        Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, reason)))
                                with
                                | :? OperationCanceledException -> ()
                                | _ -> ()
                            } :> Task

                        // Drain subscription reader → wire messages.
                        let reader = sub.Reader
                        let mutable keepGoing = true
                        while keepGoing do
                            let! ok =
                                try reader.WaitToReadAsync(context.CancellationToken).AsTask()
                                with :? OperationCanceledException -> Task.FromResult(false)
                            if not ok then keepGoing <- false
                            else
                                let mutable entry = Unchecked.defaultof<HubLog.LogEntry>
                                while reader.TryRead(&entry) do
                                    let corr =
                                        match entry.CorrelationId with
                                        | Some (CorrelationId.CorrelationId s) -> s
                                        | None -> ""
                                    let sessionId =
                                        match entry.SessionId with
                                        | Some g -> g.ToString()
                                        | None -> ""
                                    let clientId =
                                        match entry.ScriptingClientId with
                                        | Some g -> g.ToString()
                                        | None -> ""
                                    let seq = HubLog.nextSequenceFor log sub.Id
                                    let dropped = HubLog.exchangeDroppedSinceLast log sub.Id
                                    let wire: LogEntryMessage = {
                                        TimestampUnixMs = entry.TimestampUnixMs
                                        Severity = logSeverityToWire entry.Severity
                                        Category = logCategoryToWire entry.Category
                                        Message = entry.Message
                                        CorrelationId = corr
                                        SessionId = sessionId
                                        ScriptingClientId = clientId
                                        Sequence = seq
                                        DroppedSinceLast = dropped
                                    }
                                    do! responseStream.WriteAsync(wire)
                        // Ensure filter loop completes.
                        try do! filterUpdateTask with _ -> ()
                    finally
                        sub.Dispose()
            } :> Task

        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    try frameSubscription.Dispose() with _ -> ()
                    for kv in clients do
                        let client = kv.Value
                        try client.Cancellation.Cancel() with _ -> ()
                        try client.Channel.Writer.TryComplete() |> ignore with _ -> ()
                        events.Publish(
                            HubEvents.ScriptingClientDetached(
                                client.Id,
                                HubEvents.ServerShutdown))
                    clients.Clear()
