(**
---
title: Callbacks
category: Tutorials
categoryindex: 2
index: 3
---
*)

(**
# Callbacks

Callbacks are mid-frame queries that ask the engine for current game state. Unlike events (which
the engine pushes to you), callbacks are pull-based: you send a request and receive a response
on the same socket connection.

## How Callbacks Work

During a frame handler (inside `StepWith` or when using the raw `Protocol` API), you can call
any function in the `Callbacks` module. Each function:

1. Constructs a `CallbackRequest` protobuf message
2. Sends it via `Protocol.sendCallback`
3. Blocks until the proxy responds with a `CallbackResponse`
4. Extracts and returns the typed result

All callback functions take a `NetworkStream` as the first parameter. Get this from `client.Stream`.

## Timing Constraints

Callbacks work reliably at high game speeds (100x) but can cause protocol desync at low speeds.
At low game speeds, the engine may send the next frame before the callback round-trip completes.
See [Known Issues](known-issues.html) for details and workarounds.

## Callback Reference

### Team Info

#### getMyTeam

Get the team ID assigned to this AI.
*)

(*** do-not-eval ***)
open FSBar.Client
open System.Net.Sockets

let stream : NetworkStream = client.Stream
let teamId = Callbacks.getMyTeam stream
printfn "My team: %d" teamId

(**
#### getMyAllyTeam

Get the ally-team ID for this AI.
*)

(*** do-not-eval ***)
let allyTeamId = Callbacks.getMyAllyTeam stream
printfn "My ally team: %d" allyTeamId

(**
#### getStartPos

Get the start position for a given team as `(x, y, z)`.
*)

(*** do-not-eval ***)
let (sx, sy, sz) = Callbacks.getStartPos stream 0
printfn "Team 0 start: (%.0f, %.0f, %.0f)" sx sy sz

(**
### Map Info

#### getMapWidth / getMapHeight

Get the map dimensions in heightmap squares. Multiply by 8 to get elmo coordinates.
*)

(*** do-not-eval ***)
let mapW = Callbacks.getMapWidth stream
let mapH = Callbacks.getMapHeight stream
printfn "Map size: %d x %d heightmap squares (%d x %d elmos)" mapW mapH (mapW * 8) (mapH * 8)

(**
#### getMetalSpots

Get all metal extraction spots as `(x, y, z, value)` tuples.
*)

(*** do-not-eval ***)
let spots = Callbacks.getMetalSpots stream
printfn "Found %d metal spots" spots.Length
for (x, y, z, v) in spots do
    printfn "  Metal at (%.0f, %.0f, %.0f) value=%.1f" x y z v

(**
### Unit Info

#### getUnitPos

Get the current position of a unit.
*)

(*** do-not-eval ***)
let (ux, uy, uz) = Callbacks.getUnitPos stream 1
printfn "Unit 1 at (%.0f, %.0f, %.0f)" ux uy uz

(**
#### getUnitHealth / getUnitMaxHealth

Get the current and maximum health of a unit.
*)

(*** do-not-eval ***)
let hp = Callbacks.getUnitHealth stream 1
let maxHp = Callbacks.getUnitMaxHealth stream 1
printfn "Unit 1 health: %.0f / %.0f (%.0f%%)" hp maxHp (hp / maxHp * 100.0f)

(**
#### getUnitDef

Get the unit-definition ID for a unit instance.
*)

(*** do-not-eval ***)
let defId = Callbacks.getUnitDef stream 1
printfn "Unit 1 def ID: %d" defId

(**
#### getUnitDefName

Get the string name of a unit definition.
*)

(*** do-not-eval ***)
let defName = Callbacks.getUnitDefName stream defId
printfn "Def %d is: %s" defId defName

(**
#### getBuildOptions

Get the unit-definition IDs that a builder can construct.
*)

(*** do-not-eval ***)
let buildOpts = Callbacks.getBuildOptions stream defId
printfn "Builder can build %d unit types" buildOpts.Length

(**
#### getMaxWeaponRange

Get the maximum weapon range for a unit definition.
*)

(*** do-not-eval ***)
let range = Callbacks.getMaxWeaponRange stream defId
printfn "Max weapon range: %.0f" range

