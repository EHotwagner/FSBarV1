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

/// Encyclopedia tab filter + selection combined. `SelectedDefId = None`
/// means no unit is pinned.
type EncyclopediaSelection =
    { FactionFilter: Set<FactionFilterKey>
      SelectedDefId: int option }

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
