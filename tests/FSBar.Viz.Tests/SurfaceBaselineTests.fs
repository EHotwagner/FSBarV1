module FSBar.Viz.Tests.SurfaceBaselineTests

open System
open System.Reflection
open Xunit
open SkiaSharp
open FSBar.Client
open FSBar.Viz

// Helper to check a module has expected members via reflection
let private moduleHasMember (moduleType: Type) (memberName: string) =
    let members = moduleType.GetMembers(BindingFlags.Public ||| BindingFlags.Static)
    members |> Array.exists (fun m -> m.Name = memberName)

let private getModuleType (assemblyQualifiedTypeName: string) =
    let asm = typeof<LayerKind>.Assembly
    asm.GetType(assemblyQualifiedTypeName)

// ---- VizTypes ----

[<Fact>]
let ``VizTypes - LayerKind type exists with expected cases`` () =
    let t = typeof<LayerKind>
    Assert.NotNull(t)
    // Check it's a union type
    Assert.True(Reflection.FSharpType.IsUnion(t), "LayerKind should be a discriminated union")
    let cases = Reflection.FSharpType.GetUnionCases(t)
    let caseNames = cases |> Array.map (fun c -> c.Name)
    Assert.Contains("BaseTerrain", caseNames)
    Assert.Contains("HeightMap", caseNames)
    Assert.Contains("SlopeMap", caseNames)
    Assert.Contains("ResourceMap", caseNames)
    Assert.Contains("Passability", caseNames)

[<Fact>]
let ``VizTypes - OverlayKind type exists with expected cases`` () =
    let t = typeof<OverlayKind>
    Assert.True(Reflection.FSharpType.IsUnion(t))
    let cases = Reflection.FSharpType.GetUnionCases(t)
    let caseNames = cases |> Array.map (fun c -> c.Name)
    Assert.Contains("Units", caseNames)
    Assert.Contains("Events", caseNames)
    Assert.Contains("Grid", caseNames)
    Assert.Contains("MetalSpots", caseNames)
    Assert.Contains("EconomyHud", caseNames)

[<Fact>]
let ``VizTypes - EventKind type exists with expected cases`` () =
    let t = typeof<EventKind>
    Assert.True(Reflection.FSharpType.IsUnion(t))
    let cases = Reflection.FSharpType.GetUnionCases(t)
    let caseNames = cases |> Array.map (fun c -> c.Name)
    Assert.Contains("UnitCreated", caseNames)
    Assert.Contains("UnitDestroyed", caseNames)
    Assert.Contains("EnemySpotted", caseNames)
    Assert.Contains("Combat", caseNames)

[<Fact>]
let ``VizTypes - VizConfig record has expected fields`` () =
    let t = typeof<VizConfig>
    Assert.True(Reflection.FSharpType.IsRecord(t))
    let fields = Reflection.FSharpType.GetRecordFields(t) |> Array.map (fun f -> f.Name)
    Assert.Contains("BaseLayer", fields)
    Assert.Contains("ActiveOverlays", fields)
    Assert.Contains("UnitMarkerSize", fields)
    Assert.Contains("OverlayOpacity", fields)
    Assert.Contains("ShowGridLines", fields)
    Assert.Contains("BackgroundColor", fields)

[<Fact>]
let ``VizTypes - GameSnapshot record has expected fields`` () =
    let t = typeof<GameSnapshot>
    Assert.True(Reflection.FSharpType.IsRecord(t))
    let fields = Reflection.FSharpType.GetRecordFields(t) |> Array.map (fun f -> f.Name)
    Assert.Contains("FrameNumber", fields)
    Assert.Contains("MapGrid", fields)
    Assert.Contains("Units", fields)
    Assert.Contains("EventIndicators", fields)
    Assert.Contains("EconomyMetal", fields)
    Assert.Contains("Connected", fields)

[<Fact>]
let ``VizTypes - VizCommand type exists with expected cases`` () =
    let t = typeof<VizCommand>
    Assert.True(Reflection.FSharpType.IsUnion(t))
    let cases = Reflection.FSharpType.GetUnionCases(t)
    let caseNames = cases |> Array.map (fun c -> c.Name)
    Assert.Contains("SetBaseLayer", caseNames)
    Assert.Contains("ToggleOverlay", caseNames)
    Assert.Contains("Pan", caseNames)
    Assert.Contains("Zoom", caseNames)
    Assert.Contains("Stop", caseNames)

[<Fact>]
let ``VizTypes - VizDefaults module has expected members`` () =
    let t = getModuleType "FSBar.Viz.VizDefaults"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "defaultViewState")
    Assert.True(moduleHasMember t "defaultEconomy")
    Assert.True(moduleHasMember t "defaultConfig")

// ---- ColorMaps ----

[<Fact>]
let ``ColorMaps module has expected members`` () =
    let t = getModuleType "FSBar.Viz.ColorMaps"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "grayscale")
    Assert.True(moduleHasMember t "terrain")
    Assert.True(moduleHasMember t "heatMap")
    Assert.True(moduleHasMember t "binary")
    Assert.True(moduleHasMember t "colorSchemeFor")

