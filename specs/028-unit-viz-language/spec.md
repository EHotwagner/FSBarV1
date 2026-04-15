# Feature Specification: Unit Visual Representation for SkiaViewer

**Feature Branch**: `028-unit-viz-language`
**Created**: 2026-04-15
**Status**: Draft
**Input**: User description: "create visual representation of units and buildings for skiaviewer. there should be permanent attributes displayed and optional per keypress/script. the display should be completely functional informative. every aspect designed to convey information without unnecessary fluff... maximal informational density with no useless graphical information."

## Clarifications

### Session 2026-04-15

- Q: How is tech tier (T1/T2/T3) derived for each unit, given `BarData.UnitDef` has no explicit tier field? → A: Read `customParams["techlevel"]` directly; fallback to parsing `category` for `LEVEL1`/`LEVEL2`/`LEVEL3` tokens; default T1 if neither present.
- Q: How is a unit's `movementClass` mapped to one of the six silhouettes? → A: Rule stack — (1) `!canMove` → building (hexagon); (2) `canFly` → air (triangle); (3) prefix-match `movementClass` — `BOT`/`KBOT`/`ARMBOT` → bot (circle), `TANK`/`VEHICLE`/`ATV` → vehicle (square), `HOVER` → hover (diamond), `BOAT`/`UBOAT`/`SHIP` → ship (rounded rect); (4) unknown → fallback silhouette + log once.
- Q: Which overlays are in MVP scope versus deferred? → A: MVP ships `W` weapon ranges, `L` sight, `C` command queue, `N` full names. Deferred to follow-up features: `R` radar/jammer, `E` economy pulse, `B` build reach, `T` threat heatmap, `V` velocity vector, `I` info card, `X` cloak reveal.
- Q: Which `GameState` source must the MVP consume? → A: `FSBar.SyntheticData` only. Live-game consumption via `FSBar.Client` is a follow-up feature that swaps the data source; MVP defines the visual language on deterministic synthetic fixtures.
- Q: How is a unit's faction (Armada / Cortex / Legion / Raptors / Scavengers) derived? → A: Match the second path segment of `BarData.UnitDef.subfolder` (`armada` / `cortex` / `legion` / `scavengers` / `raptors`); fallback to unit-name prefix (`arm`/`cor`/`leg`/`scav`/`rap`) when subfolder is empty or unrecognized; unresolved units render with a neutral stroke and log once.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read unit identity and state at a glance (Priority: P1)

An observer watching a live or replayed game needs to identify — without any interaction — what every unit on the map is, which team owns it, which faction it belongs to, what tech tier it is, which direction it is facing, and whether it is healthy, damaged, under construction, or disabled. The permanent visual language must encode all of this simultaneously using shape, size, color, stroke, and a compact unique label, so that reading one unit costs a single glance and reading the whole map costs no more than a sweep of the eyes.

**Why this priority**: This is the baseline of the feature. Without a readable permanent layer the overlays are meaningless, because the observer cannot tell what they are looking at. Every other story builds on this one.

**Independent Test**: Load a representative `GameState` snapshot containing units from every movement class, faction, tier, and state (healthy, damaged, under-construction, stunned) into the SkiaViewer, and verify by visual inspection that each unit's identity and state can be read without toggling any overlay.

**Acceptance Scenarios**:

1. **Given** a snapshot containing one unit of each movement class (bot, vehicle, hover, ship, air, building), **When** the viewer renders the scene, **Then** each unit is drawn with a distinct silhouette that unambiguously conveys its movement class.
2. **Given** two units of the same type owned by two different teams, **When** the viewer renders the scene, **Then** the two units are distinguishable by fill color with no change in shape.
3. **Given** an Armada, a Cortex, and a Legion unit of the same tier, **When** the viewer renders the scene, **Then** the three units are distinguishable by stroke color with no change in fill or shape.
4. **Given** a T1, T2, and T3 unit of the same faction, **When** the viewer renders the scene, **Then** the three units are distinguishable by stroke width.
5. **Given** a unit at 100% HP and a unit at 40% HP, **When** the viewer renders the scene, **Then** the first shows no HP arc and the second shows a visible arc on the side of the unit opposite the facing pip, proportional to the damage taken.
6. **Given** a unit facing east and the same unit facing north, **When** the viewer renders the scene, **Then** the facing pip is located on the east and north perimeter respectively, such that the observer can determine the unit's heading without reference to any overlay.
7. **Given** a unit under construction at 30% buildProgress, **When** the viewer renders the scene, **Then** the unit is drawn with a dashed stroke and a fill whose alpha visibly indicates construction progress.
8. **Given** a unit that has just been hit, **When** the viewer renders the next frame, **Then** the unit's stroke briefly pulses red without requiring any toggle.
9. **Given** a stunned or EMP'd unit, **When** the viewer renders the scene, **Then** the unit is drawn desaturated and continues to render correctly when it recovers.
10. **Given** any BAR unit from the full `BarData` catalog, **When** the viewer draws its label, **Then** the label is 2 characters if possible and 3 characters otherwise, is stable across sessions, and is unique relative to every other label in the catalog.

