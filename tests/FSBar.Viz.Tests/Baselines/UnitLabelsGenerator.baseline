namespace FSBar.Viz

/// Pure, deterministic label generator used at build time by
/// `scripts/gen-unit-labels.fsx` to produce `UnitLabels.generated.fs`.
/// Feature 028-unit-viz-language.
///
/// Algorithm (research.md R3, two-pass):
///   Pass 1 — propose 2-char `Aa` labels for every unit in sorted order,
///            walking candidate letter pairs and preferring consonants;
///            fall back to 3-char `Aaa` labels when 2 chars cannot be made
///            unique.
///   Pass 2 — if a `previous` map is supplied, preserve each incumbent
///            unit's existing label wherever the slot is still achievable
///            (not taken by another preserved incumbent). Only forced
///            collisions cause reassignment.
module UnitLabelsGenerator =

    /// Generate a deterministic lookup from unit internal name to a 2- or
    /// 3-character display code. `previous` is the map committed by the
    /// prior generation (used for SC-006 stability). Pass `None` for a
    /// clean generation.
    val generate:
        names: string seq ->
        previous: Map<string, string> option ->
            Map<string, string>
