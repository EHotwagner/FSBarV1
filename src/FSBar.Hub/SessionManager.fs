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

        let transitionTo (newState: SessionState) =
            lock sync (fun () -> state <- newState)
            events.Publish(HubEvents.StateChanged (stateTag newState))

        let publishDiagnostic (severity: HubEvents.Severity) (msg: string) =
            events.Publish(HubEvents.DiagnosticsLine(severity, msg))

        let detachFrames () =
            match currentSubscription with
            | Some sub ->
                try sub.Dispose() with _ -> ()
                currentSubscription <- None
            | None -> ()

        let attachFrames (client: BarClient) =
            detachFrames ()
            let subscription =
                client.Frames.Subscribe(
                    { new IObserver<GameFrame> with
                        member _.OnNext(frame) = fanOut.Publish(frame)
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

        let startSessionBackground (lobby: LobbyConfig.LobbyConfig) (engineCfg: EngineConfig) =
            ignore (System.Threading.Tasks.Task.Run(fun () ->
                try
                    let client = BarClient.create engineCfg
                    client.Start()
                    attachFrames client
                    let rs: RunningSession = {
                        Id = Guid.NewGuid()
                        Config = lobby
                        EngineConfig = engineCfg
                        BarClient = client
                        GraphicalEngineProcess = None
                        StartedAt = DateTimeOffset.UtcNow
                    }
                    transitionTo (Running rs)
                    publishDiagnostic HubEvents.Info
                        (sprintf "session %s started — map=%s vs %s"
                            (rs.Id.ToString("N").Substring(0, 8))
                            lobby.MapName
                            engineCfg.OpponentAI)
                with ex ->
                    publishDiagnostic HubEvents.Error
                        (sprintf "session launch failed: %s" ex.Message)
                    transitionTo (Failed(lobby, ex.Message, None))))

        member _.State =
            lock sync (fun () -> state)

        member _.Frames: IObservable<GameFrame> = fanOut :> IObservable<GameFrame>

        member this.Launch(config: LobbyConfig.LobbyConfig) : Result<unit, string> =
            if Volatile.Read(&disposed) <> 0 then
                Result.Error "session manager has been disposed"
            else
                match this.State with
                | Starting _ | Running _ | Ending _ ->
                    Result.Error "a session is already active; call End first"
                | Idle | Failed _ ->
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
                            transitionTo (Starting validated)
                            startSessionBackground validated engineCfg
                            Ok ()

        member _.SetSpeed(speed: float32) =
            events.Publish(HubEvents.EngineSpeedChanged speed)

        member _.SetPaused(paused: bool) =
            events.Publish(HubEvents.SessionPaused paused)

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
                transitionTo Idle
            | Ending _ | Idle | Failed _ | Starting _ -> ()

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
