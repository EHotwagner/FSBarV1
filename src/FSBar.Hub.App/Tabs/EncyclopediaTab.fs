namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Viz
open FSBar.Hub

module EncyclopediaTab =

    /// Feature 038 lifted the entry type to `FSBar.Viz.EncyclopediaData`
    /// so `UnitDisplayAdapter` can share it. Keep the local alias so
    /// external consumers (Hub scripting examples, tests) still compile.
    type UnitEntry = EncyclopediaData.EncyclopediaEntry

    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        | ScrollList of offset: float32

    type EncyclopediaTabState = {
        Entries: UnitEntry list
        ListScroll: float32
    }

    // --- Helpers --------------------------------------------------------

    let init () : EncyclopediaTabState =
        { Entries = EncyclopediaData.buildFromBarData ()
          ListScroll = 0.0f }

    // Bidirectional map between the Viz-side FactionId (used in the
    // list rendering + entry classification) and the Hub-side
    // FactionFilterKey (the wire-aligned type stored in HubState).
    let private factionIdToKey (f: FactionId) : FactionFilterKey =
        match f with
        | FactionId.Armada -> FactionFilterKey.Armada
        | FactionId.Cortex -> FactionFilterKey.Cortex
        | FactionId.Legion -> FactionFilterKey.Legion
        | FactionId.Raptors -> FactionFilterKey.Raptors
        | FactionId.Scavengers -> FactionFilterKey.Scavengers
        | FactionId.Neutral -> FactionFilterKey.Neutral

    let private factionKeyToId (k: FactionFilterKey) : FactionId =
        match k with
        | FactionFilterKey.Armada -> FactionId.Armada
        | FactionFilterKey.Cortex -> FactionId.Cortex
        | FactionFilterKey.Legion -> FactionId.Legion
        | FactionFilterKey.Raptors -> FactionId.Raptors
        | FactionFilterKey.Scavengers -> FactionId.Scavengers
        | FactionFilterKey.Neutral -> FactionId.Neutral

    let private filterFromStore (store: HubStateStore.T) : Set<FactionId> =
        (HubStateStore.current store).Encyclopedia.FactionFilter
        |> Set.map factionKeyToId

    let private selectedFromStore (store: HubStateStore.T) : int option =
        (HubStateStore.current store).Encyclopedia.SelectedDefId

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

    let private isFactionVisible (filter: Set<FactionId>) (f: FactionId) =
        Set.isEmpty filter || Set.contains f filter

    let private visibleEntries (state: EncyclopediaTabState) (filter: Set<FactionId>) : UnitEntry list =
        state.Entries
        |> List.filter (fun e -> isFactionVisible filter e.Faction)

    let private findEntry (state: EncyclopediaTabState) (defId: int) : UnitEntry option =
        state.Entries |> List.tryFind (fun e -> e.DefId = defId)

    // --- Render --------------------------------------------------------

    let private renderChips
            (filter: Set<FactionId>) (contentX: float32) (contentY: float32)
            : Element list =
        let chipEls =
            chipRects contentX contentY
            |> List.collect (fun (f, (x, y, w, h)) ->
                let active = Set.contains f filter
                let bg = if active then chipActive else chipInactive
                let paint = if active then headingText else bodyText
                [ Scene.rect x y w h bg
                  Scene.text (chipLabel f) (x + 6.0f) (y + h * 0.68f) 13.0f paint ])
        let filterHint =
            if Set.isEmpty filter then "(showing all factions)"
            else sprintf "(filter: %s)"
                    (filter |> Set.toList |> List.map chipLabel |> String.concat ", ")
        chipEls @
        [ Scene.text filterHint
              (contentX + 410.0f) (chipsY contentY + chipHeight * 0.68f)
              13.0f dimText ]

    let private renderList
            (state: EncyclopediaTabState)
            (filter: Set<FactionId>)
            (selected: int option)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let (x, y, w, h) = listPanelRect contentX contentY contentW contentH
        let visible = visibleEntries state filter
        let firstIdx = int (state.ListScroll / rowHeight)
        let visibleRows = int (h / rowHeight) + 2
        [ yield Scene.rect x y w h panelBg
          yield Scene.text (sprintf "%d units" visible.Length) (x + 8.0f) (y - 6.0f) 14.0f dimText
          for i in firstIdx .. min (visible.Length - 1) (firstIdx + visibleRows) do
              let e = visible.[i]
              let rowY = y + float32 (i - firstIdx) * rowHeight - (state.ListScroll - float32 firstIdx * rowHeight)
              if rowY + rowHeight < y || rowY > y + h then () else
              let isSelected = selected = Some e.DefId
              let bg = if isSelected then rowActiveBg else rowBg
              yield Scene.rect (x + 4.0f) (rowY + 2.0f) (w - 8.0f) (rowHeight - 3.0f) bg
              let label =
                  sprintf "%s   %A %A   %dm %de"
                      e.InternalName e.Faction e.Tier e.MetalCost e.EnergyCost
              let paint = if isSelected then headingText else bodyText
              yield Scene.text label (x + 14.0f) (rowY + rowHeight - 8.0f) 13.0f paint ]

    let private renderDetail
            (state: EncyclopediaTabState)
            (selected: int option)
            (style: UnitGlyphStyle)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let (x, y, w, h) = detailPanelRect contentX contentY contentW contentH
        let header =
            [ Scene.rect x y w h panelBg
              Scene.text "Detail" (x + 8.0f) (y - 6.0f) 14.0f dimText ]
        match selected |> Option.bind (findEntry state) with
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
            // Synthesise a UnitDisplay via the shared UnitDisplayAdapter
            // and render the glyph with UnitGlyph.buildUnit. Feature 038
            // FR-002: every Hub surface funnels through the same
            // constructor so Viewer-tab and Units-tab glyphs can't
            // diverge.
            let targetRadius = 48.0f
            let scale = targetRadius / style.MinPixelRadius
            let encyclopediaStyle =
                { style with
                    MinPixelRadius = targetRadius
                    T1StrokeWidth = style.T1StrokeWidth * scale
                    T2StrokeWidth = style.T2StrokeWidth * scale
                    T3StrokeWidth = style.T3StrokeWidth * scale
                    // Pip is offset by `r + pipR*2` from centre — full scaling
                    // would park it well outside the shape. Keep it modest.
                    FacingPipRadius = style.FacingPipRadius * scale * 0.4f
                    HpArcWidth = style.HpArcWidth * scale
                    LabelFontSizePx = style.LabelFontSizePx * scale
                    LabelLegibilityZoomThreshold = 0.0f }
            let glyphCx = x + w - 160.0f
            let glyphCy = y + h - 120.0f
            // Pin footprint to targetRadius (rawR = footprint/16) so large
            // buildings don't blow past the panel edge.
            let pinnedFootprint = targetRadius * 16.0f
            let baseDisplay = UnitDisplayAdapter.ofEncyclopediaEntry e pinnedFootprint
            let unit =
                { baseDisplay with
                    PositionX = glyphCx * 8.0f
                    PositionZ = glyphCy * 8.0f }
            let glyphEls = UnitGlyph.buildUnit unit encyclopediaStyle []
            let glyphHint =
                Scene.text "↑ same renderer as Viewer"
                    (glyphCx - 80.0f) (glyphCy + 80.0f) 12.0f dimText
            header @ textEls @ glyphEls @ [ glyphHint ]

    let render
            (state: EncyclopediaTabState)
            (store: HubStateStore.T)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let snap = HubStateStore.current store
        let filter = snap.Encyclopedia.FactionFilter |> Set.map factionKeyToId
        let selected = snap.Encyclopedia.SelectedDefId
        let style = snap.VizConfig.GlyphStyle
        let header =
            [ Scene.text "Units — BarData encyclopedia" (contentX + 8.0f) (contentY + 22.0f) 20.0f headingText
              Scene.text
                (sprintf "%d units total · click a faction chip to filter · click a row to see the glyph"
                    state.Entries.Length)
                (contentX + 8.0f) (contentY + 42.0f) 14.0f dimText ]
        header
        @ renderChips filter contentX contentY
        @ renderList state filter selected contentX contentY contentW contentH
        @ renderDetail state selected style contentX contentY contentW contentH

    // --- Input ---------------------------------------------------------

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let handleMouse
            (state: EncyclopediaTabState)
            (store: HubStateStore.T)
            (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : EncyclopediaTabAction option =
        // Chip row? Faction-filter mutations route directly through
        // HubStateStore.setEncyclopedia (FR-019); the ScrollList action
        // is the only thing that bubbles out for the entrypoint to apply.
        let chipHit =
            chipRects contentX contentY
            |> List.tryFind (fun (_, rect) -> hit rect x y)
        match chipHit with
        | Some (f, _) ->
            let snap = HubStateStore.current store
            let key = factionIdToKey f
            let nextFilter =
                if Set.contains key snap.Encyclopedia.FactionFilter then
                    Set.remove key snap.Encyclopedia.FactionFilter
                else Set.add key snap.Encyclopedia.FactionFilter
            let updated = { snap.Encyclopedia with FactionFilter = nextFilter }
            HubStateStore.setEncyclopedia store updated |> ignore
            None
        | None ->
            let listR = listPanelRect contentX contentY contentW contentH
            if hit listR x y then
                let (lx, ly, _, _) = listR
                let firstIdx = int (state.ListScroll / rowHeight)
                let localY = y - ly + (state.ListScroll - float32 firstIdx * rowHeight)
                let rowIdx = firstIdx + int (localY / rowHeight)
                let snap = HubStateStore.current store
                let filter = snap.Encyclopedia.FactionFilter |> Set.map factionKeyToId
                let visible = visibleEntries state filter
                if rowIdx < 0 || rowIdx >= visible.Length then None
                else
                    let updated =
                        { snap.Encyclopedia with SelectedDefId = Some visible.[rowIdx].DefId }
                    HubStateStore.setEncyclopedia store updated |> ignore
                    None
            else None

    let handleScroll
            (state: EncyclopediaTabState)
            (store: HubStateStore.T)
            (delta: float32) (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : EncyclopediaTabAction option =
        let listR = listPanelRect contentX contentY contentW contentH
        if hit listR x y then
            let snap = HubStateStore.current store
            let filter = snap.Encyclopedia.FactionFilter |> Set.map factionKeyToId
            let visible = visibleEntries state filter
            let totalH = float32 visible.Length * rowHeight
            let (_, _, _, lh) = listR
            let maxScroll = max 0.0f (totalH - lh)
            let next = state.ListScroll - delta * rowHeight * 3.0f
            let clamped = max 0.0f (min maxScroll next)
            Some (EncyclopediaTabAction.ScrollList clamped)
        else None
