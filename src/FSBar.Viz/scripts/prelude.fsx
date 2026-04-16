// FSBar.Viz FSI Prelude
// Load with: #load "src/FSBar.Viz/scripts/prelude.fsx"

open System.Runtime.InteropServices

// Preload native libraries
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)

let private np = __SOURCE_DIRECTORY__ + "/../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/runtimes/linux-x64/native"
let _ = dlopen(np + "/libglfw.so.3", 0x2 ||| 0x100)
let _ = dlopen(np + "/libSkiaSharp.so", 0x2 ||| 0x100)

// Load assemblies from test output (has all transitive dependencies)
let private binDir = __SOURCE_DIRECTORY__ + "/../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Core.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Input.dll"
#r "../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/Silk.NET.Windowing.dll"

open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open SkiaSharp

/// Convert a SyntheticData GameState to a FSBar.Viz GameSnapshot.
let convertToSnapshot (scene: Scene) (gs: GameState) : GameSnapshot =
    let w = int scene.MapWidth / 8
    let h = int scene.MapHeight / 8
    let grid: MapGrid =
        { WidthElmos = int scene.MapWidth; HeightElmos = int scene.MapHeight
          WidthHeightmap = w; HeightHeightmap = h
          HeightMap = Array2D.zeroCreate (h + 1) (w + 1)
          SlopeMap = Array2D.zeroCreate (h / 2) (w / 2)
          ResourceMap = Array2D.zeroCreate h w
          LosMap = Array2D.zeroCreate h w
          RadarMap = Array2D.zeroCreate h w }
    let units =
        let friendlies =
            gs.Units |> Map.toSeq |> Seq.map (fun (id, u) ->
                let (px, py, pz) = u.Position
                id, { UnitId = id; PositionX = px; PositionY = py; PositionZ = pz
                      TeamId = gs.TeamId; DefId = u.DefId; Health = u.Health
                      MaxHealth = u.MaxHealth; IsEnemy = false })
        let enemies =
            gs.Enemies |> Map.toSeq |> Seq.choose (fun (id, e) ->
                if e.InLOS || e.InRadar then
                    let (px, py, pz) = e.Position
                    Some (id, { UnitId = id; PositionX = px; PositionY = py; PositionZ = pz
                                TeamId = 1; DefId = (e.DefId |> Option.defaultValue 0)
                                Health = (e.Health |> Option.defaultValue 100.0f)
                                MaxHealth = 100.0f; IsEnemy = true })
                else None)
        Map.ofSeq (Seq.append friendlies enemies)
    { FrameNumber = int gs.FrameNumber; MapGrid = grid; Units = units
      EventIndicators = []; EconomyMetal = { Current = gs.Metal.Current; Income = gs.Metal.Income; Usage = gs.Metal.Usage; Storage = gs.Metal.Storage }
      EconomyEnergy = { Current = gs.Energy.Current; Income = gs.Energy.Income; Usage = gs.Energy.Usage; Storage = gs.Energy.Storage }
      MetalSpots = [||]; Connected = true }

/// Preview a single synthetic scene frame.
let previewScene (sceneId: SceneId) (frameIdx: int) =
    let scene = Scenes.generate sceneId
    let snap = convertToSnapshot scene scene.Frames.[frameIdx]
    PreviewSession.startWithSnapshot snap

/// Play back a full synthetic scene.
let playScene (sceneId: SceneId) (fps: int) =
    let scene = Scenes.generate sceneId
    let snaps = scene.Frames |> Array.map (convertToSnapshot scene)
    PreviewSession.startPlayback snaps fps

// --- Configurator helpers (feature 033-viz-style-configurator) ---

/// Toggle the style configurator side panel in the live viewer.
let openConfigurator () = GameViz.toggleConfigPanel ()

/// List all saved style preset names.
let listPresets () = StylePreset.listNames ()

/// Save the current VizConfig as a preset with the given name.
let savePreset (name: string) (cfg: VizConfig) =
    StylePreset.fromConfig name cfg
    |> StylePreset.save

/// Load and apply a style preset to a VizConfig.
let loadPreset (name: string) (cfg: VizConfig) =
    match StylePreset.load name with
    | Result.Ok preset -> Some (StylePreset.applyToConfig preset cfg)
    | Result.Error _ -> None

/// Show the list of attribute descriptors available in the configurator.
let listDescriptors () =
    ConfigDescriptors.all
    |> List.map (fun d -> sprintf "%s  (%s)" d.Key d.Label)

printfn "FSBar.Viz prelude loaded."
printfn "  previewScene SceneId.SceneA 0  — preview a single frame"
printfn "  playScene SceneId.SceneA 30    — play back a scene at 30 FPS"
printfn "  openConfigurator ()            — toggle the style configurator (P in window)"
printfn "  listPresets ()                 — list saved style presets"
printfn "  listDescriptors ()             — list configurable attributes"
