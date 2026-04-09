# Quickstart: Idiomatic F# Streams Refactor

**Branch**: `016-idiomatic-fsharp-streams` | **Date**: 2026-04-09

## Before (current API)

```fsharp
use client = BarClient.startHeadless()

// Step-based handler: receive frame, return commands
let frames =
    client.Run(100, fun frame ->
        let myTeam = Callbacks.getMyTeam client.Stream
        // ... decide based on frame.Events ...
        [ Commands.MoveCommand(unitId, x, y, z) ]
    )

// Or conditional loop
let frames =
    client.RunUntil(
        (fun f -> f.FrameNumber > 1000u),
        fun frame -> []
    )
```

## After (new stream API)

```fsharp
use session = BarClient.startHeadless()

// Iterate frames as a standard F# sequence
for frame in session.Frames do
    let myTeam = Callbacks.getMyTeam session.Stream

    // Process events with standard seq operations
    let newUnits =
        frame.Events
        |> List.choose (function
            | GameEvent.UnitCreated ev -> Some ev
            | _ -> None)

    // Send commands separately
    session.SendCommands [
        Commands.MoveCommand(unitId, x, y, z)
    ]

// Stream terminates when engine disconnects
// Commands after session end raise an error
```

## What changes

| Aspect | Before | After |
|--------|--------|-------|
| Frame iteration | `StepWith(handler)`, `Run(n, handler)`, `RunUntil(pred, handler)` | `session.Frames` (seq<GameFrame>) |
| Command submission | Return value from handler function | `session.SendCommands(cmds)` |
| No-op frames | `Step()` or handler returning `[]` | Just iterate without calling SendCommands |
| Callbacks | `Callbacks.getX client.Stream` | `Callbacks.getX session.Stream` (unchanged) |
| Private qualifiers | Redundant `private` in .fs files | Removed; .fsi files control visibility |

## Build & Test

```bash
dotnet build src/FSBar.Client/
dotnet test src/FSBar.Client.Tests/
```
