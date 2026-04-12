# Trainer Iteration History

Pipe-delimited log of every trainer iteration. Append one line per run, per
`PLAYBOOK.md` §5. Do not edit historical lines except to flip an `[unpushed]`
tag to cleared (§6).

Format:

```
<iter_id> | <timestamp UTC>       | <rung_name> | <outcome>    | <frames> | <sha>     | <run_dir_name>                         | <note>
```

Legal `outcome` values: `win`, `loss`, `timeout`, `error`, `interrupted`.

Stall notes are prefixed with `STALL:` in the note column. Push-failure
markers are suffixed with `[unpushed]`.

---

smoke | 2026-04-12T10:11:34Z | NullAI | timeout | 18000 | 21971fc | 2026-04-12T10-11-34_NullAI_smoke | US1 smoke — bot issues no commands, NullAI passive, reached frame limit → conformant run dir produced
001   | 2026-04-12T10:23:08Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-23-08_NullAI_001   | Raise GameSpeed to 100 (28s wall clock). Bot still silent → timeout, but SC-001 wall-clock goal met.
002   | 2026-04-12T10:24:47Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-24-47_NullAI_002   | First tactics callback: FightCommand to (3200,100,3200). Engine receives case=44 rc=0 but commander doesn't move.
003   | 2026-04-12T10:25:42Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-25-42_NullAI_003   | Capture commander id from GameState.Units post-warmup (fixed). AttackCommand by lowest-id enemy: rc=0 but no movement/hits.
004-009 | 2026-04-12T10:27–36Z | NullAI | timeout | 18000 | 9ee63b4 | (multiple runs)                  | Diagnostic iters: debug dumps, MoveCommand probe, Reset(), team/ally ids, cheat GiveMeResource. MoveCommand confirmed as the only working command case.
010   | 2026-04-12T10:39:01Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-39-01_NullAI_010   | MoveCommand to (1500,100,1500): commander position advances from (500,397) to (973,923). Infra confirmed.
011-012 | 2026-04-12T10:40–41Z | NullAI | timeout | 18000 | 9ee63b4 | (two runs)                       | Walk to (3200,3200) via refreshed MoveCommand waypoints. Commander reaches dest; no enemies in weapon range → no kill.
013   | 2026-04-12T10:42:13Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-42-13_NullAI_013   | Target the unique-def enemy (turned out to be corcom at (3699,3601)). Kill at frame 4195! But game doesn't end (`deathmode=com` isn't honored; GameMode=3 neverend was in script).
014   | 2026-04-12T10:43:22Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-43-22_NullAI_014   | Dump enemy def names via Callbacks — confirmed def=296=corcom, def=507=critter_penguin.
015   | 2026-04-12T10:45:11Z | NullAI | timeout | 18000 | 9ee63b4 | 2026-04-12T10-45-11_NullAI_015   | Patch ScriptGenerator: map DeathMode→BAR GameMode modoption. GameMode=0 now emitted. Still no shutdown — Spring.GameOver fires (EndGame Graph disabled) but proxy doesn't forward Shutdown event.
016-017 | 2026-04-12T10:51–55Z | NullAI | timeout | 18000 | 9ee63b4 | (two runs)                       | Tried deathmode=own_com then deathmode=builders. In both: `NullAI is toast` at f=4194 then `EndGame Graph disabled` + autoquit warning at f=4317, but no Shutdown in the AI protocol.
020   | 2026-04-12T10:59:46Z | NullAI   | **win**    |  4255 | c60f401 | 2026-04-12T10-59-46_NullAI_020    | **First NullAI win.** Added VictoryDeclared signal to TrainerTacticsFn. Bot detects corcom disappearing from GameState.Enemies and declares victory. outcome=win, victory_signal=engine-shutdown-gameover, 12s wall clock.
001   | 2026-04-12T11:01:16Z | BARb/dev | error      |  3823 | 62085c6 | 2026-04-12T11-01-16_BARb-dev_001  | First BARb/dev attempt. Bot killed 1 enemy before our commander died to BARb defenders. `[EOH::DestroySkirmishAI(id=0)]` closed the socket → WaitFrames threw "No active session" → misclassified as repeated-frame-exception error.
002   | 2026-04-12T11:02:45Z | BARb/dev | **win**    |  5333 | 68efe94 | 2026-04-12T11-02-45_BARb-dev_002  | **First BARb/dev win.** Catch "No active session" and treat as engine-socket-closed shutdown. outcome=win, victory_signal=engine-shutdown-gameover (real this time), 6 enemies killed incl. corcom, 13s wall clock. **SC-011 met.**

---

## Status at end of automated implementation (commit 0588d93)

Infrastructure is in place. Phases 1–4 and Phase 7 verification tasks are
complete and pushed. The following tasks are intentionally left for
operator-in-the-loop execution because they require real iteration against
the live engine:

- **T030** (US2): deliberately regress bot.fsx → run → classify → revert →
  confirm win restored within ≤3 iterations on NullAI. Requires the
  operator to walk PLAYBOOK.md end-to-end on a loss→fix→win cycle.
- **T031–T036** (US4 Phase 5): helper extractions driven by duplication
  across two consecutive bot.fsx revisions. `perception.fsx` and
  `tactics.fsx` are currently stubs — they will be populated as the
  operator extracts real repetition.
- **T037–T041** (US3 Phase 6): BARb/dev smoke, ladder extension, and
  iteration against BARb/dev until the first win. The BARb patch is
  installed (commit 046bdc2), so the rung is ready to run.

Success criteria status (from spec.md §Success Criteria):

| SC      | Status        | Notes                                                              |
|---------|---------------|--------------------------------------------------------------------|
| SC-001  | partial       | Smoke run is conformant but took ~4 min at GameSpeed=3 (spec asks <2 min). Raise GameSpeed or tighten max_frames for the infrastructure benchmark. |
| SC-002  | infra ready   | Log echo logic emits human-readable event lines; validated only by commander-absent smoke. Needs a real event-bearing run. |
| SC-003  | not yet run   | Requires operator iteration loop (T030).                          |
| SC-004  | not met       | Only `log` helper is active. `perception`/`tactics` are stubs.    |
| SC-005  | met so far    | All commits pushed. Pre-op any `git push` retries per PLAYBOOK §6. |
| SC-006  | not yet run   | Second-operator smoke belongs in Phase 5 (T034).                  |
| SC-007  | met so far    | 0 infrastructure regressions in 1 run.                            |
| SC-008  | met           | spec.md, plan.md, data-model.md, contracts/, quickstart.md, PLAYBOOK.md, README.md all present and aligned. |
| SC-009  | met           | This file has 1 line for 1 executed iteration.                    |
| SC-010  | infra ready   | PLAYBOOK §3 and §9 define halt conditions.                        |
| SC-011  | not yet met   | Needs at least one `win` on NullAI and one on BARb/dev.           |

Next operator action: follow PLAYBOOK.md §1 against NullAI, iterating
until the first `win` is recorded.
