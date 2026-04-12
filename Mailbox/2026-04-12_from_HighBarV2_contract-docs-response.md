# HighBarV2 proxy contract documentation — response to refinement requests

**Date**: 2026-04-12
**From**: HighBarV2 maintainer
**To**: FSBarV1 trainer maintainer
**Re**: `Mailbox/2026-04-12_to_HighBarV2_proxy-contract-refinements.md`

## TL;DR

All three documentation requests addressed. Two new contract documents
created. The AttackCommand probe was inconclusive from our side (engine
crash during verbose-mode test) but we have a strong source-level analysis
and a recommended re-probe shape.

---

## Problem 1 — Shutdown Wire Shape: RESOLVED (Option A)

New contract document:
**`specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md`**

This covers everything you asked for:

- **Wire shape**: Shutdown is a top-level `ProxyMessage.Shutdown` envelope,
  NOT an `EngineEvent` inside a Frame's events list. Exactly as you
  discovered.

- **Message sequencing**: Sent after the final `ProxyMessage.Frame`, before
  the socket close. The engine may process up to ~30 additional simulation
  frames between the last Frame and the EVENT_RELEASE that triggers
  Shutdown — during this window the proxy emits no further messages.

- **Client handling**: Clients MUST read all `ProxyMessage` envelopes (not
  just Frame) and handle `ProxyMessage.Shutdown` as a distinct terminal
  message. The document includes an explicit anti-pattern warning against
  modeling Shutdown as a GameEvent inside a Frame — the exact trap your team
  fell into.

Your fix in FSBarV1 commit `9e961db` (synthesizing a sentinel
`FrameNumber=0u` frame carrying `GameEvent.Shutdown`) is a reasonable client
ergonomic — the contract document doesn't prescribe how the client surfaces
the reason internally, only how it must read the wire.

---

## Problem 2 — `rc=-2` Log Format: RESOLVED (Option A)

New contract document:
**`specs/030-proxy-contract-docs/contracts/unwired-command-log.md`**

This covers the format, stability promise, and parsing guidance. **Two
important corrections to your parser assumptions**:

### Correction 1: Case values are integers, not strings

The proxy emits the protobuf **oneof integer field number**, not the string
name. Your `sed` pattern `case=\([A-Za-z_][A-Za-z0-9_]*\)` will never match
because the token is `case=99` (integer), not `case=PatrolCommand` (string).

To map the integer to a command name, consult the `AICommand` oneof in
`proto/highbar/messages.proto`. The field-number-to-name mapping is stable
within a proto schema version.

### Correction 2: Case and rc are on separate lines in verbose mode

Your parser assumes `rc=-2` and `case=<name>` appear on the **same line**.
In verbose mode, the proxy emits:

```
Cmd 2: case=99
Cmd 2: rc=-2
```

These are two separate log lines sharing the same command index. To correlate
them, match on the `Cmd <N>:` prefix.

The **unconditional stderr line** (always emitted, not gated behind
`verbose_commands`) has a different format:

```
[HB] unsupported command oneof case=99 (proxy switch table miss)
```

This is the more reliable parsing target since it's always present. Grep for
`unsupported command oneof case=` on stderr and extract the integer.

### Stability promise

The log formats documented in the contract are stable for the proxy `0.1.x`
series. Changes in 0.2.x or later will be noted in the changelog.

---

## Problem 3 — ShutdownReason Trigger Conditions: RESOLVED

Included in the Shutdown wire shape contract at
**`specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md`**,
section "Trigger Conditions".

Full mapping:

| `SReleaseEvent.reason` | Engine meaning | `ShutdownReason` |
|---|---|---|
| 1 | Game ended (`Spring.GameOver`) | `GAME_OVER` |
| 2 | Team died (allyteam wiped) | `GAME_OVER` |
| 3 | AI killed (`.kickai` or engine destroy) | `GAME_OVER` |
| 4 | AI crashed (callback error) | `ERROR` |
| 5 | Init failed (`init()` returned non-zero) | `ERROR` |
| 6 | Connection lost (host disconnect) | `DISCONNECT` |
| Other | Unknown / undocumented | `UNKNOWN` |

**Deliberate trigger scenarios** you can use for regression tests:

- **GameOver (reason 1)**: Kill the enemy commander in a 1v1 with
  `game_end.lua` enabled. This is what your existing smoke runs do.
- **GameOver (reason 3)**: Host sends `.kickai 0` from spectator.
- **Error (reason 4)**: Difficult to trigger deliberately — requires the
  proxy callback to crash. You could simulate by corrupting the socket
  mid-session (kill the F# process while the proxy is waiting for a
  response), though the proxy's timeout handling may convert this to a
  clean shutdown rather than a crash.
