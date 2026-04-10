# Public API Contract: FSBar.SyntheticData

## Module: SyntheticData.Scenes

### Types

```
type SceneId = SceneA | SceneB | SceneC

type Scene = {
    Id: SceneId
    Name: string
    MapWidth: float32
    MapHeight: float32
    Frames: GameState array
    GameFrames: GameFrame array
    UnitDefs: UnitDefCache
}
```

### Functions

```
/// Generate a specific scene by ID. Returns 300 frames of synthetic data.
val generate: SceneId -> Scene

/// Generate all three scenes.
val generateAll: unit -> Scene list
```

## Module: SyntheticData.Validation

### Functions

```
/// Validate a scene for internal consistency. Returns a list of validation errors (empty = valid).
val validate: Scene -> string list
```

## Invariants

- `generate` is pure and deterministic (same SceneId always produces identical output)
- `Scene.Frames` always has exactly 300 elements
- `Scene.GameFrames` always has exactly 300 elements
- `Scene.Frames.[i].FrameNumber = uint32 (i + 1)` for all i
- `Scene.GameFrames.[i].FrameNumber = uint32 (i + 1)` for all i
- All TrackedUnit/TrackedEnemy DefIds exist in Scene.UnitDefs
