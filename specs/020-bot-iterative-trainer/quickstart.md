# Quickstart — Bot Iterative Trainer

**Feature**: 020-bot-iterative-trainer

This quickstart walks an operator through (1) installing the one-time engine patch, (2) running a single smoke match against the no-op opponent, (3) starting the iteration loop. Target audience: an experienced developer (or AI developer) sitting in front of the repository, on the `020-bot-iterative-trainer` feature branch.

## 0. Prerequisites

- Linux development machine with Beyond All Reason installed at `~/.local/state/Beyond All Reason/`.
- `dotnet` SDK 10.0 on `PATH`.
- `git` configured with push access to `origin` on the feature branch.
- Working directory: `/home/<user>/projects/FSBarV1`.
- Branch: `020-bot-iterative-trainer` (already checked out).

```bash
# Verify you are on the feature branch
git rev-parse --abbrev-ref HEAD
# → 020-bot-iterative-trainer
```

## 1. One-time engine patch (install BARb difficulty profiles)

The installed BARb ships with `easy`/`medium`/`hard` profile items commented out in `AIOptions.lua`. The trainer needs them uncommented so the ladder's `profile` option is accepted. The patch is stored in the repo and applied by an installer script.

```bash
bash bots/trainer/engine-patches/install-barb-profiles.sh
```

Expected output:

```
→ Detecting BAR engine versions...
→ Patching recoil_2025.06.19 ... OK (already applied is a no-op)
→ Done. BARb profiles available: dev, easy, medium, hard.
```

This is idempotent — running it twice is safe. Re-run it after every engine reinstall.

## 2. Build FSBar.Client

The bot `#r`-references the compiled `FSBar.Client.dll`, so a build is required before the first run. Subsequent bot edits do NOT require a rebuild — only edits to `src/FSBar.Client/*.fs` do.

```bash
dotnet build src/FSBar.Client/FSBar.Client.fsproj -c Debug
dotnet test tests/FSBar.Client.Tests/FSBar.Client.Tests.fsproj -c Debug
```

The tests MUST be green before the first run. If they are not, that's the first iteration's work — fix the repo, commit, push, retry.

## 3. Smoke run — one match against the no-op opponent

```bash
bots/trainer/run.sh NullAI smoke
```

Expected output (elided):

```
[run.sh] iter=smoke rung=NullAI
[run.sh] run dir: bots/runs/2026-04-12T14-30-00_NullAI_smoke/
[run.sh] meta.json written (engine=recoil_2025.06.19, sha=a1b2c3d)
[run.sh] launching dotnet fsi bot.fsx ...
FSBar.Client loaded, connecting to engine ...
Connected. Starting frame loop.
[frame 60] warmup complete, 2 units, 0 enemies
[frame 4120] enemy commander destroyed
Match over: win
[run.sh] copying engine logs from /tmp/fsbar-<guid>/
[run.sh] result: win frames=4123 cause="Enemy commander unit 17 destroyed ..."
```

Verify the run directory is conformant:

```bash
RUN=$(ls -1td bots/runs/* | head -1)
ls "$RUN"
# → meta.json bot.fsx.snapshot ladder.snapshot.json stdout.log frames.jsonl engine.stdout engine.stderr engine.infolog result.json

jq .outcome "$RUN/result.json"
# → "win"
```

If this works, US1 (Run one full match and capture everything) is validated end-to-end.

## 4. First iteration (diagnose → improve → commit → push)

Open `bots/trainer/PLAYBOOK.md` and follow it. The short version:

1. Read the most recent `result.json`:
   ```bash
   jq . "$(ls -1td bots/runs/* | head -1)/result.json"
   ```
2. If `outcome == "win"`: advance the current rung pointer (operator tracks this in `HISTORY.md`); commit any bot or helper changes made during analysis; push; re-run on the next rung.
3. If `outcome != "win"`: read `stdout.log` and tail `engine.infolog`; classify the failure:
   - **bot-logic** → edit `bots/trainer/bot.fsx`.
   - **repo-bug** → edit `src/FSBar.Client/*.fs`, run `dotnet build && dotnet test tests/FSBar.Client.Tests`.
   - **helper-extraction** → lift the repeated logic into `bots/trainer/helpers/<perception|tactics>.fsx`, update `bot.fsx` to call the helper.
   - **out-of-scope** → write `bots/runs/REPORT.md`, HALT, ask the user.
4. Commit with a focused message and push:
   ```bash
   git add -A
   git commit -m "trainer: bot iter N — <what changed>"
   git push origin 020-bot-iterative-trainer
   ```
5. Append a line to `bots/trainer/HISTORY.md` recording the completed iteration.
6. Re-run: `bots/trainer/run.sh <rung> <next_iter_id>`.

## 5. Stall detection (manual check via playbook)

After every iteration on the same rung, compute whether the last five iterations made progress on any of the telemetry fields. The playbook includes a one-liner:

```bash
# Compare telemetry of the last 5 runs on the current rung
ls -1td bots/runs/*_<rung_slug>_* | head -5 | while read d; do
  jq -c '{iter: .iter_id, tel: .telemetry}' "$d/result.json"
done
```

If none of `frames_survived`, `enemy_units_killed`, `peak_metal`, `peak_energy`, `units_built` strictly increased across the five runs → STALL. Write a stall note, HALT, ask the user before retrying the same rung.

## 6. Feature completion check

The feature is complete when `HISTORY.md` shows at least one `win` on the no-op rung AND at least one `win` on the first competitive rung (per SC-011). You can eyeball this, or run:

```bash
awk -F'|' '/win/ { print $4 }' bots/trainer/HISTORY.md | sort -u
```

When both rung names appear in the output, the feature's primary objective is met. Continue iterating against harder rungs opportunistically.

## 7. Where everything lives

| Thing | Path |
|---|---|
| Bot under iteration | `bots/trainer/bot.fsx` |
| Helper library | `bots/trainer/helpers/` |
| Ladder config | `bots/trainer/ladder.json` |
| Runner | `bots/trainer/run.sh` |
| Operator procedure | `bots/trainer/PLAYBOOK.md` |
| Iteration history | `bots/trainer/HISTORY.md` |
| Per-run artifacts | `bots/runs/<timestamp>_<rung>_<iter>/` (gitignored) |
| Out-of-scope reports | `bots/runs/REPORT.md` (gitignored) |
| Engine patches | `bots/trainer/engine-patches/` |
| Feature spec | `specs/020-bot-iterative-trainer/spec.md` |
| Feature plan | `specs/020-bot-iterative-trainer/plan.md` |
| Run directory contract | `specs/020-bot-iterative-trainer/contracts/run-directory.md` |
