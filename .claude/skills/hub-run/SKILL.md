---
name: "hub-run"
description: "Launch the FSBar Hub GUI (FSBar.Hub.App) with the required display env vars, and document the FSBAR_HUB_* env-var matrix for CI smoke tests, initial tab, auto-launch, screenshotting, and bundled-proxy overrides."
user-invocable: true
---

## Launch

```bash
XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 \
  dotnet run --project src/FSBar.Hub.App
```

`XDG_RUNTIME_DIR` is required for GLFW; `DISPLAY=:0` for X.

## Environment variables

| Var | Effect |
|-----|--------|
| `FSBAR_HUB_SCREENSHOT_DIR` | Take a screenshot after settle delay, then exit cleanly |
| `FSBAR_HUB_AUTO_LAUNCH=1` | Fire SetupTab.Launch immediately (needs `FSBAR_HUB_SCREENSHOT_DIR`) |
| `FSBAR_HUB_SCREENSHOT_WAIT_MS=N` | Extra delay before screenshot |
| `FSBAR_HUB_INITIAL_TAB=Setup\|Viewer\|Units\|Style\|Settings\|Grpc` | Land on a specific tab (equivalent to `SetActiveTab` RPC) |
| `FSBAR_HUB_ENCYCLOPEDIA_SELECT=<name>` | Pre-select a unit in the Units tab (equivalent to `SelectUnit` RPC) |
| `FSBAR_HUB_BUNDLED_PROXY_DIR=/path` | Override bundled-proxy root for dev runs |

## Graphical mode

Always run the graphical engine windowed (never fullscreen). `EngineLauncher` writes `Fullscreen=0` to `springsettings.cfg` in each session dir automatically.
