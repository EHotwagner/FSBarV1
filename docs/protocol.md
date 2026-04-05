# Protocol

FSBar.Client communicates with the BAR game engine through the HighBarV2 C proxy over a Unix domain socket. Messages are protobuf-encoded with a custom length-prefix framing.

## Wire Format

Every message is framed as:

```
┌──────────────────┬──────────────────────────┐
│ 4 bytes (LE u32) │ N bytes (protobuf)       │
│ payload length   │ serialized message       │
└──────────────────┴──────────────────────────┘
```

- Length is 4-byte **little-endian** unsigned integer
- Payload is a protobuf-serialized `ProxyMessage` or `AIMessage`

## Message Flow

### Connection Setup

```
1. Client creates Unix domain socket listener at /tmp/fsbar-<guid>.sock
2. Client launches engine process (spring-headless or graphical spring)
3. Engine loads HighBarV2 proxy plugin
4. Proxy connects to client's socket
5. Client accepts the connection
```

### Handshake

```
Proxy ──► ProxyMessage.Handshake ──► Client
          { ProtocolVersion: 1
            EngineVersion: "..."
            GameId: "..."
            MapName: "..."
            ModName: "..."
            TeamId: 0
            AllyTeamId: 0
            PlayerCount: 1 }

Client ──► AIMessage.HandshakeResponse ──► Proxy
           { Accepted: true
             ProtocolVersion: 1 }
```

The client validates `ProtocolVersion == 1` and rejects otherwise.

### Frame Loop

Each game tick follows this exchange:

```
Proxy ──► ProxyMessage.Frame ──► Client
          { FrameNumber: N
            Events: [ ... ]
            TeamId: 0 }

     ┌─── [Optional: Callback queries] ───┐
     │                                      │
     │  Client ──► AIMessage.CallbackRequest ──► Proxy
     │             { RequestId, CallbackId, Params }
     │                                      │
     │  Proxy ──► ProxyMessage.CallbackResponse ──► Client
     │            { RequestId, Success, Result }
     │                                      │
     └──────────────────────────────────────┘

Client ──► AIMessage.FrameResponse ──► Proxy
           { Commands: [ ... ]
             TeamId: 0 }
```

Callbacks are optional and can be issued multiple times between receiving a frame and sending the response.

### Save/Load

The engine may request state persistence:

```
Proxy ──► ProxyMessage.SaveRequest ──► Client
Client ──► AIMessage.SaveResponse { StateData: bytes } ──► Proxy
```

The client currently responds with empty state data. The protocol module handles this transparently and continues to the next frame.

### Shutdown

```
Proxy ──► ProxyMessage.Shutdown ──► Client
          { Reason: GameOver | Disconnect | Error }
```

`Protocol.receiveFrame` returns `None` on shutdown, signaling the frame loop to exit.

## Message Types

### ProxyMessage (Proxy to Client)

| Variant | Fields | When |
|---------|--------|------|
| `Handshake` | ProtocolVersion, EngineVersion, GameId, MapName, ModName, TeamId, AllyTeamId, PlayerCount | Once at connection |
| `Frame` | FrameNumber, Events[], TeamId | Every game tick |
| `CallbackResponse` | RequestId, Success, Result, ErrorMessage | After CallbackRequest |
| `SaveRequest` | — | Engine checkpoint |
| `LoadRequest` | StateData | Engine restore |
| `Shutdown` | Reason | Game end |

### AIMessage (Client to Proxy)

| Variant | Fields | When |
|---------|--------|------|
| `HandshakeResponse` | Accepted, ProtocolVersion | Once after Handshake |
| `FrameResponse` | Commands[], TeamId | Every game tick |
| `CallbackRequest` | RequestId, CallbackId, Params[] | Mid-frame queries |
| `SaveResponse` | StateData | After SaveRequest |

## Callback IDs

| Range | Category | Examples |
|-------|----------|----------|
| 10-14 | Game | GetMyTeam, GetMyAllyTeam |
| 20-31 | Unit | GetPos, GetHealth, GetMaxHealth, GetDef |
| 40-46 | UnitDef | GetName, GetBuildOptions, GetMaxWeaponRange, GetBuildSpeed, GetCost |
| 50-58 | Map | GetWidth, GetHeight, GetStartPos, GetMetalSpots |
| 61-64 | Economy | GetCurrent, GetIncome, GetUsage, GetStorage |
| 70+ | Misc | GetUnitDefs, team/mod/info queries |

## Error Handling

| Situation | Behavior |
|-----------|----------|
| Protocol version mismatch | `failwith` with version details |
| Unexpected message type during handshake | `failwith "Expected Handshake"` |
| Unexpected message type during callback | `failwith "Expected CallbackResponse"` |
| Connection closed mid-read | `failwith "Connection closed"` |
| Invalid message length (<= 0) | `failwith "Invalid message length"` |
| Connection timeout | `failwith` with timeout details |
