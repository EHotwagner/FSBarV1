# Phase 0 Research — Bot Iterative Trainer

**Feature**: 020-bot-iterative-trainer
**Date**: 2026-04-12

The spec's Clarifications section already resolves the five high-impact unknowns (win condition, stall detection, ladder terminal rung, map fixing, seed fixing). Technical Context in `plan.md` declares no additional `NEEDS CLARIFICATION` markers. This document captures the design decisions the plan took based on codebase exploration, with rationale and rejected alternatives, so implementers don't have to re-derive them.

---

## Decision 1 — Extend `EngineConfig` with `OpponentAIOptions` + `DeathMode`

**Decision**: Add two fields to `src/FSBar.Client/EngineConfig.fs`:
- `OpponentAIOptions: Map<string, string>` (default `Map.empty`) — rendered by `ScriptGenerator` as an `[AI1].[OPTIONS]` block when non-empty.
- `DeathMode: string` (default `"com"`) — rendered by `ScriptGenerator` as `deathmode=<value>` replacing the hard-coded `neverend`.

**Rationale**: The current `ScriptGenerator.fs` (lines 73-79) emits `[AI1]` with only `Name`, `Team`, `ShortName`, `Host` — no options block. BARb's `AIOptions.lua` supports keys (`profile`, `cheating`, `disabledunits`, `game_config`, …) that significantly alter opponent behaviour, and the spec's ladder needs `profile` to switch between `easy`/`medium`/`hard`. Passing these as a typed `Map<string,string>` keeps the generator rendering generic while letting the ladder configuration be data-only.

The `DeathMode` change is strictly required by Clarification Q1 (commander-kill win) and FR-011. The engine currently gets `deathmode=neverend` unconditionally; making it a field lets the trainer set `com` (commander-death) while leaving the existing default unchanged for any pre-existing callers that omit the field.

**Alternatives considered**:
- *Hard-code `deathmode=com` for the trainer only.* Rejected — that would fork the engine config path and diverge from existing callers, violating the spec-first principle that public API changes are captured once.
- *Add a dedicated `TrainerEngineConfig` record.* Rejected — duplicative. `EngineConfig` is already the single source of truth for engine launches.
- *Pass options via environment variables to BARb.* Rejected — BARb does not read env vars; it reads `[AI1].[OPTIONS]` from the start script.

---

## Decision 2 — `.fsi` surface change minimalism

**Decision**: Update `src/FSBar.Client/EngineConfig.fsi` to add the two new fields (record fields are part of the surface). `ScriptGenerator.fsi` stays unchanged because `generate` already takes an `EngineConfig` and its signature doesn't change — the new branches are internal.

**Rationale**: Constitution §II requires that every public `.fs` module has a corresponding `.fsi`, and that any symbol omitted from the `.fsi` becomes module-private. Record-field additions to a public record are an API change and therefore MUST land in the `.fsi`. The surface-area baseline for `FSBar.Client` will also need regenerating in the same commit.

**Alternatives considered**:
- *Add a new wrapper record that extends `EngineConfig`.* Rejected — adds needless complexity, breaks ergonomic "copy-and-set" usage in FSI scripts.
- *Leave `.fsi` as-is and expose the field via an accessor function.* Rejected — inconsistent with the rest of `EngineConfig`, which is a plain record, and violates the principle of structural contract clarity.

---

## Decision 3 — BARb difficulty via in-repo patch + installer

**Decision**: Store a patched copy of BARb's `AIOptions.lua` at `bots/trainer/engine-patches/BARb_AIOptions.lua` with the `easy`/`medium`/`hard` profile entries uncommented, and a bash installer `bots/trainer/engine-patches/install-barb-profiles.sh` that copies the file into `~/.local/state/Beyond All Reason/engine/recoil_*/AI/Skirmish/BARb/stable/AIOptions.lua` idempotently (detects if the patch is already applied).

