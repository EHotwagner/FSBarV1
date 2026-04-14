# Implementation Plan: Macro Bot Primitive-Driven Command Path

**Branch**: `025-macro-primitive-driven` | **Date**: 2026-04-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/025-macro-primitive-driven/spec.md`

## Summary

Feature 024 shipped the five Tier-1 `FSBar.Client` tactical primitives (`Pathing`, `SmfParser`, `Chokepoints`, `WallIn`, `BasePlan`) and wired them into `bots/trainer/bot_macro.fsx` as **observability-only** telemetry. The bot prints `[plan] resolved N slots`, `[chokepoint] loaded N from cache`, `[attack] findPath skipped (no MapGrid)` — but the commands the bot actually emits still come from the 023 hardcoded helpers, not the primitives. Feature 024's US5 spec called this out as "deep single-pass refactor replacing hardcoded logic in one commit," but the refactor was deferred because the live-iteration attempts tripped a catch-up OOM when loading a real `MapGrid` at warmup.

Feature 025 lands the deferred US5 work as a single **behaviour-preserving refactor**:

1. **US1**: `BasePlan.resolvePlan defaultArmadaOpening` drives opening-phase `BuildCommand` emission. The 023 `helpers/opening_build.fsx` helper stays in-tree as an exception-fallback path only (FR-006 / FR-020).
2. **US2**: `Pathing.findPath` drives attack-phase routing with one `findPath` per attack launch, cached as `AttackPathCache`. Per-combat-unit waypoint emission uses a **new queued `MoveCommand` variant** on `FSBar.Client.Commands` — the first waypoint unqueued to replace any existing order, remaining waypoints queued with Spring's `SHIFT_KEY = 32u` option bit so they append to the unit's order queue rather than overwrite (clarification Q1 / FR-008 / FR-008a).
3. **US3**: Defend interrupt routes only units where `Attack_launch.isCombatDef` returns `true`, preventing workers and the commander from being marched to a chokepoint they cannot hold (FR-012 / FR-013).
4. **US4**: Warmup loads a **real `MapGrid`** (non-zero slopes, true heightmap) via an extended map-cache pipeline — hard-fail on cache-miss for the 025 target set (Avalanche 3.4), degrade to the 024 synthetic skeleton for maps outside the target set with a loud `[cache-miss] WARN` trace (clarification Q2 / FR-014). Warmup CPU budget stays under 100 ms (FR-015) and zero `Socket not writable` lines in `engine.infolog` during the warmup window (FR-017).
5. **US5 invariant**: The macro bot still wins cleanly on NullAI (`result.json.cause = "commander-death-win-after-upgrade"`) on the first integration iteration, within the 3-iter SC-007 budget. The rush bot (`bot.fsx`) stays runnable at every commit.

**Technical approach**: one tiny public API delta on `FSBar.Client.Commands` (new queued-MoveCommand variant — a Tier 1 change with matching `.fsi` and surface-area baseline update), plus a substantial edit to `bot_macro.fsx` that replaces the 023 opening-helper call site, the single-move attack launcher, and the defend-interrupt filter. The feature owns **one source file in `FSBar.Client`** and **one script in `bots/trainer/`**, plus a contract-level extension to the offline `scripts/examples/14-cache-map-analysis.fsx` cache writer to carry a compressed `MapGrid` blob alongside the chokepoint list (or, pending the R1 research outcome, a direct inline `SmfParser` re-parse at warmup).

The clarifications pinned four additional behavioural rules that shape the task breakdown:

- **Q3**: `AttackPathCache` is invalidated not only on Attack-phase end / target change, but also when the cached target-unit-id is absent from `GameState.Units` — `pickAttackTarget` and `findPath` re-run in the same tick.
- **Q4**: `resolvePlan` runs once per **tactics tick** (~30 game frames / ~1 sim-second), not per game frame, not event-driven. ~60 calls across a full opening phase at <1 ms each.
- **Q5**: If `findPath` returns `Status = Partial true` the bot issues the partial waypoints and **does not retry** with a larger budget — partial is final for the attack launch, and re-pathing only fires via Q3 target-death or the next Attack phase.

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot script is `.fsx` loaded by `dotnet fsi`.
**Primary Dependencies**: existing in-repo `FSBar.Client` (all 024 primitives: `Pathing`, `Chokepoints`, `BasePlan`, `WallIn`, `SmfParser`, plus pre-024 `Commands`, `Callbacks`, `GameState`, `MapGrid`, `UnitDefCache`, `Protocol`), `FSBar.Proto` (generated types incl. `Highbar.AICommand`), `BarData` (NuGet local feed, unit definitions), `xUnit 2.9.x` for Commands unit test. **No new NuGet dependencies.**
**Storage**: Filesystem only. Extended `bots/trainer/map-cache/<map>.json` carries a compressed MapGrid blob (heightmap + slope map + resource map + dimensions) alongside the existing chokepoint list — OR the bot inline re-parses `.sd7` via `SmfParser` at warmup, pending the Phase-0 R1 measurement decision. Bot run artifacts land under `bots/runs/` (gitignored, unchanged from 020/023/024).
**Testing**: three layers inherited from 024, narrowed to the 025 scope:
1. **Unit** — `FSBar.Client.Tests` gets one new test on the queued-MoveCommand variant validating that `Options` has both `INTERNAL_ORDER` (8u) and `SHIFT_KEY` (32u) bits set when the queue flag is passed. Maps to FR-008a.
2. **Integration** — the 024 `PathingTests` / `BasePlanTests` already cover the primitives; 025 adds no new integration tests because the consumer is `bot_macro.fsx` (an FSI script, not an `.fsproj`-buildable library).
3. **Live bot iteration** — `bash bots/trainer/run.sh NullAI <iter>` (macro + rush smokes). Maps to FR-018/019, SC-001..SC-005, SC-007. Iteration budget: 3 live attempts per 023 PLAYBOOK §2c "one fix per iter" rule (SC-007).
**Target Platform**: Linux developer workstation with BAR installed at `~/.local/state/Beyond All Reason/` and Avalanche 3.4 present under `maps/`. CI environments without BAR installed skip the live iteration layer — the unit test on the queued-MoveCommand variant is the only thing CI needs to run for this feature's Tier 1 change.
**Project Type**: Library delta + script consumer. One `.fs`/`.fsi` pair edited in `src/FSBar.Client/Commands` — adds a queued `MoveCommand` variant. One `.fsx` heavily rewritten in `bots/trainer/bot_macro.fsx` — the integration commit. No new dotnet projects; no new modules.
**Performance Goals**:
- Warmup total CPU budget: < 100 ms (FR-015). Covers real-`MapGrid` load (from extended cache or inline `SmfParser` re-parse per R1) + `resolvePlan` + chokepoint cache load.
- Engine-frame advance during warmup: ≤ 1000 game frames between `[trainer] BarClient connected` and `[trainer] entering main frame loop` (FR-016).
- `engine.infolog | grep "Socket not writable"` during warmup window: zero (FR-017).
- `findPath` per attack launch: default 50 ms wall-clock budget, single call (not 12 per unit). Cached as `AttackPathCache` and reused (FR-007/FR-009).
- `resolvePlan` per tactics tick during Opening: < 1 ms × ~60 ticks = < 60 ms cumulative over Opening phase (clarification Q4 / FR-001).
- Macro bot preserves the 023 iter 026 / 024 iter clean-win timing envelope on NullAI (FR-018, SC-001).
**Constraints**:
- **Tier 1**: modifying `FSBar.Client.Commands` is a public API surface change. `.fsi` update, surface-area baseline update, and a unit test are all mandatory per Constitution §II/§III.
- **FR-021**: The 024 primitive modules (`Pathing`, `SmfParser`, `Chokepoints`, `WallIn`, `BasePlan`) MUST NOT be edited by this feature. All 025 consumer logic lives in `bot_macro.fsx` (possibly with small additions to a new `helpers/primitive_driver.fsx` if two consumer call sites emerge organically per 020 FR-020 two-site extraction rule — the plan assumes this will NOT emerge in practice, so no new helper is in the default Structure Decision).
- **Known stale doc — do not "fix"**: the bit-layout comment at `proto/highbar/common.proto:18` is **known-stale** per research R2 and contradicts every other source — HighBarV2 `docs/protocol.md` line 210, the C++ bridge cast `s.options = (short)c->options;`, and the observably-working `src/FSBar.Client/Commands.fs` literal `INTERNAL_ORDER = 8u`. The authoritative wire values are `INTERNAL_ORDER = 8u` and `SHIFT_KEY = 32u`. Do NOT "correct" `Commands.fs` or the queued variant in T006/T007 to match the stale comment — doing so would break `FSBar.Client` command emission end-to-end. Fixing the comment itself is out of scope for 025 and flagged for a future doc-only cleanup via T057.
- **FR-006 / FR-020**: `bots/trainer/helpers/opening_build.fsx` MUST remain in-tree and compile. It is consumed only on the exception-fallback path from `BasePlan.resolvePlan`.
- **FR-019**: The rush bot (`bots/trainer/bot.fsx`) MUST be runnable at every commit on the 025 branch. Each commit on the branch that is rebased-ready runs a rush-smoke as part of the commit gate.
- Commit-and-push discipline inherited from 023/024: the integration commit for US1+US2+US3+US4 is a single atomic commit per the spec framing of "deep single-pass refactor" (FR-031 inherited).
**Scale/Scope**: one public API addition (one function, ~5 LOC on `.fsi`, ~15 LOC on `.fs`, 1 unit test), one FSI script rewrite (~150–250 LOC changed inside the existing 965-LOC `bot_macro.fsx`), one cache-script extension (~40 LOC in `scripts/examples/14-cache-map-analysis.fsx`), one surface-area baseline refresh. 21 FRs, 7 SCs, 5 user stories. Test corpus delta: 1 new unit test.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change classification**: **Tier 1** — the feature modifies the public API surface of `FSBar.Client.Commands` by adding a queued `MoveCommand` variant. All Tier 1 obligations apply: spec ✓, plan (this file), `.fsi` updates (Phase 1 contract), surface-area baseline update (Phase 1 / tasks), test evidence (Phase 1 unit test + live iteration), fsdoc refresh at feature end.

**I — Spec-First Delivery**: ✅ PASS. Spec at `specs/025-macro-primitive-driven/spec.md` has 5 user stories, 21 FRs (FR-001..FR-021 with FR-008a and FR-009a added during clarification), 7 SCs, and 5 clarifications resolved in the Clarifications session 2026-04-14. Each FR is traceable to the module + task row below, and each acceptance scenario maps onto a live-iteration assertion or (for FR-008a) a unit test.

**II — Compiler-Enforced Structural Contracts**: ✅ PASS. The queued `MoveCommand` variant lands as a curated signature on `src/FSBar.Client/Commands.fsi` before the implementation edit on `Commands.fs`. The existing surface-area baseline `tests/FSBar.Client.Tests/Baselines/Commands.baseline` (or equivalent; the task step enumerates it) is refreshed to capture the new symbol. Any drift between `.fsi` and `.fs` fails the build. `bot_macro.fsx` is an FSI script and therefore outside the compiler contract layer — the clean-win invariant (FR-018, SC-001) is its structural gate.

**III — Test Evidence Is Mandatory**: ✅ PASS. Two layers of verification:
- **Unit**: one new test in `src/FSBar.Client.Tests/CommandsTests.fs` asserting the queued variant's `Options` bitmask contains both `INTERNAL_ORDER = 8u` and `SHIFT_KEY = 32u`, and that the unqueued variant does NOT contain `SHIFT_KEY`. Maps to FR-008a. Mandatory under Constitution §III because this is a behaviour change on the public API.
- **Live**: the macro smoke run is the test for US1..US5. SC-001 is binary (clean-win cause string); SC-002/003 assert on stdout traces; SC-004 asserts on the unchanged rush smoke timing; SC-005 asserts on warmup engine-frame advance. Integration runs are executed live via `bots/trainer/run.sh`, artifacts land under `bots/runs/`, and each iteration's `result.json` + `stdout.log` + `engine.infolog` is the evidence record. Iteration budget is 3 per SC-007.
Each user story has acceptance scenarios that map onto either a unit test (US2 FR-008a) or a live-iteration assertion (all others). Mid-iteration TDD is operator discretion; the unit test on FR-008a MUST fail before the `.fs` edit and pass after.

**IV — Observability and Safe Failure Handling**: ✅ PASS. Feature's observability surface:
- `[plan] issuing BuildCommand <def> @ (x,z) from resolvePlan` per opening-slot emission (SC-002).
- `[plan] resolvePlan exception — falling back to 023 helper: <msg>` on the exception path (FR-005).
- `[attack] path waypoints=N cost=C status=Complete|Partial budget-exhausted` per attack launch (FR-011, SC-003).
- `[attack] findPath NoRoute — falling back to direct move` on the NoRoute fallback (FR-010).
- `[attack] target <id> absent from GameState — re-pathing` on Q3 target-death invalidation (FR-009a).
- `[defend] routing combat units only n=N` (FR-012) / `[defend] no combat units available — commander fallback` (FR-013).
- `[cache-miss] WARN: US1/US2 will behave like 024 partial — run 14-cache-map-analysis.fsx` on off-target-set cache miss (FR-014 degrade path).
- Warmup error on target-set cache miss is fatal-fast with an explicit message naming `scripts/examples/14-cache-map-analysis.fsx` (FR-014 hard-fail path).
Silent swallows are forbidden — every fallback path carries a distinct trace so post-run analysis can distinguish intended fallbacks from silent failures.

**V — Scripting Accessibility**: ✅ PASS. The queued `MoveCommand` variant is loadable via the existing `scripts/prelude.fsx` (which already `#r`s `FSBar.Client.dll` and opens `FSBar.Client.Commands`). No new numbered example script is strictly required — the variant is a one-line API extension, not a new module — but Phase 1 delivers one: `scripts/examples/NN-queued-move.fsx` loads a synthetic unit id list and emits a three-waypoint queued-move sequence, printing the `Options` bitmask for operator inspection. Justification: Constitution §V mandates example coverage of "core API scenarios end-to-end," and queue semantics are a load-bearing correctness detail that deserves a demonstrable FSI walkthrough.

