namespace FSBar.SyntheticData

open FSBar.Client

/// Identifies one of the three pre-defined synthetic scenes.
type SceneId =
    | SceneA
    | SceneB
    | SceneC

/// A complete synthetic scene: 300 frames of game state with metadata.
type Scene = {
    Id: SceneId
    Name: string
    MapWidth: float32
    MapHeight: float32
    Frames: GameState array
    GameFrames: GameFrame array
    UnitDefs: UnitDefCache
}
