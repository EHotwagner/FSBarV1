open System
open System.Runtime.InteropServices

[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)

let bd = __SOURCE_DIRECTORY__ + "/FSBar.Viz.Tests/bin/Debug/net10.0"
let np = bd + "/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)

#r "FSBar.Viz.Tests/bin/Debug/net10.0/FsGrpc.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Core.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Maths.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.Common.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.Glfw.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Common.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.Glfw.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.GLFW.dll"
#r "FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.OpenGL.dll"

open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open SkiaSharp
open SkiaViewer

// --- Helpers ---
let mkGrid w h : MapGrid =
    { WidthElmos=w*8; HeightElmos=h*8; WidthHeightmap=w; HeightHeightmap=h
      HeightMap=Array2D.init (h+1) (w+1) (fun z x -> float32 x*3.0f+float32 z*2.0f)
      SlopeMap=Array2D.init (h/2) (w/2) (fun z x -> float32 x/float32(max 1 (w/2)))
      ResourceMap=Array2D.init h w (fun z x -> if (x+z)%8=0 then 200 else 0)
      LosMap=Array2D.init h w (fun _ x -> if x<w*2/3 then 1 else 0)
      RadarMap=Array2D.init h w (fun z _ -> if z<h*2/3 then 1 else 0) }

let toSnap (grid: MapGrid) (gs: GameState) : GameSnapshot =
    let units =
        let f = gs.Units |> Map.toList |> List.map (fun (uid, u: TrackedUnit) ->
            let (px,py,pz)=u.Position
            uid, ({UnitId=uid;PositionX=px;PositionY=py;PositionZ=pz;TeamId=0;DefId=u.DefId;Health=u.Health;MaxHealth=u.MaxHealth;IsEnemy=false}:UnitState))
        let e = gs.Enemies |> Map.toList |> List.map (fun (eid, en: TrackedEnemy) ->
            let (px,py,pz)=en.Position
            eid, ({UnitId=eid;PositionX=px;PositionY=py;PositionZ=pz;TeamId=1;DefId=(en.DefId|>Option.defaultValue 0);Health=(en.Health|>Option.defaultValue 100.0f);MaxHealth=100.0f;IsEnemy=true}:UnitState))
        (f@e)|>Map.ofList
    {FrameNumber=int gs.FrameNumber;MapGrid=grid;Units=units;EventIndicators=[]
     EconomyMetal={Current=gs.Metal.Current;Income=gs.Metal.Income;Usage=gs.Metal.Usage;Storage=gs.Metal.Storage}
     EconomyEnergy={Current=gs.Energy.Current;Income=gs.Energy.Income;Usage=gs.Energy.Usage;Storage=gs.Energy.Storage}
     MetalSpots=[||];Connected=true}

let playback (title: string) (scenes: Scene array) (seconds: float) =
    let ev = Event<Scene>()
    let vh, _ = Viewer.run {Title=title;Width=1024;Height=640;TargetFps=60;ClearColor=SKColors.Black;PreferredBackend=Some Backend.GL} ev.Publish
    let sw = Diagnostics.Stopwatch.StartNew()
    while sw.Elapsed.TotalSeconds < seconds do
        let idx = int(sw.Elapsed.TotalSeconds * 30.0) % scenes.Length
        ev.Trigger scenes.[idx]
        Threading.Thread.Sleep(33)
    (vh :> IDisposable).Dispose()

