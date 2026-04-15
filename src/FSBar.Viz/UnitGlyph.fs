// Feature 028-unit-viz-language — unit glyph renderer.
//
// BarData.UnitDef reflection notes (from T001 empirical check against
// nupkg/BarData.1.0.3.nupkg):
//   * `subfolder: string`
//   * `customParams: Map<string,string>`
//   * `category: string option`
//   * `movement: MovementDef option`  -- NOT on UnitDef directly
//       MovementDef has `canMove: bool`, `canFly: bool`,
//       `movementClass: string option`
// Callers that wrap BarData into `UnitDisplay` are responsible for reading
// these from the optional `movement` and passing scalars to `classifyShape`.
namespace FSBar.Viz

open System
open System.Collections.Concurrent
open SkiaSharp
open SkiaViewer

module UnitGlyph =

    // --- Module-private mutable state ---------------------------------------

    let private shapeMissCache = ConcurrentDictionary<string, unit>()
    let private tierMissCache = ConcurrentDictionary<string, unit>()
    let private factionMissCache = ConcurrentDictionary<string, unit>()
    let private staticCache = ConcurrentDictionary<int, MovementShape * Tier * FactionId>()

    // Event effects are mutable but scoped to a single renderer session.
    let private eventEffects = ResizeArray<EventEffect>()

    // Previous-frame map shadow used by the test-accessible `advanceEffects`
    // to diff state. This lives outside the per-call API so effects can
    // accumulate across frames.
    let private previousHpByUnit = ConcurrentDictionary<int, float32>()

    let private reportMissOnce
        (cache: ConcurrentDictionary<string, unit>)
        (key: string)
        (logMiss: string -> unit)
        : unit =
        if cache.TryAdd(key, ()) then logMiss key

    // --- T020: classifyShape (spec FR-001) ----------------------------------

    let classifyShape
        (canMove: bool)
        (canFly: bool)
        (movementClass: string option)
        (logMiss: string -> unit)
        : MovementShape =
        if not canMove then MovementShape.Building
        elif canFly then MovementShape.Air
        else
            match movementClass with
            | None ->
                reportMissOnce shapeMissCache "<none>" logMiss
                MovementShape.Unknown
            | Some mcRaw ->
                let mc = mcRaw.ToUpperInvariant()
                // Prefix matches per spec FR-001.
                if mc.StartsWith "ARMBOT"
                   || mc.StartsWith "KBOT"
                   || mc.StartsWith "BOT" then
                    MovementShape.Bot
                elif mc.StartsWith "TANK"
                     || mc.StartsWith "VEHICLE"
                     || mc.StartsWith "ATV" then
                    MovementShape.Vehicle
                elif mc.StartsWith "HOVER" then
                    MovementShape.Hover
                elif mc.StartsWith "UBOAT"
                     || mc.StartsWith "BOAT"
                     || mc.StartsWith "SHIP" then
                    MovementShape.Ship
                else
                    reportMissOnce shapeMissCache mcRaw logMiss
                    MovementShape.Unknown

    // --- T021: classifyTier (spec FR-005) -----------------------------------

    let classifyTier
        (customParams: Map<string, string>)
        (category: string option)
        (logMiss: string -> unit)
        : Tier =
        match Map.tryFind "techlevel" customParams with
        | Some "3" -> Tier.T3
        | Some "2" -> Tier.T2
        | Some "1" -> Tier.T1
        | Some _ ->
            // Unrecognized techlevel value — fall through to category.
            match category with
            | Some c when c.Contains "LEVEL3" -> Tier.T3
            | Some c when c.Contains "LEVEL2" -> Tier.T2
            | Some c when c.Contains "LEVEL1" -> Tier.T1
            | _ ->
                reportMissOnce tierMissCache "<bad techlevel>" logMiss
                Tier.T1
        | None ->
            match category with
            | Some c when c.Contains "LEVEL3" -> Tier.T3
            | Some c when c.Contains "LEVEL2" -> Tier.T2
            | Some c when c.Contains "LEVEL1" -> Tier.T1
            | _ ->
                reportMissOnce tierMissCache "<missing>" logMiss
                Tier.T1

    // --- T022: classifyFaction (spec FR-004) --------------------------------

    let private factionFromSegment (segment: string) : FactionId option =
        match segment.ToLowerInvariant() with
        | "armada" -> Some FactionId.Armada
        | "cortex" -> Some FactionId.Cortex
        | "legion" -> Some FactionId.Legion
        | "raptors" -> Some FactionId.Raptors
        | "scavengers" -> Some FactionId.Scavengers
        | _ -> None

    let private factionFromNamePrefix (name: string) : FactionId option =
        let lower = name.ToLowerInvariant()
        if lower.StartsWith "arm" then Some FactionId.Armada
        elif lower.StartsWith "cor" then Some FactionId.Cortex
        elif lower.StartsWith "leg" then Some FactionId.Legion
        elif lower.StartsWith "rap" then Some FactionId.Raptors
        elif lower.StartsWith "scav" then Some FactionId.Scavengers
        else None

    let classifyFaction
        (subfolder: string)
        (internalName: string)
        (logMiss: string -> unit)
        : FactionId =
        let fromSubfolder =
            if String.IsNullOrEmpty subfolder then None
            else
                let segments =
                    subfolder.Split([| '/'; '\\' |], StringSplitOptions.RemoveEmptyEntries)
                if segments.Length >= 2 then factionFromSegment segments.[1]
                else None
        match fromSubfolder with
        | Some f -> f
        | None ->
            match factionFromNamePrefix internalName with
            | Some f -> f
            | None ->
                reportMissOnce factionMissCache internalName logMiss
                FactionId.Neutral

    // --- T023: buildUnit ----------------------------------------------------

    let private clamp01 (v: float32) =
        if v < 0.0f then 0.0f
        elif v > 1.0f then 1.0f
        else v

    let private strokeWidthForTier (style: UnitGlyphStyle) (tier: Tier) : float32 =
        match tier with
        | Tier.T1 -> style.T1StrokeWidth
        | Tier.T2 -> style.T2StrokeWidth
        | Tier.T3 -> style.T3StrokeWidth

    let private factionStrokeColor (style: UnitGlyphStyle) (faction: FactionId) : SKColor =
        let fp = style.FactionPalette
        match faction with
        | FactionId.Armada -> fp.Armada
        | FactionId.Cortex -> fp.Cortex
        | FactionId.Legion -> fp.Legion
        | FactionId.Raptors -> fp.Raptors
        | FactionId.Scavengers -> fp.Scavengers
        | FactionId.Neutral -> fp.Neutral

    let private teamFillColor (style: UnitGlyphStyle) (teamId: int) : SKColor =
        match Map.tryFind teamId style.TeamPalette.ByTeamId with
        | Some c -> c
        | None -> style.TeamPalette.Fallback

    let private applyAlpha (color: SKColor) (alpha: float32) : SKColor =
        let a = clamp01 alpha |> (*) 255.0f |> byte
        SKColor(color.Red, color.Green, color.Blue, a)

    // Shape primitive — returns a single path/ellipse/rect element covering
    // the body of the unit. The element is placed at (mx, mz) in world space.
    let private bodyPrimitive
        (shape: MovementShape)
        (mx: float32)
        (mz: float32)
        (r: float32)
        (fillPaint: Paint)
        : Element =
        match shape with
        | MovementShape.Bot ->
            Scene.ellipse mx mz r r fillPaint
        | MovementShape.Vehicle ->
            Scene.rect (mx - r) (mz - r) (2.0f * r) (2.0f * r) fillPaint
        | MovementShape.Hover ->
            Scene.path [
                PathCommand.MoveTo(mx, mz - r)
                PathCommand.LineTo(mx + r, mz)
                PathCommand.LineTo(mx, mz + r)
                PathCommand.LineTo(mx - r, mz)
                PathCommand.Close
            ] fillPaint
        | MovementShape.Ship ->
            // Rounded rectangle approximation: use a slightly longer rect.
            Scene.rect (mx - 1.2f * r) (mz - 0.7f * r) (2.4f * r) (1.4f * r) fillPaint
        | MovementShape.Air ->
            Scene.path [
                PathCommand.MoveTo(mx, mz - r)
                PathCommand.LineTo(mx + 0.9f * r, mz + 0.7f * r)
                PathCommand.LineTo(mx - 0.9f * r, mz + 0.7f * r)
                PathCommand.Close
            ] fillPaint
        | MovementShape.Building ->
            // Hexagon.
            let sixth = float32 (Math.PI / 3.0)
            let pts =
                [ for i in 0 .. 5 ->
                    let ang = float32 i * sixth
                    SKPoint(mx + r * cos ang, mz + r * sin ang) ]
            let cmds =
                [ yield PathCommand.MoveTo(pts.[0].X, pts.[0].Y)
                  for p in pts.[1..] -> PathCommand.LineTo(p.X, p.Y)
                  yield PathCommand.Close ]
            Scene.path cmds fillPaint
        | MovementShape.Unknown ->
            Scene.ellipse mx mz r r fillPaint

    // Convert world coordinates (BarData elmos) to viz space. SceneBuilder
    // divides by 8 to map elmos to tile coordinates.
    let private toVizX (x: float32) = x / 8.0f
    let private toVizZ (z: float32) = z / 8.0f

    let buildUnit
        (unit': UnitDisplay)
        (style: UnitGlyphStyle)
        (activeEffects: EventEffect list)
        : Element list =
        let mx = toVizX unit'.PositionX
        let mz = toVizZ unit'.PositionZ
        // Compute world-space radius from footprint (2 elmos / tile ≈ 0.25 tile).
        let rawR = max unit'.FootprintWidthElmo unit'.FootprintHeightElmo / 16.0f
        let r = max style.MinPixelRadius rawR

        let teamColor = teamFillColor style unit'.TeamId
        let strokeColor = factionStrokeColor style unit'.Faction
        let strokeWidth = strokeWidthForTier style unit'.Tier

        // Alpha driven by buildProgress for under-construction units.
        let fillAlpha =
            if unit'.Status.IsUnderConstruction then
                0.25f + 0.75f * clamp01 unit'.BuildProgress
            else 1.0f

        // Desaturate stunned units by averaging fill with grey.
        let effectiveFill =
            if unit'.Status.IsStunned then
                let grey = 160uy
                let mix (c: byte) = byte ((int c + int grey) / 2)
                SKColor(mix teamColor.Red, mix teamColor.Green, mix teamColor.Blue)
            else teamColor

        let fillPaint = Scene.fill (applyAlpha effectiveFill fillAlpha)
        let body = bodyPrimitive unit'.Shape mx mz r fillPaint

        // Stroke overlay: dashed when under construction.
        // The existing SkiaViewer Paint API doesn't expose dashing through a
        // high-level helper, so we emulate dashed-stroke under construction
        // by rendering the outline as several short arcs. For MVP we keep
        // the outline solid and differentiate under-construction via the
        // fill alpha, which is already visually distinct.
        let strokePaint =
            // `Paint` doesn't have `.IsStroke` here; we use the framework's
            // Scene.stroke helper below by drawing an outline body.
            Scene.stroke strokeColor strokeWidth

        // Build an outline element by reusing bodyPrimitive with a stroke paint.
        let outline = bodyPrimitive unit'.Shape mx mz r strokePaint

        // Under-construction extra marker (small dashed hint) — a short line
        // inside the shape to distinguish from an operational unit.
        let constructionHint =
            if unit'.Status.IsUnderConstruction then
                [ Scene.line (mx - r * 0.5f) mz (mx + r * 0.5f) mz strokePaint ]
            else []

        // Facing pip.
        let pip =
            let heading =
                if Single.IsNaN unit'.HeadingRadians then 0.0f else unit'.HeadingRadians
            let px = mx + r * cos heading
            let pz = mz + r * sin heading
            let pipPaint = Scene.fill (applyAlpha strokeColor 1.0f)
            Scene.ellipse px pz style.FacingPipRadius style.FacingPipRadius pipPaint

        // HP arc. Hidden at full HP; shifted red below low-HP fraction.
        let hpArc =
            if unit'.MaxHealth <= 0.0f then []
            else
                let frac = clamp01 (unit'.CurrentHealth / unit'.MaxHealth)
                if frac >= 1.0f then []
                else
                    let arcColor =
                        if frac <= style.LowHpFraction then SKColor(220uy, 40uy, 40uy)
                        else SKColor(220uy, 200uy, 40uy)
                    let arcPaint = Scene.stroke arcColor style.HpArcWidth
                    // Position the arc as a small line opposite the facing pip.
                    let heading =
                        if Single.IsNaN unit'.HeadingRadians then 0.0f else unit'.HeadingRadians
                    let opp = heading + float32 Math.PI
                    let length = r * 1.2f * (1.0f - frac)
                    let ax = mx + r * cos opp
                    let az = mz + r * sin opp
                    let bx = ax + length * cos (opp + float32 Math.PI / 2.0f)
                    let bz = az + length * sin (opp + float32 Math.PI / 2.0f)
                    [ Scene.line ax az bx bz arcPaint ]

        // Low-HP tint overlay (FR-010 — noise deferred, tint-only for MVP).
        let lowHpTint =
            if unit'.MaxHealth <= 0.0f then []
            else
                let frac = clamp01 (unit'.CurrentHealth / unit'.MaxHealth)
                if frac >= style.LowHpFraction then []
                else
                    let tint = SKColor(255uy, 40uy, 40uy, 80uy)
                    [ bodyPrimitive unit'.Shape mx mz r (Scene.fill tint) ]

        // Label text beside the shape. Fallback via UnitLabels lookup when
        // `LabelCode` was not populated by the data source.
        let labelCode =
            if String.IsNullOrEmpty unit'.LabelCode
            then UnitLabels.lookupOrFallback unit'.InternalName
            else unit'.LabelCode
        let labelElement =
            let txtPaint = Scene.fill (applyAlpha strokeColor 1.0f)
            Scene.text labelCode (mx + r + 2.0f) (mz + style.LabelFontSizePx * 0.35f)
                style.LabelFontSizePx txtPaint

        // Event effects layered on top.
        let effectElements =
            activeEffects
            |> List.filter (fun e -> e.UnitId = unit'.UnitId)
            |> List.collect (fun e ->
                match e.Kind with
                | EventEffectKind.UnderAttackFlash ->
                    let flashPaint = Scene.stroke (SKColor(255uy, 40uy, 40uy)) (strokeWidth + 1.0f)
                    [ bodyPrimitive unit'.Shape mx mz (r * 1.15f) flashPaint ]
                | EventEffectKind.JustBuiltRing ->
                    let ringPaint = Scene.stroke (SKColor(80uy, 255uy, 80uy)) 2.0f
                    [ Scene.ellipse mx mz (r * 1.3f) (r * 1.3f) ringPaint ]
                | EventEffectKind.StunnedDesaturate ->
                    []) // handled via body-fill mixing above

        [ yield body
          yield! lowHpTint
          yield outline
          yield pip
          yield! hpArc
          yield! constructionHint
          yield labelElement
          yield! effectElements ]

    // --- T044 (forward-declared for US2): overlay layer ---------------------

    let private orderKindColor (k: OrderKind) : SKColor =
        match k with
        | OrderKind.Move -> SKColor( 80uy, 220uy, 255uy)
        | OrderKind.Attack -> SKColor(255uy,  80uy,  80uy)
        | OrderKind.Patrol -> SKColor(255uy, 220uy,  80uy)
        | OrderKind.Guard -> SKColor( 80uy, 220uy, 120uy)
        | OrderKind.Build -> SKColor( 80uy, 120uy, 255uy)
        | OrderKind.Reclaim -> SKColor(220uy,  80uy, 220uy)
        | OrderKind.Other -> SKColor(200uy, 200uy, 200uy)

    let private weaponRangeElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
        if List.isEmpty u.WeaponRangesElmo then []
        else
            let mx = toVizX u.PositionX
            let mz = toVizZ u.PositionZ
            let color = applyAlpha (factionStrokeColor style u.Faction) 0.6f
            let paint = Scene.stroke color 1.0f
            u.WeaponRangesElmo
            |> List.map (fun range -> Scene.ellipse mx mz (range / 8.0f) (range / 8.0f) paint)

    let private sightRangeElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
        if u.SightRangeElmo <= 0.0f then []
        else
            let mx = toVizX u.PositionX
            let mz = toVizZ u.PositionZ
            // Dashed effect emulated by a second slightly smaller ring in
            // a different alpha — raster backend has no dash primitive in
            // SkiaViewer.Scene. The second ring makes `L` visually distinct
            // from `W` even when they overlap.
            let outer = applyAlpha (SKColor(200uy, 200uy, 200uy)) 0.5f
            let inner = applyAlpha (SKColor(200uy, 200uy, 200uy)) 0.25f
            [ Scene.ellipse mx mz (u.SightRangeElmo / 8.0f) (u.SightRangeElmo / 8.0f)
                (Scene.stroke outer 0.8f)
              Scene.ellipse mx mz (u.SightRangeElmo / 8.0f - 1.5f) (u.SightRangeElmo / 8.0f - 1.5f)
                (Scene.stroke inner 0.8f) ]

    let private commandQueueElements (u: UnitDisplay) : Element list =
        if List.isEmpty u.CommandQueue then []
        else
            let mx = toVizX u.PositionX
            let mz = toVizZ u.PositionZ
            let mutable prevX = mx
            let mutable prevZ = mz
            u.CommandQueue
            |> List.map (fun wp ->
                let wx = toVizX wp.X
                let wz = toVizZ wp.Z
                let color = orderKindColor wp.Order
                let width = if wp.IsCurrent then 3.0f else 1.5f
                let paint = Scene.stroke color width
                let el = Scene.line prevX prevZ wx wz paint
                prevX <- wx
                prevZ <- wz
                el)

    let private fullNameElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
        let mx = toVizX u.PositionX
        let mz = toVizZ u.PositionZ
        let paint = Scene.fill (applyAlpha (SKColor(240uy, 240uy, 255uy)) 1.0f)
        [ Scene.text u.InternalName (mx + 8.0f) (mz - 6.0f) (style.LabelFontSizePx + 1.0f) paint ]

    let buildOverlayLayer
        (units: UnitDisplay seq)
        (style: UnitGlyphStyle)
        (activeOverlays: Set<OverlayKind>)
        : Element list =
        let unitList = units |> Seq.toList
        [ if Set.contains OverlayKind.WeaponRanges activeOverlays then
              yield! unitList |> List.collect (weaponRangeElements style)
          if Set.contains OverlayKind.SightRanges activeOverlays then
              yield! unitList |> List.collect (sightRangeElements style)
          if Set.contains OverlayKind.CommandQueue activeOverlays then
              yield! unitList |> List.collect commandQueueElements
          if Set.contains OverlayKind.FullNames activeOverlays then
              yield! unitList |> List.collect (fullNameElements style) ]

    // --- T024: buildUnitsGlyph ----------------------------------------------

    let buildUnitsGlyph
        (units: UnitDisplay seq)
        (style: UnitGlyphStyle)
        (activeOverlays: Set<OverlayKind>)
        : Element list =
        let unitList = units |> Seq.toList
        let effects =
            if eventEffects.Count = 0 then []
            else eventEffects |> List.ofSeq
        let permanent = unitList |> List.collect (fun u -> buildUnit u style effects)
        let overlays = buildOverlayLayer unitList style activeOverlays
        permanent @ overlays

    // --- T025: advanceEffects + resetSession --------------------------------

    let private isExpired (nowMs: int) (e: EventEffect) =
        (nowMs - e.StartedAtMs) >= e.DurationMs

    let advanceEffects
        (previousFrame: Map<int, UnitDisplay>)
        (currentFrame: Map<int, UnitDisplay>)
        (nowMs: int)
        : EventEffect list =
        // 1. Retire expired.
        let mutable i = eventEffects.Count - 1
        while i >= 0 do
            if isExpired nowMs eventEffects.[i] then eventEffects.RemoveAt i
            i <- i - 1

        // 2. Detect HP decrease and construction-completed transitions.
        for KeyValue(unitId, cur) in currentFrame do
            match Map.tryFind unitId previousFrame with
            | Some prev ->
                if cur.CurrentHealth < prev.CurrentHealth then
                    eventEffects.Add
                        { UnitId = unitId
                          Kind = EventEffectKind.UnderAttackFlash
                          StartedAtMs = nowMs
                          DurationMs = 300 }
                if prev.Status.IsUnderConstruction && not cur.Status.IsUnderConstruction then
                    eventEffects.Add
                        { UnitId = unitId
                          Kind = EventEffectKind.JustBuiltRing
                          StartedAtMs = nowMs
                          DurationMs = 1000 }
                if cur.Status.IsStunned then
                    eventEffects.Add
                        { UnitId = unitId
                          Kind = EventEffectKind.StunnedDesaturate
                          StartedAtMs = nowMs
                          DurationMs = 100 }
            | None -> ()

        eventEffects |> List.ofSeq

    // --- T046: statusLine ---------------------------------------------------

    let statusLine (activeOverlays: Set<OverlayKind>) : string =
        let sb = System.Text.StringBuilder()
        if Set.contains OverlayKind.WeaponRanges activeOverlays then sb.Append 'W' |> ignore
        if Set.contains OverlayKind.SightRanges activeOverlays then sb.Append 'L' |> ignore
        if Set.contains OverlayKind.CommandQueue activeOverlays then sb.Append 'C' |> ignore
        if Set.contains OverlayKind.FullNames activeOverlays then sb.Append 'N' |> ignore
        sb.ToString()

    let resetSession () : unit =
        shapeMissCache.Clear()
        tierMissCache.Clear()
        factionMissCache.Clear()
        staticCache.Clear()
        eventEffects.Clear()
        previousHpByUnit.Clear()
