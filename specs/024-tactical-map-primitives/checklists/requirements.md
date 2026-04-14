# Specification Quality Checklist: Tactical Map Primitives

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-04-13
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- The spec references the feature 023 iteration history and `FSBar.Client.MapGrid`/`MapQuery` by name because these are the **entities** the feature builds on, not implementation mandates. A reader can substitute "map-data record" and "coordinate-query module" and lose no meaning.
- `MoveType.Kbot/Tank/Hover/Ship` is named because it's an existing project entity (from 004-array-map-layers), not a new design choice in 024. The spec does not prescribe how `findPath` is *implemented* — A*, Dijkstra, Jump Point Search, hierarchical pathing are all valid.
- Performance targets (50 ms wall-clock budget, 95% completion rate in SC-001) are framed in user-facing terms ("the bot should remain responsive") rather than technical budgets.
- The Armada-first default and the reference to specific plans (`defaultArmadaOpening`) are scoped to match the 023 baseline; follow-up features can add other faction plans without re-specifying.
- No NEEDS CLARIFICATION markers — all four user stories were directly implied by the user's request ("that, also add building plans, anti wall in checks"), and the iteration 023 history provides concrete grounding for every edge case.
- Items marked incomplete require spec updates before `/speckit.clarify` or `/speckit.plan`.

## Clarification round — 2026-04-13

Five questions asked and resolved (workflow cap), all recorded in spec `## Clarifications` section:

1. **Tier / location**: primitives added to `FSBar.Client` (Tier 1 compiled module, surface-area baselines updated).
2. **Test fixture strategy**: write an SMF parser reading `.sd7`/`.smf` directly from `~/.local/state/Beyond All Reason/maps/`; synthetic fixtures for edge cases.
3. **Friendly structures in pathing**: yes — `findPath` takes explicit `ownStructures` input, caller owns cache invalidation.
4. **Clearance semantics**: additive margin over footprint edge (not a from-centre exclusion radius).
5. **US5 integration depth**: deep single-pass refactor (opening plan + attack routing + defend-at-chokepoint all replace hardcoded logic in one commit).

Spec deltas from the clarification round:
- New Clarifications section with session entry
- New FR-024..FR-028 block for the SMF parser (5 FRs)
- FR-001/FR-002/FR-003 updated to take `ownStructures` parameter
- New FR-006a for caller-owned cache invalidation
- FR-013/FR-015 updated to use "clearance margin over footprint edge" language
- FR-020 strengthened to tie `wouldWallIn` passability to `findPath`'s (including ownStructures)
- US5 rewritten for deep one-pass integration (was "behavior-preserving observability")
- New SC-010 for SMF parser agreement with live-engine output
- New assumptions for `.sd7` extraction tooling and BAR-install dependency
- New `SmfMap` key entity
- All FR numbering increments: original 026 → 031

Total functional requirements after clarifications: **31** (was 26). Success criteria: **10** (was 9). User stories: **5** (unchanged).

