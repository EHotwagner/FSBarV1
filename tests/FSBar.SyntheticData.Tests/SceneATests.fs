module FSBar.SyntheticData.Tests.SceneATests

open Xunit
open FSBar.Client
open FSBar.SyntheticData

[<Fact>]
let ``Scene A produces exactly 300 frames`` () =
    let scene = Scenes.generate SceneA
    Assert.Equal(300, scene.Frames.Length)
    Assert.Equal(300, scene.GameFrames.Length)

[<Fact>]
let ``Scene A frame numbers are 1 through 300`` () =
    let scene = Scenes.generate SceneA
    for i in 0 .. 299 do
        Assert.Equal(uint32 (i + 1), scene.Frames.[i].FrameNumber)
        Assert.Equal(uint32 (i + 1), scene.GameFrames.[i].FrameNumber)

[<Fact>]
let ``Scene A has Init event in frame 1`` () =
    let scene = Scenes.generate SceneA
    let hasInit =
        scene.GameFrames.[0].Events
        |> List.exists (function GameEvent.Init _ -> true | _ -> false)
    Assert.True(hasInit, "Frame 1 should contain Init event")

[<Fact>]
let ``Scene A has Update event in every frame`` () =
    let scene = Scenes.generate SceneA
    for i in 0 .. 299 do
        let hasUpdate =
            scene.GameFrames.[i].Events
            |> List.exists (function GameEvent.Update _ -> true | _ -> false)
        Assert.True(hasUpdate, $"Frame {i + 1} should contain Update event")

[<Fact>]
let ``Scene A has non-empty Units map by frame 50`` () =
    let scene = Scenes.generate SceneA
    let unitCount = Map.count scene.Frames.[49].Units
    Assert.True(unitCount > 0, $"Expected units by frame 50, got {unitCount}")

[<Fact>]
let ``Scene A has correct map dimensions`` () =
    let scene = Scenes.generate SceneA
    Assert.Equal(4096.0f, scene.MapWidth)
    Assert.Equal(4096.0f, scene.MapHeight)

[<Fact>]
let ``Scene A all DefIds exist in UnitDefCache`` () =
    let scene = Scenes.generate SceneA
    for i in 0 .. 299 do
        for kv in scene.Frames.[i].Units do
            let u = kv.Value
            let found = UnitDefCache.tryFindById scene.UnitDefs u.DefId
            Assert.True(found.IsSome, $"Frame {i + 1}, Unit {u.UnitId}: DefId {u.DefId} not in cache")

[<Fact>]
let ``Scene A has at least 5 distinct unit types`` () =
    let scene = Scenes.generate SceneA
    let allDefIds =
        scene.UnitDefs |> UnitDefCache.all |> Seq.length
    Assert.True(allDefIds >= 5, $"Expected >= 5 unit types, got {allDefIds}")

[<Fact>]
let ``Scene A has enemies appearing`` () =
    let scene = Scenes.generate SceneA
    let lastFrame = scene.Frames.[299]
    let enemyCount = Map.count lastFrame.Enemies
    Assert.True(enemyCount > 0, "Expected enemies by frame 300")

[<Fact>]
let ``Scene A builds units over time`` () =
    let scene = Scenes.generate SceneA
    let unitsFrame1 = Map.count scene.Frames.[0].Units
    let unitsFrame200 = Map.count scene.Frames.[199].Units
    Assert.True(unitsFrame200 > unitsFrame1, $"Expected more units at frame 200 ({unitsFrame200}) than frame 1 ({unitsFrame1})")
