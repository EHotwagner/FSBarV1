namespace FSBar.Viz

open SkiaSharp

module UnitGlyphPalettes =

    let defaultFactionPalette : FactionPalette =
        { Armada     = SKColor(120uy, 180uy, 255uy)
          Cortex     = SKColor(255uy, 120uy,  80uy)
          Legion     = SKColor(180uy, 255uy, 120uy)
          Raptors    = SKColor(255uy, 200uy,  40uy)
          Scavengers = SKColor(200uy,  80uy, 220uy)
          Neutral    = SKColor(180uy, 180uy, 180uy) }

    let defaultTeamPalette : TeamPalette =
        { ByTeamId = Map.empty
          Fallback = SKColor(128uy, 128uy, 128uy) }

    let defaults : UnitGlyphStyle =
        { FactionPalette = defaultFactionPalette
          TeamPalette = defaultTeamPalette
          MinPixelRadius = 4.0f
          T1StrokeWidth = 1.0f
          T2StrokeWidth = 2.0f
          T3StrokeWidth = 3.0f
          FacingPipRadius = 1.5f
          HpArcWidth = 1.5f
          LowHpFraction = 0.25f
          LabelFontSizePx = 9.0f
          LabelLegibilityZoomThreshold = 0.5f
          EventFlashDurationMs = 300
          JustBuiltRingDurationMs = 1000 }

    let withOverrides (f: UnitGlyphStyle -> UnitGlyphStyle) : UnitGlyphStyle =
        f defaults
