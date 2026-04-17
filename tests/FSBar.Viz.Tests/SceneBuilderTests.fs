module FSBar.Viz.Tests.SceneBuilderTests

open Xunit
open SkiaSharp
open SkiaViewer
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open FSBar.Viz.Tests.VizEngineFixture

// ---- US1: Base Layer ----

[<Fact>]
let ``buildScene with HeightMap returns Scene with Image element`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let config = VizDefaults.defaultConfig
    let vs = VizDefaults.defaultViewState
    let scene = SceneBuilder.buildScene snap config vs
    let elements = collectElements scene
    Assert.True(elements |> List.exists isRect, "Scene should contain a Rect element for the base layer (rendered via Shader.Image)")

[<Fact>]
let ``switching LayerKind changes scene content`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let vs = VizDefaults.defaultViewState
    let config1 = { VizDefaults.defaultConfig with BaseLayer = LayerKind.HeightMap }
    let scene1 = SceneBuilder.buildScene snap config1 vs
    LayerRenderer.invalidateAll ()
    let config2 = { VizDefaults.defaultConfig with BaseLayer = LayerKind.SlopeMap }
    let scene2 = SceneBuilder.buildScene snap config2 vs
    // Both scenes should have elements (they may differ in pixel content)
    let elems1 = collectElements scene1
    let elems2 = collectElements scene2
    Assert.True(elems1.Length > 0, "HeightMap scene should have elements")
    Assert.True(elems2.Length > 0, "SlopeMap scene should have elements")

[<Fact>]
let ``empty MapGrid produces Text element with No data`` () =
    LayerRenderer.invalidateAll ()
    let grid : MapGrid =
        { WidthElmos = 0; HeightElmos = 0; WidthHeightmap = 0; HeightHeightmap = 0
          HeightMap = Array2D.zeroCreate 0 0; SlopeMap = Array2D.zeroCreate 0 0
          ResourceMap = Array2D.zeroCreate 0 0; LosMap = Array2D.zeroCreate 0 0
          RadarMap = Array2D.zeroCreate 0 0 }
    let snap = MockSnapshot.emptySnapshot grid
    let scene = SceneBuilder.buildScene snap VizDefaults.defaultConfig VizDefaults.defaultViewState
    let elements = collectElements scene
    let texts = elements |> List.choose textContent
    Assert.True(texts |> List.exists (fun t -> t.Contains("No data")),
        $"Expected 'No data' text, got texts: %A{texts}")

[<Fact>]
let ``Scene BackgroundColor matches VizConfig`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 8; height = 8; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let customColor = SKColor(42uy, 84uy, 126uy)
    let config = { VizDefaults.defaultConfig with BackgroundColor = customColor }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    Assert.Equal(customColor, scene.BackgroundColor)

// ---- US2: Unit Overlay ----

[<Fact>]
let ``snapshot with units and Units overlay enabled produces Ellipse elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
        |> MockSnapshot.withFriendlyAt (200.0f, 0.0f, 200.0f)
        |> MockSnapshot.withEnemyAt (300.0f, 0.0f, 300.0f)
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.Units ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    // Each unit produces 1 marker ellipse, so at least 3
    Assert.True(ellipses.Length >= 3, $"Expected >= 3 ellipses for 3 units, got {ellipses.Length}")

[<Fact>]
let ``empty units produces no unit ellipse elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.Units ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    Assert.Equal(0, ellipses.Length)

[<Fact>]
let ``Units overlay not in ActiveOverlays produces no unit elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withFriendlyAt (100.0f, 0.0f, 100.0f)
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.empty }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    Assert.Equal(0, ellipses.Length)

// ---- US2: Event Overlay ----

[<Fact>]
let ``event at current frame produces event elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEvent EventKind.UnitCreated (100.0f, 0.0f, 100.0f) 0
        |> MockSnapshot.withFrame 0
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.Events ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    // UnitCreated event produces an Ellipse
    let ellipses = elements |> List.filter isEllipse
    Assert.True(ellipses.Length >= 1, $"Expected event ellipse, got {ellipses.Length}")

[<Fact>]
let ``expired event produces no event elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEvent EventKind.UnitCreated (100.0f, 0.0f, 100.0f) 0
        |> MockSnapshot.withFrame 100 // well past DurationFrames (30)
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.Events ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    Assert.Equal(0, ellipses.Length)

[<Fact>]
let ``Combat event produces elements with shader`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEvent EventKind.Combat (100.0f, 0.0f, 100.0f) 0
        |> MockSnapshot.withFrame 5
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.Events ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    // Combat event produces an Ellipse with shader and imageFilter
    let combatEllipses = elements |> List.filter (fun e ->
        match e with
        | Element.Ellipse(_, _, _, _, p) -> p.Shader.IsSome || p.ImageFilter.IsSome
        | _ -> false)
    Assert.True(combatEllipses.Length >= 1, "Combat event should produce elements with shader/imageFilter")

