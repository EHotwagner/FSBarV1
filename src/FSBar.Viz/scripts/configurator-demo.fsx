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
    // Skip tiny-footprint cosmetic / prop defs like `cor_hat_fightnight`
    // that get mis-classified as buildings. Real combat units have
    // footprints ≥ 2×2.
    allDefs
    |> List.filter (fun d ->
        factionOf d = faction
        && shapeOf d = shape
        && d.footprintX >= 2.0 && d.footprintZ >= 2.0)
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

// Demo zoom factor — inflates footprints so the glyph renderer produces
// radii in the 12–24 px range rather than the default 4–6 px. Emulates a
// zoomed-in battlefield view, appropriate for a style configurator where
// the point is to *see* the decorations (outlines, pips, arcs).
let private footprintZoom = 4.0f

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
      FootprintWidthElmo = float32 d.footprintX * 16.0f * footprintZoom
      FootprintHeightElmo = float32 d.footprintZ * 16.0f * footprintZoom
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

// Tableau occupies upper region; well-spaced so the larger zoomed-in
// glyphs (up to ~24 px radius, plus outline + label) have room to breathe.
let private colX = [ 240; 480; 720 ]          // faction columns (screen px)
let private rowZ = [ 170; 240; 310; 380; 450; 520 ]  // shape rows (screen px)

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

// --- Special showcase cells (bottom region) --------------------------------

// Five labeled cells below the tableau. Each cell has a title at the top and
// its subject unit(s) below. Cells are wide enough that annotations render
// comfortably above the glyphs.
let private specialHeaderZ = 620   // section title Y
let private specialLabelZ  = 655   // per-cell label Y
let private specialUnitZ   = 780   // glyph centre Y (well below labels)

// Cell centre X positions: five evenly-spaced columns in the 1160px usable
// area (left of the 280-px panel). Attack cell needs extra horizontal span
// for the attacker→target pair, so it gets a wider slot at the start.
let private cellXs = [ 80; 310; 500; 690; 900 ]

let private armadaAnyBot   = pickOne FactionId.Armada MovementShape.Bot   |> Option.get
let private armadaAnyTank  = pickOne FactionId.Armada MovementShape.Vehicle |> Option.get
let private cortexAnyBot   = pickOne FactionId.Cortex MovementShape.Bot   |> Option.get
let private cortexAnyTank  = pickOne FactionId.Cortex MovementShape.Vehicle |> Option.get
let private cortexAnyAir   = pickOne FactionId.Cortex MovementShape.Air   |> Option.get

// Cell 0: Attacker (team 0) → Target (team 1). Two glyphs side-by-side.
let private attackerXPx = cellXs.[0] + 40   // left side of cell
let private targetXPx   = cellXs.[0] + 190  // right side of cell
let private attackerUid = 100
let private targetUid   = 101
let private attacker =
    mkDisplay armadaAnyTank attackerUid 0 attackerXPx specialUnitZ 0.9f statusNone
let private target =
    mkDisplay cortexAnyTank targetUid 1 targetXPx specialUnitZ 0.45f
        { statusNone with JustDamagedWithinMs = Some 100 }

// Cell 1: Cloaked stealth unit.
let private stealthXPx = cellXs.[1] + 80
let private stealthUid = 102
let private stealth =
    mkDisplay armadaAnyBot stealthUid 0 stealthXPx specialUnitZ 1.0f
        { statusNone with IsCloaked = true }

// Cell 2: Heavily damaged unit (low HP).
let private woundedXPx = cellXs.[2] + 80
let private woundedUid = 103
let private wounded =
    mkDisplay cortexAnyBot woundedUid 1 woundedXPx specialUnitZ 0.18f
        { statusNone with JustDamagedWithinMs = Some 200 }

// Cell 3: Just-built unit (under-construction glow).
let private freshXPx = cellXs.[3] + 80
let private freshUid = 104
let private fresh =
    mkDisplay cortexAnyAir freshUid 1 freshXPx specialUnitZ 1.0f
        { statusNone with JustCompletedWithinMs = Some 400 }

