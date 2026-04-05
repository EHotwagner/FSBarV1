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
}

module EngineConfig =
    val defaultConfig: unit -> EngineConfig
