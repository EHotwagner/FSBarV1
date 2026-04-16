# Tasks: Viz Style Configurator

**Input**: Design documents from `/specs/033-viz-style-configurator/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per plan.md — constitution §III mandates test evidence for new modules.

**Organization**: Tasks grouped by user story. US1 and US2 are both P1 but US2 depends on the descriptor registry established in US1's phase.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization — new module files, types, and directory structure

- [X] T001 Add `ConfigPanelState` record type to `src/FSBar.Viz/VizTypes.fsi` (IsOpen, ScrollOffset, ExpandedSections, ActiveControl, DirtyIndicator)
- [X] T002 Add `ConfigPanelState` record implementation to `src/FSBar.Viz/VizTypes.fs`
- [X] T003 Add `InputKind`, `AttributeCategory`, `AttributeDescriptor` types to `src/FSBar.Viz/ConfigDescriptors.fsi` (from contracts/ConfigDescriptors.fsi.draft)
- [X] T004 Add `PresetValue`, `StylePreset` types and `StylePreset` module signature to `src/FSBar.Viz/StylePreset.fsi` (from contracts/StylePreset.fsi.draft)
- [X] T005 Add `ConfigPanel` module signature to `src/FSBar.Viz/ConfigPanel.fsi` (from contracts/ConfigPanel.fsi.draft)
- [X] T006 Create `viz-presets/` directory at project root with `.gitkeep`; add `viz-presets/*.json` to `.gitignore` (presets are user-local, not committed)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core descriptor registry that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Implement `ConfigDescriptors.fs` — define all ~30-40 attribute descriptors covering VizConfig and UnitGlyphStyle fields in `src/FSBar.Viz/ConfigDescriptors.fs`. Each descriptor must have: key, label, category, input kind, get/set lambdas, default value, and range. Categories: Colors, Sizes, Strokes, Overlays, HealthDamage, Effects
- [X] T008 Implement `ConfigDescriptors.tryFind`, `applyValues`, `extractValues`, and `isDirty` functions in `src/FSBar.Viz/ConfigDescriptors.fs`
- [X] T009 Create descriptor completeness tests in `tests/FSBar.Viz.Tests/ConfigDescriptorsTests.fs` — verify every VizConfig field has a descriptor, every UnitGlyphStyle field has a descriptor, get/set roundtrip for each descriptor, isDirty detection
- [X] T010 Add surface-area baseline for `ConfigDescriptors` module in `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs`
- [X] T011 Register new .fs files in `src/FSBar.Viz/FSBar.Viz.fsproj` (ConfigDescriptors.fsi, ConfigDescriptors.fs, StylePreset.fsi, StylePreset.fs, ConfigPanel.fsi, ConfigPanel.fs) in correct compilation order
- [X] T012 Build and verify all tests pass: `dotnet build src/FSBar.Viz/FSBar.Viz.fsproj && dotnet test tests/FSBar.Viz.Tests/`

**Checkpoint**: Foundation ready — descriptor registry complete and tested

---

## Phase 3: User Story 1 - Live Style Preview (Priority: P1) MVP

**Goal**: Users adjust any visual attribute in the configurator and see the change reflected in the rendered scene within one frame.

**Independent Test**: Open the configurator with synthetic data, change a color or size attribute, and visually confirm the scene updates on the next frame.

### Implementation for User Story 1

- [X] T013 [US1] Implement `ConfigPanel.buildPanel` in `src/FSBar.Viz/ConfigPanel.fs` — render panel background (280px wide), collapsible section headers with chevrons, attribute rows (label + control) at 24px height, vertical scroll
- [X] T014 [US1] Implement `ConfigPanel.handleInput` in `src/FSBar.Viz/ConfigPanel.fs` — mouse click for toggles and section expand/collapse, mouse drag for sliders with range clamping, color swatch click to enter hex edit mode, scroll via mouse wheel
- [X] T015 [US1] Implement `ConfigPanel.hitTest` and `ConfigPanel.toggle` in `src/FSBar.Viz/ConfigPanel.fs`
- [X] T016 [US1] Implement `ConfigPanel.initialState` in `src/FSBar.Viz/ConfigPanel.fs`
- [X] T017 [US1] Modify `src/FSBar.Viz/GameViz.fsi` — expose `toggleConfigPanel` and `isConfigPanelOpen`
- [X] T018 [US1] Modify `src/FSBar.Viz/GameViz.fs` — add `ConfigPanelState` to internal viewer state, route `P` key to `ConfigPanel.toggle`, route mouse events to `ConfigPanel.handleInput` when panel open and cursor in panel bounds (else existing handlers), call `setConfig` when panel returns updated VizConfig
- [X] T019 [US1] Modify `src/FSBar.Viz/GameViz.fs` — after `SceneBuilder.buildScene` returns the game scene, post-compose `ConfigPanel.buildPanel` elements and adjust the game scene viewport width (window width - 280px) when panel is open. No change to SceneBuilder's public API
- [X] T020 [US1] Create panel tests in `tests/FSBar.Viz.Tests/ConfigPanelTests.fs` — panel elements generated when open / empty when closed, hit-test correct for points inside/outside panel, section toggle updates expanded set, slider drag produces clamped value
- [X] T021 [US1] Add surface-area baseline for `ConfigPanel` module in `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs`
- [X] T022 [US1] Build and run tests: `dotnet test tests/FSBar.Viz.Tests/`

**Checkpoint**: User Story 1 functional — panel opens, attributes editable, scene updates in one frame

---

## Phase 4: User Story 2 - Browse and Edit All Visual Attributes (Priority: P1)

**Goal**: Every visual attribute from VizConfig and UnitGlyphStyle is represented in categorized, collapsible sections with appropriate controls.

**Independent Test**: Open the configurator and verify every attribute from VizConfig and UnitGlyphStyle is listed under the correct category with an appropriate input control.

### Implementation for User Story 2

- [X] T023 [US2] Audit `ConfigDescriptors.all` against current `VizConfig` and `UnitGlyphStyle` fields in `src/FSBar.Viz/ConfigDescriptors.fs` — ensure 100% coverage including per-faction colors, per-team colors, overlay toggle states
- [X] T024 [US2] Verify category labels display correctly in panel — Colors, Sizes, Strokes, Overlays, Health/Damage, Effects in `src/FSBar.Viz/ConfigPanel.fs`
- [X] T025 [US2] Add enum cycling control for `EnumChoice` input kind in `src/FSBar.Viz/ConfigPanel.fs` (click cycles through labels)
- [X] T026 [US2] Update `tests/FSBar.Viz.Tests/ConfigDescriptorsTests.fs` — add test asserting descriptor count matches VizConfig + UnitGlyphStyle field count (SC-002 verification)

**Checkpoint**: US1 + US2 functional — all attributes browseable and editable with instant preview

---

## Phase 5: User Story 3 - Save and Load Style Presets (Priority: P2)

**Goal**: Users save current attribute state as a named preset to JSON, load presets to restore all settings, presets persist across sessions.

**Independent Test**: Save a preset, verify the JSON file appears in `viz-presets/`, load it back, confirm all attributes match.

### Implementation for User Story 3

- [X] T027 [US3] Implement `StylePreset.fromConfig` and `StylePreset.applyToConfig` in `src/FSBar.Viz/StylePreset.fs` — convert between VizConfig and PresetValue map using ConfigDescriptors
- [X] T028 [US3] Implement JSON serialization/deserialization in `src/FSBar.Viz/StylePreset.fs` — colors as `#AARRGGBB` hex strings, floats/ints as JSON numbers, booleans as JSON booleans, sets as string arrays. Use `System.Text.Json`
- [X] T029 [US3] Implement `StylePreset.save`, `load`, `listNames`, `delete` in `src/FSBar.Viz/StylePreset.fs` — file I/O to `viz-presets/<name>.json`, preset name validation (non-empty, filesystem-safe)
- [X] T030 [US3] Add preset UI controls to `ConfigPanel.buildPanel` in `src/FSBar.Viz/ConfigPanel.fs` — save button (text input for name), load dropdown (list from `StylePreset.listNames`), current preset name display
- [X] T031 [US3] Wire preset save/load into `GameViz.fs` — `ConfigPanel` emits save/load actions, GameViz calls `StylePreset.save`/`load` and applies via `setConfig`
- [X] T032 [US3] Create preset tests in `tests/FSBar.Viz.Tests/StylePresetTests.fs` — roundtrip save/load preserves all values, partial preset (missing keys) applies cleanly, unknown keys silently skipped, corrupted JSON returns descriptive error, preset name validation
- [X] T033 [US3] Add surface-area baseline for `StylePreset` module in `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs`
- [X] T034 [US3] Build and run tests: `dotnet test tests/FSBar.Viz.Tests/`

**Checkpoint**: US1 + US2 + US3 functional — presets save/load from disk, scene reflects loaded preset instantly

---

## Phase 6: User Story 4 - Reset to Defaults (Priority: P3)

**Goal**: Users can quickly reset all attributes to default values.

**Independent Test**: Modify several attributes, press reset, verify all values match VizDefaults.

### Implementation for User Story 4

- [X] T035 [US4] Add "Reset to Defaults" button to `ConfigPanel.buildPanel` in `src/FSBar.Viz/ConfigPanel.fs`
- [X] T036 [US4] Implement reset action in `ConfigPanel.handleInput` in `src/FSBar.Viz/ConfigPanel.fs` — construct default VizConfig from descriptor defaults, return as updated config
- [X] T037 [US4] Wire reset action into `GameViz.fs` — apply default config via `setConfig`
- [X] T038 [US4] Add dirty indicator (FR-012) to panel header in `src/FSBar.Viz/ConfigPanel.fs` — compare current config to loaded preset or defaults using `ConfigDescriptors.isDirty`

**Checkpoint**: All user stories functional

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verification, edge cases, and documentation

- [X] T039 [P] Verify SC-001: attribute change reflects in scene within one frame. Manual verification 2026-04-16 via FSI: `ConfigPanel.handleInput` returns `UpdatedConfig` synchronously; next `buildPanel` call on the same turn reflects the change. Live-render confirmed: `GameViz.setConfig` → next screenshot shows Armada swatch flipped from `#FF40B0` to `#FFFFFF`. During live-verification, extended `emitScene` so the panel also renders when no game snapshot is loaded (FR-010 synthetic-data support).
- [X] T040 [P] Verify SC-004: profile frame rate with panel open vs closed, confirm < 10% impact. Manual verification 2026-04-16 via FSI (5000 iters, warmed): panel-closed build = 0.0001 ms, panel-open build = 0.0223 ms → overhead 0.0222 ms ≈ **0.133% of a 60 FPS frame** (cap 10% / 1.67 ms). Live session ran on Vulkan backend (NVIDIA RTX 4070).
- [X] T041 Edge case: out-of-range numeric clamping verified for all sliders in `src/FSBar.Viz/ConfigPanel.fs` (clamp in `ConfigDescriptors.floatDesc`/`intDesc` Set lambdas; tested in ConfigDescriptorsTests)
- [X] T042 Edge case: no-map-loaded state — configurator still functions for unit-level attributes (panel is independent of MapGrid)
- [X] T043 Update CLAUDE.md with configurator panel documentation (hotkey, module locations, preset directory)
- [X] T044 Run full test suite and update baselines: `dotnet test tests/FSBar.Viz.Tests/` (224 passed, 7 skipped)
- [X] T045 Run quickstart.md validation — build and launch configurator with synthetic data (example script 06 runs end-to-end)
- [X] T046 [P] Update `src/FSBar.Viz/scripts/prelude.fsx` — add ergonomic helpers for `ConfigDescriptors`, `StylePreset`, and `ConfigPanel`; add `openConfigurator` helper that calls `GameViz.toggleConfigPanel()` (constitution §V)
- [X] T047 [P] Create `src/FSBar.Viz/scripts/examples/06-style-configurator.fsx` — load prelude, launch synthetic scene, toggle configurator panel, save/load a preset programmatically (constitution §V)
- [X] T048 Update surface-area baselines for modified modules (GameViz, VizTypes) in `tests/FSBar.Viz.Tests/SurfaceBaselineTests.fs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup (Phase 1) — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational (Phase 2) — core panel rendering + input
- **US2 (Phase 4)**: Depends on US1 (Phase 3) — attribute completeness audit needs working panel
- **US3 (Phase 5)**: Depends on Foundational (Phase 2) — preset persistence is independent of panel UI, but preset UI in panel depends on US1
- **US4 (Phase 6)**: Depends on US1 (Phase 3) — reset button lives in panel
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: Depends on Phase 2 only — core panel + live preview
- **US2 (P1)**: Depends on US1 — attribute audit needs working panel to verify
- **US3 (P2)**: Preset module (T027-T029) can start after Phase 2; preset UI (T030-T031) needs US1
- **US4 (P3)**: Depends on US1 — reset button in panel

### Parallel Opportunities

- T001-T006 (Setup): T003, T004, T005 can run in parallel (different .fsi files)
- T007-T008 (Foundation): Sequential (T008 depends on descriptors from T007)
- T027-T029 (US3 persistence): Can start in parallel with US1 panel work (different files)
- T039, T040 (Polish): Independent verification tasks, can run in parallel

---

## Parallel Example: User Story 3

```bash
# StylePreset implementation can start alongside US1 panel work:
Task: "Implement StylePreset.fromConfig and applyToConfig in src/FSBar.Viz/StylePreset.fs"  # T027
Task: "Implement JSON serialization in src/FSBar.Viz/StylePreset.fs"  # T028
# These run in parallel with:
Task: "Implement ConfigPanel.buildPanel in src/FSBar.Viz/ConfigPanel.fs"  # T013
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (types + .fsi files)
2. Complete Phase 2: Foundational (descriptor registry)
3. Complete Phase 3: User Story 1 (panel rendering + live preview)
4. **STOP and VALIDATE**: Open configurator with synthetic data, change attributes, verify instant preview
5. Demo-ready: core value proposition proven

### Incremental Delivery

1. Setup + Foundational -> Descriptor registry complete
2. Add US1 -> Live style preview functional (MVP!)
3. Add US2 -> All attributes browseable
4. Add US3 -> Presets save/load to disk
5. Add US4 -> Reset to defaults
6. Polish -> Verification, edge cases, docs
