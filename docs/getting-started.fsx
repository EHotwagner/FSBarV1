(**
---
title: Getting Started
category: Overview
categoryindex: 1
index: 2
description: Prerequisites, build, and first run.
---
*)

(**
# Getting Started

## Prerequisites

- .NET 10.0 SDK
- Linux (primary dev target). Other platforms work for library/unit tests, but live engine integration is Linux-only.
- For live runs: Beyond All Reason installation with `spring-headless` (or `spring`) and the HighBar V2 proxy (`libSkirmishAI.so`). The Hub can install a bundled proxy for you.

Standard BAR data directory: `~/.local/state/Beyond All Reason`. Engines live under `engine/recoil_<YYYY.MM.DD>/` and are auto-detected by `FSBar.Client.EngineDiscovery`.

## Build

```bash
git clone https://github.com/EHotwagner/FSBarV1.git
cd FSBarV1
dotnet tool restore
dotnet build FSBarV1.slnx
```

## Run the Hub GUI

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

See [Hub GUI](hub.html) for the tab walkthrough.

## Run a headless script

```bash
dotnet build tests/FSBar.Viz.Tests/
dotnet fsi scripts/examples/Repl.fsx
```

`Repl.fsx` loads `FSBar.Client` + `FSBar.Viz` and exposes convenience helpers (`start`, `step`, `units`, `move`, `viz`, `economy`). Use it for interactive exploration.

## Minimal library example
*)

(*** condition: prepare ***)
#r "../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Proto.dll"
#r "../tests/FSBar.Viz.Tests/bin/Debug/net10.0/FSBar.Client.dll"

(*** condition: fsx ***)
#r "nuget: FSBar.Client"

(**
```fsharp
open FSBar.Client
open FSBar.Client.Commands

use client = BarClient.startHeadless ()

// Pull: block for 5 frames, inspecting each one.
client.WaitFrames 5 (fun frame ->
    printfn "Frame %d — %d events" frame.FrameNumber frame.Events.Length)

// Push: subscribe to the observable.
use _ = client.Frames.Subscribe(fun frame ->
    printfn "[obs] Frame %d" frame.FrameNumber)

// Always-current snapshot.
let state = client.GameState
printfn "%d tracked units, metal %.0f" state.TrackedUnits.Count state.Economy.Metal
```

## Container

A fully provisioned dev container (SDK, fsautocomplete, FSI MCP, native libs) is available:

```bash
podman build --build-arg GH_TOKEN=<token> -t fsbar-dev -f container/Containerfile container/
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -p 5020:5020 fsbar-dev
```

See [container/README.md](https://github.com/EHotwagner/FSBarV1/blob/master/container/README.md) for GPU passthrough and X11 forwarding.

## Running tests

```bash
./tests/run-all.sh --category unit   # unit tests, no engine
./tests/run-all.sh                    # unit + live (requires BAR install)
./tests/check-prerequisites.sh        # diagnose BAR + proxy setup
```

## Next

- [Hub GUI](hub.html)
- [Library](library.html)
- [Architecture](architecture.html)
*)
