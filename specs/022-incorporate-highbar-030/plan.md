# Implementation Plan: Incorporate HighBarV2 030 proxy contract docs and parser corrections

**Branch**: `022-incorporate-highbar-030` | **Date**: 2026-04-12 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/022-incorporate-highbar-030/spec.md`

## Summary

HighBarV2 shipped two authoritative proxy contract documents (`shutdown-wire-shape.md`, `unwired-command-log.md`) and a diagnostic note for AttackCommand stationary-unit behaviour. The trainer's existing post-match `unwired_commands.json` parser in `bots/trainer/run.sh` is silently always-zero because it assumes `case=` carries an alphabetic command name and that `case=`/`rc=` share a line — both wrong per the upstream contract. This feature fixes that parser, points FSBarV1's local docs at the upstream contracts, closes the AttackCommand probe with a written decision, and sends an outbound mailbox acknowledgement. No `Protocol.fs` behavioural change.

Technical approach: surgical bash edit to `run.sh` (replace the regex-and-bucket loop with one targeting the always-on stderr line `[HB] unsupported command oneof case=<INT> (proxy switch table miss)`); add a small pure-bash fixture under `bots/trainer/tests/` so SC-001 is provable without a real engine session; add upstream-contract reference comments at two specific source locations; write one closure note and one outbound mailbox markdown file; verify `BarData` 1.0.2 is or isn't required.

## Technical Context

**Language/Version**: F# / .NET 10.0 (no F# changes in this feature — references-only) plus Bash for the trainer runner edit
**Primary Dependencies**: existing in-repo `FSBar.Client`, `FSBar.Proto`, `BarData` (NuGet from local store). No new dependencies.
**Storage**: filesystem only — JSONL frame logs and JSON metadata under `bots/runs/` (gitignored, unchanged from 020/021); `Mailbox/` for outbound report; `specs/022-incorporate-highbar-030/` for closure note.
**Testing**: bash-level fixture under `bots/trainer/tests/` exercising the parser against synthetic stderr/infolog inputs. No new xUnit tests (no F# code is changing). Existing F# test suites remain unchanged.
**Target Platform**: Linux (Arch / dev container). Same environment as 020/021.
**Project Type**: tooling / runner script + documentation references. No library surface change.
**Performance Goals**: N/A — parser runs once at end of a multi-minute trainer iteration. The new shell loop is O(lines-in-stderr) on a few-MB stream, identical to today's complexity.
**Constraints**: MUST NOT modify `src/FSBar.Client/Protocol.fs` shutdown synthesis logic (FR-008/SC-005). MUST NOT introduce a new dependency. MUST preserve "always emit `unwired_commands.json`" behaviour from feature 021.
**Scale/Scope**: ~30-line bash diff in `run.sh`, one fixture file (~200 bytes of synthetic log lines), one fixture-runner script (~30 lines), comment additions in `Protocol.fs` and `run.sh`, one closure note (~80 lines markdown), one outbound mailbox file (~60 lines markdown). No code generation.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle / Constraint | Status | Notes |
|---|---|---|
| I. Spec-First Delivery | **PASS** | Spec exists with 10 FRs, 5 SCs, 3 user stories. This plan maps every FR to a concrete artifact. |
| II. Compiler-Enforced Structural Contracts | **PASS (N/A)** | No F# public API surface changes. No `.fs`/`.fsi` edits. No surface-area baseline updates. The Protocol.fs comment-only addition required by FR-005 does NOT touch any signature — verified against the existing block at lines 56-70. |
| III. Test Evidence Is Mandatory | **PASS** | FR-004 mandates a parser fixture demonstrating non-zero count. SC-001 makes the step change (today=0 → fix=N) the verification metric. SC-002 covers the regression-on-zero case via the smoke runs we already produce. |
| IV. Observability and Safe Failure Handling | **PASS** | The `unwired_commands.json` report IS the observability surface for proxy/command coverage gaps. Today's silent zero is exactly the "swallowed signal" failure mode this principle prohibits — fixing it strengthens compliance. The fix must not crash on unknown integer cases (edge case in spec). |
| V. Scripting Accessibility | **PASS (N/A)** | No public F# API touched, so no FSI prelude / example script obligations triggered. The trainer is already a `dotnet fsi` script. |
| Engineering Constraint: F# on .NET exclusive | **PASS** | The runner is bash, which is the existing trainer harness layer (per 020/021 plans, accepted). No new languages introduced. |
| Engineering Constraint: `.fsi` for every public `.fs` module | **PASS (N/A)** | No `.fs` modules added or modified. |
| Engineering Constraint: surface-area baselines | **PASS (N/A)** | No public API surface change. SC-005 enforces the Protocol.fs Shutdown branch is byte-identical. |
| Engineering Constraint: dependencies minimized | **PASS** | Zero new dependencies. FR-010 explicitly handles the conditional `BarData` 1.0.2 question (default: not needed unless build proves otherwise). |
| Engineering Constraint: `dotnet pack` to local nupkg | **PASS (N/A)** | No new packable library. |
| Workflow gate: Plan defines `.fsi` contracts for new/changed public modules | **PASS (N/A)** | No new/changed public modules. |
| Workflow gate: Tasks include verification + `.fsi` tasks | **DEFER to /speckit.tasks** | Verification tasks will be enumerated under US1 (parser fixture) in tasks.md. No `.fsi` tasks needed. |

**Result**: All gates PASS or are correctly N/A. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/022-incorporate-highbar-030/
├── plan.md                          # This file
├── spec.md                          # Feature spec
├── research.md                      # Phase 0 — what we read upstream + what we ignored
├── data-model.md                    # Phase 1 — JSON shape of unwired_commands.json (revised)
├── quickstart.md                    # Phase 1 — how to verify the parser fix locally in <2 minutes
├── contracts/
│   └── unwired-commands-report.md   # FSBarV1-side contract for the JSON report shape
├── attack-command-closure.md        # FR-007 closure decision (default: close-with-reference)
├── checklists/
│   └── requirements.md              # Spec quality checklist (already present)
└── tasks.md                         # Phase 2 — produced by /speckit.tasks (NOT this command)
```

