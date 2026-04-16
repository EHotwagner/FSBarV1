# Implementation Plan: Viz Style Configurator

**Branch**: `033-viz-style-configurator` | **Date**: 2026-04-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/033-viz-style-configurator/spec.md`

## Summary

Add an in-viewer style configurator panel to GameViz as a fixed-width side panel (280px, right edge) with collapsible category sections. Every visual attribute from `VizConfig` and `UnitGlyphStyle` is exposed via typed descriptors with appropriate input controls (color swatches, sliders, toggles). Changes apply within one frame. Named presets persist as JSON files. Three new modules: `ConfigDescriptors`, `ConfigPanel`, `StylePreset` — each with `.fsi` signature files and surface-area baselines.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0  
**Primary Dependencies**: FSBar.Viz (in-repo), SkiaViewer 1.1.3-dev, SkiaSharp 2.88.6, System.Text.Json (BCL)  
**Storage**: JSON files on disk (`viz-presets/` directory) for presets  
**Testing**: xUnit 2.9.x  
**Target Platform**: Linux (desktop, windowed SkiaViewer via Silk.NET/GLFW)  
**Project Type**: Desktop visualization tool (library + viewer)  
**Performance Goals**: Panel rendering ≤10% frame rate impact; attribute changes visible within one frame  
**Constraints**: No new NuGet dependencies; all UI rendered via SkiaViewer's declarative Scene API  
**Scale/Scope**: ~30-40 attribute descriptors, ~5-20 presets, single-user developer tool

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| §I Spec-First | **PASS** | Spec, clarifications, plan all completed before coding |
| §II .fsi Contracts | **PASS** | Three new modules (ConfigDescriptors, ConfigPanel, StylePreset) each have draft .fsi contracts |
| §II Surface-Area Baselines | **PASS** | Baselines will be added for each new public module |
| §III Test Evidence | **PASS** | Descriptor completeness tests, panel interaction tests, preset roundtrip tests planned |
| §IV Observability | **PASS** | Preset load errors emit structured warnings; invalid input logged |
| §V Scripting Accessibility | **PASS** | ConfigPanel is a library module usable from FSI via existing prelude; GameViz.setConfig already scriptable |
| §Eng F#-only | **PASS** | All new code is F# |
| §Eng No new dependencies | **PASS** | Uses only BCL System.Text.Json (already in dependency graph) |
| §Eng .fsi for public modules | **PASS** | Three .fsi.draft contracts defined |
| §Eng Packable library | **PASS** | FSBar.Viz already packable; no structural changes |

**Post-Phase 1 re-check**: All gates remain PASS. No violations detected.

## Project Structure

### Documentation (this feature)

```text
specs/033-viz-style-configurator/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 quickstart guide
├── contracts/           # Phase 1 .fsi draft contracts
│   ├── ConfigDescriptors.fsi.draft
│   ├── ConfigPanel.fsi.draft
│   └── StylePreset.fsi.draft
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/FSBar.Viz/
├── VizTypes.fsi              # MODIFIED — add ConfigPanelState type
├── VizTypes.fs               # MODIFIED — add ConfigPanelState type
├── ConfigDescriptors.fsi     # NEW — attribute descriptor registry
├── ConfigDescriptors.fs      # NEW — descriptor definitions
├── ConfigPanel.fsi           # NEW — panel rendering + input handling
├── ConfigPanel.fs            # NEW — panel layout, hit-test, scroll, controls
├── StylePreset.fsi           # NEW — preset save/load/list
├── StylePreset.fs            # NEW — JSON serialization
├── GameViz.fsi               # MODIFIED — add toggleConfigPanel, isConfigPanelOpen
├── GameViz.fs                # MODIFIED — integrate panel into render loop + input dispatch
└── SceneBuilder.fsi          # UNCHANGED — panel composed in GameViz post-render

tests/FSBar.Viz.Tests/
├── ConfigDescriptorsTests.fs # NEW — descriptor completeness, get/set roundtrip
├── ConfigPanelTests.fs       # NEW — layout, hit-testing, input routing
├── StylePresetTests.fs       # NEW — JSON roundtrip, partial load, error handling
└── SurfaceBaselineTests.fs   # MODIFIED — baselines for 3 new modules

