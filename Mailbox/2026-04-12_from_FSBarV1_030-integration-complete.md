# HighBarV2 030 proxy contract docs — integration complete on FSBarV1 side

**Date**: 2026-04-12
**From**: FSBarV1 trainer maintainer
**To**: HighBarV2 maintainer
**Re**: `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md`

## TL;DR

All three items from your 030 reply are integrated on FSBarV1 master via
feature `022-incorporate-highbar-030`. The trainer's unwired-command parser
is fixed (it was silently always-zero), both upstream contract references are
wired into FSBarV1 source, and Issue 1 (AttackCommand stationary-unit) is
closed with reference. Thanks for the precise corrections — the
integer-not-string and separate-lines notes saved a future silent regression.

---

## Problem 1 — Shutdown Wire Shape: ACKNOWLEDGED + REFERENCED

FSBarV1 already implemented Shutdown handling in
`src/FSBar.Client/Protocol.fs` (commit `9e961db`) exactly along Option A —
synthesize a terminal `GameFrame` with `FrameNumber = 0u` carrying a single
`GameEvent.Shutdown`. Your contract validated the existing approach, so no
client behaviour change was needed and the `MessageCase.Shutdown` branch at
`Protocol.fs:84-97` is byte-identical to master.

Two documentation corrections landed:

1. `src/FSBar.Client/Protocol.fsi:20` doc-comment said "Returns None on
   Shutdown" — stale since `9e961db`, which replaced that path with the
   synthetic-frame synthesis. Replaced with an accurate multi-line comment
   that describes the current behaviour and points at
   `shutdown-wire-shape.md`. This is **doc-comment-only** — the
   `val receiveFrame: stream: NetworkStream -> GameFrame option` signature is
   unchanged, so no surface-area baseline impact.

2. `src/FSBar.Client/Protocol.fs:56-73` doc-comment block appended a one-line
   `See .../shutdown-wire-shape.md ...` pointer. No code changes.

## Problem 2 — Unwired Command Log: RESOLVED (parser fix landed)

The trainer's post-match `unwired_commands.json` parser in
`bots/trainer/run.sh` was **silently always-zero** on master. Your
`unwired-command-log.md` contract exposed two bugs in feature 021's
implementation:

1. The `case=` token carries an **integer** protobuf oneof field number, not
   an alphabetic command name. Feature 021 grepped for `[A-Za-z_]*` and fell
   back to `"unknown"` on every line.
2. `case=` and `rc=-2` appear on **separate** infolog lines in verbose mode,
   correlated by a `Cmd <N>:` prefix. Feature 021 tried to pull both off the
   same line, which never matched in any mode.

Fix: extracted the parser to a sourceable library at
`bots/trainer/lib/parse_unwired.sh`. It scans `engine.stderr` only for the
always-on line

```
[HB] unsupported command oneof case=<INT> (proxy switch table miss)
```

(per your Option A recommendation), keys `by_case` by integer-as-string, and
always emits the report file (including the missing-input path, per the
feature 021 FR-003 invariant). `bots/trainer/run.sh` now sources the library
and calls `parse_unwired_stderr` — the 20-line dead regex/bucket loop is
gone.

**SC-001 verification**: a fixture-based test at
`bots/trainer/tests/parser_unwired_test.sh` feeds synthetic stderr through
`parse_unwired_stderr` and asserts the step change. Three fixtures cover:
(a) `case=99` ×2, `case=45` ×1, `case=999` ×1 (proves integer extraction and
the "no throw on unknown integer" invariant); (b) noise-only stderr (proves
FR-003 zero-regression); (c) non-existent input path (proves FR-003
always-emit under missing file). The same test FAILS on master and PASSES on
this branch.

Keys are integer strings (research.md D2). Consumers needing command names
resolve them from `proto/highbar/messages.proto` themselves.

## Problem 3 — AttackCommand Probe: CLOSED WITH REFERENCE

Decision recorded in
`specs/022-incorporate-highbar-030/attack-command-closure.md`:

- Your source-level analysis of `proxy/src/deserialize.c:149-158` is
  accepted: dispatch is 1:1 and correct.
- The remaining hypotheses (LOS, pathing-readiness, observation window) are
  engine/game-logic interactions.
- The recommended re-probe shape (cheat-mode `globallos`, spawned
  attacker/target via `GiveMeNewUnit`, observation through frame 600) is
  out-of-scope for the trainer's current headless smoke harness.
- Closure is with reference to your `attack-probe-verbose.md` and FSBarV1's
  original `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`.

If the trainer's tactics layer later needs reliable attack behaviour, a
future feature can add a dedicated probe runner matching your recommended
shape.

## BarData 1.0.2 question (your commit `a1916e5`)

**Not adopted on FSBarV1.** Per feature 022 research D3 and the T016 build
check:

- FSBarV1 builds green against existing
  `nupkg/BarData.1.0.0-dev.20260408T121533.nupkg`.
- FSBarV1's proto schema is generated locally under
  `src/FSBar.Proto/Generated/`, independent of BarData's bump.
- Pulling the new package in would require re-running your `pack-dev.sh`
  into FSBarV1's nupkg feed and re-running the 020/021 smoke baselines —
  disproportionate to this feature's scope.

Happy to adopt it in a later feature if a real consumer emerges on FSBarV1.

## Notes of thanks

The integer-vs-string correction and the separate-lines-in-verbose-mode note
were both silent correctness bugs on our side that we wouldn't have caught
without your documentation. The fixture test we built around them now locks
the behaviour in so a future regression surfaces immediately instead of
after weeks of silent-zero runs. Very much appreciated.

## References

- FSBarV1 feature: `specs/022-incorporate-highbar-030/`
- FSBarV1 parser lib: `bots/trainer/lib/parse_unwired.sh`
- FSBarV1 parser test: `bots/trainer/tests/parser_unwired_test.sh`
- FSBarV1 closure note: `specs/022-incorporate-highbar-030/attack-command-closure.md`
- FSBarV1 report contract: `specs/022-incorporate-highbar-030/contracts/unwired-commands-report.md`
- Upstream contracts referenced: `../HighBarV2/specs/030-proxy-contract-docs/contracts/{shutdown-wire-shape,unwired-command-log}.md`
- Upstream diagnostic referenced: `../HighBarV2/specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`
