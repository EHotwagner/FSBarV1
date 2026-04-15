// Generated — do not edit by hand. Regenerate via scripts/gen-unit-labels.fsx.
namespace FSBar.Viz

/// Generated unit-label lookup table mapping each `BarData` unit internal
/// name to a unique 2- or 3-character display code.
/// Feature 028-unit-viz-language.
module UnitLabels =

    /// `BarData` package version this table was generated against.
    val BarDataVersion: string

    /// UTC ISO 8601 timestamp at which this table was generated.
    val GeneratedAtUtc: string

    /// Lookup: internal name → 2- or 3-char display code.
    /// Contract: no duplicate values; every value is 2 or 3 characters.
    val Labels: Map<string, string>

    /// Returns the code for an internal name, or `None` if unknown.
    val tryLookup: internalName: string -> string option

    /// Returns the code for an internal name, or `"??"` if unknown.
    val lookupOrFallback: internalName: string -> string
