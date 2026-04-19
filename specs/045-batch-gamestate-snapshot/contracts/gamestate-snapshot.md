# Contract: `CALLBACK_GAME_GET_STATE`

**Callback ID**: 15
**Oneof variant**: `CallbackResult.value = snapshot_value` (field 8)
**Upstream spec**: HighBarV2 `specs/032-batch-callback-rpcs/contracts/gamestate-snapshot.md` (0.1.5)

## Request

```
CallbackRequest {
  callback_id = 15;
  params = [];           // always empty
}
```

## Response — success

```
CallbackResponse {
  success = true;
  result = { snapshot_value = GameStateSnapshot { ... } };
}
```

## Response — failure (terminal, no partial state)

```
CallbackResponse {
  success = false;
  error_message = "<descriptive>";
}
```

Error-message prefixes the client recognizes:

| Prefix | Client action |
|--------|---------------|
| `Snapshot unit count exceeds HIGHBAR_SNAPSHOT_MAX_UNITS` | Raise descriptive error; prior `GameState` retained |
| `Unknown callback id` (first call only) | Raise `ProxyVersionMismatchException`; terminate session |
| Any other | Raise generic snapshot error; prior `GameState` retained |

## Invariants enforced by the proxy (trusted by the client)

- All four list memberships are mutually consistent:
  - `friendlies[i].team` always equals `myTeam`.
  - `los_enemies` and `radar_only_enemies` are disjoint.
- `radar_only_enemies[i]` has NO `health` field (type-level).
- `economy` is always present on success.
- `frame` matches the engine frame number at enumeration time.

## Client invariants (enforced by the mapper)

- A unit appearing in `friendlies` but absent from prior `state.Units`
  is inserted with `MaxHealth = 0.0f`, `IsFinished = false`,
  `IsIdle = false` (subsequent `UnitCreated` / `UnitFinished` events
  will correct `MaxHealth` and `IsFinished`).
- An enemy absent from both `los_enemies` and `radar_only_enemies`
  retains its prior `Position` and has `Health = None`,
  `InLOS = false`, `InRadar = false`.
- Radar-only enemies never have `Health = Some _` — the mapper sets
  `Health = None` even if the prior state had a value.

## Version gating

- Required HighBarV2 proxy version: `>= 0.1.5`.
- Pre-0.1.5 proxies reject callback 15 with `Unknown callback id`; the
  client raises `ProxyVersionMismatchException` immediately and the
  session terminates. No fallback.
