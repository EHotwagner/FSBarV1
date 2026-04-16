// Configurator demo — renders a rich synthetic scene alongside the style
// configurator panel so every visual attribute has something to affect.
//
// Layout (1440×900 window):
//   * 3 columns × 6 rows of units — one BarData-resolved unit per
//     (faction ∈ {Armada, Cortex, Legion}) × (shape ∈ Bot, Vehicle, Hover,
//     Ship, Air, Building).
//   * Extra row below: attacker → target pair (weapon range shown on A),
//     stealth unit (IsCloaked), damaged unit (flashing UnderAttackFlash),
//     and a "just built" unit (JustCompletedWithinMs).
//   * 280-px style configurator panel pinned on the right.
//
// Hotkeys:
//   * P  — toggle configurator panel
//   * W / L / C / N — toggle weapon ranges / sight / command queue / full names
//   * Ctrl+C to quit
//
// Run from repo root:
//   dotnet build tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj
//   dotnet fsi src/FSBar.Viz/scripts/configurator-demo.fsx

open System
open System.IO
open System.Runtime.InteropServices

let repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let testBin =
    Path.Combine(repoRoot, "tests", "FSBar.Viz.Tests", "bin", "Debug", "net10.0")

let nativeDir =
    Path.Combine(testBin, "runtimes", "linux-x64", "native")

#r "nuget: Silk.NET.Input.Common, 2.22.0"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/BarData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaSharp.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Viz.dll"
#r "../../../tests/FSBar.Viz.Tests/bin/Debug/net10.0/SkiaViewer.dll"

// Register a DllImport resolver for SkiaSharp so its P/Invoke calls find
// libSkiaSharp.so under `runtimes/linux-x64/native/` (the NuGet package
// layout). Must be done before the first native call — i.e. before any
// Scene.* function triggers SKFontManager's static constructor.
NativeLibrary.SetDllImportResolver(
    typeof<SkiaSharp.SKColor>.Assembly,
    fun libName _ _ ->
        match libName with
        | "libSkiaSharp" ->
            NativeLibrary.Load(Path.Combine(nativeDir, "libSkiaSharp.so"))
        | _ -> nativeint 0)

// GLFW uses runtime-unspecified names on Linux (libglfw.so.3). Preload it
// explicitly via dlopen so Silk.NET.Windowing.Glfw finds it at call time.
[<DllImport("libdl.so.2")>]
extern nativeint dlopen(string filename, int flags)
let private _glfw =
    dlopen(Path.Combine(nativeDir, "libglfw.so.3"), 0x2 ||| 0x100)

open FSBar.Viz
open SkiaSharp
open SkiaViewer
open Silk.NET.Input

// --- Window geometry --------------------------------------------------------

let private winW = 1440
let private winH = 900
let private panelW = int ConfigPanel.panelWidth

/// elmo = pixel * 8 so UnitGlyph.toVizX / 8 lands back on the pixel.
let private px (x: int) = float32 x * 8.0f

// --- Pick one BarData unit per (faction × shape) ---------------------------

let private shapeOf (d: BarData.UnitDef) =
    let canMove = match d.movement with Some m -> m.canMove | None -> false
    let canFly  = match d.movement with Some m -> m.canFly  | None -> false
    let mClass  = match d.movement with Some m -> m.movementClass | None -> None
    UnitGlyph.classifyShape canMove canFly mClass ignore

let private factionOf (d: BarData.UnitDef) =
    UnitGlyph.classifyFaction d.subfolder d.name ignore

let private tierOf (d: BarData.UnitDef) =
    UnitGlyph.classifyTier d.customParams d.category ignore

let private allDefs =
    BarData.AllUnitDefs.all |> List.map (fun (_, _, d) -> d)

let private pickOne (faction: FactionId) (shape: MovementShape) : BarData.UnitDef option =
    allDefs
    |> List.filter (fun d -> factionOf d = faction && shapeOf d = shape)
    |> List.sortBy (fun d ->
        let tierRank = match tierOf d with Tier.T1 -> 1 | Tier.T2 -> 2 | Tier.T3 -> 3
        tierRank, d.name)
    |> List.tryHead

let private factions = [ FactionId.Armada; FactionId.Cortex; FactionId.Legion ]
let private shapes =
    [ MovementShape.Bot; MovementShape.Vehicle; MovementShape.Hover
      MovementShape.Ship; MovementShape.Air; MovementShape.Building ]

// --- Default status helpers -------------------------------------------------

let private statusNone : StatusFlags =
    { IsUnderConstruction = false
      IsStunned = false
      JustDamagedWithinMs = None
      JustCompletedWithinMs = None
      IsCloaked = false }

