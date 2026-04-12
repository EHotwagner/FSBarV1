# Research: Incorporate HighBarV2 030 proxy contract docs

**Feature**: 022-incorporate-highbar-030
**Date**: 2026-04-12

## Sources read

### Mailbox response (the trigger for this feature)

- `Mailbox/2026-04-12_from_HighBarV2_contract-docs-response.md` — full reply from the HighBarV2 maintainer to FSBarV1's `2026-04-12_to_HighBarV2_proxy-contract-refinements.md` request.

### Upstream contract documents (authoritative for proxy 0.1.x)

- `../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md`
- `../HighBarV2/specs/030-proxy-contract-docs/contracts/unwired-command-log.md`
- `../HighBarV2/specs/030-proxy-contract-docs/diagnostic/attack-probe-verbose.md`

### Upstream commits inspected (`../HighBarV2`, branch master)

| SHA | Subject | Relevance |
|---|---|---|
| `a1916e5` | chore: bump BarData to 1.0.2 and HighBar.Client to 0.1.2 | Informational. FSBarV1 doesn't consume HighBar.Client. BarData bump evaluated under D3. |
| `9a91b4a` | feat: merge 030-proxy-contract-docs (squash) | Source of the two contract documents and the diagnostic. |
| `83b020c` | docs(mailbox): inbound from FSBarV1 — proxy contract refinements after 021 integration | The inbound trigger (our outbound letter, archived on their side). |
| `32ce22f` | chore: bump BarData to 1.0.1 and HighBar.Client to 0.1.1 | Already accounted for in feature 021 timeframe. |
| `9855255` | feat: merge 029-fix-trainer-issues (squash) | Already integrated via FSBarV1 feature 021 (the 029 contracts are referenced from `bots/trainer/HISTORY.md`). |

No commit between 029 and 030 changes proxy wire format or command dispatch in a way that contradicts FSBarV1's current behaviour. The 030 work is documentation + diagnostic only.

## Key findings

### Finding 1 — The trainer's existing parser is silently always-zero

`bots/trainer/run.sh` lines 204-225 (added in feature 021 per FR-004) implement post-match `rc=-2` aggregation. The current pattern is:

```bash
case_name="$(printf '%s\n' "$line" | sed -n 's/.*case=\([A-Za-z_][A-Za-z0-9_]*\).*/\1/p')"
```

This assumes the proxy emits `case=<NAME>` where `<NAME>` is alphabetic (e.g., `case=AttackCommand`). The upstream `unwired-command-log.md` contract states the proxy emits `case=<INT>` (e.g., `case=99`) — both on the always-on stderr line and in verbose-mode infolog lines. The current regex matches zero proxy lines and `rc_by_case` is always empty. Combined with the `grep -F 'rc=-2'` filter — which never matches the always-on stderr line because that line doesn't carry `rc=-2` — `rc_minus_2_count` is always 0.

### Finding 2 — The always-on stderr line is the right primary signal

Per `unwired-command-log.md` §"Parsing Guidance":

> **Option A — Parse stderr** (recommended, always available):
> - Grep for the literal substring `unsupported command oneof case=` on stderr
> - Extract the integer after `case=` using pattern: `case=([0-9]+)`
> - One line = one unsupported command dispatch

The line shape is:
```
[HB] unsupported command oneof case=<INT> (proxy switch table miss)
```

This is emitted regardless of the `verbose_commands` AI option, so it works for both BARb/dev and NullAI smoke rungs without configuration changes.

### Finding 3 — Verbose-mode parsing is more invasive

Verbose mode adds two **separate** infolog lines per command:
```
Cmd 2: case=99
Cmd 2: rc=-2
```

Correlating the two requires tracking the `Cmd <N>:` prefix across lines. Cheap in awk but uglier in pure-bash. The contract recommends Option A (stderr) as primary — we can satisfy SC-001 entirely from the always-on stderr line, treating verbose-mode parsing as an optional Phase-2 enhancement.

### Finding 4 — AttackCommand probe is not actionable from FSBarV1's current shape

The recommended re-probe (`shutdown-wire-shape.md`-adjacent diagnostic) needs:
- `cheat|globallos` debug command (BAR cheat-mode debug, not normally available in headless smoke)
- Spawned attacker + target via `GiveMeNewUnit` 200-500 elmos apart
- Command issued at frame ~20, observation through frame 600

The trainer's smoke harness runs full-game scenarios via `EngineLauncher` and does not currently emit cheat commands or stage attacker/target pairs. Building a one-off probe runner is a 1-2 day effort with no impact on the trainer loop. The upstream maintainer also notes that even if `rc=0` and the unit doesn't move, the next escalation is engine-side and out of scope for both repos.

### Finding 5 — `BarData` 1.0.2 not required by FSBarV1

