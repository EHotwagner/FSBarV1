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
