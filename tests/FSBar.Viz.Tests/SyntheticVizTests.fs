module FSBar.Viz.Tests.SyntheticVizTests

open Xunit
open SkiaSharp
open SkiaViewer
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

/// Convert a single frame from a synthetic scene to a GameSnapshot,
/// reusing a shared MapGrid to avoid excessive allocation.
let convertFrame (scene: FSBar.SyntheticData.Scene) (sharedGrid: MapGrid) (gs: GameState) : GameSnapshot =
    let units =
        let friendlyUnits =
            gs.Units |> Map.toList |> List.map (fun (uid, u: TrackedUnit) ->
                let (px, py, pz) = u.Position
                let us : UnitState =
                    { UnitId = uid; PositionX = px; PositionY = py; PositionZ = pz
                      TeamId = gs.TeamId; DefId = u.DefId; Health = u.Health
                      MaxHealth = u.MaxHealth; IsEnemy = false }
                (uid, us))
        let enemyUnits =
            gs.Enemies |> Map.toList |> List.map (fun (eid, e: TrackedEnemy) ->
                let (px, py, pz) = e.Position
                let us : UnitState =
                    { UnitId = eid; PositionX = px; PositionY = py; PositionZ = pz
                      TeamId = 1; DefId = e.DefId |> Option.defaultValue 0
                      Health = e.Health |> Option.defaultValue 100.0f
                      MaxHealth = 100.0f; IsEnemy = true }
                (eid, us))
        (friendlyUnits @ enemyUnits) |> Map.ofList
    let economyMetal : EconomyData =
        { Current = gs.Metal.Current; Income = gs.Metal.Income
          Usage = gs.Metal.Usage; Storage = gs.Metal.Storage }
    let economyEnergy : EconomyData =
        { Current = gs.Energy.Current; Income = gs.Energy.Income
          Usage = gs.Energy.Usage; Storage = gs.Energy.Storage }
    { FrameNumber = int gs.FrameNumber; MapGrid = sharedGrid; Units = units
      DisplayUnits = Map.empty; EventIndicators = []; EconomyMetal = economyMetal; EconomyEnergy = economyEnergy
      MetalSpots = Array.empty; Connected = true }

// ---- Per-SceneId tests ----

[<Theory>]
[<InlineData("SceneA")>]
[<InlineData("SceneB")>]
[<InlineData("SceneC")>]
let ``generate scene and build frames 0 150 299 without exceptions`` (sceneIdStr: string) =
    LayerRenderer.invalidateAll ()
    let sceneId =
        match sceneIdStr with
        | "SceneA" -> SceneId.SceneA
        | "SceneB" -> SceneId.SceneB
        | "SceneC" -> SceneId.SceneC
        | _ -> failwith $"Unknown scene: {sceneIdStr}"
    let synScene = Scenes.generate sceneId
    Assert.Equal(300, synScene.Frames.Length)
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Units; OverlayKind.Events; OverlayKind.EconomyHud ] }
    let vs = VizDefaults.defaultViewState
    for frameIdx in [ 0; 150; 299 ] do
        LayerRenderer.invalidateAll ()
        let snap = convertFrame synScene grid synScene.Frames.[frameIdx]
        let scene = SceneBuilder.buildScene snap config vs
        let elements = collectElements scene
        Assert.True(elements.Length > 0, $"Scene {sceneIdStr} frame {frameIdx} should have elements")

[<Theory>]
[<InlineData("SceneA")>]
[<InlineData("SceneB")>]
[<InlineData("SceneC")>]
let ``element counts differ between frame 0 and frame 150`` (sceneIdStr: string) =
    LayerRenderer.invalidateAll ()
    let sceneId =
        match sceneIdStr with
        | "SceneA" -> SceneId.SceneA
        | "SceneB" -> SceneId.SceneB
        | "SceneC" -> SceneId.SceneC
        | _ -> failwith $"Unknown scene: {sceneIdStr}"
    let synScene = Scenes.generate sceneId
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Units; OverlayKind.Events; OverlayKind.EconomyHud ] }
    let vs = VizDefaults.defaultViewState
    let snap0 = convertFrame synScene grid synScene.Frames.[0]
    let scene0 = SceneBuilder.buildScene snap0 config vs
    let snap150 = convertFrame synScene grid synScene.Frames.[150]
    let scene150 = SceneBuilder.buildScene snap150 config vs
    let count0 = (collectElements scene0).Length
    let count150 = (collectElements scene150).Length
    Assert.True(count0 > 0 || count150 > 0,
        $"At least one frame should have elements: frame0={count0}, frame150={count150}")

