# Feature Specification: Adopt HighBarV2 0.1.5 batched GameState snapshot + test-engine env-var alias

**Feature Branch**: `045-batch-gamestate-snapshot`
**Created**: 2026-04-19
**Status**: Draft
**Input**: User description: "integrate @Mailbox/2026-04-19_from_HighBarV2_032-batch-callback-and-engine-discovery-port.md and do your own research on the upgrade in HighBar"

## Clarifications

### Session 2026-04-19

- Q: Should the client retain a legacy per-unit refresh path as a fallback when the proxy does not advertise `CALLBACK_GAME_GET_STATE`? → A: No — the batched snapshot is the only supported path; an unsupported proxy is a hard error and the legacy refresh code is removed.

## Context

HighBarV2 `0.1.5` (branch `032-batch-callback-rpcs`) adds a new proxy callback
`CALLBACK_GAME_GET_STATE = 15` that returns a full per-tick snapshot
(friendlies, LOS enemies, radar-only enemies, 8-field economy record) in a
single RPC round-trip. The proxy enumerates units and fetches per-unit fields
server-side on the per-frame arena, collapsing ~750 per-tick RPCs (at 200
friendlies + 50 enemies) to one.

FSBarV1's `FSBar.Client.GameState.processEvent` currently handles
`GameEvent.Update` by walking every tracked friendly (`refreshUnit` →
`Unit_getPos` + `Unit_getHealth` per unit) and every tracked enemy that is in
LOS or radar (two more RPCs each), followed by 8 `Economy_*` calls. This is
the exact pattern HighBarV2 flagged, and FSBarV1 consumes the same
`HighBar.Client` proxy protocol (via `FSBar.Client.Protocol` +
`FSBar.Client.Callbacks`), so the new callback is directly available once the
proxy is upgraded.

A second, lower-severity item from the memo: HighBarV2 observed that
FSBarV1's `EngineDiscovery.resolveFromEnvVar` reads `HIGHBAR_TEST_ENGINE` —
a HighBar-shaped variable name inside an FSBar module — and suggested
renaming or dual-accepting.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Single-RPC per-tick GameState refresh (Priority: P1)

As a trainer / viewer developer relying on `FSBar.Client.GameState`, I want
each `GameEvent.Update` tick to be serviced by one batched snapshot RPC so
that large armies do not stall the per-tick refresh loop and do not risk
interleaving hundreds of request/response round-trips with engine `Frame`
messages.

**Why this priority**: This is the substantive upgrade from the HighBarV2
memo and the only change that alters observable runtime behavior of the
trainer, viewer, and Hub live sessions. At 200 friendlies + 50 enemies
FSBarV1 currently issues ~700+ RPCs per tick; the batched path collapses
this to one.

**Independent Test**: Run the live trainer against a headless engine with
`cheat-spawn` of 200 friendly bots and 50 visible enemies. Observe that
each `GameEvent.Update` issues exactly one `CallbackRequest` of id
`CALLBACK_GAME_GET_STATE` (verified via a `Protocol` trace or wire-level
counter) and that the resulting `GameState` matches the baseline produced
by the legacy per-unit refresh path within float tolerance.

**Acceptance Scenarios**:

1. **Given** an active engine session with N friendly units and M visible
   enemies, **When** a `GameEvent.Update` is processed, **Then** the
   `GameState.Units`, `GameState.Enemies`, `GameState.Metal`, and
   `GameState.Energy` values match the legacy per-unit refresh output for
   the same tick within floating-point tolerance.
2. **Given** an enemy that is in radar only (not in LOS), **When** it
   appears in the batched snapshot, **Then** it is recorded with
   `Health = None` — never a zero or negative sentinel — matching the
   wire contract decision in HighBarV2 spec 032.
3. **Given** an engine session where the tracked friendly + enemy count
   exceeds the proxy's `HIGHBAR_SNAPSHOT_MAX_UNITS` cap, **When** the
   client issues a snapshot request, **Then** the failure is surfaced to
   the caller with a descriptive error and the prior `GameState` is left
   untouched (no partial application).
4. **Given** a session where the proxy does not support callback id 15
   (older HighBar binary), **When** the client performs its first update,
   **Then** the connection fails fast with a descriptive error naming
   the required proxy version; no legacy fallback is attempted.

---

### User Story 2 — Environment variable alignment for engine override (Priority: P3)

