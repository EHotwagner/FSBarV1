# Data Model — Bot Iterative Trainer

**Feature**: 020-bot-iterative-trainer
**Date**: 2026-04-12

This feature does not introduce a database. All entities are serialised as files on disk under `bots/` (in-repo, committed) and `bots/runs/` (gitignored, per-run artifacts). This document defines the logical entities, their fields, relationships, and lifecycle transitions.

---

## Entities

### 1. `EngineConfig` (extended in-repo type)

**Location**: `src/FSBar.Client/EngineConfig.fs` (and `.fsi`).

**Status**: Existing F# record. This feature extends it.

**New fields added by this feature**:

| Field | Type | Default | Meaning |
|---|---|---|---|
| `OpponentAIOptions` | `Map<string, string>` | `Map.empty` | Key/value options passed to the opponent AI under `[AI1].[OPTIONS]` in the generated start script. Keys and values are opaque to the generator — they are rendered verbatim as `key=value;`. Empty map produces no `[OPTIONS]` block at all (backward-compatible). |
| `DeathMode` | `string` | `"com"` | Value for `deathmode=` in the generated start script's `[MODOPTIONS]` block. Replaces the previously hard-coded `"neverend"`. Valid values: `"com"` (commander-death), `"neverend"`, `"all"` — the generator does not validate. |

**Pre-existing fields unchanged**: `MapName`, `GameSpeed`, `OpponentAI`, `EngineBin`, `SpringDataDir`, `SocketPath`, `TimeoutMs`, etc. — see existing `EngineConfig.fsi`.

**Validation rules**:
- `DeathMode` MUST be a non-empty string; the default `"com"` is used when callers do not set it explicitly.
- `OpponentAIOptions` keys MUST NOT contain `=` or `;` (which would break the TDF script format). Values SHOULD NOT contain `;`. The generator does NOT escape — callers are responsible. A unit test asserts the generator's output contains `=` and `;` exactly where expected for a small fixture.

**Surface-area impact**: Addition of two record fields changes the `FSBar.Client` public API surface. The baseline file under `tests/FSBar.Client.Tests/baselines/` MUST be regenerated in the same commit.

---

### 2. `Ladder` (on-disk JSON)

**Location**: `bots/trainer/ladder.json` (committed).

**Schema**: see `contracts/ladder.schema.json`. Informal shape:

```json
{
  "map": "Red Comet",
  "seed": 1337,
  "rungs": [
    { "name": "NullAI",       "opponent": "NullAI", "options": {},                      "max_frames": 20000 },
    { "name": "BARb/dev",     "opponent": "BARb",   "options": { "profile": "dev" },    "max_frames": 30000 },
    { "name": "BARb/easy",    "opponent": "BARb",   "options": { "profile": "easy" },   "max_frames": 45000 },
    { "name": "BARb/medium",  "opponent": "BARb",   "options": { "profile": "medium" }, "max_frames": 60000 },
    { "name": "BARb/hard",    "opponent": "BARb",   "options": { "profile": "hard" },   "max_frames": 90000 }
  ]
}
```

**Fields**:
- `map` (string, required) — fixed map name for every run in this feature (Clarifications Q4).
- `seed` (integer, required) — fixed RNG seed for every run in this feature (Clarifications Q5). Reported via `meta.json` and forwarded to `EngineConfig.FixedRNGSeed` which already exists in the start script generator.
- `rungs` (array of rung objects, required) — at least one no-op rung AND at least one competitive rung must be present, in order (FR-012).

**Rung object fields**:
- `name` (string, required) — unique short name used in `HISTORY.md`, run directory naming, and operator commands. E.g. `"NullAI"`, `"BARb/easy"`. Must be unique within the ladder.
- `opponent` (string, required) — opponent AI short name as recognised by the engine (e.g. `"NullAI"`, `"BARb"`).
- `options` (object, required; may be empty) — maps directly to `EngineConfig.OpponentAIOptions`.
- `max_frames` (integer, required) — frame limit after which the match is terminated with `outcome: "timeout"`.

**Lifecycle**: static configuration. Edited by the operator only when the ladder definition itself changes. Every run reads the current `ladder.json` state.