**Engineering Constraints**: ✅ PASS.
- F# on .NET exclusive — yes. The one `.fs`/`.fsi` edit and the `.fsx` script rewrite are all F#.
- Every public `.fs` module has a curated `.fsi` — `Commands.fsi` is updated in lockstep.
- Surface-area baselines — `Commands.baseline` (or the equivalent pre-existing baseline file for `FSBar.Client`) is refreshed.
- No new NuGet dependencies — yes.
- `FSBar.Client` already packable via `dotnet pack`; no project-file changes.
- gRPC services — N/A (feature does not change `.proto`).
- OpenAPI specs — N/A.

**Workflow and Quality Gates**: specify ✅ (5 clarifications resolved), plan = this file. The `.fsi` signature contract for the queued `MoveCommand` variant is enumerated in `contracts/commands-queued-move.md`. Tasks will be story-grouped (US1/US2/US3/US4/US5) with verification and `.fsi`/baseline tasks. `/speckit.analyze` SHOULD be run before implementation. Implementation discipline follows 023/024 commit-and-push cadence. `fsdoc` is MANDATORY at feature end for the `FSBar.Client.Commands` surface change (this is a Tier 1 public API change).

**Gate decision: PASS.** No constitution violations; no Complexity Tracking entries required. Phase 0 research proceeds.

