# Phase 1 Data Model: Batched GameState snapshot

## Wire-level (proto) — `proto/highbar/callbacks.proto`

### Enum addition

`CallbackId.CALLBACK_GAME_GET_STATE = 15`

### New messages

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
  int32 unit_def_id = 3;  // NOTE: no health field by design
  int32 team = 4;
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

### Oneof extension

`CallbackResult.value` gains `GameStateSnapshot snapshot_value = 8;`.

## Client-level (F#) — `FSBar.Client.Callbacks`

New publicly exposed record types:

```fsharp
type FriendlyUnitSnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    Health: float32
    UnitDefId: int
    Team: int
}

type LosEnemySnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    Health: float32
    UnitDefId: int
    Team: int
}

type RadarOnlyEnemySnapshot = {
    UnitId: int
    Position: float32 * float32 * float32
    UnitDefId: int
    Team: int
}

type EconomyRecordSnapshot = {
    MetalCurrent: float32; MetalIncome: float32
    MetalUsage: float32;   MetalStorage: float32
    EnergyCurrent: float32; EnergyIncome: float32
    EnergyUsage: float32;   EnergyStorage: float32
}

type GameStateSnapshotResult = {
    Frame: int
    Friendlies: FriendlyUnitSnapshot list
    LosEnemies: LosEnemySnapshot list
    RadarOnlyEnemies: RadarOnlyEnemySnapshot list
    Economy: EconomyRecordSnapshot
}
```

New callback entry point:

```fsharp
val getGameStateSnapshot: stream: NetworkStream -> GameStateSnapshotResult
```

Raises `ProxyVersionMismatchException` when the proxy rejects callback
id 15. Raises `EngineDisconnectedException` on connection loss (existing
path, unchanged).

## Client-level — existing types (unchanged public shape)

- `EconomySnapshot` — unchanged.
- `TrackedUnit` — unchanged.
- `TrackedEnemy` — unchanged (already has `Health: float32 option` and
  `Position` frozen when `InLOS=false && InRadar=false`).
- `GameState` — unchanged.

## Mapping from snapshot → `GameState`

For each `GameEvent.Update` with prior `state` and fresh `snapshot`:

```
Units' =
  snapshot.Friendlies
  |> List.map (fun f ->
      f.UnitId, {
          UnitId    = f.UnitId
          DefId     = f.UnitDefId
          Position  = f.Position
          Health    = f.Health
          MaxHealth = prior.Get(f.UnitId).MaxHealth  // retained from state.Units or 0.0f if newly seen
          IsFinished = prior.Get(f.UnitId).IsFinished // retained; GameEvent.UnitFinished keeps authority
          IsIdle    = if f.Position <> prior.Position then false else prior.IsIdle
      })
  |> Map.ofList

Enemies' =
  (prior.Enemies
   |> Map.map (fun _ e -> { e with InLOS = false; InRadar = false; Health = None }))  // baseline
  |> foldIn snapshot.LosEnemies  (fun e l ->
      { e with InLOS = true;  InRadar = e.InRadar;
               Position = l.Position;
               Health = Some l.Health;
               DefId = Some l.UnitDefId })
  |> foldIn snapshot.RadarOnlyEnemies (fun e r ->
      { e with InRadar = true;
               Position = r.Position;
               DefId = e.DefId |> Option.orElse (Some r.UnitDefId) })
  // entries not mentioned: keep prior position (set by baseline step which only clears LOS/Radar/Health)

Metal'  = fromEconomy snapshot.Economy metal-fields
Energy' = fromEconomy snapshot.Economy energy-fields
```

## Engine discovery — `EngineDiscovery`

No new types. One added helper function + documentation changes:

```fsharp
val resolveOverrideEnvVar:
    unit ->
        {| Value: string option
           Conflict: (string * string) option |}   // (fsbarVal, highbarVal) when both set differently
```

`ResolutionSource.OverrideEnvVar` label updated to
`"env:FSBAR_TEST_ENGINE"` (or the chosen variable name in the conflict
case).

## Error types

New exception:

```fsharp
exception ProxyVersionMismatchException of message: string * requiredVersion: string
```

Placed in `FSBar.Client.Protocol` (alongside `EngineDisconnectedException`).

## State transitions

- **Unit**: snapshot-authoritative membership; `IsFinished` /
  `IsIdle` remain event-driven.
- **Enemy**: three states per tick — `InLOS` (health known) →
  `InRadar only` (health=None, position jittered) → `stale` (not in
  snapshot; position frozen, health=None). Transitions via whichever
  list the snapshot places the id in.
- **Economy**: fully replaced per snapshot; no accumulation.