### Source Code (repository root)

```text
bots/trainer/
├── run.sh                           # EDIT — replace lines ~204-225 (parser block) + add upstream contract reference comment
└── tests/                           # NEW DIR
    ├── fixtures/
    │   ├── unwired_stderr.txt       # NEW — synthetic stderr with N `[HB] unsupported command oneof case=<INT>` lines
    │   └── unwired_verbose_infolog.txt  # NEW — synthetic verbose-mode infolog with `Cmd <N>: case=<INT>` / `Cmd <N>: rc=-2` pairs
    └── parser_unwired_test.sh       # NEW — feeds fixtures to the run.sh parser block, asserts JSON output

src/FSBar.Client/
└── Protocol.fs                      # EDIT — extend the existing Shutdown comment block (lines 56-70) to reference `shutdown-wire-shape.md`. NO behavioural change to the MessageCase.Shutdown branch.

Mailbox/
└── 2026-04-12_from_FSBarV1_030-integration-complete.md   # NEW — outbound acknowledgement (FR-009)
```

**Structure Decision**: Single-project repo with ad-hoc trainer subdirectory (`bots/trainer/`). The parser fix is in bash (the existing runner layer), the only F# touch is a comment expansion that adds no behaviour and no surface area, and the new test artefacts live alongside the script they exercise rather than under `tests/` (which is the xUnit tree for the F# libraries and would be the wrong home for a bash fixture). This mirrors the pattern established by feature 021.

## Phase 0 — Outline & Research

No NEEDS CLARIFICATION markers exist in the spec. The "research" for this feature is reading the upstream documents and recording what we are choosing to act on vs. ignore.

`research.md` will contain:

1. **Upstream contracts read** — `shutdown-wire-shape.md`, `unwired-command-log.md`, `attack-probe-verbose.md`, plus the response letter at `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md`.
2. **Upstream commits inspected** — `9a91b4a` (030 squash merge), `a1916e5` (BarData 1.0.2 / HighBar.Client 0.1.2 bump). Both confirmed via `git log` in `../HighBarV2`.
3. **Decisions**:
   - **D1: Parse the always-on stderr line, not verbose-mode infolog pairs.** Per `unwired-command-log.md` §"Parsing Guidance" Option A, the stderr line is "always available" and "recommended". Verbose-mode infolog parsing is a fallback we can add cheaply but the always-on line is the primary signal.
   - **D2: Key `by_case` by integer-as-string.** The proto field-number → command-name mapping requires reading `messages.proto` at parse time, which is out of scope. Keying by integer keeps the parser dependency-free; consumers can resolve names themselves if they care. Spec edge case "unknown integer must surface" is satisfied automatically.
   - **D3: Drop `BarData` 1.0.2 upgrade.** FSBarV1 currently has `BarData.1.0.0-dev.20260408T121533.nupkg`. The HighBarV2 bump to 1.0.2 is on the HighBar tree only; FSBarV1 builds successfully against its current package and the proto schema is generated locally under `src/FSBar.Proto/Generated/`. No build evidence forces the upgrade. Documented per FR-010.
   - **D4: Close-with-reference for AttackCommand (default per spec assumptions).** The re-probe shape requires `cheat|globallos` (a graphical-mode debug command), close-spawned units, and a 600-frame observation. The trainer's headless smoke rungs do not support that shape and adding it is out of scope. Closure cites the upstream diagnostic and the existing `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`.
   - **D5: Do not modify Protocol.fs Shutdown synthesis.** Locked by FR-008/SC-005. The only Protocol.fs edit is appending a one-line "See also" reference inside the existing doc-comment block at lines 56-70.