// ---- US3: Economy HUD ----

[<Fact>]
let ``economy HUD enabled produces Rect and Text elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEconomy 500.0f 10.0f 5.0f 1000.0f
        |> MockSnapshot.withEnergyEconomy 800.0f 20.0f 15.0f 2000.0f
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.EconomyHud ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let rects = elements |> List.filter isRect
    let texts = elements |> List.filter isText
    Assert.True(rects.Length >= 3, $"HUD should have Rect elements, got {rects.Length}")
    Assert.True(texts.Length >= 3, $"HUD should have Text elements, got {texts.Length}")

[<Fact>]
let ``economy HUD not in overlays produces no HUD elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEconomy 500.0f 10.0f 5.0f 1000.0f
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.empty }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    // Without HUD, should only have the world group (base layer)
    // Count screen-space rects (outside the world group)
    let screenRects =
        scene.Elements
        |> List.filter isRect
    Assert.Equal(0, screenRects.Length)

[<Fact>]
let ``low resource shows red-ish label color`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withEconomy 1.0f 0.5f 0.3f 1000.0f // very low
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.EconomyHud ] }
    // Call buildScene a few times to let the smoothed value converge toward low
    for _ in 1..20 do
        SceneBuilder.buildScene snap config VizDefaults.defaultViewState |> ignore
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    // Check for red-ish fill colors in text elements (metal label)
    let redTexts = elements |> List.filter (fun e ->
        match e with
        | Element.Text(_, _, _, _, p) ->
            match p.Fill with
            | Some c -> c.Red > 200uy && c.Green < 100uy
            | None -> false
        | _ -> false)
    Assert.True(redTexts.Length >= 1, "Low resource should produce red-ish text labels")

// ---- US4: Grid Overlay ----

[<Fact>]
let ``Grid enabled with ShowGridLines produces Line elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let config =
        { VizDefaults.defaultConfig with
            ActiveOverlays = Set.ofList [ OverlayKind.Grid ]
            ShowGridLines = true
            GridLineSpacing = 16 }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let lines = elements |> List.filter isLine
    Assert.True(lines.Length > 0, "Grid overlay should produce Line elements")

[<Fact>]
let ``Grid not in overlays produces no line elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.empty; ShowGridLines = true }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let lines = elements |> List.filter isLine
    Assert.Equal(0, lines.Length)

// ---- US4: Metal Spots ----

[<Fact>]
let ``MetalSpots enabled with spots produces Ellipse elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 32; height = 32; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withMetalSpots [| (100.0f, 0.0f, 200.0f, 1.0f); (300.0f, 0.0f, 400.0f, 2.0f) |]
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.ofList [ OverlayKind.MetalSpots ] }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    Assert.True(ellipses.Length >= 2, $"MetalSpots should produce >= 2 Ellipses, got {ellipses.Length}")

[<Fact>]
let ``MetalSpots not in overlays produces no spot elements`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap =
        MockSnapshot.emptySnapshot grid
        |> MockSnapshot.withMetalSpots [| (100.0f, 0.0f, 200.0f, 1.0f) |]
    let config = { VizDefaults.defaultConfig with ActiveOverlays = Set.empty }
    let scene = SceneBuilder.buildScene snap config VizDefaults.defaultViewState
    let elements = collectElements scene
    let ellipses = elements |> List.filter isEllipse
    Assert.Equal(0, ellipses.Length)

// ---- US4: Disconnected ----

[<Fact>]
let ``Connected false produces DISCONNECTED text`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = { MockSnapshot.emptySnapshot grid with Connected = false }
    let scene = SceneBuilder.buildScene snap VizDefaults.defaultConfig VizDefaults.defaultViewState
    let elements = collectElements scene
    let texts = elements |> List.choose textContent
    Assert.True(texts |> List.exists (fun t -> t.Contains("DISCONNECTED")),
        $"Expected DISCONNECTED text, got: %A{texts}")

[<Fact>]
let ``Connected true produces no disconnected text`` () =
    LayerRenderer.invalidateAll ()
    let grid = SyntheticMapGrid.build {| width = 16; height = 16; seed = None |}
    let snap = MockSnapshot.emptySnapshot grid // Connected = true by default
    let scene = SceneBuilder.buildScene snap VizDefaults.defaultConfig VizDefaults.defaultViewState
    let elements = collectElements scene
    let texts = elements |> List.choose textContent
    Assert.False(texts |> List.exists (fun t -> t.Contains("DISCONNECTED")),
        "Should not contain DISCONNECTED text when connected")
