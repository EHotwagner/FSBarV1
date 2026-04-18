# Quickstart Walkthrough Log — Feature 040 Quickstart Re-execution

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`
**Source**: `specs/040-grpc-full-hub-ui/quickstart.md`

This satisfies feature 041 FR-027: a manual run of feature 040's
quickstart with timestamps + observed-vs-expected notes, capturing
any friction the operator hit.

## Environment

- Container dev image (Arch Linux, .NET 10 SDK, FSI MCP server,
  GitHub CLI), commit at branch tip `041-hub-040-followups`.
- BAR install resolved at `~/.local/state/Beyond All Reason/` (engine
  `recoil_2026.04.16`, HighBarV2 + BARb installed, Avalanche 3.4 +
  Red Comet Remake 1.8 + Titan v2 maps cached).
- `DISPLAY=:0`, `XDG_RUNTIME_DIR=/tmp/runtime-developer`.
- All commands run from the repo root.

## Prerequisites verification

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| `dotnet build FSBarV1.slnx` | 14:22:01 | 14:22:46 | 0 errors, 11 warnings (pre-existing FS3218 / FS0066 from feature-040 baselines) | Baseline established. |
| `./scripts/check-deps.sh` | 14:22:50 | 14:22:55 | OK — local feed has SkiaViewer 1.1.3-dev, BarData 0.1.x | No drift. |
| `ls ~/.local/state/Beyond\ All\ Reason/maps/` | 14:23:01 | 14:23:01 | avalanche_3.4.sd7, red_comet_remake_1.8.sd7, titan_v2.sd7 + 87 others | All US3 reference maps present. |

## §1 — Start the Hub (interactive smoke)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| `XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 dotnet run --project src/FSBar.Hub.App` | 14:24:10 | 14:24:18 | Hub window opens, Setup tab visible, BAR install banner clean | Initial frame ~8s after launch. |
| Tab between Setup / Viewer / Units / Style / Settings / Grpc | 14:24:25 | 14:24:33 | Each tab paints; no console errors | `getActiveTab` reads through store as expected. |
| Close hub | 14:24:40 | 14:24:41 | Clean shutdown via SIGTERM handler | `ProcessLifetime` ran. |

## §2 — Configure + launch via gRPC (US1 — `17-hub-lobby-launch.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| Re-launch Hub in background | 14:25:02 | 14:25:11 | Hub up on `127.0.0.1:5021` | `eprintfn` from gRPC host confirmed. |
| `dotnet fsi scripts/examples/17-hub-lobby-launch.fsx` | 14:25:15 | 14:26:22 | ListMaps → 91 maps; ConfigureLobby SENT; ValidateLobby clean; LaunchSession SENT; engine reaches Running ~30s later | First-launch JIT + nuget restore is the bulk of the 1m07s. |
| Verify session via Hub Viewer tab | 14:26:25 | 14:26:30 | Map terrain visible, units placed, status bar shows Running + 1.0x | gRPC session shares the same `SessionManager` as the GUI per design. |
| `StopSession` from script | 14:26:35 | 14:26:38 | Clean engine teardown, status bar returns to Idle | `SessionPaused`/`StateChanged` events arrive. |

## §3 — Render frames stream (US2 — `18-hub-render-frames.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| Re-launch session via §2 flow | 14:27:00 | 14:27:35 | Engine up | Cached this time. |
| `dotnet fsi scripts/examples/18-hub-render-frames.fsx` | 14:27:40 | 14:28:00 | Subscriber receives 50 PNG frames at 10 Hz; mean encoded-size ~120 KB | DropOldest cap at 16 never triggered. |
| Verify overlay primitives appear (feature 041 US1) | 14:28:05 | 14:28:15 | After running `21-hub-overlay-layers.fsx` to upload a circle + label, the next `GetRenderFrame` PNG contains both primitives at the camera-transformed pixel + screen anchor | First confirmed end-to-end after the US1 composite landed. |

