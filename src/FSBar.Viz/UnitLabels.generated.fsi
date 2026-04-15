// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.
namespace FSBar.Viz

/// Generated unit-label lookup table mapping each `BarData` unit internal
/// name to a 1- or 2-character display code. Uniqueness is guaranteed
/// per `(MovementShape, FactionId)` bucket — the rendered glyph's shape
/// and stroke colour already distinguish factions and movement classes,
/// so two units can share a label iff they differ in at least one of
/// those two dimensions.
/// Feature 028-unit-viz-language.
module UnitLabels =

    /// `BarData` package version this table was generated against.
    val BarDataVersion: string

    /// UTC ISO 8601 timestamp at which this table was generated.
    val GeneratedAtUtc: string

    /// Lookup: internal name → 1- or 2-character display code.
    /// Contract: unique within each (shape, faction) bucket; most
    /// entries are a single glyph, only the densest tails overflow
    /// to two characters.
    val Labels: Map<string, string>

    /// Returns the code for an internal name, or `None` if unknown.
    val tryLookup: internalName: string -> string option

    /// Returns the code for an internal name, or `"??"` if unknown.
    val lookupOrFallback: internalName: string -> string
