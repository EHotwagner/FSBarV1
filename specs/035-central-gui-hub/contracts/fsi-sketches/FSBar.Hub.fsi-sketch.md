# FSBar.Hub `.fsi` sketches (Phase 1)

These signature sketches are the planning-time contract for each public
module in the new `FSBar.Hub` library. They are not the final `.fsi` files —
those are produced during implementation and live in `src/FSBar.Hub/`. The
shapes here exist so `/speckit.tasks` can produce per-`.fsi` tasks (per
constitution §II) without re-deriving them.

Every module below maps 1:1 to an entity in `data-model.md`.

---

## `FSBar.Hub.HubSettings`

```fsharp
namespace FSBar.Hub

module HubSettings =

    type HubSettings = {
        BarDataDirOverride: string option
        EngineVersionOverride: string option
        LastLobby: LobbyConfig.LobbyConfig option
        GrpcPort: int
        LaunchGraphicalViewerDefault: bool
        SchemaVersion: int
    }

    /// Resolves the on-disk settings path under XDG_CONFIG_HOME.
    val settingsPath: unit -> string

    /// Loads settings from disk, returning defaults when the file is absent.
    val load: unit -> HubSettings

    /// Atomically writes settings to disk via temp-file + rename.
    val save: settings: HubSettings -> Result<unit, string>

    val defaults: HubSettings
```

---

## `FSBar.Hub.BarInstall`

```fsharp
namespace FSBar.Hub

open FSBar.Client

module BarInstall =

    type EngineVersionEntry = {
        Version: string
        EngineDir: string
        HasHeadlessBin: bool
        HasGraphicalBin: bool
        AiSkirmishDir: string
    }

    type BarInstall = {
        DataDir: string
        Engines: EngineVersionEntry list
        ActiveEngine: EngineVersionEntry
        DataDirIsDefault: bool
    }

    type BarInstallError =
        | DataDirNotFound of path: string
        | EngineSubdirMissing of path: string
        | NoEngineVersions of path: string
        | OverriddenEngineNotFound of version: string

    /// Resolves the BAR data dir from settings + XDG default.
    val resolveDataDir: settings: HubSettings.HubSettings -> string

    /// Enumerates installed engines under dataDir/engine and returns the
    /// active one per the user's override (or newest if no override).
    val detect: settings: HubSettings.HubSettings -> Result<BarInstall, BarInstallError>

    /// Lists the names of skirmish AIs installed under the given engine.
    val listSkirmishAis: engine: EngineVersionEntry -> string list
```

---

## `FSBar.Hub.BundledProxy`

```fsharp
namespace FSBar.Hub

module BundledProxy =

    type BundledProxyInfo = {
        Version: string
        BundleRoot: string
        LibSkirmishAiPath: string
        AiInfoLuaPath: string
        AiOptionsLuaPath: string
    }

    type BundledProxyError =
        | VersionFileMissing of path: string
        | VersionFileMalformed of path: string
        | BundleDirMissing of path: string
        | RequiredFileMissing of path: string

    /// Resolves the bundled-proxy root from $FSBAR_HUB_BUNDLED_PROXY_DIR or
    /// the assembly-relative `proxy/` dir. See research.md R6.
    val resolve: unit -> Result<BundledProxyInfo, BundledProxyError>
```

---

## `FSBar.Hub.ProxyInstaller`

```fsharp
namespace FSBar.Hub

module ProxyInstaller =

    type ProxyInstallStatus = {
        EngineVersion: string
        InstalledAtPath: string
        InstalledVersion: string option
        AiFilesPresent: bool
        DevModeFilePresent: bool
        SimpleAiListDisabled: bool
        MatchesBundled: bool
    }

    type ProxyHealth =
        | UpToDate
        | NotInstalled
        | StaleVersion of installed: string * bundled: string
        | StaleEngine of forEngine: string * activeEngine: string
        | ConfigIncomplete of reasons: string list

    val checkStatus:
        install: BarInstall.BarInstall ->
        bundled: BundledProxy.BundledProxyInfo ->
            ProxyInstallStatus

    val health: status: ProxyInstallStatus -> ProxyHealth

    /// Installs the proxy AI files, touches devmode.txt, and toggles
    /// simpleAiList = false. Idempotent. Steps emit ProxyInstallProgress
    /// events through HubEvents. Returns Ok with the new status, or Error
    /// listing every step that failed.
    val install:
        install: BarInstall.BarInstall ->
        bundled: BundledProxy.BundledProxyInfo ->
            Result<ProxyInstallStatus, string list>

    /// Pure helper exposed for testing — applies the simpleAiList edit to a
    /// string and returns the new contents (None if no change needed).
    val rewriteSimpleAiList: contents: string -> string option
```

---

## `FSBar.Hub.LobbyConfig`

