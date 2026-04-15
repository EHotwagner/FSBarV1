namespace FSBar.Viz

/// Pure, deterministic label generator used at build time by
/// `scripts/gen-unit-labels.fsx` to produce `UnitLabels.generated.fs`.
/// Feature 028-unit-viz-language.
///
/// Unit labels are partitioned by `(MovementShape, FactionId)` and only
/// required to be unique *within* that bucket — the rendered glyph's
/// shape + stroke colour already distinguishes factions and movement
/// types, so a one-glyph label reads unambiguously alongside them.
///
/// Allocation order per bucket:
///   1. Preserve any label from `previous` that is still unique within
///      the bucket (SC-006 stability).
///   2. For each remaining name, propose a single-character label
///      derived from the internal name (first consonant → any letter
///      → name digit), then sweep the global single-char pool (upper
///      Latin → lower Latin → digits → unique Greek letters).
///   3. When the single-char pool is exhausted in a bucket, fall back
///      to the two-char pool (`Aa` style) using the same name-derived
///      → exhaustive sweep policy. Only the densest shape+faction
///      tails of `BarData` should ever reach this path.
module UnitLabelsGenerator =

    /// Generate a deterministic lookup from unit internal name to a 1- or
    /// 2-character display code. Each item carries the shape and faction
    /// the unit renders as so labels can be uniquified per bucket.
    /// `previous` is the map committed by the prior generation (used for
    /// SC-006 stability). Pass `None` for a clean generation.
    val generate:
        items: (string * MovementShape * FactionId) seq ->
        previous: Map<string, string> option ->
            Map<string, string>
