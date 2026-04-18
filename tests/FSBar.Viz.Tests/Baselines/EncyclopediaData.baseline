namespace FSBar.Viz

/// BarData-derived per-unit encyclopedia entries. Shared between the
/// Hub's Units-tab encyclopedia (`FSBar.Hub.App.Tabs.EncyclopediaTab`)
/// and the feature-038 `UnitDisplayAdapter.ofEncyclopediaEntry` that
/// builds static `UnitDisplay` previews from these entries.
///
/// The record is intentionally flat / immutable so both the Hub UI
/// and downstream adapters can consume it without threading a
/// `BarData.UnitDef` pointer through their render paths.
module EncyclopediaData =

    /// Pre-classified summary of one unit from `BarData.AllUnitDefs`.
    /// Classification uses the same `UnitGlyph.classifyShape` /
    /// `classifyTier` / `classifyFaction` helpers the live `GameViz`
    /// path uses, so an encyclopedia entry and an in-session glyph
    /// for the same internal name always agree on faction / tier /
    /// shape (feature 038 FR-002).
    type EncyclopediaEntry = {
        /// Stable index into `BarData.AllUnitDefs.all` at build time.
        /// Not an engine def-id; used only as a per-entry key for the
        /// Hub's list selection.
        DefId: int
        /// BarData `name` — the canonical internal name (e.g. `"armcom"`).
        InternalName: string
        /// BarData `printableName` — human-readable display name from the
        /// BAR Lua `name` field (e.g. `"Armada Commander"`). `None` when
        /// the source unit def didn't define one.
        HumanName: string option
        /// BarData `subfolder` — used for faction derivation.
        Subfolder: string
        Faction: FactionId
        Tier: Tier
        Shape: MovementShape
        MetalCost: int
        EnergyCost: int
        BuildTime: int
        Health: int
        /// Footprint X in BAR footprint-cells (1 cell = 16 elmos).
        FootprintX: int
        /// Footprint Z in BAR footprint-cells.
        FootprintZ: int
        SightRangeElmo: float32
        WeaponRangesElmo: float32 list
        /// BarData `movement.movementClass` (e.g. `"ATANK2"`, `"AHOVER1"`,
        /// `"ABOAT1"`). `None` for buildings and air units. Used by the
        /// Hub encyclopedia filter to identify amphibious units since
        /// `MovementShape` lumps bot / vehicle under `Ground`.
        MovementClass: string option
    }

    /// Materialises every `BarData.AllUnitDefs.all` entry into an
    /// `EncyclopediaEntry`. Runs once per Hub startup; takes
    /// ~10–20 ms on a typical dev box.
    val buildFromBarData: unit -> EncyclopediaEntry list