4. **Alternatives considered and rejected**:
   - Resolving integer field numbers to command names by reading `messages.proto` at parser time → rejected; introduces a proto-parsing dependency in bash for negligible benefit. Consumers reading `unwired_commands.json` can resolve names from the proto themselves.
   - Running the AttackCommand re-probe in this feature → rejected; out of scope for the current trainer harness shape and would block US1+US2 unnecessarily.
   - Bumping `BarData` to 1.0.2 preemptively → rejected; the upgrade chain (regenerate proto, retest 020/021 smoke runs) is larger than this feature's scope and there is no observed need.
   - Switching FSBarV1 to consume `HighBar.Client` from the local nupkg feed → rejected; explicitly excluded in spec assumptions.

**Output**: research.md (decisions D1-D5 + alternatives + upstream commit cross-references)

## Phase 1 — Design & Contracts

### Data model (`data-model.md`)

One entity: `unwired_commands.json` (revised shape).

```text
{
  "rc_minus_2_count": <int>,        // total stderr "[HB] unsupported command oneof case=<INT>" lines
  "by_case": {
    "<integer-as-string>": <int>,   // count per integer field number
    ...
  }
}
```

State transitions: none — write-once at end of each trainer iteration.

Validation rules (enforced by parser_unwired_test.sh):
- `rc_minus_2_count` is the sum of `by_case` values.
- File is always present (empty `by_case: {}` if no rejections).
- Unknown integer values surface as their raw integer; never throw, never coerce to "unknown".

### Contract (`contracts/unwired-commands-report.md`)

A FSBarV1-side mini-contract documenting:
- The JSON shape (matching data-model).
- The single source line on stderr that defines `rc_minus_2_count` (the always-on stderr line from `unwired-command-log.md`).
- A "See also" pointer to `../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md` as the upstream authoritative source.
- The parser corrections vs. feature 021's implementation (integer not string; separate lines in verbose mode).
- The intended consumer (trainer iteration loop / human reviewer) and how they should interpret a non-zero count.

### Quickstart (`quickstart.md`)

Three steps, each runnable on a fresh checkout in <2 minutes:

1. **Reproduce the bug today**: feed `bots/trainer/tests/fixtures/unwired_stderr.txt` through the *current* parser block on master and observe `rc_minus_2_count: 0`.
2. **Verify the fix**: same input, post-edit, observe `rc_minus_2_count: N` and `by_case: {"99": ..., ...}` matching the fixture.
3. **Verify zero-regression**: the existing smoke run script for BARb/dev still produces `rc_minus_2_count: 0` and an empty `by_case`.

### Closure note (`attack-command-closure.md`)

Decision: **close-with-reference** (per assumption D4). One-paragraph rationale, link to upstream diagnostic, link to outbound mailbox letter, explicit statement that no further FSBarV1-side action is planned in this feature. If the user overrides during /speckit.implement and asks for the re-probe, this file is replaced with the probe results table.

### Outbound mailbox (`Mailbox/2026-04-12_from_FSBarV1_030-integration-complete.md`)

Short report (~60 lines): acknowledges 030 contracts, confirms the parser fix landed (with the bug→fix delta), states the AttackCommand closure decision, notes that BarData 1.0.2 was not adopted and why, thanks them for the precise corrections.

### Agent context update

Run `.specify/scripts/bash/update-agent-context.sh claude` after Phase 1 artifacts are written. This adds the feature's "Active Technologies" line to `CLAUDE.md`. No new tech stack — the script will record the bash + filesystem context with the feature ID.

### Post-design Constitution Re-check

| Item | Status | Notes |
|---|---|---|
| Surface-area / `.fsi` impact | UNCHANGED | Still no F# behaviour changes. Phase 1 confirmed only the comment-block extension in Protocol.fs (lines 56-70) is in scope. |
| Test evidence path | CONFIRMED | Phase 1's `parser_unwired_test.sh` is the SC-001 verification artefact. |
| Observability | STRENGTHENED | The contract document under `contracts/` makes the `unwired_commands.json` report a first-class FSBarV1 surface, not just an undocumented runner side effect. |
| New dependencies | ZERO | Confirmed across all Phase 1 artefacts. |

**Result**: PASS. No new gate violations introduced by the design.

## Complexity Tracking

> No Constitution Check violations. Section intentionally empty.
