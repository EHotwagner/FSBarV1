# Coverage Audit — Hub GUI Action ↔ gRPC RPC Mapping

**Date**: 2026-04-18 · **Branch**: `041-hub-040-followups`
**Source**: hand-curated from `src/FSBar.Hub.App/Tabs/*.fs` handlers and
`src/FSBar.Hub/ScriptingHub.fs` RPC overrides.

This audit satisfies feature 041 FR-024 + SC-008. Every user-facing
action surfaced by a tab is enumerated below with the corresponding
`fsbar.hub.scripting.v1.ScriptingService/<Method>` RPC. Actions tagged
`no RPC — intentional` are local presentation state with no business
on the wire. Actions tagged `no RPC — gap` are candidates for a future
feature.

## Setup tab — `Tabs/SetupTab.fs`

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Click "Map list row" → SelectMap | `SetupTab.fs:handleMouse SelectMap` | `ConfigureLobby` (lobby with new MapName) | FR-014 / 040 FR-001 | mapped |
| Scroll map list | `SetupTab.fs:handleScroll ScrollMapList` | n/a | — | no RPC — intentional (transient scroll) |
| Click "Launch" | `SetupTab.fs:handleMouse Launch` | `LaunchSession` | 040 FR-002 | mapped |
| Toggle "Start paused" checkbox | `SetupTab.fs:handleMouse ToggleStartPaused` | `SetHubSettings` (StartPausedDefault) | 041 FR-022 | mapped |
| Toggle "Launch graphical viewer" | `SetupTab.fs:handleMouse ToggleGraphicalEngine` | `SetHubSettings` (LaunchGraphicalViewerDefault) + `ConfigureLobby` (lobby.LaunchGraphicalViewer) | 041 FR-022 | mapped |

## Viewer tab — `Tabs/ViewerTab.fs`

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Click ⏸ pause button | `ViewerTab.fs:handleMouse AdminPause` | `Pause` | 039 FR-002 | mapped |
| Click ⏹ force-end button | `ViewerTab.fs:handleMouse AdminForceEnd` | `ForceEndMatch` | 039 FR-005 | mapped |
| Click [0.5x/1x/2x/5x/10x] preset | `ViewerTab.fs:handleMouse AdminSpeed` | `SetEngineSpeed` | 039 FR-003 | mapped |
| Submit admin message text | `ViewerTab.fs:handleMouse AdminMessage` | `SendAdminMessage` | 039 FR-004 | mapped |
| Left-drag pan camera | `Program.fs:MouseMove` | `SetCamera` (echo via `pushCameraToStore`) | 040 FR-009 | mapped |
| Mouse-wheel zoom | `Program.fs:MouseScroll` | `SetCamera` (echo) | 040 FR-009 | mapped |
| `R` key — reset camera | `Program.fs:KeyDown Key.R` | `SetCamera` (echo) | 040 FR-009 | mapped |
| `W` key — toggle weapon ranges | `Program.fs:KeyDown Key.W` | `ToggleOverlay(WeaponRanges)` (via store) | 040 FR-008 | mapped |
| `L` key — toggle sight ranges | `Program.fs:KeyDown Key.L` | `ToggleOverlay(SightRanges)` (via store) | 040 FR-008 | mapped |
| `C` key — toggle command queue | `Program.fs:KeyDown Key.C` | `ToggleOverlay(CommandQueue)` (via store) | 040 FR-008 | mapped |
| `N` key — toggle full names | `Program.fs:KeyDown Key.N` | `ToggleOverlay(FullNames)` (via store) | 040 FR-008 | mapped |

## Encyclopedia / Units tab — `Tabs/EncyclopediaTab.fs`

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Click faction chip | `EncyclopediaTab.fs:handleMouse chip-hit` | `SetEncyclopedia`-equivalent (no first-class RPC — selection routes via `SelectUnit` for SelectedDefId; FactionFilter is currently GUI-side only) | 041 FR-019 | no RPC — gap (faction-filter wire-surface deferred) |
| Click unit row | `EncyclopediaTab.fs:handleMouse list-hit` | `SelectUnit` (DefId) | 041 FR-019 / 040 FR-005 | mapped |
| Mouse-wheel scroll list | `EncyclopediaTab.fs:handleScroll` | n/a | — | no RPC — intentional (transient scroll) |

## Configurator / Style tab — `Tabs/ConfiguratorTab.fs`

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Slider drag / color cycle / toggle | `ConfiguratorTab.fs:handleInput UpdatedConfig` | `SetVizConfig` (whole-config write) | 041 FR-018 | mapped |
| Per-attribute mutation (future) | n/a | `SetVizAttribute` (key + value) | 040 FR-006 | mapped |
| Click "Save preset" | `ConfiguratorTab.fs:handleInput SavePreset` | `SavePreset` | 040 FR-007 | mapped |
| Click "Load preset" row | `ConfiguratorTab.fs:handleInput LoadPreset` | `LoadPreset` | 040 FR-007 | mapped |
| Click "Delete preset" | `ConfiguratorTab.fs:handleInput DeletePreset` | `DeletePreset` | 040 FR-007 | mapped |
| Click "Reset defaults" | `ConfiguratorTab.fs:handleInput ResetDefaults` | `SetVizConfig(VizDefaults.defaultConfig)` | 040 FR-007 | mapped |
| Toast dismissal (auto-fade after action) | n/a | n/a | — | no RPC — intentional (UI artifact) |

## Settings / Cfg tab — `Tabs/SettingsTab.fs`

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Click "Install / Upgrade" | `SettingsTab.fs:handleMouse InstallProxy` | `InstallProxy` | 035 FR-010 | mapped |
| Click "Force reinstall" | `SettingsTab.fs:handleMouse ForceReinstallProxy` | `InstallProxy` (`Force = true`) | 035 FR-010 | mapped |
| Click "Refresh status" | `SettingsTab.fs:handleMouse RefreshStatus` | `RefreshProxyStatus` | 035 FR-010 | mapped |

## Grpc tab — `Tabs/GrpcTab.fs`

The Grpc tab is a read-only diagnostic surface (lists every RPC + the
service URL) and exposes no input handlers. No RPC mapping needed.

## Chrome (TabBar / StatusBar)

| Action label | Code site | RPC | FR ref | Status |
|---|---|---|---|---|
| Click tab in left rail | `Program.fs:MouseDown TabBar.handleMouse` | `SetActiveTab` | 040 FR-014 | mapped |
| Click ⏸/▶ in status bar | `Program.fs:MouseDown StatusBar.TogglePause` | `Pause`/`Resume` | 039 FR-002 | mapped |
| Click speed preset in status bar | `Program.fs:MouseDown StatusBar.SetSpeed` | `SetEngineSpeed` | 039 FR-003 | mapped |
| Click ⏹ in status bar | `Program.fs:MouseDown StatusBar.EndSession` | `StopSession` | 040 FR-002 | mapped |

## Summary

- **mapped**: 28 actions cover every Setup / Viewer / Configurator /
  Settings / chrome action, plus the keyboard overlay-toggle row and
  encyclopedia row-select.
- **no RPC — intentional**: 3 actions (scroll positions, toast
  dismissal) — genuinely transient view state with no remote-client
  use case.
- **no RPC — gap**: 1 action (Encyclopedia faction-chip filter) — the
  store carries the filter (`HubState.Encyclopedia.FactionFilter`)
  but no RPC mutates it directly today. Logged for follow-up; the
  per-tab `SelectUnit` RPC continues to work because it bypasses the
  filter.

SC-008 acceptance: every user-facing tab action either has a mapped
RPC or is explicitly categorized above. ✅
