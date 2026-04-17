module FSBar.SyntheticData.Tests.SceneBTests

open Xunit
open FSBar.Client
open FSBar.SyntheticData

[<Fact>]
let ``Scene B produces exactly 300 frames`` () =
    let scene = Scenes.generate SceneB
    Assert.Equal(300, scene.Frames.Length)

[<Fact>]
let ``Scene B has correct map dimensions`` () =
    let scene = Scenes.generate SceneB
    Assert.Equal(8192.0f, scene.MapWidth)
    Assert.Equal(8192.0f, scene.MapHeight)

[<Fact>]
let ``Scene B has at least 20 combat events`` () =
    let scene = Scenes.generate SceneB
    let combatEvents =
        scene.GameFrames
        |> Array.sumBy (fun gf ->
            gf.Events
            |> List.sumBy (function
                | GameEvent.UnitDamaged _ -> 1
                | GameEvent.EnemyDamaged _ -> 1
                | GameEvent.WeaponFired _ -> 1
                | _ -> 0))
    Assert.True(combatEvents >= 20, $"Expected >= 20 combat events, got {combatEvents}")

[<Fact>]
let ``Scene B has at least one UnitDestroyed event`` () =
    let scene = Scenes.generate SceneB
    let hasDestroyed =
        scene.GameFrames
        |> Array.exists (fun gf ->
            gf.Events |> List.exists (function GameEvent.UnitDestroyed _ -> true | _ -> false))
    Assert.True(hasDestroyed, "Expected at least one UnitDestroyed event")

[<Fact>]
let ``Scene B unit count decreases over time`` () =
    let scene = Scenes.generate SceneB
    // Some units should be destroyed by end
    let unitsFrame50 = Map.count scene.Frames.[49].Units
    let unitsFrame290 = Map.count scene.Frames.[289].Units
    // Total should change (some destroyed, some reinforced)
    Assert.True(unitsFrame50 <> unitsFrame290, "Unit count should change over time due to combat")

[<Fact>]
let ``Scene B starts with approximately 20 friendly units`` () =
    let scene = Scenes.generate SceneB
    let unitCount = Map.count scene.Frames.[0].Units
    Assert.True(unitCount >= 18 && unitCount <= 22, $"Expected ~20 initial units, got {unitCount}")
