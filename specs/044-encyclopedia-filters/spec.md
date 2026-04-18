# Feature Specification: Unit Encyclopedia Filters

**Feature Branch**: `044-encyclopedia-filters`
**Created**: 2026-04-18
**Status**: Draft
**Input**: User description: "implement filters for the unit encyclopedia in the hub. tags to filter by like tier, faction, mobility...."

## User Scenarios & Testing

### User Story 1 - Narrow the list by core tags (Priority: P1)

A Hub user browsing the Units tab wants to quickly find units matching one
or more categorical tags — faction (Arm/Cor/Leg/etc.), tier (T1/T2/T3/Com),
and mobility class (Building / Ground / Hover / Ship / Air / Amphib) —
without scrolling through hundreds of entries.

**Why this priority**: The encyclopedia is currently a flat list; tag
filtering is the single biggest usability win and unblocks every later
filter refinement.

**Independent Test**: Open the Units tab, click a faction chip (e.g.
"Arm") and a tier chip (e.g. "T2"). The unit list shrinks to only Arm T2
units; clearing the chips restores the full list. No other tab or
workflow is affected.

**Acceptance Scenarios**:

1. **Given** the Units tab is open with all units shown, **When** the user activates the "Arm" faction chip, **Then** only Arm-faction units appear in the list and the active-filter summary shows "Arm".
2. **Given** the "Arm" faction chip is active, **When** the user also activates the "T2" tier chip, **Then** the list narrows further to Arm T2 units (AND across categories).
3. **Given** multiple faction chips ("Arm" + "Cor") are active, **When** the user views the list, **Then** units from either faction appear (OR within the faction category).
4. **Given** any filter is active, **When** the user clicks "Clear filters", **Then** the list returns to the unfiltered state and all chips become inactive.

---

### User Story 2 - Combine tag filters with free-text search (Priority: P2)

The user wants to narrow by tag first, then type part of a unit name or label to find a specific unit within the filtered set.

**Why this priority**: Search already exists implicitly via scroll; layering it onto filters amplifies the P1 value without being required for MVP.

**Independent Test**: Activate "Air" mobility, type "bomb" in the search box, and confirm only air bombers appear. Clear the text — only the tag filter remains active.

**Acceptance Scenarios**:

1. **Given** the "Air" mobility chip is active, **When** the user types "bomb" in the search field, **Then** the list shows only air units whose name or label contains "bomb" (case-insensitive).
2. **Given** a search string is active with no tag filters, **When** the user activates a tag chip, **Then** both conditions apply together.

---

### User Story 3 - Remember my filter state (Priority: P3)

When the user switches away from the Units tab and returns, the previously active filter chips should still be active so they don't lose their place.

**Why this priority**: Quality-of-life polish; users can reapply filters manually without it.

**Independent Test**: Set filters on the Units tab, switch to the Viewer tab, switch back to Units — chips remain active.

**Acceptance Scenarios**:

1. **Given** filters are active on the Units tab, **When** the user switches to another tab and back, **Then** the filter chips and search text are preserved for the current Hub session.
2. **Given** the user closes and relaunches the Hub, **When** the Units tab first opens, **Then** filters start in the cleared state (session scope only, not persisted to disk).

---

### Edge Cases

- A filter combination matches zero units → the list shows a neutral empty-state message ("No units match the active filters") plus a one-click "Clear filters" action.
- A unit's underlying data is missing a tag (e.g. faction classifier returns the catch-all bucket) → the unit is excluded from any filter that names a specific faction, but included when no faction chip is active, and appears under the `Neutral` chip inside the faction category so it is still discoverable.
- The user activates every chip in a category → semantically identical to activating none in that category (all pass); the UI reflects this by offering a "Select all / none" toggle per category.
- The currently-selected unit falls out of the filtered set → selection moves to the first unit of the filtered list, or becomes empty when the list is empty.

## Requirements

### Functional Requirements

