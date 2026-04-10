namespace FSBar.SyntheticData

/// Scene generation functions.
module Scenes =
    /// Generate a specific scene by ID. Returns 300 frames of synthetic data.
    val generate: sceneId: SceneId -> Scene

    /// Generate all three scenes.
    val generateAll: unit -> Scene list
