# Callbacks

Callbacks allow querying game state mid-frame. They are synchronous request/response exchanges issued between receiving a frame and sending the frame response.

## When to Use Callbacks

Callbacks work within the protocol's frame exchange window:

```
Receive Frame ──► [Issue callbacks here] ──► Send FrameResponse
```

**With raw Protocol API** (recommended for callbacks):

```fsharp
let stream = client.Stream

match Protocol.receiveFrame stream with
| Some frame ->
    // Query state before responding
    let (x, y, z) = Callbacks.getUnitPos stream unitId
    let hp = Callbacks.getUnitHealth stream unitId

    // Now respond with commands
    Protocol.sendFrameResponse stream [ Commands.MoveCommand unitId 4096.0f y 4096.0f ]
| None ->
    printfn "Game ended"
```

**With BarClient.StepWith** — callbacks work at high game speeds (100x) but may conflict with frames at low speeds (1-5x) because the engine sends the next frame before the callback round-trip completes.

## Available Callbacks

### Team Information

```fsharp
Callbacks.getMyTeam stream       // -> int (our team ID)
Callbacks.getMyAllyTeam stream   // -> int (our ally-team ID)
```

### Map Queries

```fsharp
Callbacks.getMapWidth stream     // -> int (width in heightmap squares)
Callbacks.getMapHeight stream    // -> int (height in heightmap squares)

Callbacks.getStartPos stream teamId
// -> (x: float32, y: float32, z: float32)
// Team 0 and Team 1 start positions from the game script

Callbacks.getMetalSpots stream
// -> (x, y, z, value)[] — all metal extraction points on the map
```

### Unit State

```fsharp
Callbacks.getUnitPos stream unitId
// -> (x: float32, y: float32, z: float32)

Callbacks.getUnitHealth stream unitId    // -> float32 (current HP)
Callbacks.getUnitMaxHealth stream unitId // -> float32 (max HP)
Callbacks.getUnitDef stream unitId       // -> int (unit definition ID)
```

These work for own units. For enemy units in line of sight, `getUnitDef` returns the definition ID which can be used with `getUnitDefName` to identify the unit type.

### Unit Definition Queries

```fsharp
Callbacks.getUnitDefName stream defId     // -> string (e.g. "armcom", "corcom")
Callbacks.getBuildOptions stream defId     // -> int[] (buildable unit def IDs)
Callbacks.getMaxWeaponRange stream defId   // -> float32
Callbacks.getBuildSpeed stream defId       // -> float32
Callbacks.getUnitDefCost stream defId      // -> float32
```

### Economy

Resource IDs: `0` = metal, `1` = energy.

```fsharp
Callbacks.getEconomyCurrent stream 0   // -> float32 (current metal)
Callbacks.getEconomyIncome stream 1    // -> float32 (energy income rate)
Callbacks.getEconomyUsage stream 0     // -> float32 (metal usage rate)
Callbacks.getEconomyStorage stream 1   // -> float32 (energy storage capacity)
```

### Bulk Queries

```fsharp
Callbacks.getUnitDefs stream 500
// -> int[] (up to 500 available unit definition IDs)
```

## Example: Identify Enemy Commander

```fsharp
// When an enemy enters LOS, check if it's a commander
let identifyUnit stream enemyId =
    let defId = Callbacks.getUnitDef stream enemyId
    if defId > 0 then
        let name = Callbacks.getUnitDefName stream defId
        if name.Contains("com") then
            printfn "Enemy commander found: %s (unit %d)" name enemyId
            Some enemyId
        else
            None
    else
        None
```

## Example: Track Unit Movement

```fsharp
let trackMovement stream unitId targetX targetZ =
    let (x, _, z) = Callbacks.getUnitPos stream unitId
    let dist = sqrt (float ((targetX - x) * (targetX - x) + (targetZ - z) * (targetZ - z)))
    printfn "Unit %d at (%.0f, %.0f), distance to target: %.0f" unitId x z dist
    dist < 200.0  // returns true if arrived
```
