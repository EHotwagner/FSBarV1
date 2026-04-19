# Contract — `FSBar.Hub.EncyclopediaFilter`

In-process F# contract. No on-wire surface.

## Module

`FSBar.Hub.EncyclopediaFilter` — pure, stateless. Lives under
`src/FSBar.Hub/EncyclopediaFilter.fs` with a curated `.fsi`. Consumed by
`FSBar.Hub.App.Tabs.EncyclopediaTab` for rendering and by
`tests/FSBar.Hub.Tests/EncyclopediaFilterTests.fs` for unit coverage.

## Public API (`.fsi` authoritative)

```fsharp
namespace FSBar.Hub

open FSBar.Viz

module EncyclopediaFilter =

    /// Returns `true` iff `entry` satisfies every active category in
    /// `selection` AND the search text (case-insensitive substring of
    /// InternalName, glyph label, or human-readable name). Empty
    /// category = "all pass".
    val matches:
        selection: EncyclopediaSelection ->
        entry:     EncyclopediaData.EncyclopediaEntry ->
            bool

    /// `entries |> List.filter (matches selection)`. Preserves input
    /// ordering.
    val apply:
        selection: EncyclopediaSelection ->
        entries:   EncyclopediaData.EncyclopediaEntry list ->
            EncyclopediaData.EncyclopediaEntry list

    /// Display-name derivation used by both the tab detail pane and
    /// the search-substring test. Canonical so the tab and the
    /// predicate can never disagree.
    val humanName:
        entry: EncyclopediaData.EncyclopediaEntry ->
            string

    /// Map a viz-internal `Tier` / `MovementShape` into the
    /// Hub-UI-visible chip key. Returns `None` for cases the Hub
    /// chooses not to expose (e.g. `Tier.Unknown`).
    val toTierKey:     Tier          -> TierFilterKey     option
    val toMobilityKey: MovementShape -> MobilityFilterKey option

    /// Convenience: the canonical "no filters, no search" snapshot.
    val defaultSelection: EncyclopediaSelection
```

## Semantics

- **Within-category OR**: `Set.contains` over the active set.
- **Across-category AND**: `&&` of per-category predicates.
- **Empty category**: short-circuits to `true` without iterating.
- **Search**: trimmed at the store boundary; the predicate assumes it
  already arrived trimmed.

## Invariants callers must uphold

1. `EncyclopediaSelection.SearchText` is trimmed and length ≤ 128.
   (`HubStateStore.setEncyclopedia` is the enforcement point.)
2. `SelectedDefId`, if `Some`, must reference an entry returned by
   `apply` with the same `selection`. The filter module does NOT
   re-validate this; callers (the tab) reconcile after filter changes
   per FR-011.

## Non-goals

- No IObservable / event surface — this module is pure.
- No gRPC / proto projection — selection changes already flow through
  the existing `HubEvent.EncyclopediaSelectionChanged` stream.
