namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Hub

module SettingsTab =

    [<RequireQualifiedAccess>]
    type SettingsTabAction =
        | InstallProxy
        | ForceReinstallProxy
        | RefreshStatus

    type SettingsTabState = {
        Status: ProxyInstaller.ProxyInstallStatus option
        Health: ProxyInstaller.ProxyHealth option
        LastInstallResult: Result<string, string> option
        InstallInFlight: bool
    }

    let init
            (install: BarInstall.BarInstall)
            (bundled: BundledProxy.BundledProxyInfo)
            : SettingsTabState =
        let status = ProxyInstaller.checkStatus install bundled
        { Status = Some status
          Health = Some (ProxyInstaller.health status)
          LastInstallResult = None
          InstallInFlight = false }

    let applyStatus
            (state: SettingsTabState)
            (status: ProxyInstaller.ProxyInstallStatus)
            : SettingsTabState =
        { state with
            Status = Some status
            Health = Some (ProxyInstaller.health status) }

    let applyInstallResult
            (state: SettingsTabState)
            (result: Result<ProxyInstaller.ProxyInstallStatus, string list>)
            : SettingsTabState =
        match result with
        | Ok status ->
            { state with
                Status = Some status
                Health = Some (ProxyInstaller.health status)
                LastInstallResult = Some (Ok "install completed")
                InstallInFlight = false }
        | Result.Error reasons ->
            { state with
                LastInstallResult = Some (Result.Error (String.concat "; " reasons))
                InstallInFlight = false }

    // --- Paints ---------------------------------------------------------

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private bodyText = Scene.fill (SKColor(0xf3uy, 0xf5uy, 0xfauy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private okText = Scene.fill (SKColor(0x7auy, 0xe0uy, 0x8buy, 0xffuy))
    let private warnText = Scene.fill (SKColor(0xffuy, 0xc0uy, 0x60uy, 0xffuy))
    let private errText = Scene.fill (SKColor(0xffuy, 0x7auy, 0x7auy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x10uy, 0x14uy, 0x1cuy, 0xffuy))
    let private rowBg = Scene.fill (SKColor(0x16uy, 0x1buy, 0x26uy, 0xffuy))
    let private installBg = Scene.fill (SKColor(0x23uy, 0x7auy, 0x3duy, 0xffuy))
    let private installBgDisabled = Scene.fill (SKColor(0x2buy, 0x35uy, 0x44uy, 0xffuy))
    let private dangerBg = Scene.fill (SKColor(0x6auy, 0x1cuy, 0x28uy, 0xffuy))
    let private refreshBg = Scene.fill (SKColor(0x21uy, 0x29uy, 0x38uy, 0xffuy))

    // --- Layout --------------------------------------------------------

    let private buttonHeight : float32 = 28.0f
    let private buttonSpacing : float32 = 8.0f

    // Button row sits below all the diagnostic text. Y is measured
    // from a fixed offset that matches the worst-case text block
    // height (header + 3 sections with 4–6 rows each + gaps).
    let private buttonRowY (contentY: float32) : float32 =
        contentY + 420.0f

    let private installButtonRect
            (contentX: float32) (contentY: float32)
            (_: float32) (_: float32) =
        let w = 200.0f
        let x = contentX + 16.0f
        x, buttonRowY contentY, w, buttonHeight

    let private forceButtonRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let (ix, iy, iw, _) = installButtonRect contentX contentY contentW contentH
        let x = ix + iw + buttonSpacing
        x, iy, 180.0f, buttonHeight

    let private refreshButtonRect
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32) =
        let (fx, fy, fw, _) = forceButtonRect contentX contentY contentW contentH
        let x = fx + fw + buttonSpacing
        x, fy, 140.0f, buttonHeight

    // --- Render --------------------------------------------------------

    let private healthParts (h: ProxyInstaller.ProxyHealth) : string * Paint =
        match h with
        | ProxyInstaller.UpToDate -> "✓ up to date", okText
        | ProxyInstaller.NotInstalled -> "✗ proxy not installed", warnText
        | ProxyInstaller.StaleVersion(inst, bundled) ->
            sprintf "↻ stale version: %s → %s" inst bundled, warnText
        | ProxyInstaller.StaleEngine(forEng, active) ->
            sprintf "↻ installed under %s, active is %s" forEng active, warnText
        | ProxyInstaller.ConfigIncomplete reasons ->
            sprintf "⚠ config incomplete: %s" (String.concat "; " reasons), warnText

    let render
            (state: SettingsTabState)
            (install: BarInstall.BarInstall option)
            (bundled: BundledProxy.BundledProxyInfo option)
            (settings: HubSettings.HubSettings)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        let headerY = contentY + 22.0f
        let mutable rowY = headerY + 52.0f
        let nextRow () =
            let y = rowY
            rowY <- rowY + 22.0f
            y
        let lines =
            [ yield Scene.text "Settings — BAR install · bundled proxy · ports"
                      (contentX + 8.0f) headerY 20.0f headingText
              yield Scene.text
                (sprintf "gRPC port %d · LaunchGraphicalViewerDefault=%b · schemaVersion=%d"
                    settings.GrpcPort
                    settings.LaunchGraphicalViewerDefault
                    settings.SchemaVersion)
                (contentX + 8.0f) (headerY + 22.0f) 14.0f dimText

              // Section 1: BAR install
              yield Scene.text "BAR install" (contentX + 8.0f) (nextRow ()) 15.0f headingText
              match install with
              | Some i ->
                  yield Scene.text (sprintf "  data dir     : %s" i.DataDir)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text (sprintf "  active engine: %s%s"
                                        i.ActiveEngine.Version
                                        (if i.DataDirIsDefault then " (default data dir)" else " (overridden)"))
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text (sprintf "  engines      : %d detected, newest first"
                                        i.Engines.Length)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  let ais = BarInstall.listSkirmishAis i.ActiveEngine
                  yield Scene.text
                          (sprintf "  AIs installed: %s"
                              (if ais.IsEmpty then "(none)" else String.concat ", " ais))
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
              | None ->
                  yield Scene.text "  ⚠ BAR install not detected"
                          (contentX + 8.0f) (nextRow ()) 14.0f errText

              // Section 2: bundled proxy
              rowY <- rowY + 8.0f
              yield Scene.text "Bundled proxy" (contentX + 8.0f) (nextRow ()) 15.0f headingText
              match bundled with
              | Some b ->
                  yield Scene.text (sprintf "  version  : %s" b.Version)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text (sprintf "  root     : %s" b.BundleRoot)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
              | None ->
                  yield Scene.text "  ⚠ bundled proxy not resolved — run scripts/refresh-bundled-proxy.sh"
                          (contentX + 8.0f) (nextRow ()) 14.0f warnText

              // Section 3: proxy install status
              rowY <- rowY + 8.0f
              yield Scene.text "Proxy install status" (contentX + 8.0f) (nextRow ()) 15.0f headingText
              match state.Status, state.Health with
              | Some s, Some h ->
                  let (healthText, paint) = healthParts h
                  yield Scene.text (sprintf "  engine       : %s" s.EngineVersion)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text (sprintf "  installed to : %s" s.InstalledAtPath)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text
                          (sprintf "  installed ver: %s"
                              (s.InstalledVersion |> Option.defaultValue "(none)"))
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text
                          (sprintf "  files / devmode / simpleAiList: %b / %b / %b"
                              s.AiFilesPresent s.DevModeFilePresent s.SimpleAiListDisabled)
                          (contentX + 8.0f) (nextRow ()) 14.0f bodyText
                  yield Scene.text (sprintf "  health       : %s" healthText)
                          (contentX + 8.0f) (nextRow ()) 14.0f paint
              | _ ->
                  yield Scene.text
                          "  (status unavailable — BAR install or bundle not resolved)"
                          (contentX + 8.0f) (nextRow ()) 14.0f dimText ]

        // Buttons
        let haveBoth = Option.isSome install && Option.isSome bundled
        let enableInstall = haveBoth && not state.InstallInFlight
        let (ix, iy, iw, ih) = installButtonRect contentX contentY contentW contentH
        let (fx, fy, fw, fh) = forceButtonRect contentX contentY contentW contentH
        let (rx, ry, rw, rh) = refreshButtonRect contentX contentY contentW contentH
        let installBgPaint = if enableInstall then installBg else installBgDisabled
        let buttons =
            [ Scene.rect ix iy iw ih installBgPaint
              Scene.text
                (if state.InstallInFlight then "Installing…" else "Install / Upgrade")
                (ix + 16.0f) (iy + ih * 0.68f) 15.0f (if enableInstall then headingText else dimText)
              Scene.rect fx fy fw fh (if enableInstall then dangerBg else installBgDisabled)
              Scene.text "Force reinstall" (fx + 16.0f) (fy + fh * 0.68f) 15.0f
                  (if enableInstall then headingText else dimText)
              Scene.rect rx ry rw rh refreshBg
              Scene.text "Refresh status" (rx + 16.0f) (ry + rh * 0.68f) 15.0f bodyText ]

        // Toast / last-result line below buttons.
        let toastY = iy + ih + 14.0f
        let toast =
            match state.LastInstallResult with
            | Some (Ok msg) ->
                [ Scene.text (sprintf "✓ %s" msg) (contentX + 16.0f) toastY 14.0f okText ]
            | Some (Result.Error msg) ->
                [ Scene.text (sprintf "✗ %s" msg) (contentX + 16.0f) toastY 14.0f errText ]
            | None -> []

        let bg = Scene.rect contentX contentY contentW contentH panelBg
        (bg :: lines) @ buttons @ toast

    // --- Input ---------------------------------------------------------

    let private hit (rx: float32, ry: float32, rw: float32, rh: float32) (x: float32) (y: float32) =
        x >= rx && x < rx + rw && y >= ry && y < ry + rh

    let handleMouse
            (state: SettingsTabState)
            (x: float32) (y: float32)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : SettingsTabAction option =
        if state.InstallInFlight then None else
        let installR = installButtonRect contentX contentY contentW contentH
        let forceR = forceButtonRect contentX contentY contentW contentH
        let refreshR = refreshButtonRect contentX contentY contentW contentH
        if hit installR x y then Some SettingsTabAction.InstallProxy
        elif hit forceR x y then Some SettingsTabAction.ForceReinstallProxy
        elif hit refreshR x y then Some SettingsTabAction.RefreshStatus
        else None
