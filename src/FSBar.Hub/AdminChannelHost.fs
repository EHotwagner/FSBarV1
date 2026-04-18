namespace FSBar.Hub

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open FSBar.Client

module AdminChannelHost =

    type AdminChannelStatus = HubEvents.AdminChannelStatus

    type SubmitOutcome =
        | Sent
        | Coalesced of droppedCount: int
        | Rejected of reason: string

    /// Identifies the "kind" of an outbound command for coalescing.
    /// Two submits with the same kind within the quiet window collapse
    /// to the newest — same-kind older submits report `Coalesced`.
    /// `KillServer` and `SayMessage` are NOT coalesced (each one is
    /// independently meaningful).
    type private CommandKind =
        | KindPause
        | KindSpeed
        | KindMessage of slot: int
        | KindKill of slot: int

    /// One queued command + promise back to the submitter.
    type private QueuedCommand = {
        Kind: CommandKind
        Cmd: AdminChannel.AdminCommandOut
        Completion: TaskCompletionSource<SubmitOutcome>
    }

    type private HostMessage =
        | Enqueue of QueuedCommand
        | Flush
        | SetStatus of AdminChannelStatus
        | Shutdown

    // -----------------------------------------------------------------
    // Status observable fan-out

    type private StatusFanOut() =
        let observers = ResizeArray<IObserver<AdminChannelStatus>>()
        let sync = obj ()

        let snapshot () =
            lock sync (fun () -> observers.ToArray())

        interface IObservable<AdminChannelStatus> with
            member _.Subscribe(observer: IObserver<AdminChannelStatus>) =
                if isNull (box observer) then
                    raise (ArgumentNullException("observer"))
                lock sync (fun () -> observers.Add(observer))
                { new IDisposable with
                    member _.Dispose() =
                        lock sync (fun () -> observers.Remove(observer) |> ignore) }

        member _.Publish(status: AdminChannelStatus) =
            for obs in snapshot () do
                try obs.OnNext(status)
                with _ -> ()

        member _.CompleteAll() =
            let all = snapshot ()
            for obs in all do
                try obs.OnCompleted()
                with _ -> ()

    // -----------------------------------------------------------------
    // Host

    [<Sealed>]
    type AdminChannelHost internal (
            channel: AdminChannel.AdminChannel option,
            events: HubEvents.IHubEventSink,
            initialStatus: AdminChannelStatus) =

        let sync = obj ()
        let mutable status: AdminChannelStatus = initialStatus
        let mutable isPaused = false
        let mutable currentSpeed = 1.0f
        let mutable disposed = 0
        let fanOut = StatusFanOut()

        let rejectReason (s: AdminChannelStatus) =
            match s with
            | HubEvents.Attached -> None
            | HubEvents.Unavailable r -> Some r
            | HubEvents.Lost r -> Some r

        let publishStatus (newStatus: AdminChannelStatus) =
            let publish =
                lock sync (fun () ->
                    if status <> newStatus then
                        status <- newStatus
                        true
                    else false)
            if publish then
                fanOut.Publish(newStatus)
                events.Publish(HubEvents.AdminChannelStatusChanged newStatus)

        // Coalescing window pending commands. A same-kind command arriving
        // within 100 ms of an existing pending one drops the older and
        // the older's completion reports `Coalesced`.
        let pending = Dictionary<CommandKind, QueuedCommand>()
        let pendingLock = obj ()
        let mutable droppedSameKind = 0
        let flushTimer = new ManualResetEventSlim(false)

        let sendOne (qc: QueuedCommand) =
            match channel with
            | None ->
                let reason =
                    rejectReason status |> Option.defaultValue "admin channel unavailable"
                qc.Completion.TrySetResult(Rejected reason) |> ignore
            | Some ch ->
                match ch.Send(qc.Cmd) with
                | Ok () ->
                    // Update hub-side optimistic state
                    match qc.Cmd with
                    | AdminChannel.Pause p ->
                        lock sync (fun () -> isPaused <- p)
                    | AdminChannel.SetGameSpeed s ->
                        lock sync (fun () -> currentSpeed <- s)
                    | _ -> ()
                    qc.Completion.TrySetResult(Sent) |> ignore
                | Result.Error e ->
                    qc.Completion.TrySetResult(Rejected e) |> ignore

        // Agent that serializes sends. We run the flush on a short-delay
        // timer so same-kind submits within 100 ms coalesce to the last.
        let agent = MailboxProcessor<HostMessage>.Start(fun inbox ->
            let rec loop () = async {
                let! msg = inbox.Receive()
                match msg with
                | Enqueue qc ->
                    // If the channel is non-Attached and non-None, reject
                    // immediately (invariant I5).
                    let currentStatus = lock sync (fun () -> status)
                    match currentStatus with
                    | HubEvents.Unavailable r | HubEvents.Lost r ->
                        qc.Completion.TrySetResult(Rejected r) |> ignore
                    | HubEvents.Attached ->
                        let supersededOpt =
                            lock pendingLock (fun () ->
                                match pending.TryGetValue(qc.Kind) with
                                | true, prior ->
                                    pending.[qc.Kind] <- qc
                                    Some prior
                                | false, _ ->
                                    pending.[qc.Kind] <- qc
                                    None)
                        match supersededOpt with
                        | Some prior ->
                            Interlocked.Increment(&droppedSameKind) |> ignore
                            prior.Completion.TrySetResult(Coalesced 1) |> ignore
                        | None -> ()
                        // Schedule a flush 100 ms from now (if not already).
                        // Use Async.Start over System.Threading.Timer because
                        // the latter is GC-eligible the moment its constructor
                        // closure exits — so the 100 ms callback can be lost
                        // mid-session and Submit will time out at 500 ms,
                        // making pause/resume "stop working" after a GC pass.
                        // The async closure captures `inbox`, which is rooted
                        // by the agent itself.
                        if not flushTimer.IsSet then
                            flushTimer.Set()
                            Async.Start(async {
                                do! Async.Sleep 100
                                inbox.Post Flush
                            })
                    return! loop ()
                | Flush ->
                    flushTimer.Reset()
                    let due =
                        lock pendingLock (fun () ->
                            let arr = pending.Values |> Seq.toArray
                            pending.Clear()
                            arr)
                    for qc in due do
                        sendOne qc
                    return! loop ()
                | SetStatus s ->
                    publishStatus s
                    // If we just lost the channel, reject any pending.
                    match s with
                    | HubEvents.Unavailable r | HubEvents.Lost r ->
                        let due =
                            lock pendingLock (fun () ->
                                let arr = pending.Values |> Seq.toArray
                                pending.Clear()
                                arr)
                        for qc in due do
                            qc.Completion.TrySetResult(Rejected r) |> ignore
                    | _ -> ()
                    return! loop ()
                | Shutdown ->
                    let due =
                        lock pendingLock (fun () ->
                            let arr = pending.Values |> Seq.toArray
                            pending.Clear()
                            arr)
                    for qc in due do
                        qc.Completion.TrySetResult(Rejected "host disposed") |> ignore
                    return ()
            }
            loop ())

        // Subscribe to the underlying channel's events, if any.
        let channelSubscription =
            channel |> Option.map (fun ch ->
                ch.Events.Subscribe(
                    { new IObserver<AdminChannel.AdminEventIn> with
                        member _.OnNext(evt) =
                            match evt with
                            | AdminChannel.ServerStarted ->
                                agent.Post (SetStatus HubEvents.Attached)
                            | AdminChannel.ServerQuit reason ->
                                let r =
                                    if String.IsNullOrWhiteSpace(reason) then "engine quit"
                                    else reason
                                agent.Post (SetStatus (HubEvents.Lost r))
                            | AdminChannel.GameWarning text ->
                                events.Publish(HubEvents.DiagnosticsLine(HubEvents.Warning, text))
                            | _ -> ()
                        member _.OnError(ex) =
                            agent.Post (SetStatus (HubEvents.Lost (sprintf "channel error: %s" ex.Message)))
                        member _.OnCompleted() =
                            // Only transition to Lost if we were Attached.
                            let cur = lock sync (fun () -> status)
                            match cur with
                            | HubEvents.Attached ->
                                agent.Post (SetStatus (HubEvents.Lost "admin channel closed"))
                            | _ -> () }))

        // Publish the initial status on creation so downstream observers
        // see the `Unavailable` state when the channel failed to bind.
        do
            match initialStatus with
            | HubEvents.Unavailable _ | HubEvents.Lost _ ->
                events.Publish(HubEvents.AdminChannelStatusChanged initialStatus)
            | _ -> ()

        member _.Status =
            lock sync (fun () -> status)

        member _.StatusChanges: IObservable<AdminChannelStatus> =
            fanOut :> IObservable<AdminChannelStatus>

        member this.Submit(cmd: AdminChannel.AdminCommandOut) : SubmitOutcome =
            if Volatile.Read(&disposed) <> 0 then
                Rejected "host disposed"
            else
                // Fast-path rejection when the channel is known unavailable
                // (invariant I5) — don't even enqueue.
                let currentStatus = this.Status
                match currentStatus with
                | HubEvents.Unavailable r | HubEvents.Lost r ->
                    Rejected r
                | HubEvents.Attached ->
                    // Serialize through the agent; wait briefly for the outcome.
                    let kind =
                        match cmd with
                        | AdminChannel.Pause _ -> KindPause
                        | AdminChannel.SetGameSpeed _ -> KindSpeed
                        | AdminChannel.SayMessage _ -> KindMessage (Guid.NewGuid().GetHashCode())
                        | AdminChannel.KillServer -> KindKill (Guid.NewGuid().GetHashCode())
                    let tcs = TaskCompletionSource<SubmitOutcome>()
                    let qc = { Kind = kind; Cmd = cmd; Completion = tcs }
                    agent.Post (Enqueue qc)
                    // The coalesce window is 100 ms; allow ample slack.
                    let gotResult = tcs.Task.Wait(500)
                    if gotResult then tcs.Task.Result
                    else Rejected "submit timed out"

        member _.IsPaused =
            lock sync (fun () -> isPaused)

        member _.CurrentSpeed =
            lock sync (fun () -> currentSpeed)

        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    agent.Post Shutdown
                    channelSubscription |> Option.iter (fun s ->
                        try s.Dispose() with _ -> ())
                    fanOut.CompleteAll()

    let attach
            (channel: AdminChannel.AdminChannel, events: HubEvents.IHubEventSink)
            : AdminChannelHost =
        // Initial status is "Attached optimistic" — the engine may not
        // yet have sent ServerStarted; OnNext will re-fire Attached when
        // the event arrives, which is a no-op status transition.
        new AdminChannelHost(Some channel, events, HubEvents.Attached)

    let unavailable
            (reason: string, events: HubEvents.IHubEventSink)
            : AdminChannelHost =
        new AdminChannelHost(None, events, HubEvents.Unavailable reason)