**Relationships**:
- A `Rung` is referenced by zero-or-more `Iteration` records via `rung_name`.
- The first rung in `rungs` MUST be the no-op rung; the second MUST be the first competitive rung (these are the two minimum-for-completion rungs per Clarifications Q3 / SC-011).

---

### 3. `Iteration` (on-disk, append-only log entry)

**Location**: `bots/trainer/HISTORY.md` (committed). One line per iteration.

**Shape** (line format, tab-separated or fixed columns):

```
| iter_id | timestamp           | rung_name   | outcome | frames | commit_sha | run_dir_name                                 | note           |
|---------|---------------------|-------------|---------|--------|------------|-----------------------------------------------|----------------|
| 001     | 2026-04-12T14:30:00 | NullAI      | win     | 4123   | a1b2c3d    | 2026-04-12T14-30-00_NullAI_001/               | first win      |
| 002     | 2026-04-12T14:38:11 | BARb/dev    | loss    | 18900  | a1b2c3d    | 2026-04-12T14-38-11_BARb-dev_002/             | no metal yet   |
```

**Fields**:
- `iter_id` (zero-padded integer, required) — monotonically increasing per session.
- `timestamp` (ISO-8601, required) — when the run started.
- `rung_name` (string, required) — matches a `Rung.name` from `ladder.json`.
- `outcome` (enum, required) — `win` | `loss` | `timeout` | `error` | `interrupted`.
- `frames` (integer, required) — frame count at termination.
- `commit_sha` (short git SHA, required) — the commit in effect when the iteration ran.
- `run_dir_name` (string, required) — the basename of the run directory under `bots/runs/`.
- `note` (free text, optional) — operator remark.

**Lifecycle**: append-only. Never rewritten. Halts and stalls are appended as separate lines with outcome `error` or `interrupted`. The operator classification (bot-logic / repo-bug / helper / out-of-scope) goes into the commit message of whatever change followed — not into this log.

**Validation rules**:
- `iter_id` strictly increases by 1.
- Each line's `commit_sha` MUST be a real commit reachable from the feature branch.
- Each line's `run_dir_name` MUST exist under `bots/runs/` on the operator's machine at the time the line was written. Existence on remote is not required (`bots/runs/` is gitignored).

**Relationships**:
- One-to-one with `Run Directory` (via `run_dir_name`).
- Zero-or-more per `Rung` (via `rung_name`).

---

### 4. `Run Directory` (on-disk, one per iteration)

**Location**: `bots/runs/<timestamp>_<rung_slug>_<iter_id>/` (gitignored).

**Contents**: see `contracts/run-directory.md` for the canonical layout and `contracts/meta.schema.json`, `contracts/frame.schema.json`, `contracts/result.schema.json` for per-file schemas.

**Components** (every run MUST contain all of these):

| File | Format | Purpose |
|---|---|---|
| `meta.json` | JSON object | Immutable run facts: rung name, opponent, opponent options, engine version, bot git SHA, seed, map, frame limit, start timestamp, host. |
| `bot.fsx.snapshot` | F# script copy | Verbatim copy of `bots/trainer/bot.fsx` at the moment the run started. Always reflects exactly what ran. |
| `stdout.log` | plain text | Bot's own `printfn` output, captured from `dotnet fsi bot.fsx`. |
| `frames.jsonl` | one JSON object per line | Sampled per-frame summaries (see `contracts/frame.schema.json`). Flushed after each write. |
| `engine.stdout` | plain text | Engine stdout, copied from `getSessionDir(config)/stdout.log`. |
| `engine.stderr` | plain text | Engine stderr, copied from `getSessionDir(config)/stderr.log`. |
| `engine.infolog` | plain text | Engine native infolog, copied from `getSessionDir(config)/infolog.txt` if present. |
| `result.json` | JSON object | Terminal result record (see `contracts/result.schema.json`). Written once, at match end or error. |