---

### User Story 2 - Toggle informational overlays with sticky hotkeys (Priority: P2)

An observer studying a specific aspect of the battle — ranges, line of sight, economy flow, command intent, threat concentration — needs to enable and disable overlays one at a time without losing focus on the map. Each overlay is a single keypress (sticky toggle, not hold), each overlay is independent, and each overlay is composable with every other overlay so the observer can layer information as deeply as they want. A status indicator shows which overlays are currently active.

**Why this priority**: The permanent layer answers "what is this unit"; overlays answer "what is this unit doing and what can it do to me." Overlays are where the viewer becomes an analytical tool rather than a minimap. They are lower priority than P1 only because they are meaningless without the permanent layer underneath.

**Independent Test**: With a snapshot loaded, press each overlay hotkey in turn and verify that the corresponding overlay appears exactly once, persists after the key is released, disappears on a second press, can be combined with any other overlay, and is reflected in the status-line indicator.

**Acceptance Scenarios**:

1. **Given** the viewer is showing the permanent layer only, **When** the observer presses `W`, **Then** weapon range rings appear for every armed unit and persist until `W` is pressed again.
2. **Given** weapon ranges are active, **When** the observer presses `L`, **Then** sight ranges also appear without weapon ranges disappearing, and both remain visible until individually toggled off.
3. **Given** any set of overlays is active, **When** the observer looks at the status line, **Then** the status line lists exactly those overlays that are currently active, using single-letter codes.
4. **Given** overlay `E` (economy pulse) is active, **When** the viewer renders successive frames, **Then** producers visibly pulse in green (metal) or yellow (energy) and consumers pulse red, with pulse intensity proportional to their rate.
5. **Given** overlay `C` (command queue) is active, **When** a unit has queued orders, **Then** a polyline is drawn through each waypoint in queue order, color-coded by order type.
6. **Given** overlay `B` (build reach) is active on a builder that is currently constructing, **When** the viewer renders, **Then** a dashed circle at the builder's build distance is drawn and a line connects the builder to its current build target.
7. **Given** overlay `N` (full names) is active, **When** the viewer renders, **Then** each unit's full internal name is drawn beside its shape, at a size chosen for legibility rather than constrained to unit footprint.

---

### User Story 3 - Unique labels across the full unit catalog (Priority: P2)

The observer must be able to identify any unit type from its 2-char label alone, across every faction and every unit in `BarData`. Because 2 characters are insufficient for ~953 unique units naively, a deterministic generator walks the catalog at build time, derives a code from the unit's internal name by stripping the faction prefix and selecting letters from the remainder, resolves collisions by walking deeper into the name, and falls back to 3 characters only when 2 characters cannot yield uniqueness. The resulting lookup is stable across sessions and checked into the repository so that codes do not silently change under observers who have memorized them.

**Why this priority**: Ties with Story 2 as P2 because although labels are part of the permanent layer, the uniqueness guarantee is an independent workstream (a build-time generator + lookup table) that can be delivered and tested without the renderer being complete.

**Independent Test**: Run the label generator against the current `BarData` catalog, inspect the generated lookup table for duplicates, verify every entry is 2 or 3 characters, verify the mapping is deterministic across runs, and verify that every unit type encountered in a snapshot resolves to a label.

**Acceptance Scenarios**:

1. **Given** the full `BarData.AllUnits` list, **When** the generator runs, **Then** every entry in the output table is unique.
2. **Given** the generator output, **When** any entry is 3 characters, **Then** a 2-character code for that unit would collide with another entry in the table.
3. **Given** a unit internal name that begins with a faction prefix (`arm`, `cor`, `leg`, `rap`, `scav`), **When** the generator processes it, **Then** the prefix is stripped before deriving the code.
4. **Given** the generator is run twice against the same `BarData` version, **When** the two outputs are compared, **Then** they are byte-identical.
5. **Given** a new `BarData` version introduces new units, **When** the generator is re-run, **Then** existing labels do not change unless a genuine collision with a new unit forces a reassignment, and any reassignment is surfaced to the developer.

---

### User Story 4 - Scale, zoom, and declutter (Priority: P3)

The observer zooms from full-map overview down to a single base. At full-map zoom, individual 1x1 footprints would be subpixel and labels would be illegible; at full zoom, oversized icons would occlude the terrain. The renderer must draw units to true footprint scale in world space, clamp to a minimum visible pixel radius so small units do not disappear, and suppress labels below a zoom threshold at which they would become unreadable, returning instead to shape-only rendering.

**Why this priority**: Readability at varying zoom is a real requirement but can be layered on top of a fixed-zoom MVP. P3 because an MVP at a single canonical zoom still delivers Stories 1 and 2.

**Independent Test**: Load a snapshot and exercise the zoom slider from minimum to maximum; verify small units remain visible at every zoom level, labels disappear below the legibility threshold, and no unit is occluded by its own label at any zoom.

**Acceptance Scenarios**:

1. **Given** a 1x1 footprint unit rendered at minimum zoom, **When** the viewer draws the scene, **Then** the unit is rendered at the configured minimum pixel radius rather than at its true footprint scale.
2. **Given** the viewer is zoomed out below the label legibility threshold, **When** the scene is drawn, **Then** no labels are rendered and shape, color, and stroke continue to carry identity and state.
3. **Given** overlay `N` (full names) is active at any zoom, **When** the scene is drawn, **Then** full names are rendered regardless of the label legibility threshold, at whatever size the observer configures for legibility.

---

### Edge Cases

- A unit whose movement class is not represented in the six silhouette categories (e.g. a static weapon, a non-building immobile, an amphibious class) is drawn with a fallback silhouette and logged once so the catalog can be extended.
- A unit with zero maxHP (or pre-spawn) renders without an HP arc and without the low-HP shader.
- A unit whose team color is near-black or near-white against the map background still reads; stroke color provides a luminance-independent second channel.
- A snapshot contains a `defId` that is not in `BarData` — the renderer falls back to a neutral "unknown" silhouette and a `??` label rather than crashing.
- A unit with no facing data (e.g. a partially serialized snapshot) renders the pip at a fixed default angle and flags the unit visually as "facing unknown."
- A unit under construction at 0% buildProgress is drawn with minimum-visible alpha but not invisible, so the observer can see where structures will be.
- Two overlays that both draw rings on the same unit (e.g. `W` weapon and `L` sight) render both rings distinctly (different stroke style or color) so neither is hidden by the other.
- An overlay that requires per-unit animation (`E` economy pulse, `T` threat) continues to animate smoothly under normal snapshot update rates without consuming meaningful frame budget.
- Toggle state persists for the lifetime of the viewer session but does not persist across viewer restarts unless the observer explicitly saves a profile (out of scope for this feature).

## Requirements *(mandatory)*

### Functional Requirements

#### Permanent visual layer

