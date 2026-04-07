(**
---
title: Getting Started
category: Overview
categoryindex: 1
index: 1
---
*)

(**
# Getting Started

This guide walks you through installing FSBarV1, setting up prerequisites, and running your first headless game session.

## Installation

### From NuGet (when published)
*)

(*** do-not-eval ***)
// In your .fsproj or via CLI:
// dotnet add package FSBar.Client

(**
### Building from Source
*)

(*** do-not-eval ***)
// Clone the repository and build:
// git clone https://github.com/EHotwagner/FSBarV1.git
// cd FSBarV1
// dotnet build

(**
The solution produces two main assemblies:

- **FSBar.Client** — the client library (references FSBar.Proto and BarData)
- **FSBar.Proto** — generated protobuf types from the HighBar V2 `.proto` schema

## Prerequisites

### BAR Engine

You need the Beyond All Reason (BAR) headless engine binary (`spring-headless`) installed locally.
The engine is typically found at:

```
~/.local/state/Beyond All Reason/engine/<version>/spring-headless
```

Install BAR from [https://www.beyondallreason.info/](https://www.beyondallreason.info/) to get the engine and game data.

### Game Data

The engine needs access to the BAR game data directory. This is usually auto-detected, but you can
override it via the `SpringDataDir` config option:
*)

(*** do-not-eval ***)
open FSBar.Client

let config =
    { EngineConfig.defaultConfig () with
        EngineBin = "/path/to/spring-headless"
        SpringDataDir = Some "/path/to/BAR/data" }

(**
### HighBar V2 Proxy

The HighBar V2 proxy is embedded as an AI module within the BAR engine. It is configured automatically
via the game script that `ScriptGenerator` produces. No separate proxy process is needed.

## First Headless Session

The simplest way to start a game is with `BarClient.startHeadless`:
*)

(*** do-not-eval ***)
open FSBar.Client

// Start a headless engine with default settings
let client = BarClient.startHeadless ()

// The client is now connected. Handshake info is available:
let hs = client.Handshake.Value
printfn "Connected: engine=%s map=%s team=%d" hs.EngineVersion hs.MapName hs.TeamId

(**
## Basic Frame Loop

The game progresses one frame at a time. Each call to `Step` or `StepWith` receives one frame
from the engine, processes events, and sends commands back.
*)

(*** do-not-eval ***)
// Simple observation loop (no commands)
for _ in 1..100 do
    let frame = client.Step()
    for evt in frame.Events do
        match evt with
        | GameEvent.UnitCreated(uid, _) -> printfn "Unit %d created" uid
        | GameEvent.UnitFinished uid -> printfn "Unit %d finished" uid
        | _ -> ()

(**
### Using StepWith for Command Responses

`StepWith` lets you process a frame and return commands in one call:
*)

(*** do-not-eval ***)
let frame =
    client.StepWith(fun frame ->
        frame.Events
        |> List.choose (function
            | GameEvent.UnitIdle uid ->
                Some (Commands.PatrolCommand uid 2048.0f 100.0f 2048.0f)
            | _ -> None))

(**
### Using Run for Multi-Frame Execution

`Run` executes a handler for a fixed number of frames:
*)

(*** do-not-eval ***)
let allFrames =
    client.Run(500, fun frame ->
        // Return commands for each frame
        [])

printfn "Processed %d frames" allFrames.Length

(**
### Using RunUntil for Condition-Based Execution

`RunUntil` runs until a predicate returns true:
*)

(*** do-not-eval ***)
let frames =
    client.RunUntil(
        (fun frame -> frame.FrameNumber > 1000u),
        fun frame -> [])

(**
## Cleanup

Always stop the client when done to clean up the engine process and socket file:
*)

(*** do-not-eval ***)
client.Stop()

// Or use `use` for automatic disposal:
// use client = BarClient.startHeadless ()
// ... client is disposed when it goes out of scope

(**
## Interactive REPL Sessions

The fastest way to explore the engine interactively is with the REPL scripts. These provide
helper functions for stepping frames, querying units, issuing commands, and more.

### Headless REPL

Start a headless engine session (no GUI, fast simulation):
*)

(*** do-not-eval ***)
// From the repo root:
//   dotnet build tests/FSBar.Viz.Tests/
//   dotnet fsi scripts/examples/Repl.fsx

// Or from FSI / MCP server:
//   #load "scripts/examples/Repl.fsx"
//   open Repl

// start ()         — launch headless engine
// step 100         — advance 100 frames
// units ()         — list all tracked units
// economy ()       — show metal/energy
// move 25947 2000f 2000f  — move a unit
// viz ()           — open live visualization window
// stop ()          — shut down

(**
### Graphical REPL

Start a full windowed BAR game with an opponent AI you can watch:
*)

(*** do-not-eval ***)
// From the repo root:
//   dotnet build tests/FSBar.Viz.Tests/
//   DISPLAY=:0 dotnet fsi scripts/examples/ReplGraphical.fsx

// Or from FSI / MCP server:
//   #load "scripts/examples/ReplGraphical.fsx"
//   open ReplGraphical

// start ()         — launch windowed game (BARb opponent, 5x speed)
// step 100         — advance 100 frames
// units ()         — list all tracked units
// move 25947 3000f 3000f  — move a unit
// attack 25947 21640      — attack an enemy
// economy ()       — show metal/energy
// stop ()          — shut down

(**
Both REPL scripts run a 30-frame warmup on `start()` so your commander is immediately tracked.
All helper functions (`step`, `move`, `attack`, `units`, `economy`, etc.) are available after
`open Repl` or `open ReplGraphical`.

## Configuration Options

The `EngineConfig` record controls all session parameters:

| Field | Default | Description |
|-------|---------|-------------|
| `Mode` | `Headless` | `Headless` or `Graphical` |
| `SocketPath` | `/tmp/fsbar-<guid>.sock` | Unix socket path |
| `MapName` | `"Avalanche 3.4"` | BAR map name |
| `GameType` | `"Beyond All Reason test-29871-90f4bc1"` | Game version string |
| `OpponentAI` | `"NullAI"` | Opponent AI name |
| `OurSide` | `"Armada"` | Our faction |
| `OpponentSide` | `"Cortex"` | Opponent faction |
| `TimeoutMs` | `30000` | Connection accept timeout (ms) |
| `EngineBin` | `"spring-headless"` | Engine binary path |
| `GameSpeed` | `100` | Game speed multiplier |
| `ReadTimeoutMs` | `None` | Socket read timeout override |
*)