(**
#### getBuildSpeed

Get the build speed for a unit definition.
*)

(*** do-not-eval ***)
let buildSpeed = Callbacks.getBuildSpeed stream defId
printfn "Build speed: %.1f" buildSpeed

(**
#### getUnitDefCost

Get the metal cost for a unit definition.
*)

(*** do-not-eval ***)
let cost = Callbacks.getUnitDefCost stream defId
printfn "Unit cost: %.0f metal" cost

(**
#### getUnitDefs

Get all available unit-definition IDs (up to a max count).
*)

(*** do-not-eval ***)
let allDefs = Callbacks.getUnitDefs stream 1000
printfn "Game has %d unit definitions" allDefs.Length

(**
### Economy

Resource IDs: `0` = metal, `1` = energy.

#### getEconomyCurrent

Get the current amount of a resource.
*)

(*** do-not-eval ***)
let metalAmount = Callbacks.getEconomyCurrent stream 0
let energyAmount = Callbacks.getEconomyCurrent stream 1
printfn "Metal: %.0f  Energy: %.0f" metalAmount energyAmount

(**
#### getEconomyIncome / getEconomyUsage

Get the income and usage rates for a resource.
*)

(*** do-not-eval ***)
let metalIncome = Callbacks.getEconomyIncome stream 0
let metalUsage = Callbacks.getEconomyUsage stream 0
printfn "Metal: +%.1f / -%.1f" metalIncome metalUsage

(**
#### getEconomyStorage

Get the storage capacity for a resource.
*)

(*** do-not-eval ***)
let metalStorage = Callbacks.getEconomyStorage stream 0
printfn "Metal storage capacity: %.0f" metalStorage

(**
### Map Data (Raw Arrays)

These callbacks return large flat arrays of map data. They are used internally by `MapGrid.loadFromEngine`
but can be called directly for custom analysis.

#### getHeightMap

Get the full heightmap as a flat float32 list in row-major order.
*)

(*** do-not-eval ***)
let heightData = Callbacks.getHeightMap stream
printfn "Heightmap: %d values" heightData.Length

(**
#### getCornersHeightMap

Get the vertex-resolution corners heightmap. Returns `(mapWidth+1) * (mapHeight+1)` values.
*)

(*** do-not-eval ***)
let cornersData = Callbacks.getCornersHeightMap stream
printfn "Corners heightmap: %d values" cornersData.Length

(**
#### getSlopeMap

Get the slope map as a flat float32 list.
*)

(*** do-not-eval ***)
let slopeData = Callbacks.getSlopeMap stream
printfn "Slope map: %d values" slopeData.Length

(**
#### getLosMap / getRadarMap / getResourceMap

Get line-of-sight, radar coverage, and resource distribution maps as flat int lists.
*)

(*** do-not-eval ***)
let losData = Callbacks.getLosMap stream
let radarData = Callbacks.getRadarMap stream
let resourceData = Callbacks.getResourceMap stream
printfn "LOS: %d  Radar: %d  Resource: %d values" losData.Length radarData.Length resourceData.Length

(**
## Usage Example

A complete example querying unit info and economy during a frame:
*)

(*** do-not-eval ***)
open FSBar.Client

let client = BarClient.startHeadless ()

// Run 30 warm-up frames
for _ in 1..30 do client.Step() |> ignore

// Use the raw protocol API for callback-heavy work
let stream = client.Stream

match Protocol.receiveFrame stream with
| Some frame ->
    // Query economy
    let metal = Callbacks.getEconomyCurrent stream 0
    let energy = Callbacks.getEconomyCurrent stream 1
    printfn "Frame %d: Metal=%.0f Energy=%.0f" frame.FrameNumber metal energy

    // Query each unit
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitIdle uid ->
            let (x, _, z) = Callbacks.getUnitPos stream uid
            let hp = Callbacks.getUnitHealth stream uid
            printfn "  Idle unit %d at (%.0f, %.0f) hp=%.0f" uid x z hp
        | _ -> ()

    Protocol.sendFrameResponse stream []
| None ->
    printfn "Game ended"

client.Stop()
