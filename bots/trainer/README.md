# Bot Iterative Trainer

**Feature**: [020-bot-iterative-trainer](../../specs/020-bot-iterative-trainer/spec.md)
**Branch**: `020-bot-iterative-trainer`

A scripted trainer for Beyond All Reason AI bots. Runs an F# `.fsx` bot in a
headless engine against one of several opponent rungs, captures structured
logs into a run directory, and supports an operator-driven diagnose-improve-
commit-push loop.

## Mantra

**The primary objective is the helper library. Winning is the forcing function.**

Every iteration either improves `bot.fsx` or extracts a reusable helper into
`helpers/`. The ladder escalation exists to drive extractions, not to produce
a leaderboard bot.

## Quickstart

See [`specs/020-bot-iterative-trainer/quickstart.md`](../../specs/020-bot-iterative-trainer/quickstart.md)
for the one-time setup (engine patch, FSBar.Client build) and the first run.

Short version:

```bash
bash bots/trainer/engine-patches/install-barb-profiles.sh
dotnet build src/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug
bash bots/trainer/run.sh NullAI smoke
```

## Iteration loop

Follow [`PLAYBOOK.md`](PLAYBOOK.md) every iteration. Append to
[`HISTORY.md`](HISTORY.md) after every commit. No pull requests — all work
goes directly on the `020-bot-iterative-trainer` branch with commit-and-push
after every change (FR-025 through FR-030).

## Directory layout

```
bots/trainer/
├── bot.fsx              the bot under iteration (edit every loop)
├── helpers/
│   ├── prelude.fsx      #r directives + opens (FSBar.Client DLL loading)
│   ├── log.fsx          structured frame log + result.json writer
│   ├── perception.fsx   stub — grows by extraction
│   └── tactics.fsx      the main match loop (trainerLoopRun)
├── engine-patches/
│   ├── BARb_AIOptions.lua       in-repo patched copy with easy/medium/hard items uncommented
│   └── install-barb-profiles.sh idempotent installer, copies the patch into every engine
├── ladder.json          opponent rungs + fixed map + fixed seed
├── run.sh               one-iteration runner (writes run dir under bots/runs/)
├── PLAYBOOK.md          operator decision tree (READ THIS)
├── HISTORY.md           per-iteration ledger
└── README.md            this file
```

The per-iteration artifacts live under `bots/runs/<timestamp>_<rung>_<iter>/`
and are **gitignored**. They are the operator's working memory, not the
feature's output.

## Helper catalogue

The first and always-present helper is **logging** — `helpers/log.fsx`
exposes `createLogger`, `logStart`, `logFrame`, `writeResult`, `writeError`,
plus the `TrainerEventDetail`, `TrainerLogger`, `TrainerTelemetry` records.

Additional helpers will be added by extraction as iterations produce
duplication (US4 — Phase 5 of tasks.md). This section should be refreshed
on every extraction commit.

## Success criteria

The feature is "done" when:

- `HISTORY.md` shows at least one `win` on `NullAI` (no-op rung).
- `HISTORY.md` shows at least one `win` on `BARb/dev` (first competitive rung).
- `bots/trainer/helpers/` has ≥3 actively-used helpers.

See `specs/020-bot-iterative-trainer/spec.md` §Success Criteria for the
full list.

## 021-rerun-trainer-highbar integration note

Feature `021-rerun-trainer-highbar` re-ran the iteration loop against the
integrated HighBarV2 `029-fix-trainer-issues` proxy. Summary of the
integration is in the outbound mailbox
`Mailbox/2026-04-12_from_FSBarV1_integration_complete.md`, and a
follow-up probe report is in
`Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`.

Key outcomes:
- The four feature-020 trainer-side workarounds (`botDeclaredVictory`,
  "No active session" sniffer, `enum_move=42`, real-path `peak_metal: 0`)
  are gone from shipping code.
- Canonical end-of-game flows through `GameEvent.Shutdown` in the frame
  event stream. `FSBar.Client.Protocol.fs` was updated to synthesize a
  terminal frame for the proxy's standalone `Shutdown` envelope, because
  the proxy delivers it out-of-band (not inside a final `Frame` message).
- `bots/trainer/helpers/perception.fsx` gained its first substantive
  helper — `pickEnemyCommanderPos`, used from two organic call sites in
  `bot.fsx`.
- `PLAYBOOK.md` §10 (10-iteration per-rung budget) and §11 (cross-repo
  defect routing) added for future iteration sessions.
- The `NullAI` rung was dropped from the iteration loop in 021 because
  the engine does not declare `Spring.GameOver` for the scenario; its
  MVP contribution (economy fix verification) is still captured in the
  `smoke-021` / `smoke-021b` HISTORY entries.

The 021 spec, plan, tasks, research, data-model, contracts delta, and
quickstart live under `specs/021-rerun-trainer-highbar/`.
