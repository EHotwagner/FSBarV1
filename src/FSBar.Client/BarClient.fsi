namespace FSBar.Client

type SessionState =
    | Idle
    | Starting
    | Connected
    | Running
    | Stopped
    | Error of string

type BarClient =
    new: config: EngineConfig -> BarClient
    member State: SessionState
    member Config: EngineConfig
    member Handshake: HandshakeInfo option
    member Stream: System.Net.Sockets.NetworkStream
    member Start: unit -> unit
    member Step: unit -> GameFrame
    member StepWith: handler: (GameFrame -> Highbar.AICommand list) -> GameFrame
    member Run: frameCount: int * handler: (GameFrame -> Highbar.AICommand list) -> GameFrame list
    member RunUntil: predicate: (GameFrame -> bool) * handler: (GameFrame -> Highbar.AICommand list) -> GameFrame list
    member Reset: unit -> unit
    member Stop: unit -> unit
    interface System.IDisposable

module BarClient =
    val defaultConfig: unit -> EngineConfig
    val create: config: EngineConfig -> BarClient
    val startHeadless: unit -> BarClient
    val startGraphical: unit -> BarClient
