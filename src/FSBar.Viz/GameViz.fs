namespace FSBar.Viz

open System
open System.Threading
open SkiaSharp
open SkiaViewer
open FSBar.Client
open Silk.NET.Input

module GameViz =

    // --- BarData-backed unit enrichment ---

    type DefProps =
        { InternalName: string
          Shape: MovementShape
          Faction: FactionId
          Tier: Tier
          LabelCode: string
          FootprintW: float32
          FootprintH: float32
          WeaponRanges: float32 list
          SightRange: float32 }

    let barDataByName =
        lazy (
            BarData.AllUnitDefs.all
            |> List.map (fun (_, _, d: BarData.UnitDef) -> d.name, d)
            |> Map.ofList)

    let concreteOrDefault (v: BarData.ValueOrExpr<float>) (fallback: float) : float =
        match v with
        | BarData.ValueOrExpr.Concrete x -> x
        | _ -> fallback

    let resolveDefPropsFromBarData (name: string) : DefProps =
        match Map.tryFind name barDataByName.Value with
        | Some d ->
            // Feature 038 FR-002: unify canMove derivation with
            // `FSBar.Viz.UnitDisplayAdapter` so standalone GameViz and
            // Hub-Viewer-via-SceneBuilder produce byte-identical glyphs
            // for the same internal name. Previous derivation used
            // `m.canMove` directly; the encyclopedia path has always
            // used `canFly || movementClass <> None` and is the
            // reference per spec 038.
            let canMove =
                match d.movement with
                | Some m -> m.canFly || (m.movementClass <> None)
                | None -> false
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

    let defaultStatus : StatusFlags =
        { IsUnderConstruction = false
          IsStunned = false
          JustDamagedWithinMs = None
          JustCompletedWithinMs = None
          IsCloaked = false }

    let toUnitDisplay (u: UnitState) (props: DefProps) (isUnfinished: bool) : UnitDisplay =
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

    // --- Lock-free dataflow types ---

    /// Raw inputs from the bot thread, atomically published for the render thread.
    type RawFrame =
        { GameState: GameState
          MapGrid: MapGrid
          MyTeamId: int
          MetalSpots: (float32 * float32 * float32 * float32) array
          FrameCounter: int }

    // --- Locks ---
    /// Protects config, viewState, dragStart, dragOrigin. Adequate for <10 ops/sec.
    let configLock = obj ()
    /// Protects lifecycle state and socket-path publisher state.
    let lifecycleLock = obj ()

    // --- Atomic shared state (bot thread -> render thread) ---
    /// Atomically swapped by onFrameWithState; sampled by render thread.
    let mutable private latestFrame: RawFrame option = None
    let mutable private frameCounter = 0

    /// Atomically published by onFrame (socket path); sampled by render thread.
    let mutable private latestDirectSnapshot: GameSnapshot option = None
    let mutable private directSnapshotCounter = 0

    // --- Config/view state (configLock for writes; direct reads are safe on x86) ---
    let mutable private config = VizDefaults.defaultConfig
    let mutable private viewState = VizDefaults.defaultViewState
    let mutable private dragStart: (float32 * float32) option = None
    let mutable private dragOrigin: (float32 * float32) option = None

    // --- Configurator panel state (under configLock) ---
    let mutable private panelState : ConfigPanelState = ConfigPanel.initialState
    let mutable private activePresetName : string option = None
    let mutable private referenceConfig : VizConfig = VizDefaults.defaultConfig

    // --- Lifecycle state (under lifecycleLock) ---
    let mutable private viewer: ViewerHandle option = None
    let mutable private sceneEvent: Event<Scene> option = None
    let mutable private inputSub: IDisposable option = None
    let mutable private clientRef: BarClient option = None
    let mutable private mapGridRef: MapGrid option = None
    let mutable private myTeamId = 0
    let mutable private metalSpots: (float32 * float32 * float32 * float32) array = [||]
    /// 0 = connected, 1 = disconnected. Int for Volatile.Read/Write compatibility.
    let mutable private disconnected = 0

    // --- Publisher state for socket path (under lifecycleLock) ---
    let mutable private units: Map<int, UnitState> = Map.empty
    let mutable private indicators: EventIndicator list = []
    let mutable private defPropsCache: Map<int, DefProps> = Map.empty
    let mutable private unfinishedUnits: Set<int> = Set.empty

    // --- Render-thread-local state (exclusively owned by the render thread) ---
    let mutable private renderSnapshot: GameSnapshot option = None
    let mutable private renderUnits: Map<int, UnitState> = Map.empty
    let mutable private renderPrevUnits: Map<int, UnitState> = Map.empty
    let mutable private renderIndicators: EventIndicator list = []
    let mutable private renderDefPropsCache: Map<int, DefProps> = Map.empty
    let mutable private renderUnfinishedUnits: Set<int> = Set.empty
    let mutable private renderMapGrid: MapGrid option = None
    let mutable private lastProcessedCounter = -1
    let mutable private lastProcessedDirectCounter = -1

    // Performance counter state (render-thread-local)
    let perfStopwatch = System.Diagnostics.Stopwatch.StartNew()
    let mutable private renderFrameCount = 0
    let mutable private stateUpdateCount = 0
    let mutable private perfLastSampleMs = 0.0
    let mutable private renderFpsDisplay = 0.0
    let mutable private stateUpsDisplay = 0.0
    let mutable private lastStateFrame = 0
    let mutable private stateFrameDelta = 0

    // Interpolation state (render-thread-local)
    let mutable private interpT = 1.0f  // 0..1 progress toward current snapshot
    let interpStopwatch = System.Diagnostics.Stopwatch()
    let mutable private interpDurationMs = 200.0  // estimated ms between state updates

    // --- Helpers ---

    let computeAutoFit (grid: MapGrid) =
        // Caller must hold configLock
        let mapW = float32 grid.WidthHeightmap
        let mapH = float32 grid.HeightHeightmap
        if mapW > 0.0f && mapH > 0.0f then
            let scaleX = float32 viewState.WindowWidth / mapW
            let scaleY = float32 viewState.WindowHeight / mapH
            let scale = min scaleX scaleY
            viewState <- { viewState with Scale = scale; OriginX = 0.0f; OriginY = 0.0f }

    let ensureDefProps (stream: Net.Sockets.NetworkStream) (defId: int) =
        match Map.tryFind defId defPropsCache with
        | Some _ -> ()
        | None ->
            let name = try Callbacks.getUnitDefName stream defId with _ -> sprintf "def%d" defId
            defPropsCache <- Map.add defId (resolveDefPropsFromBarData name) defPropsCache

    let trackedUnitToUnitState (teamId: int) (uid: int) (u: TrackedUnit) : UnitState =
        let (px, py, pz) = u.Position
        { UnitId = uid; PositionX = px; PositionY = py; PositionZ = pz
          TeamId = teamId; DefId = u.DefId; Health = u.Health; MaxHealth = u.MaxHealth; IsEnemy = false }

    let trackedEnemyToUnitState (eid: int) (e: TrackedEnemy) : UnitState =
        let (px, py, pz) = e.Position
        { UnitId = eid; PositionX = px; PositionY = py; PositionZ = pz
          TeamId = 1; DefId = (e.DefId |> Option.defaultValue 0)
          Health = (e.Health |> Option.defaultValue 100.0f); MaxHealth = 100.0f; IsEnemy = true }

    let economyFromSnapshot (snap: FSBar.Client.EconomySnapshot) : EconomyData =
        { Current = snap.Current; Income = snap.Income; Usage = snap.Usage; Storage = snap.Storage }

    let buildDisplayUnits () =
        // Uses publisher state — socket path only
        units |> Map.map (fun unitId u ->
            let props =
                match Map.tryFind u.DefId defPropsCache with
                | Some p -> p
                | None -> resolveDefPropsFromBarData (sprintf "def%d" u.DefId)
            toUnitDisplay u props (Set.contains unitId unfinishedUnits))

    let buildSnapshot (grid: MapGrid) (frameNum: int) (connected: bool) (metalEcon: EconomyData) (energyEcon: EconomyData) =
        { FrameNumber = frameNum
          MapGrid = grid
          Units = units
          DisplayUnits = buildDisplayUnits ()
          EventIndicators = indicators
          EconomyMetal = metalEcon
          EconomyEnergy = energyEcon
          MetalSpots = metalSpots
          Connected = connected }

    let lerpUnit (prev: UnitState) (cur: UnitState) (t: float32) : UnitState =
        let t = min 1.0f (max 0.0f t)
        { cur with
            PositionX = prev.PositionX + (cur.PositionX - prev.PositionX) * t
            PositionY = prev.PositionY + (cur.PositionY - prev.PositionY) * t
            PositionZ = prev.PositionZ + (cur.PositionZ - prev.PositionZ) * t }

    // --- Render thread: process new RawFrame from state-based path ---

    let processRawFrame (frame: RawFrame) =
        let gs = frame.GameState
        let frameNum = int gs.FrameNumber

        stateUpdateCount <- stateUpdateCount + 1
        stateFrameDelta <- frameNum - lastStateFrame
        lastStateFrame <- frameNum

        // Process events BEFORE rebuilding units so destruction/damage can read
        // previous frame's positions from the existing renderUnits map.
        for evt in gs.Events do
            match evt with
            | GameEvent.UnitCreated(unitId, _) ->
                match Map.tryFind unitId gs.Units with
                | Some u ->
                    let (px, py, pz) = u.Position
                    renderIndicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.UnitCreated; FrameCreated = frameNum; DurationFrames = 30 } :: renderIndicators
                | None -> ()
            | GameEvent.UnitDestroyed(unitId, _) ->
                match Map.tryFind unitId renderUnits with
                | Some u ->
                    renderIndicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = frameNum; DurationFrames = 45 } :: renderIndicators
                | None -> ()
            | GameEvent.UnitDamaged(unitId, _, _, _, _) ->
                match Map.tryFind unitId renderUnits with
                | Some u ->
                    renderIndicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.Combat; FrameCreated = frameNum; DurationFrames = 20 } :: renderIndicators
                | None -> ()
            | GameEvent.EnemyEnterLOS enemyId ->
                match Map.tryFind enemyId gs.Enemies with
                | Some e ->
                    let (px, py, pz) = e.Position
                    renderIndicators <- { PositionX = px; PositionY = py; PositionZ = pz; Kind = EventKind.EnemySpotted; FrameCreated = frameNum; DurationFrames = 40 } :: renderIndicators
                | None -> ()
            | GameEvent.EnemyDestroyed(enemyId, _) ->
                match Map.tryFind enemyId renderUnits with
                | Some u ->
                    renderIndicators <- { PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ; Kind = EventKind.UnitDestroyed; FrameCreated = frameNum; DurationFrames = 45 } :: renderIndicators
                | None -> ()
            | _ -> ()

        // Rebuild units from GameState, preserving previous for interpolation
        renderPrevUnits <- renderUnits
        let mutable newUnits = Map.empty
        for (KeyValue(uid, u)) in gs.Units do
            newUnits <- Map.add uid (trackedUnitToUnitState frame.MyTeamId uid u) newUnits
        for (KeyValue(eid, e)) in gs.Enemies do
            if e.InLOS then
                newUnits <- Map.add eid (trackedEnemyToUnitState eid e) newUnits
        renderUnits <- newUnits

        // Ensure def props for all encountered DefIds (render-local cache)
        for (KeyValue(_, u)) in renderUnits do
            match Map.tryFind u.DefId renderDefPropsCache with
            | Some _ -> ()
            | None ->
                let name =
                    match UnitDefCache.tryFindById gs.UnitDefs u.DefId with
                    | Some info -> info.Name
                    | None -> sprintf "def%d" u.DefId
                renderDefPropsCache <- Map.add u.DefId (resolveDefPropsFromBarData name) renderDefPropsCache

        // Track unfinished units
        renderUnfinishedUnits <-
            gs.Units
            |> Map.toSeq
            |> Seq.choose (fun (uid, u) -> if not u.IsFinished then Some uid else None)
            |> Set.ofSeq

        // Prune expired indicators
        renderIndicators <- renderIndicators |> List.filter (fun ev ->
            frameNum - ev.FrameCreated < ev.DurationFrames)

        // Build display units from render-local state
        let displayUnits =
            renderUnits |> Map.map (fun unitId u ->
                let props =
                    match Map.tryFind u.DefId renderDefPropsCache with
                    | Some p -> p
                    | None -> resolveDefPropsFromBarData (sprintf "def%d" u.DefId)
                toUnitDisplay u props (Set.contains unitId renderUnfinishedUnits))

        // Derive economy
        let metalEcon = economyFromSnapshot gs.Metal
        let energyEcon = economyFromSnapshot gs.Energy

        // Update render map grid
        renderMapGrid <- Some frame.MapGrid

        // Build snapshot
        renderSnapshot <- Some
            { FrameNumber = frameNum
              MapGrid = frame.MapGrid
              Units = renderUnits
              DisplayUnits = displayUnits
              EventIndicators = renderIndicators
              EconomyMetal = metalEcon
              EconomyEnergy = energyEcon
              MetalSpots = frame.MetalSpots
              Connected = Volatile.Read(&disconnected) = 0 }

        // Reset interpolation timer
        let elapsedSinceLastUpdate = interpStopwatch.Elapsed.TotalMilliseconds
        if elapsedSinceLastUpdate > 10.0 then
            interpDurationMs <- elapsedSinceLastUpdate
        interpStopwatch.Restart()
        interpT <- 0.0f
        lastProcessedCounter <- frame.FrameCounter

    // --- Render thread: process direct snapshot from socket path ---

    let processDirectSnapshot (snap: GameSnapshot) (counter: int) =
        stateUpdateCount <- stateUpdateCount + 1
        stateFrameDelta <- snap.FrameNumber - lastStateFrame
        lastStateFrame <- snap.FrameNumber
        renderPrevUnits <- renderUnits
        renderUnits <- snap.Units
        renderSnapshot <- Some snap
        renderMapGrid <- Some snap.MapGrid
        let elapsedSinceLastUpdate = interpStopwatch.Elapsed.TotalMilliseconds
        if elapsedSinceLastUpdate > 10.0 then
            interpDurationMs <- elapsedSinceLastUpdate
        interpStopwatch.Restart()
        interpT <- 0.0f
        lastProcessedDirectCounter <- counter

    // --- Render thread: emit scene ---

    let emitScene () =
        match sceneEvent with
        | None -> ()
        | Some evt ->
        // Panel-only path: render just the configurator over the clear color
        // when no game data is available yet. Lets users open the panel
        // before any snapshot arrives (FR-010 synthetic-data support and
        // interactive verification).
        match renderSnapshot with
        | None ->
            let cfg = config
            let vs = viewState
            let panelElems =
                let ps = panelState
                if ps.IsOpen then
                    let presetNames = try StylePreset.listNames() with _ -> []
                    let dirty = ConfigDescriptors.isDirty cfg referenceConfig
                    let ps' = { ps with DirtyIndicator = dirty }
                    ConfigPanel.buildPanel cfg ps'
                        (float32 vs.WindowWidth) (float32 vs.WindowHeight)
                        presetNames activePresetName
                else []
            if not (List.isEmpty panelElems) then
                evt.Trigger (Scene.create cfg.BackgroundColor panelElems)
        | Some snap ->
            let snap = if Volatile.Read(&disconnected) > 0 then { snap with Connected = false } else snap
            // Advance interpolation
            let elapsedMs = interpStopwatch.Elapsed.TotalMilliseconds
            interpT <- if interpDurationMs > 0.0 then min 1.0f (float32 (elapsedMs / interpDurationMs)) else 1.0f
            // Build interpolated units map
            let interpUnits =
                snap.Units |> Map.map (fun uid cur ->
                    match Map.tryFind uid renderPrevUnits with
                    | Some prev -> lerpUnit prev cur interpT
                    | None -> cur)
            let interpDisplayUnits =
                snap.DisplayUnits |> Map.map (fun uid du ->
                    match Map.tryFind uid interpUnits with
                    | Some u -> { du with PositionX = u.PositionX; PositionY = u.PositionY; PositionZ = u.PositionZ }
                    | None -> du)
            let interpSnap = { snap with Units = interpUnits; DisplayUnits = interpDisplayUnits }
            // Read config and viewState (atomic reference reads, safe on x86)
            let cfg = config
            let vs = viewState
            // Update perf counters (sample once per 5 seconds)
            renderFrameCount <- renderFrameCount + 1
            let nowMs = perfStopwatch.Elapsed.TotalMilliseconds
            let elapsed = nowMs - perfLastSampleMs
            if elapsed >= 5000.0 then
                renderFpsDisplay <- float renderFrameCount / (elapsed / 1000.0)
                stateUpsDisplay <- float stateUpdateCount / (elapsed / 1000.0)
                printfn "[viz-perf] render=%.0f fps  state=%.0f ups  game_frame=%d  delta=%d  interp=%.2f"
                    renderFpsDisplay stateUpsDisplay snap.FrameNumber stateFrameDelta interpT
                renderFrameCount <- 0
                stateUpdateCount <- 0
                perfLastSampleMs <- nowMs
            let scene = SceneBuilder.buildScene interpSnap cfg vs
            let perfText =
                sprintf "render %.0f fps | state %.0f ups | game frame %d (delta %d)"
                    renderFpsDisplay stateUpsDisplay snap.FrameNumber stateFrameDelta
            let perfPaint = Scene.fill (SKColor(200uy, 200uy, 200uy, 200uy))
            let perfLabel = Scene.text perfText 8.0f (float32 vs.WindowHeight - 12.0f) 13.0f perfPaint
            // Configurator panel overlay (feature 033-viz-style-configurator)
            let panelElems =
                let ps = panelState
                if ps.IsOpen then
                    let presetNames = try StylePreset.listNames() with _ -> []
                    let dirty = ConfigDescriptors.isDirty cfg referenceConfig
                    let ps' = { ps with DirtyIndicator = dirty }
                    ConfigPanel.buildPanel cfg ps' (float32 vs.WindowWidth) (float32 vs.WindowHeight) presetNames activePresetName
                else []
            let augmented = Scene.create cfg.BackgroundColor (scene.Elements @ [ perfLabel ] @ panelElems)
            evt.Trigger augmented

    // --- Input handling ---

    // --- Panel action application (caller must hold configLock) ---
    let applyPanelAction (action: ConfigPanelAction) =
        match action with
        | ConfigPanelAction.SavePreset name ->
            let preset = StylePreset.fromConfig name config
            match StylePreset.save preset with
            | Result.Ok _ ->
                activePresetName <- Some name
                referenceConfig <- config
            | Result.Error msg ->
                eprintfn "[GameViz] preset save failed: %s" msg
        | ConfigPanelAction.LoadPreset name ->
            match StylePreset.load name with
            | Result.Ok preset ->
                config <- StylePreset.applyToConfig preset config
                activePresetName <- Some name
                referenceConfig <- config
            | Result.Error msg ->
                eprintfn "[GameViz] preset load failed: %s" msg
        | ConfigPanelAction.DeletePreset name ->
            match StylePreset.delete name with
            | Result.Ok _ ->
                if activePresetName = Some name then activePresetName <- None
            | Result.Error msg ->
                eprintfn "[GameViz] preset delete failed: %s" msg
        | ConfigPanelAction.ResetDefaults ->
            config <- VizDefaults.defaultConfig
            activePresetName <- None
            referenceConfig <- VizDefaults.defaultConfig

    let processKey (key: Key) =
        lock configLock (fun () ->
            match key with
            | Key.P ->
                panelState <- ConfigPanel.toggle panelState
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
                renderMapGrid |> Option.iter computeAutoFit
            | _ -> ())

    // Try to route a mouse event to the panel. Returns true if the panel
    // consumed the event (and the default pan/zoom/drag handlers should
    // be skipped). Caller must NOT hold configLock.
    let routeToPanelIfOpen (evt: InputEvent) (x: float32) (y: float32) : bool =
        // Snapshot panel state for a cheap bounds check outside the lock.
        let ps = panelState
        if not ps.IsOpen then false
        else
            let ww = float32 viewState.WindowWidth
            // Only consume when the cursor is actually in the panel region,
            // OR when the panel is currently dragging a control (so that
            // MouseMove/MouseUp outside the panel bounds still updates the
            // active slider).
            let inPanel = ConfigPanel.hitTest x y ps ww
            let dragging = ps.ActiveControl.IsSome
            if not (inPanel || dragging) then false
            else
                lock configLock (fun () ->
                    let ww = float32 viewState.WindowWidth
                    let wh = float32 viewState.WindowHeight
                    let res = ConfigPanel.handleInput evt config panelState ww wh
                    panelState <- res.PanelState
                    match res.UpdatedConfig with
                    | Some c -> config <- c
                    | None -> ()
                    match res.Action with
                    | Some a -> applyPanelAction a
                    | None -> ())
                true

    let handleInput (evt: InputEvent) =
        match evt with
        | InputEvent.KeyDown key -> processKey key
        | InputEvent.MouseScroll(delta, x, y) ->
            if routeToPanelIfOpen evt x y then ()
            else
                lock configLock (fun () ->
                    let factor = if delta > 0.0f then 1.1f else 1.0f / 1.1f
                    let mapX = x / viewState.Scale + viewState.OriginX
                    let mapY = y / viewState.Scale + viewState.OriginY
                    let newScale = viewState.Scale * factor
                    viewState <- { viewState with Scale = newScale; OriginX = mapX - x / newScale; OriginY = mapY - y / newScale; AutoFit = false })
        | InputEvent.MouseDown(_, x, y) ->
            if routeToPanelIfOpen evt x y then ()
            else
                lock configLock (fun () ->
                    dragStart <- Some (x, y)
                    dragOrigin <- Some (viewState.OriginX, viewState.OriginY))
        | InputEvent.MouseMove(x, y) ->
            if routeToPanelIfOpen evt x y then ()
            else
                lock configLock (fun () ->
                    match dragStart, dragOrigin with
                    | Some (sx, sy), Some (ox, oy) ->
                        let dx = (x - sx) / viewState.Scale
                        let dy = (y - sy) / viewState.Scale
                        viewState <- { viewState with OriginX = ox - dx; OriginY = oy - dy; AutoFit = false }
                    | _ -> ())
        | InputEvent.MouseUp (btn, x, y) ->
            if routeToPanelIfOpen evt x y then ()
            else
                ignore btn
                lock configLock (fun () -> dragStart <- None; dragOrigin <- None)
        | InputEvent.WindowResize(w, h) ->
            lock configLock (fun () ->
                viewState <- { viewState with WindowWidth = w; WindowHeight = h }
                if viewState.AutoFit then
                    renderMapGrid |> Option.iter (fun g ->
                        if g.WidthHeightmap > 0 then computeAutoFit g))
        | InputEvent.FrameTick elapsed ->
            // No bot-thread lock on the hot path
            SceneBuilder.updatePulsePhase elapsed
            // Sample latest frame from state-based path (lock-free read)
            let frame = latestFrame
            match frame with
            | Some f when f.FrameCounter > lastProcessedCounter ->
                processRawFrame f
            | _ ->
                // Check socket-based path
                let dsc = Volatile.Read(&directSnapshotCounter)
                if dsc > lastProcessedDirectCounter then
                    match latestDirectSnapshot with
                    | Some snap -> processDirectSnapshot snap dsc
                    | None -> ()
            // Auto-fit check (requires configLock for viewState write)
            lock configLock (fun () ->
                if viewState.AutoFit then
                    renderMapGrid |> Option.iter (fun g ->
                        if g.WidthHeightmap > 0 then computeAutoFit g))
            emitScene ()
        | _ -> ()

    // --- Public API ---

    let start (cfg: VizConfig option) =
        lock lifecycleLock (fun () ->
            // Reset config (no render thread running yet)
            config <- cfg |> Option.defaultValue VizDefaults.defaultConfig
            viewState <- VizDefaults.defaultViewState
            // Reset render-thread-local state (safe — no render thread yet)
            renderUnits <- Map.empty
            renderPrevUnits <- Map.empty
            renderIndicators <- []
            renderSnapshot <- None
            renderDefPropsCache <- Map.empty
            renderUnfinishedUnits <- Set.empty
            renderMapGrid <- None
            lastProcessedCounter <- -1
            lastProcessedDirectCounter <- -1
            latestFrame <- None
            frameCounter <- 0
            latestDirectSnapshot <- None
            directSnapshotCounter <- 0
            // Reset publisher state
            units <- Map.empty
            indicators <- []
            defPropsCache <- Map.empty
            unfinishedUnits <- Set.empty
            disconnected <- 0
            // Reset perf/interp state
            renderFrameCount <- 0
            stateUpdateCount <- 0
            perfLastSampleMs <- perfStopwatch.Elapsed.TotalMilliseconds
            renderFpsDisplay <- 0.0
            stateUpsDisplay <- 0.0
            lastStateFrame <- 0
            stateFrameDelta <- 0
            interpT <- 1.0f
            interpStopwatch.Reset()
            interpDurationMs <- 200.0
            dragStart <- None
            dragOrigin <- None
            // Create viewer
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
        lock lifecycleLock (fun () ->
            inputSub |> Option.iter (fun s -> s.Dispose())
            inputSub <- None
            viewer |> Option.iter (fun v -> (v :> IDisposable).Dispose())
            viewer <- None
            sceneEvent <- None
            // Clear publisher state
            units <- Map.empty
            indicators <- []
            defPropsCache <- Map.empty
            unfinishedUnits <- Set.empty
            mapGridRef <- None
            clientRef <- None
            metalSpots <- [||]
            disconnected <- 0
            // Clear render-thread-local state (safe — render thread stopped by viewer dispose)
            renderSnapshot <- None
            renderUnits <- Map.empty
            renderPrevUnits <- Map.empty
            renderIndicators <- []
            renderDefPropsCache <- Map.empty
            renderUnfinishedUnits <- Set.empty
            renderMapGrid <- None
            latestFrame <- None
            latestDirectSnapshot <- None
            LayerRenderer.invalidateAll ()
            eprintfn "[GameViz] Stopped")

    let attachToClient (client: BarClient) =
        lock lifecycleLock (fun () ->
            clientRef <- Some client
            try
                let grid = MapGrid.loadFromEngine client.Stream
                mapGridRef <- Some grid
                renderMapGrid <- Some grid
                metalSpots <- Callbacks.getMetalSpots client.Stream
                myTeamId <- Callbacks.getMyTeam client.Stream
                eprintfn "[GameViz] Attached to client, map %dx%d" grid.WidthHeightmap grid.HeightHeightmap
            with ex ->
                eprintfn "[GameViz] Failed to load map data: %s" ex.Message)

    let seedUnits (unitStates: UnitState list) =
        lock lifecycleLock (fun () ->
            for u in unitStates do
                units <- Map.add u.UnitId u units
            match clientRef with
            | Some c ->
                for u in unitStates do
                    try ensureDefProps c.Stream u.DefId with _ -> ()
            | None -> ())

    let attachWithState (mapGrid: MapGrid) (spots: (float32 * float32 * float32 * float32) array) (teamId: int) =
        lock lifecycleLock (fun () ->
            mapGridRef <- Some mapGrid
            renderMapGrid <- Some mapGrid
            metalSpots <- spots
            myTeamId <- teamId)
        lock configLock (fun () ->
            computeAutoFit mapGrid)
        eprintfn "[GameViz] Attached via state, map %dx%d" mapGrid.WidthHeightmap mapGrid.HeightHeightmap

    let onFrameWithState (gameState: GameState) (mapGrid: MapGrid) =
        // Lock-free: build RawFrame and atomically publish for the render thread.
        // No derived-data computation — the render thread handles that.
        let tid = myTeamId
        let spots = metalSpots
        let counter = Interlocked.Increment(&frameCounter)
        let frame =
            { GameState = gameState
              MapGrid = mapGrid
              MyTeamId = tid
              MetalSpots = spots
              FrameCounter = counter }
        Interlocked.Exchange(&latestFrame, Some frame) |> ignore

    let onFrame (frame: GameFrame) =
        lock lifecycleLock (fun () ->
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

            // Build snapshot and publish atomically for the render thread
            let snap = buildSnapshot grid frameNum true metalEcon energyEcon
            latestDirectSnapshot <- Some snap
            Volatile.Write(&directSnapshotCounter, directSnapshotCounter + 1))

    let setDisconnected () =
        Volatile.Write(&disconnected, 1)
        eprintfn "[GameViz] Disconnected"

    let resetView () =
        lock configLock (fun () ->
            renderMapGrid |> Option.iter computeAutoFit)

    let setBaseLayer (layer: LayerKind) =
        lock configLock (fun () ->
            config <- { config with BaseLayer = layer })

    let toggleOverlay (overlay: OverlayKind) =
        lock configLock (fun () ->
            let ov = if Set.contains overlay config.ActiveOverlays then Set.remove overlay config.ActiveOverlays else Set.add overlay config.ActiveOverlays
            config <- { config with ActiveOverlays = ov })

    let enableOverlay (overlay: OverlayKind) =
        lock configLock (fun () ->
            config <- { config with ActiveOverlays = Set.add overlay config.ActiveOverlays })

    let disableOverlay (overlay: OverlayKind) =
        lock configLock (fun () ->
            config <- { config with ActiveOverlays = Set.remove overlay config.ActiveOverlays })

    let getActiveOverlays () : Set<OverlayKind> =
        lock configLock (fun () -> config.ActiveOverlays)

    let setActiveOverlays (overlays: Set<OverlayKind>) : unit =
        lock configLock (fun () ->
            config <- { config with ActiveOverlays = overlays })

    let setConfig (cfg: VizConfig) =
        lock configLock (fun () -> config <- cfg)

    let updateConfig (f: VizConfig -> VizConfig) =
        lock configLock (fun () -> config <- f config)

    let setColorScheme (layer: LayerKind) (scheme: ColorScheme) =
        lock configLock (fun () ->
            config <- { config with ColorSchemes = Map.add layer scheme config.ColorSchemes }
            LayerRenderer.invalidateCache layer)

    let setMarkerSize (size: float32) =
        lock configLock (fun () ->
            config <- { config with UnitMarkerSize = size })

    let setOverlayOpacity (opacity: float32) =
        lock configLock (fun () ->
            config <- { config with OverlayOpacity = max 0.0f (min 1.0f opacity) })

    let toggleGridLines () =
        lock configLock (fun () ->
            config <- { config with ShowGridLines = not config.ShowGridLines })

    let pan (dx: float32) (dy: float32) =
        lock configLock (fun () ->
            viewState <- { viewState with OriginX = viewState.OriginX + dx; OriginY = viewState.OriginY + dy; AutoFit = false })

    let zoom (factor: float32) (centerX: float32) (centerY: float32) =
        lock configLock (fun () ->
            let mapX = centerX / viewState.Scale + viewState.OriginX
            let mapY = centerY / viewState.Scale + viewState.OriginY
            let newScale = viewState.Scale * factor
            viewState <- { viewState with Scale = newScale; OriginX = mapX - centerX / newScale; OriginY = mapY - centerY / newScale; AutoFit = false })

    let screenshot (folder: string) : Result<string, string> =
        lock lifecycleLock (fun () ->
            match viewer with
            | Some v -> v.Screenshot(folder)
            | None -> Result.Error "No viewer running")

    // --- Configurator panel (feature 033-viz-style-configurator) ---

    let toggleConfigPanel () =
        lock configLock (fun () ->
            panelState <- ConfigPanel.toggle panelState
            if panelState.IsOpen then
                // Snapshot current config as the reference so dirty is false
                // on first open.
                referenceConfig <- config)

    let isConfigPanelOpen () : bool =
        panelState.IsOpen
