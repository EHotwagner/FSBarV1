// Delta sketch for src/FSBar.Hub/HubEvents.fsi
// Feature 040 — new HubEvent cases. Every pre-existing case (StateChanged,
// EngineSpeedChanged, SessionPaused, DiagnosticsLine, ScriptingClientConnected,
// ScriptingClientDetached, ProxyInstallProgress, AdminChannelStatusChanged)
// remains unchanged.

namespace FSBar.Hub

open FSBar.Viz

type HubEvent =
    // --- existing cases omitted ---

    // US5 additions:
    | ActiveTabChanged of HubStateStore.HubTab
    | VizConfigChanged of VizConfig
    | VizAttributeChanged of key: string *
                             oldValue: ConfigDescriptors.AttributeValue *
                             newValue: ConfigDescriptors.AttributeValue
    | CameraChanged of HubStateStore.ViewerCamera
    | LobbyChanged of LobbyConfig
    | EncyclopediaSelectionChanged of HubStateStore.EncyclopediaSelection
    | PresetSaved of name: string
    | PresetDeleted of name: string
    | PresetLoaded of name: string
    | HubSettingsChanged of HubSettings
