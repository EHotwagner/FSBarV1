// bots/trainer/helpers/attack_launch.fsx — army composition + attack launch
//
// FR link: FR-013, FR-014, FR-015
// Contract: specs/023-trainer-builder-economy/contracts/helpers.md §5
//
// Extraction provenance (two organic sites):
//   - Site 1: bot_macro.fsx iter 024 (commit 0f03be1) — inlined the
//     combat-unit classifier, CombatUnitThreshold gate, and a launch
//     loop using FightCommand. FightCommand stopped units to engage
//     en route, so 12 peewees didn't reach the target in time.
//   - Site 2: bot_macro.fsx iter 025-026 (commit 0f03be1) — same
//     launch shape with MoveCommand instead of FightCommand, and with
//     max_frames raised to 36000 (ladder.json) so there's enough
//     attack-phase window. Iter 026 produced the first macro clean
//     win on NullAI with victory_signal=engine-shutdown-gameover.
//
// prelude + log must be #loaded before this file.

open FSBar.Client
open FSBar.Client.Commands
open Perception

printfn "[trainer] attack_launch.fsx loaded"

/// Frozen snapshot of the attack force at launch time. Used by the
/// caller to log telemetry or later re-issue commands via
/// `maybeRetarget`.
type AttackLaunchState = {
    TargetPos: float32 * float32 * float32
    LaunchedIds: Set<int>
    LaunchFrame: uint32
}

let emptyAttackLaunchState : AttackLaunchState = {
    TargetPos = (0.0f, 0.0f, 0.0f)
    LaunchedIds = Set.empty
    LaunchFrame = 0u
}

/// Classify a def as "combat" using its UnitDefCache entry:
/// MaxWeaponRange > 0 (has a weapon) AND BuildOptions empty (not a
/// builder). Excludes the commander, construction units, and
/// structures with defensive turrets would NOT be excluded by this
/// rule — operator iteration should refine the allowlist if it
/// matters. For the iter-26 baseline, factories/mex/solar have
/// BuildOptions or zero weapon range, so they pass the filter
/// correctly.
let isCombatDef (cache: UnitDefCache) (defId: int) : bool =
    match UnitDefCache.tryFindById cache defId with
    | Some info ->
        info.MaxWeaponRange > 0.0f && info.BuildOptions.Length = 0
    | None -> false

/// Count combat units in our army. Pure over inputs.
let countCombatUnits (gs: GameState) (cache: UnitDefCache) : int =
    gs.Units
    |> Map.toSeq
    |> Seq.filter (fun (_, u) -> u.IsFinished && isCombatDef cache u.DefId)
    |> Seq.length

/// Build the launch snapshot. The caller passes the target position
/// (typically from `pickEnemyCommanderPos` with a fallback).
let buildLaunchSnapshot
    (gs: GameState)
    (cache: UnitDefCache)
    (targetPos: float32 * float32 * float32)
    (currentFrame: uint32)
    : AttackLaunchState =
    let combatIds =
        gs.Units
        |> Map.toSeq
        |> Seq.filter (fun (_, u) -> u.IsFinished && isCombatDef cache u.DefId)
        |> Seq.map fst
        |> Set.ofSeq
    {
        TargetPos = targetPos
        LaunchedIds = combatIds
        LaunchFrame = currentFrame
    }

/// Emit the launch commands — one MoveCommand per combat unit toward
/// the target position. Iter 024 found FightCommand too slow (units
/// stopped to engage en route); MoveCommand lets units run straight
/// and auto-fire on targets in weapon range, matching the rush bot's
/// proven win path.
let issueLaunch
    (gs: GameState)
    (cache: UnitDefCache)
    (target: float32 * float32 * float32)
    : Highbar.AICommand list =
    let (tx, _, tz) = target
    gs.Units
    |> Map.toSeq
    |> Seq.filter (fun (_, u) -> u.IsFinished && isCombatDef cache u.DefId)
    |> Seq.map (fun (uid, _) -> MoveCommand uid tx 100.0f tz)
    |> Seq.toList

/// Incremental launch — pick up any combat unit NOT in the caller's
/// `launched` set and issue a MoveCommand. Used each frame during the
/// Attack phase so new factory-produced combat units join the attack.
/// Returns the new commands plus the updated `launched` set.
let launchFreshCombat
    (gs: GameState)
    (cache: UnitDefCache)
    (target: float32 * float32 * float32)
    (launched: Set<int>)
    : Highbar.AICommand list * Set<int> =
    let (tx, _, tz) = target
    let fresh =
        gs.Units
        |> Map.toSeq
        |> Seq.filter (fun (uid, u) ->
            u.IsFinished
            && isCombatDef cache u.DefId
            && not (Set.contains uid launched))
        |> Seq.map fst
        |> Seq.toList
    let cmds =
        fresh |> List.map (fun uid -> MoveCommand uid tx 100.0f tz)
    let newLaunched =
        fresh |> List.fold (fun acc uid -> Set.add uid acc) launched
    cmds, newLaunched

/// Re-issue move commands for still-alive combat units if the target
/// position has moved by more than `retargetThreshold`. Returns the
/// commands to apply. Iter-26 baseline doesn't invoke retarget because
/// the NullAI commander position is static; reserved for the BARb/dev
/// rung where the enemy commander is mobile.
let maybeRetarget
    (state: AttackLaunchState)
    (gs: GameState)
    (cache: UnitDefCache)
    (newTarget: float32 * float32 * float32)
    (retargetThreshold: float32)
    : Highbar.AICommand list =
    let (ox, _, oz) = state.TargetPos
    let (nx, _, nz) = newTarget
    let dx = nx - ox
    let dz = nz - oz
    let distSq = dx * dx + dz * dz
    if distSq < retargetThreshold * retargetThreshold then []
    else
        state.LaunchedIds
        |> Set.toSeq
        |> Seq.filter (fun uid -> Map.containsKey uid gs.Units)
        |> Seq.map (fun uid -> MoveCommand uid nx 100.0f nz)
        |> Seq.toList

/// Pick a target position for the attack. First preference: the
/// unique-def enemy (enemy commander). Fallback: fixed position
/// matching the rush bot's hardcoded (3200, 100, 3200) used in
/// bot.fsx as the NullAI-baseline target.
let pickAttackTarget (gs: GameState) : float32 * float32 * float32 =
    pickEnemyCommanderPos gs
    |> Option.defaultValue (3200.0f, 100.0f, 3200.0f)
