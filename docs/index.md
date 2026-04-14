---
title: FSBarV1
---

# FSBarV1

F# client library for orchestrating Beyond All Reason (BAR) AI games via the HighBar V2 proxy.

> **Note:** This project uses [Spec Kit](https://github.com/github/spec-kit) for specification-driven development.
> Development is guided by a project constitution — see [constitution.md](https://github.com/EHotwagner/FSBarV1/blob/master/.specify/memory/constitution.md) for the governing principles and architectural constraints.

## Quick Start

```fsharp
open FSBar.Client

use client = BarClient.startHeadless ()

// Block for N frames via the handler-style API
client.WaitFrames 5 (fun frame ->
    printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length)

// Or subscribe to the push-based observable
use _ = client.Frames.Subscribe(fun frame ->
    printfn "Frame %d" frame.FrameNumber)
```

## Documentation

### Core library (`FSBar.Client`)

- [Getting Started](getting-started.html) — Installation, prerequisites, first game
- [Architecture](architecture.html) — System design and component overview
- [Commands & Events](commands-and-events.html) — 17 command builders and 28 event types
- [Game State](gamestate.html) — Observable frames and the `GameState` snapshot
- [Callbacks](callbacks.html) — Mid-frame engine state queries
- [Map Analysis](map-analysis.html) — Terrain, heightmaps, resource analysis
- [Tactical Map Primitives](tactical-primitives.html) — Pathing, Chokepoints, BasePlan, WallIn, SmfParser (feature 024)
- [Protocol Details](protocol.html) — Wire format and message flow
- [Examples](examples.html) — End-to-end scenarios and AI patterns

### Visualization (`FSBar.Viz`)

- [Visualization](viz.html) — Live and preview sessions, layer rendering, scene API

### Synthetic data (`FSBar.SyntheticData`)

- [Synthetic Data](synthetic-data.html) — Simulated scenes for offline viz tests

### Reference

- [Test Suite](tests.html) — Full test inventory
- [Known Issues](known-issues.html) — Current limitations
- [API Reference](reference/index.html) — Auto-generated API docs