// ---- LayerRenderer ----

[<Fact>]
let ``LayerRenderer module has expected members`` () =
    let t = getModuleType "FSBar.Viz.LayerRenderer"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "renderLayer")
    Assert.True(moduleHasMember t "invalidateCache")
    Assert.True(moduleHasMember t "invalidateAll")
    Assert.True(moduleHasMember t "cacheStats")

// ---- SceneBuilder ----

[<Fact>]
let ``SceneBuilder module has expected members`` () =
    let t = getModuleType "FSBar.Viz.SceneBuilder"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "buildScene")

// ---- MapData ----

[<Fact>]
let ``MapData module has expected members`` () =
    let t = getModuleType "FSBar.Viz.MapData"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "save")
    Assert.True(moduleHasMember t "load")

// ---- MockSnapshot ----

[<Fact>]
let ``MockSnapshot module has expected members`` () =
    let t = getModuleType "FSBar.Viz.MockSnapshot"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "emptySnapshot")
    Assert.True(moduleHasMember t "withUnits")
    Assert.True(moduleHasMember t "withFriendlyAt")
    Assert.True(moduleHasMember t "withEnemyAt")
    Assert.True(moduleHasMember t "withEvent")
    Assert.True(moduleHasMember t "withEconomy")
    Assert.True(moduleHasMember t "withEnergyEconomy")
    Assert.True(moduleHasMember t "withMetalSpots")
    Assert.True(moduleHasMember t "withFrame")

// ---- PreviewSession ----

[<Fact>]
let ``PreviewSession module has expected members`` () =
    let t = getModuleType "FSBar.Viz.PreviewSession"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "startWithMap")
    Assert.True(moduleHasMember t "startWithSnapshot")
    Assert.True(moduleHasMember t "startPlayback")
    Assert.True(moduleHasMember t "startWithCachedMaps")
    Assert.True(moduleHasMember t "advanceCycleIndex")
    Assert.True(moduleHasMember t "stop")

// ---- GameViz ----

[<Fact>]
let ``GameViz module has expected members`` () =
    let t = getModuleType "FSBar.Viz.GameViz"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "start")
    Assert.True(moduleHasMember t "stop")
    Assert.True(moduleHasMember t "setBaseLayer")
    Assert.True(moduleHasMember t "toggleOverlay")
    Assert.True(moduleHasMember t "pan")
    Assert.True(moduleHasMember t "zoom")
    Assert.True(moduleHasMember t "screenshot")
    Assert.True(moduleHasMember t "attachToClient")
    Assert.True(moduleHasMember t "onFrame")
    Assert.True(moduleHasMember t "seedUnits")
    Assert.True(moduleHasMember t "enableOverlay")
    Assert.True(moduleHasMember t "disableOverlay")
    Assert.True(moduleHasMember t "setConfig")
    Assert.True(moduleHasMember t "updateConfig")

// ---- LiveSession ----

[<Fact>]
let ``LiveSession module has expected members`` () =
    let t = getModuleType "FSBar.Viz.LiveSession"
    Assert.NotNull(t)
    Assert.True(moduleHasMember t "start")
    Assert.True(moduleHasMember t "startWithClient")

[<Fact>]
let ``LiveSessionHandle type has expected members`` () =
    let t = typeof<LiveSessionHandle>
    Assert.NotNull(t)
    Assert.True(typeof<IDisposable>.IsAssignableFrom(t))
    Assert.NotNull(t.GetProperty("FrameCount"))
    Assert.NotNull(t.GetProperty("IsRunning"))
    Assert.NotNull(t.GetProperty("LastError"))

// ---- Smoke test: key functions can be called ----

[<Fact>]
let ``smoke test - VizDefaults defaultConfig is valid`` () =
    let config = VizDefaults.defaultConfig
    Assert.Equal(LayerKind.HeightMap, config.BaseLayer)
    Assert.Contains(OverlayKind.MetalSpots, config.ActiveOverlays)
    Assert.Equal(6.0f, config.UnitMarkerSize)

[<Fact>]
let ``smoke test - VizDefaults defaultViewState is valid`` () =
    let vs = VizDefaults.defaultViewState
    Assert.Equal(1.0f, vs.Scale)
    Assert.Equal(1024, vs.WindowWidth)
    Assert.Equal(640, vs.WindowHeight)

[<Fact>]
let ``smoke test - ColorMaps colorSchemeFor all layer kinds`` () =
    let layers = [
        LayerKind.BaseTerrain
        LayerKind.HeightMap; LayerKind.SlopeMap; LayerKind.ResourceMap
        LayerKind.LosMap; LayerKind.RadarMap; LayerKind.TerrainClassification
        LayerKind.Passability MoveType.Kbot
    ]
    for layer in layers do
        let scheme = ColorMaps.colorSchemeFor layer
        Assert.NotNull(scheme.Name)
        // Verify MapValue can be called
        let _ = scheme.MapValue 0.5f
        ()
