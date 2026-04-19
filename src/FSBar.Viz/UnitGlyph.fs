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

    let shapeMissCache = ConcurrentDictionary<string, unit>()
    let tierMissCache = ConcurrentDictionary<string, unit>()
    let factionMissCache = ConcurrentDictionary<string, unit>()
    let staticCache = ConcurrentDictionary<int, MovementShape * Tier * FactionId>()

    // Event effects are mutable but scoped to a single renderer session.
    let eventEffects = ResizeArray<EventEffect>()

    // Previous-frame map shadow used by the test-accessible `advanceEffects`
    // to diff state. This lives outside the per-call API so effects can
    // accumulate across frames.
    let previousHpByUnit = ConcurrentDictionary<int, float32>()

    let reportMissOnce
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
        let parseTech (raw: string) : Tier option =
            // BarData stores techlevel as e.g. "1.0" / "2.0" / "3" — be
            // permissive about trailing ".0", case, and whitespace.
            match raw with
            | "3" | "3.0" -> Some Tier.T3
            | "2" | "2.0" -> Some Tier.T2
            | "1" | "1.0" -> Some Tier.T1
            | _ ->
                match System.Double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture) with
                | true, v when v >= 2.5 -> Some Tier.T3
                | true, v when v >= 1.5 -> Some Tier.T2
                | true, v when v >= 0.5 -> Some Tier.T1
                | _ -> None
        match Map.tryFind "techlevel" customParams with
        | Some raw ->
            match parseTech raw with
            | Some t -> t
            | None ->
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

    let factionFromSegment (segment: string) : FactionId option =
        match segment.ToLowerInvariant() with
        | "armada" -> Some FactionId.Armada
        | "cortex" -> Some FactionId.Cortex
        | "legion" -> Some FactionId.Legion
        | "raptors" -> Some FactionId.Raptors
        | "scavengers" -> Some FactionId.Scavengers
        | _ -> None

    let factionFromNamePrefix (name: string) : FactionId option =
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

    let clamp01 (v: float32) =
        if v < 0.0f then 0.0f
        elif v > 1.0f then 1.0f
        else v

    let strokeWidthForTier (style: UnitGlyphStyle) (tier: Tier) : float32 =
        match tier with
        | Tier.T1 -> style.T1StrokeWidth
        | Tier.T2 -> style.T2StrokeWidth
        | Tier.T3 -> style.T3StrokeWidth

    let factionStrokeColor (style: UnitGlyphStyle) (faction: FactionId) : SKColor =
        let fp = style.FactionPalette
        match faction with
        | FactionId.Armada -> fp.Armada
        | FactionId.Cortex -> fp.Cortex
        | FactionId.Legion -> fp.Legion
        | FactionId.Raptors -> fp.Raptors
        | FactionId.Scavengers -> fp.Scavengers
        | FactionId.Neutral -> fp.Neutral

    let teamFillColor (style: UnitGlyphStyle) (teamId: int) : SKColor =
        match Map.tryFind teamId style.TeamPalette.ByTeamId with
        | Some c -> c
        | None -> style.TeamPalette.Fallback

    let applyAlpha (color: SKColor) (alpha: float32) : SKColor =
        let a = clamp01 alpha |> (*) 255.0f |> byte
        SKColor(color.Red, color.Green, color.Blue, a)

    // Shape outline as a closed path starting at the unit's "front" (east,
    // i.e. +X) and walking clockwise around the perimeter. The returned
    // commands form a single closed loop so `PathEffect.Trim` can extract
    // the back half of the outline by path-length fractions:
    //   fraction 0.0  = front (east, +X)
    //   fraction 0.25 = south (+Z)
    //   fraction 0.5  = back (west, -X)
    //   fraction 0.75 = north (-Z)
    // The whole shape is expected to be rotated by `heading` around (mx, mz)
    // so the rendered front always points along the unit's heading.
    let shapeOutlineCommands
        (shape: MovementShape)
        (mx: float32)
        (mz: float32)
        (r: float32)
        : PathCommand list =
        match shape with
        | MovementShape.Bot
        | MovementShape.Unknown ->
            // Explicit circle path starting at east, going clockwise.
            // Four quadrant arcs keep path length ≈ 2πr so PathEffect.Trim
            // fractions line up with angular position.
            let rect = SKRect(mx - r, mz - r, mx + r, mz + r)
            [ PathCommand.MoveTo(mx + r, mz)
              PathCommand.ArcTo(rect, 0.0f, 90.0f)
              PathCommand.ArcTo(rect, 90.0f, 90.0f)
              PathCommand.ArcTo(rect, 180.0f, 90.0f)
              PathCommand.ArcTo(rect, 270.0f, 90.0f)
              PathCommand.Close ]
        | MovementShape.Vehicle ->
            // Square with front at +X centre. Start at front-centre so
            // fraction 0.5 lands on back-centre.
            [ PathCommand.MoveTo(mx + r, mz)
              PathCommand.LineTo(mx + r, mz + r)
              PathCommand.LineTo(mx - r, mz + r)
              PathCommand.LineTo(mx - r, mz - r)
              PathCommand.LineTo(mx + r, mz - r)
              PathCommand.LineTo(mx + r, mz)
              PathCommand.Close ]
        | MovementShape.Hover ->
            // Diamond — front vertex at +X.
            [ PathCommand.MoveTo(mx + r, mz)
              PathCommand.LineTo(mx, mz + r)
              PathCommand.LineTo(mx - r, mz)
              PathCommand.LineTo(mx, mz - r)
              PathCommand.Close ]
        | MovementShape.Ship ->
            // Wide rectangle (1.2r × 0.7r half-extents) oriented east.
            let halfW = 1.2f * r
            let halfH = 0.7f * r
            [ PathCommand.MoveTo(mx + halfW, mz)
              PathCommand.LineTo(mx + halfW, mz + halfH)
              PathCommand.LineTo(mx - halfW, mz + halfH)
              PathCommand.LineTo(mx - halfW, mz - halfH)
              PathCommand.LineTo(mx + halfW, mz - halfH)
              PathCommand.LineTo(mx + halfW, mz)
              PathCommand.Close ]
        | MovementShape.Air ->
            // Isoceles triangle pointing east.
            [ PathCommand.MoveTo(mx + r, mz)
              PathCommand.LineTo(mx - 0.5f * r, mz + 0.9f * r)
              PathCommand.LineTo(mx - 0.5f * r, mz - 0.9f * r)
              PathCommand.Close ]
        | MovementShape.Building ->
            let sixth = float32 (Math.PI / 3.0)
            let pts =
                [ for i in 0 .. 5 ->
                    let ang = float32 i * sixth
                    SKPoint(mx + r * cos ang, mz + r * sin ang) ]
            [ yield PathCommand.MoveTo(pts.[0].X, pts.[0].Y)
              for p in pts.[1..] -> PathCommand.LineTo(p.X, p.Y)
              yield PathCommand.Close ]

    // Shape primitive — returns a single path/ellipse/rect element covering
    // the body of the unit. The element is placed at (mx, mz) in world space.
    let bodyPrimitive
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
    let toVizX (x: float32) = x / 8.0f
    let toVizZ (z: float32) = z / 8.0f

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
        // Faction outline matches the damage-stroke width so the two read
        // as equals — a heavily damaged unit looks like its outline is
        // changing colour rather than picking up a thin red highlight.
        let outlineStrokeWidth = strokeWidth + style.HpArcWidth
        let strokePaint = Scene.stroke strokeColor outlineStrokeWidth

        // Under-construction extra marker (small dashed hint) — a short line
        // inside the shape to distinguish from an operational unit.
        let constructionHint =
            if unit'.Status.IsUnderConstruction then
                [ Scene.line (mx - r * 0.5f) mz (mx + r * 0.5f) mz strokePaint ]
            else []

        let heading =
            if Single.IsNaN unit'.HeadingRadians then 0.0f else unit'.HeadingRadians

        // Canonical (east-facing) outline of this shape. Body + outline
        // + HP stroke are all rendered from the same path and rotated
        // together so the unit's front always points along `heading`.
        let shapeCmds = shapeOutlineCommands unit'.Shape mx mz r
        let bodyFillElement = Scene.path shapeCmds fillPaint
        let bodyOutlineElement = Scene.path shapeCmds strokePaint

        // Damage indicator — red stroke along the back half of the unit's
        // outline, growing as the unit takes damage. `PathEffect.Trim`
        // keeps only the sub-segment
        // `[0.5 − 0.25·damage, 0.5 + 0.25·damage]` of the path, centered
        // on the back midpoint (fraction 0.5). At full HP `damage = 0`
        // and no stroke is emitted; at zero HP `damage = 1` and the
        // entire back half of the perimeter is red.
        let hpStrokeElement =
            if unit'.MaxHealth <= 0.0f then None
            else
                let health = clamp01 (unit'.CurrentHealth / unit'.MaxHealth)
                let damage = 1.0f - health
                if damage <= 0.0f then None
                else
                    // Damage stroke colour is pure white so it contrasts
                    // against every faction outline — dark red blends into
                    // Armada's fuchsia, orange blends with Cortex, etc.
                    // The alpha pulses with the same clock as the facing
                    // pip (1 s period); the oscillation amplitude scales
                    // with `damage`, so a lightly-grazed unit shows a
                    // steady stripe while a near-dead unit flashes hard.
                    let t =
                        float32 (DateTime.UtcNow.Ticks % 10_000_000L) / 10_000_000.0f
                    let pulse = 0.5f + 0.5f * cos (t * 2.0f * float32 Math.PI)
                    let alpha = byte (int (255.0f - 200.0f * damage * pulse))
                    let hpColor = SKColor(255uy, 255uy, 255uy, alpha)
                    let baseHp = Scene.stroke hpColor outlineStrokeWidth
                    let hpPaint =
                        { baseHp with
                            PathEffect =
                                Some (PathEffect.Trim(
                                        0.5f - 0.25f * damage,
                                        0.5f + 0.25f * damage,
                                        TrimMode.Normal))
                            StrokeCap = StrokeCap.Round }
                    Some (Scene.path shapeCmds hpPaint)

        // Group body, outline and HP stroke under a single rotation around
        // (mx, mz) so all three stay aligned with heading.
        let headingDeg = heading * 180.0f / float32 Math.PI
        let shapeChildren =
            [ yield bodyFillElement
              yield bodyOutlineElement
              match hpStrokeElement with
              | Some e -> yield e
              | None -> () ]
        let rotatedShape =
            Scene.rotate headingDeg mx mz shapeChildren

        // Facing pip — feature 038 US4: rendered as a triangle whose
        // apex points in the unit's current facing direction. Alliance
        // (team) coloured, pulsing, drawn outside the shape in the
        // direction of travel. Suppressed for non-rotating structures
        // per FR-010; they have no meaningful facing and a persistent
        // "east-pointing" arrow would mislead. Static previews pass
        // `HeadingRadians = 0.0f` and get an east-pointing triangle —
        // the renderer's canonical shape convention is "east-facing",
        // so the pip direction is visually consistent with the shape
        // outline (FR-010a: "remains visually equivalent to live glyphs").
        let pip =
            match unit'.Shape with
            | MovementShape.Building -> []
            | _ ->
                let pipR = style.FacingPipRadius
                // Triangle dimensions (in shape-local elmos).
                // Apex sits further out than the old ellipse centre —
                // the old pip was at `r + pipR * 2` with radius `pipR`,
                // giving it a diameter `2 * pipR` on either side of its
                // centre. The triangle tapers to its apex, so we can
                // position the apex at roughly the same outer edge while
                // keeping the base at `r + pipR * 0.5f` — this keeps
                // visual density comparable to the ellipse version.
                let baseOffset = r + pipR * 0.5f
                let apexOffset = r + pipR * 2.5f
                let halfBase = pipR * 1.0f
                // Direction unit vector toward the unit's front.
                let cosH = cos heading
                let sinH = sin heading
                // Apex point (forward along heading).
                let apexX = mx + apexOffset * cosH
                let apexY = mz + apexOffset * sinH
                // Base midpoint and perpendicular offset. The perpendicular
                // to (cosH, sinH) is (-sinH, cosH).
                let baseMidX = mx + baseOffset * cosH
                let baseMidY = mz + baseOffset * sinH
                let leftX = baseMidX + halfBase * (-sinH)
                let leftY = baseMidY + halfBase * cosH
                let rightX = baseMidX - halfBase * (-sinH)
                let rightY = baseMidY - halfBase * cosH
                let t =
                    float32 (DateTime.UtcNow.Ticks % 10_000_000L) / 10_000_000.0f
                let pulse = 0.5f + 0.5f * cos (t * 2.0f * float32 Math.PI)
                let alpha = byte (int (110.0f + 145.0f * pulse))
                let allianceColor = teamFillColor style unit'.TeamId
                let pipPaint =
                    Scene.fill (
                        SKColor(allianceColor.Red, allianceColor.Green, allianceColor.Blue, alpha))
                let cmds =
                    [ PathCommand.MoveTo(apexX, apexY)
                      PathCommand.LineTo(leftX, leftY)
                      PathCommand.LineTo(rightX, rightY)
                      PathCommand.Close ]
                [ Scene.path cmds pipPaint ]

        // The red damage stroke now doubles as the low-HP alert, so the
        // redundant body-tint overlay from the earlier design is dropped.
        let lowHpTint : Element list = []

        // Label text centered inside the shape. Fallback via UnitLabels
        // lookup when `LabelCode` was not populated by the data source.
        // Colour is picked for contrast against the unit's fill: black on
        // light fills, white on dark fills.
        let labelCode =
            if String.IsNullOrEmpty unit'.LabelCode
            then UnitLabels.lookupOrFallback unit'.InternalName
            else unit'.LabelCode
        let labelElement =
            let luminance =
                (0.299f * float32 effectiveFill.Red
                 + 0.587f * float32 effectiveFill.Green
                 + 0.114f * float32 effectiveFill.Blue) / 255.0f
            let labelColor =
                if luminance > 0.5f then SKColor(0uy, 0uy, 0uy)
                else SKColor(255uy, 255uy, 255uy)
            let txtPaint = Scene.fill labelColor
            let approxCharWidth = style.LabelFontSizePx * 0.55f
            let textWidth = float32 (String.length labelCode) * approxCharWidth
            Scene.text labelCode (mx - textWidth * 0.5f) (mz + style.LabelFontSizePx * 0.35f)
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

        [ yield rotatedShape
          yield! lowHpTint
          yield! pip
          yield! constructionHint
          yield labelElement
          yield! effectElements ]

    // --- T044 (forward-declared for US2): overlay layer ---------------------

    let orderKindColor (k: OrderKind) : SKColor =
        match k with
        | OrderKind.Move -> SKColor( 80uy, 220uy, 255uy)
        | OrderKind.Attack -> SKColor(255uy,  80uy,  80uy)
        | OrderKind.Patrol -> SKColor(255uy, 220uy,  80uy)
        | OrderKind.Guard -> SKColor( 80uy, 220uy, 120uy)
        | OrderKind.Build -> SKColor( 80uy, 120uy, 255uy)
        | OrderKind.Reclaim -> SKColor(220uy,  80uy, 220uy)
        | OrderKind.Other -> SKColor(200uy, 200uy, 200uy)

    let weaponRangeElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
        if List.isEmpty u.WeaponRangesElmo then []
        else
            let mx = toVizX u.PositionX
            let mz = toVizZ u.PositionZ
            let color = applyAlpha (factionStrokeColor style u.Faction) 0.6f
            let paint = Scene.stroke color 1.0f
            u.WeaponRangesElmo
            |> List.map (fun range -> Scene.ellipse mx mz (range / 8.0f) (range / 8.0f) paint)

    let sightRangeElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
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

    let commandQueueElements (u: UnitDisplay) : Element list =
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

    let fullNameElements (style: UnitGlyphStyle) (u: UnitDisplay) : Element list =
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

    let isExpired (nowMs: int) (e: EventEffect) =
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
