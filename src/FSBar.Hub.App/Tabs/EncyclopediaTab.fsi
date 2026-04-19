namespace FSBar.Hub.App.Tabs

open SkiaViewer
open Silk.NET.Input
open FSBar.Viz

/// Encyclopedia tab (feature 035-central-gui-hub T055 + T056, extended
/// by feature 044-encyclopedia-filters). Standalone browser of every
/// unit in `BarData.AllUnitDefs`. Renders a filterable scrollable list
/// on the left and a detail pane on the right, driven entirely by
/// `HubState.Encyclopedia` through `HubStateStore.setEncyclopedia`
/// (feature 041 state-routing convention, feature 044 FR-017..FR-022).
///
/// Feature 044 scope: adds Tier + Mobility chip rows, a free-text
/// search input, a "N of M units shown" count, a Clear-filters
/// button, and an empty-state panel when the active filter matches
/// nothing. All mutations still route through
/// `HubStateStore.setEncyclopedia`; no per-tab mutable mirror of the
/// filter state is introduced.
module EncyclopediaTab =

    /// Pre-computed display record for one unit. Alias preserved for
    /// external callers and Hub script consumers.
    type UnitEntry = EncyclopediaData.EncyclopediaEntry

    /// Actions the tab surfaces on input. Filter / selection
    /// mutations route through `HubStateStore.setEncyclopedia` inside
    /// `handleMouse` / `handleKey`; only genuinely transient view
    /// state (scroll) bubbles up to the entrypoint.
    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        /// User scrolled the list.
        | ScrollList of offset: float32

    /// Per-tab render state. Holds only view-local fields that are
    /// not authoritatively owned by `HubState.Encyclopedia`:
    /// pre-built entry list, scroll offset, and whether the search
    /// input currently has keyboard focus.
    type EncyclopediaTabState = {
        /// All entries sorted alphabetically by InternalName.
        Entries: UnitEntry list
        /// Scroll offset into the visible list (pixels).
        ListScroll: float32
        /// `true` when keystrokes should edit the search field.
        SearchFocused: bool
    }

    /// Build the initial tab state. Synthesises `Entries` from
    /// `BarData.AllUnitDefs` (~10–20 ms).
    val init: unit -> EncyclopediaTabState

    /// Render the tab content into the given content rectangle.
    val render:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Hit-test a mouse click. Chip / Clear-filters / search-focus /
    /// selection mutations are written through
    /// `HubStateStore.setEncyclopedia` before returning. The tab
    /// maintains selection stickiness (feature 044 FR-011) in the
    /// same submit that changes the filter. Returns the updated tab
    /// state (for focus changes) and an optional bubbled action.
    val handleMouse:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            EncyclopediaTabState * EncyclopediaTabAction option

    /// Handle a wheel scroll on the list pane.
    val handleScroll:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        delta: float32 ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            EncyclopediaTabAction option

    /// Route a `KeyDown` event through the tab. When the search
    /// field is focused, alphanumeric keys append, Backspace pops
    /// the last character, Escape clears the search text, and all
    /// other keys fall through. Returns `(state, true)` when the
    /// key was consumed so the caller skips the global
    /// overlay-toggle path.
    val handleKey:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        key: Key ->
            EncyclopediaTabState * bool
