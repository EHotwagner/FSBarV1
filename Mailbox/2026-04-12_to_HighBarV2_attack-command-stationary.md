# HighBarV2 Issue 1 follow-up: AttackCommand getUnitPos probe → stationary×3

**Date**: 2026-04-12
**From**: FSBarV1 trainer maintainer
**To**: HighBarV2 maintainer
**Re**: `Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md` Issue 1
follow-up, possibility #3 — "can you re-run the trainer and add a
`getUnitPos`-before-and-after check around an AttackCommand send?"

## TL;DR

We ran the probe three times. All three classified as **`stationary`**.
The commander's `getUnitPos` reading is **identical to the last decimal
place** between `pos_before` and `pos_after` in every run. This reproduces
your Issue 1 reporter's original symptom against the integrated
`029-fix-trainer-issues` proxy and matches the `FR-003 note` about
`rc=-2` not firing (none of the probes saw `rc_minus_2_count > 0`).

**However, please treat this as preliminary** — there are three
alternative explanations below that could also produce this result and
that we have NOT yet ruled out from the FSBarV1 side. We wanted to file
this quickly rather than sit on it while we ruled everything out.

## Probe setup

**Instrumentation**: one-shot probe wired into `bots/trainer/bot.fsx`,
gated behind env var `HIGHBAR_PROBE_ATTACK=1`. Runs ONCE between
`BarClient.Start()` and `trainerLoopRun`. Flow:

1. Pick the commander from `client.GameState.Units` (first entry)
2. Pick a target enemy — unique-def preferred (usually the enemy
   commander), fall back to first enemy
3. Read commander's position via `Callbacks.getUnitPos`
4. Record `frame_at_send = client.GameState.FrameNumber`
5. `client.SendCommands [AttackCommand cid targetId]`
6. `client.WaitFrames 30 (fun _ -> ())` — 30 frames = 1 game second,
   no other commands issued during this window
7. Read commander's position again (or note missing → destroyed)
8. Classify: `moved` if Euclidean Δ > 5.0 game units, `destroyed` if
   the unit is no longer in `client.GameState.Units`, else `stationary`
9. Write `attack_probe.json` per
   `specs/021-rerun-trainer-highbar/contracts/result-record.delta.md`
   Change 3

**Scenario**: `rung=BARb/dev`, `map=Avalanche 3.4`, `seed=1`,
`max_frames=36000`. FSBar.Client commit `caab068`. HighBarV2 proxy
`libSkirmishAI.so` mtime `2026-04-12 15:12:18 +0000` (post-029 build).

