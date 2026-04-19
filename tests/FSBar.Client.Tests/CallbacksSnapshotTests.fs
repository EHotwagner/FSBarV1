module FSBar.Client.Tests.CallbacksSnapshotTests

open Xunit
open FSBar.Client
open Highbar
open FsGrpc.Protobuf

// Helper: encode a message to bytes then decode it back — exercises
// the generated proto roundtrip path.
let inline private roundtrip< ^T when ^T : equality and ^T : (static member Proto : System.Lazy<ProtoDef< ^T>>) > (value: ^T) : ^T =
    let bytes = encode value
    decode< ^T> bytes

// ---- T006: proto roundtrip ----

[<Fact>]
let ``FriendlyUnit_roundtrip`` () =
    let f : FriendlyUnit =
        { UnitId = 42
          Position = Some { X = 1.0f; Y = 2.0f; Z = 3.0f }
          Health = 95.5f
          UnitDefId = 17
          Team = 1 }
    Assert.Equal(f, roundtrip f)

[<Fact>]
let ``LosEnemyUnit_roundtrip`` () =
    let e : LosEnemyUnit =
        { UnitId = 100
          Position = Some { X = -5.0f; Y = 0.0f; Z = 10.0f }
          Health = 300.0f
          UnitDefId = 5
          Team = 2 }
    Assert.Equal(e, roundtrip e)

[<Fact>]
let ``RadarOnlyEnemyUnit_roundtrip_has_no_health_field`` () =
    // Structural check: the generated record type has no Health member.
    let r : RadarOnlyEnemyUnit =
        { UnitId = 200
          Position = Some { X = 50.0f; Y = 0.0f; Z = 50.0f }
          UnitDefId = 9
          Team = 3 }
    Assert.Equal(r, roundtrip r)
    // Compile-time check: if a Health field existed this would fail to
    // compile. We assert the set of fields via reflection as a regression guard.
    let fields =
        typeof<RadarOnlyEnemyUnit>.GetProperties()
        |> Array.map (fun p -> p.Name)
        |> Set.ofArray
    Assert.DoesNotContain("Health", fields)

[<Fact>]
let ``EconomyRecord_roundtrip_all_eight_fields`` () =
    let e : EconomyRecord =
        { MetalCurrent = 1.0f;  MetalIncome = 2.0f
          MetalUsage = 3.0f;    MetalStorage = 4.0f
          EnergyCurrent = 5.0f; EnergyIncome = 6.0f
          EnergyUsage = 7.0f;   EnergyStorage = 8.0f }
    Assert.Equal(e, roundtrip e)

[<Fact>]
let ``GameStateSnapshot_roundtrip_populated`` () =
    let snap : GameStateSnapshot =
        { Frame = 1234
          Friendlies =
            [ { UnitId = 1; Position = Some { X = 0.0f; Y = 0.0f; Z = 0.0f }
                Health = 100.0f; UnitDefId = 1; Team = 0 } ]
          LosEnemies =
            [ { UnitId = 10; Position = Some { X = 5.0f; Y = 0.0f; Z = 5.0f }
                Health = 50.0f; UnitDefId = 2; Team = 1 } ]
          RadarOnlyEnemies =
            [ { UnitId = 20; Position = Some { X = 9.0f; Y = 0.0f; Z = 9.0f }
                UnitDefId = 3; Team = 1 } ]
          Economy =
            Some { MetalCurrent = 500.0f; MetalIncome = 10.0f
                   MetalUsage = 5.0f; MetalStorage = 1000.0f
                   EnergyCurrent = 800.0f; EnergyIncome = 20.0f
                   EnergyUsage = 15.0f; EnergyStorage = 2000.0f } }
    Assert.Equal(snap, roundtrip snap)

