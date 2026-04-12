# Tasks: Incorporate HighBarV2 030 proxy contract docs and parser corrections

**Input**: Design documents from `/specs/022-incorporate-highbar-030/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Test tasks are included because the spec mandates a parser fixture (FR-004) and SC-001 frames the fix as a step change provable only via fixture-based verification.

**Organization**: Tasks are grouped by user story (US1=P1, US2=P2, US3=P3). US1 is the MVP — landing it alone fixes the silent-zero parser bug.

**Note (post-`/speckit.analyze` remediation, 2026-04-12)**: This task list incorporates the C1, C2, C3 remediations from the analyze report — the parser is extracted to a sourceable library (`bots/trainer/lib/parse_unwired.sh`) so the fixture test consumes the same code as `run.sh`, and US2 fixes the stale `Protocol.fsi` doc-comment in addition to adding the upstream contract reference.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story (US1, US2, US3) — omitted for Setup, Foundational, and Polish phases
- File paths are absolute or repo-relative

## Path Conventions

This feature edits a small set of existing files plus a new test directory and a new lib directory under `bots/trainer/`. No new F# projects, no new packages. All paths are relative to the repo root `/home/developer/projects/FSBarV1/`.

---

## Phase 1: Setup

**Purpose**: Create the new directory layout for the parser library and the fixture-based test.

- [X] T001 Create directories `bots/trainer/lib/`, `bots/trainer/tests/`, and `bots/trainer/tests/fixtures/` (all new — none exist on master)

**Checkpoint**: Directories exist and are empty.

---

## Phase 2: Foundational (blocking prerequisites)

**Purpose**: None. This feature has no shared infrastructure that must land before user stories begin. US1, US2, and US3 are mostly file-disjoint and independently mergeable (the only cross-story constraint is T011 vs T006 — see "Cross-story dependency" below).

**Checkpoint**: N/A — proceed directly to Phase 3.

---

## Phase 3: User Story 1 — Trainer surfaces unwired commands accurately (P1) 🎯 MVP

**Goal**: Replace the silently-always-zero parser block in `bots/trainer/run.sh` (lines ~204-225) with one that targets the always-on stderr line `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` and reports counts in `unwired_commands.json` keyed by integer. The parser logic lives in a sourceable library (`bots/trainer/lib/parse_unwired.sh`) consumed by both `run.sh` and the fixture test.

**Independent Test**: Run `bash bots/trainer/tests/parser_unwired_test.sh` against the new fixtures. The test FAILS on master (silent zero) and PASSES on this branch. Optionally re-run a smoke trainer iteration and verify clean matches still produce `{"rc_minus_2_count": 0, "by_case": {}}`.

### Tests for User Story 1 (write before implementation)

- [X] T002 [P] [US1] Create fixture `bots/trainer/tests/fixtures/unwired_stderr.txt` containing:
  - At least three `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` lines covering at least three distinct integers, including one deliberately-high integer to exercise the "no throw on unknown integer case values" invariant from spec edge case "Mixed proto schema versions". Suggested: `case=99` twice, `case=45` once, `case=999` once (total 4 matches across 3 distinct integers).
  - A few non-matching noise lines (e.g., `[ENGINE] Loading unit defs`, `[HB] frame 1234 dispatched ok`, an `rc=-2` line that does NOT carry the `[HB] unsupported command oneof case=` prefix) to prove the regex doesn't over-match.
- [X] T003 [P] [US1] Create fixture `bots/trainer/tests/fixtures/unwired_stderr_empty.txt` containing only non-matching noise lines (no `[HB] unsupported command oneof case=` entries) — the no-rejection path used to verify FR-003 always-emit and SC-002 zero-regression.

### Implementation for User Story 1

- [X] T004 [US1] Create `bots/trainer/lib/parse_unwired.sh` (executable, designed to be sourced — no shebang execution path needed, but include `#!/usr/bin/env bash` for editor highlighting). Defines a single function `parse_unwired_stderr <stderr_path> <output_json_path>` that:
  - Greps `<stderr_path>` for `^\[HB\] unsupported command oneof case=([0-9]+) ` lines (note the trailing space — guards against `case=99(...)` without separator)
  - Extracts the integer with `sed -n 's/^\[HB\] unsupported command oneof case=\([0-9]\+\) .*/\1/p'`
  - Aggregates counts in a local associative array keyed by integer string
  - Emits `<output_json_path>` via `jq -n --argjson total "$total" --argjson by_case "$by_case_json" '{rc_minus_2_count: $total, by_case: $by_case}'`
  - Always emits the file (FR-003 invariant) even when the input has zero matches OR when `<stderr_path>` does not exist (writes `{"rc_minus_2_count": 0, "by_case": {}}` in both cases — never throws)
  - Returns 0 on success, non-zero on jq/IO failure
  - Header comment cites `specs/022-incorporate-highbar-030/contracts/unwired-commands-report.md` (FSBarV1-side contract) and `../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md` (upstream authoritative source), and notes the two parser corrections vs. feature 021's implementation: (1) `case=` carries an integer field number, not a string name; (2) in verbose mode `case=`/`rc=` are on separate lines correlated by `Cmd <N>:` prefix, but the always-on stderr line is the primary signal we parse. **This satisfies FR-006.**
