module FSBar.Viz.Tests.VizEngineFixture

open System.Runtime.InteropServices
open FSBar.Client
open FSBar.Viz
open FSBar.SyntheticData
open SkiaViewer

// Preload native libraries to avoid crashes when SkiaSharp types are used.
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)

let nativePath =
    System.IO.Path.Combine(
        System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
        "runtimes/linux-x64/native")

let _skiaLoaded = dlopen(nativePath + "/libSkiaSharp.so", 0x2 ||| 0x100)

/// Convert a FSBar.Client.GameState to a FSBar.Viz.GameSnapshot using scene metadata.
let convertToSnapshot (scene: FSBar.SyntheticData.Scene) (gs: GameState) : GameSnapshot =
    let mapW = int scene.MapWidth / 8
    let mapH = int scene.MapHeight / 8
    let grid = SyntheticMapGrid.build {| width = mapW; height = mapH; seed = None |}

    let units =
        let friendlyUnits =
            gs.Units |> Map.toList |> List.map (fun (uid, u: TrackedUnit) ->
                let (px, py, pz) = u.Position
                let us : UnitState =
                    { UnitId = uid
                      PositionX = px
                      PositionY = py
                      PositionZ = pz
                      TeamId = gs.TeamId
                      DefId = u.DefId
                      Health = u.Health
                      MaxHealth = u.MaxHealth
                      IsEnemy = false }
                (uid, us))
        let enemyUnits =
            gs.Enemies |> Map.toList |> List.map (fun (eid, e: TrackedEnemy) ->
                let (px, py, pz) = e.Position
                let us : UnitState =
                    { UnitId = eid
                      PositionX = px
                      PositionY = py
                      PositionZ = pz
                      TeamId = 1
                      DefId = e.DefId |> Option.defaultValue 0
                      Health = e.Health |> Option.defaultValue 100.0f
                      MaxHealth = 100.0f
                      IsEnemy = true }
                (eid, us))
        (friendlyUnits @ enemyUnits) |> Map.ofList

    let economyMetal : EconomyData =
        { Current = gs.Metal.Current
          Income = gs.Metal.Income
          Usage = gs.Metal.Usage
          Storage = gs.Metal.Storage }
    let economyEnergy : EconomyData =
        { Current = gs.Energy.Current
          Income = gs.Energy.Income
          Usage = gs.Energy.Usage
          Storage = gs.Energy.Storage }

    { FrameNumber = int gs.FrameNumber
      MapGrid = grid
      Units = units
      DisplayUnits = Map.empty
      EventIndicators = []
      EconomyMetal = economyMetal
      EconomyEnergy = economyEnergy
      MetalSpots = Array.empty
      Connected = true }

/// Check if DISPLAY is available for graphical tests.
let hasDisplay () =
    System.Environment.GetEnvironmentVariable("DISPLAY") <> null

/// Recursively collect all elements from a Scene into a flat list.
let rec flattenElement (e: Element) : Element list =
    match e with
    | Element.Group(_, _, _, children) -> e :: (children |> List.collect flattenElement)
    | _ -> [e]

let collectElements (scene: Scene) : Element list =
    scene.Elements |> List.collect flattenElement

/// Check if an element is an Ellipse.
let isEllipse (e: Element) =
    match e with
    | Element.Ellipse _ -> true
    | _ -> false

/// Check if an element is an Image.
let isImage (e: Element) =
    match e with
    | Element.Image _ -> true
    | _ -> false

/// Check if an element is a Text.
let isText (e: Element) =
    match e with
    | Element.Text _ -> true
    | _ -> false

/// Check if an element is a Rect.
let isRect (e: Element) =
    match e with
    | Element.Rect _ -> true
    | _ -> false

/// Check if an element is a Line.
let isLine (e: Element) =
    match e with
    | Element.Line _ -> true
    | _ -> false

/// Check if an element is a Group.
let isGroup (e: Element) =
    match e with
    | Element.Group _ -> true
    | _ -> false

/// Check if an element is a Path.
let isPath (e: Element) =
    match e with
    | Element.Path _ -> true
    | _ -> false

/// Extract text content from a Text element.
let textContent (e: Element) =
    match e with
    | Element.Text(text, _, _, _, _) -> Some text
    | _ -> None
