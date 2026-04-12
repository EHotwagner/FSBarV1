# Implementation Plan: Integrate HighBar Proxy Fixes and Re-run the Iterative Trainer Cycle

**Branch**: `021-rerun-trainer-highbar` | **Date**: 2026-04-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/021-rerun-trainer-highbar/spec.md`

## Summary

Pull HighBarV2 master (which now contains the squash-merged `029-fix-trainer-issues` work), rebuild and reinstall the proxy `libSkirmishAI.so`, then drive the existing feature 020 trainer loop a second time on top of the integrated proxy. The integration is mostly *deletion* on the FSBarV1 side: with the proxy now emitting Shutdown(GAME_OVER) for surviving AIs, returning real numbers from `Economy_get*`, defaulting per-command tracing OFF, and surfacing `rc=-2` for unwired protobuf oneof cases, three trainer-side workarounds become dead code (`botDeclaredVictory`, the "No active session" exception sniffer, and any `enum_move=42` constant). Two small additions are required: NaN guards on the economy reads (FR-003, with a stall-rule skip per Clarification Q2) and a hard 10-iteration per-rung budget with a budget-exhaustion mailbox (FR-016a, per Clarification Q4). The iteration loop itself reuses `bots/trainer/PLAYBOOK.md`, `ladder.json`, `run.sh`, and `HISTORY.md` as-is, against the same fixed map (`Avalanche 3.4`) and seed (`1`) from feature 020.

Technical approach: (1) integrate the proxy via the existing HighBarV2 `cmake --install` path; (2) add NaN-aware reads in `bots/trainer/helpers/tactics.fsx` and a stall-check helper that skips NaN fields; (3) delete the workarounds and verify with one smoke iteration on each rung; (4) walk the playbook for both rungs, allowing helper extraction whenever duplication appears across two iterations (SC-006 substance bar = 2 motivating iterations + 2 distinct call sites); (5) add an `AttackCommand` getUnitPos before/after probe somewhere in the loop to close the upstream Issue 1 follow-up; (6) file an outbound mailbox summarising the integration. No new dotnet projects, no new NuGet dependencies, no `.fsi` changes (the F# wrappers are unchanged per the inbound mailbox; only call-site behaviour shifts).

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`.
**Primary Dependencies**: existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); rebuilt HighBarV2 `libSkirmishAI.so` from sibling `../HighBarV2` checkout (post `029-fix-trainer-issues` squash-merge). `System.Text.Json` (BCL). Bash for the runner.
**Storage**: filesystem only — JSONL frame logs, JSON metadata/result files, plain-text stdout/infolog captures under `bots/runs/` (gitignored, unchanged from 020); in-repo `bots/trainer/` tree edited in place; `Mailbox/` for inbound and new outbound reports.
**Testing**: existing xUnit suites under `tests/FSBar.Client.Tests` (no new test project). One new pure-F# unit test for the NaN-skip stall comparison helper introduced under FR-015. Live-engine smoke is the integration test vehicle: `bots/trainer/run.sh NullAI smoke-021` and `bots/trainer/run.sh BARb/dev smoke-021` after the workaround deletions (this is how 020 verified its own work).
**Target Platform**: Linux developer workstation with BAR installed at `~/.local/state/Beyond All Reason/engine/recoil_*` and HighBarV2 cloned at `../HighBarV2` relative to this repo (per `container/Containerfile` line 77 reference).
**Project Type**: in-place edit of an existing in-repo tool tree (`bots/trainer/`) plus an FSI-time fix in `tactics.fsx`. No new projects.
**Performance Goals**: maintain feature 020's SC-001 (single no-op-opponent run end-to-end under two minutes); SC-004 sets a new perf-adjacent target (engine infolog file at least 80% smaller than the comparable 020 infolog due to default-off `verbose_commands`).
**Constraints**: same fixed map (`Avalanche 3.4`) and seed (`1`) as feature 020 (FR-011); commit-and-push on every change as in 020 §FR-025–FR-029; no merge-back to master; cross-repo defect → file inbound mailbox + halt (FR-021, Clarification Q1).
**Scale/Scope**: at most 10 iterations per rung (FR-016a, hard cap from Clarification Q4) × 2 minimum rungs = up to ~20 iteration cycles total. One operator, one match at a time. Helper library grows by at most 1–2 new substantive extractions during this feature.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**I — Spec-First Delivery**: ✅ PASS. Feature spec exists at `specs/021-rerun-trainer-highbar/spec.md` with 4 clarifications resolved. This plan links to it; tasks will map to its 4 user stories. **Change classification: Tier 2 (sub-tier of "Implementation-only changes with documented user value")** — this feature does not modify any FSBarV1 public API surface (`EngineConfig`, `ScriptGenerator`, `Callbacks`, `BarClient`, `Events` all unchanged), does not introduce new dependencies, does not change any inter-project contract on the FSBarV1 side, and does not alter observable behaviour covered by existing FSBar surface-area baselines. The behavioural change is entirely in the trainer's bot/helper FSI scripts under `bots/trainer/` and in one new pure-F# helper for NaN-aware stall comparison. **Tier 1 obligations therefore do not apply** — no `.fsi` updates, no surface-area baseline regeneration. Spec ↔ plan ↔ tasks traceability is the only quality-gate concern, and this plan establishes it.

