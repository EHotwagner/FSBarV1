# Research: 033-viz-style-configurator

**Date**: 2026-04-16

## R1: Side Panel Rendering in SkiaViewer's Declarative Scene API

**Decision**: Render the configurator panel as additional `Scene` elements in screen-space (not world-space). SceneBuilder already separates world-space (map, units) from screen-space (economy HUD, performance overlay). The configurator panel will be a new screen-space layer built by a dedicated `ConfigPanel` module and composed into the final scene.

**Rationale**: SkiaViewer's Scene API supports text, rectangles, lines, and circles — sufficient for labels, sliders, color swatches, toggles, and section headers. No external UI framework is needed. The economy HUD already demonstrates this pattern (320×110px panel with text, bars, and background). Rendering in-scene means changes to VizConfig are reflected in the same frame — zero-latency preview by construction.

**Alternatives considered**:
- **Separate native window**: Would break the "instant, faithful" requirement (cross-window sync adds latency and complexity). Rejected.
- **ImGui-style overlay library**: No F#/.NET ImGui binding in the dependency set; adding one violates the constitution's dependency minimization rule. Rejected.
- **SkiaSharp canvas overlay outside Scene**: Would bypass the declarative model and create a parallel rendering path. Rejected.

## R2: Input Handling for Panel Controls

**Decision**: Extend the existing `InputEvent` handling in GameViz to route mouse events to the panel when the cursor is within the panel bounds. Keyboard events already have a dispatch model (single-key hotkeys); add a new toggle key (e.g., `P` or `F2`) for panel open/close. Panel controls respond to mouse click (toggles, section expand/collapse), mouse drag (sliders), and mouse click + text entry (color hex input via keyboard capture).

**Rationale**: The existing input pipeline dispatches `MouseDown/Move/Up`, `KeyDown`, and `MouseScroll` events. The configurator needs a hit-test layer: if the mouse X is within the panel width, route to panel logic; otherwise route to existing pan/zoom. This is a simple spatial partition, not a full event-bubbling system.

**Alternatives considered**:
- **Full retained-mode UI framework**: Overkill for a property editor with ~30 controls. Rejected.
- **Keyboard-only navigation**: Poor UX for color picking and continuous slider adjustment. Rejected.

## R3: Attribute Reflection from VizConfig and UnitGlyphStyle

**Decision**: Define a static `AttributeDescriptor` list that enumerates every configurable field from `VizConfig` and `UnitGlyphStyle`, along with category, label, value range, and getter/setter functions. This list is the single source of truth for what the panel displays.

**Rationale**: F# records don't support runtime reflection ergonomically. A static descriptor list is explicit, type-safe, and allows custom labels/ranges per field. When a new field is added to VizConfig or UnitGlyphStyle, adding a descriptor is a one-line change. The descriptor list also serves as the serialization schema for presets.

**Alternatives considered**:
- **Reflection-based auto-discovery**: Fragile, loses type safety, can't attach custom metadata (ranges, labels). Rejected.
- **Code generation from .fsi**: Over-engineered for ~30 fields; descriptor list is simpler and explicit. Rejected.

## R4: Preset Serialization Format

**Decision**: JSON files stored in a `viz-presets/` directory under the project root. Each file is `<name>.json` containing a flat key-value map of attribute name → serialized value. Colors serialize as `#AARRGGBB` hex strings. Floats and ints serialize as JSON numbers. Booleans as JSON booleans. Sets serialize as string arrays.

**Rationale**: JSON is human-readable and editable, already used elsewhere in the project (`System.Text.Json` in BCL). No new dependencies. The flat structure means unknown keys are silently skipped on load (forward compatibility).

**Alternatives considered**:
- **Binary serialization**: Not human-readable, harder to debug. Rejected.
- **TOML/YAML**: Would require new dependencies. Rejected.

## R5: Panel Layout and Sizing

**Decision**: Fixed panel width of 280 pixels. Category sections are collapsible with a header row (category name + expand/collapse chevron). Each attribute row is ~24px tall: label on the left, control on the right. Color swatches are 16×16px clickable squares. Sliders are horizontal bars with a draggable thumb. Scroll via mouse wheel when the cursor is over the panel.

**Rationale**: 280px is wide enough for labels + controls without dominating the viewer on a 1920px-wide display (leaves 1640px for the scene). The 24px row height matches the existing 16px label font with padding. These values match the economy HUD's visual density.

**Alternatives considered**:
- **Resizable panel**: Adds complexity for marginal benefit on a developer tool. Rejected for v1; can be added later.
- **Wider panel (400px)**: Consumes too much scene space. Rejected.

## R6: Constitution Compliance — New Module .fsi Requirements

**Decision**: New modules introduced by this feature MUST have `.fsi` signature files per Constitution §II. The new modules are:
- `ConfigPanel.fsi` — panel rendering (scene elements) and input handling
- `ConfigDescriptors.fsi` — attribute descriptor list and getter/setter functions
- `StylePreset.fsi` — preset load/save/list

Surface-area baseline tests MUST be added for each new public module.

**Rationale**: Constitution §II mandates `.fsi` for every public module. §III mandates test evidence. Surface-area baselines catch accidental API drift.
