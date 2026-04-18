namespace FSBar.Hub

open System
open System.Collections.Generic
open System.Threading
open FSBar.Client

module SessionManager =

    type RunningSession = {
        Id: Guid
        Config: LobbyConfig.LobbyConfig
        EngineConfig: EngineConfig
        BarClient: BarClient
        GraphicalEngineProcess: System.Diagnostics.Process option
        StartedAt: DateTimeOffset
        MapGrid: MapGrid option
        MetalSpots: (float32 * float32 * float32 * float32) array
    }

    type SessionState =
        | Idle
        | Starting of LobbyConfig.LobbyConfig
        | Running of RunningSession
        | Ending of RunningSession
        | Failed of lobby: LobbyConfig.LobbyConfig * reason: string * infologExcerpt: string option

    let private stateTag (st: SessionState) : HubEvents.SessionStateTag =
        match st with
        | Idle -> HubEvents.Idle
        | Starting _ -> HubEvents.Starting
        | Running _ -> HubEvents.Running
        | Ending _ -> HubEvents.Ending
        | Failed _ -> HubEvents.Failed

    /// Per-subscription proxy observer that forwards frames from the
    /// currently-live `BarClient.Frames` to each SessionManager
    /// subscriber. When the session ends, `OnCompleted` is signalled;
    /// subscribers MAY re-subscribe on a subsequent session, which
    /// pulls frames from the new BarClient.
    type private FrameFanOut() =
        let observers = ResizeArray<IObserver<GameFrame>>()
        let sync = obj ()

        let snapshot () =
            lock sync (fun () -> observers.ToArray())

        interface IObservable<GameFrame> with
            member _.Subscribe(observer: IObserver<GameFrame>) =
                if isNull (box observer) then
                    raise (ArgumentNullException("observer"))
                lock sync (fun () -> observers.Add(observer))
                { new IDisposable with
                    member _.Dispose() =
                        lock sync (fun () -> observers.Remove(observer) |> ignore) }

        member _.Publish(frame: GameFrame) =
            for obs in snapshot () do
                try obs.OnNext(frame)
                with _ -> ()

        member _.CompleteAll() =
            let all = snapshot ()
            for obs in all do
                try obs.OnCompleted()
                with _ -> ()

    [<Sealed>]
    type SessionManager(install: BarInstall.BarInstall, events: HubEvents.IHubEventSink) =
        let sync = obj ()
        let mutable state: SessionState = Idle
        let mutable disposed = 0
        let fanOut = FrameFanOut()

        /// Subscription to the currently-running BarClient's Frames,
        /// if any. Swapped on every session transition.
        let mutable currentSubscription: IDisposable option = None

        // Feature 039: per-session admin channel + host. Set at Launch,
        // cleared at Ending → Idle. When `AdminChannel.bind()` fails at
        // launch the channel slot stays None and the host is constructed
        // in `Unavailable` state (FR-009).
        let mutable adminChannel: AdminChannel.AdminChannel option = None
        let mutable adminHost: AdminChannelHost.AdminChannelHost option = None

        // Feature 039: `startPaused` now flips through the admin
        // channel's Pause command. We defer issuing the first
        // `Pause true` until the autohost `ServerStartPlaying` event
        // arrives — this is the first instant the engine will honor
        // a pause (research.md §R9). Subscription is armed inside
        // Launch and self-disposes after firing once.
        let mutable startPausedForNextLaunch: bool = false
        let mutable startPausedSubscription: IDisposable option = None

        // Feature 042: optional HubLog bus threaded in via AttachLog.
        // Emit sites fire only when this is `Some` — tests that never
        // call AttachLog observe the legacy no-log behaviour.
        let mutable logBusOpt: HubLog.T option = None

        // In-flight BarClient held while state = Starting, so End()
        // can dispose it (killing the engine subprocess) before the
        // background launch task finishes. Cleared when the task
        // transitions to Running or Idle.
        let mutable startingClient: BarClient option = None

        let currentSessionId () : Guid option =
            match state with
            | Running rs -> Some rs.Id
            | Ending rs -> Some rs.Id
            | _ -> None

        let emitLog (category: HubLog.LogCategory) (severity: HubLog.LogSeverity) (build: unit -> string) =
            match logBusOpt with
            | Some bus ->
                HubLog.emit bus category severity (currentSessionId ()) None build
            | None -> ()

        let transitionTo (newState: SessionState) =
            let priorTag = stateTag state
            lock sync (fun () -> state <- newState)
            let newTag = stateTag newState
            events.Publish(HubEvents.StateChanged newTag)
            emitLog HubLog.SessionManager HubLog.Info (fun () ->
                sprintf "session state %A → %A" priorTag newTag)

        let publishDiagnostic (severity: HubEvents.Severity) (msg: string) =
            events.Publish(HubEvents.DiagnosticsLine(severity, msg))
            match logBusOpt with
            | Some bus ->
                HubLog.emitFromDiagnosticsLine bus HubLog.SessionManager severity
                    (currentSessionId ()) None msg
            | None -> ()

        let detachFrames () =
            match currentSubscription with
            | Some sub ->
                try sub.Dispose() with _ -> ()
                currentSubscription <- None
            | None -> ()

        let mutable lastReportedFrame = 0u
        let mutable lastReportTime = DateTimeOffset.UtcNow
        let attachFrames (client: BarClient) =
            detachFrames ()
            lastReportedFrame <- 0u
            lastReportTime <- DateTimeOffset.UtcNow
            let subscription =
                client.Frames.Subscribe(
                    { new IObserver<GameFrame> with
                        member _.OnNext(frame) =
                            let now = DateTimeOffset.UtcNow
                            if (now - lastReportTime).TotalSeconds >= 2.0 then
                                let delta = int frame.FrameNumber - int lastReportedFrame
                                eprintfn "[SessionManager] frame=%d (Δ=%d in 2s)"
                                    frame.FrameNumber delta
                                lastReportedFrame <- frame.FrameNumber
                                lastReportTime <- now
                            fanOut.Publish(frame)
                        member _.OnError(ex) =
                            publishDiagnostic HubEvents.Error
                                (sprintf "BarClient.Frames errored: %s" ex.Message)
                        member _.OnCompleted() =
                            // Engine disconnected or sent shutdown —
                            // collapse to Idle unless the caller
                            // already moved the state forward.
                            lock sync (fun () -> ())
                            match state with
                            | Running _ | Ending _ ->
                                transitionTo Idle
                            | _ -> () })
            currentSubscription <- Some subscription

        let disposeAdminChannel () =
            startPausedSubscription |> Option.iter (fun s ->
                try s.Dispose() with _ -> ())
            startPausedSubscription <- None
            adminHost |> Option.iter (fun h ->
                try (h :> IDisposable).Dispose() with _ -> ())
            adminHost <- None
            adminChannel |> Option.iter (fun c ->
                try (c :> IDisposable).Dispose() with _ -> ())
            adminChannel <- None

        let startSessionBackground (lobby: LobbyConfig.LobbyConfig) (engineCfg: EngineConfig) =
            // Feature 042 R3: capture the caller's correlation ID so
            // background work (admin-channel bind, engine spawn, frame
            // wiring) retains attribution in any `HubLog.emit` calls
            // they issue. `AsyncLocal` flows across Task.Run only while
            // the parent scope is still live — by capturing + re-scoping
            // we make propagation survive even if the handler returns.
            let cid = CorrelationId.current ()
            ignore (System.Threading.Tasks.Task.Run(fun () ->
                use _scope = CorrelationId.withScope cid
                try
                    // Feature 039: bind the admin channel BEFORE the
                    // engine is spawned so the engine's autohost client
                    // can dial back into a socket we already own. On
                    // bind failure, construct the host in `Unavailable`
                    // state and continue the launch in read-only mode
                    // (FR-009) so the Viewer-tab toolbar can render the
                    // reason.
                    let wantStartPaused = startPausedForNextLaunch
                    let initialSpeed = lobby.EngineSpeed
                    startPausedForNextLaunch <- false
                    let engineCfg =
                        match AdminChannel.bind () with
                        | Ok ch ->
                            let port =
                                ch.LocalPort |> Option.defaultValue 0
                            adminChannel <- Some ch
                            let h = AdminChannelHost.attach(ch, events)
                            // Feature 042: propagate the hub log bus to
                            // the per-session admin host so wire-level
                            // emit sites light up for its lifetime.
                            logBusOpt |> Option.iter (fun bus -> h.AttachLog bus)
                            adminHost <- Some h
                            // Subscribe to ServerStartPlaying BEFORE the
                            // engine spawns so we never miss it — the
                            // Events fanOut is hot with no replay, and by
                            // the time MapGrid/MetalSpots finish loading
                            // the event may already have fired.
                            if wantStartPaused || initialSpeed <> 1.0f then
                                let sub =
                                    ch.Events.Subscribe(
                                        { new IObserver<AdminChannel.AdminEventIn> with
                                            member _.OnNext(evt) =
                                                match evt with
                                                | AdminChannel.ServerStartPlaying ->
                                                    adminHost |> Option.iter (fun host ->
                                                        if initialSpeed <> 1.0f then
                                                            host.Submit(AdminChannel.SetGameSpeed initialSpeed) |> ignore
                                                        if wantStartPaused then
                                                            match host.Submit(AdminChannel.Pause true) with
                                                            | AdminChannelHost.Sent ->
                                                                events.Publish(HubEvents.SessionPaused true)
                                                            | _ -> ())
                                                    startPausedSubscription |> Option.iter (fun s ->
                                                        try s.Dispose() with _ -> ())
                                                    startPausedSubscription <- None
                                                | _ -> ()
                                            member _.OnError(_) = ()
                                            member _.OnCompleted() = () })
                                startPausedSubscription <- Some sub
                            { engineCfg with AutohostPort = Some port }
                        | Result.Error reason ->
                            adminChannel <- None
                            let h = AdminChannelHost.unavailable(reason, events)
                            logBusOpt |> Option.iter (fun bus -> h.AttachLog bus)
                            adminHost <- Some h
                            if wantStartPaused || initialSpeed <> 1.0f then
                                publishDiagnostic HubEvents.Warning
                                    "startPaused/initial-speed requested but admin channel is unavailable"
                            publishDiagnostic HubEvents.Warning
                                (sprintf "admin channel unavailable: %s" reason)
                            { engineCfg with AutohostPort = None }
                    let client = BarClient.create engineCfg
                    startingClient <- Some client
                    client.Start()
                    // Load the MapGrid BEFORE subscribing to
                    // `client.Frames`. BarClient.Frames.Subscribe spawns
                    // the async pump thread that reads the stream, and
                    // Callbacks.* requests done concurrently collide
                    // with frame reads ("Protocol message contained an
                    // invalid tag" — the callback response reads bytes
                    // that are actually frame bytes). The stream is
                    // quiescent between Start() and the first
                    // Frames.Subscribe, so this is the safe window.
                    // Failure here is non-fatal — we keep the session
                    // Running with None and the viewer falls back to
                    // its synthetic grid.
                    let mapGrid =
                        try Some (MapGrid.loadFromEngine client.Stream)
                        with ex ->
                            publishDiagnostic HubEvents.Warning
                                (sprintf "MapGrid load failed (viewer will use synthetic grid): %s" ex.Message)
                            None
                    let metalSpots =
                        try Callbacks.getMetalSpots client.Stream
                        with ex ->
                            publishDiagnostic HubEvents.Warning
                                (sprintf "MetalSpots load failed (viewer omits metal markers): %s" ex.Message)
                            [||]
                    // If End() fired while we were spawning, bail instead
                    // of transitioning Idle → Running behind its back.
                    let stillStarting =
                        lock sync (fun () ->
                            match state with Starting _ -> true | _ -> false)
                    if not stillStarting then
                        try (client :> IDisposable).Dispose() with _ -> ()
                        startingClient <- None
                        publishDiagnostic HubEvents.Info
                            "session launch cancelled during warmup"
                    else
                    attachFrames client
                    let rs: RunningSession = {
                        Id = Guid.NewGuid()
                        Config = lobby
                        EngineConfig = engineCfg
                        BarClient = client
                        GraphicalEngineProcess = None
                        StartedAt = DateTimeOffset.UtcNow
                        MapGrid = mapGrid
                        MetalSpots = metalSpots
                    }
                    startingClient <- None
                    transitionTo (Running rs)
                    publishDiagnostic HubEvents.Info
                        (sprintf "session %s started — map=%s vs %s (MapGrid=%s, metalSpots=%d)"
                            (rs.Id.ToString("N").Substring(0, 8))
                            lobby.MapName
                            engineCfg.OpponentAI
                            (match mapGrid with
                             | Some g -> sprintf "%dx%d elmos" g.WidthElmos g.HeightElmos
                             | None -> "synthetic")
                            metalSpots.Length)
                with ex ->
                    publishDiagnostic HubEvents.Error
                        (sprintf "session launch failed: %s" ex.Message)
                    transitionTo (Failed(lobby, ex.Message, None))))

        member _.State =
            lock sync (fun () -> state)

        member _.Frames: IObservable<GameFrame> = fanOut :> IObservable<GameFrame>

        member this.Launch(config: LobbyConfig.LobbyConfig, startPaused: bool) : Result<unit, string> =
            if Volatile.Read(&disposed) <> 0 then
                Result.Error "session manager has been disposed"
            else
                match LobbyConfig.validate install config with
                | Result.Error errs ->
                    let msg =
                        errs
                        |> List.map LobbyConfig.formatError
                        |> String.concat "; "
                    Result.Error (sprintf "lobby validation failed: %s" msg)
                | Ok validated ->
                    match LobbyConfig.toEngineConfig install validated with
                    | Result.Error err ->
                        Result.Error (sprintf "toEngineConfig failed: %s" (LobbyConfig.formatError err))
                    | Ok engineCfg ->
                        // Atomic claim: re-check state and transition under the
                        // same lock so two concurrent callers cannot both pass
                        // the Idle check before either flips to Starting.
                        let claimed =
                            lock sync (fun () ->
                                match state with
                                | Idle | Failed _ ->
                                    startPausedForNextLaunch <- startPaused
                                    state <- Starting validated
                                    true
                                | _ -> false)
                        if not claimed then
                            Result.Error "a session is already active; call End first"
                        else
                            events.Publish(HubEvents.StateChanged (stateTag (Starting validated)))
                            emitLog HubLog.SessionManager HubLog.Info (fun () ->
                                sprintf "session state Idle → Starting (via Launch)")
                            startSessionBackground validated engineCfg
                            Ok ()

        member _.SetSpeed(speed: float32) =
            events.Publish(HubEvents.EngineSpeedChanged speed)

        member _.SetEngineSpeed(speed: float32) : AdminChannelHost.SubmitOutcome =
            emitLog HubLog.SessionManager HubLog.Info (fun () ->
                sprintf "admin dispatch: SetEngineSpeed %f" speed)
            if Single.IsNaN(speed) || Single.IsInfinity(speed) || speed <= 0.0f then
                AdminChannelHost.Rejected "engine speed must be a positive finite number"
            else
                match adminHost with
                | Some h ->
                    let outcome = h.Submit(AdminChannel.SetGameSpeed speed)
                    match outcome with
                    | AdminChannelHost.Sent ->
                        events.Publish(HubEvents.EngineSpeedChanged speed)
                    | _ -> ()
                    outcome
                | None -> AdminChannelHost.Rejected "no active session"

        member this.ForceEnd() : AdminChannelHost.SubmitOutcome =
            emitLog HubLog.SessionManager HubLog.Info (fun () ->
                "admin dispatch: ForceEnd")
            match adminHost with
            | Some h ->
                let outcome = h.Submit(AdminChannel.KillServer)
                match outcome with
                | AdminChannelHost.Sent ->
                    // Arm the escalation watchdog on a background task.
                    // Per research.md §R8: SIGTERM at 5s, SIGKILL at 8s.
                    let cid = CorrelationId.current ()
                    ignore (System.Threading.Tasks.Task.Run(fun () ->
                        use _scope = CorrelationId.withScope cid
                        Thread.Sleep(5000)
                        if this.State <> Idle then
                            match this.State with
                            | Running rs | Ending rs ->
                                publishDiagnostic HubEvents.Warning
                                    "force-end: engine still alive after 5s, escalating"
                                try rs.BarClient.Stop() with _ -> ()
                            | _ -> ()
                            Thread.Sleep(3000)
                            match this.State with
                            | Running rs | Ending rs ->
                                publishDiagnostic HubEvents.Warning
                                    "force-end: SIGKILL escalation at 8s"
                                match rs.GraphicalEngineProcess with
                                | Some p when not p.HasExited ->
                                    try p.Kill(true) with _ -> ()
                                | _ -> ()
                                try (rs.BarClient :> IDisposable).Dispose() with _ -> ()
                            | _ -> ()))
                | _ -> ()
                outcome
            | None -> AdminChannelHost.Rejected "no active session"

        member _.SendAdminMessage(text: string) : AdminChannelHost.SubmitOutcome =
            emitLog HubLog.SessionManager HubLog.Info (fun () ->
                sprintf "admin dispatch: SendAdminMessage (%d chars)" text.Length)
            if String.IsNullOrWhiteSpace(text) then
                AdminChannelHost.Rejected "empty message"
            else
                match adminHost with
                | Some h -> h.Submit(AdminChannel.SayMessage text)
                | None -> AdminChannelHost.Rejected "no active session"

        member _.IsPaused =
            match adminHost with
            | Some h -> h.IsPaused
            | None -> false

        // While paused, the engine sim doesn't tick → the proxy stops
        // sending frames → BarClient's read times out (default 10s) →
        // session ends. We bump the stream read timeout to effectively
        // infinite while paused and restore on resume.
        member private this.SetStreamReadTimeout(timeoutMs: int) =
            match this.State with
            | Running rs ->
                try rs.BarClient.Stream.ReadTimeout <- timeoutMs with _ -> ()
            | _ -> ()

        member private this.RestoreStreamReadTimeout() =
            match this.State with
            | Running rs ->
                let ms = EngineConfig.resolveReadTimeout rs.EngineConfig
                try rs.BarClient.Stream.ReadTimeout <- ms with _ -> ()
            | _ -> ()

        member this.Pause() : AdminChannelHost.SubmitOutcome =
            emitLog HubLog.SessionManager HubLog.Info (fun () -> "admin dispatch: Pause")
            match adminHost with
            | Some h ->
                // Disarm the frame-loop timeout BEFORE the engine pauses
                // so the next blocking read doesn't fire on a stale 10s
                // budget while sim is frozen.
                this.SetStreamReadTimeout(System.Threading.Timeout.Infinite)
                let outcome = h.Submit(AdminChannel.Pause true)
                match outcome with
                | AdminChannelHost.Sent ->
                    events.Publish(HubEvents.SessionPaused true)
                | _ ->
                    // Pause didn't reach the engine — restore the timeout
                    // so the session keeps normal disconnect detection.
                    this.RestoreStreamReadTimeout()
                outcome
            | None ->
                AdminChannelHost.Rejected "no active session"

        member this.Resume() : AdminChannelHost.SubmitOutcome =
            emitLog HubLog.SessionManager HubLog.Info (fun () -> "admin dispatch: Resume")
            match adminHost with
            | Some h ->
                let outcome = h.Submit(AdminChannel.Pause false)
                match outcome with
                | AdminChannelHost.Sent ->
                    this.RestoreStreamReadTimeout()
                    events.Publish(HubEvents.SessionPaused false)
                | _ -> ()
                outcome
            | None ->
                AdminChannelHost.Rejected "no active session"

        member this.TogglePause() =
            if this.IsPaused then this.Resume() |> ignore
            else this.Pause() |> ignore

        member _.AdminStatus : HubEvents.AdminChannelStatus option =
            lock sync (fun () ->
                match state with
                | Idle -> None
                | _ -> adminHost |> Option.map (fun h -> h.Status))

        member this.End() =
            if Volatile.Read(&disposed) <> 0 then () else
            let stateNow = this.State
            match stateNow with
            | Running rs ->
                transitionTo (Ending rs)
                try
                    rs.BarClient.Stop()
                    match rs.GraphicalEngineProcess with
                    | Some p when not p.HasExited ->
                        try p.Kill(true) with _ -> ()
                    | _ -> ()
                    (rs.BarClient :> IDisposable).Dispose()
                with ex ->
                    publishDiagnostic HubEvents.Warning
                        (sprintf "session end encountered: %s" ex.Message)
                detachFrames ()
                disposeAdminChannel ()
                transitionTo Idle
            | Starting lobby ->
                // Engine may still be spawning on the background task.
                // Kill the in-flight client + admin channel and flip to
                // Idle; the background task checks the state before
                // transitioning to Running and bails when it sees Idle.
                transitionTo Idle
                startingClient |> Option.iter (fun c ->
                    try c.Stop() with _ -> ()
                    try (c :> IDisposable).Dispose() with _ -> ())
                startingClient <- None
                detachFrames ()
                disposeAdminChannel ()
                publishDiagnostic HubEvents.Info
                    (sprintf "session cancelled during warmup (map=%s)" lobby.MapName)
            | Ending _ | Idle | Failed _ -> ()

        member _.AttachLog(log: HubLog.T) =
            logBusOpt <- Some log

        member this.Stop() : SubmitOutcome =
            match this.State with
            | Idle -> Rejected "no active session"
            | _ ->
                this.End()
                Sent

        member this.IsLobbyEditable() : bool =
            match this.State with
            | Idle -> true
            | _ -> false

        interface IDisposable with
            member this.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    this.End()
                    fanOut.CompleteAll()

    let create
            (install: BarInstall.BarInstall)
            (events: HubEvents.IHubEventSink)
            : SessionManager =
        new SessionManager(install, events)
