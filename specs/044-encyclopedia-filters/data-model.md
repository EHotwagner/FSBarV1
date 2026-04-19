# Data Model — Unit Encyclopedia Filters (044)

## Types (extended / new in `FSBar.Hub.HubUiTypes`)

```fsharp
// Existing (unchanged shape, only listed for context)
type FactionFilterKey =
    | Armada | Cortex | Legion | Raptors | Scavengers | Neutral

// NEW
type TierFilterKey =
    | T1 | T2 | T3 | Commander

type MobilityFilterKey =
    | Building | Ground | Hover | Ship | Air | Amphib

// EXTENDED — adds TierFilter, MobilityFilter, SearchText
type EncyclopediaSelection = {
    FactionFilter  : Set<FactionFilterKey>
    TierFilter     : Set<TierFilterKey>
    MobilityFilter : Set<MobilityFilterKey>
    SearchText     : string
    SelectedDefId  : int option
}
```

### Invariants

| Field | Invariant | Enforced by |
|-------|-----------|-------------|
| `FactionFilter` / `TierFilter` / `MobilityFilter` | Any subset of its DU cases is valid (including empty = "all pass"). | Type system. |
| `SearchText` | Stored trimmed; length ≤ 128 UTF-16 code units. | `HubStateStore.setEncyclopedia`. |
| `SelectedDefId` | `None` OR references an entry that matches the active filter. | `EncyclopediaTab` (store is oblivious). |
| Faction chip label `"Neutral"` | Surface label for `FactionFilterKey.Neutral`; covers unclassified / environment factions (no separate `Unknown` case). | `EncyclopediaTab` render code. |

### Defaults

`EncyclopediaSelection.defaults`:

```fsharp
{ FactionFilter  = Set.empty
  TierFilter     = Set.empty
  MobilityFilter = Set.empty
  SearchText     = ""
  SelectedDefId  = None }
```

## Pure filter module (`FSBar.Hub.EncyclopediaFilter`)

Signature summary (full `.fsi` is the authoritative contract):

```fsharp
module EncyclopediaFilter =
    val matches  : EncyclopediaSelection -> EncyclopediaData.EncyclopediaEntry -> bool
    val apply    : EncyclopediaSelection -> EncyclopediaData.EncyclopediaEntry list -> EncyclopediaData.EncyclopediaEntry list
    val humanName: EncyclopediaData.EncyclopediaEntry -> string
    val toTierKey: Tier -> TierFilterKey option
    val toMobilityKey: MovementShape -> MobilityFilterKey option
```

### Predicate shape

```text
matches selection entry :=
    factionOk && tierOk && mobilityOk && searchOk
where
    factionOk  = selection.FactionFilter.IsEmpty  || Set.contains entry.Faction (as-faction selection)
    tierOk     = selection.TierFilter.IsEmpty     || Set.contains (toTierKey entry.Tier) selection.TierFilter
    mobilityOk = selection.MobilityFilter.IsEmpty || Set.contains (toMobilityKey entry.Shape) selection.MobilityFilter
    searchOk   = selection.SearchText = ""        ||
                 contains(entry.InternalName, selection.SearchText, ordinalIgnoreCase) ||
                 contains(label(entry),       selection.SearchText, ordinalIgnoreCase) ||
                 contains(humanName entry,    selection.SearchText, ordinalIgnoreCase)
```

`toTierKey` / `toMobilityKey` return `None` for viz-only enum cases
that the Hub chooses not to expose (e.g. an unknown tier). Those
entries are excluded whenever the corresponding category has any chip
active — consistent with the Edge Case "missing tag excluded when a
specific chip is active".

## Events

No new `HubEvent` cases — mutations reuse
`HubEvent.EncyclopediaSelectionChanged` which already carries the full
`EncyclopediaSelection` snapshot.

## Persistence

None. `EncyclopediaSelection` lives in `HubState` for the process
lifetime and resets to `defaults` on Hub relaunch (FR-008).
