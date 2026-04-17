namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Viz
open FSBar.Hub

module ViewerTab =

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x08uy, 0x0buy, 0x12uy, 0xffuy))

    let render
            (sessionState: SessionManager.SessionState)
            (vizConfig: VizConfig)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        match sessionState with
        | SessionManager.Running rs ->
            // Pull live GameState + MapGrid off the active BarClient.
            // buildSceneHeadless tolerates map=None for frames that
            // arrive before MapGrid is loaded.
            let state = rs.BarClient.GameState
            // Enable Units + MetalSpots overlays on top of the
            // incoming config. The caller's config controls
            // everything else (BaseLayer, palette, marker size, etc.).
            let cfg =
                { vizConfig with
                    ActiveOverlays =
                        vizConfig.ActiveOverlays
                        |> Set.add OverlayKind.Units
                        |> Set.add OverlayKind.MetalSpots }
            let embedded =
                SceneBuilder.buildSceneHeadlessSized
                    state rs.MapGrid rs.MetalSpots cfg (int contentW) (int contentH)
            // Scene elements are authored in viewport-relative space
            // with origin at (0,0); wrap in a translated group so they
            // land inside the Viewer tab's content rectangle.
            let tx = Transform.Translate(contentX, contentY)
            let content = Scene.group (Some tx) None embedded.Elements
            [ Scene.rect contentX contentY contentW contentH panelBg
              content ]
        | SessionManager.Starting _ ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session starting…" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText
              Scene.text "Engine warmup typically takes 5–30s." (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
        | SessionManager.Ending _ ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session ending…" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText ]
        | SessionManager.Failed(_, reason, _) ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session failed" (contentX + 16.0f) (contentY + 32.0f) 16.0f headingText
              Scene.text reason (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
        | SessionManager.Idle ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "No session active" (contentX + 16.0f) (contentY + 32.0f) 16.0f dimText
              Scene.text "Switch to the Setup tab to pick a lobby and launch." (contentX + 16.0f) (contentY + 52.0f) 14.0f dimText ]