// ========== TEST 1: Static snapshot ==========
eprintfn "=== TEST 1: Static snapshot — units, events, HUD, metal spots (10s) ==="
let g1 = mkGrid 64 64
let s1 =
    MockSnapshot.emptySnapshot g1
    |> MockSnapshot.withFriendlyAt (200.0f,0.0f,200.0f)
    |> MockSnapshot.withFriendlyAt (350.0f,0.0f,150.0f)
    |> MockSnapshot.withFriendlyAt (100.0f,0.0f,400.0f)
    |> MockSnapshot.withEnemyAt (450.0f,0.0f,300.0f)
    |> MockSnapshot.withEnemyAt (480.0f,0.0f,350.0f)
    |> MockSnapshot.withEvent EventKind.Combat (300.0f,0.0f,250.0f) 0
    |> MockSnapshot.withEvent EventKind.UnitCreated (100.0f,0.0f,100.0f) 0
    |> MockSnapshot.withEconomy 500.0f 12.0f 8.0f 1000.0f
    |> MockSnapshot.withEnergyEconomy 800.0f 25.0f 18.0f 1500.0f
    |> MockSnapshot.withMetalSpots [|(160.0f,0.0f,160.0f,5.0f);(400.0f,0.0f,400.0f,3.0f);(50.0f,0.0f,300.0f,7.0f)|]
LayerRenderer.invalidateAll()
let cfg1 = {VizDefaults.defaultConfig with ActiveOverlays=Set.ofList [OverlayKind.Units;OverlayKind.Events;OverlayKind.EconomyHud;OverlayKind.MetalSpots]}
let vs1 = {VizDefaults.defaultViewState with Scale=10.0f}
let sc1 = SceneBuilder.buildScene s1 cfg1 vs1
playback "TEST 1: Static Snapshot" [|sc1|] 10.0
eprintfn "TEST 1 done.\n"

// ========== TEST 2: SceneA playback ==========
eprintfn "=== TEST 2: SceneA — early game buildup (10s) ==="
let scA = Scenes.generate SceneId.SceneA
let gA = mkGrid (int scA.MapWidth/8) (int scA.MapHeight/8)
let cfgP = {VizDefaults.defaultConfig with ActiveOverlays=Set.ofList [OverlayKind.Units;OverlayKind.EconomyHud]}
let vsA = {VizDefaults.defaultViewState with Scale=float32 1024/float32 gA.WidthHeightmap}
LayerRenderer.invalidateAll()
let scenesA = scA.Frames |> Array.map (fun gs -> SceneBuilder.buildScene (toSnap gA gs) cfgP vsA)
eprintfn "  %d scenes ready." scenesA.Length
playback "TEST 2: SceneA" scenesA 10.0
eprintfn "TEST 2 done.\n"

// ========== TEST 3: SceneB playback ==========
eprintfn "=== TEST 3: SceneB — mid-game skirmish (10s) ==="
let scB = Scenes.generate SceneId.SceneB
let gB = mkGrid (int scB.MapWidth/8) (int scB.MapHeight/8)
let vsB = {VizDefaults.defaultViewState with Scale=float32 1024/float32 gB.WidthHeightmap}
LayerRenderer.invalidateAll()
let scenesB = scB.Frames |> Array.map (fun gs -> SceneBuilder.buildScene (toSnap gB gs) cfgP vsB)
eprintfn "  %d scenes ready." scenesB.Length
playback "TEST 3: SceneB" scenesB 10.0
eprintfn "TEST 3 done.\n"

// ========== TEST 4: SceneC playback ==========
eprintfn "=== TEST 4: SceneC — late game (10s) ==="
let scC = Scenes.generate SceneId.SceneC
let gC = mkGrid (int scC.MapWidth/8) (int scC.MapHeight/8)
let vsC = {VizDefaults.defaultViewState with Scale=float32 1024/float32 gC.WidthHeightmap}
LayerRenderer.invalidateAll()
let scenesC = scC.Frames |> Array.map (fun gs -> SceneBuilder.buildScene (toSnap gC gs) cfgP vsC)
eprintfn "  %d scenes ready." scenesC.Length
playback "TEST 4: SceneC" scenesC 10.0
eprintfn "TEST 4 done.\n"

