namespace FSBar.Viz

open FSBar.Client

/// Single source of truth for translating upstream unit shapes into
/// `UnitDisplay` records before handing them to `UnitGlyph.buildUnit`.
/// FR-002 ("single shared code path") relies on every caller going
/// through this module instead of synthesising `UnitDisplay` values
/// inline.
module UnitDisplayAdapter =

    /// Resolve a friendly tracked unit (ours) to a display record.
    ///
    /// `defCache` is used to look up the unit's internal name by
    /// `DefId`; that name then drives faction / tier / shape /
    /// footprint classification via the same `BarData`-backed
    /// helpers that drive the Units-tab encyclopedia.
    ///
    /// When the cache has no entry for the def id yet (cold start),
    /// the returned record has `Faction = Neutral`, `Tier = T1`,
    /// `Shape = Bot`, and `LabelCode = "??"` — i.e. falls back to the
    /// legacy placeholder glyph. Callers SHOULD prime the cache at
    /// warmup so this only shows for net-new units in-flight.
    val ofTrackedUnit:
        defCache: UnitDefCache ->
        teamId: int ->
        unitId: int ->
        unit: TrackedUnit ->
            UnitDisplay

    /// Resolve a tracked enemy to a display record. Same fallback
    /// behaviour as `ofTrackedUnit` when the def id is unresolved.
    val ofTrackedEnemy:
        defCache: UnitDefCache ->
        enemyId: int ->
        enemy: TrackedEnemy ->
            UnitDisplay

    /// Resolve a BarData encyclopedia entry to a display record
    /// (static preview variant).
    ///
    /// `pinnedFootprint` lets the encyclopedia over-size the glyph
    /// for legibility — the Viewer tab passes the live footprint
    /// instead. `HeadingRadians` is `0.0f` — the feature-038 triangle
    /// pip is drawn "apex up" at heading zero by the renderer's
    /// convention (see `UnitGlyph` triangle-pip block). The triangle
    /// is suppressed for `MovementShape.Building` per FR-010.
    val ofEncyclopediaEntry:
        entry: EncyclopediaData.EncyclopediaEntry ->
        pinnedFootprint: float32 ->
            UnitDisplay