viz-presets/                   # NEW — preset storage directory (gitignored initially)
```

**Structure Decision**: All new code lives within the existing `FSBar.Viz` project. No new projects needed — the configurator is a visual feature of the viewer, not a separate concern. Three new module pairs (.fsi + .fs) plus modifications to GameViz and VizTypes.

## Phased Implementation

### Phase 1: ConfigDescriptors + VizTypes Extension

**Goal**: Establish the attribute descriptor registry and panel state type.

1. Add `ConfigPanelState` record to `VizTypes.fsi`/`.fs`
2. Create `ConfigDescriptors.fsi`/`.fs` with all ~30-40 attribute descriptors
3. Each descriptor has: key, label, category, input kind, get/set functions, default, range
4. Create `ConfigDescriptorsTests.fs`:
   - Test every VizConfig field has a descriptor
   - Test every UnitGlyphStyle field has a descriptor
   - Test get/set roundtrip for each descriptor
   - Test `isDirty` detection
5. Update surface-area baselines

### Phase 2: StylePreset (Persistence)

**Goal**: Enable saving/loading configurations to disk independently of the panel UI.

1. Create `StylePreset.fsi`/`.fs`
2. Implement JSON serialization for `PresetValue` DU using `System.Text.Json`
3. Implement `save`, `load`, `listNames`, `delete`, `fromConfig`, `applyToConfig`
4. Create `StylePresetTests.fs`:
   - Roundtrip save → load preserves all values
   - Partial preset (missing keys) applies cleanly
   - Unknown keys in JSON are silently skipped
   - Corrupted JSON returns descriptive error
   - Preset name validation (filesystem-safe)
5. Update surface-area baselines

### Phase 3: ConfigPanel (UI Rendering + Input)

**Goal**: Render the side panel with all controls and handle user interaction.

1. Create `ConfigPanel.fsi`/`.fs`
2. Implement panel layout engine:
   - Background rectangle (280px wide, full window height)
   - Collapsible section headers (category name + chevron)
   - Attribute rows (label + control) at 24px height
   - Vertical scroll with mouse wheel
3. Implement input controls:
   - Color swatch (click to enter hex edit mode; type hex digits, Enter to confirm)
   - Slider (horizontal bar; click/drag to set value; clamped to range)
   - Toggle (click to flip boolean)
   - Section expand/collapse (click header)
4. Implement hit-testing (panel bounds check for input routing)
5. Create `ConfigPanelTests.fs`:
   - Panel elements generated when open, empty when closed
   - Hit-test returns true for points inside panel, false outside
   - Section toggle updates expanded set
   - Slider drag produces clamped value
6. Update surface-area baselines

### Phase 4: GameViz Integration

**Goal**: Wire the panel into the existing viewer loop.

1. Modify `GameViz.fsi`/`.fs`:
   - Add `toggleConfigPanel: unit -> unit`
   - Add `isConfigPanelOpen: unit -> bool`
   - Add panel state to the internal viewer state
   - Route `P` key to panel toggle
   - Route mouse events: if panel open and mouse in panel bounds → `ConfigPanel.handleInput`; else → existing handlers
   - When panel returns an updated VizConfig → `setConfig`
2. Post-compose panel in `GameViz.fs` (no SceneBuilder API change):
   - After `SceneBuilder.buildScene`, append `ConfigPanel.buildPanel` elements to the scene
   - Adjust viewport (scene width = window width - panel width)
3. Add preset UI to panel: save button, load dropdown, reset button
4. Integration tests:
   - Toggle panel open/close via key
   - Config change through panel reflects in next scene build
   - Preset save/load cycle via panel controls

### Phase 5: Polish + Verification

**Goal**: Ensure all acceptance criteria pass and the feature is complete.

1. Verify SC-001: attribute change → scene update in one frame
2. Verify SC-002: audit descriptors against VizConfig + UnitGlyphStyle fields
3. Verify SC-004: profile frame rate with panel open vs closed
4. Dirty indicator (FR-012): compare current config to loaded preset or defaults
5. Edge cases: out-of-range clamping, no-map-loaded state, corrupted preset
6. Update CLAUDE.md with configurator documentation
7. Run full test suite, update baselines

## Complexity Tracking

No constitution violations. No complexity justifications needed.
