# Research — Unit Encyclopedia Filters (044)

All items resolved. No `NEEDS CLARIFICATION` markers remain.

## R1 — Reuse existing unit classifiers

**Decision**: Filter predicates derive Tier / Mobility / Faction via the
same `UnitGlyph.classifyTier` / `classifyShape` / `classifyFaction`
helpers already baked into `EncyclopediaData.buildFromBarData`. The
`EncyclopediaEntry` record already exposes `Faction`, `Tier`, and
`Shape`; the filter simply reads those.

**Rationale**: FR-010 mandates byte-consistency between filter behaviour
and the glyph renderer. Re-deriving from BarData would create a second
source of truth and invite drift (cf. feature 038's de-duplication of
classification via `UnitDisplayAdapter`).

**Alternatives considered**:
- Re-classify from `BarData.UnitDef` at filter time — rejected (double
  cost, drift risk).
- Introduce a `Tags: Set<Tag>` field on `EncyclopediaEntry` — rejected
  (larger API blast radius for negligible payoff at three categories).

## R2 — New DUs vs reuse of viz DUs

**Decision**: Introduce `TierFilterKey` and `MobilityFilterKey` closed
DUs in `FSBar.Hub.HubUiTypes` (alongside the existing
`FactionFilterKey`). Bidirectional mapping to `UnitGlyph.Tier` /
`MovementShape` lives in `EncyclopediaFilter`.

**Rationale**: `HubUiTypes` is the Hub's public UI-state contract;
mixing viz-internal DUs into it would couple the hub's wire-visible
state to viz internals. Keeping the three filter DUs co-located also
makes surface-area baselines self-contained.

**Alternatives considered**:
- Reuse `UnitGlyph.Tier` and `MovementShape` directly — rejected to
  avoid cross-module coupling.
- Use plain strings as chip keys — rejected (loses exhaustive matching,
  invites typos in handlers).

## R3 — Empty-set-in-category semantics

**Decision**: Empty set for a category = "all pass for that category".
This matches the existing `FactionFilter` semantics (see
`EncyclopediaTab` render path) and the spec Edge Case "activates every
chip = identical to none".

**Rationale**: Matches user intuition (no chip selected = no
restriction) and keeps the predicate trivially short-circuit-able.

**Alternatives considered**:
- Empty = "nothing passes" — rejected (turns the default state into an
  empty list; hostile UX).

## R4 — Search field semantics

**Decision**: `SearchText` matches case-insensitively (invariant
culture, `String.Contains` with `StringComparison.OrdinalIgnoreCase`)
against:
1. `EncyclopediaEntry.InternalName` (e.g. `"armcom"`)
2. The 2–3 char glyph label from `UnitLabels.generated`
3. The human-readable display name already surfaced in the tab's
   detail pane (derived inside `EncyclopediaFilter.humanName`, which
   mirrors the tab's existing name formatting).

Text is stored trimmed. Empty string = "search disabled" (no
restriction). Max length 128 chars enforced by `setEncyclopedia`.

**Rationale**: Matches "name or label contains 'bomb'" from AS-1 of
User Story 2. 128 chars is far beyond any legitimate BAR unit name and
bounds event-payload size.

**Alternatives considered**:
- Regex / glob matching — rejected (out of scope, hostile to typing).
- Match only `InternalName` — rejected (users recognise display names).

## R5 — Selection stickiness (FR-011)

**Decision**: `EncyclopediaTab` recomputes the filtered list after
every filter mutation and, in the same `setEncyclopedia` call,
preserves `SelectedDefId` iff the selected entry still matches;
otherwise sets it to the first filtered entry's `DefId`, or `None` if
the filtered list is empty.

**Rationale**: Keeps selection-stickiness invariant colocated with the
mutator that can violate it; one `EncyclopediaEvent` per user action
rather than a split "filter then fix selection" pair.

**Alternatives considered**:
- Leave stale selection pinned until next click — rejected (detail pane
  would show a unit not in the visible list; violates FR-011).
- Push the invariant into `HubStateStore.setEncyclopedia` — rejected
  (store has no access to the `EncyclopediaEntry` list; would force a
  layering violation).

## R6 — Out-of-scope for MVP

- Persisting filter state across Hub launches (explicit non-goal).
- Extending the gRPC scripting contract to surface tier/mobility/search
  filters on the wire (the `HubState` event carries them, but no
  dedicated RPC is added).
- New filter categories (weapon / role / cost band) — follow-up.
