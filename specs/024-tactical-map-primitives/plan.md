# Implementation Plan: Tactical Map Primitives

**Branch**: `024-tactical-map-primitives` | **Date**: 2026-04-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/024-tactical-map-primitives/spec.md`

## Summary

Feature 024 ships five new Tier-1 compiled modules on `FSBar.Client`:

1. **`SmfParser`** — reads BAR's native `.sd7`/`.smf` (Spring Map File) format from the local BAR installation and produces a `MapGrid`-compatible value. Enables unit + integration tests against real maps without a running engine.
2. **`Pathing`** — A* over `MapGrid.passability` with slope-weighted edge costs. Takes an explicit `ownStructures` input (friendly structures block paths per clarification Q3). Configurable wall-clock budget. Pure over inputs.
3. **`Chokepoints`** — distance-transform based detection of narrow corridors leading into a base radius. Returns stable-ID descriptors with position, width, and outward direction.
4. **`BasePlan`** — declarative structure-slot layout with `resolvePlan` enforcing terrain, clearance (additive margin over footprint edge per Q4), builder reach, and wall-in checks.
5. **`WallIn`** — pure connectivity predicate `wouldWallIn` sharing passability rules with `Pathing`, integrated into `resolvePlan`.

A sixth deliverable is the **US5 deep integration**: `bot_macro.fsx` is refactored in a single commit (per Q5) to drive its opening via `resolvePlan`, its attack routing via `findPath`, and its defend interrupt via `findChokepoints`, replacing the 023 hardcoded logic while preserving the iter 026 clean-win outcome on NullAI (`commander-death-win-after-upgrade`).

**Technical approach**: every new module is a Tier-1 F# library with curated `.fsi` and an updated surface-area baseline. Tests split into three layers: pure unit tests against synthetic `MapGrid` fixtures, integration tests against SMF-parsed real BAR maps (Avalanche 3.4 + opportunistic Red Rock Desert v2 + Comet Catcher Remake), and live-bot iteration tests via the inherited 023 trainer loop. Pathing uses plain A\* with a Manhattan heuristic (research R1); chokepoint detection uses distance-transform ridges (R2); `.sd7` extraction shells out to `bsdtar` (R3, already present on the dev image).

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: existing in-repo `FSBar.Client` (`MapGrid`, `MapQuery`, `Callbacks`, `GameState`), `BarData` (NuGet local feed, unit definitions), `xUnit 2.9.x` for tests. **No new NuGet dependencies.** `bsdtar` (system tool, present on dev image via libarchive) is shelled out to extract `.sd7` → `.smf` at SMF-parser runtime.
**Storage**: Filesystem only. Test fixtures split: synthetic `MapGrid` values constructed in-memory in `tests/FSBar.Client.Tests/SyntheticMapGrid.fs`; SMF fixtures are read on-demand from `~/.local/state/Beyond All Reason/maps/*.sd7` (no binaries committed to the repo). Bot run artifacts continue to land under `bots/runs/` (gitignored, unchanged from 020/023).
**Testing**: `xUnit 2.9.x` + `Microsoft.NET.Test.Sdk 17.x` (existing `FSBar.Client.Tests` project). Three test categories:
1. **Unit** (synthetic `MapGrid` — no external dependencies). SC-001 / SC-005.
2. **Integration** (SMF parser against BAR install, skipped if maps absent). SC-002 / SC-003 / SC-010.
3. **Live bot iteration** (via existing `bots/trainer/run.sh` against NullAI). SC-004 / SC-006 / SC-007.
**Target Platform**: Linux developer workstation with BAR installed at `~/.local/state/Beyond All Reason/` (inherited from 020/023). CI environments without BAR installed skip category-2 tests via the same pattern feature 003 (live-game-tests) established.
**Project Type**: Library extension. Five new `.fs`/`.fsi` modules inside the existing `FSBar.Client` project — no new dotnet projects. `bot_macro.fsx` is the integration consumer.
**Performance Goals**:
- `findPath` default wall-clock budget 50 ms, ≥95% completion on a representative workload (SC-001).
- `findChokepoints` deterministic under <200 ms on a 512×512 cell map (Avalanche 3.4 dimensions).
- `resolvePlan` for a 5-slot opening plan: <20 ms on a cached `MapGrid`.
- SMF parse of Avalanche 3.4 (~35 MB `.sd7`, 512×512 heightmap): <500 ms wall-clock including `bsdtar` shell-out, one-time per bot warmup.
- US5 refactored macro bot: preserves 023 iter 026 timing (Opening→Production at ~f=2750, Upgrade→Attack at ~f=16500, clean win at ~f=21000).
**Constraints**:
- Tier 1 per clarification Q1: all five new modules require curated `.fsi` signatures and surface-area baseline files.
- Slope map computed locally in the SMF parser MUST be dimensionally equivalent to the engine's `getSlopeMap` output (FR-026).
- `findPath` and `wouldWallIn` MUST share the same passability evaluator so a placement rejected by one is consistent with the other (FR-020).
- No `.fsmg` binary fixtures committed (per clarification Q2).
- `bot.fsx` (rush bot) MUST remain runnable at every commit (FR-030, inherited from 023 FR-022/023).
- Commit-and-push discipline inherited from 023 (FR-031).
**Scale/Scope**: Five new modules (~1500–2500 LOC total including `.fsi` + tests), 31 FRs, 10 SCs, 5 user stories. Integration with one consumer (`bot_macro.fsx`). Test corpus: 1 synthetic MapGrid factory, ~20 unit tests, ~8 SMF integration tests, 1 live bot iteration sequence.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change classification: Tier 1** (public API surface changes — five new `FSBar.Client` modules with new types and functions). All Tier 1 obligations apply: spec ✓, plan (this file), `.fsi` updates (Phase 1), surface-area baselines (Phase 1 / tasks), test evidence (unit + integration + live), fsdoc refresh at feature end.

**I — Spec-First Delivery**: ✅ PASS. Spec at `specs/024-tactical-map-primitives/spec.md` has 5 user stories, 31 FRs, 10 SCs, and 5 resolved clarifications. This plan traces each functional requirement to a module + task row. Implementation-only drift is forbidden by the test layer.

**II — Compiler-Enforced Structural Contracts**: ✅ PASS. Every new module ships with a curated `.fsi` before the `.fs` (Phase 1 plans the signature bodies). Surface-area baseline files land under `tests/FSBar.Client.Tests/Baselines/` (one per module) and are validated by the existing baseline test harness — any `.fs` symbol not in the `.fsi` is module-private by design. Phase 1 explicitly enumerates the `.fsi` contracts in `contracts/` so the task breakdown can order "`.fsi` first, then `.fs`".

**III — Test Evidence Is Mandatory**: ✅ PASS. Three-layer test strategy:
- Unit tests for pure logic (synthetic `MapGrid`, no filesystem, fast). Maps to FR-001/002/003/006/006a, FR-007..011, FR-012..018 (plan resolution over synthetic), FR-019..023.
- Integration tests for the SMF parser reading real BAR maps. Maps to FR-024..028, SC-002/003/010.
- Live bot iteration tests for US5. Maps to FR-029..031, SC-004/006/007.
Each user story has acceptance scenarios that translate directly to test cases. Every test MUST fail before its target code is written and pass after (TDD is operator discretion for the internal A\* details but is mandatory for the public API-level tests).

**IV — Observability and Safe Failure Handling**: ✅ PASS. Feature's observability surface:
- `findPath` returns `Result<Path, PathFailure>` — no silent empty lists.
- `findChokepoints` returns an explicit empty list when no chokepoints exist (FR-009), never a fabricated fallback.
- `resolvePlan` returns `ResolvedSlot` records including explicit `Failure` variants (FR-015) — slot rejection is visible, not swallowed.
- `wouldWallIn` returns `WallInResult` with `Fails { reason; unreachablePositions }` diagnostic payload (FR-021).
- SMF parser fails with descriptive errors on unsupported format / truncated data (FR-027).
- US5 integrated bot emits `[plan] resolved N slots`, `[attack] path waypoints=N`, `[defend] chokepoint pos=(X,Y) width=W`, `[wall-in-defect]` stdout traces — matching 023's telemetry discipline.

**V — Scripting Accessibility**: ✅ PASS. Every new module is loadable via the existing `scripts/prelude.fsx` (which already `#r`s `FSBar.Client.dll`). Phase 1 delivers four new numbered example scripts under `scripts/examples/`:
- `NN-pathing.fsx` — loads a map via SMF parser, computes a corner-to-corner path, prints waypoints.
- `NN-chokepoints.fsx` — SMF-parsed map, visualises chokepoints at a given base centre.
- `NN-plan.fsx` — resolves `defaultArmadaOpening` against Avalanche 3.4, prints slot positions.
- `NN-smf.fsx` — parses `~/.local/state/Beyond All Reason/maps/avalanche_3.4.sd7`, prints dimensions + heightmap range.

`NN` is the next available example number at the time of implementation (current range: see `scripts/examples/`).

**Engineering Constraints**: ✅ PASS.
- F# on .NET exclusive — yes. The `bsdtar` shell-out is a system-tool invocation, not another programming language in the project. Spec assumption documents that a managed 7-zip library can be swapped in during the plan phase without spec change (the decision to stay with `bsdtar` is R3 in `research.md`).
- Every public `.fs` module has a curated `.fsi` — enforced by Phase 1.
- Surface-area baselines — one per new module, listed in the task breakdown.
- No new NuGet dependencies — yes.
- Every library project packable via `dotnet pack` — `FSBar.Client` is already packable; new modules land inside it and inherit.
- gRPC services — N/A.
- OpenAPI specs — N/A.

**Workflow and Quality Gates**: specify ✅ (5 clarifications resolved), plan = this file. `.fsi` signature contracts for all five new modules are enumerated in `contracts/`. Tasks will be story-grouped. `/speckit.analyze` SHOULD be run before implementation. Implementation discipline inherits the 020/023 commit-and-push cadence. `fsdoc` is MANDATORY at feature end (this is a Tier 1 public API change).

**Gate decision: PASS.** No constitution violations; no Complexity Tracking entries required. Phase 0 research proceeds.

## Project Structure

### Documentation (this feature)

```text
specs/024-tactical-map-primitives/
├── spec.md              # Completed with Clarifications session 2026-04-13
├── plan.md              # This file
├── research.md          # Phase 0 output — pathing algorithm, chokepoint algorithm, 7-zip approach
├── data-model.md        # Phase 1 output — entity records for all 5 modules + integration records
├── quickstart.md        # Phase 1 output — operator walk-through for each module
├── contracts/
│   ├── pathing.md            # Pathing module .fsi contract (findPath, pathCost, types)
│   ├── chokepoints.md        # Chokepoints module .fsi contract
│   ├── base-plan.md          # BasePlan module .fsi contract
│   ├── wall-in.md            # WallIn module .fsi contract
│   └── smf-parser.md         # SmfParser module .fsi contract
├── checklists/
│   └── requirements.md       # Completed by /speckit.specify; updated by /speckit.clarify
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── FSBar.Client.fsproj                 # MODIFY: add 5 new .fs/.fsi pairs in canonical order
├── MapGrid.fs / .fsi                   # unchanged — pathing/chokepoints/wall-in consume MapGrid
├── MapQuery.fs / .fsi                  # unchanged
├── Callbacks.fs / .fsi                 # unchanged
├── GameState.fs / .fsi                 # unchanged
├── UnitDefCache.fs / .fsi              # unchanged
├── SmfParser.fsi                       # NEW: Tier 1 contract for SMF reader
├── SmfParser.fs                        # NEW: .sd7 → .smf → MapGrid, via bsdtar shell-out
├── Pathing.fsi                         # NEW: findPath / pathCost / Path / PathFailure
├── Pathing.fs                          # NEW: A* with slope-weighted edges + ownStructures mask
├── Chokepoints.fsi                     # NEW: findChokepoints / Chokepoint / ChokepointId
├── Chokepoints.fs                      # NEW: distance-transform ridges
├── BasePlan.fsi                        # NEW: BasePlan / PlanSlot / ResolvedSlot / resolvePlan
├── BasePlan.fs                         # NEW: plan resolution + clearance + reach + wall-in hookup
├── WallIn.fsi                          # NEW: wouldWallIn / WallInResult
└── WallIn.fs                           # NEW: connectivity check sharing Pathing's passability

src/FSBar.Client.Tests/                 # unchanged project structure; new files added
├── Baselines/
│   ├── SmfParser.baseline              # NEW: generated from SmfParser.fsi surface
│   ├── Pathing.baseline                # NEW
│   ├── Chokepoints.baseline            # NEW
│   ├── BasePlan.baseline               # NEW
│   └── WallIn.baseline                 # NEW
├── SurfaceAreaTests.fs                 # MODIFY: add 5 new baseline assertions
├── SmfParserTests.fs                   # NEW: unit + integration tests
├── PathingTests.fs                     # NEW: unit (synthetic) + integration (SMF-parsed)
├── ChokepointsTests.fs                 # NEW
├── BasePlanTests.fs                    # NEW
├── WallInTests.fs                      # NEW
└── SyntheticMapGrid.fs                 # NEW: helper factory for in-memory test fixtures

scripts/
├── prelude.fsx                         # unchanged
└── examples/
    ├── NN-pathing.fsx                  # NEW: FSI example — parse map, compute path
    ├── NN-chokepoints.fsx              # NEW: FSI example — visualise chokepoints
    ├── NN-plan.fsx                     # NEW: FSI example — resolve defaultArmadaOpening
    └── NN-smf.fsx                      # NEW: FSI example — parse a .sd7 and inspect

bots/trainer/
├── bot.fsx                             # unchanged — MUST remain runnable (FR-030)
├── bot_macro.fsx                       # MODIFY: US5 deep integration commit (single pass)
├── helpers/                            # unchanged — 5 feature-023 helpers still consumed
│   └── ...
├── ladder.json                         # unchanged
├── run.sh                              # unchanged
├── PLAYBOOK.md                         # MODIFY: add §13 tactical primitives quickstart
├── HISTORY.md                          # APPEND: one line per iteration on the 024 branch
└── README.md                           # MODIFY: document the four new primitives

bots/runs/                              # unchanged, gitignored
```

**Structure Decision**: Tier 1 library extension — five new `.fs`/`.fsi` pairs inside `FSBar.Client`, new test files inside `FSBar.Client.Tests`, four new FSI example scripts, one modified bot consumer (`bot_macro.fsx`). No new dotnet project. The feature lives almost entirely inside `src/FSBar.Client/` so existing consumers (`FSBar.Viz`, `FSBar.SyntheticData`, `bots/trainer/helpers/*.fsx`) can all consume the primitives without project-reference churn. The integration surface (`bot_macro.fsx`) is the single place where runtime bot behaviour changes; everything else is new surface area.

## Complexity Tracking

No constitution violations. Table left empty by design.

---

## Planning execution log

**Phase 0 (Outline & Research)**: completed — see [research.md](./research.md). Three research topics resolved: A\* with slope-weighted edges (R1), distance-transform chokepoint detection (R2), `bsdtar` shell-out for `.sd7` extraction (R3).

**Phase 1 (Design & Contracts)**: completed — see [data-model.md](./data-model.md), [contracts/](./contracts/), and [quickstart.md](./quickstart.md). Agent context updated via `.specify/scripts/bash/update-agent-context.sh claude`.

**Next**: run `/speckit.tasks` to generate `tasks.md` from this plan + contracts.
