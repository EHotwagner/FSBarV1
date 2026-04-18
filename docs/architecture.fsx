(**
---
title: Architecture
category: Design
categoryindex: 4
index: 1
description: Projects, data flow, and key modules.
---
*)

(**
# Architecture

## Project graph

```
                   FSBar.Proto
                        │
       ┌────────────────┼────────────────┐
       ▼                ▼                ▼
 FSBar.Client   FSBar.SyntheticData      │
       │                │                │
       └───────┬────────┘                │
               ▼                         │
           FSBar.Viz                     │
               │                         │
               └──────┬──────────────────┘
                      ▼
                  FSBar.Hub
                      │
                      ▼
                FSBar.Hub.App
```

- `FSBar.Proto` — generated protobuf types. No dependencies.
- `FSBar.Client` — wire protocol, engine lifecycle, `GameState`, map analysis. Depends on Proto.
- `FSBar.SyntheticData` — deterministic scenes + economy. Depends on Client types only.
- `FSBar.Viz` — SkiaSharp + Silk.NET renderer and style configurator. Depends on Client + SyntheticData.
- `FSBar.Hub` — session manager, admin channel host, scripting service, state store, headless renderer. Depends on all of the above.
- `FSBar.Hub.App` — GUI entrypoint; binds the core into a SkiaViewer window.

## Data flow (live game)

```
spring-headless ──► HighBar V2 proxy ──► Unix socket ──► FSBar.Client.Connection
                                                             │ decodes protobuf
                                                             ▼
                                                      GameEvent stream
                                                             │ fold
                                                             ▼
                                                        GameState
                                                             │
                              ┌──────────────┬───────────────┼─────────────────┐
                              ▼              ▼               ▼                 ▼
                     client.Frames    client.WaitFrames   FSBar.Viz       FSBar.Hub
                     (IObservable)   (handler callback)  (live render)   (session + gRPC)
```

Outbound: scripts and the Hub push `AICommand` lists via `client.SendCommands`, which the proxy relays to the engine.

## Hub internals

The Hub is deliberately split so downstream scripting tools can depend on just the wire contract:

- `FSBar.Proto` — just the `.proto` contract.
- `FSBar.Hub` — packable core. No GUI deps.
- `FSBar.Hub.App` — the SkiaViewer-bound GUI.

Key modules:

| Module | Role |
|---|---|
| `SessionManager` | Spawns the engine, attaches the admin channel, exposes pause/speed/force-end. |
| `AdminChannelHost` | Wraps one `AdminChannel`, coalesces rapid same-kind submits (100 ms window), tracks status (`Attached` / `Unavailable` / `Lost`). |
| `HubStateStore` | Atomic LWW store for active tab, `VizConfig`, camera, lobby, encyclopedia, preset cache, settings. Every mutation publishes a `HubEvent`. |
| `HeadlessRenderer` | Off-screen Viewer render pipeline, per-subscriber bounded channel, raster `SKSurface` (GPU unavailable here). |
| `OverlayLayerStore` | Per-client, name-keyed overlay layers with cap enforcement. Auto-cleans on client detach. |
| `HubLog` | In-process log-emit surface. O(1) when no subscriber is attached. |
| `ScriptingHub` + `ScriptingService` | gRPC service backed by the stores above. |
| `CorrelationId` / `DispatchTracer` | gRPC interceptors for correlation IDs + per-RPC entry/completion log entries. |

## Map analysis cache

Static per-map analysis is committed under `bots/trainer/map-cache/*.json` — authoritative contract in `src/FSBar.Client/MapCacheFile.fsi`. Each file carries `schemaVersion`, `codeVersion`, parameters, source identity, and gzip+base64 blobs for heightmap / slope / resource / dimensions. Target size: ≤1.5 MB/map, ≤15 MB total. Regenerate via `bots/trainer/map-cache/refresh-all.sh` after bumping `codeVersion`.

## Surface-area discipline

Public API is gated exclusively by `.fsi` signature files (F# `private`/`internal` modifiers are banned in non-generated source per constitution §II). Baselines live under `tests/*/Baselines/`; run with `SURFACE_AREA_UPDATE=1` to regenerate after an intentional `.fsi` change. Tests use `tests/Common/SurfaceAreaHelper.fs`.

## Testing layout

```
tests/
├── FSBar.Client.Tests/           # Unit tests (protocol, gamestate, map analysis)
├── FSBar.SyntheticData.Tests/    # Unit tests (synthetic scenes)
├── FSBar.Viz.Tests/              # Unit tests + snapshot baselines
├── FSBar.Hub.Tests/              # Unit tests (core hub library)
├── FSBar.LiveTests/              # Real BAR engine integration
├── FSBar.Hub.LiveTests/          # Hub + real engine (admin channel, log stream)
└── FSBar.Hub.GrpcTests/          # gRPC client against a live hub
```

Live tests are tagged with xUnit `Trait("Category", ...)` — filter via e.g. `dotnet test --filter "Category=AdminChannel"`.

## Bundled proxy

`proxy/bundled/<version>/` contains `libSkirmishAI.so`, `AIInfo.lua`, `AIOptions.lua`. `proxy/BUNDLED_VERSION` pins the active bundle. Users inherit the committed bundle on clone; maintainers refresh via `scripts/refresh-bundled-proxy.sh <version>`.
*)