- [X] T005 [US1] Create `bots/trainer/tests/parser_unwired_test.sh` (executable). It sources `bots/trainer/lib/parse_unwired.sh` directly (no fragment copying — the library is the same code `run.sh` consumes), then runs the following assertions:
  - Calls `parse_unwired_stderr bots/trainer/tests/fixtures/unwired_stderr.txt /tmp/unwired_test_out.json` and asserts the resulting JSON matches expected: `rc_minus_2_count == 4` and `by_case == {"99": 2, "45": 1, "999": 1}` (matching the fixture defined in T002 — adjust if T002 changes)
  - Asserts `by_case` includes the `"999"` key, explicitly verifying the "no throw on unknown integer" invariant
  - Calls `parse_unwired_stderr bots/trainer/tests/fixtures/unwired_stderr_empty.txt /tmp/unwired_test_out_empty.json` and asserts `rc_minus_2_count == 0` and `by_case == {}`
  - Calls `parse_unwired_stderr /nonexistent/path.txt /tmp/unwired_test_out_missing.json` and asserts the file is still emitted with `{"rc_minus_2_count": 0, "by_case": {}}` — FR-003 always-emit invariant under missing-input
  - Exits non-zero with a `diff` of expected vs actual JSON on any failure
  - Prints `PASS: parser_unwired_test` on success
- [X] T006 [US1] Edit `bots/trainer/run.sh` to replace the parser block at lines ~204-225 (the one starting with `# Post-match rc=-2 grep per 021 FR-004 / contracts delta Change 2.`):
  - Source the new library near the top of `run.sh` (after the `set -euo pipefail` and `SCRIPT_DIR` lines): `source "$SCRIPT_DIR/lib/parse_unwired.sh"`
  - Replace the entire parser block with two lines: `parse_unwired_stderr "$run_dir/engine.stderr" "$run_dir/unwired_commands.json"` followed by `echo "[run.sh] unwired_commands.json: rc_minus_2_count=$(jq -r '.rc_minus_2_count' "$run_dir/unwired_commands.json")"`
  - Remove the now-dead `declare -A rc_by_case`, the for-loop over infolog/stdout/stderr, and the `by_case_json` jq accumulation — they live in the library now
  - Preserve all other lines around the parser block byte-for-byte (the `newest_session` log copy block above and the `write_stub_if_missing` call below)
- [X] T007 [US1] Run `bash bots/trainer/tests/parser_unwired_test.sh` and confirm `PASS: parser_unwired_test`. Iterate on T004/T005/T006 if any assertion fails. Also run `bash -n bots/trainer/run.sh` to syntax-check the modified script.
- [X] T008 [US1] (Optional smoke validation) Run `bash bots/trainer/run.sh NullAI smoke-022` once and verify the resulting `bots/runs/<latest>/unwired_commands.json` is `{"rc_minus_2_count": 0, "by_case": {}}`. Skip if no engine is reachable; the fixture test in T007 already proves SC-001, the empty-fixture path in T005 proves SC-002, and the missing-file path proves FR-003.

**Checkpoint**: SC-001 (step-change provable from fixtures), SC-002 (no regression on zero-rejection runs), and FR-001/FR-002/FR-003/FR-004/FR-006 are all met. The MVP is shippable here.

---

## Phase 4: User Story 2 — Authoritative contract docs are referenced from FSBarV1 (P2)

**Goal**: Fix the stale `Protocol.fsi` doc-comment (which says "Returns None on Shutdown" but the implementation never does that post-`9e961db`), add upstream contract references at three source locations (`Protocol.fsi`, `Protocol.fs`, `run.sh`), and verify nothing else moves.

