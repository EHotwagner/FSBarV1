# Phase 0 Research: HighBar proxy fix integration

**Feature**: `021-rerun-trainer-highbar` | **Date**: 2026-04-12

This file resolves the open questions deferred from `/speckit.clarify` to planning, and pins the technical approaches for each Functional Requirement that needed an explicit decision before tasks can be written.

---

## Decision 1 — How the rebuilt HighBarV2 proxy is consumed by FSBarV1

**Decision**: Use HighBarV2's existing in-tree CMake install path (`cmake --install build`), which writes the rebuilt `libSkirmishAI.so` directly into `~/.local/state/Beyond All Reason/engine/recoil_<version>/AI/Skirmish/HighBarV2/0.1/`. FSBarV1 does **not** vendor or copy the artifact — the engine loads it from BAR's data dir on next session start. The integration sequence is therefore:
1. `cd ../HighBarV2 && git pull && cmake --build build && cmake --install build`
2. Restart the FSI MCP server (per `CLAUDE.md` "DLL references are locked" guidance) so any pre-existing FSI session is not holding the previous proxy DLL through an active socket.
3. Run `bash bots/trainer/run.sh NullAI smoke-021` from the FSBarV1 repo root.

**Rationale**: This matches the existing dev-loop documented in `docs/bar-info.md` (line 43, `ENGINE_AI_DIR=...AI/Skirmish/HighBarV2/0.1`) and the `container/Containerfile` (line 77, `git clone https://github.com/EHotwagner/HighBarV2.git`). It does not require any FSBarV1-side build script change. The verification mechanism is FR-002's "observe a Shutdown(GAME_OVER) event delivered through the AI protocol on a known game-over trigger" — that event simply will not appear if the rebuilt `.so` did not land in the expected path.

**Alternatives considered**:
- *Vendor the proxy artifact under FSBarV1's `nupkg/` feed*: rejected — the proxy is a native `.so` loaded by the engine, not a NuGet package; vendoring would require a packaging step HighBarV2 does not have.
- *Add a `scripts/install-proxy.sh` to FSBarV1 that copies from `../HighBarV2/build/`*: rejected — duplicates HighBarV2's `cmake --install` and creates a second source of truth that can drift.
- *Pin a specific HighBarV2 git SHA via a submodule*: rejected as over-engineering for a feature whose explicit purpose is "pull master and integrate"; the inbound mailbox already names the squash-merged commits as the contract.

**FR(s) addressed**: FR-001, FR-002.

---

## Decision 2 — How `Single.NaN` is propagated through trainer code