// ========== TEST 5: Layer switching ==========
eprintfn "=== TEST 5: Layer switching — all 10 layer kinds (10s) ==="
let g5 = mkGrid 64 64
let s5 = MockSnapshot.emptySnapshot g5 |> MockSnapshot.withFriendlyAt (200.0f,0.0f,200.0f) |> MockSnapshot.withEnemyAt (400.0f,0.0f,300.0f) |> MockSnapshot.withEconomy 600.0f 15.0f 10.0f 1000.0f
let vs5 = {VizDefaults.defaultViewState with Scale=10.0f}
let layers = [
    LayerKind.HeightMap; LayerKind.SlopeMap; LayerKind.ResourceMap
    LayerKind.LosMap; LayerKind.RadarMap; LayerKind.TerrainClassification
    LayerKind.Passability MoveType.Kbot; LayerKind.Passability MoveType.Tank
    LayerKind.Passability MoveType.Hover; LayerKind.Passability MoveType.Ship ]
let ev5 = Event<Scene>()
let vh5, _ = Viewer.run {Title="TEST 5: Layer Switching";Width=1024;Height=640;TargetFps=60;ClearColor=SKColors.Black;PreferredBackend=Some Backend.GL} ev5.Publish
for layer in layers do
    LayerRenderer.invalidateAll()
    let c = {VizDefaults.defaultConfig with BaseLayer=layer; ActiveOverlays=Set.ofList [OverlayKind.Units;OverlayKind.EconomyHud]}
    ev5.Trigger(SceneBuilder.buildScene s5 c vs5)
    eprintfn "  %A" layer
    Threading.Thread.Sleep(1000)
(vh5 :> IDisposable).Dispose()
eprintfn "TEST 5 done.\n"

// ========== TEST 6: Event animations ==========
eprintfn "=== TEST 6: Event animations — 60 frames (10s) ==="
let g6 = mkGrid 64 64
let cfg6 = {VizDefaults.defaultConfig with ActiveOverlays=Set.ofList [OverlayKind.Units;OverlayKind.Events;OverlayKind.EconomyHud;OverlayKind.MetalSpots]}
let vs6 = {VizDefaults.defaultViewState with Scale=10.0f}
LayerRenderer.invalidateAll()
let scenes6 =
    [| for frame in 0..59 do
        let snap =
            MockSnapshot.emptySnapshot g6
            |> MockSnapshot.withFrame frame
            |> MockSnapshot.withFriendlyAt (200.0f,0.0f,200.0f)
            |> MockSnapshot.withFriendlyAt (300.0f,0.0f,150.0f)
            |> MockSnapshot.withEnemyAt (400.0f,0.0f,350.0f)
            |> MockSnapshot.withEconomy (500.0f+float32 frame*5.0f) 12.0f 8.0f 1000.0f
            |> MockSnapshot.withEnergyEconomy (800.0f-float32 frame*10.0f) 20.0f 25.0f 1500.0f
            |> MockSnapshot.withMetalSpots [|(160.0f,0.0f,160.0f,5.0f);(400.0f,0.0f,400.0f,3.0f)|]
        let snap =
            { snap with
                EventIndicators = [
                    {PositionX=200.0f;PositionY=0.0f;PositionZ=200.0f;Kind=EventKind.UnitCreated;FrameCreated=0;DurationFrames=30}
                    {PositionX=400.0f;PositionY=0.0f;PositionZ=350.0f;Kind=EventKind.Combat;FrameCreated=5;DurationFrames=20}
                    {PositionX=350.0f;PositionY=0.0f;PositionZ=250.0f;Kind=EventKind.EnemySpotted;FrameCreated=10;DurationFrames=40}
                    {PositionX=150.0f;PositionY=0.0f;PositionZ=300.0f;Kind=EventKind.UnitDestroyed;FrameCreated=20;DurationFrames=45} ] }
        SceneBuilder.buildScene snap cfg6 vs6 |]
eprintfn "  %d animation frames ready." scenes6.Length
playback "TEST 6: Event Animations" scenes6 10.0
eprintfn "TEST 6 done.\n"

eprintfn "=== All 6 visual tests complete ==="
