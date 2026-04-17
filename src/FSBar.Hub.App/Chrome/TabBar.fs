namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer

[<RequireQualifiedAccess>]
type HubTab =
    | Setup
    | Viewer
    | Encyclopedia
    | Configurator
    | Settings
    | Grpc

module TabBar =

    let Width : float32 = 56.0f
    let private tabHeight : float32 = 56.0f
    let private topOffset : float32 = 8.0f

    let private allTabs =
        [| HubTab.Setup
           HubTab.Viewer
           HubTab.Encyclopedia
           HubTab.Configurator
           HubTab.Settings
           HubTab.Grpc |]

    let label (tab: HubTab) : string =
        match tab with
        | HubTab.Setup -> "Setup"
        | HubTab.Viewer -> "Viewer"
        | HubTab.Encyclopedia -> "Units"
        | HubTab.Configurator -> "Style"
        | HubTab.Settings -> "Cfg"
        | HubTab.Grpc -> "gRPC"

    // Longer display text (wraps above/below the row label) giving
    // operators a clearer hint without forcing the column wider.
    let private subLabel (tab: HubTab) : string =
        match tab with
        | HubTab.Setup -> "Lobby"
        | HubTab.Viewer -> "Live"
        | HubTab.Encyclopedia -> "Encyc"
        | HubTab.Configurator -> "Config"
        | HubTab.Settings -> "BAR"
        | HubTab.Grpc -> "API"

    let tabBounds (tabIndex: int) : float32 * float32 =
        let y0 = topOffset + float32 tabIndex * tabHeight
        y0, y0 + tabHeight

    let private bgPaint = Scene.fill (SKColor(0x18uy, 0x1cuy, 0x24uy, 0xffuy))
    let private activeBg = Scene.fill (SKColor(0x2buy, 0x36uy, 0x4buy, 0xffuy))
    let private divider = Scene.fill (SKColor(0x31uy, 0x3buy, 0x4auy, 0xffuy))
    let private textPaint = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let private textDim = Scene.fill (SKColor(0x8auy, 0x94uy, 0xa6uy, 0xffuy))

    /// Index of the tab in `allTabs`.
    let private indexOf (tab: HubTab) : int =
        allTabs |> Array.findIndex (fun t -> t = tab)

    let render (active: HubTab) (windowHeight: int) : Element list =
        let height = float32 windowHeight
        [ // Background column.
          yield Scene.rect 0.0f 0.0f Width height bgPaint
          // Right-edge vertical divider.
          yield Scene.rect (Width - 1.0f) 0.0f 1.0f height divider
          // Per-tab row.
          for i in 0 .. allTabs.Length - 1 do
              let tab = allTabs.[i]
              let y0, y1 = tabBounds i
              if tab = active then
                  yield Scene.rect 0.0f y0 Width (y1 - y0) activeBg
              // Label (short, 3-5 chars). Centered vertically within the row.
              let lbl = label tab
              let sub = subLabel tab
              let labelY = y0 + tabHeight * 0.45f
              let subY = y0 + tabHeight * 0.78f
              let labelPaint = if tab = active then textPaint else textDim
              yield Scene.text lbl 8.0f labelY 13.0f labelPaint
              yield Scene.text sub 8.0f subY 10.0f textDim
              // Row divider (except after last tab).
              if i < allTabs.Length - 1 then
                  yield Scene.rect 4.0f (y1 - 0.5f) (Width - 8.0f) 1.0f divider ]

    let handleMouse (x: float32) (y: float32) : HubTab option =
        if x < 0.0f || x > Width then None
        else
            let adj = y - topOffset
            if adj < 0.0f then None
            else
                let idx = int (adj / tabHeight)
                if idx < 0 || idx >= allTabs.Length then None
                else Some allTabs.[idx]