## Project Structure

### Documentation (this feature)

```text
specs/025-macro-primitive-driven/
├── spec.md              # Completed with Clarifications session 2026-04-14 (5 Qs resolved)
├── plan.md              # This file
├── research.md          # Phase 0 output — MapGrid cache vs re-parse, SHIFT_KEY bit value, findPath budget
├── data-model.md        # Phase 1 output — AttackPathCache, ResolveContext extension, MapGridCache extension
├── quickstart.md        # Phase 1 output — operator walkthrough: cache, smoke, trace verification
├── contracts/
│   ├── commands-queued-move.md     # .fsi delta for the queued MoveCommand variant (Tier 1 contract)
│   ├── bot-macro-integration.md    # bot_macro.fsx integration shape — function signatures, mutable state, call-site map
│   └── map-cache-format.md         # Extended JSON cache schema (or inline re-parse decision per R1)
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── FSBar.Client.fsproj                 # unchanged — Commands.fs already listed
├── Commands.fsi                        # MODIFY: add queued MoveCommand variant signature
├── Commands.fs                         # MODIFY: add queued variant OR'ing SHIFT_KEY | INTERNAL_ORDER
├── Pathing.fs / .fsi                   # unchanged (FR-021)
├── Chokepoints.fs / .fsi               # unchanged (FR-021)
├── BasePlan.fs / .fsi                  # unchanged (FR-021)
├── WallIn.fs / .fsi                    # unchanged (FR-021)
├── SmfParser.fs / .fsi                 # unchanged (FR-021) — consumed if R1 chooses inline re-parse
├── MapGrid.fs / .fsi                   # unchanged
├── Callbacks.fs / .fsi                 # unchanged
├── GameState.fs / .fsi                 # unchanged
└── (other existing modules)            # unchanged

src/FSBar.Client.Tests/                 # unchanged project structure; one file modified
├── CommandsTests.fs                    # MODIFY: add test for queued MoveCommand (FR-008a)
├── Baselines/
│   └── Commands.baseline               # MODIFY: refresh to include queued variant symbol
└── (other existing files)              # unchanged

scripts/
├── prelude.fsx                         # unchanged — already opens FSBar.Client.Commands
└── examples/
    ├── 14-cache-map-analysis.fsx       # MODIFY (conditional on R1): write compressed MapGrid blob alongside chokepoints
    └── NN-queued-move.fsx              # NEW: FSI example — three-waypoint queued move sequence

bots/trainer/
├── bot.fsx                             # unchanged — MUST remain runnable at every commit (FR-019)
├── bot_macro.fsx                       # MODIFY: US1+US2+US3+US4 integration commit (single atomic commit per spec framing)
├── helpers/
│   ├── opening_build.fsx               # unchanged — stays on exception-fallback path (FR-006, FR-020)
│   ├── attack_launch.fsx               # unchanged — isCombatDef is reused, module content not edited
│   └── (other helpers)                 # unchanged
├── map-cache/
│   └── avalanche_3.4.json              # REGENERATED (by operator) — extended schema per R1 if cache-extension chosen
├── ladder.json                         # unchanged
├── run.sh                              # unchanged
├── PLAYBOOK.md                         # APPEND: one line per iteration on the 025 branch
├── HISTORY.md                          # APPEND: per-iteration entries
└── README.md                           # MODIFY (optional): document the primitive-driven command path

bots/runs/                              # unchanged, gitignored
```

