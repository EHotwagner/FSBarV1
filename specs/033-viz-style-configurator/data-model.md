# Data Model: 033-viz-style-configurator

**Date**: 2026-04-16

## Entities

### AttributeDescriptor

Describes a single configurable visual property. Static, defined at compile time.

| Field | Type | Description |
|-------|------|-------------|
| Key | string | Unique identifier (e.g., `"colors.background"`, `"sizes.markerSize"`) |
| Label | string | Display name shown in panel (e.g., `"Background"`, `"Marker Size"`) |
| Category | AttributeCategory | Which collapsible section this belongs to |
| InputKind | InputKind | Control type: ColorPicker, Slider, Toggle, EnumChoice |
| Get | VizConfig → obj | Extracts the current value from VizConfig |
| Set | obj → VizConfig → VizConfig | Applies a new value to VizConfig, returning updated config |
| Default | obj | The default value (from VizDefaults) |
| Range | (float32 * float32) option | Min/max for numeric inputs; None for non-numeric |

### AttributeCategory (DU)

```
Colors | Sizes | Strokes | Overlays | HealthDamage | Effects
```

Each variant maps to a collapsible section in the panel with a display name.

### InputKind (DU)

```
ColorPicker          — SKColor value, rendered as a colored swatch + hex input
Slider of min * max  — float32 value, rendered as a horizontal drag bar
IntSlider of min * max — int value, rendered as a stepped drag bar
Toggle               — bool value, rendered as an on/off switch
EnumChoice of labels — string list, rendered as a cycling selector
```

### StylePreset

Persisted to disk as a JSON file.

| Field | Type | Description |
|-------|------|-------------|
| Name | string | User-assigned preset name (also the filename stem) |
| CreatedAt | DateTimeOffset | When the preset was saved |
| Values | Map<string, PresetValue> | Key → serialized value, keyed by AttributeDescriptor.Key |

### PresetValue (DU)

```
ColorVal of uint32       — ARGB packed as 0xAARRGGBB
FloatVal of float32      — numeric float
IntVal of int            — numeric int
BoolVal of bool          — toggle state
StringVal of string      — enum choice label
StringSetVal of string set — overlay set
```

## Relationships

```
VizConfig ←—[get/set]—→ AttributeDescriptor ←—[serialize]—→ StylePreset
                              |
                         AttributeCategory (grouping)
                              |
                         InputKind (control type)
```

- Each `AttributeDescriptor` reads/writes one field of `VizConfig` (or `VizConfig.GlyphStyle`).
- `StylePreset.Values` keys correspond 1:1 to `AttributeDescriptor.Key` values.
- Unknown keys in a preset file are ignored on load (forward compatibility).
- Missing keys on load retain their current value (partial preset application).

## State Transitions

### Panel State

```
Closed ──[toggle key]──→ Open
Open   ──[toggle key]──→ Closed
```

### Preset State

```
Default ──[user edits]──→ Modified
Modified ──[save]──→ Saved (name assigned)
Saved ──[user edits]──→ Modified (dirty indicator shown)
Modified ──[reset]──→ Default
Modified ──[load preset]──→ Saved
```

## Validation Rules

- Numeric values clamped to `AttributeDescriptor.Range` on input.
- Color values validated as valid ARGB (alpha defaults to 255 if omitted in hex input).
- Preset names: non-empty, filesystem-safe characters only (alphanumeric, spaces, hyphens, underscores).
- Preset files: unknown keys silently skipped; type mismatches logged and skipped.

## Scale Assumptions

- ~30-40 total attribute descriptors (13 UnitGlyphStyle fields + 11 VizConfig fields + per-faction/per-team color entries).
- ~5-20 preset files maximum (developer tool, not end-user scale).
- Panel renders ~30-40 rows at 24px each = ~720-960px of content; scrollable within typical 1080px window height.