**Lifecycle**:
1. `run.sh` creates the directory and writes `meta.json` and `bot.fsx.snapshot`.
2. Bot starts, opens `frames.jsonl` append-only, writes to it as the match proceeds.
3. Match ends (win/loss/timeout) → bot writes `result.json` and closes `frames.jsonl`.
4. `run.sh` copies engine logs from the session dir into the run dir.
5. Directory is then immutable for post-hoc analysis.

**Error transitions**:
- If bot crashes before writing `result.json`, `run.sh` writes a stub `result.json` with `outcome: "error"` and `cause: "bot-exit-without-result"`.
- If engine fails to start, `run.sh` writes `result.json` with `outcome: "error"` and `cause: "<specific engine error>"`; `frames.jsonl` is empty, `engine.*` logs may still be copied if they exist.
- If operator interrupts (SIGINT), `run.sh` writes `outcome: "interrupted"` and ensures engine cleanup (FR-006).

**Uniqueness**: the directory name is `<iso-timestamp>_<rung-slug>_<iter-id>` where `rung-slug` is `rung_name` with `/` replaced by `-` and non-alphanumerics stripped. Timestamp has second resolution; `iter_id` provides extra uniqueness for fast back-to-back invocations.

---

### 5. `Terminal Result Record` (subcomponent of Run Directory)

**Location**: `bots/runs/<run_dir>/result.json` (see `contracts/result.schema.json`).

**Shape**:

```json
{
  "outcome": "win",
  "frames": 4123,
  "cause": "Enemy commander unit 17 destroyed by friendly unit 12 at frame 4120",
  "victory_signal": "engine-shutdown-gameover",
  "error_message": null,
  "telemetry": {
    "commands_total": 412,
    "units_built": 14,
    "units_lost": 3,
    "enemy_units_killed": 1,
    "peak_metal": 842.5,
    "peak_energy": 3120.0,
    "frames_survived": 4123
  }
}
```

**Fields**:
- `outcome` (enum, required): `"win"` | `"loss"` | `"timeout"` | `"error"` | `"interrupted"`. Ordering for spec comparison: `win > timeout > loss > error ≈ interrupted`.
- `frames` (integer, required): frame count at termination.
- `cause` (string, required): human-readable termination cause.
- `victory_signal` (string, required for win): the specific signal that triggered the win classification. For this feature, only `"engine-shutdown-gameover"` is valid (Clarifications Q1 + FR-011).
- `error_message` (string or null): present when `outcome ∈ {error}`.
- `telemetry` (object, required): block of numeric fields used for stall detection.

**Telemetry subfields** (all required, per FR-018 + FR-004):
- `commands_total` (integer): total number of `AICommand`s the bot sent to the engine.
- `units_built` (integer): number of friendly `UnitFinished` events.
- `units_lost` (integer): number of friendly `UnitDestroyed` events.
- `enemy_units_killed` (integer): number of `EnemyDestroyed` events attributable to friendly units.
- `peak_metal` (float): highest value of `GameState.Metal.Current` observed during the match.
- `peak_energy` (float): highest value of `GameState.Energy.Current` observed during the match.
- `frames_survived` (integer): frame count at which the bot's commander was still alive (equals `frames` unless bot died before frame limit).

**Progress rule (FR-018 / Clarifications Q2)**: iteration N+1 made progress over iteration N iff at least one of `{ frames_survived, enemy_units_killed, peak_metal, peak_energy, units_built }` strictly increased. Five consecutive iterations with no progress → stall halt.

---

### 6. `Helper Module` (in-repo F# script)

**Location**: `bots/trainer/helpers/<name>.fsx`.

**Initial members** (bootstrapped per Decision 7 in research.md):
- `prelude.fsx` — `#r` directives for `FSBar.Client.dll` and dependencies, `open FSBar.Client`, common aliases. Always loaded first by `bot.fsx`.
- `log.fsx` — module `Trainer.Log`. Exposes `Logger.create : runDir:string -> Logger` with methods `LogStart`, `LogFrame`, `WriteResult`, `WriteError`. Bootstrapped up-front.
- `perception.fsx` — module `Trainer.Perception`. Starts empty (module declaration + `// extracted helpers land here` comment). Grows by extraction.
- `tactics.fsx` — module `Trainer.Tactics`. Starts with one function: `TrainerLoop.run : BarClient -> Logger -> Result`. The loop body initially inlines all per-match logic; extraction moves pieces out of `TrainerLoop.run` into the appropriate helper as patterns appear.