let private mkDisplay
    (d: BarData.UnitDef)
    (uid: int) (teamId: int) (pxX: int) (pxZ: int)
    (hpFrac: float32) (status: StatusFlags)
    : UnitDisplay =
    let hpMax =
        match d.health with
        | BarData.ValueOrExpr.Concrete v -> float32 v
        | _ -> 100.0f
    let weaponRanges =
        match d.weapons with
        | Some ws ->
            ws |> List.choose (fun w ->
                match w.range with
                | Some (BarData.ValueOrExpr.Concrete r) -> Some (float32 r)
                | _ -> None)
            |> List.filter (fun r -> r > 0.0f)
        | None -> []
    let sightRange =
        match d.sightDistance with
        | BarData.ValueOrExpr.Concrete v -> float32 v
        | _ -> 0.0f
    { UnitId = uid
      DefId = uid
      InternalName = d.name
      Shape = shapeOf d
      Faction = factionOf d
      Tier = tierOf d
      LabelCode = UnitLabels.lookupOrFallback d.name
      FootprintWidthElmo = float32 d.footprintX * 16.0f
      FootprintHeightElmo = float32 d.footprintZ * 16.0f
      TeamId = teamId
      PositionX = px pxX
      PositionY = 0.0f
      PositionZ = px pxZ
      HeadingRadians = 0.0f
      CurrentHealth = hpMax * hpFrac
      MaxHealth = hpMax
      BuildProgress = 1.0f
      Status = status
      WeaponRangesElmo = weaponRanges
      SightRangeElmo = sightRange
      BuildRangeElmo = None
      CommandQueue = [] }

// --- Build the tableau ------------------------------------------------------

let private colX = [ 190; 320; 450 ]          // faction columns (screen px)
let private rowZ = [ 100; 170; 240; 310; 380; 450 ]  // shape rows (screen px)

let private tableauUnits : UnitDisplay list =
    [
        let mutable uid = 1
        for (colIdx, faction) in List.indexed factions do
            for (rowIdx, shape) in List.indexed shapes do
                match pickOne faction shape with
                | Some d ->
                    yield mkDisplay d uid colIdx (colX.[colIdx]) (rowZ.[rowIdx]) 1.0f statusNone
                    uid <- uid + 1
                | None ->
                    eprintfn "[demo] no BarData unit for %A / %A" faction shape
    ]

// --- Special units in the bottom row ---------------------------------------

let private specialRowZ = 560
let private armadaAnyBot = pickOne FactionId.Armada MovementShape.Bot |> Option.get
let private armadaAnyTank = pickOne FactionId.Armada MovementShape.Vehicle |> Option.get
let private cortexAnyBot = pickOne FactionId.Cortex MovementShape.Bot |> Option.get
let private cortexAnyTank = pickOne FactionId.Cortex MovementShape.Vehicle |> Option.get
let private cortexAnyAir = pickOne FactionId.Cortex MovementShape.Air |> Option.get

// 1. Attacker (team 0) → Target (team 1)
let private attackerUid = 100
let private targetUid = 101
let private attacker =
    mkDisplay armadaAnyTank attackerUid 0 150 specialRowZ 0.9f statusNone
let private target =
    mkDisplay cortexAnyTank targetUid 1 220 specialRowZ 0.45f
        { statusNone with JustDamagedWithinMs = Some 100 }

// 2. Stealth unit (IsCloaked) — this demo renders it with reduced alpha
let private stealthUid = 102
let private stealth =
    mkDisplay armadaAnyBot stealthUid 0 310 specialRowZ 1.0f
        { statusNone with IsCloaked = true }

// 3. Heavily damaged unit (low HP)
let private woundedUid = 103
let private wounded =
    mkDisplay cortexAnyBot woundedUid 1 370 specialRowZ 0.18f
        { statusNone with JustDamagedWithinMs = Some 200 }

// 4. Just-built unit
let private freshUid = 104
let private fresh =
    mkDisplay cortexAnyAir freshUid 1 430 specialRowZ 1.0f
        { statusNone with JustCompletedWithinMs = Some 400 }

// 5. Stunned unit
let private stunnedUid = 105
let private stunned =
    mkDisplay armadaAnyBot stunnedUid 0 500 specialRowZ 0.7f
        { statusNone with IsStunned = true }

let private specials = [ attacker; target; stealth; wounded; fresh; stunned ]

let private allUnits = tableauUnits @ specials

// --- Labels for the tableau rows / columns ---------------------------------

let private textPaint = Scene.fill (SKColor(230uy, 230uy, 230uy))
let private dimPaint  = Scene.fill (SKColor(160uy, 160uy, 170uy))
let private accentPaint = Scene.fill (SKColor(240uy, 180uy, 80uy))

