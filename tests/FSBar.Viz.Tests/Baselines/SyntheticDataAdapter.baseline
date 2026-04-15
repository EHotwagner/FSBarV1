namespace FSBar.Viz

open FSBar.Client
open FSBar.SyntheticData

/// Adapter from `FSBar.SyntheticData` scenes to the renderer's `UnitDisplay`
/// input. Feature 028-unit-viz-language — MVP data source is synthetic only;
/// a live-game adapter consuming `FSBar.Client.TrackedUnit` is a follow-up.
///
/// The adapter uses a small lookup table over the known synthetic DefIds to
/// populate `Shape`, `Faction`, `Tier`, and `LabelCode`. Unknown DefIds fall
/// back to `Bot`/`Neutral`/`T1` with a `??` label and no hard failure.
module SyntheticDataAdapter =

    /// Convert one `TrackedUnit` in a given scene to a `UnitDisplay` with a
    /// deterministic synthetic heading and buildProgress derived from frame
    /// number and unit id.
    val fromTrackedUnit:
        scene: Scene ->
        frame: int ->
        unit': TrackedUnit ->
            UnitDisplay

    /// Convert all units in one frame of a scene to `UnitDisplay` values.
    val toUnitDisplays:
        scene: Scene ->
        frame: GameState ->
            UnitDisplay seq
