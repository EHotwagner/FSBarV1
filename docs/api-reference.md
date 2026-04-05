# API Reference

Complete public API surface for FSBar.Client. All signatures are from the `.fsi` files which serve as the authoritative contract.

## EngineConfig

Configuration for engine sessions.

```fsharp
type EngineMode =
    | Headless      // No display, for automated testing and CI
    | Graphical     // Full game window with rendering

type EngineConfig = {
    Mode: EngineMode                // Headless or Graphical
    SocketPath: string              // Unix socket path (auto-generated)
    MapName: string                 // BAR map name
    GameType: string                // BAR mod version string
    OpponentAI: string              // AI opponent: "NullAI", "BARb", "CircuitAI"
    OpponentSide: string            // "Cortex" or "Armada"
    OurSide: string                 // "Armada" or "Cortex"
    TimeoutMs: int                  // Connection timeout in milliseconds
    EngineBin: string               // Path to spring-headless binary
    AppImagePath: string            // Path to graphical engine binary/AppImage
    SpringDataDir: string option    // Game data directory (auto-detected if None)
    GameSpeed: int                  // Game speed multiplier (100 = normal)
}

module EngineConfig =
    val defaultConfig: unit -> EngineConfig
```

**Defaults**: Headless mode, "Red Rock Desert v2" map, NullAI opponent, Armada vs Cortex, 30s timeout, 100x speed.

## BarClient

Main session orchestrator. Implements `IDisposable` for resource cleanup.

```fsharp
type SessionState =
    | Idle | Starting | Connected | Running | Stopped | Error of string

type BarClient =
    new: config: EngineConfig -> BarClient

    member State: SessionState
    member Config: EngineConfig
    member Handshake: HandshakeInfo option
    member Stream: NetworkStream          // Throws if not connected

    member Start: unit -> unit            // Launch engine, accept proxy, handshake
    member Step: unit -> GameFrame        // Receive one frame, send empty response
    member StepWith: (GameFrame -> AICommand list) -> GameFrame
                                          // Receive frame, send handler's commands
    member Run: int * (GameFrame -> AICommand list) -> GameFrame list
                                          // Run N frames with handler
    member RunUntil: (GameFrame -> bool) * (GameFrame -> AICommand list) -> GameFrame list
                                          // Run until predicate is true
    member Reset: unit -> unit            // Reset game state via cheats
    member Stop: unit -> unit             // Shut down engine and clean up

    interface IDisposable

module BarClient =
    val defaultConfig: unit -> EngineConfig
    val create: EngineConfig -> BarClient
    val startHeadless: unit -> BarClient  // Create + Start with headless defaults
    val startGraphical: unit -> BarClient // Create + Start with graphical defaults
```

## GameFrame and GameEvent

Frame data received each game tick.

```fsharp
type GameFrame = {
    FrameNumber: uint32
    Events: GameEvent list
}

[<RequireQualifiedAccess>]
type GameEvent =
    // Lifecycle
    | Init of teamId: int
    | Release
    | Update of frame: int
    | Load
    | Save
    | Shutdown of reason: string

    // Units
    | UnitCreated of unitId: int * builderId: int
    | UnitFinished of unitId: int
    | UnitIdle of unitId: int
    | UnitMoveFailed of unitId: int
    | UnitDamaged of unitId: int * attackerId: int option * damage: float32
                     * weaponDefId: int * isParalyzer: bool
    | UnitDestroyed of unitId: int * attackerId: int option
    | UnitGiven of unitId: int * oldTeamId: int * newTeamId: int
    | UnitCaptured of unitId: int * oldTeamId: int * newTeamId: int

    // Enemies
    | EnemyEnterLOS of enemyId: int
    | EnemyLeaveLOS of enemyId: int
    | EnemyEnterRadar of enemyId: int
    | EnemyLeaveRadar of enemyId: int
    | EnemyDamaged of enemyId: int * attackerId: int option * damage: float32
                      * weaponDefId: int
    | EnemyDestroyed of enemyId: int * attackerId: int option
    | EnemyCreated of enemyId: int
    | EnemyFinished of enemyId: int

    // Combat & Communication
    | WeaponFired of unitId: int * weaponDefId: int
    | PlayerCommand of units: int list * commandTopicId: int * commandId: int
    | SeismicPing of x: float32 * y: float32 * z: float32 * strength: float32
    | CommandFinished of unitId: int * commandId: int * commandTopicId: int
    | Message of player: int * message: string
    | LuaMessage of data: string * inMessageId: int
    | Unknown
```

## HandshakeInfo

Metadata returned after successful proxy handshake.

```fsharp
type HandshakeInfo = {
    ProtocolVersion: uint32
    EngineVersion: string
    GameId: string
    MapName: string
    ModName: string
    TeamId: int
    AllyTeamId: int
    PlayerCount: int
}
```

## Commands Module

Builder functions for AI commands. All commands include the `INTERNAL_ORDER` flag and max timeout automatically.