let private buildChromeLabels () : Element list =
    [
      yield Scene.text "Style Configurator Demo" 16.0f 28.0f 18.0f textPaint
      yield Scene.text "Press  P  to toggle panel" 16.0f 52.0f 12.0f dimPaint
      yield Scene.text "Press  W L C N  for weapon / sight / commands / names overlays"
                16.0f 68.0f 12.0f dimPaint
      for (i, f) in List.indexed factions do
          let name =
              match f with
              | FactionId.Armada -> "Armada"
              | FactionId.Cortex -> "Cortex"
              | FactionId.Legion -> "Legion"
              | _ -> string f
          yield Scene.text name (float32 colX.[i] - 26.0f) 88.0f 14.0f textPaint
      for (i, s) in List.indexed shapes do
          let name =
              match s with
              | MovementShape.Bot -> "Bot"
              | MovementShape.Vehicle -> "Vehicle"
              | MovementShape.Hover -> "Hover"
              | MovementShape.Ship -> "Ship"
              | MovementShape.Air -> "Air"
              | MovementShape.Building -> "Building"
              | _ -> "?"
          yield Scene.text name 10.0f (float32 rowZ.[i] + 4.0f) 12.0f textPaint
      yield Scene.text "special:" 10.0f (float32 specialRowZ + 4.0f) 12.0f accentPaint
      yield Scene.text "A → T" (float32 (colX.[0] - 30)) (float32 specialRowZ - 12.0f)
                11.0f accentPaint
      yield Scene.text "stealth"    (float32 310 - 16.0f) (float32 specialRowZ - 12.0f)
                11.0f accentPaint
      yield Scene.text "low HP"     (float32 370 - 14.0f) (float32 specialRowZ - 12.0f)
                11.0f accentPaint
      yield Scene.text "just built" (float32 430 - 20.0f) (float32 specialRowZ - 12.0f)
                11.0f accentPaint
      yield Scene.text "stunned"    (float32 500 - 16.0f) (float32 specialRowZ - 12.0f)
                11.0f accentPaint
    ]

// --- Config + panel state (mutable, single-threaded: render thread only) ---

let mutable private config = VizDefaults.defaultConfig
let mutable private panelState = ConfigPanel.toggle ConfigPanel.initialState  // open by default
let mutable private activePreset : string option = None
let mutable private referenceConfig = config

// Give Team 0 a cyan fill and Team 1 a warm orange so the pair stands out.
let private withDemoTeamColors (style: UnitGlyphStyle) : UnitGlyphStyle =
    { style with
        TeamPalette =
            { style.TeamPalette with
                ByTeamId =
                    Map.ofList
                        [ 0, SKColor(80uy, 220uy, 255uy)
                          1, SKColor(255uy, 150uy, 60uy) ] } }

// --- Scene build ------------------------------------------------------------

let private bgPaint = Scene.fill (SKColor(20uy, 22uy, 28uy))

let private buildAttackLine () =
    // A dashed-looking line from attacker to target to signal combat intent.
    let x1 = float32 colX.[0] - 30.0f  // attacker pixel x
    let z1 = float32 specialRowZ
    let x2 = 220.0f                    // target pixel x
    let z2 = float32 specialRowZ
    let paint = Scene.stroke (SKColor(255uy, 80uy, 80uy, 180uy)) 2.0f
    Scene.line x1 z1 x2 z2 paint

let private applyCloakFade (elements: Element list) =
    // The glyph renderer doesn't bake IsCloaked into alpha, so we can't
    // easily post-apply opacity without walking the tree. Instead we draw
    // a translucent overlay rectangle over the stealth unit's cell to
    // signal it visually.
    let box =
        let x = 310.0f - 16.0f
        let z = float32 specialRowZ - 16.0f
        Scene.rect x z 32.0f 32.0f (Scene.fill (SKColor(40uy, 40uy, 60uy, 140uy)))
    elements @ [ box ]

let private buildScene () : Scene =
    let style = withDemoTeamColors config.GlyphStyle
    let bg = Scene.rect 0.0f 0.0f (float32 winW) (float32 winH) bgPaint
    let chrome = buildChromeLabels ()
    let glyphs =
        UnitGlyph.buildUnitsGlyph allUnits style config.ActiveOverlays
        |> applyCloakFade
    let attackLine = buildAttackLine ()
    let presetNames = try StylePreset.listNames() with _ -> []
    let dirty = ConfigDescriptors.isDirty config referenceConfig
    let panelElems =
        ConfigPanel.buildPanel config { panelState with DirtyIndicator = dirty }
            (float32 winW) (float32 winH) presetNames activePreset
    let elements =
        bg :: chrome @ (attackLine :: glyphs) @ panelElems
    Scene.create config.BackgroundColor elements

