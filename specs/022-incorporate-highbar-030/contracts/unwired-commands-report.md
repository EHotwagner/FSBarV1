# Contract: `unwired_commands.json` Report Shape

**Feature**: 022-incorporate-highbar-030
**Producer**: `bots/trainer/run.sh` (post-match parser block)
**Authoritative upstream source**: [`../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md`](../../../../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md)

## Overview

After every trainer iteration, `bots/trainer/run.sh` writes a JSON report listing every command the proxy rejected as "unsupported oneof case" during the match. The report is the FSBarV1 trainer's surface for detecting protocol coverage gaps between the F# client (`FSBar.Client`) and the C proxy (`HighBarV2/proxy`).

This contract supersedes the implicit shape introduced by feature 021 (`bots/trainer/run.sh` lines 204-225 in commit `876173f`), which incorrectly assumed:
- `case=` carries an alphabetic command name (it carries an integer)
- `case=` and `rc=-2` appear on the same log line (they are on separate lines in verbose mode, and `rc=-2` does not appear at all on the always-on stderr line)

The result was a parser that always reported `rc_minus_2_count: 0` regardless of actual proxy behaviour. This contract defines the corrected shape.

## Wire shape

```json
{
  "rc_minus_2_count": <int>,
  "by_case": {
    "<integer-as-string>": <int>
  }
}
```

| Field | Type | Description |
|---|---|---|
| `rc_minus_2_count` | non-negative integer | Total number of `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` lines on `engine.stderr` for this match. |
| `by_case` | object | Per-integer-case rejection counts. Keys are decimal integer strings; values are non-negative integers. May be empty `{}`. |

### Invariants

- The file is always present, even on a clean match (`{"rc_minus_2_count": 0, "by_case": {}}`).
- `rc_minus_2_count == sum(by_case.values())`.
- Keys in `by_case` always match `^[0-9]+$`. Never alphabetic.
- The parser never throws on a previously-unseen integer case.

## Source signal

Per the upstream `unwired-command-log.md` contract, every unsupported command produces exactly one stderr line:

```
[HB] unsupported command oneof case=<INT> (proxy switch table miss)
```

This line is emitted regardless of the `verbose_commands` AI option, making it the recommended primary parsing target. The FSBarV1 trainer scans `engine.stderr` only and matches lines against the regex `^\[HB\] unsupported command oneof case=([0-9]+) `.

## Mapping integer to command name

The integer is the `AICommand` protobuf oneof field number. Consumers needing the command name should look it up against `../HighBarV2/proto/highbar/messages.proto` (or FSBarV1's mirrored generated bindings under `src/FSBar.Proto/Generated/highbar/`). The trainer parser does NOT resolve names — keeping the parser dependency-free is a deliberate decision (see `research.md` D2).

## Stability

Tied to the upstream `unwired-command-log.md` stability promise: the stderr line format `[HB] unsupported command oneof case=%d (proxy switch table miss)` is stable for the proxy `0.1.x` series. If HighBarV2 changes the format in 0.2.x, this contract and the trainer parser must be revisited together.

## Consumer guidance

A non-zero `rc_minus_2_count` means the bot tried to issue a command type the proxy does not currently dispatch. The bot is not crashing — the proxy logs the rejection and returns `-2`. To resolve:

1. Look up each integer in `by_case` against `messages.proto` to identify the command type(s).
2. File an issue against HighBarV2 to wire the command in `proxy/src/deserialize.c`.
3. Until wired, the bot should avoid issuing that command type or fall back to a wired alternative.

A clean match has `rc_minus_2_count: 0` and an empty `by_case` — the canonical green state.

## Anti-patterns (do not do)

- **Do not** key `by_case` by alphabetic command names. Names require a proto lookup and are out of scope for the parser.
- **Do not** scan `engine.infolog` or `engine.stdout` in addition to `engine.stderr` for the always-on line — it would double-count if the engine ever mirrors stderr to those streams.
- **Do not** treat the absence of the file as success. The producer always emits it; absence means the runner crashed before reaching the parser block.

## See also

- Upstream: [`shutdown-wire-shape.md`](../../../../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md) — sister contract for the `Shutdown` envelope.
- FSBarV1: [`research.md`](../research.md) decisions D1, D2, D3 for the rationale behind the parser design.
- FSBarV1: [`data-model.md`](../data-model.md) for the same shape stated as a producer-side data model.