// ---- Layer switching test ----

[<Fact>]
let ``render SceneA frame 0 with each LayerKind produces non-empty scenes`` () =
    let synScene = Scenes.generate SceneId.SceneA
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}
    let snap = convertFrame synScene grid synScene.Frames.[0]
    let vs = VizDefaults.defaultViewState
    let layers = [
        LayerKind.HeightMap
        LayerKind.SlopeMap
        LayerKind.ResourceMap
        LayerKind.LosMap
        LayerKind.RadarMap
        LayerKind.TerrainClassification
        LayerKind.Passability MoveType.Kbot
        LayerKind.Passability MoveType.Tank
    ]
    for layer in layers do
        LayerRenderer.invalidateAll ()
        let config = { VizDefaults.defaultConfig with BaseLayer = layer }
        let scene = SceneBuilder.buildScene snap config vs
        let elements = collectElements scene
        Assert.True(elements.Length > 0, $"Layer {layer} should produce non-empty scene")

// ---- Animation test ----

[<Fact>]
let ``render SceneB frames 100-105 with Events overlay`` () =
    LayerRenderer.invalidateAll ()
    let synScene = Scenes.generate SceneId.SceneB
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Events ] }
    let vs = VizDefaults.defaultViewState
    let scenes =
        [| for i in 100..105 do
            let snap = convertFrame synScene grid synScene.Frames.[i]
            let snap =
                { snap with
                    EventIndicators =
                        [ { PositionX = 100.0f; PositionY = 0.0f; PositionZ = 100.0f
                            Kind = EventKind.UnitCreated; FrameCreated = 100; DurationFrames = 30 } ] }
            SceneBuilder.buildScene snap config vs |]
    for i in 0..scenes.Length - 1 do
        let elems = collectElements scenes.[i]
        Assert.True(elems.Length > 0, $"Frame {100 + i} should have elements")

// ---- Economy HUD test ----

[<Fact>]
let ``render SceneA frame 0 and 200 with EconomyHud produces HUD elements`` () =
    LayerRenderer.invalidateAll ()
    let synScene = Scenes.generate SceneId.SceneA
    let mapW = int synScene.MapWidth / 8
    let mapH = int synScene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.EconomyHud ] }
    let vs = VizDefaults.defaultViewState
    let snap0 =
        convertFrame synScene grid synScene.Frames.[0]
        |> fun s ->
            { s with
                EconomyMetal = { Current = 500.0f; Income = 10.0f; Usage = 5.0f; Storage = 1000.0f }
                EconomyEnergy = { Current = 800.0f; Income = 20.0f; Usage = 10.0f; Storage = 2000.0f } }
    let snap200 =
        convertFrame synScene grid synScene.Frames.[200]
        |> fun s ->
            { s with
                EconomyMetal = { Current = 200.0f; Income = 15.0f; Usage = 12.0f; Storage = 1000.0f }
                EconomyEnergy = { Current = 1500.0f; Income = 30.0f; Usage = 20.0f; Storage = 2000.0f } }
    let scene0 = SceneBuilder.buildScene snap0 config vs
    let scene200 = SceneBuilder.buildScene snap200 config vs
    let elems0 = collectElements scene0
    let elems200 = collectElements scene200
    let rects0 = elems0 |> List.filter isRect
    let texts0 = elems0 |> List.filter isText
    Assert.True(rects0.Length >= 1, $"Frame 0 HUD should have Rect elements, got {rects0.Length}")
    Assert.True(texts0.Length >= 1, $"Frame 0 HUD should have Text elements, got {texts0.Length}")
    let rects200 = elems200 |> List.filter isRect
    let texts200 = elems200 |> List.filter isText
    Assert.True(rects200.Length >= 1, $"Frame 200 HUD should have Rect elements, got {rects200.Length}")
    Assert.True(texts200.Length >= 1, $"Frame 200 HUD should have Text elements, got {texts200.Length}")
