namespace FSBar.Viz

open SkiaViewer

/// Renders units and buildings using the information-dense visual language
/// specified in feature 028-unit-viz-language. Consumers supply per-frame
/// `UnitDisplay` values (MVP: `FSBar.SyntheticData`), and this module produces
/// a `SkiaViewer.Scene` subtree suitable for composition into the main
/// `SceneBuilder.buildScene` output.
///
/// Classification, tier derivation, faction derivation, and label lookup
/// happen inside this module and are cached per `DefId` — callers do not
/// need to know about `BarData` at all.
module UnitGlyph =

    /// Classifies a `BarData.UnitDef` into one of the six movement shapes.
    /// Pure and total; `MovementShape.Unknown` is returned for unrecognized
    /// classes, and a one-shot structured warning is emitted through the
    /// supplied `logMiss` callback (allows tests to observe misses).
    val classifyShape:
        canMove: bool ->
        canFly: bool ->
        movementClass: string option ->
        logMiss: (string -> unit) ->
            MovementShape

    /// Derives tech tier from BarData fields per spec FR-005.
    val classifyTier:
        customParams: Map<string, string> ->
        category: string option ->
        logMiss: (string -> unit) ->
            Tier

    /// Derives faction from BarData subfolder + internal name per spec FR-004.
    val classifyFaction:
        subfolder: string ->
        internalName: string ->
        logMiss: (string -> unit) ->
            FactionId

    /// Builds the Scene subtree for a single `UnitDisplay` under the given style.
    /// Includes the permanent layer (shape, stroke, pip, HP arc, label,
    /// construction overlay, automatic event effects). Does NOT include overlay
    /// layers (weapon ranges, sight, command queue, full names) — those are
    /// added by `buildOverlayLayer` when the corresponding `OverlayKind` is
    /// active in the viz config.
    val buildUnit:
        unit': UnitDisplay ->
        style: UnitGlyphStyle ->
        activeEffects: EventEffect list ->
            Scene list

    /// Builds the overlay layer (weapon ranges, sight, command queue, full
    /// names) for a set of units given the currently-active overlays.
    val buildOverlayLayer:
        units: UnitDisplay seq ->
        style: UnitGlyphStyle ->
        activeOverlays: Set<OverlayKind> ->
            Scene list

    /// Builds the complete glyph-based unit layer (permanent + overlay).
    /// Intended to be called by `SceneBuilder.buildScene` when
    /// `VizConfig.UseGlyphRenderer = true`.
    val buildUnitsGlyph:
        units: UnitDisplay seq ->
        style: UnitGlyphStyle ->
        activeOverlays: Set<OverlayKind> ->
            Scene list

    /// Observes per-frame state deltas and advances the internal event-effect
    /// queue. Must be called once per rendered frame before `buildUnitsGlyph`.
    /// Pure in terms of its inputs/outputs except for the module-private
    /// effect queue, which is scoped per session and cleared by `resetSession`.
    val advanceEffects:
        previousFrame: Map<int, UnitDisplay> ->
        currentFrame: Map<int, UnitDisplay> ->
        nowMs: int ->
            EventEffect list

    /// Projects the currently-active overlay set onto the single-letter
    /// status-line string per FR-015. Stable ordering: W, L, C, N. Deferred
    /// overlays (R E B T V I X) are ignored until they are added to
    /// OverlayKind. Pure function — no side effects, no session state.
    val statusLine:
        activeOverlays: Set<OverlayKind> ->
            string

    /// Clears the module-private event-effect queue and per-`DefId` static
    /// classification cache. Called by `GameViz.start` / `GameViz.stop`.
    val resetSession: unit -> unit
