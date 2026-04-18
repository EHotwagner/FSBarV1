(**
---
title: Visualization
category: Tutorials
categoryindex: 2
index: 3
description: FSBar.Viz — glyph language, scene builder, live viewer, style configurator.
---
*)

(**
# Visualization

`FSBar.Viz` renders BAR game state through a declarative scene API built on SkiaSharp + Silk.NET (via the in-house `SkiaViewer` package).

## Entry points

- **`GameViz`** — live attach to a `BarClient`. Opens a window, streams frames, supports hotkey overlays.
- **`SceneBuilder`** — pure function `GameState -> Scene`. Used by the Hub Viewer tab, preview thumbnails, and screenshots.
- **`PreviewSession`** — headless framebuffer render for thumbnails and CI screenshots.

## Unit glyph language

`FSBar.Viz.UnitGlyph` is the information-dense renderer behind `VizConfig.UseGlyphRenderer` (default `true`). Every unit draws as:

- **Shape** — movement class (ground / hover / ship / air / building)
- **Fill** — team color
- **Stroke** — faction (arm / core / legion / …)
- **Tier badge** — T1 / T2 / T3 / commander
- **Label** — 2- or 3-char code from the committed `UnitLabels.generated.fs` table
- **Facing pip** — triangle pointing along `HeadingRadians` (suppressed for buildings)

Hotkeys layer additional overlays on the glyph: `W` weapon ranges, `L` sight, `C` command queue, `N` full names, `P` style configurator.

## Unit display adapter

`UnitDisplayAdapter` is the single constructor for `UnitDisplay` values across every surface (live viewer, encyclopedia, style preview). Three entry points:

```fsharp
UnitDisplayAdapter.ofTrackedUnit defCache teamId unitId trackedUnit
UnitDisplayAdapter.ofTrackedEnemy defCache enemyId trackedEnemy
UnitDisplayAdapter.ofEncyclopediaEntry entry pinnedFootprint
```

This guarantees that a glyph in the Viewer matches the glyph in the Units tab byte-for-byte.

## Style configurator

Press `P` in the live viewer (or open the Hub **Style** tab) to toggle a side panel exposing every `VizConfig` / `UnitGlyphStyle` attribute via typed descriptors:

- `ConfigDescriptors.all` — static registry (`key`, `label`, `category`, `inputKind`, `get`/`set`, default, range). Single source of truth for what's editable.
- `ConfigPanel` — declarative rendering + mouse/scroll input.
- `StylePreset` — JSON persistence under `viz-presets/<name>.json` (gitignored).

Adding a new visual attribute means appending one entry to `ConfigDescriptors.all`; the panel and preset roundtrip pick it up automatically.

## Synthetic data pipeline

`FSBar.SyntheticData.Scenes` produces deterministic `Scene` values without an engine. Used by:

- Every `FSBar.Viz` baseline / snapshot test
- The Hub Units tab glyph preview
- The Style tab's live preview canvas

## Notes

- GPU backend (`GRContext`) segfaults in this environment. The renderer uses a raster `SKSurface` + GL texture upload instead — see `src/FSBar.Viz/Viewer.fs`.
- Graphical engine runs in windowed mode only. `EngineLauncher` forces `Fullscreen=0` in per-session `springsettings.cfg`.
- Throttle viz updates to ~60 fps when driving a high-speed game loop — calling `onFrame` on every engine step at 10× speed will pin a core.

## Regenerating unit labels

When the `BarData` package bumps, regenerate the committed label table:

```bash
dotnet fsi src/FSBar.Viz/scripts/gen-unit-labels.fsx
```

The generator exits non-zero if an existing label would silently change (SC-006 tripwire).
*)
