# Quickstart: GameState API

**Feature**: 016-gamestate-api

## Using GameState in the REPL

```fsharp
#load "scripts/examples/Repl.fsx"
open Repl

// Start a game — GameState is initialized automatically
start()

// All units are tracked automatically
units()          // shows commander from GameState
economy()        // reads from GameState.Metal / .Energy

// Instant unit def lookup (no more slow scans)
defByName "armmex"    // returns UnitDefInfo with defId, cost, build options

// Build using the cached defId
let mex = defByName "armmex"
build 25947 mex.Value.DefId 152.0f 168.0f 0

// Step with tracked state updates
step 100

// Query map
nearestMetal 500.0f 400.0f   // nearest metal spot to commander
```

## Unit Debugging

```fsharp
// Watch a specific unit
watch 25947        // watch the commander

// Auto-report prints status each frame
step 10            // prints commander's position/health/idle each frame

// Manual report
watches()          // show all watched units

// Stop watching
unwatch 25947
```

## Using GameState Programmatically

```fsharp
// Access the full game state
let state = getState()

// Query units
state.Units |> Map.iter (fun id u -> printfn "%d: %s at (%.0f, %.0f)" id u.Name u.X u.Z)

// Find idle units
GameState.idleUnits state |> List.iter (fun u -> printfn "Idle: %s" u.Name)

// Find units by type
GameState.unitsByName "armmex" state |> List.length |> printfn "Mex count: %d"

// Check enemies
state.Enemies |> Map.iter (fun id e ->
    if e.InLOS then printfn "Enemy %d: %s at (%.0f, %.0f)" id e.Name e.X e.Z)
```

## Map Queries

```fsharp
// Nearest metal spot to a position (elmo coordinates)
MapCache.nearestMetalSpot (stream()) 500.0f 400.0f

// Check passability
MapCache.isPassable MoveType.Tank 500 400

// Get cached map grid
match MapCache.current() with
| Some grid -> printfn "Map: %dx%d" grid.WidthHeightmap grid.HeightHeightmap
| None -> printfn "Map not loaded yet"
```

## Build Requirements

```bash
dotnet build src/FSBar.Client/
dotnet build tests/FSBar.Viz.Tests/  # includes all transitive deps
```
