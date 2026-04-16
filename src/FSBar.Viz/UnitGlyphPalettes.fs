namespace FSBar.Viz

open SkiaSharp

module UnitGlyphPalettes =

    // Vivid factional palette. Avoid blue (reserved for water) and
    // brown (reserved for ground). Team / alliance colours are separate
    // and vary per match, so the faction palette sticks to five widely
    // separated hues that read cleanly on both dark and light
    // backgrounds.
    let defaultFactionPalette : FactionPalette =
        { Armada     = SKColor(255uy,  64uy, 176uy)  // fuchsia / hot pink
          Cortex     = SKColor(255uy, 120uy,  32uy)  // pure orange
          Legion     = SKColor( 64uy, 240uy,  64uy)  // lime green
          Raptors    = SKColor(255uy, 220uy,  32uy)  // saturated yellow
          Scavengers = SKColor(176uy,  64uy, 255uy)  // vivid violet
          Neutral    = SKColor(170uy, 170uy, 170uy) }

    let defaultTeamPalette : TeamPalette =
        { ByTeamId = Map.empty
          Fallback = SKColor(128uy, 128uy, 128uy) }

    let defaults : UnitGlyphStyle =
        { FactionPalette = defaultFactionPalette
          TeamPalette = defaultTeamPalette
          MinPixelRadius = 4.0f
          T1StrokeWidth = 1.0f
          T2StrokeWidth = 1.75f
          T3StrokeWidth = 2.5f
          FacingPipRadius = 3.0f
          HpArcWidth = 1.5f
          LowHpFraction = 0.25f
          LabelFontSizePx = 9.0f
          LabelLegibilityZoomThreshold = 0.5f
          EventFlashDurationMs = 300
          JustBuiltRingDurationMs = 1000 }

    let withOverrides (f: UnitGlyphStyle -> UnitGlyphStyle) : UnitGlyphStyle =
        f defaults
