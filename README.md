# FSBarV1

> Uses [Spec Kit](https://github.com/github/spec-kit). See [constitution](.specify/memory/constitution.md) for governing principles.

F# toolkit for controlling [Beyond All Reason](https://www.beyondallreason.info/) (BAR) through the HighBar V2 proxy. Ships a typed client, live map/unit visualization, synthetic data generators, and a GUI hub with a gRPC scripting endpoint.

## Packages

| Project | Purpose |
|---|---|
| `FSBar.Proto` | Generated protobuf types |
| `FSBar.Client` | Engine lifecycle, commands, events, `GameState`, map analysis |
| `FSBar.Viz` | SkiaSharp/Silk.NET live renderer, glyph language, style configurator |
| `FSBar.SyntheticData` | Deterministic scenes + economy without a running engine |
| `FSBar.Hub` / `FSBar.Hub.App` | Core hub library + GUI app (6-tab cockpit, gRPC scripting) |

## Quick Start

### Hub (GUI)

```bash
dotnet build FSBarV1.slnx
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

Tabs: **Setup** (lobby + launch), **Viewer** (live map/units), **Units** (encyclopedia), **Style** (VizConfig editor + presets), **Cfg/BAR** (install + proxy), **gRPC** (scripting endpoint, default `127.0.0.1:5021`).

### Library

```fsharp
open FSBar.Client
open FSBar.Client.Commands

use client = BarClient.startHeadless ()

client.WaitFrames 100 (fun frame ->
    frame.Events
    |> List.choose (function
        | GameEvent.UnitIdle uid -> Some (MoveCommand uid 4096.0f 100.0f 4096.0f)
        | _ -> None)
    |> function [] -> () | cmds -> client.SendCommands cmds)
```

`client.GameState` exposes an always-current snapshot. `client.Frames : IObservable<GameFrame>` is the push-based equivalent.

### REPL

```bash
dotnet build tests/FSBar.Viz.Tests/
dotnet fsi scripts/examples/Repl.fsx
```

### Container

```bash
podman build --build-arg GH_TOKEN=<token> -t fsbar-dev -f container/Containerfile container/
podman run -it --rm \
  -v "<path-to-BAR>:/home/developer/.local/state/Beyond All Reason" \
  -p 5020:5020 fsbar-dev
```

See [container/README.md](container/README.md) for GPU/X11 passthrough.

## Documentation

Full docs: **https://EHotwagner.github.io/FSBarV1/**

Build locally:

```bash
dotnet tool restore
dotnet fsdocs watch   # http://localhost:8901
```

- [Getting Started](https://EHotwagner.github.io/FSBarV1/getting-started.html)
- [Hub GUI](https://EHotwagner.github.io/FSBarV1/hub.html)
- [Library](https://EHotwagner.github.io/FSBarV1/library.html)
- [Visualization](https://EHotwagner.github.io/FSBarV1/visualization.html)
- [gRPC Scripting](https://EHotwagner.github.io/FSBarV1/scripting.html)
- [Architecture](https://EHotwagner.github.io/FSBarV1/architecture.html)
- [Known Issues](https://EHotwagner.github.io/FSBarV1/known-issues.html)
- [API Reference](https://EHotwagner.github.io/FSBarV1/reference/index.html)

## Tests

```bash
./tests/run-all.sh --category unit      # unit only
./tests/run-all.sh                       # unit + live
./tests/run-all.sh --graphical           # graphical engine

./tests/check-prerequisites.sh           # verify BAR + proxy
```

Live tests auto-detect the engine under `~/.local/state/Beyond All Reason/engine/recoil_*`. Pin via `FSBAR_TEST_ENGINE` (preferred), `HIGHBAR_TEST_ENGINE` (legacy alias), or `tests/engine-version.json`.

## License

MIT — see [LICENSE](LICENSE).
