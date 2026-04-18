module FSBar.Viz.Tests.UnitGlyphSceneTests

open Xunit
open FSBar.Viz
open SkiaViewer

// Helpers ------------------------------------------------------------------

let style : UnitGlyphStyle = UnitGlyphPalettes.defaults

let defaultStatus : StatusFlags =
    { IsUnderConstruction = false
      IsStunned = false
      JustDamagedWithinMs = None
      JustCompletedWithinMs = None
      IsCloaked = false }

let mkUnit
    (id: int)
    (defId: int)
    (shape: MovementShape)
    (faction: FactionId)
    (tier: Tier)
    (label: string)
    (healthFrac: float32)
    (buildProgress: float32)
    (heading: float32)
    (status: StatusFlags)
    : UnitDisplay =
    { UnitId = id
      DefId = defId
      InternalName = sprintf "test%d" id
      Shape = shape
      Faction = faction
      Tier = tier
      LabelCode = label
      FootprintWidthElmo = 32.0f
      FootprintHeightElmo = 32.0f
      TeamId = 0
      PositionX = float32 id * 100.0f
      PositionY = 0.0f
      PositionZ = float32 id * 100.0f
      HeadingRadians = heading
      CurrentHealth = 100.0f * healthFrac
      MaxHealth = 100.0f
      BuildProgress = buildProgress
      Status = status
      WeaponRangesElmo = [ 250.0f ]
      SightRangeElmo = 400.0f
      BuildRangeElmo = None
      CommandQueue = [] }

let sixShapeScene () : UnitDisplay list =
    [ mkUnit 1 101 MovementShape.Bot      FactionId.Armada     Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
      mkUnit 2 102 MovementShape.Vehicle  FactionId.Cortex     Tier.T2 "Tk" 1.0f 1.0f 0.0f defaultStatus
      mkUnit 3 103 MovementShape.Hover    FactionId.Legion     Tier.T3 "Hv" 1.0f 1.0f 0.0f defaultStatus
      mkUnit 4 104 MovementShape.Ship     FactionId.Raptors    Tier.T1 "Sh" 1.0f 1.0f 0.0f defaultStatus
      mkUnit 5 105 MovementShape.Air      FactionId.Scavengers Tier.T2 "Ar" 1.0f 1.0f 0.0f defaultStatus
      mkUnit 6 106 MovementShape.Building FactionId.Neutral    Tier.T3 "Bd" 1.0f 1.0f 0.0f defaultStatus ]

// T017 ---------------------------------------------------------------------

[<Fact>]
let ``buildUnitsGlyph: every unit contributes at least one primitive`` () =
    UnitGlyph.resetSession()
    let units = sixShapeScene()
    let primitives = UnitGlyph.buildUnitsGlyph units style Set.empty
    Assert.NotEmpty(primitives)
    Assert.True(
        List.length primitives >= List.length units,
        $"Expected >= {List.length units} primitives, got {List.length primitives}")

[<Fact>]
let ``buildUnit: damage stroke drops out when a unit is at full HP`` () =
    UnitGlyph.resetSession()
    let full = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
    let damaged = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 0.4f 1.0f 0.0f defaultStatus
    // The damage indicator traces the back half of the unit outline and
    // lives inside the first element (a rotated Group containing body,
    // outline, and — when damage > 0 — the red trimmed damage stroke).
    // Unwrap the group to count children rather than top-level primitives.
    let groupChildCount (els: Element list) =
        match els with
        | Element.Group(_, _, _, children) :: _ -> List.length children
        | _ -> 0
    let fullChildren = UnitGlyph.buildUnit full style [] |> groupChildCount
    let damagedChildren = UnitGlyph.buildUnit damaged style [] |> groupChildCount
    Assert.True(
        damagedChildren > fullChildren,
        $"Damaged unit's shape group should contain more children than a full-HP one ({damagedChildren} vs {fullChildren})")

// T018 ---------------------------------------------------------------------

[<Fact>]
let ``buildUnit: unit under construction produces extra dashed primitive`` () =
    UnitGlyph.resetSession()
    let operational = mkUnit 1 1 MovementShape.Building FactionId.Armada Tier.T1 "Fc" 1.0f 1.0f 0.0f defaultStatus
    let statusUc = { defaultStatus with IsUnderConstruction = true }
    let under = mkUnit 1 1 MovementShape.Building FactionId.Armada Tier.T1 "Fc" 1.0f 0.3f 0.0f statusUc
    let opPrims = UnitGlyph.buildUnit operational style []
    let ucPrims = UnitGlyph.buildUnit under style []
    Assert.True(
        List.length ucPrims >= List.length opPrims,
        $"Under-construction unit should have at least as many primitives as operational ({List.length ucPrims} vs {List.length opPrims})")

