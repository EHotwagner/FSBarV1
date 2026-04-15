// Show every Armada unit at 50% health in a SkiaViewer window.
// Usage (from repo root):
//   dotnet fsi src/FSBar.Viz/scripts/armada-half-health.fsx
//
// The script loads BarData, classifies every Armada unit into
// (shape, tier, faction), constructs fully populated UnitDisplay
// records, and drives `SkiaViewer.Viewer.run` directly. The scene is
// rebuilt on every FrameTick so the front-facing alliance pip keeps
// pulsing.
//
// Ctrl+C to exit.

open System
open System.IO
open System.Runtime.InteropServices

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private testBin =
    Path.Combine(repoRoot, "tests", "FSBar.Viz.Tests", "bin", "Debug", "net10.0")

let private nativeDir =
    Path.Combine(testBin, "runtimes", "linux-x64", "native")

[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)

let private preloadNative () =
    let _ = dlopen(Path.Combine(nativeDir, "libglfw.so.3"), 0x2 ||| 0x100)
    let _ = dlopen(Path.Combine(nativeDir, "libSkiaSharp.so"), 0x2 ||| 0x100)
    ()

preloadNative ()

#r "nuget: Silk.NET.Input.Common, 2.22.0"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/BarData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"

open FSBar.Viz
open SkiaSharp
open SkiaViewer

// --- data ------------------------------------------------------------------

let private defaultStatus : StatusFlags =
    { IsUnderConstruction = false
      IsStunned = false
      JustDamagedWithinMs = None
      JustCompletedWithinMs = None
      IsCloaked = false }

let private shapeOf (d: BarData.UnitDef) =
    let canMove = match d.movement with Some m -> m.canMove | None -> false
    let canFly = match d.movement with Some m -> m.canFly | None -> false
    let mClass = match d.movement with Some m -> m.movementClass | None -> None
    UnitGlyph.classifyShape canMove canFly mClass ignore

let private armadaDefs =
    BarData.AllUnitDefs.all
    |> List.map (fun (_, _, d) -> d)
    |> List.filter (fun d ->
        UnitGlyph.classifyFaction d.subfolder d.name ignore = FactionId.Armada)
    |> List.sortBy (fun d ->
        let t =
            match UnitGlyph.classifyTier d.customParams d.category ignore with
            | Tier.T1 -> 1
            | Tier.T2 -> 2
            | Tier.T3 -> 3
        t, d.name)

// 24x12 grid on a 1200x800 window.
let private cols = 24
let private cellW = 50.0f
let private cellH = 66.0f
let private originPx = 25.0f
let private originPy = 40.0f

let private mkDisplay (i: int) (d: BarData.UnitDef) : UnitDisplay =
    let col = i % cols
    let row = i / cols
    let pxX = originPx + float32 col * cellW
    let pxY = originPy + float32 row * cellH
    let shape = shapeOf d
    let tier = UnitGlyph.classifyTier d.customParams d.category ignore
    let faction = UnitGlyph.classifyFaction d.subfolder d.name ignore
    let label = UnitLabels.lookupOrFallback d.name
    let hp =
        match d.health with
        | BarData.ValueOrExpr.Concrete v -> float32 v
        | _ -> 100.0f
    { UnitId = i + 1
      DefId = i + 1
      InternalName = d.name
      Shape = shape
      Faction = faction
      Tier = tier
      LabelCode = label
      FootprintWidthElmo = float32 d.footprintX * 16.0f
      FootprintHeightElmo = float32 d.footprintZ * 16.0f
      TeamId = 0
      PositionX = pxX * 8.0f
      PositionY = 0.0f
      PositionZ = pxY * 8.0f
      HeadingRadians = -float32 Math.PI / 2.0f
      CurrentHealth = hp * 0.5f
      MaxHealth = hp
      BuildProgress = 1.0f
      Status = defaultStatus
      WeaponRangesElmo = []
      SightRangeElmo = 0.0f
      BuildRangeElmo = None
      CommandQueue = [] }

let private displays = armadaDefs |> List.mapi mkDisplay

// --- style -----------------------------------------------------------------

let private demoStyle : UnitGlyphStyle =
    let baseStyle = VizDefaults.defaultConfig.GlyphStyle
    { baseStyle with
        MinPixelRadius = 16.0f
        LabelFontSizePx = 10.0f
        HpArcWidth = 2.0f
        FacingPipRadius = 3.0f
        // Demo alliance colour for Team 0 — a distinct cyan so the pip
        // stands out against the faction-coloured outline.
        TeamPalette =
            { baseStyle.TeamPalette with
                ByTeamId = Map.ofList [ 0, SKColor(80uy, 220uy, 255uy) ] } }

// --- viewer ----------------------------------------------------------------

let private ground = SKColor(30uy, 30uy, 34uy)

let private buildScene () : Scene =
    let els = UnitGlyph.buildUnitsGlyph displays demoStyle Set.empty
    let bg = Scene.rect 0.0f 0.0f 1200.0f 800.0f (Scene.fill ground)
    Scene.create ground (bg :: els)

let private sceneEvt = Event<Scene>()

let private viewerCfg : ViewerConfig =
    { Title = "Armada @ 50%"
      Width = 1200
      Height = 800
      TargetFps = 60
      ClearColor = ground
      PreferredBackend = Some Backend.GL }

let private handle, inputs = Viewer.run viewerCfg sceneEvt.Publish

// Rebuild per FrameTick so the pulsing alliance pip actually animates.
let private frameSub =
    inputs
    |> Observable.subscribe (fun evt ->
        match evt with
        | InputEvent.FrameTick _ -> sceneEvt.Trigger (buildScene ())
        | _ -> ())

sceneEvt.Trigger (buildScene ())
printfn "Showing %d Armada units at 50%% HP. Ctrl+C to exit." displays.Length

// Block so the script (and therefore the viewer window) stays alive.
System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite)
