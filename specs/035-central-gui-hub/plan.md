# Implementation Plan: Central GUI Hub App

**Branch**: `035-central-gui-hub` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/035-central-gui-hub/spec.md`

## Summary

Deliver a long-lived F# desktop application — `FSBar.Hub.App` — that owns the
end-to-end BAR play loop: detect / configure the BAR install, install the
bundled HighBarV2 proxy into Chobby, configure a full lobby (variable teams /
AI + human seats / mode / map / speed), launch a session, embed the existing
SkiaSharp live viewer, and expose a localhost gRPC scripting endpoint that
streams gamestates outbound and accepts commands inbound. A persistent
sidebar / tab bar plus a session-status bar (speed, pause/resume, end-session)
are visible from every tab.

Technical approach: a Skia + Silk.NET single-window UI (consistent with the
existing viewer / configurator pattern; **no new GUI framework**), an
`FSBar.Hub` core library that holds install / session / settings logic with
`.fsi` contracts and surface-area baselines, hub-side protobuf added to the
existing `FSBar.Proto` project (FsGrpc, contract-first per constitution), and
a fan-out gRPC service that decouples each scripting client onto its own
bounded `System.Threading.Channels.Channel` so a slow / dead client cannot
stall the viewer or other clients (extends the lock-free dataflow pattern from
feature 032). The bundled proxy lives under `proxy/bundled/<version>/` in this
repo; a maintainer refresh script copies it from a sibling HighBarV2 build.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per constitution §Engineering Constraints)

**Primary Dependencies**: Existing in-repo only —
`FSBar.Client` (`BarClient`, `EngineConfig`, `EngineDiscovery`, `EngineLauncher`,
`GameState`, `MapCacheFile`, `Commands`),
`FSBar.Viz` (`GameViz`, `SceneBuilder`, `LayerRenderer`, `UnitGlyph`,
`ConfigDescriptors`, `ConfigPanel`, `StylePreset`),
`FSBar.SyntheticData` (`SyntheticDataAdapter` for encyclopedia glyph rendering),
`FSBar.Proto` (extended with `hub/scripting.proto`),
`SkiaViewer` 1.1.3-dev (Window + declarative `Scene`),
`SkiaSharp` 2.88.6, `Silk.NET` 2.22.0,
`FsGrpc` 1.0.6 + `FsGrpc.Tools` 1.0.6 (already on `FSBar.Proto`),
`BarData` (NuGet local feed), `xUnit` 2.9.x.
**One new transitive dependency**: `Grpc.AspNetCore` 2.67.0 + `Microsoft.AspNetCore.App`
(framework reference) for hosting the scripting gRPC server — this is the
documented FsGrpc server-hosting path. No GUI framework added.

**Storage**:
- Hub settings: single JSON file at `$XDG_CONFIG_HOME/fsbar-hub/settings.json`
  (BAR data dir, engine dir override, last lobby, gRPC port).
- Style presets: existing `viz-presets/*.json` (feature 033, untouched format).
- Bundled proxy: committed under `proxy/bundled/<version>/{libSkirmishAI.so,AIInfo.lua,AIOptions.lua}`
  with a sibling `BUNDLED_VERSION` text file naming the active version.
- Maintainer refresh: `scripts/refresh-bundled-proxy.sh` copies from `../HighBarV2/build/`.

**Testing**: xUnit 2.9.x. Two new test projects: `FSBar.Hub.Tests` (unit:
settings round-trip, IGL_data.lua targeted-edit idempotency, lobby validation,
proxy-install dry-run on a tmp tree) and `FSBar.Hub.LiveTests` (integration:
launch a real session through the hub, gRPC fan-out, proxy install against a
disposable BAR data dir). Surface-area baselines per public module via the
existing `tests/Common/SurfaceAreaHelper.fs` wrapper.

**Target Platform**: Linux desktop x86_64, single-user. No cross-platform in v1.

**Project Type**: Desktop application + supporting F# library + gRPC service.
Multi-project addition to the existing `FSBarV1.slnx` solution.

**Performance Goals**:
- Viewer frame cadence ≤60 fps regardless of engine tick rate (FR-018).
- Launch → first viewer frame ≤30 s (SC-002).
- gRPC client first gamestate frame ≤2 s after connect (SC-004).
- ≥5 concurrent scripting clients with zero hub-attributable drops (SC-005).
- A slow / disconnected client MUST NOT degrade the viewer or other clients
  (FR-028, FR-029, SC-006) — enforced via per-client bounded channel + drop /
  detach policy on overflow.

**Constraints**:
- At most one active session in v1 (assumption).
- Hub only observes sessions it launched itself (FR-015a). The proxy installed
  for Chobby still serves human play but is opaque to the hub.
- gRPC endpoint is localhost-only, unauthenticated (assumption).
- Hub-owned engine processes MUST be torn down when the hub exits (FR-001).
  Implementation must survive both clean shutdown and SIGTERM / window close —
  use Linux process-group kill on the hub's child engine PIDs.
- IGL_data.lua edit is a *targeted* per-key rewrite (FR-008, AS-2.5).
  Strict requirement: bytes outside the matched key/value range must be
  preserved exactly — including comments, whitespace, key order.
- No modification of `packages/` or `pool/` (FR-010).

**Scale/Scope**:
- 7 user stories; 31 functional requirements; 8 success criteria.
- 1 new GUI executable project; 1 new core library project; existing
  `FSBar.Proto` extended; 2 new test projects.
- Estimated 6 hub-core modules + 6 UI tab modules + 1 gRPC service module.

## Constitution Check

*Tier classification (§I)*: This is a **Tier 1** change — new public F# API
surface, new transitive dependency (`Grpc.AspNetCore`), new inter-project
contract (`hub/scripting.proto`). Full artifact chain required: spec, plan,
`.fsi` updates, surface-area baselines, test evidence, documentation.

| Gate | Status | Notes |
|------|--------|-------|
| §I Spec-First Delivery | PASS | Spec finalized 2026-04-17 with 5 clarifications resolved; this plan is its successor; tasks will be story-grouped by `/speckit.tasks`. |
| §II Compiler-Enforced Structural Contracts | PASS | Every new `.fs` in `FSBar.Hub` and `FSBar.Hub.App` will ship with a curated `.fsi`. Surface-area baselines added under `tests/FSBar.Hub.Tests/Baselines/` via the shared `SurfaceAreaHelper`. The hub *application* exe still gets `.fsi` for any reusable modules; pure entrypoint glue stays unsignatured per existing convention. |
| §III Test Evidence Is Mandatory | PASS | Each user story (US1–US7) maps to at least one verification test (unit + live where applicable). Pre-fix / post-fix evidence captured in tasks via fail-first / pass-after pattern. |
| §IV Observability and Safe Failure | PASS | Each operationally significant event has a defined diagnostic: BAR-install validation failure → structured error w/ path; engine launch failure → captured infolog excerpt (FR-031); proxy-install step → step-level success/skip/fail log; gRPC client overflow → emit drop event + detach. No silent catches. |
| §V Scripting Accessibility | PASS | `FSBar.Hub` is a packable library — must ship `scripts/prelude.fsx` plus numbered examples (`01-detect-bar-install.fsx`, `02-install-proxy-dry-run.fsx`, `03-launch-and-stream.fsx`, `04-grpc-client-roundtrip.fsx`). The `FSBar.Hub.App` exe is the GUI shell and is intentionally not FSI-accessible — its FSI-relevant logic lives in `FSBar.Hub`. |
| §EC F# on .NET exclusive | PASS | All new code is F# 9 / .NET 10. The maintainer `refresh-bundled-proxy.sh` is a thin file-copy shell script — analogous to the existing `bots/trainer/map-cache/refresh-all.sh` and `tests/run-all.sh` precedents — and contains no application logic. |
| §EC Dependency minimization | PASS w/ justification | `Grpc.AspNetCore` is the FsGrpc-recommended server host (per `fsgrpc-setup`/`fsgrpc-server` skills). No additional GUI framework introduced — UI builds on the existing SkiaViewer + Silk.NET stack already validated by features 008 / 027 / 028 / 033. Recorded in Complexity Tracking. |
| §EC Library packaging | PASS | `FSBar.Hub` packs to `~/.local/share/nuget-local/`; `FSBar.Hub.App` is `OutputType=Exe` and is not packed. |
| §EC gRPC via FsGrpc | PASS | Hub-side service uses contract-first FsGrpc (`hub/scripting.proto` added to existing `FSBar.Proto` buf workspace). Server implemented per `fsgrpc-server`; example client per `fsgrpc-client`. |

**Result**: All gates pass. One justified addition (`Grpc.AspNetCore`)
recorded in Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/035-central-gui-hub/
├── plan.md              # this file
├── spec.md              # feature spec (already written)
├── checklists/
│   └── requirements.md  # spec-quality checklist (already written)
├── research.md          # Phase 0 output (this run)
├── data-model.md        # Phase 1 output (this run)
├── quickstart.md        # Phase 1 output (this run)
├── contracts/           # Phase 1 output (this run) — proto + .fsi sketches
└── tasks.md             # Phase 2 output (/speckit.tasks, NOT this run)
```

### Source Code (repository root)

```text
src/
├── FSBar.Proto/                        # existing — EXTENDED with hub/scripting.proto
│   ├── proto/highbar/                  # (existing engine protocol .protos, generated under Generated/highbar/)
│   └── proto/hub/                      # NEW
│       └── scripting.proto             # ScriptingService: StreamGameFrames, SendCommand, GetUnitDef, GetSessionStatus
├── FSBar.Hub/                          # NEW core library — packable
│   ├── HubSettings.fs(i)               # JSON persisted hub settings under XDG_CONFIG_HOME/fsbar-hub/
│   ├── BarInstall.fs(i)                # detect data dir, enumerate recoil_* engines, validate
│   ├── BundledProxy.fs(i)              # locate proxy/bundled/<v>/, parse BUNDLED_VERSION
│   ├── ProxyInstaller.fs(i)            # copy AI files, touch devmode.txt, IGL_data.lua targeted edit
│   ├── LobbyConfig.fs(i)               # SessionConfig: teams[], seats (AI/human/spectator), mode, handicap; validate(); toEngineConfig()
│   ├── SessionManager.fs(i)            # owns at-most-one session; lifecycle events; speed/pause/end controls
│   ├── ScriptingHub.fs(i)              # gRPC service impl — fan-out to N clients via per-client bounded Channel
│   ├── HubEvents.fs(i)                 # IObservable<HubEvent> for status bar (state, speed, pause, errors)
│   ├── scripts/
│   │   ├── prelude.fsx                 # constitution §V — single #load entrypoint
│   │   └── examples/
│   │       ├── 01-detect-bar-install.fsx
│   │       ├── 02-install-proxy-dry-run.fsx
│   │       ├── 03-launch-and-stream.fsx
│   │       └── 04-grpc-client-roundtrip.fsx
│   └── FSBar.Hub.fsproj                # PackageId=FSBar.Hub, packs to local nuget-local
├── FSBar.Hub.App/                      # NEW GUI executable
│   ├── Tabs/
│   │   ├── SetupTab.fs(i)              # lobby builder UI (variable teams/seats)
│   │   ├── ViewerTab.fs(i)             # hosts FSBar.Viz.GameViz; mirrors hotkeys + chrome toggles
│   │   ├── EncyclopediaTab.fs(i)       # BarData browser; reuses UnitGlyph for parity (FR-020, SC-003)
│   │   ├── ConfiguratorTab.fs(i)       # wraps existing ConfigPanel from feature 033
│   │   ├── SettingsTab.fs(i)           # BAR install paths, engine override, bundled proxy version
│   │   └── GrpcTab.fs(i)               # endpoint URL, connected clients, recent traffic
│   ├── Chrome/
│   │   ├── TabBar.fs(i)                # persistent left sidebar (FR-002a)
│   │   └── StatusBar.fs(i)             # session state + speed slider + pause + end-session (FR-015b–d)
│   ├── FirstRun.fs(i)                  # wizard: detect BAR → confirm → install proxy → done (US2)
│   ├── ProcessLifetime.fs(i)           # Linux process-group child kill on hub exit (FR-001)
│   ├── Program.fs                      # entry: build host, start gRPC, open SkiaViewer window
│   └── FSBar.Hub.App.fsproj            # OutputType=Exe; no PackageId
proxy/                                  # NEW (committed)
├── bundled/
│   └── <version>/                      # e.g. 0.1.17/
│       ├── libSkirmishAI.so
│       ├── AIInfo.lua
│       └── AIOptions.lua
├── BUNDLED_VERSION                     # plain text — single line: the active <version>
└── README.md                           # what this dir is, how to refresh
scripts/
└── refresh-bundled-proxy.sh            # NEW maintainer script: copies from ../HighBarV2/build/

tests/
├── FSBar.Hub.Tests/                    # NEW unit tests
│   ├── HubSettingsTests.fs             # JSON round-trip; XDG path resolution
│   ├── BarInstallTests.fs              # path validation against synthetic dirs
│   ├── ProxyInstallerTests.fs          # IGL_data.lua targeted edit idempotency + golden fixture
│   ├── LobbyConfigTests.fs             # validate(): min seats, mode-specific rules
│   ├── ScriptingHubTests.fs            # in-process fan-out: 5 clients, slow-client back-pressure
│   ├── Baselines/                      # surface-area baselines for FSBar.Hub
│   └── FSBar.Hub.Tests.fsproj
└── FSBar.Hub.LiveTests/                # NEW integration tests against real engine
    ├── LiveSessionLaunchTests.fs       # US1: full launch via SessionManager
    ├── LiveProxyInstallTests.fs        # US2: install into a disposable BAR data dir
    ├── LiveGrpcStreamTests.fs          # US7: external gRPC client receives frames
    └── FSBar.Hub.LiveTests.fsproj

FSBarV1.slnx                            # add the 4 new project entries
```

**Structure Decision**: Two new `src/` projects (`FSBar.Hub` library +
`FSBar.Hub.App` exe), one extension to `FSBar.Proto` (additive `hub/`
namespace), and two new `tests/` projects. The split keeps GUI code (Skia,
Silk.NET, SkiaViewer) out of the packable library and lets scripting clients
depend on `FSBar.Hub` (or just `FSBar.Proto` if they want the wire only)
without dragging GUI dependencies. The `proxy/` and `scripts/` top-level
additions match existing conventions (cf. `bots/trainer/`, `tests/run-all.sh`).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Adds `Grpc.AspNetCore` 2.67.0 + `Microsoft.AspNetCore.App` framework reference | FsGrpc's server-side hosting (`fsgrpc-server` skill) is built on Grpc.AspNetCore. The hub MUST run a gRPC server (FR-025–FR-030); there is no in-repo precedent yet. | Rolling our own gRPC server over raw HTTP/2 (rejected: large, error-prone, violates §EC "gRPC services MUST be set up using fsgrpc-setup"). Using a non-gRPC IPC (rejected: spec mandates gRPC; constitution mandates FsGrpc). |
| Two new src projects (`FSBar.Hub`, `FSBar.Hub.App`) instead of one | Library / exe split is required so scripting tooling (`prelude.fsx`, examples, downstream gRPC client packages) can consume the hub's logic without taking a hard dependency on Skia / Silk.NET / Grpc.AspNetCore hosting. | Single project (rejected: forces every consumer of `FSBar.Hub` types to drag in the GUI host stack — violates §EC dependency minimization for downstream packages). |
