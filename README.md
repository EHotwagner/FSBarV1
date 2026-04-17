# FSBarV1

> **Note:** This project uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development.
> Development is guided by a project constitution — see [constitution](.specify/memory/constitution.md) for the
> governing principles and architectural constraints.

FSBarV1 is an F# client library for controlling the [Beyond All Reason](https://www.beyondallreason.info/) (BAR) real-time strategy game engine. It provides a type-safe interface to command units, query game state, and process events through the HighBar V2 proxy over a protobuf-based Unix socket protocol.

## Installation

### From source

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

### Container

A minimal development container is available with all dependencies pre-installed (.NET 10.0, FSI MCP server, fsautocomplete, native libraries):

```bash
# Build the image
podman build --build-arg GH_TOKEN=<your-github-token> \
  -t fsbar-dev -f container/Containerfile container/

# Run with BAR game folder mounted
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -p 5020:5020 \
  fsbar-dev
```

For GPU passthrough, X11 display forwarding, and full setup instructions, see [container/README.md](container/README.md).

## Quick Start

### FSBar Hub (GUI)

`FSBar.Hub.App` is the turn-key graphical cockpit that wraps every
moving piece in the repo — BAR session launch, live Skia-rendered
map + units, style configurator, unit encyclopedia, bundled-proxy
installer, and a localhost gRPC scripting endpoint — behind a
persistent six-tab side bar.

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

Tabs:
- **Setup** — map picker + lobby summary + Launch button
- **Viewer** — live terrain + metal spots + unit glyphs; W/L/C/N
  hotkeys toggle weapon-range / sight / command-queue / full-name
  overlays
- **Units** — every unit in `BarData.AllUnitDefs` (~953), faction
  filter, detail pane with a glyph preview byte-matching the Viewer
- **Style** — live `VizConfig` editor (colors / sizes / strokes /
  overlays); save + load presets under `viz-presets/`
- **Cfg / BAR** — BAR install diagnostics, bundled-proxy health,
  one-click Install / Upgrade / Force reinstall buttons
- **gRPC / API** — scripting endpoint URL (default
  `http://127.0.0.1:5021`) + live connected-client roster

Status bar shows session state + engine-speed slider + pause + end.
External scripting clients (F# `.fsx`, Python, any gRPC-capable
language) can attach to the endpoint and stream live gameplay frames
via the `ScriptingService` contract in
`proto/hub/scripting.proto`.

See [specs/035-central-gui-hub/](specs/035-central-gui-hub/) for the
full spec, data model, and task breakdown.

### Refreshing the bundled proxy (maintainers)

```bash
# After rebuilding HighBarV2 in a sibling checkout
scripts/refresh-bundled-proxy.sh 0.1.17

# Or point at an arbitrary source
scripts/refresh-bundled-proxy.sh 0.1.17 --source /path/to/proxy/build

# Overwrite an existing committed bundle
scripts/refresh-bundled-proxy.sh 0.1.17 --force
```

The script copies `libSkirmishAI.so` + `AIInfo.lua` + `AIOptions.lua`
into `proxy/bundled/<version>/` and atomically rewrites
`proxy/BUNDLED_VERSION`. Users pulling the repo never need to run
it — they pick up the committed bundle on clone.

### Interactive REPL

The fastest way to get started is the interactive REPL script:

```bash
dotnet build tests/FSBar.Viz.Tests/
dotnet fsi scripts/examples/Repl.fsx
```

