---
title: FSBarV1
---

# FSBarV1

F# client library for orchestrating Beyond All Reason (BAR) AI games via the HighBar V2 proxy.

> **Note:** This project uses [Spec Kit](https://github.com/humankind-project/specify) for specification-driven development.
> Development is guided by a project constitution — see [constitution.md](https://github.com/EHotwagner/FSBarV1/blob/master/.specify/memory/constitution.md) for the governing principles and architectural constraints.

## Quick Start

```fsharp
open FSBar.Client

let client = BarClient.startHeadless ()
let frame = client.Step()
printfn "Frame %d: %d events" frame.FrameNumber frame.Events.Length
client.Stop()
```

## Documentation

- [Getting Started](getting-started.html) — Installation, prerequisites, first game
- [Architecture](architecture.html) — System design and component overview
- [Commands & Events](commands-and-events.html) — 17 command builders and 28 event types
- [Callbacks](callbacks.html) — Mid-frame engine state queries
- [Map Analysis](map-analysis.html) — Terrain, heightmaps, resource analysis
- [Protocol Details](protocol.html) — Wire format and message flow
- [Examples](examples.html) — End-to-end scenarios and AI patterns
- [Test Suite](tests.html) — All 143 tests documented
- [Known Issues](known-issues.html) — Current limitations
- [API Reference](reference/index.html) — Auto-generated API docs
