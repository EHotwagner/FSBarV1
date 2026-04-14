module FSBar.Client.Tests.WallInTests

open Xunit
open FSBar.Client

let private atCell (x: int) (z: int) : float32 * float32 * float32 =
    float32 x * 8.0f + 4.0f, 0.0f, float32 z * 8.0f + 4.0f

let private footprint (x: int) (z: int) (r: float32) (tag: string) : OwnStructureFootprint =
    { Centre = atCell x z
      RadiusElmos = r
      Tag = Some tag }

// ---------------------------------------------------------------------------
// reachableCells correctness + FR-020 shared-passability invariant
// ---------------------------------------------------------------------------

[<Fact>]
let ``reachableCells on flat grid reaches every passable cell`` () =
    let grid = SyntheticMapGrid.flat 32 32
    let reach = WallIn.reachableCells grid MoveType.Kbot Seq.empty (atCell 16 16)
    let pass = MapGrid.passability grid MoveType.Kbot
    let w = Array2D.length1 reach
    let h = Array2D.length2 reach
    for x in 0 .. w - 1 do
        for z in 0 .. h - 1 do
            Assert.Equal(pass.[x, z], reach.[x, z])

[<Fact>]
let ``reachableCells matches Pathing passability view (FR-020)`` () =
    // Shared-passability invariant: a cell that WallIn.reachableCells marks
    // reachable from `origin` must also be reachable from Pathing.findPath.
    let grid =
        SyntheticMapGrid.flat 64 64
        |> fun g -> SyntheticMapGrid.withCliff g 32 32 4
    let origin = atCell 4 4
    let reach = WallIn.reachableCells grid MoveType.Kbot Seq.empty origin
    // Pick a few reachable cells and verify Pathing.findPath connects.
    let w = Array2D.length1 reach
    let h = Array2D.length2 reach
    let mutable probed = 0
    let mutable ok = true
    for x in 10 .. 10 .. w - 2 do
        for z in 10 .. 10 .. h - 2 do
            if reach.[x, z] && probed < 6 then
                probed <- probed + 1
                let goal = atCell x z
                match Pathing.findPath grid MoveType.Kbot Seq.empty origin goal Pathing.defaultPathBudget with
                | Result.Ok _ -> ()
                | Result.Error e ->
                    ok <- false
                    printfn "Pathing disagreed with reachableCells at (%d, %d): %A" x z e
    Assert.True(ok)
    Assert.True(probed > 0)

// ---------------------------------------------------------------------------
// wouldWallIn scenarios
// ---------------------------------------------------------------------------

[<Fact>]
let ``wouldWallIn passes when placement is clear of structures`` () =
    let grid = SyntheticMapGrid.flat 64 64
    let baseCentre = atCell 32 32
    let existing = [
        footprint 24 32 16.0f "factory"
    ]
    let proposed = footprint 40 32 16.0f "new-solar"
    let result = WallIn.wouldWallIn grid baseCentre existing proposed WallIn.defaultWallInQuery
    Assert.Equal(Passes, result)

[<Fact>]
let ``wouldWallIn flags DisconnectsStructures when a corridor is sealed`` () =
    // One-gap corridor: the base sits on the left, the "factory" sits on the right
    // of the wall. Proposing a structure that plugs the gap must report the factory
    // as disconnected.
    let grid = SyntheticMapGrid.oneGapCorridor 64 64 3
    let baseCentre = atCell 16 32
    let factory = footprint 48 32 16.0f "factory"
    // First sanity-check that the factory IS reachable without any proposal.
    let emptyProposal = footprint 56 32 16.0f "decoy"
    let sanity = WallIn.wouldWallIn grid baseCentre [ factory ] emptyProposal WallIn.defaultWallInQuery
    Assert.Equal(Passes, sanity)
    // Now plug the gap at heightmap corner (32, 32) with a large footprint.
    let plug =
        { Centre = atCell 32 32
          RadiusElmos = 32.0f  // covers the full 4-corner gap
          Tag = Some "plug" }
    let result = WallIn.wouldWallIn grid baseCentre [ factory ] plug WallIn.defaultWallInQuery
    match result with
    | Fails (DisconnectsStructures names) ->
        Assert.Contains("factory", names)
    | other -> Assert.Fail(sprintf "expected Fails DisconnectsStructures, got %A" other)

[<Fact>]
let ``wouldWallIn is pure — ownStructures list unchanged after call`` () =
    let grid = SyntheticMapGrid.flat 64 64
    let baseCentre = atCell 32 32
    let existing = [
        footprint 20 32 16.0f "a"
        footprint 40 32 16.0f "b"
    ]
    let lengthBefore = existing.Length
    let proposed = footprint 32 20 16.0f "c"
    let a = WallIn.wouldWallIn grid baseCentre existing proposed WallIn.defaultWallInQuery
    let b = WallIn.wouldWallIn grid baseCentre existing proposed WallIn.defaultWallInQuery
    Assert.Equal(a, b)
    Assert.Equal(lengthBefore, existing.Length)

[<Fact>]
let ``wouldWallIn without RequireMapEdgeExit ignores map edge`` () =
    let grid = SyntheticMapGrid.flat 32 32
    let baseCentre = atCell 16 16
    let proposed = footprint 10 16 4.0f "x"
    let q = { WallIn.defaultWallInQuery with RequireMapEdgeExit = false }
    let result = WallIn.wouldWallIn grid baseCentre [] proposed q
    Assert.Equal(Passes, result)
