// Example 01: Build a basic Scene from a MockSnapshot
#load "../prelude.fsx"
open Prelude
open FSBar.Viz
open SkiaViewer

// Create a simple map grid
let grid = FSBar.Viz.Tests.VizEngineFixture.testMapGrid 64 64

// Build a snapshot with some units and economy
let snap =
    MockSnapshot.emptySnapshot grid
    |> MockSnapshot.withFriendlyAt (200.0f, 0.0f, 200.0f)
    |> MockSnapshot.withFriendlyAt (400.0f, 0.0f, 300.0f)
    |> MockSnapshot.withEnemyAt (600.0f, 0.0f, 500.0f)
    |> MockSnapshot.withEconomy 500.0f 10.0f 5.0f 1000.0f
    |> MockSnapshot.withEnergyEconomy 800.0f 20.0f 15.0f 1000.0f

// Build a scene with all overlays enabled
let config =
    { VizDefaults.defaultConfig with
        ActiveOverlays = Set.ofList [OverlayKind.Units; OverlayKind.EconomyHud] }
let vs = VizDefaults.defaultViewState
let scene = SceneBuilder.buildScene snap config vs

printfn "Scene has %d top-level elements" scene.Elements.Length
printfn "Background: %A" scene.BackgroundColor
