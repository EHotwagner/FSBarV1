namespace FSBar.Viz

open System
open System.Diagnostics
open SkiaSharp
open SkiaViewer
open FSBar.Client
open Silk.NET.Input

module PreviewSession =

    let private stateLock = obj ()
    let mutable private config = VizDefaults.defaultConfig
    let mutable private viewState = VizDefaults.defaultViewState
    let mutable private currentSnapshot: GameSnapshot option = None
    let mutable private playbackFrames: GameSnapshot array = [||]
    let mutable private playbackStopwatch: Stopwatch option = None
    let mutable private gameFpsVal = 30
    let mutable private viewer: ViewerHandle option = None
    let mutable private sceneEvent: Event<Scene> option = None
    let mutable private inputSub: IDisposable option = None
    let mutable private dragStart: (float32 * float32) option = None
    let mutable private dragOrigin: (float32 * float32) option = None
    let mutable private autoFitDone = false

    let private computeAutoFit (grid: MapGrid) =
        let mapW = float32 grid.WidthHeightmap
        let mapH = float32 grid.HeightHeightmap
        let scaleX = float32 viewState.WindowWidth / mapW
        let scaleY = float32 viewState.WindowHeight / mapH
        let scale = min scaleX scaleY
        viewState <- { viewState with Scale = scale; OriginX = 0.0f; OriginY = 0.0f; AutoFit = false }

    let private getSnapshot () =
        if playbackFrames.Length > 0 then
            match playbackStopwatch with
            | Some sw ->
                let elapsed = sw.Elapsed.TotalSeconds
                let frameIdx = int (elapsed * float gameFpsVal) % playbackFrames.Length
                Some playbackFrames.[frameIdx]
            | None -> currentSnapshot
        else
            currentSnapshot

    let private emitScene () =
        match sceneEvent, getSnapshot () with
        | Some evt, Some snap ->
            let scene = SceneBuilder.buildScene snap config viewState
            evt.Trigger scene
        | _ -> ()

    let private processKey (key: Key) =
        lock stateLock (fun () ->
            match key with
            | Key.Number1 -> config <- { config with BaseLayer = LayerKind.HeightMap }; LayerRenderer.invalidateCache LayerKind.HeightMap
            | Key.Number2 -> config <- { config with BaseLayer = LayerKind.SlopeMap }; LayerRenderer.invalidateCache LayerKind.SlopeMap
            | Key.Number3 -> config <- { config with BaseLayer = LayerKind.ResourceMap }
            | Key.Number4 -> config <- { config with BaseLayer = LayerKind.LosMap }
            | Key.Number5 -> config <- { config with BaseLayer = LayerKind.RadarMap }
            | Key.Number6 -> config <- { config with BaseLayer = LayerKind.TerrainClassification }
            | Key.Number7 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Kbot }
            | Key.Number8 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Tank }
            | Key.Number9 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Hover }
            | Key.Number0 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Ship }
            | Key.U ->
                let overlays = if Set.contains OverlayKind.Units config.ActiveOverlays then Set.remove OverlayKind.Units config.ActiveOverlays else Set.add OverlayKind.Units config.ActiveOverlays
                config <- { config with ActiveOverlays = overlays }
            | Key.E ->
                let overlays = if Set.contains OverlayKind.Events config.ActiveOverlays then Set.remove OverlayKind.Events config.ActiveOverlays else Set.add OverlayKind.Events config.ActiveOverlays
                config <- { config with ActiveOverlays = overlays }
            | Key.G ->
                config <- { config with ShowGridLines = not config.ShowGridLines; ActiveOverlays = Set.add OverlayKind.Grid config.ActiveOverlays }
            | Key.M ->
                let overlays = if Set.contains OverlayKind.MetalSpots config.ActiveOverlays then Set.remove OverlayKind.MetalSpots config.ActiveOverlays else Set.add OverlayKind.MetalSpots config.ActiveOverlays
                config <- { config with ActiveOverlays = overlays }
            | Key.H ->
                let overlays = if Set.contains OverlayKind.EconomyHud config.ActiveOverlays then Set.remove OverlayKind.EconomyHud config.ActiveOverlays else Set.add OverlayKind.EconomyHud config.ActiveOverlays
                config <- { config with ActiveOverlays = overlays }
            | Key.Home ->
                match getSnapshot () with
                | Some snap -> computeAutoFit snap.MapGrid
                | None -> ()
            | _ -> ())

    let private handleInput (evt: InputEvent) =
        match evt with
        | InputEvent.KeyDown key -> processKey key
        | InputEvent.MouseScroll(delta, x, y) ->
            lock stateLock (fun () ->
                let factor = if delta > 0.0f then 1.1f else 1.0f / 1.1f
                let mapX = x / viewState.Scale + viewState.OriginX
                let mapY = y / viewState.Scale + viewState.OriginY
                let newScale = viewState.Scale * factor
                let newOriginX = mapX - x / newScale
                let newOriginY = mapY - y / newScale
                viewState <- { viewState with Scale = newScale; OriginX = newOriginX; OriginY = newOriginY; AutoFit = false })
        | InputEvent.MouseDown(_, x, y) ->
            lock stateLock (fun () ->
                dragStart <- Some (x, y)
                dragOrigin <- Some (viewState.OriginX, viewState.OriginY))
        | InputEvent.MouseMove(x, y) ->
            lock stateLock (fun () ->
                match dragStart, dragOrigin with
                | Some (sx, sy), Some (ox, oy) ->
                    let dx = (x - sx) / viewState.Scale
                    let dy = (y - sy) / viewState.Scale
                    viewState <- { viewState with OriginX = ox - dx; OriginY = oy - dy; AutoFit = false }
                | _ -> ())
        | InputEvent.MouseUp _ ->
            lock stateLock (fun () ->
                dragStart <- None
                dragOrigin <- None)
        | InputEvent.WindowResize(w, h) ->
            lock stateLock (fun () ->
                viewState <- { viewState with WindowWidth = w; WindowHeight = h })
        | InputEvent.FrameTick _ ->
            lock stateLock (fun () ->
                if not autoFitDone then
                    match getSnapshot () with
                    | Some snap when snap.MapGrid.WidthHeightmap > 0 ->
                        computeAutoFit snap.MapGrid
                        autoFitDone <- true
                    | _ -> ()
                emitScene ())
        | _ -> ()

    let private doStart (initialSnapshot: GameSnapshot option) =
        lock stateLock (fun () ->
            config <- VizDefaults.defaultConfig
            viewState <- VizDefaults.defaultViewState
            autoFitDone <- false
            currentSnapshot <- initialSnapshot
            let evt = Event<Scene>()
            sceneEvent <- Some evt
            let viewerConfig: ViewerConfig =
                { Title = "FSBar Preview"
                  Width = 1024
                  Height = 640
                  TargetFps = 60
                  ClearColor = SKColors.Black
                  PreferredBackend = Some Backend.GL }
            let handle, inputs = Viewer.run viewerConfig evt.Publish
            viewer <- Some handle
            eprintfn "[PreviewSession] Viewer started"
            let sub = inputs |> Observable.subscribe handleInput
            inputSub <- Some sub)

    let rec stop () =
        lock stateLock (fun () ->
            inputSub |> Option.iter (fun s -> s.Dispose())
            inputSub <- None
            viewer |> Option.iter (fun v -> (v :> IDisposable).Dispose())
            viewer <- None
            sceneEvent <- None
            playbackFrames <- [||]
            playbackStopwatch <- None
            currentSnapshot <- None
            autoFitDone <- false
            LayerRenderer.invalidateAll ()
            eprintfn "[PreviewSession] Stopped")

    and startWithMap (grid: MapGrid) =
        let snap =
            { FrameNumber = 0; MapGrid = grid; Units = Map.empty
              EventIndicators = []; EconomyMetal = VizDefaults.defaultEconomy
              EconomyEnergy = VizDefaults.defaultEconomy
              MetalSpots = [||]; Connected = true }
        doStart (Some snap)
        { new IDisposable with member _.Dispose() = stop () }

    and startWithSnapshot (snapshot: GameSnapshot) =
        doStart (Some snapshot)
        { new IDisposable with member _.Dispose() = stop () }

    and startPlayback (frames: GameSnapshot seq) (gameFps: int) =
        let arr = frames |> Seq.toArray
        lock stateLock (fun () ->
            playbackFrames <- arr
            gameFpsVal <- gameFps
            playbackStopwatch <- Some (Stopwatch.StartNew()))
        doStart (if arr.Length > 0 then Some arr.[0] else None)
        { new IDisposable with member _.Dispose() = stop () }
