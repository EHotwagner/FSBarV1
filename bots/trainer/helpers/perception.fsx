// bots/trainer/helpers/perception.fsx — Perception helpers for the trainer bot.
//
// Extracted from bot.fsx when the same perception snippet is used from more
// than one call site in a single iteration (or across two consecutive
// iterations). Each let in this file must have at least two organic call
// sites in bot.fsx at the time of extraction — synthetic splits are
// forbidden per 021 Clarification Q3.
//
// log.fsx must be #loaded before this file. bot.fsx does that in order.

open FSBar.Client

printfn "[trainer] perception.fsx loaded"

/// 023 T008: compute the bot's base centre from the commander's current
/// position. Called once at macro-bot warmup; the caller pins the value
/// for the whole match. Returns None when the commander is not present
/// in GameState.Units (pre-warmup or commander-already-dead edge cases).
let computeBaseCentre (gs: GameState) (commanderId: int) : (float32 * float32 * float32) option =
    gs.Units
    |> Map.tryFind commanderId
    |> Option.map (fun u -> u.Position)

/// 023 T008 (FR-016b): return the set of enemy unit ids inside the
/// bot's base radius. Uses 2D (x/z) distance per research R5 — the
/// y component is the height axis in BAR and is ignored for base-
/// proximity checks because the enemy commander can be on terrain
/// at a different altitude than the bot's base without that meaning
/// "far away" in map units. Empty GameState.Enemies returns Set.empty.
let enemiesInBase
    (gs: GameState)
    (baseCentre: float32 * float32 * float32)
    (baseRadius: float32)
    : Set<int> =
    if Map.isEmpty gs.Enemies then Set.empty
    else
        let (bx, _, bz) = baseCentre
        let r2 = baseRadius * baseRadius
        gs.Enemies
        |> Map.toSeq
        |> Seq.choose (fun (eid, e) ->
            let (ex, _, ez) = e.Position
            let dx = ex - bx
            let dz = ez - bz
            if dx * dx + dz * dz <= r2 then Some eid else None)
        |> Set.ofSeq

/// Pick the enemy that has a DefId unique across the Enemies map and return
/// its position. Used when the opponent spawn pattern contains many copies
/// of a shared def (e.g. NullAI's seven identical buildings) plus exactly
/// one distinct unit — the commander. Returns None when the Enemies map is
/// empty or when no enemy has a unique DefId.
///
/// Extracted from bot.fsx iter 001 (commit 88bc186, postclean-021) and iter
/// 002 (this file): used from two organic call sites in bot.fsx — the
/// periodic progress log and the per-refresh MoveCommand target selection.
let pickEnemyCommanderPos (gs: GameState) : (float32 * float32 * float32) option =
    if Map.isEmpty gs.Enemies then None
    else
        let defCounts =
            gs.Enemies
            |> Map.toSeq
            |> Seq.choose (fun (_, e) -> e.DefId)
            |> Seq.countBy id
            |> Map.ofSeq
        let uniqueEnemy =
            gs.Enemies
            |> Map.toSeq
            |> Seq.tryFind (fun (_, e) ->
                match e.DefId with
                | Some d -> Map.tryFind d defCounts = Some 1
                | None -> false)
        uniqueEnemy |> Option.map (fun (_, e) -> e.Position)
