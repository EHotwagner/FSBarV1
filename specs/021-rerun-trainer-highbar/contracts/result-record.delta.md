# Contract Delta: result.schema.json (additive)

**Feature**: `021-rerun-trainer-highbar` | **Date**: 2026-04-12
**Source schema**: `specs/020-bot-iterative-trainer/contracts/result.schema.json`
**Status**: Backwards-compatible relaxation (every record valid under the 020 schema remains valid under the 021 schema; new records with `null` economy fields become valid for the first time).

## Why a delta and not a new schema

The 020 result.schema.json is the canonical "terminal result record" contract for trainer iterations. This feature does not own that schema and does not want to fork it — there is one schema, used by both features, and the changes here are strictly additive. We document the delta in this file rather than rewriting `020/contracts/result.schema.json` because:

1. The 020 schema is referenced by the 020 spec's data-model and tests; touching it would re-open 020's gates.
2. Every change here is a pure relaxation (no new required field, no new constraint, no removed field). Old consumers continue to round-trip every old record correctly.
3. If a future feature consolidates the schemas, it will see exactly one delta to apply.

The implementation MUST update `specs/020-bot-iterative-trainer/contracts/result.schema.json` in place with the changes below as part of this feature's tasks (see `tasks.md` once `/speckit.tasks` runs). The 020 spec text does not change; only its schema artifact.

## Change 1 — `telemetry.peak_metal` and `telemetry.peak_energy` may be `null`

**Was**:

```json
"peak_metal": {
  "type": "number",
  "minimum": 0,
  "description": "Highest observed Metal.Current during the match."
}
```

**Becomes**:

```json
"peak_metal": {
  "type": ["number", "null"],
  "minimum": 0,
  "description": "Highest observed Metal.Current during the match. Null when the proxy returned Single.NaN for the resource for every frame of the match (e.g. an invalid resource id was queried), per feature 021 FR-003."
}
```

The same change applies to `peak_energy`.

**Backwards compatibility**: Every record that was valid under the old schema remains valid (a `number` is still a member of `["number", "null"]`). The new schema adds *only* the `null` possibility for the case that the 020 schema implicitly forbade (which never actually arose under feature 020 because the proxy returned 0, not NaN, when the callbacks were broken).

**Why not keep emitting `0` for the all-NaN case**: zero is a real telemetry value (a match where the bot built nothing and never accumulated metal does legitimately have `peak_metal = 0`). Conflating "real zero" with "callback unavailable" silently corrupts the stall check (`peak_metal=0` always counts as no improvement). `null` makes the distinction explicit and lets the stall checker apply the FR-015 NaN-skip rule correctly.

## Change 2 — Add an optional sibling artifact: `unwired_commands.json`

This is **not** a change to `result.schema.json` — it is a new sibling file in the run directory, written by `bots/trainer/run.sh` as part of the FR-004 implementation (Decision 4 in `research.md`). It is documented here because it ships alongside `result.json` and the run-directory contract should know about it.

**Path**: `<run_dir>/unwired_commands.json`
**Required**: yes — written on every successful (non-interrupted) run, even when the count is zero
**Schema**:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://fsbarv1.local/schemas/021-rerun-trainer-highbar/unwired_commands.schema.json",
  "title": "Unwired Command Counts",
  "description": "Post-match grep of the engine infolog for proxy 'rc=-2' lines, grouped by protobuf oneof case name. Surfaces FR-004's 'command type not wired in proxy' classification without modifying BarClient.SendCommands.",
  "type": "object",
  "additionalProperties": false,
  "required": ["rc_minus_2_count", "by_case"],
  "properties": {
    "rc_minus_2_count": {
      "type": "integer",
      "minimum": 0,
      "description": "Total count of rc=-2 lines in engine.infolog/engine.stderr/engine.stdout for this run."
    },
    "by_case": {
      "type": "object",
      "additionalProperties": { "type": "integer", "minimum": 0 },
      "description": "Map from protobuf oneof case name (e.g. 'PatrolCommand') to the count of rc=-2 occurrences for that case in the match. Empty object when rc_minus_2_count is 0."
    }
  }
}
```

**Consumer**: PLAYBOOK §3 classification step. A non-zero `rc_minus_2_count` does **not** by itself classify an iteration's outcome — it tells the operator "the bot tried to send a command type the proxy never wired" so the operator can either remove that command type from the bot or file a HighBarV2 inbound mailbox per FR-021.

## Change 3 — Add an optional sibling artifact: `attack_probe.json`

Also new, also a sibling file, also written by the bot (not by `run.sh`) when the bot script's tactics callback contains the Issue 1 probe instrumentation. This file is **optional** at the run-directory level: not every iteration runs the probe (FR-017 only requires "at least one iteration in this feature"), and iterations without probe instrumentation simply omit the file.

**Path**: `<run_dir>/attack_probe.json`
**Required**: no — only present when the iteration's bot script wired the probe
**Schema**:

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://fsbarv1.local/schemas/021-rerun-trainer-highbar/attack_probe.schema.json",
  "title": "Issue 1 AttackCommand getUnitPos Probe",
  "description": "Per-iteration record of one AttackCommand send with before/after position observations on the issuing unit. Closes the FSBarV1-side commitment in Mailbox/2026-04-12_to_FSBarV1_proxy_fixes_complete.md Issue 1 follow-up.",
  "type": "object",
  "additionalProperties": false,
  "required": ["issuing_unit_id", "issuing_unit_def", "target_unit_id", "frame_at_send", "pos_before", "frame_at_check", "pos_after", "outcome"],
  "properties": {
    "issuing_unit_id": { "type": "integer" },
    "issuing_unit_def": { "type": "string" },
    "target_unit_id":   { "type": "integer" },
    "frame_at_send":    { "type": "integer", "minimum": 0 },
    "pos_before": {
      "type": "array",
      "items": { "type": "number" },
      "minItems": 3,
      "maxItems": 3,
      "description": "World-space [x,y,z] of the issuing unit immediately before the AttackCommand send."
    },
    "frame_at_check":   { "type": "integer", "minimum": 0 },
    "pos_after": {
      "type": "array",
      "items": { "type": "number" },
      "minItems": 3,
      "maxItems": 3,
      "description": "World-space [x,y,z] of the issuing unit at frame_at_check (frame_at_send + 30 frames). Same coordinate frame as pos_before."
    },
    "outcome": {
      "type": "string",
      "enum": ["moved", "stationary", "destroyed"],
      "description": "Interpretation: 'moved' if Euclidean(pos_before, pos_after) > 5.0 game units; 'destroyed' if the issuing unit is no longer in client.GameState.Units at frame_at_check; 'stationary' otherwise."
    }
  }
}
```

**Consumer**: SC-008 (probe interpretation must be referenced in `HISTORY.md` or in an outbound mailbox) and FR-018 (closes the upstream Issue 1 follow-up).

## What does NOT change

- `result.schema.json` field names, ordering, required-ness (other than the `null` relaxation above).
- `frame.schema.json`, `meta.schema.json`, `ladder.schema.json` from feature 020 — completely unchanged.
- `run-directory.md` from feature 020 — only the *list of files* in the run directory is implicitly extended by the two new sibling artifacts; the existing files keep their schemas and roles.
- The wire protocol (`.proto` files, FsGrpc generated types, FSBar.Client public API surface). The HighBarV2 proxy fixes are all behavioural inside the existing protobuf message shapes; no new fields, no new message types.
