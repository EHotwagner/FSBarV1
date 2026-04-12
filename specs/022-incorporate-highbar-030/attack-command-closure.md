# Closure Note: AttackCommand stationary-unit issue (Issue 1)

**Feature**: 022-incorporate-highbar-030
**Date**: 2026-04-12
**Decision**: **Close with reference. No re-probe in this feature.**

## Background

FSBarV1 raised the AttackCommand "stationary unit" concern as Issue 1 in `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`. The trainer observed that issuing an `AttackCommand` against a target unit resulted in `rc=0` from the proxy but no observable in-game movement of the attacking unit.

## What HighBarV2 found

Per `../HighBarV2/specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`:

1. **Proxy dispatch is correct.** The HighBarV2 maintainer verified at the source level that `proxy/src/deserialize.c:149-158` populates `SAttackUnitCommand` 1:1 from the protobuf message with the correct `unit_id`, `target_unit_id`, `options=8 (INTERNAL_ORDER)`, and `timeout=INT32_MAX`. There is no field-mapping bug.

2. **The verbose-mode probe was inconclusive on their side.** A test session (`T6.11`) crashed during the warm-up phase before the AttackCommand was dispatched. The crash is a test infrastructure issue, not an AttackCommand issue.

3. **The most likely root causes are engine/game-logic interactions, not the proxy.** In descending probability:
   - Target out of line-of-sight (the trainer probed targets at 2438 and 4527 elmos; without global LOS, the AI team cannot "see" them and the engine silently ignores attack orders).
   - Pathfinding not initialized at frame 0 (the probe issued the command very early in the session).
   - Observation window too short (the probe checked position immediately after issuing the command; pathing may need many frames before producing visible movement).

## Why close, not re-probe

The recommended re-probe shape requires:

- BAR cheat-mode debug command `cheat|globallos`
- Spawned attacker + spawned target via `GiveMeNewUnit`, 200-500 elmos apart
- Command issued at frame ~20 (not frame 0)
- Position observation through frame 600 (record at 20, 300, 600)

The FSBarV1 trainer's current headless smoke harness does not support cheat-command injection or staged attacker/target pairs. Adding that probe shape would be a 1-2 day side-feature with no impact on the trainer iteration loop.

The HighBarV2 maintainer also notes that even if the re-probe shows `rc=0` and the unit still doesn't move with global LOS and a nearby target, the next escalation is engine-side (`COMMAND_ATTACK` handling in the BAR engine itself) and out of scope for both repos.

Given:
- The proxy dispatch is verified correct at the source level by the upstream maintainer.
- The remaining hypotheses are engine/game-logic interactions FSBarV1 cannot test from its current harness.
- The trainer loop is not blocked by this — the bot's tactics layer can choose other commands when AttackCommand fails to produce movement.

…closing with reference is the right call.

## Closure statement

**Issue 1 from `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md` is closed.** The proxy dispatch for `AttackCommand` is verified correct. Stationary in-game behaviour is attributed to game-logic preconditions (most likely line-of-sight, possibly pathing-readiness or observation window). No further FSBarV1-side investigation is planned in this feature.

If the trainer's tactics layer needs reliable attack behaviour in a future iteration, the recommended path is to add a probe runner (separate feature) that uses BAR cheat-mode to enable global LOS and stages attacker/target pairs at close range — per the upstream `attack-probe-verbose.md` recommended shape. That probe could either confirm the LOS hypothesis (closing the line of investigation entirely) or escalate to engine-side analysis.

## References

- Upstream diagnostic: `../HighBarV2/specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`
- Upstream contract (sister doc): `../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md`
- Original outbound report: `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`
- HighBarV2 reply (the trigger for this feature): `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md`, §"Problem 4 — AttackCommand Probe"
- HighBarV2 source reference: `../HighBarV2/proxy/src/deserialize.c` lines 149-158 (`HIGHBAR__AICOMMAND__COMMAND_ATTACK` case)
