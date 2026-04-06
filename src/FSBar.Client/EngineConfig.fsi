namespace FSBar.Client

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
    ReadTimeoutMs: int option
}

module EngineConfig =
    val defaultConfig: unit -> EngineConfig
    val resolveReadTimeout: config: EngineConfig -> int
