# Phase 1 Data Model

**Feature**: `021-rerun-trainer-highbar` | **Date**: 2026-04-12

This feature is overwhelmingly a *behavioural integration* on top of feature 020's existing data model. It introduces zero new persisted entities and zero schema-breaking changes. The data model below documents only the deltas — the entities defined in `specs/020-bot-iterative-trainer/data-model.md` are inherited unchanged.

## Inherited entities (unchanged from feature 020)

| Entity | Source | Notes |
|---|---|---|
| `Iteration` | 020 §Key Entities | Per-iteration cycle id, timestamp, rung, outcome, source revision, run dir. Reused as-is. |
| `Rung` | 020 §Key Entities + `bots/trainer/ladder.json` | The two rungs `NullAI` and `BARb/dev` from `ladder.json` are reused unchanged. Map = `Avalanche 3.4`, seed = `1`. |
| `Run directory` | 020 contracts/run-directory.md | Self-contained per-iteration artifact directory. *Two new optional sibling files* added in this feature (see contracts delta). |
| `Terminal result record` | 020 contracts/result.schema.json + this feature's contracts/result-record.delta.md | Two telemetry fields relaxed from `number` to `["number","null"]`. |
| `Helper module` | 020 §Key Entities | At least one helper beyond `log.fsx` MUST gain real (non-stub) content during this feature, motivated by 2-iteration duplication and used from 2+ call sites (SC-006). |
| `Playbook` | `bots/trainer/PLAYBOOK.md` | Reused; gains a §10 entry for the 10-iteration cap and the cross-repo defect route. |
| `Iteration history log` | `bots/trainer/HISTORY.md` | Reused as-is. Pipe-delimited line per iteration. |
| `Out-of-scope report` | `Mailbox/` | Reused. Now also the venue for budget-exhaustion reports (FR-016a) and inbound mailboxes to HighBarV2 for cross-repo defects (FR-021). |

## New / changed entities

### `StallTelemetry` (new — internal F# type)

Lives in `bots/trainer/helpers/tactics.fsx` (and is exercised by `tests/FSBar.Client.Tests/TrainerStallTests.fs`). Not persisted; not part of any wire or file contract. Exists only to give the NaN-aware stall comparison a clean signature.

```fsharp
type StallTelemetry = {
    FramesSurvived: int       // From feature 020 telemetry; never NaN
    EnemyKilled: int          // From feature 020 telemetry; never NaN
    UnitsBuilt: int           // From feature 020 telemetry; never NaN
    PeakMetal: float option   // None when the proxy returned NaN for the entire match (FR-003)
    PeakEnergy: float option  // Same — None means "not available", not zero
}
```

**Construction**: built from a parsed `result.json` via a small adapter that maps JSON `null` → `None` and JSON number → `Some <value>` for the two `peak_*` fields.

**Validation**: integer fields are non-negative (delegated to the JSON schema, not re-checked); option fields have no constraint beyond the `option` type.

**Lifetime**: one instance per iteration loaded into memory by the stall checker; not persisted in this form. The persisted form is the `result.json` itself.

### `improvedOverPrior` (new — internal F# function)

Pure function on two `StallTelemetry` records:

```fsharp
val improvedOverPrior : prior:StallTelemetry -> current:StallTelemetry -> bool
```

**Behaviour** (test cases enumerated):

| Case | `prior` | `current` | Result | Why |
|---|---|---|---|---|
| Strict improvement on int field | `{ FramesSurvived=100; ...}` | `{ FramesSurvived=200; ...same}` | `true` | Any improvement clears the stall counter. |
| All int fields stagnant, both peaks `None` | `{ FramesSurvived=100; PeakMetal=None; PeakEnergy=None; ...}` | `{ FramesSurvived=100; PeakMetal=None; PeakEnergy=None; ...same}` | `false` | All non-NaN fields stagnated; NaN fields skipped. |
| Both peaks `None` in current, ints stagnant, prior had `Some` peaks | `{ ...; PeakMetal=Some 500.0; PeakEnergy=Some 300.0 }` | `{ ...same ints; PeakMetal=None; PeakEnergy=None }` | `false` | NaN in current means "skip" — does not count as improvement OR stagnation. The stall verdict comes from the non-NaN fields, which here are all stagnant. |
| `prior.PeakMetal=None`, `current.PeakMetal=Some 500.0` | `{ ...; PeakMetal=None }` | `{ ...same ints; PeakMetal=Some 500.0 }` | `true` | Newly available real value counts as improvement (we just learned something we did not know before). |
| `prior.PeakMetal=Some 500.0`, `current.PeakMetal=Some 600.0` | regular increase | regular increase | `true` | Real numeric improvement on PeakMetal alone. |
| `prior.PeakMetal=Some 500.0`, `current.PeakMetal=Some 400.0`, ints all stagnant | regression | regression | `false` | Regression is not improvement; ints stagnated; no improvement found. |

