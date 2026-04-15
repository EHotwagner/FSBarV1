# Quickstart — Unit Visual Representation for SkiaViewer

**Feature**: 028-unit-viz-language
**Audience**: Developers implementing or reviewing the feature.

This quickstart shows how to exercise the new unit-glyph renderer end-to-end against a deterministic synthetic scene, from FSI and from a test runner.

---

## 1. Regenerate the label table

The label table is committed at `src/FSBar.Viz/UnitLabels.generated.fs`. Regenerate when `BarData` changes:

```bash
cd /home/developer/projects/FSBarV1
dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx
```

Expected output: a diff on `UnitLabels.generated.fs` showing only new/collided entries, with ≥ 95% of existing labels preserved. The script exits non-zero if any existing label would change without a genuine collision — investigate before committing.

---

## 2. Run the new tests

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~UnitGlyph|FullyQualifiedName~UnitLabels"
```

Coverage expected:
- `UnitLabelsGeneratorTests` — determinism, uniqueness, ≥ 90% 2-char rate, ≥ 95% stability.
- `UnitGlyphTests` — shape classifier rule stack, tier derivation with fallbacks, faction derivation with fallbacks.
- `UnitGlyphSceneTests` — scene composition on `FSBar.SyntheticData.SceneA/B/C`, overlay toggle independence, event-effect lifecycle.
- `SurfaceBaselineTests` — `FSBar.Viz.baseline` reflects the additive surface of `UnitGlyph`, `UnitGlyphPalettes`, and the new `VizTypes` declarations.

---

## 3. FSI walkthrough

```fsharp
// Load the Viz prelude (extended for feature 028).
#load "src/FSBar.Viz/scripts/prelude.fsx"
open FSBar.Viz
open FSBar.SyntheticData

// Pick a synthetic scene — any of SceneA/B/C.
let scene = Scenes.load SceneId.SceneA
let frame = scene.Frames.[0]

// Convert one live TrackedUnit to the display record via the adapter
// that SyntheticData now exposes (MVP — live-game adapter is follow-up).
let displays = SyntheticData.toUnitDisplays scene frame

// Default glyph style.
let style = UnitGlyphPalettes.defaults

// Build the unit-glyph scene fragment directly.
let glyphs =
    UnitGlyph.buildUnitsGlyph
        displays
        style
        (Set.ofList [OverlayKind.WeaponRanges; OverlayKind.SightRanges])

printfn "glyph primitives: %d" (List.length glyphs)
```

Expected: a non-empty primitive list, no classifier warnings (every synthetic unit resolves cleanly), and correct counts for the active overlays.

To render interactively instead of counting primitives:

```fsharp
#load "src/FSBar.Viz/scripts/examples/NN-unit-glyph.fsx"
```

The example script opens a SkiaViewer window on synthetic Scene A and toggles between permanent-only, `W`, `W+L`, and `W+L+C` so you can eyeball the composition.

---

## 4. Story-level smoke test

To exercise each user story's independent test criterion:

**Story 1 (P1) — permanent layer reads at a glance**:

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~UnitGlyphTests.PermanentLayer"
```

**Story 2 (P2) — sticky overlay toggles**:

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~UnitGlyphSceneTests.Overlay"
```

**Story 3 (P2) — unique labels**:

```bash
dotnet test tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj \
  --filter "FullyQualifiedName~UnitLabelsGeneratorTests"
```

**Story 4 (P3) — scale, zoom, declutter**: manual test via the FSI example script at three zoom levels (min / mid / max); check that 1x1 footprints are still visible at min zoom and that labels disappear below the legibility threshold.

---

## 5. Plug into `GameViz`

The glyph renderer is behind `VizConfig.UseGlyphRenderer`, which defaults to `true` in `VizDefaults.defaultConfig`. Any consumer that builds `VizConfig` from `VizDefaults.defaultConfig` gets the new renderer automatically. To explicitly opt in or out:

```fsharp
let cfg =
    { VizDefaults.defaultConfig with
        UseGlyphRenderer = true
        GlyphStyle = UnitGlyphPalettes.defaults }
GameViz.setConfig cfg
```

The legacy `OverlayKind.Units` path in `SceneBuilder.buildUnits` is only invoked when `UseGlyphRenderer = false`. Leaving the flag on routes unit drawing through `UnitGlyph.buildUnitsGlyph` instead.

---

## 6. What NOT to do in this feature

- Do not extend `FSBar.Client.TrackedUnit`. The live-game adapter is a follow-up feature.
- Do not implement the deferred overlays (`R E B T V I X`). They get dedicated follow-ups.
- Do not introduce a new rendering backend. The raster `SKSurface` path documented in `CLAUDE.md` is the only supported path.
- Do not touch surface-area baselines for projects other than `FSBar.Viz`.
