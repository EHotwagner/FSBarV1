namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Viz

module EncyclopediaTab =

    type UnitEntry = {
        DefId: int
        InternalName: string
        DisplayName: string
        Subfolder: string
        Faction: FactionId
        Tier: Tier
        Shape: MovementShape
        MetalCost: int
        EnergyCost: int
        Health: int
        BuildTime: int
        SightRangeElmo: float32
        WeaponRangesElmo: float32 list
        FootprintX: int
        FootprintZ: int
    }

    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        | ToggleFaction of faction: FactionId
        | SelectUnit of defId: int
        | ScrollList of offset: float32

    type EncyclopediaTabState = {
        Entries: UnitEntry list
        FactionFilter: Set<FactionId>
        Selected: int option
        ListScroll: float32
    }

    // --- Helpers --------------------------------------------------------

    let private concreteFloat (v: BarData.ValueOrExpr<float>) (fallback: float) : float =
        match v with
        | BarData.ValueOrExpr.Concrete x -> x
        | _ -> fallback

    let private buildEntry
            (idx: int)
            (d: BarData.UnitDef)
            : UnitEntry =
        // Same classification heuristics the live viewer uses —
        // ensures Encyclopedia glyphs byte-match session glyphs
        // (SC-003). `ignore` discards classification-miss logs.
        let canMove =
            match d.movement with
            | Some m -> m.canFly || (m.movementClass <> None)
            | None -> false
        let canFly =
            match d.movement with
            | Some m -> m.canFly
            | None -> false
        let mClass =
            match d.movement with
            | Some m -> m.movementClass
            | None -> None
        let shape = UnitGlyph.classifyShape canMove canFly mClass ignore
        let faction = UnitGlyph.classifyFaction d.subfolder d.name ignore
        let tier = UnitGlyph.classifyTier d.customParams d.category ignore
        let weaponRanges =
            match d.weapons with
            | Some ws ->
                ws
                |> List.choose (fun w ->
                    match w.range with
                    | Some v -> Some (float32 (concreteFloat v 0.0))
                    | None -> None)
                |> List.filter (fun r -> r > 0.0f)
            | None -> []
        let metalCost = int (concreteFloat d.metalCost 0.0)
        let energyCost = int (concreteFloat d.energyCost 0.0)
        let health = int (concreteFloat d.health 0.0)
        let buildTime = int (concreteFloat d.buildTime 0.0)
        let sightRange = float32 (concreteFloat d.sightDistance 0.0)
        { DefId = idx
          InternalName = d.name
          DisplayName = d.name
          Subfolder = d.subfolder
          Faction = faction
          Tier = tier
          Shape = shape
          MetalCost = metalCost
          EnergyCost = energyCost
          Health = health
          BuildTime = buildTime
          SightRangeElmo = sightRange
          WeaponRangesElmo = weaponRanges
          FootprintX = max 1 (int d.footprintX)
          FootprintZ = max 1 (int d.footprintZ) }

    let init () : EncyclopediaTabState =
        let entries =
            BarData.AllUnitDefs.all
            |> List.mapi (fun i (_, _, d) -> buildEntry i d)
            |> List.sortBy (fun e -> e.InternalName)
        { Entries = entries
          FactionFilter = Set.empty
          Selected = None
          ListScroll = 0.0f }

    // --- Paints --------------------------------------------------------

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private bodyText = Scene.fill (SKColor(0xf3uy, 0xf5uy, 0xfauy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private accentText = Scene.fill (SKColor(0x7auy, 0x9fuy, 0xd5uy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x10uy, 0x14uy, 0x1cuy, 0xffuy))
    let private rowBg = Scene.fill (SKColor(0x16uy, 0x1buy, 0x26uy, 0xffuy))
    let private rowActiveBg = Scene.fill (SKColor(0x2buy, 0x38uy, 0x52uy, 0xffuy))
    let private chipInactive = Scene.fill (SKColor(0x21uy, 0x29uy, 0x38uy, 0xffuy))
    let private chipActive = Scene.fill (SKColor(0x7auy, 0x9fuy, 0xd5uy, 0xffuy))

    // --- Layout --------------------------------------------------------

    let private rowHeight : float32 = 22.0f
    let private chipHeight : float32 = 22.0f
    let private chipSpacing : float32 = 6.0f
    let private chipsY (contentY: float32) = contentY + 46.0f

    let private factionChipOrder =
        [ FactionId.Armada; FactionId.Cortex; FactionId.Legion
          FactionId.Raptors; FactionId.Scavengers; FactionId.Neutral ]

    let private chipLabel (f: FactionId) =
        match f with
        | FactionId.Armada -> "Armada"
        | FactionId.Cortex -> "Cortex"
        | FactionId.Legion -> "Legion"
        | FactionId.Raptors -> "Raptors"
        | FactionId.Scavengers -> "Scavs"
        | FactionId.Neutral -> "Neutral"

    let private chipRects (contentX: float32) (contentY: float32) : (FactionId * (float32 * float32 * float32 * float32)) list =
        let y = chipsY contentY
        let mutable x = contentX + 8.0f
        factionChipOrder
        |> List.map (fun f ->
            let w =
                match f with
                | FactionId.Armada | FactionId.Cortex | FactionId.Legion -> 58.0f
                | FactionId.Raptors -> 60.0f
                | FactionId.Scavengers -> 50.0f
                | FactionId.Neutral -> 60.0f
            let rect = (x, y, w, chipHeight)
            x <- x + w + chipSpacing
            f, rect)

    let private listPanelRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let y = chipsY contentY + chipHeight + 14.0f
        let w = contentW * 0.40f
        let h = contentH - (y - contentY) - 16.0f
        contentX + 8.0f, y, w, h

    let private detailPanelRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let (lx, ly, lw, lh) = listPanelRect contentX contentY contentW contentH
        let x = lx + lw + 16.0f
        let w = contentW - (x - contentX) - 16.0f
        x, ly, w, lh

    // --- Filter + lookup helpers ---------------------------------------

    let private isFactionVisible (state: EncyclopediaTabState) (f: FactionId) =
        Set.isEmpty state.FactionFilter || Set.contains f state.FactionFilter

    let private visibleEntries (state: EncyclopediaTabState) : UnitEntry list =
        state.Entries
        |> List.filter (fun e -> isFactionVisible state e.Faction)

    let private findEntry (state: EncyclopediaTabState) (defId: int) : UnitEntry option =
        state.Entries |> List.tryFind (fun e -> e.DefId = defId)

    // --- Render --------------------------------------------------------

    let private renderChips
            (state: EncyclopediaTabState) (contentX: float32) (contentY: float32)
            : Element list =
        let chipEls =
            chipRects contentX contentY
            |> List.collect (fun (f, (x, y, w, h)) ->
                let active = Set.contains f state.FactionFilter
                let bg = if active then chipActive else chipInactive
                let paint = if active then headingText else bodyText
                [ Scene.rect x y w h bg
                  Scene.text (chipLabel f) (x + 6.0f) (y + h * 0.68f) 13.0f paint ])
        let filterHint =
            if Set.isEmpty state.FactionFilter then "(showing all factions)"
            else sprintf "(filter: %s)"
                    (state.FactionFilter |> Set.toList |> List.map chipLabel |> String.concat ", ")
        chipEls @
        [ Scene.text filterHint
              (contentX + 410.0f) (chipsY contentY + chipHeight * 0.68f)
              13.0f dimText ]

    let private renderList
            (state: EncyclopediaTabState)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let (x, y, w, h) = listPanelRect contentX contentY contentW contentH
        let visible = visibleEntries state
        let firstIdx = int (state.ListScroll / rowHeight)
        let visibleRows = int (h / rowHeight) + 2
        [ yield Scene.rect x y w h panelBg
          yield Scene.text (sprintf "%d units" visible.Length) (x + 8.0f) (y - 6.0f) 14.0f dimText
          for i in firstIdx .. min (visible.Length - 1) (firstIdx + visibleRows) do
              let e = visible.[i]
              let rowY = y + float32 (i - firstIdx) * rowHeight - (state.ListScroll - float32 firstIdx * rowHeight)
              if rowY + rowHeight < y || rowY > y + h then () else
              let isSelected = state.Selected = Some e.DefId
              let bg = if isSelected then rowActiveBg else rowBg
              yield Scene.rect (x + 4.0f) (rowY + 2.0f) (w - 8.0f) (rowHeight - 3.0f) bg
              let label =
                  sprintf "%s   %A %A   %dm %de"
                      e.InternalName e.Faction e.Tier e.MetalCost e.EnergyCost
              let paint = if isSelected then headingText else bodyText
              yield Scene.text label (x + 14.0f) (rowY + rowHeight - 8.0f) 13.0f paint ]

    let private renderDetail
            (state: EncyclopediaTabState)
            (style: UnitGlyphStyle)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let (x, y, w, h) = detailPanelRect contentX contentY contentW contentH
        let header =
            [ Scene.rect x y w h panelBg
              Scene.text "Detail" (x + 8.0f) (y - 6.0f) 14.0f dimText ]
        match state.Selected |> Option.bind (findEntry state) with
        | None ->
            header @
            [ Scene.text "Select a unit on the left to see its details + glyph."
                (x + 14.0f) (y + 32.0f) 14.0f dimText ]
        | Some e ->
            let lines =
                [ sprintf "%s  (%A %A %A)" e.InternalName e.Faction e.Tier e.Shape
                  sprintf "subfolder: %s" e.Subfolder
                  sprintf "cost: %d metal · %d energy · %d buildTime" e.MetalCost e.EnergyCost e.BuildTime
                  sprintf "health: %d" e.Health
                  sprintf "footprint: %dx%d elmos" e.FootprintX e.FootprintZ
                  sprintf "sight range: %.0f elmos" e.SightRangeElmo
                  sprintf "weapon ranges: %s"
                      (if e.WeaponRangesElmo.IsEmpty then "(none)"
                       else e.WeaponRangesElmo
                            |> List.map (sprintf "%.0f")
                            |> String.concat ", ") ]
            let textEls =
                lines
                |> List.mapi (fun i line ->
                    let baseY = y + 32.0f + float32 i * 20.0f
                    let size = if i = 0 then 16.0f else 14.0f
                    let paint = if i = 0 then headingText else bodyText
                    Scene.text line (x + 14.0f) baseY size paint)
            // Synthesise a UnitDisplay + render the glyph with UnitGlyph.buildUnit.
            // Place it in the bottom-right of the detail panel at a comfortable size.
            let glyphCx = x + w - 120.0f
            let glyphCy = y + h - 120.0f
            let unit =
                { UnitId = 0
                  DefId = e.DefId
                  InternalName = e.InternalName
                  Shape = e.Shape
                  Faction = e.Faction
                  Tier = e.Tier
                  LabelCode = UnitLabels.lookupOrFallback e.InternalName
                  FootprintWidthElmo = float32 e.FootprintX * 16.0f
                  FootprintHeightElmo = float32 e.FootprintZ * 16.0f
                  TeamId = 0
                  PositionX = glyphCx * 8.0f
                  PositionY = 0.0f
                  PositionZ = glyphCy * 8.0f
                  HeadingRadians = 0.0f
                  CurrentHealth = float32 e.Health
                  MaxHealth = float32 e.Health
                  BuildProgress = 1.0f
                  Status =
                    { IsUnderConstruction = false
                      IsStunned = false
                      JustDamagedWithinMs = None
                      JustCompletedWithinMs = None
                      IsCloaked = false }
                  WeaponRangesElmo = e.WeaponRangesElmo
                  SightRangeElmo = e.SightRangeElmo
                  BuildRangeElmo = None
                  CommandQueue = [] }
            let glyphEls = UnitGlyph.buildUnit unit style []
            let glyphHint =
                Scene.text "↑ glyph (same renderer as Viewer tab)"
                    (glyphCx - 40.0f) (glyphCy + 60.0f) 12.0f dimText
            header @ textEls @ glyphEls @ [ glyphHint ]

    let render
            (state: EncyclopediaTabState)
            (style: UnitGlyphStyle)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let header =
            [ Scene.text "Units — BarData encyclopedia" (contentX + 8.0f) (contentY + 22.0f) 20.0f headingText
              Scene.text
                (sprintf "%d units total · click a faction chip to filter · click a row to see the glyph"
                    state.Entries.Length)
                (contentX + 8.0f) (contentY + 42.0f) 14.0f dimText ]
        header
        @ renderChips state contentX contentY
        @ renderList state contentX contentY contentW contentH
        @ renderDetail state style contentX contentY contentW contentH

    // --- Input ---------------------------------------------------------

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let handleMouse
            (state: EncyclopediaTabState)
            (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : EncyclopediaTabAction option =
        // Chip row?
        let chipHit =
            chipRects contentX contentY
            |> List.tryFind (fun (_, rect) -> hit rect x y)
        match chipHit with
        | Some (f, _) -> Some (EncyclopediaTabAction.ToggleFaction f)
        | None ->
            let listR = listPanelRect contentX contentY contentW contentH
            if hit listR x y then
                let (lx, ly, _, _) = listR
                let firstIdx = int (state.ListScroll / rowHeight)
                let localY = y - ly + (state.ListScroll - float32 firstIdx * rowHeight)
                let rowIdx = firstIdx + int (localY / rowHeight)
                let visible = visibleEntries state
                if rowIdx < 0 || rowIdx >= visible.Length then None
                else Some (EncyclopediaTabAction.SelectUnit visible.[rowIdx].DefId)
            else None

    let handleScroll
            (state: EncyclopediaTabState)
            (delta: float32) (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : EncyclopediaTabAction option =
        let listR = listPanelRect contentX contentY contentW contentH
        if hit listR x y then
            let visible = visibleEntries state
            let totalH = float32 visible.Length * rowHeight
            let (_, _, _, lh) = listR
            let maxScroll = max 0.0f (totalH - lh)
            let next = state.ListScroll - delta * rowHeight * 3.0f
            let clamped = max 0.0f (min maxScroll next)
            Some (EncyclopediaTabAction.ScrollList clamped)
        else None
