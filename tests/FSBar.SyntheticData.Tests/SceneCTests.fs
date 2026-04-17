module FSBar.SyntheticData.Tests.SceneCTests

open Xunit
open FSBar.Client
open FSBar.SyntheticData

[<Fact>]
let ``Scene C produces exactly 300 frames`` () =
    let scene = Scenes.generate SceneC
    Assert.Equal(300, scene.Frames.Length)

[<Fact>]
let ``Scene C has correct map dimensions`` () =
    let scene = Scenes.generate SceneC
    Assert.Equal(16384.0f, scene.MapWidth)
    Assert.Equal(16384.0f, scene.MapHeight)

[<Fact>]
let ``Scene C starts with approximately 50 friendly units`` () =
    let scene = Scenes.generate SceneC
    let unitCount = Map.count scene.Frames.[0].Units
    Assert.True(unitCount >= 48 && unitCount <= 55, $"Expected ~50 initial units, got {unitCount}")

[<Fact>]
let ``Scene C starts with approximately 40 enemies`` () =
    let scene = Scenes.generate SceneC
    let enemyCount = Map.count scene.Frames.[0].Enemies
    Assert.True(enemyCount >= 38 && enemyCount <= 42, $"Expected ~40 initial enemies, got {enemyCount}")

[<Fact>]
let ``Scene C has at least 5 distinct DefIds`` () =
    let scene = Scenes.generate SceneC
    let distinctDefIds =
        scene.UnitDefs |> UnitDefCache.all |> Seq.length
    Assert.True(distinctDefIds >= 5, $"Expected >= 5 distinct DefIds, got {distinctDefIds}")

[<Fact>]
let ``Scene C economy Current is near Storage`` () =
    let scene = Scenes.generate SceneC
    let firstFrame = scene.Frames.[0]
    Assert.True(firstFrame.Metal.Current >= firstFrame.Metal.Storage * 0.5f,
        $"Expected Metal.Current near Storage, got {firstFrame.Metal.Current}/{firstFrame.Metal.Storage}")
