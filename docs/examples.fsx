(**
---
title: Examples
category: How-To
categoryindex: 3
index: 1
---
*)

(**
# Examples

End-to-end examples demonstrating common AI patterns with FSBarV1. Each example is a
self-contained scenario that uses the current `BarClient` API:

- `client.WaitFrames count handler` to consume a fixed number of frames synchronously
- `client.SendCommands commands` to queue commands for the next frame response
- `client.Frames : IObservable<GameFrame>` to subscribe to the push stream
- `client.GameState` for an always-current snapshot of tracked units, enemies, and economy
- `client.Stream` for direct `Callbacks` and raw `Protocol` access when needed

## Basic Observation Loop

The simplest AI: observe events for 200 frames and print what happens.
*)

(*** do-not-eval ***)
open FSBar.Client
open FSBar.Client.Commands

use client = BarClient.startHeadless ()

client.WaitFrames 200 (fun frame ->
    for evt in frame.Events do
        match evt with
        | GameEvent.Init teamId ->
            printfn "[Frame %d] Game init, team %d" frame.FrameNumber teamId
        | GameEvent.UnitCreated(uid, builder) ->
            printfn "[Frame %d] Unit %d created by %d" frame.FrameNumber uid builder
        | GameEvent.UnitFinished uid ->
            printfn "[Frame %d] Unit %d complete" frame.FrameNumber uid
        | GameEvent.UnitIdle uid ->
            printfn "[Frame %d] Unit %d idle" frame.FrameNumber uid
        | GameEvent.EnemyEnterLOS eid ->
            printfn "[Frame %d] Enemy %d spotted" frame.FrameNumber eid
        | GameEvent.UnitDestroyed(uid, attacker) ->
            printfn "[Frame %d] Unit %d destroyed by %A" frame.FrameNumber uid attacker
        | _ -> ())