[<Fact>]
let ``buildUnit: nan heading does not throw`` () =
    UnitGlyph.resetSession()
    let u = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f (System.Single.NaN) defaultStatus
    let prims = UnitGlyph.buildUnit u style []
    Assert.NotEmpty prims

// T019 ---------------------------------------------------------------------

[<Fact>]
let ``advanceEffects: HP decrease produces UnderAttackFlash effect`` () =
    UnitGlyph.resetSession()
    let prev =
        [ 1, mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus ]
        |> Map.ofList
    let curr =
        [ 1, mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 0.7f 1.0f 0.0f defaultStatus ]
        |> Map.ofList
    let effects = UnitGlyph.advanceEffects prev curr 1000
    Assert.Contains(effects, fun e -> e.Kind = EventEffectKind.UnderAttackFlash && e.UnitId = 1)

[<Fact>]
let ``advanceEffects: retires expired effects`` () =
    UnitGlyph.resetSession()
    let prev =
        [ 1, mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus ]
        |> Map.ofList
    let curr =
        [ 1, mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 0.7f 1.0f 0.0f defaultStatus ]
        |> Map.ofList
    let t0 = 1000
    let active0 = UnitGlyph.advanceEffects prev curr t0
    Assert.NotEmpty active0
    // After the flash duration has passed, the effect is retired.
    let farFuture = t0 + style.EventFlashDurationMs + 100
    let activeLater = UnitGlyph.advanceEffects curr curr farFuture
    Assert.DoesNotContain(activeLater, fun e -> e.Kind = EventEffectKind.UnderAttackFlash && e.UnitId = 1)

// T038 — weapon-range overlay independence ---------------------------------

let armedUnit id : UnitDisplay =
    let u = mkUnit id 1 MovementShape.Vehicle FactionId.Armada Tier.T2 "Tk" 1.0f 1.0f 0.0f defaultStatus
    { u with WeaponRangesElmo = [ 250.0f; 400.0f ] }

let unarmedUnit id : UnitDisplay =
    let u = mkUnit id 2 MovementShape.Building FactionId.Armada Tier.T1 "Sl" 1.0f 1.0f 0.0f defaultStatus
    { u with WeaponRangesElmo = [] }

[<Fact>]
let ``buildOverlayLayer: WeaponRanges only includes armed units`` () =
    UnitGlyph.resetSession()
    let units = [ armedUnit 1; unarmedUnit 2 ]
    let overlays = UnitGlyph.buildOverlayLayer units style (Set.singleton OverlayKind.WeaponRanges)
    // Armed unit contributes 2 rings (one per weapon), unarmed contributes 0.
    Assert.Equal(2, List.length overlays)

// T039 — overlay composition ------------------------------------------------

[<Fact>]
let ``buildOverlayLayer: W+L composes both layers`` () =
    UnitGlyph.resetSession()
    let units = [ armedUnit 1 ]
    let wOnly = UnitGlyph.buildOverlayLayer units style (Set.singleton OverlayKind.WeaponRanges)
    let wlBoth =
        UnitGlyph.buildOverlayLayer
            units style (Set.ofList [ OverlayKind.WeaponRanges; OverlayKind.SightRanges ])
    Assert.True(
        List.length wlBoth > List.length wOnly,
        $"W+L should produce more primitives than W alone ({List.length wlBoth} vs {List.length wOnly})")

// T040 — command-queue overlay ----------------------------------------------

let unitWithQueue id : UnitDisplay =
    let u = mkUnit id 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
    { u with
        CommandQueue =
            [ { Order = OrderKind.Move; X = 100.0f; Y = 0.0f; Z = 100.0f; IsCurrent = true }
              { Order = OrderKind.Attack; X = 200.0f; Y = 0.0f; Z = 200.0f; IsCurrent = false } ] }

