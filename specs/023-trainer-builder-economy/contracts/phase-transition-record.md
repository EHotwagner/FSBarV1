# Phase Transition Record — on-disk contract

**Branch**: `023-trainer-builder-economy`
**File**: `<run_dir>/phase_transitions.jsonl`
**Written by**: `bot_macro.fsx` via `log.fsx.logPhaseTransition`
**Not written by**: `bot.fsx` (existing rush bot) — absence is the indicator that a given run came from the rush bot.

## File layout

One JSON object per line (JSONL), UTF-8, LF line endings, appended as the bot observes phase transitions during a match. The file is optional in the run-directory conformance check — `run.sh` does NOT stub it. For a macro-bot iteration, its absence or emptiness is a **bot-logic bug**, not an infrastructure regression. For a rush-bot iteration, its absence is expected.

Line ordering is strictly by `frame` (frame numbers monotonically increase; ties are resolved in the order events arrive within the same frame). Two lines with the same `frame` and `from`→`to` is a bug.

## Record schema

```jsonc
{
  // Required
  "frame": 1234,                              // uint32, frame at which the transition occurred
  "from": "Opening",                           // MacroPhase name; see allowed values below
  "to": "Production",                          // MacroPhase name; see allowed values below
  "reason": "first-factory-finished",          // short slug; see allowed values below

  // Optional
  "telemetry": {                               // free-form numeric snapshot at transition time
    "units": 1,
    "metal_current": 120.3,
    "metal_income": 12.3,
    "energy_current": 450.0,
    "energy_income": 35.0,
    "combat_units": 0,
    "constructors": 1,
    "structures_built": {"cormex": 2, "corsolar": 2, "corlab": 1}
  },
  "notes": "factory unit_id=42"                // free-form operator-facing string
}
```

### Allowed `from` / `to` values

- `"Opening"`
- `"Production"`
- `"Upgrade"`
- `"Attack"`
- `"Defending"`              (used only for enter/exit defend interrupt)

Any other string is a bug.

### Allowed `reason` slugs

This list is not closed — operators may add new slugs when iteration surfaces a new transition case — but the *meaning* of each slug MUST be documented in `PLAYBOOK.md §12`.

Initial slugs:

| Slug                             | From        | To          | Meaning                                                 |
|----------------------------------|-------------|-------------|---------------------------------------------------------|
| `first-factory-finished`         | `Opening`   | `Production`| FR-004 canonical opening → production trigger           |
| `upgrade-entry-predicate-met`    | `Production`| `Upgrade`   | Economy thresholds reached; begin upgrading             |
| `upgrade-reached-normal`         | `Upgrade`   | `Attack`    | `decideUpgradeExit` returned `AttackNow(Normal)`        |
| `upgrade-deadline-fallback`      | `Upgrade`   | `Attack`    | `decideUpgradeExit` returned `AttackNow(DeadlineFallback)` — FR-012 path |
| `upgrade-stall-no-army`          | `Upgrade`   | `Upgrade`   | `decideUpgradeExit` returned `StallAndLose`; NOT a real transition, recorded for diagnosis |
| `enemy-in-base`                  | *any*       | `Defending` | FR-016b defend interrupt entered                        |
| `enemy-cleared`                  | `Defending` | *previous*  | FR-016b defend interrupt exited                         |
| `opening-placement-retry-exhausted` | `Opening`| `Production`| FR-003 — item retry budget exhausted; advancing anyway  |

### Validation rules

1. `frame` is a non-negative integer; increases monotonically within the file.
2. `from` and `to` are both one of the allowed values (or `from = to` for `upgrade-stall-no-army` and for `Defending` self-loops which must not occur).
3. `reason` MUST be one of the allowed slugs or a slug documented in an iteration's HISTORY line.
4. `telemetry` keys are free-form but the values MUST be JSON numbers or JSON objects with numeric values. No strings inside telemetry (use `notes` for that).

### Fields explicitly NOT in the record

- No wall-clock time (use `frame` + `meta.json.start_timestamp` for time).
- No git sha (in `meta.json`).
- No iteration id (in `meta.json`).
- No bot script path (in `bot.fsx.snapshot`).

Keep this file small and diff-friendly. Two iterations' phase logs should diff in under 10 lines.

## Example

```jsonl
{"frame":245,"from":"Opening","to":"Defending","reason":"enemy-in-base","notes":"1 enemy at dist=950"}
{"frame":312,"from":"Defending","to":"Opening","reason":"enemy-cleared"}
{"frame":1420,"from":"Opening","to":"Production","reason":"first-factory-finished","telemetry":{"units":1,"metal_income":14.2,"energy_income":38.0}}
{"frame":4550,"from":"Production","to":"Upgrade","reason":"upgrade-entry-predicate-met","telemetry":{"combat_units":6,"metal_income":21.5}}
{"frame":6230,"from":"Upgrade","to":"Attack","reason":"upgrade-reached-normal","telemetry":{"combat_units":14,"metal_income":30.1}}
```

## Runner behaviour

`run.sh` does NOT touch `phase_transitions.jsonl`:

- Not created as a stub.
- Not copied from anywhere.
- Not parsed for the run summary.

The bot creates the file on its first `logPhaseTransition` call (opening for append is fine — the run directory exists).
