---
title: FSBarV1
category: Overview
categoryindex: 1
index: 1
---

# FSBarV1

F# toolkit for controlling [Beyond All Reason](https://www.beyondallreason.info/) (BAR) via the HighBar V2 proxy. Provides a typed client, live visualization, synthetic-data generators, and a GUI hub with a gRPC scripting endpoint.

> Uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development — see the [constitution](https://github.com/EHotwagner/FSBarV1/blob/master/.specify/memory/constitution.md).

## Where to start

- **[Getting Started](getting-started.html)** — prerequisites, build, first run
- **[Hub GUI](hub.html)** — the six-tab cockpit app (`FSBar.Hub.App`)
- **[Library](library.html)** — `BarClient`, commands, events, `GameState`
- **[Visualization](visualization.html)** — `FSBar.Viz`, glyph language, style presets
- **[gRPC Scripting](scripting.html)** — remote control, render streaming, overlay layers
- **[Architecture](architecture.html)** — projects, data flow, key modules
- **[Known Issues](known-issues.html)** — current limitations
- **[API Reference](reference/index.html)** — auto-generated from XML doc comments

## Projects

| Project | Role |
|---|---|
| `FSBar.Proto` | Generated protobuf types (`highbar/*.proto`, `hub/scripting.proto`) |
| `FSBar.Client` | Engine lifecycle, wire protocol, commands, events, `GameState`, map analysis |
| `FSBar.SyntheticData` | Deterministic scenes + economy without a running engine |
| `FSBar.Viz` | SkiaSharp/Silk.NET renderer, unit glyphs, live style configurator |
| `FSBar.Hub` | Core hub library (session manager, admin channel, scripting service, state store) |
| `FSBar.Hub.App` | GUI entrypoint binding the core into a SkiaViewer window |

All projects target F# 9 on .NET 10.0.

## Source

https://github.com/EHotwagner/FSBarV1
