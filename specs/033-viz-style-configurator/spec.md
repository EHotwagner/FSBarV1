# Feature Specification: Viz Style Configurator

**Feature Branch**: `033-viz-style-configurator`  
**Created**: 2026-04-16  
**Status**: Draft  
**Input**: User description: "create a configurator for a comprehensive config how bar gamestate is displayed in viewer. colors, sizes, strokes, width, attacks, health/damage, team..... have a list of attributes to configure and show the result. configurator should be in skiaviewer so instant and faithful result."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Live Style Preview (Priority: P1)

A user opens the viz style configurator panel alongside the game state viewer. They adjust a color value (e.g., team 1 primary color) and immediately see the change reflected in the rendered game state scene — no restart, no reload, no delay beyond the next frame.

**Why this priority**: The core value proposition is instant, faithful preview. Without this, the configurator is just a text editor for config values. The tight feedback loop is what makes it useful.

**Independent Test**: Can be tested by opening the configurator with any game state (live or synthetic), changing a single attribute, and visually confirming the scene updates on the next frame.

**Acceptance Scenarios**:

1. **Given** the configurator panel is open with a game state loaded, **When** the user changes the team 1 primary color from blue to red, **Then** all team 1 units in the scene render in red within one frame.
2. **Given** the configurator panel is open, **When** the user adjusts unit marker size from 8 to 16 pixels, **Then** all unit markers visibly grow in the scene immediately.
3. **Given** the configurator is open with synthetic data, **When** the user modifies stroke width for T2 units, **Then** only T2 unit outlines change thickness.

---

### User Story 2 - Browse and Edit All Visual Attributes (Priority: P1)

A user sees a categorized list of every visual attribute that controls how the BAR game state is rendered: colors (faction, team, background, labels), sizes (markers, fonts, radii), strokes (outline widths per tier), overlays (opacity, toggle states), health/damage indicators (HP arc width, damage flash duration), and attack/command visuals (weapon range color, command queue style). They can modify any attribute from this list.

**Why this priority**: Without a comprehensive attribute list, the configurator is incomplete — users would still need to know the API to change unlisted properties.

**Independent Test**: Open the configurator and verify every attribute from VizConfig and UnitGlyphStyle is represented and editable. Changing each attribute produces a visible effect in the scene.

**Acceptance Scenarios**:

1. **Given** the configurator is open, **When** the user inspects the attribute list, **Then** they see categories for Colors, Sizes, Strokes, Overlays, Health/Damage, and Effects.
2. **Given** the user selects the "Colors" category, **When** they view the entries, **Then** they see faction colors, team colors, background color, label color, and overlay colors — each editable.
3. **Given** the user selects the "Sizes" category, **When** they view the entries, **Then** they see unit marker size, label font size, minimum glyph radius, and HP arc width — each editable with numeric input.

---

### User Story 3 - Save and Load Style Presets (Priority: P2)

A user creates a visual style they like and saves it as a named preset. Later, they load the preset to restore all settings at once. Presets persist across sessions.

**Why this priority**: Reusable styles reduce repetitive work, but the configurator is fully useful without persistence (users can tweak live each session).

**Independent Test**: Save a preset, close and reopen the configurator, load the preset, and verify all attributes match the saved values.

**Acceptance Scenarios**:

1. **Given** the user has modified several attributes, **When** they save a preset named "Tournament Dark", **Then** the preset appears in the preset list.
2. **Given** a saved preset "Tournament Dark" exists, **When** the user loads it, **Then** all visual attributes update to the saved values and the scene reflects them immediately.
3. **Given** the user loads a preset and then modifies one attribute, **When** they view the preset status, **Then** the configurator indicates the current state differs from the loaded preset.

---

### User Story 4 - Reset to Defaults (Priority: P3)

A user who has experimented with many changes wants to return to the default visual style quickly.

**Why this priority**: Safety net for experimentation — low effort to implement but important for user confidence.

**Independent Test**: Modify several attributes, press reset, verify all values match the documented defaults.

**Acceptance Scenarios**:

1. **Given** the user has modified multiple attributes, **When** they select "Reset to Defaults", **Then** all attributes revert to their default values and the scene updates immediately.

---

### Edge Cases

