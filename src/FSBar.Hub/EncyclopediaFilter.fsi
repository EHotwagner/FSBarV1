namespace FSBar.Hub

open FSBar.Viz

/// Pure predicate + helpers for the Units-tab encyclopedia filter
/// (feature 044). Stateless — one source of truth for both render
/// and tests.
module EncyclopediaFilter =

    /// `true` iff `entry` passes every active category in
    /// `selection` AND the search text. Empty category = pass-all.
    val matches:
        selection: EncyclopediaSelection ->
        entry: EncyclopediaData.EncyclopediaEntry ->
            bool

    /// `entries |> List.filter (matches selection)` (order-preserving).
    val apply:
        selection: EncyclopediaSelection ->
        entries: EncyclopediaData.EncyclopediaEntry list ->
            EncyclopediaData.EncyclopediaEntry list

    /// Canonical display-name derivation used by both the tab detail
    /// pane and the search-substring test.
    val humanName:
        entry: EncyclopediaData.EncyclopediaEntry ->
            string

    /// Map a viz-internal `Tier` into the Hub-UI chip key. `None` for
    /// values the Hub chooses not to expose as a chip.
    val toTierKey: Tier -> TierFilterKey option

    /// Map a viz-internal `MovementShape` into the Hub-UI chip key.
    /// `None` for values the Hub chooses not to expose (e.g. `Unknown`).
    val toMobilityKey: MovementShape -> MobilityFilterKey option

    /// The canonical "no filters, no search" snapshot. Alias for
    /// `EncyclopediaSelection.defaults`.
    val defaultSelection: EncyclopediaSelection
