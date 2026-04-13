// bots/trainer/helpers/constructor_dispatch.fsx — idle-constructor dispatcher
//
// FR link: FR-007
// Contract: specs/023-trainer-builder-economy/contracts/helpers.md §3
//
// Extraction provenance (two organic sites):
//   - Site 1: bot_macro.fsx iter 010 (commit 0606921) — inlined the
//     dispatch scan with an (incorrect) u.IsIdle filter; 0 dispatches.
//   - Site 2: bot_macro.fsx iter 011 (commit 0606921) — kept the
//     dispatch in place and swapped the filter to track
//     `dispatchedConstructors` explicitly; 17 dispatches fired.
//
// Both inline versions used the same shape: for each unmatched
// finished-constructor unit, issue BuildCommand(uid, mexDefId,
// spot.x, con.y, spot.z, 0) and record the dispatch. This helper
// preserves that shape while exposing it behind a tiny state type.
//
// prelude + log must be #loaded before this file.

open FSBar.Client
open FSBar.Client.Commands

printfn "[trainer] constructor_dispatch.fsx loaded"

/// Runtime dispatch state. Immutable from the caller's point of view;
/// dispatchIdle returns an updated record alongside the commands.
type DispatchState = {
    /// Constructor unit ids we've already sent a job to.
    Dispatched: Set<int>
    /// Index into sortedMetalSpots for the next mex site.
    NextMetalSpotIdx: int
    /// Frame each constructor was first-observed-idle (FR-007 telemetry).
    IdleSinceFrame: Map<int, uint32>
    /// Constructor ids we've already emitted an [idle-dispatch-defect]
    /// line for — one-shot per crossing.
    DefectReported: Set<int>
}

let emptyDispatchState (firstFreeSpotIdx: int) : DispatchState = {
    Dispatched = Set.empty
    NextMetalSpotIdx = firstFreeSpotIdx
    IdleSinceFrame = Map.empty
    DefectReported = Set.empty
}

/// Constructor decision record returned by dispatchIdle for the bot
/// to log / forward.
type DispatchDecision = {
    ConstructorId: int
    Command: Highbar.AICommand
    ChosenSpotIdx: int
    ChosenPosition: float32 * float32 * float32
}

/// Find all own units whose DefId is the given constructor def id AND
/// which are finished — regardless of engine IsIdle (which is
/// unreliable per the iter 010 finding).
let findConstructors (gs: GameState) (constructorDefId: int) : int list =
    gs.Units
    |> Map.toSeq
    |> Seq.filter (fun (_, u) -> u.IsFinished && u.DefId = constructorDefId)
    |> Seq.map fst
    |> Seq.toList

/// Dispatch all not-yet-dispatched constructors to the next free
/// metal spots. Returns (decisions, updatedState). The caller
/// extracts Command from each decision and concatenates for
/// trainerLoopRun.
///
/// Assigns one constructor per frame to the next free spot; extras
/// (no spots left) are recorded in IdleSinceFrame for the defect
/// detector.
let dispatchIdle
    (state: DispatchState)
    (gs: GameState)
    (constructorDefId: int)
    (mexDefId: int)
    (sortedMetalSpots: (float32 * float32 * float32 * float32) array)
    (currentFrame: uint32)
    : DispatchDecision list * DispatchState =
    let candidates =
        findConstructors gs constructorDefId
        |> List.filter (fun uid -> not (Set.contains uid state.Dispatched))
    let mutable st = state
    let mutable decisions : DispatchDecision list = []
    for conUid in candidates do
        if st.NextMetalSpotIdx < sortedMetalSpots.Length then
            let idx = st.NextMetalSpotIdx
            let (mx, _, mz, _) = sortedMetalSpots.[idx]
            let cmdY =
                match Map.tryFind conUid gs.Units with
                | Some u ->
                    let (_, y, _) = u.Position
                    y
                | None -> 100.0f
            decisions <- {
                ConstructorId = conUid
                Command = BuildCommand conUid mexDefId mx cmdY mz 0
                ChosenSpotIdx = idx
                ChosenPosition = (mx, cmdY, mz)
            } :: decisions
            st <- { st with
                      NextMetalSpotIdx = idx + 1
                      Dispatched = Set.add conUid st.Dispatched
                      IdleSinceFrame = Map.remove conUid st.IdleSinceFrame
                      DefectReported = Set.remove conUid st.DefectReported }
        else
            // No free metal spots — record idle since now.
            if not (Map.containsKey conUid st.IdleSinceFrame) then
                st <- { st with
                          IdleSinceFrame =
                              Map.add conUid currentFrame st.IdleSinceFrame }
    List.rev decisions, st

/// Return constructor ids whose idle duration exceeds `threshold`
/// frames and which we have not yet emitted a defect line for. The
/// caller emits the defect telemetry and then calls
/// `markDefectReported` to suppress duplicates.
let idleDefectCandidates
    (state: DispatchState)
    (currentFrame: uint32)
    (threshold: uint32)
    : int list =
    state.IdleSinceFrame
    |> Map.toSeq
    |> Seq.choose (fun (uid, sinceFrame) ->
        if currentFrame > sinceFrame
           && currentFrame - sinceFrame > threshold
           && not (Set.contains uid state.DefectReported) then
            Some uid
        else None)
    |> Seq.toList

/// Mark defect as reported for a set of constructors — caller pipes
/// through after emitting telemetry.
let markDefectReported (state: DispatchState) (ids: int seq) : DispatchState =
    { state with
        DefectReported =
            ids |> Seq.fold (fun acc uid -> Set.add uid acc) state.DefectReported }