- What happens when a user enters an out-of-range value (e.g., negative marker size, opacity > 1.0)? Values are clamped to valid ranges with visual feedback.
- What happens when synthetic data is loaded but no map data is available? The configurator still functions for unit-level attributes; map layer attributes are shown but noted as "no map loaded."
- What happens when the user edits a color that affects multiple elements (e.g., background color used in contrast calculations)? Dependent elements update accordingly in the same frame.
- What happens if a preset file is corrupted or references attributes that no longer exist? The system loads what it can, ignores unknown attributes, and warns the user about skipped entries.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST render a configurator as a fixed-width side panel on the right edge of the SkiaViewer window, with the game scene shrinking horizontally to accommodate it. All visual attributes are displayed organized by category.
- **FR-002**: System MUST apply attribute changes to the rendered game state scene within one frame of the change being made (instant preview).
- **FR-003**: System MUST expose the following attribute categories with all editable properties derived from `VizConfig` and `UnitGlyphStyle`:
  - **Colors**: faction colors (6 FactionPalette fields: Armada, Cortex, Legion, Raptors, Scavengers, Neutral), team palette fallback color, background color, label color
  - **Sizes**: unit marker size, minimum glyph radius, label font size, facing pip radius, grid line spacing, label legibility zoom threshold
  - **Strokes**: outline width per unit tier (T1StrokeWidth, T2StrokeWidth, T3StrokeWidth), HP arc width
  - **Overlays**: overlay opacity, active overlay toggles (Units, Events, Grid, MetalSpots, EconomyHud, WeaponRanges, SightRanges, CommandQueue, FullNames), base layer selection, show grid lines toggle, use glyph renderer toggle
  - **Health/Damage**: low HP fraction threshold, event flash duration (ms), "just built" ring duration (ms)
  - **Effects**: event flash duration (ms), just built ring duration (ms)
- **FR-004**: System MUST organize attributes into collapsible sections by category, each with an expand/collapse toggle. Multiple sections may be open simultaneously. The panel is vertically scrollable.
- **FR-005**: System MUST provide appropriate input controls for each attribute type — color pickers for colors, numeric sliders for sizes and opacities, toggles for booleans.
- **FR-006**: System MUST clamp all numeric inputs to valid ranges (e.g., marker size >= 1, opacity 0.0–1.0, stroke widths >= 0.5) and reject invalid color values.
- **FR-007**: System MUST allow saving the current attribute state as a named preset to a file.
- **FR-008**: System MUST allow loading a previously saved preset, restoring all attributes and updating the scene.
- **FR-009**: System MUST provide a "Reset to Defaults" action that restores all attributes to their default values as defined by the existing defaults.
- **FR-010**: System MUST work with both live game state data and synthetic data sources.
- **FR-011**: System MUST allow the configurator panel to be toggled open/closed via a keyboard shortcut without disrupting the game state view.
- **FR-012**: System MUST indicate when the current configuration differs from the loaded preset or from defaults.

### Key Entities

- **StylePreset**: A named collection of all visual attribute values. Identified by name, contains a timestamp and the full attribute map.
- **AttributeCategory**: A logical grouping of related visual attributes (Colors, Sizes, Strokes, etc.) used for panel organization.
- **AttributeEntry**: A single configurable visual property with its current value, default value, valid range, and display metadata (label, category, input type).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can modify any visual attribute and see the result in the viewer within one rendered frame (no perceptible delay).
- **SC-002**: 100% of visual attributes that affect game state rendering are exposed in the configurator — no attribute requires code changes to adjust.
- **SC-003**: Users can save, load, and switch between style presets in under 3 seconds each.
- **SC-004**: The configurator panel does not reduce the viewer's frame rate by more than 10% when open.
- **SC-005**: A new user can find and modify any specific visual attribute within 30 seconds using the categorized layout.

## Clarifications

### Session 2026-04-16

- Q: What panel layout should the configurator use? → A: Side panel — fixed-width panel on the right edge; scene shrinks horizontally to make room.
- Q: How should category navigation work in the panel? → A: Collapsible sections — all categories listed vertically with expand/collapse toggles; multiple can be open simultaneously.

## Assumptions

- The configurator is rendered as a fixed-width side panel on the right edge of the SkiaViewer window; the game scene shrinks horizontally to make room (not an overlay, not a separate window).
- The existing VizConfig and UnitGlyphStyle records define the full set of attributes to expose; if new attributes are added to those types in the future, they should be added to the configurator as well.
- Color input uses HSV or hex representation suitable for the SkiaViewer rendering context (no OS-native color picker dialog needed).
- Presets are stored as files on disk in a human-readable format within the project or user data directory.
- The configurator uses keyboard and mouse interaction consistent with the existing SkiaViewer input model (no external UI framework).
- Synthetic data (from FSBar.SyntheticData) provides sufficient visual variety (multiple teams, unit tiers, overlays) to meaningfully preview all attribute changes.
