namespace FSBar.SyntheticData

open FSBar.Client

type SceneId =
    | SceneA
    | SceneB
    | SceneC

type Scene = {
    Id: SceneId
    Name: string
    MapWidth: float32
    MapHeight: float32
    Frames: GameState array
    GameFrames: GameFrame array
    UnitDefs: UnitDefCache
}
