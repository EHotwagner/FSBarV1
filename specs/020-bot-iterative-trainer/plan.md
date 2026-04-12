# Implementation Plan: Iterative AI Bot Trainer with Helper Library

**Branch**: `020-bot-iterative-trainer` | **Date**: 2026-04-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/020-bot-iterative-trainer/spec.md`

## Summary

Build a trainer that runs an F# bot script (`.fsx`) in a headless Beyond All Reason session against one of several opponent rungs, captures comprehensive structured logs into a self-contained run directory, and supports an operator-driven diagnose-improve-commit-push loop that grows a reusable **helper library** for future bot authors. The primary deliverable is not a winning bot — it is the helper library plus the runner infrastructure that makes writing a new bot easy.

Technical approach: extend `FSBar.Client`'s `EngineConfig` / `ScriptGenerator` with two new fields (`OpponentAIOptions: Map<string,string>` and `DeathMode: string`) and a fixed `FixedRNGSeed`/`MapName` that come from the trainer config (still on `EngineConfig`); add a scripted `bots/trainer/` tree containing the bot script, helper `.fsx` modules (logging, perception, tactics), a bash runner that materialises one run directory per iteration, a ladder JSON, an operator playbook, and a history log. Engine-installed BARb `AIOptions.lua` is patched out-of-tree via an in-repo installer script. All iteration work happens on the `020-bot-iterative-trainer` branch with commit-and-push on every change; no pull request.

## Technical Context

**Language/Version**: F# on .NET 10.0 (exclusive per Constitution §Engineering Constraints). Trainer bot scripts are `.fsx` loaded by `dotnet fsi`.
**Primary Dependencies**: existing in-repo `FSBar.Client`, `FSBar.Proto`, `FsGrpc 1.0.6`, `BarData` (NuGet from local store); `System.Text.Json` (BCL) for run artifacts; bash for the runner.
**Storage**: filesystem only — JSONL frame logs, JSON metadata/result files, plain-text stdout/infolog captures under `bots/runs/` (gitignored); in-repo `bots/trainer/` tree for bot + helpers + ladder + playbook. No database.
**Testing**: xUnit 2.9.x with `Microsoft.NET.Test.Sdk` under `tests/FSBar.Client.Tests` for the `EngineConfig`/`ScriptGenerator` extensions. Live-engine smoke via `bots/trainer/run.sh NullAI smoke` as a manual integration check; no automated live-engine test is added by this feature (existing `FSBar.LiveTests` remains unchanged).
**Target Platform**: Linux developer workstation with BAR installed at `~/.local/state/Beyond All Reason/engine/recoil_*`. The trainer is not portable to Windows/macOS within this feature (bash runner + socket-path assumptions).
**Project Type**: single-project extension of an existing library + new in-repo tool tree. No new dotnet project is added — the trainer bot runs entirely as FSI scripts referencing existing compiled DLLs.
**Performance Goals**: SC-001: single no-op-opponent run end-to-end in under two minutes of wall-clock. Frame log sampling every 30 frames (plus every event-bearing frame) to keep the JSONL file under a few MB per match.
**Constraints**: fixed map for the whole feature; fixed RNG seed for the whole feature (Clarifications Q4 & Q5); engine-installed files are patched only via the installer script (FR-030); no commits on `master`; commit-and-push on every change (FR-025 through FR-030).
**Scale/Scope**: single operator, single match at a time, no concurrency. Ladder holds a minimum of 2 rungs (no-op + one competitive) and at most ~5 rungs in practice. Helper library starts with 3 helpers (logging up-front, perception + tactics extracted) and grows from there.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

**I — Spec-First Delivery**: ✅ PASS. Feature spec exists at `specs/020-bot-iterative-trainer/spec.md` with five clarifications resolved. This plan links to it; tasks will map to its user stories. **Change classification: Tier 1** — the feature modifies the public API surface of `FSBar.Client` (adds two fields to `EngineConfig`), so the full artifact chain (spec → plan → `.fsi` update → surface-area baseline update → test evidence → docs) is required.

**II — Compiler-Enforced Structural Contracts**: ✅ PASS with obligations. Every modification to `EngineConfig.fs` MUST be mirrored in `EngineConfig.fsi`. The existing `ScriptGenerator.fs` is already module-public via its `.fsi`; adding internal branches doesn't change its signature, so no `.fsi` change is required there — but the fact MUST be verified by a compiler build. Surface-area baselines in `tests/FSBar.Client.Tests` for the `FSBar.Client` module MUST be updated in the same commit as the API change, and the baseline test MUST be run.

**III — Test Evidence Is Mandatory**: ✅ PASS with obligations. The `EngineConfig`/`ScriptGenerator` extensions MUST ship with xUnit tests that (a) fail on unmodified `ScriptGenerator` output when the new fields are set and (b) pass after the generator change. The bot script's own behaviour is validated manually via the live-engine smoke run (SC-001); this is documented in `quickstart.md`. The iteration loop itself is operator-driven, not an automated test, which is explicit in the spec — this is not a constitution violation because the iteration loop is a workflow, not a behaviour-changing subsystem.

**IV — Observability and Safe Failure Handling**: ✅ PASS. The run directory format (metadata, frame log, engine-captured logs, terminal result record) is the operational observability surface. The runner MUST fail fast on engine-launch errors with a terminal result record, and MUST clean up engine processes and socket files on every exit path (FR-006). Silent failures are prohibited — the playbook forces classification of every non-win.

**V — Scripting Accessibility**: ✅ PASS. `FSBar.Client` already exposes `scripts/prelude.fsx` and numbered example scripts. The new public API fields `OpponentAIOptions` and `DeathMode` are demonstrated by a new numbered example script added under `src/FSBar.Client/scripts/examples/` in task T012a, satisfying §V's obligation that example scripts cover core API scenarios end-to-end. The trainer's own bot + helper `.fsx` modules live under `bots/trainer/helpers/` and are an application of the same scripting model.

**Engineering Constraints** ✅ PASS:
- F# on .NET is the exclusive stack — trainer is bash + F# scripts + existing F# library; bash is permitted for the runner because it is not a "project governed by the constitution" and its role is purely filesystem orchestration (directory creation, process invocation, log copying). All domain logic is F#.
- Public `.fs` module gets `.fsi` update — addressed above (EngineConfig.fsi).
- Surface-area baselines updated — addressed above.
- No new NuGet dependencies are added. `System.Text.Json` is BCL.
- No gRPC or OpenAPI changes: the bot speaks the existing proxy protocol unchanged.

**Workflow and Quality Gates**: specify ✅ complete, plan = this file. Tasks to follow via `/speckit.tasks`. Analysis SHOULD run before implementation. Implementation phase-by-phase. `fsdoc` agent runs after implementation if public API changes land (it will — `EngineConfig`).

**Gate decision**: **PASS** — proceed to Phase 0. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/020-bot-iterative-trainer/
├── spec.md              # Completed feature specification with Clarifications
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── run-directory.md      # Run directory layout and per-file schemas
│   ├── ladder.schema.json    # JSON schema for ladder config
│   ├── meta.schema.json      # JSON schema for run meta
│   ├── result.schema.json    # JSON schema for terminal result record
│   └── frame.schema.json     # JSON schema for one JSONL frame log line
├── checklists/
│   └── requirements.md  # From /speckit.specify (already present)
└── tasks.md             # /speckit.tasks output (NOT created here)
```

