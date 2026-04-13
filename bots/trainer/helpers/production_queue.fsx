// bots/trainer/helpers/production_queue.fsx — factory production-queue keeper
//
// FR link: FR-005, FR-006, FR-008
// Contract: specs/023-trainer-builder-economy/contracts/helpers.md §2
//
// Extraction provenance (two organic sites):
//   - Site 1: bot_macro.fsx iter 007 (commit de138b3) — inlined the
//     factory queue top-up + factoryUnitId capture + FR-008 gate.
//   - Site 2: bot_macro.fsx iter 008 (commit 734515e) — kept the same
//     queue top-up while adding a one-shot GuardCommand at the
//     Opening→Production transition; the queue code itself was
//     unchanged, so iter 008 is the second organic occurrence.
//
// prelude + log must be #loaded before this file.

open FSBar.Client
open FSBar.Client.Commands

printfn "[trainer] production_queue.fsx loaded"

/// Role classification for queue items. Used by the ratio policy.
type UnitRole =
    | Constructor
    | Combat

/// One item in the factory's symbolic queue policy.
type QueueItem = {
    Role: UnitRole
    DefName: string
}

/// The symbolic policy. Resolved against UnitDefCache at warmup.
type QueuePolicy = {
    FactoryDefName: string
    Items: QueueItem list
    MinQueueDepth: int
    TargetConstructorRatio: float32
    MinCombatIncomeThreshold: float32
}

/// Resolved policy with concrete defIds attached.
type ResolvedQueuePolicy = {
    FactoryDefId: int
    ResolvedItems: (int * QueueItem) array
    MinQueueDepth: int
    TargetConstructorRatio: float32
    MinCombatIncomeThreshold: float32
}

/// Runtime state tracked across frames.
type QueueState = {
    FactoryUnitId: int option
    AskedCounts: Map<int, int>     // defId → submitted count
    ObservedBuilt: Map<int, int>   // defId → UnitFinished count
    LastRefillFrame: uint32
    FactoryIdleSinceFrame: uint32 option
}

let emptyQueueState : QueueState = {
    FactoryUnitId = None
    AskedCounts = Map.empty
    ObservedBuilt = Map.empty
    LastRefillFrame = 0u
    FactoryIdleSinceFrame = None
}

/// Default policy for an Armada t1 kbot lab. Iterations may grow the
/// items list. The factory name must match what the opening plan
/// actually finishes — otherwise factoryUnitId will never populate.
let defaultArmadaKbotPolicy : QueuePolicy = {
    FactoryDefName = "armlab"
    Items = [
        { Role = Constructor; DefName = "armck" }    // t1 construction kbot
        { Role = Combat;      DefName = "armpw" }    // Peewee — light combat kbot
    ]
    MinQueueDepth = 3
    TargetConstructorRatio = 0.4f
    MinCombatIncomeThreshold = 10.0f
}

/// Resolve a symbolic policy against UnitDefCache. Fails fast on an
/// unresolved name so misconfigurations surface at bot start.
let resolveQueuePolicy (cache: UnitDefCache) (policy: QueuePolicy) : ResolvedQueuePolicy =
    let factoryDefId =
        match UnitDefCache.tryFindByName cache policy.FactoryDefName with
        | Some info -> info.DefId
        | None -> failwithf "[production_queue] unresolved factory def '%s'" policy.FactoryDefName
    let resolved =
        policy.Items
        |> List.map (fun item ->
            match UnitDefCache.tryFindByName cache item.DefName with
            | Some info -> (info.DefId, item)
            | None -> failwithf "[production_queue] unresolved queue item '%s'" item.DefName)
        |> List.toArray
    {
        FactoryDefId = factoryDefId
        ResolvedItems = resolved
        MinQueueDepth = policy.MinQueueDepth
        TargetConstructorRatio = policy.TargetConstructorRatio
        MinCombatIncomeThreshold = policy.MinCombatIncomeThreshold
    }

/// Pure: compute observed queue depth (asked minus built, summed).
let queueDepth (state: QueueState) : int =
    let asked = state.AskedCounts |> Map.toSeq |> Seq.sumBy snd
    let built = state.ObservedBuilt |> Map.toSeq |> Seq.sumBy snd
    asked - built

