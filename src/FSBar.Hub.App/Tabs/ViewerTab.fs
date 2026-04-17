namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Viz
open FSBar.Hub

module ViewerTab =

    let private headingText = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0x7auy, 0x86uy, 0x9cuy, 0xffuy))
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
            let mapGrid =
                // BarClient doesn't publicly expose MapGrid today; rely
                // on GameState alone. Phase-9 follow-up: thread MapGrid
                // through SessionManager so this branch renders base
                // terrain as well. For now the scene still shows units
                // + economy over the synthetic empty grid fallback.
                None
            let embedded = SceneBuilder.buildSceneHeadless state mapGrid vizConfig
            // Clip the embedded scene to the content rectangle. Use a
            // Group transform to offset the scene into the content area.
            [ yield Scene.rect contentX contentY contentW contentH panelBg
              yield! embedded.Elements ]
        | SessionManager.Starting _ ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session starting…" (contentX + 16.0f) (contentY + 32.0f) 14.0f headingText
              Scene.text "Engine warmup typically takes 5–30s." (contentX + 16.0f) (contentY + 52.0f) 12.0f dimText ]
        | SessionManager.Ending _ ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session ending…" (contentX + 16.0f) (contentY + 32.0f) 14.0f headingText ]
        | SessionManager.Failed(_, reason, _) ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "Session failed" (contentX + 16.0f) (contentY + 32.0f) 14.0f headingText
              Scene.text reason (contentX + 16.0f) (contentY + 52.0f) 12.0f dimText ]
        | SessionManager.Idle ->
            [ Scene.rect contentX contentY contentW contentH panelBg
              Scene.text "No session active" (contentX + 16.0f) (contentY + 32.0f) 14.0f dimText
              Scene.text "Switch to the Setup tab to pick a lobby and launch." (contentX + 16.0f) (contentY + 52.0f) 12.0f dimText ]
