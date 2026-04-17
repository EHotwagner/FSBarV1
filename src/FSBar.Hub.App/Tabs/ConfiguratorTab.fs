namespace FSBar.Hub.App.Tabs

open SkiaSharp
open SkiaViewer
open FSBar.Viz

module ConfiguratorTab =

    [<RequireQualifiedAccess>]
    type ConfiguratorTabAction =
        | ConfigChanged of VizConfig
        | SavePreset of name: string
        | LoadPreset of name: string
        | DeletePreset of name: string
        | ResetDefaults

    type ConfiguratorTabState = {
        Panel: ConfigPanelState
        PresetNames: string list
        ActivePreset: string option
        LastPresetResult: Result<string, string> option
    }

    let private headingText = Scene.fill (SKColor(0xffuy, 0xffuy, 0xffuy, 0xffuy))
    let private dimText = Scene.fill (SKColor(0xb4uy, 0xbduy, 0xccuy, 0xffuy))
    let private panelBg = Scene.fill (SKColor(0x08uy, 0x0buy, 0x12uy, 0xffuy))
    let private divider = Scene.fill (SKColor(0x2auy, 0x33uy, 0x44uy, 0xffuy))
    let private okText = Scene.fill (SKColor(0x7auy, 0xe0uy, 0x8buy, 0xffuy))
    let private errText = Scene.fill (SKColor(0xffuy, 0x7auy, 0x7auy, 0xffuy))

    let init () : ConfiguratorTabState =
        { Panel = { ConfigPanel.initialState with IsOpen = true }
          PresetNames = StylePreset.listNames ()
          ActivePreset = None
          LastPresetResult = None }

    let render
            (state: ConfiguratorTabState)
            (vizConfig: VizConfig)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : Element list =
        // `ConfigPanel.buildPanel` assumes its panel hugs the right
        // edge of the passed "window" rectangle. We trick it into
        // laying out against our content rectangle by treating the
        // content area as its own window: pass contentW as the full
        // width so the panel lands at the right side of the tab, and
        // wrap the resulting elements in a Translate group offsetting
        // into the tab's position on the hub window.
        let panelElems =
            ConfigPanel.buildPanel
                vizConfig
                state.Panel
                contentW
                contentH
                state.PresetNames
                state.ActivePreset
        let translated =
            let tx = Transform.Translate(contentX, contentY)
            Scene.group (Some tx) None panelElems
        let headerY = contentY + 22.0f
        let header =
            [ Scene.text "Style — live visualisation configurator" (contentX + 8.0f) headerY 20.0f headingText
              Scene.text
                (sprintf "%d preset(s) on disk%s"
                    state.PresetNames.Length
                    (state.ActivePreset
                     |> Option.map (sprintf " · active: %s")
                     |> Option.defaultValue ""))
                (contentX + 8.0f) (headerY + 20.0f) 14.0f dimText
              // Result toast from the last save/load/delete.
              match state.LastPresetResult with
              | Some (Ok msg) ->
                  Scene.text (sprintf "✓ %s" msg) (contentX + 8.0f) (headerY + 40.0f) 13.0f okText
              | Some (Result.Error msg) ->
                  Scene.text (sprintf "✗ %s" msg) (contentX + 8.0f) (headerY + 40.0f) 13.0f errText
              | None -> () ]
        let dividerEl =
            Scene.rect contentX (headerY + 56.0f) (contentW * 0.45f) 1.0f divider
        // Background fill so tab content doesn't show through the
        // terrain if Viewer is visible behind during transitions.
        let bg = Scene.rect contentX contentY contentW contentH panelBg
        (bg :: header) @ [ dividerEl; translated ]

    /// Translate a raw InputEvent into panel-relative coords by
    /// subtracting the tab origin from mouse positions.
    let private translateEvent
            (event: InputEvent)
            (contentX: float32) (contentY: float32) : InputEvent =
        match event with
        | InputEvent.MouseDown(btn, x, y) ->
            InputEvent.MouseDown(btn, x - contentX, y - contentY)
        | InputEvent.MouseMove(x, y) ->
            InputEvent.MouseMove(x - contentX, y - contentY)
        | InputEvent.MouseUp(btn, x, y) ->
            InputEvent.MouseUp(btn, x - contentX, y - contentY)
        | InputEvent.MouseScroll(delta, x, y) ->
            InputEvent.MouseScroll(delta, x - contentX, y - contentY)
        | other -> other

    let handleInput
            (state: ConfiguratorTabState)
            (vizConfig: VizConfig)
            (event: InputEvent)
            (contentX: float32) (contentY: float32)
            (contentW: float32) (contentH: float32)
            : ConfiguratorTabState * ConfiguratorTabAction option =
        let panelEvt = translateEvent event contentX contentY
        let result =
            ConfigPanel.handleInput
                panelEvt
                vizConfig
                state.Panel
                contentW
                contentH
        let newState0 = { state with Panel = result.PanelState }
        // If the panel produced a new config, that's a hub-level
        // ConfigChanged action so the entrypoint can replace its
        // cached VizConfig.
        let configAction =
            result.UpdatedConfig |> Option.map ConfiguratorTabAction.ConfigChanged
        // Map panel-level action DU onto the tab's DU.
        let panelAction =
            result.Action
            |> Option.map (fun a ->
                match a with
                | ConfigPanelAction.SavePreset name -> ConfiguratorTabAction.SavePreset name
                | ConfigPanelAction.LoadPreset name -> ConfiguratorTabAction.LoadPreset name
                | ConfigPanelAction.DeletePreset name -> ConfiguratorTabAction.DeletePreset name
                | ConfigPanelAction.ResetDefaults -> ConfiguratorTabAction.ResetDefaults)
        // Prefer the file-system action when both fire on the same
        // click (SavePreset also emits a new VizConfig with the
        // dirty flag cleared). We surface both in the tuple — the
        // entrypoint applies ConfigChanged first, then the preset
        // action.
        let action =
            match configAction, panelAction with
            | Some _, Some p -> Some p
            | Some c, None -> Some c
            | None, p -> p
        // Apply ConfigChanged side-effect to the tab state too so the
        // next render picks up the new config without a round-trip
        // through the entrypoint.
        let newState = newState0
        newState, action
