namespace FSBar.Hub

open System
open System.Collections.Concurrent
open System.Threading
open System.Threading.Channels
open System.Threading.Tasks
open SkiaSharp
open SkiaViewer
open FSBar.Client
open FSBar.Viz

type ImageFormat =
    | Png
    | Jpeg

type RenderFrameMessage =
    { ImageBytes: byte[]
      Format: ImageFormat
      RenderedAtUnixMs: int64
      EncodedAtUnixMs: int64
      ClientSequence: uint64
      ViewportWidth: int
      ViewportHeight: int
      Quality: int
      IsPlaceholder: bool }

type RenderSubscriptionRequest =
    { ClientLabel: string
      TargetHz: int
      Format: ImageFormat
      ViewportWidth: int
      ViewportHeight: int
      JpegQuality: int
      CloseOnSessionEnd: bool
      EmitNoSessionPlaceholder: bool }

type RenderSubscription = {
    Id: Guid
    Channel: ChannelReader<RenderFrameMessage>
    Dispose: unit -> unit
}

type SubscribeOutcome =
    | Subscribed of RenderSubscription
    | SubscribeRejected of reason: string

module HeadlessRenderer =

    let private unixMillis () =
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    let private clampHz (requested: int) : int =
        if requested <= 0 then 10
        elif requested > 30 then 30
        else requested

    let private clampViewport (w: int) (h: int) : int * int =
        let cw = if w <= 0 then 1024 elif w > 3840 then 3840 else w
        let ch = if h <= 0 then 768 elif h > 2160 then 2160 else h
        cw, ch

    let private clampJpegQuality (q: int) : int =
        if q <= 0 then 85
        elif q > 100 then 100
        else q

    /// Build the viz snapshot to rasterize, or return a placeholder
    /// scene when no session is running. Placeholder is a simple
    /// background fill + status text so the client gets *something*
    /// rather than a silent stream.
    let private buildScene
            (sm: SessionManager.SessionManager)
            (camera: ViewerCamera)
            (vizConfig: VizConfig)
            (viewportW: int)
            (viewportH: int)
            : Scene * bool =
        match sm.State with
        | SessionManager.Running rs ->
            let viewState : ViewState =
                { Scale = camera.Scale
                  OriginX = camera.OriginX
                  OriginY = camera.OriginY
                  WindowWidth = viewportW
                  WindowHeight = viewportH
                  AutoFit = camera.AutoFit }
            let state =
                try rs.BarClient.GameState
                with _ -> FSBar.Client.GameState.empty
            let unitDefs =
                try Some rs.BarClient.GameState.UnitDefs
                with _ -> None
            let scene =
                SceneBuilder.buildSceneHeadlessView
                    state rs.MapGrid rs.MetalSpots unitDefs vizConfig viewState
            scene, false
        | _ ->
            // Placeholder: grey background + "No session" label.
            let bg = SKColor(0x0cuy, 0x10uy, 0x18uy, 0xffuy)
            let labelPaint = Scene.fill (SKColor(0xa0uy, 0xa8uy, 0xb4uy, 0xffuy))
            let scene : Scene =
                { BackgroundColor = bg
                  Elements =
                      [ Scene.text
                            "No active session — awaiting LaunchSession …"
                            (float32 viewportW * 0.5f - 180.0f)
                            (float32 viewportH * 0.5f)
                            16.0f
                            labelPaint ] }
            scene, true

    /// Render a Scene to an SKSurface via the public
    /// `Scene.recordPicture` helper (SceneRenderer is internal).
    let private rasterize
            (scene: Scene)
            (viewportW: int)
            (viewportH: int)
            : SKImage =
        let info = SKImageInfo(viewportW, viewportH, SKColorType.Rgba8888, SKAlphaType.Premul)
        use surface = SKSurface.Create(info)
        let canvas = surface.Canvas
        canvas.Clear(scene.BackgroundColor)
        let bounds = SKRect(0.0f, 0.0f, float32 viewportW, float32 viewportH)
        use picture = Scene.recordPicture bounds scene.Elements
        canvas.DrawPicture(picture)
        canvas.Flush()
        surface.Snapshot()

    let private encodeImage
            (image: SKImage) (format: ImageFormat) (jpegQuality: int)
            : byte[] =
        let skFormat, quality =
            match format with
            | Png -> SKEncodedImageFormat.Png, 100
            | Jpeg -> SKEncodedImageFormat.Jpeg, clampJpegQuality jpegQuality
        use data = image.Encode(skFormat, quality)
        data.ToArray()

    type private SubscriberRegistration = {
        Id: Guid
        Label: string
        Channel: Channel<RenderFrameMessage>
        Request: RenderSubscriptionRequest
        mutable DropCount: int
        mutable Sequence: uint64
        Cancellation: CancellationTokenSource
        Worker: Task
    }

    [<Sealed>]
    type T(
            sessions: SessionManager.SessionManager,
            store: HubStateStore.T,
            overlays: OverlayLayerStore.T,
            settings: unit -> HubSettings.HubSettings) =

        let subscribers = ConcurrentDictionary<Guid, SubscriberRegistration>()

        // `overlays` is retained here; US2 renders base-scene only. US6
        // composes primitives from the store on top via a follow-up change.
        let _ = overlays

        let channelOpts =
            BoundedChannelOptions(16,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true)

        /// Render + encode a single frame into a RenderFrameMessage.
        member this.RenderOnce(format, viewportW, viewportH, jpegQuality) =
            let vw, vh = clampViewport viewportW viewportH
            let q = clampJpegQuality jpegQuality
            let s = HubStateStore.current store
            let scene, isPlaceholder = buildScene sessions s.Camera s.VizConfig vw vh
            let renderedAt = unixMillis ()
            use image = rasterize scene vw vh
            let bytes = encodeImage image format q
            let encodedAt = unixMillis ()
            { ImageBytes = bytes
              Format = format
              RenderedAtUnixMs = renderedAt
              EncodedAtUnixMs = encodedAt
              ClientSequence = 0UL
              ViewportWidth = vw
              ViewportHeight = vh
              Quality = q
              IsPlaceholder = isPlaceholder }

        member this.Subscribe(request: RenderSubscriptionRequest) : SubscribeOutcome =
            let current = settings ()
            if subscribers.Count >= current.MaxRenderFrameSubscribers then
                SubscribeRejected "max subscribers reached"
            else
                let id = Guid.NewGuid()
                let label =
                    if String.IsNullOrEmpty(request.ClientLabel) then
                        id.ToString("N").Substring(0, 8)
                    else request.ClientLabel
                let ch = Channel.CreateBounded<RenderFrameMessage>(channelOpts)
                let cts = new CancellationTokenSource()
                let hz = clampHz request.TargetHz
                let tickMs = 1000 / hz
                let work () =
                    task {
                        try
                            while not cts.IsCancellationRequested do
                                let shouldEmit =
                                    match sessions.State with
                                    | SessionManager.Running _ -> true
                                    | _ -> request.EmitNoSessionPlaceholder
                                if shouldEmit then
                                    try
                                        let msg =
                                            this.RenderOnce(
                                                request.Format,
                                                request.ViewportWidth,
                                                request.ViewportHeight,
                                                request.JpegQuality)
                                        // Stamp a subscriber-local sequence number.
                                        match subscribers.TryGetValue(id) with
                                        | true, reg ->
                                            let seq = Interlocked.Increment(&reg.Sequence) |> uint64
                                            let stamped = { msg with ClientSequence = seq }
                                            if not (ch.Writer.TryWrite(stamped)) then
                                                Interlocked.Increment(&reg.DropCount) |> ignore
                                        | _ -> ()
                                    with _ -> ()
                                do! Task.Delay(tickMs, cts.Token)
                        with
                        | :? OperationCanceledException -> ()
                        | _ -> ()
                        ch.Writer.TryComplete() |> ignore
                    } :> Task
                let reg = {
                    Id = id
                    Label = label
                    Channel = ch
                    Request = request
                    DropCount = 0
                    Sequence = 0UL
                    Cancellation = cts
                    Worker = Unchecked.defaultof<Task>
                }
                subscribers.[id] <- reg
                // Start the worker AFTER registration so the stamping
                // lookup always hits.
                let t = work ()
                // Patch Worker field via a replacement entry; F# records
                // with mutable fields require this dance because
                // `reg.Worker` is set-once to Unchecked.defaultof above.
                subscribers.[id] <- { reg with Worker = t }
                let dispose () =
                    try cts.Cancel() with _ -> ()
                    subscribers.TryRemove(id) |> ignore
                let sub = {
                    Id = id
                    Channel = ch.Reader
                    Dispose = dispose
                }
                Subscribed sub

        member _.SubscriberCount = subscribers.Count

    let create sessions store overlays settings = T(sessions, store, overlays, settings)

    let subscribe (t: T) (request: RenderSubscriptionRequest) =
        t.Subscribe(request)

    let renderOnce (t: T) format viewportW viewportH jpegQuality =
        t.RenderOnce(format, viewportW, viewportH, jpegQuality)

    let subscriberCount (t: T) = t.SubscriberCount
