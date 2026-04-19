# HighBarV2 feature 032 shipped + FSBarV1 `EngineDiscovery` ported upstream

**Date**: 2026-04-19
**From**: HighBarV2 maintainer (feature 032 batch-callback-rpcs)
**To**: FSBarV1 trainer maintainer
**Severity**: INFORMATIONAL — new client API available; no FSBarV1 action required
**Related**:
- `2026-04-14_to_HighBarV2_mid-game-callback-event-drop.md` (031 precursor — now shipped)
- `2026-04-12_to_HighBarV2_proxy-contract-refinements.md` (earlier batching ask)
- FSBarV1 `src/FSBar.Client/EngineDiscovery.fs` (the module we just mirrored)

---

## TL;DR

HighBarV2 `0.1.5` (branch `032-batch-callback-rpcs`) ships two changes you
will likely care about:

1. **New `client.GetGameState()` — one callback round-trip returns the full
   per-tick snapshot** (every friendly, every LOS enemy, every radar-only
   enemy, and the full 8-field economy record). This is the batching API the
   2026-04-12 contract-refinements memo asked for. See **Wire contract**
   below.

2. **Dynamic engine + game discovery ported from FSBarV1** into
   `HighBar.Client.EngineDiscovery` + `tests/check-prerequisites.sh`. We no
   longer care what name is baked into `engine-version.json` — the installed
   rapid `byar:test` entry wins. Credit: your
   `src/FSBar.Client/EngineDiscovery.fs` is effectively the upstream spec;
   we copied it verbatim (attribution comment in our file).

All 3 C tests + 3 F# unit tests + 56 F# live integration tests pass against
headless `recoil_2025.06.19` with the current rapid pool
(`Beyond All Reason test-29926-0571aa8`).

---

## 1 — `GetGameState()` one-call per-tick snapshot

### Why you might care

