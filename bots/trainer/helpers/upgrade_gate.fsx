// bots/trainer/helpers/upgrade_gate.fsx — upgrade-gate predicates
//
// FR link: FR-009, FR-010, FR-011, FR-012
// Contract: specs/023-trainer-builder-economy/contracts/helpers.md §4
//
// Extraction provenance (two organic sites):
//   - Site 1: bot_macro.fsx iter 013 (commit 23b5103) — inlined the
//     upgrade gate (tunables, Production→Upgrade entry predicate,
//     stall path, FR-012 deadline handling).
//   - Site 2: bot_macro.fsx iter 021 (commit 8e7967c) — kept the
//     same predicates while fixing the armalab build dispatch
//     (commander guards armck). Gate logic itself was stable across
//     iters 013-021, satisfying the two-site rule.
//
// prelude + log must be #loaded before this file.

open FSBar.Client

printfn "[trainer] upgrade_gate.fsx loaded"

/// Named advanced-tech predicates. Only AdvancedFactory is used in
/// the iter-21 baseline, but the DU is kept wide per contract §4 so
/// later iterations can add AdvancedConstructor / AdvancedCombatUnit
/// without churning the signature.
type UpgradePredicateName =
    | AdvancedFactory
    | AdvancedConstructor
    | AdvancedCombatUnit

/// Exit decision path when the gate fires AttackNow.
type UpgradeAttackPath =
    | Normal
    | DeadlineFallback

/// Exit decision record returned by decideUpgradeExit.
type UpgradeExitDecision =
    | AttackNow of path:UpgradeAttackPath
    | StallAndLose of reason:string
    | WaitLonger

/// Thresholds the caller plugs in. Held outside the state so they
/// survive bot restarts without being rehydrated.
type UpgradeThresholds = {
    MetalIncome: float32
    InitialProductionCount: int
    DeadlineFrame: uint32
    CombatUnitThreshold: int
}

/// Runtime gate state. Held by the bot as a mutable binding.
type UpgradeGateState = {
    Reached: UpgradePredicateName option
    ReachedFrame: uint32 option
    StallRecorded: bool
}

let emptyUpgradeGateState : UpgradeGateState = {
    Reached = None
    ReachedFrame = None
    StallRecorded = false
}

/// Entry predicate — FR-009. Production is "ready" when metal income
/// and factory-built count both cross their thresholds.
let entryPredicateMet
    (gs: GameState)
    (productionCount: int)
    (thresholds: UpgradeThresholds)
    : bool =
    gs.Metal.Income >= thresholds.MetalIncome
    && productionCount >= thresholds.InitialProductionCount

/// First-wins advanced predicate check — FR-010. Returns Some name on
/// first true, None otherwise. The caller supplies the resolved defIds
/// for each advanced-tech class; an `-1` sentinel means "not tracked".
let upgradeReached
    (gs: GameState)
    (advancedFactoryDefId: int)
    (_advancedConstructorDefId: int)
    (_advancedCombatDefId: int)
    : UpgradePredicateName option =
    // iter 021 baseline: only AdvancedFactory is wired. The caller
    // detects armalab UnitFinished and calls markReached directly.
    // This predicate stays in the helper for contract compatibility.
    if advancedFactoryDefId > 0 then
        let hasAdvancedFactory =
            gs.Units
            |> Map.toSeq
            |> Seq.exists (fun (_, u) ->
                u.IsFinished && u.DefId = advancedFactoryDefId)
        if hasAdvancedFactory then Some AdvancedFactory else None
    else None

/// Mark the upgrade as reached on a specific frame. Idempotent — a
/// second call with a different predicate name is ignored because
/// `Reached` is a first-wins record.
let markReached
    (state: UpgradeGateState)
    (name: UpgradePredicateName)
    (frame: uint32)
    : UpgradeGateState =
    match state.Reached with
    | Some _ -> state
    | None ->
        { state with
            Reached = Some name
            ReachedFrame = Some frame }

/// FR-011 / FR-012 exit decision.
///
/// Invariants (see contract §4):
/// - StallAndLose when deadline exceeded AND combat-unit count below
///   threshold (FR-012 stall).
/// - Never AttackNow when both Reached.IsNone AND combat-unit count
///   below threshold — "no degenerate rush" from spec Q5.
let decideUpgradeExit
    (state: UpgradeGateState)
    (currentFrame: uint32)
    (combatUnitCount: int)
    (thresholds: UpgradeThresholds)
    : UpgradeExitDecision =
    let pastDeadline = currentFrame > thresholds.DeadlineFrame
    let enoughCombat = combatUnitCount >= thresholds.CombatUnitThreshold
    match state.Reached, pastDeadline, enoughCombat with
    | Some _, _, true ->
        AttackNow Normal
    | Some _, _, false ->
        // Upgrade reached but not enough army — wait for kbots.
        WaitLonger
    | None, true, true ->
        // Deadline exceeded but we have an army → fallback attack.
        AttackNow DeadlineFallback
    | None, true, false ->
        StallAndLose "upgrade-stall-no-army"
    | None, false, _ ->
        WaitLonger
