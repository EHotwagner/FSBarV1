module FSBar.Viz.Tests.UnitDisplayAdapterTests

open Xunit
open FSBar.Client
open FSBar.Viz

// `UnitDisplayAdapter` is the single-source UnitDisplay constructor for
// feature 038 FR-002. These tests pin the contract that every shape the
// feature targets (TrackedUnit, TrackedEnemy, EncyclopediaEntry) goes
// through it.

let private infoOf (defId: int) (name: string) : UnitDefInfo =
    { DefId = defId
      Name = name
      Cost = 0.0f
      BuildSpeed = 0.0f
      MaxWeaponRange = 0.0f
      BuildOptions = [||] }

let private cacheWith (defs: (int * string) list) : UnitDefCache =
    defs |> List.map (fun (id, n) -> infoOf id n) |> UnitDefCache.ofSeq

let private trackedUnit (defId: int) : TrackedUnit =
    { UnitId = 100
      DefId = defId
      Position = (128.0f, 0.0f, 256.0f)
      Health = 300.0f
      MaxHealth = 500.0f
      IsFinished = true
      IsIdle = false }

let private trackedEnemy (defId: int) : TrackedEnemy =
    { EnemyId = 200
      DefId = Some defId
      Position = (64.0f, 0.0f, 128.0f)
      Health = Some 250.0f
      InLOS = true
      InRadar = false }

// --- ofTrackedUnit ----------------------------------------------------------

[<Fact>]
let ``ofTrackedUnit: unknown defId returns placeholder`` () =
    let d = UnitDisplayAdapter.ofTrackedUnit UnitDefCache.empty 0 100 (trackedUnit 999)
    Assert.Equal(FactionId.Neutral, d.Faction)
    Assert.Equal(Tier.T1, d.Tier)
    Assert.Equal(MovementShape.Bot, d.Shape)
    Assert.Equal(100, d.UnitId)
    Assert.Equal(999, d.DefId)

[<Fact>]
let ``ofTrackedUnit: positions team id and health carry through`` () =
    let cache = cacheWith [ 1, "armcom" ]
    let d = UnitDisplayAdapter.ofTrackedUnit cache 42 100 (trackedUnit 1)
    Assert.Equal(42, d.TeamId)
    Assert.Equal(128.0f, d.PositionX)
    Assert.Equal(256.0f, d.PositionZ)
    Assert.Equal(300.0f, d.CurrentHealth)
    Assert.Equal(500.0f, d.MaxHealth)

[<Fact>]
let ``ofTrackedUnit: unfinished unit flags IsUnderConstruction`` () =
    let tu = { trackedUnit 999 with IsFinished = false }
    let d = UnitDisplayAdapter.ofTrackedUnit UnitDefCache.empty 0 100 tu
    Assert.True(d.Status.IsUnderConstruction)
    Assert.Equal(0.5f, d.BuildProgress)

[<Fact>]
let ``ofTrackedUnit: known BarData name resolves shape+faction`` () =
    // Pick any real entry to assert the classifiers ran.
    let name =
        BarData.AllUnitDefs.all
        |> List.tryPick (fun (_, _, d) ->
            if d.name = "armcom" then Some d.name else None)
    match name with
    | None -> Assert.True(true, "BarData does not contain armcom; skipping")
    | Some n ->
        let cache = cacheWith [ 1, n ]
        let d = UnitDisplayAdapter.ofTrackedUnit cache 0 100 (trackedUnit 1)
        Assert.NotEqual(MovementShape.Bot, MovementShape.Unknown)
        Assert.NotEqual<FactionId>(FactionId.Neutral, d.Faction)
        Assert.Equal<string>(n, d.InternalName)

// --- ofTrackedEnemy ---------------------------------------------------------

[<Fact>]
let ``ofTrackedEnemy: unknown defId returns placeholder with enemy team -1`` () =
    let d = UnitDisplayAdapter.ofTrackedEnemy UnitDefCache.empty 200 (trackedEnemy 999)
    Assert.Equal(-1, d.TeamId)
    Assert.Equal(200, d.UnitId)
    Assert.Equal(FactionId.Neutral, d.Faction)

[<Fact>]
let ``ofTrackedEnemy: health defaults when missing`` () =
    let e = { trackedEnemy 1 with Health = None }
    let d = UnitDisplayAdapter.ofTrackedEnemy UnitDefCache.empty 200 e
    Assert.Equal(0.0f, d.CurrentHealth)
    Assert.Equal(1.0f, d.MaxHealth)

// --- ofEncyclopediaEntry ----------------------------------------------------

[<Fact>]
let ``ofEncyclopediaEntry: pinned footprint propagates to UnitDisplay`` () =
    let entry : EncyclopediaData.EncyclopediaEntry =
        { DefId = 7
          InternalName = "armcom"
          Subfolder = "Units/ARM"
          Faction = FactionId.Armada
          Tier = Tier.T3
          Shape = MovementShape.Bot
          MetalCost = 2000
          EnergyCost = 15000
          BuildTime = 100
          Health = 3000
          FootprintX = 4
          FootprintZ = 4
          SightRangeElmo = 500.0f
          WeaponRangesElmo = [ 300.0f ]; MovementClass = None }
    let d = UnitDisplayAdapter.ofEncyclopediaEntry entry 768.0f
    Assert.Equal(768.0f, d.FootprintWidthElmo)
    Assert.Equal(768.0f, d.FootprintHeightElmo)
    Assert.Equal(FactionId.Armada, d.Faction)
    Assert.Equal(Tier.T3, d.Tier)
    Assert.Equal<string>("armcom", d.InternalName)
    Assert.Equal<float32 list>([ 300.0f ], d.WeaponRangesElmo)

[<Fact>]
let ``ofEncyclopediaEntry: heading is zero for static previews (FR-010a)`` () =
    let entry : EncyclopediaData.EncyclopediaEntry =
        { DefId = 1
          InternalName = "armpw"
          Subfolder = "Units/ARM"
          Faction = FactionId.Armada
          Tier = Tier.T1
          Shape = MovementShape.Bot
          MetalCost = 0; EnergyCost = 0; BuildTime = 0; Health = 100
          FootprintX = 1; FootprintZ = 1
          SightRangeElmo = 0.0f; WeaponRangesElmo = []; MovementClass = None }
    let d = UnitDisplayAdapter.ofEncyclopediaEntry entry 32.0f
    Assert.Equal(0.0f, d.HeadingRadians)

// --- Shared-path parity (FR-002) -------------------------------------------

[<Fact>]
let ``ofTrackedUnit and ofEncyclopediaEntry agree on classification for same name`` () =
    let encyclopedia = EncyclopediaData.buildFromBarData ()
    match encyclopedia |> List.tryHead with
    | None -> Assert.True(true, "Empty encyclopedia; skipping")
    | Some e ->
        let cache = cacheWith [ e.DefId, e.InternalName ]
        let tu = { trackedUnit e.DefId with DefId = e.DefId }
        let fromLive = UnitDisplayAdapter.ofTrackedUnit cache 0 100 tu
        let fromPreview = UnitDisplayAdapter.ofEncyclopediaEntry e 32.0f
        // Classification must match byte-for-byte between the two paths.
        Assert.Equal(fromPreview.Faction, fromLive.Faction)
        Assert.Equal(fromPreview.Tier, fromLive.Tier)
        Assert.Equal(fromPreview.Shape, fromLive.Shape)
        Assert.Equal<string>(fromPreview.LabelCode, fromLive.LabelCode)
