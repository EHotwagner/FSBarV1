(**
---
title: Commands & Events
category: Tutorials
categoryindex: 2
index: 1
---
*)

(**
# Commands & Events

FSBarV1 provides 17 command builders for controlling units and 28 event types for reacting to
game state changes. This page documents all commands and events with usage examples.

## Commands

All command functions live in the `Commands` module. Each returns a `Highbar.AICommand` protobuf
message ready to send via `Protocol.sendFrameResponse` or return from a `StepWith`/`Run` handler.

Movement and action commands automatically set the internal order flag (`options = 8`).

### Movement Commands

#### MoveCommand

Move a unit to a position in world (elmo) coordinates.
*)

(*** do-not-eval ***)
open FSBar.Client

// Move unit 42 to position (2048, 100, 2048)
let cmd = Commands.MoveCommand 42 2048.0f 100.0f 2048.0f

(**
**Parameters**: `unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand`

#### PatrolCommand

Order a unit to patrol to a position. The unit will engage enemies along the way.
*)

(*** do-not-eval ***)
let patrol = Commands.PatrolCommand 42 1024.0f 100.0f 1024.0f

(**
**Parameters**: `unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand`

#### FightCommand

Fight-move to a position. The unit moves toward the target but engages enemies aggressively.
*)

(*** do-not-eval ***)
let fight = Commands.FightCommand 42 3000.0f 100.0f 3000.0f

(**
**Parameters**: `unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand`

#### SetWantedMaxSpeedCommand

Set the maximum speed for a unit.
*)

(*** do-not-eval ***)
let speed = Commands.SetWantedMaxSpeedCommand 42 5.5f

(**
**Parameters**: `unitId: int -> wantedMaxSpeed: float32 -> AICommand`

### Combat Commands

#### AttackCommand

Order a unit to attack a specific target unit.
*)

(*** do-not-eval ***)
let attack = Commands.AttackCommand 42 99

(**
**Parameters**: `unitId: int -> targetUnitId: int -> AICommand`

#### GuardCommand

Order a unit to guard (follow and protect) another unit.
*)

(*** do-not-eval ***)
let guard = Commands.GuardCommand 42 1

(**
**Parameters**: `unitId: int -> guardUnitId: int -> AICommand`

#### StopCommand

Order a unit to stop all current actions.
*)

(*** do-not-eval ***)
let stop = Commands.StopCommand 42

(**
**Parameters**: `unitId: int -> AICommand`

#### SelfDestructCommand

Order a unit to self-destruct (5 second countdown).
*)

(*** do-not-eval ***)
let selfDestruct = Commands.SelfDestructCommand 42

(**
**Parameters**: `unitId: int -> AICommand`

### Construction Commands

#### BuildCommand

Order a builder unit to construct a building at a position with a facing direction.
*)

(*** do-not-eval ***)
// Builder unit 1 builds unit-def 42 at (600, 100, 600), facing south (2)
let build = Commands.BuildCommand 1 42 600.0f 100.0f 600.0f 2

(**
**Parameters**: `unitId: int -> toBuildUnitDefId: int -> x: float32 -> y: float32 -> z: float32 -> facing: int -> AICommand`

Facing values: 0 = south, 1 = east, 2 = north, 3 = west.

#### RepairCommand

Order a unit to repair another unit.
*)

(*** do-not-eval ***)
let repair = Commands.RepairCommand 1 42

(**
**Parameters**: `unitId: int -> repairUnitId: int -> AICommand`

#### ReclaimUnitCommand

Order a unit to reclaim (destroy and recover resources from) another unit.
*)

(*** do-not-eval ***)
let reclaim = Commands.ReclaimUnitCommand 1 42

(**
**Parameters**: `unitId: int -> reclaimUnitId: int -> AICommand`

### Cheat Commands

These commands require cheats to be enabled in the game configuration.

#### GiveMeResourceCommand

Give yourself a resource amount. Resource IDs: 0 = metal, 1 = energy.
*)

(*** do-not-eval ***)
let metal = Commands.GiveMeResourceCommand 0 1000.0f
let energy = Commands.GiveMeResourceCommand 1 5000.0f

(**
**Parameters**: `resourceId: int -> amount: float32 -> AICommand`

#### GiveMeNewUnitCommand

Spawn a new unit at a position.
*)

(*** do-not-eval ***)
let spawn = Commands.GiveMeNewUnitCommand 42 2048.0f 100.0f 2048.0f

(**
**Parameters**: `unitDefId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand`

### Communication Commands

#### SendTextMessageCommand

Send a text message to the game chat.
*)

(*** do-not-eval ***)
let msg = Commands.SendTextMessageCommand "Hello from F#!" 0

(**
**Parameters**: `text: string -> zone: int -> AICommand`

#### CallLuaRulesCommand

Send a string to the game's Lua rules environment.
*)

