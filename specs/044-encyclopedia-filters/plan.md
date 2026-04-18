# Implementation Plan: Unit Encyclopedia Filters

**Branch**: `044-encyclopedia-filters` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/044-encyclopedia-filters/spec.md`

## Summary

Extend the Hub Units-tab encyclopedia so users can narrow the unit list by
chip-style tag filters (Faction, Tier, Mobility) plus a free-text search
field. The existing `EncyclopediaSelection` already carries a faction
filter set; this feature grows it to hold tier and mobility sets plus a
search string, routes all mutations through `HubStateStore.setEncyclopedia`
(feature 041 convention), renders a chip row + search input + result count
at the top of the tab, and reuses `UnitGlyph.classifyTier` /
`classifyShape` classifications so filter semantics stay byte-consistent
with the glyph renderer.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: Existing in-repo only — `FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Viz` (`EncyclopediaData`, `UnitGlyph`, `UnitDisplayAdapter`), `SkiaViewer` 1.1.3-dev, `SkiaSharp` 2.88.6. **No new NuGet dependencies.**
**Storage**: In-memory only — filter state lives on `HubState.Encyclopedia` for the Hub process lifetime. No disk persistence (FR-008).
**Testing**: `xUnit 2.9.x` via `tests/FSBar.Hub.Tests/` for the filter-predicate unit tests and `HubStateStore` round-trip; no live-engine tests required.
**Target Platform**: Linux desktop Hub GUI (same env as feature 040/041/042).
**Project Type**: Desktop app (`FSBar.Hub.App`) layered over packable library (`FSBar.Hub`).
**Performance Goals**: Filter apply within a single 60 fps render frame over ~500 encyclopedia entries (SC-002).
**Constraints**: Must not change the `fsbar.hub.scripting.v1` wire contract for MVP; no on-disk schema bumps.
**Scale/Scope**: ~512 unit entries, 3 filter categories, 6 + 4 + 6 chip values.

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| §I Spec-First Delivery | Pass | Spec `spec.md` + this plan. Tier-1 change (extends public `HubUiTypes.EncyclopediaSelection` record + `HubStateStore.setEncyclopedia` semantics). |
| §II Compiler-Enforced Structural Contracts | Pass | `.fsi` updates required for `HubUiTypes`, `EncyclopediaTab`, and any new `EncyclopediaFilter` helper module. Surface-area baselines under `tests/FSBar.Hub.Tests/Baselines/` regenerated with `SURFACE_AREA_UPDATE=1` (covers both `FSBar.Hub` and the `EncyclopediaTab.fsi` surface; no separate `FSBar.Hub.App.Tests` project). |
| §III Test Evidence | Pass | P1: unit tests on the pure filter predicate (AND-across, OR-within, empty-category = pass-all). P2: search-combine test. P3: store round-trip test. |
| §IV Observability | Pass | Filter mutations reuse existing `HubStateStore` `SubmitOutcome` + `HubEvent.EncyclopediaSelectionChanged` paths; no new log surface. |
| §V Scripting Accessibility | Pass | No new public API on `FSBar.Hub` library beyond the extended record; existing `scripts/examples/` coverage unaffected. A one-liner snippet in `quickstart.md` demonstrates extending the filter via FSI. |

No violations. Complexity Tracking table empty.

## Project Structure

### Documentation (this feature)

```text
specs/044-encyclopedia-filters/
├── plan.md              # This file
├── spec.md              # Feature spec
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── encyclopedia-filter.md   # In-process F# contract for the filter predicate + state record
└── checklists/
    └── requirements.md  # Spec-quality checklist (passed)
```

### Source Code (repository root)

```text
src/
├── FSBar.Hub/
│   ├── HubUiTypes.fs(i)              # EXTEND: FactionFilterKey (already present),
│   │                                 #         NEW: TierFilterKey, MobilityFilterKey,
│   │                                 #         EXTEND: EncyclopediaSelection with
│   │                                 #                 TierFilter, MobilityFilter, SearchText.
│   └── HubStateStore.fs(i)           # EXTEND: setEncyclopedia validation (trim SearchText,
│                                     #         reject > 128 chars) — signature unchanged.
└── FSBar.Hub.App/Tabs/
    └── EncyclopediaTab.fs(i)         # EXTEND: render chip row + search input + result count;
                                      #         route every chip/search change through
                                      #         HubStateStore.setEncyclopedia; preserve
                                      #         selection-stickiness (FR-011).
