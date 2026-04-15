namespace FSBar.Viz

open System
open SkiaSharp
open SkiaViewer
open FSBar.Client
open Silk.NET.Input

module GameViz =

    let private stateLock = obj ()
    let mutable private config = VizDefaults.defaultConfig
    let mutable private viewState = VizDefaults.defaultViewState
    let mutable private snapshot: GameSnapshot option = None
    let mutable private viewer: ViewerHandle option = None
    let mutable private sceneEvent: Event<Scene> option = None
    let mutable private inputSub: IDisposable option = None
    let mutable private clientRef: BarClient option = None
    let mutable private mapGridRef: MapGrid option = None
    let mutable private myTeamId = 0
    let mutable private units: Map<int, UnitState> = Map.empty
    let mutable private indicators: EventIndicator list = []
    let mutable private metalSpots: (float32 * float32 * float32 * float32) array = [||]
    let mutable private dragStart: (float32 * float32) option = None
    let mutable private dragOrigin: (float32 * float32) option = None

    let private computeAutoFit (grid: MapGrid) =
        let mapW = float32 grid.WidthHeightmap
        let mapH = float32 grid.HeightHeightmap
        if mapW > 0.0f && mapH > 0.0f then
            let scaleX = float32 viewState.WindowWidth / mapW
            let scaleY = float32 viewState.WindowHeight / mapH
            let scale = min scaleX scaleY
            viewState <- { viewState with Scale = scale; OriginX = 0.0f; OriginY = 0.0f }

    let private buildSnapshot (grid: MapGrid) (frameNum: int) (connected: bool) (metalEcon: EconomyData) (energyEcon: EconomyData) =
        { FrameNumber = frameNum
          MapGrid = grid
          Units = units
          EventIndicators = indicators
          EconomyMetal = metalEcon
          EconomyEnergy = energyEcon
          MetalSpots = metalSpots
          Connected = connected }

    let private emitScene () =
        match sceneEvent, snapshot with
        | Some evt, Some snap ->
            let scene = SceneBuilder.buildScene snap config viewState
            evt.Trigger scene
        | _ -> ()

    let private processKey (key: Key) =
        lock stateLock (fun () ->
            match key with
            | Key.B -> config <- { config with BaseLayer = LayerKind.BaseTerrain }
            | Key.Number1 -> config <- { config with BaseLayer = LayerKind.HeightMap }
            | Key.Number2 -> config <- { config with BaseLayer = LayerKind.SlopeMap }
            | Key.Number3 -> config <- { config with BaseLayer = LayerKind.ResourceMap }
            | Key.Number4 -> config <- { config with BaseLayer = LayerKind.LosMap }
            | Key.Number5 -> config <- { config with BaseLayer = LayerKind.RadarMap }
            | Key.Number6 -> config <- { config with BaseLayer = LayerKind.TerrainClassification }
            | Key.Number7 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Kbot }
            | Key.Number8 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Tank }
            | Key.Number9 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Hover }
            | Key.Number0 -> config <- { config with BaseLayer = LayerKind.Passability MoveType.Ship }
            | Key.U ->
                let ov = if Set.contains OverlayKind.Units config.ActiveOverlays then Set.remove OverlayKind.Units config.ActiveOverlays else Set.add OverlayKind.Units config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.E ->
                let ov = if Set.contains OverlayKind.Events config.ActiveOverlays then Set.remove OverlayKind.Events config.ActiveOverlays else Set.add OverlayKind.Events config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.G -> config <- { config with ShowGridLines = not config.ShowGridLines; ActiveOverlays = Set.add OverlayKind.Grid config.ActiveOverlays }
            | Key.M ->
                let ov = if Set.contains OverlayKind.MetalSpots config.ActiveOverlays then Set.remove OverlayKind.MetalSpots config.ActiveOverlays else Set.add OverlayKind.MetalSpots config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.H ->
                let ov = if Set.contains OverlayKind.EconomyHud config.ActiveOverlays then Set.remove OverlayKind.EconomyHud config.ActiveOverlays else Set.add OverlayKind.EconomyHud config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.Home ->
                mapGridRef |> Option.iter computeAutoFit
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
                viewState <- { viewState with Scale = newScale; OriginX = mapX - x / newScale; OriginY = mapY - y / newScale; AutoFit = false })
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
            lock stateLock (fun () -> dragStart <- None; dragOrigin <- None)
        | InputEvent.WindowResize(w, h) ->
            lock stateLock (fun () ->
                viewState <- { viewState with WindowWidth = w; WindowHeight = h }
                if viewState.AutoFit then
                    mapGridRef |> Option.iter (fun g ->
                        if g.WidthHeightmap > 0 then computeAutoFit g))
        | InputEvent.FrameTick elapsed ->
            lock stateLock (fun () ->
                SceneBuilder.updatePulsePhase elapsed
                if viewState.AutoFit then
                    mapGridRef |> Option.iter (fun g ->
                        if g.WidthHeightmap > 0 then computeAutoFit g)
                emitScene ())
        | _ -> ()

    let start (cfg: VizConfig option) =
        lock stateLock (fun () ->
            config <- cfg |> Option.defaultValue VizDefaults.defaultConfig
            viewState <- VizDefaults.defaultViewState
            units <- Map.empty
            indicators <- []
            let evt = Event<Scene>()
            sceneEvent <- Some evt
            let viewerConfig: ViewerConfig =
                { Title = "FSBar GameViz"
                  Width = 1024
                  Height = 640
                  TargetFps = 60
                  ClearColor = SKColors.Black
                  PreferredBackend = Some Backend.GL }
            let handle, inputs = Viewer.run viewerConfig evt.Publish
            viewer <- Some handle
            let sub = inputs |> Observable.subscribe handleInput
            inputSub <- Some sub
            eprintfn "[GameViz] Viewer started")

    let stop () =
        lock stateLock (fun () ->
            inputSub |> Option.iter (fun s -> s.Dispose())
            inputSub <- None
            viewer |> Option.iter (fun v -> (v :> IDisposable).Dispose())
            viewer <- None
            sceneEvent <- None
            units <- Map.empty
            indicators <- []
            mapGridRef <- None
            clientRef <- None
            snapshot <- None
            metalSpots <- [||]
            LayerRenderer.invalidateAll ()
            eprintfn "[GameViz] Stopped")

    let attachToClient (client: BarClient) =
        lock stateLock (fun () ->
            clientRef <- Some client
            try
                let grid = MapGrid.loadFromEngine client.Stream
                mapGridRef <- Some grid
                metalSpots <- Callbacks.getMetalSpots client.Stream
                myTeamId <- Callbacks.getMyTeam client.Stream
                eprintfn "[GameViz] Attached to client, map %dx%d" grid.WidthHeightmap grid.HeightHeightmap
            with ex ->
                eprintfn "[GameViz] Failed to load map data: %s" ex.Message)

    let seedUnits (unitStates: UnitState list) =
        lock stateLock (fun () ->
            for u in unitStates do
                units <- Map.add u.UnitId u units)

    let onFrame (frame: GameFrame) =
        lock stateLock (fun () ->
            let grid =
                match mapGridRef, clientRef with
                | Some g, Some c ->
                    try
                        let g = MapGrid.refreshLos c.Stream g
                        let g = MapGrid.refreshRadar c.Stream g
                        mapGridRef <- Some g
                        g
                    with ex ->
                        eprintfn "[GameViz] LOS/Radar refresh failed: %s" ex.Message
                        g
                | Some g, None -> g
                | None, _ ->
                    { WidthElmos = 0; HeightElmos = 0; WidthHeightmap = 0; HeightHeightmap = 0
                      HeightMap = Array2D.zeroCreate 0 0; SlopeMap = Array2D.zeroCreate 0 0
                      ResourceMap = Array2D.zeroCreate 0 0; LosMap = Array2D.zeroCreate 0 0
                      RadarMap = Array2D.zeroCreate 0 0 }

            for evt in frame.Events do
                match evt with
                | GameEvent.UnitCreated(unitId, _) ->
                    match clientRef with
                    | Some c ->
                        try
                            let pos = Callbacks.getUnitPos c.Stream unitId
                            let defId = Callbacks.getUnitDef c.Stream unitId
                            let hp = Callbacks.getUnitHealth c.Stream unitId
                            let maxHp = Callbacks.getUnitMaxHealth c.Stream unitId
                            let (px, py, pz) = pos
                            let u = { UnitId = unitId; PositionX = px; PositionY = py; PositionZ = pz
                                      TeamId = myTeamId; DefId = defId; Health = hp; MaxHealth = maxHp; IsEnemy = false }
                            units <- Map.add unitId u units
                            indicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.UnitCreated; FrameCreated = int frame.FrameNumber; DurationFrames = 30 } :: indicators
                        with _ -> ()
                    | None -> ()
                | GameEvent.UnitFinished unitId ->
                    match clientRef with
                    | Some c ->
                        try
                            let pos = Callbacks.getUnitPos c.Stream unitId
                            let (px, py, pz) = pos
                            units <- units |> Map.change unitId (Option.map (fun u -> { u with PositionX = px; PositionY = py; PositionZ = pz }))
                        with _ -> ()
                    | None -> ()
                | GameEvent.UnitDestroyed(unitId, _) ->
                    match Map.tryFind unitId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = int frame.FrameNumber; DurationFrames = 45 } :: indicators
                        units <- Map.remove unitId units
                    | None -> ()
                | GameEvent.EnemyEnterLOS enemyId ->
                    match clientRef with
                    | Some c ->
                        try
                            let pos = Callbacks.getUnitPos c.Stream enemyId
                            let (px, py, pz) = pos
                            let defId = try Callbacks.getUnitDef c.Stream enemyId with _ -> 0
                            let hp = try Callbacks.getUnitHealth c.Stream enemyId with _ -> 100.0f
                            let maxHp = try Callbacks.getUnitMaxHealth c.Stream enemyId with _ -> 100.0f
                            let u = { UnitId = enemyId; PositionX = px; PositionY = py; PositionZ = pz
                                      TeamId = 1; DefId = defId; Health = hp; MaxHealth = maxHp; IsEnemy = true }
                            units <- Map.add enemyId u units
                            indicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.EnemySpotted; FrameCreated = int frame.FrameNumber; DurationFrames = 40 } :: indicators
                        with _ -> ()
                    | None -> ()
                | GameEvent.EnemyLeaveLOS enemyId ->
                    units <- Map.remove enemyId units
                | GameEvent.EnemyDestroyed(enemyId, _) ->
                    match Map.tryFind enemyId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = int frame.FrameNumber; DurationFrames = 45 } :: indicators
                        units <- Map.remove enemyId units
                    | None -> ()
                | GameEvent.UnitDamaged(unitId, _, _, _, _) ->
                    match Map.tryFind unitId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.Combat; FrameCreated = int frame.FrameNumber; DurationFrames = 20 } :: indicators
                    | None -> ()
                | GameEvent.Update _ ->
                    // Refresh friendly unit positions
                    match clientRef with
                    | Some c ->
                        units <- units |> Map.map (fun uid u ->
                            if not u.IsEnemy then
                                try
                                    let (px, py, pz) = Callbacks.getUnitPos c.Stream uid
                                    let hp = Callbacks.getUnitHealth c.Stream uid
                                    { u with PositionX = px; PositionY = py; PositionZ = pz; Health = hp }
                                with _ -> u
                            else u)
                    | None -> ()
                | _ -> ()

            // Prune expired indicators
            let frameNum = int frame.FrameNumber
            indicators <- indicators |> List.filter (fun ev ->
                frameNum - ev.FrameCreated < ev.DurationFrames)

            // Query economy
            let metalEcon, energyEcon =
                match clientRef with
                | Some c ->
                    try
                        ({ Current = Callbacks.getEconomyCurrent c.Stream 0
                           Income = Callbacks.getEconomyIncome c.Stream 0
                           Usage = Callbacks.getEconomyUsage c.Stream 0
                           Storage = Callbacks.getEconomyStorage c.Stream 0 } : EconomyData),
                        ({ Current = Callbacks.getEconomyCurrent c.Stream 1
                           Income = Callbacks.getEconomyIncome c.Stream 1
                           Usage = Callbacks.getEconomyUsage c.Stream 1
                           Storage = Callbacks.getEconomyStorage c.Stream 1 } : EconomyData)
                    with _ ->
                        VizDefaults.defaultEconomy, VizDefaults.defaultEconomy
                | None -> VizDefaults.defaultEconomy, VizDefaults.defaultEconomy

            snapshot <- Some (buildSnapshot grid frameNum true metalEcon energyEcon))

    let setDisconnected () =
        lock stateLock (fun () ->
            snapshot <- snapshot |> Option.map (fun s -> { s with Connected = false })
            eprintfn "[GameViz] Disconnected")

    let resetView () =
        lock stateLock (fun () ->
            mapGridRef |> Option.iter computeAutoFit)

    let setBaseLayer (layer: LayerKind) =
        lock stateLock (fun () ->
            config <- { config with BaseLayer = layer })

    let toggleOverlay (overlay: OverlayKind) =
        lock stateLock (fun () ->
            let ov = if Set.contains overlay config.ActiveOverlays then Set.remove overlay config.ActiveOverlays else Set.add overlay config.ActiveOverlays
            config <- { config with ActiveOverlays = ov })

    let enableOverlay (overlay: OverlayKind) =
        lock stateLock (fun () ->
            config <- { config with ActiveOverlays = Set.add overlay config.ActiveOverlays })

    let disableOverlay (overlay: OverlayKind) =
        lock stateLock (fun () ->
            config <- { config with ActiveOverlays = Set.remove overlay config.ActiveOverlays })

    let setConfig (cfg: VizConfig) =
        lock stateLock (fun () -> config <- cfg)

    let updateConfig (f: VizConfig -> VizConfig) =
        lock stateLock (fun () -> config <- f config)

    let setColorScheme (layer: LayerKind) (scheme: ColorScheme) =
        lock stateLock (fun () ->
            config <- { config with ColorSchemes = Map.add layer scheme config.ColorSchemes }
            LayerRenderer.invalidateCache layer)

    let setMarkerSize (size: float32) =
        lock stateLock (fun () ->
            config <- { config with UnitMarkerSize = size })

    let setOverlayOpacity (opacity: float32) =
        lock stateLock (fun () ->
            config <- { config with OverlayOpacity = max 0.0f (min 1.0f opacity) })

    let toggleGridLines () =
        lock stateLock (fun () ->
            config <- { config with ShowGridLines = not config.ShowGridLines })

    let pan (dx: float32) (dy: float32) =
        lock stateLock (fun () ->
            viewState <- { viewState with OriginX = viewState.OriginX + dx; OriginY = viewState.OriginY + dy; AutoFit = false })

    let zoom (factor: float32) (centerX: float32) (centerY: float32) =
        lock stateLock (fun () ->
            let mapX = centerX / viewState.Scale + viewState.OriginX
            let mapY = centerY / viewState.Scale + viewState.OriginY
            let newScale = viewState.Scale * factor
            viewState <- { viewState with Scale = newScale; OriginX = mapX - centerX / newScale; OriginY = mapY - centerY / newScale; AutoFit = false })

    let screenshot (folder: string) : Result<string, string> =
        lock stateLock (fun () ->
            match viewer with
            | Some v -> v.Screenshot(folder)
            | None -> Result.Error "No viewer running")
