# Phase 1 — Data Model

All entities live on the wire as `proto3` messages in
`proto/hub/scripting.proto` (package `fsbar.hub.scripting.v1`).
Underlying F# source of truth for state entities is
`BarClient.GameState` (FSBar.Client, feature 045). Field numbers
below are illustrative — final numbers are assigned at proto-authoring
time subject to the "no reuse of existing numbers" rule (FR-015).

## Entities

### GameStateFrame

Per-tick projection of `BarClient.GameState`. Carried as the
`game_state` field on `GameFrameMessage`.

| Field | Type | Source | Notes |
|---|---|---|---|
| frame_number | int32 | `GameState.FrameNumber` | monotonically non-decreasing across consecutive messages (US1 acceptance 1) |
| team_id | int32 | `GameState.TeamId` | |
| friendlies | repeated FriendlyUnitState | `GameState.Friendlies` | |
| enemies | repeated EnemyUnitState | `GameState.Enemies` | union of LOS + radar-only + frozen-last-known |
| economy | EconomySnapshotWire | `GameState.Economy` | 8-field |

### FriendlyUnitState

| Field | Type | Source | Notes |
|---|---|---|---|
| unit_id | int32 | engine | |
| def_id | int32 | engine | |
| position | highbar.Vec3 | engine | reuses highbar common type |
| health | float | engine | |
| max_health | float | engine | |
| finished | bool | engine | |
| idle | bool | engine | |

### EnemyUnitState

| Field | Type | Source | Notes |
|---|---|---|---|
| unit_id | int32 | engine | |
| def_id | optional int32 | engine | absent iff engine has not disclosed the def (radar-only of unknown class) |
| position | highbar.Vec3 | engine / last-known | frozen value when absent this tick (FR-003) |
| health_info | oneof { float health; google.protobuf.Empty unknown; } | engine | `unknown` for radar-only and frozen-last-known; `health = 0` is legitimate "dying" |
| in_los | bool | engine | cleared for frozen-last-known |
| in_radar | bool | engine | cleared for frozen-last-known |

### EconomySnapshotWire

Metal & Energy each with: current, income, usage, storage (8 float fields total).

### GameEventEnvelope

Decoded `FSBar.Client.Events.GameEvent` union on the wire. One
`oneof payload` with one variant per F# case (Init, UnitCreated,
UnitFinished, UnitIdle, UnitDestroyed, UnitGiven, UnitCaptured,
EnemyEnterLOS, EnemyLeaveLOS, EnemyEnterRadar, EnemyLeaveRadar,
EnemyDestroyed, EnemyCreated, EnemyFinished, Update, Shutdown).
Carried in `repeated GameEventEnvelope game_events` on
`GameFrameMessage`; events are in arrival order within a frame.
The Phase-9 placeholder `events = []` is superseded.

### Map-query responses

All unary. Grid responses carry `width`, `height`, and the raw grid
as `repeated float` or `repeated int32`.

| Message | Payload shape | Source |
|---|---|---|
| MapInfoResponse | width, height, data_dir, map_name | `Callbacks.getMapInfo` |
| HeightmapResponse | width, height, repeated float heights | `Callbacks.getHeightMap` |
| CornersHeightmapResponse | width, height, repeated float heights | `Callbacks.getCornersHeightMap` |
| SlopeMapResponse | width, height, repeated float slopes | `Callbacks.getSlopeMap` |
| LosMapResponse | width, height, repeated int32 los | `Callbacks.getLosMap` |
| RadarMapResponse | width, height, repeated int32 radar | `Callbacks.getRadarMap` |
| ResourceMapResponse | width, height, repeated float values | `Callbacks.getResourceMap` (metal density) |
| MetalSpotsResponse | repeated MetalSpot { x, y, z, metal_value } | `Callbacks.getMetalSpots` |

Hub scripting server channel raises `MaxReceiveMessageSize` /
`MaxSendMessageSize` enough to fit worst-case SupportedMap grids.

### UnitDefInfoExtended

Superset of current `UnitDefInfo`. Returned by `GetUnitDef`.

| Field | Type | Notes |
|---|---|---|
| internal_name | string | already present |
| display_name | string | already present |
| faction | string/enum | already present |
| cost | Cost { metal, energy } | new |
| build_time | float | new |
| build_speed | float | new |
| sight_range_elmo | float | new |
| max_weapon_range | float | new (0 for non-combat) |
| footprint_x | int32 | new |
| footprint_z | int32 | new |
| build_options | repeated int32 | new, def ids |

### SendCommandBatchRequest / Response

Request:
- `repeated highbar.AICommand commands` — ordered; ≤1024 entries
- optional `string correlation_id` hint (the header-based
  correlation id from FR-014 remains authoritative)

Response:
- `int32 forwarded_at_frame` — single frame for the whole batch
- `repeated CommandOutcome { int32 index; bool accepted; string diagnostic; }`
- If the request exceeds 1024 entries: response carries zero
  outcomes and a single rejection diagnostic naming the cap and
  received size; no commands are forwarded.

## Relationships

- `GameFrameMessage` — contains one `GameStateFrame` + zero-or-more
  `GameEventEnvelope`. Events observed between the previous and
  current frame are carried in the current message.
- `BarClient.GameState` (F#) → `GameStateFrame` (wire): pure
  projection; no new engine calls.
- `FSBar.Client.Callbacks.*` (F#) → map-query response messages:
  direct field mapping; no caching in scripting path (SC-007
  byte-identity is with the committed map cache via the existing
  FSBar.Viz/trainer paths).
- `highbar.AICommand` (existing) — reused unchanged by
  `SendCommand` and `SendCommandBatch`. FR-009 requires every
  variant to have an end-to-end live test.

## Invariants

- **Monotonic frames** (US1 A1): consecutive `GameStateFrame` values
  on one subscriber stream have non-decreasing `frame_number`.
- **Health discriminator totality** (FR-003): every `EnemyUnitState`
  sets exactly one arm of `health_info`.
- **Batch atomicity** (FR-008): an oversize batch forwards zero
  commands.
- **Additive proto** (FR-015): no existing field number renamed or
  repurposed; `buf breaking` reports zero incompatibilities.
- **Single source of truth** (FR-011): state messages never trigger
  per-unit engine round-trips in the scripting path.
