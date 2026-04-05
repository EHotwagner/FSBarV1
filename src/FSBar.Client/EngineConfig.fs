namespace FSBar.Client

open System

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
    let defaultConfig () =
        let guid = Guid.NewGuid().ToString("N").[..7]
        {
            Mode = Headless
            SocketPath = $"/tmp/fsbar-{guid}.sock"
            MapName = "Red Rock Desert v2"
            GameType = "Beyond All Reason test-29840-d9b7dba"
            OpponentAI = "NullAI"
            OpponentSide = "Cortex"
            OurSide = "Armada"
            TimeoutMs = 30000
            EngineBin = "spring-headless"
            AppImagePath = "/home/developer/applications/Beyond-All-Reason-1.2988.0.AppImage"
            SpringDataDir = None
            GameSpeed = 100
        }
