namespace FSBar.Viz

open SkiaSharp

[<RequireQualifiedAccess>]
type InputKind =
    | ColorPicker
    | Slider of min: float32 * max: float32
    | IntSlider of min: int * max: int
    | Toggle
    | EnumChoice of labels: string list

[<RequireQualifiedAccess>]
type AttributeCategory =
    | Colors
    | Sizes
    | Strokes
    | Overlays
    | HealthDamage
    | Effects

type AttributeDescriptor =
    { Key: string
      Label: string
      Category: AttributeCategory
      InputKind: InputKind
      Get: VizConfig -> obj
      Set: obj -> VizConfig -> VizConfig
      Default: obj
      Range: (float32 * float32) option }

module ConfigDescriptors =

    let categoryLabel (c: AttributeCategory) : string =
        match c with
        | AttributeCategory.Colors -> "Colors"
        | AttributeCategory.Sizes -> "Sizes"
        | AttributeCategory.Strokes -> "Strokes"
        | AttributeCategory.Overlays -> "Overlays"
        | AttributeCategory.HealthDamage -> "Health/Damage"
        | AttributeCategory.Effects -> "Effects"

    let categoryOrder : AttributeCategory list =
        [ AttributeCategory.Colors
          AttributeCategory.Sizes
          AttributeCategory.Strokes
          AttributeCategory.Overlays
          AttributeCategory.HealthDamage
          AttributeCategory.Effects ]

    // --- Construction helpers --------------------------------------------

    let private colorDesc
        (key: string) (label: string) (cat: AttributeCategory)
        (get: VizConfig -> SKColor) (set: SKColor -> VizConfig -> VizConfig)
        (dflt: SKColor) =
        { Key = key
          Label = label
          Category = cat
          InputKind = InputKind.ColorPicker
          Get = (fun c -> box (get c))
          Set = (fun v c -> set (unbox<SKColor> v) c)
          Default = box dflt
          Range = None }

    let private floatDesc
        (key: string) (label: string) (cat: AttributeCategory)
        (minV: float32) (maxV: float32)
        (get: VizConfig -> float32) (set: float32 -> VizConfig -> VizConfig)
        (dflt: float32) =
        { Key = key
          Label = label
          Category = cat
          InputKind = InputKind.Slider(minV, maxV)
          Get = (fun c -> box (get c))
          Set = (fun v c ->
              let f = unbox<float32> v
              let clamped = max minV (min maxV f)
              set clamped c)
          Default = box dflt
          Range = Some (minV, maxV) }

    let private intDesc
        (key: string) (label: string) (cat: AttributeCategory)
        (minV: int) (maxV: int)
        (get: VizConfig -> int) (set: int -> VizConfig -> VizConfig)
        (dflt: int) =
        { Key = key
          Label = label
          Category = cat
          InputKind = InputKind.IntSlider(minV, maxV)
          Get = (fun c -> box (get c))
          Set = (fun v c ->
              let i = unbox<int> v
              let clamped = max minV (min maxV i)
              set clamped c)
          Default = box dflt
          Range = Some (float32 minV, float32 maxV) }

    let private boolDesc
        (key: string) (label: string) (cat: AttributeCategory)
        (get: VizConfig -> bool) (set: bool -> VizConfig -> VizConfig)
        (dflt: bool) =
        { Key = key
          Label = label
          Category = cat
          InputKind = InputKind.Toggle
          Get = (fun c -> box (get c))
          Set = (fun v c -> set (unbox<bool> v) c)
          Default = box dflt
          Range = None }

    let private enumDesc
        (key: string) (label: string) (cat: AttributeCategory) (labels: string list)
        (get: VizConfig -> string) (set: string -> VizConfig -> VizConfig)
        (dflt: string) =
        { Key = key
          Label = label
          Category = cat
          InputKind = InputKind.EnumChoice labels
          Get = (fun c -> box (get c))
          Set = (fun v c -> set (unbox<string> v) c)
          Default = box dflt
          Range = None }

    // --- Nested update helpers -------------------------------------------

    let private updateGlyph (f: UnitGlyphStyle -> UnitGlyphStyle) (cfg: VizConfig) =
        { cfg with GlyphStyle = f cfg.GlyphStyle }

    let private updateFaction (f: FactionPalette -> FactionPalette) (cfg: VizConfig) =
        updateGlyph (fun g -> { g with FactionPalette = f g.FactionPalette }) cfg

    let private updateTeam (f: TeamPalette -> TeamPalette) (cfg: VizConfig) =
        updateGlyph (fun g -> { g with TeamPalette = f g.TeamPalette }) cfg

    let private toggleOverlay (kind: OverlayKind) (on: bool) (cfg: VizConfig) =
        let next =
            if on then Set.add kind cfg.ActiveOverlays
            else Set.remove kind cfg.ActiveOverlays
        { cfg with ActiveOverlays = next }

    // --- Base layer enum --------------------------------------------------

    let private baseLayerLabels =
        [ "BaseTerrain"; "HeightMap"; "SlopeMap"; "ResourceMap"
          "LosMap"; "RadarMap"; "TerrainClassification" ]

    let private layerOfLabel (s: string) : LayerKind =
        match s with
        | "BaseTerrain" -> LayerKind.BaseTerrain
        | "HeightMap" -> LayerKind.HeightMap
        | "SlopeMap" -> LayerKind.SlopeMap
        | "ResourceMap" -> LayerKind.ResourceMap
        | "LosMap" -> LayerKind.LosMap
        | "RadarMap" -> LayerKind.RadarMap
        | "TerrainClassification" -> LayerKind.TerrainClassification
        | _ -> LayerKind.HeightMap

    let private labelOfLayer (l: LayerKind) : string =
        match l with
        | LayerKind.BaseTerrain -> "BaseTerrain"
        | LayerKind.HeightMap -> "HeightMap"
        | LayerKind.SlopeMap -> "SlopeMap"
        | LayerKind.ResourceMap -> "ResourceMap"
        | LayerKind.LosMap -> "LosMap"
        | LayerKind.RadarMap -> "RadarMap"
        | LayerKind.TerrainClassification -> "TerrainClassification"
        | LayerKind.Passability _ -> "HeightMap" // collapse Passability to a safe default

    // --- Descriptor list --------------------------------------------------

    let private defaults = VizDefaults.defaultConfig

    let all : AttributeDescriptor list =
        [
          // --- Colors (9) -----------------------------------------------
          colorDesc "colors.faction.armada" "Faction — Armada" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Armada)
              (fun v c -> updateFaction (fun p -> { p with Armada = v }) c)
              defaults.GlyphStyle.FactionPalette.Armada
          colorDesc "colors.faction.cortex" "Faction — Cortex" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Cortex)
              (fun v c -> updateFaction (fun p -> { p with Cortex = v }) c)
              defaults.GlyphStyle.FactionPalette.Cortex
          colorDesc "colors.faction.legion" "Faction — Legion" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Legion)
              (fun v c -> updateFaction (fun p -> { p with Legion = v }) c)
              defaults.GlyphStyle.FactionPalette.Legion
          colorDesc "colors.faction.raptors" "Faction — Raptors" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Raptors)
              (fun v c -> updateFaction (fun p -> { p with Raptors = v }) c)
              defaults.GlyphStyle.FactionPalette.Raptors
          colorDesc "colors.faction.scavengers" "Faction — Scavengers" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Scavengers)
              (fun v c -> updateFaction (fun p -> { p with Scavengers = v }) c)
              defaults.GlyphStyle.FactionPalette.Scavengers
          colorDesc "colors.faction.neutral" "Faction — Neutral" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.FactionPalette.Neutral)
              (fun v c -> updateFaction (fun p -> { p with Neutral = v }) c)
              defaults.GlyphStyle.FactionPalette.Neutral
          colorDesc "colors.team.fallback" "Team Fallback" AttributeCategory.Colors
              (fun c -> c.GlyphStyle.TeamPalette.Fallback)
              (fun v c -> updateTeam (fun p -> { p with Fallback = v }) c)
              defaults.GlyphStyle.TeamPalette.Fallback
          colorDesc "colors.background" "Background" AttributeCategory.Colors
              (fun c -> c.BackgroundColor)
              (fun v c -> { c with BackgroundColor = v })
              defaults.BackgroundColor
          colorDesc "colors.label" "Label" AttributeCategory.Colors
              (fun c -> c.LabelColor)
              (fun v c -> { c with LabelColor = v })
              defaults.LabelColor

          // --- Sizes (6) -------------------------------------------------
          floatDesc "sizes.unitMarker" "Unit Marker Size" AttributeCategory.Sizes 1.0f 32.0f
              (fun c -> c.UnitMarkerSize)
              (fun v c -> { c with UnitMarkerSize = v })
              defaults.UnitMarkerSize
          floatDesc "sizes.minGlyphRadius" "Min Glyph Radius" AttributeCategory.Sizes 1.0f 32.0f
              (fun c -> c.GlyphStyle.MinPixelRadius)
              (fun v c -> updateGlyph (fun g -> { g with MinPixelRadius = v }) c)
              defaults.GlyphStyle.MinPixelRadius
          floatDesc "sizes.facingPipRadius" "Facing Pip Radius" AttributeCategory.Sizes 0.0f 8.0f
              (fun c -> c.GlyphStyle.FacingPipRadius)
              (fun v c -> updateGlyph (fun g -> { g with FacingPipRadius = v }) c)
              defaults.GlyphStyle.FacingPipRadius
          floatDesc "sizes.labelFontSize" "Label Font Size (px)" AttributeCategory.Sizes 6.0f 32.0f
              (fun c -> c.GlyphStyle.LabelFontSizePx)
              (fun v c -> updateGlyph (fun g -> { g with LabelFontSizePx = v }) c)
              defaults.GlyphStyle.LabelFontSizePx
          floatDesc "sizes.labelLegibilityZoom" "Label Legibility Zoom" AttributeCategory.Sizes 0.0f 4.0f
              (fun c -> c.GlyphStyle.LabelLegibilityZoomThreshold)
              (fun v c -> updateGlyph (fun g -> { g with LabelLegibilityZoomThreshold = v }) c)
              defaults.GlyphStyle.LabelLegibilityZoomThreshold
          intDesc "sizes.gridLineSpacing" "Grid Line Spacing" AttributeCategory.Sizes 1 256
              (fun c -> c.GridLineSpacing)
              (fun v c -> { c with GridLineSpacing = v })
              defaults.GridLineSpacing

          // --- Strokes (4) -----------------------------------------------
          floatDesc "strokes.t1" "T1 Stroke Width" AttributeCategory.Strokes 0.5f 8.0f
              (fun c -> c.GlyphStyle.T1StrokeWidth)
              (fun v c -> updateGlyph (fun g -> { g with T1StrokeWidth = v }) c)
              defaults.GlyphStyle.T1StrokeWidth
          floatDesc "strokes.t2" "T2 Stroke Width" AttributeCategory.Strokes 0.5f 8.0f
              (fun c -> c.GlyphStyle.T2StrokeWidth)
              (fun v c -> updateGlyph (fun g -> { g with T2StrokeWidth = v }) c)
              defaults.GlyphStyle.T2StrokeWidth
          floatDesc "strokes.t3" "T3 Stroke Width" AttributeCategory.Strokes 0.5f 8.0f
              (fun c -> c.GlyphStyle.T3StrokeWidth)
              (fun v c -> updateGlyph (fun g -> { g with T3StrokeWidth = v }) c)
              defaults.GlyphStyle.T3StrokeWidth
          floatDesc "strokes.hpArc" "HP Arc Width" AttributeCategory.Strokes 0.5f 8.0f
              (fun c -> c.GlyphStyle.HpArcWidth)
              (fun v c -> updateGlyph (fun g -> { g with HpArcWidth = v }) c)
              defaults.GlyphStyle.HpArcWidth

          // --- Overlays (13) ---------------------------------------------
          enumDesc "overlays.baseLayer" "Base Layer" AttributeCategory.Overlays baseLayerLabels
              (fun c -> labelOfLayer c.BaseLayer)
              (fun v c -> { c with BaseLayer = layerOfLabel v })
              (labelOfLayer defaults.BaseLayer)
          floatDesc "overlays.opacity" "Overlay Opacity" AttributeCategory.Overlays 0.0f 1.0f
              (fun c -> c.OverlayOpacity)
              (fun v c -> { c with OverlayOpacity = v })
              defaults.OverlayOpacity
          boolDesc "overlays.showGridLines" "Show Grid Lines" AttributeCategory.Overlays
              (fun c -> c.ShowGridLines)
              (fun v c -> { c with ShowGridLines = v })
              defaults.ShowGridLines
          boolDesc "overlays.useGlyphRenderer" "Use Glyph Renderer" AttributeCategory.Overlays
              (fun c -> c.UseGlyphRenderer)
              (fun v c -> { c with UseGlyphRenderer = v })
              defaults.UseGlyphRenderer
          boolDesc "overlays.units" "Overlay: Units" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.Units c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.Units v c)
              (Set.contains OverlayKind.Units defaults.ActiveOverlays)
          boolDesc "overlays.events" "Overlay: Events" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.Events c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.Events v c)
              (Set.contains OverlayKind.Events defaults.ActiveOverlays)
          boolDesc "overlays.grid" "Overlay: Grid" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.Grid c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.Grid v c)
              (Set.contains OverlayKind.Grid defaults.ActiveOverlays)
          boolDesc "overlays.metalSpots" "Overlay: Metal Spots" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.MetalSpots c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.MetalSpots v c)
              (Set.contains OverlayKind.MetalSpots defaults.ActiveOverlays)
          boolDesc "overlays.economyHud" "Overlay: Economy HUD" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.EconomyHud c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.EconomyHud v c)
              (Set.contains OverlayKind.EconomyHud defaults.ActiveOverlays)
          boolDesc "overlays.weaponRanges" "Overlay: Weapon Ranges" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.WeaponRanges c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.WeaponRanges v c)
              (Set.contains OverlayKind.WeaponRanges defaults.ActiveOverlays)
          boolDesc "overlays.sightRanges" "Overlay: Sight Ranges" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.SightRanges c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.SightRanges v c)
              (Set.contains OverlayKind.SightRanges defaults.ActiveOverlays)
          boolDesc "overlays.commandQueue" "Overlay: Command Queue" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.CommandQueue c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.CommandQueue v c)
              (Set.contains OverlayKind.CommandQueue defaults.ActiveOverlays)
          boolDesc "overlays.fullNames" "Overlay: Full Names" AttributeCategory.Overlays
              (fun c -> Set.contains OverlayKind.FullNames c.ActiveOverlays)
              (fun v c -> toggleOverlay OverlayKind.FullNames v c)
              (Set.contains OverlayKind.FullNames defaults.ActiveOverlays)

          // --- Health/Damage (1) -----------------------------------------
          floatDesc "health.lowHpFraction" "Low HP Fraction" AttributeCategory.HealthDamage 0.0f 1.0f
              (fun c -> c.GlyphStyle.LowHpFraction)
              (fun v c -> updateGlyph (fun g -> { g with LowHpFraction = v }) c)
              defaults.GlyphStyle.LowHpFraction

          // --- Effects (2) -----------------------------------------------
          intDesc "effects.eventFlashMs" "Event Flash Duration (ms)" AttributeCategory.Effects 0 5000
              (fun c -> c.GlyphStyle.EventFlashDurationMs)
              (fun v c -> updateGlyph (fun g -> { g with EventFlashDurationMs = v }) c)
              defaults.GlyphStyle.EventFlashDurationMs
          intDesc "effects.justBuiltRingMs" "Just-Built Ring Duration (ms)" AttributeCategory.Effects 0 10000
              (fun c -> c.GlyphStyle.JustBuiltRingDurationMs)
              (fun v c -> updateGlyph (fun g -> { g with JustBuiltRingDurationMs = v }) c)
              defaults.GlyphStyle.JustBuiltRingDurationMs
        ]

    let private byKey : Map<string, AttributeDescriptor> =
        all |> List.map (fun d -> d.Key, d) |> Map.ofList

    let tryFind (key: string) : AttributeDescriptor option =
        Map.tryFind key byKey

    let applyValues (values: Map<string, obj>) (config: VizConfig) : VizConfig =
        values
        |> Map.fold (fun cfg key v ->
            match Map.tryFind key byKey with
            | Some d ->
                try d.Set v cfg
                with _ -> cfg
            | None -> cfg) config

    let extractValues (config: VizConfig) : Map<string, obj> =
        all
        |> List.map (fun d -> d.Key, d.Get config)
        |> Map.ofList

    let isDirty (current: VizConfig) (reference: VizConfig) : bool =
        all
        |> List.exists (fun d ->
            let a = d.Get current
            let b = d.Get reference
            not (System.Object.Equals(a, b)))
