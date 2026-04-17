namespace FSBar.Viz

open SkiaSharp
open SkiaViewer
open FSBar.Client

module SceneBuilder =

    // --- Economy HUD interpolation state ---
    let mutable private prevMetalDisplay = 0.0f
    let mutable private prevEnergyDisplay = 0.0f
    let smoothFactor = 0.15f

    let lerpF (a: float32) (b: float32) (t: float32) = a + (b - a) * t

    // --- Metal-spot pulse state ---
    let mutable private pulsePhase = 0.0f
    // Cumulative wall-clock seconds since the session started. SkiaViewer's
    // `InputEvent.FrameTick(delta)` passes per-frame delta, not a running
    // total, so we accumulate it ourselves.
    let mutable private pulseElapsedSeconds = 0.0

    let twoPi = 2.0 * System.Math.PI

    let computePulseAlpha (elapsed: float) (periodSeconds: float) : byte =
        let phase = 0.5 + 0.5 * sin (twoPi * elapsed / periodSeconds)
        let v = 60.0 + 160.0 * phase
        let clamped =
            if v < 60.0 then 60.0
            elif v > 220.0 then 220.0
            else v
        byte clamped

    /// Reset the pulse clock — call on session start/stop so two back-to-back
    /// viewers don't leak state.
    let resetPulsePhase () : unit =
        pulseElapsedSeconds <- 0.0
        pulsePhase <- 0.0f

    /// Advance the pulse clock by one FrameTick's delta and recompute the
    /// shared phase used by metal-spot markers.
    let updatePulsePhase (deltaSeconds: float) : unit =
        pulseElapsedSeconds <- pulseElapsedSeconds + deltaSeconds
        let phase = 0.5 + 0.5 * sin (twoPi * pulseElapsedSeconds / 1.5)
        pulsePhase <- float32 phase

    // --- Coordinate transform helpers ---
    let inline private mapX (posX: float32) = posX / 8.0f
    let inline private mapZ (posZ: float32) = posZ / 8.0f

    let viewportTransform (vs: ViewState) =
        Transform.Compose [
            Transform.Translate(-vs.OriginX * vs.Scale, -vs.OriginY * vs.Scale)
            Transform.Scale(vs.Scale, vs.Scale, 0.0f, 0.0f)
        ]

    // --- Base Layer ---
    let buildBaseLayer (snap: GameSnapshot) (config: VizConfig) (vs: ViewState) =
        let grid = snap.MapGrid
        let w = grid.WidthHeightmap
        let h = grid.HeightHeightmap
        if w = 0 || h = 0 then
            [ Scene.text "No data" (float32 vs.WindowWidth / 2.0f - 40.0f) (float32 vs.WindowHeight / 2.0f)
                  18.0f (Scene.fill config.LabelColor) ]
        else
            let scheme =
                match Map.tryFind config.BaseLayer config.ColorSchemes with
                | Some s -> s
                | None -> ColorMaps.colorSchemeFor config.BaseLayer
            let bmp = LayerRenderer.renderLayer grid config.BaseLayer scheme
            let paint =
                Scene.fill SKColors.White
                |> Scene.withShader (Shader.Image(bmp, TileMode.Clamp, TileMode.Clamp))
            [ Scene.rect 0.0f 0.0f (float32 bmp.Width) (float32 bmp.Height) paint ]

    // --- Grid Overlay ---
    let buildGrid (snap: GameSnapshot) (config: VizConfig) =
        if not (Set.contains OverlayKind.Grid config.ActiveOverlays) || not config.ShowGridLines then []
        else
            let w = snap.MapGrid.WidthHeightmap
            let h = snap.MapGrid.HeightHeightmap
            let spacing = config.GridLineSpacing
            let paint =
                Scene.stroke (SKColor(255uy, 255uy, 255uy, 60uy)) 0.5f
                |> Scene.withOpacity config.OverlayOpacity
            let lines = System.Collections.Generic.List<Element>()
            let mutable x = 0
            while x <= w do
                lines.Add(Scene.line (float32 x) 0.0f (float32 x) (float32 h) paint)
                x <- x + spacing
            let mutable z = 0
            while z <= h do
                lines.Add(Scene.line 0.0f (float32 z) (float32 w) (float32 z) paint)
                z <- z + spacing
            lines |> Seq.toList

    // --- Metal Spots Overlay ---
    let buildMetalSpots (snap: GameSnapshot) (config: VizConfig) (vs: ViewState) =
        if not (Set.contains OverlayKind.MetalSpots config.ActiveOverlays) then []
        else
            let phase = pulsePhase
            // Gentle pulse — 30% of the earlier swings.
            // Alpha: 160..210, dot: 200..225, radius scale: 0.91..1.09 (~±9%).
            let coreAlpha = byte (160.0f + 50.0f * phase)
            let dotAlpha = byte (200.0f + 25.0f * phase)
            let radiusScale = 0.91f + 0.18f * phase
            // Target sizes in SCREEN pixels, converted to world space by dividing
            // by viewState.Scale — so markers stay the same apparent size when
            // the user zooms in/out.
            let scale = max 0.0001f vs.Scale
            let nominalScreenR = 4.5f
            let dotScreenR = 0.9f
            snap.MetalSpots |> Array.toList |> List.collect (fun (x, _y, z, richness) ->
                let mx = mapX x
                let mz = mapZ z
                let r = (nominalScreenR + richness * 1.5f) * radiusScale / scale
                let dotR = dotScreenR / scale
                let glowPaint =
                    Scene.fill (SKColor(255uy, 210uy, 40uy, coreAlpha))
                    |> Scene.withOpacity config.OverlayOpacity
                let dotPaint =
                    Scene.fill (SKColor(20uy, 10uy, 0uy, dotAlpha))
                    |> Scene.withOpacity config.OverlayOpacity
                [ Scene.ellipse mx mz r r glowPaint
                  Scene.ellipse mx mz dotR dotR dotPaint ])

    // --- Unit Overlay ---
    let defaultStatus : StatusFlags =
        { IsUnderConstruction = false
          IsStunned = false
          JustDamagedWithinMs = None
          JustCompletedWithinMs = None
          IsCloaked = false }

    let legacyToUnitDisplay (u: UnitState) : UnitDisplay =
        { UnitId = u.UnitId
          DefId = u.DefId
          InternalName = sprintf "def%d" u.DefId
          Shape = MovementShape.Bot
          Faction = FactionId.Neutral
          Tier = Tier.T1
          LabelCode = "??"
          FootprintWidthElmo = 32.0f
          FootprintHeightElmo = 32.0f
          TeamId = u.TeamId
          PositionX = u.PositionX
          PositionY = u.PositionY
          PositionZ = u.PositionZ
          HeadingRadians = 0.0f
          CurrentHealth = u.Health
          MaxHealth = u.MaxHealth
          BuildProgress = 1.0f
          Status = defaultStatus
          WeaponRangesElmo = []
          SightRangeElmo = 0.0f
          BuildRangeElmo = None
          CommandQueue = [] }

    let resolveDisplayUnits (snap: GameSnapshot) =
        if not (Map.isEmpty snap.DisplayUnits) then
            snap.DisplayUnits |> Map.toSeq |> Seq.map snd
        else
            snap.Units |> Map.toSeq |> Seq.map (fun (_, u) -> legacyToUnitDisplay u)

    let buildUnits (snap: GameSnapshot) (config: VizConfig) =
        if not (Set.contains OverlayKind.Units config.ActiveOverlays) then []
        elif config.UseGlyphRenderer then
            let displays = resolveDisplayUnits snap
            let glyphOverlays =
                config.ActiveOverlays
                |> Set.filter (fun o ->
                    match o with
                    | OverlayKind.WeaponRanges
                    | OverlayKind.SightRanges
                    | OverlayKind.CommandQueue
                    | OverlayKind.FullNames -> true
                    | _ -> false)
            UnitGlyph.buildUnitsGlyph displays config.GlyphStyle glyphOverlays
        else
            let displays = resolveDisplayUnits snap
            displays |> Seq.toList |> List.collect (fun u ->
                let mx = mapX u.PositionX
                let mz = mapZ u.PositionZ
                let r = config.UnitMarkerSize
                let centerColor, edgeColor =
                    if u.TeamId <> 0 then
                        SKColor(255uy, 40uy, 40uy, 220uy), SKColor(255uy, 40uy, 40uy, 0uy)
                    else
                        SKColor(40uy, 220uy, 255uy, 220uy), SKColor(40uy, 220uy, 255uy, 0uy)
                let paint =
                    Scene.fill SKColors.Transparent
                    |> Scene.withShader (
                        Shader.RadialGradient(
                            SKPoint(mx, mz), r,
                            [| centerColor; edgeColor |],
                            [| 0.0f; 1.0f |],
                            TileMode.Clamp))
                    |> Scene.withOpacity config.OverlayOpacity
                let marker = Scene.ellipse mx mz r r paint
                let healthFrac = if u.MaxHealth > 0.0f then u.CurrentHealth / u.MaxHealth else 1.0f
                let barW = r * 1.5f
                let barH = 1.5f
                let barX = mx - barW / 2.0f
                let barY = mz + r + 1.0f
                let bgPaint = Scene.fill (SKColor(40uy, 40uy, 40uy, 160uy))
                let barColor =
                    if healthFrac > 0.5f then SKColor(40uy, 200uy, 40uy, 200uy)
                    elif healthFrac > 0.25f then SKColor(220uy, 200uy, 40uy, 200uy)
                    else SKColor(220uy, 40uy, 40uy, 200uy)
                let fgPaint = Scene.fill barColor
                let bg = Scene.rect barX barY barW barH bgPaint
                let fg = Scene.rect barX barY (barW * healthFrac) barH fgPaint
                [ marker; bg; fg ])

    // --- Event Overlay ---
    let buildEvents (snap: GameSnapshot) (config: VizConfig) =
        if not (Set.contains OverlayKind.Events config.ActiveOverlays) then []
        else
            snap.EventIndicators |> List.choose (fun ev ->
                let progress =
                    float32 (snap.FrameNumber - ev.FrameCreated) / float32 ev.DurationFrames
                    |> max 0.0f |> min 1.0f
                if progress >= 1.0f then None
                else
                    let mx = mapX ev.PositionX
                    let mz = mapZ ev.PositionZ
                    let alpha = byte (255.0f * (1.0f - progress))
                    let elem =
                        match ev.Kind with
                        | EventKind.UnitCreated ->
                            let r = 4.0f + progress * 16.0f
                            let paint =
                                Scene.stroke (SKColor(40uy, 255uy, 40uy, alpha)) 1.5f
                                |> Scene.withOpacity config.OverlayOpacity
                            Scene.ellipse mx mz r r paint
                        | EventKind.UnitDestroyed ->
                            let r = 16.0f * (1.0f - progress)
                            let paint =
                                Scene.fill (SKColor(255uy, 100uy, 0uy, alpha))
                                |> Scene.withMaskFilter (MaskFilter.Blur(BlurStyle.Normal, 3.0f * (1.0f - progress)))
                                |> Scene.withOpacity config.OverlayOpacity
                            Scene.ellipse mx mz r r paint
                        | EventKind.EnemySpotted ->
                            let pulse = 0.5f + 0.5f * sin (progress * 6.2832f * 3.0f)
                            let sz = 6.0f + pulse * 4.0f
                            let paint =
                                Scene.fill (SKColor(255uy, 220uy, 0uy, alpha))
                                |> Scene.withOpacity config.OverlayOpacity
                            Scene.path [
                                PathCommand.MoveTo(mx, mz - sz)
                                PathCommand.LineTo(mx + sz * 0.6f, mz)
                                PathCommand.LineTo(mx, mz + sz)
                                PathCommand.LineTo(mx - sz * 0.6f, mz)
                                PathCommand.Close
                            ] paint
                        | EventKind.Combat ->
                            let r = 8.0f + progress * 8.0f
                            let paint =
                                Scene.fill SKColors.Transparent
                                |> Scene.withShader (
                                    Shader.RadialGradient(
                                        SKPoint(mx, mz), r,
                                        [| SKColor(255uy, 60uy, 0uy, alpha); SKColor(255uy, 60uy, 0uy, 0uy) |],
                                        [| 0.0f; 1.0f |],
                                        TileMode.Clamp))
                                |> Scene.withImageFilter (ImageFilter.Blur(2.0f, 2.0f))
                                |> Scene.withOpacity config.OverlayOpacity
                            Scene.ellipse mx mz r r paint
                    Some elem)

    // --- Economy HUD ---
    let buildEconomyHud (snap: GameSnapshot) (config: VizConfig) (vs: ViewState) =
        if not (Set.contains OverlayKind.EconomyHud config.ActiveOverlays) then []
        else
            let ww = float32 vs.WindowWidth
            let hudW = 320.0f
            let hudH = 110.0f
            let hudX = ww - hudW - 10.0f
            let hudY = float32 vs.WindowHeight - hudH - 10.0f
            let barW = 260.0f
            let barH = 20.0f
            let labelX = hudX + 10.0f
            let barX = hudX + 40.0f

            // Interpolate economy display values
            let metalFrac =
                if snap.EconomyMetal.Storage > 0.0f then snap.EconomyMetal.Current / snap.EconomyMetal.Storage
                else 0.0f
            let energyFrac =
                if snap.EconomyEnergy.Storage > 0.0f then snap.EconomyEnergy.Current / snap.EconomyEnergy.Storage
                else 0.0f
            prevMetalDisplay <- lerpF prevMetalDisplay metalFrac smoothFactor
            prevEnergyDisplay <- lerpF prevEnergyDisplay energyFrac smoothFactor
            let metalDisplay = prevMetalDisplay |> max 0.0f |> min 1.0f
            let energyDisplay = prevEnergyDisplay |> max 0.0f |> min 1.0f

            // HUD background with Perlin noise texture
            let bgPaint =
                Scene.fill (SKColor(20uy, 20uy, 30uy, 200uy))
                |> Scene.withShader (
                    Shader.Compose(
                        Shader.SolidColor(SKColor(20uy, 20uy, 30uy, 200uy)),
                        Shader.PerlinNoiseFractalNoise(0.05f, 0.05f, 2, 42.0f),
                        BlendMode.SoftLight))
            let bg = Scene.rect hudX hudY hudW hudH bgPaint

            // Metal bar
            let metalLow = metalDisplay < 0.1f
            let metalGradColors =
                if metalLow then [| SKColor(200uy, 40uy, 40uy); SKColor(255uy, 80uy, 80uy) |]
                else [| SKColor(160uy, 170uy, 180uy); SKColor(220uy, 230uy, 240uy) |]
            let metalBarBg = Scene.rect barX (hudY + 12.0f) barW barH (Scene.fill (SKColor(40uy, 40uy, 50uy, 180uy)))
            let metalBarFg =
                let paint =
                    Scene.fill SKColors.Transparent
                    |> Scene.withShader (
                        Shader.LinearGradient(
                            SKPoint(barX, 0.0f), SKPoint(barX + barW * metalDisplay, 0.0f),
                            metalGradColors, [| 0.0f; 1.0f |], TileMode.Clamp))
                Scene.rect barX (hudY + 12.0f) (barW * metalDisplay) barH paint
            let metalLabelColor = if metalLow then SKColor(255uy, 80uy, 80uy) else SKColors.White
            let metalLabel =
                Scene.text "M" labelX (hudY + 28.0f) 16.0f (Scene.fill metalLabelColor)
            let metalValues =
                let txt = sprintf "%.0f/%.0f +%.1f -%.1f"
                            snap.EconomyMetal.Current snap.EconomyMetal.Storage
                            snap.EconomyMetal.Income snap.EconomyMetal.Usage
                Scene.text txt (barX + 6.0f) (hudY + 28.0f) 13.0f (Scene.fill SKColors.White)

            // Energy bar
            let energyLow = energyDisplay < 0.1f
            let energyGradColors =
                if energyLow then [| SKColor(200uy, 40uy, 40uy); SKColor(255uy, 80uy, 80uy) |]
                else [| SKColor(220uy, 200uy, 40uy); SKColor(255uy, 180uy, 40uy) |]
            let energyBarBg = Scene.rect barX (hudY + 42.0f) barW barH (Scene.fill (SKColor(40uy, 40uy, 50uy, 180uy)))
            let energyBarFg =
                let paint =
                    Scene.fill SKColors.Transparent
                    |> Scene.withShader (
                        Shader.LinearGradient(
                            SKPoint(barX, 0.0f), SKPoint(barX + barW * energyDisplay, 0.0f),
                            energyGradColors, [| 0.0f; 1.0f |], TileMode.Clamp))
                Scene.rect barX (hudY + 42.0f) (barW * energyDisplay) barH paint
            let energyLabelColor = if energyLow then SKColor(255uy, 80uy, 80uy) else SKColors.White
            let energyLabel =
                Scene.text "E" labelX (hudY + 58.0f) 16.0f (Scene.fill energyLabelColor)
            let energyValues =
                let txt = sprintf "%.0f/%.0f +%.1f -%.1f"
                            snap.EconomyEnergy.Current snap.EconomyEnergy.Storage
                            snap.EconomyEnergy.Income snap.EconomyEnergy.Usage
                Scene.text txt (barX + 6.0f) (hudY + 58.0f) 13.0f (Scene.fill SKColors.White)

            // Frame counter
            let frameText =
                Scene.text (sprintf "Frame %d" snap.FrameNumber) (hudX + 10.0f) (hudY + hudH - 8.0f)
                    12.0f (Scene.fill (SKColor(200uy, 200uy, 220uy, 220uy)))

            [ bg; metalBarBg; metalBarFg; metalLabel; metalValues
              energyBarBg; energyBarFg; energyLabel; energyValues; frameText ]

    // --- Disconnected Overlay ---
    let buildDisconnected (snap: GameSnapshot) (vs: ViewState) =
        if snap.Connected then []
        else
            let w = float32 vs.WindowWidth
            let h = float32 vs.WindowHeight
            let overlay = Scene.rect 0.0f 0.0f w h (Scene.fill (SKColor(0uy, 0uy, 0uy, 160uy)))
            let label =
                Scene.text "DISCONNECTED" (w / 2.0f - 80.0f) (h / 2.0f)
                    24.0f (Scene.fill (SKColor(255uy, 60uy, 60uy)))
            [ overlay; label ]

    // --- Main entry point ---
    let buildScene (snapshot: GameSnapshot) (config: VizConfig) (viewState: ViewState) : Scene =
        let vt = viewportTransform viewState

        // World-space elements (under viewport transform)
        let worldElements =
            List.concat [
                buildBaseLayer snapshot config viewState
                buildGrid snapshot config
                buildMetalSpots snapshot config viewState
                buildUnits snapshot config
                buildEvents snapshot config
            ]

        let worldGroup =
            Scene.group (Some vt) None worldElements

        // Screen-space elements (no viewport transform)
        let screenElements =
            List.concat [
                buildEconomyHud snapshot config viewState
                buildDisconnected snapshot viewState
            ]

        Scene.create config.BackgroundColor (worldGroup :: screenElements)

    // --- Headless entry (feature 035-central-gui-hub T013) ---

    /// Synthesises a minimal MapGrid for the None case so early-frame
    /// scenes render an empty base layer instead of throwing. 16x16
    /// heightmap cells = 128 elmos square, small enough to be cheap,
    /// large enough that downstream dimension asserts pass.
    let private emptyHeadlessMapGrid () : MapGrid =
        let widthH = 16
        let heightH = 16
        { WidthElmos = widthH * 8
          HeightElmos = heightH * 8
          WidthHeightmap = widthH
          HeightHeightmap = heightH
          HeightMap = Array2D.create (widthH + 1) (heightH + 1) 0.0f
          SlopeMap = Array2D.create (widthH / 2) (heightH / 2) 0.0f
          ResourceMap = Array2D.zeroCreate widthH heightH
          LosMap = Array2D.zeroCreate widthH heightH
          RadarMap = Array2D.zeroCreate widthH heightH }

    let private emptyEconomy : EconomyData =
        { Current = 0.0f; Income = 0.0f; Usage = 0.0f; Storage = 0.0f }

    let private economyFrom (s: FSBar.Client.EconomySnapshot) : EconomyData =
        { Current = s.Current
          Income = s.Income
          Usage = s.Usage
          Storage = s.Storage }

    let private gameStateToSnapshotWith
            (state: FSBar.Client.GameState)
            (mapGrid: MapGrid)
            (metalSpots: (float32 * float32 * float32 * float32) array)
            : GameSnapshot =
        let friendlies =
            state.Units
            |> Map.toSeq
            |> Seq.map (fun (uid, u) ->
                let (px, py, pz) = u.Position
                uid, { UnitId = uid
                       PositionX = px
                       PositionY = py
                       PositionZ = pz
                       TeamId = state.TeamId
                       DefId = u.DefId
                       Health = u.Health
                       MaxHealth = u.MaxHealth
                       IsEnemy = false })
            |> Map.ofSeq
        let enemies =
            state.Enemies
            |> Map.toSeq
            |> Seq.filter (fun (_, e) -> e.InLOS)
            |> Seq.map (fun (eid, e) ->
                let (px, py, pz) = e.Position
                let defId = e.DefId |> Option.defaultValue 0
                let hp = e.Health |> Option.defaultValue 0.0f
                eid, { UnitId = eid
                       PositionX = px
                       PositionY = py
                       PositionZ = pz
                       TeamId = -1
                       DefId = defId
                       Health = hp
                       MaxHealth = max hp 1.0f
                       IsEnemy = true })
            |> Map.ofSeq
        let allUnits =
            enemies |> Map.fold (fun acc k v -> Map.add k v acc) friendlies
        { FrameNumber = int state.FrameNumber
          MapGrid = mapGrid
          Units = allUnits
          DisplayUnits = Map.empty
          EventIndicators = []
          EconomyMetal = economyFrom state.Metal
          EconomyEnergy = economyFrom state.Energy
          MetalSpots = metalSpots
          Connected = true }

    let private gameStateToSnapshot (state: FSBar.Client.GameState) (mapGrid: MapGrid) : GameSnapshot =
        gameStateToSnapshotWith state mapGrid [||]

    let buildSceneHeadless (state: FSBar.Client.GameState) (map: FSBar.Client.MapGrid option) (config: VizConfig) : Scene =
        let mapGrid = map |> Option.defaultWith emptyHeadlessMapGrid
        let snapshot = gameStateToSnapshot state mapGrid
        buildScene snapshot config VizDefaults.defaultViewState

    let buildSceneHeadlessSized
            (state: FSBar.Client.GameState)
            (map: FSBar.Client.MapGrid option)
            (metalSpots: (float32 * float32 * float32 * float32) array)
            (config: VizConfig)
            (viewportWidth: int)
            (viewportHeight: int)
            : Scene =
        let mapGrid = map |> Option.defaultWith emptyHeadlessMapGrid
        let snapshot = gameStateToSnapshotWith state mapGrid metalSpots
        // Compute Scale so the map fits the viewport. The base-layer
        // renders at 1 pixel per heightmap cell under Scale=1.0;
        // choose the smaller of the two axes' ratios so the whole map
        // fits with letterboxing rather than cropping.
        let scale =
            if mapGrid.WidthHeightmap = 0 || mapGrid.HeightHeightmap = 0 then 1.0f
            else
                let sx = float32 viewportWidth / float32 mapGrid.WidthHeightmap
                let sy = float32 viewportHeight / float32 mapGrid.HeightHeightmap
                min sx sy
        let vs =
            { VizDefaults.defaultViewState with
                Scale = scale
                WindowWidth = viewportWidth
                WindowHeight = viewportHeight
                AutoFit = true }
        buildScene snapshot config vs
