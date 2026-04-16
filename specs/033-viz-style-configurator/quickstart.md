# Quickstart: 033-viz-style-configurator

**Date**: 2026-04-16

## What This Feature Does

Adds an in-viewer style configurator panel to GameViz. Press a hotkey to open a side panel on the right edge that lists every visual attribute (colors, sizes, stroke widths, overlay settings, timing parameters) in collapsible categories. Edit any value with mouse-driven controls (color swatches, sliders, toggles) and see the result instantly in the game scene. Save/load named presets to JSON files.

## Key Files (New)

| File | Purpose |
|------|---------|
| `src/FSBar.Viz/ConfigDescriptors.fsi` | Attribute descriptor list — the single source of truth for what's configurable |
| `src/FSBar.Viz/ConfigDescriptors.fs` | Descriptor definitions with get/set/range/default for each VizConfig + GlyphStyle field |
| `src/FSBar.Viz/ConfigPanel.fsi` | Panel rendering API — builds scene elements for the side panel |
| `src/FSBar.Viz/ConfigPanel.fs` | Panel layout, hit-testing, input handling, scroll state |
| `src/FSBar.Viz/StylePreset.fsi` | Preset save/load/list API |
| `src/FSBar.Viz/StylePreset.fs` | JSON serialization of preset values |

## Key Files (Modified)

| File | Change |
|------|--------|
| `src/FSBar.Viz/GameViz.fs` | Add panel toggle, route input to panel when open, compose panel scene elements |
| `src/FSBar.Viz/GameViz.fsi` | Expose `toggleConfigPanel`, `isConfigPanelOpen` |
| `src/FSBar.Viz/SceneBuilder.fs` | Accept optional panel elements for composition into final scene |
| `src/FSBar.Viz/VizTypes.fsi` | Add `ConfigPanelState` type (open/closed, scroll offset, expanded sections) |

## Key Files (Tests)

| File | Purpose |
|------|---------|
| `tests/FSBar.Viz.Tests/ConfigDescriptorsTests.fs` | Verify every VizConfig/GlyphStyle field has a descriptor |
| `tests/FSBar.Viz.Tests/ConfigPanelTests.fs` | Panel layout, hit-testing, input routing |
| `tests/FSBar.Viz.Tests/StylePresetTests.fs` | Roundtrip serialization, partial load, unknown key handling |
| `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs` | Updated with baselines for new public modules |

## Architecture

```
GameViz (orchestrator)
  ├── SceneBuilder.buildScene (world-space: map + units + overlays)
  ├── ConfigPanel.buildPanel (screen-space: side panel UI elements)
  │     └── ConfigDescriptors.all (attribute list with get/set)
  └── Scene.compose (merge world + panel into final scene)

Input flow:
  InputEvent → GameViz
    ├── mouse in panel bounds? → ConfigPanel.handleInput → updateConfig
    └── mouse in scene bounds? → existing pan/zoom/hotkey handling

Preset flow:
  ConfigPanel → StylePreset.save/load → JSON file on disk
  StylePreset.load → ConfigDescriptors.apply → GameViz.setConfig
```

## How to Test Manually

```bash
# Build
dotnet build src/FSBar.Viz/FSBar.Viz.fsproj

# Run with synthetic data (from FSI)
dotnet fsi src/FSBar.Viz/scripts/prelude.fsx
# Then in FSI:
# GameViz.start None
# // press P to toggle configurator panel
```

## Constitution Compliance Notes

- Three new `.fsi` files required (ConfigDescriptors, ConfigPanel, StylePreset)
- Surface-area baselines added for each new public module
- No new NuGet dependencies (uses BCL `System.Text.Json` already in dependency graph)
- Test evidence: descriptor completeness tests, panel interaction tests, preset roundtrip tests
