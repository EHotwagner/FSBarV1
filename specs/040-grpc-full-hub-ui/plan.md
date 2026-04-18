# Implementation Plan: gRPC parity for Hub UI and rendered viewer

**Branch**: `040-grpc-full-hub-ui` | **Date**: 2026-04-18 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/040-grpc-full-hub-ui/spec.md`

## Summary

Expose every Hub-GUI action (Setup / Viewer / Units / Style / Cfg / gRPC tabs) through the existing `fsbar.hub.scripting.v1` gRPC service, add a server-streaming RPC that delivers the Viewer-tab's rendered Skia output to remote clients as image bytes, and add a bi-directional overlay pipeline so remote clients can upload declarative Skia primitives that the hub renders on top of the Viewer scene every frame. The approach has four pillars:

1. **Central `HubStateStore`** (new module in `FSBar.Hub`) becomes the authoritative owner of Hub UI state — active tab, `VizConfig`, Viewer-tab camera, encyclopedia selection, cached preset list. Tabs refactor into thin readers/writers; gRPC handlers write through the same store. Every write emits a typed event via the existing `HubEventBus`, so gRPC stream subscribers and the local GUI observe changes consistently (FR-015, FR-016, FR-020).
2. **`HeadlessRenderer`** (new module in `FSBar.Hub`) renders the Viewer scene off the GUI thread using the already-pure `SceneBuilder.buildSceneHeadlessView`, composes per-client overlay layers on top, rasterizes to an SKSurface, encodes PNG/JPEG, and fans out to per-subscriber bounded channels (reusing the 16-deep drop-oldest / detach-at-32 policy established for `StreamGameFrames`). Targets SC-008's P95 ≤ 200 ms at 10 Hz.
3. **`OverlayLayerStore`** (new module in `FSBar.Hub`) owns per-client, name-keyed overlay layers as declarative `OverlayPrimitive` lists. Handles `PutLayer` / `DeleteLayer` / `ListLayers` / `ClearLayers`, enforces FR-026 caps (16 layers/client, 500 primitives/layer, 1 MB/push, 2048²/256 KB per image primitive), and auto-deletes all of a client's layers on disconnect (per FR-025, SC-010). The render pipeline queries this store each frame to composite overlays above built-in overlays (FR-024, FR-027).
4. **Additive proto extension** — new RPCs (`ConfigureLobby`, `LaunchSession`, `StopSession`, `StreamRenderFrames`, `GetRenderFrame`, `SetVizAttribute`/`SetVizConfig`, `ToggleOverlay`, `SetCamera`, `ListPresets`/`SavePreset`/`LoadPreset`/`DeletePreset`, `ListUnits`/`SelectUnit`, `GetHubSettings`/`SetHubSettings`, `InstallProxy`/`RefreshProxyStatus`, `SetActiveTab`, `StreamHubStateEvents`, `GetHubState`, `PutLayer`/`DeleteLayer`/`ListLayers`/`ClearLayers`) + new messages (including `OverlayPrimitive` oneof, `CoordinateSpace` enum, and per-primitive geometry + styling), all additive, existing RPCs untouched (FR-019, SC-007).

Primary risks:
- Refactor from tab-local state to `HubStateStore` is invasive but mechanical. Six tabs each follow the same pattern.
- Overlay-composite add to `HeadlessRenderer` must stay within the SC-008 latency budget. Each overlay primitive is a simple Skia draw call (typical dashboard layers: a few dozen primitives) — budget impact is dwarfed by PNG encode (see R2). Per-client cap (FR-026) keeps even a maximally-loaded hub within ~1 ms extra draw time per frame per client.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints).
**Primary Dependencies**: Existing in-repo only — `FSBar.Proto`, `FSBar.Client`, `FSBar.Viz`, `FSBar.Hub`, `FSBar.Hub.App`. NuGet: `Grpc.AspNetCore 2.67.0`, `Grpc.Core.Api 2.67.0`, `FsGrpc 1.0.6`, `SkiaSharp 2.88.6`, `SkiaViewer 1.1.3-dev` (local feed), `BarData` (local feed), `xUnit 2.9.x`, `Microsoft.NET.Test.Sdk 17.x`. **No new NuGet dependencies.**
**Storage**: Filesystem only — unchanged from pre-feature. `$XDG_CONFIG_HOME/fsbar-hub/settings.json` (HubSettings), `viz-presets/*.json` (style presets), `bots/trainer/map-cache/*.json` (map analysis). `HubStateStore` and `HeadlessRenderer` are in-memory only.
**Testing**: `xUnit 2.9.x` in-process tests under `tests/FSBar.Hub.Tests/` (unit) and `tests/FSBar.Hub.LiveTests/` (integration against a real BAR engine). Live tests marked `[<Trait("Category", "UiParity")>]` so the full matrix can be filtered.
**Target Platform**: Linux x86-64 (container dev image / host), loopback gRPC only (`127.0.0.1:5021`). Hub continues to require a DISPLAY + GLFW for its GUI viewport; the new render-frame RPC uses an off-screen SKSurface that does not depend on the GUI's GRContext (raster backend, consistent with the CLAUDE.md segfault note).
**Project Type**: desktop-app (`FSBar.Hub.App` SkiaViewer executable) plus packable core library (`FSBar.Hub`) plus in-repo `.proto` contract (`proto/hub/scripting.proto`) hosting a gRPC service via `Kestrel` + `Grpc.AspNetCore`.
**Performance Goals**: Render-frame stream P95 ≤ 200 ms at 10 Hz (SC-008); preset round-trip ≤ 500 ms (SC-004); pixel fidelity ≥ 99% vs local Viewer tab (SC-003); 100% GUI-action coverage (SC-002); multi-client state convergence within one render frame (SC-005); no breakage of existing scripting clients (SC-007).
**Constraints**: Proto surface is additive-only (FR-019); loopback-only network surface (FR-017); no new dependencies (Assumptions); hub still requires DISPLAY (Assumptions); every public F# module needs a `.fsi` + surface baseline (Constitution II).
**Scale/Scope**: ~34 new or extended RPCs (4 added for overlay upload), ~14 new messages (6 added for overlay primitives/styling), three new `FSBar.Hub` modules (`HubStateStore`, `HeadlessRenderer`, `OverlayLayerStore`) plus `PresetFacade` and `EncyclopediaFacade`, six tab refactors, ~8 new live integration tests (one added for US6), ~5 new FSI example scripts (one added for US6). Concurrent scripting clients: single-digit (per the existing bounded-channel fanout budget). Overlay load per client capped at 16 layers × 500 primitives = 8000 primitives, measured 1 MB serialized max per `PutLayer`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I — Spec-First Delivery
- Spec at `specs/040-grpc-full-hub-ui/spec.md` exists, has 5 prioritized user stories, 21 FRs, 8 SCs, 3 clarifications. Tier 1 change (adds proto contract + new public F# modules). **PASS.**

### Principle II — Compiler-Enforced Structural Contracts
- Every new public module has a planned `.fsi`:
  - `src/FSBar.Hub/HubStateStore.fsi` (new)
  - `src/FSBar.Hub/HeadlessRenderer.fsi` (new)
  - `src/FSBar.Hub/OverlayLayerStore.fsi` (new, per-client overlay state + cap enforcement)
  - `src/FSBar.Hub/PresetFacade.fsi` (new, thin wrapper over `FSBar.Viz.StylePreset`)
  - `src/FSBar.Hub/EncyclopediaFacade.fsi` (new, thin wrapper over `FSBar.Viz.EncyclopediaData`)
- Extended `.fsi` (widened public surface): `ScriptingHub.fsi`, `SessionManager.fsi`, `HubEvents.fsi`, `HubSettings.fsi` (typed field accessors).
- Surface-area baselines updated under `tests/FSBar.Hub.Tests/Baselines/`. `SURFACE_AREA_UPDATE=1` used once after intentional widening, then committed.
- **PASS** gated on task-list including the `.fsi` + baseline updates.

### Principle III — Test Evidence Is Mandatory
- Each user story gets at least one independent test (unit for pure logic, live for real-engine interaction). Task list will enumerate per-story tests explicitly.
- **PASS** gated on tasks.

### Principle IV — Observability and Safe Failure Handling
- All new rejection paths emit a `HubEvent.DiagnosticsLine` with actionable context (`bad field name`, `session already running`, `preset name invalid`, etc.).
- New rate-limit/backpressure detach path on `StreamRenderFrames` emits the same `ScriptingClientDetached` event today's `StreamGameFrames` uses, with reason extended for `RenderOverflowDropLimit`.
- gRPC handlers map well-formed failures to canonical status codes (`INVALID_ARGUMENT`, `FAILED_PRECONDITION`, `NOT_FOUND`, `DATA_LOSS`).
- **PASS.**

### Principle V — Scripting Accessibility
- `FSBar.Hub` keeps its existing `scripts/prelude.fsx` + numbered examples. New examples:
  - `scripts/examples/17-hub-lobby-launch.fsx` (US1)
  - `scripts/examples/18-hub-render-frames.fsx` (US2)
  - `scripts/examples/19-hub-vizconfig-drive.fsx` (US3)
  - `scripts/examples/20-hub-state-observer.fsx` (US5)
  - `scripts/examples/21-hub-overlay-layers.fsx` (US6)
- Existing `16-hub-admin.fsx` MUST keep working (SC-007) — smoke-test task included.
- **PASS** gated on tasks.

### Engineering Constraints
- F# on .NET exclusive — YES.
- `.fsi` + baselines — see II above.
- `dotnet pack` → `~/.local/share/nuget-local/` — `FSBar.Hub` and `FSBar.Proto` packable today; their versions bump at feature completion.
- gRPC via `fsgrpc-setup` / `fsgrpc-server` / `fsgrpc-client` — the repository's existing `ScriptingService` is already wired through `fsgrpc`; the feature only regenerates via `cd proto && buf generate` and reuses the same generator/plumbing. **PASS.**

### Complexity Tracking
No violations; no entries required.

## Project Structure

### Documentation (this feature)

```text
specs/040-grpc-full-hub-ui/
├── plan.md                      # this file
├── spec.md                      # feature spec (existing)
├── research.md                  # Phase 0 output (this run)
├── data-model.md                # Phase 1 output (this run)
├── quickstart.md                # Phase 1 output (this run)
├── contracts/
│   ├── scripting.proto          # extended proto (new RPCs + messages) — overlay on proto/hub/scripting.proto
│   └── fsi/
│       ├── HubStateStore.fsi
│       ├── HeadlessRenderer.fsi
│       ├── OverlayLayerStore.fsi
│       ├── PresetFacade.fsi
│       ├── EncyclopediaFacade.fsi
│       ├── ScriptingHub.additions.fsi
│       ├── SessionManager.additions.fsi
│       └── HubEvents.additions.fsi
└── checklists/
    └── requirements.md          # existing (from /speckit.specify)
```

### Source Code (repository root)

```text
proto/hub/
└── scripting.proto              # EXTEND — additive RPCs + messages

src/FSBar.Proto/
└── Generated/hub/scripting.gen.fs  # regenerate (buf generate)

src/FSBar.Hub/
├── HubStateStore.fsi            # NEW — central UI-state store
├── HubStateStore.fs
├── HeadlessRenderer.fsi         # NEW — off-screen Viewer render pipeline (composes OverlayLayerStore)
├── HeadlessRenderer.fs
├── OverlayLayerStore.fsi        # NEW — per-client named overlay layers + cap enforcement
├── OverlayLayerStore.fs
├── PresetFacade.fsi             # NEW — thin wrapper over FSBar.Viz.StylePreset
├── PresetFacade.fs
├── EncyclopediaFacade.fsi       # NEW — thin wrapper over FSBar.Viz.EncyclopediaData
├── EncyclopediaFacade.fs
├── ScriptingHub.fsi             # EXTEND — new RPC handler surface
├── ScriptingHub.fs
├── SessionManager.fsi           # EXTEND — Stop(), LobbyConfig read/write, IsLobbyEditable
├── SessionManager.fs
├── HubEvents.fsi                # EXTEND — new event cases (ActiveTab, VizConfig, Lobby,
├── HubEvents.fs                 #          Preset, Encyclopedia, Camera, StateSnapshotEvent)
├── HubSettings.fsi              # EXTEND — typed per-field read/write accessors
└── HubSettings.fs

src/FSBar.Hub.App/Tabs/
├── SetupTab.fs                  # REFACTOR — reads Lobby from HubStateStore instead of local
├── ViewerTab.fs                 # REFACTOR — reads camera + VizConfig from HubStateStore
├── EncyclopediaTab.fs           # REFACTOR — reads selection + filter from HubStateStore
├── ConfiguratorTab.fs           # REFACTOR — reads VizConfig + active preset from HubStateStore
├── SettingsTab.fs               # REFACTOR — reads HubSettings through store (for change events)
└── GrpcTab.fs                   # UNCHANGED (already event-driven from HubEvents)

scripts/examples/
├── 17-hub-lobby-launch.fsx      # NEW — US1
├── 18-hub-render-frames.fsx     # NEW — US2
├── 19-hub-vizconfig-drive.fsx   # NEW — US3
├── 20-hub-state-observer.fsx    # NEW — US5
└── 21-hub-overlay-layers.fsx    # NEW — US6

tests/FSBar.Hub.Tests/
├── HubStateStoreTests.fs        # NEW — LWW, event emission, concurrent writes
├── HeadlessRendererTests.fs     # NEW — pure encode timing, PNG dimensions, empty-session placeholder
├── OverlayLayerStoreTests.fs    # NEW — put/replace/delete, per-client isolation, cap enforcement, disconnect cleanup
├── PresetFacadeTests.fs         # NEW — round-trip, invalid-name rejection
├── EncyclopediaFacadeTests.fs   # NEW — filter predicate, select by id / internal name
├── ScriptingServiceUnaryTests.fs  # NEW — validation paths for every new unary RPC (incl. PutLayer / DeleteLayer)
└── Baselines/                   # UPDATED — surface-area baselines regenerated

tests/FSBar.Hub.LiveTests/
├── LiveHeadlessOrchestrationTests.fs  # NEW — US1
├── LiveRenderFrameStreamTests.fs      # NEW — US2 (pixel-compare, latency probe)
├── LiveVizConfigPushTests.fs          # NEW — US3
├── LivePresetRoundtripTests.fs        # NEW — US4
├── LiveHubStateEventTests.fs          # NEW — US5 (two-client convergence)
└── LiveOverlayLayerTests.fs           # NEW — US6 (PutLayer → frame contains primitive, disconnect cleanup)

tests/FSBar.Proto.Tests/Baselines/     # UPDATED — proto surface baselines
```

**Structure Decision**: Single-repo .NET/F# layout with existing four-project Hub split (`FSBar.Hub` core library, `FSBar.Hub.App` SkiaViewer executable, `FSBar.Proto` generated wire types, `FSBar.Hub.Tests` + `FSBar.Hub.LiveTests` test projects). All new code lands in `FSBar.Hub` (not the executable) so scripting clients that link only the core library get the full surface. Existing tab refactors stay in `FSBar.Hub.App`.

## Complexity Tracking

No Constitution violations; this section intentionally empty.
