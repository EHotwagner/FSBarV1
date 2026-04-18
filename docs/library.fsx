(**
---
title: Library
category: Tutorials
categoryindex: 2
index: 2
description: BarClient, commands, events, GameState, callbacks, map analysis.
---
*)

(**
# Library

`FSBar.Client` is the core library. It owns the engine lifecycle, the protobuf wire protocol, the `GameState` projection, and the map-analysis primitives.

## BarClient

```fsharp
open FSBar.Client

// Headless — fastest path for CI / scripts.
use client = BarClient.startHeadless ()

// Graphical — full BAR window (Linux only, windowed; fullscreen is disabled).
use graphical = BarClient.startGraphical ()
```

Two frame APIs:

- **Pull** — `client.WaitFrames n handler` blocks until `n` frames have passed, invoking `handler : GameFrame -> unit` per frame. Best for REPL.
- **Push** — `client.Frames : IObservable<GameFrame>` for reactive pipelines.

A `GameFrame` is:

```fsharp
type GameFrame =
    { FrameNumber: int
      Events: GameEvent list
      State: GameState }
```

`client.GameState` is an always-current snapshot updated each frame.

## Commands

`FSBar.Client.Commands` produces typed `AICommand` values. Send any sequence with `client.SendCommands : AICommand list -> unit`.

```fsharp
open FSBar.Client.Commands

client.SendCommands [
    MoveCommand 42 2000.0f 100.0f 1000.0f
    BuildCommand 12 "armllt" 2100.0f 100.0f 1050.0f
    StopCommand 17
]
```

Builders cover Move, Build, Attack, Guard, Patrol, Repair, Reclaim, Stop, Wait, SetFireState, SetMoveState, and more.

## Events

`GameEvent` is a flat DU of all engine-sourced facts: `UnitCreated`, `UnitFinished`, `UnitIdle`, `UnitDamaged`, `UnitDestroyed`, `EnemyEnterLOS`, `EnemyLeaveLOS`, `EnemyEnterRadar`, `PlayerCommand`, and many more.

Typical handler shape:

```fsharp
client.WaitFrames 500 (fun frame ->
    frame.Events
    |> List.choose (function
        | GameEvent.UnitIdle uid -> Some (MoveCommand uid 4096.0f 100.0f 4096.0f)
        | _ -> None)
    |> function [] -> () | cmds -> client.SendCommands cmds)
```

## GameState

```fsharp
type GameState =
    { TrackedUnits: Map<int, TrackedUnit>
      TrackedEnemies: Map<int, TrackedEnemy>
      Economy: EconomySnapshot
      FrameNumber: int
      UnitDefs: UnitDefCache }
```

`TrackedUnit` carries position, heading, health, def id, team, and build progress. `TrackedEnemy` adds LOS/radar state. `EconomySnapshot` holds metal/energy income, storage, and pull.

## Callbacks

`FSBar.Client.Callbacks` exposes 26 mid-frame queries (unit position/health, team economy, map info, raw heightmap data, LOS arrays, …). Use these when you need richer data than `GameState` carries, or data not fact-extracted into events.

## Map analysis

| Module | Purpose |
|---|---|
| `MapGrid` | Array2D heightmap / slope / resource / passability layers |
| `MapQuery` | Spatial queries over a `MapGrid` (points, regions, LOS samples) |
| `SmfParser` | Parses `.smf` map files out of `.sd7` archives via `bsdtar` |
| `Pathing` | Navmesh-style reachable-region primitives |
| `Chokepoints` | Static chokepoint extraction |
| `BasePlan` | Candidate base-layout scoring |
| `WallIn` | Wall/choke blocking planner |
| `MapCacheFile` | JSON+gzip on-disk cache under `bots/trainer/map-cache/` |

`MapCacheFile.read` is the hot path — the trainer bots warm up against committed per-map caches and hard-abort on `codeVersion` mismatch.

## Synthetic data

`FSBar.SyntheticData` produces deterministic `GameState` snapshots + `Scene` values without a running engine — the basis of every Viz unit test and every Hub-tab preview. See [Visualization](visualization.html).

## API reference

Full auto-generated reference: [API](reference/index.html).
*)
