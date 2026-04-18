namespace FSBar.Hub

open System
open FSBar.Viz

module EncyclopediaFilter =

    let toTierKey (tier: Tier) : TierFilterKey option =
        match tier with
        | Tier.T1 -> Some TierFilterKey.T1
        | Tier.T2 -> Some TierFilterKey.T2
        | Tier.T3 -> Some TierFilterKey.T3

    let toMobilityKey (shape: MovementShape) : MobilityFilterKey option =
        match shape with
        | MovementShape.Building -> Some MobilityFilterKey.Building
        | MovementShape.Bot -> Some MobilityFilterKey.Ground
        | MovementShape.Vehicle -> Some MobilityFilterKey.Ground
        | MovementShape.Hover -> Some MobilityFilterKey.Hover
        | MovementShape.Ship -> Some MobilityFilterKey.Ship
        | MovementShape.Air -> Some MobilityFilterKey.Air
        | MovementShape.Unknown -> None

    // BAR amphibious movement classes start with "A" (e.g. ATANK, ABOT,
    // AHOVER, ABOAT). Other classes like "TANK" / "BOT" / "BOAT" are
    // non-amphib. `movementClass` is `None` for buildings and air.
    let private isAmphibiousMovementClass (mc: string) : bool =
        if String.IsNullOrEmpty mc then false
        else
            let up = mc.ToUpperInvariant()
            up.StartsWith "ATANK"
            || up.StartsWith "ABOT"
            || up.StartsWith "AKBOT"
            || up.StartsWith "AHOVER"
            || up.StartsWith "ABOAT"
            || up.StartsWith "AUBOAT"
            || up.StartsWith "AMPH"

    let private isAmphibious (entry: EncyclopediaData.EncyclopediaEntry) : bool =
        match entry.MovementClass with
        | Some mc -> isAmphibiousMovementClass mc
        | None -> false

    let private factionIdToKey (f: FactionId) : FactionFilterKey =
        match f with
        | FactionId.Armada -> FactionFilterKey.Armada
        | FactionId.Cortex -> FactionFilterKey.Cortex
        | FactionId.Legion -> FactionFilterKey.Legion
        | FactionId.Raptors -> FactionFilterKey.Raptors
        | FactionId.Scavengers -> FactionFilterKey.Scavengers
        | FactionId.Neutral -> FactionFilterKey.Neutral

    // Commander detection: BAR commanders are the faction "com" units
    // (armcom, corcom, legcom, armcom_decoy, etc. — any InternalName
    // that, after the faction prefix, contains "com"). Since these
    // also have `Tier.T1`, we surface them as a separate chip so users
    // can narrow to/away from commanders.
    let private isCommander (entry: EncyclopediaData.EncyclopediaEntry) : bool =
        let n = entry.InternalName.ToLowerInvariant()
        n = "armcom" || n = "corcom" || n = "legcom"
        || n.EndsWith "commander"
        || n.Contains "_com_"

    let humanName (entry: EncyclopediaData.EncyclopediaEntry) : string =
        // The encyclopedia tab currently renders `InternalName`
        // prominently. Mirror that so search over "human name"
        // matches what the user sees.
        entry.InternalName

    let private containsCI (haystack: string) (needle: string) : bool =
        if String.IsNullOrEmpty haystack then false
        else haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0

    let private glyphLabel (entry: EncyclopediaData.EncyclopediaEntry) : string =
        // UnitLabels.generated doesn't expose a direct lookup by
        // InternalName in a compilation-order-safe way here; the
        // InternalName-based search below already covers the primary
        // case. Keep this hook for future label-table wiring.
        entry.InternalName

    let matches
            (selection: EncyclopediaSelection)
            (entry: EncyclopediaData.EncyclopediaEntry) : bool =
        let factionOk =
            Set.isEmpty selection.FactionFilter
            || Set.contains (factionIdToKey entry.Faction) selection.FactionFilter

        let tierOk =
            if Set.isEmpty selection.TierFilter then true
            else
                let baseTier =
                    match toTierKey entry.Tier with
                    | Some k -> Set.contains k selection.TierFilter
                    | None -> false
                let commanderHit =
                    Set.contains TierFilterKey.Commander selection.TierFilter
                    && isCommander entry
                baseTier || commanderHit

        let mobilityOk =
            if Set.isEmpty selection.MobilityFilter then true
            else
                let baseShape =
                    match toMobilityKey entry.Shape with
                    | Some k -> Set.contains k selection.MobilityFilter
                    | None -> false
                let amphibHit =
                    Set.contains MobilityFilterKey.Amphib selection.MobilityFilter
                    && isAmphibious entry
                baseShape || amphibHit

        let searchOk =
            if String.IsNullOrEmpty selection.SearchText then true
            else
                let needle = selection.SearchText
                containsCI entry.InternalName needle
                || containsCI (glyphLabel entry) needle
                || containsCI (humanName entry) needle

        factionOk && tierOk && mobilityOk && searchOk

    let apply
            (selection: EncyclopediaSelection)
            (entries: EncyclopediaData.EncyclopediaEntry list)
            : EncyclopediaData.EncyclopediaEntry list =
        entries |> List.filter (matches selection)

    let defaultSelection: EncyclopediaSelection = EncyclopediaSelection.defaults
