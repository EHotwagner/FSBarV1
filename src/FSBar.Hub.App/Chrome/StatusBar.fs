namespace FSBar.Hub.App.Chrome

open SkiaSharp
open SkiaViewer
open FSBar.Hub

module StatusBar =

    let Height : float32 = 24.0f

    [<RequireQualifiedAccess>]
    type StatusBarAction =
        | SetSpeed of speed: float32
        | TogglePause
        | EndSession

    type StatusBarState = {
        SessionState: SessionManager.SessionState
        Paused: bool
        Speed: float32
    }

    let private bg = Scene.fill (SKColor(0x14uy, 0x18uy, 0x22uy, 0xffuy))
    let private divider = Scene.fill (SKColor(0x2auy, 0x33uy, 0x44uy, 0xffuy))
    let private textPaint = Scene.fill (SKColor(0xeauy, 0xeeuy, 0xf6uy, 0xffuy))
    let private textDim = Scene.fill (SKColor(0x8auy, 0x94uy, 0xa6uy, 0xffuy))
    let private buttonBg = Scene.fill (SKColor(0x21uy, 0x29uy, 0x38uy, 0xffuy))
    let private buttonHot = Scene.fill (SKColor(0x34uy, 0x40uy, 0x58uy, 0xffuy))
    let private buttonDanger = Scene.fill (SKColor(0x6auy, 0x1cuy, 0x28uy, 0xffuy))
    let private sliderTrack = Scene.fill (SKColor(0x34uy, 0x40uy, 0x58uy, 0xffuy))
    let private sliderThumb = Scene.fill (SKColor(0x7auy, 0x9fuy, 0xd5uy, 0xffuy))

    // --- Control layout (measured from the right edge) --------------------
    //
    // [ speed slider | speed label ] [ pause ] [ end ] | right edge
    // 120px wide       40px          48px       48px

    let private endButtonWidth = 48.0f
    let private pauseButtonWidth = 48.0f
    let private speedLabelWidth = 44.0f
    let private sliderWidth = 120.0f
    let private padX = 8.0f

    let private buttonHeight = 20.0f

    // Rectangle helpers — return (x, y, w, h).
    let private endButtonRect (winW: int) (winH: int) =
        let y = float32 winH - Height + 2.0f
        let x = float32 winW - endButtonWidth - padX
        x, y, endButtonWidth, buttonHeight

    let private pauseButtonRect (winW: int) (winH: int) =
        let (ex, y, _, h) = endButtonRect winW winH
        let x = ex - pauseButtonWidth - padX
        x, y, pauseButtonWidth, h

    let private sliderRect (winW: int) (winH: int) =
        let (px, y, _, h) = pauseButtonRect winW winH
        let x = px - speedLabelWidth - sliderWidth - padX
        x, y, sliderWidth, h

    let private stateLabel (st: SessionManager.SessionState) (paused: bool) =
        match st with
        | SessionManager.Idle -> "Idle", textDim
        | SessionManager.Starting _ -> "Starting…", textPaint
        | SessionManager.Running rs ->
            let elapsed = System.DateTimeOffset.UtcNow - rs.StartedAt
            let stamp = sprintf "%02d:%02d" (int elapsed.TotalMinutes) elapsed.Seconds
            let prefix = if paused then "Paused" else "Running"
            sprintf "%s · %s · %s" prefix rs.Config.MapName stamp, textPaint
        | SessionManager.Ending _ -> "Ending…", textPaint
        | SessionManager.Failed(_, reason, _) -> sprintf "Failed: %s" reason, textDim

    // Map slider position to speed value [0.1, 10.0] on a log scale.
    let private sliderToSpeed (frac: float32) : float32 =
        let clamped = max 0.0f (min 1.0f frac)
        let logMin = log 0.1f
        let logMax = log 10.0f
        exp (logMin + clamped * (logMax - logMin))

    let private speedToSlider (speed: float32) : float32 =
        let clamped = max 0.1f (min 10.0f speed)
        let logMin = log 0.1f
        let logMax = log 10.0f
        (log clamped - logMin) / (logMax - logMin)

    let render (state: StatusBarState) (windowWidth: int) (windowHeight: int) : Element list =
        let winW = float32 windowWidth
        let winH = float32 windowHeight
        let y = winH - Height
        [ // Background band + top divider.
          yield Scene.rect 0.0f y winW Height bg
          yield Scene.rect 0.0f y winW 1.0f divider
          // Left-side state text.
          let (txt, paint) = stateLabel state.SessionState state.Paused
          yield Scene.text txt (padX * 2.0f) (y + Height * 0.68f) 12.0f paint
          // Right-side controls. Only rendered when a session is actually
          // running / starting — avoids misleading controls when Idle.
          let showControls =
              match state.SessionState with
              | SessionManager.Running _
              | SessionManager.Starting _
              | SessionManager.Ending _ -> true
              | _ -> false
          if showControls then
              // Slider (speed).
              let (sx, sy, sw, sh) = sliderRect windowWidth windowHeight
              yield Scene.rect sx (sy + sh * 0.4f) sw 3.0f sliderTrack
              let thumbX = sx + speedToSlider state.Speed * sw
              yield Scene.rect (thumbX - 3.0f) sy 6.0f sh sliderThumb
              // Speed label (numeric).
              yield Scene.text (sprintf "%.1fx" state.Speed)
                      (sx + sw + 4.0f) (sy + sh * 0.7f) 11.0f textDim
              // Pause button.
              let (px, py, pw, ph) = pauseButtonRect windowWidth windowHeight
              let pauseBg = if state.Paused then buttonHot else buttonBg
              yield Scene.rect px py pw ph pauseBg
              let pauseText = if state.Paused then "Resume" else "Pause"
              yield Scene.text pauseText (px + 6.0f) (py + ph * 0.7f) 11.0f textPaint
              // End button.
              let (ex, ey, ew, eh) = endButtonRect windowWidth windowHeight
              yield Scene.rect ex ey ew eh buttonDanger
              yield Scene.text "End" (ex + 14.0f) (ey + eh * 0.7f) 11.0f textPaint ]

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let handleMouse
            (state: StatusBarState)
            (x: float32)
            (y: float32)
            (windowWidth: int)
            (windowHeight: int)
            : StatusBarAction option =
        let showControls =
            match state.SessionState with
            | SessionManager.Running _
            | SessionManager.Starting _
            | SessionManager.Ending _ -> true
            | _ -> false
        if not showControls then None
        else
            let endRect = endButtonRect windowWidth windowHeight
            let pauseRect = pauseButtonRect windowWidth windowHeight
            let slider = sliderRect windowWidth windowHeight
            if hit endRect x y then Some StatusBarAction.EndSession
            elif hit pauseRect x y then Some StatusBarAction.TogglePause
            elif hit slider x y then
                let (sx, _, sw, _) = slider
                let frac = (x - sx) / sw
                Some (StatusBarAction.SetSpeed (sliderToSpeed frac))
            else None
