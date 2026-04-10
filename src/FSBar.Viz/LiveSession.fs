namespace FSBar.Viz

open System
open FSBar.Client

[<Sealed>]
type LiveSessionHandle private (client: BarClient option, ownsClient: bool) =
    let mutable frameCount = 0
    let mutable isRunning = true
    let mutable lastError: string option = None
    let mutable subscription: IDisposable option = None

    member _.FrameCount = frameCount
    member _.IsRunning = isRunning
    member _.LastError = lastError

    member internal _.SetSubscription(sub: IDisposable) =
        subscription <- Some sub

    member internal _.IncrementFrame() =
        frameCount <- frameCount + 1

    member internal _.SetStopped(?error: string) =
        isRunning <- false
        lastError <- error
        eprintfn "[LiveSession] Stopped%s" (error |> Option.map (sprintf ": %s") |> Option.defaultValue "")

    interface IDisposable with
        member this.Dispose() =
            isRunning <- false
            subscription |> Option.iter (fun s -> s.Dispose())
            subscription <- None
            GameViz.stop ()
            if ownsClient then
                client |> Option.iter (fun c -> c.Stop())
            eprintfn "[LiveSession] Disposed"

    static member internal Create(client: BarClient option, ownsClient: bool) =
        new LiveSessionHandle(client, ownsClient)

module LiveSession =

    let start (engineConfig: EngineConfig) (vizConfig: VizConfig option) =
        let client = new BarClient(engineConfig)
        client.Start()
        eprintfn "[LiveSession] Engine started"
        GameViz.start vizConfig
        GameViz.attachToClient client
        let handle = LiveSessionHandle.Create(Some client, true)
        let sub =
            client.Frames
            |> Observable.subscribe (fun frame ->
                try
                    GameViz.onFrame frame
                    handle.IncrementFrame()
                with ex ->
                    eprintfn "[LiveSession] Frame error: %s" ex.Message)
        handle.SetSubscription(sub)
        // Handle stream completion
        let completionSub =
            client.Frames
            |> Observable.subscribe (fun _ -> ())
        // Note: Observable completion is handled when the stream naturally ends
        handle

    let startWithClient (client: BarClient) (vizConfig: VizConfig option) =
        GameViz.start vizConfig
        GameViz.attachToClient client
        let handle = LiveSessionHandle.Create(Some client, false)
        let sub =
            client.Frames
            |> Observable.subscribe (fun frame ->
                try
                    GameViz.onFrame frame
                    handle.IncrementFrame()
                with ex ->
                    eprintfn "[LiveSession] Frame error: %s" ex.Message)
        handle.SetSubscription(sub)
        handle
