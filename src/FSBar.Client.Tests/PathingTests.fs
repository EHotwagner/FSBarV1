module FSBar.Client.Tests.PathingTests

open System
open System.IO
open Xunit
open FSBar.Client

// Layer-1 helpers: centre-of-cell in world elmos.
let private atCell (x: int) (z: int) : float32 * float32 * float32 =
    float32 x * 8.0f + 4.0f, 0.0f, float32 z * 8.0f + 4.0f

// ---------------------------------------------------------------------------
// Layer-1 unit tests (synthetic MapGrid)
// ---------------------------------------------------------------------------

[<Fact>]
let ``findPath on flat grid returns a straight-line path`` () =
    let grid = SyntheticMapGrid.flat 64 64
    let start = atCell 4 4
    let goal = atCell 30 4
    match Pathing.findPath grid MoveType.Kbot Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Ok path ->
        Assert.Equal(Complete, path.Status)
        // Straight-line → start + goal should suffice after waypoint collapse.
        Assert.True(path.Waypoints.Length >= 2)
        Assert.True(path.EstimatedCost > 0.0f)
        // First and last waypoint coincide with start/goal cells (centre coordinates).
        let (fx, _, fz) = path.Waypoints.[0]
        let (lx, _, lz) = path.Waypoints.[path.Waypoints.Length - 1]
        let (sx, _, sz) = start
        let (gx, _, gz) = goal
        Assert.InRange(fx, sx - 4.0f, sx + 4.0f)
        Assert.InRange(fz, sz - 4.0f, sz + 4.0f)
        Assert.InRange(lx, gx - 4.0f, gx + 4.0f)
        Assert.InRange(lz, gz - 4.0f, gz + 4.0f)
    | Result.Error e -> Assert.Fail(sprintf "Expected Ok, got %A" e)

[<Fact>]
let ``findPath detours around a central cliff`` () =
    let grid =
        SyntheticMapGrid.flat 64 64
        |> fun g -> SyntheticMapGrid.withCliff g 32 32 6
    let start = atCell 4 32
    let goal = atCell 60 32
    match Pathing.findPath grid MoveType.Tank Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Ok path ->
        Assert.Equal(Complete, path.Status)
        // Detour produces a cost > straight-line distance (56 * 8 = 448 elmos straight).
        Assert.True(path.Waypoints.Length >= 2)
        // Every waypoint should be in a Tank-passable slope region.
        let pass = MapGrid.passability grid MoveType.Tank
        for (wx, _, wz) in path.Waypoints do
            let cx = int (wx / 8.0f)
            let cz = int (wz / 8.0f)
            Assert.True(pass.[cx, cz], sprintf "waypoint (%d,%d) should be Tank-passable" cx cz)
    | Result.Error e -> Assert.Fail(sprintf "Expected Ok, got %A" e)

[<Fact>]
let ``findPath returns NoRoute when a wall fully separates regions for Tank`` () =
    // 32×32 grid with a full impassable wall and no gap → no route.
    let grid = SyntheticMapGrid.oneGapCorridor 32 32 0
    let start = atCell 4 16
    let goal = atCell 28 16
    match Pathing.findPath grid MoveType.Tank Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Error NoRoute -> ()
    | other -> Assert.Fail(sprintf "Expected NoRoute, got %A" other)

[<Fact>]
let ``findPath returns OutOfBounds for start off the map`` () =
    let grid = SyntheticMapGrid.flat 32 32
    let start = (-40.0f, 0.0f, 100.0f)
    let goal = atCell 10 10
    match Pathing.findPath grid MoveType.Kbot Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Error OutOfBounds -> ()
    | other -> Assert.Fail(sprintf "Expected OutOfBounds, got %A" other)

[<Fact>]
let ``findPath returns EndpointImpassable when start is on an impassable cell`` () =
    let grid =
        SyntheticMapGrid.flat 32 32
        |> fun g -> SyntheticMapGrid.withCliff g 4 4 2
    let start = atCell 4 4  // sits inside the cliff zone
    let goal = atCell 20 20
    match Pathing.findPath grid MoveType.Tank Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Error EndpointImpassable -> ()
    | other -> Assert.Fail(sprintf "Expected EndpointImpassable, got %A" other)