Your 023-trainer-builder-economy macro bot currently walks the unit
registry with one `Unit_getPos` + `Unit_getHealth` + `Unit_getDef` RPC per
unit per tick. At ~200 friendlies + ~50 visible enemies that is **~750+
round-trips per tick** over the Unix socket, every one of which risks
interleaving with an engine `Frame` (i.e. the exact scenario that motivated
feature 031's replay buffer). `GetGameState()` collapses that to **one**
RPC — all enumeration + field population happens proxy-side on the
per-frame arena, enumerated via `getTeamUnits`, `getEnemyUnits`,
`getEnemyUnitsInRadarAndLos`.

### Wire contract

New enum value: `CallbackId.CALLBACK_GAME_GET_STATE = 15`.

New `CallbackResult.oneof value` variant: `GameStateSnapshot snapshot_value = 8;`.

New messages (in `proto/highbar/callbacks.proto`):

```protobuf
message FriendlyUnit {
  int32 unit_id = 1;
  Vector3 position = 2;
  float health = 3;
  int32 unit_def_id = 4;
  int32 team = 5;
}

message LosEnemyUnit {
  int32 unit_id = 1;
  Vector3 position = 2;
  float health = 3;
  int32 unit_def_id = 4;
  int32 team = 5;
}

message RadarOnlyEnemyUnit {
  int32 unit_id = 1;
  Vector3 position = 2;
  int32 unit_def_id = 3;
  int32 team = 4;
  // NOTE: structurally no `health` field — radar-only contacts have no
  // LOS-quality health readings, and we refuse to invent a zero or -1
  // sentinel (Decision 4 in the 032 spec).
}

message EconomyRecord {
  float metal_current = 1;  float metal_income = 2;
  float metal_usage = 3;    float metal_storage = 4;
  float energy_current = 5; float energy_income = 6;
  float energy_usage = 7;   float energy_storage = 8;
}

message GameStateSnapshot {
  int32 frame = 1;
  repeated FriendlyUnit friendlies = 2;
  repeated LosEnemyUnit los_enemies = 3;
  repeated RadarOnlyEnemyUnit radar_only_enemies = 4;
  EconomyRecord economy = 5;
}
```

Request: `CallbackRequest { callback_id = 15; params = [] }`.

Response — success:
`CallbackResponse { success = true; result = { snapshot_value = <...> } }`.

Response — failure: `success = false; error_message = "<descriptive>"`; no
partial snapshot. The existing disconnected-engine path (`EngineDisconnectedException`
from feature 023) surfaces unchanged.

### Proxy enumeration strategy

- Friendlies: `getTeamUnits(myTeam)`.
- Enemy classification: `getEnemyUnits` → LOS set. `getEnemyUnitsInRadarAndLos`
  → LOS ∪ Radar. `RadarOnly = (LOS ∪ Radar) \ LOS`. We don't rely on a
  `Unit_isInLos` callback because the Recoil `SSkirmishAICallback` vendored
  in `proxy/include/AI/` doesn't expose one.
- Economy: 8 `Economy_*` calls (metal=0, energy=1, × current/income/usage/storage).
- All allocation from the per-frame arena (Constitution V / Decision 6).

### Cap / failure modes

- Env var `HIGHBAR_SNAPSHOT_MAX_UNITS` (default **4096**) caps total
  `friendly + enemy` count. Over the cap → `success=false`,
  `error_message="Snapshot unit count exceeds HIGHBAR_SNAPSHOT_MAX_UNITS"`,
  **no** `snapshot_value` emitted.
- Any sub-callback missing from the Recoil callback table → `success=false`,
  descriptive `error_message`, no partial snapshot.

### F# client surface

New module `HighBar.Client.GameStateSnapshot` (file
`clients/fsharp/src/GameStateSnapshot.fs`) with records `FriendlyUnit`,
`LosEnemyUnit`, `RadarOnlyEnemyUnit` (no `Health`), `EconomyRecord`,
`GameStateSnapshot`, and `GameStateSnapshot.fromProto`.

New method on `HighBarClient`:

```fsharp
member this.GetGameState() : GameStateSnapshot
```

It issues one `CallbackRequest` with `CALLBACK_GAME_GET_STATE` and no
params, awaits the matching `CallbackResponse`, and returns the converted
record. Frame interleaving on the wait path is handled by the existing
031 replay buffer — no new drop surface.

### Performance target (spec, not measured yet at reference load)

- SC-001: `<10 ms` wall-clock at 200 friendlies + 50 enemies (one RPC, no
  per-unit marshalling overhead).
- SC-003: zero upstream `GameEvent.Update` throttling or drops over ≥300
  consecutive frames (the 031 interleaving path is exercised every tick).
- SC-002: **exactly one** `CallbackRequest`/`CallbackResponse` pair per
  `GetGameState()` call.

The perf test (`T010` in `specs/032-batch-callback-rpcs/tasks.md`) is not
yet authored — it belongs to the persistent-engine suite and wants
`cheat-spawn` of 200+50 before it measures. Expect it in a follow-up.

### Caveat: radar-only is structurally distinct, by design

Please do not collapse the three F# records into a shared
`UnitObservation` with an `Option<float32>` health or a `0.0f` sentinel.
We deliberately picked three nominal types so the compiler refuses
`snapshot.RadarOnlyEnemies |> List.averageBy (fun e -> e.Health)`. If
your bot wants a unified stream, write the downcast once in bot-side
code, not in the wire contract.

---

## 2 — `EngineDiscovery` ported from FSBarV1

### Problem

HighBarV2's integration tests were hardcoding a stale rapid game name
(`engine-version.json` → `"Beyond All Reason test-29907-d3b337a"`),
whereas the installed rapid pool had already advanced to
`test-29926-0571aa8`. Every engine-requiring test was failing at
`SpringApp::Kill` with:

```
Fatal: [ExitSpringProcess] errorMsg="Dependent archive "beyond all reason
test-29907-d3b337a" (resolved to ...) not found"
```

All 36 live tests failed; all 20 non-engine tests passed. Classic stale-
pin blast radius.

### Fix (this is your code, we just moved it next door)

1. **New `clients/fsharp/src/EngineDiscovery.fs`** — a direct port of
   your `src/FSBar.Client/EngineDiscovery.fs`. We only kept the two
   functions we needed: `defaultDataDir ()` and
   `discoverGameName (dataDir: string) (tag: string) : string option`.
   The gzipped `versions.gz` reader is structurally identical; we
   shortened the fallback list to two paths (the two you check). The
   file header credits FSBarV1 explicitly:

   ```fsharp
   /// Dynamic engine + game resolution.
   ///
   /// Ported from FSBarV1 (src/FSBar.Client/EngineDiscovery.fs). Rationale:
   /// the rapid-pool game name changes every few days ... Hardcoding the
   /// name in engine-version.json gets stale and breaks live integration
   /// tests. This module resolves the current name from
   /// `rapid/repos-cdn.beyondallreason.dev/byar/versions.gz` at runtime.
   ```

2. **`clients/fsharp/src/EngineConfig.fs` → `fromVersionFile`** — when
   `engine-version.json` has a `game.rapidTag`, we call
   `EngineDiscovery.discoverGameName` and, if it resolves, **override**
   the JSON `game.name`. The JSON value is still the fallback for
   environments without rapid data.

3. **`tests/check-prerequisites.sh`** — two changes, both mirroring
   FSBarV1's `tests/check-prerequisites.sh`:
   - **Engine auto-detection** when `HIGHBAR_TEST_ENGINE` is unset and
     the binary isn't on `PATH`: walk
     `~/.local/state/Beyond All Reason/engine/recoil_*/` newest-first
     and use the first executable `spring-headless` found. This is
     exactly your priority-3 branch.
   - **Dynamic `GAME_NAME` resolution** from
     `rapid/.../byar/versions.gz` when `game.rapidTag` is set. Mirrors
     your `_VERSIONS_GZ` block.

### Result

```
━━━ Engine Prerequisites ━━━
  ✓ Engine available: /home/developer/.local/state/Beyond All Reason/engine/recoil_2025.06.19/spring-headless

━━━ F# Integration Tests ━━━
  ✓ PASSED (56 passed)
```

All 56 integration tests green on the first run after the port, including
the two new `GetGameStateTests` for the 032 API.

### Observation for the FSBarV1 side

Your `EngineDiscovery.resolveFromEnvVar` reads the
`HIGHBAR_TEST_ENGINE` environment variable despite the file being in
`FSBar.Client`. That's a HighBar-shaped variable name in an FSBar
module. You may want to rename to `FSBAR_TEST_ENGINE` or accept both —
not a blocker, just the kind of small divergence that bites in shared
test environments.

---

## 3 — What ships in HighBar.Client 0.1.5 (recap)

| Area | File | Change |
|------|------|--------|
| Proto | `proto/highbar/callbacks.proto` | +`CALLBACK_GAME_GET_STATE=15`, +5 messages, +`snapshot_value=8` oneof |
| Proxy | `proxy/src/callbacks.c` | +`CALLBACK_GAME_GET_STATE` handler with LOS/radar classification + economy |
| Client types | `clients/fsharp/src/GameStateSnapshot.fs` (new) | F# records + `fromProto` |
| Client API | `clients/fsharp/src/Client.fs` | +`member this.GetGameState()` |
| Engine resolve | `clients/fsharp/src/EngineDiscovery.fs` (new, from FSBarV1) | rapid-tag → game name |
| Engine resolve | `clients/fsharp/src/EngineConfig.fs` | consumes `EngineDiscovery` |
| Test infra | `tests/check-prerequisites.sh` | auto-detect engine + dynamic game name |
| Tests | `proxy/tests/test_gamestate_snapshot.c` (new) | wire round-trip + cap-exceeded |
| Tests | `tests/integration/fsharp/GameStateSnapshotUnitTests.fs` (new) | unit + reflection (no-Health) |
| Tests | `tests/integration/fsharp/GetGameStateTests.fs` (new) | live-engine round-trip + disconnect |

Version: `HighBar.Client` `0.1.4 → 0.1.5`. `BarData` unchanged.

Branch: `032-batch-callback-rpcs` (pre-merge at the time of this memo).

---

## 4 — Outstanding (non-blockers, flagged for transparency)

- **T010 perf test** (`200 friendlies + 50 enemies cheat-spawn, <10 ms`)
  not yet authored. It belongs in `tests/persistent/fsharp/` and requires
  a fresh harness for cheat-spawn loops. Follow-up commit.
- **T016 UnitRegistry refactor** intentionally **skipped**. The spec asked
  to "replace the per-unit `refreshUnit` loop" but there is no such loop
  in `UnitRegistry.fs` — it's purely event-driven today. `GetGameState()`
  is additive; bots can adopt it at leisure.
- **T018 docs prose update** (`docs/protocol.md` or `docs/proxy-reference.md`).
  The canonical contract is already in
  `specs/032-batch-callback-rpcs/contracts/gamestate-snapshot.md`, but a
  user-facing entry in `docs/` is still pending.
- **T020 full `./tests/run-all.sh`** across all four categories not yet
  executed — only the `integration` category has been run end-to-end.
- **T021 quickstart manual walkthrough** requires a human.

None of these gate the API. `GetGameState()` is callable against a live
headless engine today.

---

## 5 — If you want to try it from FSBarV1

Once the 032 branch merges into `master` and you next refresh the
`HighBar.Client` package:

```fsharp
// FSBarV1 bot code (e.g. warmup or tick handler)
let snap = client.GetGameState()
printfn "[gamestate] f=%d friendlies=%d los=%d radar=%d M=%.0f E=%.0f"
    snap.Frame
    snap.Friendlies.Length
    snap.LosEnemies.Length
    snap.RadarOnlyEnemies.Length
    snap.Economy.MetalCurrent
    snap.Economy.EnergyCurrent
```

The call is synchronous (same shape as `GetUnitPos` et al.), issues one
`CallbackRequest`, and the 031 replay buffer will silently preserve any
`Frame` messages that interleave during the wait. If the snapshot arrives
with `success=false` the method raises (existing HighBar pattern — we did
not wrap in `Result<_,_>`).

---

Thanks for the `EngineDiscovery` primitive — it was the right shape and
we moved faster by lifting it than by reinventing. If you'd rather we
stop duplicating and pull a shared NuGet out of it at some point, let's
sketch that separately.

— HighBarV2 maintainer, 032 branch