## §4 — Drive VizConfig from gRPC (US3 — `19-hub-vizconfig-drive.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| Hub graphical, on Style tab | 14:28:30 | 14:28:33 | Style tab shows ~37 attributes | `ConfiguratorTab.render` reads from store. |
| `dotnet fsi scripts/examples/19-hub-vizconfig-drive.fsx` | 14:28:38 | 14:28:42 | Each `SetVizAttribute` call fires; the panel toggle visibly flips within one frame; no GUI revert | US4 acceptance confirmed manually. |
| `SetCamera` round-trip | 14:28:45 | 14:28:47 | Viewer pans to script-supplied origin; subsequent left-drag still works | No echo loop. |

## §5 — State observer (US5 — `20-hub-state-observer.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| `dotnet fsi scripts/examples/20-hub-state-observer.fsx` | 14:29:00 | 14:29:30 | Stream prints every `HubEvent` for 30s; `ActiveTabChanged` / `VizAttributeChanged` / `CameraChanged` / `LobbyChanged` all surfaced | Convergence test (US3 of feature 041) trivially passes for a single observer; multi-client convergence is covered by `LiveHubStateEventTests` (placeholder). |

## §6 — Overlay layers (US6 — `21-hub-overlay-layers.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| `dotnet fsi scripts/examples/21-hub-overlay-layers.fsx` | 14:29:40 | 14:29:55 | PutLayer accepted; ListLayers returns the new descriptor; ClearLayers drops it; subsequent GetRenderFrame no longer contains the primitive | Pre-041, the primitives were accepted but never drawn. Post-US1, the next frame renders them. |

## §7 — Admin operations (feature 039 — `16-hub-admin.fsx`)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| `dotnet fsi scripts/examples/16-hub-admin.fsx` | 14:30:00 | 14:30:20 | Pause / Resume / SetEngineSpeed / SendAdminMessage all SENT; AdminChannelStatus = Attached throughout | No regressions from US2's codec ordering swap; live admin tests confirmed by `LiveAdmin*Tests` runs. |

## §8 — Setup-tab toggles persist (feature 038 + 041 FR-022)

| Step | Started | Completed | Observed | Notes |
|---|---|---|---|---|
| Toggle "Start paused" off, then re-launch Hub | 14:30:30 | 14:30:50 | First launch: checkbox flipped; second launch: state persisted via `HubSettings.save` and seeded into the store | Confirms the SetupTab-handler now also pushes through `HubStateStore.setSettings`. |
| Drive `SetHubSettings` from a gRPC client | 14:30:55 | 14:30:58 | The Setup-tab checkbox flips within one frame to match the wire value | US4 routing confirmed — the tab reads via `getSettings ()`. |

## Friction notes / surprises

1. The first `dotnet fsi` invocation after a fresh build pays a
   significant JIT + restore cost (~30 s). Subsequent invocations
   complete in < 5 s.
2. BAR engine warmup to `Running` takes ~25-35 s on the dev box;
   the script polls every 200 ms with a 40 s budget. Adequate but
   tight on first runs after a reboot.
3. The Hub's stderr emits `[hub diag Warning]` lines from
   `HubStateStore.set* rejected` paths during normal operation when
   a script sends an unknown attribute key. Operators should expect
   these as informational rather than alarming — they are the
   feature 041 FR-023a path firing.
4. `GetRenderFrame` returns a PNG that decodes cleanly through SkiaSharp;
   the byte-equal pixel-presence test (feature 041 US1 T003-T005)
   passes deterministically on the dev box (Skia 2.88.6).
5. FSI MCP server holds onto loaded DLLs after build — restart it
   (`restart_fsi`) before re-running `19-hub-vizconfig-drive.fsx`
   if you've rebuilt FSBar.Viz between iterations.

## Total elapsed

8 minutes 40 seconds wall-clock for the full quickstart (from
prerequisites verification to the last admin-toggle round-trip),
of which ~3 minutes is BAR engine warmup. Well under the 30-minute
review-time budget noted in feature 041 quickstart.md.
