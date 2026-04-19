# Quickstart — 046-scripting-full-client

Audience: a developer picking up this feature after `/speckit.plan`
and before `/speckit.tasks`.

## Prerequisites

- Feature 045 has landed (`BarClient.GameState` is the authoritative
  per-tick snapshot). Verify:
  `grep -R "CALLBACK_GAME_GET_STATE" src/FSBar.Client/` returns hits.
- Live engine available via `EngineDiscovery` or `FSBAR_TEST_ENGINE`.
- Skills ready: `proto-regen`, `hub-run`, `fsi-fsbar-load`.

## Build order

1. **Edit the proto** — `proto/hub/scripting.proto`:
   - Extend `GameFrameMessage` with `game_state` and `game_events`.
   - Add `GameStateFrame`, `FriendlyUnitState`, `EnemyUnitState`
     (with `oneof health_info`), `EconomySnapshotWire`,
     `GameEventEnvelope`.
   - Add the eight map-query messages + `MetalSpot`.
   - Add `UnitDefInfoExtended` (superset of existing `UnitDefInfo`).
   - Add `SendCommandBatchRequest/Response/CommandOutcome`.
   - Change `GetUnitDef` return to `UnitDefInfoExtended`.
   - Add the eight new unary RPCs + `SendCommandBatch`.
2. **Regenerate F#** — run `/skill proto-regen`.
3. **Gate: `buf breaking`** MUST be clean.
4. **Implement the Hub side** — `src/FSBar.Hub/ScriptingHub.fs(i)`:
   - Extend `StreamGameFrames` handler to co-emit `GameStateFrame`
     + `GameEventEnvelope[]` per proxy frame.
   - Add handlers for the eight map-query RPCs, delegating to
     `FSBar.Client.Callbacks`.
   - Upgrade `GetUnitDef` to populate `UnitDefInfoExtended`.
   - Implement `SendCommandBatch`: validate size cap (1024) first,
     then forward the batch in one engine dispatch and return
     per-entry outcomes.
   - Raise `MaxReceiveMessageSize` / `MaxSendMessageSize` on the
     scripting channel (server + default client).
5. **Update `.fsi` signatures** for any new public API in
   `ScriptingHub.fsi`; regenerate surface-area baselines with
   `SURFACE_AREA_UPDATE=1 dotnet test`.
6. **Live tests** under `tests/FSBar.Hub.LiveTests/`:
   - `StateStreamLiveTests` — US1 acceptance scenarios 1–5.
   - `EventsStreamLiveTests` — US2 acceptance scenarios 1–3.
   - `MapQueriesLiveTests` — US3 acceptance scenarios 1–3, plus
     SC-007 byte-identity against `bots/trainer/map-cache/*.json`
     for the three SupportedMaps.
   - `CommandBatchLiveTests` — US4 acceptance scenarios 1–3.
   - `CommandSurfaceLiveTests` — one live assertion per FR-009
     command variant.
7. **Scripting walkthrough** — add
   `scripts/examples/24-hub-full-client.fsx` (SC-006: under 50 lines,
   launches session, streams state+events for N ticks, sends a
   batched command, prints summary).
8. **Docs** — update Hub scripting README + `CLAUDE.md` scripting
   section (FR-013).

## Acceptance gates

- `dotnet build FSBarV1.slnx` green.
- `dotnet test FSBarV1.slnx` green (including surface-area tests).
- Live tests pass against the discovered engine.
- `buf breaking` zero incompatibilities (SC-005).
- FSI walkthrough runs end-to-end.
- SC-002 / SC-003 / SC-007 asserted by the corresponding live tests.

## Where to look in the existing code

- `src/FSBar.Client/GameState.fs` — authoritative per-tick state
  (feature 045).
- `src/FSBar.Client/Callbacks.fs(i)` — map-query functions to
  forward.
- `src/FSBar.Hub/ScriptingHub.fs(i)` — current scripting surface;
  all new RPCs land here.
- `scripts/examples/22-hub-log-stream.fsx` and
  `23-gamestate-snapshot.fsx` — closest existing walkthroughs to
  use as a structural template for `24-hub-full-client.fsx`.
