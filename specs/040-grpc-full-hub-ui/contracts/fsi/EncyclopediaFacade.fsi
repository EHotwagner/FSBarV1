// Sketch for src/FSBar.Hub/EncyclopediaFacade.fsi
// Feature 040 — hub-side wrapper over FSBar.Viz.EncyclopediaData. Caches
// entries at Hub startup. Exposes filter and select operations; select
// routes through HubStateStore.

namespace FSBar.Hub

open FSBar.Viz

module EncyclopediaFacade =
    type T

    val create:
        store: HubStateStore.T ->
        entries: EncyclopediaData.EncyclopediaEntry list ->
        T

    val listEntries:
        T ->
        factionFilter: Set<HubStateStore.FactionFilterKey> ->
        EncyclopediaData.EncyclopediaEntry list

    val getByDefId:
        T -> id: int -> EncyclopediaData.EncyclopediaEntry option

    val getByName:
        T -> internalName: string -> EncyclopediaData.EncyclopediaEntry option

    /// Update HubStateStore.Encyclopedia; emits EncyclopediaSelectionChanged.
    val select:
        T -> HubStateStore.EncyclopediaSelection -> HubStateStore.SubmitOutcome
