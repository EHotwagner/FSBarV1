module FSBar.Viz.Tests.ConfigDescriptorsTests

open System
open Xunit
open SkiaSharp
open FSBar.Viz

let private cfg = VizDefaults.defaultConfig

[<Fact>]
let ``all returns a non-empty descriptor list`` () =
    Assert.NotEmpty(ConfigDescriptors.all)
    Assert.True(ConfigDescriptors.all.Length >= 30,
        sprintf "Expected at least 30 descriptors, got %d" ConfigDescriptors.all.Length)

[<Fact>]
let ``every descriptor has a unique key`` () =
    let keys = ConfigDescriptors.all |> List.map (fun d -> d.Key)
    let distinct = keys |> List.distinct
    Assert.Equal(keys.Length, distinct.Length)

[<Fact>]
let ``categoryOrder contains all six categories`` () =
    Assert.Equal(6, ConfigDescriptors.categoryOrder.Length)

[<Fact>]
let ``every descriptor category appears in categoryOrder`` () =
    for d in ConfigDescriptors.all do
        Assert.Contains(d.Category, ConfigDescriptors.categoryOrder)

[<Fact>]
let ``tryFind returns descriptor for known key`` () =
    let key = (List.head ConfigDescriptors.all).Key
    Assert.True(ConfigDescriptors.tryFind(key) |> Option.isSome)

[<Fact>]
let ``tryFind returns None for unknown key`` () =
    Assert.True(ConfigDescriptors.tryFind("no.such.key") |> Option.isNone)