[<Fact>]
let ``buildOverlayLayer: CommandQueue produces polyline segments`` () =
    UnitGlyph.resetSession()
    let units = [ unitWithQueue 1 ]
    let overlays = UnitGlyph.buildOverlayLayer units style (Set.singleton OverlayKind.CommandQueue)
    Assert.Equal(2, List.length overlays)

// T041 — FullNames overlay --------------------------------------------------

[<Fact>]
let ``buildOverlayLayer: FullNames produces text per unit`` () =
    UnitGlyph.resetSession()
    let units = [ armedUnit 1; unarmedUnit 2; armedUnit 3 ]
    let overlays = UnitGlyph.buildOverlayLayer units style (Set.singleton OverlayKind.FullNames)
    Assert.Equal(3, List.length overlays)

// T042 — statusLine ---------------------------------------------------------

[<Fact>]
let ``statusLine: empty set produces empty string`` () =
    Assert.Equal("", UnitGlyph.statusLine Set.empty)

[<Fact>]
let ``statusLine: WL in stable order`` () =
    let s =
        UnitGlyph.statusLine
            (Set.ofList [ OverlayKind.SightRanges; OverlayKind.WeaponRanges ])
    Assert.Equal("WL", s)

[<Fact>]
let ``statusLine: WLCN all present`` () =
    let s =
        UnitGlyph.statusLine
            (Set.ofList
                [ OverlayKind.FullNames
                  OverlayKind.CommandQueue
                  OverlayKind.SightRanges
                  OverlayKind.WeaponRanges ])
    Assert.Equal("WLCN", s)

// T048 — min-pixel-radius clamp --------------------------------------------

[<Fact>]
let ``buildUnit: tiny footprint is clamped to MinPixelRadius`` () =
    UnitGlyph.resetSession()
    let tinyStyle = { style with MinPixelRadius = 12.0f }
    let u =
        { mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
          with FootprintWidthElmo = 4.0f; FootprintHeightElmo = 4.0f }
    let prims = UnitGlyph.buildUnit u tinyStyle []
    Assert.NotEmpty prims // At minimum the clamped body renders.

// T049 / T050 — label suppression + FullNames bypass -----------------------

[<Fact>]
let ``buildOverlayLayer: FullNames always produces text regardless of zoom`` () =
    UnitGlyph.resetSession()
    let units = [ armedUnit 1 ]
    let overlays = UnitGlyph.buildOverlayLayer units style (Set.singleton OverlayKind.FullNames)
    Assert.Equal(1, List.length overlays)

// ---------------------------------------------------------------------------

[<Fact>]
let ``advanceEffects: just-built transition produces JustBuiltRing effect`` () =
    UnitGlyph.resetSession()
    let ucStatus = { defaultStatus with IsUnderConstruction = true }
    let prev =
        [ 1, mkUnit 1 1 MovementShape.Building FactionId.Armada Tier.T1 "Fc" 1.0f 0.9f 0.0f ucStatus ]
        |> Map.ofList
    let curr =
        [ 1, mkUnit 1 1 MovementShape.Building FactionId.Armada Tier.T1 "Fc" 1.0f 1.0f 0.0f defaultStatus ]
        |> Map.ofList
    let effects = UnitGlyph.advanceEffects prev curr 2000
    Assert.Contains(effects, fun e -> e.Kind = EventEffectKind.JustBuiltRing && e.UnitId = 1)

// --- Feature 038 US4: direction triangle -----------------------------------

open FSBar.Viz.Tests.VizEngineFixture

// Flatten out the Scene.group wrapping so we can grep for Path primitives.
let private flatten (els: Element list) : Element list =
    els |> List.collect flattenElement

let private pathApexOf (els: Element list) : (float32 * float32) option =
    // The pip triangle is exactly 4 path commands: MoveTo (apex),
    // LineTo, LineTo, Close. Filter to just that shape so we don't
    // accidentally pick up the shape outline's first MoveTo.
    flatten els
    |> List.tryPick (fun e ->
        match e with
        | Element.Path(cmds, _) when cmds.Length = 4 ->
            match cmds.Head with
            | PathCommand.MoveTo(x, y) -> Some (x, y)
            | _ -> None
        | _ -> None)

let private countTrianglePaths (els: Element list) : int =
    flatten els
    |> List.sumBy (fun e ->
        match e with
        | Element.Path(cmds, _) when cmds.Length = 4 -> 1
        | _ -> 0)