// Cell 4: Stunned unit.
let private stunnedXPx = cellXs.[4] + 80
let private stunnedUid = 105
let private stunned =
    mkDisplay armadaAnyBot stunnedUid 0 stunnedXPx specialUnitZ 0.7f
        { statusNone with IsStunned = true }

let private specials = [ attacker; target; stealth; wounded; fresh; stunned ]

let private allUnits = tableauUnits @ specials

// --- Labels for the tableau rows / columns ---------------------------------

let private textPaint = Scene.fill (SKColor(230uy, 230uy, 230uy))
let private dimPaint  = Scene.fill (SKColor(160uy, 160uy, 170uy))
let private accentPaint = Scene.fill (SKColor(240uy, 180uy, 80uy))

let private cellLabels =
    // (x-centre, label)
    [ cellXs.[0] + 90, "ATTACK"
      cellXs.[1] + 80, "CLOAKED"
      cellXs.[2] + 80, "LOW HP"
      cellXs.[3] + 80, "JUST BUILT"
      cellXs.[4] + 80, "STUNNED" ]

let private cellSubLabels =
    [ cellXs.[0] + 90, "animated red projectile trail"
      cellXs.[1] + 80, "shimmer rings overlay"
      cellXs.[2] + 80, "18% health, rhythmic damage pulse"
      cellXs.[3] + 80, "completed <400 ms ago"
      cellXs.[4] + 80, "stunned desaturate" ]

let private buildChromeLabels () : Element list =
    [
      // Header
      yield Scene.text "Style Configurator Demo" 16.0f 28.0f 18.0f textPaint
      yield Scene.text "P  toggle panel        W L C N  overlays        S  screenshot"
                16.0f 54.0f 12.0f dimPaint
      yield Scene.text "Ctrl+C  quit" 16.0f 72.0f 12.0f dimPaint
      // Tableau column headers (Armada / Cortex / Legion) — pushed higher so
      // they never overlap the glyphs beneath.
      yield Scene.text "Every mobility type × faction" 16.0f 104.0f 13.0f accentPaint
      for (i, f) in List.indexed factions do
          let name =
              match f with
              | FactionId.Armada -> "Armada"
              | FactionId.Cortex -> "Cortex"
              | FactionId.Legion -> "Legion"
              | _ -> string f
          yield Scene.text name (float32 colX.[i] - 26.0f) 126.0f 14.0f textPaint
      // Tableau row labels on the left edge.
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
          yield Scene.text name 18.0f (float32 rowZ.[i] + 4.0f) 12.0f textPaint
      // Divider between tableau and special section
      let divY = float32 specialHeaderZ - 30.0f
      yield Scene.rect 16.0f divY 1120.0f 1.0f (Scene.fill (SKColor(64uy, 64uy, 72uy)))
      // Special section header
      yield Scene.text "Special cases"
                18.0f (float32 specialHeaderZ) 14.0f accentPaint
      // Per-cell labels (bold) and subtitles
      for (cx, lbl) in cellLabels do
          yield Scene.text lbl (float32 cx - 26.0f) (float32 specialLabelZ)
                    12.0f textPaint
      for (cx, sub) in cellSubLabels do
          yield Scene.text sub (float32 cx - 60.0f) (float32 specialLabelZ + 16.0f)
                    10.0f dimPaint
      // Sub-labels under the attacker/target glyphs
      yield Scene.text "A" (float32 attackerXPx - 3.0f)
                (float32 specialUnitZ + 28.0f) 10.0f dimPaint
      yield Scene.text "T" (float32 targetXPx - 3.0f)
                (float32 specialUnitZ + 28.0f) 10.0f dimPaint
    ]

// --- Config + panel state (mutable, single-threaded: render thread only) ---

// Zoomed-in starting style — bigger min radius, thicker strokes, larger
// labels. User can drag every one of these sliders back down via the
// configurator panel to restore the battlefield-scale default.
let private zoomedInStyle =
    { VizDefaults.defaultConfig.GlyphStyle with
        MinPixelRadius = 14.0f
        T1StrokeWidth  = 2.2f
        T2StrokeWidth  = 3.0f
        T3StrokeWidth  = 3.8f
        HpArcWidth     = 3.0f
        LabelFontSizePx = 13.0f
        LabelLegibilityZoomThreshold = 0.0f }  // always show labels

