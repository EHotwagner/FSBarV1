namespace FSBar.Viz

open System
open SkiaSharp
open SkiaViewer
open FSBar.Client
open Silk.NET.Input

module GameViz =

    // --- BarData-backed unit enrichment ---

    type private DefProps =
        { InternalName: string
          Shape: MovementShape
          Faction: FactionId
          Tier: Tier
          LabelCode: string
          FootprintW: float32
          FootprintH: float32
          WeaponRanges: float32 list
          SightRange: float32 }

    let private barDataByName =
        lazy (
            BarData.AllUnitDefs.all
            |> List.map (fun (_, _, d: BarData.UnitDef) -> d.name, d)
            |> Map.ofList)

    let private concreteOrDefault (v: BarData.ValueOrExpr<float>) (fallback: float) : float =
        match v with
        | BarData.ValueOrExpr.Concrete x -> x
        | _ -> fallback

    let private resolveDefPropsFromBarData (name: string) : DefProps =
        match Map.tryFind name barDataByName.Value with
        | Some d ->
            let canMove = match d.movement with Some m -> m.canMove | None -> false
            let canFly = match d.movement with Some m -> m.canFly | None -> false
            let mClass = match d.movement with Some m -> m.movementClass | None -> None
            let weaponRanges =
                match d.weapons with
                | Some weapons ->
                    weapons |> List.choose (fun w ->
                        match w.range with
                        | Some r -> Some (float32 (concreteOrDefault r 0.0))
                        | None -> None)
                    |> List.filter (fun r -> r > 0.0f)
                | None -> []
            { InternalName = name
              Shape = UnitGlyph.classifyShape canMove canFly mClass ignore
              Faction = UnitGlyph.classifyFaction d.subfolder d.name ignore
              Tier = UnitGlyph.classifyTier d.customParams d.category ignore
              LabelCode = UnitLabels.lookupOrFallback name
              FootprintW = float32 d.footprintX * 16.0f
              FootprintH = float32 d.footprintZ * 16.0f
              WeaponRanges = weaponRanges
              SightRange = float32 (concreteOrDefault d.sightDistance 0.0) }
        | None ->
            { InternalName = name
              Shape = MovementShape.Bot
              Faction = FactionId.Neutral
              Tier = Tier.T1
              LabelCode = UnitLabels.lookupOrFallback name
              FootprintW = 32.0f
              FootprintH = 32.0f
              WeaponRanges = []
              SightRange = 0.0f }

    let private defaultStatus : StatusFlags =
        { IsUnderConstruction = false
          IsStunned = false
          JustDamagedWithinMs = None
          JustCompletedWithinMs = None
          IsCloaked = false }

    let private toUnitDisplay (u: UnitState) (props: DefProps) (isUnfinished: bool) : UnitDisplay =
        { UnitId = u.UnitId
          DefId = u.DefId
          InternalName = props.InternalName
          Shape = props.Shape
          Faction = props.Faction
          Tier = props.Tier
          LabelCode = props.LabelCode
          FootprintWidthElmo = props.FootprintW
          FootprintHeightElmo = props.FootprintH
          TeamId = u.TeamId
          PositionX = u.PositionX
          PositionY = u.PositionY
          PositionZ = u.PositionZ
          HeadingRadians = 0.0f
          CurrentHealth = u.Health
          MaxHealth = u.MaxHealth
          BuildProgress = if isUnfinished then 0.5f else 1.0f
          Status =
              if isUnfinished then { defaultStatus with IsUnderConstruction = true }
              else defaultStatus
          WeaponRangesElmo = props.WeaponRanges
          SightRangeElmo = props.SightRange
          BuildRangeElmo = None
          CommandQueue = [] }

    // --- Mutable state ---

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
    let mutable private defPropsCache: Map<int, DefProps> = Map.empty
    let mutable private unfinishedUnits: Set<int> = Set.empty

    // Performance counter state
    let private perfStopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mutable private renderFrameCount = 0
    let mutable private stateUpdateCount = 0
    let mutable private perfLastSampleMs = 0.0
    let mutable private renderFpsDisplay = 0.0
    let mutable private stateUpsDisplay = 0.0
    let mutable private lastStateFrame = 0
    let mutable private stateFrameDelta = 0

    // Interpolation state: lerp unit positions between state updates
    let mutable private prevUnits: Map<int, UnitState> = Map.empty
    let mutable private interpT = 1.0f  // 0..1 progress toward current snapshot
    let private interpStopwatch = System.Diagnostics.Stopwatch()
    let mutable private interpDurationMs = 200.0  // estimated ms between state updates

    let private computeAutoFit (grid: MapGrid) =
        let mapW = float32 grid.WidthHeightmap
        let mapH = float32 grid.HeightHeightmap
        if mapW > 0.0f && mapH > 0.0f then
            let scaleX = float32 viewState.WindowWidth / mapW
            let scaleY = float32 viewState.WindowHeight / mapH
            let scale = min scaleX scaleY
            viewState <- { viewState with Scale = scale; OriginX = 0.0f; OriginY = 0.0f }

    let private ensureDefProps (stream: Net.Sockets.NetworkStream) (defId: int) =
        match Map.tryFind defId defPropsCache with
        | Some _ -> ()
        | None ->
            let name = try Callbacks.getUnitDefName stream defId with _ -> sprintf "def%d" defId
            defPropsCache <- Map.add defId (resolveDefPropsFromBarData name) defPropsCache

    let private ensureDefPropsFromCache (unitDefs: UnitDefCache) (defId: int) =
        match Map.tryFind defId defPropsCache with
        | Some _ -> ()
        | None ->
            let name =
                match UnitDefCache.tryFindById unitDefs defId with
                | Some info -> info.Name
                | None -> sprintf "def%d" defId
            defPropsCache <- Map.add defId (resolveDefPropsFromBarData name) defPropsCache

    let private trackedUnitToUnitState (teamId: int) (uid: int) (u: TrackedUnit) : UnitState =
        let (px, py, pz) = u.Position
        { UnitId = uid; PositionX = px; PositionY = py; PositionZ = pz
          TeamId = teamId; DefId = u.DefId; Health = u.Health; MaxHealth = u.MaxHealth; IsEnemy = false }

    let private trackedEnemyToUnitState (eid: int) (e: TrackedEnemy) : UnitState =
        let (px, py, pz) = e.Position
        { UnitId = eid; PositionX = px; PositionY = py; PositionZ = pz
          TeamId = 1; DefId = (e.DefId |> Option.defaultValue 0)
          Health = (e.Health |> Option.defaultValue 100.0f); MaxHealth = 100.0f; IsEnemy = true }

    let private economyFromSnapshot (snap: FSBar.Client.EconomySnapshot) : EconomyData =
        { Current = snap.Current; Income = snap.Income; Usage = snap.Usage; Storage = snap.Storage }

    let private buildDisplayUnits () =
        units |> Map.map (fun unitId u ->
            let props =
                match Map.tryFind u.DefId defPropsCache with
                | Some p -> p
                | None -> resolveDefPropsFromBarData (sprintf "def%d" u.DefId)
            toUnitDisplay u props (Set.contains unitId unfinishedUnits))

    let private buildSnapshot (grid: MapGrid) (frameNum: int) (connected: bool) (metalEcon: EconomyData) (energyEcon: EconomyData) =
        { FrameNumber = frameNum
          MapGrid = grid
          Units = units
          DisplayUnits = buildDisplayUnits ()
          EventIndicators = indicators
          EconomyMetal = metalEcon
          EconomyEnergy = energyEcon
          MetalSpots = metalSpots
          Connected = connected }

    let private lerpUnit (prev: UnitState) (cur: UnitState) (t: float32) : UnitState =
        let t = min 1.0f (max 0.0f t)
        { cur with
            PositionX = prev.PositionX + (cur.PositionX - prev.PositionX) * t
            PositionY = prev.PositionY + (cur.PositionY - prev.PositionY) * t
            PositionZ = prev.PositionZ + (cur.PositionZ - prev.PositionZ) * t }

    let private emitScene () =
        match sceneEvent, snapshot with
        | Some evt, Some snap ->
            // Advance interpolation
            let elapsedMs = interpStopwatch.Elapsed.TotalMilliseconds
            interpT <- if interpDurationMs > 0.0 then min 1.0f (float32 (elapsedMs / interpDurationMs)) else 1.0f
            // Build interpolated units map
            let interpUnits =
                snap.Units |> Map.map (fun uid cur ->
                    match Map.tryFind uid prevUnits with
                    | Some prev -> lerpUnit prev cur interpT
                    | None -> cur)
            let interpDisplayUnits =
                snap.DisplayUnits |> Map.map (fun uid du ->
                    match Map.tryFind uid interpUnits with
                    | Some u -> { du with PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ }
                    | None -> du)
            let interpSnap = { snap with Units = interpUnits; DisplayUnits = interpDisplayUnits }
            // Update perf counters (sample once per second)
            renderFrameCount <- renderFrameCount + 1
            let nowMs = perfStopwatch.Elapsed.TotalMilliseconds
            let elapsed = nowMs - perfLastSampleMs
            if elapsed >= 1000.0 then
                renderFpsDisplay <- float renderFrameCount / (elapsed / 1000.0)
                stateUpsDisplay <- float stateUpdateCount / (elapsed / 1000.0)
                printfn "[viz-perf] render=%.0f fps  state=%.0f ups  game_frame=%d  delta=%d  interp=%.2f"
                    renderFpsDisplay stateUpsDisplay snap.FrameNumber stateFrameDelta interpT
                renderFrameCount <- 0
                stateUpdateCount <- 0
                perfLastSampleMs <- nowMs
            let scene = SceneBuilder.buildScene interpSnap config viewState
            let perfText =
                sprintf "render %.0f fps | state %.0f ups | game frame %d (delta %d)"
                    renderFpsDisplay stateUpsDisplay snap.FrameNumber stateFrameDelta
            let perfPaint = Scene.fill (SKColor(200uy, 200uy, 200uy, 200uy))
            let perfLabel = Scene.text perfText 8.0f (float32 viewState.WindowHeight - 12.0f) 13.0f perfPaint
            let augmented = Scene.create config.BackgroundColor (scene.Elements @ [ perfLabel ])
            evt.Trigger augmented
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
            // --- Unit-glyph overlays (feature 028-unit-viz-language) ---
            | Key.W ->
                let ov =
                    if Set.contains OverlayKind.WeaponRanges config.ActiveOverlays
                    then Set.remove OverlayKind.WeaponRanges config.ActiveOverlays
                    else Set.add OverlayKind.WeaponRanges config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.L ->
                let ov =
                    if Set.contains OverlayKind.SightRanges config.ActiveOverlays
                    then Set.remove OverlayKind.SightRanges config.ActiveOverlays
                    else Set.add OverlayKind.SightRanges config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.C ->
                let ov =
                    if Set.contains OverlayKind.CommandQueue config.ActiveOverlays
                    then Set.remove OverlayKind.CommandQueue config.ActiveOverlays
                    else Set.add OverlayKind.CommandQueue config.ActiveOverlays
                config <- { config with ActiveOverlays = ov }
            | Key.N ->
                let ov =
                    if Set.contains OverlayKind.FullNames config.ActiveOverlays
                    then Set.remove OverlayKind.FullNames config.ActiveOverlays
                    else Set.add OverlayKind.FullNames config.ActiveOverlays
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
            defPropsCache <- Map.empty
            unfinishedUnits <- Set.empty
            let evt = Event<Scene>()
            sceneEvent <- Some evt
            let viewerConfig: ViewerConfig =
                { Title = "FSBar GameViz"
                  Width = 1024
                  Height = 640
                  TargetFps = 60
                  ClearColor = SKColors.Black
                  PreferredBackend = Some Backend.Vulkan }
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
            defPropsCache <- Map.empty
            unfinishedUnits <- Set.empty
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
                units <- Map.add u.UnitId u units
            match clientRef with
            | Some c ->
                for u in unitStates do
                    try ensureDefProps c.Stream u.DefId with _ -> ()
            | None -> ())

    let attachWithState (mapGrid: MapGrid) (spots: (float32 * float32 * float32 * float32) array) (teamId: int) =
        lock stateLock (fun () ->
            mapGridRef <- Some mapGrid
            metalSpots <- spots
            myTeamId <- teamId
            computeAutoFit mapGrid
            eprintfn "[GameViz] Attached via state, map %dx%d" mapGrid.WidthHeightmap mapGrid.HeightHeightmap)

    let onFrameWithState (gameState: GameState) (mapGrid: MapGrid) =
        lock stateLock (fun () ->
            let frameNum = int gameState.FrameNumber
            stateUpdateCount <- stateUpdateCount + 1
            stateFrameDelta <- frameNum - lastStateFrame
            lastStateFrame <- frameNum

            // Process events for indicators (before rebuilding units so destruction
            // can read previous frame's positions from the existing units map)
            for evt in gameState.Events do
                match evt with
                | GameEvent.UnitCreated(unitId, _) ->
                    match Map.tryFind unitId gameState.Units with
                    | Some u ->
                        let (px, py, pz) = u.Position
                        indicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.UnitCreated; FrameCreated = frameNum; DurationFrames = 30 } :: indicators
                    | None -> ()
                | GameEvent.UnitDestroyed(unitId, _) ->
                    match Map.tryFind unitId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = frameNum; DurationFrames = 45 } :: indicators
                    | None -> ()
                | GameEvent.UnitDamaged(unitId, _, _, _, _) ->
                    match Map.tryFind unitId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.Combat; FrameCreated = frameNum; DurationFrames = 20 } :: indicators
                    | None -> ()
                | GameEvent.EnemyEnterLOS enemyId ->
                    match Map.tryFind enemyId gameState.Enemies with
                    | Some e ->
                        let (px, py, pz) = e.Position
                        indicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.EnemySpotted; FrameCreated = frameNum; DurationFrames = 40 } :: indicators
                    | None -> ()
                | GameEvent.EnemyDestroyed(enemyId, _) ->
                    match Map.tryFind enemyId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = frameNum; DurationFrames = 45 } :: indicators
                    | None -> ()
                | _ -> ()

            // Rebuild units from GameState, preserving previous for interpolation
            prevUnits <- units
            let mutable newUnits = Map.empty
            for (KeyValue(uid, u)) in gameState.Units do
                newUnits <- Map.add uid (trackedUnitToUnitState myTeamId uid u) newUnits
            for (KeyValue(eid, e)) in gameState.Enemies do
                if e.InLOS then
                    newUnits <- Map.add eid (trackedEnemyToUnitState eid e) newUnits
            units <- newUnits
            // Reset interpolation timer
            let elapsedSinceLastUpdate = interpStopwatch.Elapsed.TotalMilliseconds
            if elapsedSinceLastUpdate > 10.0 then
                interpDurationMs <- elapsedSinceLastUpdate
            interpStopwatch.Restart()
            interpT <- 0.0f

            // Ensure def props for all encountered DefIds
            for (KeyValue(_, u)) in units do
                ensureDefPropsFromCache gameState.UnitDefs u.DefId

            // Track unfinished units
            unfinishedUnits <-
                gameState.Units
                |> Map.toSeq
                |> Seq.choose (fun (uid, u) -> if not u.IsFinished then Some uid else None)
                |> Set.ofSeq

            // Prune expired indicators
            indicators <- indicators |> List.filter (fun ev ->
                frameNum - ev.FrameCreated < ev.DurationFrames)

            // Derive economy
            let metalEcon = economyFromSnapshot gameState.Metal
            let energyEcon = economyFromSnapshot gameState.Energy

            // Update map grid
            mapGridRef <- Some mapGrid

            snapshot <- Some (buildSnapshot mapGrid frameNum true metalEcon energyEcon))

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
                            ensureDefProps c.Stream defId
                            let u = { UnitId = unitId; PositionX = px; PositionY = py; PositionZ = pz
                                      TeamId = myTeamId; DefId = defId; Health = hp; MaxHealth = maxHp; IsEnemy = false }
                            units <- Map.add unitId u units
                            unfinishedUnits <- Set.add unitId unfinishedUnits
                            indicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.UnitCreated; FrameCreated = int frame.FrameNumber; DurationFrames = 30 } :: indicators
                        with _ -> ()
                    | None -> ()
                | GameEvent.UnitFinished unitId ->
                    match clientRef with
                    | Some c ->
                        try
                            let pos = Callbacks.getUnitPos c.Stream unitId
                            let (px, py, pz) = pos
                            unfinishedUnits <- Set.remove unitId unfinishedUnits
                            units <- units |> Map.change unitId (Option.map (fun u -> { u with PositionX = px; PositionY = py; PositionZ = pz }))
                        with _ -> ()
                    | None -> ()
                | GameEvent.UnitDestroyed(unitId, _) ->
                    match Map.tryFind unitId units with
                    | Some u ->
                        indicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = int frame.FrameNumber; DurationFrames = 45 } :: indicators
                        units <- Map.remove unitId units
                        unfinishedUnits <- Set.remove unitId unfinishedUnits
                    | None -> ()
                | GameEvent.EnemyEnterLOS enemyId ->
                    match clientRef with
                    | Some c ->
                        try
                            let pos = Callbacks.getUnitPos c.Stream enemyId
                            let (px, py, pz) = pos
                            let defId = try Callbacks.getUnitDef c.Stream enemyId with _ -> 0
                            if defId > 0 then ensureDefProps c.Stream defId
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
