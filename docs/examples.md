# Examples

## Getting Started

### Minimal: Step Through Frames

```fsharp
open FSBar.Client

use client = BarClient.startHeadless ()

for i in 1..5 do
    let frame = client.Step()
    printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length
```

### Run with a Handler

```fsharp
open FSBar.Client

use client = BarClient.startHeadless ()

let frames = client.Run(100, fun frame ->
    // Return commands for each frame
    frame.Events
    |> List.choose (function
        | GameEvent.UnitIdle uid ->
            Some (Commands.MoveCommand uid 4096.0f 100.0f 4096.0f)
        | _ -> None))

printfn "Processed %d frames" frames.Length
```

### Custom Configuration

```fsharp
open FSBar.Client

let config =
    { EngineConfig.defaultConfig () with
        OpponentAI = "BARb"       // Play against BARb AI
        GameSpeed = 100           // 100x speed for fast simulation
        MapName = "Red Rock Desert v2" }

use client = new BarClient(config)
client.Start()

// ... interact with the game ...
client.Stop()
```

## Graphical Mode

Launch a windowed game for visual debugging:

```fsharp
open FSBar.Client

use client = BarClient.startGraphical ()

// Run 300 frames — watch the game window
client.Run(300, fun _ -> []) |> ignore
```

To use the direct `spring` binary instead of the AppImage:

```fsharp
let config =
    { EngineConfig.defaultConfig () with
        Mode = Graphical
        AppImagePath = "/path/to/spring"  // graphical engine binary
        TimeoutMs = 120000 }              // longer timeout for GUI startup
```

## Commander Rush (Against BARb AI)

Move the commander to the enemy base:

```fsharp
open FSBar.Client

let config =
    { EngineConfig.defaultConfig () with
        OpponentAI = "BARb"
        GameSpeed = 100 }

use client = new BarClient(config)
client.Start()

// Warm up to capture commander
let mutable comId = -1
for _ in 1..30 do
    let frame = client.Step()
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitCreated(uid, _) when comId < 0 -> comId <- uid
        | _ -> ()

// Enemy base position (from game script — Team 1)
let enemyX, enemyZ = 4608.0f, 4096.0f

// Send commander to enemy base
let stream = client.Stream
let mutable arrived = false
let mutable frameCount = 0

while not arrived && frameCount < 5000 do
    match Protocol.receiveFrame stream with
    | Some frame ->
        frameCount <- frameCount + 1

        // Check distance every 500 frames
        if frameCount % 500 = 0 then
            let (cx, _, cz) = Callbacks.getUnitPos stream comId
            let dist = sqrt (float ((enemyX - cx) * (enemyX - cx) + (enemyZ - cz) * (enemyZ - cz)))
            printfn "Frame %d — distance: %.0f" frame.FrameNumber dist
            if dist < 300.0 then arrived <- true

        // Send/resend move command
        if frameCount = 1 || frameCount % 1000 = 0 then
            Protocol.sendFrameResponse stream [ Commands.MoveCommand comId enemyX 100.0f enemyZ ]
        else
            Protocol.sendFrameResponse stream []
    | None -> arrived <- true

printfn "Arrived: %b" arrived
```

## Assassinate Enemy Commander

Find the enemy commander by checking unit definitions, then attack:

```fsharp
// After reaching the enemy base (see above), hunt for their commander:
let mutable enemyComId = -1
let checkedDefs = System.Collections.Generic.HashSet<int>()
let enemiesInLOS = System.Collections.Generic.HashSet<int>()

// In the frame loop, collect EnemyEnterLOS events:
// GameEvent.EnemyEnterLOS eid -> enemiesInLOS.Add(eid)

// Then identify the commander:
for eid in enemiesInLOS do
    if enemyComId < 0 && not (checkedDefs.Contains(eid)) then
        checkedDefs.Add(eid) |> ignore
        let defId = Callbacks.getUnitDef stream eid
        if defId > 0 then
            let name = Callbacks.getUnitDefName stream defId
            // BAR commander names: "armcom", "corcom", etc.
            if name.Contains("com") then
                enemyComId <- eid

// Attack the enemy commander:
if enemyComId > 0 then
    Protocol.sendFrameResponse stream [ Commands.AttackCommand comId enemyComId ]
```

## Querying Game State

### Map Information

```fsharp
let stream = client.Stream

// Between receiveFrame and sendFrameResponse:
let width = Callbacks.getMapWidth stream
let height = Callbacks.getMapHeight stream
printfn "Map size: %d x %d" width height

let (startX, startY, startZ) = Callbacks.getStartPos stream 0
printfn "Our start: (%.0f, %.0f, %.0f)" startX startY startZ

let metalSpots = Callbacks.getMetalSpots stream
printfn "Metal spots: %d" metalSpots.Length
```

### Economy Tracking

```fsharp
let metal = Callbacks.getEconomyCurrent stream 0
let energy = Callbacks.getEconomyCurrent stream 1
let metalIncome = Callbacks.getEconomyIncome stream 0
let energyIncome = Callbacks.getEconomyIncome stream 1
printfn "Metal: %.0f (+%.1f/s) | Energy: %.0f (+%.1f/s)" metal metalIncome energy energyIncome
```

### Unit Inspection

```fsharp
let defId = Callbacks.getUnitDef stream unitId
let name = Callbacks.getUnitDefName stream defId
let hp = Callbacks.getUnitHealth stream unitId
let maxHp = Callbacks.getUnitMaxHealth stream unitId
let (x, y, z) = Callbacks.getUnitPos stream unitId
let range = Callbacks.getMaxWeaponRange stream defId
let buildOpts = Callbacks.getBuildOptions stream defId

printfn "%s (HP: %.0f/%.0f) at (%.0f, %.0f) range=%.0f builds=%d options"
    name hp maxHp x z range buildOpts.Length
```

## Using F# Interactive

Load the prelude for REPL access:

```fsharp
#load "scripts/prelude.fsx"
open FSBar.Client

// Start a session
let client = BarClient.startHeadless ()

// Step interactively
let f1 = client.Step()
f1.Events |> List.iter (printfn "%A")

// Send a command
let f2 = client.StepWith(fun _ ->
    [ Commands.MoveCommand 42 2048.0f 100.0f 2048.0f ])

// Clean up
client.Stop()
```