[<Fact>]
let ``get/set roundtrip preserves values for all descriptors`` () =
    for d in ConfigDescriptors.all do
        let v = d.Get cfg
        let cfg' = d.Set v cfg
        let v' = d.Get cfg'
        Assert.True(Object.Equals(v, v'),
            sprintf "Roundtrip failed for '%s': got %A expected %A" d.Key v' v)

[<Fact>]
let ``extractValues produces one entry per descriptor`` () =
    let values = ConfigDescriptors.extractValues cfg
    Assert.Equal(ConfigDescriptors.all.Length, values.Count)

[<Fact>]
let ``applyValues is identity for extractValues roundtrip`` () =
    let values = ConfigDescriptors.extractValues cfg
    let cfg' = ConfigDescriptors.applyValues values cfg
    Assert.False(ConfigDescriptors.isDirty cfg' cfg)

[<Fact>]
let ``applyValues skips unknown keys`` () =
    let values = Map.ofList [ "no.such.key", box 42 ]
    let cfg' = ConfigDescriptors.applyValues values cfg
    Assert.Equal(cfg.UnitMarkerSize, cfg'.UnitMarkerSize)

[<Fact>]
let ``isDirty returns false for identical configs`` () =
    Assert.False(ConfigDescriptors.isDirty cfg cfg)

[<Fact>]
let ``isDirty returns true when a value differs`` () =
    let cfg' = { cfg with UnitMarkerSize = cfg.UnitMarkerSize + 1.0f }
    Assert.True(ConfigDescriptors.isDirty cfg' cfg)

[<Fact>]
let ``descriptor count matches VizConfig+UnitGlyphStyle fields plus overlays (SC-002)`` () =
    // Coverage mapping:
    //   VizConfig atoms:
    //     BaseLayer (1 enum descriptor)
    //     ActiveOverlays → 9 overlay toggle descriptors
    //     {UnitMarkerSize, OverlayOpacity, ShowGridLines, GridLineSpacing,
    //      BackgroundColor, LabelColor, UseGlyphRenderer} = 7 descriptors
    //   GlyphStyle atoms:
    //     FactionPalette → 6 descriptors (Armada/Cortex/Legion/Raptors/
    //                                     Scavengers/Neutral)
    //     TeamPalette.Fallback = 1 descriptor (ByTeamId map excluded —
    //                                          populated at runtime by host)
    //     {MinPixelRadius, T1/2/3StrokeWidth, FacingPipRadius, HpArcWidth,
    //      LowHpFraction, LabelFontSizePx, LabelLegibilityZoomThreshold,
    //      EventFlashDurationMs, JustBuiltRingDurationMs} = 11 descriptors
    //   Excluded: VizConfig.ColorSchemes (Map<LayerKind, ColorScheme> —
    //             per-layer schemes managed via separate API), TeamPalette.ByTeamId
    // Total: 1 + 9 + 7 + 6 + 1 + 11 = 35
    Assert.Equal(35, ConfigDescriptors.all.Length)

[<Fact>]
let ``every non-excluded VizConfig field is represented`` () =
    // VizConfig field → at least one descriptor key that mentions it
    // (excluding ColorSchemes and the nested GlyphStyle which fans out).
    let fieldKeywords =
        [ "BaseLayer", ["overlays.baseLayer"]
          "ActiveOverlays", ["overlays.units"; "overlays.events"; "overlays.grid";
                             "overlays.metalSpots"; "overlays.economyHud";
                             "overlays.weaponRanges"; "overlays.sightRanges";
                             "overlays.commandQueue"; "overlays.fullNames"]
          "UnitMarkerSize", ["sizes.unitMarker"]
          "OverlayOpacity", ["overlays.opacity"]
          "ShowGridLines", ["overlays.showGridLines"]
          "GridLineSpacing", ["sizes.gridLineSpacing"]
          "BackgroundColor", ["colors.background"]
          "LabelColor", ["colors.label"]
          "UseGlyphRenderer", ["overlays.useGlyphRenderer"] ]
    for fieldName, keys in fieldKeywords do
        for k in keys do
            Assert.True((ConfigDescriptors.tryFind k |> Option.isSome),
                sprintf "VizConfig.%s coverage missing descriptor: %s" fieldName k)

[<Fact>]
let ``every non-excluded UnitGlyphStyle field is represented`` () =
    let keys =
        [ "colors.faction.armada"; "colors.faction.cortex"
          "colors.faction.legion"; "colors.faction.raptors"
          "colors.faction.scavengers"; "colors.faction.neutral"
          "colors.team.fallback"
          "sizes.minGlyphRadius"; "strokes.t1"; "strokes.t2"; "strokes.t3"
          "sizes.facingPipRadius"; "strokes.hpArc"; "health.lowHpFraction"
          "sizes.labelFontSize"; "sizes.labelLegibilityZoom"
          "effects.eventFlashMs"; "effects.justBuiltRingMs" ]
    for k in keys do
        Assert.True((ConfigDescriptors.tryFind k |> Option.isSome),
            sprintf "UnitGlyphStyle coverage missing descriptor: %s" k)

[<Fact>]
let ``categoryLabel returns friendly display names`` () =
    Assert.Equal("Colors", ConfigDescriptors.categoryLabel AttributeCategory.Colors)
    Assert.Equal("Sizes", ConfigDescriptors.categoryLabel AttributeCategory.Sizes)
    Assert.Equal("Strokes", ConfigDescriptors.categoryLabel AttributeCategory.Strokes)
    Assert.Equal("Overlays", ConfigDescriptors.categoryLabel AttributeCategory.Overlays)
    Assert.Equal("Health/Damage", ConfigDescriptors.categoryLabel AttributeCategory.HealthDamage)
    Assert.Equal("Effects", ConfigDescriptors.categoryLabel AttributeCategory.Effects)

[<Fact>]
let ``all 9 OverlayKind cases have descriptors`` () =
    let overlayKeys =
        [ "overlays.units"; "overlays.events"; "overlays.grid"
          "overlays.metalSpots"; "overlays.economyHud"
          "overlays.weaponRanges"; "overlays.sightRanges"
          "overlays.commandQueue"; "overlays.fullNames" ]
    for k in overlayKeys do
        Assert.True((ConfigDescriptors.tryFind k |> Option.isSome),
            sprintf "Missing overlay descriptor: %s" k)

[<Fact>]
let ``all 6 faction colors have descriptors`` () =
    let factionKeys =
        [ "colors.faction.armada"; "colors.faction.cortex"
          "colors.faction.legion"; "colors.faction.raptors"
          "colors.faction.scavengers"; "colors.faction.neutral" ]
    for k in factionKeys do
        Assert.True((ConfigDescriptors.tryFind k |> Option.isSome),
            sprintf "Missing faction color descriptor: %s" k)

[<Fact>]
let ``color descriptor set applies to config`` () =
    match ConfigDescriptors.tryFind "colors.background" with
    | Some d ->
        let newColor = SKColor(100uy, 50uy, 25uy)
        let cfg' = d.Set (box newColor) cfg
        Assert.Equal(newColor, cfg'.BackgroundColor)
    | None -> Assert.True(false, "background color descriptor missing")

[<Fact>]
let ``float slider clamps to range`` () =
    match ConfigDescriptors.tryFind "sizes.unitMarker" with
    | Some d ->
        let cfg' = d.Set (box 1000.0f) cfg
        let v = d.Get cfg' |> unbox<float32>
        // Slider is 1..32
        Assert.True(v <= 32.0f, sprintf "Expected clamp to 32, got %f" v)
    | None -> Assert.True(false, "unit marker descriptor missing")

[<Fact>]
let ``overlay toggle descriptor flips ActiveOverlays set`` () =
    match ConfigDescriptors.tryFind "overlays.units" with
    | Some d ->
        let wasOn = Set.contains OverlayKind.Units cfg.ActiveOverlays
        let cfg' = d.Set (box (not wasOn)) cfg
        let isOn' = Set.contains OverlayKind.Units cfg'.ActiveOverlays
        Assert.NotEqual<bool>(wasOn, isOn')
    | None -> Assert.True(false, "overlays.units missing")
