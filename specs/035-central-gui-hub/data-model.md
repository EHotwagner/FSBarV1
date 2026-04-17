# Phase 1 Data Model — Central GUI Hub App

**Feature**: 035-central-gui-hub
**Date**: 2026-04-17

This document defines every entity introduced by the hub, the F# record / DU
shape for each, and validation rules. Wire-format types (protobuf) are listed
where they show up on the gRPC contract; the canonical source is
`contracts/hub/scripting.proto`.

---

## Entity catalogue

| # | Entity | Module | Lifetime | Persistence |
|---|--------|--------|----------|-------------|
| 1 | `BarInstall` | `FSBar.Hub.BarInstall` | startup-cached | derived from disk + settings |
| 2 | `EngineVersionEntry` | `FSBar.Hub.BarInstall` | startup-cached | derived from disk |
| 3 | `BundledProxyInfo` | `FSBar.Hub.BundledProxy` | startup-cached | `proxy/bundled/<v>/` + `proxy/BUNDLED_VERSION` |
| 4 | `ProxyInstallStatus` | `FSBar.Hub.ProxyInstaller` | re-checked on demand | derived from disk |
| 5 | `HubSettings` | `FSBar.Hub.HubSettings` | persistent | `$XDG_CONFIG_HOME/fsbar-hub/settings.json` |
| 6 | `LobbyConfig` (`SessionConfig`) | `FSBar.Hub.LobbyConfig` | session-scoped + last-used persisted | persisted as part of `HubSettings.lastLobby` |
| 7 | `Team`, `Seat`, `SeatKind`, `GameMode` | `FSBar.Hub.LobbyConfig` | nested in 6 | — |
| 8 | `RunningSession` | `FSBar.Hub.SessionManager` | runtime only | — |
| 9 | `SessionState` | `FSBar.Hub.SessionManager` | runtime | event-bus payload |
| 10 | `HubEvent` | `FSBar.Hub.HubEvents` | runtime | observable stream |
| 11 | `ScriptingClient` | `FSBar.Hub.ScriptingHub` | per-RPC-call | runtime only |
| 12 | `UnitEntry` | `FSBar.Hub.App.Encyclopedia` | derived from BarData | runtime cache |

---

## 1. `BarInstall`

```fsharp
type BarInstall = {
    DataDir: string                         // e.g. "/home/u/.local/state/Beyond All Reason"
    Engines: EngineVersionEntry list        // sorted newest-first
    ActiveEngine: EngineVersionEntry        // chosen by user or default = head
    DataDirIsDefault: bool                  // true if dataDir matches XDG default
}
```

**Validation rules** (FR-003 / FR-004 / FR-005):
- `DataDir` MUST exist and be a directory.
- `DataDir` MUST contain `engine/` subdir with at least one `recoil_*` child.
- `Engines` MUST be non-empty; the sort key is the `recoil_<YYYY.MM.DD>`
  date suffix, descending.
- `ActiveEngine` MUST be a member of `Engines` by reference equality.
- A `BarInstall` value never carries an invalid combination — invalid input
  yields `Result<BarInstall, BarInstallError>` instead.

---

## 2. `EngineVersionEntry`

```fsharp
type EngineVersionEntry = {
    Version: string                         // "2026.03.14"
    EngineDir: string                       // ".../engine/recoil_2026.03.14"
    HasHeadlessBin: bool                    // file "spring-headless" exists
    HasGraphicalBin: bool                   // file "spring" exists
    AiSkirmishDir: string                   // engineDir + "/AI/Skirmish"
}
```

**Validation rules**:
- `EngineDir` MUST exist; if `HasHeadlessBin = false` the entry is still
  enumerated but flagged so the UI can disable launches against it.

---

## 3. `BundledProxyInfo`

```fsharp
type BundledProxyInfo = {
    Version: string                         // contents of proxy/BUNDLED_VERSION
    BundleRoot: string                      // absolute path to proxy/bundled/<version>/
    LibSkirmishAiPath: string               // .../libSkirmishAI.so
    AiInfoLuaPath: string                   // .../AIInfo.lua
    AiOptionsLuaPath: string                // .../AIOptions.lua
}
```