let mutable private config =
    { VizDefaults.defaultConfig with GlyphStyle = zoomedInStyle }
let mutable private panelState = ConfigPanel.toggle ConfigPanel.initialState  // open by default
let mutable private activePreset : string option = None
let mutable private referenceConfig = config

// --- Animation clock (driven by FrameTick) ---------------------------------

let mutable private animTime : float32 = 0.0f

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

// --- Animated attack: projectile trail moving attacker → target ------------
//
// A comet-like trail of ~5 dots moves along the line from A to T. Each frame
// we compute the head's normalised position (phase ∈ [0, 1]) from animTime,
// then lay down trailing dots at decreasing phases. Colour fades from
// near-white at the head through bright red to dark red at the tail — the
// colour progression requested in the demo brief.

let private attackLine () : Element =
    // Static faint guide line so the trajectory is always readable.
    let x1 = float32 attackerXPx + 10.0f
    let z1 = float32 specialUnitZ
    let x2 = float32 targetXPx  - 10.0f
    let z2 = float32 specialUnitZ
    Scene.line x1 z1 x2 z2 (Scene.stroke (SKColor(120uy, 30uy, 30uy, 100uy)) 1.0f)

let private attackTrail () : Element list =
    let x1 = float32 attackerXPx + 10.0f
    let z1 = float32 specialUnitZ
    let x2 = float32 targetXPx  - 10.0f
    let z2 = float32 specialUnitZ
    let period = 0.9f
    let headPhase = (animTime % period) / period
    // 6-dot tail, each offset ~0.065 of the travel behind the previous.
    let steps = 6
    [ for i in 0 .. steps - 1 ->
        let t = headPhase - float32 i * 0.065f
        if t < 0.0f then None
        else
            let x = x1 + (x2 - x1) * t
            let z = z1 + (z2 - z1) * t
            // Colour gradient along the tail:
            //   i = 0  light-white-red  (255, 230, 220)
            //   i = 1  bright red       (255, 100,  80)
            //   i = 2  red              (230,  50,  50)
            //   i = 3+ deep red         (160,  25,  25)
            let r, g, b =
                match i with
                | 0 -> 255uy, 230uy, 220uy
                | 1 -> 255uy, 140uy, 100uy
                | 2 -> 240uy,  70uy,  50uy
                | 3 -> 200uy,  30uy,  30uy
                | 4 -> 140uy,  20uy,  20uy
                | _ ->  90uy,  15uy,  15uy
            let alpha = byte (max 0 (230 - i * 35))
            let radius = 5.5f - float32 i * 0.55f
            Some (Scene.circle x z radius (Scene.fill (SKColor(r, g, b, alpha))))
    ] |> List.choose id

// --- Taking-damage effect --------------------------------------------------
//
// One "flash" of taking damage: an expanding red ring plus 4 spark
// particles radiating outward. `intensity` ∈ [0, 1] — 1 = just hit, 0 =
// faded. Pure function; callers compute the intensity from whatever clock
// they want (projectile impact cycle, rhythmic HP pulse, etc).

let private damageFlash (cx: float32) (cz: float32) (intensity: float32) : Element list =
    if intensity <= 0.0f then []
    else
        let inv = 1.0f - intensity
        let r = 10.0f + inv * 22.0f
        let alpha = byte (255.0f * intensity)
        let coreAlpha = byte (200.0f * intensity)
        let ring =
            Scene.circle cx cz r
                (Scene.stroke (SKColor(255uy, 80uy, 60uy, alpha))
                              (1.5f + intensity * 2.5f))
        // Inner bright glow at the unit centre for the first half.
        let glow =
            if intensity > 0.5f then
                [ Scene.circle cx cz 8.0f
                    (Scene.fill (SKColor(255uy, 220uy, 180uy, coreAlpha))) ]
            else []
        // 6 radiating spark particles.
        let nBurst = 6
        let particles =
            [ for i in 0 .. nBurst - 1 ->
                let ang = (float32 i / float32 nBurst) * 2.0f * float32 System.Math.PI
                let rr = 8.0f + inv * 18.0f
                let px = cx + (cos ang) * rr
                let pz = cz + (sin ang) * rr
                Scene.circle px pz (2.5f * intensity)
                    (Scene.fill (SKColor(255uy, 140uy, 90uy, alpha))) ]
        glow @ (ring :: particles)

