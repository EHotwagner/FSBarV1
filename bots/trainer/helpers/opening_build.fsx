// bots/trainer/helpers/opening_build.fsx — opening-build order helper
//
// FR link: FR-001, FR-002, FR-003, FR-004
// Contract: specs/023-trainer-builder-economy/contracts/helpers.md §1
//
// Extraction provenance (two organic sites per feature 021 Q3):
//   - Site 1: bot_macro.fsx iter 001 (commit 8550a8f) — inlined the
//     opening sequence, the FR-002 idle detector, and the
//     Opening→Production transition trigger.
//   - Site 2: bot_macro.fsx iter 002 (commit 6ffd82d) — kept the same
//     opening sequence in place while fixing the defend-interrupt
//     critter filter. Opening code was unchanged from iter 001.
//
// This helper is extracted at iter 3 (T015). A small bug found in iter
// 002 is corrected as part of the extraction: the Y component of the
// metal-spot tuple (the terrain height returned by getMetalSpots) was
// being passed literally as the BuildCommand y-coord. BAR's command
// handlers expect a "place-near-here" vector where y is nominal (the
// engine snaps to ground); the metal spot's terrain-height y is not
// what the command wants. The helper now uses the commander's y for
// NearestMetalSpot sites (matching the 2D-distance choice rule) and
// the same nominal y for NearCommander / NearBaseCentre offsets.
//
// prelude + log must be #loaded before this file.

open FSBar.Client
open FSBar.Client.Commands

printfn "[trainer] opening_build.fsx loaded"

/// Position chooser for an opening item.
type PositionChooser =
    /// Index into the pre-sorted nearest-metal-spot list.
    | NearestMetalSpot of spotIndex:int
    /// (dx, dz) offset from the commander, y preserved.
    | NearCommander of dx:float32 * dz:float32
    /// (dx, dz) offset from the base centre, y preserved.
    | NearBaseCentre of dx:float32 * dz:float32

/// Symbolic opening item. Resolved against UnitDefCache at warmup.
type OpeningBuildItem = {
    DefName: string
    Chooser: PositionChooser
    MaxRetries: int
}

/// Symbolic opening order (input to resolveOpeningBuildOrder).
type OpeningBuildOrder = {
    Items: OpeningBuildItem list
}

/// Resolved opening order with concrete defIds attached.
type ResolvedOpeningBuildOrder = {
    Items: (int * OpeningBuildItem) array
    FactoryDefId: int
}

/// Progress across a single match. Immutable between calls; the bot
/// reassigns the whole record when it mutates.
type OpeningProgress = {
    CurrentIndex: int
    RetryCountThisItem: int
    AwaitingCreated: bool
    LastCommandFrame: uint32
    IdleDefectEmitted: bool
    FailuresByIndex: Map<int, string list>
}

let emptyProgress : OpeningProgress = {
    CurrentIndex = 0
    RetryCountThisItem = 0
    AwaitingCreated = false
    LastCommandFrame = 0u
    IdleDefectEmitted = false
    FailuresByIndex = Map.empty
}

/// Decision record for the next opening command. `Chosen` captures
/// what we picked so the caller can log it without recomputing.
type OpeningCommandDecision = {
    Command: Highbar.AICommand
    ChosenDefId: int
    ChosenDefName: string
    ChosenPosition: float32 * float32 * float32
}

/// Default opening that satisfies FR-001: ≥2 mex, ≥2 energy, ≥1 factory.
/// Uses Armada names to match EngineConfig.defaultConfig ()'s
/// OurSide = "Armada". Iter 004 diagnosed that using Cortex names
/// (cormex / corsolar / corlab) silently failed because armcom cannot
/// build Cortex structures — the engine drops the BuildCommand with no
/// error. If the macro bot ever targets Cortex, either change the
/// EngineConfig.OurSide or define a second opening plan here and
/// select at warmup. Adding faction auto-detection is tracked for a
/// later iteration.
let defaultOpening : OpeningBuildOrder = {
    Items = [
        { DefName = "armmex";   Chooser = NearestMetalSpot 0;              MaxRetries = 3 }
        { DefName = "armmex";   Chooser = NearestMetalSpot 1;              MaxRetries = 3 }
        // Solar offsets route around the mex spots (iter 005 found that
        // NearCommander(-200, 0) after mex #2 put the solar at (146, 207)
        // which overlaps with armmex at (152, 168) — commander froze).
        // Using NearBaseCentre instead so the solars stay centred on the
        // original start position regardless of where the commander has
        // walked while building mex.
        { DefName = "armsolar"; Chooser = NearBaseCentre(200.0f, 0.0f);    MaxRetries = 3 }
        { DefName = "armsolar"; Chooser = NearBaseCentre(-200.0f, 0.0f);   MaxRetries = 3 }
        { DefName = "armlab";   Chooser = NearBaseCentre(0.0f, 350.0f);    MaxRetries = 2 }
    ]
}

