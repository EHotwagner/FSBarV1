namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open Silk.NET.Input
open FSBar.Viz
open FSBar.Hub

module EncyclopediaTab =

    type UnitEntry = EncyclopediaData.EncyclopediaEntry

    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        | ScrollList of offset: float32

    type EncyclopediaTabState = {
        Entries: UnitEntry list
        ListScroll: float32
        SearchFocused: bool
    }

    let init () : EncyclopediaTabState =
        { Entries = EncyclopediaData.buildFromBarData ()
          ListScroll = 0.0f
          SearchFocused = false }

    // --- Key <-> char mapping ------------------------------------------

    let private keyToChar (key: Key) : char option =
        match key with
        | Key.A -> Some 'a' | Key.B -> Some 'b' | Key.C -> Some 'c'
        | Key.D -> Some 'd' | Key.E -> Some 'e' | Key.F -> Some 'f'
        | Key.G -> Some 'g' | Key.H -> Some 'h' | Key.I -> Some 'i'
        | Key.J -> Some 'j' | Key.K -> Some 'k' | Key.L -> Some 'l'
        | Key.M -> Some 'm' | Key.N -> Some 'n' | Key.O -> Some 'o'
        | Key.P -> Some 'p' | Key.Q -> Some 'q' | Key.R -> Some 'r'
        | Key.S -> Some 's' | Key.T -> Some 't' | Key.U -> Some 'u'
        | Key.V -> Some 'v' | Key.W -> Some 'w' | Key.X -> Some 'x'
        | Key.Y -> Some 'y' | Key.Z -> Some 'z'
        | Key.Number0 -> Some '0' | Key.Number1 -> Some '1'
        | Key.Number2 -> Some '2' | Key.Number3 -> Some '3'
        | Key.Number4 -> Some '4' | Key.Number5 -> Some '5'
        | Key.Number6 -> Some '6' | Key.Number7 -> Some '7'
        | Key.Number8 -> Some '8' | Key.Number9 -> Some '9'
        | Key.Space -> Some ' '
        | Key.Minus -> Some '-'
        | _ -> None

    // --- Paints --------------------------------------------------------

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private bodyText = Scene.fill (SKColor(0xf3uy, 0xf5uy, 0xfauy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x10uy, 0x14uy, 0x1cuy, 0xffuy))
    let private rowBg = Scene.fill (SKColor(0x16uy, 0x1buy, 0x26uy, 0xffuy))
    let private rowActiveBg = Scene.fill (SKColor(0x2buy, 0x38uy, 0x52uy, 0xffuy))
    let private chipInactive = Scene.fill (SKColor(0x21uy, 0x29uy, 0x38uy, 0xffuy))
    let private chipActive = Scene.fill (SKColor(0x7auy, 0x9fuy, 0xd5uy, 0xffuy))
    let private searchBg = Scene.fill (SKColor(0x1auy, 0x20uy, 0x2euy, 0xffuy))
    let private searchBgFocused = Scene.fill (SKColor(0x24uy, 0x2euy, 0x42uy, 0xffuy))
    let private clearBtnBg = Scene.fill (SKColor(0x3auy, 0x1fuy, 0x28uy, 0xffuy))

    // --- Layout --------------------------------------------------------

    let private rowHeight : float32 = 22.0f
    let private chipHeight : float32 = 22.0f
    let private chipSpacing : float32 = 6.0f
    let private chipRowGap : float32 = 6.0f

    let private factionChipRowY (contentY: float32) = contentY + 46.0f
    let private tierChipRowY    (contentY: float32) = factionChipRowY contentY + chipHeight + chipRowGap
    let private mobilityChipRowY (contentY: float32) = tierChipRowY contentY + chipHeight + chipRowGap
    let private searchRowY       (contentY: float32) = mobilityChipRowY contentY + chipHeight + chipRowGap + 4.0f
    let private searchHeight : float32 = 24.0f

    let private chipsRegionHeight () = 3.0f * chipHeight + 2.0f * chipRowGap
    let private totalFilterBarHeight () = chipsRegionHeight () + chipRowGap + 4.0f + searchHeight + 12.0f

    let private factionChipOrder =
        [ FactionFilterKey.Armada; FactionFilterKey.Cortex; FactionFilterKey.Legion
          FactionFilterKey.Raptors; FactionFilterKey.Scavengers; FactionFilterKey.Neutral ]

    let private factionLabel (f: FactionFilterKey) =
        match f with
        | FactionFilterKey.Armada -> "Armada"
        | FactionFilterKey.Cortex -> "Cortex"
        | FactionFilterKey.Legion -> "Legion"
        | FactionFilterKey.Raptors -> "Raptors"
        | FactionFilterKey.Scavengers -> "Scavs"
        | FactionFilterKey.Neutral -> "Neutral"

    let private tierChipOrder =
        [ TierFilterKey.T1; TierFilterKey.T2; TierFilterKey.T3; TierFilterKey.Commander ]

    let private tierLabel (t: TierFilterKey) =
        match t with
        | TierFilterKey.T1 -> "T1"
        | TierFilterKey.T2 -> "T2"
        | TierFilterKey.T3 -> "T3"
        | TierFilterKey.Commander -> "Com"

    let private mobilityChipOrder =
        [ MobilityFilterKey.Building; MobilityFilterKey.Ground; MobilityFilterKey.Hover
          MobilityFilterKey.Ship; MobilityFilterKey.Air; MobilityFilterKey.Amphib ]

    let private mobilityLabel (m: MobilityFilterKey) =
        match m with
        | MobilityFilterKey.Building -> "Bldg"
        | MobilityFilterKey.Ground -> "Ground"
        | MobilityFilterKey.Hover -> "Hover"
        | MobilityFilterKey.Ship -> "Ship"
        | MobilityFilterKey.Air -> "Air"
        | MobilityFilterKey.Amphib -> "Amphib"

    type private ChipRect<'K> = { Key: 'K; X: float32; Y: float32; W: float32; H: float32 }

    let private layOutChips (items: ('K * string) list) (baseX: float32) (y: float32) =
        let mutable x = baseX
        items
        |> List.map (fun (k, label) ->
            let pad = 14.0f
            let w = max 42.0f (float32 label.Length * 8.5f + pad)
            let rect = { Key = k; X = x; Y = y; W = w; H = chipHeight }
            x <- x + w + chipSpacing
            rect)

    let private factionChipRects (contentX: float32) (contentY: float32) =
        layOutChips
            (factionChipOrder |> List.map (fun k -> k, factionLabel k))
            (contentX + 8.0f) (factionChipRowY contentY)

    let private tierChipRects (contentX: float32) (contentY: float32) =
        layOutChips
            (tierChipOrder |> List.map (fun k -> k, tierLabel k))
            (contentX + 8.0f) (tierChipRowY contentY)

    let private mobilityChipRects (contentX: float32) (contentY: float32) =
        layOutChips
            (mobilityChipOrder |> List.map (fun k -> k, mobilityLabel k))
            (contentX + 8.0f) (mobilityChipRowY contentY)

    let private clearBtnRect (contentX: float32) (contentW: float32) (contentY: float32) =
        let y = factionChipRowY contentY
        let w = 110.0f
        let x = contentX + contentW - w - 12.0f
        x, y, w, chipHeight

    let private searchRect (contentX: float32) (contentW: float32) (contentY: float32) =
        let x = contentX + 8.0f
        let y = searchRowY contentY
        let w = contentW * 0.45f
        x, y, w, searchHeight

    let private listPanelRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let y = contentY + 46.0f + totalFilterBarHeight ()
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

    // --- Filter helpers ------------------------------------------------

    let private findEntry (state: EncyclopediaTabState) (defId: int) : UnitEntry option =
        state.Entries |> List.tryFind (fun e -> e.DefId = defId)

    /// Reconcile `SelectedDefId` after a filter change (FR-011):
    /// keep the selection if it still matches, otherwise pin the
    /// first visible entry, or `None` if the filter is empty.
    let private reconcileSelection
            (state: EncyclopediaTabState)
            (selection: EncyclopediaSelection)
            : EncyclopediaSelection =
        let visible = EncyclopediaFilter.apply selection state.Entries
        match selection.SelectedDefId with
        | Some id when visible |> List.exists (fun e -> e.DefId = id) -> selection
        | _ ->
            let newSel =
                visible |> List.tryHead |> Option.map (fun e -> e.DefId)
            { selection with SelectedDefId = newSel }

    let private submit (store: HubStateStore.T) (state: EncyclopediaTabState) (selection: EncyclopediaSelection) =
        let reconciled = reconcileSelection state selection
        HubStateStore.setEncyclopedia store reconciled |> ignore

    // --- Render --------------------------------------------------------

    let private renderChipRow
            (rects: ChipRect<'K> list)
            (active: 'K -> bool)
            (label: 'K -> string)
            : Element list =
        rects
        |> List.collect (fun r ->
            let isActive = active r.Key
            let bg = if isActive then chipActive else chipInactive
            let paint = if isActive then headingText else bodyText
            [ Scene.rect r.X r.Y r.W r.H bg
              Scene.text (label r.Key) (r.X + 8.0f) (r.Y + r.H * 0.68f) 13.0f paint ])

    let private renderFilterBar
            (sel: EncyclopediaSelection)
            (searchFocused: bool)
            (contentX: float32) (contentY: float32) (contentW: float32)
            : Element list =
        let fRects = factionChipRects contentX contentY
        let tRects = tierChipRects contentX contentY
        let mRects = mobilityChipRects contentX contentY
        let (cbx, cby, cbw, cbh) = clearBtnRect contentX contentW contentY
        let (sx, sy, sw, sh) = searchRect contentX contentW contentY
        let factionActive k = Set.contains k sel.FactionFilter
        let tierActive k = Set.contains k sel.TierFilter
        let mobilityActive k = Set.contains k sel.MobilityFilter
        let rowLabel (text: string) (y: float32) =
            Scene.text text (contentX + 8.0f) (y - 4.0f) 12.0f dimText
        let searchBgPaint = if searchFocused then searchBgFocused else searchBg
        let searchPlaceholder =
            if sel.SearchText = "" && not searchFocused then "search…"
            elif sel.SearchText = "" && searchFocused then "type to search · Esc to clear"
            else sel.SearchText
        let caret =
            if searchFocused then
                let tw = float32 sel.SearchText.Length * 7.5f
                [ Scene.rect (sx + 8.0f + tw) (sy + 5.0f) 1.5f (sh - 10.0f) headingText ]
            else []
        [ rowLabel "Faction" (factionChipRowY contentY) ]
        @ renderChipRow fRects factionActive factionLabel
        @ [ Scene.rect cbx cby cbw cbh clearBtnBg
            Scene.text "Clear filters" (cbx + 8.0f) (cby + cbh * 0.68f) 12.0f bodyText ]
        @ [ rowLabel "Tier" (tierChipRowY contentY) ]
        @ renderChipRow tRects tierActive tierLabel
        @ [ rowLabel "Mobility" (mobilityChipRowY contentY) ]
        @ renderChipRow mRects mobilityActive mobilityLabel
        @ [ Scene.rect sx sy sw sh searchBgPaint
            Scene.text searchPlaceholder (sx + 8.0f) (sy + sh * 0.68f) 13.0f bodyText ]
        @ caret

    let private renderList
            (state: EncyclopediaTabState)
            (selection: EncyclopediaSelection)
            (visible: UnitEntry list)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let (x, y, w, h) = listPanelRect contentX contentY contentW contentH
        let selected = selection.SelectedDefId
        let totalEntries = state.Entries.Length
        let countLabel = sprintf "%d of %d units shown" visible.Length totalEntries
        let firstIdx = int (state.ListScroll / rowHeight)
        let visibleRows = int (h / rowHeight) + 2
        [ yield Scene.rect x y w h panelBg
          yield Scene.text countLabel (x + 8.0f) (y - 6.0f) 14.0f dimText
          if visible.IsEmpty then
              yield Scene.text "No units match the active filters."
                  (x + 14.0f) (y + 32.0f) 14.0f dimText
              yield Scene.text "→ use the 'Clear filters' button above."
                  (x + 14.0f) (y + 52.0f) 13.0f dimText
          else
              for i in firstIdx .. min (visible.Length - 1) (firstIdx + visibleRows) do
                  let e = visible.[i]
                  let rowY = y + float32 (i - firstIdx) * rowHeight - (state.ListScroll - float32 firstIdx * rowHeight)
                  if rowY + rowHeight < y || rowY > y + h then () else
                  let isSelected = selected = Some e.DefId
                  let bg = if isSelected then rowActiveBg else rowBg
                  yield Scene.rect (x + 4.0f) (rowY + 2.0f) (w - 8.0f) (rowHeight - 3.0f) bg
                  let display =
                      match e.HumanName with
                      | Some n when n <> "" -> sprintf "%s  (%s)" n e.InternalName
                      | _ -> e.InternalName
                  let label =
                      sprintf "%s   %A %A   %dm %de"
                          display e.Faction e.Tier e.MetalCost e.EnergyCost
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
            let title =
                match e.HumanName with
                | Some n when n <> "" -> sprintf "%s  [%s]" n e.InternalName
                | _ -> e.InternalName
            let lines =
                [ sprintf "%s  (%A %A %A)" title e.Faction e.Tier e.Shape
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
            let targetRadius = 48.0f
            let scale = targetRadius / style.MinPixelRadius
            let encyclopediaStyle =
                { style with
                    MinPixelRadius = targetRadius
                    T1StrokeWidth = style.T1StrokeWidth * scale
                    T2StrokeWidth = style.T2StrokeWidth * scale
                    T3StrokeWidth = style.T3StrokeWidth * scale
                    FacingPipRadius = style.FacingPipRadius * scale * 0.4f
                    HpArcWidth = style.HpArcWidth * scale
                    LabelFontSizePx = style.LabelFontSizePx * scale
                    LabelLegibilityZoomThreshold = 0.0f }
            let glyphCx = x + w - 160.0f
            let glyphCy = y + h - 120.0f
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
        let sel = snap.Encyclopedia
        let style = snap.VizConfig.GlyphStyle
        let visible = EncyclopediaFilter.apply sel state.Entries
        let header =
            [ Scene.text "Units — BarData encyclopedia" (contentX + 8.0f) (contentY + 22.0f) 20.0f headingText
              Scene.text
                (sprintf "%d units total · click chips to filter · type to search · click a row to see the glyph"
                    state.Entries.Length)
                (contentX + 8.0f) (contentY + 42.0f) 14.0f dimText ]
        header
        @ renderFilterBar sel state.SearchFocused contentX contentY contentW
        @ renderList state sel visible contentX contentY contentW contentH
        @ renderDetail state sel.SelectedDefId style contentX contentY contentW contentH

    // --- Input ---------------------------------------------------------

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let private hitChip (r: ChipRect<'K>) (x: float32) (y: float32) =
        hit (r.X, r.Y, r.W, r.H) x y

    let private toggleIn (set: Set<'K>) (k: 'K) : Set<'K> =
        if Set.contains k set then Set.remove k set else Set.add k set

    let handleMouse
            (state: EncyclopediaTabState)
            (store: HubStateStore.T)
            (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : EncyclopediaTabState * EncyclopediaTabAction option =
        let snap = HubStateStore.current store
        let sel = snap.Encyclopedia

        // Faction chip?
        let fHit = factionChipRects contentX contentY |> List.tryFind (fun r -> hitChip r x y)
        match fHit with
        | Some r ->
            let updated = { sel with FactionFilter = toggleIn sel.FactionFilter r.Key }
            submit store state updated
            { state with SearchFocused = false }, None
        | None ->
        // Tier chip?
        let tHit = tierChipRects contentX contentY |> List.tryFind (fun r -> hitChip r x y)
        match tHit with
        | Some r ->
            let updated = { sel with TierFilter = toggleIn sel.TierFilter r.Key }
            submit store state updated
            { state with SearchFocused = false }, None
        | None ->
        // Mobility chip?
        let mHit = mobilityChipRects contentX contentY |> List.tryFind (fun r -> hitChip r x y)
        match mHit with
        | Some r ->
            let updated = { sel with MobilityFilter = toggleIn sel.MobilityFilter r.Key }
            submit store state updated
            { state with SearchFocused = false }, None
        | None ->
        // Clear filters?
        let cbr = clearBtnRect contentX contentW contentY
        if hit cbr x y then
            submit store state EncyclopediaFilter.defaultSelection
            { state with SearchFocused = false; ListScroll = 0.0f }, None
        else
        // Search box?
        let sr = searchRect contentX contentW contentY
        if hit sr x y then
            { state with SearchFocused = true }, None
        else
            // List row?
            let listR = listPanelRect contentX contentY contentW contentH
            if hit listR x y then
                let (lx, ly, _, _) = listR
                let firstIdx = int (state.ListScroll / rowHeight)
                let localY = y - ly + (state.ListScroll - float32 firstIdx * rowHeight)
                let rowIdx = firstIdx + int (localY / rowHeight)
                let visible = EncyclopediaFilter.apply sel state.Entries
                if rowIdx < 0 || rowIdx >= visible.Length then
                    { state with SearchFocused = false }, None
                else
                    let updated = { sel with SelectedDefId = Some visible.[rowIdx].DefId }
                    HubStateStore.setEncyclopedia store updated |> ignore
                    { state with SearchFocused = false }, None
            else
                { state with SearchFocused = false }, None

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
            let visible = EncyclopediaFilter.apply snap.Encyclopedia state.Entries
            let totalH = float32 visible.Length * rowHeight
            let (_, _, _, lh) = listR
            let maxScroll = max 0.0f (totalH - lh)
            let next = state.ListScroll - delta * rowHeight * 3.0f
            let clamped = max 0.0f (min maxScroll next)
            Some (EncyclopediaTabAction.ScrollList clamped)
        else None

    let handleKey
            (state: EncyclopediaTabState)
            (store: HubStateStore.T)
            (key: Key)
            : EncyclopediaTabState * bool =
        if not state.SearchFocused then state, false
        else
            let snap = HubStateStore.current store
            let sel = snap.Encyclopedia
            match key with
            | Key.Escape ->
                submit store state { sel with SearchText = "" }
                { state with SearchFocused = false }, true
            | Key.Backspace ->
                let t = sel.SearchText
                let next = if t.Length = 0 then "" else t.Substring(0, t.Length - 1)
                submit store state { sel with SearchText = next }
                state, true
            | Key.Enter ->
                { state with SearchFocused = false }, true
            | _ ->
                match keyToChar key with
                | Some c when sel.SearchText.Length < 128 ->
                    let next = sel.SearchText + string c
                    submit store state { sel with SearchText = next }
                    state, true
                | _ -> state, false
