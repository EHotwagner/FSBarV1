namespace FSBar.Hub.App.Tabs

open System.IO
open SkiaSharp
open SkiaViewer
open FSBar.Hub

module SetupTab =

    [<RequireQualifiedAccess>]
    type SetupTabAction =
        | SelectMap of mapName: string
        | ScrollMapList of offset: float32
        | Launch

    type SetupTabState = {
        MapListScroll: float32
        Maps: string list
        Lobby: LobbyConfig.LobbyConfig
        Errors: LobbyConfig.LobbyError list
        LastLaunchError: string option
    }

    // --- Paints ----------------------------------------------------------
    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private bodyText = Scene.fill (SKColor(0xf3uy, 0xf5uy, 0xfauy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private errorText = Scene.fill (SKColor(0xffuy, 0x7auy, 0x7auy, 0xffuy))
    let private warnText = Scene.fill (SKColor(0xffuy, 0xc0uy, 0x60uy, 0xffuy))
    let private rowBg = Scene.fill (SKColor(0x16uy, 0x1buy, 0x26uy, 0xffuy))
    let private rowActiveBg = Scene.fill (SKColor(0x2buy, 0x38uy, 0x52uy, 0xffuy))
    let private rowHoverBg = Scene.fill (SKColor(0x1fuy, 0x26uy, 0x34uy, 0xffuy))
    let private rowBorder = Scene.fill (SKColor(0x2cuy, 0x35uy, 0x48uy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x10uy, 0x14uy, 0x1cuy, 0xffuy))
    let private launchBg = Scene.fill (SKColor(0x23uy, 0x7auy, 0x3duy, 0xffuy))
    let private launchBgDisabled = Scene.fill (SKColor(0x2buy, 0x35uy, 0x44uy, 0xffuy))

    // --- Layout ---------------------------------------------------------

    let private mapRowHeight : float32 = 24.0f
    let private launchButtonHeight : float32 = 32.0f

    /// Archive stems like `avalanche_3.4` are what the engine indexes
    /// in the maps directory. We surface the known-working display name
    /// for the one map the live tests rely on; other maps show their
    /// filename stem and Launch may still succeed if the engine's
    /// archive scan resolves them.
    let private archiveToDisplayName (archiveStem: string) : string =
        match archiveStem with
        | "avalanche_3.4" -> "Avalanche 3.4"
        | s -> s

    let private loadMaps (install: BarInstall.BarInstall) : string list =
        let mapsDir = Path.Combine(install.DataDir, "maps")
        if not (Directory.Exists(mapsDir)) then []
        else
            Directory.GetFiles(mapsDir, "*.sd7")
            |> Array.map (fun p -> Path.GetFileNameWithoutExtension(p))
            |> Array.sort
            |> Array.toList

    let validate (install: BarInstall.BarInstall) (state: SetupTabState) : SetupTabState =
        match LobbyConfig.validate install state.Lobby with
        | Ok _ -> { state with Errors = [] }
        | Result.Error errs -> { state with Errors = errs }

    let init (install: BarInstall.BarInstall) : SetupTabState =
        let archiveStems = loadMaps install
        // Seed the lobby's MapName with the first archive that resolves
        // to a known-working display name, else the first stem.
        let seedMap =
            archiveStems
            |> List.tryFind (fun s -> archiveToDisplayName s <> s)
            |> Option.map archiveToDisplayName
            |> Option.orElseWith (fun () -> archiveStems |> List.tryHead)
            |> Option.defaultValue ""
        let lobby = { LobbyConfig.defaults with MapName = seedMap }
        let initial =
            { MapListScroll = 0.0f
              Maps = archiveStems
              Lobby = lobby
              Errors = []
              LastLaunchError = None }
        validate install initial

    // --- Layout helpers ------------------------------------------------

    // Split the tab content area into a left "map list" panel (45%) and
    // a right "summary + launch" panel (55%).
    let private mapPanelRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let w = contentW * 0.42f
        contentX, contentY + 48.0f, w, contentH - 96.0f

    let private summaryPanelRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let mx, my, mw, mh = mapPanelRect contentX contentY contentW contentH
        let sx = mx + mw + 16.0f
        let sw = contentW - (sx - contentX) - 16.0f
        sx, my, sw, mh

    let private launchButtonRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let sx, _, sw, _ = summaryPanelRect contentX contentY contentW contentH
        let y = contentY + contentH - launchButtonHeight - 16.0f
        sx, y, sw, launchButtonHeight

    // --- Render --------------------------------------------------------

    let private renderHeader
            (state: SetupTabState) (contentX: float32) (contentY: float32) =
        [ Scene.text "Setup — configure your next session" (contentX + 8.0f) (contentY + 22.0f) 20.0f headingText
          Scene.text
            (sprintf "%d maps · %d teams · mode=%A · speed=%.1fx"
                state.Maps.Length state.Lobby.Teams.Length state.Lobby.Mode state.Lobby.EngineSpeed)
            (contentX + 8.0f) (contentY + 42.0f) 14.0f dimText ]

    let private renderMapList
            (state: SetupTabState)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let x, y, w, h = mapPanelRect contentX contentY contentW contentH
        let selectedDisplay = state.Lobby.MapName
        let rows : Element list =
            [ yield Scene.rect x y w h panelBg
              yield Scene.text "Map" (x + 8.0f) (y - 6.0f) 14.0f dimText
              // Determine first visible index.
              let firstIdx = int (state.MapListScroll / mapRowHeight)
              let visibleRows = int (h / mapRowHeight) + 1
              for i in firstIdx .. min (state.Maps.Length - 1) (firstIdx + visibleRows) do
                  let stem = state.Maps.[i]
                  let display = archiveToDisplayName stem
                  let rowY = y + float32 (i - firstIdx) * mapRowHeight - (state.MapListScroll - float32 firstIdx * mapRowHeight)
                  if rowY + mapRowHeight < y || rowY > y + h then () else
                  let isSelected = display = selectedDisplay
                  let bg = if isSelected then rowActiveBg else rowBg
                  yield Scene.rect (x + 4.0f) (rowY + 2.0f) (w - 8.0f) (mapRowHeight - 3.0f) bg
                  let textColor = if isSelected then headingText else bodyText
                  let label =
                      if display <> stem then sprintf "%s  (%s)" display stem
                      else stem
                  yield Scene.text label (x + 14.0f) (rowY + mapRowHeight - 8.0f) 14.0f textColor ]
        rows

    let private renderSummaryPanel
            (state: SetupTabState)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let x, y, w, h = summaryPanelRect contentX contentY contentW contentH
        let teamLines =
            state.Lobby.Teams
            |> List.mapi (fun idx team ->
                let seatText =
                    team.Seats
                    |> List.map (fun s ->
                        match s.Kind with
                        | LobbyConfig.AiSeat(name, _) -> sprintf "AI %s (%s)" name s.Side
                        | LobbyConfig.HumanSeat name -> sprintf "human %s (%s)" name s.Side)
                    |> String.concat ", "
                sprintf "Team %d [ally %d]: %s" idx team.AllyTeamId seatText)
        let lines =
            [ sprintf "Map: %s" (if System.String.IsNullOrEmpty(state.Lobby.MapName) then "<none>" else state.Lobby.MapName)
              sprintf "Mode: %A    Speed: %.1fx    Graphical viewer: %b"
                  state.Lobby.Mode state.Lobby.EngineSpeed state.Lobby.LaunchGraphicalViewer
              yield! teamLines ]
        [ yield Scene.rect x y w h panelBg
          yield Scene.text "Lobby" (x + 8.0f) (y - 6.0f) 14.0f dimText
          for i in 0 .. lines.Length - 1 do
              yield Scene.text lines.[i] (x + 14.0f) (y + 22.0f + float32 i * 20.0f) 15.0f bodyText
          // Errors
          let errBaseY = y + 22.0f + float32 lines.Length * 20.0f + 20.0f
          if state.Errors.IsEmpty then
              yield Scene.text "✓ Lobby validates — ready to launch." (x + 14.0f) errBaseY 14.0f headingText
          else
              yield Scene.text (sprintf "✗ %d validation error(s):" state.Errors.Length)
                      (x + 14.0f) errBaseY 14.0f errorText
              for i in 0 .. state.Errors.Length - 1 do
                  let msg = LobbyConfig.formatError state.Errors.[i]
                  yield Scene.text (sprintf "  · %s" msg)
                          (x + 14.0f) (errBaseY + 20.0f + float32 i * 16.0f) 14.0f errorText
          // Last launch error (from SessionManager)
          match state.LastLaunchError with
          | Some err ->
              let launchErrY = errBaseY + 40.0f + float32 (max 1 state.Errors.Length) * 16.0f
              yield Scene.text (sprintf "⚠ last Launch attempt: %s" err)
                      (x + 14.0f) launchErrY 13.0f warnText
          | None -> () ]

    let private renderLaunchButton
            (state: SetupTabState)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let x, y, w, h = launchButtonRect contentX contentY contentW contentH
        let enabled = state.Errors.IsEmpty
        let bg = if enabled then launchBg else launchBgDisabled
        let label = if enabled then "Launch →" else "Launch (fix errors first)"
        let textColor = if enabled then headingText else dimText
        [ Scene.rect x y w h bg
          Scene.text label (x + w / 2.0f - 40.0f) (y + h * 0.66f) 17.0f textColor ]

    let render
            (state: SetupTabState)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        List.concat [
            renderHeader state contentX contentY
            renderMapList state contentX contentY contentW contentH
            renderSummaryPanel state contentX contentY contentW contentH
            renderLaunchButton state contentX contentY contentW contentH ]

    // --- Input ---------------------------------------------------------

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let handleMouse
            (state: SetupTabState)
            (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : SetupTabAction option =
        let launchR = launchButtonRect contentX contentY contentW contentH
        let mapR = mapPanelRect contentX contentY contentW contentH
        if hit launchR x y then
            if state.Errors.IsEmpty then Some SetupTabAction.Launch
            else None
        elif hit mapR x y then
            let (mx, my, _, _) = mapR
            let firstIdx = int (state.MapListScroll / mapRowHeight)
            let localY = y - my + (state.MapListScroll - float32 firstIdx * mapRowHeight)
            let rowIdx = firstIdx + int (localY / mapRowHeight)
            if rowIdx < 0 || rowIdx >= state.Maps.Length then None
            else
                let stem = state.Maps.[rowIdx]
                Some (SetupTabAction.SelectMap(archiveToDisplayName stem))
        else None

    let handleScroll
            (state: SetupTabState)
            (delta: float32) (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : SetupTabAction option =
        let mapR = mapPanelRect contentX contentY contentW contentH
        if hit mapR x y then
            let totalHeight = float32 state.Maps.Length * mapRowHeight
            let (_, _, _, mh) = mapR
            let maxScroll = max 0.0f (totalHeight - mh)
            let next = state.MapListScroll - delta * mapRowHeight * 2.0f
            let clamped = max 0.0f (min maxScroll next)
            Some (SetupTabAction.ScrollMapList clamped)
        else None
