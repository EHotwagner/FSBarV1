namespace FSBar.Hub

open System
open System.Collections.Concurrent
open System.Diagnostics
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

    // --- Overlay composite helpers (feature 041 US1) ------------------

    let private cameraToMatrix (c: ViewerCamera) : SKMatrix =
        // Mirrors SceneBuilder.viewportTransform: pixel = (world − Origin) * Scale.
        SKMatrix.CreateScaleTranslation(
            c.Scale, c.Scale,
            -c.OriginX * c.Scale, -c.OriginY * c.Scale)

    let private toSKColor (rgba: uint32) =
        let r = byte ((rgba >>> 24) &&& 0xFFu)
        let g = byte ((rgba >>> 16) &&& 0xFFu)
        let b = byte ((rgba >>> 8) &&& 0xFFu)
        let a = byte (rgba &&& 0xFFu)
        SKColor(r, g, b, a)

    let private applyOpacity (c: SKColor) (opacity: float32) : SKColor =
        if opacity >= 1.0f then c
        else
            let alpha = byte (max 0.0f (min 255.0f (float32 c.Alpha * opacity)))
            c.WithAlpha(alpha)

    let private mkStrokePaint (style: OverlayStyle) : SKPaint =
        let p = new SKPaint()
        p.IsAntialias <- true
        p.Style <- SKPaintStyle.Stroke
        p.Color <- applyOpacity (toSKColor style.StrokeColorRgba) style.Opacity
        p.StrokeWidth <- style.StrokeWidth
        match style.Dash with
        | Some dashes when dashes.Length >= 2 ->
            p.PathEffect <- SKPathEffect.CreateDash(dashes, 0.0f)
        | _ -> ()
        p

    let private mkFillPaint (rgba: uint32) (opacity: float32) : SKPaint =
        let p = new SKPaint()
        p.IsAntialias <- true
        p.Style <- SKPaintStyle.Fill
        p.Color <- applyOpacity (toSKColor rgba) opacity
        p

    let private xformPoint (m: SKMatrix) (space: CoordinateSpace) (p: OverlayPoint) : SKPoint =
        match space with
        | World -> m.MapPoint(p.X, p.Y)
        | Screen -> SKPoint(p.X, p.Y)

    let private xformLength (m: SKMatrix) (space: CoordinateSpace) (len: float32) : float32 =
        match space with
        | World -> len * m.ScaleX
        | Screen -> len

    let private buildPath (m: SKMatrix) (space: CoordinateSpace) (verbs: PathVerb list) : SKPath =
        let path = new SKPath()
        for v in verbs do
            match v with
            | MoveTo p ->
                let q = xformPoint m space p
                path.MoveTo(q)
            | LineTo p ->
                let q = xformPoint m space p
                path.LineTo(q)
            | CubicTo (c1, c2, p) ->
                let q1 = xformPoint m space c1
                let q2 = xformPoint m space c2
                let q3 = xformPoint m space p
                path.CubicTo(q1, q2, q3)
            | Close ->
                path.Close()
        path

    let private drawPrimitive (canvas: SKCanvas) (m: SKMatrix) (prim: OverlayPrimitive) : unit =
        match prim with
        | Line (a, b, style, space) ->
            use paint = mkStrokePaint style
            let pa = xformPoint m space a
            let pb = xformPoint m space b
            canvas.DrawLine(pa, pb, paint)
        | Polyline (points, style, space) ->
            use paint = mkStrokePaint style
            let path = new SKPath()
            try
                let mutable first = true
                for p in points do
                    let q = xformPoint m space p
                    if first then path.MoveTo(q); first <- false
                    else path.LineTo(q)
                canvas.DrawPath(path, paint)
            finally
                path.Dispose()
        | Polygon (points, style, space) ->
            use paint = mkStrokePaint style
            let path = new SKPath()
            try
                let mutable first = true
                for p in points do
                    let q = xformPoint m space p
                    if first then path.MoveTo(q); first <- false
                    else path.LineTo(q)
                path.Close()
                match style.FillColorRgba with
                | Some fill ->
                    use fillPaint = mkFillPaint fill style.Opacity
                    canvas.DrawPath(path, fillPaint)
                | None -> ()
                canvas.DrawPath(path, paint)
            finally
                path.Dispose()
        | Rectangle (x, y, w, h, cornerRadius, style, space) ->
            let topLeft = xformPoint m space { X = x; Y = y }
            let sw = xformLength m space w
            let sh = xformLength m space h
            let sr = xformLength m space cornerRadius
            let rect = SKRect(topLeft.X, topLeft.Y, topLeft.X + sw, topLeft.Y + sh)
            match style.FillColorRgba with
            | Some fill ->
                use fillPaint = mkFillPaint fill style.Opacity
                if sr > 0.0f then canvas.DrawRoundRect(rect, sr, sr, fillPaint)
                else canvas.DrawRect(rect, fillPaint)
            | None -> ()
            use strokePaint = mkStrokePaint style
            if sr > 0.0f then canvas.DrawRoundRect(rect, sr, sr, strokePaint)
            else canvas.DrawRect(rect, strokePaint)
        | Circle (center, radius, style, space) ->
            let c = xformPoint m space center
            let r = xformLength m space radius
            match style.FillColorRgba with
            | Some fill ->
                use fillPaint = mkFillPaint fill style.Opacity
                canvas.DrawCircle(c, r, fillPaint)
            | None -> ()
            use strokePaint = mkStrokePaint style
            canvas.DrawCircle(c, r, strokePaint)
        | Path (verbs, style, space) ->
            let path = buildPath m space verbs
            try
                match style.FillColorRgba with
                | Some fill ->
                    use fillPaint = mkFillPaint fill style.Opacity
                    canvas.DrawPath(path, fillPaint)
                | None -> ()
                use strokePaint = mkStrokePaint style
                canvas.DrawPath(path, strokePaint)
            finally
                path.Dispose()
        | Text (anchor, text, fontSize, fontFamily, align, style, space) ->
            // Text uses the stroke color as fill; font size is in viewport
            // pixels regardless of coordinate space (data-model §2).
            let pos = xformPoint m space anchor
            use paint = new SKPaint()
            paint.IsAntialias <- true
            paint.Style <- SKPaintStyle.Fill
            paint.Color <- applyOpacity (toSKColor style.StrokeColorRgba) style.Opacity
            let mutable ownedTypeface : SKTypeface = null
            let typeface =
                if String.IsNullOrEmpty(fontFamily) then SKTypeface.Default
                else
                    let t = SKTypeface.FromFamilyName(fontFamily)
                    ownedTypeface <- t
                    t
            use font = new SKFont(typeface, fontSize)
            let textAlign =
                match align with
                | Left -> SKTextAlign.Left
                | Center -> SKTextAlign.Center
                | Right -> SKTextAlign.Right
            canvas.DrawText(text, pos.X, pos.Y, textAlign, font, paint)
            if not (isNull ownedTypeface) then ownedTypeface.Dispose()
        | Image (anchor, w, h, bytes, space) ->
            try
                use data = SKData.CreateCopy(bytes)
                use img = SKImage.FromEncodedData(data)
                if not (isNull img) then
                    let pos = xformPoint m space anchor
                    let sw = xformLength m space (float32 w)
                    let sh = xformLength m space (float32 h)
                    let dest = SKRect(pos.X, pos.Y, pos.X + sw, pos.Y + sh)
                    canvas.DrawImage(img, dest)
            with _ -> ()

    let private compositeOverlays
            (canvas: SKCanvas)
            (snapshot: OverlayLayerSnapshot)
            (camera: ViewerCamera)
            : int =
        // FR-001 / FR-002 / FR-003 / FR-004 / FR-005:
        // iterate pre-sorted entries, draw each primitive with a
        // per-frame world-matrix; Screen primitives bypass the matrix.
        let m = cameraToMatrix camera
        let mutable count = 0
        for (_, layer) in snapshot.Entries do
            for prim in layer.Primitives do
                drawPrimitive canvas m prim
                count <- count + 1
        count

    /// Render a Scene + overlay snapshot to an SKSurface and return the
    /// composited image. The overlay composite pass is timed; a P95-overrun
    /// emits `HubEvent.DiagnosticsLine Warning` via `events` (FR-006a / R2)
    /// but the frame still ships with all overlays drawn.
    let private rasterize
            (scene: Scene)
            (viewportW: int)
            (viewportH: int)
            (snapshot: OverlayLayerSnapshot)
            (camera: ViewerCamera)
            (subscriberCount: int)
            (events: HubEvents.IHubEventSink)
            : SKImage =
        let info = SKImageInfo(viewportW, viewportH, SKColorType.Rgba8888, SKAlphaType.Premul)
        use surface = SKSurface.Create(info)
        let canvas = surface.Canvas
        canvas.Clear(scene.BackgroundColor)
        let bounds = SKRect(0.0f, 0.0f, float32 viewportW, float32 viewportH)
        use picture = Scene.recordPicture bounds scene.Elements
        canvas.DrawPicture(picture)
        let sw = Stopwatch.StartNew()
        let primitiveCount = compositeOverlays canvas snapshot camera
        sw.Stop()
        canvas.Flush()
        let elapsedMs = float sw.ElapsedTicks * 1000.0 / float Stopwatch.Frequency
        if elapsedMs > 5.0 && primitiveCount > 0 then
            let msg =
                sprintf "HeadlessRenderer overlay composite over budget: %.1f ms (budget 5.0 ms), %d primitives, %d subscribers"
                    elapsedMs primitiveCount subscriberCount
            try events.Publish(HubEvents.DiagnosticsLine(HubEvents.Severity.Warning, msg))
            with _ -> ()
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
            events: HubEvents.IHubEventSink,
            settings: unit -> HubSettings.HubSettings) =

        let subscribers = ConcurrentDictionary<Guid, SubscriberRegistration>()

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
            let snapshot = OverlayLayerStore.snapshot overlays
            let renderedAt = unixMillis ()
            use image = rasterize scene vw vh snapshot s.Camera subscribers.Count events
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

    let create sessions store overlays events settings = T(sessions, store, overlays, events, settings)

    let subscribe (t: T) (request: RenderSubscriptionRequest) =
        t.Subscribe(request)

    let renderOnce (t: T) format viewportW viewportH jpegQuality =
        t.RenderOnce(format, viewportW, viewportH, jpegQuality)

    let subscriberCount (t: T) = t.SubscriberCount