**Rationale**: The installed `AIOptions.lua` only exposes `profile=dev`. Without the patch, the `[AI1].[OPTIONS]` block from Decision 1 cannot use `profile=easy|medium|hard` — BARb would reject the value at startup. FR-030 forbids committing engine-installed file modifications directly; the installer pattern (source-of-truth in-repo, installer applies) is the compliant workaround.

**Alternatives considered**:
- *Fork BARb and include a local copy in the repo.* Rejected — BARb is non-trivial, dozens of `.json`/`.as`/`.lua` files, and licensing/attribution adds friction.
- *Inject the difficulty via `MODOPTIONS` instead.* Rejected — BARb reads `AIOptions.lua` at AI startup, not `MODOPTIONS`; wrong layer.
- *Skip BARb difficulty escalation entirely.* Rejected — spec US3 requires at least one competitive rung (clarified via Q3), and without `profile` all BARb runs would be identical difficulty.

---

## Decision 4 — Bot script runs via FSI, no new dotnet project

**Decision**: The bot, helpers, prelude, and `TrainerLoop.run` all live as `.fsx` files under `bots/trainer/` and are executed via `dotnet fsi bot.fsx`. They `#r` the already-compiled `FSBar.Client.dll` from `src/FSBar.Client/bin/Debug/net10.0/`. No new `.fsproj` or project added.

**Rationale**: This matches the existing pattern (`tests/FSBar.LiveTests/BarbRushTest.fsx`, `docs/examples.fsx`, `scripts/examples/*.fsx`) and honours Constitution §V (Scripting Accessibility) by building on the existing FSI-first story. It also keeps bot iterations fast: editing `bot.fsx` is a no-compile change — just rerun the script. Compiling a new project on every iteration would slow the loop by tens of seconds and invite DLL-lock issues that the MCP/FSI workflow has already seen in this repo.

**Alternatives considered**:
- *Create a `bots/trainer/Trainer.fsproj` compiled binary.* Rejected — slower iteration, more rebuild pressure, no meaningful gain.
- *Inline the bot inside the existing `FSBar.LiveTests` project.* Rejected — would require xUnit wrapping, entangles iteration logs with test reports, and places the bot inside a project that has different test-runner semantics.
- *Use the FSI MCP server directly.* Plausible but deferred — the `fsi-server` MCP tool is an interactive REPL, not batch. The runner needs a repeatable scripted invocation, which `dotnet fsi bot.fsx` gives cleanly.

---

## Decision 5 — Runner in bash, not F#

**Decision**: `bots/trainer/run.sh` is a short bash script (~100 lines) that: takes `<rung_name>` and `<iter_id>` args, parses the ladder, exports env vars the bot reads (`BOT_OPPONENT`, `BOT_OPPONENT_OPTIONS`, `HIGHBAR_BOT_RUN_DIR`), creates the run directory, snapshots `bot.fsx`, writes `meta.json`, invokes `dotnet fsi bot.fsx`, copies engine logs from the session directory, and prints a one-line summary.

**Rationale**: The runner is entirely filesystem orchestration. Bash is the simplest tool for this job and is already assumed present on the target platform (Linux dev workstation, per Technical Context). Using F# for the runner would require yet another FSI script or a new project, and would make the "plain-file" debugging story worse (bash is easy to read and modify in emergencies). All actual domain logic — bot behaviour, engine config, game protocol — stays in F#.

**Constitution check**: Constitution §Engineering Constraints says "F# on .NET is the exclusive stack" within projects governed by the constitution. The `bots/trainer/` tree is a tool tree, not a project, and its runner is orchestration, not a governed subsystem. This is analogous to the existing `scripts/check-deps.sh` and other shell tooling in the repo. **No violation.**

