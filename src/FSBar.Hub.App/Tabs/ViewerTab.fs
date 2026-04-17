namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Client
open FSBar.Viz
open FSBar.Hub

module ViewerTab =

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x08uy, 0x0buy, 0x12uy, 0xffuy))

    // Feature 038: BAR headless (2025.06.19) crashes on `/pause` chat
    // commands, and neither `/speed 0` nor the proto-level PauseTeamCommand
    // halts global simulation. Until a non-broken engine-level pause
    // ships, the Viewer-tab pause button freezes the *rendered* snapshot
    // in place. The engine keeps advancing in the background; on unpause
    // the view jumps to the current state. This is enough for the
    // "study a moment" use case; it is NOT a true game pause.
    let mutable private frozenState: GameState option = None
    let private pauseBtnBg = Scene.fill (SKColor(0x23uy, 0x2buy, 0x38uy, 0xffuy))
    let private pauseBtnBgActive = Scene.fill (SKColor(0x7auy, 0x4auy, 0x2auy, 0xffuy))
    let private pauseBtnBorder = Scene.stroke (SKColor(0x7auy, 0x9fuy, 0xd5uy, 0xffuy)) 1.5f

    // Feature 038 FR-004b. Pause button geometry — 34×28 pixels,
    // anchored to the top-right of the content area with an 8 px inset.
    let pauseButtonRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : float32 * float32 * float32 * float32 =
        let w = 34.0f
        let h = 28.0f
        let x = contentX + contentW - w - 8.0f
        let y = contentY + 8.0f
        x, y, w, h

    let private renderPauseButton
            (isPaused: bool)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) : Element list =
        let x, y, w, h = pauseButtonRect contentX contentY contentW contentH
        let bg = if isPaused then pauseBtnBgActive else pauseBtnBg
        let glyph = if isPaused then "▶" else "⏸"
        [ Scene.rect x y w h bg
          Scene.rect x y w h pauseBtnBorder
          Scene.text glyph (x + w * 0.30f) (y + h * 0.72f) 16.0f headingText ]

    let render
            (sessionState: SessionManager.SessionState)
            (vizConfig: VizConfig)
            (viewStateRef: ViewState ref)
            (isPaused: bool)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        match sessionState with
        | SessionManager.Running rs ->
            // Pull live GameState + MapGrid off the active BarClient.
            // buildSceneHeadlessView tolerates map=None for frames that
            // arrive before MapGrid is loaded.
            // Feature 038 pause: freeze the displayed snapshot while
            // `isPaused = true` by caching the first paused-frame state
            // and re-using it on subsequent renders.
            let state =
                if isPaused then
                    match frozenState with
                    | Some s -> s
                    | None ->
                        let s = rs.BarClient.GameState
                        frozenState <- Some s
                        s
                else
                    frozenState <- None
                    rs.BarClient.GameState
            // Enable Units + MetalSpots overlays on top of the
            // incoming config. The caller's config controls
            // everything else (BaseLayer, palette, marker size, etc.).
            let cfg =
                { vizConfig with
                    ActiveOverlays =
                        vizConfig.ActiveOverlays
                        |> Set.add OverlayKind.Units
                        |> Set.add OverlayKind.MetalSpots }
            // Keep the ViewState's WindowWidth/Height in sync with the
            // content rect so downstream math (fit, event indicators)
            // is correct.
            let vw, vh = int contentW, int contentH
            let current = viewStateRef.Value
            let resized =
                if current.WindowWidth <> vw || current.WindowHeight <> vh then
                    { current with WindowWidth = vw; WindowHeight = vh }
                else current
            // When AutoFit is on, re-letterbox to the content rect so
            // the user sees the whole map before zooming in. Once the
            // user scrolls or drags, the caller flips AutoFit off and
            // the explicit Scale/Origin take over.
            let effective =
                if resized.AutoFit then
                    let scale = SceneBuilder.computeFitScale rs.MapGrid vw vh
                    { resized with Scale = scale; OriginX = 0.0f; OriginY = 0.0f }
                else resized
            if not (System.Object.ReferenceEquals(effective, current)) then
                viewStateRef.Value <- effective
            // Feature 038 FR-001/002: thread the live UnitDefCache into
            // SceneBuilder so Viewer-tab glyphs byte-match the Units-tab
            // encyclopedia via the shared UnitDisplayAdapter.
            let embedded =
                SceneBuilder.buildSceneHeadlessView
                    state rs.MapGrid rs.MetalSpots (Some state.UnitDefs) cfg effective
            // Scene elements are authored in viewport-relative space
            // with origin at (0,0); wrap in a translated group so they
            // land inside the Viewer tab's content rectangle.
            let tx = Transform.Translate(contentX, contentY)
            let content = Scene.group (Some tx) None embedded.Elements
            [ yield Scene.rect contentX contentY contentW contentH panelBg
              yield content
              yield! renderPauseButton isPaused contentX contentY contentW contentH ]
        | SessionManager.Starting _ ->
            frozenState <- None
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session starting…" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText
              Scene.text "Engine warmup typically takes 5–30s." (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
        | SessionManager.Ending _ ->
            frozenState <- None
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session ending…" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText ]
        | SessionManager.Failed(_, reason, _) ->
            frozenState <- None
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session failed" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText
              Scene.text reason (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
        | SessionManager.Idle ->
            frozenState <- None
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "No session active" (contentX + 16.0f) (contentY + 32.0f) 16.0f dimText
              Scene.text "Switch to the Setup tab to pick a lobby and launch." (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
