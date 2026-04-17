namespace FSBar.Hub

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks

module HubEvents =

    type Severity =
        | Info
        | Warning
        | Error

    type DetachReason =
        | ClientDisconnected
        | OverflowDropLimit
        | ServerShutdown

    type ProxyInstallStep =
        | CopyAiFiles
        | TouchDevMode
        | ToggleSimpleAiList

    type StepOutcome =
        | Skipped
        | Performed
        | StepFailed of reason: string

    type SessionStateTag =
        | Idle
        | Starting
        | Running
        | Ending
        | Failed

    type HubEvent =
        | StateChanged of tag: SessionStateTag
        | EngineSpeedChanged of speed: float32
        | SessionPaused of paused: bool
        | DiagnosticsLine of severity: Severity * message: string
        | ScriptingClientConnected of clientId: Guid * remote: string
        | ScriptingClientDetached of clientId: Guid * reason: DetachReason
        | ProxyInstallProgress of step: ProxyInstallStep * outcome: StepOutcome

    type IHubEventSink =
        abstract Publish: HubEvent -> unit

    type private Subscription(owner: ResizeArray<IObserver<HubEvent>>, sync: obj, observer: IObserver<HubEvent>) =
        let mutable disposed = 0
        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    lock sync (fun () -> owner.Remove(observer) |> ignore)

    [<Sealed>]
    type HubEventBus() =
        let observers = ResizeArray<IObserver<HubEvent>>()
        let sync = obj ()
        let channel = Channel.CreateUnbounded<HubEvent>(
                        UnboundedChannelOptions(SingleReader = true, SingleWriter = false))
        let cts = new CancellationTokenSource()
        let mutable disposed = 0

        let snapshotObservers () =
            lock sync (fun () -> observers.ToArray())

        let pumpLoop () =
            task {
                try
                    let reader = channel.Reader
                    let mutable keepGoing = true
                    while keepGoing do
                        let! ok = reader.WaitToReadAsync(cts.Token)
                        if ok then
                            let mutable evt = Unchecked.defaultof<HubEvent>
                            while reader.TryRead(&evt) do
                                let snapshot = snapshotObservers ()
                                for obs in snapshot do
                                    try obs.OnNext(evt)
                                    with _ -> ()
                        else
                            keepGoing <- false
                with
                | :? OperationCanceledException -> ()
                | _ -> ()

                // Channel drained or cancelled — signal completion to
                // every subscriber exactly once.
                let finalSnapshot = snapshotObservers ()
                for obs in finalSnapshot do
                    try obs.OnCompleted()
                    with _ -> ()
            }

        let pumpTask = pumpLoop ()

        let sink =
            { new IHubEventSink with
                member _.Publish evt =
                    if Volatile.Read(&disposed) = 0 then
                        channel.Writer.TryWrite(evt) |> ignore }

        let events =
            { new IObservable<HubEvent> with
                member _.Subscribe(observer: IObserver<HubEvent>) =
                    if isNull (box observer) then
                        raise (ArgumentNullException("observer"))
                    if Volatile.Read(&disposed) <> 0 then
                        observer.OnCompleted()
                        { new IDisposable with member _.Dispose() = () }
                    else
                        lock sync (fun () -> observers.Add(observer))
                        new Subscription(observers, sync, observer) :> IDisposable }

        member _.Sink = sink
        member _.Events = events

        interface IDisposable with
            member _.Dispose() =
                if Interlocked.Exchange(&disposed, 1) = 0 then
                    channel.Writer.TryComplete() |> ignore
                    cts.Cancel()
                    try pumpTask.Wait(TimeSpan.FromSeconds(1.0)) |> ignore
                    with _ -> ()
                    cts.Dispose()

    let create () = new HubEventBus()
