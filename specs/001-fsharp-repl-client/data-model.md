# Data Model: F# REPL Client for BAR AI Orchestration

**Branch**: `001-fsharp-repl-client` | **Date**: 2026-04-05

## Core Entities

### EngineMode (Discriminated Union)

Represents the two supported engine launch modes.

| Variant | Description |
|---------|-------------|
| Headless | Uses `spring-headless` binary, no display required |
| Graphical | Uses BAR AppImage, windowed mode, requires display |

### EngineConfig (Record)

Configuration for launching and connecting to a BAR engine instance.

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| Mode | EngineMode | Headless | Engine launch mode |
| SocketPath | string | `/tmp/fsbar-<guid>.sock` | Unix domain socket path |
| MapName | string | `"Red Rock Desert v2"` | BAR map name |
| GameType | string | auto-detected | BAR game archive name |
| OpponentAI | string | `"NullAI"` | Opponent AI short name |
| OpponentSide | string | `"Cortex"` | Opponent faction |
| OurSide | string | `"Armada"` | Our faction |
| TimeoutMs | int | 30000 | Proxy connection timeout (ms) |
| EngineBin | string | `"spring-headless"` | Headless engine binary path |
| AppImagePath | string | `"~/applications/Beyond-All-Reason-*.AppImage"` | BAR AppImage path |
| SpringDataDir | string option | None (auto-detect) | Engine data directory override |
| GameSpeed | int | 100 | Engine game speed (1-100) |

### SessionState (Discriminated Union)

Lifecycle state of the BarClient.

| Variant | Description |
|---------|-------------|
| Idle | No game running, ready to start |
| Starting | Engine launched, waiting for proxy connection |
| Connected | Proxy connected, handshake complete, ready for frames |
| Running | Actively in frame loop |
| Stopped | Game ended or shutdown, resources cleaned up |
| Error of string | Unrecoverable error with description |

**State transitions**:
```
Idle → Starting → Connected → Running → Stopped → Idle
                                  ↓
                               Error
Starting → Error (timeout, engine crash)
Connected → Error (disconnection)
Running → Error (engine crash, socket error)
```

### GameFrame (Record)

A single frame received from the proxy.

| Field | Type | Description |
|-------|------|-------------|
| FrameNumber | uint32 | Engine frame counter |
| Events | GameEvent list | Typed events for this frame |

### GameEvent (Discriminated Union — 28 variants)

Mirrors HighBarV2's `EngineEvent` oneof with idiomatic F# types. Key variants:

| Variant | Fields | Description |
|---------|--------|-------------|
| Init | teamId: int | Game initialization, our team ID |
| Update | frame: int | Per-frame tick |
| UnitCreated | unitId: int, builderId: int | New unit under construction |
| UnitFinished | unitId: int | Unit construction complete |
| UnitIdle | unitId: int | Unit has no orders |
| UnitDamaged | unitId, attackerId option, damage, weaponDefId, isParalyzer | Unit took damage |
| UnitDestroyed | unitId, attackerId option | Unit destroyed |
| EnemyEnterLOS | enemyId: int | Enemy unit visible |
| EnemyDestroyed | enemyId, attackerId option | Enemy unit destroyed |
| Shutdown | reason: ShutdownReason | Game ending |
| *(+ 18 more)* | | See events.proto for full list |

### HandshakeInfo (Record)

Information received during the proxy handshake.

| Field | Type | Description |
|-------|------|-------------|
| ProtocolVersion | uint32 | Protocol version (expected: 1) |
| EngineVersion | string | Recoil engine version string |
| GameId | string | Game instance identifier |
| MapName | string | Active map name |
| ModName | string | Game mod name |
| TeamId | int | Our team ID |
| AllyTeamId | int | Our ally team ID |
| PlayerCount | int | Number of players |

### BarClient (Stateful Object)

The top-level orchestrator. Not a record — a class with mutable state tracking `SessionState`.

| Member | Signature | Description |
|--------|-----------|-------------|
| State | SessionState | Current lifecycle state |
| Config | EngineConfig | Configuration used for this session |
| Handshake | HandshakeInfo option | Handshake data (after connection) |
| Start | unit -> unit | Launch engine, connect, handshake |
| Step | unit -> GameFrame | Receive and return one frame (send empty response) |
| StepWith | (GameFrame -> AICommand list) -> GameFrame | Receive frame, apply handler, send commands |
| Run | int -> (GameFrame -> AICommand list) -> GameFrame list | Run N frames with handler |
| RunUntil | (GameFrame -> bool) -> (GameFrame -> AICommand list) -> GameFrame list | Run until predicate |
| SendCallback | CallbackId -> CallbackParam list -> CallbackResponse | Invoke engine callback |
| Reset | unit -> unit | Reset game state via cheat commands |
| Stop | unit -> unit | Graceful shutdown |

## Protocol Wire Format

### Framing

All messages use length-prefixed framing:
```
[4 bytes, little-endian uint32: payload length][N bytes: protobuf payload]
```

### Message Flow

```
Proxy → AI:  ProxyMessage { Handshake }
AI → Proxy:  AIMessage { HandshakeResponse }

[frame loop]
Proxy → AI:  ProxyMessage { Frame { frame_number, events[] } }
AI → Proxy:  AIMessage { FrameResponse { commands[] } }
  [optional during frame processing]
  AI → Proxy:  AIMessage { CallbackRequest { callback_id, params[] } }
  Proxy → AI:  ProxyMessage { CallbackResponse { result } }

[shutdown]
Proxy → AI:  ProxyMessage { Shutdown { reason } }
```

## External Entity: BarData (NuGet)

Consumed from HighBarV2 as a NuGet package. Key types used by FSBar.Client:

| Type | Description |
|------|-------------|
| `BarData.AllUnits.all` | `UnitSummary list` — all 953 unit definitions |
| `BarData.UnitSummary` | Record: name, metalCost, energyCost, health, isBuilder, isArmed, isMobile, etc. |
| `BarData.WeaponDef` | Weapon stats: damage, range, reloadTime, areaOfEffect |
| `BarData.ValueOrExpr<'T>` | Wrapper for concrete values vs runtime Lua expressions |
