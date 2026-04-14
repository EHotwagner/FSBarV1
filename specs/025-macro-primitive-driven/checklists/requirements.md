# Specification Quality Checklist: Macro Bot Primitive-Driven Command Path

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-04-14
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

### Content Quality notes

- The spec uses `BasePlan`, `Pathing.findPath`, `Chokepoints`, `WallIn`, `MapGrid`, `resolvePlan` and similar identifiers throughout. These are not "implementation details" in the traditional sense — they are the **entity names** from the Tier-1 public API surface of feature 024, which this feature consumes. A reader could substitute "plan resolver" for `BasePlan.resolvePlan`, "route planner" for `Pathing.findPath`, "connectivity check" for `WallIn.wouldWallIn`, and so on without losing meaning. The module names are documentary anchors that let the reader cross-reference the already-shipped code, not prescriptions on how to implement 025.
- Concrete frame numbers (f=12500, 1000-frame catch-up budget, 100 ms warmup budget), trace strings (`[plan] issuing BuildCommand`, `[attack] path waypoints=N`), and command counts (`12 × N MoveCommand entries`) are the language the trainer's observability pipeline already speaks — they are success-criterion anchors, not implementation prescriptions. Any rewrite that produces the same traces and the same run-dir shape satisfies the spec regardless of how the F# source is organised.
- The scope is deliberately bounded to `bot_macro.fsx` (plus a possible organic extraction into a new `helpers/primitive_driver.fsx` under the 020 two-site rule). The 024 primitive modules themselves are explicitly marked off-limits in FR-021.

### Requirement Completeness notes

- **Zero [NEEDS CLARIFICATION] markers**. The feature description is a direct follow-up to the 024 honest-audit gap list, so every open question from that audit is either:
  - Answered by a reasonable default grounded in the 024 merge state (e.g., "re-resolve plan on every tick" assumption),
  - Explicitly listed as an **Assumption** with its alternative called out (e.g., the "extend the cache vs re-parse .sd7 at warmup" decision is deferred to plan-phase research),
  - Or **explicitly out of scope** (BARb clean wins, chokepoint minImpact tuning, thread safety, etc.).
- US5 is phrased as a **P1 invariant**, not a P1 user story. The distinction matters because US5 is verified by a single `run.sh` invocation with a binary pass/fail — it has no independent implementation, it is an acceptance gate over US1–US4 landing together.
- SC-002 is phrased so that the two mutually-exclusive traces (`[plan] issuing BuildCommand …` vs. `[opening] idx=N issuing BuildCommand`) give a precise, grep-able oracle for whether the 023 helper is on the command path or the 024 resolver is. Without this, "driven by resolvePlan" would be ambiguous.
- Edge-case enumeration covers the real risks surfaced in the 024 implementation: mid-game structure destruction, `findPath` budget exhaustion, stale cache, cache miss on a new map, defend during Upgrade. Each has a documented expected behaviour in the spec.
- **One hidden risk not called out**: if the 024 `replayBufferEnabled` flag is cleared mid-match by a future Protocol change, US4's warmup catch-up protection evaporates. This is listed under **Assumptions** but not **Edge Cases** because the bot cannot reasonably detect or recover from it — the mitigation is on the HighBarV2 side of the contract.

### Feature Readiness notes

- Each FR (FR-001 through FR-021) is testable via either (a) direct inspection of a run-dir artefact (`stdout.log`, `frames.jsonl`, `unwired_commands.json`, `result.json`, `engine.infolog`) or (b) a deterministic xUnit assertion against a synthetic `MapGrid` / `GameState` — exactly the same two-layer pattern feature 024 used.
- The user stories are independently testable:
  - **US1 alone** could ship with US2/US3/US4 still on their 024 partial behaviour. The bot would consume `resolvePlan` for opening emission and fall through to the 024 partial's attack and defend paths. That is a valid intermediate state for bisecting a regression.
  - **US2 alone** could ship with US1 on the 024 opening path. That is also a valid intermediate state, though with less observable value.
  - **US3 alone** is the narrowest change — one filter application — and is effectively a bugfix on the 024 partial.
  - **US4 alone** is a warmup-flow change that enables but does not require US1/US2.
- **Priority rationale**: US1 and US2 are P1 because they are the literal spec-intent of 024's US5 and are both called out in 024's FR-029. US3 is P2 because the latent bug is real but only materialises on rungs other than NullAI. US4 is P2 because US1/US2 can technically land on a synthetic skeleton with degraded fidelity; US4 upgrades the fidelity.
- No ambiguous requirements. Every FR has a concrete test path listed above.
- Items marked incomplete would require spec updates before `/speckit.clarify` or `/speckit.plan` — this checklist reports **all items passing** on the first validation pass.