**Decision**: Treat `Single.NaN` (the proxy's "invalid resource id" sentinel from the inbound mailbox) as the canonical "unknown" value end-to-end. Concretely:
- `bots/trainer/helpers/tactics.fsx` reads `client.GameState.Metal.Current` and `.Energy.Current` per frame as today, but the `peakMetal` / `peakEnergy` accumulators use a `nanSafeMax` helper: `let nanSafeMax (acc: float) (v: float32) = if Single.IsNaN v then acc else max acc (float v)`. This prevents NaN from poisoning the running max.
- The accumulators stay `float`. If *every* frame's read is NaN (the resource id was invalid for the entire match — should never happen for `0`/`1`, but defensive), the accumulator stays at its initial value `0.0`, and the result writer (FR-005) emits a JSON `null` for that field rather than `0`. The `result.schema.json` from feature 020 already permits `null` for these fields; the `log.fsx` writer change is one branch.
- The stall comparison helper (Decision 3) consumes the JSON value, with `null` round-tripping through as the "skip" sentinel.

**Rationale**: NaN cannot be compared with the standard `<`/`>` operators (`NaN > 0.0` is false; `NaN < 0.0` is false), so unguarded use silently corrupts the running max. The inbound mailbox's "use `Single.IsNaN(v)` to detect" guidance is the authoritative pattern. JSON has no NaN representation, so persisting the value requires a `null` choice — the result.schema.json supports this and downstream consumers (the stall checker) can treat `null` and "missing" identically.

**Alternatives considered**:
- *Treat NaN as `0.0` at the read site*: rejected — violates Clarification Q2 (NaN must skip the field, not collapse to a real value) and breaks observability (a real-zero economy reading is meaningfully different from "callback failed").
- *Throw on NaN*: rejected — too brittle for a defensive guard against an upstream-fixed-but-not-yet-trusted callback.
- *Persist NaN as the JavaScript string `"NaN"` in JSON*: rejected — requires non-standard JSON parsing on the consumer side.

**FR(s) addressed**: FR-003, FR-005.

---

## Decision 3 — NaN-aware stall comparison helper

**Decision**: Add a small pure F# function in `bots/trainer/helpers/tactics.fsx` (and a sibling test in `tests/FSBar.Client.Tests/TrainerStallTests.fs`) with this shape:

```fsharp
type StallTelemetry = {
    FramesSurvived: int
    EnemyKilled: int
    UnitsBuilt: int
    PeakMetal: float option       // None means "not available" / NaN
    PeakEnergy: float option
}

/// Returns true if `current` shows ANY improvement over `prior` on at least
/// one tracked field. Fields where `current` is None are skipped (they
/// neither improve nor stagnate); fields where `prior` is None but `current`
/// is Some count as improvement (we just learned a real value).
let improvedOverPrior (prior: StallTelemetry) (current: StallTelemetry) : bool = ...
```

The stall counter resets to zero on any iteration where `improvedOverPrior priorIter currentIter = true`, and the loop halts after five consecutive iterations on the same rung where `improvedOverPrior` returned false. This matches feature 020 §FR-018 plus this feature's FR-015 NaN refinement.

**Rationale**: A separately testable helper lets the constitution's §III "Test Evidence Is Mandatory" obligation be met by a small unit test (no live engine), and it isolates the NaN logic from the rest of `tactics.fsx`. The shape mirrors the existing `Telemetry` record on lines 239–247 of `tactics.fsx`, with `float option` swapped in for the two NaN-eligible fields.

**Alternatives considered**:
- *Inline the NaN check at every comparison site*: rejected — duplicates a tricky predicate three times and prevents unit testing.
- *Use `nan`-as-`Double.NaN` and rely on `Double.IsNaN` everywhere*: rejected — `option`-typing makes the "missing" case visible at the type level and prevents accidental arithmetic against a NaN sentinel.

**FR(s) addressed**: FR-015 (NaN-aware stall), FR-018 from feature 020 (preserved).

---

## Decision 4 — Surfacing proxy `rc=-2` to the trainer

**Decision**: The F# `BarClient.SendCommands` API does not return per-command rc values (commands are queued and sent via `Protocol.sendFrameResponse` which is a one-way message). The proxy logs each command's rc to its own log file (which the existing `bots/trainer/run.sh` already copies into the run directory as `engine.infolog` and `engine.stdout`/`stderr`). FR-004 will therefore be satisfied at *post-match analysis time*, not at frame time:

- Post-match (after `dotnet fsi bot.fsx` exits and before `run.sh` writes the final stub-or-real `result.json`), `run.sh` greps the captured engine log files for any line matching the proxy's `rc=-2` emission and counts them by `case=` value.
- The result is written to a sibling file `unwired_commands.json` inside the run directory, with shape `{ "rc_minus_2_count": <int>, "by_case": { "<case-name>": <int>, ... } }`. If no `rc=-2` lines are found, the file contains `{ "rc_minus_2_count": 0, "by_case": {} }`.
- The trainer's structured frame log (`frames.jsonl`) is **not** modified to carry per-frame rc. The contract change is purely additive: a new sibling file in the run directory.
- The PLAYBOOK §3 classification step reads `unwired_commands.json` and treats `rc_minus_2_count > 0` as a yellow flag — it does not by itself classify an iteration's outcome, but it tells the operator "the bot tried to send a command type the proxy never wired", which is a real diagnostic distinct from "the engine accepted the command but ignored its effect".

**Rationale**: Modifying `BarClient.SendCommands` to return `rc list` would be a public API change (Tier 1 obligation: `.fsi` update + surface-area baseline regeneration + `fsdoc` agent run) for a value that's only useful in the trainer's post-match diagnostic step. The grep-the-log approach has zero compiled-API impact, fits in `run.sh` as a 5-line jq pipeline, and produces a structured artifact that the playbook can consume.

**Alternatives considered**:
- *Extend `BarClient` to return per-command rc*: rejected — Tier 1 cost for diagnostic-only value; HighBarV2 would also need a wire-protocol response message that doesn't exist today.
- *Parse `engine.infolog` from inside `bot.fsx`*: rejected — bot.fsx exits before `run.sh` finishes copying the log files into the run dir, so the parser would be racing the file copy. Doing it in `run.sh` after the copy is sequenced correctly.
- *Skip FR-004 entirely and rely on operator log inspection*: rejected — FR-004 says the trainer MUST distinguish `0`/`-1`/`-2` in something the operator can read without grep, and `unwired_commands.json` is the smallest such artifact.

**FR(s) addressed**: FR-004.

---

## Decision 5 — Where to wire the Issue 1 `getUnitPos` probe

**Decision**: Add the probe to a single iteration on the **NullAI rung**, not BARb/dev. The probe lives inline in `bot.fsx`'s tactics callback for one iteration, reuses the existing `MoveCommand`-then-`AttackCommand` pattern from feature 020 iteration 020 (the first NullAI win), and writes its result into the run directory as a sidecar file `attack_probe.json` with shape:

```json
{
  "issuing_unit_id": <int>,
  "issuing_unit_def": "<string>",
  "target_unit_id": <int>,
  "frame_at_send": <int>,
  "pos_before": [<x>, <y>, <z>],
  "frame_at_check": <int>,
  "pos_after": [<x>, <y>, <z>],
  "outcome": "moved" | "stationary" | "destroyed"
}
```

`outcome` is `"moved"` when the Euclidean distance between `pos_before` and `pos_after` exceeds 5.0 game units, `"destroyed"` when the issuing unit is no longer in `client.GameState.Units` at the check frame, and `"stationary"` otherwise. The check frame is `frame_at_send + 30` (one game-second at 30fps).

**Rationale**: NullAI is the right rung because the enemy is passive — the issuing unit isn't being shot at, so any movement is unambiguously the result of the AttackCommand dispatch (or its absence). On BARb/dev, hostile fire could push the issuing unit around or destroy it within the probe window, polluting the signal. Reusing the iteration 020 unit + target makes the probe directly comparable to a known-good run. A 5.0-game-unit movement threshold is small enough to catch any real engine response and large enough to suppress floating-point jitter.

**Alternatives considered**:
- *Probe on BARb/dev to test the "real combat" claim*: rejected — too much noise; the upstream HighBarV2 maintainer specifically asked for "an `AttackCommand` send" probe, not "an `AttackCommand` send under combat conditions".
- *Probe on every iteration*: rejected — once the probe answers the upstream question, additional probes add no information; FR-017 only requires "at least one iteration".
- *Build a BAR-Lua trace alongside the probe*: rejected — out of scope; the probe is purely an FSBarV1-side observation.

**FR(s) addressed**: FR-017, FR-018.

---

## Decision 6 — 10-iteration per-rung budget mechanics (FR-016a)

**Decision**: The 10-iteration cap is enforced by the operator per-iteration via PLAYBOOK §10, not by automated count enforcement inside `run.sh` or `tactics.fsx`. Specifically:
- Each iteration on a rung increments an iteration id (the existing `iter_id` argument to `run.sh`).
- After every iteration, the operator (per the playbook) checks the count of iterations on the current rung in `HISTORY.md`. If the count reaches 10 without a `win` outcome and the FR-015 stall rule has not already fired, the operator MUST file a budget-exhaustion report under `Mailbox/` with shape `2026-04-XX_from_FSBarV1_budget_exhausted_<rung>.md` containing: rung name, all 10 iteration ids, terminal outcomes, frames, and the telemetry trend across them.
- The operator MUST then halt iteration on that rung and request a decision before proceeding.
- The PLAYBOOK §10 entry is added by this feature's tasks; the runner is not modified.

**Rationale**: The count is trivially observable from `HISTORY.md` (one line per iteration, one column for rung), so building automated enforcement into `run.sh` would duplicate a check the operator already performs. Keeping it in the playbook is consistent with feature 020's overall pattern of operator-driven loop control (FR-013). The mailbox-as-report format reuses the existing `Mailbox/` channel rather than introducing a new artifact location.

**Alternatives considered**:
- *Auto-halt in `run.sh` after iteration 10*: rejected — `run.sh` runs one iteration in isolation and does not currently know about prior iterations on the same rung; teaching it to read `HISTORY.md` is more complexity than the simpler operator-side check.
- *Define the budget per rung in `ladder.json` so different rungs can have different caps*: rejected — the 10 cap is uniform per Clarification Q4, and adding a per-rung override is YAGNI for this feature.

**FR(s) addressed**: FR-016a.

---

## Decision 7 — How to interpret FR-008 ("zeroed-econ placeholder removal")

**Decision**: The current `bots/trainer/run.sh` contains `peak_metal: 0` and `peak_energy: 0` literals **only** in the `write_stub_if_missing` (line 129–130) and `write_interrupted_stub` (line 150–151) error/interrupted-state stubs. These are *fallback values* for the case where the bot crashed before writing `result.json` and there is no telemetry data at all. They are **not** placeholders in the real telemetry path — the real path is `tactics.fsx` writing `result.json` from the `peakMetal`/`peakEnergy` mutables, which were broken in 020 only because the proxy returned 0 from `Economy_get*`. Now that the proxy is fixed, the real path produces real values without any FSBarV1 code change.

Therefore: **the stub fallback zeros in `run.sh` are left in place**. Replacing them with `null` would change the schema-conformance of error-state results (the existing `result.schema.json` may currently require numeric fields in the telemetry block — see Phase 1 contract delta). FR-008 is satisfied trivially because no real-path zero placeholders exist; the FR is interpreted to mean "the real telemetry path no longer carries zeros that were caused by the broken callbacks" rather than "every zero literal in the runner must be removed".

**Rationale**: Reading the spec narrowly — FR-008 says "any zeroed-econ placeholder ... MUST be removed once the real values from FR-005 are flowing". The stub fallback zeros are not "placeholders for the real values" — they are the canonical "no telemetry available because the bot crashed" representation. Conflating the two would force the runner to invent a NaN-or-null encoding for an error state that the schema may not even permit.

**Alternatives considered**:
- *Convert stub fallbacks to `null`*: deferred to Phase 1's contract delta investigation; if `result.schema.json` already permits `null` here, this is a harmless cleanup, otherwise it's a schema change that's out of scope for this feature.
- *Aggressively grep the codebase for any `peak_metal: 0` literal and rewrite it*: rejected — would touch error-state stubs that have nothing to do with the proxy fix.

**FR(s) addressed**: FR-008.

---

## Decision 8 — Cross-repo defect handling mechanics (FR-021)

**Decision**: When an iteration's failure root-causes to a HighBarV2 proxy defect, the operator follows the existing PLAYBOOK §3 out-of-scope branch from feature 020 with one specialisation: the report format is an inbound mailbox to HighBarV2 named `Mailbox/2026-04-XX_to_HighBarV2_<short-symptom>.md` containing the iteration directory, the symptom, the relevant frame log excerpt, and the relevant `engine.infolog` excerpt. The operator then halts the loop (FR-021) and requests an operator decision. HighBarV2's source tree is not edited from inside this feature.

**Rationale**: This reuses feature 020 §FR-016's "out-of-scope report" pattern verbatim, only changing the destination repo. The mailbox naming convention mirrors the inbound `2026-04-12_to_FSBarV1_proxy_fixes_complete.md` so the upstream maintainer can find replies in the same `Mailbox/` directory.

**Alternatives considered**:
- *Open a GitHub issue on HighBarV2 instead of using a mailbox*: rejected — the existing inbound mailbox is the authoritative coordination channel between the two repos; introducing a second channel splits the conversation.
- *Allow trivial fixes (typos, one-liners) in HighBarV2 within this feature*: rejected at clarification time (Q1 chose Option A, hard out-of-scope).

**FR(s) addressed**: FR-021.

---

## Open questions deferred from spec / clarify

None remaining. All FRs in the spec map to a decision above or are satisfied by direct reuse of feature 020 mechanisms (FR-010 commit-and-push discipline, FR-011 fixed map+seed from `ladder.json`, FR-012 reuse-as-is of playbook+ladder+runner+history, FR-013 history-log format, FR-014 helper-extraction discipline, FR-016 minimum ladder, FR-019/FR-020 docs).