**II — Compiler-Enforced Structural Contracts**: ✅ PASS. No public `.fs` modules are touched. The NaN-skip comparison helper is added to `bots/trainer/helpers/tactics.fsx` (an FSI script, not a compiled module), so no `.fsi` mirror is required. Surface-area baselines for `FSBar.Client` are not affected. The constitution's structural rules apply only to compiled `src/**/*.fs` modules.

**III — Test Evidence Is Mandatory**: ✅ PASS with obligations. The NaN-skip stall comparison helper introduced for FR-015 MUST ship with a small set of pure-F# unit tests (no live engine required) that demonstrate (a) NaN in `peak_metal` does not count as improvement *or* stagnation, (b) all-stagnation across non-NaN fields fires the stall, (c) any single non-NaN improvement clears the stall counter. These tests live in `tests/FSBar.Client.Tests/` (the only existing F# test project for FSBar). The remaining behavioural changes (workaround deletions, iteration loop re-run, Issue 1 probe) are validated by the live-engine smoke runs documented in `quickstart.md`; the iteration loop is operator-driven and was already explicitly considered in-scope as a workflow rather than an automated test in feature 020 — this feature inherits that classification.

**IV — Observability and Safe Failure Handling**: ✅ PASS. The integration *strengthens* observability: replacing `botDeclaredVictory` and the "No active session" sniffer with the proxy's canonical Shutdown(GAME_OVER) signal eliminates two silent inference paths and surfaces a real engine-side classification. NaN-as-not-available (FR-003) is an explicit "fail loud" pattern — no silent zero substitution. The 10-iteration budget exhaustion (FR-016a) writes a structured mailbox report rather than letting the loop grind. FR-021 cross-repo defect handling routes new HighBarV2 issues through the existing inbound-mailbox channel rather than swallowing them.

**V — Scripting Accessibility**: ✅ PASS. `FSBar.Client`'s public API surface and its `scripts/prelude.fsx` + numbered examples are unchanged by this feature. The trainer's own `bots/trainer/helpers/*.fsx` scripts (which compose the FSBar.Client DLLs via `#r`) remain runnable, and the playbook's quickstart instructions remain valid. No new public API examples are required.

**Engineering Constraints** ✅ PASS:
- F# on .NET is the exclusive stack — bash runner unchanged from 020 (filesystem orchestration only); all domain logic is F# in `tactics.fsx`/`bot.fsx`/`log.fsx`.
- No new compiled modules → no new `.fsi` files.
- No new NuGet dependencies. The HighBarV2 proxy is a *runtime* dependency consumed via the engine's AI plugin path, not a NuGet package.
- gRPC / OpenAPI: untouched. The wire protocol with the proxy is the same protobuf surface; the proxy's *behavioural* fixes don't alter the protobuf schema.
- Packable libraries: no library is repacked by this feature. No `dotnet pack` is required.