**Validation rules** (FR-006, FR-006a, FR-006b):
- `BUNDLED_VERSION` MUST exist and contain exactly one non-empty line.
- `bundled/<version>/` MUST exist.
- All three file paths MUST exist and be non-empty.
- Errors yield `Result<BundledProxyInfo, BundledProxyError>`.

---

## 4. `ProxyInstallStatus`

```fsharp
type ProxyInstallStatus = {
    EngineVersion: string                   // recoil version this status pertains to
    InstalledAtPath: string                 // engineDir/AI/Skirmish/HighBarV2/<v>/
    InstalledVersion: string option         // None if not installed
    AiFilesPresent: bool                    // all three of the proxy files
    DevModeFilePresent: bool                // dataDir/devmode.txt
    SimpleAiListDisabled: bool              // IGL_data.lua simpleAiList = false
    MatchesBundled: bool                    // installedVersion = bundled.Version
}

type ProxyHealth =
    | UpToDate
    | NotInstalled
    | StaleVersion of installed: string * bundled: string
    | StaleEngine of forEngine: string * activeEngine: string
    | ConfigIncomplete of reasons: string list   // devmode missing OR simpleAiList true
```

**State transitions**: `health: ProxyInstallStatus -> ProxyHealth` is a pure
function (FR-009).

---

## 5. `HubSettings`

```fsharp
type HubSettings = {
    BarDataDirOverride: string option       // None = use XDG default
    EngineVersionOverride: string option    // None = use newest
    LastLobby: LobbyConfig option           // FR-011b
    GrpcPort: int                           // default 5021
    LaunchGraphicalViewerDefault: bool      // default false
    SchemaVersion: int                      // 1 (for forward-compat migrations)
}
```

**Persistence**: JSON via `System.Text.Json` to
`$XDG_CONFIG_HOME/fsbar-hub/settings.json`, written via temp-file + rename
for atomicity (constitution §IV — fail-safe IO).

**Validation rules**:
- `GrpcPort` ∈ `[1024, 65535]`.
- `SchemaVersion = 1`. Older versions trigger an in-place migration
  (currently no-op).

---

## 6. `LobbyConfig` (a.k.a. `SessionConfig` in the spec)

```fsharp
type LobbyConfig = {
    MapName: string                         // matches a *.sd7 in dataDir/maps/
    Mode: GameMode
    EngineSpeed: float32                    // multiplier; UI clamps to [0.1f, 100f]
    LaunchGraphicalViewer: bool
    Teams: Team list                        // ordered; index = engine team id
    Spectators: Spectator list
}
```

**Validation rules** (FR-011 / FR-011a / FR-012):
- `Teams.Length >= 2`.
- Every team has at least one non-spectator seat.
- All `Seat.Handicap` ∈ `[-100, 100]`.
- `MapName` MUST resolve to an existing `dataDir/maps/<name>.sd7`.
- Every AI seat's AI name MUST be in the active engine's installed AI list.
- `Mode = FFA` ⇒ `Teams.Length >= 3` AND every team has exactly one seat.

The `validate: BarInstall -> LobbyConfig -> Result<LobbyConfig, LobbyError list>`
returns *all* failures (not just the first), so the UI can render them as a
single coherent error list.

---

## 7. `Team`, `Seat`, `SeatKind`, `GameMode`

```fsharp
type GameMode =
    | Skirmish      // 1 vs 1 or 1 vs N or NvN team game
    | FFA           // free-for-all, every team is one player
    | Team          // explicit team game with team-vs-team scoring

type SeatKind =
    | AiSeat of aiName: string * options: Map<string, string>
    | HumanSeat of playerName: string

type Seat = {
    Kind: SeatKind
    Side: string                            // "Armada" | "Cortex" | ...
    Handicap: int                           // [-100, 100]
}

type Team = {
    Seats: Seat list                        // non-empty
    AllyTeamId: int                         // 0 = first ally team; teams in same ally team are allies
}

type Spectator = {
    PlayerName: string
}
```

**Mapping to `EngineConfig`**: `LobbyConfig.toEngineConfig: BarInstall ->
LobbyConfig -> EngineConfig` builds the existing
`FSBar.Client.EngineConfig` record + the start-script lobby block consumed by
`ScriptGenerator` (already in `FSBar.Client`).

---

## 8. `RunningSession`

