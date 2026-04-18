namespace FSBar.Hub.App.Tabs

open SkiaViewer
open FSBar.Viz

/// Encyclopedia tab (feature 035-central-gui-hub T055 + T056) — a
/// standalone browser of every unit in `BarData.AllUnitDefs`. Renders
/// a filterable scrollable list on the left and a detail pane with
/// the same `UnitGlyph.buildUnit` output the live viewer uses — so
/// the glyph in the card byte-matches what the same DefId would draw
/// in a session (SC-003).
///
/// Phase-3 scope: faction chip filter + alphabetical list + detail
/// card (cost / health / build time / weapons / sight range / glyph
/// preview). Role / tier filters and a BarData-version-change watcher
/// (FR-022) are deferred.
module EncyclopediaTab =

    /// Pre-computed display record for one unit. Feature 038 moved
    /// the underlying record to `FSBar.Viz.EncyclopediaData.EncyclopediaEntry`
    /// so that `UnitDisplayAdapter.ofEncyclopediaEntry` can share it —
    /// `UnitEntry` here is a simple alias preserved for external
    /// callers and Hub script consumers.
    type UnitEntry = EncyclopediaData.EncyclopediaEntry

    /// Actions the tab surfaces on mouse input. Faction-filter +
    /// selection mutations route through `HubStateStore.setEncyclopedia`
    /// inside `handleMouse` (feature 041 FR-019/FR-020); scroll
    /// position is genuinely transient view state and bubbles up
    /// here for the entrypoint to apply.
    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        /// User scrolled the list.
        | ScrollList of offset: float32

    /// Per-tab render state. Excludes any field already
    /// authoritatively held by `HubStateStore.HubState.Encyclopedia` (R6).
    type EncyclopediaTabState = {
        /// All entries sorted alphabetically by InternalName.
        Entries: UnitEntry list
        /// Scroll offset into the visible list (pixels).
        ListScroll: float32
    }

    /// Build the initial tab state. Synthesises `Entries` from
    /// `BarData.AllUnitDefs`; takes ~10–20 ms on a typical dev box
    /// — fast enough to run at hub startup.
    val init: unit -> EncyclopediaTabState

    /// Render the tab content into the given content rectangle.
    /// Reads `EncyclopediaSelection` and `UnitGlyphStyle` from the
    /// supplied `HubStateStore.T` so remote gRPC writes
    /// (`SelectUnit`, `SetVizAttribute`) appear in the next paint
    /// without going through the entrypoint.
    val render:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Hit-test a mouse click. Faction-filter + selection mutations
    /// are written back through `HubStateStore.setEncyclopedia`
    /// before returning. Scroll-bar action bubbles up via
    /// `EncyclopediaTabAction.ScrollList`.
    val handleMouse:
        state: EncyclopediaTabState ->
        store: FSBar.Hub.HubStateStore.T ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            EncyclopediaTabAction option

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
