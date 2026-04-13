# Implementation Plan: Builder-Economy Bot via the Iterative Trainer

**Branch**: `023-trainer-builder-economy` | **Date**: 2026-04-13 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/023-trainer-builder-economy/spec.md`

## Summary

Drive the existing **Iterative Trainer** (features 020/021/022) toward a new bot archetype: a macro builder-economy bot whose commander lays down an opening base (≥2 metal extractors, ≥2 energy structures, ≥1 factory), whose factory sustains a production stream of constructors and combat units, which reaches a tier-2 / upgrade milestone, and which only then commits a coordinated attack on the enemy commander for the canonical commander-death win. The new bot lives alongside the existing rush bot as a **second `.fsx` file in `bots/trainer/`**; both bots must remain runnable on every commit. The primary deliverable is the **helper library growth** — five new helpers (opening-build order, production-queue keeper, idle-constructor dispatcher, upgrade-gate, army-composition / attack-launch) extracted under the feature-020 two-site / two-iteration extraction rule, each with at least one bot consumer in-tree and a corresponding section in the operator playbook.

Technical approach: **no changes to any compiled F# project**. `FSBar.Client` already exposes the primitives the macro archetype needs (`Commands.BuildCommand`, `Callbacks.getMetalSpots`, `Callbacks.getBuildOptions`, `UnitDefCache`, `EngineConfig.OpponentAIOptions`, `EngineConfig.DeathMode`, `EngineConfig.GameSpeed`). This feature lives entirely in:

1. A new `bots/trainer/bot_macro.fsx` that wires the four-phase state machine (opening → production → upgrade → attack) using helper modules.
2. New/grown `bots/trainer/helpers/*.fsx` modules — the five helpers from FR-021 plus additions to `perception.fsx` / `tactics.fsx` when warranted under the extraction rule.
3. An updated `bots/trainer/run.sh` accepting a `BOT_SCRIPT` selector so the same runner can launch either `bot.fsx` (existing rush bot) or `bot_macro.fsx` (new macro bot) without duplication, plus a branch-guard update.
4. Updates to `bots/trainer/PLAYBOOK.md`, `HISTORY.md`, and `ladder.json` where operator iteration surfaces the need.
5. A new run-log phase-transition record (written by the bot via `log.fsx`) so iterations can diagnose which phase the bot stalled in without re-reading `frames.jsonl` by hand.

All match-execution infrastructure — run directory layout, metadata/frame/result schemas, engine launch path, cleanup guarantees, iteration history, stall detector, commit-and-push discipline — is inherited **unchanged** from features 020/021/022 per FR-017. No parallel trainer, no divergent run schema, no pull requests.

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`.
**Primary Dependencies**: existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); `System.Text.Json` (BCL) for run artifacts; bash for the runner. **No new dependencies.**
**Storage**: filesystem only — JSONL frame logs, JSON metadata/result/phase-transition files, plain-text stdout/infolog captures under `bots/runs/` (gitignored, unchanged from 020/021/022); in-repo `bots/trainer/` tree edited in place. No database.
**Testing**: No new xUnit projects. The bot's behaviour is validated through the **iteration loop itself** — each match produces a run directory that is inspected against the user-story acceptance scenarios. The existing `tests/FSBar.Client.Tests` baselines remain unchanged (no public API surface changes). A parser-style unit test MAY be added under `bots/trainer/tests/` if a helper's pure-F# logic (e.g. opening-build ordering) proves easy to test offline, but it is not required.
**Target Platform**: Linux developer workstation with BAR installed at `~/.local/state/Beyond All Reason/engine/recoil_*`. Inherits 020's platform assumptions unchanged.
**Project Type**: Scripting-tree extension. **No new dotnet project, no edits to any `.fs`/`.fsi` file.** All work is in `.fsx` scripts, bash, JSON, and Markdown under `bots/trainer/`.
**Performance Goals**: Inherits SC-001 from 020 (no-op run under ~2 minutes wall-clock). Macro matches may run longer than rush matches against competitive rungs because the bot's strategy is to survive long enough to finish the opening and upgrade — the existing `max_frames=36000` on `BARb/dev` is assumed sufficient; raising it is operator discretion if iteration surfaces the need.
**Constraints**: Fixed map (`Avalanche 3.4`) and fixed RNG seed (`1`) reused from 020 per FR-018; changing either is explicitly out of scope. Phase transitions MUST be internal-predicate-driven per FR-016a with the single enemy-in-base defend override per FR-016b. Every helper extraction requires a dedicated commit under FR-021 and the 020 FR-025..FR-029 commit-and-push discipline. Both `bot.fsx` and `bot_macro.fsx` MUST remain runnable on every commit on this branch per FR-022/FR-023.
**Scale/Scope**: Single operator, single match at a time, no concurrency. Two bots in-tree at end of feature. Helper library grows by at least five new helpers (FR-021). Ladder holds the same two rungs as 020/021/022 unless iteration adds one.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**Change classification: Tier 2 (non-API).** This feature does **not** modify any public API surface (`EngineConfig`, `ScriptGenerator`, `Commands`, `Callbacks`, `BarClient`), introduce dependencies, change inter-project contracts (`.proto`), or alter observable behaviour of any spec-covered subsystem. It adds F# scripts and operator documentation that *consume* the existing surface. Therefore the full artifact chain (spec → plan → `.fsi` updates → surface-area baselines → test evidence) collapses to the Tier 2 subset: spec + plan + operator-facing evidence (run directories + HISTORY lines + playbook update).

**I — Spec-First Delivery**: ✅ PASS. Feature spec exists at `specs/023-trainer-builder-economy/spec.md` with five clarifications resolved (phase-transition trigger, army threshold, bot-file location, enemy-awareness surface, upgrade-deadline fallback). This plan links to it; tasks will map to its user stories.

**II — Compiler-Enforced Structural Contracts**: ✅ PASS (vacuously). No `.fs` or `.fsi` files are touched. All `bots/trainer/*.fsx` scripts remain outside the `.fsi` signature regime by design (they are consumers, not library modules). Surface-area baselines in `tests/FSBar.Client.Tests` require no update because no public API changes. If an iteration surfaces a missing primitive on `FSBar.Client` (e.g. a build command that does not render correctly), that iteration is classified as `repo-bug` per the PLAYBOOK and handled as a Tier 1 sub-change in the same commit, per 020 FR-015 — it is not assumed up-front and is not in Phase 0 of this plan.

**III — Test Evidence Is Mandatory**: ✅ PASS. The iteration loop *is* the test evidence: every user-story acceptance scenario maps to an inspectable run directory, and SC-004/SC-010 define the completion bar as a specific run-directory telemetry signature (commander-death win after upgrade + ≥12 combat units on the no-op rung). This inherits the 020 precedent that the iteration loop is a workflow, not a subsystem, and is therefore not required to be automated xUnit. A small offline unit test may be added under `bots/trainer/tests/` if a helper's logic benefits from it, but it is not mandatory.

**IV — Observability and Safe Failure Handling**: ✅ PASS. The run directory format (metadata, frame log, engine-captured logs, terminal result record, unwired commands, and — new in this feature — the phase-transition record) is the operational observability surface. The macro bot MUST write one phase-transition entry at each phase boundary (FR-004, FR-011, FR-014) and MUST record stall reasons explicitly (FR-012). Silent failure is prohibited — the enemy-in-base defend override (FR-016b) MUST log the interruption and resumption, not swallow the phase transition.

**V — Scripting Accessibility**: ✅ PASS (with noted observation). The new macro bot *is* an FSI script and reuses `scripts/prelude.fsx` transitively through `bots/trainer/helpers/prelude.fsx`. No new example script under `src/FSBar.Client/scripts/examples/` is required because no public API fields are added. The five new helper modules (`.fsx` files) are themselves the scripting-accessibility story for the macro archetype — they demonstrate how to compose the existing `FSBar.Client` API into a phase-driven bot.

**Engineering Constraints** ✅ PASS:
- F# on .NET is the exclusive stack — this feature is `.fsx` scripts + bash runner edits + JSON/Markdown. Bash is permitted for runner edits because it is not "a project governed by the constitution" (same latitude granted in 020 plan).
- Public `.fs` module `.fsi` updates — not required, nothing compiled changes.
- Surface-area baselines — no update required, no public API surface changes.
- No new NuGet dependencies — explicit.
- No gRPC or OpenAPI changes — the macro bot speaks the existing proxy protocol unchanged.
- Every library project remains packable — unaffected by this feature.

**Workflow and Quality Gates**: specify ✅ complete, plan = this file. `/speckit.tasks` to follow. `/speckit.analyze` SHOULD be run before implementation. Implementation is **iteration-driven** (not phase-driven in the conventional sense): each iteration on this branch is one diagnose → improve → commit → push cycle, per the 020 PLAYBOOK. `fsdoc` is **not** invoked at feature end because no public API or `.fsi` changes land.

**Gate decision**: **PASS** — proceed to Phase 0. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/023-trainer-builder-economy/
├── spec.md              # Completed feature specification with Clarifications
├── plan.md              # This file
├── research.md          # Phase 0 output (/speckit.plan)
├── data-model.md        # Phase 1 output (/speckit.plan)
├── quickstart.md        # Phase 1 output (/speckit.plan)
├── contracts/
│   ├── helpers.md                  # Interfaces of the five new helper modules
│   └── phase-transition-record.md  # New run-log phase-transition entry format
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
bots/trainer/                         # Existing in-repo tool tree from 020/021/022
├── bot.fsx                           # UNCHANGED: existing rush bot (must remain runnable)
├── bot_macro.fsx                     # NEW: the macro builder-economy bot (this feature)
├── helpers/
│   ├── prelude.fsx                   # UNCHANGED: #r references shared by both bots
│   ├── log.fsx                       # MODIFY: add phase-transition record writer + record type
│   ├── perception.fsx                # MAY GROW: extracted macro perception (base radius,
│   │                                 #           metal-spot candidates, enemy-in-base query)
│   ├── tactics.fsx                   # MAY GROW: trainerLoopRun unchanged; tactics helpers
│   │                                 #           for phase interruption/resume under FR-016b
│   ├── opening_build.fsx             # NEW (FR-021): opening-build order helper
│   ├── production_queue.fsx          # NEW (FR-021): factory production-queue keeper
│   ├── constructor_dispatch.fsx      # NEW (FR-021): idle-constructor dispatcher
│   ├── upgrade_gate.fsx              # NEW (FR-021): upgrade-gate predicates
│   └── attack_launch.fsx             # NEW (FR-021): army-composition / attack-launch helper
├── engine-patches/                   # UNCHANGED from 020/021/022
├── lib/
│   └── parse_unwired.sh              # UNCHANGED: reused by run.sh
├── tests/
│   ├── parser_unwired_test.sh        # UNCHANGED
│   └── (optional) helpers_test.fsx   # OPTIONAL: offline unit tests for pure helper logic
├── ladder.json                       # UNCHANGED unless an iteration needs a new rung
├── run.sh                            # MODIFY: BOT_SCRIPT selector; update branch-guard to
│                                     #         023-trainer-builder-economy
├── PLAYBOOK.md                       # MODIFY: new §12 "Macro archetype" + phase-classification
│                                     #         extensions to the decision tree
├── HISTORY.md                        # APPEND: one line per iteration on this branch
└── README.md                         # MODIFY: mention bot_macro.fsx as second in-tree bot

bots/runs/                            # UNCHANGED, gitignored — trainer output
└── <timestamp>_<rung_slug>_<iter>/
    ├── meta.json                     # unchanged schema
    ├── bot.fsx.snapshot              # now reflects whichever bot was launched
    ├── frames.jsonl                  # unchanged schema
    ├── phase_transitions.jsonl       # NEW: one line per phase boundary (macro bot only)
    ├── stdout.log
    ├── engine.stdout
    ├── engine.stderr
    ├── engine.infolog
    ├── unwired_commands.json         # unchanged (from 022)
    └── result.json                   # unchanged schema
```

**Structure Decision**: Scripting-tree extension only. The feature adds one new `.fsx` bot file, five new helper `.fsx` modules, one new run-log artifact (`phase_transitions.jsonl`), and operator documentation edits. No new dotnet project, no `.fs`/`.fsi` edits, no new dependencies. This keeps the Constitution gates at their Tier 2 minimum and keeps the iteration fast-path entirely outside the compile loop — each iteration is still "edit `.fsx`, run `dotnet fsi`, inspect run directory".

The macro bot lives alongside the existing rush bot (not instead of it) per the Session 2026-04-13 clarification. FR-022 / FR-023 obligate us to keep both bots runnable on every commit; this structure makes that trivial because the two bots share all helpers and the runner distinguishes them only via the `BOT_SCRIPT` environment variable.

## Complexity Tracking

No constitution violations. Table left empty by design.