```fsharp
module Commands =
    // Movement
    val MoveCommand:    unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val PatrolCommand:  unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val FightCommand:   unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val StopCommand:    unitId: int -> AICommand

    // Combat
    val AttackCommand:  unitId: int -> targetUnitId: int -> AICommand
    val GuardCommand:   unitId: int -> guardUnitId: int -> AICommand
    val SelfDestructCommand: unitId: int -> AICommand

    // Construction
    val BuildCommand:   unitId: int -> toBuildUnitDefId: int
                        -> x: float32 -> y: float32 -> z: float32 -> facing: int -> AICommand

    // Maintenance
    val RepairCommand:       unitId: int -> repairUnitId: int -> AICommand
    val ReclaimUnitCommand:  unitId: int -> reclaimUnitId: int -> AICommand

    // Control
    val SetWantedMaxSpeedCommand: unitId: int -> wantedMaxSpeed: float32 -> AICommand
    val CustomCommand:  unitId: int -> commandId: int -> params: float32 list -> AICommand

    // Communication
    val SendTextMessageCommand: text: string -> zone: int -> AICommand
    val CallLuaRulesCommand:    data: string -> AICommand
    val CallLuaUICommand:       data: string -> AICommand

    // Cheats
    val GiveMeResourceCommand:  resourceId: int -> amount: float32 -> AICommand
    val GiveMeNewUnitCommand:   unitDefId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
```

## Callbacks Module

Mid-frame queries for game state. Must be called between receiving a frame and sending the response (via raw `Protocol` API) or during `StepWith` handlers at high game speeds.

```fsharp
module Callbacks =
    // Team info
    val getMyTeam:      stream: NetworkStream -> int
    val getMyAllyTeam:  stream: NetworkStream -> int

    // Map queries
    val getMapWidth:    stream: NetworkStream -> int
    val getMapHeight:   stream: NetworkStream -> int
    val getStartPos:    stream: NetworkStream -> teamId: int -> float32 * float32 * float32
    val getMetalSpots:  stream: NetworkStream -> (float32 * float32 * float32 * float32) array

    // Unit state
    val getUnitPos:       stream: NetworkStream -> unitId: int -> float32 * float32 * float32
    val getUnitHealth:    stream: NetworkStream -> unitId: int -> float32
    val getUnitMaxHealth: stream: NetworkStream -> unitId: int -> float32
    val getUnitDef:       stream: NetworkStream -> unitId: int -> int

    // Unit definitions
    val getUnitDefName:     stream: NetworkStream -> defId: int -> string
    val getBuildOptions:    stream: NetworkStream -> defId: int -> int array
    val getMaxWeaponRange:  stream: NetworkStream -> defId: int -> float32
    val getBuildSpeed:      stream: NetworkStream -> defId: int -> float32
    val getUnitDefCost:     stream: NetworkStream -> defId: int -> float32

    // Economy
    val getEconomyCurrent:  stream: NetworkStream -> resourceId: int -> float32
    val getEconomyIncome:   stream: NetworkStream -> resourceId: int -> float32
    val getEconomyUsage:    stream: NetworkStream -> resourceId: int -> float32
    val getEconomyStorage:  stream: NetworkStream -> resourceId: int -> float32

    // Bulk
    val getUnitDefs: stream: NetworkStream -> maxCount: int -> int array
```

## Connection Module

Low-level Unix socket communication.

```fsharp
module Connection =
    val createListener:    socketPath: string -> Socket
    val acceptConnection:  listener: Socket -> timeoutMs: int -> Socket * NetworkStream
    val sendMessage:       stream: NetworkStream -> data: byte[] -> unit
    val recvBytes:         stream: NetworkStream -> byte[]
    val cleanup:           socketPath: string -> Socket option -> unit
```

## Protocol Module

Protobuf message exchange protocol.

```fsharp
module Protocol =
    val handshake:         stream: NetworkStream -> HandshakeInfo
    val receiveFrame:      stream: NetworkStream -> GameFrame option
    val sendFrameResponse: stream: NetworkStream -> commands: AICommand list -> unit
    val sendCallback:      stream: NetworkStream -> callbackId: uint32
                           -> paramList: CallbackParam list -> CallbackResponse
```

## EngineLauncher Module

Engine process lifecycle management.

```fsharp
module EngineLauncher =
    val launchHeadless:  config: EngineConfig -> scriptContent: string -> Process
    val launchGraphical: config: EngineConfig -> scriptContent: string -> Process
    val stopEngine:      socketPath: string -> proc: Process -> unit
    val getSessionDir:   config: EngineConfig -> string
```

## ScriptGenerator Module

Generates Spring engine start scripts from configuration.

```fsharp
module ScriptGenerator =
    val generate: config: EngineConfig -> string
```

## Events Module

Protobuf event deserialization.

```fsharp
module Events =
    val fromProto: Highbar.EngineEvent -> GameEvent
```