- **FR-001**: The renderer MUST draw every unit with a silhouette determined by movement class, using exactly six shapes: circle (bot), square (vehicle), diamond (hover), rounded rectangle (ship), triangle (air), hexagon (building). Movement class is resolved by a deterministic rule stack: (a) if the unit cannot move it is a building; (b) else if the unit can fly it is air; (c) else its `movementClass` string is prefix-matched against `BOT`/`KBOT`/`ARMBOT` (bot), `TANK`/`VEHICLE`/`ATV` (vehicle), `HOVER` (hover), and `BOAT`/`UBOAT`/`SHIP` (ship); (d) any unit that falls through every rule is drawn with a neutral fallback silhouette and logged once so the rule stack can be extended.
- **FR-002**: The renderer MUST draw every unit at its true `BarData` footprint size in world space, clamped to a configurable minimum pixel radius so that small units remain visible at low zoom.
- **FR-003**: The renderer MUST draw every unit's fill in its owning team's color.
- **FR-004**: The renderer MUST draw every unit's stroke in a color determined by faction (Armada, Cortex, Legion, Raptors, Scavengers), independent of team color. Faction is derived deterministically by matching the second path segment of `BarData.UnitDef.subfolder` against the known faction names; when the subfolder is empty or unrecognized, the renderer falls back to matching the unit-name prefix (`arm`, `cor`, `leg`, `scav`, `rap`); unresolved units render with a neutral stroke and the miss is logged once so the derivation can be extended.
- **FR-005**: The renderer MUST draw every unit's stroke width proportional to its tech tier (T1 thin, T2 medium, T3 thick). Tier is derived deterministically by reading `customParams["techlevel"]` on the `BarData.UnitDef` when present; when absent, by scanning the `category` string for a `LEVEL1` / `LEVEL2` / `LEVEL3` token; when neither is present, the unit is treated as T1 and the miss is logged once so the derivation can be extended.
- **FR-006**: The renderer MUST draw a facing pip as a small glowing dot on the unit's perimeter at the angle corresponding to its current heading, such that the observer can read the unit's facing without enabling any overlay.
- **FR-007**: The renderer MUST draw an HP arc as a ring segment positioned opposite the facing pip, whose length is proportional to damage taken; the arc MUST be hidden when the unit is at full HP and MUST shift to a red hue below 25% HP.
- **FR-008**: The renderer MUST draw a 2-character (or 3-character fallback) label beside every unit whose code comes from the generated `BarData` label table (FR-020), positioned for legibility rather than constrained to unit footprint size.
- **FR-009**: The renderer MUST draw units under construction with a dashed stroke and a fill alpha proportional to their `buildProgress`.
- **FR-010**: The renderer MUST apply a low-HP visual indicator (red tint overlay) automatically to any unit below 25% HP, requiring no overlay toggle. A procedural noise component is deferred to a follow-up feature that ships a SkiaSharp shader pass.
- **FR-011**: The renderer MUST pulse a unit's stroke red for a brief interval when its HP decreases, without requiring any overlay toggle.
- **FR-012**: The renderer MUST desaturate any unit that is stunned or EMP'd for the duration of the effect.
- **FR-013**: The renderer MUST render a just-completed build with a one-shot green ring that fades out over a short interval.

#### Overlay layer

- **FR-014**: The viewer MUST expose each MVP overlay (`W` weapon ranges, `L` sight, `C` command queue, `N` full names) as a sticky toggle bound to a single hotkey, such that tapping the key enables or disables the corresponding overlay without affecting other overlays. The toggle-dispatch layer MUST be extensible so that the deferred overlays (`R`, `E`, `B`, `T`, `V`, `I`, `X`) can be added later without reworking the input or composition logic.
- **FR-015**: The viewer MUST display a status line that shows, using single-letter codes, exactly which overlays are currently active.
- **FR-016**: Overlays MUST compose: enabling any combination of overlays MUST render all of them simultaneously, and no overlay MUST mask or occlude any other without the observer's explicit action.
- **FR-017**: Overlay `W` (weapon ranges) MUST draw a stroked circle at each weapon's maximum range for every armed unit.
- **FR-018**: Overlay `L` (sight) MUST draw a dashed circle at each unit's `sightDistance`, rendered in a stroke style distinct from `W` so both overlays can be read when composed.
- **FR-019**: Overlay `C` (command queue) MUST draw a polyline through the unit's queued waypoints, color-coded by order type (move, attack, patrol, guard, build, reclaim), with the current order highlighted.
- **FR-019a**: Overlay `N` (full names) MUST draw each unit's full internal name beside its shape at a legibility-chosen size, rendered regardless of the zoom-based label-legibility threshold that suppresses the default 2-char labels.

#### Label generator

