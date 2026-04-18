namespace FSBar.Hub

open FSBar.Viz

/// Aggregate UI-state owned by `HubStateStore`. Every top-level tab and
/// the Viewer camera is represented here. Mutations go through the
/// store's mutator helpers (LWW + event emission); direct reads use
/// `HubStateStore.current`.
type HubState =
    { ActiveTab: HubTab
      VizConfig: VizConfig
      Camera: ViewerCamera
      Lobby: LobbyConfig.LobbyConfig
      Encyclopedia: EncyclopediaSelection
      PresetList: string list
      Settings: HubSettings.HubSettings }

/// Central, authoritative state store for the Hub UI (feature 040).
///
/// Every user-facing action — active-tab swap, Viewer camera pan/zoom,
/// lobby edit, `VizConfig` change, encyclopedia selection, preset list
/// invalidation, settings change — routes through this module. Both the
/// local GUI (tab files under `FSBar.Hub.App`) and the gRPC handlers
/// (`ScriptingHub`) read and write the same cell, so the two surfaces
/// can never drift. Every successful mutation publishes exactly one
/// `HubEvent` through the supplied `IHubEventSink`, powering both local
/// redraw and the remote `StreamHubStateEvents` fan-out.
///
/// Concurrency model: single `HubState` reference updated via
/// `Interlocked.CompareExchange`. Under contention the loser re-reads
/// and retries up to 3 times before returning
/// `Rejected "write contention"`. This matches the FR-015 /
/// data-model.md §HubStateStore atomic-LWW guarantee.
module HubStateStore =

    /// Opaque handle over the atomic state cell. Created once per
    /// Hub process by `create`. All mutator helpers below take a `T`
    /// and return either the resulting `SubmitOutcome` (no payload) or
    /// a compound `SubmitOutcome * <payload>` for read-after-write
    /// queries like `toggleOverlay`.
    type T

    /// Allocate a new store seeded with `initial`. The supplied
    /// `IHubEventSink` is retained for the lifetime of the store and
    /// receives every `HubEvent` triggered by a successful mutator
    /// call.
    val create: events: HubEvents.IHubEventSink -> initial: HubState -> T

    /// Read the current aggregate state. Safe from any thread;
    /// non-blocking (a single volatile read of the cell).
    val current: T -> HubState

    /// Set the active Hub tab. Always succeeds (every `HubTab` case is
    /// valid); emits `HubEvent.ActiveTabChanged`.
    val setActiveTab: T -> HubTab -> SubmitOutcome

    /// Set the Viewer camera. Validates via `ViewerCamera.validate`
    /// (finite components, `Scale ∈ [0.05, 100.0]`); rejection emits
    /// no event.
    val setCamera: T -> ViewerCamera -> SubmitOutcome

    /// Replace the Setup-tab lobby snapshot. Emits `HubEvent.LobbyChanged`.
    /// Callers gate the Lobby-editable precondition externally (via
    /// `SessionManager.IsLobbyEditable`) — this mutator does not re-check.
    val setLobby: T -> LobbyConfig.LobbyConfig -> SubmitOutcome

    /// Replace the entire `VizConfig` (e.g. a preset load or a wire
    /// `SetVizConfig` call). Emits `HubEvent.VizConfigChanged` once.
    val setVizConfig: T -> VizConfig -> SubmitOutcome

    /// Apply a single-attribute change via a `ConfigDescriptors` key.
    /// Emits `HubEvent.VizAttributeChanged` with old + new values when
    /// the key resolves. Returns `Rejected "unknown key: ..."` for
    /// unknown keys.
    val setVizAttribute:
        T -> key: string -> value: AttributeValue -> SubmitOutcome

    /// Flip / set an overlay toggle. Returns both the submit outcome
    /// and the new effective state; emits `HubEvent.VizAttributeChanged`
    /// with the corresponding descriptor key when the mutation applies.
    val toggleOverlay:
        T -> key: OverlayKind -> target: ToggleTarget -> SubmitOutcome * bool

    /// Replace encyclopedia filter / selection. Emits
    /// `HubEvent.EncyclopediaSelectionChanged`.
    val setEncyclopedia: T -> EncyclopediaSelection -> SubmitOutcome

    /// Replace the cached preset-name list. Facade-only (no validation).
    /// Does NOT emit a `HubEvent` — the `PresetSaved` / `PresetDeleted`
    /// events from the preset facade already convey the change; this
    /// helper is the store-side cache refresh path.
    val updatePresetList: T -> string list -> unit

    /// Replace the in-memory `HubSettings` snapshot. Emits
    /// `HubEvent.HubSettingsChanged`. Does not persist — callers
    /// combine this with `HubSettings.save`.
    val setSettings: T -> HubSettings.HubSettings -> SubmitOutcome
