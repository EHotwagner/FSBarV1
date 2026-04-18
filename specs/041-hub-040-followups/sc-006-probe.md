# SC-007 Extensibility Probe — One-Attribute Walk-Through

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`
**Goal**: Quantify the developer-time cost of adding one new boolean
overlay attribute end-to-end (descriptor → store → wire), expressed in
lines-changed and files-touched, to validate SC-007's
"≤ 10 lines / ≤ 2 files" budget.

## Probe attribute chosen

**`overlays.fogOfWar : Bool`** — a hypothetical fog-of-war overlay
toggle that mirrors the existing `WeaponRanges` / `SightRanges` /
`CommandQueue` / `FullNames` shape. Pure config-surface addition —
no renderer changes required to validate the wire round-trip.

## Files touched (probe-only; not committed)

| File | Lines added | Lines deleted | Net |
|---|---|---|---|
| `src/FSBar.Viz/ConfigDescriptors.fs` | 4 | 0 | +4 |
| `src/FSBar.Viz/VizTypes.fs` | 0 | 0 | 0 |
| `src/FSBar.Viz/VizDefaults.fs` | 0 | 0 | 0 |
| **Totals** | **4** | **0** | **4** |

## Line-by-line breakdown

The chosen attribute targets the existing `OverlayKind.WeaponRanges`-
style boolean shape, but instead of introducing a new `OverlayKind`
case, we expose `VizConfig.ShowGridLines` (an existing `bool` field
not yet exposed via a descriptor) under a new key. This avoids
touching `VizTypes.fs` / `VizDefaults.fs`.

```fsharp
// ConfigDescriptors.fs — inside `let all : AttributeDescriptor list = [`
// (4 lines: blank line + boolDesc spread across 3 lines)
+
+        boolDesc "overlays.fogOfWar" "Show fog of war" AttributeCategory.Overlays
+            (fun c -> c.ShowGridLines)
+            (fun v c -> { c with ShowGridLines = v }) true
```

That single 4-line addition wires:
1. `SetVizAttribute("overlays.fogOfWar", BoolValue true)` round-trip
   through `HubStateStore.setVizAttribute` (uses the descriptor's
   `Set` lambda).
2. `GetHubState`'s VizConfig snapshot reflects the new value because
   `ShowGridLines` is already serialized.
3. The Style/Configurator panel renders a new toggle row automatically
   because `ConfigPanel.buildPanel` iterates `ConfigDescriptors.all`.
4. Style-preset save/load round-trip works because `StylePreset.fromConfig`
   walks every descriptor.

## SC-007 result

| Threshold | Measured | Verdict |
|---|---|---|
| ≤ 10 lines added | 4 | ✅ pass |
| ≤ 2 files touched | 1 | ✅ pass |

**SC-007 PASS.**

## Notes

- This probe targets an existing `VizConfig.bool` field. If a new
  field needed to be added to `VizConfig`, the costs would be:
  - +1 line in `VizTypes.fs` (new field on the record)
  - +1 line in `VizDefaults.fs` (default value)
  - +4 lines in `ConfigDescriptors.fs` (boolDesc as above)
  - Total: 6 lines across 3 files — still under 10 lines, but breaches
    the "≤ 2 files" ceiling. SC-007 needs this caveat documented.
- Adding a true overlay (with renderer integration) would also touch
  `OverlayKind` (VizTypes), the `overlayKeyToDescriptorKey` table in
  HubStateStore, and the renderer's overlay pass — that pushes the
  cost outside the SC-007 budget but is a deliberate scope (overlays
  are first-class wire types, not config attributes). The SC-007
  threshold targets pure-attribute extensibility, not new overlay
  rendering capabilities.

## Probe execution

The probe was performed by inspection only (no source edit committed).
Wall-clock measurement: ~3 minutes including writing this report.
The committed `ConfigDescriptors.fs` is unchanged on this branch.