**Extraction rules** (FR-019 / FR-020 / FR-021):
- A helper MUST NOT be created preemptively. Exception: `log.fsx` and `TrainerLoop.run` skeleton, which are bootstrapped.
- When the same logic appears in two iterations' `bot.fsx` history, it MUST be extracted into the appropriate helper in a single commit that also updates `bot.fsx` to call the helper.
- After any single commit, `bot.fsx` MUST still load and run. No half-extractions.

**Lifecycle**:
- Created: at feature start (day 1 commit for `prelude`/`log`/skeletons of perception/tactics).
- Modified: by extraction commits throughout the iteration phase.
- Removed: never, within this feature.

---

### 7. `Playbook` and `Out-of-Scope Report` (in-repo documents)

**`bots/trainer/PLAYBOOK.md`** — the operator procedure. Static document. Defines the decision tree: after every run, classify → act → commit → push → advance-or-retry. Written once at feature start, amended only if the procedure itself needs updating.

**`bots/runs/REPORT.md`** — out-of-scope bug report. Created on demand when the operator classifies a failure as external (FR-016). Gitignored (lives under `bots/runs/`), so the operator is free to write freely without polluting git. If the operator decides a report should be preserved, they copy it manually into the spec directory as `specs/020-bot-iterative-trainer/out-of-scope-reports/<iter_id>.md`.

---

## Entity Relationships

```text
EngineConfig (code)  ──rendered-by──▶  ScriptGenerator (code)  ──emits──▶  script.txt (transient, in engine session dir)

Ladder (bots/trainer/ladder.json)
  │
  └── Rung (element)
        ▲
        │ referenced-by
        │
Iteration (bots/trainer/HISTORY.md line)
  │
  ├── 1:1 Run Directory (bots/runs/<dirname>)
  │       │
  │       ├── meta.json          (references Rung via opponent+options, Commit via sha)
  │       ├── bot.fsx.snapshot
  │       ├── frames.jsonl
  │       ├── stdout.log
  │       ├── engine.{stdout,stderr,infolog}
  │       └── result.json        (Terminal Result Record)
  │
  └── references Commit on feature branch via commit_sha

Helper Module (bots/trainer/helpers/*.fsx)
  │
  ├── prelude.fsx      (always loaded)
  ├── log.fsx          (bootstrapped)
  ├── perception.fsx   (grows by extraction)
  └── tactics.fsx      (grows by extraction; owns TrainerLoop.run)
        ▲
        │ #load-ed-by
        │
bot.fsx (bots/trainer/bot.fsx)
```

## State Transitions

### Iteration state machine

```
[start] ──run.sh──▶ RUNNING ──(engine Shutdown + commander dead)──▶ WIN
                          ├──(engine Shutdown + our commander dead)──▶ LOSS
                          ├──(frame_count ≥ max_frames)──▶ TIMEOUT
                          ├──(unhandled exception / engine crash)──▶ ERROR
                          └──(SIGINT / operator kill)──▶ INTERRUPTED

WIN on competitive rung ──classify=clean-win──▶ advance to next rung
WIN on no-op rung (not last)──classify=clean-win──▶ advance to next rung
LOSS / TIMEOUT ──classify=(bot-logic|repo-bug|helper-extraction)──▶ commit → push → retry same rung
LOSS / TIMEOUT ──classify=out-of-scope──▶ write REPORT → HALT
ERROR ──classify=infrastructure-regression──▶ fix runner/helper → commit → push → retry
INTERRUPTED ──no classification required──▶ retry same rung

After 5 consecutive non-progressing iterations on the same rung → STALL → HALT (automatic, per FR-018)
Feature complete when WIN recorded on no-op rung AND on first competitive rung (SC-011)
```

---

## Validation Summary

All entities are defined with explicit fields, lifecycle, relationships, and validation rules. No `NEEDS CLARIFICATION` markers. Phase 1 proceeds to contracts and quickstart.
