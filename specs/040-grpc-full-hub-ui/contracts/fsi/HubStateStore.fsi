// Sketch for src/FSBar.Hub/HubStateStore.fsi
// Feature 040 — central authoritative UI-state store.

namespace FSBar.Hub

open FSBar.Viz

type HubTab =
    | Setup
    | Viewer
    | Units
    | Style
    | Cfg
    | Grpc

type ViewerCamera =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      AutoFit: bool }

type FactionFilterKey =
    | Armada
    | Cortex
    | Legion
    | Raptors
    | Scavengers
    | Neutral

type EncyclopediaSelection =
    { FactionFilter: Set<FactionFilterKey>
      SelectedDefId: int option }

type HubState =
    { ActiveTab: HubTab
      VizConfig: VizConfig
      Camera: ViewerCamera
      Lobby: LobbyConfig
      Encyclopedia: EncyclopediaSelection
      PresetList: string list
      Settings: HubSettings }

type SubmitOutcome =
    | Sent
    | Rejected of reason: string

module ViewerCamera =
    val defaults: ViewerCamera
    val validate: ViewerCamera -> Result<ViewerCamera, string>

module HubStateStore =
    type T

    /// Allocate a new store with given initial state. Events flow through the
    /// supplied IHubEventSink.
    val create: HubEvents.IHubEventSink -> HubState -> T

    /// Read current state atomically. Safe from any thread.
    val current: T -> HubState

    /// --- mutators (atomic, LWW, emit events) ---

    val setActiveTab:      T -> HubTab -> SubmitOutcome
    val setCamera:         T -> ViewerCamera -> SubmitOutcome
    val setLobby:          T -> LobbyConfig -> SubmitOutcome
    val setVizConfig:      T -> VizConfig -> SubmitOutcome
    val setVizAttribute:   T -> key: string -> value: ConfigDescriptors.AttributeValue -> SubmitOutcome
    val toggleOverlay:     T -> ConfigDescriptors.OverlayKey -> target: ToggleTarget -> SubmitOutcome * bool
    val setEncyclopedia:   T -> EncyclopediaSelection -> SubmitOutcome
    val updatePresetList:  T -> string list -> unit                // facade-only, no event
    val setSettings:       T -> HubSettings -> SubmitOutcome

and ToggleTarget =
    | Toggle
    | On
    | Off