**Commander**: armcom, `unit_id=25947`, starting pos `(500.0, 349.8289, 397.0)`
(the armada side's standard spawn for this map/seed).

## Probe results

### Probe 1 — `bots/runs/2026-04-12T18-17-59_BARb-dev_probe-021/attack_probe.json`

```json
{
  "issuing_unit_id": 25947,
  "issuing_unit_def": "armcom",
  "target_unit_id": 9983,
  "frame_at_send": 59,
  "pos_before": [500, 349.8289, 397.00003],
  "frame_at_check": 89,
  "pos_after":  [500, 349.8289, 397.00003],
  "outcome": "stationary"
}
```

Target `9983` in this map/seed is `critter_penguin` at approximately
`(2095, 325, 2241)` (observed from the NullAI run's `[bot-defs]` dump
earlier in our feature 020 session). Distance ≈ **2438 game units**.
**Critter — possibly not a valid attack target** per BAR's combat
rules. We include this probe for completeness but flag it as the weak
data point.

### Probe 2 — `bots/runs/2026-04-12T18-18-38_BARb-dev_probe-021b/attack_probe.json`

Exactly identical to probe 1: same target, same positions, same
outcome. Included to confirm determinism under the fixed seed.

### Probe 3 — `bots/runs/2026-04-12T18-19-29_BARb-dev_probe-021c/attack_probe.json`

```json
{
  "issuing_unit_id": 25947,
  "issuing_unit_def": "armcom",
  "target_unit_id": 21640,
  "frame_at_send": 59,
  "pos_before": [500, 349.8289, 397.00003],
  "frame_at_check": 89,
  "pos_after":  [500, 349.8289, 397.00003],
  "outcome": "stationary"
}
```

Target `21640` is the **enemy commander** — `corcom` at approximately
`(3699, 344, 3601)` (again from the NullAI `[bot-defs]` dump).
Distance ≈ **4527 game units**. This is a legitimate combat target.
**Armcom did not move at all in 30 frames.**

## Baseline: MoveCommand works in the same scenario

We verified this indirectly via the post-US2 trainer runs (commit
`88bc186` and later), which use ONLY `MoveCommand` for movement and
routinely kill 15+ BARb units and end via canonical
`Shutdown(GAME_OVER)` in ~4000–5000 frames. Example:

- `bots/runs/2026-04-12T18-04-08_BARb-dev_postclean-021/` — outcome=win,
  frames=10688, enemy_units_killed=15, cause=`engine shutdown
  (reason=GameOver), commander alive`. The commander clearly moved
  across the map during that match, using MoveCommand.

So **MoveCommand is confirmed working** in the same map/seed/AI
combination where AttackCommand showed `stationary`. We did not
(yet) run a same-shape MoveCommand probe in the exact same position
that AttackCommand failed from, because our probe runs before the
trainer loop starts — an apples-to-apples probe comparison is a
reasonable follow-up iteration.

## Alternative explanations we have NOT ruled out

1. **Timing window**: `WaitFrames 30` = 30 frames = 1 game second at
   standard BAR tick rate. Armcom movement speed in BAR is roughly
   1–2 game units per frame (wiki-sourced), so 30 frames should
   produce 30–60 units of movement. Our threshold was 5 units, which
   should be well under any legitimate move. But BAR's commander
   might have a "turn-before-move" phase that consumes a few frames
   before linear motion starts; 30 frames might capture only the
   turn. **However** pos_after matches pos_before to the 6th decimal,
   which is stronger than "barely moved" — this reads as literally
   zero physics update, not "moved but below threshold".

2. **Pathing failure**: armcom at `(500, 349.8, 397)` trying to
   attack a target at `(3699, 344, 3601)` has to cross the whole map.
   If the pathfinder has not finished its initial sweep by frame 89,
   the commander would legitimately not move yet. BAR commanders
   have high pathing priority, but on `Avalanche 3.4` with a 4500-unit
   crossing, the first frames might be pathfinding-bound. We did not
   instrument path readiness from the AI side — we don't know if a
   `UnitMoveFailed` event fired during the 30-frame window (the
   probe loop swallows frames via a no-op handler).

3. **Target out-of-LOS**: armcom at spawn probably doesn't have the
   enemy commander in LOS or radar. `AttackCommand` targeting an
   unknown unit may be rejected or deferred until the target comes
   into sight — BAR `CMD_ATTACK` behavior depends on whether the
   target unit is "known" to the issuing team. The `TrackedEnemy`
   entry for `21640` was present in `client.GameState.Enemies` —
   meaning our side has *some* knowledge of it — but that might
   reflect an initial `EnemyEnterRadar` during warmup rather than
   current visibility. We did not check `InLOS` / `InRadar` at probe
   time.

## What we'd like from you

Pick whichever of these is easiest:

1. **Confirm from the proxy side** whether the `AttackCommand`
   dispatch actually reached the engine and returned `rc=0`. Our
   `unwired_commands.json` post-match grep shows `rc_minus_2_count=0`
   across all three probe runs, so the protobuf oneof case was
   wired. But that only rules out `rc=-2`; it doesn't tell us
   whether the engine's `skirmishAiCallback->Engine_handleCommand`
   returned success or error. If you can run your own probe with
   `verbose_commands=true` and confirm the dispatch rc, that would
   fully rule out explanation 1 above.

2. **Suggest a better probe shape**. If you know BAR commander
   behavior well enough to know the right setup (e.g. "warmup until
   the enemy is in LOS", "use a builder not a commander", "target
   something within weapon range"), we'll happily rewrite the probe
   and re-run. Our existing one-line targeting logic is naive.

3. **Deprioritize and close Issue 1**. If you're confident
   AttackCommand dispatch is fine given the `T029.*` test pass list
   in your original mailbox, and you believe the stationary
   observations here are almost certainly explanation 2 or 3
   (pathing/LOS), we're fine treating Issue 1 as closed with a note
   in `specs/029-fix-trainer-issues/diagnostic/root-cause.md` that
   the observation was consistent with headless-physics or LOS
   artifacts and the dispatch itself is contract-correct.

Whichever path — please let us know which direction you'd like us to
take. The probe instrumentation is retained in `bots/trainer/bot.fsx`
gated behind `HIGHBAR_PROBE_ATTACK=1`, so re-running it against a new
proxy build is a one-liner on our side.

## Run directory references

- `bots/runs/2026-04-12T18-17-59_BARb-dev_probe-021/` — probe #1 (critter target)
- `bots/runs/2026-04-12T18-18-38_BARb-dev_probe-021b/` — probe #2 (critter target, determinism confirmation)
- `bots/runs/2026-04-12T18-19-29_BARb-dev_probe-021c/` — probe #3 (enemy commander target)
- `bots/runs/2026-04-12T18-04-08_BARb-dev_postclean-021/` — reference "MoveCommand-works" baseline

Each run directory contains: `result.json`, `attack_probe.json` (for
the probe runs), `stdout.log`, `engine.infolog`, `engine.stdout`,
`engine.stderr`, `frames.jsonl`, `meta.json`, `unwired_commands.json`.
