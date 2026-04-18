namespace FSBar.Hub

/// Hub-UI-facing data types referenced by both the event bus
/// (`HubEvents`) and the authoritative state store (`HubStateStore`).
///
/// Split out from `HubStateStore` because `HubEvents` carries these
/// values in its event payloads (so it must compile *before*
/// `HubStateStore`), while `HubStateStore` itself depends on
/// `HubEvents.IHubEventSink` to publish mutations. Without this
/// split the two modules form a cycle.
///
/// No module or behaviour lives here — only the shapes.

/// Which top-level tab is currently shown in the Hub GUI.
type HubTab =
    | Setup
    | Viewer
    | Units
    | Style
    | Cfg
    | Grpc

/// Viewer-tab camera state. Pan + zoom are expressed as an origin in
/// world coordinates and a scale factor. `AutoFit = true` makes the
/// Viewer tab re-fit the scene to the viewport on each redraw; the
/// Viewer UI flips it to `false` the first time the user manually
/// pans or zooms.
type ViewerCamera =
    { Scale: float32
      OriginX: float32
      OriginY: float32
      AutoFit: bool }

/// Faction groups used for filtering the Units-tab encyclopedia.
/// Membership is set-valued (empty = "all factions").
type FactionFilterKey =
    | Armada
    | Cortex
    | Legion
    | Raptors
    | Scavengers
    | Neutral

/// Tier chip keys for the encyclopedia filter. Empty set = "all tiers".
type TierFilterKey =
    | T1
    | T2
    | T3
    | Commander

/// Mobility chip keys for the encyclopedia filter. Empty set = "all mobilities".
type MobilityFilterKey =
    | Building
    | Ground
    | Hover
    | Ship
    | Air
    | Amphib

/// Encyclopedia tab filter + selection combined. All filter sets are
/// empty = "pass all" by convention; `SearchText` is trimmed and
/// length-capped at 128 chars by `HubStateStore.setEncyclopedia`.
/// Session-scoped: lives on `HubState` for the Hub process lifetime
/// and is NOT persisted to disk (feature 044 FR-008).
type EncyclopediaSelection =
    { FactionFilter: Set<FactionFilterKey>
      TierFilter: Set<TierFilterKey>
      MobilityFilter: Set<MobilityFilterKey>
      SearchText: string
      SelectedDefId: int option }

module EncyclopediaSelection =
    /// The canonical "no filters, no search, no pinned unit" snapshot.
    val defaults: EncyclopediaSelection

/// Return type for every mutating operation on the state stores.
/// Mirrors the gRPC `MutationResult` wire shape.
type SubmitOutcome =
    | Sent
    | Rejected of reason: string

/// Explicit on/off/flip request for a toggle-style mutation.
type ToggleTarget =
    | Toggle
    | On
    | Off

module ViewerCamera =
    /// Factory value: identity scale, origin at world origin, auto-fit enabled.
    val defaults: ViewerCamera

    /// Finite + range-validated copy of a camera snapshot.
    /// Rejects NaN / ±∞ components and any `Scale` outside `[0.05, 100.0]`.
    val validate: camera: ViewerCamera -> Result<ViewerCamera, string>