(*** do-not-eval ***)
let lua = Commands.CallLuaRulesCommand "my_custom_data"

(**
**Parameters**: `data: string -> AICommand`

#### CallLuaUICommand

Send a string to the game's Lua UI environment.
*)

(*** do-not-eval ***)
let luaUi = Commands.CallLuaUICommand "ui_custom_data"

(**
**Parameters**: `data: string -> AICommand`

#### CustomCommand

Send an arbitrary command with a custom command ID and float parameters.
*)

(*** do-not-eval ***)
let custom = Commands.CustomCommand 42 999 [1.0f; 2.0f; 3.0f]

(**
**Parameters**: `unitId: int -> commandId: int -> params: float32 list -> AICommand`

---

## Events

Events arrive as a list inside each `GameFrame`. The `GameEvent` discriminated union has 28 cases
covering the full engine event lifecycle.

### GameFrame Record

*)

(*** do-not-eval ***)
type GameFrame = {
    FrameNumber: uint32
    Events: GameEvent list
}

(**
### GameEvent Discriminated Union

| Event | Fields | Description |
|-------|--------|-------------|
| `Init` | `teamId: int` | Game initialization, provides our team ID |
| `Release` | (none) | AI release notification |
| `Update` | `frame: int` | Frame tick, provides the engine frame number |
| `Message` | `player: int * message: string` | Chat message from a player |
| `UnitCreated` | `unitId: int * builderId: int` | A new unit started being built |
| `UnitFinished` | `unitId: int` | A unit completed construction |
| `UnitIdle` | `unitId: int` | A unit has no more orders |
| `UnitMoveFailed` | `unitId: int` | A unit could not reach its move target |
| `UnitDamaged` | `unitId * attackerId option * damage * weaponDefId * isParalyzer` | One of our units took damage |
| `UnitDestroyed` | `unitId: int * attackerId: int option` | One of our units was destroyed |
| `UnitGiven` | `unitId * oldTeamId * newTeamId` | A unit was transferred to another team |
| `UnitCaptured` | `unitId * oldTeamId * newTeamId` | A unit was captured by another team |
| `EnemyEnterLOS` | `enemyId: int` | An enemy unit entered our line of sight |
| `EnemyLeaveLOS` | `enemyId: int` | An enemy unit left our line of sight |
| `EnemyEnterRadar` | `enemyId: int` | An enemy unit entered our radar coverage |
| `EnemyLeaveRadar` | `enemyId: int` | An enemy unit left our radar coverage |
| `EnemyDamaged` | `enemyId * attackerId option * damage * weaponDefId` | An enemy unit took damage |
| `EnemyDestroyed` | `enemyId: int * attackerId: int option` | An enemy unit was destroyed |
| `WeaponFired` | `unitId: int * weaponDefId: int` | One of our units fired a weapon |
| `PlayerCommand` | `units: int list * commandTopicId * commandId` | A player issued a command |
| `SeismicPing` | `x * y * z * strength` | A seismic ping was detected |
| `CommandFinished` | `unitId * commandId * commandTopicId` | A unit finished executing a command |
| `Load` | (none) | Game load notification |
| `Save` | (none) | Game save notification |
| `EnemyCreated` | `enemyId: int` | An enemy unit was created |
| `EnemyFinished` | `enemyId: int` | An enemy unit finished construction |
| `LuaMessage` | `data: string * inMessageId: int` | A message from Lua rules/UI |
| `Shutdown` | `reason: string` | Engine is shutting down |
| `Unknown` | (none) | Unrecognized event type |

### Event Handling Pattern

A typical frame handler processes events with pattern matching:
*)

(*** do-not-eval ***)
open FSBar.Client

let handleFrame (frame: GameFrame) : Highbar.AICommand list =
    let mutable commands = []

    for evt in frame.Events do
        match evt with
        | GameEvent.UnitIdle uid ->
            // Send idle units to patrol
            commands <- Commands.PatrolCommand uid 2048.0f 100.0f 2048.0f :: commands

        | GameEvent.UnitCreated(uid, builderId) ->
            printfn "Unit %d created by builder %d" uid builderId

        | GameEvent.UnitFinished uid ->
            printfn "Unit %d construction complete" uid

        | GameEvent.UnitDamaged(uid, attacker, damage, _, _) ->
            printfn "Unit %d took %.0f damage" uid damage
            match attacker with
            | Some attackerId ->
                commands <- Commands.AttackCommand uid attackerId :: commands
            | None -> ()

        | GameEvent.EnemyEnterLOS enemyId ->
            printfn "Enemy %d spotted!" enemyId

        | GameEvent.UnitDestroyed(uid, _) ->
            printfn "Unit %d destroyed" uid

        | _ -> ()

    commands

(**
### Using with BarClient.Run
*)

(*** do-not-eval ***)
let client = BarClient.startHeadless ()
let frames = client.Run(1000, handleFrame)
client.Stop()
