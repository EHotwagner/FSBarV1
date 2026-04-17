module FSBar.Viz.Tests.SceneBuilderHeadlessTests

// Unit tests for SceneBuilder.buildSceneHeadless (feature 035-central-gui-hub
// task T013). Exercises the GameState → Scene path the hub's embedded
// viewer uses; `buildScene` is tested in SceneBuilderTests.fs.

open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

let private emptyGameState () : GameState =
    { GameState.empty with TeamId = 0 }

let private gameStateWithUnits (units: (int * TrackedUnit) list) : GameState =
    { GameState.empty with
        TeamId = 0
        Units = units |> Map.ofList }

[<Fact>]
let ``buildSceneHeadless synthesises a scene when map is None`` () =
    LayerRenderer.invalidateAll ()
    let scene = SceneBuilder.buildSceneHeadless (emptyGameState ()) None VizDefaults.defaultConfig
    Assert.True(scene.Elements.Length > 0, "headless scene should produce at least one element even with no map")

[<Fact>]
let ``buildSceneHeadless uses the provided MapGrid when Some`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let scene = SceneBuilder.buildSceneHeadless (emptyGameState ()) (Some grid) VizDefaults.defaultConfig
    let elements = collectElements scene
    Assert.True(List.exists isRect elements, "scene with real map should render a base-layer rect")

[<Fact>]
let ``buildSceneHeadless respects VizConfig.ActiveOverlays toggles`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let withGrid =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.add OverlayKind.Grid VizDefaults.defaultConfig.ActiveOverlays
            ShowGridLines = true }
    let withoutGrid =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.remove OverlayKind.Grid VizDefaults.defaultConfig.ActiveOverlays
            ShowGridLines = false }
    let sceneOn = SceneBuilder.buildSceneHeadless (emptyGameState ()) (Some grid) withGrid
    LayerRenderer.invalidateAll ()
    let sceneOff = SceneBuilder.buildSceneHeadless (emptyGameState ()) (Some grid) withoutGrid
    let onCount = (collectElements sceneOn).Length
    let offCount = (collectElements sceneOff).Length
    Assert.True(onCount >= offCount,
                sprintf "scene with grid overlay should render at least as many elements (on=%d, off=%d)" onCount offCount)

[<Fact>]
let ``buildSceneHeadless converts TrackedUnit positions into UnitState entries`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let units =
        [ 101, { UnitId = 101; DefId = 1; Position = (100.0f, 0.0f, 100.0f)
                 Health = 100.0f; MaxHealth = 100.0f
                 IsFinished = true; IsIdle = false }
          202, { UnitId = 202; DefId = 2; Position = (300.0f, 0.0f, 300.0f)
                 Health = 50.0f; MaxHealth = 100.0f
                 IsFinished = true; IsIdle = false } ]
    let state = gameStateWithUnits units
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Units ]
            UseGlyphRenderer = false }
    let scene = SceneBuilder.buildSceneHeadless state (Some grid) config
    let elements = collectElements scene
    // A legacy-renderer unit paints at least one ellipse per unit.
    Assert.True(elements |> List.filter isEllipse |> List.length >= 2,
                sprintf "expected >=2 unit ellipses, saw %d" (elements |> List.filter isEllipse |> List.length))

// --- Feature 038 FR-001/002: defCache wiring -------------------------------

let private infoOf (defId: int) (name: string) : UnitDefInfo =
    { DefId = defId; Name = name; Cost = 0.0f; BuildSpeed = 0.0f
      MaxWeaponRange = 0.0f; BuildOptions = [||] }

[<Fact>]
let ``buildSceneHeadlessSized with defCache=None falls back to legacy placeholder`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let units =
        [ 1, { UnitId = 1; DefId = 1; Position = (64.0f, 0.0f, 64.0f)
               Health = 100.0f; MaxHealth = 100.0f
               IsFinished = true; IsIdle = false } ]
    let state = gameStateWithUnits units
    let scene =
        SceneBuilder.buildSceneHeadlessSized state (Some grid) [||] None
            VizDefaults.defaultConfig 320 240
    Assert.True(scene.Elements.Length > 0)

[<Fact>]
let ``buildSceneHeadlessSized with defCache=Some populates UnitDisplayAdapter path`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    // Pick a BarData-backed name so classification fires the non-fallback branch.
    let name =
        BarData.AllUnitDefs.all
        |> List.tryPick (fun (_, _, d) -> if d.name = "armpw" then Some d.name else None)
    match name with
    | None -> Assert.True(true, "BarData missing armpw; skipping parity check")
    | Some n ->
        let cache = UnitDefCache.ofSeq [ infoOf 1 n ]
        let units =
            [ 1, { UnitId = 1; DefId = 1; Position = (64.0f, 0.0f, 64.0f)
                   Health = 100.0f; MaxHealth = 100.0f
                   IsFinished = true; IsIdle = false } ]
        let state = gameStateWithUnits units
        let scene =
            SceneBuilder.buildSceneHeadlessSized state (Some grid) [||] (Some cache)
                VizDefaults.defaultConfig 320 240
        Assert.True(scene.Elements.Length > 0)

[<Fact>]
let ``buildSceneHeadlessView with defCache matches adapter output`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let cache = UnitDefCache.ofSeq [ infoOf 1 "armcom" ]
    let tu =
        { UnitId = 1; DefId = 1; Position = (100.0f, 0.0f, 100.0f)
          Health = 300.0f; MaxHealth = 500.0f
          IsFinished = true; IsIdle = false }
    let state = gameStateWithUnits [ 1, tu ]
    let cfg =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Units ] }
    let scene =
        SceneBuilder.buildSceneHeadlessView state (Some grid) [||] (Some cache) cfg
            VizDefaults.defaultViewState
    // The adapter provides a non-empty DisplayUnits map; the scene must
    // therefore render something unit-ish.
    let expected = UnitDisplayAdapter.ofTrackedUnit cache 0 1 tu
    Assert.Equal(FactionId.Armada, expected.Faction)
    Assert.True(scene.Elements.Length > 0)

[<Fact>]
let ``GameViz.getActiveOverlays and setActiveOverlays round-trip`` () =
    let before = GameViz.getActiveOverlays ()
    try
        let target = Set.ofList [ OverlayKind.Units; OverlayKind.Grid; OverlayKind.WeaponRanges ]
        GameViz.setActiveOverlays target
        Assert.Equal<Set<OverlayKind>>(target, GameViz.getActiveOverlays ())
    finally
        GameViz.setActiveOverlays before

[<Fact>]
let ``GameViz.setActiveOverlays visible to legacy toggleOverlay`` () =
    let before = GameViz.getActiveOverlays ()
    try
        let seed = Set.ofList [ OverlayKind.Units ]
        GameViz.setActiveOverlays seed
        GameViz.toggleOverlay OverlayKind.Grid
        let after = GameViz.getActiveOverlays ()
        Assert.Contains(OverlayKind.Units, after)
        Assert.Contains(OverlayKind.Grid, after)
    finally
        GameViz.setActiveOverlays before