**Structure Decision**: Tier 1 library delta + script consumer rewrite. The feature's code footprint is deliberately narrow: one `.fs`/`.fsi` pair on `FSBar.Client.Commands` (the queued-MoveCommand variant — a Tier 1 API surface change), one FSI script rewrite on `bot_macro.fsx` (the integration itself), and — pending the R1 research outcome — one targeted extension to `scripts/examples/14-cache-map-analysis.fsx` to persist the MapGrid alongside chokepoints. Every other file listed above is unchanged by design. The 024 primitive modules are frozen for this feature (FR-021) and the `helpers/opening_build.fsx` helper stays in-tree on the exception-fallback path (FR-006/FR-020). No new dotnet project, no new module inside `FSBar.Client`, no new helper `.fsx` file unless a genuine second call site emerges (020 FR-020 two-site extraction rule).

## Complexity Tracking

No constitution violations. Table left empty by design.

---

## Planning execution log

**Phase 0 (Outline & Research)**: complete — see [research.md](./research.md). Three research topics resolved:
- **R1** — extend `bots/trainer/map-cache/<map>.json` with a base64-gzipped MapGrid blob (heightmap + slope + resource); bot loads from cache at warmup and never inline-parses `.sd7`. Rationale: `SmfParser.parseSd7` measured at 500ms–1.2s, 5–12× the FR-015 100 ms budget.
- **R2** — `SHIFT_KEY = 32u`, authoritative per HighBarV2 `docs/protocol.md` line 210 and the C++ bridge cast `s.options = (short)c->options`. The `proto/highbar/common.proto:18` comment is stale and contradicts every other source; out of scope to fix here.
- **R3** — single `findPath` per attack launch at 50 ms default budget fits comfortably on Avalanche 3.4's 512×512 map per 024 research.md R1's ~3k–13k cell expansion estimate. No per-tick budget bump needed; cached reuse per FR-009 + Q3 invalidation bounds cumulative cost.