tests/
└── FSBar.Hub.Tests/
    ├── EncyclopediaFilterTests.fs    # NEW: pure-predicate tests (P1, P2 acceptance).
    └── Baselines/                    # REGEN: HubUiTypes.baseline + HubStateStore.baseline
                                      #        + EncyclopediaTab.baseline (all FSBar.Hub*
                                      #        surface baselines live here — no separate
                                      #        FSBar.Hub.App.Tests project exists).
```

**Structure Decision**: Extend the existing `FSBar.Hub` + `FSBar.Hub.App`
pair in place. A new tiny pure module `FSBar.Hub.EncyclopediaFilter`
(`.fs` + `.fsi`) holds the predicate so both the tab's render path and
unit tests share one source of truth.

## Phase 0 — Research

Produced in `research.md`. All items resolved; no `NEEDS CLARIFICATION`
markers remain. Key decisions:

- R1: Reuse `UnitGlyph.classifyTier` / `classifyShape` / `classifyFaction`
  rather than deriving fresh classifiers from BarData, matching FR-010.
- R2: Add two new closed DUs (`TierFilterKey`, `MobilityFilterKey`) rather
  than re-using the glyph-render DUs directly, so the Hub's public UI
  state stays decoupled from viz internals.
- R3: Empty set per category = "all pass" (matches existing
  `FactionFilter` semantics and Edge Case in spec).
- R4: Search matches case-insensitively against `InternalName` +
  `EncyclopediaEntry.InternalName`-derived label (already stored in
  `UnitLabels.generated`) + the human-readable name the tab already
  surfaces; trimmed, max 128 chars, stored as-typed.
- R5: Selection stickiness (FR-011) implemented in `EncyclopediaTab` by
  re-evaluating the predicate after every mutation and updating
  `SelectedDefId` in the same `setEncyclopedia` call.

## Phase 1 — Design & Contracts

### Data model — `data-model.md`

Central record (all fields immutable F# records):

```fsharp
type TierFilterKey    = | T1 | T2 | T3 | Commander
type MobilityFilterKey = | Building | Ground | Hover | Ship | Air | Amphib

type EncyclopediaSelection = {
    FactionFilter : Set<FactionFilterKey>   // unchanged — already exists
    TierFilter    : Set<TierFilterKey>      // NEW
    MobilityFilter: Set<MobilityFilterKey>  // NEW
    SearchText    : string                  // NEW, trimmed, ≤128 chars
    SelectedDefId : int option              // unchanged
}
```

Invariants (enforced in `HubStateStore.setEncyclopedia`):
- `SearchText` is trimmed and length-capped at 128 characters; overruns
  rejected with `SubmitOutcome.Rejected "search text > 128 chars"`.
- `SelectedDefId` MUST either be `None` or reference a unit still
  matching every active filter; tab code maintains this invariant —
  store does not validate it.

### Contract — `contracts/encyclopedia-filter.md`

In-process F# contract for the pure predicate:

```fsharp
module EncyclopediaFilter =
    val matches
        : selection: EncyclopediaSelection
       -> entry:     EncyclopediaData.EncyclopediaEntry
       -> bool
    val apply
        : selection: EncyclopediaSelection
       -> entries:   EncyclopediaData.EncyclopediaEntry list
       -> EncyclopediaData.EncyclopediaEntry list
    val humanName
        : entry: EncyclopediaData.EncyclopediaEntry
       -> string
```

No gRPC/proto surface changes for MVP. The existing `SelectUnit` RPC
plus the `HubState.Encyclopedia` snapshot already expose selection; the
additional filter fields are carried forward on `HubState` events for
free (they're part of `EncyclopediaSelection`) without a proto bump —
consumers that don't care ignore the new fields.

### Quickstart — `quickstart.md`

FSI walkthrough showing:
1. Launch Hub with `FSBAR_HUB_INITIAL_TAB=Units`.
2. Click chip row to activate Arm + T2.
3. Observe `StreamHubStateEvents` emit `EncyclopediaSelectionChanged`.
4. Script-side: build an `EncyclopediaSelection`, apply
   `EncyclopediaFilter.apply`, verify count matches Hub display.

### Agent context update

Run `.specify/scripts/bash/update-agent-context.sh claude` after this
file lands so `CLAUDE.md` picks up the new module.

## Complexity Tracking

> None — no constitution gate violations.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| — | — | — |