Or via the [FSI MCP server](https://github.com/EHotwagner/fsi-mcp-server), which gives an AI agent access to the same FSI session. Both the user and the agent share the same REPL state, allowing them to co-control the game — the agent can query units, issue commands, and reason about strategy while the user experiments interactively:

```fsharp
#load "/home/developer/projects/FSBarV1/scripts/examples/Repl.fsx"
open Repl
start ()           // launch headless engine
step 10            // advance 10 frames
units ()           // list all known units
move 42 2000 1000  // move unit 42 to (2000, 1000)
viz ()             // open live visualization
economy ()         // show metal/energy
```

### Library usage

```fsharp
open FSBar.Client
open FSBar.Client.Commands

// Start a headless game session
use client = BarClient.startHeadless ()

// Block and process the next 5 frames
client.WaitFrames 5 (fun frame ->
    printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length)

// Or run 100 frames with a handler that queues commands back to the engine
client.WaitFrames 100 (fun frame ->
    let cmds =
        frame.Events
        |> List.choose (function
            | GameEvent.UnitIdle uid ->
                Some (MoveCommand uid 4096.0f 100.0f 4096.0f)
            | _ -> None)
    if not cmds.IsEmpty then client.SendCommands cmds)

// Alternatively, subscribe to the push-based Frames observable
use _ = client.Frames.Subscribe(fun frame ->
    printfn "[obs] Frame %d" frame.FrameNumber)
```

`client.GameState` exposes an always-current snapshot (tracked units, enemies,
metal/energy) updated each frame — see
[Game State](https://EHotwagner.github.io/FSBarV1/gamestate.html).

## Documentation

Full documentation is available at **https://EHotwagner.github.io/FSBarV1/**

To build and preview locally:

```bash
dotnet tool restore
dotnet fsdocs watch
```

Then open http://localhost:8901.

### Documentation Index

- [Getting Started](https://EHotwagner.github.io/FSBarV1/getting-started.html) — installation, prerequisites, first game
- [Architecture](https://EHotwagner.github.io/FSBarV1/architecture.html) — system design and component overview
- [Commands & Events](https://EHotwagner.github.io/FSBarV1/commands-and-events.html) — unit commands and game events
- [Game State](https://EHotwagner.github.io/FSBarV1/gamestate.html) — observable frames and the `GameState` snapshot
- [Callbacks](https://EHotwagner.github.io/FSBarV1/callbacks.html) — querying game state mid-frame
- [Map Analysis](https://EHotwagner.github.io/FSBarV1/map-analysis.html) — terrain, heightmaps, resource analysis
- [Protocol Details](https://EHotwagner.github.io/FSBarV1/protocol.html) — protobuf communication protocol
- [Examples](https://EHotwagner.github.io/FSBarV1/examples.html) — usage tutorials and AI patterns
- [Visualization](https://EHotwagner.github.io/FSBarV1/viz.html) — `FSBar.Viz` live and preview sessions
- [Synthetic Data](https://EHotwagner.github.io/FSBarV1/synthetic-data.html) — `FSBar.SyntheticData` simulated scenes
- [Test Suite](https://EHotwagner.github.io/FSBarV1/tests.html) — full test inventory
- [Known Issues](https://EHotwagner.github.io/FSBarV1/known-issues.html) — current limitations
- [API Reference](https://EHotwagner.github.io/FSBarV1/reference/index.html) — auto-generated API docs

## Prerequisites (Live Tests)

Live integration tests require:
- Beyond All Reason engine with `spring-headless` binary
- HighBarV2 proxy (`libSkirmishAI.so`) deployed to the engine's AI directory
- BAR game data and maps

For detailed BAR environment setup — directory layout, AI registration, developer mode, and build/install steps — see [docs/bar-info.md](docs/bar-info.md).

The engine version is auto-detected from the installed BAR data directory. To pin a specific version, see [tests/ENGINE-VERSION.md](tests/ENGINE-VERSION.md).

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

- **Type-safe commands** — 17 command builders for unit control (Move, Build, Attack, Guard, Patrol, etc.)
- **28 event types** — discriminated union covering all engine events (UnitCreated, UnitDamaged, EnemyEnterLOS, etc.)
- **26 callback queries** — mid-frame queries for unit position, health, map info, economy, raw map data
- **Observable frame stream** — `client.Frames : IObservable<GameFrame>` plus a synchronous `WaitFrames` helper for REPL use
- **`GameState` snapshot** — always-current tracked units, enemies, and economy, updated each frame
- **Map layers** — `MapGrid`, `MapQuery`, `MapCache` for heightmap, slope, resource, LOS, radar, terrain classification, and passability
- **Engine lifecycle** — automatic engine discovery (`EngineDiscovery`), launch, connection, handshake, and cleanup
- **Headless and graphical modes** — run without display for CI or with a full game window for debugging
- **Live visualization** — `FSBar.Viz` (SkiaViewer + Silk.NET) renders maps, units, events, and HUD overlays live from a running `BarClient`
- **Synthetic data** — `FSBar.SyntheticData` produces deterministic scenes (units, enemies, economy) for offline visualization and validation
- **F# Interactive support** — `scripts/prelude.fsx`, `scripts/examples/Repl.fsx`, and `scripts/examples/ReplGraphical.fsx` for REPL-driven development

## License

This project is licensed under the MIT License — see [LICENSE](LICENSE) for details.