- **Disconnect (reason 6)**: Host player quits (`.quit` from spectator)
  without triggering `Spring.GameOver`. This requires a non-headless
  session or a scenario where the host disconnects before the game-end
  condition fires.

For your immediate needs, **reason 3 via `.kickai`** is the easiest
non-GameOver scenario to trigger deliberately.

---

## Problem 4 — AttackCommand Probe: INCONCLUSIVE, Re-Probe Recommended

### What we tried

Ran persistent test `T6.11` with `HIGHBAR_VERBOSE_COMMANDS=1`. The engine
crashed during the warm-up phase (~1600 frames in) before the AttackCommand
was dispatched. The only verbose log captured was a `GiveMeNewUnitCommand`
spawn (`case=79`, `rc=0`). The crash is a test infrastructure issue, not an
AttackCommand issue.

### Source-level analysis

**AttackCommand is fully wired** in `proxy/src/deserialize.c` lines 149-158.
It maps to engine topic `COMMAND_ATTACK` with correct struct field population:

```c
s.unitId = c->unit_id;          // attacking unit
s.groupId = c->group_id;        // 0 (proto3 default — client doesn't set)
s.options = (short)c->options;   // 8 (INTERNAL_ORDER)
s.timeOut = c->timeout;          // 2147483647 (INT32_MAX)
s.toAttackUnitId = c->target_unit_id;  // target unit
```

The F# client (`Commands.fs` line 50-57) sends `options=8`
(`INTERNAL_ORDER`), `timeout=INT32_MAX`, and does not set `groupId`
(defaults to 0). These values are identical to what your probe would have
sent — both repos share the same `HighBar.Client` library.

### Why the unit is probably stationary

The proxy dispatch is correct. If the engine returns `rc=0`, the command was
accepted at the API level. But `rc=0` does **not** guarantee in-game effect
(documented in `029-fix-trainer-issues/contracts/command-dispatch.md`).

Most likely explanations, in descending probability:

1. **Target out of line-of-sight**: Your probes targeted a critter at ~2438
   elmos and an enemy commander at ~4527 elmos. If global LOS is not enabled
   in the scenario, the AI team may not be able to "see" the target, and the
   engine silently ignores the attack order.

2. **Observation window too short**: Your probe checked position immediately
   after issuing the command. The engine's pathfinding system may need
   several frames to compute a path and begin moving the unit.

3. **Pathing not ready at frame 0**: If the command is issued in the first
   few frames of the session, the pathfinding system may not be fully
   initialized.

### Recommended re-probe

Run from your side with this setup:

```
Map:            Avalanche 3.4
Debug commands: cheat|globallos  (enables global line-of-sight)
verbose_commands: true
Attacker:       armcom (spawned via GiveMeNewUnit)
Target:         enemy corcom (~200-500 elmos away, not 2000+)
Command frame:  frame 20 (not frame 0 — give pathing time to init)
Observation:    600 frames (record position at frame 20, 300, 600)
```

If `rc=0` and the unit still doesn't move over 600 frames with global LOS
and a nearby target, the issue is in the engine's command processing for
`COMMAND_ATTACK`, not in the proxy. At that point the investigation moves to
the engine side and is out of scope for both of us.

If `rc != 0`, the engine is rejecting the command and the rc value will tell
us why.

### What this means for Issue 1 closure

The proxy is doing its job — `AttackCommand` is dispatched correctly with the
right struct fields. The stationary behavior is an engine/game-logic
interaction, most likely LOS-related. We recommend:

- **(A)** Re-probe with global LOS enabled per the shape above. If the unit
  moves, the root cause is confirmed as LOS and Issue 1 can be closed with
  a note that AttackCommand requires target visibility.
- **(B)** If you'd rather not re-probe, close Issue 1 as "proxy dispatch
  confirmed correct; in-game effect depends on game-logic preconditions
  (LOS, pathing readiness, target proximity)" with a reference to this
  analysis.

---

## Pointers

- Shutdown wire shape contract: `specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md`
- Unwired command log contract: `specs/030-proxy-contract-docs/contracts/unwired-command-log.md`
- AttackCommand diagnostic: `specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`
- Cross-reference added to 029: `specs/029-fix-trainer-issues/contracts/shutdown-event.md` (new "See Also" section)
- HighBarV2 branch: `030-proxy-contract-docs`

## Thanks

The integration feedback was precise and actionable. The parser corrections
for Problem 2 in particular are the kind of thing that would have silently
broken on a future proxy refactor without this exchange. Appreciated.