[<Fact>]
let ``GameStateSnapshot_roundtrip_empty_lists`` () =
    let snap : GameStateSnapshot =
        { Frame = 0
          Friendlies = []
          LosEnemies = []
          RadarOnlyEnemies = []
          Economy =
            Some { MetalCurrent = 0.0f; MetalIncome = 0.0f
                   MetalUsage = 0.0f; MetalStorage = 0.0f
                   EnergyCurrent = 0.0f; EnergyIncome = 0.0f
                   EnergyUsage = 0.0f; EnergyStorage = 0.0f } }
    Assert.Equal(snap, roundtrip snap)

// ---- T007: applySnapshot mapper ----

let private priorUnit (id: int) (pos: float32 * float32 * float32) (health: float32) (maxH: float32) (finished: bool) (idle: bool) : TrackedUnit =
    { UnitId = id; DefId = 1; Position = pos; Health = health
      MaxHealth = maxH; IsFinished = finished; IsIdle = idle }

let private priorEnemy (id: int) (pos: float32 * float32 * float32) (health: float32 option) (los: bool) (radar: bool) (defId: int option) : TrackedEnemy =
    { EnemyId = id; DefId = defId; Position = pos; Health = health
      InLOS = los; InRadar = radar }

let private emptyEco : EconomyRecordSnapshot =
    { MetalCurrent = 0.0f; MetalIncome = 0.0f; MetalUsage = 0.0f; MetalStorage = 0.0f
      EnergyCurrent = 0.0f; EnergyIncome = 0.0f; EnergyUsage = 0.0f; EnergyStorage = 0.0f }

[<Fact>]
let ``applySnapshot_updates_friendly_position_preserving_MaxHealth`` () =
    let state =
        { GameState.empty with
            Units = Map.ofList [ 1, priorUnit 1 (0.0f, 0.0f, 0.0f) 50.0f 100.0f true false ] }
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = [ { UnitId = 1; Position = (5.0f, 0.0f, 5.0f); Health = 75.0f; UnitDefId = 1; Team = 0 } ]
          LosEnemies = []; RadarOnlyEnemies = []; Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    let u = result.Units.[1]
    Assert.Equal((5.0f, 0.0f, 5.0f), u.Position)
    Assert.Equal(75.0f, u.Health)
    Assert.Equal(100.0f, u.MaxHealth)     // preserved
    Assert.True(u.IsFinished)             // preserved
    Assert.False(u.IsIdle)                // posChanged -> idle cleared

[<Fact>]
let ``applySnapshot_inserts_new_friendly_with_default_MaxHealth`` () =
    let state = GameState.empty
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = [ { UnitId = 7; Position = (1.0f, 2.0f, 3.0f); Health = 10.0f; UnitDefId = 4; Team = 0 } ]
          LosEnemies = []; RadarOnlyEnemies = []; Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    let u = result.Units.[7]
    Assert.Equal(0.0f, u.MaxHealth)
    Assert.False(u.IsFinished)
    Assert.False(u.IsIdle)

[<Fact>]
let ``applySnapshot_LOS_enemy_sets_InLOS_true_and_concrete_Health`` () =
    let state = GameState.empty
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = []
          LosEnemies = [ { UnitId = 100; Position = (10.0f, 0.0f, 10.0f); Health = 200.0f; UnitDefId = 2; Team = 1 } ]
          RadarOnlyEnemies = []
          Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    let e = result.Enemies.[100]
    Assert.True(e.InLOS)
    Assert.False(e.InRadar)
    Assert.Equal(Some 200.0f, e.Health)
    Assert.Equal(Some 2, e.DefId)

[<Fact>]
let ``applySnapshot_radar_only_clears_Health_even_if_prior_had_Some`` () =
    // FR-004: radar-only Health must be None even when prior state had Some _.
    let state =
        { GameState.empty with
            Enemies = Map.ofList [ 100, priorEnemy 100 (0.0f, 0.0f, 0.0f) (Some 500.0f) true false (Some 2) ] }
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = []
          LosEnemies = []
          RadarOnlyEnemies = [ { UnitId = 100; Position = (10.0f, 0.0f, 10.0f); UnitDefId = 2; Team = 1 } ]
          Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    let e = result.Enemies.[100]
    Assert.False(e.InLOS)
    Assert.True(e.InRadar)
    Assert.Equal(None, e.Health)
    Assert.Equal((10.0f, 0.0f, 10.0f), e.Position)