`src/FSBar.Client/FSBar.Client.fsproj` references `BarData` with `Version="*-*"`. The local feed contains `nupkg/BarData.1.0.0-dev.20260408T121533.nupkg`. FSBarV1 builds against this package successfully today (recent commits `7368626`, `5c1e772` confirm green). The HighBarV2 bump to 1.0.2 happened in HighBarV2's tree only — no nupkg has been delivered into FSBarV1's `nupkg/` dir, and there is no build evidence forcing the upgrade. The proto schema FSBarV1 consumes is generated locally in `src/FSBar.Proto/Generated/`, not pulled from `BarData`.

## Decisions

### D1 — Parse the always-on stderr line as the primary signal

**Decision**: Replace the existing `sed`-based loop with a single `grep -E '^\[HB\] unsupported command oneof case=[0-9]+ '`-based extraction over `engine.stderr`. Extract the integer with `sed -n 's/^\[HB\] unsupported command oneof case=\([0-9]\+\).*/\1/p'`. Aggregate counts in a `declare -A` map keyed by integer string. Emit JSON via `jq` exactly as today.

**Rationale**: Matches the upstream contract's recommended Option A. Always-available regardless of verbose-mode setting. Zero new dependencies.

**Alternatives considered**:
- Awk-based correlation of verbose-mode `Cmd N: case=` / `Cmd N: rc=-2` pairs — more code, only adds value when verbose mode is on, no runs in flight enable it.
- Reading proto field-number → command-name map at parse time — see D2.

### D2 — Key `by_case` by integer-as-string, not by command name

**Decision**: `by_case` keys are decimal integer strings (`"99"`, `"45"`). The parser does not resolve these to command names.

**Rationale**: Resolving names requires reading `messages.proto` at parse time (a bash-side proto parser, or shelling out to `protoc --decode`). Both add a dependency for negligible benefit — consumers reading `unwired_commands.json` already have the proto file in-tree and can resolve names themselves. Keeps the parser dependency-free.

**Edge case satisfied**: Spec edge case "unknown integer must surface" is automatic — we just record whatever integer the proxy emits. No mapping table to maintain.

### D3 — Do not bump `BarData` to 1.0.2 in this feature

**Decision**: Document under FR-010 that `BarData` 1.0.2 is not adopted because no build evidence forces the upgrade. FSBarV1 stays on `BarData.1.0.0-dev.20260408T121533`.

**Rationale**: The HighBarV2 commit `a1916e5` is informational. FSBarV1's proto generation is independent of `BarData`'s schema. Pulling the new package in would require re-running the upstream `pack-dev.sh` against HighBarV2, copying the resulting nupkg into FSBarV1's `nupkg/` dir, and re-running the 020/021 smoke baselines to confirm no regression — work disproportionate to a feature whose primary deliverable is a bash parser fix.

**Alternatives considered**: Preemptive upgrade — rejected as scope creep.

**Build verification (T016, 2026-04-12)**: `dotnet build src/FSBar.Client/FSBar.Client.fsproj` green against existing `nupkg/BarData.1.0.0-dev.20260408T121533.nupkg`; no upgrade needed.

### D4 — Close AttackCommand stationary-unit issue with reference, not re-probe

**Decision**: Write `attack-command-closure.md` recording the close-with-reference decision, citing the upstream `attack-probe-verbose.md` diagnostic and FSBarV1's outbound `Mailbox/2026-04-12_to_HighBarV2_attack-command-stationary.md`. State explicitly that no further FSBarV1-side action is planned.

**Rationale**: Per spec assumption, this is the default. The upstream maintainer's source-level analysis is sufficient — the proxy dispatch is verified correct. The remaining hypotheses (LOS, pathing-readiness, observation window) are engine/game-logic interactions, and confirming them requires a different harness shape than the trainer currently has. Re-probe stays available as a future feature if needed.

**Alternatives considered**: Run the re-probe in this feature — rejected as out-of-scope for the current trainer harness.

### D5 — Do not modify `Protocol.fs` shutdown synthesis

**Decision**: Leave the `MessageCase.Shutdown` branch (lines 84-97) byte-identical. Only extend the existing doc-comment block at lines 56-70 with one line: `/// See ../HighBarV2/specs/030-proxy-contract-docs/contracts/shutdown-wire-shape.md for the upstream wire-shape contract.`

**Rationale**: FR-008/SC-005 lock this in. The upstream contract validates the existing approach as a "reasonable client ergonomic"; introducing a behavioural change here would be net-negative.

**Alternatives considered**: Refactor the synthesis to expose `ShutdownReason` as a typed value instead of a string — rejected; that would be a Tier 1 surface change requiring `.fsi` and baseline updates and is unrelated to the integration work.

## Open questions

None. No NEEDS CLARIFICATION markers in the spec.
