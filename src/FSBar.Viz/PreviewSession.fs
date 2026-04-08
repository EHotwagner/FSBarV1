namespace FSBar.Viz

open System
open System.Diagnostics
open SkiaSharp
open Silk.NET.Maths
open FSBar.Client
open SkiaViewer

module PreviewSession =

    let private stateLock = obj ()
    let mutable private config = VizDefaults.defaultConfig
    let mutable private viewState = VizDefaults.defaultViewState
    let mutable private viewer: IDisposable option = None
    let mutable private currentSnapshot: GameSnapshot option = None
    let mutable private playbackFrames: GameSnapshot[] option = None
    let mutable private playbackStopwatch: Stopwatch option = None
    let mutable private gameFps = 30

    let private computeAutoFit (grid: MapGrid) (ww: int) (wh: int) =
        if grid.WidthHeightmap <= 0 || grid.HeightHeightmap <= 0 then ()
        else
            let scaleX = float32 ww / float32 grid.WidthHeightmap
            let scaleY = float32 wh / float32 grid.HeightHeightmap
            let s = min scaleX scaleY
            let ox = (float32 ww - float32 grid.WidthHeightmap * s) / 2.0f
            let oy = (float32 wh - float32 grid.HeightHeightmap * s) / 2.0f
            lock stateLock (fun () ->
                viewState <-
                    { viewState with
                        Scale = s
                        OriginX = ox
                        OriginY = oy
                        WindowWidth = ww
                        WindowHeight = wh })

    let private processKeyDown (key: Silk.NET.Input.Key) =
        lock stateLock (fun () ->
            match key with
            | Silk.NET.Input.Key.Number1 -> config <- { config with BaseLayer = LayerKind.HeightMap }
            | Silk.NET.Input.Key.Number2 -> config <- { config with BaseLayer = LayerKind.SlopeMap }
            | Silk.NET.Input.Key.Number3 -> config <- { config with BaseLayer = LayerKind.ResourceMap }
            | Silk.NET.Input.Key.Number4 -> config <- { config with BaseLayer = LayerKind.LosMap }
            | Silk.NET.Input.Key.Number5 -> config <- { config with BaseLayer = LayerKind.RadarMap }
            | Silk.NET.Input.Key.Number6 -> config <- { config with BaseLayer = LayerKind.TerrainClassification }
            | Silk.NET.Input.Key.Number7 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Kbot }
            | Silk.NET.Input.Key.Number8 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Tank }
            | Silk.NET.Input.Key.Number9 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Hover }
            | Silk.NET.Input.Key.Number0 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Ship }
            | Silk.NET.Input.Key.U ->
                if config.ActiveOverlays.Contains OverlayKind.Units then
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Remove OverlayKind.Units }
                else
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Add OverlayKind.Units }
            | Silk.NET.Input.Key.E ->
                if config.ActiveOverlays.Contains OverlayKind.Events then
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Remove OverlayKind.Events }
                else
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Add OverlayKind.Events }
            | Silk.NET.Input.Key.G ->
                if config.ActiveOverlays.Contains OverlayKind.Grid then
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Remove OverlayKind.Grid }
                else
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Add OverlayKind.Grid }
            | Silk.NET.Input.Key.M ->
                if config.ActiveOverlays.Contains OverlayKind.MetalSpots then
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Remove OverlayKind.MetalSpots }
                else
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Add OverlayKind.MetalSpots }
            | Silk.NET.Input.Key.Home ->
                viewState <- { viewState with AutoFit = true }
                match currentSnapshot with
                | Some snap -> computeAutoFit snap.MapGrid viewState.WindowWidth viewState.WindowHeight
                | None -> ()
            | _ -> ())

    let private getSnapshot () =
        lock stateLock (fun () ->
            match playbackFrames, playbackStopwatch with
            | Some frames, Some sw when frames.Length > 0 ->
                let elapsed = sw.Elapsed.TotalSeconds
                let idx = int (elapsed * float gameFps) % frames.Length
                frames.[idx]
            | _ ->
                match currentSnapshot with
                | Some snap -> snap
                | None ->
                    { FrameNumber = 0
                      MapGrid =
                        { WidthElmos = 0; HeightElmos = 0
                          WidthHeightmap = 0; HeightHeightmap = 0
                          HeightMap = Array2D.zeroCreate 1 1
                          SlopeMap = Array2D.zeroCreate 1 1
                          ResourceMap = Array2D.zeroCreate 1 1
                          LosMap = Array2D.zeroCreate 1 1
                          RadarMap = Array2D.zeroCreate 1 1 }
                      Units = Map.empty
                      EventIndicators = []
                      EconomyMetal = VizDefaults.defaultEconomy
                      EconomyEnergy = VizDefaults.defaultEconomy
                      MetalSpots = [||]
                      Connected = false })

    let private doStop () =
        match viewer with
        | Some v ->
            v.Dispose()
            viewer <- None
            LayerRenderer.invalidateAll ()
            currentSnapshot <- None
            playbackFrames <- None
            playbackStopwatch <- None
        | None -> ()

    let private startViewer (initialGrid: MapGrid) =
        doStop ()

        let viewerConfig: ViewerConfig =
            { Title = "FSBar Preview"
              Width = 1024
              Height = 640
              TargetFps = 60
              ClearColor = config.BackgroundColor
              OnRender =
                fun canvas _fbSize ->
                    let snap = getSnapshot ()
                    let cfg = lock stateLock (fun () -> config)
                    let vs = lock stateLock (fun () -> viewState)
                    SceneBuilder.drawFrame canvas snap cfg vs
              OnResize =
                fun w h ->
                    lock stateLock (fun () ->
                        viewState <- { viewState with WindowWidth = w; WindowHeight = h })
                    if viewState.AutoFit then
                        computeAutoFit initialGrid w h
              OnKeyDown = processKeyDown
              OnMouseScroll =
                fun delta cx cy ->
                    let factor = if delta > 0.0f then 1.1f else 0.9f
                    lock stateLock (fun () ->
                        let newScale = viewState.Scale * factor |> max 0.1f |> min 100.0f
                        let ratio = newScale / viewState.Scale
                        let newOx = cx - (cx - viewState.OriginX) * ratio
                        let newOy = cy - (cy - viewState.OriginY) * ratio
                        viewState <-
                            { viewState with
                                Scale = newScale
                                OriginX = newOx
                                OriginY = newOy
                                AutoFit = false })
              OnMouseDrag =
                fun dx dy ->
                    lock stateLock (fun () ->
                        viewState <-
                            { viewState with
                                OriginX = viewState.OriginX + dx
                                OriginY = viewState.OriginY + dy
                                AutoFit = false })
              PreferredBackend = None }

        viewer <- Some(Viewer.run viewerConfig)
        viewState <- { VizDefaults.defaultViewState with AutoFit = true }
        computeAutoFit initialGrid 1024 640

    let startWithMap (grid: MapGrid) : IDisposable =
        let snap = MockSnapshot.emptySnapshot grid
        lock stateLock (fun () -> currentSnapshot <- Some snap)
        startViewer grid
        { new IDisposable with member _.Dispose() = doStop () }

    let startWithSnapshot (snapshot: GameSnapshot) : IDisposable =
        lock stateLock (fun () -> currentSnapshot <- Some snapshot)
        startViewer snapshot.MapGrid
        { new IDisposable with member _.Dispose() = doStop () }

    let startPlayback (frames: GameSnapshot seq) (gameFps': int) : IDisposable =
        let arr = frames |> Seq.toArray
        if arr.Length = 0 then failwith "Playback sequence must have at least one frame"
        lock stateLock (fun () ->
            playbackFrames <- Some arr
            currentSnapshot <- Some arr.[0]
            gameFps <- gameFps'
            playbackStopwatch <- Some(Stopwatch.StartNew()))
        startViewer arr.[0].MapGrid
        { new IDisposable with member _.Dispose() = doStop () }

    let stop () = doStop ()