As a developer running FSBarV1 tests or launching the trainer, I want to
override the engine path via `FSBAR_TEST_ENGINE` without also having to
know that an upstream-named `HIGHBAR_TEST_ENGINE` variable is the one the
code actually reads, while existing CI and scripts using
`HIGHBAR_TEST_ENGINE` continue to work unchanged.

**Why this priority**: Cosmetic and ergonomic. No functional change in
default environments; only matters when the two variables are set to
different values or when a contributor is debugging which variable takes
effect.

**Independent Test**: Unset both variables, set only `FSBAR_TEST_ENGINE`,
run `tests/check-prerequisites.sh` and the live test suite — prerequisite
resolution succeeds using the new variable. Repeat with only
`HIGHBAR_TEST_ENGINE` set — still succeeds. Set both to different values —
`FSBAR_TEST_ENGINE` wins and a warning is emitted.

**Acceptance Scenarios**:

1. **Given** only `FSBAR_TEST_ENGINE` is set, **When** `EngineDiscovery`
   resolves the engine binary, **Then** the value of `FSBAR_TEST_ENGINE`
   is used.
2. **Given** only `HIGHBAR_TEST_ENGINE` is set, **When**
   `EngineDiscovery` resolves the engine binary, **Then** the value of
   `HIGHBAR_TEST_ENGINE` is used unchanged (legacy compatibility).
3. **Given** both variables are set to different values, **When**
   resolution occurs, **Then** `FSBAR_TEST_ENGINE` takes precedence and
   a single diagnostic warning is emitted identifying both values.

---

### Edge Cases

- **Proxy disconnect mid-snapshot wait**: the existing
  `EngineDisconnectedException` surface from FSBar feature 023 must remain
  unchanged.
- **Engine frame interleaving during the snapshot wait**: handled by the
  existing replay buffer (HighBar feature 031 / FSBar incorporation
  feature 022) — no new drop surface is introduced.
- **Zero units tracked** (pre-game warmup): batched snapshot must still
  return a well-formed result with empty unit lists and a valid economy
  record.
- **Enemy that transitions from radar-only to LOS between snapshots**:
  `Enemies` map must update `InLOS = true` and receive a concrete `Health`
  value without retaining the previous `None`.
- **Enemy currently in neither LOS nor radar** (last-known frozen
  position): must remain in `Enemies` with its stored position unchanged —
  the batched snapshot lists only LOS + radar contacts, so the client's
  existing frozen-position logic must continue to apply for contacts not
  mentioned in the snapshot.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `FSBar.Client` MUST expose a single-call per-tick snapshot
  API that returns friendly units, LOS enemies, radar-only enemies, and
  both resource records in one client-visible operation.
- **FR-002**: On every `GameEvent.Update`, `FSBar.Client.GameState` MUST
  use the batched snapshot. The prior per-unit position / health /
  economy refresh code path MUST be removed from the client — batched
  snapshot is the sole supported refresh mechanism.
- **FR-003**: The post-update `GameState.Units` / `Enemies` / `Metal` /
  `Energy` values MUST be observationally equivalent to the legacy path
  for the same engine state, modulo the frozen-position behavior for
  enemies not present in the snapshot.
- **FR-004**: Radar-only enemies in the resulting `GameState` MUST carry
  `Health = None`. The client MUST NOT invent a zero or sentinel health
  for radar-only contacts.
- **FR-005**: If the snapshot request fails (cap exceeded, proxy error,
  missing sub-callback), the client MUST raise a descriptive error and
  MUST leave the existing `GameState` unchanged. No partial application.
- **FR-006**: When the proxy returns an "unknown callback" error for id
  15 on the first snapshot attempt, the client MUST raise a descriptive
  connection error naming the minimum required proxy version and MUST
  terminate the session. No fallback, no degraded mode.
- **FR-007**: Enemies present in the prior `GameState` but absent from a
  successful snapshot (neither LOS nor radar) MUST remain in the map
  with their last-known position and `Health = None`, preserving the
  current frozen-last-known-position behavior.
- **FR-008**: `FSBar.Proto` MUST carry the new proto surface
  (`CALLBACK_GAME_GET_STATE = 15`; `FriendlyUnit`, `LosEnemyUnit`,
  `RadarOnlyEnemyUnit`, `EconomyRecord`, `GameStateSnapshot` messages;
  `snapshot_value = 8` oneof variant of `CallbackResult`) mirroring the
  HighBarV2 032 contract byte-for-byte.
- **FR-009**: `EngineDiscovery.resolveFromEnvVar` MUST accept both
  `FSBAR_TEST_ENGINE` (preferred) and `HIGHBAR_TEST_ENGINE` (legacy).
  When both are set, `FSBAR_TEST_ENGINE` wins and a single warning is
  emitted.
