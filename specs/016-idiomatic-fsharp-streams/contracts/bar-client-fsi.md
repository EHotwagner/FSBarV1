# Contract: BarClient.fsi (Updated Public API)

**Branch**: `016-idiomatic-fsharp-streams` | **Date**: 2026-04-09

## New BarClient.fsi Signature

```fsharp
namespace FSBar.Client

/// Session lifecycle states
[<RequireQualifiedAccess>]
type SessionState =
    | Idle
    | Starting
    | Connected
    | Running
    | Stopped
    | Error of string

/// Manages an engine session with stream-based frame iteration
type BarClient =
    /// Create a new client with the given configuration
    new: config: EngineConfig -> BarClient

    /// Current session state
    member State: SessionState

    /// Engine configuration
    member Config: EngineConfig

    /// Handshake info (None if not connected)
    member Handshake: Protocol.HandshakeInfo option

    /// Active network stream for callback queries
    member Stream: System.Net.Sockets.NetworkStream

    /// Game frames as a lazy sequence. Iterating pulls frames from
    /// the engine. Terminates when engine disconnects.
    member Frames: seq<Protocol.GameFrame>

    /// Queue commands to send with the next frame response.
    /// Raises if session is not in Connected or Running state.
    member SendCommands: commands: Highbar.AICommand list -> unit

    /// Launch engine, connect to proxy, perform handshake
    member Start: unit -> unit

    /// Reset in-game state via cheat commands
    member Reset: unit -> unit

    /// Stop session and clean up resources
    member Stop: unit -> unit

    interface System.IDisposable

/// Module-level convenience functions
module BarClient =
    /// Default engine configuration
    val defaultConfig: unit -> EngineConfig

    /// Create a client in Idle state
    val create: config: EngineConfig -> BarClient

    /// Create, configure for headless, start, and return
    val startHeadless: unit -> BarClient

    /// Create, configure for graphical, start, and return
    val startGraphical: unit -> BarClient
```

## Changes from Current Signature

### Removed
- `Step: unit -> GameFrame` — replaced by iterating `Frames`
- `StepWith: handler: (GameFrame -> Highbar.AICommand list) -> GameFrame` — replaced by `Frames` + `SendCommands`
- `Run: frameCount: int * handler: (GameFrame -> Highbar.AICommand list) -> GameFrame list` — replaced by `Frames |> Seq.take n`
- `RunUntil: predicate: (GameFrame -> bool) * handler: (GameFrame -> Highbar.AICommand list) -> GameFrame list` — replaced by `Frames |> Seq.takeWhile`

### Added
- `Frames: seq<GameFrame>` — lazy sequence of game frames
- `SendCommands: commands: Highbar.AICommand list -> unit` — command submission