### Source Code (repository root)

```text
src/FSBar.Client/
├── EngineConfig.fs           # MODIFY: add OpponentAIOptions, DeathMode
├── EngineConfig.fsi          # MODIFY: mirror new fields
└── ScriptGenerator.fs        # MODIFY: emit [AI1].[OPTIONS] block + configurable deathmode

tests/FSBar.Client.Tests/
├── ScriptGeneratorTests.fs   # MODIFY/CREATE: assertions for new fields
└── baselines/
    └── FSBar.Client.baseline # UPDATE via baseline regeneration workflow

bots/                         # NEW top-level directory
└── trainer/
    ├── bot.fsx               # the active bot under iteration (evolves each iteration)
    ├── helpers/
    │   ├── prelude.fsx       # #r references + opens + common aliases
    │   ├── log.fsx           # structured frame log + result writer
    │   ├── perception.fsx    # thin initially; grows by extraction
    │   └── tactics.fsx       # thin initially; holds TrainerLoop.run
    ├── engine-patches/
    │   ├── BARb_AIOptions.lua      # patched copy (easy/medium/hard uncommented)
    │   └── install-barb-profiles.sh  # idempotent installer
    ├── ladder.json           # opponent rungs
    ├── run.sh                # launches one iteration
    ├── PLAYBOOK.md           # operator decision tree (diagnose/classify/commit/push)
    ├── HISTORY.md            # iteration lineage, one line per iteration
    └── README.md             # operator-facing intro

bots/runs/                    # gitignored — trainer output, not committed
└── <timestamp>_<rung_slug>_<iter>/
    ├── meta.json
    ├── bot.fsx.snapshot
    ├── frames.jsonl
    ├── stdout.log
    ├── engine.stdout
    ├── engine.stderr
    ├── engine.infolog
    └── result.json

.gitignore                    # MODIFY: add bots/runs/
```

**Structure Decision**: Single-project extension. The trainer is a thin in-repo tool tree (`bots/trainer/`) that composes the existing `FSBar.Client` DLLs via FSI `#r` directives; no new dotnet project is created. The only compiled-code changes live in `src/FSBar.Client` and `tests/FSBar.Client.Tests`. This keeps the Constitution gates simple (one `.fsi` update, one baseline update, one test file touched) and keeps the bot iteration fast-path out of the compile loop entirely — each bot edit is just re-running `dotnet fsi bot.fsx`.

## Complexity Tracking

No constitution violations. Table left empty by design.