**Alternatives considered**:
- *Pure F# runner as another `.fsx`.* Plausible; the cost is re-implementing bash features (subprocess invocation, env var propagation, file copy) in F# at a time when the feature's main value is elsewhere. Deferred as a possible follow-up if the bash runner becomes a pain point.
- *.NET console app runner.* Rejected — needs a new `.fsproj`, new baseline, and its own build loop. Over-engineered for filesystem glue.

---

## Decision 6 — Logging format: JSONL frames + JSON meta/result

**Decision**: Per-run artifacts:
- `meta.json` — single JSON object with immutable run facts (rung, opponent options, engine version, git SHA, seed, map, frame limit, start timestamp).
- `frames.jsonl` — one JSON object per sampled frame (frame number, event counts by type, unit count, enemy count, metal, energy, commands issued in that frame). Sampled every 30 frames by default, plus every frame on which a notable event occurs.
- `result.json` — single JSON object with outcome, frame count, cause, and telemetry block (commands total, units built, units lost, peak metal, peak energy, enemies killed).
- `stdout.log` — raw bot `printfn` output.
- `engine.stdout`, `engine.stderr`, `engine.infolog` — verbatim copies from `EngineLauncher.getSessionDir(config)`.

**Rationale**: JSONL is append-friendly (log as you go, no re-reading), line-wise parseable for diagnosis (`jq`, `grep`, `head`), and bounded in size via sampling. JSON for fixed-shape objects (`meta`, `result`) is simple to write and validate. Using `System.Text.Json` (BCL) avoids a new dependency. Plain-text engine logs stay plain-text because that is the engine's native format.

**Alternatives considered**:
- *Single structured log (one JSON object per run with everything inside).* Rejected — unbounded growth, harder to stream-read, can't flush incrementally for crash-resilience.
- *SQLite DB per run.* Rejected — overkill, requires a new dep, hurts debuggability.
- *Protobuf/MessagePack logs.* Rejected — not human-readable without tooling; debuggability is a first-class goal here.

---

## Decision 7 — Helper bootstrapping exception

**Decision**: The spec (FR-020) forbids creating helpers preemptively — helpers must emerge from at least two iterations of duplicated logic. An explicit exception is carved out for the logging helper and the runner infrastructure: they exist from day one because US1 depends on them.

**Rationale**: US1 ("Run one full match and capture everything") is P1 and is the infrastructure floor. It cannot be tested without a working logging helper and a working runner. Waiting for "two iterations" before writing them would mean US1 has no way to pass its independent test. The spec explicitly notes this exception; this plan formalises it.

Perception and tactics helpers do NOT get the exception — they start empty (files exist as placeholders with `module` declarations only) and are grown by extraction.

---

## Decision 8 — Stall detection implemented in the operator playbook, not in F# code

**Decision**: FR-018's stall detection (5 consecutive non-progressing iterations) is enforced by the operator walking `bots/trainer/PLAYBOOK.md`, which instructs comparing the last five `result.json` telemetry blocks and halting if none improved. It is NOT a function inside the bot or the runner.

**Rationale**: The spec frames the iteration loop as operator-driven (FR-013). Encoding stall detection in F# would push it inside either the bot (wrong — bot is per-match) or the runner (possible but couples orchestration to domain knowledge). The playbook is the right place: it is the canonical operator procedure, and automating the check is a future hardening step, not part of the initial feature. The operator can grep the last five `result.json` files with `jq` in a single command, and the playbook will show that command.

**Alternatives considered**:
- *Stall detection as a separate `.fsx` diagnostic script.* Plausible, can be added opportunistically during iteration if the playbook command gets clunky. Not required for the first version.
- *Stall detection inside `run.sh` as a pre-flight check.* Rejected — couples the runner to iteration policy. Keeps the runner focused on "one run, one output".

---

## Summary

All design decisions are resolved. No `NEEDS CLARIFICATION` markers remain. The plan's Technical Context is fully specified. Phase 1 design can proceed: extract entities into `data-model.md`, define JSON contracts for the run directory, and write a quickstart that walks through "install → smoke run → first iteration".
