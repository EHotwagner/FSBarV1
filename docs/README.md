# FSBarV1

> **Note:** This project uses [Spec Kit](https://github.com/specify/speckit) for specification-driven development.
> Development is guided by a project constitution — see [constitution](.specify/memory/constitution.md) for the
> governing principles and architectural constraints.

FSBarV1 is an F# client library for controlling the [Beyond All Reason](https://www.beyondallreason.info/) (BAR) real-time strategy game engine. It provides a type-safe interface to command units, query game state, and process events through the HighBar V2 proxy over a protobuf-based Unix socket protocol.

## Installation

```bash
# Clone and build
git clone https://github.com/EHotwagner/FSBarV1.git
cd FSBarV1
dotnet build
```

The library is also available as a local NuGet package:

```bash
dotnet pack src/FSBar.Client/ -o ~/.local/share/nuget-local/
```

## Quick Start

```fsharp
open FSBar.Client

// Start a headless game session
use client = BarClient.startHeadless ()

// Step through 5 frames
for _ in 1..5 do
    let frame = client.Step()
    printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length

// Or run with a handler that returns commands
client.Run(100, fun frame ->
    frame.Events
    |> List.choose (function
        | GameEvent.UnitIdle uid ->
            Some (Commands.MoveCommand uid 4096.0f 100.0f 4096.0f)
        | _ -> None))
|> ignore
```

## Documentation

- [Architecture](docs/architecture.md) — system design and component overview
- [API Reference](docs/api-reference.md) — complete public API surface
- [Protocol](docs/protocol.md) — protobuf communication protocol
- [Commands & Events](docs/commands-and-events.md) — unit commands and game events
- [Callbacks](docs/callbacks.md) — querying game state mid-frame
- [Examples](docs/examples.md) — usage tutorials and scenarios
- [Test Suite](docs/tests.md) — full test documentation
- [Known Issues](docs/known-issues.md) — current limitations

## Prerequisites (Live Tests)

Live integration tests require:
- Beyond All Reason engine with `spring-headless` binary
- HighBarV2 proxy (`libSkirmishAI.so`) deployed to the engine's AI directory
- BAR game data and maps

Check prerequisites:

```bash
./tests/check-prerequisites.sh
```

## Running Tests

```bash
# Unit tests only (no engine needed)
./tests/run-all.sh --category unit

# All tests (unit + live integration)
./tests/run-all.sh

# Launch graphical game for visual validation
./tests/run-all.sh --graphical
```

## Features

- **Type-safe commands** — 16 command builders for unit control (Move, Build, Attack, Guard, Patrol, etc.)
- **28 event types** — discriminated union covering all engine events (UnitCreated, UnitDamaged, EnemyEnterLOS, etc.)
- **15+ callback queries** — mid-frame queries for unit position, health, map info, economy
- **Engine lifecycle** — automatic engine launch, connection, handshake, and cleanup
- **Headless and graphical modes** — run without display for CI or with full game window for debugging
- **F# Interactive support** — prelude script for REPL-driven development

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