[<Fact>]
let ``applySnapshot_absent_enemy_retains_prior_Position_and_clears_contact`` () =
    // FR-007: enemies absent from both lists keep last-known position.
    let state =
        { GameState.empty with
            Enemies = Map.ofList [ 50, priorEnemy 50 (7.0f, 0.0f, 7.0f) (Some 100.0f) true true (Some 3) ] }
    let snap : GameStateSnapshotResult =
        { Frame = 1; Friendlies = []; LosEnemies = []; RadarOnlyEnemies = []; Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    let e = result.Enemies.[50]
    Assert.Equal((7.0f, 0.0f, 7.0f), e.Position)  // frozen
    Assert.False(e.InLOS)
    Assert.False(e.InRadar)
    Assert.Equal(None, e.Health)
    Assert.Equal(Some 3, e.DefId)  // preserved

[<Fact>]
let ``applySnapshot_replaces_economy_fully`` () =
    let state =
        { GameState.empty with
            Metal = { Current = 1.0f; Income = 1.0f; Usage = 1.0f; Storage = 1.0f }
            Energy = { Current = 1.0f; Income = 1.0f; Usage = 1.0f; Storage = 1.0f } }
    let snap : GameStateSnapshotResult =
        { Frame = 1; Friendlies = []; LosEnemies = []; RadarOnlyEnemies = []
          Economy =
            { MetalCurrent = 100.0f; MetalIncome = 10.0f; MetalUsage = 5.0f; MetalStorage = 1000.0f
              EnergyCurrent = 200.0f; EnergyIncome = 20.0f; EnergyUsage = 15.0f; EnergyStorage = 2000.0f } }
    let result = GameState.applySnapshot state snap
    Assert.Equal(100.0f, result.Metal.Current)
    Assert.Equal(10.0f, result.Metal.Income)
    Assert.Equal(5.0f, result.Metal.Usage)
    Assert.Equal(1000.0f, result.Metal.Storage)
    Assert.Equal(200.0f, result.Energy.Current)
    Assert.Equal(2000.0f, result.Energy.Storage)

[<Fact>]
let ``applySnapshot_friendly_absent_from_snapshot_is_dropped`` () =
    // The snapshot is authoritative for friendly membership — a unit
    // present prior but absent from the snapshot has died/been transferred.
    let state =
        { GameState.empty with
            Units = Map.ofList
                      [ 1, priorUnit 1 (0.0f, 0.0f, 0.0f) 50.0f 100.0f false false
                        2, priorUnit 2 (1.0f, 0.0f, 1.0f) 80.0f 100.0f true false ] }
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = [ { UnitId = 2; Position = (1.0f, 0.0f, 1.0f); Health = 80.0f; UnitDefId = 1; Team = 0 } ]
          LosEnemies = []; RadarOnlyEnemies = []; Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    Assert.False(result.Units.ContainsKey 1)
    Assert.True(result.Units.ContainsKey 2)

[<Fact>]
let ``applySnapshot_IsIdle_preserved_when_position_unchanged`` () =
    let state =
        { GameState.empty with
            Units = Map.ofList [ 1, priorUnit 1 (5.0f, 0.0f, 5.0f) 100.0f 100.0f true true ] }
    let snap : GameStateSnapshotResult =
        { Frame = 1
          Friendlies = [ { UnitId = 1; Position = (5.0f, 0.0f, 5.0f); Health = 100.0f; UnitDefId = 1; Team = 0 } ]
          LosEnemies = []; RadarOnlyEnemies = []; Economy = emptyEco }
    let result = GameState.applySnapshot state snap
    Assert.True(result.Units.[1].IsIdle)  // unchanged pos -> IsIdle preserved
