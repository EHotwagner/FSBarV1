# Feature Specification: Incorporate HighBarV2 030 proxy contract docs and parser corrections

**Feature Branch**: `022-incorporate-highbar-030`
**Created**: 2026-04-12
**Status**: Draft
**Input**: User description: "highbar has been updated and left a report in our mailbox. incorporate the findings and changes. also research the latest git commits of highbar."

## Context

HighBarV2 responded to FSBarV1's `Mailbox/2026-04-12_to_HighBarV2_proxy-contract-refinements.md` request with a substantial reply at `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md`. Recent HighBarV2 commits (`9a91b4a feat: merge 030-proxy-contract-docs` and `a1916e5 chore: bump BarData to 1.0.2 and HighBar.Client to 0.1.2`) shipped two new authoritative contract documents and a diagnostic note:

- `specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md` — confirms `Shutdown` is a top-level `ProxyMessage` envelope (not a `Frame.Event`), specifies the `SReleaseEvent.reason` → `ShutdownReason` mapping, and lists deliberate trigger scenarios.
- `specs/030-proxy-contract-docs/contracts/unwired-command-log.md` — corrects two parser assumptions FSBarV1 baked into `bots/trainer/run.sh`: the `case=` token is the **integer** oneof field number (not the string name), and in verbose mode `case=` and `rc=` appear on **separate lines** correlated by `Cmd <N>:` prefix. Names a stable always-on stderr line: `[HB] unsupported command oneof case=<INT> (proxy switch table miss)`.
- `specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md` — concludes the AttackCommand "stationary unit" issue is almost certainly a line-of-sight / pathing-readiness issue, not a proxy bug. Recommends a specific re-probe shape (`cheat|globallos`, target ~200–500 elmos away, command at frame ~20, 600-frame observation).

This feature lands those corrections in FSBarV1 and decides on a path forward for the AttackCommand probe.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Trainer surfaces unwired commands accurately (Priority: P1)

The trainer's post-match `unwired_commands.json` report (added in feature 021) currently uses a `sed` pattern that only matches alphabetic case names. With HighBarV2's clarification that the proxy emits **integers** (and that `case=`/`rc=` are on separate lines in verbose mode), the existing parser will never match a real proxy line and silently reports `rc_minus_2_count=0` even when commands are being rejected. The trainer maintainer needs the report to actually count and group rejections so iterative bot tuning can detect protocol gaps.

**Why this priority**: This is a silent-correctness bug — an existing report whose output is structurally wrong. Anyone relying on `unwired_commands.json` to gate iteration is being misled today, including the smoke baselines we just shipped.

**Independent Test**: Run a trainer match (any rung), then verify `runs/<...>/unwired_commands.json` correctly reports `rc_minus_2_count` and `by_case` keyed by the integer field number from the always-on stderr line `[HB] unsupported command oneof case=<INT>`. Force at least one synthetic miss (or use a fixture) to confirm a non-zero count is reported when one occurs.

**Acceptance Scenarios**:

1. **Given** a match where the proxy emits one or more `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` stderr lines, **When** the run finishes, **Then** `unwired_commands.json` reports `rc_minus_2_count` equal to the number of such stderr lines and `by_case` keyed by the integer `<INT>` value.
2. **Given** a match with no unsupported commands, **When** the run finishes, **Then** `unwired_commands.json` reports `rc_minus_2_count: 0` with an empty `by_case` object (preserving today's "always emit" behaviour).
3. **Given** a verbose-mode run that produces correlated `Cmd <N>: case=<INT>` and `Cmd <N>: rc=-2` infolog pairs, **When** the parser inspects the infolog, **Then** the same total counts and per-integer grouping hold (the verbose path agrees with the always-on stderr path).

---

### User Story 2 — Authoritative contract docs are referenced from FSBarV1 (Priority: P2)

FSBarV1's existing local docs (the 021-rerun-trainer-highbar contracts and the older 016/017 client docs) describe the Shutdown wire shape and the reason mapping based on FSBarV1's investigation. HighBarV2 now publishes authoritative contract documents that supersede FSBarV1's local descriptions. The FSBarV1 maintainer wants the local docs to point at the upstream contract files so future readers (and Claude in future sessions) follow the source-of-truth, not the older derived notes.

**Why this priority**: Documentation hygiene. No runtime impact, but reduces drift risk and prevents the team from re-deriving knowledge that's already settled upstream.

**Independent Test**: Open the FSBarV1 docs for shutdown handling and unwired-command logging. Verify each contains a "See also" / "Authoritative source" pointer to the matching `specs/030-proxy-contract-docs/contracts/*.md` file in `../HighBarV2`, with a one-line summary of what the upstream doc adds beyond the local notes.

**Acceptance Scenarios**:

1. **Given** the FSBarV1 client-side `Protocol.fs` shutdown synthesis comment block, **When** a future maintainer reads it, **Then** it references the upstream `shutdown-wire-shape.md` contract by path.
2. **Given** the trainer's `unwired_commands.json` parser, **When** a future maintainer reads the surrounding comments, **Then** they reference the upstream `unwired-command-log.md` contract by path and explain the "integer not string" and "separate lines" parser corrections.

---

### User Story 3 — AttackCommand "stationary unit" issue closed with a documented decision (Priority: P3)

FSBarV1 raised the AttackCommand stationary-unit concern as Issue 1 in the contract refinement letter. HighBarV2's source-level analysis confirms the proxy dispatch is correct and that the most likely root cause is line-of-sight or pathing-readiness, not a proxy bug. The maintainer wants the issue resolved with a clear written decision: either re-probe per the recommended shape and document the result, or close the issue as "proxy dispatch verified; in-game effect depends on game-logic preconditions" with a reference to the upstream diagnostic.

**Why this priority**: Investigative loose-end. Doesn't block the trainer loop (T6.11 already passes the proxy-side verification path), but should not be left open indefinitely.

**Independent Test**: Read the new closure note in this feature's directory and confirm it picks one of the two paths (re-probe vs. close-with-reference), records the chosen path, and links the upstream `attack-probe-verbose.md` diagnostic.

**Acceptance Scenarios**:

1. **Given** the closure decision is "close with reference", **When** the closure note is read, **Then** it cites the upstream `attack-probe-verbose.md` diagnostic and FSBarV1's `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md` outbound report and states explicitly that no further FSBarV1-side action is planned in this feature.
2. **Given** the closure decision is "re-probe", **When** the re-probe is run, **Then** the result (rc value, position samples at frame 20/300/600, LOS state) is recorded under `specs/022-incorporate-highbar-030/` and the next-step decision (close vs. escalate to engine) is documented based on the observed outcome.

---

### Edge Cases

- **Stale stderr from prior runs**: The trainer's run.sh copies engine logs from "the newest `/tmp/fsbar-*` dir modified during this run". If a stale dir from an earlier run is picked up, the parser could over-count. The fix should preserve today's session-isolation behaviour and not introduce new sources of cross-run contamination.
- **Mixed proto schema versions**: If the proxy emits an integer case value that does not exist in the proto schema FSBarV1 was built against (i.e., HighBarV2 added a new command type), the parser must surface the unknown integer rather than crash. `by_case: {"99": 1}` is acceptable; throwing on an unknown integer is not.
- **Verbose-mode infolog rotation**: When `verbose_commands=true`, the engine infolog can grow large. If the parser scans the infolog as well as stderr, it must avoid double-counting commands that appear on both streams.
- **HighBarV2 NuGet package bumps**: HighBarV2 commit `a1916e5` bumped `HighBar.Client` to 0.1.2 and `BarData` to 1.0.2. FSBarV1 does not consume `HighBar.Client` (it has its own `FSBar.Client`), but it does consume `BarData` from the local nupkg feed. If the bump introduces incompatible types, FSBarV1 may need to mirror the new BarData package into its own `nupkg/` dir.
- **No unsupported commands ever observed in current rungs**: If neither smoke run produces an unsupported-command line, the parser fix is observationally indistinguishable from today. Validation must include at least one fixture or synthetic input to prove the new parser actually counts what it claims.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The trainer's post-match unwired-command parser MUST extract integer values from the always-on stderr line `[HB] unsupported command oneof case=<INT> (proxy switch table miss)` and report them in `unwired_commands.json` keyed by the integer (e.g., `"by_case": {"99": 2}`).
- **FR-002**: The trainer's parser MUST use the always-on stderr line `[HB] unsupported command oneof case=<INT>` as the primary signal for `rc_minus_2_count`. It MUST NOT scan `engine.infolog`/`engine.stdout` for `rc=-2` markers — the always-on stderr line is sufficient and verbose-mode infolog parsing is out of scope for this feature.
- **FR-003**: The trainer's parser MUST always emit `unwired_commands.json` (preserving today's behaviour) with a numeric `rc_minus_2_count` and an object `by_case` (possibly empty), and MUST NOT throw on unknown integer case values.
- **FR-004**: A test or fixture MUST demonstrate the parser correctly counts a non-zero number of unsupported commands from a representative stderr/infolog input, so the fix is observable without requiring a real engine session that happens to produce one.
- **FR-005**: The doc-comment for `receiveFrame` in `src/FSBar.Client/Protocol.fsi` MUST be updated to (a) describe the current behaviour (synthesizes a sentinel `GameFrame` carrying `GameEvent.Shutdown` on the proxy's standalone Shutdown envelope; never returns `None` on Shutdown — the existing comment "Returns None on Shutdown" is stale post-`9e961db` and misleads API consumers reading IntelliSense) and (b) reference the upstream `shutdown-wire-shape.md` contract by path. The matching doc-comment block in `Protocol.fs` MUST also reference the upstream contract for source-level readers. The `val receiveFrame` signature line itself MUST NOT change (no surface-area baseline impact).
- **FR-006**: FSBarV1's trainer parser source MUST reference the upstream `unwired-command-log.md` contract by path and call out the "integer not string" and "separate lines" corrections in a brief comment.
- **FR-007**: The AttackCommand stationary-unit issue (Issue 1 in `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`) MUST be closed via a single decision document under `specs/022-incorporate-highbar-030/` that picks either "re-probe" or "close-with-reference" and records the rationale and the link to the upstream `attack-probe-verbose.md` diagnostic.
- **FR-008**: This feature MUST NOT modify FSBarV1's `Protocol.fs` shutdown synthesis logic. The upstream contract validates the existing approach as a reasonable client ergonomic; no behavioural change is required and none should be introduced.
- **FR-009**: A short outbound mailbox report under `Mailbox/2026-04-12_from_FSBarV1_*.md` MUST acknowledge HighBarV2's response, confirm the parser fix landed, and state the AttackCommand closure decision.
- **FR-010**: If `BarData` 1.0.2 (or newer) is required by the upstream proto schema FSBarV1 consumes, this feature MUST update FSBarV1's local `nupkg/` feed entry. Otherwise the feature MUST document explicitly that no upgrade is required (and why).

### Key Entities

- **Unwired-command report (`unwired_commands.json`)**: Per-run JSON file with `rc_minus_2_count: <int>` and `by_case: { "<integer-as-string>": <int> }`. Produced by `bots/trainer/run.sh` after every match.
- **Upstream contract reference**: A file path inside `../HighBarV2/specs/030-proxy-contract-docs/` cited from FSBarV1 source/comments. Not copied — referenced.
- **AttackCommand closure note**: A single Markdown file under `specs/022-incorporate-highbar-030/` recording the chosen path (re-probe vs. close-with-reference), the rationale, and any probe results if a re-probe was run.
- **Mailbox acknowledgement**: A new outbound Markdown file under `Mailbox/` confirming the integration is complete.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A trainer run on a representative fixture (or a synthetic input file fed to the parser) where the engine log contains N `[HB] unsupported command oneof case=<INT>` stderr lines produces `unwired_commands.json` with `rc_minus_2_count == N`. Today's parser produces `rc_minus_2_count == 0` for the same input — the fix is provable as a step change.
- **SC-002**: A clean smoke run with no unsupported commands continues to produce `unwired_commands.json` with `rc_minus_2_count: 0` and an empty `by_case` (no regression on the BARb/dev or NullAI smoke rungs).
- **SC-003**: A grep of `src/FSBar.Client/Protocol.fs` and `bots/trainer/run.sh` returns at least one reference to each of the two upstream contract paths (`shutdown-wire-shape.md` and `unwired-command-log.md`).
- **SC-004**: Issue 1 from the original outbound report is recorded as resolved in exactly one place (the closure note) with a defensible one-paragraph rationale and an upstream link, and the outbound mailbox report cites it.
- **SC-005**: The change set for this feature adds no behavioural change to `Protocol.fs` shutdown synthesis (no edits to the `MessageCase.Shutdown` branch). Diff review confirms the synthesis logic is byte-identical.
- **SC-006**: `grep "Returns None on Shutdown" src/FSBar.Client/Protocol.fsi` returns zero matches. The stale doc-comment that contradicts the current behaviour is gone. The replacement doc-comment correctly describes the sentinel-frame synthesis path and references the upstream contract.

## Assumptions

- HighBarV2's `030-proxy-contract-docs` documents are authoritative for the `0.1.x` proxy series and stable through any 0.1.x point release. We do not need to defensively re-derive any of the wire-shape or log-format facts.
- FSBarV1 will remain on its own `FSBar.Client` (a fork-equivalent of `HighBar.Client`) and is not switching to consume `HighBar.Client` from the local nupkg feed in this feature. The HighBarV2 commit `a1916e5` bumping `HighBar.Client` to 0.1.2 is informational, not an action item.
- `BarData` is consumed from `nupkg/` by version wildcard. If the upstream `pack-dev.sh` produced a newer build under HighBarV2's tree, FSBarV1 will copy that nupkg into its own `nupkg/` dir if and only if the build actually requires it; otherwise no upgrade is performed.
- The "close-with-reference" path is the expected default for FR-007 unless the maintainer explicitly chooses to run the re-probe during this feature's implementation. The re-probe requires a graphical or BAR-cheat-enabled session shape that is out of scope for the trainer's current headless smoke rungs.
- The trainer maintainer is the same person reviewing this spec; no separate stakeholder sign-off is required before `/speckit.plan`.
- No CI changes are required — the parser fix is verifiable via a small fixture-based shell test or a one-shot replay against an existing run's `engine.stderr` file.