// Intensity source for the attack target: flashes during the last 20% of
// each projectile cycle (when the trail head is landing).
let private attackImpactIntensity () =
    let period = 0.9f
    let headPhase = (animTime % period) / period
    if headPhase < 0.80f then 0.0f
    else 1.0f - (headPhase - 0.80f) / 0.20f

// Intensity source for the low-HP unit: rapid rhythmic pulse (~2.5 Hz).
let private lowHpDamageIntensity () =
    let hitPeriod = 0.45f
    let phase = (animTime % hitPeriod) / hitPeriod
    if phase < 0.35f then 1.0f - phase / 0.35f
    else 0.0f

// --- Cloaked unit: shimmer-ring shader animation ---------------------------
//
// Three concentric rings expand from the unit centre, each at a different
// animation phase, fading as they grow. Effect evokes active cloak-field
// shimmer without needing a pixel shader.

let private cloakEffect () : Element list =
    let cx = float32 stealthXPx
    let cz = float32 specialUnitZ
    let base1Col = SKColor(120uy, 220uy, 255uy)  // electric cyan
    let rings =
        [ for i in 0 .. 2 ->
            let phase = (animTime * 0.9f + float32 i * 0.33f) % 1.0f
            let r = 6.0f + phase * 22.0f
            let alpha = byte (220.0f * (1.0f - phase))
            let colr =
                SKColor(base1Col.Red, base1Col.Green, base1Col.Blue, alpha)
            Scene.circle cx cz r (Scene.stroke colr 1.3f) ]
    // Subtle desaturate / dim backing circle so the unit beneath appears
    // "phased out" even between ring pulses.
    let dim =
        Scene.circle cx cz 16.0f
            (Scene.fill (SKColor(40uy, 60uy, 90uy, 90uy)))
    dim :: rings

let private buildScene () : Scene =
    let style = withDemoTeamColors config.GlyphStyle
    let bg = Scene.rect 0.0f 0.0f (float32 winW) (float32 winH) bgPaint
    let chrome = buildChromeLabels ()
    let glyphs =
        UnitGlyph.buildUnitsGlyph allUnits style config.ActiveOverlays
    let attack = attackLine () :: attackTrail ()
    let cloak = cloakEffect ()
    // Taking-damage effects — target gets hit each projectile cycle; the
    // LOW-HP unit pulses continuously to show "under fire".
    let targetFlash =
        damageFlash (float32 targetXPx) (float32 specialUnitZ)
            (attackImpactIntensity ())
    let lowHpFlash =
        damageFlash (float32 woundedXPx) (float32 specialUnitZ)
            (lowHpDamageIntensity ())
    let presetNames = try StylePreset.listNames() with _ -> []
    let dirty = ConfigDescriptors.isDirty config referenceConfig
    let panelElems =
        ConfigPanel.buildPanel config { panelState with DirtyIndicator = dirty }
            (float32 winW) (float32 winH) presetNames activePreset
    // Composition: background, chrome text, underlying attack line, glyphs,
    // then effects overlay (attack trail + cloak shimmer) on top, then panel.
    let elements =
        [ yield bg
          yield! chrome
          yield! attack          // guide line + moving projectile dots
          yield! glyphs
          yield! targetFlash      // damage burst on attack target
          yield! lowHpFlash       // damage pulse on LOW HP unit
          yield! cloak            // shimmer rings above the stealth glyph
          yield! panelElems ]
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
    | InputEvent.FrameTick dt, _ ->
        animTime <- animTime + float32 dt
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