**Workflow and Quality Gates**: specify ✅ complete (`spec.md` + `requirements.md` checklist). Clarify ✅ complete (4 questions answered, recorded in spec §Clarifications). Plan = this file. Tasks to follow via `/speckit.tasks`. `fsdoc` agent run after implementation: **not required** because no public API surface changes (Tier 2 classification above).

**Gate decision**: **PASS** — proceed to Phase 0. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/021-rerun-trainer-highbar/
├── spec.md              # Feature spec with 4 resolved clarifications
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Phase 0 output (proxy install path, rc=-2 surfacing, Issue 1 probe placement)
├── data-model.md        # Phase 1 output (NaN-aware telemetry comparison, budget-exhaustion report shape)
├── quickstart.md        # Phase 1 output (operator how-to: rebuild proxy → smoke → loop)
├── contracts/
│   └── result-record.delta.md   # Document the *additive* changes to feature 020's result.schema.json (probe field)
├── checklists/
│   └── requirements.md  # From /speckit.specify (already present)
└── tasks.md             # /speckit.tasks output (NOT created here)
```

### Source Code (repository root)

```text
bots/trainer/                            # Existing — edited in place
├── bot.fsx                              # MODIFY: optional Issue 1 probe wiring; otherwise iteration-driven edits
├── helpers/
│   ├── prelude.fsx                      # UNCHANGED
│   ├── log.fsx                          # MODIFY: result writer accepts NaN-as-null for peak_metal/peak_energy in result.json (FR-003 propagation)
│   ├── perception.fsx                   # MODIFY ON EXTRACTION: gain real code from iteration duplication (SC-006)
│   └── tactics.fsx                      # MODIFY: delete botDeclaredVictory + No-active-session sniffer; add NaN guards on peakMetal/peakEnergy reads; honour Shutdown(GAME_OVER) as canonical victory; thread proxy log scan for rc=-2 into terminal result; (optionally) AttackCommand getUnitPos probe wiring
├── engine-patches/                      # UNCHANGED — installer reused as-is
├── ladder.json                          # UNCHANGED — same map, same seed, same rungs
├── run.sh                               # MODIFY: branch-name guard updated to 021-rerun-trainer-highbar; post-match step parses engine.infolog for rc=-2 hits and writes a side file `unwired_commands.json` into the run dir; stub `peak_metal: 0` literals in error/interrupted paths are left as-is (these are stub fallbacks for missing-result.json error states, not real telemetry — FR-008 is satisfied trivially because no real-path zero placeholders exist)
├── PLAYBOOK.md                          # MODIFY: add a §10 entry covering the new 10-iteration hard cap (FR-016a) and the cross-repo-defect route (FR-021)
├── HISTORY.md                           # APPEND-ONLY: per-iteration lines for this feature
└── README.md                            # MODIFY: small note pointing at this feature's outbound mailbox

tests/FSBar.Client.Tests/                # Existing — minimal addition
└── TrainerStallTests.fs                 # NEW: pure-F# unit tests for the NaN-aware stall comparison helper

Mailbox/
├── 2026-04-12_to_FSBarV1_proxy_fixes_complete.md   # INBOUND, unchanged
└── 2026-04-XX_from_FSBarV1_integration_complete.md # NEW (FR-019) outbound report after US1+US2 finish
```

**Structure Decision**: In-place edit of the existing `bots/trainer/` tree from feature 020. No new dotnet projects. The single piece of compiled-test code added (`TrainerStallTests.fs`) lives in the existing `FSBar.Client.Tests` project because that's where the only F# xUnit infrastructure already lives in the repo, and because the NaN-skip comparison helper is small enough to be defined inline in either the test file or a sibling helper module without justifying its own project. The trainer's helper script tree (`bots/trainer/helpers/*.fsx`) is the natural home for the runtime instance of the helper used by `tactics.fsx`. This structure keeps Tier 1 obligations off the table and keeps the bot iteration fast-path (`dotnet fsi bot.fsx`) outside the compile loop, exactly as in feature 020.

## Complexity Tracking

No constitution violations. Table left empty by design.
