namespace FSBar.Viz

open System
open System.Diagnostics
open SkiaSharp
open SkiaViewer
open FSBar.Client
open Silk.NET.Input

module PreviewSession =

    let stateLock = obj ()
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

    type CyclingState =
        { SupportedMaps: MapCacheFile.SupportedMap list
          mutable CurrentIndex: int
          RepoRoot: string }

    let mutable private cyclingState: CyclingState option = None
    let mutable private errorBanner: string option = None

    let computeAutoFit (grid: MapGrid) =
        let mapW = float32 grid.WidthHeightmap
        let mapH = float32 grid.HeightHeightmap
        if mapW > 0.0f && mapH > 0.0f then
            let scaleX = float32 viewState.WindowWidth / mapW
            let scaleY = float32 viewState.WindowHeight / mapH
            let scale = min scaleX scaleY
            viewState <- { viewState with Scale = scale; OriginX = 0.0f; OriginY = 0.0f }

    let getSnapshot () =
        if playbackFrames.Length > 0 then
            match playbackStopwatch with
            | Some sw ->
                let elapsed = sw.Elapsed.TotalSeconds
                let frameIdx = int (elapsed * float gameFpsVal) % playbackFrames.Length
                Some playbackFrames.[frameIdx]
            | None -> currentSnapshot
        else
            currentSnapshot

    let bannerElements (msg: string) : Element list =
        let w = float32 viewState.WindowWidth
        let h = float32 viewState.WindowHeight
        // Dim backdrop so the banner reads clearly even over a fresh window.
        let backdrop = Scene.rect 0.0f 0.0f w h (Scene.fill (SKColor(0uy, 0uy, 0uy, 200uy)))
        let boxTop = 20.0f
        let boxHeight = 40.0f + 14.0f * float32 (max 1 (msg.Split('\n').Length))
        let box = Scene.rect 20.0f boxTop (w - 40.0f) boxHeight (Scene.fill (SKColor(40uy, 0uy, 0uy, 230uy)))
        let title =
            Scene.text "Map load error" 32.0f (boxTop + 20.0f) 14.0f
                (Scene.fill (SKColor(255uy, 180uy, 180uy)))
        let lines =
            msg.Split('\n')
            |> Array.mapi (fun i line ->
                Scene.text line 32.0f (boxTop + 40.0f + 14.0f * float32 i) 11.0f
                    (Scene.fill (SKColor(255uy, 220uy, 220uy))))
            |> Array.toList
        backdrop :: box :: title :: lines

    let emitScene () =
        match sceneEvent with
        | None -> ()
        | Some evt ->
            let scene =
                match getSnapshot () with
                | Some snap ->
                    let baseScene = SceneBuilder.buildScene snap config viewState
                    match errorBanner with
                    | Some msg ->
                        { baseScene with Elements = baseScene.Elements @ bannerElements msg }
                    | None -> baseScene
                | None ->
                    // No snapshot yet — either a failed initial load (show banner)
                    // or a purely uninitialised session (clear to background). Never
                    // emit a completely empty scene: always at least a background
                    // so the viewer isn't a silent black window (FR-010).
                    let backdrop =
                        Scene.rect 0.0f 0.0f
                            (float32 viewState.WindowWidth)
                            (float32 viewState.WindowHeight)
                            (Scene.fill config.BackgroundColor)
                    let extras =
                        match errorBanner with
                        | Some msg -> bannerElements msg
                        | None -> []
                    Scene.create config.BackgroundColor (backdrop :: extras)
            evt.Trigger scene

    let advanceCycleIndex (n: int) (direction: int) (current: int) : int =
        if n <= 0 then 0
        else ((current + direction) % n + n) % n

    let findRepoRoot () =
        let rec walk (dir: string) =
            if String.IsNullOrEmpty dir then None
            elif System.IO.Directory.Exists(System.IO.Path.Combine(dir, ".specify"))
                 || System.IO.Directory.Exists(System.IO.Path.Combine(dir, "bots")) then
                Some dir
            else
                let parent = System.IO.Path.GetDirectoryName dir
                if String.IsNullOrEmpty parent || parent = dir then None
                else walk parent
        match walk AppContext.BaseDirectory with
        | Some d -> d
        | None ->
            eprintfn "[PreviewSession] Warning: could not find repo root, falling back to CWD"
            Environment.CurrentDirectory

    let snapshotOfGrid (grid: MapGrid) (metalSpots: (float32 * float32 * float32 * float32) array) : GameSnapshot =
        { FrameNumber = 0
          MapGrid = grid
          Units = Map.empty
          DisplayUnits = Map.empty
          EventIndicators = []
          EconomyMetal = VizDefaults.defaultEconomy
          EconomyEnergy = VizDefaults.defaultEconomy
          MetalSpots = metalSpots
          Connected = true }

    let loadAtIndex (state: CyclingState) (idx: int) : Result<GameSnapshot, string> =
        let supported = state.SupportedMaps.[idx]
        let path = MapCacheFile.cachePathFor state.RepoRoot supported
        match MapCacheFile.read supported path with
        | Result.Ok loaded ->
            let metalSpots = MapQuery.metalSpotsFromResourceMap loaded.Grid
            Result.Ok (snapshotOfGrid loaded.Grid metalSpots)
        | Result.Error e -> Result.Error (MapCacheFile.formatLoadError e)

    let cycleTo (direction: int) =
        match cyclingState with
        | None -> ()
        | Some state ->
            let n = state.SupportedMaps.Length
            let nextIdx = advanceCycleIndex n direction state.CurrentIndex
            match loadAtIndex state nextIdx with
            | Result.Ok snap ->
                LayerRenderer.invalidateAll ()
                currentSnapshot <- Some snap
                viewState <- { viewState with AutoFit = true }
                errorBanner <- None
                state.CurrentIndex <- nextIdx
                eprintfn "[PreviewSession] Switched to map %s" state.SupportedMaps.[nextIdx].MapName
            | Result.Error msg ->
                errorBanner <- Some msg
                eprintfn "[PreviewSession] Load failure: %s" msg

    let processKey (key: Key) =
        lock stateLock (fun () ->
            match key with
            | Key.B ->
                config <- { config with BaseLayer = LayerKind.BaseTerrain }
                LayerRenderer.invalidateCache LayerKind.BaseTerrain
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
                | Some snap ->
                    viewState <- { viewState with AutoFit = true }
                    computeAutoFit snap.MapGrid
                | None -> ()
            | Key.RightBracket | Key.Period -> cycleTo 1
            | Key.LeftBracket  | Key.Comma  -> cycleTo -1
            | _ -> ())

    let handleInput (evt: InputEvent) =
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
                viewState <- { viewState with WindowWidth = w; WindowHeight = h }
                if viewState.AutoFit then
                    match getSnapshot () with
                    | Some snap when snap.MapGrid.WidthHeightmap > 0 -> computeAutoFit snap.MapGrid
                    | _ -> ())
        | InputEvent.FrameTick elapsed ->
            lock stateLock (fun () ->
                SceneBuilder.updatePulsePhase elapsed
                if viewState.AutoFit then
                    match getSnapshot () with
                    | Some snap when snap.MapGrid.WidthHeightmap > 0 ->
                        computeAutoFit snap.MapGrid
                    | _ -> ()
                emitScene ())
        | _ -> ()

    let doStart (initialSnapshot: GameSnapshot option) =
        lock stateLock (fun () ->
            config <- VizDefaults.defaultConfig
            viewState <- VizDefaults.defaultViewState
            SceneBuilder.resetPulsePhase ()
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
            cyclingState <- None
            errorBanner <- None
            LayerRenderer.invalidateAll ()
            eprintfn "[PreviewSession] Stopped")

    and startWithMap (grid: MapGrid) =
        doStart (Some (snapshotOfGrid grid [||]))
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

    and startWithCachedMaps (supportedMaps: MapCacheFile.SupportedMap list) (initialMapName: string option) =
        if List.isEmpty supportedMaps then
            raise (ArgumentException("supportedMaps must be non-empty", "supportedMaps"))
        let repoRoot = findRepoRoot ()
        let initialIdx =
            match initialMapName with
            | None -> 0
            | Some name ->
                match supportedMaps |> List.tryFindIndex (fun m -> m.MapName = name) with
                | Some i -> i
                | None ->
                    let valid = supportedMaps |> List.map (fun m -> m.MapName) |> String.concat ", "
                    eprintfn "[PreviewSession] Warning: map %s not in supportedMaps [%s]; falling back to index 0" name valid
                    0
        let state =
            { SupportedMaps = supportedMaps
              CurrentIndex = initialIdx
              RepoRoot = repoRoot }
        let initialSnapshot =
            match loadAtIndex state initialIdx with
            | Result.Ok snap ->
                errorBanner <- None
                eprintfn "[PreviewSession] Loaded map %s" supportedMaps.[initialIdx].MapName
                Some snap
            | Result.Error msg ->
                errorBanner <- Some msg
                eprintfn "[PreviewSession] Initial load failure: %s" msg
                None
        cyclingState <- Some state
        doStart initialSnapshot
        { new IDisposable with
            member _.Dispose() =
                cyclingState <- None
                errorBanner <- None
                stop () }
