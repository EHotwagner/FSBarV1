---

description: "Task list for feature 035-central-gui-hub"
---

# Tasks: Central GUI Hub App

**Feature**: `035-central-gui-hub`
**Input**: Design documents from `/specs/035-central-gui-hub/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Test tasks are INCLUDED — constitution §III ("Test Evidence Is Mandatory")
plus the plan calls out `FSBar.Hub.Tests` (unit) and `FSBar.Hub.LiveTests` (live)
as required artifacts. Each user story carries at least one verification test.

**Organization**: Tasks are grouped by user story (US1–US7) so each story can be
implemented, tested, and shipped independently. Setup + Foundational phases must
complete before any user-story phase can start.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story this task belongs to (US1, US2, …). Setup / Foundational /
  Polish tasks omit the story label.
- All paths are repo-relative to `/home/developer/projects/FSBarV1/`.

## Path Conventions

- Library code: `src/FSBar.Hub/` (packable; no GUI deps)
- GUI executable: `src/FSBar.Hub.App/` (Skia + Silk.NET + Grpc.AspNetCore host)
- Proto extension: `src/FSBar.Proto/proto/hub/` (FsGrpc generates into `Generated/`)
- Bundled proxy: `proxy/bundled/<version>/` + `proxy/BUNDLED_VERSION`
- Maintainer scripts: `scripts/`
- Unit tests: `tests/FSBar.Hub.Tests/`
- Live tests: `tests/FSBar.Hub.LiveTests/`
- Surface baselines: `tests/FSBar.Hub.Tests/Baselines/<Module>.baseline`
  (regenerated with `SURFACE_AREA_UPDATE=1` per `CLAUDE.md`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Register the new projects, lay down the on-disk directory layout, and
extend `FSBar.Proto` with the hub scripting contract so everything downstream can
compile against it.

- [ ] T001 Create `proxy/` directory tree at repo root with subdirs `proxy/bundled/0.1.17/` (use the version currently shipped per `nupkg/` packages), and add an empty `proxy/BUNDLED_VERSION` placeholder file plus a stub `proxy/README.md` explaining the layout (research.md R6).
- [ ] T002 [P] Create empty F# library project skeleton at `src/FSBar.Hub/FSBar.Hub.fsproj` (`<TargetFramework>net10.0</TargetFramework>`, `PackageId=FSBar.Hub`, `IsPackable=true`, output to `~/.local/share/nuget-local/`) — mirror `src/FSBar.Client/FSBar.Client.fsproj` conventions.
- [ ] T003 [P] Create empty F# executable project skeleton at `src/FSBar.Hub.App/FSBar.Hub.App.fsproj` (`<Project Sdk="Microsoft.NET.Sdk.Web">`, `<TargetFramework>net10.0</TargetFramework>`, `OutputType=Exe`, `IsPackable=false`, `<PackageReference Include="Grpc.AspNetCore" Version="2.67.0" />`, references to `FSBar.Hub`, `FSBar.Client`, `FSBar.Viz`, `FSBar.SyntheticData`, `FSBar.Proto`, `SkiaViewer`).
- [ ] T004 [P] Create empty xUnit test project skeleton at `tests/FSBar.Hub.Tests/FSBar.Hub.Tests.fsproj` referencing `src/FSBar.Hub/FSBar.Hub.fsproj` and `tests/Common/SurfaceAreaHelper.fs` (compile-included), mirroring `tests/FSBar.Viz.Tests/FSBar.Viz.Tests.fsproj`.
- [ ] T005 [P] Create empty xUnit test project skeleton at `tests/FSBar.Hub.LiveTests/FSBar.Hub.LiveTests.fsproj` referencing `FSBar.Hub`, `FSBar.Hub.App` (for in-process gRPC hosting), and the same engine-version helpers used by `tests/FSBar.LiveTests/`.
- [ ] T006 Add the four new project entries (`FSBar.Hub`, `FSBar.Hub.App`, `FSBar.Hub.Tests`, `FSBar.Hub.LiveTests`) to `FSBarV1.slnx` — verify with `dotnet build FSBarV1.slnx` (skeletons compile to empty assemblies).
- [ ] T007 Copy `specs/035-central-gui-hub/contracts/hub/scripting.proto` to `src/FSBar.Proto/proto/hub/scripting.proto` and verify `FsGrpc.Tools` picks it up by running `dotnet build src/FSBar.Proto` and confirming `Generated/hub/Scripting.gen.fs` appears.
- [ ] T008 Update `src/FSBar.Proto/FSBar.Proto.fsproj` `<Description>` from "protobuf bindings for HighBar V2 protocol" to "protobuf bindings for the HighBar V2 engine protocol and the FSBar hub scripting service" (research.md R4).

**Checkpoint**: `dotnet build FSBarV1.slnx` succeeds; the new projects exist as empty
shells; the hub scripting `.proto` is generated into `FSBar.Proto`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Ship the modules every user story depends on — the event bus, the
settings store, BAR install detection, the bundled-proxy locator, the headless
scene-build entrypoint in `FSBar.Viz`, and the GUI shell skeleton (window +
tab router + status bar shell + ASP.NET host) — without yet wiring any user-story
flow through them.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

### Core library plumbing (`FSBar.Hub`)

- [ ] T009 Implement `src/FSBar.Hub/HubEvents.fsi` and `.fs` per the sketch in `contracts/fsi-sketches/FSBar.Hub.fsi-sketch.md` (HubEvent DU, IHubEventSink, HubEventBus with `Sink`, `Events: IObservable<HubEvent>`, `IDisposable`). Use a `System.Reactive.Subjects.Subject`-equivalent via BCL only — `System.Threading.Channels` + custom `IObservable` adapter (NO new System.Reactive dep).
- [ ] T010 [P] Implement `src/FSBar.Hub/HubSettings.fsi` and `.fs` — JSON round-trip via `System.Text.Json`, atomic write via temp-file + rename, XDG path resolution (`$XDG_CONFIG_HOME/fsbar-hub/settings.json` with `$HOME/.config` fallback). Defaults: `GrpcPort=5021`, `LaunchGraphicalViewerDefault=false`, `SchemaVersion=1`.
- [ ] T011 [P] Implement `src/FSBar.Hub/BundledProxy.fsi` and `.fs` — read `proxy/BUNDLED_VERSION` (resolve from `$FSBAR_HUB_BUNDLED_PROXY_DIR` or assembly-relative path), enumerate the matching `bundled/<version>/` subdir, validate the three required files, return `Result<BundledProxyInfo, BundledProxyError>` (research.md R6).
- [ ] T012 Implement `src/FSBar.Hub/BarInstall.fsi` and `.fs` — depends on `HubSettings`. Resolve data dir from settings or XDG default, enumerate `engine/recoil_*/` subdirs newest-first, build `EngineVersionEntry` records (with `HasHeadlessBin` / `HasGraphicalBin` checks), select `ActiveEngine` per `EngineVersionOverride` or default newest, list installed skirmish AIs from `<engineDir>/AI/Skirmish/`. Return `Result<BarInstall, BarInstallError>`.

### `FSBar.Viz` extension (R8)

- [ ] T013 Add a headless scene-build entrypoint to `src/FSBar.Viz/SceneBuilder.fsi` (and `.fs`) — `val buildSceneHeadless: state: GameState -> map: MapGrid option -> config: VizConfig -> Scene` — that returns a `Scene` without creating a window. Existing `GameViz.start` callers MUST be untouched. Bump `tests/FSBar.Viz.Tests/Baselines/SceneBuilder.baseline` via `SURFACE_AREA_UPDATE=1` and re-run baseline tests.
- [ ] T014 [P] Add an additive headless overlay-state API to `src/FSBar.Viz/GameViz.fsi` (and `.fs`) so the hub chrome can read/write the same overlay-toggle state the in-viewer hotkeys touch (FR-017). Concretely: `val getOverlayState: GameVizSession -> OverlayState` and `val setOverlayState: GameVizSession -> OverlayState -> unit`. Refresh the `GameViz` baseline.

### GUI shell skeleton (`FSBar.Hub.App`)

- [ ] T015 Implement `src/FSBar.Hub.App/Chrome/TabBar.fsi` + `.fs` — pure rendering of a left-edge 56px sidebar with six tabs (Setup, Viewer, Encyclopedia, Configurator, Settings, gRPC), input routing for tab selection. No tab content yet (FR-002a, research.md R1).
- [ ] T016 [P] Implement `src/FSBar.Hub.App/Chrome/StatusBar.fsi` + `.fs` — pure rendering of a bottom-edge 24px bar showing `SessionState` text. Speed slider / pause / end-session controls render but emit no actions yet (US1 wires them) (FR-015b shell).
- [ ] T017 Implement `src/FSBar.Hub.App/ProcessLifetime.fsi` + `.fs` — track child engine PIDs, install Linux signal handlers (SIGTERM, window-close), iterate tracked PIDs sending SIGTERM then SIGKILL after 3s grace (FR-001, research.md R9). Expose `requestClose: SessionState -> CloseDecision` (`AllowClose | RequireConfirm of message: string`) — when `SessionState = Running`, return `RequireConfirm` so the GUI can prompt before tearing the session down (spec.md Edge Case "User closes the hub while a session is running"). Provide `scripts/hub-spawn-engine.sh` (sibling new file) that does `prctl(PR_SET_PDEATHSIG, SIGTERM)` then `exec`s `spring-headless` — pattern matches the existing trainer wrapper.
- [ ] T018 Implement `src/FSBar.Hub.App/Program.fs` — entry point that: (a) loads `HubSettings`, (b) constructs `HubEventBus`, (c) detects `BarInstall` and `BundledProxyInfo`, (d) starts a `WebApplicationBuilder` Kestrel host bound to `127.0.0.1:<grpcPort>` with HTTP/2 cleartext (research.md R2), (e) opens a `SkiaViewer.Window`, (f) runs the tab router with the empty tab implementations from T015 stub. Window-close handler consults `ProcessLifetime.requestClose`; if `RequireConfirm`, render a modal confirm dialog; only on confirm does the hub proceed with teardown.

### Test scaffolding

- [ ] T019 [P] Add `tests/FSBar.Hub.Tests/Baselines/HubEvents.baseline`, `HubSettings.baseline`, `BarInstall.baseline`, `BundledProxy.baseline` (initial snapshots produced via the shared `SurfaceAreaHelper` wrapper with `SURFACE_AREA_UPDATE=1`).
- [ ] T020 [P] Implement `tests/FSBar.Hub.Tests/HubSettingsTests.fs` — unit tests covering: JSON round-trip preserves all fields; absent file returns defaults; atomic save survives concurrent reads; `GrpcPort` outside `[1024, 65535]` is rejected on load.
- [ ] T021 [P] Implement `tests/FSBar.Hub.Tests/BarInstallTests.fs` — synthesize a temp dir tree with two fake `recoil_<date>/` subdirs (one with a `spring-headless` file, one without), assert `detect` returns them sorted newest-first, that `ActiveEngine` honors `EngineVersionOverride`, and that missing data dir yields `DataDirNotFound`.
- [ ] T022 [P] Implement `tests/FSBar.Hub.Tests/BundledProxyTests.fs` — temp-dir variant: write a fake `bundled/0.1.17/{libSkirmishAI.so,AIInfo.lua,AIOptions.lua}` plus matching `BUNDLED_VERSION`, assert `resolve` returns the right paths; assert each error variant fires when its precondition is violated.
- [ ] T022a [P] Implement `tests/FSBar.Hub.Tests/SessionIsolationTests.fs` — start a `SessionManager` in `Idle`, simulate an inbound proxy attempting to attach to the hub's listening socket WITHOUT going through `SessionManager.Launch`. Assert: `SessionState` remains `Idle`, no `HubEvent.StateChanged` is emitted, and `SessionManager.Frames` produces zero items. Covers FR-015a.

**Checkpoint**: Foundation ready — the hub launches an empty window with sidebar
and status bar, the gRPC port is bound (no service registered yet), settings
persist, BarInstall + BundledProxy detection works, and external (non-hub-launched)
proxy connections cannot engage the viewer or gRPC stream (FR-015a). User-story
implementation can now begin in parallel.

---

## Phase 3: User Story 1 — One-click BAR session from the hub (Priority: P1) 🎯 MVP

**Goal**: A user picks a map, configures teams, sets mode + speed, clicks Launch,
and within 30 s sees the embedded viewer rendering the live session.

**Independent Test**: From a clean hub install, choose Avalanche 3.4 + HighBarV2
(ally) + BARb (enemy) + Skirmish + 1.0x → click Launch → status bar transitions
`Idle → Starting → Running` ≤ 30 s, viewer shows both teams' starting units within
one frame after Running (`quickstart.md` US1, AS-1.1, AS-3.1, SC-002).

### Tests for User Story 1

- [ ] T023 [P] [US1] Add `tests/FSBar.Hub.Tests/Baselines/LobbyConfig.baseline` and `SessionManager.baseline` snapshots.
- [ ] T024 [P] [US1] Implement `tests/FSBar.Hub.Tests/LobbyConfigTests.fs` — `validate` returns `NotEnoughTeams` when only one team, `TeamHasNoActiveSeats` when a team has zero non-spectator seats, `HandicapOutOfRange` outside `[-100,100]`, `MapNotInstalled` for a fake map name, `FfaTooFewTeams` when Mode=FFA with two teams, `FfaTeamHasMultipleSeats` when an FFA team has >1 seat. Round-trip `defaults` survives `validate` against a synthetic `BarInstall`.
- [ ] T025 [P] [US1] Implement `tests/FSBar.Hub.LiveTests/LiveSessionLaunchTests.fs` (skipped if no engine present per `tests/engine-version.json`) — full launch via `SessionManager.create` against the real engine with HighBarV2 vs. BARb on Avalanche 3.4. Assert state transitions reach `Running`, that `Frames` produces at least one frame within 30 s, then call `End` and assert clean transition to `Idle`. **Pre-fix**: this test fails because the modules don't exist yet (compile error). **Post-fix**: passes.
- [ ] T026 [P] [US1] Implement `tests/FSBar.Hub.Tests/StatusBarControlsTests.fs` — pure unit: with a mock `IHubEventSink`, calling `setSpeed 2.0f` emits `EngineSpeedChanged 2.0f`; calling `setPaused true` then `setPaused false` round-trips through the event stream and the second resume restores the previously-observed engine speed (FR-015c).
- [ ] T026a [P] [US1] Implement `tests/FSBar.Hub.Tests/CloseWhileRunningTests.fs` — pure unit: with `SessionState = Running`, `ProcessLifetime.requestClose` returns `RequireConfirm`; with `SessionState = Idle | Failed`, returns `AllowClose`. Covers spec.md Edge Case "User closes the hub while a session is running".

### Implementation for User Story 1

- [ ] T027 [US1] Implement `src/FSBar.Hub/LobbyConfig.fsi` and `.fs` per the sketch — types (`GameMode`, `SeatKind`, `Seat`, `Team`, `Spectator`, `LobbyConfig`, `LobbyError`), `defaults`, `validate` (returns *all* errors as a list, not first-error), `toEngineConfig: BarInstall -> LobbyConfig -> EngineConfig` that reuses the existing `FSBar.Client.EngineConfig` + start-script generator.
- [ ] T028 [US1] Implement `src/FSBar.Hub/SessionManager.fsi` and `.fs` — owns at-most-one `RunningSession`, exposes `State`, `Frames: IObservable<Highbar.GameFrame>` (sourced from the underlying `BarClient` callbacks), `Launch`, `SetSpeed`, `SetPaused`, `End`, `IDisposable`. Drives `HubEvents.StateChanged` / `EngineSpeedChanged` / `SessionPaused` / `DiagnosticsLine` through the supplied `IHubEventSink`. On engine exit (clean or crash), capture infolog excerpt and emit `Failed`.
- [ ] T029 [US1] Implement `src/FSBar.Hub.App/Tabs/SetupTab.fsi` and `.fs` — Skia-rendered lobby builder: map dropdown (sourced from `BarInstall.DataDir/maps/*.sd7`), team list with add-seat / remove-seat / change-side controls, mode picker (Skirmish / FFA / Team), speed slider, Launch button. Shows the *full* `LobbyError` list when validation fails; disables Launch until `validate` returns Ok (FR-011, FR-011a, FR-012, AS-1.3). Persists last-used lobby into `HubSettings.LastLobby` after every successful Launch (FR-011b).
- [ ] T030 [US1] Implement `src/FSBar.Hub.App/Tabs/ViewerTab.fsi` and `.fs` — composes `SceneBuilder.buildSceneHeadless` (T013) into the active tab scene when `SessionState = Running`, sources `GameState` and `MapGrid` from `RunningSession.BarClient`. Throttles to ≤60 fps regardless of engine tick rate (FR-018). When state ≠ Running, renders an empty placeholder.
- [ ] T031 [US1] Wire `src/FSBar.Hub.App/Chrome/StatusBar.fs` controls to `SessionManager` — speed slider → `SetSpeed`, pause toggle → `SetPaused`, end-session → `End` (FR-015b, FR-015c, FR-015d). End-Session must NOT exit the hub and must leave gRPC clients connected.
- [ ] T032 [US1] Wire `src/FSBar.Hub.App/Tabs/SetupTab.fs` Launch button: confirms-replace prompt when `SessionState ≠ Idle` (AS-1.4); blocks Launch with explanatory message when the chosen map / AI is not installed locally (AS-1.3, FR-012); on success transitions Setup → Viewer tab automatically.
- [ ] T033 [US1] Update `src/FSBar.Hub.App/Program.fs` to construct `SessionManager`, wire it into `SetupTab`, `ViewerTab`, and `StatusBar`, and ensure `ProcessLifetime` is registered with the engine PID(s) on every Launch (FR-001).

**Checkpoint**: User Story 1 is fully functional and independently testable.
A user can launch BAR sessions from the hub and watch them in the embedded
viewer with live speed / pause / end controls. The hub still has stub
implementations for first-run wizard, encyclopedia, configurator, and gRPC
service.

---

## Phase 4: User Story 2 — First-run setup: locate BAR and install the proxy (Priority: P1)

**Goal**: First time the hub starts, walk the user through detecting BAR, picking
the active engine, and installing the bundled HighBarV2 proxy + `devmode.txt` +
`simpleAiList = false` Chobby edit. Idempotent across re-runs (SC-008).

**Independent Test**: On a machine without the proxy installed, opening the hub
triggers the wizard. Completing it leaves the proxy files under
`<engineDir>/AI/Skirmish/HighBarV2/<v>/`, `devmode.txt` present in the data dir,
and `simpleAiList = false` in `IGL_data.lua`. Re-running the wizard produces
zero file changes (`quickstart.md` US2 + idempotency check; SC-007, SC-008).

### Tests for User Story 2

- [ ] T034 [P] [US2] Add `tests/FSBar.Hub.Tests/Baselines/ProxyInstaller.baseline`.
- [ ] T035 [P] [US2] Implement `tests/FSBar.Hub.Tests/ProxyInstallerTests.fs` — pure tests for `rewriteSimpleAiList`: input with `simpleAiList = true` rewrites to `false` and returns `Some`; input already `= false` returns `None` (no-op); input missing the key gets a new line appended in the correct table block; surrounding comments / whitespace / key order are byte-preserved (R5, AS-2.5). Use a golden fixture in `tests/FSBar.Hub.Tests/fixtures/IGL_data.lua` (a representative copy committed for reproducibility).
- [ ] T036 [P] [US2] Implement `tests/FSBar.Hub.Tests/ProxyInstallerIdempotencyTests.fs` — synthesize a complete fake BAR data dir + engine dir under a temp path with the proxy already installed at the bundled version. Call `install`. Assert: every step's `StepOutcome = Skipped`, `IGL_data.lua` mtime unchanged, file diff byte-empty (SC-008).
- [ ] T036a [P] [US2] Implement `tests/FSBar.Hub.Tests/ProxyInstallerNewerLocalTests.fs` — synthesize a fake BAR data dir with an `libSkirmishAI.so` whose mtime is 1 hour newer than the bundled copy. Call `install` without `force`; assert `CopyAiFiles` outcome is `Skipped` with a warning message; assert the on-disk file is byte-unchanged. Then call `install ~force:true`; assert outcome is `Performed`. Covers spec.md Edge Cases line 142.
- [ ] T037 [P] [US2] Implement `tests/FSBar.Hub.LiveTests/LiveProxyInstallTests.fs` — point the installer at a disposable copy of the user's BAR data dir under `/tmp/`, run `install`, assert all three files appear at `<engineDir>/AI/Skirmish/HighBarV2/<bundledVersion>/`, `devmode.txt` exists, and `simpleAiList = false` in the copied `IGL_data.lua`. Then launch a real session against the disposable dir and assert the proxy connects (proves SC-007 effect end-to-end without touching the user's real BAR install).

### Implementation for User Story 2

- [ ] T038 [US2] Implement `src/FSBar.Hub/ProxyInstaller.fsi` and `.fs` — `checkStatus`, `health`, `install`, plus the pure helper `rewriteSimpleAiList: string -> string option` (regex per research.md R5 — `^(\s*)simpleAiList(\s*)=(\s*)(true|false)(\s*,?\s*)$`, multiline anchored, group-4 replace). Each step (`CopyAiFiles` / `TouchDevMode` / `ToggleSimpleAiList`) emits `HubEvents.ProxyInstallProgress` with `Skipped` / `Performed` / `Failed`. Before overwriting `libSkirmishAI.so`, compare the on-disk file's mtime/size against the bundled file; if the on-disk file is newer, emit `ProxyInstallProgress(CopyAiFiles, Skipped)` with a warning and require an explicit `force=true` arg to overwrite (spec.md Edge Case "libSkirmishAI.so on disk is newer than what the hub bundles"). Refuses to touch any path under `packages/` or `pool/` (FR-010).
- [ ] T039 [US2] Implement `src/FSBar.Hub.App/FirstRun.fsi` and `.fs` — three-step wizard scene rendered when `HubSettings` is freshly defaulted (no settings file present): Step 1 confirm/override BAR data dir, Step 2 pick active engine, Step 3 review bundled proxy version + confirm install. Calls `ProxyInstaller.install` on confirm; closes wizard and reverts to Setup tab on success. Surface every `StepOutcome.Failed` with its message verbatim.
- [ ] T040 [US2] Implement `src/FSBar.Hub.App/Tabs/SettingsTab.fsi` and `.fs` — shows BAR data dir, engine dir override, active engine version, bundled proxy version (FR-006b), per-engine `ProxyInstallStatus` rows with one-click "Install / Upgrade" buttons that call `ProxyInstaller.install` (FR-009). Detects stale-engine case (proxy installed under an older `recoil_*` dir than the active one) and surfaces it (AS-2.4).
- [ ] T041 [US2] Wire `src/FSBar.Hub.App/Program.fs` to detect first-run state (no `settings.json` present OR no proxy install detected on the active engine) and switch the initial scene to `FirstRun` instead of `SetupTab`.
- [ ] T042 [US2] Author `scripts/refresh-bundled-proxy.sh` (executable bash) — takes a version arg, copies `${HIGHBARV2_REPO:-../HighBarV2}/build/{libSkirmishAI.so}` plus `proxy/data/AIInfo.lua` and `proxy/data/AIOptions.lua` from the same source tree into `proxy/bundled/<version>/`, then atomically rewrites `proxy/BUNDLED_VERSION` last (research.md R6, FR-006a). Refuses to overwrite if the destination dir already has files unless `--force` is passed. Add a `proxy/README.md` section documenting usage.

**Checkpoint**: User Stories 1 AND 2 work independently. A clean machine can be
brought from "BAR installed, no proxy" to "first session running" without leaving
the hub.

---

## Phase 5: User Story 3 — Embedded live viewer as the hub's primary surface (Priority: P2)

**Goal**: While a session is running, the Viewer tab is the hub's main pane;
overlay toggles are reachable both via the existing in-viewer hotkeys and via
hub-chrome controls, with state synchronized. Tab-switching does not interrupt
the session.

**Independent Test**: With a session running, pressing `W` / `L` / `C` / `N`
toggles the same overlays the standalone viewer does, hub-chrome buttons reflect
the toggle state, switching to Encyclopedia/Configurator and back leaves the
session and frame pump untouched (`quickstart.md` US3, FR-017, AS-3.4).

### Tests for User Story 3

- [ ] T043 [P] [US3] Implement `tests/FSBar.Hub.Tests/OverlayStateSyncTests.fs` — pure unit: simulate a hotkey event into `GameViz.setOverlayState`, then read via `GameViz.getOverlayState` and assert the chrome-side button-state record matches; do the inverse (chrome-side click → in-viewer state) and assert the same.
- [ ] T044 [P] [US3] Implement `tests/FSBar.Hub.LiveTests/LiveTabSwitchingTests.fs` — launch a real session, observe `SessionManager.Frames` for 5 s of frames, then programmatically switch tabs through every non-Viewer tab and back, assert `Frames` continued to emit during the switch with no >100 ms gap (AS-3.4 wall-clock evidence).

### Implementation for User Story 3

- [ ] T045 [US3] Implement `src/FSBar.Hub.App/Tabs/ViewerTab.fs` chrome-overlay panel — small toolbar at the top edge of the Viewer tab with toggle buttons for Weapon ranges (W), Sight (L), Commands (C), Names (N). Buttons are bidirectional: clicking calls `GameViz.setOverlayState`, hotkey events update button state via `GameViz.getOverlayState`, polled per frame.
- [ ] T046 [US3] Confirm tab-switch is non-disruptive — `Program.fs`'s tab router MUST NOT pause `SessionManager.Frames` subscription when switching away from the Viewer tab; the gRPC scripting service (Phase 9) MUST keep receiving frames as well. Add a manual-test note to `quickstart.md` US3 if not already explicit (it is — AS-3.4).
- [ ] T047 [US3] When the session ends (`SessionState = Idle | Failed`), the Viewer tab clears to an empty placeholder, the Setup tab becomes reselectable, and the failure reason / infolog excerpt is surfaced (FR-031, AS-3.3).

**Checkpoint**: The hub is now a usable cockpit — sessions run, the embedded
viewer renders them, and the user can move between tabs without losing the
session.

---

## Phase 6: User Story 4 — Optional launch of the original BAR graphical engine (Priority: P2)

**Goal**: A toggle in Setup, when enabled, also launches `spring` (graphical,
windowed) in parallel with the headless engine. When disabled, only the headless
engine runs.

**Independent Test**: Toggle ON → Launch → graphical window appears windowed,
not fullscreen, and closing it does not tear down the hub or session. Toggle OFF
→ Launch → no graphical window (`quickstart.md` US4, FR-014, AS-4.1).

### Tests for User Story 4

- [ ] T048 [P] [US4] Implement `tests/FSBar.Hub.Tests/GraphicalEngineToggleTests.fs` — pure unit: with a mock `BarInstall` whose `ActiveEngine.HasGraphicalBin = false`, `LobbyConfig.validate` MUST surface a clear error if `LaunchGraphicalViewer = true` (AS-4.2 — block launch when the binary is missing).
- [ ] T049 [P] [US4] Extend `tests/FSBar.Hub.LiveTests/LiveSessionLaunchTests.fs` (or add a sibling `LiveGraphicalLauncherTests.fs`) with a smoke case: launch with `LaunchGraphicalViewer = true`, assert the `Process` handle for `spring` is non-null and alive within 30 s; kill the graphical process explicitly, assert the headless session continues running (AS-4.1).

### Implementation for User Story 4

- [ ] T050 [US4] Extend `src/FSBar.Hub/LobbyConfig.fs` `validate` to reject `LaunchGraphicalViewer = true` when the active engine's `HasGraphicalBin = false`, surfacing a new `LobbyError` variant (e.g. `GraphicalBinaryMissing of engineDir: string`).
- [ ] T051 [US4] Extend `src/FSBar.Hub/SessionManager.fs` to start the graphical `spring` binary in windowed mode (`Fullscreen=0` per existing `EngineLauncher` convention, `CLAUDE.md` §Graphical mode) when `Config.LaunchGraphicalViewer = true`, store the `Process` handle in `RunningSession.GraphicalEngineProcess`, and register its PID with `ProcessLifetime` (FR-013, FR-014). Closing that window does NOT cause `SessionState` to transition out of `Running`.
- [ ] T052 [US4] Add the toggle to `src/FSBar.Hub.App/Tabs/SetupTab.fs`, default-sourced from `HubSettings.LaunchGraphicalViewerDefault`. Persist the chosen value into `HubSettings` after each launch.

**Checkpoint**: Users can sanity-check the Skia viewer against BAR's native
renderer side-by-side when desired.

---

## Phase 7: User Story 5 — Unit and building encyclopedia (Priority: P3)

**Goal**: Encyclopedia tab lists every unit / building from `BarData` with the
*same* glyph the live viewer renders (SC-003 byte-match), full data card per
entry, and faction/tier/role filtering.

**Independent Test**: Open Encyclopedia with no session running. Every unit in
`BarData.AllUnitDefs` has an entry. Filtering by faction "Armada" narrows the
list and switches glyphs to the Armada palette. Selecting an entry shows its
cost / health / weapons / build options (`quickstart.md` US5, AS-5.1, AS-5.2,
SC-003).

### Tests for User Story 5

- [ ] T053 [P] [US5] Implement `tests/FSBar.Hub.Tests/EncyclopediaCoverageTests.fs` — assert `Encyclopedia.buildEntries (BarData.AllUnitDefs)` returns one `UnitEntry` per unit def, with `Glyph` non-empty for every entry. Assert filter-by-faction returns only matching entries (SC-003 count assertion).
- [ ] T054 [P] [US5] Implement `tests/FSBar.Hub.Tests/EncyclopediaGlyphParityTests.fs` — for a representative set of units (one per faction × shape class), build the glyph via `Encyclopedia.buildEntries` AND via `UnitGlyph.buildUnit` directly with the same synthetic `UnitDisplay`, assert the resulting `Scene` element trees are byte-equal (SC-003 byte-match).

### Implementation for User Story 5

- [ ] T055 [US5] Implement `src/FSBar.Hub.App/Tabs/Encyclopedia.fsi` and `.fs` (named `Encyclopedia` per data-model.md §12) — `buildEntries: UnitDef seq -> UnitEntry list` constructs each entry via `FSBar.SyntheticData.SyntheticDataAdapter` + `UnitGlyph.buildUnit` directly (research.md R10), caches in memory, re-derives if `BarData` package version changes between runs (FR-022, AS-5.3).
- [ ] T056 [US5] Implement `src/FSBar.Hub.App/Tabs/EncyclopediaTab.fsi` and `.fs` — Skia-rendered scrollable list-detail layout: left-pane filterable list (faction / tier / role chips), right-pane detail card with cost / health / build time / weapons / movement / build options + the rendered glyph (FR-019–FR-022, AS-5.1, AS-5.2).

**Checkpoint**: Users can browse and understand the unit catalog from inside the
hub.

---

## Phase 8: User Story 6 — Embedded style configurator (Priority: P3)

**Goal**: The existing feature-033 style configurator is reachable from the hub
without entering a separate mode; changes apply to the embedded viewer live;
named presets save/load through the existing `viz-presets/*.json` format.

**Independent Test**: With a session running, open Configurator, change a color
swatch → viewer updates within one frame. Save preset under `hub-qa`, restart
hub, reopen — preset is restored (`quickstart.md` US6, AS-6.1, AS-6.2).

### Tests for User Story 6

- [ ] T057 [P] [US6] Implement `tests/FSBar.Hub.Tests/ConfiguratorPresetTests.fs` — round-trip a `VizConfig` mutation through `StylePreset.fromConfig` → write to a temp `viz-presets/` → re-read via `StylePreset.applyToConfig` → assert byte-equivalent (reuses existing feature-033 helpers).

### Implementation for User Story 6

- [ ] T058 [US6] Implement `src/FSBar.Hub.App/Tabs/ConfiguratorTab.fsi` and `.fs` — wraps the existing `FSBar.Viz.ConfigPanel` from feature 033, sourcing the live `VizConfig` from the same instance the `ViewerTab` renders against. Apply changes within one frame. Add hub-side "Save preset" / "Load preset" buttons backed by `FSBar.Viz.StylePreset` (FR-023, FR-024).

**Checkpoint**: Style customization is accessible without leaving the hub.

---

## Phase 9: User Story 7 — gRPC scripting API for external clients (Priority: P3)

**Goal**: External `.fsx` (and other) clients connect to `127.0.0.1:5021`,
subscribe to the gamestate stream, send commands. Multi-client safe; slow
clients are isolated; disconnect doesn't affect other clients or the viewer.

**Independent Test**: With a session running, run
`dotnet fsi src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx` →
prints first 5 frames + sends one no-op command + disconnects cleanly. First
frame ≤ 2 s after connect (`quickstart.md` US7, SC-004, SC-005, SC-006).

### Tests for User Story 7

- [ ] T059 [P] [US7] Add `tests/FSBar.Hub.Tests/Baselines/ScriptingHub.baseline`.
- [ ] T060 [P] [US7] Implement `tests/FSBar.Hub.Tests/ScriptingHubTests.fs` — in-process fan-out: instantiate `ScriptingService` with a fake `SessionManager` whose `Frames` is a controllable subject, attach 5 simulated client channels, push 100 frames, assert every channel received every frame in order (SC-005). Then attach a sixth "slow" client whose drain is artificially delayed; push frames until cumulative drops > 32, assert that client gets `ScriptingClientDetached(OverflowDropLimit)` while the other five continue uninterrupted (SC-006, R3).
- [ ] T061 [P] [US7] Implement `tests/FSBar.Hub.LiveTests/LiveGrpcStreamTests.fs` — stand up the full hub host in-process, launch a real session, connect a real gRPC client, assert first frame received within 2 s (SC-004), send one no-op `AICommand` via `SendCommand`, assert it appears in the next frame's command-history slot (FR-027, AS-7.2). Disconnect mid-stream, assert the hub remains alive (FR-029).
- [ ] T062 [P] [US7] Implement `tests/FSBar.Hub.LiveTests/LiveGrpcEmptyStreamTests.fs` — connect a gRPC client BEFORE launching any session, assert the connection succeeds, assert no frames flow until a session starts, then launch a session and assert frames begin flowing (FR-030, AS-7.4).

### Implementation for User Story 7

- [ ] T063 [US7] Implement `src/FSBar.Hub/ScriptingHub.fsi` and `.fs` — `ScriptingHubOptions` (defaults: `FrameBufferCapacity=16`, `MaxCumulativeDrops=32`), `ScriptingService` constructor wired with `SessionManager` + `IHubEventSink` + `UnitDefCache` + opts. Implements the four RPCs from `scripting.proto`: `StreamGameFrames` (per-client `BoundedChannel<GameFrameMessage>(16, DropOldest)`, single internal subscriber to the **unthrottled** `SessionManager.Frames` enqueues to all active client channels — viewer-side 60fps cap (T030) MUST NOT influence this stream (FR-026), per-client `IAsyncEnumerable` drains to gRPC stream; on cumulative drop ≥32 emit `ScriptingClientDetached(OverflowDropLimit)` + close stream — research.md R3); honors `close_on_session_end` by closing the stream when `SessionState` transitions out of `Running`; `SendCommand` (forwards to `RunningSession.BarClient.Commands`, returns NOT_FOUND when `SessionState ≠ Running`); `GetSessionStatus` (assembles `GetSessionStatusResponse` from `SessionManager.State` + `BarInstall` + `BundledProxy` + connected client roster); `GetUnitDef` (looks up via `UnitDefCache` by id-or-name).
- [ ] T064 [US7] Wire `src/FSBar.Hub.App/Program.fs` to register `ScriptingService` into the Kestrel host via `app.MapGrpcService<ScriptingService>()` (per `fsgrpc-server` skill). Hosted on the port from `HubSettings.GrpcPort` with HTTP/2 cleartext.
- [ ] T065 [US7] Implement `src/FSBar.Hub.App/Tabs/GrpcTab.fsi` and `.fs` — endpoint URL display (`http://127.0.0.1:<port>`), connected-client roster (id, label, remote, attached-at, cumulative dropped frames), recent `ScriptingClientConnected` / `ScriptingClientDetached` event log subscribed from `HubEventBus`.
- [ ] T066 [P] [US7] Author `src/FSBar.Hub/scripts/prelude.fsx` — single `#load`-able entrypoint that sets up `#r` references to `FSBar.Proto`, `FSBar.Client`, `FSBar.Hub`, and the FsGrpc client runtime (per constitution §V).
- [ ] T067 [P] [US7] Author `src/FSBar.Hub/scripts/examples/01-detect-bar-install.fsx` — calls `BarInstall.detect`, prints the active engine version + AI list.
- [ ] T068 [P] [US7] Author `src/FSBar.Hub/scripts/examples/02-install-proxy-dry-run.fsx` — calls `ProxyInstaller.checkStatus` + `health`, prints the `ProxyHealth` outcome WITHOUT performing any writes.
- [ ] T069 [P] [US7] Author `src/FSBar.Hub/scripts/examples/03-launch-and-stream.fsx` — opens an FsGrpc client to `127.0.0.1:5021`, calls `GetSessionStatus`, opens `StreamGameFrames`, prints the first 5 frames' `client_sequence` + `frame.frame_number`, sends one no-op `AICommand` via `SendCommand`, disconnects cleanly. **This is the script the SC-004 / `quickstart.md` US7 test runs.**
- [ ] T070 [P] [US7] Author `src/FSBar.Hub/scripts/examples/04-grpc-client-roundtrip.fsx` — connects, calls `GetUnitDef` for a known internal name (e.g. `armcom`), prints the result. Demonstrates the v1 contract scope (gamestate stream + command + unit-def lookup) listed in spec Assumptions.

**Checkpoint**: All seven user stories are independently functional. The hub is
the user's cockpit + the trainer / scripting clients have a stable detached API.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Final tightening — surface-area baselines refreshed, docs, README,
quickstart validation pass, and the Tier-1 evidence chain captured.

- [ ] T071 [P] Refresh ALL surface-area baselines under `tests/FSBar.Hub.Tests/Baselines/` and `tests/FSBar.Viz.Tests/Baselines/` (the latter for SceneBuilder + GameViz changes from Phase 2) via `SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx` and commit any drift.
- [ ] T072 [P] Update `CLAUDE.md` "Active Technologies" / "Recent Changes" sections to mention `FSBar.Hub` + `FSBar.Hub.App` + `Grpc.AspNetCore 2.67.0` + the bundled-proxy directory layout.
- [ ] T073 [P] Add a "Hub" section to the top-level `README.md` (or create one if missing) covering how to launch the hub, the `dotnet run --project src/FSBar.Hub.App` command, and a one-paragraph description of what each tab does. Cross-link to `specs/035-central-gui-hub/quickstart.md` and `proxy/README.md`.
- [ ] T074 [P] Ensure `scripts/refresh-bundled-proxy.sh` is executable (`chmod +x`), and that `proxy/README.md` documents the `HIGHBARV2_REPO` env var, `--force` flag, and the version-bump workflow.
- [ ] T075 Run the full `quickstart.md` end-to-end manually (US1 → US7) on a real BAR install. Capture the result in the PR description as the Tier-1 test evidence.
- [ ] T076 Constitution §V check: confirm `FSBar.Hub` packs cleanly to `~/.local/share/nuget-local/` via `dotnet pack src/FSBar.Hub`, and that `dotnet fsi src/FSBar.Hub/scripts/examples/01-detect-bar-install.fsx` works against the published package without referencing the source tree.
- [ ] T077 `tests/run-all.sh` smoke run on the full repo: assert `dotnet test FSBarV1.slnx` passes (or skips with reason for engine-dependent tests when no engine present per `tests/engine-version.json` policy).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories.
- **User Stories (Phases 3–9)**: All depend on Foundational completion. Within
  the user stories:
  - **US1 (P1)** is the MVP — every other story is testable only after US1's
    `SessionManager` and `LobbyConfig` exist (US3, US4, US6, US7 all need a
    live session to verify). Build US1 first.
  - **US2 (P1)** is independent of US1 in code (different modules) but
    operationally chained — without US2 first-run + proxy install, US1 only
    works on a hand-configured machine.
  - **US3, US4, US6** depend on US1's `SessionManager`/`ViewerTab` infrastructure
    being in place but do not depend on each other.
  - **US5** is independent of every other user story (read-only browser of
    BarData).
  - **US7** depends on US1's `SessionManager.Frames` (it fans out exactly that
    stream) but is otherwise independent.
- **Polish (Phase 10)**: Depends on all desired user stories being complete.

### User Story Dependencies (compact)

- **US1 (P1, MVP)**: needs Phase 2 only.
- **US2 (P1)**: needs Phase 2 only.
- **US3 (P2)**: needs US1's `SessionManager` + `ViewerTab` + `GameViz` overlay-state API (T013, T014, T028, T030).
- **US4 (P2)**: needs US1's `SessionManager` + `LobbyConfig` (T027, T028).
- **US5 (P3)**: needs Phase 2 only.
- **US6 (P3)**: needs US1's `ViewerTab` (T030) so changes are observable live.
- **US7 (P3)**: needs US1's `SessionManager.Frames` + Phase 2's Kestrel host (T018, T028).

### Within Each User Story

- Tests written first, fail-first / pass-after pattern (constitution §III).
- Models / pure types before services / IO.
- IO before UI.
- UI wiring last.

### Parallel Opportunities

- Phase 1 T002–T005 can all run in parallel (different `.fsproj`).
- Phase 2 T010–T012, T013/T014, T015/T016, and T020–T022 are largely parallel
  (different `.fs` files and different test files).
- Once Phase 2 completes, US1, US2, and US5 can be picked up by three different
  developers in parallel (US5 has no deps on US1/US2 in code).
- Within US7, T066–T070 (the four `.fsx` scripts + prelude) are all parallel.

---

## Parallel Example: User Story 1

```bash
# Independent tests that can run together (T023–T026):
Task: "Add baselines (T023)"
Task: "LobbyConfigTests.fs (T024)"
Task: "LiveSessionLaunchTests.fs (T025)"
Task: "StatusBarControlsTests.fs (T026)"

# Then implementation (some sequential, some parallel):
Task: "LobbyConfig.fs(i) (T027)"     # sequential — used by SessionManager + SetupTab
Task: "SessionManager.fs(i) (T028)"  # depends on T027
# T029, T030, T031 can then run in parallel — different files, all consume T027 + T028
Task: "SetupTab.fs(i) (T029)"
Task: "ViewerTab.fs(i) (T030)"
Task: "StatusBar wiring (T031)"
# T032 + T033 final wiring serially (Program.fs is one file)
```

---

## Implementation Strategy

### MVP First (US1 only)

1. Complete Phase 1 (Setup) — solution builds, projects exist.
2. Complete Phase 2 (Foundational) — settings, BAR detect, bundled-proxy
   detect, GUI shell, headless scene-build all work.
3. Complete Phase 3 (US1) — Launch button works end-to-end.
4. **STOP and VALIDATE**: Run `quickstart.md` US1 manually on a hand-configured
   machine. If it works, the hub is already a usable BAR launcher.
5. (Optionally demo at this point — first-run wizard not required if BAR was
   pre-configured.)

### Incremental Delivery After MVP

1. Add US2 (first-run + proxy install) → hub is now self-contained on a fresh
   machine. **This is the v1 ship target.**
2. Add US3 (overlay-toggle sync) and US4 (graphical engine toggle) together —
   small, both use existing US1 infrastructure.
3. Add US5 (encyclopedia) — read-only, low-risk.
4. Add US6 (configurator embedding) — wraps existing feature-033 code.
5. Add US7 (gRPC scripting) → trainer + external scripting clients can attach
   to live hub sessions.

### Parallel Team Strategy

With three developers after Phase 2 completes:

- Developer A: US1 (Phase 3) → US3 (Phase 5) → US4 (Phase 6).
- Developer B: US2 (Phase 4) → US6 (Phase 8).
- Developer C: US5 (Phase 7) → US7 (Phase 9).

US7 needs US1's `SessionManager.Frames` to test live, so Developer C should
synchronize with Developer A's US1 completion before starting Phase 9.

---

## Notes

- **Surface-area baselines**: every `.fsi` change in this feature requires
  regenerating its baseline via `SURFACE_AREA_UPDATE=1` and committing the
  diff. Do NOT add `private` / `internal` modifiers to non-generated source —
  surface is gated only by `.fsi` per constitution §II.
- **Tier classification (per plan)**: this is a **Tier 1** feature — full
  artifact chain required (spec + plan + `.fsi` updates + baselines + test
  evidence + docs).
- **Existing-component reuse**: this feature reuses `FSBar.Client`,
  `FSBar.Viz`, `FSBar.SyntheticData`, and `FSBar.Proto` — do NOT fork or
  duplicate logic. The only additive change to an existing project is the
  headless scene-build entrypoint in `FSBar.Viz` (T013) and the overlay-state
  accessors on `GameViz` (T014).
- **Test posture**: live tests guarded by engine-availability per
  `tests/engine-version.json` skip-when-missing convention; never marked passed
  if the underlying scenario can't be exercised (per `CLAUDE.md` Testing
  guidance).
- **Constitution gate (§EC) reminder**: `Grpc.AspNetCore` 2.67.0 is the only
  new transitive dependency and is recorded in `plan.md` Complexity Tracking.
  No new GUI framework is introduced — UI builds on the existing
  SkiaViewer + Silk.NET stack.
- **Avoid**: vague tasks, same-file conflicts marked [P], cross-story
  dependencies that break independent testability.