**Test home**: `tests/FSBar.Client.Tests/TrainerStallTests.fs` (new file). Six test methods, one per row above. Pure xUnit `[<Fact>]` style. No live engine, no I/O.

### `unwired_commands.json` (new — sibling file in run directory)

Schema lives in `contracts/result-record.delta.md` (reproduced once there to keep contracts in one place). Written by `bots/trainer/run.sh`'s post-match step (Decision 4 in `research.md`). Required on every successful run; the count is permitted to be zero. Not modelled as an F# type because it is consumed by the operator reading the file directly, not by F# code.

### `attack_probe.json` (new — optional sibling file in run directory)

Schema lives in `contracts/result-record.delta.md`. Optional: present only on iterations whose bot script wired the Issue 1 probe instrumentation (FR-017 requires "at least one"). Written by the bot script (`bot.fsx`) as a final action before exiting. Not modelled as an F# type because it is iteration-local and consumed by hand for the upstream Issue 1 follow-up.

## State transitions

No new state machines. The iteration loop's existing states from feature 020 (`run → diagnose → improve → commit → push → next`) are reused. The 10-iteration budget exhaustion and the FR-021 cross-repo defect path are both *exit edges* added to the existing PLAYBOOK §3 decision tree:

```
                       ┌── win → advance rung (or finish)
                       │
                       ├── stall (5 iters no improvement) → halt + stall report
                       │
                       ├── budget exhausted (10 iters no win) → halt + budget mailbox  ◄── NEW (FR-016a)
diagnose & classify ──┤
                       ├── bot/helper/repo issue → fix → commit → push → next
                       │
                       ├── upstream HighBarV2 defect → halt + inbound mailbox          ◄── NEW (FR-021)
                       │
                       └── operator-classified out-of-scope → halt + out-of-scope report
```

Both new exits route through `Mailbox/` and end with the operator pausing the loop. Neither modifies any existing entity's state machine.

## Validation rules

| Rule | Source FR | Where enforced |
|---|---|---|
| `peakMetal` / `peakEnergy` accumulators must skip NaN reads | FR-003 | `tactics.fsx` `nanSafeMax` helper |
| `peak_metal` / `peak_energy` in `result.json` may be `null` (was: required `number`) | FR-003 + FR-005 | `result.schema.json` (relaxation per contracts delta) |
| Stall comparison must skip `None`-typed `peak_*` fields | FR-015 | `improvedOverPrior` |
| Stall fires only after 5 consecutive `improvedOverPrior = false` iterations on the same rung | FR-015 + 020 §FR-018 | Operator playbook + the helper |
| 10-iteration cap per rung enforced via PLAYBOOK §10 check after each iteration | FR-016a | `PLAYBOOK.md` §10 (new) |
| Cross-repo defects route to inbound mailbox + halt; HighBarV2 source not edited | FR-021 | `PLAYBOOK.md` §3 decision tree (new branch) |
| Issue 1 probe written exactly once during the feature, on NullAI rung | FR-017 + Decision 5 | Operator playbook reminder + bot.fsx instrumentation on the chosen iteration |
| `unwired_commands.json` written on every successful run | FR-004 | `run.sh` post-match step |
| All four workaround identifiers (`botDeclaredVictory`, `"No active session"` heuristic, hard-coded `enum_move=42`, real-path zero placeholders) absent from shipping helpers and runner | FR-006 through FR-009 + SC-003 | Repository grep verification at the end of US2 |

## What is intentionally NOT in the data model

- A persistent "iteration counter per rung" — the count is computed on demand from `HISTORY.md`. This is consistent with feature 020's design and avoids a second source of truth.
- A persistent "feature progress" record — the spec uses commits on the feature branch as the progress signal.
- An automated decision tree implementation — the PLAYBOOK is the decision tree; the operator walks it.
- A new wire-level message — the rebuilt HighBarV2 proxy uses the same protobuf schema as before; only its behaviour changed.
