# Feature Specification: Fully comprehensive scripting gRPC client

**Feature Branch**: `046-scripting-full-client`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "make the scripting client grpc a fully comprehensive client, that has all the gamestate data. can issue all commands. basically a fully fledged headless game client."

## Context

Today the Hub's scripting gRPC surface (`fsbar.hub.scripting.v1`) supports
enough to *observe a session from the outside* (frame tick pulses, render
frames, Hub state events, log stream), *drive lifecycle* (launch / stop /
pause / resume / speed / force-end / admin-message), *look up static
encyclopedia data* (`ListUnits` / `GetUnitDef` / `SelectUnit`), and *push
a single command per RPC* (`SendCommand`). It deliberately does **not**
project the live `BarClient.GameState` to the wire: `StreamGameFrames`
returns `highbar.Frame { frame_number, team_id, events = [] }` — the
proto comment flags this as a Phase-9 deferral.

Consequently, a scripting client running over gRPC cannot today function
as a BAR bot. It cannot see friendly units, enemies, or economy; it
cannot react to gameplay events (`UnitCreated`, `EnemyEnterLOS`,
`UnitDestroyed`); it cannot query map data (heightmap, metal spots, LOS
/ radar maps) needed for planning; and it cannot see full unit-def
attributes (build options, weapon ranges) needed for decision-making.

Feature 045 has just landed: FSBar.Client now refreshes the entire
per-tick GameState (friendlies + LOS/radar enemies + economy) via one
batched `CALLBACK_GAME_GET_STATE = 15` RPC. `BarClient.GameState` is the
authoritative, already-computed per-tick snapshot that the Hub tabs
read from — so projecting it to scripting clients is mostly plumbing,
not new engine-side work.

This feature makes the scripting gRPC surface a fully-fledged headless
game client: a consumer written in any gRPC-capable language can read
complete per-tick state, subscribe to typed gameplay events, query map
and unit-def data, and issue any AI command — without ever running F#
code or linking FSBar.Client.

## Clarifications

### Session 2026-04-19

