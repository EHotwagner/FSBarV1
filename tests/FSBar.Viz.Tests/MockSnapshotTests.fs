module FSBar.Viz.Tests.MockSnapshotTests

open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``emptySnapshot has empty units and events`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    Assert.True(Map.isEmpty snap.Units, "Units should be empty")
    Assert.True(List.isEmpty snap.EventIndicators, "Events should be empty")
    Assert.Equal(0, snap.FrameNumber)
    Assert.True(snap.Connected)

[<Fact>]
let ``withFriendlyAt adds a unit with IsEnemy false`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 50.0f, 200.0f)
    Assert.Equal(1, snap.Units.Count)
    let unit = snap.Units |> Map.toList |> List.head |> snd
    Assert.False(unit.IsEnemy)
    Assert.Equal(100.0f, unit.PositionX)
    Assert.Equal(50.0f, unit.PositionY)
    Assert.Equal(200.0f, unit.PositionZ)

[<Fact>]
let ``withEnemyAt adds a unit with IsEnemy true`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEnemyAt (300.0f, 0.0f, 400.0f)
    Assert.Equal(1, snap.Units.Count)
    let unit = snap.Units |> Map.toList |> List.head |> snd
    Assert.True(unit.IsEnemy)
    Assert.Equal(300.0f, unit.PositionX)

[<Fact>]
let ``withEvent adds an EventIndicator`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEvent EventKind.UnitCreated (100.0f, 0.0f, 200.0f) 10
    Assert.Equal(1, snap.EventIndicators.Length)
    let evt = snap.EventIndicators.Head
    Assert.Equal(EventKind.UnitCreated, evt.Kind)
    Assert.Equal(100.0f, evt.PositionX)
    Assert.Equal(10, evt.FrameCreated)

[<Fact>]
let ``withEconomy sets metal economy`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEconomy 500.0f 10.0f 5.0f 1000.0f
    Assert.Equal(500.0f, snap.EconomyMetal.Current)
    Assert.Equal(10.0f, snap.EconomyMetal.Income)
    Assert.Equal(5.0f, snap.EconomyMetal.Usage)
    Assert.Equal(1000.0f, snap.EconomyMetal.Storage)

[<Fact>]
let ``withEnergyEconomy sets energy economy`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEnergyEconomy 800.0f 20.0f 15.0f 2000.0f
    Assert.Equal(800.0f, snap.EconomyEnergy.Current)
    Assert.Equal(20.0f, snap.EconomyEnergy.Income)

[<Fact>]
let ``pipeline composition works with chaining`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
        |> MockSnapshot.withFriendlyAt (200.0f, 0.0f, 200.0f)
        |> MockSnapshot.withEnemyAt (300.0f, 0.0f, 300.0f)
        |> MockSnapshot.withEvent EventKind.Combat (150.0f, 0.0f, 150.0f) 5
        |> MockSnapshot.withEconomy 100.0f 5.0f 3.0f 500.0f
        |> MockSnapshot.withFrame 42
    Assert.Equal(3, snap.Units.Count)
    Assert.Equal(1, snap.EventIndicators.Length)
    Assert.Equal(100.0f, snap.EconomyMetal.Current)
    Assert.Equal(42, snap.FrameNumber)

[<Fact>]
let ``auto-generated UnitIds are unique`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
        |> MockSnapshot.withFriendlyAt (200.0f, 0.0f, 200.0f)
        |> MockSnapshot.withEnemyAt (300.0f, 0.0f, 300.0f)
    let ids = snap.Units |> Map.toList |> List.map fst
    Assert.Equal(3, ids.Length)
    Assert.Equal(ids.Length, (ids |> List.distinct |> List.length))

[<Fact>]
let ``withMetalSpots sets spots`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let spots = [| (100.0f, 0.0f, 200.0f, 1.0f); (300.0f, 0.0f, 400.0f, 2.0f) |]
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withMetalSpots spots
    Assert.Equal(2, snap.MetalSpots.Length)

[<Fact>]
let ``withFrame sets frame number`` () =
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFrame 99
    Assert.Equal(99, snap.FrameNumber)
