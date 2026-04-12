# Quickstart: Verify the unwired-command parser fix

**Feature**: 022-incorporate-highbar-030
**Audience**: a future maintainer (or Claude in a future session) verifying that the parser fix is real and SC-001 is met.
**Time**: ~2 minutes on a fresh checkout.

## What you are verifying

The trainer's post-match `unwired_commands.json` parser used to silently report `rc_minus_2_count: 0` regardless of whether the proxy actually rejected commands, because it grepped for an alphabetic `case=<NAME>` pattern that the proxy never emits. This feature replaces it with a parser that targets the always-on stderr line `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` and reports counts keyed by integer.

## Prerequisites

- Repo checked out at branch `022-incorporate-highbar-030` (post-implementation).
- `jq` installed (already required by the trainer).

## Step 1 — Inspect the fixture

```bash
cat bots/trainer/tests/fixtures/unwired_stderr.txt
```

You should see ~5 lines, including at least three `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` entries with two distinct integers (e.g., `case=99` twice and `case=45` once). Some non-matching noise lines are mixed in to prove the parser doesn't over-match.

## Step 2 — Run the parser test

```bash
bash bots/trainer/tests/parser_unwired_test.sh
```

Expected output (last line):
```
PASS: parser_unwired_test
```

The script:
1. Feeds `unwired_stderr.txt` through the parser block extracted from `bots/trainer/run.sh`.
2. Asserts `rc_minus_2_count` matches the count of `[HB] unsupported command oneof case=` lines in the fixture.
3. Asserts `by_case` keys are decimal integer strings and values sum to `rc_minus_2_count`.
4. Asserts the file is always emitted (even when fed an empty fixture).

If any assertion fails the script exits non-zero with a diff between expected and actual.

## Step 3 — (Optional) Verify against a real run

If you have a recent BARb/dev smoke run under `bots/runs/`, inspect its `unwired_commands.json`:

```bash
ls -1t bots/runs/ | head -1
cat "bots/runs/$(ls -1t bots/runs/ | head -1)/unwired_commands.json"
```

A clean smoke match should report `{"rc_minus_2_count": 0, "by_case": {}}`. This is SC-002 — the no-regression guarantee for matches with no rejections.

## Step 4 — Reproduce the bug on master (optional, instructive)

To convince yourself the fix actually changes behaviour:

```bash
git stash                             # set aside your tree
git checkout master -- bots/trainer/run.sh
bash bots/trainer/tests/parser_unwired_test.sh
```

Expected: the test FAILS — the master parser produces `rc_minus_2_count: 0` against the same fixture, demonstrating the silent-zero bug.

Restore:
```bash
git checkout 022-incorporate-highbar-030 -- bots/trainer/run.sh
git stash pop
```

## Tying back to success criteria

| SC | Verified by |
|---|---|
| SC-001 | Steps 1-2 above (non-zero count from a representative input). |
| SC-002 | Step 3 against a clean smoke run, OR feeding an empty stderr fixture in step 2. |
| SC-003 | `grep -n "unwired-command-log.md" bots/trainer/run.sh` and `grep -n "shutdown-wire-shape.md" src/FSBar.Client/Protocol.fs` should each return at least one hit. |
| SC-004 | `cat specs/022-incorporate-highbar-030/attack-command-closure.md` shows the close-with-reference decision and an upstream link. |
| SC-005 | `git diff master..022-incorporate-highbar-030 -- src/FSBar.Client/Protocol.fs` shows ONLY comment additions in the lines 56-70 doc block; the `MessageCase.Shutdown` branch (lines 84-97) is unchanged. |
