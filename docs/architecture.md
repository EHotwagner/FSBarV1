# Architecture

## Overview

FSBarV1 is a client library that enables F# programs to control units in a running Beyond All Reason game. It communicates with the game engine through a C proxy plugin (HighBarV2) over a Unix domain socket using length-prefixed protobuf messages.

```
┌─────────────┐     Unix Socket      ┌─────────────┐     Engine API     ┌─────────────┐
│  F# Client  │◄────────────────────►│  HighBarV2   │◄──────────────────►│   Recoil     │
│ (FSBar.Client)│   protobuf frames   │  C Proxy     │   native callbacks │   Engine     │
└─────────────┘                      └─────────────┘                    └─────────────┘
```

## Components

### FSBar.Proto

Generated F# protobuf bindings from `.proto` definitions via FsGrpc. Contains all message types for the proxy protocol:

- `ProxyMessage` / `AIMessage` — top-level message envelopes
- `EngineEvent` — 28 event variants (Init, UnitCreated, UnitDamaged, etc.)
- `AICommand` — 97 command variants (Move, Build, Attack, etc.)
- `CallbackRequest` / `CallbackResponse` — mid-frame query protocol
- `Handshake` / `HandshakeResponse` — connection setup

These types are auto-generated and should not be edited manually.

### FSBar.Client

The main library, organized as 9 modules each with an `.fsi` signature file:

| Module | Responsibility |
|--------|---------------|
| `EngineConfig` | Configuration record for engine mode, paths, timeouts, game settings |
| `Connection` | Unix domain socket lifecycle and length-prefixed message framing |
| `Protocol` | Protobuf handshake, frame receive/send, callback request/response |
| `Events` | Deserialize `EngineEvent` protobuf to `GameEvent` discriminated union |
| `Commands` | Builder functions for all AI command types |
| `Callbacks` | Typed wrappers for engine state queries (position, health, economy) |
| `ScriptGenerator` | Generate Spring engine start scripts from `EngineConfig` |
| `EngineLauncher` | Launch/stop headless or graphical engine processes |
| `BarClient` | Session state machine orchestrating the full lifecycle |

### FSBar.Client.Tests

Unit tests (84 xUnit facts) covering all modules with in-process socket pairs — no engine required.

### FSBar.LiveTests

Integration tests (17 scenarios) running against a live headless BAR engine instance. Uses xUnit `IAsyncLifetime` and `ICollectionFixture` for shared engine lifecycle.

## Data Flow

### Session Lifecycle

```
Idle ──► Starting ──► Connected ──► Running ──► Stopped
              │                          │
              └──── Error ◄──────────────┘
```

1. **Idle**: Client created with config
2. **Starting**: Listening socket created, engine process launched
3. **Connected**: Proxy connected, handshake completed
4. **Running**: Processing frames (Step/StepWith/Run)
5. **Stopped**: Engine shut down, resources cleaned up

### Frame Processing

```
Engine sends Frame ──► Client receives ──► [Optional callbacks] ──► Client sends FrameResponse
                             │                                              │
                             ▼                                              ▼
                      Parse GameEvents                              Send AICommands
```

Each game tick:
1. The proxy sends a `Frame` message containing the frame number and a list of events
2. The client deserializes events into `GameEvent` discriminated union cases
3. Optionally, the client can issue `CallbackRequest` messages to query game state (unit positions, health, economy) before responding
4. The client sends a `FrameResponse` containing a list of `AICommand` messages

### Callback Protocol

Callbacks are synchronous queries issued between receiving a frame and sending the response:

```
Client sends CallbackRequest ──► Proxy queries engine ──► Proxy sends CallbackResponse
```

Each callback has a unique `CallbackId` enum value. Parameters and results are typed via `CallbackParam` / `CallbackResult` oneofs.

## Design Decisions

### Unix Domain Sockets over TCP

The proxy communicates via Unix domain sockets for minimal latency on the same machine. The socket path is configurable and auto-cleaned on shutdown.

### Length-Prefixed Framing

Messages use a 4-byte little-endian length prefix rather than protobuf delimiters, matching the HighBarV2 proxy's wire format.

### Shared Engine Fixture for Tests

Integration tests share a single engine instance via xUnit's `ICollectionFixture` pattern. This avoids the ~10-second engine startup cost per test while ensuring test isolation through the frame-based protocol.

### Signature Files (.fsi) for Every Module

The project constitution mandates `.fsi` files for all public modules. The compiler enforces that implementations conform to their signatures, preventing accidental API drift.

## Dependencies

| Dependency | Version | Purpose |
|-----------|---------|---------|
| FsGrpc | 1.0.6 | Protobuf serialization for F# |
| BarData | latest | BAR unit definitions library |
| Google.Protobuf | 3.28.x | Protobuf runtime (transitive) |
| xUnit | 2.9.x | Test framework |
