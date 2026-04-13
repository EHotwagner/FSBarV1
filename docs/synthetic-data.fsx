(**
---
title: Synthetic Data
category: How-To
categoryindex: 3
index: 7
description: Deterministic synthetic scenes for offline visualization and tests.
---
*)

(**
# Synthetic Data (`FSBar.SyntheticData`)

`FSBar.SyntheticData` produces fully-formed `GameState` / `GameFrame` sequences **without
launching the BAR engine**. It is a pure-functional library: given a `SceneId`, it
deterministically generates 300 frames of simulated units, enemies, and economy that match
the real `FSBar.Client` types one-for-one.

This library exists for three reasons:

1. **Visualization tests.** `FSBar.Viz` tests need realistic `GameSnapshot` inputs that do
   not depend on a running engine.
2. **Doc examples.** The docs can show renderer behavior by feeding a generated scene into
   `PreviewSession.startPlayback`.
3. **Offline experimentation.** You can iterate on scene builders, color maps, or scene
   graphs against known data before wiring them up to a live client.

Everything is deterministic — running the same `SceneId` always yields identical output.

## Available Scenes

Three pre-built scenes are shipped, each 300 frames long.
*)

(*** do-not-eval ***)
open FSBar.Client
open FSBar.SyntheticData

type SceneId =
    | SceneA   // sparse — a handful of units moving around
    | SceneB   // medium — builder + factory + moving enemies
    | SceneC   // dense — multi-team unit movement + combat events

(**
## Generating a Scene

`Scenes.generate` returns a fully-populated `Scene` containing the per-frame `GameState`,
the matching `GameFrame` list (for anyone who wants to replay events), the map bounds, and
a `UnitDefCache` for unit metadata lookups.
*)

(*** do-not-eval ***)
let scene = Scenes.generate SceneA

printfn "Scene: %s" scene.Name
printfn "Map: %.0f x %.0f elmos" scene.MapWidth scene.MapHeight
printfn "Frames: %d" scene.Frames.Length

// Each frame is a GameState — use it anywhere client.GameState would go
let firstFrame = scene.Frames.[0]
let lastFrame = scene.Frames.[scene.Frames.Length - 1]
printfn "Frame 0 units:   %d" firstFrame.Units.Count
printfn "Frame 299 units: %d" lastFrame.Units.Count

(**
Generate all three scenes at once for batch validation:
*)

(*** do-not-eval ***)
let allScenes = Scenes.generateAll ()
for s in allScenes do
    printfn "%s: %d frames, %d unit defs" s.Name s.Frames.Length s.UnitDefs.Count

(**
## Scene Structure
*)

(*** do-not-eval ***)
type Scene = {
    Id: SceneId
    Name: string
    MapWidth: float32
    MapHeight: float32
    Frames: GameState array        // 300 entries, one per simulation tick
    GameFrames: GameFrame array    // matching raw frames with events
    UnitDefs: UnitDefCache         // all unit defs referenced in the scene
}

(**
## Validating a Scene

`Validation.validate` checks structural invariants (unit IDs stable across frames, unit
positions in-bounds, economy values non-negative, etc.). `Validation.validateContinuity`
verifies frame-to-frame deltas make sense (units do not teleport, health deltas match
damage events).

Both return an empty list if the scene is valid; otherwise each entry is a human-readable
error message.
*)

(*** do-not-eval ***)
let scene = Scenes.generate SceneC

match Validation.validate scene with
| [] -> printfn "Structural check: OK"
| errors ->
    printfn "Structural errors:"
    for e in errors do printfn "  %s" e

match Validation.validateContinuity scene with
| [] -> printfn "Continuity check: OK"
| errors ->
    printfn "Continuity errors:"
    for e in errors do printfn "  %s" e

(**
## Per-Module Building Blocks

The three scenes above are built from smaller, reusable primitives — you can compose your
own scenes from these if the pre-built ones do not cover what you need.

### `UnitSim` — Per-unit movement

`UnitSim.create` wraps a `TrackedUnit` with a speed and a target. `UnitSim.step` advances
one frame: the unit moves toward its target and picks a new random target when it arrives.
All randomness is derived from the seed you pass in, so the simulation is reproducible.
*)

(*** do-not-eval ***)
let startingUnit : TrackedUnit = {
    UnitId = 1
    DefId = UnitDefs.ArmCommander
    Position = (1000.0f, 100.0f, 1000.0f)
    Health = 3000.0f
    MaxHealth = 3000.0f
    IsFinished = true
    IsIdle = false
}

let mutable moving = UnitSim.create startingUnit 5.0f 4096.0f 4096.0f seed=42

for frame in 1..100 do
    moving <- UnitSim.step moving 4096.0f 4096.0f frame

printfn "After 100 frames: %A" moving.Unit.Position

(**
### `EnemySim` — LOS/radar state machine

`EnemySim` drives a `TrackedEnemy` through LOS-enter / radar-enter / leave transitions,
matching the event patterns a real engine would emit.

### `EconomySim` — Resource integration

`EconomySim.step` advances an `EconomySnapshot` by one frame: `Current <- clamp (Current +
Income - Usage) 0 Storage`. Pure, no state beyond the snapshot itself.
*)

(*** do-not-eval ***)
let mutable metal : EconomySnapshot = {
    Current = 500.0f
    Income = 10.0f
    Usage = 7.0f
    Storage = 1000.0f
}

for _ in 1..60 do
    metal <- EconomySim.step metal

printfn "Metal after 60 frames: %.1f" metal.Current

(**
### `UnitDefs` — Pre-built def constants

`UnitDefs` exposes `int` literals for the canonical BAR unit defs used by the three
scenes (`ArmCommander = 1`, `ArmMex = 2`, `ArmLab = 5`, `CorCommander = 11`, ...). Use
these instead of magic numbers when you construct `TrackedUnit` values by hand.

## Feeding a Scene to FSBar.Viz

Synthetic scenes are the canonical offline input for `PreviewSession.startPlayback`.
Convert each `GameState` to a `GameSnapshot` (via `SceneBuilder` or a manual projection)
and hand the sequence to the playback viewer.
*)

(*** do-not-eval ***)
open FSBar.Viz

let scene = Scenes.generate SceneB

// Simplest approach: use MockSnapshot to build one snapshot per frame
let snapshots =
    scene.Frames
    |> Array.map (fun state ->
        // Project GameState.Units -> UnitState sequence suitable for viz
        let units =
            state.Units
            |> Map.toList
            |> List.map (fun (uid, u) ->
                let (x, y, z) = u.Position
                { UnitId = uid
                  PositionX = x
                  PositionY = y
                  PositionZ = z
                  TeamId = state.TeamId
                  DefId = u.DefId
                  Health = u.Health
                  MaxHealth = u.MaxHealth
                  IsEnemy = false })

        // Grid is synthetic and shared across frames; pick any MapGrid you have at hand
        MockSnapshot.emptySnapshot placeholderGrid
        |> MockSnapshot.withUnits units
        |> MockSnapshot.withFrame (int state.FrameNumber))

// Play back at 30 fps, looping
use _ = PreviewSession.startPlayback snapshots 30

(**
## Next Steps

- [Visualization](viz.html) — where the generated scenes get rendered
- [Game State](gamestate.html) — the same `GameState` type, but produced by a live client
- [Test Suite](tests.html) — `FSBar.SyntheticData.Tests` contains the scene validators and
  the canonical examples of each generator's behavior
*)