[<Fact>]
let ``ownStructures mask blocks a clear route and forces a detour`` () =
    let grid = SyntheticMapGrid.flat 64 64
    let start = atCell 10 32
    let goal = atCell 50 32
    let footprints : OwnStructureFootprint seq = seq {
        { Centre = (30.0f * 8.0f + 4.0f, 0.0f, 32.0f * 8.0f + 4.0f)
          RadiusElmos = 48.0f
          Tag = Some "blocker" }
    }
    // With no footprint the shortest path goes straight across z=32.
    // With the footprint we expect Ok (the detour exists on a 64×64 grid).
    let withoutMask =
        Pathing.findPath grid MoveType.Kbot Seq.empty start goal Pathing.defaultPathBudget
    let withMask =
        Pathing.findPath grid MoveType.Kbot footprints start goal Pathing.defaultPathBudget
    match withoutMask, withMask with
    | Result.Ok baseline, Result.Ok detour ->
        Assert.Equal(Complete, baseline.Status)
        Assert.Equal(Complete, detour.Status)
        Assert.True(detour.EstimatedCost > baseline.EstimatedCost,
            sprintf "masked path cost %.2f should exceed baseline %.2f"
                detour.EstimatedCost baseline.EstimatedCost)
    | a, b -> Assert.Fail(sprintf "Expected both Ok, got %A / %A" a b)

[<Fact>]
let ``findPath produces deterministic results for identical inputs`` () =
    let grid =
        SyntheticMapGrid.flat 64 64
        |> fun g -> SyntheticMapGrid.withCliff g 32 32 4
    let start = atCell 2 2
    let goal = atCell 60 60
    let run () =
        Pathing.findPath grid MoveType.Kbot Seq.empty start goal Pathing.defaultPathBudget
    let a = run ()
    let b = run ()
    match a, b with
    | Result.Ok pa, Result.Ok pb ->
        Assert.Equal(pa.Status, pb.Status)
        Assert.Equal(pa.EstimatedCost, pb.EstimatedCost)
        Assert.Equal(pa.Waypoints.Length, pb.Waypoints.Length)
        for i in 0 .. pa.Waypoints.Length - 1 do
            Assert.Equal(pa.Waypoints.[i], pb.Waypoints.[i])
    | _ -> Assert.Fail("expected two Ok results")

[<Fact>]
let ``budget exhaustion returns Partial with a non-empty best-so-far`` () =
    let grid = SyntheticMapGrid.flat 128 128
    let start = atCell 1 1
    let goal = atCell 126 126
    let tinyBudget : PathBudget =
        { WallClockMs = 50; MaxExpansions = 64; SlopeCost = 2.0f }
    match Pathing.findPath grid MoveType.Kbot Seq.empty start goal tinyBudget with
    | Result.Ok path ->
        match path.Status with
        | Partial true ->
            Assert.True(path.Waypoints.Length >= 1)
        | s -> Assert.Fail(sprintf "expected Partial true, got %A" s)
    | Result.Error e -> Assert.Fail(sprintf "expected Partial Ok, got Error %A" e)

[<Fact>]
let ``pathCost rescoring under higher slope cost is monotonic`` () =
    let grid =
        SyntheticMapGrid.flat 64 64
        |> fun g -> SyntheticMapGrid.withCliff g 32 32 4
    let start = atCell 2 2
    let goal = atCell 60 60
    match Pathing.findPath grid MoveType.Kbot Seq.empty start goal Pathing.defaultPathBudget with
    | Result.Ok path ->
        let lowCost = Pathing.pathCost grid MoveType.Kbot path 0.0f
        let highCost = Pathing.pathCost grid MoveType.Kbot path 10.0f
        Assert.True(highCost >= lowCost,
            sprintf "re-scored cost should be >= lower-slope cost (got %.2f vs %.2f)" highCost lowCost)
    | Result.Error e -> Assert.Fail(sprintf "pathfinding failed: %A" e)

// ---------------------------------------------------------------------------
// Layer-2 integration test (skipped without Avalanche)
// ---------------------------------------------------------------------------

let private avalanchePath =
    let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    Path.Combine(home, ".local", "state", "Beyond All Reason", "maps", "avalanche_3.4.sd7")

[<Fact>]
let ``Layer-2 findPath Avalanche Kbot base-to-enemy returns Complete path`` () =
    if not (File.Exists avalanchePath) then () else
    match SmfParser.parseSd7 avalanchePath with
    | Result.Error e -> Assert.Fail(sprintf "SMF parse failed: %A" e)
    | Result.Ok smf ->
        let grid = SmfParser.toMapGrid smf
        // Generous budget so the SC-001 "95% completion" check won't flake when xUnit
        // schedules the test alongside others. 500 ms wall clock, 500k cell expansions.
        let budget : PathBudget = { WallClockMs = 500; MaxExpansions = 500_000; SlopeCost = 2.0f }
        let start = (500.0f, 0.0f, 397.0f)
        let goal = (3699.0f, 0.0f, 3601.0f)
        match Pathing.findPath grid MoveType.Kbot Seq.empty start goal budget with
        | Result.Ok path ->
            Assert.Equal(Complete, path.Status)
            Assert.True(path.Waypoints.Length >= 2)
            Assert.True(path.EstimatedCost > 0.0f)
        | Result.Error e -> Assert.Fail(sprintf "findPath failed: %A" e)
