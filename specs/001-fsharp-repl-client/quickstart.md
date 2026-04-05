# Quickstart: FSBar.Client

**Branch**: `001-fsharp-repl-client` | **Date**: 2026-04-05

## Prerequisites

1. **BAR installed**: The Beyond All Reason AppImage at `~/applications/Beyond-All-Reason-*.AppImage`
2. **HighBarV2 proxy installed**: Run `./scripts/install-ai.sh` in the HighBarV2 repo
3. **BarData NuGet packed**: Run `cd data/bar && dotnet pack -o ~/.local/share/nuget-local/` in HighBarV2
4. **spring-headless on PATH**: Available via the BAR AppImage extraction
5. **.NET 10.0 SDK**: Required for building and FSI

## Build

```bash
cd /home/developer/projects/FSBarV1

# Generate F# protobuf bindings and build
dotnet build src/FSBar.Proto/
dotnet build src/FSBar.Client/

# Pack for FSI use
dotnet pack src/FSBar.Client/ -o ~/.local/share/nuget-local/
```

## Run Tests

```bash
# Unit tests (no engine needed)
dotnet test src/FSBar.Client.Tests/ --filter "Category=Unit"

# Integration tests (needs headless engine)
dotnet test src/FSBar.Client.Tests/ --filter "Category=Integration"
```

## Use from FSI

```fsharp
// Load the prelude (handles all assembly references)
#load "scripts/prelude.fsx"

// Start a headless game with defaults
let client = BarClient.startHeadless()
// Output: "Listening on /tmp/fsbar-a1b2c3d4.sock..."
//         "Engine started (PID 12345)"
//         "Proxy connected. Handshake OK (protocol v1)"

// Receive one frame and inspect events
let frame = client.Step()
printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length

// Run 60 frames (1 second at 60fps), moving the commander
client.Run(60, fun frame ->
    frame.Events
    |> List.choose (fun ev ->
        match ev with
        | GameEvent.UnitIdle unitId ->
            Some (Commands.MoveCommand unitId 4096.0f 100.0f 4096.0f)
        | _ -> None
    )
) |> ignore

// Query economy
let metal = Callbacks.getEconomyCurrent client 0
let energy = Callbacks.getEconomyCurrent client 1
printfn "Metal: %.0f  Energy: %.0f" metal energy

// Query BarData (offline unit data)
let builders =
    BarData.AllUnits.all
    |> List.filter (fun u -> u.isBuilder && not u.canFly)
printfn "Ground builders: %d" builders.Length

// Reset game state (destroy all non-initial units, reset resources)
client.Reset()

// Clean shutdown
client.Stop()
// Output: "Engine stopped (PID 12345)"
//         "Socket cleaned up"
```

## Graphical Mode

```fsharp
#load "scripts/prelude.fsx"

// Start with the full BAR window
let client = BarClient.startGraphical()
// BAR window opens — you'll see the game while controlling it from REPL

client.Run(300, fun frame ->
    // Your AI logic here
    []
) |> ignore

client.Stop()
```

## Custom Configuration

```fsharp
#load "scripts/prelude.fsx"

let config =
    { BarClient.defaultConfig() with
        MapName = "Comet Catcher Remake"
        OurSide = "Cortex"
        OpponentAI = "NullAI"
        TimeoutMs = 60000
        GameSpeed = 50 }

let client = BarClient.create config
client.Start()
// ...
client.Stop()
```
