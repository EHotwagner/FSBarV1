namespace FSBar.Viz

open System
open FSBar.Client
open SkiaViewer

module GameViz =
    let private stateLock = obj ()
    let mutable private config = VizDefaults.defaultConfig
    let mutable private viewState = VizDefaults.defaultViewState
    let mutable private snapshot: GameSnapshot option = None
    let mutable private viewer: IDisposable option = None
    let mutable private clientRef: BarClient option = None
    let mutable private mapGridRef: MapGrid option = None
    let mutable private myTeamId = 0
    let mutable private units: Map<int, UnitState> = Map.empty
    let mutable private indicators: EventIndicator list = []

    let private emptySnapshot =
        { FrameNumber = 0
          MapGrid =
            { WidthElmos = 0
              HeightElmos = 0
              WidthHeightmap = 0
              HeightHeightmap = 0
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
          Connected = true }

    let private getSnapshot () =
        lock stateLock (fun () -> snapshot |> Option.defaultValue emptySnapshot)

    let private getConfig () = lock stateLock (fun () -> config)
    let private getViewState () = lock stateLock (fun () -> viewState)

    let private computeAutoFit (grid: MapGrid) (ww: int) (wh: int) =
        if grid.WidthHeightmap <= 0 || grid.HeightHeightmap <= 0 then
            ()
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

    let private processCommand (cmd: VizCommand) =
        lock stateLock (fun () ->
            match cmd with
            | VizCommand.SetBaseLayer layer -> config <- { config with BaseLayer = layer }
            | VizCommand.ToggleOverlay overlay ->
                if config.ActiveOverlays.Contains overlay then
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Remove overlay }
                else
                    config <- { config with ActiveOverlays = config.ActiveOverlays.Add overlay }
            | VizCommand.Pan(dx, dy) ->
                viewState <-
                    { viewState with
                        OriginX = viewState.OriginX + dx
                        OriginY = viewState.OriginY + dy
                        AutoFit = false }
            | VizCommand.Zoom(factor, cx, cy) ->
                let newScale = viewState.Scale * factor |> max 0.1f |> min 100.0f
                let ratio = newScale / viewState.Scale
                let newOx = cx - (cx - viewState.OriginX) * ratio
                let newOy = cy - (cy - viewState.OriginY) * ratio

                viewState <-
                    { viewState with
                        Scale = newScale
                        OriginX = newOx
                        OriginY = newOy
                        AutoFit = false }
            | VizCommand.ResetView ->
                viewState <- { viewState with AutoFit = true }

                match mapGridRef with
                | Some g -> computeAutoFit g viewState.WindowWidth viewState.WindowHeight
                | None -> ()
            | VizCommand.SetColorScheme(layer, scheme) ->
                config <-
                    { config with
                        ColorSchemes = config.ColorSchemes.Add(layer, scheme) }

                LayerRenderer.invalidateCache layer
            | VizCommand.SetMarkerSize size -> config <- { config with UnitMarkerSize = size }
            | VizCommand.SetOverlayOpacity opacity ->
                config <- { config with OverlayOpacity = opacity |> max 0.0f |> min 1.0f }
            | VizCommand.ToggleGridLines -> config <- { config with ShowGridLines = not config.ShowGridLines }
            | VizCommand.Stop -> ())

    let private processKeyDown (key: Silk.NET.Input.Key) =
        let cmd =
            match key with
            | Silk.NET.Input.Key.Number1 -> Some(VizCommand.SetBaseLayer LayerKind.HeightMap)
            | Silk.NET.Input.Key.Number2 -> Some(VizCommand.SetBaseLayer LayerKind.SlopeMap)
            | Silk.NET.Input.Key.Number3 -> Some(VizCommand.SetBaseLayer LayerKind.ResourceMap)
            | Silk.NET.Input.Key.Number4 -> Some(VizCommand.SetBaseLayer LayerKind.LosMap)
            | Silk.NET.Input.Key.Number5 -> Some(VizCommand.SetBaseLayer LayerKind.RadarMap)
            | Silk.NET.Input.Key.Number6 -> Some(VizCommand.SetBaseLayer LayerKind.TerrainClassification)
            | Silk.NET.Input.Key.Number7 -> Some(VizCommand.SetBaseLayer(LayerKind.Passability MoveType.Kbot))
            | Silk.NET.Input.Key.Number8 -> Some(VizCommand.SetBaseLayer(LayerKind.Passability MoveType.Tank))
            | Silk.NET.Input.Key.Number9 -> Some(VizCommand.SetBaseLayer(LayerKind.Passability MoveType.Hover))
            | Silk.NET.Input.Key.Number0 -> Some(VizCommand.SetBaseLayer(LayerKind.Passability MoveType.Ship))
            | Silk.NET.Input.Key.U -> Some(VizCommand.ToggleOverlay OverlayKind.Units)
            | Silk.NET.Input.Key.E -> Some(VizCommand.ToggleOverlay OverlayKind.Events)
            | Silk.NET.Input.Key.G -> Some(VizCommand.ToggleOverlay OverlayKind.Grid)
            | Silk.NET.Input.Key.M -> Some(VizCommand.ToggleOverlay OverlayKind.MetalSpots)
            | Silk.NET.Input.Key.Home -> Some VizCommand.ResetView
            | _ -> None

        match cmd with
        | Some c -> processCommand c
        | None -> ()

    let private doStop () =
        match viewer with
        | Some v ->
            v.Dispose() // Viewer.run completion signaling handles the wait
            viewer <- None
            LayerRenderer.invalidateAll ()
            units <- Map.empty
            indicators <- []
            mapGridRef <- None
            clientRef <- None
            snapshot <- None
        | None -> ()

    let start (config': VizConfig option) =
        // Stop any existing visualization before starting a new one
        doStop ()

        match config' with
        | Some c -> lock stateLock (fun () -> config <- c)
        | None -> ()

        let viewerConfig: ViewerConfig =
            { Title = "FSBar GameViz"
              Width = 1024
              Height = 640
              TargetFps = 60
              ClearColor = config.BackgroundColor
              OnRender =
                fun canvas _fbSize ->
                    let snap = getSnapshot ()
                    let cfg = getConfig ()
                    let vs = getViewState ()
                    SceneBuilder.drawFrame canvas snap cfg vs
              OnResize =
                fun w h ->
                    lock stateLock (fun () ->
                        viewState <-
                            { viewState with
                                WindowWidth = w
                                WindowHeight = h })

                    if viewState.AutoFit then
                        match mapGridRef with
                        | Some g -> computeAutoFit g w h
                        | None -> ()
              OnKeyDown = processKeyDown
              OnMouseScroll =
                fun delta cx cy ->
                    let factor = if delta > 0.0f then 1.1f else 0.9f
                    processCommand (VizCommand.Zoom(factor, cx, cy))
              OnMouseDrag = fun dx dy -> processCommand (VizCommand.Pan(dx, dy))
              PreferredBackend = None }

        viewer <- Some(Viewer.run viewerConfig)
        printfn "[GameViz] Visualization started."

    let stop () =
        doStop ()
        printfn "[GameViz] Visualization stopped."

    let attachToClient (client: BarClient) =
        clientRef <- Some client
        myTeamId <- Callbacks.getMyTeam client.Stream

        // Load map grid — may fail mid-session due to LOS dimension mismatch;
        // in that case, defer full load to first onFrame call
        let grid =
            try
                MapGrid.loadFromEngine client.Stream
            with
            | ex ->
                printfn "[GameViz] Warning: initial loadFromEngine failed (%s), will retry on first frame" ex.Message
                // Build a minimal grid from dimensions only
                let w = Callbacks.getMapWidth client.Stream
                let h = Callbacks.getMapHeight client.Stream
                { WidthElmos = w * 8
                  HeightElmos = h * 8
                  WidthHeightmap = w
                  HeightHeightmap = h
                  HeightMap = Array2D.zeroCreate (h + 1) (w + 1)
                  SlopeMap = Array2D.zeroCreate (h / 2) (w / 2)
                  ResourceMap = Array2D.zeroCreate h w
                  LosMap = Array2D.zeroCreate h w
                  RadarMap = Array2D.zeroCreate h w }

        mapGridRef <- Some grid

        let metalSpots =
            try Callbacks.getMetalSpots client.Stream
            with _ -> [||]

        lock stateLock (fun () ->
            snapshot <-
                Some
                    { emptySnapshot with
                        MapGrid = grid
                        MetalSpots = metalSpots
                        Connected = true })

        if viewState.AutoFit then
            computeAutoFit grid viewState.WindowWidth viewState.WindowHeight

        printfn "[GameViz] Attached to client. Map: %dx%d" grid.WidthHeightmap grid.HeightHeightmap

    let onFrame (frame: GameFrame) =
        match clientRef with
        | None -> ()
        | Some client ->
            let stream = client.Stream
            let grid =
                match mapGridRef with
                | Some g when g.HeightMap.Length > 1 && g.HeightMap.[0, 0] <> 0.0f ->
                    // Map data loaded — just refresh dynamic layers
                    let g = try MapGrid.refreshLos stream g with _ -> g
                    let g = try MapGrid.refreshRadar stream g with _ -> g
                    mapGridRef <- Some g
                    g
                | Some g ->
                    // HeightMap is still empty (all zeros) — retry full load
                    try
                        let loaded = MapGrid.loadFromEngine stream
                        mapGridRef <- Some loaded
                        loaded
                    with _ ->
                        g
                | None ->
                    try
                        let g = MapGrid.loadFromEngine stream
                        mapGridRef <- Some g
                        g
                    with _ ->
                        emptySnapshot.MapGrid

            let frameNum = int frame.FrameNumber

            // Process events
            for event in frame.Events do
                match event with
                | GameEvent.UnitCreated(unitId, _) ->
                    try
                        let (px, py, pz) = Callbacks.getUnitPos stream unitId
                        let hp = Callbacks.getUnitHealth stream unitId
                        let maxHp = Callbacks.getUnitMaxHealth stream unitId
                        let defId = Callbacks.getUnitDef stream unitId

                        let u =
                            { UnitId = unitId
                              PositionX = px
                              PositionY = py
                              PositionZ = pz
                              TeamId = myTeamId
                              DefId = defId
                              Health = hp
                              MaxHealth = maxHp
                              IsEnemy = false }

                        units <- units.Add(unitId, u)

                        indicators <-
                            { PositionX = px
                              PositionY = py
                              PositionZ = pz
                              Kind = EventKind.UnitCreated
                              FrameCreated = frameNum
                              DurationFrames = 60 }
                            :: indicators
                    with
                    | _ -> ()
                | GameEvent.UnitFinished unitId ->
                    try
                        let (px, py, pz) = Callbacks.getUnitPos stream unitId
                        let hp = Callbacks.getUnitHealth stream unitId
                        let maxHp = Callbacks.getUnitMaxHealth stream unitId

                        match units.TryFind unitId with
                        | Some u ->
                            units <-
                                units.Add(
                                    unitId,
                                    { u with
                                        PositionX = px
                                        PositionY = py
                                        PositionZ = pz
                                        Health = hp
                                        MaxHealth = maxHp }
                                )
                        | None -> ()
                    with
                    | _ -> ()
                | GameEvent.UnitDestroyed(unitId, _) ->
                    match units.TryFind unitId with
                    | Some u ->
                        indicators <-
                            { PositionX = u.PositionX
                              PositionY = u.PositionY
                              PositionZ = u.PositionZ
                              Kind = EventKind.UnitDestroyed
                              FrameCreated = frameNum
                              DurationFrames = 90 }
                            :: indicators

                        units <- units.Remove unitId
                    | None -> ()
                | GameEvent.EnemyEnterLOS enemyId ->
                    try
                        let (px, py, pz) = Callbacks.getUnitPos stream enemyId
                        let hp = Callbacks.getUnitHealth stream enemyId
                        let maxHp = Callbacks.getUnitMaxHealth stream enemyId
                        let defId = Callbacks.getUnitDef stream enemyId

                        let u =
                            { UnitId = enemyId
                              PositionX = px
                              PositionY = py
                              PositionZ = pz
                              TeamId = -1
                              DefId = defId
                              Health = hp
                              MaxHealth = maxHp
                              IsEnemy = true }

                        units <- units.Add(enemyId, u)

                        indicators <-
                            { PositionX = px
                              PositionY = py
                              PositionZ = pz
                              Kind = EventKind.EnemySpotted
                              FrameCreated = frameNum
                              DurationFrames = 60 }
                            :: indicators
                    with
                    | _ -> ()
                | GameEvent.EnemyLeaveLOS enemyId -> units <- units.Remove enemyId
                | GameEvent.EnemyDestroyed(enemyId, _) ->
                    match units.TryFind enemyId with
                    | Some u ->
                        indicators <-
                            { PositionX = u.PositionX
                              PositionY = u.PositionY
                              PositionZ = u.PositionZ
                              Kind = EventKind.UnitDestroyed
                              FrameCreated = frameNum
                              DurationFrames = 90 }
                            :: indicators

                        units <- units.Remove enemyId
                    | None -> ()
                | GameEvent.UnitDamaged(unitId, _, _, _, _) ->
                    match units.TryFind unitId with
                    | Some u ->
                        indicators <-
                            { PositionX = u.PositionX
                              PositionY = u.PositionY
                              PositionZ = u.PositionZ
                              Kind = EventKind.Combat
                              FrameCreated = frameNum
                              DurationFrames = 30 }
                            :: indicators
                    | None -> ()
                | GameEvent.Update _ ->
                    // Refresh positions of known friendly units
                    let updatedUnits =
                        units
                        |> Map.map (fun id u ->
                            if not u.IsEnemy then
                                try
                                    let (px, py, pz) = Callbacks.getUnitPos stream id
                                    let hp = Callbacks.getUnitHealth stream id

                                    { u with
                                        PositionX = px
                                        PositionY = py
                                        PositionZ = pz
                                        Health = hp }
                                with
                                | _ -> u
                            else
                                u)

                    units <- updatedUnits
                | _ -> ()

            // Prune expired indicators
            indicators <- indicators |> List.filter (fun i -> frameNum - i.FrameCreated < i.DurationFrames)

            // Query economy
            let metalEcon =
                try
                    { Current = Callbacks.getEconomyCurrent stream 0
                      Income = Callbacks.getEconomyIncome stream 0
                      Usage = Callbacks.getEconomyUsage stream 0
                      Storage = Callbacks.getEconomyStorage stream 0 }
                with
                | _ -> VizDefaults.defaultEconomy

            let energyEcon =
                try
                    { Current = Callbacks.getEconomyCurrent stream 1
                      Income = Callbacks.getEconomyIncome stream 1
                      Usage = Callbacks.getEconomyUsage stream 1
                      Storage = Callbacks.getEconomyStorage stream 1 }
                with
                | _ -> VizDefaults.defaultEconomy

            lock stateLock (fun () ->
                snapshot <-
                    Some
                        { FrameNumber = frameNum
                          MapGrid = grid
                          Units = units
                          EventIndicators = indicators
                          EconomyMetal = metalEcon
                          EconomyEnergy = energyEcon
                          MetalSpots =
                            match snapshot with
                            | Some s -> s.MetalSpots
                            | None -> [||]
                          Connected = true })

    let setDisconnected () =
        lock stateLock (fun () ->
            snapshot <-
                snapshot
                |> Option.map (fun s -> { s with Connected = false }))

    let resetView () = processCommand VizCommand.ResetView
    let setBaseLayer layer = processCommand (VizCommand.SetBaseLayer layer)

    let toggleOverlay overlay =
        processCommand (VizCommand.ToggleOverlay overlay)

    let enableOverlay overlay =
        lock stateLock (fun () ->
            config <- { config with ActiveOverlays = config.ActiveOverlays.Add overlay })

    let disableOverlay overlay =
        lock stateLock (fun () ->
            config <- { config with ActiveOverlays = config.ActiveOverlays.Remove overlay })

    let setConfig (config': VizConfig) = lock stateLock (fun () -> config <- config')

    let updateConfig f =
        lock stateLock (fun () -> config <- f config)

    let setColorScheme layer scheme =
        processCommand (VizCommand.SetColorScheme(layer, scheme))

    let setMarkerSize size =
        processCommand (VizCommand.SetMarkerSize size)

    let setOverlayOpacity opacity =
        processCommand (VizCommand.SetOverlayOpacity opacity)

    let toggleGridLines () = processCommand VizCommand.ToggleGridLines
    let pan dx dy = processCommand (VizCommand.Pan(dx, dy))

    let zoom factor centerX centerY =
        processCommand (VizCommand.Zoom(factor, centerX, centerY))