- Q: How should the new per-tick GameState be delivered on the wire? → A: Extend `GameFrameMessage` on the existing `StreamGameFrames` RPC with `game_state` + `game_events` fields (single unified stream).
- Q: At what cadence does each subscriber receive `GameFrameMessage`? → A: One message per proxy-emitted frame (currently 1/sec, driven by the proxy's fixed cadence; not configurable from this feature). Every proxy-emitted frame is processed and forwarded; `GameState` is the snapshot as of that frame and `game_events` carries every event observed since the previous frame.
- Q: How should the "unknown health" distinction for enemies be encoded on the wire? → A: `oneof health_info { float health; google.protobuf.Empty unknown; }` — first-class discriminator, impossible to confuse with `health = 0`.
- Q: How should the heightmap and sibling large grids be returned on the wire? → A: Unary RPCs with natural repeated `float` / `int32` fields (plus width/height). The Hub's scripting channel `MaxReceiveMessageSize` / `MaxSendMessageSize` MUST be raised to fit the worst-case SupportedMap grid payload.
- Q: Should `SendCommandBatch` enforce a maximum batch size? → A: Server-side cap of 1024 commands per batch; oversize batches are rejected whole (no partial forward) with a diagnostic naming the cap and received size.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Per-tick GameState readout over gRPC (Priority: P1) 🎯 MVP

As a developer writing a BAR bot in Python / Go / TypeScript over gRPC,
I want each tick of the live session to deliver a complete GameState
snapshot (frame number + friendly units + LOS enemies + radar-only
enemies + 8-field economy) so that I can make tactical and strategic
decisions from outside the F# process.

**Why this priority**: Without this, a gRPC scripting client is blind
to the game. Every other capability (events, map queries) is only
useful once the client can see unit positions, health, and economy.
This is the one change that transforms the scripting surface from
"observer" to "player".

**Independent Test**: Launch a live session via `LaunchSession`, attach
a gRPC scripting client, subscribe to the per-tick state stream, and
assert that within 10 ticks the client has received a snapshot with at
least one friendly unit (the commander), a populated economy record,
and that the frame numbers on consecutive snapshots are monotonically
non-decreasing.

**Acceptance Scenarios**:

1. **Given** an active session with `N` friendlies and `M` visible
   enemies, **When** the scripting client consumes the next per-tick
   state message, **Then** it contains exactly `N` friendly entries and
   `M` enemy entries (sum of LOS + radar-only), with positions and
   health matching what the Hub GUI's Viewer tab displays for the same
   frame within float tolerance.
2. **Given** an enemy that is in radar only (not in LOS), **When** that
   enemy appears in a snapshot, **Then** the wire form carries a
   concrete position but an "unknown health" indicator — never a zero
   or negative sentinel.
3. **Given** an enemy that was in LOS last tick and is in neither LOS
   nor radar this tick, **When** the new snapshot is delivered, **Then**
   the enemy is still present with its last-known position frozen,
   health marked unknown, and both contact flags cleared (mirrors the
   FSBar.Client frozen-last-known-position rule from feature 045).
4. **Given** no session is active, **When** the scripting client
   subscribes to the state stream, **Then** it either waits for the
   next session start (long-poll) or receives an explicit
   "no-session" terminal message, per the existing `close_on_session_end`
   pattern used by `StreamGameFrames`.
5. **Given** a session that ends, **When** the state stream is open
   with `close_on_session_end = true`, **Then** the stream closes
   cleanly; with `close_on_session_end = false`, **Then** the stream
   stays open and resumes delivering state on the next session.

---

### User Story 2 — Typed gameplay events on the frame stream (Priority: P2)

As a bot author, I want each gRPC frame message to also carry the
decoded gameplay events that fired on that tick (`Init`,
`UnitCreated`, `UnitFinished`, `UnitIdle`, `UnitDestroyed`, `UnitGiven`,
`UnitCaptured`, `EnemyEnterLOS`, `EnemyLeaveLOS`, `EnemyEnterRadar`,
`EnemyLeaveRadar`, `EnemyDestroyed`, `EnemyCreated`, `EnemyFinished`,
`Update`, `Shutdown`) so that I can react to unit lifecycle changes
without re-deriving them by diffing consecutive state snapshots.

**Why this priority**: Event-driven reactions (e.g., "build a repairer
when `UnitFinished` fires for a commander") are the natural way to
structure a bot. Without typed events, every client would have to
implement diff-based event synthesis, duplicating logic that already
lives in `FSBar.Client.Events`. This is P2 because state readout (US1)
alone is sufficient for many decision loops; events are an
ergonomics-and-latency win on top.

**Independent Test**: Launch a session and assert that the first 5
frames delivered to a scripting client collectively contain at least
one `Init` event (with a valid team id) and one `UnitCreated` event
(with a unit id > 0), and that the frame numbers on the events align
with the enclosing frame's `frame_number`.

**Acceptance Scenarios**:

1. **Given** the scripting client is subscribed to the frame stream,
   **When** the engine emits a `UnitCreated` event for the commander,
   **Then** the corresponding wire frame carries that event with unit
   id and (if known) builder id, on the same tick the Hub-side
   `BarClient.GameState` observed it.
2. **Given** an enemy unit transitions from radar-only to LOS,
   **When** the scripting client consumes the frame containing the
   `EnemyEnterLOS` event, **Then** the event is present with the
   enemy's unit id before that frame's state snapshot reflects the
   new LOS membership.
3. **Given** the session is shutting down, **When** the scripting
   client consumes the final frame, **Then** it carries a `Shutdown`
   event and no further frames are delivered.

---

### User Story 3 — Map and extended unit-def queries (Priority: P2)

As a bot author, I want to query static map data (dimensions,
heightmap, slope map, LOS / radar grids, metal spots, corners
heightmap) and the complete unit-def surface (build options, weapon
ranges, cost, build speed, build time, sight range) over gRPC so that
I can plan construction, pathing, and base layout from the scripting
client.

**Why this priority**: A bot that can see units but not the terrain
it's fighting on cannot plan a base or a push. These are
already-computed data in the live session (`FSBar.Client.Callbacks`
provides every query and the Hub already invokes them during warmup
for its own renderer). Exposing them is additive plumbing. This is
P2 because a bot can still act reactively on unit positions alone —
map queries are required for *deliberate* play, not basic play.

**Independent Test**: With a live session on a known map
(e.g., `Red_Comet`), the scripting client calls `GetMapInfo` and
asserts the returned width × height matches the committed
per-map cache for that map. It also calls `ListMetalSpots` and
asserts the result is non-empty and each spot has `metalValue > 0`.
It calls `GetUnitDef` for the commander def id and asserts at least
one build option is returned.

**Acceptance Scenarios**:

1. **Given** a live session on `Red_Comet`, **When** the client calls
   `GetMapInfo`, **Then** the returned width, height, and data dir
   match the engine's values for that map.
2. **Given** a live session, **When** the client calls
   `ListMetalSpots`, **Then** it receives a list of `(x, y, z,
   metalValue)` entries with at least one entry and every
   `metalValue > 0`.
3. **Given** a live session, **When** the client calls `GetUnitDef`
   for a commander def id, **Then** the response includes
   `build_options` (non-empty array of def ids), `max_weapon_range`
   (> 0), `cost`, `build_time`, `build_speed`, `sight_range_elmo`,
   and `footprint_x/z` — the full planning-relevant surface, not the
   encyclopedia-only subset exposed today.

---

### User Story 4 — Complete command surface + batch submission (Priority: P3)

As a bot author, I want to issue any valid `AICommand` (move, stop,
patrol, guard, attack, fight, build, repair, reclaim, self-destruct,
set-state, etc.) and to batch multiple commands into one RPC, so that
my tick loop can enqueue an entire wave of orders without N round-trips.

**Why this priority**: The existing `SendCommand` takes one
`highbar.AICommand` per call, which is enough to drive a functional
bot but scales poorly when a commander's tick needs to retask 50
units. Command *types* are already fully defined in
`proto/highbar/commands.proto`; the gap is ergonomic (batch) and
confirmation that every command type has a working end-to-end
scripting path. This is P3 because a correct-but-chatty bot can still
play.

**Independent Test**: The scripting client calls `SendCommandBatch`
with three distinct commands (one move, one build, one patrol) for
three different units, and receives a single response indicating the
frame at which they were forwarded. After 10 ticks, a follow-up
`GetGameState` confirms the issuing units have non-empty current-command
lists or have begun moving.

**Acceptance Scenarios**:

1. **Given** a live session with at least one idle builder, **When**
   the client sends one `BuildCommand` via `SendCommandBatch`, **Then**
   within 30 ticks a `UnitCreated` event fires for the built def id.
2. **Given** a batch of 50 move commands in one `SendCommandBatch`
   RPC, **When** the server forwards them, **Then** they are all
   forwarded on the same frame (single `forwarded_at_frame` value in
   the response), rather than being spread across multiple ticks.
3. **Given** an invalid command (e.g., target id that does not exist),
   **When** the client submits it, **Then** the response carries a
   per-command diagnostic indicating which entry was rejected and why,
   and accepted commands in the same batch are still forwarded.

---

### Edge Cases

- **Large army** (e.g., 200 friendlies + 50 enemies): per-tick state
  message size must not stall the stream. The batched snapshot already
  handles this on the FSBar.Client side; the projection must not
  re-introduce a bottleneck.
- **Map with no metal spots** (synthetic scenario): `ListMetalSpots`
  returns an empty list, not an error.
- **Client subscribes mid-game**: the first per-tick state message
  delivered is the one associated with the next tick — not a replay
  of historical state.
- **Multiple scripting clients**: each gets its own view of the state
  stream with its own back-pressure, reusing the existing fan-out /
  drop-on-slow-client behavior from `StreamGameFrames`.
- **Proxy version shortfall** (pre-0.1.5): the session fails at
  `LaunchSession` time per feature 045, so the scripting state stream
  is never established — no new error surface here.
- **Event burst during a callback batch** (e.g., UnitDefCache warmup):
  events still arrive in order on the frame stream, preserving the
  feature-031 / feature-022 replay-buffer ordering; the scripting
  client never sees a reordered or dropped event.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The existing `StreamGameFrames` RPC MUST be extended so
  each `GameFrameMessage` carries the authoritative per-tick
  `GameState` (frame number + team id + friendly units + enemies +
  economy) alongside the typed `game_events` field. No separate
  `StreamGameState` RPC is introduced — state and events travel on
  the same stream so they cannot reorder relative to each other.
- **FR-002**: Each friendly entry in the per-tick state MUST carry
  unit id, unit-def id, position, current health, max health, finished
  flag, and idle flag — the same shape the Hub Viewer tab consumes.
- **FR-003**: Each enemy entry in the per-tick state MUST carry unit
  id, optional unit-def id, position, a protobuf
  `oneof health_info { float health; google.protobuf.Empty unknown; }`
  discriminator, and both `in_los` / `in_radar` flags. Radar-only
  entries MUST set the `unknown` branch; enemies absent from the
  snapshot MUST set the `unknown` branch with their last-known
  position frozen. A `health = 0` value on the `health` branch is a
  legitimate "visible-but-dying" reading, distinct from "unknown".
- **FR-004**: Each per-tick economy record MUST carry all eight fields
  (metal / energy × current / income / usage / storage).
- **FR-005**: The scripting frame stream MUST populate a typed
  `game_events` field on each `GameFrameMessage` with decoded
  `GameEvent` entries (Init, UnitCreated, UnitFinished, UnitIdle,
  UnitDestroyed, UnitGiven, UnitCaptured, EnemyEnterLOS, EnemyLeaveLOS,
  EnemyEnterRadar, EnemyLeaveRadar, EnemyDestroyed, EnemyCreated,
  EnemyFinished, Update, Shutdown). The Phase-9 `events = []` placeholder
  MUST be removed.
- **FR-006**: Scripting clients MUST be able to query static map data
  over gRPC via unary RPCs: map dimensions, heightmap, corners
  heightmap, slope map, LOS map, radar map, resource/metal-density
  map, and the typed metal-spots list. Grid responses MUST carry the
  grid as natural `repeated float` / `repeated int32` fields plus
  width/height (no chunking, no `bytes` encoding). The Hub scripting
  server and the default client channel MUST configure
  `MaxReceiveMessageSize` / `MaxSendMessageSize` large enough to fit
  the worst-case SupportedMap grid payload. Each map query MUST
  return the same values the FSBar.Viz headless renderer already
  uses.
- **FR-007**: The `GetUnitDef` response MUST expose the complete
  planning-relevant attribute set: internal name, display name, build
  options, max weapon range, cost, build time, build speed, sight
  range, footprint, and faction — not a subset.
- **FR-008**: The Hub MUST expose a new `SendCommandBatch` RPC that
  accepts an ordered list of `AICommand` entries (maximum 1024 per
  call) and returns a single response naming the frame at which the
  batch was forwarded plus a per-entry accepted/rejected diagnostic.
  A batch exceeding 1024 entries MUST be rejected whole (no entries
  forwarded) with a diagnostic naming the cap and the received size.
- **FR-009**: Every `highbar.AICommand` variant that the engine accepts
  (Move, Stop, Patrol, Guard, Attack, Fight, Build, Repair, Reclaim,
  SelfDestruct, SetState, and any other already in the proto) MUST
  have a verified end-to-end path from scripting RPC call to engine
  execution, covered by at least one live test.
- **FR-010**: The per-tick state stream MUST reuse the existing fan-out
  and back-pressure behavior of `StreamGameFrames` (bounded per-client
  channel, drop-on-slow-client, correlation-id pass-through).
- **FR-011**: The per-tick state MUST derive directly from
  `BarClient.GameState` (the authoritative Hub-side view) — no
  additional per-unit / per-enemy / per-economy RPCs MUST be issued
  to the engine to satisfy a scripting state request. Feature 045's
  batched-snapshot path is the single source of truth.
- **FR-012**: When no live session exists, each scripting state /
  frame / map / unit-def RPC MUST return a well-formed empty or
  "no-session" response — not hang indefinitely and not crash the
  Hub. The `close_on_session_end` semantics already in
  `StreamGameFrames` apply uniformly.
- **FR-013**: The scripting-service surface-area additions MUST be
  documented via (a) the existing `.proto` file contract, (b) a new
  numbered FSI example exercising the new RPCs end-to-end, and (c)
  updated entries in the Hub scripting README / `CLAUDE.md`.
- **FR-014**: All new RPCs MUST flow correlation ids through the
  existing `x-fsbar-correlation-id` header interceptor (feature 042),
  so log entries and Hub state events triggered by a scripting call
  remain attributable to the caller.
- **FR-015**: The new wire types MUST NOT reuse or rename any existing
  `fsbar.hub.scripting.v1` field number; they MUST extend the service
  additively. `buf breaking` over the scripting proto MUST report zero
  incompatibilities after the change lands.

### Key Entities

- **GameStateFrame**: Per-tick projection of `BarClient.GameState` —
  frame number, team id, friendly list, enemy list, economy record.
  Carried as a `game_state` field on `GameFrameMessage` (same stream
  as `game_events`); one message per proxy-emitted frame per
  subscriber (proxy cadence is currently 1 Hz; tuning that cadence
  is a proxy-side concern, out of scope for this feature).
- **FriendlyUnitState**: Friendly unit on the wire — id, def id,
  position, health, max health, finished flag, idle flag.
- **EnemyUnitState**: Enemy unit on the wire — id, optional def id,
  position, `oneof health_info { float health; google.protobuf.Empty
  unknown; }`, in-LOS flag, in-radar flag.
- **EconomySnapshotWire**: Eight-field economy record on the wire.
- **GameEventEnvelope**: Decoded `GameEvent` union on the wire, with
  one variant per FSBar.Client `GameEvent` case. Carried inside
  `GameFrameMessage.game_events`.
- **MapInfoResponse / HeightmapResponse / MetalSpotsResponse /
  LosMapResponse / RadarMapResponse / SlopeMapResponse /
  ResourceMapResponse / CornersHeightmapResponse**: One-shot
  responses mirroring the corresponding `FSBar.Client.Callbacks`
  return types.
- **UnitDefInfoExtended**: Superset of today's `UnitDefInfo` — adds
  build options, max weapon range, build speed, build time, cost,
  sight range, footprint.
- **SendCommandBatchRequest / Response**: Ordered list of `AICommand`
  entries plus a per-entry accepted / rejected diagnostic.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A scripting client written in a non-F# language (e.g.,
  Python) can start a session, subscribe to the per-tick state stream,
  and implement a "build one metal extractor per visible metal spot"
  bot without ever calling into FSBar.Client.
- **SC-002**: Per-tick state message size at 200 friendlies + 50
  enemies stays under 64 KiB wire bytes (sanity ceiling, not a
  regression gate).
- **SC-003**: End-to-end state latency (FSBar.Client observing a
  proxy-emitted frame → scripting client receives the decoded
  message) adds under 30 ms on top of the proxy's own cadence
  (currently 1 Hz) under the live-test harness on the reference
  machine. The proxy cadence itself is not a gate for this feature.
- **SC-004**: A live-integration test drives at least one of every
  FR-009 command type and observes the expected side effect within a
  bounded tick window; zero command types are marked "not wired".
- **SC-005**: `buf breaking` on the scripting proto before vs. after
  this feature reports zero breaking changes; existing scripting
  clients (e.g., the existing FSI examples `15..22`) continue to work
  unchanged.
- **SC-006**: A new FSI walkthrough (one of the `NN-hub-*.fsx`
  scripts) demonstrates an end-to-end loop — launch session, stream
  state + events for N ticks, send a batched command, and print a
  summary — in under 50 lines of code.
- **SC-007**: Map-query RPCs return values byte-identical to the
  committed per-map cache (`bots/trainer/map-cache/*.json`) for the
  three SupportedMaps; any drift is a test failure.

## Assumptions

- **Feature 045 has landed.** The batched-snapshot path is the
  authoritative per-tick state; the scripting projection reads
  `BarClient.GameState` directly and does not reintroduce per-unit
  RPCs.
- **Single-session Hub scope.** The Hub continues to host one active
  session at a time (existing FR-023 from feature 039). Multi-session
  scripting is out of scope.
- **Wire format: Protocol Buffers over gRPC**, continuing the
  existing `fsbar.hub.scripting.v1` service. No new RPC framework,
  transport, or serialization is introduced.
- **Client language**: any gRPC-capable language. The FSI walkthrough
  is written in F# because the repo already hosts F# scripting
  infrastructure, but nothing in this feature's contract is
  F#-specific.
- **Fan-out and back-pressure**: the new per-tick state stream reuses
  the bounded-channel + drop-on-slow-client pattern from
  `StreamGameFrames`. A slow scripting client will see dropped state
  messages (never corruption), matching the existing diagnostic
  surface.
- **Authentication / authorization**: none added in this feature — the
  scripting service remains loopback-only per the existing Hub
  deployment model.
- **No new NuGet dependencies** are required. Proto regeneration uses
  the existing `proto-regen` skill.
- **Map data freshness**: the map-query RPCs return whatever the live
  engine reports for the active session. Pre-computed per-map cache
  files are used only by the trainer warmup path and are not a
  scripting concern.
