# Commands and Events

## Commands

Commands are sent to the engine as part of the `FrameResponse`. Each builder function in the `Commands` module creates an `AICommand` protobuf message with the `INTERNAL_ORDER` flag (bit 3) and maximum timeout set automatically.

### Movement Commands

**MoveCommand** — Move a unit to a world position.

```fsharp
Commands.MoveCommand unitId x y z
// Example: move unit 42 to center of map
Commands.MoveCommand 42 4096.0f 100.0f 4096.0f
```

**PatrolCommand** — Patrol to a position (unit will loop between waypoints).

```fsharp
Commands.PatrolCommand unitId x y z
```

**FightCommand** — Attack-move: move toward position, engaging enemies along the way.

```fsharp
Commands.FightCommand unitId x y z
```

**StopCommand** — Halt all current orders.

```fsharp
Commands.StopCommand unitId
```

### Combat Commands

**AttackCommand** — Attack a specific target unit.

```fsharp
Commands.AttackCommand unitId targetUnitId
```

**GuardCommand** — Follow and protect another unit.

```fsharp
Commands.GuardCommand unitId guardUnitId
```

**SelfDestructCommand** — Self-destruct the unit (5-second countdown).

```fsharp
Commands.SelfDestructCommand unitId
```

### Construction Commands

**BuildCommand** — Order a constructor to build a structure.

```fsharp
Commands.BuildCommand unitId toBuildUnitDefId x y z facing
// facing: 0=south, 1=east, 2=north, 3=west
```

The `toBuildUnitDefId` can be obtained from `Callbacks.getBuildOptions`.

### Maintenance Commands

**RepairCommand** — Repair a damaged unit.

```fsharp
Commands.RepairCommand unitId repairUnitId
```

**ReclaimUnitCommand** — Reclaim a unit (dead or alive) for resources.

```fsharp
Commands.ReclaimUnitCommand unitId reclaimUnitId
```

### Control Commands

**SetWantedMaxSpeedCommand** — Limit a unit's movement speed.

```fsharp
Commands.SetWantedMaxSpeedCommand unitId maxSpeed
```

**CustomCommand** — Send an arbitrary command by ID with float parameters.

```fsharp
Commands.CustomCommand unitId commandId [ param1; param2 ]
```

### Communication Commands

**SendTextMessageCommand** — Send a chat message (useful for enabling cheats with `.cheat`).

```fsharp
Commands.SendTextMessageCommand ".cheat" 0
```

**CallLuaRulesCommand** / **CallLuaUICommand** — Execute Lua code in the engine.

```fsharp
Commands.CallLuaRulesCommand "some_lua_data"
```

### Cheat Commands

These require cheats to be enabled (send `.cheat` text message first).

**GiveMeResourceCommand** — Add or remove resources (0=metal, 1=energy).

```fsharp
Commands.GiveMeResourceCommand 0 1000.0f   // +1000 metal
Commands.GiveMeResourceCommand 1 -500.0f   // -500 energy
```

**GiveMeNewUnitCommand** — Spawn a unit at a position.

```fsharp
Commands.GiveMeNewUnitCommand unitDefId x y z
```

---

## Events

Events arrive in each `GameFrame.Events` list. They are represented as a discriminated union requiring qualified access (`GameEvent.Init`, not just `Init`).

### Lifecycle Events

| Event | Fields | When |
|-------|--------|------|
| `Init` | `teamId: int` | Once at game start |
| `Release` | — | AI being released |
| `Update` | `frame: int` | Every game tick |
| `Load` | — | Game state being loaded |
| `Save` | — | Game state being saved |
| `Shutdown` | `reason: string` | Game ending |

### Unit Events

| Event | Fields | When |
|-------|--------|------|
| `UnitCreated` | `unitId, builderId` | Construction started (builderId=0 for commanders) |
| `UnitFinished` | `unitId` | Construction completed |
| `UnitIdle` | `unitId` | Unit has no orders |
| `UnitMoveFailed` | `unitId` | Pathfinding failure |
| `UnitDamaged` | `unitId, attackerId option, damage, weaponDefId, isParalyzer` | Unit took damage |
| `UnitDestroyed` | `unitId, attackerId option` | Unit killed |
| `UnitGiven` | `unitId, oldTeamId, newTeamId` | Unit transferred between teams |
| `UnitCaptured` | `unitId, oldTeamId, newTeamId` | Unit captured by enemy |

### Enemy Events

| Event | Fields | When |
|-------|--------|------|
| `EnemyEnterLOS` | `enemyId` | Enemy enters line of sight |
| `EnemyLeaveLOS` | `enemyId` | Enemy leaves line of sight |
| `EnemyEnterRadar` | `enemyId` | Enemy enters radar range |
| `EnemyLeaveRadar` | `enemyId` | Enemy leaves radar range |
| `EnemyDamaged` | `enemyId, attackerId option, damage, weaponDefId` | Enemy took damage |
| `EnemyDestroyed` | `enemyId, attackerId option` | Enemy killed |
| `EnemyCreated` | `enemyId` | Enemy unit spotted for first time |
| `EnemyFinished` | `enemyId` | Enemy construction completed |

### Other Events

| Event | Fields | When |
|-------|--------|------|
| `WeaponFired` | `unitId, weaponDefId` | Our unit fired a weapon |
| `PlayerCommand` | `units list, commandTopicId, commandId` | Player issued a command |
| `SeismicPing` | `x, y, z, strength` | Seismic event detected |
| `CommandFinished` | `unitId, commandId, commandTopicId` | Command completed |
| `Message` | `player, message` | Chat message received |
| `LuaMessage` | `data, inMessageId` | Lua widget/gadget message |
| `Unknown` | — | Unrecognized event (safe to ignore) |

### Pattern Matching Example

```fsharp
let handler (frame: GameFrame) =
    let commands = ResizeArray<AICommand>()
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitIdle uid ->
            // Send idle units to patrol
            commands.Add(Commands.PatrolCommand uid 4096.0f 100.0f 4096.0f)
        | GameEvent.UnitDamaged(uid, Some attackerId, _, _, _) ->
            // Fight back
            commands.Add(Commands.AttackCommand uid attackerId)
        | GameEvent.EnemyEnterLOS enemyId ->
            printfn "Enemy spotted: %d" enemyId
        | _ -> ()
    commands |> Seq.toList
```