**Phase 1 (Design & Contracts)**: complete — see [data-model.md](./data-model.md), [contracts/commands-queued-move.md](./contracts/commands-queued-move.md), [contracts/bot-macro-integration.md](./contracts/bot-macro-integration.md), [contracts/map-cache-format.md](./contracts/map-cache-format.md), and [quickstart.md](./quickstart.md). Agent context updated via `.specify/scripts/bash/update-agent-context.sh claude`.

**Post-design Constitution Check**: re-evaluated after Phase 1 artifacts written. **Still PASS.** No new violations surfaced by the design phase:

- **I Spec-First**: ✅ Every FR traces to a contract or a task-ready design decision. FR-008a maps to `contracts/commands-queued-move.md`; FR-001..FR-013 map to `contracts/bot-macro-integration.md`; FR-014/FR-015 map to `contracts/map-cache-format.md`; FR-018/FR-019 map to `quickstart.md` step 4 verification checklist.
- **II Compiler-Enforced Structural Contracts**: ✅ The Commands `.fsi` delta is one new symbol with full XML-doc comment; surface-area baseline refresh is enumerated as a Phase-2 task. No signature ambiguity.
- **III Test Evidence**: ✅ One new unit test (FR-008a) gated TDD-style before the `.fs` edit; live iteration verification covers every other FR via SC-001..SC-005.
- **IV Observability**: ✅ Every fallback and invalidation path carries a distinct `[plan] | [attack] | [defend] | [cache-miss]` trace. No silent swallows. `contracts/bot-macro-integration.md` enumerates each trace per FR.
- **V Scripting Accessibility**: ✅ Phase 1 delivers `scripts/examples/NN-queued-move.fsx` (Phase-2 task) to demonstrate the queued variant end-to-end in FSI.
- **Engineering Constraints**: ✅ Design stays in F#, keeps `FSBar.Client` packable, adds no NuGet dependencies, touches no `.proto` or OpenAPI. The stale `common.proto:18` comment is flagged for a future doc-only cleanup, not this feature.

**Gate decision (post-design): PASS.** Complexity Tracking remains empty.

**Next**: run `/speckit.tasks` to generate `tasks.md` from this plan + contracts.
