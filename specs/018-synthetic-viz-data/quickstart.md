# Quickstart: Synthetic Visualization Test Data

## Build

```bash
dotnet build src/FSBar.SyntheticData/
```

## Run Tests

```bash
dotnet test src/FSBar.SyntheticData.Tests/
```

## Usage from F# Interactive

```fsharp
#r "src/FSBar.SyntheticData/bin/Debug/net10.0/FSBar.SyntheticData.dll"
#r "src/FSBar.SyntheticData/bin/Debug/net10.0/FSBar.Client.dll"

open FSBar.Client
open FSBar.SyntheticData

// Generate a single scene
let scene = Scenes.generate SceneA

// Inspect frame 0
let firstState = scene.Frames.[0]
printfn "Frame %d: %d units, %d enemies" firstState.FrameNumber (Map.count firstState.Units) (Map.count firstState.Enemies)

// Generate all scenes
let allScenes = Scenes.generateAll ()

// Validate a scene
let errors = Validation.validate scene
if errors.IsEmpty then printfn "Scene is valid"
```

## Scene Overview

| Scene | Map Size | Starting Units | Theme |
|-------|----------|----------------|-------|
| A     | 4096x4096 | 1 commander | Early-game buildup |
| B     | 8192x8192 | ~20 friendly, ~15 enemy | Mid-game skirmish |
| C     | 16384x16384 | ~50 friendly, ~40 enemy | Late-game siege |

Each scene produces exactly 300 frames (10 seconds at 30 fps).
