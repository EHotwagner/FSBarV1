(**
---
title: Architecture Overview
category: Design
categoryindex: 4
index: 1
---
*)

(**
# Architecture Overview

FSBarV1 is an F# client library that communicates with the Beyond All Reason (BAR) real-time strategy
game engine via the HighBar V2 proxy. This page describes the system design, component responsibilities,
and key architectural decisions.

## System Summary

```
+----------------+       Unix Domain Socket       +------------------+       Engine API       +-------------+
|  F# AI Client  | <--(protobuf over socket)-->   | HighBar V2 Proxy | <--(native C++ API)--> | BAR Engine  |
|  (FSBar.Client)|                                 |  (in-engine AI)  |                        | (Spring RTS)|
+----------------+                                 +------------------+                        +-------------+
```

The F# client never talks to the engine directly. All communication flows through the HighBar V2 proxy,
which runs as an AI module inside the engine process. The proxy translates between its protobuf-based
socket protocol and the engine's native C++ AI callback interface.

## Communication Flow

1. **Launch**: `EngineLauncher` starts the engine with a generated game script
2. **Connect**: `Connection` creates a Unix domain socket listener; the proxy connects to it
3. **Handshake**: `Protocol` exchanges version info and team assignment
4. **Frame Loop**: Each frame: receive events, process them, send commands back
5. **Callbacks**: Mid-frame queries (unit positions, map data) via request/response on the same socket
6. **Shutdown**: Engine sends a shutdown message; client cleans up socket and process

## Component Diagram

```
FSBar.Client Assembly
+------------------------------------------------------------------+
|                                                                  |
|  EngineConfig          ScriptGenerator        EngineLauncher     |
|  (session params)      (game script text)     (process mgmt)     |
|       |                      |                      |            |
|       v                      v                      v            |
|  +----------------------------------------------------------+   |
|  |                      BarClient                            |   |
|  |  (orchestrates lifecycle: start, step, run, stop)         |   |
|  +----------------------------------------------------------+   |
|       |              |               |              |            |
|       v              v               v              v            |
|  Connection      Protocol        Commands        Events          |
|  (socket I/O)    (wire format)   (cmd builders)  (event parse)   |
|       |              |                                           |
|       v              v                                           |
|  Callbacks       MapGrid          MapQuery       MapCache        |
|  (engine queries)(terrain layers) (point queries)(caching)       |
|                                                                  |
+------------------------------------------------------------------+
```

## Module Responsibilities

### EngineConfig

Defines the `EngineConfig` record and `EngineMode` discriminated union. All session parameters live here:
socket path, map name, game type, factions, timeouts, engine binary path, and game speed.
`defaultConfig()` generates a config with sensible defaults and a unique socket path.

### ScriptGenerator

Produces the Spring engine game script (`[GAME]` format) from an `EngineConfig`. This script configures
two teams, two ally-teams, a human player (spectating), our HighBar V2 AI, and the opponent AI.

### EngineLauncher

Starts the engine process (`spring-headless` or AppImage), manages session directories for logs
and scripts, and handles process teardown. Each session gets a directory under `/tmp/`.

### Connection

Low-level socket operations: create a Unix domain socket listener, accept connections with timeout,
send/receive length-prefixed binary frames, and clean up socket files.

### Protocol

Implements the wire protocol on top of `Connection`:
- **Handshake**: exchange `ProxyMessage.Handshake` / `AIMessage.HandshakeResponse`
- **receiveFrame**: deserialize `ProxyMessage.Frame` into `GameFrame`, handle `SaveRequest` transparently
- **sendFrameResponse**: serialize commands as `AIMessage.FrameResponse`
- **sendCallback**: send a callback request and block for the response

### Commands

17 pure functions that construct `Highbar.AICommand` protobuf messages. Each function takes typed
parameters (unit IDs, positions, etc.) and returns a ready-to-send command. All movement/action
commands set the internal order flag (options = 8).

### Events

Defines the `GameEvent` discriminated union (28 cases) and the `fromProto` function that converts
`Highbar.EngineEvent` protobuf messages to F# DU values. Also defines the `GameFrame` record
(frame number + event list).

### Callbacks

26 functions for mid-frame engine queries. Each takes a `NetworkStream` and optional parameters,
sends a callback request via `Protocol.sendCallback`, and returns typed results. Categories include
team info, map info, unit info, economy, and raw map data.

### MapGrid

Loads all map layers (heightmap, slope, resource, LOS, radar) into a `MapGrid` record of `Array2D`
grids. Provides terrain classification (`Land`, `Water`, `Cliff`) and passability computation for
four movement types (`Kbot`, `Tank`, `Hover`, `Ship`). Includes active patterns for pattern matching.

### MapQuery

Point queries and region operations on `MapGrid` data. Converts between elmo coordinates (game world)
and heightmap grid indices. Supports height/slope lookups, rectangular sub-region extraction, and
resource hotspot detection.

### MapCache

Session-level caching using `ConcurrentDictionary`. Caches the `MapGrid` (loaded once per session)
and passability grids (computed once per movement type). `clear()` resets for new sessions.

### BarClient

The main orchestrator. Manages the full session lifecycle:
- Creates socket listener and launches engine
- Accepts connection and performs handshake
- Provides `Step`, `StepWith`, `Run`, `RunUntil` for frame processing
- Handles cleanup on `Stop` or `Dispose`
- Tracks session state: `Idle -> Starting -> Connected -> Running -> Stopped`

## Design Decisions

### Why .fsi Signature Files?

Every module has a corresponding `.fsi` file that defines its public API surface. This provides:
- **Explicit contracts**: The public API is declared, not inferred
- **Surface area testing**: The `SurfaceAreaTests` compare `.fsi` files against baselines to detect accidental API changes
- **Documentation**: Signature files serve as concise API reference

### Why Protobuf (FsGrpc)?

The HighBar V2 proxy uses Protocol Buffers for serialization. FsGrpc 1.0.6 generates idiomatic F#
types from `.proto` files, giving us type-safe message construction and pattern matching on DU-style
message cases.

### Why Unix Domain Sockets?

Unix domain sockets provide low-latency, zero-copy IPC between the F# client and the in-engine proxy.
They avoid TCP overhead and work naturally with the single-machine architecture. The socket path is
generated per session to allow concurrent test runs.

### External Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| FsGrpc | 1.0.6 | Protobuf code generation and runtime |
| FsGrpc.Tools | 1.0.6 | Build-time protobuf compilation |
| BarData | (NuGet) | BAR unit definitions and game constants |
| xUnit | 2.9.x | Test framework |
*)
