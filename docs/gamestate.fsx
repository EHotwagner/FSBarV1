(**
---
title: Game State
category: Tutorials
categoryindex: 2
index: 2
description: Observable frames and the always-current GameState snapshot.
---
*)

(**
# Game State

`FSBar.Client` exposes two complementary views of the running game:

1. **Push stream** — `client.Frames : IObservable<GameFrame>` emits raw per-frame events
2. **Current snapshot** — `client.GameState : GameState` is refolded from the stream every
   frame and exposes tracked units, enemies, and economy

Both views are wired up automatically when `BarClient.startHeadless ()` or
`BarClient.startGraphical ()` returns a running client — you do not need to start an event
loop yourself.

## The Observable Frame Stream

`client.Frames` is a hot observable: frames are produced as soon as the engine sends them,
whether or not anyone is subscribed. Use `Subscribe` to attach a handler.
*)

(*** do-not-eval ***)
open FSBar.Client

use client = BarClient.startHeadless ()

use _ =
    client.Frames.Subscribe(fun frame ->
        printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length)

// Drive the session however you want — the subscriber will receive events in parallel
client.WaitFrames 500 (fun _ -> ())

(**
Subscribers are invoked from the reader thread, so keep them fast. For heavier work (UI
updates, disk I/O), hand the frame off to a channel or the thread pool and return
immediately.

### Subscribing and filtering with LINQ / Rx-style operators

Because `Frames` implements `IObservable<GameFrame>`, you can compose it with any standard
.NET reactive operator. A simple hand-rolled filter is shown below; `System.Reactive.Linq`
adds the usual `Where` / `Select` / `Buffer` primitives on top.
*)

(*** do-not-eval ***)
let onlyUnitEvents =
    { new System.IObserver<GameFrame> with
        member _.OnNext frame =
            for evt in frame.Events do
                match evt with
                | GameEvent.UnitCreated _
                | GameEvent.UnitFinished _
                | GameEvent.UnitIdle _
                | GameEvent.UnitDestroyed _ ->
                    printfn "[Frame %d] %A" frame.FrameNumber evt
                | _ -> ()
        member _.OnError _ = ()
        member _.OnCompleted () = () }

use _ = client.Frames.Subscribe(onlyUnitEvents)

(**
## Synchronous Handler: WaitFrames

For REPL sessions and linear scripts, `client.WaitFrames count handler` blocks until
exactly `count` frames have been consumed. It uses the same observable stream under the
hood, so anything you do inside the handler happens between frame arrivals.
*)

(*** do-not-eval ***)
client.WaitFrames 100 (fun frame ->
    if frame.FrameNumber % 10u = 0u then
        printfn "Tick %d — %d events this frame" frame.FrameNumber frame.Events.Length)

(**
## Queueing Commands

Commands are not returned from the handler — they are queued with `SendCommands` and
flushed with the next `FrameResponse` automatically.
*)

(*** do-not-eval ***)
open FSBar.Client.Commands

client.WaitFrames 500 (fun frame ->
    let cmds =
        frame.Events
        |> List.choose (function
            | GameEvent.UnitIdle uid ->
                Some (PatrolCommand uid 2048.0f 100.0f 2048.0f)
            | _ -> None)
    if not cmds.IsEmpty then client.SendCommands cmds)

(**
## The GameState Snapshot

`client.GameState` holds an always-current rollup of the game. Every incoming frame is
folded through `GameState.processFrame`, so the snapshot is guaranteed to reflect the most
recent event the client has processed.

### Shape
*)

(*** do-not-eval ***)
type GameState = {
    FrameNumber: uint32
    TeamId: int
    Units: Map<int, TrackedUnit>           // our friendly units
    Enemies: Map<int, TrackedEnemy>        // known enemies (from LOS/radar/damage events)
    Metal: EconomySnapshot
    Energy: EconomySnapshot
    UnitDefs: UnitDefCache                 // lazily filled metadata (names, costs, ...)
    Events: GameEvent list                 // events from the frame just processed
}

(**
### Tracked Unit

A `TrackedUnit` is kept for each friendly unit from its `UnitCreated` event until a
`UnitDestroyed` / `UnitGiven` / `UnitCaptured` event removes it. Positions come from the
initial creation event; periodic `Update` events refresh health and idle state.
*)

(*** do-not-eval ***)
type TrackedUnit = {
    UnitId: int
    DefId: int
    Position: float32 * float32 * float32
    Health: float32
    MaxHealth: float32
    IsFinished: bool
    IsIdle: bool
}

(**
### Tracked Enemy

Enemies are populated from `EnemyEnterLOS`, `EnemyEnterRadar`, `EnemyDamaged`, and
`EnemyCreated` events. Because radar-only contacts do not give position, `Health` is
`float32 option` and the `InLOS` / `InRadar` flags indicate what sensor state we have.
*)

(*** do-not-eval ***)
type TrackedEnemy = {
    EnemyId: int
    DefId: int option
    Position: float32 * float32 * float32
    Health: float32 option
    InLOS: bool
    InRadar: bool
}

(**
### Economy Snapshot

`Metal` and `Energy` both use the same shape — current amount, income rate, usage rate, and
storage cap — all updated from the engine's `UpdateResources` event.
*)

(*** do-not-eval ***)
type EconomySnapshot = {
    Current: float32
    Income: float32
    Usage: float32
    Storage: float32
}

(**
## Example: Build-Order Watcher

Monitor an economy threshold and react once metal crosses a certain level.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()
client.WaitFrames 30 (fun _ -> ())  // warmup

let mutable expansionStarted = false

client.WaitFrames 5000 (fun _ ->
    if not expansionStarted && client.GameState.Metal.Current > 500.0f then
        expansionStarted <- true
        printfn "Metal hit 500 at frame %d — starting expansion"
            client.GameState.FrameNumber
        // ... queue build commands via SendCommands here
)

(**
## Example: Enemy Threat Detection

Scan `GameState.Enemies` each frame and react to new LOS contacts without manually
tracking them.
*)

(*** do-not-eval ***)
let seen = System.Collections.Generic.HashSet<int>()

client.WaitFrames 3000 (fun _ ->
    for KeyValue(eid, enemy) in client.GameState.Enemies do
        if enemy.InLOS && seen.Add eid then
            let (x, _, z) = enemy.Position
            printfn "New enemy %d sighted at (%.0f, %.0f)" eid x z)

(**
## Reset

Calling `client.Reset ()` sends the engine's cheat-reset commands (give ~, kill all units,
etc.) without tearing down the session. The `GameState.Units` and `GameState.Enemies` maps
will drain naturally as `UnitDestroyed` / `EnemyDestroyed` events arrive on the next
frames — they are not cleared eagerly.

## Rolling Your Own Processing

`GameState.processFrame` is public, so if you want to fork a parallel state (for example,
to snapshot the game every N frames without blocking), you can fold your own copy:
*)

(*** do-not-eval ***)
let mutable snapshots : GameState list = []

client.WaitFrames 1000 (fun frame ->
    if frame.FrameNumber % 100u = 0u then
        // client.GameState is already the folded current state
        snapshots <- client.GameState :: snapshots)

(**
## Next Steps

- [Commands & Events](commands-and-events.html) — the full `GameEvent` vocabulary
- [Callbacks](callbacks.html) — query engine state that is *not* in `GameState`
  (map data, unit-def metadata, start positions)
- [Visualization](viz.html) — attach `FSBar.Viz` to the live `GameState` for rendering
*)