(**
## Commander Rush

Move the commander directly to the enemy base. A simple but effective strategy against
`NullAI`. Uses `client.GameState` to find the commander after the 30-frame warmup.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()

// Warmup: let the engine emit Init + initial UnitCreated events
client.WaitFrames 30 (fun _ -> ())

// Grab the first tracked unit — that's our commander after the warmup
let commanderId =
    client.GameState.Units
    |> Seq.map (fun kv -> kv.Key)
    |> Seq.head

printfn "Commander: unit %d" commanderId

let enemyX, enemyY, enemyZ = 3200.0f, 100.0f, 3200.0f
let mutable moveSent = false

client.WaitFrames 5000 (fun frame ->
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitDestroyed(uid, _) when uid = commanderId ->
            printfn "Commander destroyed at frame %d!" frame.FrameNumber
        | _ -> ()

    // Send move command periodically so path updates survive order resets
    if not moveSent || frame.FrameNumber % 1000u = 0u then
        moveSent <- true
        client.SendCommands [ MoveCommand commanderId enemyX enemyY enemyZ ])

(**
## Building a Factory

Order the commander to build a factory, then wait for it to complete. Uses `Callbacks`
for build-option discovery and positioning.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()
client.WaitFrames 30 (fun _ -> ())

let stream = client.Stream

// First tracked unit is the commander; look up its build options
let commanderId = client.GameState.Units |> Seq.head |> fun kv -> kv.Key
let comDefId = Callbacks.getUnitDef stream commanderId
let buildOptions = Callbacks.getBuildOptions stream comDefId

printfn "Commander can build %d unit types:" buildOptions.Length
for defId in buildOptions do
    let name = Callbacks.getUnitDefName stream defId
    printfn "  [%d] %s" defId name

// Pick the first build option and place it 200 elmos NE of the commander
let factoryDefId = buildOptions.[0]
let factoryName = Callbacks.getUnitDefName stream factoryDefId
printfn "Building: %s (def %d)" factoryName factoryDefId

let (cx, cy, cz) = Callbacks.getUnitPos stream commanderId
let buildX = cx + 200.0f
let buildZ = cz + 200.0f

let mutable factoryBuilt = false
let mutable buildCommandSent = false

client.WaitFrames 2000 (fun frame ->
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitFinished uid when uid <> commanderId ->
            printfn "Factory built! Unit %d at frame %d" uid frame.FrameNumber
            factoryBuilt <- true
        | _ -> ()

    if not buildCommandSent then
        buildCommandSent <- true
        client.SendCommands
            [ BuildCommand commanderId factoryDefId buildX cy buildZ 0 ])

if factoryBuilt then printfn "Success!"
else printfn "Factory not completed in time"

(**
## Scouting with Patrol

Send the commander on a patrol route to explore the map. Uses `GameState.Enemies` to
track what we've seen instead of accumulating events manually.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()
client.WaitFrames 30 (fun _ -> ())

let commanderId = client.GameState.Units |> Seq.head |> fun kv -> kv.Key

// Define patrol waypoints (clockwise around center)
let waypoints = [|
    (2048.0f, 100.0f,  512.0f)  // North
    (3584.0f, 100.0f, 2048.0f)  // East
    (2048.0f, 100.0f, 3584.0f)  // South
    ( 512.0f, 100.0f, 2048.0f)  // West
|]

let mutable waypointIdx = 0
let mutable patrolSent = false

client.WaitFrames 3000 (fun frame ->
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitIdle _ when waypointIdx < waypoints.Length ->
            waypointIdx <- waypointIdx + 1
        | _ -> ()

    // Re-issue patrol periodically
    if not patrolSent || frame.FrameNumber % 500u = 0u then
        patrolSent <- true
        let (wx, wy, wz) = waypoints.[waypointIdx % waypoints.Length]
        client.SendCommands [ PatrolCommand commanderId wx wy wz ])

printfn "Scouting complete: %d enemies in GameState" client.GameState.Enemies.Count

(**
## Economy Monitoring

Monitor metal and energy levels via `client.GameState` — no callbacks required.
*)

(*** do-not-eval ***)
use client = BarClient.startHeadless ()
client.WaitFrames 30 (fun _ -> ())

client.WaitFrames 1000 (fun frame ->
    if frame.FrameNumber % 100u = 0u then
        let m = client.GameState.Metal
        let e = client.GameState.Energy
        printfn "Frame %d Economy:" frame.FrameNumber
        printfn "  Metal:  %.0f (+%.1f/s income, -%.1f/s usage)" m.Current m.Income m.Usage
        printfn "  Energy: %.0f (+%.1f/s income, -%.1f/s usage)" e.Current e.Income e.Usage
        if m.Current < 100.0f then printfn "  WARNING: Metal low!"
        if e.Current < 200.0f then printfn "  WARNING: Energy low!")

(**
## Callbacks for Situational Awareness

Combine `GameState.Enemies` with `Callbacks.getUnitPos` to make tactical decisions.
Callback queries work reliably at high game speeds — see [Known Issues](known-issues.html)
for timing caveats at 1–5x.
*)

(*** do-not-eval ***)
open System

use client = BarClient.startHeadless ()
client.WaitFrames 30 (fun _ -> ())

let stream = client.Stream
let commanderId = client.GameState.Units |> Seq.head |> fun kv -> kv.Key

client.WaitFrames 3000 (fun frame ->
    if frame.FrameNumber % 200u = 0u && client.GameState.Enemies.Count > 0 then
        let (cx, _, cz) = Callbacks.getUnitPos stream commanderId
        let hp = Callbacks.getUnitHealth stream commanderId
        let maxHp = Callbacks.getUnitMaxHealth stream commanderId

        // Pick nearest known enemy from GameState
        let nearest =
            client.GameState.Enemies
            |> Seq.minBy (fun kv ->
                let (ex, _, ez) = kv.Value.Position
                (cx - ex) * (cx - ex) + (cz - ez) * (cz - ez))

        let (ex, _, ez) = nearest.Value.Position

        if hp / maxHp < 0.3f then
            // Retreat away from nearest enemy
            let dx, dz = cx - ex, cz - ez
            let len = float32 (Math.Sqrt(float (dx * dx + dz * dz)))
            let retreatX = cx + dx / len * 500.0f
            let retreatZ = cz + dz / len * 500.0f
            printfn "Retreating!"
            client.SendCommands [ MoveCommand commanderId retreatX 100.0f retreatZ ]
        else
            client.SendCommands [ AttackCommand commanderId nearest.Key ])

(**
## Raw Protocol Loop (Advanced)

For maximum control, you can bypass `BarClient` and drive the protocol directly. This is
the pattern used by the `BarbAssassin` live integration tests: a single while-loop that
reads frames, issues callbacks inline, and sends `FrameResponse` messages manually.

This approach avoids the internal `BarClient` reader thread and `GameState` fold, so it
is the most precise way to interleave callbacks with the frame loop — at the cost of doing
more bookkeeping yourself.
*)

(*** do-not-eval ***)
open System
open FSBar.Client

// Build the client but don't let the reader thread start: construct directly
// then Start() to perform handshake and own the stream.
let config = BarClient.defaultConfig ()
use client = BarClient.create config
client.Start ()

let stream = client.Stream
let commanderId = client.GameState.Units |> Seq.head |> fun kv -> kv.Key

let enemyX, enemyY, enemyZ = 3200.0f, 100.0f, 3200.0f
let mutable phase = "move"
let mutable enemyComId = -1
let mutable frameCount = 0
let checkedDefs = System.Collections.Generic.HashSet<int>()
let mutable gameOver = false

while not gameOver && frameCount < 12000 do
    match Protocol.receiveFrame stream with
    | None -> gameOver <- true
    | Some frame ->
        frameCount <- frameCount + 1

        for evt in frame.Events do
            match evt with
            | GameEvent.EnemyDestroyed(eid, _) when eid = enemyComId ->
                printfn "ENEMY COMMANDER DESTROYED at frame %d!" frameCount
                gameOver <- true
            | GameEvent.UnitDestroyed(uid, _) when uid = commanderId ->
                printfn "Our commander died at frame %d" frameCount
                gameOver <- true
            | _ -> ()

        // Identify enemy commander by def name
        if enemyComId < 0 && frameCount % 100 = 0 then
            for KeyValue(eid, _) in client.GameState.Enemies do
                if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
                    checkedDefs.Add(eid) |> ignore
                    let defId = Callbacks.getUnitDef stream eid
                    if defId > 0 then
                        let name = Callbacks.getUnitDefName stream defId
                        if name.Contains("commander") || name.Contains("com_") then
                            enemyComId <- eid
                            phase <- "kill"
                            printfn "Found enemy commander: unit %d (%s)" eid name

        // Phase transitions based on arrival distance
        if phase = "move" && frameCount % 500 = 0 then
            let (cx, _, cz) = Callbacks.getUnitPos stream commanderId
            let dist =
                Math.Sqrt(
                    float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
            if dist < 400.0 then
                phase <- "hunt"
                printfn "Arrived at enemy base, hunting..."

        let commands =
            match phase with
            | "move" when frameCount = 1 || frameCount % 1000 = 0 ->
                [ Commands.MoveCommand commanderId enemyX enemyY enemyZ ]
            | "hunt" when frameCount % 300 = 0 ->
                let angle = float frameCount / 300.0 * Math.PI / 3.0
                let px = enemyX + 500.0f * float32 (Math.Cos(angle))
                let pz = enemyZ + 500.0f * float32 (Math.Sin(angle))
                [ Commands.MoveCommand commanderId px enemyY pz ]
            | "kill" when frameCount % 200 = 0 ->
                [ Commands.AttackCommand commanderId enemyComId ]
            | _ -> []

        Protocol.sendFrameResponse stream commands

printfn "Game ended after %d frames (phase: %s)" frameCount phase

(**
## Next Steps

- [Game State](gamestate.html) — full reference for `client.GameState`, `TrackedUnit`,
  `TrackedEnemy`, and `EconomySnapshot`
- [Commands & Events](commands-and-events.html) — every builder and event case
- [Callbacks](callbacks.html) — all 26 mid-frame queries
- [Visualization](viz.html) — attach `FSBar.Viz` to a running client for live rendering
*)
