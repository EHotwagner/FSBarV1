# Contract: Run Directory Layout

**Feature**: 020-bot-iterative-trainer

Every trainer invocation (`bots/trainer/run.sh <rung_name> <iter_id>`) produces exactly one run directory under `bots/runs/`. This document is the authoritative contract for that directory's layout and contents. Any deviation is an infrastructure regression (FR-022, SC-007).

## Directory naming

```
bots/runs/<iso_timestamp>_<rung_slug>_<iter_id>/
```

- `iso_timestamp`: `YYYY-MM-DDTHH-MM-SS` (colons replaced with hyphens for filesystem safety).
- `rung_slug`: `Rung.name` with `/` → `-` and any character outside `[A-Za-z0-9._-]` stripped. E.g. `BARb/easy` → `BARb-easy`.
- `iter_id`: three-digit zero-padded iteration counter from `HISTORY.md`, e.g. `001`, `042`.

Example: `bots/runs/2026-04-12T14-30-00_NullAI_001/`.

## Required files

Every run directory MUST contain the following files. Any missing file is a conformance failure.

| File | Format | Writer | Purpose |
|---|---|---|---|
| `meta.json` | JSON object | `run.sh` | Immutable run facts. See `meta.schema.json`. |
| `bot.fsx.snapshot` | F# script | `run.sh` | Verbatim copy of `bots/trainer/bot.fsx` at launch time. |
| `ladder.snapshot.json` | JSON | `run.sh` | Verbatim copy of `bots/trainer/ladder.json` at launch time. |
| `stdout.log` | plain text | `run.sh` (redirect from `dotnet fsi`) | Bot's own `printfn` output. |
| `frames.jsonl` | JSON Lines | bot (via `Trainer.Log`) | One JSON object per sampled frame. See `frame.schema.json`. |
| `engine.stdout` | plain text | `run.sh` (copy) | Engine stdout from `getSessionDir(config)/stdout.log`. |
| `engine.stderr` | plain text | `run.sh` (copy) | Engine stderr from `getSessionDir(config)/stderr.log`. |
| `engine.infolog` | plain text | `run.sh` (copy) | Engine native infolog if present at `<sessionDir>/infolog.txt`. May be empty if engine didn't write one. |
| `result.json` | JSON object | bot (via `Trainer.Log`) or `run.sh` (stub on crash) | Terminal result record. See `result.schema.json`. |

## Write ordering

1. `run.sh` writes `meta.json`, `bot.fsx.snapshot`, `ladder.snapshot.json` **before** launching `dotnet fsi`.
2. Bot opens `frames.jsonl` append-only at startup.
3. Bot writes to `stdout.log` via natural process stdout redirection (bash `>`).
4. Bot writes `result.json` immediately before clean exit.
5. After `dotnet fsi` returns, `run.sh` copies `engine.*` from the session directory.
6. If `result.json` is missing after bot exit, `run.sh` writes a stub with `outcome: "error"` and `cause: "bot-exit-without-result"`.

## Consumer guarantees

A consumer reading a run directory MAY assume:

- Every JSON file is valid JSON (or JSON Lines for `frames.jsonl`).
- `meta.json` exists and is complete.
- `result.json` exists with a valid `outcome` field — even on error paths.
- The bot script that produced this run is readable at `bot.fsx.snapshot`, independent of the live `bots/trainer/bot.fsx`.

A consumer MUST NOT assume:

- `frames.jsonl` is non-empty (a crash during warmup can leave it empty).
- `engine.*` files are non-empty (engine may have failed before producing logs).
- File modification times are meaningful — `run.sh` may `cp` files with preserved mtimes from older engine sessions.

## Non-conformance

A run directory is **non-conformant** if any of the following holds:

- A required file is missing.
- A JSON file fails to parse.
- `result.json.outcome` is absent or not in the enum `{win, loss, timeout, error, interrupted}`.
- `meta.json` is missing any required field from `meta.schema.json`.

Non-conformant runs MUST be counted against SC-007's 10% infrastructure regression budget and MUST produce at least one fix commit on the feature branch.
