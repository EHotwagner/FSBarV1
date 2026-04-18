(**
---
title: gRPC Scripting
category: Tutorials
categoryindex: 2
index: 4
description: Remote-controlling the Hub via fsbar.hub.scripting.v1.
---
*)

(**
# gRPC Scripting

The Hub exposes every GUI action on `fsbar.hub.scripting.v1` at `http://127.0.0.1:5021` (default). Any gRPC-capable language can attach — F# `.fsx`, Python, Go, TypeScript.

Contract: [`proto/hub/scripting.proto`](https://github.com/EHotwagner/FSBarV1/blob/master/proto/hub/scripting.proto).

## RPCs by user story

| Story | RPCs |
|---|---|
| **Session** | `ConfigureLobby`, `ListMaps`, `ValidateLobby`, `LaunchSession`, `StopSession` |
| **Admin** | `Pause`, `Resume`, `SetEngineSpeed`, `ForceEndMatch`, `SendAdminMessage` |
| **Rendering** | `StreamRenderFrames`, `GetRenderFrame` |
| **Viz / camera** | `SetVizConfig`, `SetVizAttribute`, `ToggleOverlay`, `SetCamera`, `SetActiveTab` |
| **Presets / encyc / settings / proxy** | `ListPresets`, `SavePreset`, `LoadPreset`, `DeletePreset`, `ListUnits`, `SelectUnit`, `GetHubSettings`, `SetHubSettings`, `InstallProxy`, `RefreshProxyStatus` |
| **State observation** | `GetHubState`, `StreamHubStateEvents`, `StreamLogEntries` |
| **Client overlays** | `PutLayer`, `DeleteLayer`, `ListLayers`, `ClearLayers` |

## Render streaming

`StreamRenderFrames` delivers the Viewer-tab render as PNG/JPEG bytes over a server-streaming RPC. Each subscriber gets a `BoundedChannel<RenderFrameMessage>` (capacity 16, `DropOldest`). Cap: `HubSettings.MaxRenderFrameSubscribers` (default 8).

## Log streaming

`StreamLogEntries` surfaces Hub-internal diagnostics (session manager, admin channel, scripting RPC dispatch, proxy install, preset persistence, lobby validation, …). Per-subscriber bounded buffer (256 entries, drop-oldest, dropped count carried on the next entry). Categories are a closed DU mirroring the proto enum; default floor is `Info`.

Shipped filter presets: `session-lifecycle`, `admin-channel`, `scripting-wire`.

Correlation IDs flow via the `x-fsbar-correlation-id` request-metadata header — the server generates one if absent and echoes it on the response trailer. Every RPC emits an entry + completion with elapsed ms on the `ScriptingHub` category.

## Overlay layers

Clients can push their own overlay primitives (line, polyline, polygon, rectangle, circle, path, text, image) in either `World` or `Screen` coordinate space. Caps per `OverlayLayerStore`:

- 16 layers per client
- 500 primitives per layer
- 1 MiB per push
- 256 KiB per image / 2048² dims
- 4 KiB per text element

Layers auto-clean on client detach.

## Walkthroughs

End-to-end `.fsx` examples live under `scripts/examples/`:

| Script | Covers |
|---|---|
| `16-hub-admin.fsx` | Admin channel (pause / resume / speed / force-end / message) |
| `17-hub-lobby-launch.fsx` | Session orchestration |
| `18-hub-render-frames.fsx` | Render streaming |
| `19-hub-vizconfig-drive.fsx` | VizConfig + camera + active tab |
| `20-hub-state-observer.fsx` | `StreamHubStateEvents` |
| `21-hub-overlay-layers.fsx` | Client overlay primitives |
| `22-hub-log-stream.fsx` | `StreamLogEntries` filters + correlation IDs |

Run any example with `dotnet fsi scripts/examples/<name>.fsx` after starting the Hub.

## Regenerating the proto

`FSBar.Proto` commits generated files under `src/FSBar.Proto/Generated/`. A plain `dotnet build` does not need the plugin.

To regenerate after editing `.proto` files, install the patched `protoc-gen-fsgrpc` plugin via the helper in the sibling `fsGRPCSkills` repo:

```bash
~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh
cd proto && buf generate
```

Verify `dotnet build FSBarV1.slnx` still succeeds and that the committed `highbar/*.gen.fs` files weren't gratuitously rewritten.
*)
