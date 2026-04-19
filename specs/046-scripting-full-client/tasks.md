---

description: "Task list for 046-scripting-full-client"
---

# Tasks: Fully comprehensive scripting gRPC client

**Input**: Design documents from `/specs/046-scripting-full-client/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/scripting.proto.md, quickstart.md

**Tests**: Included. Constitution §III mandates automated test evidence for behavior changes; live-engine tests only (no mocks per CLAUDE.md).

**Organization**: Tasks are grouped by user story (US1–US4) so each story can land as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- [P] — parallelizable (different files, no blocking dependency on an incomplete task)
- [Story] — US1..US4; phases without a story label are shared (setup / foundational / polish)

## Path Conventions

Single-project F# solution. Key paths:
- `proto/hub/scripting.proto` — wire surface
- `src/FSBar.Proto/Generated/*.gen.fs` — generated (regen via `/skill proto-regen`)
- `src/FSBar.Hub/ScriptingHub.fs(i)` — server implementation + `.fsi` surface
- `tests/FSBar.Hub.LiveTests/` — live-engine tests
- `scripts/examples/` — FSI walkthroughs

---

## Phase 1: Setup

**Purpose**: pre-work before touching the proto or server code.

- [X] T001 Verify feature-045 batched-snapshot path is present: `grep -n "CALLBACK_GAME_GET_STATE" src/FSBar.Client/` must return hits; abort if absent.
- [X] T002 Confirm `/skill proto-regen` runs clean on the current tree before any edits (baseline: no diff after regen).
- [X] T003 [P] Snapshot current `buf breaking` state on `proto/hub/scripting.proto` (against last release tag) for comparison after the edit — write baseline note into `specs/046-scripting-full-client/research.md` appendix if drift already exists.

---

## Phase 2: Foundational (blocks all user stories)

**Purpose**: additive proto surface, regenerated F# types, raised channel message-size limits, and `.fsi` scaffolding. Nothing user-story-specific can land until this phase compiles.

- [X] T004 Edit `proto/hub/scripting.proto`: add `GameStateFrame`, `FriendlyUnitState`, `EnemyUnitState` (with `oneof health_info { float health; google.protobuf.Empty unknown; }`), `EconomySnapshotWire`, `GameEventEnvelope`. Preserve every existing field number (FR-015).
- [X] T005 Edit `proto/hub/scripting.proto`: extend `GameFrameMessage` with `game_state` (GameStateFrame) and `game_events` (repeated GameEventEnvelope). Reserve or deprecate the Phase-9 `events = []` placeholder; do NOT reuse its number for a different type.
- [X] T006 Edit `proto/hub/scripting.proto`: add map-data messages (`MapInfoResponse`, `HeightmapResponse`, `CornersHeightmapResponse`, `SlopeMapResponse`, `LosMapResponse`, `RadarMapResponse`, `ResourceMapResponse`, `MetalSpotsResponse`, `MetalSpot`) plus the eight unary RPC declarations. Grids use `repeated float` / `repeated int32` + width/height (FR-006).
- [X] T007 Edit `proto/hub/scripting.proto`: add `UnitDefInfoExtended` (superset — build_options, max_weapon_range, cost, build_time, build_speed, sight_range_elmo, footprint_x/z, faction) and update `GetUnitDef` return type. Keep underlying `UnitDefInfo` field numbers stable (FR-007, FR-015).
- [X] T008 Edit `proto/hub/scripting.proto`: add `SendCommandBatchRequest` / `SendCommandBatchResponse` / `CommandOutcome` and the `SendCommandBatch` RPC (FR-008).
- [X] T009 Run `/skill proto-regen`; confirm `src/FSBar.Proto/Generated/*.gen.fs` regenerates cleanly.
- [X] T010 Gate: run `buf breaking` on `proto/hub/scripting.proto`; MUST report zero incompatibilities (SC-005, FR-015). If not, fix the proto and re-run T009.
- [X] T011 [P] In `src/FSBar.Hub/ScriptingHub.fs(i)` raise both `MaxReceiveMessageSize` and `MaxSendMessageSize` on the scripting server channel to fit worst-case SupportedMap grid payload (FR-006). Mirror the change in the default client channel used by FSI examples.
- [X] T012 [P] In `src/FSBar.Hub/ScriptingHub.fsi` add signatures for the new public helpers introduced by later phases (projection function stubs, batch handler) so downstream tasks only fill bodies.
- [X] T013 Confirm `dotnet build FSBarV1.slnx` compiles after proto regen (empty handler bodies are acceptable here — they will be filled per story).

**Checkpoint**: Wire surface exists, generated types compile, message-size limits raised. User-story phases may now start in parallel.

---

## Phase 3: User Story 1 — Per-tick GameState readout (Priority: P1) 🎯 MVP

**Goal**: gRPC clients receive a complete per-tick `GameStateFrame` on `StreamGameFrames`, projected directly from `BarClient.GameState` with no new engine round-trips.

**Independent Test**: Spec US1 — within 10 ticks of `LaunchSession`, a subscriber receives a snapshot with ≥1 friendly (commander), a populated economy, and monotonically non-decreasing frame numbers across consecutive snapshots.

### Tests for US1

- [X] T014 [P] [US1] `tests/FSBar.Hub.LiveTests/StateStreamLiveTests.fs`: test matches US1 Acceptance Scenario 1 — friendlies/enemies count + positions/health match Hub-side `BarClient.GameState` within float tolerance.
- [ ] T015 [P] [US1] Same file: US1 AS2 — radar-only enemy: `health_info` uses the `unknown` arm, position is concrete.
- [ ] T016 [P] [US1] Same file: US1 AS3 — enemy lost from both LOS and radar: still present, frozen last-known position, `unknown` health, both contact flags cleared.
- [ ] T017 [P] [US1] Same file: US1 AS4 and AS5 — no-session long-poll/terminal semantics + `close_on_session_end` true/false.
- [ ] T018 [P] [US1] Same file: SC-002 guard — at 200 friendlies + 50 enemies (use synthetic scene from `FSBar.SyntheticData` if live reaching that scale is impractical, but the test must still go through the real scripting channel), per-message wire bytes < 64 KiB.

### Implementation for US1

- [X] T019 [US1] In `src/FSBar.Hub/ScriptingHub.fs` implement `projectGameState : BarClient.GameState -> GameStateFrame` — pure, no IO, no new callbacks (FR-011). Map friendlies, enemies (LOS + radar + frozen-last-known), economy 8-field.
- [X] T020 [US1] Implement the `health_info` oneof logic in the enemy projector: `unknown` for radar-only + frozen-last-known; `health` for LOS (including health = 0 as "dying"). FR-003 totality test must pass.
- [X] T021 [US1] Wire the projection into the `StreamGameFrames` handler so each proxy-emitted frame produces exactly one `GameFrameMessage` per subscriber with `game_state` populated. Preserve existing fan-out / drop-on-slow-client from today's stream (FR-010).
- [X] T022 [US1] No-session + `close_on_session_end` semantics on the state payload path (FR-012) — match the existing `StreamGameFrames` behavior.
- [X] T023 [US1] Update `src/FSBar.Hub/ScriptingHub.fsi` with the final public surface for US1; regenerate surface-area baseline with `SURFACE_AREA_UPDATE=1 dotnet test --filter FullyQualifiedName~SurfaceArea` and commit the diff.

**Checkpoint**: US1 complete — a Python/Go gRPC client can loop over state messages and see the live session.

---

## Phase 4: User Story 2 — Typed gameplay events on the frame stream (Priority: P2)

**Goal**: each `GameFrameMessage` carries the `GameEvent`s observed since the previous frame, decoded into a typed `oneof` — no client-side diffing.

**Independent Test**: Spec US2 — first 5 frames collectively contain ≥1 `Init` (valid team id) and ≥1 `UnitCreated` (unit id > 0); each event's frame aligns with the enclosing `GameFrameMessage.frame_number`.

### Tests for US2

- [X] T024 [P] [US2] `tests/FSBar.Hub.LiveTests/EventsStreamLiveTests.fs`: US2 AS1 — `UnitCreated` for commander fires on the same tick Hub observes it.
- [ ] T025 [P] [US2] Same file: US2 AS2 — `EnemyEnterLOS` appears in the frame whose state then reflects new LOS membership (ordering guarantee).
- [ ] T026 [P] [US2] Same file: US2 AS3 — on session shutdown, final frame carries `Shutdown`; no further frames delivered.

### Implementation for US2

- [X] T027 [US2] In `src/FSBar.Hub/ScriptingHub.fs` implement `projectGameEvent : FSBar.Client.Events.GameEvent -> GameEventEnvelope` covering all 16 cases listed in FR-005.
- [ ] T028 [US2] Batch each tick's observed events into `GameFrameMessage.game_events` in arrival order (preserves feature-031 / feature-022 replay ordering — FR-005, Edge "Event burst").
- [ ] T029 [US2] Remove the Phase-9 `events = []` placeholder from the wire path; ensure no caller still depends on the empty field (FR-005).
- [ ] T030 [US2] Update `.fsi` + surface-area baseline for new US2 public helpers.

**Checkpoint**: US2 complete — bots can react to typed events without diffing.

---

## Phase 5: User Story 3 — Map and extended unit-def queries (Priority: P2)

**Goal**: unary RPCs return the same map data FSBar.Viz uses; `GetUnitDef` returns the full planning surface.

**Independent Test**: Spec US3 — on `Red_Comet`, `GetMapInfo` width×height matches the per-map cache; `ListMetalSpots` non-empty, each `metalValue > 0`; `GetUnitDef(commander)` returns ≥1 build option.

### Tests for US3

- [X] T031 [P] [US3] `tests/FSBar.Hub.LiveTests/MapQueriesLiveTests.fs`: US3 AS1 — `GetMapInfo` matches engine values.
- [X] T032 [P] [US3] Same file: US3 AS2 — `ListMetalSpots` non-empty, every `metalValue > 0`.
- [X] T033 [P] [US3] Same file: US3 AS3 — `GetUnitDef(commander)` returns build_options / max_weapon_range > 0 / cost / build_time / build_speed / sight_range_elmo / footprint_x/z / faction.
- [ ] T034 [P] [US3] Same file: SC-007 — map-query responses byte-identical to `bots/trainer/map-cache/*.json` across the three SupportedMaps (heightmap + metal spots at minimum).
- [ ] T035 [P] [US3] Edge case: map with no metal spots (synthetic) returns empty list, no error.

### Implementation for US3

- [X] T036 [US3] In `src/FSBar.Hub/ScriptingHub.fs` implement the eight unary map RPC handlers, delegating to `FSBar.Client.Callbacks` (no new caching in the scripting path).
- [X] T037 [US3] Implement grid projection helpers: `repeated float` for heightmap/corners/slope/resource; `repeated int32` for LOS/radar; include width/height.
- [X] T038 [US3] Upgrade `GetUnitDef` handler to populate `UnitDefInfoExtended` — fetch build options, weapon ranges, cost, build speed, build time, sight range, footprint, faction from the existing FSBar.Client unit-def cache.
- [X] T039 [US3] No-session semantics: each unary RPC returns a well-formed "no-session" response (FR-012), not a hang or crash.
- [ ] T040 [US3] Update `.fsi` + surface-area baseline for US3 public helpers.

**Checkpoint**: US3 complete — a bot can plan base layout from terrain + metal spots and pick builds from the full unit-def surface.

---

## Phase 6: User Story 4 — Complete command surface + batch submission (Priority: P3)

**Goal**: `SendCommandBatch` with ≤1024 entries forwarded on a single frame; every `highbar.AICommand` variant has a verified end-to-end path.

**Independent Test**: Spec US4 — 3 distinct commands (move, build, patrol) across 3 units land on one frame; 10 ticks later, a follow-up state snapshot confirms units have non-empty current-command lists or have begun moving.

### Tests for US4

- [X] T041 [P] [US4] `tests/FSBar.Hub.LiveTests/CommandBatchLiveTests.fs`: US4 AS1 — one `BuildCommand` triggers a `UnitCreated` event within 30 ticks.
- [ ] T042 [P] [US4] Same file: US4 AS2 — 50 move commands in one batch share a single `forwarded_at_frame`.
- [ ] T043 [P] [US4] Same file: US4 AS3 — invalid command (bogus target id) yields per-command diagnostic; accepted peers in same batch still forwarded.
- [X] T044 [P] [US4] Same file: oversize batch (1025 entries) → whole-batch rejection, zero forwards, diagnostic names cap + received size (FR-008, Clarification Q5).
- [ ] T045 [P] [US4] `tests/FSBar.Hub.LiveTests/CommandSurfaceLiveTests.fs`: one end-to-end assertion per `highbar.AICommand` variant (Move, Stop, Patrol, Guard, Attack, Fight, Build, Repair, Reclaim, SelfDestruct, SetState, plus any other present in `proto/highbar/commands.proto`) — FR-009, SC-004.

### Implementation for US4

- [X] T046 [US4] In `src/FSBar.Hub/ScriptingHub.fs` implement `SendCommandBatch`: validate size cap FIRST (reject whole above 1024), then forward the batch in one engine dispatch, capture the frame number, and build per-entry `CommandOutcome`s.
- [X] T047 [US4] Per-entry validation path (bad target id, unknown unit id, etc.) surfaces a non-empty diagnostic while still forwarding accepted peers (US4 AS3).
- [X] T048 [US4] Correlation-id flow-through via the existing `CorrelationId.ServerInterceptor` — no per-RPC code needed, but add a test asserting correlation id reaches HubLog for a batch call (FR-014).
- [ ] T049 [US4] Update `.fsi` + surface-area baseline for US4 public helpers.

**Checkpoint**: US4 complete — bots can issue any valid command and batch them efficiently.

---

## Phase 7: Polish & Cross-Cutting

- [X] T050 [P] Author `scripts/examples/24-hub-full-client.fsx` — under 50 lines: launch session, stream state+events for N ticks, send a batched command, print summary (SC-006, FR-013).
- [X] T051 [P] Update Hub scripting README (root README or `src/FSBar.Hub/scripts/README.md`) and the scripting section of `CLAUDE.md` to list the new RPCs + new example (FR-013).
- [ ] T052 Run `/skill fsdoc-agent` (per Constitution §Workflow step 7) for public-API documentation refresh.
- [ ] T053 Regenerate all surface-area baselines with `SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx --filter FullyQualifiedName~SurfaceArea`; commit the diff.
- [ ] T054 Full build + test gate: `dotnet build FSBarV1.slnx && dotnet test FSBarV1.slnx` — green.
- [ ] T055 Live-test gate: `tests/run-all.sh` (engine-aware wrapper) — green on the reference machine.
- [ ] T056 SC-003 latency measurement: added latency (FSBar.Client observing a proxy frame → scripting client decoded message) < 30 ms on the reference machine; record result in `research.md` appendix.
- [ ] T057 Walk the quickstart (`specs/046-scripting-full-client/quickstart.md`) end-to-end as a final validation, with the published FSI walkthrough executed from a clean shell.

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup) → Phase 2 (Foundational) → Phases 3..6 (user stories, any order after Phase 2) → Phase 7 (Polish).
- Phase 2 BLOCKS all user stories (proto + generated types + channel limits must exist first).

### User Story Dependencies

- **US1 (P1)**: depends only on Phase 2. No cross-story dep.
- **US2 (P2)**: depends only on Phase 2. Uses the same `GameFrameMessage` shape introduced in T005; no runtime dep on US1 code.
- **US3 (P2)**: depends only on Phase 2. Entirely separate RPCs.
- **US4 (P3)**: depends only on Phase 2. The `CommandSurfaceLiveTests` in T045 benefits from state readout (US1) as an observation channel but is independently testable via engine-side assertions.

### Within Each User Story

- Tests first (constitution §III: tests must fail before impl).
- Projection helpers (`projectGameState`, `projectGameEvent`, grid projectors) before handler wiring.
- `.fsi` + surface-area baseline update last, after the implementation is frozen.

### Parallel Opportunities

- T004–T008 touch the same proto file; do them sequentially. T011/T012 are `[P]` (different files).
- Inside every user story, all test tasks are `[P]` (same test file, distinct `[Fact]`s — either split by file per test or run-in-parallel via xUnit test collection, your call).
- After Phase 2, all four user-story phases (US1–US4) can be staffed in parallel.
- Polish tasks T050, T051 are `[P]`.

---

## Parallel Example: User Story 1

```text
# Write the failing US1 live tests together:
T014 StateStreamLiveTests: AS1 state-matches-Hub
T015 StateStreamLiveTests: AS2 radar-only unknown health
T016 StateStreamLiveTests: AS3 frozen last-known
T017 StateStreamLiveTests: AS4/AS5 no-session + close_on_session_end
T018 StateStreamLiveTests: SC-002 size ceiling
```

---

## Implementation Strategy

### MVP (US1 only)

1. Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1).
2. Stop and validate: a Python gRPC client subscribes to `StreamGameFrames` and reads live unit positions + economy.
3. Ship as the MVP increment.

### Incremental delivery

1. MVP as above.
2. Add US2 (typed events) — bots no longer need to diff state.
3. Add US3 (map + extended unit-def) — deliberate play unlocked.
4. Add US4 (command batch + full command surface) — efficient tick loops.
5. Polish phase (docs, FSI walkthrough, latency measurement, fsdoc).

### Parallel team strategy

After Phase 2:
- Dev A → US1
- Dev B → US2 (joins on `GameFrameMessage` shape from T005)
- Dev C → US3 (orthogonal RPCs)
- Dev D → US4 (orthogonal RPCs)

---

## Notes

- Tests are live-engine only — no mocks, no in-memory substitutes (CLAUDE.md testing policy).
- Tests blocked by out-of-scope issues must be skipped with a reason, never falsely passed.
- Every public API addition needs an `.fsi` update + regenerated surface-area baseline (Constitution §II).
- `buf breaking` on `proto/hub/scripting.proto` MUST remain zero at every commit boundary.
- Commit after each task or logical group; prefer small commits that keep `dotnet build FSBarV1.slnx` green.