```fsharp
namespace FSBar.Hub

open FSBar.Client

module LobbyConfig =

    type GameMode = Skirmish | FFA | Team

    type SeatKind =
        | AiSeat of aiName: string * options: Map<string, string>
        | HumanSeat of playerName: string

    type Seat = {
        Kind: SeatKind
        Side: string
        Handicap: int
    }

    type Team = {
        Seats: Seat list
        AllyTeamId: int
    }

    type Spectator = { PlayerName: string }

    type LobbyConfig = {
        MapName: string
        Mode: GameMode
        EngineSpeed: float32
        LaunchGraphicalViewer: bool
        Teams: Team list
        Spectators: Spectator list
    }

    type LobbyError =
        | NotEnoughTeams
        | TeamHasNoActiveSeats of teamIndex: int
        | HandicapOutOfRange of teamIndex: int * seatIndex: int * value: int
        | MapNotInstalled of mapName: string
        | UnknownAi of teamIndex: int * seatIndex: int * aiName: string
        | FfaTeamHasMultipleSeats of teamIndex: int
        | FfaTooFewTeams

    val defaults: LobbyConfig

    val validate:
        install: BarInstall.BarInstall ->
        config: LobbyConfig ->
            Result<LobbyConfig, LobbyError list>

    val toEngineConfig:
        install: BarInstall.BarInstall ->
        config: LobbyConfig ->
            EngineConfig
```

---

## `FSBar.Hub.SessionManager`

```fsharp
namespace FSBar.Hub

open FSBar.Client

module SessionManager =

    type RunningSession = {
        Id: System.Guid
        Config: LobbyConfig.LobbyConfig
        EngineConfig: EngineConfig
        BarClient: BarClient
        GraphicalEngineProcess: System.Diagnostics.Process option
        StartedAt: System.DateTimeOffset
    }

    type SessionState =
        | Idle
        | Starting of LobbyConfig.LobbyConfig
        | Running of RunningSession
        | Ending of RunningSession
        | Failed of LobbyConfig.LobbyConfig * reason: string * infologExcerpt: string option

    type SessionManager =
        member State: SessionState
        member Frames: System.IObservable<Highbar.GameFrame>
        member Launch: config: LobbyConfig.LobbyConfig -> Result<unit, string>
        member SetSpeed: speed: float32 -> unit
        member SetPaused: paused: bool -> unit
        member End: unit -> unit
        interface System.IDisposable

    val create:
        install: BarInstall.BarInstall ->
        events: HubEvents.IHubEventSink ->
            SessionManager
```

---

## `FSBar.Hub.ScriptingHub`

```fsharp
namespace FSBar.Hub

open FsGrpc

module ScriptingHub =

    type ScriptingHubOptions = {
        Port: int
        FrameBufferCapacity: int            // default 16
        MaxCumulativeDrops: int             // default 32
    }

    val defaults: ScriptingHubOptions

    /// Service implementation type. Registered into the Kestrel host by
    /// FSBar.Hub.App via Grpc.AspNetCore (research.md R2).
    type ScriptingService =
        new: sessions: SessionManager.SessionManager *
             events: HubEvents.IHubEventSink *
             unitDefs: FSBar.Client.UnitDefCache.UnitDefCache *
             opts: ScriptingHubOptions ->
                ScriptingService
        // FsGrpc-generated server interface methods are inherited; not
        // explicitly listed here because FsGrpc generates them from
        // contracts/hub/scripting.proto.
```

---

## `FSBar.Hub.HubEvents`

```fsharp
namespace FSBar.Hub

module HubEvents =

    type Severity = Info | Warning | Error
    type DetachReason = ClientDisconnected | OverflowDropLimit | ServerShutdown
    type ProxyInstallStep = CopyAiFiles | TouchDevMode | ToggleSimpleAiList
    type StepOutcome = Skipped | Performed | Failed of string

    type HubEvent =
        | StateChanged of SessionManager.SessionState
        | EngineSpeedChanged of float32
        | SessionPaused of bool
        | DiagnosticsLine of severity: Severity * message: string
        | ScriptingClientConnected of clientId: System.Guid * remote: string
        | ScriptingClientDetached of clientId: System.Guid * reason: DetachReason
        | ProxyInstallProgress of step: ProxyInstallStep * outcome: StepOutcome

    type IHubEventSink =
        abstract Publish: HubEvent -> unit

    type HubEventBus =
        member Sink: IHubEventSink
        member Events: System.IObservable<HubEvent>
        interface System.IDisposable

    val create: unit -> HubEventBus
```

---

## Surface-area baselines

Each module's `.fsi` will be snapshotted under
`tests/FSBar.Hub.Tests/Baselines/<Module>.baseline` via the existing
`SurfaceAreaHelper` wrapper. Eight baselines total at the planning
boundary; updated only by setting `SURFACE_AREA_UPDATE=1` after an
intentional change (per `CLAUDE.md`).
