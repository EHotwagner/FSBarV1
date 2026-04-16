namespace FSBar.Viz

open System
open SkiaSharp
open SkiaViewer

[<RequireQualifiedAccess>]
type ConfigPanelAction =
    | SavePreset of name: string
    | LoadPreset of name: string
    | DeletePreset of name: string
    | ResetDefaults

type ConfigPanelInputResult =
    { PanelState: ConfigPanelState
      UpdatedConfig: VizConfig option
      Action: ConfigPanelAction option }

module ConfigPanel =

    let panelWidth : float32 = 280.0f
    let private rowHeight : float32 = 22.0f
    let private headerHeight : float32 = 24.0f
    let private padX : float32 = 8.0f
    let private labelWidth : float32 = 140.0f

    // --- Palette of preset colors for ColorPicker cycling ---
    let private colorPalette : SKColor array =
        [|
            SKColor(255uy, 255uy, 255uy)
            SKColor(0uy,   0uy,   0uy)
            SKColor(255uy, 64uy,  64uy)
            SKColor(64uy,  255uy, 64uy)
            SKColor(64uy,  64uy,  255uy)
            SKColor(255uy, 220uy, 32uy)
            SKColor(255uy, 120uy, 32uy)
            SKColor(176uy, 64uy,  255uy)
            SKColor(64uy,  220uy, 220uy)
            SKColor(255uy, 64uy,  176uy)
            SKColor(128uy, 128uy, 128uy)
            SKColor(60uy,  60uy,  60uy)
            SKColor(200uy, 200uy, 200uy)
        |]

    let private nextColorInPalette (cur: SKColor) : SKColor =
        let idx =
            colorPalette
            |> Array.tryFindIndex (fun c ->
                c.Red = cur.Red && c.Green = cur.Green && c.Blue = cur.Blue)
        match idx with
        | Some i -> colorPalette.[(i + 1) % colorPalette.Length]
        | None -> colorPalette.[0]

    let initialState : ConfigPanelState =
        { IsOpen = false
          ScrollOffset = 0.0f
          ExpandedSections =
              ConfigDescriptors.categoryOrder
              |> List.map ConfigDescriptors.categoryLabel
              |> Set.ofList
          ActiveControl = None
          DirtyIndicator = false }

    let toggle (panelState: ConfigPanelState) : ConfigPanelState =
        { panelState with IsOpen = not panelState.IsOpen; ActiveControl = None }

    let hitTest (x: float32) (y: float32) (panelState: ConfigPanelState) (windowWidth: float32) : bool =
        ignore y
        panelState.IsOpen && x >= windowWidth - panelWidth && x <= windowWidth

    // --- Virtual row layout ----------------------------------------------

    [<RequireQualifiedAccess>]
    type private RowKind =
        | Title
        | PresetHeader
        | SaveButton
        | ResetButton
        | PresetItem of name: string
        | SectionHeader of AttributeCategory
        | AttrRow of AttributeDescriptor
        | Spacer

    type private Row = { Y: float32; Height: float32; Kind: RowKind }

    let private buildRows
        (panelState: ConfigPanelState) (presetNames: string list) : Row list =
        let rows = ResizeArray()
        let mutable y = 0.0f
        let add k h =
            rows.Add({ Y = y; Height = h; Kind = k })
            y <- y + h
        add RowKind.Title headerHeight
        add RowKind.PresetHeader headerHeight
        for n in presetNames do
            add (RowKind.PresetItem n) rowHeight
        add RowKind.SaveButton rowHeight
        add RowKind.ResetButton rowHeight
        add RowKind.Spacer (rowHeight / 2.0f)
        for cat in ConfigDescriptors.categoryOrder do
            add (RowKind.SectionHeader cat) headerHeight
            if Set.contains (ConfigDescriptors.categoryLabel cat) panelState.ExpandedSections then
                ConfigDescriptors.all
                |> List.filter (fun d -> d.Category = cat)
                |> List.iter (fun d -> add (RowKind.AttrRow d) rowHeight)
        List.ofSeq rows

    let private totalContentHeight (rows: Row list) : float32 =
        match rows with
        | [] -> 0.0f
        | _ ->
            let last = List.last rows
            last.Y + last.Height

    let private clampScroll (offset: float32) (contentH: float32) (windowH: float32) : float32 =
        let maxScroll = max 0.0f (contentH - windowH)
        max 0.0f (min maxScroll offset)

    // --- Paints -----------------------------------------------------------

    let private panelBg = Scene.fill (SKColor(24uy, 24uy, 28uy, 235uy))
    let private divider = Scene.fill (SKColor(64uy, 64uy, 72uy))
    let private headerBg = Scene.fill (SKColor(40uy, 40uy, 48uy, 235uy))
    let private buttonBg = Scene.fill (SKColor(60uy, 90uy, 140uy))
    let private buttonResetBg = Scene.fill (SKColor(140uy, 60uy, 60uy))
    let private presetActiveBg = Scene.fill (SKColor(80uy, 100uy, 140uy))
    let private presetBg = Scene.fill (SKColor(48uy, 48uy, 56uy))
    let private toggleOn = Scene.fill (SKColor(80uy, 200uy, 120uy))
    let private toggleOff = Scene.fill (SKColor(70uy, 70uy, 80uy))
    let private sliderTrack = Scene.fill (SKColor(70uy, 70uy, 80uy))
    let private sliderThumb = Scene.fill (SKColor(180uy, 200uy, 230uy))
    let private textPaint = Scene.fill (SKColor(230uy, 230uy, 230uy))
    let private textDim = Scene.fill (SKColor(160uy, 160uy, 170uy))
    let private dirtyPaint = Scene.fill (SKColor(240uy, 180uy, 80uy))

    // --- Render helpers ---------------------------------------------------

    let private renderText (s: string) (x: float32) (y: float32) (size: float32) (paint: Paint) : Element =
        Scene.text s x y size paint

    let private renderRow
        (panelX: float32) (rowScreenY: float32)
        (row: Row) (config: VizConfig) (panelState: ConfigPanelState)
        (activePresetName: string option) : Element list =
        let cx = panelX + padX
        let controlX = panelX + padX + labelWidth
        let controlWidth = panelWidth - labelWidth - 2.0f * padX
        let baseline = rowScreenY + row.Height * 0.7f
        match row.Kind with
        | RowKind.Title ->
            [ Scene.rect panelX rowScreenY panelWidth row.Height headerBg
              renderText "Style Configurator" cx baseline 13.0f textPaint
              if panelState.DirtyIndicator then
                  renderText "● modified" (panelX + panelWidth - 80.0f) baseline 11.0f dirtyPaint ]
        | RowKind.PresetHeader ->
            [ Scene.rect panelX rowScreenY panelWidth row.Height headerBg
              renderText "Presets" cx baseline 12.0f textDim ]
        | RowKind.SaveButton ->
            let btnX = panelX + padX
            let btnW = panelWidth - 2.0f * padX
            [ Scene.rect btnX (rowScreenY + 2.0f) btnW (row.Height - 4.0f) buttonBg
              renderText "Save Preset (timestamp)" (btnX + 6.0f) baseline 11.0f textPaint ]
        | RowKind.ResetButton ->
            let btnX = panelX + padX
            let btnW = panelWidth - 2.0f * padX
            [ Scene.rect btnX (rowScreenY + 2.0f) btnW (row.Height - 4.0f) buttonResetBg
              renderText "Reset to Defaults" (btnX + 6.0f) baseline 11.0f textPaint ]
        | RowKind.PresetItem name ->
            let isActive = Some name = activePresetName
            let bg = if isActive then presetActiveBg else presetBg
            let itemX = panelX + padX
            let itemW = panelWidth - 2.0f * padX
            [ Scene.rect itemX (rowScreenY + 1.0f) itemW (row.Height - 2.0f) bg
              renderText name (itemX + 6.0f) baseline 11.0f textPaint ]
        | RowKind.SectionHeader cat ->
            let lbl = ConfigDescriptors.categoryLabel cat
            let expanded = Set.contains lbl panelState.ExpandedSections
            let chevron = if expanded then "▼" else "▶"
            [ Scene.rect panelX rowScreenY panelWidth row.Height headerBg
              Scene.rect panelX (rowScreenY + row.Height - 1.0f) panelWidth 1.0f divider
              renderText (sprintf "%s %s" chevron lbl) cx baseline 12.0f textPaint ]
        | RowKind.AttrRow d ->
            let labelEl = renderText d.Label cx baseline 10.5f textPaint
            let controlEls =
                match d.InputKind with
                | InputKind.Toggle ->
                    let v = unbox<bool> (d.Get config)
                    let paint = if v then toggleOn else toggleOff
                    let sz = 12.0f
                    [ Scene.rect controlX (rowScreenY + (row.Height - sz) / 2.0f) sz sz paint
                      renderText (if v then "on" else "off") (controlX + sz + 4.0f) baseline 10.0f textDim ]
                | InputKind.ColorPicker ->
                    let c = unbox<SKColor> (d.Get config)
                    let paint = Scene.fill c
                    let sz = 14.0f
                    let hex = sprintf "#%02X%02X%02X" c.Red c.Green c.Blue
                    [ Scene.rect controlX (rowScreenY + (row.Height - sz) / 2.0f) sz sz paint
                      renderText hex (controlX + sz + 6.0f) baseline 10.0f textDim ]
                | InputKind.Slider(mn, mx) ->
                    let v = unbox<float32> (d.Get config)
                    let t = if mx - mn > 0.0f then (v - mn) / (mx - mn) else 0.0f
                    let trackW = controlWidth - 40.0f
                    let trackY = rowScreenY + row.Height / 2.0f - 2.0f
                    let thumbX = controlX + (max 0.0f (min 1.0f t)) * trackW
                    [ Scene.rect controlX trackY trackW 4.0f sliderTrack
                      Scene.rect (thumbX - 3.0f) (trackY - 3.0f) 6.0f 10.0f sliderThumb
                      renderText (sprintf "%.2f" v) (controlX + trackW + 4.0f) baseline 10.0f textDim ]
                | InputKind.IntSlider(mn, mx) ->
                    let v = unbox<int> (d.Get config)
                    let range = float32 (mx - mn)
                    let t = if range > 0.0f then float32 (v - mn) / range else 0.0f
                    let trackW = controlWidth - 40.0f
                    let trackY = rowScreenY + row.Height / 2.0f - 2.0f
                    let thumbX = controlX + (max 0.0f (min 1.0f t)) * trackW
                    [ Scene.rect controlX trackY trackW 4.0f sliderTrack
                      Scene.rect (thumbX - 3.0f) (trackY - 3.0f) 6.0f 10.0f sliderThumb
                      renderText (sprintf "%d" v) (controlX + trackW + 4.0f) baseline 10.0f textDim ]
                | InputKind.EnumChoice _ ->
                    let s = unbox<string> (d.Get config)
                    [ renderText s controlX baseline 10.5f textDim ]
            labelEl :: controlEls
        | RowKind.Spacer -> []

    let buildPanel
        (config: VizConfig) (panelState: ConfigPanelState)
        (windowWidth: float32) (windowHeight: float32)
        (presetNames: string list) (activePresetName: string option) : Element list =
        if not panelState.IsOpen then []
        else
            let panelX = windowWidth - panelWidth
            let rows = buildRows panelState presetNames
            let contentH = totalContentHeight rows
            let scroll = clampScroll panelState.ScrollOffset contentH windowHeight
            let bg = Scene.rect panelX 0.0f panelWidth windowHeight panelBg
            let rowElems =
                rows
                |> List.collect (fun r ->
                    let screenY = r.Y - scroll
                    if screenY + r.Height < 0.0f || screenY > windowHeight then []
                    else renderRow panelX screenY r config panelState activePresetName)
            bg :: rowElems

    // --- Input handling --------------------------------------------------

    let private pointRowAt
        (panelState: ConfigPanelState) (presetNames: string list)
        (windowHeight: float32) (localY: float32) : Row option =
        let rows = buildRows panelState presetNames
        let contentH = totalContentHeight rows
        let scroll = clampScroll panelState.ScrollOffset contentH windowHeight
        let virtualY = localY + scroll
        rows |> List.tryFind (fun r -> virtualY >= r.Y && virtualY < r.Y + r.Height)

    let private sliderValueAtX (d: AttributeDescriptor) (panelX: float32) (x: float32) (_config: VizConfig) =
        let controlX = panelX + padX + labelWidth
        let controlWidth = panelWidth - labelWidth - 2.0f * padX
        let trackW = controlWidth - 40.0f
        let t = (x - controlX) / trackW
        let t = max 0.0f (min 1.0f t)
        match d.InputKind with
        | InputKind.Slider(mn, mx) ->
            let v = mn + t * (mx - mn)
            Some (box v)
        | InputKind.IntSlider(mn, mx) ->
            let v = mn + int (System.Math.Round(float t * float (mx - mn)))
            Some (box v)
        | _ -> None

    let private nextEnum (labels: string list) (cur: string) : string =
        match labels with
        | [] -> cur
        | _ ->
            let arr = List.toArray labels
            let idx = Array.tryFindIndex ((=) cur) arr |> Option.defaultValue -1
            arr.[(idx + 1 + arr.Length) % arr.Length]

    let handleInput
        (event: InputEvent) (config: VizConfig) (panelState: ConfigPanelState)
        (windowWidth: float32) (windowHeight: float32) : ConfigPanelInputResult =
        let noop = { PanelState = panelState; UpdatedConfig = None; Action = None }
        let panelX = windowWidth - panelWidth
        match event with
        | InputEvent.MouseScroll(delta, _x, _y) ->
            // Scroll by 3 rows per wheel click
            let rows = buildRows panelState (StylePreset.listNames())
            let contentH = totalContentHeight rows
            let step = rowHeight * 3.0f
            let newOffset =
                clampScroll (panelState.ScrollOffset - delta * step) contentH windowHeight
            { noop with PanelState = { panelState with ScrollOffset = newOffset } }
        | InputEvent.MouseDown(_, x, y) ->
            let presetNames = StylePreset.listNames()
            match pointRowAt panelState presetNames windowHeight y with
            | None -> noop
            | Some row ->
                match row.Kind with
                | RowKind.SaveButton ->
                    let ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                    let name = sprintf "preset-%s" ts
                    { noop with Action = Some (ConfigPanelAction.SavePreset name) }
                | RowKind.ResetButton ->
                    { noop with Action = Some ConfigPanelAction.ResetDefaults }
                | RowKind.PresetItem name ->
                    { noop with Action = Some (ConfigPanelAction.LoadPreset name) }
                | RowKind.SectionHeader cat ->
                    let lbl = ConfigDescriptors.categoryLabel cat
                    let expanded =
                        if Set.contains lbl panelState.ExpandedSections then
                            Set.remove lbl panelState.ExpandedSections
                        else
                            Set.add lbl panelState.ExpandedSections
                    { noop with PanelState = { panelState with ExpandedSections = expanded } }
                | RowKind.AttrRow d ->
                    match d.InputKind with
                    | InputKind.Toggle ->
                        let cur = unbox<bool> (d.Get config)
                        let next = d.Set (box (not cur)) config
                        { noop with UpdatedConfig = Some next }
                    | InputKind.ColorPicker ->
                        let cur = unbox<SKColor> (d.Get config)
                        let nextColor = nextColorInPalette cur
                        let next = d.Set (box nextColor) config
                        { noop with UpdatedConfig = Some next }
                    | InputKind.EnumChoice labels ->
                        let cur = unbox<string> (d.Get config)
                        let next = d.Set (box (nextEnum labels cur)) config
                        { noop with UpdatedConfig = Some next }
                    | InputKind.Slider _
                    | InputKind.IntSlider _ ->
                        let controlX = panelX + padX + labelWidth
                        if x >= controlX then
                            match sliderValueAtX d panelX x config with
                            | Some newVal ->
                                let updated = d.Set newVal config
                                { PanelState =
                                      { panelState with ActiveControl = Some d.Key }
                                  UpdatedConfig = Some updated
                                  Action = None }
                            | None -> noop
                        else noop
                | _ -> noop
        | InputEvent.MouseMove(x, _y) ->
            match panelState.ActiveControl with
            | Some key ->
                match ConfigDescriptors.tryFind key with
                | Some d ->
                    match sliderValueAtX d panelX x config with
                    | Some newVal ->
                        let updated = d.Set newVal config
                        { noop with UpdatedConfig = Some updated }
                    | None -> noop
                | None -> noop
            | None -> noop
        | InputEvent.MouseUp _ ->
            { noop with PanelState = { panelState with ActiveControl = None } }
        | _ -> noop