```fsharp
type RunningSession = {
    Id: System.Guid                         // hub-assigned, used in logs
    Config: LobbyConfig
    EngineConfig: EngineConfig              // resolved from Config + BarInstall
    BarClient: BarClient                    // owns the engine + proxy connection
    GraphicalEngineProcess: System.Diagnostics.Process option   // FR-014
    StartedAt: System.DateTimeOffset
}
```

At most one `RunningSession` per hub instance in v1 (per assumption).

---

## 9. `SessionState`

```fsharp
type SessionState =
    | Idle                                  // no session
    | Starting of LobbyConfig
    | Running of RunningSession
    | Ending of RunningSession              // teardown in progress
    | Failed of LobbyConfig * reason: string * infologExcerpt: string option
```

**State diagram**:
```
Idle ── launch ──> Starting ── ok ──> Running
                          └── err ──> Failed
Running ── end / engine-exit ──> Ending ── ok ──> Idle
                                        └── err ──> Failed
Failed ── dismiss / launch-again ──> Idle / Starting
```

---

## 10. `HubEvent`

```fsharp
type HubEvent =
    | StateChanged of SessionState
    | EngineSpeedChanged of float32
    | SessionPaused of bool                 // true = paused
    | DiagnosticsLine of severity: Severity * message: string
    | ScriptingClientConnected of clientId: System.Guid * remote: string
    | ScriptingClientDetached of clientId: System.Guid * reason: DetachReason
    | ProxyInstallProgress of step: ProxyInstallStep * outcome: StepOutcome

and Severity = Info | Warning | Error
and DetachReason = ClientDisconnected | OverflowDropLimit | ServerShutdown
and ProxyInstallStep = CopyAiFiles | TouchDevMode | ToggleSimpleAiList
and StepOutcome = Skipped | Performed | Failed of string
```

`HubEvents.events: System.IObservable<HubEvent>` is the single source of
truth for the status bar, the `Settings → Diagnostics` log pane, and the
gRPC `GetSessionStatus` response.

---

## 11. `ScriptingClient`

```fsharp
type ScriptingClient = {
    Id: System.Guid
    RemoteEndpoint: string                  // for diagnostics only
    Channel: System.Threading.Channels.Channel<Highbar.GameFrame>
    DropCount: int ref                      // cumulative dropped frames
    AttachedAt: System.DateTimeOffset
}
```

Created on `StreamGameFrames` accept; lives until the call ends or the
overflow detach policy fires (see R3). Not exposed publicly — only the count
of active clients and their `(Id, RemoteEndpoint, AttachedAt, DropCount)`
projection is exposed via `GetSessionStatus`.

---

## 12. `UnitEntry`

```fsharp
type UnitEntry = {
    DefId: int
    InternalName: string                    // "armcom"
    DisplayName: string                     // "Armada Commander"
    Faction: FactionId                      // from UnitGlyph.classifyFaction
    Tier: Tier                              // from UnitGlyph.classifyTier
    Role: string                            // from UnitDef.role / category
    Cost: int * int                         // (metal, energy)
    Health: int
    BuildTime: int
    WeaponSummary: string                   // human-readable; aggregated weaponDef refs
    MovementSummary: string                 // "Tank", "K-Bot", "Sea", "Static", ...
    BuildOptionDefIds: int list             // what this unit can build
    Glyph: SkiaViewer.Element list          // pre-rendered scene subtree from UnitGlyph.buildUnit
}
```

**Derivation**: `Encyclopedia.buildEntries: BarData.UnitDef seq -> UnitEntry list`
runs once at hub startup (after BarData is loaded); cached in memory.
Re-derived if `BarData` package version changes between runs (FR-022, US5-AS3).

---

## Cross-entity invariants

- The hub never holds more than one `RunningSession` at a time (assumption).
- Every `HubEvent.ScriptingClientDetached` MUST be preceded by a matching
  `ScriptingClientConnected` event (modulo restart).
- `ProxyInstallStatus.EngineVersion = BarInstall.ActiveEngine.Version` for the
  status surfaced in the Settings tab — the installer can compute statuses
  for non-active engines on demand but the UI's primary indicator tracks the
  active engine.
- `LobbyConfig` referenced by a `RunningSession` is **immutable** for that
  session's lifetime. Speed / pause adjustments go through
  `SessionManager.setSpeed` / `setPaused` and update engine state in place
  without producing a new `LobbyConfig`.
