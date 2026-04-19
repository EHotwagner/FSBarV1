# Phase 0 Research: Batched GameState snapshot

## R1. Wire contract shape

**Decision**: Mirror the HighBarV2 032 proto contract byte-for-byte in
`proto/highbar/callbacks.proto`: add `CALLBACK_GAME_GET_STATE = 15` to
`CallbackId`; add messages `FriendlyUnit`, `LosEnemyUnit`,
`RadarOnlyEnemyUnit`, `EconomyRecord`, `GameStateSnapshot`; add
`GameStateSnapshot snapshot_value = 8` to the `CallbackResult.value`
oneof.

**Rationale**: The proxy is shared with HighBarV2; any divergence from the
upstream wire contract would break the proxy binary that FSBarV1 ships
with. The 2026-04-19 memo pins field numbers and tag shapes. FSBarV1's
current callbacks.proto has unused id 15, so no renumbering is needed.

**Alternatives considered**:
- *A unified `UnitObservation` with optional health*: explicitly rejected
  upstream (Decision 4 in HighBarV2 spec 032) to prevent callers from
  averaging nonexistent health across radar-only contacts. Rejecting here
  keeps the type-level safety.
- *Separate RPC per group (friendlies / los / radar / economy)*: loses
  atomicity and reintroduces interleaving risk.

## R2. Client-side record shape

**Decision**: Introduce three distinct F# record types in
`FSBar.Client.Callbacks`: `FriendlyUnitSnapshot`, `LosEnemySnapshot`,
`RadarOnlyEnemySnapshot` (no `Health` field), plus `EconomyRecordSnapshot`
and `GameStateSnapshotResult`. Map them into the existing `TrackedUnit` /
`TrackedEnemy` shapes inside `GameState.processEvent`.

**Rationale**: Preserves the upstream no-sentinel invariant at the F#
type system layer. `TrackedEnemy.Health` is already
`float32 option`, so mapping radar-only → `None` is a direct fit.

**Alternatives considered**:
- *Expose snapshot records as the canonical client type and delete
  `TrackedEnemy`/`TrackedUnit`*: larger blast radius (Viz, Hub, and
  trainer consumers depend on the tracked types). Out of scope for 045.

## R3. Update-path rewrite strategy

**Decision**: Rewrite only the `GameEvent.Update` branch in
`GameState.processEvent` to:
1. Call `Callbacks.getGameStateSnapshot stream`.
2. Rebuild `state.Units` from `snapshot.Friendlies` keyed by `UnitId`,
   preserving `MaxHealth`, `IsFinished`, `IsIdle` from prior state when
   the unit was already tracked (snapshot does not carry those fields);
   drop any unit absent from the snapshot (it has died or been
   transferred — `UnitDestroyed`/`UnitGiven` events already handle that,
   but the snapshot is authoritative for membership).
3. Rebuild `state.Enemies` by: (a) marking each prior entry `InLOS=false`
   `InRadar=false` as a baseline; (b) folding `snapshot.LosEnemies`
   setting `InLOS=true`, concrete `Health`; (c) folding
   `snapshot.RadarOnlyEnemies` setting `InRadar=true`, `Health=None`;
   (d) entries not mentioned in either list keep their prior position
   frozen (per FR-007) but `InLOS`/`InRadar` = false and `Health = None`.
4. Replace `state.Metal` / `state.Energy` from `snapshot.Economy`.
5. Remove functions `refreshUnit` and `refreshEconomy` entirely.

**Rationale**: Matches FR-002 (sole path) and FR-007 (frozen position).
The snapshot is the authoritative per-tick state for LOS and radar; the
client remains responsible for retaining lost contacts at their
last-known position because the proxy does not re-emit them.

**Alternatives considered**:
- *Keep `refreshUnit` as a private helper for event handlers that already
  call it (e.g., `UnitCreated`, `EnemyEnterLOS`)*: those call sites use
  individual `Callbacks.get*` functions directly, not `refreshUnit`, so
  removing `refreshUnit` is safe.

## R4. Hard-error on proxy version shortfall

**Decision**: If the first `getGameStateSnapshot` call returns
`success=false` with an error_message indicating unknown callback id,
raise a new exception `ProxyVersionMismatchException` (message: "HighBar
proxy does not advertise CALLBACK_GAME_GET_STATE (id=15). Required:
HighBarV2 >= 0.1.5. Upgrade the proxy binary."). Bubble up through
`BarClient.connect` to terminate the session immediately.

**Rationale**: Clarification from `/speckit.clarify` session 2026-04-19
— no legacy fallback. Fail fast at connection warmup rather than on the
first post-warmup `GameEvent.Update` so operators see the problem before
taking the field.

**Alternatives considered**:
- *Probe for callback 15 support at `BarClient.connect` time by issuing
  a pre-flight empty snapshot request*: cleaner operator experience
  (fail before `Init` is processed). Adopted; a pre-flight snapshot is
  issued right after the `Init` event so failure is immediate.

## R5. `FSBAR_TEST_ENGINE` alias precedence

**Decision**: Add a shared helper
`EngineDiscovery.resolveOverrideEnvVar ()` returning
`Result<string option * WarningOpt, string>`. Logic:
1. Read both `FSBAR_TEST_ENGINE` and `HIGHBAR_TEST_ENGINE`.
2. If both set and non-equal → return `FSBAR_TEST_ENGINE` value with a
   warning string identifying both.
3. If only one set → return that one.
4. If neither set → return `None`.

Route `resolveFromEnvVar` and the diagnostic strings in
`validateEngine`, `resolveEngine`, and the `ResolutionSource` labels
through this helper. Update `ENGINE-VERSION.md`, `check-prerequisites.sh`
priority-1 block, and `CLAUDE.md`'s Engine paths section.

**Rationale**: Satisfies FR-009 / FR-010 / SC-006 with a single code
path. Emits an observable warning (constitution §IV) when operators have
conflicting values.

**Alternatives considered**:
- *Rename and remove legacy var*: breaks CI / existing shell profiles
  that export `HIGHBAR_TEST_ENGINE`; memo explicitly suggests dual-accept.
- *Silent precedence (no warning)*: violates observability principle when
  the two values disagree.

## R6. Live test harness for correctness

**Decision**: In `FSBar.LiveTests`, add a test that after `cheat` +
`cheat-spawn` of a small mixed army, compares for ≥60 consecutive ticks:
snapshot-derived `GameState` against reference values obtained by
calling the individual `Callbacks.getUnitPos` / `getUnitHealth` /
`getEconomy*` helpers inside the *test harness only* (not the client
code). Tolerance: exact equality for integer ids and team; `float32`
epsilon `1e-3` for position and health.

**Rationale**: Per-unit callbacks are not on the production refresh path
any more, but they remain on `Callbacks`' public surface and are the
engine's ground truth for verification. Running them outside the client
keeps SC-002 (zero per-unit RPCs on the client's update path)
verifiable.

**Alternatives considered**:
- *Unit-test mapper only*: insufficient — FR-003 is observational
  equivalence against the engine, not against a fixture.

## R7. Baseline + surface area maintenance

**Decision**: Regenerate `tests/FSBar.Client.Tests/Baselines/*.baseline`
with `SURFACE_AREA_UPDATE=1 dotnet test` after implementation.

**Rationale**: Constitution §II mandates surface-area baselines; the new
`getGameStateSnapshot` + record types add publicly visible surface.

## R8. No new NuGet dependencies

Confirmed: all new work uses existing FsGrpc 1.0.6, SkiaSharp, etc.
Satisfies constitution §Engineering Constraints and FR assumptions.
