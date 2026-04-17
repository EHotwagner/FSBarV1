module FSBar.Client.Tests.BasePlanTests

open Xunit
open FSBar.Client
open FSBar.SyntheticData

// Convenience: synthetic flat grid big enough for a 5-slot opening plan (size is
// in heightmap cells, so 128×128 cells = 1024×1024 elmos — leaves plenty of room
// for NearBaseCentre offsets up to 350).
let flatGrid = SyntheticMapGrid.flat 128 128

let baseCentre : float32 * float32 * float32 = (512.0f, 0.0f, 512.0f)
let commanderPos : float32 * float32 * float32 = (512.0f, 0.0f, 512.0f)

let contextWith
    (metalSpots: (float32 * float32 * float32 * float32) array)
    (existing: OwnStructureFootprint list)
    (progress: PlanProgress)
    : ResolveContext =
    { Grid = flatGrid
      BaseCentre = baseCentre
      CommanderPos = commanderPos
      MetalSpotsNearest = metalSpots
      Chokepoints = []
      UnitDefs = UnitDefCache.empty
      ExistingStructures = existing
      Progress = progress }

let twoMetalSpots : (float32 * float32 * float32 * float32) array =
    [| (600.0f, 0.0f, 512.0f, 1.5f)
       (424.0f, 0.0f, 512.0f, 1.5f) |]

// ---------------------------------------------------------------------------
// Progress helpers
// ---------------------------------------------------------------------------

[<Fact>]
let ``markConsumed round-trips via PlanProgress`` () =
    let p0 = BasePlan.emptyPlanProgress
    let p1 = BasePlan.markInFlight p0 "mex#1"
    Assert.True(Set.contains "mex#1" p1.InFlight)
    let p2 = BasePlan.markConsumed p1 "mex#1"
    Assert.True(Set.contains "mex#1" p2.ConsumedSlots)
    Assert.False(Set.contains "mex#1" p2.InFlight)

[<Fact>]
let ``markUnfulfillable records the reason`` () =
    let p0 = BasePlan.emptyPlanProgress
    let p1 = BasePlan.markUnfulfillable p0 "solar#1" (TerrainNotBuildable "cliff")
    Assert.True(p1.Unfulfillable.ContainsKey "solar#1")

// ---------------------------------------------------------------------------
// Position chooser dispatch
// ---------------------------------------------------------------------------

[<Fact>]
let ``resolvePlan defaultArmadaOpening on flat grid returns 5 buildable slots`` () =
    let ctx = contextWith twoMetalSpots [] BasePlan.emptyPlanProgress
    let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening ctx
    Assert.Equal(5, resolved.Length)
    let failures = resolved |> List.filter (fun r -> r.Failure.IsSome)
    Assert.True(failures.IsEmpty,
        sprintf "expected no failures, got %A" (failures |> List.map (fun r -> r.Slot.Name, r.Failure)))

[<Fact>]
let ``resolvePlan is deterministic across calls`` () =
    let ctx = contextWith twoMetalSpots [] BasePlan.emptyPlanProgress
    let a = BasePlan.resolvePlan BasePlan.defaultArmadaOpening ctx
    let b = BasePlan.resolvePlan BasePlan.defaultArmadaOpening ctx
    Assert.Equal(a.Length, b.Length)
    for ra, rb in List.zip a b do
        Assert.Equal(ra.Slot.Name, rb.Slot.Name)
        Assert.Equal(ra.Position, rb.Position)

[<Fact>]
let ``NearestMetalSpot out of range returns NoMetalSpot`` () =
    let plan : BasePlan =
        { Name = "test"
          Strategy = "test"
          Slots =
            [ { Name = "phantom"
                DefName = "armmex"
                Chooser = NearestMetalSpot 5
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 1 } ] }
    let ctx = contextWith twoMetalSpots [] BasePlan.emptyPlanProgress
    let resolved = BasePlan.resolvePlan plan ctx
    match resolved.[0].Failure with
    | Some (NoMetalSpot 5) -> ()
    | other -> Assert.Fail(sprintf "expected Some (NoMetalSpot 5), got %A" other)

[<Fact>]
let ``AtChokepointHead out of range returns UnresolvedDependency`` () =
    let plan : BasePlan =
        { Name = "test"
          Strategy = "test"
          Slots =
            [ { Name = "defence"
                DefName = "armllt"
                Chooser = AtChokepointHead 0
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 1 } ] }
    let ctx = contextWith twoMetalSpots [] BasePlan.emptyPlanProgress
    let resolved = BasePlan.resolvePlan plan ctx
    match resolved.[0].Failure with
    | Some (UnresolvedDependency 0) -> ()
    | other -> Assert.Fail(sprintf "expected Some (UnresolvedDependency 0), got %A" other)

[<Fact>]
let ``OffMap slot via large NearBaseCentre offset`` () =
    let plan : BasePlan =
        { Name = "test"
          Strategy = "test"
          Slots =
            [ { Name = "overboard"
                DefName = "armsolar"
                Chooser = NearBaseCentre(-100_000.0f, 0.0f)
                BuilderDefName = "armcom"
                ClearanceMargin = 16.0f
                MaxRetries = 1 } ] }
    let ctx = contextWith twoMetalSpots [] BasePlan.emptyPlanProgress
    let resolved = BasePlan.resolvePlan plan ctx
    match resolved.[0].Failure with
    | Some OffMap -> ()
    | other -> Assert.Fail(sprintf "expected Some OffMap, got %A" other)

[<Fact>]
let ``ClearanceCollision against an existing structure`` () =
    let existing : OwnStructureFootprint list =
        [ { Centre = (712.0f, 0.0f, 512.0f)  // exactly on top of solar#1's NearBaseCentre(200, 0)
            RadiusElmos = 40.0f
            Tag = Some "old-solar" } ]
    let ctx = contextWith twoMetalSpots existing BasePlan.emptyPlanProgress
    let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening ctx
    let solar1 = resolved |> List.find (fun r -> r.Slot.Name = "solar#1")
    match solar1.Failure with
    | Some (ClearanceCollision "old-solar") -> ()
    | other -> Assert.Fail(sprintf "expected ClearanceCollision 'old-solar', got %A" other)

[<Fact>]
let ``Consumed slots are skipped with Failure = None`` () =
    let progress =
        BasePlan.emptyPlanProgress
        |> fun p -> BasePlan.markConsumed p "mex#1"
    let ctx = contextWith twoMetalSpots [] progress
    let resolved = BasePlan.resolvePlan BasePlan.defaultArmadaOpening ctx
    let mex1 = resolved |> List.find (fun r -> r.Slot.Name = "mex#1")
    Assert.False(mex1.BuildableNow)
    Assert.True(mex1.Failure.IsNone)
    Assert.True(mex1.Position.IsNone)
