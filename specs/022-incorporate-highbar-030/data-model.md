# Data Model: Incorporate HighBarV2 030 proxy contract docs

**Feature**: 022-incorporate-highbar-030
**Date**: 2026-04-12

This feature has one in-scope data entity: the post-match `unwired_commands.json` report. Everything else is documentation references and a closure note.

## Entity: `unwired_commands.json` (revised shape)

**Location**: `bots/runs/<timestamp>_<rung>_<iter>/unwired_commands.json`
**Producer**: `bots/trainer/run.sh` post-match parser block (rewritten in this feature)
**Consumers**: human reviewer, future trainer iteration scripts that gate on protocol coverage

### Schema

```json
{
  "rc_minus_2_count": <int>,
  "by_case": {
    "<integer-as-string>": <int>
  }
}
```

### Field semantics

| Field | Type | Description |
|---|---|---|
| `rc_minus_2_count` | non-negative integer | Total number of `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` lines observed on `engine.stderr` for this match. Equals the sum of `by_case` values. |
| `by_case` | object (map) | Per-integer-case rejection counts. Keys are decimal integer strings (e.g., `"99"`, `"45"`). Values are non-negative integers ≥ 1. May be empty `{}` when no rejections occurred. |

### Validation rules (parser-enforced and asserted by `parser_unwired_test.sh`)

1. **File is always present.** Even when no rejections occurred, the file MUST exist with `{"rc_minus_2_count": 0, "by_case": {}}`. (Preserves the FR-003 always-emit invariant from feature 021.)
2. **Sum invariant.** `rc_minus_2_count == sum(by_case.values())`. If the parser cannot extract an integer for some line, that line is dropped from both — the count remains consistent.
3. **Integer keys only.** Keys in `by_case` MUST match `^[0-9]+$`. The parser MUST NOT emit alphabetic command-name keys; consumers needing names look them up against `proto/highbar/messages.proto` themselves.
4. **No throw on unknown integers.** A `case=99999` value the parser has never seen is accepted as `"99999": 1`. The parser does not maintain an allow-list of known integer values — defensive against forward compatibility per the spec edge case.
5. **No double-counting across streams.** The parser scans `engine.stderr` only for the always-on line. It does NOT also scan `engine.infolog` or `engine.stdout` for the same line, because the always-on line is emitted exactly once per unsupported dispatch on stderr.

### State transitions

None — write-once at end of each trainer iteration. The file is never read back, mutated, or merged with prior runs.

### Source line of truth (the input the parser scans)

The single source line on `engine.stderr` per unsupported command (per `unwired-command-log.md` §"Unconditional stderr line"):

```
[HB] unsupported command oneof case=<INT> (proxy switch table miss)
```

The parser regex: `^\[HB\] unsupported command oneof case=([0-9]+) `

The trailing space is intentional — guards against partial line reads and against the rare case of `case=99(proxy...)` without the documented separator.

## Out-of-scope entities

- The verbose-mode `Cmd <N>: case=<INT>` / `Cmd <N>: rc=<RC>` infolog pairs documented in `unwired-command-log.md` §"Verbose-mode infolog lines (optional)". The trainer does not enable `verbose_commands` in any current rung. If a future feature enables it, this entity can be revisited and the parser extended; the current report shape will be a strict subset of what verbose mode could surface.
- The `ShutdownReason` enum and its mapping. Documented authoritatively in `shutdown-wire-shape.md`; FSBarV1 already maps it to a string in `Protocol.fs:88-93`. No changes in this feature.
- The `result.json` schema produced by the bot itself. Owned by feature 020/021, untouched here.
