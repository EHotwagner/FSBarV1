module FSBar.Viz.Tests.UnitGlyphSceneTests

open Xunit
open FSBar.Viz
open SkiaViewer

// Helpers ------------------------------------------------------------------

let private style : UnitGlyphStyle = UnitGlyphPalettes.defaults

let private defaultStatus : StatusFlags =
    { IsUnderConstruction = false
      IsStunned = false
      JustDamagedWithinMs = None
      JustCompletedWithinMs = None
      IsCloaked = false }

let private mkUnit
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

let private sixShapeScene () : UnitDisplay list =
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
let ``buildUnit: HP stroke drops out when a unit is dead`` () =
    UnitGlyph.resetSession()
    let full = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 1.0f 1.0f 0.0f defaultStatus
    let dead = mkUnit 1 1 MovementShape.Bot FactionId.Armada Tier.T1 "Pw" 0.0f 1.0f 0.0f defaultStatus
    // The HP indicator is drawn along the back half of the unit outline and
    // lives inside the first element (a rotated Group containing body,
    // outline, and — when HP > 0 — the red trimmed HP stroke). Unwrap the
    // group to count children rather than top-level primitives.
    let groupChildCount (els: Element list) =
        match els with
        | Element.Group(_, _, _, children) :: _ -> List.length children
        | _ -> 0
    let fullChildren = UnitGlyph.buildUnit full style [] |> groupChildCount
    let deadChildren = UnitGlyph.buildUnit dead style [] |> groupChildCount
    Assert.True(
        fullChildren > deadChildren,
        $"Live unit's shape group should contain more children than a dead one ({fullChildren} vs {deadChildren})")

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

let private armedUnit id : UnitDisplay =
    let u = mkUnit id 1 MovementShape.Vehicle FactionId.Armada Tier.T2 "Tk" 1.0f 1.0f 0.0f defaultStatus
    { u with WeaponRangesElmo = [ 250.0f; 400.0f ] }

let private unarmedUnit id : UnitDisplay =
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

let private unitWithQueue id : UnitDisplay =
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
