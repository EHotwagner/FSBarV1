module FSBar.Viz.Tests.UnitGlyphPerfTests

open System.Diagnostics
open Xunit
open FSBar.Viz

// SC-004 target: ≥ 30 fps for a 200-unit scene with the permanent layer and
// up to three overlays active (≤ 33 ms per frame). This test runs the glyph
// build path 200 iterations and checks the average per-frame budget.
//
// Tagged `Perf` so fast CI can skip it via `--filter "Category!=Perf"`.

let style = UnitGlyphPalettes.defaults

let defaultStatus : StatusFlags =
    { IsUnderConstruction = false
      IsStunned = false
      JustDamagedWithinMs = None
      JustCompletedWithinMs = None
      IsCloaked = false }

let build200Units () : UnitDisplay list =
    [ for i in 1 .. 200 ->
        let shape =
            match i % 6 with
            | 0 -> MovementShape.Bot
            | 1 -> MovementShape.Vehicle
            | 2 -> MovementShape.Hover
            | 3 -> MovementShape.Ship
            | 4 -> MovementShape.Air
            | _ -> MovementShape.Building
        let faction =
            match i % 6 with
            | 0 -> FactionId.Armada
            | 1 -> FactionId.Cortex
            | 2 -> FactionId.Legion
            | 3 -> FactionId.Raptors
            | 4 -> FactionId.Scavengers
            | _ -> FactionId.Neutral
        { UnitId = i
          DefId = i % 25
          InternalName = sprintf "unit%d" i
          Shape = shape
          Faction = faction
          Tier = Tier.T2
          LabelCode = "Pw"
          FootprintWidthElmo = 32.0f
          FootprintHeightElmo = 32.0f
          TeamId = i % 4
          PositionX = float32 (i * 12)
          PositionY = 0.0f
          PositionZ = float32 (i * 8)
          HeadingRadians = float32 i * 0.1f
          CurrentHealth = 80.0f
          MaxHealth = 100.0f
          BuildProgress = 1.0f
          Status = defaultStatus
          WeaponRangesElmo = [ 250.0f ]
          SightRangeElmo = 400.0f
          BuildRangeElmo = None
          CommandQueue = [] } ]

[<Fact>]
[<Trait("Category", "Perf")>]
let ``200-unit scene with 3 overlays stays under 33 ms per frame`` () =
    UnitGlyph.resetSession()
    let units = build200Units()
    let overlays =
        Set.ofList
            [ OverlayKind.WeaponRanges
              OverlayKind.SightRanges
              OverlayKind.CommandQueue ]

    // Warm-up pass to prime JIT + static cache.
    let _ = UnitGlyph.buildUnitsGlyph units style overlays

    let frames = 60
    let sw = Stopwatch.StartNew()
    for _ in 1 .. frames do
        let _ = UnitGlyph.buildUnitsGlyph units style overlays
        ()
    sw.Stop()

    let perFrameMs = float sw.ElapsedMilliseconds / float frames
    Assert.True(
        perFrameMs < 33.0,
        $"per-frame budget {perFrameMs:N2} ms exceeds SC-004 target (33 ms / 30 fps)")
