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

End-to-end examples demonstrating common AI patterns with FSBarV1. Each example is a complete,
self-contained scenario.

## Basic Frame Loop

The simplest AI: observe events and print what happens.
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()

let frames = client.Run(200, fun frame ->
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
        | _ -> ()
    [])

printfn "Observed %d frames" frames.Length
client.Stop()

(**
## Commander Rush

Move the commander directly to the enemy base. A simple but effective strategy against NullAI.
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()

// Warm up to receive initial events
let warmup = ResizeArray<GameEvent>()
for _ in 1..30 do
    let f = client.Step()
    warmup.AddRange(f.Events)

// Find our commander
let commanderId =
    warmup
    |> Seq.pick (function
        | GameEvent.UnitCreated(uid, _) -> Some uid
        | _ -> None)

printfn "Commander: unit %d" commanderId

// Rush toward enemy base
let enemyX, enemyY, enemyZ = 4608.0f, 100.0f, 4096.0f
let mutable moveSent = false

let frames =
    client.Run(5000, fun frame ->
        // Check if commander was destroyed
        for evt in frame.Events do
            match evt with
            | GameEvent.UnitDestroyed(uid, _) when uid = commanderId ->
                printfn "Commander destroyed at frame %d!" frame.FrameNumber
            | _ -> ()

        // Send move command periodically
        if not moveSent || frame.FrameNumber % 1000u = 0u then
            moveSent <- true
            [ Commands.MoveCommand commanderId enemyX enemyY enemyZ ]
        else
            [])

client.Stop()

(**
## Building a Factory

Order the commander to build a factory, then wait for it to complete.
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()

// Warm up
for _ in 1..30 do client.Step() |> ignore

let stream = client.Stream

// Find commander and its build options
let commanderId = 1 // Typically the first unit
let comDefId = Callbacks.getUnitDef stream commanderId
let buildOptions = Callbacks.getBuildOptions stream comDefId

printfn "Commander can build %d unit types:" buildOptions.Length
for defId in buildOptions do
    let name = Callbacks.getUnitDefName stream defId
    printfn "  [%d] %s" defId name

// Pick the first build option (usually a factory)
let factoryDefId = buildOptions.[0]
let factoryName = Callbacks.getUnitDefName stream factoryDefId
printfn "Building: %s (def %d)" factoryName factoryDefId

// Get commander position for build location
let (cx, cy, cz) = Callbacks.getUnitPos stream commanderId
let buildX = cx + 200.0f
let buildZ = cz + 200.0f

let mutable factoryBuilt = false
let mutable buildCommandSent = false

let frames =
    client.Run(2000, fun frame ->
        for evt in frame.Events do
            match evt with
            | GameEvent.UnitFinished uid when uid <> commanderId ->
                printfn "Factory built! Unit %d at frame %d" uid frame.FrameNumber
                factoryBuilt <- true
            | _ -> ()

        if not buildCommandSent then
            buildCommandSent <- true
            [ Commands.BuildCommand commanderId factoryDefId buildX cy buildZ 0 ]
        else
            [])

if factoryBuilt then printfn "Success!"
else printfn "Factory not completed in time"

client.Stop()

(**
## Scouting with Patrol

Send the commander on a patrol route to explore the map.
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()
for _ in 1..30 do client.Step() |> ignore

let commanderId = 1
let enemiesSpotted = ResizeArray<int>()

// Define patrol waypoints (clockwise around center)
let waypoints = [|
    (2048.0f, 100.0f, 512.0f)    // North
    (3584.0f, 100.0f, 2048.0f)   // East
    (2048.0f, 100.0f, 3584.0f)   // South
    (512.0f, 100.0f, 2048.0f)    // West
|]

let mutable waypointIdx = 0
let mutable patrolSent = false

let frames =
    client.Run(3000, fun frame ->
        for evt in frame.Events do
            match evt with
            | GameEvent.EnemyEnterLOS eid ->
                if not (enemiesSpotted.Contains(eid)) then
                    enemiesSpotted.Add(eid)
                    printfn "Spotted enemy %d (total: %d)" eid enemiesSpotted.Count
            | GameEvent.UnitIdle _ when waypointIdx < waypoints.Length ->
                waypointIdx <- waypointIdx + 1
            | _ -> ()

        // Send patrol to next waypoint when idle
        if not patrolSent || frame.FrameNumber % 500u = 0u then
            patrolSent <- true
            let (wx, wy, wz) = waypoints.[waypointIdx % waypoints.Length]
            [ Commands.PatrolCommand commanderId wx wy wz ]
        else
            [])

printfn "Scouting complete: spotted %d enemies" enemiesSpotted.Count
client.Stop()

(**
## Economy Management

Monitor metal and energy levels and adjust production accordingly.
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()
for _ in 1..30 do client.Step() |> ignore

let stream = client.Stream

let frames =
    client.Run(1000, fun frame ->
        // Check economy every 100 frames
        if frame.FrameNumber % 100u = 0u then
            let metalCur = Callbacks.getEconomyCurrent stream 0
            let metalInc = Callbacks.getEconomyIncome stream 0
            let metalUse = Callbacks.getEconomyUsage stream 0
            let energyCur = Callbacks.getEconomyCurrent stream 1
            let energyInc = Callbacks.getEconomyIncome stream 1
            let energyUse = Callbacks.getEconomyUsage stream 1

            printfn "Frame %d Economy:" frame.FrameNumber
            printfn "  Metal:  %.0f (%.1f/s income, %.1f/s usage)" metalCur metalInc metalUse
            printfn "  Energy: %.0f (%.1f/s income, %.1f/s usage)" energyCur energyInc energyUse

            if metalCur < 100.0f then
                printfn "  WARNING: Metal low!"
            if energyCur < 200.0f then
                printfn "  WARNING: Energy low!"

        [])

client.Stop()

(**
## Using Callbacks for Situational Awareness

Combine callbacks with events to make informed tactical decisions.
*)

(*** do-not-eval ***)
open System
open FSBar.Client

let client = BarClient.startHeadless ()
for _ in 1..30 do client.Step() |> ignore

let stream = client.Stream
let commanderId = 1

// Track known enemies
let enemies = System.Collections.Generic.Dictionary<int, float32 * float32 * float32>()

let frames =
    client.Run(3000, fun frame ->
        // Track enemy positions
        for evt in frame.Events do
            match evt with
            | GameEvent.EnemyEnterLOS eid ->
                // Query enemy position when they enter LOS
                let (ex, ey, ez) = Callbacks.getUnitPos stream eid
                enemies.[eid] <- (ex, ey, ez)
                printfn "Enemy %d at (%.0f, %.0f)" eid ex ez
            | GameEvent.EnemyLeaveLOS eid ->
                printfn "Enemy %d left LOS" eid
            | GameEvent.EnemyDestroyed(eid, _) ->
                enemies.Remove(eid) |> ignore
                printfn "Enemy %d destroyed (%d remaining)" eid enemies.Count
            | _ -> ()

        // Decision logic every 200 frames
        if frame.FrameNumber % 200u = 0u && enemies.Count > 0 then
            let (cx, _, cz) = Callbacks.getUnitPos stream commanderId
            let hp = Callbacks.getUnitHealth stream commanderId
            let maxHp = Callbacks.getUnitMaxHealth stream commanderId

            if hp / maxHp < 0.3f then
                // Retreat if low health -- run away from nearest enemy
                let (nearestId, _) =
                    enemies
                    |> Seq.minBy (fun kv ->
                        let (ex, _, ez) = kv.Value
                        (cx - ex) * (cx - ex) + (cz - ez) * (cz - ez))
                    |> fun kv -> kv.Key, kv.Value
                let (ex, _, ez) = enemies.[nearestId]
                let dx, dz = cx - ex, cz - ez
                let len = float32 (Math.Sqrt(float (dx * dx + dz * dz)))
                let retreatX = cx + dx / len * 500.0f
                let retreatZ = cz + dz / len * 500.0f
                printfn "Retreating!"
                [ Commands.MoveCommand commanderId retreatX 100.0f retreatZ ]
            else
                // Attack nearest enemy
                let nearestId =
                    enemies
                    |> Seq.minBy (fun kv ->
                        let (ex, _, ez) = kv.Value
                        (cx - ex) * (cx - ex) + (cz - ez) * (cz - ez))
                    |> fun kv -> kv.Key
                [ Commands.AttackCommand commanderId nearestId ]
        else
            [])

client.Stop()

(**
## Commander Assassin (Full BARb Scenario)

A complete multi-phase AI that rushes to the enemy base, identifies the enemy commander,
and attacks it. This is the same logic used in the live BARb integration tests.
*)

(*** do-not-eval ***)
open System
open FSBar.Client

let client = BarClient.startHeadless ()
for _ in 1..30 do client.Step() |> ignore

let stream = client.Stream
let commanderId = 1

let enemyX, enemyY, enemyZ = 4608.0f, 100.0f, 4096.0f
let mutable phase = "move"
let mutable enemyComId = -1
let mutable frameCount = 0
let enemiesInLOS = ResizeArray<int>()
let checkedDefs = System.Collections.Generic.HashSet<int>()

let mutable gameOver = false

while not gameOver && frameCount < 12000 do
    match Protocol.receiveFrame stream with
    | None -> gameOver <- true
    | Some frame ->
        frameCount <- frameCount + 1

        for evt in frame.Events do
            match evt with
            | GameEvent.EnemyEnterLOS eid ->
                if not (enemiesInLOS.Contains(eid)) then
                    enemiesInLOS.Add(eid)
            | GameEvent.EnemyDestroyed(eid, _) when eid = enemyComId ->
                printfn "ENEMY COMMANDER DESTROYED at frame %d!" frameCount
                gameOver <- true
            | GameEvent.UnitDestroyed(uid, _) when uid = commanderId ->
                printfn "Our commander died at frame %d" frameCount
                gameOver <- true
            | _ -> ()

        // Identify enemy commander
        if enemyComId < 0 && frameCount % 100 = 0 then
            for eid in enemiesInLOS do
                if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
                    checkedDefs.Add(eid) |> ignore
                    let defId = Callbacks.getUnitDef stream eid
                    if defId > 0 then
                        let name = Callbacks.getUnitDefName stream defId
                        if name.Contains("commander") || name.Contains("com_") then
                            enemyComId <- eid
                            phase <- "kill"
                            printfn "Found enemy commander: unit %d (%s)" eid name

        // Check arrival
        if phase = "move" && frameCount % 500 = 0 then
            let (cx, _, cz) = Callbacks.getUnitPos stream commanderId
            let dist = Math.Sqrt(float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
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
client.Stop()
