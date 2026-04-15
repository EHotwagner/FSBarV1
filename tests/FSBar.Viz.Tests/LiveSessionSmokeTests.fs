module FSBar.Viz.Tests.LiveSessionSmokeTests

open Xunit
open FSBar.Client
open FSBar.Viz
open FSBar.Viz.Tests.VizEngineFixture

[<Fact>]
let ``GameViz-shaped snapshot with BaseTerrain default produces terrain bitmap plus metal markers`` () =
    LayerRenderer.invalidateAll ()
    let w, h = 16, 16
    let heights =
        Array2D.init h w (fun z x ->
            // mixed land/water gradient
            float32 (x - 4) * 2.0f + float32 z * 0.5f - 5.0f)
    let grid =
        { WidthElmos = w * 8
          HeightElmos = h * 8
          WidthHeightmap = w
          HeightHeightmap = h
          HeightMap = heights
          SlopeMap = Array2D.init (h / 2) (w / 2) (fun _ _ -> 0.0f)
          ResourceMap = Array2D.init h w (fun _ _ -> 0)
          LosMap = Array2D.init h w (fun _ _ -> 0)
          RadarMap = Array2D.init h w (fun _ _ -> 0) }
    let metalSpots : (float32 * float32 * float32 * float32) array =
        [| ( 40.0f, 5.0f,  40.0f, 0.5f)
           (100.0f, 5.0f, 100.0f, 0.7f) |]
    let snap : GameSnapshot =
        { FrameNumber = 0
          MapGrid = grid
          Units = Map.empty
          EventIndicators = []
          EconomyMetal = VizDefaults.defaultEconomy
          EconomyEnergy = VizDefaults.defaultEconomy
          MetalSpots = metalSpots
          Connected = true }
    // Default config (post-T011) has BaseLayer=BaseTerrain and MetalSpots overlay active.
    let config = VizDefaults.defaultConfig
    let viewState = VizDefaults.defaultViewState
    let scene = SceneBuilder.buildScene snap config viewState
    let elements = collectElements scene
    // Terrain bitmap emitted as a Rect with a Shader.Image paint by buildBaseLayer.
    let rects = elements |> List.filter isRect
    Assert.True(rects.Length >= 1, "Expected at least one rect (terrain bitmap blit)")
    // MetalSpots default-on after T011 → each spot renders a glow ellipse plus
    // a tiny centroid dot (2 elements per spot), so 2 spots → 4 ellipses.
    let ellipses = elements |> List.filter isEllipse
    Assert.Equal(4, ellipses.Length)
