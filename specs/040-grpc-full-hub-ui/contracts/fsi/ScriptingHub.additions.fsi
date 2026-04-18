// Delta sketch for src/FSBar.Hub/ScriptingHub.fsi
// Feature 040 — additions only. Existing members (Clients, OverflowDetachCount,
// plus the override set for StreamGameFrames / SendCommand / GetSessionStatus /
// GetUnitDef / Pause / Resume / SetEngineSpeed / ForceEndMatch /
// SendAdminMessage) remain unchanged and are not re-declared here.

namespace FSBar.Hub.ScriptingHub

open Fsbar.Hub.Scripting.V1   // generated namespace

type ScriptingService =
    // --- existing overrides omitted; refer to current ScriptingHub.fsi ---

    // US1 — Session orchestration
    member ConfigureLobby:       ConfigureLobbyRequest      * ServerCallContext -> Task<ConfigureLobbyResponse>
    member ListMaps:             ListMapsRequest            * ServerCallContext -> Task<ListMapsResponse>
    member ValidateLobby:        ValidateLobbyRequest       * ServerCallContext -> Task<ValidateLobbyResponse>
    member LaunchSession:        LaunchSessionRequest       * ServerCallContext -> Task<LaunchSessionResponse>
    member StopSession:          StopSessionRequest         * ServerCallContext -> Task<StopSessionResponse>

    // US2 — Rendered viewer frames
    member StreamRenderFrames:
        request:  StreamRenderFramesRequest *
        response: IServerStreamWriter<RenderFrameMessage> *
        context:  ServerCallContext -> Task
    member GetRenderFrame:       GetRenderFrameRequest      * ServerCallContext -> Task<GetRenderFrameResponse>

    // US3 — Viz + camera
    member SetVizConfig:         SetVizConfigRequest        * ServerCallContext -> Task<SetVizConfigResponse>
    member SetVizAttribute:      SetVizAttributeRequest     * ServerCallContext -> Task<SetVizAttributeResponse>
    member ToggleOverlay:        ToggleOverlayRequest       * ServerCallContext -> Task<ToggleOverlayResponse>
    member SetCamera:            SetCameraRequest           * ServerCallContext -> Task<SetCameraResponse>
    member SetActiveTab:         SetActiveTabRequest        * ServerCallContext -> Task<SetActiveTabResponse>

    // US4 — Preset / encyclopedia / settings / proxy
    member ListPresets:          ListPresetsRequest         * ServerCallContext -> Task<ListPresetsResponse>
    member SavePreset:           SavePresetRequest          * ServerCallContext -> Task<SavePresetResponse>
    member LoadPreset:           LoadPresetRequest          * ServerCallContext -> Task<LoadPresetResponse>
    member DeletePreset:         DeletePresetRequest        * ServerCallContext -> Task<DeletePresetResponse>
    member ListUnits:            ListUnitsRequest           * ServerCallContext -> Task<ListUnitsResponse>
    member SelectUnit:           SelectUnitRequest          * ServerCallContext -> Task<SelectUnitResponse>
    member GetHubSettings:       GetHubSettingsRequest      * ServerCallContext -> Task<GetHubSettingsResponse>
    member SetHubSettings:       SetHubSettingsRequest      * ServerCallContext -> Task<SetHubSettingsResponse>
    member InstallProxy:         InstallProxyRequest        * ServerCallContext -> Task<InstallProxyResponse>
    member RefreshProxyStatus:   RefreshProxyStatusRequest  * ServerCallContext -> Task<RefreshProxyStatusResponse>

    // US5 — Hub-wide state sync
    member GetHubState:          GetHubStateRequest         * ServerCallContext -> Task<HubStateSnapshot>
    member StreamHubStateEvents:
        request:  StreamHubStateEventsRequest *
        response: IServerStreamWriter<HubStateEvent> *
        context:  ServerCallContext -> Task

    // US6 — Client-authored overlays
    member PutLayer:             PutLayerRequest            * ServerCallContext -> Task<PutLayerResponse>
    member DeleteLayer:          DeleteLayerRequest         * ServerCallContext -> Task<DeleteLayerResponse>
    member ListLayers:           ListLayersRequest          * ServerCallContext -> Task<ListLayersResponse>
    member ClearLayers:          ClearLayersRequest         * ServerCallContext -> Task<ClearLayersResponse>