- **FR-001**: The Units tab MUST expose a filter surface with at least three chip-style categories: Faction, Tier, and Mobility.
- **FR-002**: Within a single category, active chips MUST combine with OR semantics (e.g. Arm OR Cor); across categories the combined predicate MUST use AND semantics (e.g. Arm AND T2 AND Ground).
- **FR-003**: The filter surface MUST offer at minimum these chip values, derived from existing unit-classification helpers:
  - Faction: Arm, Cor, Leg, Scav, Raptor, Neutral (chip display labels; DU case names reuse the existing `FactionFilterKey` cases `Armada, Cortex, Legion, Scavengers, Raptors, Neutral` — the `Neutral` chip covers unclassified / environment factions)
  - Tier: T1, T2, T3, Commander
  - Mobility: Building, Ground, Hover, Ship, Air, Amphib
- **FR-004**: The filter surface MUST include a single "Clear filters" action that deactivates every chip and empties the search field in one click.
- **FR-005**: The filter surface MUST include a free-text search field that case-insensitively matches against a unit's display label, internal name, and human-readable name.
- **FR-006**: The filtered result count MUST be visible to the user (e.g. "42 of 512 units shown") and update within one frame of any filter change.
- **FR-007**: When the active filter set matches zero units, the tab MUST render a clearly labelled empty state rather than a blank panel, and MUST offer an inline "Clear filters" control.
- **FR-008**: Filter state MUST persist for the duration of a single Hub process lifetime across tab switches, and MUST reset to "no filters active" on Hub relaunch.
- **FR-009**: Filter state changes MUST flow through the Hub's central state-routing convention so that parallel observers (scripting, future remote clients) see a consistent snapshot.
- **FR-010**: Tag classification for each unit MUST reuse the existing faction / tier / mobility-shape derivations already used by the encyclopedia and unit-glyph renderer so filter behaviour stays byte-consistent with on-screen glyphs.
- **FR-011**: The currently-selected unit in the Units tab MUST remain selected after a filter change if it still satisfies the active filter; otherwise the selection MUST move to the first unit of the filtered list, or become empty when the result set is empty.

### Key Entities

- **FilterChip**: A single togglable tag value inside a category (e.g. `Faction=Arm`). Attributes: category, display label, active flag, predicate over a unit entry.
- **FilterCategory**: A named group of FilterChips with a uniform combination semantic (OR within the category). Attributes: name, ordered list of chips, "select all / none" affordance.
- **FilterState**: The current active selection across all categories plus the free-text search string. Scope: single Hub session.
- **EncyclopediaEntry (existing)**: The source data each filter evaluates against; attributes relevant to filtering include faction, tier, movement/mobility shape, display label, internal name.

## Success Criteria

### Measurable Outcomes

- **SC-001**: A user can locate any named Arm T2 ground unit from an unfiltered encyclopedia in under 10 seconds using only filter chips plus search, versus open-ended scrolling.
- **SC-002**: Applying or clearing any filter updates the visible list within one render frame (no visible re-layout lag).
- **SC-003**: At least 95% of units in the shipped `BarData` set are reachable through at least one faction + tier + mobility combination (no more than 5% fall into the `Unknown` bucket for every category simultaneously).
- **SC-004**: Zero-result filter combinations never leave the user on a blank panel — 100% of such states show the labelled empty state and a one-click clear action.

## Assumptions

- Faction, tier, and mobility classifications are already computable from the existing encyclopedia data; no new upstream unit metadata needs to be sourced.
- Filter state is session-scoped only; persistence across Hub launches is explicitly out of scope for this feature.
- The existing Units tab layout has enough vertical room above the unit list for a chip row (or can accommodate a collapsible filter header) without redesigning the surrounding tab chrome.
- Filter chip vocabulary is fixed at the categories named in FR-003 for this feature; additional tag dimensions (weapon type, role, resource cost band, etc.) are a follow-up.
- No changes to the on-wire scripting contract are required for MVP; exposing filter state via gRPC is a follow-up if remote clients need it.
