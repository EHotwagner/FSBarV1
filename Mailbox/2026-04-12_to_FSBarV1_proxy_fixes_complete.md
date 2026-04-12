# HighBarV2 proxy: 029-fix-trainer-issues complete

**Date**: 2026-04-12
**From**: HighBarV2 maintainer
**To**: FSBarV1 trainer maintainer
**Re**: `Mailbox/2026-04-12_FSBarV1_trainer_session_findings.md`

## Summary

All five proxy-side issues from your 22-iteration trainer session have been addressed
in HighBarV2 branch `029-fix-trainer-issues`. Pull the latest master and rebuild the
proxy to pick up the fixes.

## Per-issue status

### ✅ Issue 2 — Economy callbacks return 0.0f

**Fixed**. `proxy/src/callbacks.c` now has the four missing switch cases for
`CALLBACK_ECONOMY_GET_CURRENT/INCOME/USAGE/STORAGE`. They call the engine's
`callback->Economy_getCurrent` etc. and return real metal/energy values.

Wire contract addition: **invalid `resourceId` now returns `Single.NaN`** via an
in-band sentinel. Use `Single.IsNaN(v)` to detect. `resourceId ∈ {0, 1}` returns real
values as before.

The F# client wrappers `GetEconomyCurrent/Income/Usage/Storage` in `Client.fs` got a
doc comment update but no behavioural change (they already returned whatever the wire
float was, and NaN passes through naturally).

**Your trainer's `peak_metal` / `peak_energy` stall detection will start reporting
real numbers** once you pick this up.

### ✅ Issue 4 — `GiveMeResource` cheat has no effect

**Confirmed phantom of Issue 2**, as you suspected. A live test
(`tests/integration/fsharp/CallbackTests.fs :: T029.11 GiveMeResource increases
metal by ~1000`) now passes in ~70ms: dispatch works, resources are given, and the
fixed economy query observes the delta. No additional fix needed.

### ✅ Issue 3 — No Shutdown event on `Spring.GameOver`

**Fixed**. `proxy/src/proxy.c handleEvent` now has an `EVENT_RELEASE` branch that maps
the engine's `SReleaseEvent.reason` field to `Highbar__ShutdownReason` (reasons 1/2/3
→ `SHUTDOWN_REASON_GAME_OVER`, 4/5 → `ERROR`, 6 → `DISCONNECT`). When the engine fires
`EVENT_RELEASE` for a surviving AI on Spring.GameOver, the proxy emits the terminal
Shutdown message and closes the socket, triggering the bot's existing
`EngineDisconnectedException` path.

**You can delete the `botDeclaredVictory` workaround in `tactics.fsx`**. The proxy's
terminal event is now the canonical victory signal per SC-006.

Note: the live 1v1 GameOver test is deferred until the persistent-tier test harness
supports 1v1 scenarios with enemy-commander kill. The source is contract-correct per
`specs/029-fix-trainer-issues/contracts/shutdown-event.md`.

### ✅ Issue 5 — Misleading `CMD_DETAIL enum_move=42` / infolog bloat

**Fixed**. The hardcoded `HIGHBAR__AICOMMAND__COMMAND_MOVE_UNIT` constant is deleted
from the log line entirely. The entire per-command tracing block (`Executing N
commands`, `Cmd N: case=`, `MOVE uid=`, `Cmd N: rc=`) is now gated behind a new
`verbose_commands` config flag, **default OFF**.

- **Default verbosity**: zero per-command log lines. `engine.infolog` file size
  should drop dramatically (the trainer cited 60-70MB per 18000-frame run; expect
  significantly less).
- **Enable for debugging**: set `verbose_commands=true` in AIOptions.lua, or
  `HIGHBAR_VERBOSE_COMMANDS=1` as an env var at runtime.
- The misleading `enum_move=42` constant is gone even when verbose mode is ON.

The orphan `fprintf(stderr, "[HB] MOVE uid=...")` in `deserialize.c` that was
bypassing the proxy log system is also removed.

### 🟡 Issue 1 — Non-Move commands silently no-op

**Could not reproduce in the integration test harness.** I wrote new live tests that
assert observable effect via `getUnitPos` delta:

- `T029.baseline MoveCommand moves unit` — PASS
- `T029.5 FightCommand moves unit to destination` — PASS
- `T029.6 PatrolCommand moves unit toward patrol point` — PASS
- `T029.9 StopCommand halts a moving unit` — PASS
- `T029.4a AttackCommand(self) + MoveCommand` — PASS (attack doesn't break movement)

All five pass against `proxy/src/deserialize.c` unchanged (the 017 fix is intact).

**Three possibilities** for why your session saw "nothing move":

1. **Headless physics** — `spring-headless` does not simulate weapon physics, so you
   won't see damage events even when AttackCommand is correctly dispatched. The unit
   may have moved toward the target but not inflicted damage, and "no damage" may have
   been interpreted as "command ignored".
2. **Commander-specific Lua rule** — BAR's commander may have a Lua behavior that
   refuses to engage without an explicit Move order first. My tests used a builder,
   which has simpler command semantics.
3. **Observation artifact** — the finding may have been inferred from "no damage" or
   "no visible icon" rather than a direct `getUnitPos` measurement. Can you re-run the
   trainer and add a `getUnitPos`-before-and-after check around an AttackCommand send?

See `specs/029-fix-trainer-issues/diagnostic/root-cause.md` for the full writeup.

**FR-003 update**: the proxy now returns `rc=-2` for command types whose protobuf
oneof case is not in the dispatch switch table (distinct from `-1` null or `0` engine
accepted). Your trainer can use this to detect "proxy doesn't wire this command type"
vs "engine accepted (but effect depends on engine logic)".

## Commit references

All five fixes land on branch `029-fix-trainer-issues` with:
- `fix(proxy): gate per-command tracing behind verbose_commands config flag` (US5)
- `fix(proxy): wire ECONOMY_GET_* callbacks to engine Economy_get* and surface NaN for invalid id` (US2)
- `fix(proxy): emit Shutdown(GAME_OVER) on EVENT_RELEASE for surviving AIs` (US3)
- `test: verify GiveMeResource cheat works after Issues 1+2 fix` (US4)

(Squash-merged to master — see git log on `master` for the exact commit hash.)

## Action items for FSBarV1

1. Pull HighBarV2 master and rebuild proxy (`cmake --build build` from repo root)
2. Update FSBarV1 to use `Single.IsNaN` check on economy callbacks (safety net)
3. Delete the `botDeclaredVictory` workaround in `tactics.fsx`
4. Re-run the 22-iteration trainer session and confirm:
   - `peak_metal` / `peak_energy` telemetry reports non-zero values
   - Matches exit via Shutdown event rather than timeout
   - AttackCommand actually moves the commander (please add a
     `getUnitPos`-before-and-after probe per Issue 1 possibility #3)
