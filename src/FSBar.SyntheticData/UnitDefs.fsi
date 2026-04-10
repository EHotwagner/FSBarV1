namespace FSBar.SyntheticData

open FSBar.Client

/// Pre-built unit definition caches for each scene.
module UnitDefs =
    // DefId constants
    [<Literal>]
    val ArmCommander: int = 1
    [<Literal>]
    val ArmMex: int = 2
    [<Literal>]
    val ArmSolar: int = 3
    [<Literal>]
    val ArmWind: int = 4
    [<Literal>]
    val ArmLab: int = 5
    [<Literal>]
    val ArmPeewee: int = 6
    [<Literal>]
    val ArmFlash: int = 7
    [<Literal>]
    val ArmRockko: int = 8
    [<Literal>]
    val ArmSamson: int = 9
    [<Literal>]
    val ArmFark: int = 10
    [<Literal>]
    val CorCommander: int = 11
    [<Literal>]
    val CorGator: int = 12
    [<Literal>]
    val CorThud: int = 13
    [<Literal>]
    val CorStorm: int = 14
    [<Literal>]
    val CorLab: int = 15
    [<Literal>]
    val ArmAdvLab: int = 16
    [<Literal>]
    val ArmZeus: int = 17
    [<Literal>]
    val ArmAnni: int = 18
    [<Literal>]
    val CorAdvLab: int = 19
    [<Literal>]
    val CorSumo: int = 20
    [<Literal>]
    val CorGoliath: int = 21
    [<Literal>]
    val ArmStorage: int = 22
    [<Literal>]
    val CorStorage: int = 23

    /// Unit definitions for Scene A (early game: commander, economy, basic combat).
    val sceneA: UnitDefCache

    /// Unit definitions for Scene B (mid game: broader combat roster).
    val sceneB: UnitDefCache

    /// Unit definitions for Scene C (late game: diverse heavy units).
    val sceneC: UnitDefCache

    /// Max health for a given DefId. Returns 100 if unknown.
    val maxHealthFor: defId: int -> cache: UnitDefCache -> float32

    /// Movement speed (elmos/frame) for a given DefId. Returns 2.0 if unknown.
    val speedFor: defId: int -> cache: UnitDefCache -> float32
