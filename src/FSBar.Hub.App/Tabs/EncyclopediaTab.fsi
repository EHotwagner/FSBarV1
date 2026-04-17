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

    /// Pre-computed display record for one unit. Built once at tab
    /// construction from `BarData.AllUnitDefs`. Heavy fields —
    /// weapon-range list, glyph classification — land on this record
    /// to keep per-frame rendering cheap.
    type UnitEntry = {
        DefId: int
        InternalName: string
        DisplayName: string
        Subfolder: string
        Faction: FactionId
        Tier: Tier
        Shape: MovementShape
        MetalCost: int
        EnergyCost: int
        Health: int
        BuildTime: int
        SightRangeElmo: float32
        WeaponRangesElmo: float32 list
        FootprintX: int
        FootprintZ: int
    }

    /// Actions the tab surfaces on mouse input.
    [<RequireQualifiedAccess>]
    type EncyclopediaTabAction =
        /// User toggled a faction filter chip.
        | ToggleFaction of faction: FactionId
        /// User clicked a unit row. Payload is the unit's DefId.
        | SelectUnit of defId: int
        /// User scrolled the list.
        | ScrollList of offset: float32

    /// Per-tab render state.
    type EncyclopediaTabState = {
        /// All entries sorted alphabetically by InternalName.
        Entries: UnitEntry list
        /// Active faction filter. Empty set = show all.
        FactionFilter: Set<FactionId>
        /// Currently-selected unit's DefId. `None` when nothing is
        /// selected; the detail pane shows an instruction instead.
        Selected: int option
        /// Scroll offset into the visible list (pixels).
        ListScroll: float32
    }

    /// Build the initial tab state. Synthesises `Entries` from
    /// `BarData.AllUnitDefs`; takes ~10–20 ms on a typical dev box
    /// — fast enough to run at hub startup.
    val init: unit -> EncyclopediaTabState

    /// Render the tab content into the given content rectangle.
    val render:
        state: EncyclopediaTabState ->
        style: UnitGlyphStyle ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            Element list

    /// Hit-test a mouse click.
    val handleMouse:
        state: EncyclopediaTabState ->
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
        delta: float32 ->
        x: float32 ->
        y: float32 ->
        contentX: float32 ->
        contentY: float32 ->
        contentW: float32 ->
        contentH: float32 ->
            EncyclopediaTabAction option
