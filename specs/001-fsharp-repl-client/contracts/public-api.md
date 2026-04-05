# Public API Contract: FSBar.Client

**Branch**: `001-fsharp-repl-client` | **Date**: 2026-04-05

This document defines the public API surface that will be exposed by FSBar.Client and enforced via `.fsi` signature files.

## FSBar.Client.BarClient

The primary entry point for REPL users.

```fsharp
namespace FSBar.Client

/// Configuration for engine launch.
type EngineMode =
    | Headless
    | Graphical

type EngineConfig = {
    Mode: EngineMode
    SocketPath: string
    MapName: string
    GameType: string
    OpponentAI: string
    OpponentSide: string
    OurSide: string
    TimeoutMs: int
    EngineBin: string
    AppImagePath: string
    SpringDataDir: string option
    GameSpeed: int
}

/// Lifecycle state of the client.
type SessionState =
    | Idle
    | Starting
    | Connected
    | Running
    | Stopped
    | Error of string

/// Information from the proxy handshake.
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

/// The main client object for REPL use.
type BarClient =
    /// Current session state.
    member State: SessionState
    /// Configuration for this client.
    member Config: EngineConfig
    /// Handshake info (available after connection).
    member Handshake: HandshakeInfo option
    /// Launch engine, connect to proxy, complete handshake.
    member Start: unit -> unit
    /// Receive one frame, send empty command response. Returns the frame.
    member Step: unit -> GameFrame
    /// Receive one frame, apply handler to get commands, send them. Returns the frame.
    member StepWith: (GameFrame -> AICommand list) -> GameFrame
    /// Run N frames with a handler function. Returns all frames.
    member Run: int -> (GameFrame -> AICommand list) -> GameFrame list
    /// Run until predicate returns true. Returns all frames.
    member RunUntil: (GameFrame -> bool) -> (GameFrame -> AICommand list) -> GameFrame list
    /// Reset game state (destroy units, reset resources).
    member Reset: unit -> unit
    /// Gracefully stop the engine and clean up.
    member Stop: unit -> unit
    /// IDisposable for resource cleanup.
    interface System.IDisposable

module BarClient =
    /// Default engine configuration.
    val defaultConfig: unit -> EngineConfig
    /// Create a client with default config (headless mode).
    val startHeadless: unit -> BarClient
    /// Create a client with graphical mode.
    val startGraphical: unit -> BarClient
    /// Create a client with custom config.
    val create: EngineConfig -> BarClient
```

## FSBar.Client.Events

```fsharp
namespace FSBar.Client

/// Typed game event discriminated union (28 variants).
[<RequireQualifiedAccess>]
type GameEvent =
    | Init of teamId: int
    | Release
    | Update of frame: int
    | Message of player: int * message: string
    | UnitCreated of unitId: int * builderId: int
    | UnitFinished of unitId: int
    | UnitIdle of unitId: int
    | UnitMoveFailed of unitId: int
    | UnitDamaged of unitId: int * attackerId: int option * damage: float32 * weaponDefId: int * isParalyzer: bool
    | UnitDestroyed of unitId: int * attackerId: int option
    | UnitGiven of unitId: int * oldTeamId: int * newTeamId: int
    | UnitCaptured of unitId: int * oldTeamId: int * newTeamId: int
    | EnemyEnterLOS of enemyId: int
    | EnemyLeaveLOS of enemyId: int
    | EnemyEnterRadar of enemyId: int
    | EnemyLeaveRadar of enemyId: int
    | EnemyDamaged of enemyId: int * attackerId: int option * damage: float32 * weaponDefId: int
    | EnemyDestroyed of enemyId: int * attackerId: int option
    | WeaponFired of unitId: int * weaponDefId: int
    | PlayerCommand of units: int list * commandTopicId: int * commandId: int
    | SeismicPing of x: float32 * y: float32 * z: float32 * strength: float32
    | CommandFinished of unitId: int * commandId: int * commandTopicId: int
    | Load
    | Save
    | EnemyCreated of enemyId: int
    | EnemyFinished of enemyId: int
    | LuaMessage of data: string * inMessageId: int
    | Shutdown of reason: string
    | Unknown

type GameFrame = {
    FrameNumber: uint32
    Events: GameEvent list
}
```

## FSBar.Client.Commands

```fsharp
namespace FSBar.Client

/// Typed command builders returning protobuf AICommand messages.
module Commands =
    val MoveCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val BuildCommand: unitId: int -> unitDefId: int -> x: float32 -> y: float32 -> z: float32 -> facing: int -> AICommand
    val PatrolCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val AttackCommand: unitId: int -> targetUnitId: int -> AICommand
    val GuardCommand: unitId: int -> guardUnitId: int -> AICommand
    val StopCommand: unitId: int -> AICommand
    val RepairCommand: unitId: int -> repairUnitId: int -> AICommand
    val ReclaimUnitCommand: unitId: int -> reclaimUnitId: int -> AICommand
    val FightCommand: unitId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val SelfDestructCommand: unitId: int -> AICommand
    val SetWantedMaxSpeedCommand: unitId: int -> speed: float32 -> AICommand
    val CustomCommand: unitId: int -> commandId: int -> parameters: float32 list -> AICommand
    val SendTextMessageCommand: text: string -> zone: int -> AICommand
    val GiveMeResourceCommand: resourceId: int -> amount: float32 -> AICommand
    val GiveMeNewUnitCommand: unitDefId: int -> x: float32 -> y: float32 -> z: float32 -> AICommand
    val CallLuaRulesCommand: data: string -> AICommand
    val CallLuaUICommand: data: string -> AICommand
```

## FSBar.Client.Callbacks

```fsharp
namespace FSBar.Client

/// Convenience wrappers for engine callback queries.
module Callbacks =
    val getMyTeam: BarClient -> int
    val getMyAllyTeam: BarClient -> int
    val getMapWidth: BarClient -> int
    val getMapHeight: BarClient -> int
    val getStartPos: BarClient -> teamId: int -> float32 * float32 * float32
    val getMetalSpots: BarClient -> (float32 * float32 * float32 * float32) array
    val getUnitPos: BarClient -> unitId: int -> float32 * float32 * float32
    val getUnitHealth: BarClient -> unitId: int -> float32
    val getUnitMaxHealth: BarClient -> unitId: int -> float32
    val getUnitDef: BarClient -> unitId: int -> int
    val getUnitDefName: BarClient -> defId: int -> string
    val getBuildOptions: BarClient -> defId: int -> int array
    val getMaxWeaponRange: BarClient -> defId: int -> float32
    val getBuildSpeed: BarClient -> defId: int -> float32
    val getUnitDefCost: BarClient -> defId: int -> float32
    val getEconomyCurrent: BarClient -> resourceId: int -> float32
    val getEconomyIncome: BarClient -> resourceId: int -> float32
    val getEconomyUsage: BarClient -> resourceId: int -> float32
    val getEconomyStorage: BarClient -> resourceId: int -> float32
    val getUnitDefs: BarClient -> maxCount: int -> int array
```