/// Pure: pick the next defId to queue based on FR-008 gate + FR-006 ratio.
/// Returns None if no constructor item is present in the policy.
let pickNextQueueDef
    (resolved: ResolvedQueuePolicy)
    (state: QueueState)
    (gs: GameState)
    : int option =
    let constructorEntry =
        resolved.ResolvedItems
        |> Array.tryFind (fun (_, item) -> item.Role = Constructor)
    let combatEntry =
        resolved.ResolvedItems
        |> Array.tryFind (fun (_, item) -> item.Role = Combat)
    match constructorEntry with
    | None -> None
    | Some (constructorDefId, _) ->
        if gs.Metal.Income < resolved.MinCombatIncomeThreshold then
            // FR-008: constructor-only when metal starves.
            Some constructorDefId
        else
            let totalBuilt =
                state.ObservedBuilt |> Map.toSeq |> Seq.sumBy snd
            let constructorBuilt =
                Map.tryFind constructorDefId state.ObservedBuilt
                |> Option.defaultValue 0
            let ratio =
                if totalBuilt = 0 then 0.0f
                else float32 constructorBuilt / float32 totalBuilt
            if ratio < resolved.TargetConstructorRatio then Some constructorDefId
            else
                match combatEntry with
                | Some (combatDefId, _) -> Some combatDefId
                | None -> Some constructorDefId

/// Compute the commands to issue this frame plus the updated queue
/// state. Empty list when the queue is at depth or the factory is not
/// yet captured. Caller passes `currentFrame` for refill-frame
/// bookkeeping (unused at iter 8 — retained for the contract shape).
let computeQueueTopUp
    (resolved: ResolvedQueuePolicy)
    (state: QueueState)
    (gs: GameState)
    (currentFrame: uint32)
    : Highbar.AICommand list * QueueState =
    match state.FactoryUnitId with
    | None -> [], state
    | Some fid ->
        let depth = queueDepth state
        if depth >= resolved.MinQueueDepth then [], state
        else
            let needed = resolved.MinQueueDepth - depth
            let mutable st = state
            let cmds =
                [ for _ in 1 .. needed do
                    match pickNextQueueDef resolved st gs with
                    | Some defId ->
                        st <- { st with
                                  AskedCounts =
                                      Map.change defId
                                          (fun o -> Some ((Option.defaultValue 0 o) + 1))
                                          st.AskedCounts
                                  LastRefillFrame = currentFrame }
                        yield BuildCommand fid defId 0.0f 0.0f 0.0f 0
                    | None -> () ]
            cmds, st

/// Update queue state from a frame's events. Pure over inputs.
/// - UnitFinished for factoryDefId → capture FactoryUnitId if None
/// - UnitFinished for any ResolvedItems def → increment ObservedBuilt
let observeFrame
    (resolved: ResolvedQueuePolicy)
    (state: QueueState)
    (events: GameEvent list)
    (gs: GameState)
    : QueueState =
    let builtDefIdSet =
        resolved.ResolvedItems
        |> Array.map fst
        |> Set.ofArray
    let mutable st = state
    for ev in events do
        match ev with
        | GameEvent.UnitFinished uid ->
            match Map.tryFind uid gs.Units with
            | Some u ->
                if u.DefId = resolved.FactoryDefId && st.FactoryUnitId.IsNone then
                    st <- { st with FactoryUnitId = Some uid }
                if Set.contains u.DefId builtDefIdSet then
                    st <- { st with
                              ObservedBuilt =
                                  Map.change u.DefId
                                      (fun o -> Some ((Option.defaultValue 0 o) + 1))
                                      st.ObservedBuilt }
            | None -> ()
        | _ -> ()
    st

/// Return the frame at which the factory first became idle, or None
/// when producing. (Currently returns state.FactoryIdleSinceFrame
/// unchanged — iter 8 did not need to compute idle frames directly
/// because the queue top-up kept it busy. Reserved for FR-006 defect
/// telemetry once US2 adds idle detection.)
let factoryIdleSince (state: QueueState) (_currentFrame: uint32) : uint32 option =
    state.FactoryIdleSinceFrame
