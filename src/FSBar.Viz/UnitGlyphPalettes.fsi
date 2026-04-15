namespace FSBar.Viz

open SkiaSharp

/// Default palettes and style values for the unit-glyph renderer
/// (feature 028-unit-viz-language).
///
/// Faction and team colors are kept in separate tables so identity
/// information is never carried by a single color channel alone.
module UnitGlyphPalettes =

    /// Default faction stroke palette. Tuned for distinguishability
    /// against the default map background and standard team fills.
    val defaultFactionPalette: FactionPalette

    /// Default team fill palette. Wraps the existing ColorMaps team
    /// colors with a fallback for unknown team IDs.
    val defaultTeamPalette: TeamPalette

    /// Default full style. Consumers override fields through `VizConfig`.
    val defaults: UnitGlyphStyle

    /// Convenience constructor for a custom style that inherits defaults.
    val withOverrides: f: (UnitGlyphStyle -> UnitGlyphStyle) -> UnitGlyphStyle