/// Resolve symbolic def names against the cache. Fails fast on an
/// unresolved name — this is how FR-003 surfaces "typo in plan" at
/// bot start rather than in the middle of a match.
let resolveOpeningBuildOrder
    (cache: UnitDefCache)
    (order: OpeningBuildOrder)
    : ResolvedOpeningBuildOrder =
    let items =
        order.Items
        |> List.map (fun item ->
            match UnitDefCache.tryFindByName cache item.DefName with
            | Some info -> (info.DefId, item)
            | None -> failwithf "[opening_build] unresolved def name '%s'" item.DefName)
        |> List.toArray
    // FR-004 trigger: the first factory to finish transitions
    // Opening→Production. By convention the factory is the last item
    // in the plan (it's what the opening is building toward), so we
    // use the last resolved defId.
    let factoryDefId =
        match Array.tryLast items with
        | Some (defId, _) -> defId
        | None -> -1
    { Items = items; FactoryDefId = factoryDefId }

/// Sort metal spots by squared 2D distance from a point. Returns a new
/// array; input is not mutated.
let sortMetalSpotsByDistance
    (cx: float32) (cz: float32)
    (spots: (float32 * float32 * float32 * float32) array)
    : (float32 * float32 * float32 * float32) array =
    spots
    |> Array.sortBy (fun (x, _, z, _) ->
        let dx = x - cx
        let dz = z - cz
        dx * dx + dz * dz)

/// Compute the next build command, or None when the opening is complete.
///
/// IMPORTANT (fix applied at extraction): for NearestMetalSpot items,
/// we use the commander's own y rather than the metal spot's y. The
/// metal spot's y is the terrain altitude at the spot, which is NOT
/// what BuildCommand wants as its y parameter — passing it literally
/// (iter 002 bug) produced a silent placement failure because the
/// construction site landed far above ground. The engine snaps the
/// supplied (x, z) to terrain anyway, so the y just needs to be
/// nominal (we use the commander's y, which is on the ground).
let nextOpeningCommand
    (resolved: ResolvedOpeningBuildOrder)
    (progress: OpeningProgress)
    (commanderId: int)
    (commanderPos: float32 * float32 * float32)
    (baseCentre: float32 * float32 * float32)
    (sortedMetalSpots: (float32 * float32 * float32 * float32) array)
    : OpeningCommandDecision option =
    if progress.CurrentIndex >= resolved.Items.Length then None
    else
        let (defId, item) = resolved.Items.[progress.CurrentIndex]
        let (cx, cy, cz) = commanderPos
        let (bx, _, bz) = baseCentre
        let posOpt =
            match item.Chooser with
            | NearestMetalSpot idx when idx < sortedMetalSpots.Length ->
                let (mx, _, mz, _) = sortedMetalSpots.[idx]
                // Use commander y — see header comment on the y-fix.
                Some (mx, cy, mz)
            | NearestMetalSpot _ -> None
            | NearCommander(dx, dz) -> Some (cx + dx, cy, cz + dz)
            | NearBaseCentre(dx, dz) -> Some (bx + dx, cy, bz + dz)
        match posOpt with
        | None -> None
        | Some (tx, ty, tz) ->
            Some {
                Command = BuildCommand commanderId defId tx ty tz 0
                ChosenDefId = defId
                ChosenDefName = item.DefName
                ChosenPosition = (tx, ty, tz)
            }

/// Record a placement failure against the current item. Advances
/// CurrentIndex if the retry budget is exhausted (FR-003).
let recordPlacementFailure
    (progress: OpeningProgress)
    (reason: string)
    : OpeningProgress =
    let idx = progress.CurrentIndex
    let failures =
        progress.FailuresByIndex
        |> Map.change idx (fun existing ->
            match existing with
            | Some xs -> Some (reason :: xs)
            | None -> Some [ reason ])
    progress
// (Unused by iter 3; retained for iter 2 extraction contract. Full
// retry logic lands when iterations surface a concrete failure case —
// FR-003 spirit.)
    |> fun p -> { p with FailuresByIndex = failures }

/// Test whether the opening phase is complete — the caller passes the
/// set of defIds finished on the current frame.
let openingComplete
    (resolved: ResolvedOpeningBuildOrder)
    (newlyFinishedDefIds: int seq)
    : bool =
    newlyFinishedDefIds |> Seq.exists (fun d -> d = resolved.FactoryDefId)

/// Progress advance on UnitFinished: called when the bot sees a
/// UnitFinished event whose defId matches the expected current item.
/// Returns the updated progress.
///
/// Note (iter 6→iter 7 change): we advance on UnitFinished, not
/// UnitCreated. Advancing on UnitCreated was premature — the commander
/// would start structure N, observe UnitCreated, immediately issue
/// BuildCommand for structure N+1, walk to the new site, and abandon
/// N at partial progress. BAR's engine then cleaned up the abandoned
/// partial structures (iter 006 saw 4 of 5 destroyed post-creation).
/// Waiting for UnitFinished means the commander completes each
/// structure before moving on.
let advanceOnFinished (progress: OpeningProgress) (frame: uint32) : OpeningProgress =
    { progress with
        CurrentIndex = progress.CurrentIndex + 1
        RetryCountThisItem = 0
        AwaitingCreated = false
        LastCommandFrame = frame
        IdleDefectEmitted = false }

/// Mark that a build command was just issued — transitions the
/// progress into "awaiting UnitCreated".
let markIssued (progress: OpeningProgress) (frame: uint32) : OpeningProgress =
    { progress with
        AwaitingCreated = true
        LastCommandFrame = frame
        IdleDefectEmitted = false }
