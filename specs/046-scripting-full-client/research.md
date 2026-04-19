# Phase 0 — Research

All Technical Context slots are filled; no NEEDS CLARIFICATION remain
(five questions were resolved in `spec.md` §Clarifications, Session
2026-04-19). The investigations below capture the load-bearing
design choices.

## Decision 1 — State + events share the `StreamGameFrames` stream

- **Decision**: Extend `GameFrameMessage` with `game_state` (GameStateFrame)
  and `game_events` (repeated GameEventEnvelope) fields. No new
  `StreamGameState` RPC.
- **Rationale**: Clarification Q1 fixed this. One stream guarantees
  state and events cannot reorder relative to each other; one
  subscription; one fan-out / back-pressure path (reuses the existing
  bounded-channel + drop-on-slow-client behavior — FR-010). Reader
  trivially co-indexes events with the snapshot they belong to.
- **Alternatives considered**:
  - Separate `StreamGameState` RPC: doubles subscriptions, introduces
    cross-stream ordering race (rejected).
  - Periodic unary `GetGameState`: requires client polling loop,
    drops event granularity (rejected).

## Decision 2 — Source of truth is `BarClient.GameState`

- **Decision**: `ScriptingHub` projects directly from the Hub-resident
  `BarClient.GameState` snapshot produced by feature 045's batched
  `CALLBACK_GAME_GET_STATE = 15` path.
- **Rationale**: FR-011 mandates no new engine round-trips. Feature
  045 landed this Hub-authoritative view; every Hub tab already reads
  from it. Projection is pure data mapping.
- **Alternatives considered**: Re-issuing per-unit callbacks from the
  scripting path — rejected: duplicates engine work, violates FR-011,
  regresses SC-003 latency budget.

## Decision 3 — `oneof health_info { float health; google.protobuf.Empty unknown; }`

- **Decision**: Encode enemy health as an explicit `oneof` discriminator.
- **Rationale**: Clarification Q3. Radar-only and frozen-last-known
  enemies must be distinguishable from "visible with 0 HP" (a dying
  unit). A sentinel (`-1`, `NaN`) is error-prone across languages;
  `oneof` forces the consumer to match both arms.
- **Alternatives considered**:
  - Separate `optional float health` + `bool health_known`: two
    fields that can disagree (rejected).
  - Negative sentinel: silent bug magnet in languages without nullable
    float (rejected).

## Decision 4 — Grid payloads: unary + `repeated float`/`int32` + raise message-size

- **Decision**: Each map grid (heightmap, slope, LOS, radar, resource,
  corners heightmap) returns a unary response with `width`, `height`,
  and a natural `repeated float` or `repeated int32`. The Hub
  scripting server channel bumps `MaxReceiveMessageSize` /
  `MaxSendMessageSize` enough to fit the worst-case SupportedMap
  grid payload.
- **Rationale**: Clarification Q4. Keeps the wire readable, matches the
  shape `FSBar.Client.Callbacks` already returns, and avoids bespoke
  chunking that every client would need to re-implement. Grid sizes
  are bounded by SupportedMaps (a few MiB worst case) so a one-time
  limit bump is acceptable.
- **Alternatives considered**:
  - `bytes` blob: forces clients to know the F# packing (rejected).
  - Server-streaming chunks: client-side reassembly boilerplate,
    no measurable throughput win at these sizes (rejected).

## Decision 5 — Proxy cadence unchanged; one message per proxy frame

- **Decision**: The scripting frame stream emits exactly one
  `GameFrameMessage` per proxy-emitted frame per subscriber. Proxy
  cadence (currently 1 Hz) is a proxy-side concern, out of scope.
- **Rationale**: Clarification Q2. Matches the existing
  `StreamGameFrames` contract; no new backpressure semantics; SC-003
  latency gate is "added latency on top of proxy cadence", not
  absolute tick rate.

## Decision 6 — `SendCommandBatch` cap = 1024, whole-batch rejection above cap

- **Decision**: Server rejects any batch with >1024 entries whole (no
  partial forward) and returns a diagnostic naming the cap and the
  received size. Accepted batches return a single
  `forwarded_at_frame` plus a per-entry accepted/rejected diagnostic
  list (for individual invalid commands, e.g., bad target id).
- **Rationale**: Clarification Q5. Cap keeps per-RPC worst case
  bounded; whole-batch-rejection preserves caller intent ("all or
  nothing at this size") and avoids ambiguity. Per-entry diagnostics
  within a valid batch mirror existing Hub reject patterns.
- **Alternatives considered**:
  - Truncate silently: surprises caller (rejected).
  - Partial forward of first 1024: splits ordering guarantee (rejected).

## Decision 7 — Additive-only proto evolution

- **Decision**: No existing `fsbar.hub.scripting.v1` field number is
  reused or renamed. The Phase-9 `events = []` placeholder is
  superseded by the new populated `game_events` field (same field
  number if compatible, else reserved and replaced additively).
  `buf breaking` must pass clean (SC-005, FR-015).
- **Rationale**: Existing FSI walkthroughs 15..22 and downstream
  scripting clients must keep working unchanged.

## Decision 8 — `UnitDefInfoExtended` as a new message, not mutation

- **Decision**: Introduce `UnitDefInfoExtended` (superset: build
  options, max weapon range, build speed, build time, cost, sight
  range, footprint, faction). `GetUnitDef` is updated to return the
  extended message; existing field numbers on the underlying
  `UnitDefInfo` are preserved.
- **Rationale**: FR-007 requires the full planning surface; keeping
  the base type's field numbers stable keeps the change additive.

## Open items

None. All five clarification questions resolved; all FR / SC items
map to a decision above or are pure implementation detail picked up
in Phase 1.