- **FR-010**: All existing call sites currently reading
  `HIGHBAR_TEST_ENGINE` (client, launcher, `tests/check-prerequisites.sh`,
  documentation) MUST route through the shared resolution helper so the
  alias rule applies uniformly.
- **FR-011**: The surface-area baseline for `FSBar.Client` MUST be
  updated to include the new snapshot API and any new public types.
- **FR-012**: Live integration tests (under `FSBar.LiveTests` /
  `FSBar.Hub.LiveTests`) MUST cover at minimum: (a) snapshot correctness
  (positions, health, economy) against a spawned army, (b) radar-only
  `Health = None`, (c) hard-error behavior when the proxy does not
  advertise callback id 15.

### Key Entities

- **GameStateSnapshot**: Frame number + list of friendlies + list of
  LOS enemies + list of radar-only enemies + one economy record. The
  atomic unit returned by the batched callback.
- **FriendlyUnit / LosEnemyUnit**: Carry unit id, position, health, unit
  def id, team.
- **RadarOnlyEnemyUnit**: Carries unit id, position, unit def id, team.
  Structurally lacks a health field.
- **EconomyRecord**: Eight-field resource snapshot
  (metal/energy × current/income/usage/storage).
- **TrackedUnit / TrackedEnemy**: Existing FSBar client-side records
  updated from the snapshot; public shape unchanged except as required
  to persist radar-only `Health = None`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A live session with 200 friendlies + 50 visible enemies
  completes each `GameEvent.Update` in under 10 ms wall-clock from the
  time the client consumes the event to the time `GameState` is ready
  for the next event, under the live-test harness on the reference
  machine.
- **SC-002**: Each processed `GameEvent.Update` results in exactly one
  `CALLBACK_GAME_GET_STATE` request/response pair (verifiable via a
  wire-level counter in the live test), with zero per-unit
  `Unit_getPos` / `Unit_getHealth` / `Economy_*` calls on the batched
  path.
- **SC-003**: Over ≥300 consecutive ticks in a live scenario, the
  refreshed `GameState` reports positions, health, and economy values
  consistent with engine ground truth (sampled via independent
  per-unit callbacks in the test harness only) within float tolerance,
  while frozen-position enemies remain unchanged.
- **SC-004**: Zero `GameEvent.Update` drops attributable to RPC
  interleaving over the same ≥300-tick window (reusing the feature
  022 / HighBar 031 replay-buffer path).
- **SC-005**: All existing FSBarV1 unit + live tests continue to pass
  on the reference headless engine against a HighBarV2 0.1.5+ proxy.
  Running against a pre-0.1.5 proxy produces an immediate, descriptive
  connection error (no silent degradation).
- **SC-006**: An operator setting `FSBAR_TEST_ENGINE` alone can run the
  full prerequisite check and live test suite successfully without
  referencing `HIGHBAR_TEST_ENGINE` anywhere, and the documentation in
  `CLAUDE.md` and `tests/ENGINE-VERSION.md` lists `FSBAR_TEST_ENGINE`
  as the primary variable.

## Assumptions

- The FSBarV1 proxy binary in use will be upgraded to HighBarV2
  `0.1.5`+ before or alongside this feature landing. Pre-0.1.5 proxies
  are unsupported; no legacy fallback is retained.
- The HighBarV2 032 wire contract as documented in the 2026-04-19
  mailbox memo (and `specs/032-batch-callback-rpcs/contracts/` in the
  HighBarV2 repo) is stable and will not change before FSBarV1 adopts
  it.
- The existing `FSBar.Client.Protocol` replay buffer (feature 022
  incorporation of HighBar 031) remains the mechanism that keeps
  engine `Frame` messages from being lost while a callback response
  is awaited — no new drop surface is introduced by batching.
- The current frozen-last-known-position behavior for enemies that
  have left both LOS and radar is preserved by design; the snapshot
  callback is not expected to carry those contacts and the client
  remains responsible for retaining them.
- Performance target in SC-001 assumes the current reference headless
  engine (`recoil_2025.06.19`) on the developer's reference machine;
  the target is a sanity ceiling, not a regression gate.
- Renaming `HIGHBAR_TEST_ENGINE` to `FSBAR_TEST_ENGINE` is additive
  (dual-accept); no existing CI, developer shell profile, or script
  is broken by this feature.
- No new NuGet dependencies are required. Proto regeneration uses the
  existing `proto-regen` skill workflow.