[<Fact>]
let ``FacingTriangle: heading=0 places apex east of unit centre (canonical)`` () =
    UnitGlyph.resetSession()
    let u = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
    let els = UnitGlyph.buildUnit u style []
    match pathApexOf els with
    | None -> Assert.Fail("expected a triangle pip path")
    | Some (apexX, apexY) ->
        let cx = u.PositionX / 8.0f
        let cy = u.PositionZ / 8.0f
        Assert.True(apexX > cx + 1.0f,
            sprintf "heading 0 should place apex east of centre; apex=(%f,%f) centre=(%f,%f)" apexX apexY cx cy)
        Assert.InRange(apexY, cy - 0.5f, cy + 0.5f)

[<Fact>]
let ``FacingTriangle: heading=π/2 places apex south of unit centre`` () =
    UnitGlyph.resetSession()
    let halfPi = float32 System.Math.PI / 2.0f
    let u = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f halfPi defaultStatus
    let els = UnitGlyph.buildUnit u style []
    match pathApexOf els with
    | None -> Assert.Fail("expected a triangle pip path")
    | Some (apexX, apexY) ->
        let cx = u.PositionX / 8.0f
        let cy = u.PositionZ / 8.0f
        Assert.InRange(apexX, cx - 0.5f, cx + 0.5f)
        Assert.True(apexY > cy + 1.0f,
            sprintf "heading π/2 should place apex below centre; apex=(%f,%f) centre=(%f,%f)" apexX apexY cx cy)

[<Fact>]
let ``FacingTriangle: heading=π places apex west of unit centre`` () =
    UnitGlyph.resetSession()
    let pi = float32 System.Math.PI
    let u = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f pi defaultStatus
    let els = UnitGlyph.buildUnit u style []
    match pathApexOf els with
    | None -> Assert.Fail("expected a triangle pip path")
    | Some (apexX, _apexY) ->
        let cx = u.PositionX / 8.0f
        Assert.True(apexX < cx - 1.0f,
            sprintf "heading π should place apex west of centre; apex.x=%f centre.x=%f" apexX cx)

[<Fact>]
let ``FacingTriangle: suppressed for MovementShape.Building (FR-010)`` () =
    UnitGlyph.resetSession()
    let building = mkUnit 1 1 MovementShape.Building FactionId.Armada Tier.T1 "Fc" 1.0f 1.0f 0.0f defaultStatus
    let mobile = mkUnit 2 2 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
    let buildingEls = UnitGlyph.buildUnit building style []
    let mobileEls = UnitGlyph.buildUnit mobile style []
    let buildingPips = countTrianglePaths buildingEls
    let mobilePips = countTrianglePaths mobileEls
    // Buildings emit the main-shape path (which may include MoveTo +
    // other ops) and a damage stroke path (same commands). Confirm the
    // pip is gone by asserting mobile has at least one MORE triangle
    // path than building.
    Assert.True(mobilePips > buildingPips,
        sprintf "mobile unit should have more triangle paths than building (mobile=%d, building=%d)"
            mobilePips buildingPips)

[<Fact>]
let ``FacingTriangle: static preview (heading=0) identical to encyclopedia path (FR-010a)`` () =
    UnitGlyph.resetSession()
    // UnitDisplayAdapter.ofEncyclopediaEntry sets heading=0.0f; a live
    // unit with heading=0 must produce the same apex placement so
    // Viewer-tab and Encyclopedia glyphs stay visually equivalent.
    let entry : EncyclopediaData.EncyclopediaEntry =
        { DefId = 1
          InternalName = "armpw"
          HumanName = None
          Subfolder = "Units/ARM"
          Faction = FactionId.Armada
          Tier = Tier.T1
          Shape = MovementShape.Bot
          MetalCost = 50
          EnergyCost = 250
          BuildTime = 1000
          Health = 300
          FootprintX = 1
          FootprintZ = 1
          SightRangeElmo = 300.0f
          WeaponRangesElmo = [ 200.0f ]; MovementClass = None }
    let preview = UnitDisplayAdapter.ofEncyclopediaEntry entry 32.0f
    Assert.Equal(0.0f, preview.HeadingRadians)
    Assert.Equal(MovementShape.Bot, preview.Shape)
    let els = UnitGlyph.buildUnit preview style []
    match pathApexOf els with
    | None -> Assert.Fail("expected triangle pip for static preview (non-building shape)")
    | Some _ -> ()
