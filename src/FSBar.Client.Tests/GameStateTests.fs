module FSBar.Client.Tests.GameStateTests

open Xunit
open FSBar.Client

[<Fact>]
let ``empty_state_has_zero_frame`` () =
    let state = GameState.empty
    Assert.Equal(0u, state.FrameNumber)

[<Fact>]
let ``empty_state_has_no_units`` () =
    let state = GameState.empty
    Assert.True(state.Units.IsEmpty)

[<Fact>]
let ``empty_state_has_no_enemies`` () =
    let state = GameState.empty
    Assert.True(state.Enemies.IsEmpty)

[<Fact>]
let ``empty_state_has_zero_economy`` () =
    let state = GameState.empty
    Assert.Equal(0.0f, state.Metal.Current)
    Assert.Equal(0.0f, state.Energy.Current)

[<Fact>]
let ``empty_state_has_empty_unit_def_cache`` () =
    let state = GameState.empty
    let defs = UnitDefCache.all state.UnitDefs |> Seq.toList
    Assert.Empty(defs)

[<Fact>]
let ``economy_snapshot_fields_accessible`` () =
    let snap : EconomySnapshot = { Current = 100.0f; Income = 10.0f; Usage = 5.0f; Storage = 1000.0f }
    Assert.Equal(100.0f, snap.Current)
    Assert.Equal(10.0f, snap.Income)
    Assert.Equal(5.0f, snap.Usage)
    Assert.Equal(1000.0f, snap.Storage)

[<Fact>]
let ``tracked_unit_fields_accessible`` () =
    let unit : TrackedUnit =
        { UnitId = 1; DefId = 42; Position = (100.0f, 200.0f, 0.0f)
          Health = 500.0f; MaxHealth = 1000.0f; IsFinished = true; IsIdle = false }
    Assert.Equal(1, unit.UnitId)
    Assert.Equal(42, unit.DefId)
    Assert.Equal(500.0f, unit.Health)
    Assert.True(unit.IsFinished)
    Assert.False(unit.IsIdle)

[<Fact>]
let ``tracked_enemy_fields_accessible`` () =
    let enemy : TrackedEnemy =
        { EnemyId = 10; DefId = Some 55; Position = (300.0f, 400.0f, 0.0f)
          Health = Some 800.0f; InLOS = true; InRadar = false }
    Assert.Equal(10, enemy.EnemyId)
    Assert.Equal(Some 55, enemy.DefId)
    Assert.True(enemy.InLOS)
    Assert.False(enemy.InRadar)

[<Fact>]
let ``tracked_enemy_optional_fields_can_be_none`` () =
    let enemy : TrackedEnemy =
        { EnemyId = 10; DefId = None; Position = (0.0f, 0.0f, 0.0f)
          Health = None; InLOS = false; InRadar = true }
    Assert.True(enemy.DefId.IsNone)
    Assert.True(enemy.Health.IsNone)
