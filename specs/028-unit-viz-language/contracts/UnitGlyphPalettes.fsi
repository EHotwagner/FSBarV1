namespace FSBar.Viz

open SkiaSharp

/// Default palettes and style values for the unit-glyph renderer.
/// Faction and team colors are separated so identity information is never
/// carried by a single color channel alone (spec Assumptions §6).
module UnitGlyphPalettes =

    /// Default faction stroke palette. Tuned for distinguishability against
    /// the default map background and the standard team fills.
    val defaultFactionPalette: FactionPalette

    /// Default team fill palette. Extends the existing `ColorMaps` team colors
    /// with a fallback for unknown team IDs.
    val defaultTeamPalette: TeamPalette

    /// Default full style. Consumers override fields through `VizConfig`.
    val defaults: UnitGlyphStyle

    /// Convenience constructor for a custom style that inherits defaults.
    val withOverrides: f: (UnitGlyphStyle -> UnitGlyphStyle) -> UnitGlyphStyle
