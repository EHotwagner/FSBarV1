namespace FSBar.Viz

open System
open System.Threading
open FSBar.Client

[<Sealed>]
type LiveSessionHandle(client: BarClient, ownsClient: bool) =
    let mutable running = true
    let mutable frameCount = 0
    let mutable lastError: string option = None
    let mutable stepThread: Thread option = None

    member internal _.SetStepThread(t: Thread) = stepThread <- Some t
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

                // Wait for step thread to finish
                match stepThread with
                | Some t when t.IsAlive ->
                    if not (t.Join(TimeSpan.FromSeconds(5.0))) then
                        printfn "[LiveSession] Warning: step thread did not exit within 5s"
                | _ -> ()

                GameViz.stop ()

                if ownsClient then
                    try client.Stop()
                    with ex -> printfn "[LiveSession] Warning: client.Stop() failed: %s" ex.Message

                printfn "[LiveSession] Stopped. Frames processed: %d" frameCount

module LiveSession =

    let private runStepLoop (handle: LiveSessionHandle) (client: BarClient) =
        let thread =
            Thread(fun () ->
                printfn "[LiveSession] Step loop started."
                try
                    for frame in client.Frames do
                        if not handle.IsRunning then ()
                        else
                            GameViz.onFrame frame
                            handle.IncrementFrameCount()
                with
                | ex ->
                    if handle.IsRunning then
                        handle.SetLastError(ex.Message)
                        printfn "[LiveSession] Error in step loop: %s" ex.Message
                        GameViz.setDisconnected ()
                        handle.SetRunning(false)

                printfn "[LiveSession] Step loop ended.")

        thread.IsBackground <- true
        thread.Name <- "LiveSession-StepLoop"
        handle.SetStepThread(thread)
        thread.Start()

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

            runStepLoop handle client
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

        runStepLoop handle client
        handle