**Independent Test**:
1. `grep -n "shutdown-wire-shape.md" src/FSBar.Client/Protocol.fsi src/FSBar.Client/Protocol.fs` returns at least one hit per file.
2. `grep -n "unwired-command-log.md" bots/trainer/lib/parse_unwired.sh` returns at least one hit (this is FR-006, satisfied by T004's header comment, and re-verified here).
3. `grep "Returns None on Shutdown" src/FSBar.Client/Protocol.fsi` returns ZERO matches (SC-006).
4. `dotnet build src/FSBar.Client/FSBar.Client.fsproj` succeeds.

### Implementation for User Story 2

- [X] T009 [P] [US2] Edit `src/FSBar.Client/Protocol.fsi` line ~20. Replace the existing single-line doc-comment `/// Receive one frame from the proxy. Returns None on Shutdown.` with a multi-line doc-comment that:
  - Describes current behaviour: returns `Some frame` for normal Frame messages AND for the proxy's standalone Shutdown envelope (which is synthesized into a sentinel `GameFrame` with `FrameNumber = 0u` carrying a single `GameEvent.Shutdown` event); raises `EngineDisconnectedException` on a clean socket close without a Shutdown envelope
  - Notes that callers needing the last real game-frame number must rewrite the sentinel
  - Adds the line `/// See ../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md for the upstream wire-shape contract.`
  - Does NOT change the `val receiveFrame: stream: NetworkStream -> GameFrame option` signature line itself (no surface-area baseline impact)
  This satisfies FR-005 and SC-006.
- [X] T010 [P] [US2] Edit `src/FSBar.Client/Protocol.fs` doc-comment block at lines ~56-70 (the existing block immediately above `let rec receiveFrame`). Append one line at the end of the block: `/// See ../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md for the authoritative wire-shape contract.` Do NOT edit any code in the `MessageCase.Shutdown` branch (lines ~84-97). FR-008/SC-005 lock that branch as byte-identical.
- [X] T011 [US2] Edit `bots/trainer/run.sh` immediately above the `source "$SCRIPT_DIR/lib/parse_unwired.sh"` line added in T006. Add a 3-5 line comment block that points readers at the library's header comment for the upstream-contract corrections: `# Unwired-command parsing lives in lib/parse_unwired.sh — see that file's header for` / `# the upstream contract reference and the parser corrections vs. feature 021.` This is a thin pointer; the substantive reference comment is in T004's library header (FR-006 is already satisfied by T004; this comment helps source-level readers find it). **MUST run after T006**, see "Cross-story dependency" below.
- [X] T012 [US2] Run all three SC-003/SC-006 grep verifications:
  - `grep -n "shutdown-wire-shape.md" src/FSBar.Client/Protocol.fsi src/FSBar.Client/Protocol.fs` — both files must hit
  - `grep -n "unwired-command-log.md" bots/trainer/lib/parse_unwired.sh` — must hit
  - `grep "Returns None on Shutdown" src/FSBar.Client/Protocol.fsi` — must return ZERO matches
  All three checks must pass before this task is marked done.
- [X] T013 [US2] Run `dotnet build src/FSBar.Client/FSBar.Client.fsproj` and confirm a green build. The doc-comment-only edits in T009/T010 must not change any signature or break `.fsi`/`.fs` conformance. This also implicitly verifies FR-010 (BarData package sufficiency) — see also T016.

**Checkpoint**: SC-003 met (both upstream contracts referenced from FSBarV1 source). SC-006 met (stale doc gone). FR-005, FR-006 met. No behavioural change.

---

## Phase 5: User Story 3 — AttackCommand stationary-unit issue closed with a documented decision (P3)

**Goal**: Close Issue 1 with a written decision (default: close-with-reference) and record it in this feature's directory.

**Independent Test**: `cat specs/022-incorporate-highbar-030/attack-command-closure.md` shows the chosen path, rationale, and links to the upstream diagnostic + the original outbound mailbox letter.

### Implementation for User Story 3

- [X] T014 [US3] Verify `specs/022-incorporate-highbar-030/attack-command-closure.md` (already drafted in `/speckit.plan` Phase 1) is up to date and matches reality. Confirm it: (a) records the close-with-reference decision; (b) links `../HighBarV2/specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`; (c) links `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`; (d) states explicitly "no further FSBarV1-side action is planned in this feature". If the maintainer prefers the re-probe path instead, replace the file with a probe-results table per spec acceptance scenario US3.2 — but default is to leave the close-with-reference content as written.

**Checkpoint**: SC-004 met (Issue 1 resolved in exactly one place with rationale and upstream link). FR-007 met.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verify the no-touch invariants, decide the BarData question explicitly, send the outbound mailbox acknowledgement, and run the full quickstart.

- [X] T015 [P] Verify SC-005 by running `git diff master..022-incorporate-highbar-030 -- src/FSBar.Client/Protocol.fs`. Confirm the diff contains only additions inside the existing doc-comment block at lines ~56-70 (T010's one line). The `MessageCase.Shutdown` branch (lines ~84-97) MUST be byte-identical. If anything else changed, revert it before proceeding. Also run `git diff master..022-incorporate-highbar-030 -- src/FSBar.Client/Protocol.fsi` and confirm the diff is doc-comment-only (no signature line edits).
- [X] T016 [P] Verify FR-010 (BarData 1.0.2 question). The build in T013 already exercises this — if T013 was green against the current `nupkg/BarData.1.0.0-dev.20260408T121533.nupkg`, append a one-line note to `specs/022-incorporate-highbar-030/research.md` under D3 confirming "build verified green on 2026-04-12 against existing BarData package; no upgrade needed". If T013 failed because of a missing BarData type, escalate: copy the latest BarData nupkg from `../HighBarV2/data/bar/bin/Release/` into `nupkg/`, update D3, re-run, and document the new package version under FR-010.
- [X] T017 Create outbound mailbox file `Mailbox/2026-04-12_from_FSBarV1_030-integration-complete.md` (FR-009). Contents:
  - Acknowledge the HighBarV2 030 reply (`Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md`)
  - Confirm the parser fix landed: name the bug (silently-always-zero), the fix (target the always-on stderr line, key by integer, parser extracted to `bots/trainer/lib/parse_unwired.sh`), and cite SC-001 as the verification metric
  - Confirm the stale `Protocol.fsi` doc-comment ("Returns None on Shutdown") was fixed and replaced with an accurate description of the post-`9e961db` synthesis behaviour, plus a reference to `shutdown-wire-shape.md`. Note that this is doc-comment-only (no signature change, no surface-area baseline impact)
  - Confirm `Protocol.fs` `MessageCase.Shutdown` branch is byte-identical (FR-008/SC-005), with a one-line "the upstream contract validated our existing approach — no client behaviour change needed"
  - State the AttackCommand closure decision (close-with-reference) and link to `specs/022-incorporate-highbar-030/attack-command-closure.md`
  - State the BarData decision (not adopted; build green against existing package) per the result of T016
  - Thank the maintainer for the precise corrections (the integer-not-string + separate-lines correction in particular saved a future silent regression)
- [X] T018 Run the full feature verification checklist from `quickstart.md` end-to-end and tick off each SC mapping in the table. Any failing assertion blocks the merge. The mapping table in `quickstart.md` may need a one-line update to add SC-006 (the stale-doc-comment grep).

**Checkpoint**: All FRs and SCs verified. Feature is mergeable.

---

## Dependencies & Execution Order

### Phase ordering

- **Phase 1 (Setup)** → must complete first (creates the new lib + test directories).
- **Phase 2 (Foundational)** → empty; skipped.
- **Phase 3 (US1)** → independent; depends only on Phase 1.
- **Phase 4 (US2)** → mostly independent; T011 has a cross-story dependency on T006 (see below). T009, T010, T012, T013 are all parallel-safe with US1.
- **Phase 5 (US3)** → independent; the closure note already exists from /speckit.plan, so this phase is a verification step. Can run in parallel with Phases 3 and 4.
- **Phase 6 (Polish)** → depends on US1, US2, US3 all being complete.

### Story dependencies

- **US1 (P1, MVP)**: no upstream story dependencies. Files: `bots/trainer/run.sh`, `bots/trainer/lib/parse_unwired.sh`, `bots/trainer/tests/`. **File-disjoint from US3. Touches `run.sh` like US2 (T011)**, so T011 must land after T006.
- **US2 (P2)**: no upstream story dependencies. Files: `src/FSBar.Client/Protocol.fsi` (doc-comment fix + reference), `src/FSBar.Client/Protocol.fs` (comment-only addition), `bots/trainer/run.sh` (one comment pointer added by T011). **Touches `run.sh` like US1**, so T011 has a cross-story sequencing constraint.
- **US3 (P3)**: no upstream story dependencies. Files: `specs/022-incorporate-highbar-030/attack-command-closure.md` (already drafted). **Independent.**

### Within-story dependencies

- **US1**: T002 and T003 are file-disjoint and parallelizable. T004 (parser library) has no dependency on the fixtures but is the prerequisite for both T005 (test consumes the library) and T006 (run.sh sources the library). T005 depends on T002 + T003 + T004. T006 depends on T004. T007 depends on T005 + T006. T008 is optional and depends on T006.
- **US2**: T009 and T010 are file-disjoint and parallelizable. T011 must run after T006 (cross-story constraint — same file). T012 depends on T009 + T010 + T011 + T004 (the grep checks all four locations). T013 depends on T009 + T010 (the build verifies the doc-comment edits compile).
- **US3**: T014 has no dependencies (the file already exists from /speckit.plan).
- **Polish**: T015 and T016 are independent and parallelizable. T015 depends on US1 and US2 being complete (so the diff is meaningful). T016 depends on T013 (build result feeds the BarData decision). T017 depends on T016 (its content cites the BarData decision). T018 depends on everything else.

### Cross-story dependency

T011 (US2) → must run AFTER T006 (US1) because both edit `bots/trainer/run.sh`. This is the only cross-story ordering constraint. T011 is intentionally NOT marked `[P]` for this reason.

## Parallel Execution Examples

### Within US1 — fixture creation

```text
# After T001 completes, run in parallel:
T002 [P] [US1] Create unwired_stderr.txt
T003 [P] [US1] Create unwired_stderr_empty.txt
```

### Across stories — US2 documentation edits + US3 verification

```text
# After T001, run in parallel with US1's T002–T008:
T009 [P] [US2] Edit Protocol.fsi (doc-comment fix + reference)
T010 [P] [US2] Edit Protocol.fs (append reference line)
T014   [US3] Verify attack-command-closure.md
```

### Polish phase

```text
# After all user stories are complete:
T015 [P] Verify Protocol.fs/.fsi diff scope (SC-005)
T016 [P] Verify BarData decision (build check)
# Then T017 (depends on T016), T018 sequentially.
```

## Implementation Strategy

### MVP scope

**User Story 1 alone is the MVP.** Landing US1 fixes the silent-correctness bug that motivated this feature — every other piece (doc references, the stale Protocol.fsi doc-comment, the closure note, the mailbox acknowledgement) is doc hygiene that does not change runtime behaviour. If the maintainer wants to ship a minimum-risk PR, T001-T008 + T015 + a brief commit message is enough.

**However**, the C2 finding (the stale `Protocol.fsi` "Returns None on Shutdown" doc) is a hidden defect that misleads any API consumer reading IntelliSense. Strongly recommend bundling US2 into the same PR even when shipping the MVP — T009 alone fixes the misleading contract and is a 5-minute edit.

### Recommended sequencing for a single-PR delivery

1. T001 (setup)
2. T002, T003 in parallel (fixtures)
3. T004 (parser library — the new shared module)
4. T005 (test script — sources the library)
5. T006 (run.sh edit — also sources the library)
6. T007 (verify fix passes)
7. T009, T010, T014 in parallel (US2 .fsi/.fs edits, US3 closure verification)
8. T011 (run.sh comment — cross-story, must follow T006)
9. T012, T013 (US2 grep verification + build)
10. T015, T016 in parallel (polish verification)
11. T017 (mailbox)
12. T018 (full quickstart run)

This sequence respects the only cross-story constraint (T011 after T006) and parallelizes wherever possible.

### Independent test criteria summary

| Story | Independent test |
|---|---|
| US1 | `bash bots/trainer/tests/parser_unwired_test.sh` prints `PASS: parser_unwired_test`. Same script FAILS on master. |
| US2 | `grep -n "shutdown-wire-shape.md" src/FSBar.Client/Protocol.fsi src/FSBar.Client/Protocol.fs` returns ≥1 hit per file. `grep "Returns None on Shutdown" src/FSBar.Client/Protocol.fsi` returns ZERO matches (SC-006). `grep -n "unwired-command-log.md" bots/trainer/lib/parse_unwired.sh` returns ≥1 hit. `dotnet build src/FSBar.Client/FSBar.Client.fsproj` succeeds. |
| US3 | `cat specs/022-incorporate-highbar-030/attack-command-closure.md` shows decision + upstream link + mailbox link + "no further action" statement. |

### Task count summary

| Phase | Tasks |
|---|---|
| Phase 1 (Setup) | 1 |
| Phase 2 (Foundational) | 0 |
| Phase 3 (US1) | 7 |
| Phase 4 (US2) | 5 |
| Phase 5 (US3) | 1 |
| Phase 6 (Polish) | 4 |
| **Total** | **18** |

| Parallelizable | Count |
|---|---|
| Tasks marked [P] | 6 (T002, T003, T009, T010, T015, T016) |
