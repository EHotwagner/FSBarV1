(**
---
title: Protocol Details
category: Design
categoryindex: 4
index: 3
---
*)

(**
# Protocol Details

This page documents the wire protocol between the FSBarV1 client and the HighBar V2 proxy.
All communication uses Unix domain sockets with length-prefixed protobuf messages.

## Wire Format

Every message on the socket follows this format:

```
+------------------+---------------------------+
| 4 bytes (LE)    | N bytes                    |
| payload length   | protobuf-encoded payload  |
+------------------+---------------------------+
```

- **Length header**: 4-byte little-endian `int32` giving the size of the payload in bytes
- **Payload**: A protobuf-serialized `ProxyMessage` (from proxy) or `AIMessage` (from client)

The `Connection` module provides the low-level primitives:
*)

(*** do-not-eval ***)
open FSBar.Client

// Send a length-prefixed message
Connection.sendMessage stream data

// Receive a length-prefixed message (blocks until complete)
let payload = Connection.recvBytes stream

(**
## Message Types

### Proxy -> AI (ProxyMessage)

| Message Case | Purpose |
|-------------|---------|
| `Handshake` | Initial connection setup with game metadata |
| `Frame` | One game frame with events to process |
| `SaveRequest` | Engine wants to save game state |
| `Shutdown` | Engine is shutting down (game over, crash, etc.) |

### AI -> Proxy (AIMessage)

| Message Case | Purpose |
|-------------|---------|
| `HandshakeResponse` | Accept/reject the connection |
| `FrameResponse` | Commands to execute this frame |
| `CallbackRequest` | Mid-frame query to the engine |
| `SaveResponse` | Response to save request (empty state) |

### Callback Round-Trip

| Message Case | Direction | Purpose |
|-------------|-----------|---------|
| `CallbackRequest` | AI -> Proxy | Query engine state |
| `CallbackResponse` | Proxy -> AI | Engine state response |

## Handshake Sequence

The handshake occurs immediately after the socket connection is established.

```
Proxy                          AI Client
  |                               |
  |-- Handshake ----------------->|
  |   (protocol version,         |
  |    engine version,            |
  |    game ID, map, mod,         |
  |    team ID, ally team,        |
  |    player count)              |
  |                               |
  |<--------- HandshakeResponse --|
  |   (accepted=true,            |
  |    protocol version)          |
  |                               |
```

The `HandshakeInfo` record captures the metadata:
*)

(*** do-not-eval ***)
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

(**
Usage:
*)

(*** do-not-eval ***)
let info = Protocol.handshake stream
printfn "Protocol v%d, engine %s" info.ProtocolVersion info.EngineVersion
printfn "Map: %s, Mod: %s" info.MapName info.ModName
printfn "Team %d (ally team %d), %d players" info.TeamId info.AllyTeamId info.PlayerCount

(**
## Frame Loop

After the handshake, the game enters a frame loop. Each iteration:

```
Proxy                          AI Client
  |                               |
  |-- Frame --------------------->|
  |   (frame number,             |
  |    events[],                  |
  |    team ID)                   |
  |                               |
  |   [optional callback round-trips]
  |<-- CallbackRequest -----------|
  |-- CallbackResponse ---------->|
  |   ...                         |
  |                               |
  |<--------- FrameResponse ------|
  |   (commands[])                |
  |                               |
```

The client must send exactly one `FrameResponse` for each `Frame` received.
Between receiving a `Frame` and sending the `FrameResponse`, the client may issue
any number of callback requests.
*)

(*** do-not-eval ***)
// Low-level frame loop
let rec loop () =
    match Protocol.receiveFrame stream with
    | None ->
        printfn "Game ended"
    | Some frame ->
        // Process events
        for evt in frame.Events do
            match evt with
            | GameEvent.UnitIdle uid -> printfn "Unit %d idle" uid
            | _ -> ()

        // Issue callbacks if needed
        let (ux, _, uz) = Callbacks.getUnitPos stream 1

        // Send response with commands
        Protocol.sendFrameResponse stream
            [ Commands.MoveCommand 1 2048.0f 100.0f 2048.0f ]

        loop ()

(**
## Callback Mechanism

Callbacks are synchronous request/response pairs that happen mid-frame. Each callback has a
numeric ID and optional parameters:
*)

(*** do-not-eval ***)
// Raw callback API (used internally by Callbacks module)
let response = Protocol.sendCallback stream callbackId paramList

(**
The `Callbacks` module wraps this with typed functions. For example, `getUnitPos` sends
callback ID for unit position with the unit ID as a parameter, then extracts the x/y/z
floats from the response.

```
AI Client                      Proxy                      Engine
  |                               |                          |
  |-- CallbackRequest ----------->|                          |
  |   (callbackId, params)        |-- native API call ------>|
  |                               |<-- result ---------------|
  |<-- CallbackResponse ---------|                          |
  |   (return values)             |                          |
```

## Save Request Handling

The proxy may send a `SaveRequest` at any time during the frame loop. The `Protocol.receiveFrame`
function handles this transparently:

1. Receives a `SaveRequest`
2. Immediately responds with an empty `SaveResponse`
3. Continues reading the next message (which should be the actual `Frame`)

This means the caller never sees `SaveRequest` messages -- they are handled internally.
*)

(*** do-not-eval ***)
// This works even if the proxy sends SaveRequests between frames:
let frame = Protocol.receiveFrame stream
// SaveRequests are handled automatically -- frame is always a Frame or None (shutdown)

(**
## Connection Lifecycle

### Socket Creation

The client creates a Unix domain socket listener at a unique path:
*)

(*** do-not-eval ***)
let listener = Connection.createListener "/tmp/fsbar-abc123.sock"

(**
`createListener` removes any stale socket file before binding, so leftover sockets from
crashed sessions do not cause errors.

### Connection Accept

After launching the engine, the client waits for the proxy to connect:
*)

(*** do-not-eval ***)
let (clientSocket, networkStream) = Connection.acceptConnection listener 30000 10000
// 30000ms accept timeout, 10000ms read timeout

(**
### Cleanup

On shutdown, clean up the socket file and close connections:
*)

(*** do-not-eval ***)
Connection.cleanup "/tmp/fsbar-abc123.sock" (Some clientSocket)

(**
The `BarClient` handles all of this automatically in its `Start`/`Stop` lifecycle.

## Error Handling

### EngineDisconnectedException

If the engine process crashes or the proxy closes the socket, read operations throw
`EngineDisconnectedException`. This exception includes the last successfully processed
frame number for diagnostics:
*)

(*** do-not-eval ***)
try
    let frame = Protocol.receiveFrame stream
    ()
with
| :? EngineDisconnectedException as ex ->
    printfn "Engine disconnected at frame %A: %s" ex.LastFrameNumber ex.Message
