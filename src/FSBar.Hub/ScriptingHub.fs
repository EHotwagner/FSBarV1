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

    [<Sealed>]
    type ScriptingService(
            sessions: SessionManager.SessionManager,
            events: HubEvents.IHubEventSink,
            unitDefs: unit -> FSBar.Client.UnitDefCache,
            install: BarInstall.BarInstall,
            bundled: BundledProxy.BundledProxyInfo,
            port: int,
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