// --- Input ------------------------------------------------------------------

let private sceneEvt = Event<Scene>()

let private applyPanelAction (a: ConfigPanelAction) =
    match a with
    | ConfigPanelAction.SavePreset name ->
        let p = StylePreset.fromConfig name config
        match StylePreset.save p with
        | Result.Ok _ ->
            activePreset <- Some name
            referenceConfig <- config
        | Result.Error msg -> eprintfn "[demo] save failed: %s" msg
    | ConfigPanelAction.LoadPreset name ->
        match StylePreset.load name with
        | Result.Ok p ->
            config <- StylePreset.applyToConfig p config
            activePreset <- Some name
            referenceConfig <- config
        | Result.Error msg -> eprintfn "[demo] load failed: %s" msg
    | ConfigPanelAction.DeletePreset name ->
        match StylePreset.delete name with
        | Result.Ok _ ->
            if activePreset = Some name then activePreset <- None
        | Result.Error msg -> eprintfn "[demo] delete failed: %s" msg
    | ConfigPanelAction.ResetDefaults ->
        config <- VizDefaults.defaultConfig
        activePreset <- None
        referenceConfig <- VizDefaults.defaultConfig

let private toggleOverlay (kind: OverlayKind) =
    config <-
        { config with
            ActiveOverlays =
                if Set.contains kind config.ActiveOverlays
                then Set.remove kind config.ActiveOverlays
                else Set.add kind config.ActiveOverlays }

let private routeInput (evt: InputEvent) =
    let inPanel =
        match evt with
        | InputEvent.MouseDown(_, x, _)
        | InputEvent.MouseUp(_, x, _)
        | InputEvent.MouseMove(x, _)
        | InputEvent.MouseScroll(_, x, _) ->
            ConfigPanel.hitTest x 0.0f panelState (float32 winW)
            || panelState.ActiveControl.IsSome
        | _ -> false

    match evt, inPanel with
    | InputEvent.KeyDown Key.P, _ ->
        panelState <- ConfigPanel.toggle panelState
    | InputEvent.KeyDown Key.W, _ -> toggleOverlay OverlayKind.WeaponRanges
    | InputEvent.KeyDown Key.L, _ -> toggleOverlay OverlayKind.SightRanges
    | InputEvent.KeyDown Key.C, _ -> toggleOverlay OverlayKind.CommandQueue
    | InputEvent.KeyDown Key.N, _ -> toggleOverlay OverlayKind.FullNames
    | _, true ->
        let res =
            ConfigPanel.handleInput evt config panelState
                (float32 winW) (float32 winH)
        panelState <- res.PanelState
        match res.UpdatedConfig with Some c -> config <- c | None -> ()
        match res.Action with Some a -> applyPanelAction a | None -> ()
    | InputEvent.FrameTick _, _ ->
        sceneEvt.Trigger (buildScene ())
    | _ -> ()

// --- Viewer -----------------------------------------------------------------

let private viewerCfg : ViewerConfig =
    { Title = "FSBar.Viz — Style Configurator Demo"
      Width = winW
      Height = winH
      TargetFps = 60
      ClearColor = SKColor(20uy, 22uy, 28uy)
      PreferredBackend = Some Backend.Vulkan }

let private viewerHandle, inputs = Viewer.run viewerCfg sceneEvt.Publish

let private takeScreenshot () =
    match viewerHandle.Screenshot("/tmp/viz-verify") with
    | Result.Ok p -> printfn "[demo] screenshot saved: %s" p
    | Result.Error e -> eprintfn "[demo] screenshot failed: %s" e

// Wrap the input router to add a screenshot hotkey.
let private sub =
    inputs |> Observable.subscribe (fun evt ->
        match evt with
        | InputEvent.KeyDown Key.S -> takeScreenshot ()
        | _ -> routeInput evt)

sceneEvt.Trigger (buildScene ())

printfn "Configurator demo running."
printfn "  tableau: %d units  + %d special" (List.length tableauUnits) (List.length specials)
printfn "  hotkeys: P (panel) W L C N (overlays); S to screenshot; Ctrl+C to quit"

// If DEMO_SCREENSHOT=1, capture a screenshot after 1.5s and exit.
if Environment.GetEnvironmentVariable("DEMO_SCREENSHOT") = "1" then
    System.Threading.Thread.Sleep(1500)
    takeScreenshot ()
    System.Threading.Thread.Sleep(300)
    Environment.Exit(0)

// Block the script so the window stays alive.
System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite)
