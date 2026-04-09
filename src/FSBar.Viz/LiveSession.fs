namespace FSBar.Viz

open System
open System.Threading
open FSBar.Client

[<Sealed>]
type LiveSessionHandle(client: BarClient, ownsClient: bool) =
    let mutable running = true
    let mutable frameCount = 0
    let mutable lastError: string option = None
    let mutable subscription: IDisposable option = None

    member internal _.SetSubscription(s: IDisposable) = subscription <- Some s
    member internal _.SetRunning(v: bool) = running <- v
    member internal _.IncrementFrameCount() = frameCount <- Interlocked.Increment(&frameCount)
    member internal _.SetLastError(msg: string) = lastError <- Some msg

    member _.FrameCount = frameCount
    member _.IsRunning = running
    member _.LastError = lastError

    interface IDisposable with
        member this.Dispose() =
            if running then
                running <- false
                printfn "[LiveSession] Stopping..."

                // Unsubscribe from observable
                match subscription with
                | Some s -> s.Dispose()
                | None -> ()
                subscription <- None

                GameViz.stop ()

                if ownsClient then
                    try client.Stop()
                    with ex -> printfn "[LiveSession] Warning: client.Stop() failed: %s" ex.Message

                printfn "[LiveSession] Stopped. Frames processed: %d" frameCount

module LiveSession =

    let private subscribeToFrames (handle: LiveSessionHandle) (client: BarClient) =
        let sub = client.Frames.Subscribe(
            { new IObserver<GameFrame> with
                member _.OnNext(frame) =
                    if handle.IsRunning then
                        GameViz.onFrame frame
                        handle.IncrementFrameCount()
                member _.OnCompleted() =
                    if handle.IsRunning then
                        printfn "[LiveSession] Stream completed."
                        GameViz.setDisconnected ()
                        handle.SetRunning(false)
                member _.OnError(ex) =
                    if handle.IsRunning then
                        handle.SetLastError(ex.Message)
                        printfn "[LiveSession] Error in stream: %s" ex.Message
                        GameViz.setDisconnected ()
                        handle.SetRunning(false) })
        handle.SetSubscription(sub)

    let start (engineConfig: EngineConfig) (vizConfig: VizConfig option) : LiveSessionHandle =
        printfn "[LiveSession] Starting with engine: %s, map: %s" engineConfig.EngineBin engineConfig.MapName

        let client = new BarClient(engineConfig)
        let handle = new LiveSessionHandle(client, ownsClient = true)

        try
            client.Start()
            printfn "[LiveSession] Engine connected. Handshake: %A" client.Handshake

            GameViz.start vizConfig
            GameViz.attachToClient client
            printfn "[LiveSession] Visualization attached."

            subscribeToFrames handle client
            handle
        with ex ->
            handle.SetRunning(false)
            handle.SetLastError(ex.Message)
            try client.Stop() with _ -> ()
            reraise ()

    let startWithClient (client: BarClient) (vizConfig: VizConfig option) : LiveSessionHandle =
        printfn "[LiveSession] Starting with existing client."
        let handle = new LiveSessionHandle(client, ownsClient = false)

        GameViz.start vizConfig
        GameViz.attachToClient client
        printfn "[LiveSession] Visualization attached."

        subscribeToFrames handle client
        handle