- **FR-020**: A build-time generator MUST produce a deterministic lookup table mapping every unit in `BarData.AllUnits` to a unique 2- or 3-character label, with 3-character labels used only when 2 characters cannot be made unique.
- **FR-021**: The label generator MUST strip faction prefixes (`arm`, `cor`, `leg`, `rap`, `scav`) from internal names before deriving codes, so that codes read as mnemonics of the unit rather than restatements of faction.
- **FR-022**: The label generator MUST produce byte-identical output for a given `BarData` version, and MUST minimize label churn when `BarData` changes by preserving existing labels wherever no genuine collision forces a reassignment.
- **FR-023**: The label generator output MUST be checked into the repository so that observer memory of unit codes remains valid across sessions and machines.

#### Event effects

- **FR-024**: The renderer MUST drive automatic event effects (under-attack flash, just-built ring, stunned desaturate, low-HP shader) from per-frame state deltas without requiring any observer interaction.
- **FR-025**: Automatic event effects MUST have a bounded lifetime so that they do not accumulate into persistent visual noise.

### Key Entities

- **UnitDisplayInfo**: the merged view a renderer needs for a single unit. Combines static `BarData` fields (movement class, footprint, faction, tier, weapons, sight range, build distance, economy flows, label) with live `GameState` fields (team, position, heading, currentHealth, maxHealth, buildProgress, statusFlags). Stable enough to cache across frames for static parts; refreshed per frame for live parts.
- **LabelTable**: the generated `BarData`-derived map from unit internal name to a unique 2- or 3-character display code, committed to the repository, regenerated by the build-time label generator whenever `BarData` changes.
- **OverlayState**: the current on/off state of every sticky overlay plus the status-line projection. Scoped to the viewer session.
- **EventEffect**: a bounded-lifetime animation attached to a specific unit by an automatic trigger (damage, completion, stun, death). Advances with the viewer's animation clock and removes itself when expired.
- **FactionPalette** and **TeamPalette**: the two independent color tables that drive stroke and fill. Separated so faction reads survive any team-color configuration and vice versa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An observer presented with a snapshot they have never seen can correctly identify, without enabling any overlay, the movement class, faction, tier, team, facing, and rough HP state of any chosen unit in under 2 seconds.
- **SC-002**: The label table generated against the current `BarData` version contains zero duplicate labels across the full unit catalog, and at least 90% of labels are 2 characters (3-character fallbacks only where 2 cannot be unique).
- **SC-003**: An observer can enable, compose, and disable any combination of overlays using only single-key presses, and can read the set of active overlays from the status line in under 1 second.
- **SC-004**: The renderer draws a 200-unit scene at interactive frame rates (≥ 30 fps for the viewer's standard window) with all permanent attributes and up to three overlays active simultaneously.
- **SC-005**: No automatic event effect persists longer than its bounded lifetime, and no combination of overlays produces visual noise that hides a unit's permanent identity for longer than the duration of a single event effect.
- **SC-006**: When `BarData` is updated and the label generator is re-run, at least 95% of existing labels are preserved unchanged, and any reassignment is surfaced to the developer rather than silently applied.

## Assumptions

- The renderer consumes `GameState` snapshots produced by `FSBar.SyntheticData` for the MVP. Snapshots expose team, faction, unit defId, heading, current HP, max HP, buildProgress, and basic status flags for every unit; if any of these are missing, the edge case rules apply. Live-game consumption via `FSBar.Client` is out of scope for this feature and will be delivered as a follow-up that swaps the data source without changing the visual language.
- Faction and tier are derived from `BarData.UnitDef` fields as specified in FR-004 and FR-005; units that cannot be resolved fall back to neutral defaults and are logged once.
- SkiaViewer's existing declarative Scene API is sufficient to render all permanent attributes and overlays described here; if a specific effect (e.g. shader animation) is not yet expressible, it is scoped into a follow-up feature rather than blocking the MVP.
- 2-character labels are case-sensitive (62-char alphabet) which gives 3,844 codes — more than enough headroom for ~1,000 units even under realistic collision rates.
- Zoom and pan are handled by existing SkiaViewer camera infrastructure; this feature adds only the minimum-pixel clamp and the label-legibility threshold, not new camera behavior.
- Observer ergonomics assume a single-observer viewing session on a standard keyboard; multi-observer, touchscreen, and controller input are out of scope.
- Colorblind accessibility (alternate faction palettes) is out of scope for the MVP but the faction-vs-team separation guarantees that identity information is never carried by a single color channel alone, leaving the door open.
