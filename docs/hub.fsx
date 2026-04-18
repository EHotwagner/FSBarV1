(**
---
title: Hub GUI
category: Tutorials
categoryindex: 2
index: 1
description: The FSBar.Hub.App cockpit — tabs, admin channel, settings, scripting endpoint.
---
*)

(**
# Hub GUI

`FSBar.Hub.App` is the graphical cockpit wrapping every moving piece in the repo: lobby + session launch, live Skia-rendered map/units, style configurator, unit encyclopedia, bundled-proxy installer, and a localhost gRPC scripting endpoint.

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

## Tabs

| Tab | What it does |
|---|---|
| **Setup** | Map picker + lobby summary + Launch button. Checkboxes for *Start paused* and *Graphical viewer* persist in settings. |
| **Viewer** | Live terrain, metal spots, unit glyphs. Hotkeys: `W` weapon ranges · `L` sight · `C` command queue · `N` full names · `P` style panel. Top-right admin toolbar: pause/resume, engine-speed presets, force end, admin chat. |
| **Units** | Every unit in `BarData.AllUnitDefs` (~953) with faction filter and a detail pane whose glyph preview byte-matches the Viewer. |
| **Style** | Live `VizConfig` editor — colors, sizes, strokes, overlays. Save/load presets under `viz-presets/`. |
| **Cfg / BAR** | BAR install diagnostics, bundled-proxy health, one-click Install / Upgrade / Force reinstall. |
| **gRPC / API** | Scripting endpoint URL (default `http://127.0.0.1:5021`), connected-client roster, log-stream controls. |

## Admin channel

The Hub opens a loopback UDP autohost channel at every launch so pause / resume / engine-speed / force-end / admin message become real engine operations instead of hub-side cosmetic flags. Wire contract: `specs/039-hub-admin-channel/contracts/autohost-wire.md`.

`SessionManager` exposes typed admin methods — each returns an `AdminChannelHost.SubmitOutcome` (`Sent` / `Coalesced n` / `Rejected reason`):

```fsharp
sm.Pause ()
sm.Resume ()
sm.SetEngineSpeed 2.0f
sm.ForceEnd ()
sm.SendAdminMessage "hello"
```

Status transitions (`Attached` / `Unavailable` / `Lost`) are broadcast as `HubEvent.AdminChannelStatusChanged`.

## Settings

Persisted at `$XDG_CONFIG_HOME/fsbar-hub/settings.json`. Notable fields:

- `StartPausedDefault` (bool, default `true`)
- `LaunchGraphicalViewerDefault` (bool, default `false`)
- `MaxRenderFrameSubscribers` (int, default 8, range 1–32)
- `MaxLogStreamSubscribers` (int, default 8, range 1–32)

Schema version is bumped additively on every field addition; older files are upgraded in-place.

## State routing

Every tab reads authoritative state through `HubStateStore.current store` and writes via typed mutators (`setVizConfig`, `setVizAttribute`, `setActiveTab`, `setEncyclopedia`, `setSettings`). Rejected mutations emit a single `HubEvent.DiagnosticsLine Warning` — callers just re-read `current` and move on.

## gRPC endpoint

All hub actions are mirrored on `fsbar.hub.scripting.v1` at `127.0.0.1:5021`. The Viewer tab's rendered output is available as PNG/JPEG via `StreamRenderFrames`. Complete examples under `scripts/examples/17-*.fsx` through `22-*.fsx`. See [gRPC Scripting](scripting.html).

## Bundled proxy

Users get a working proxy on clone — the Hub uses `proxy/bundled/<version>/` and `proxy/BUNDLED_VERSION` automatically. Maintainers refresh it via:

```bash
scripts/refresh-bundled-proxy.sh 0.1.17
```

## CI smoke envs

| Var | Effect |
|---|---|
| `FSBAR_HUB_SCREENSHOT_DIR` | Take a screenshot after a settle delay and exit |
| `FSBAR_HUB_AUTO_LAUNCH=1` | Fire Setup.Launch immediately |
| `FSBAR_HUB_SCREENSHOT_WAIT_MS=N` | Extra delay before screenshot |
| `FSBAR_HUB_INITIAL_TAB=Viewer|Units|Style|Settings|Grpc|Setup` | Land on a specific tab |
| `FSBAR_HUB_ENCYCLOPEDIA_SELECT=<name>` | Pre-select a unit on the Units tab |
| `FSBAR_HUB_BUNDLED_PROXY_DIR=<path>` | Override bundled-proxy root for dev runs |
*)
