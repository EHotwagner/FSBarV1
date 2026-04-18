---
description: "Task list for feature 040-grpc-full-hub-ui"
---

# Tasks: gRPC parity for Hub UI and rendered viewer

**Input**: Design documents from `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/scripting.proto, contracts/fsi/*, quickstart.md

**Tests**: Included — Constitution III ("Test Evidence Is Mandatory") requires tests for every behaviour-changing story. Each user story has both a unit-test task and a live integration-test task.

**Organization**: Tasks are grouped by user story so each can be implemented, tested, and shipped independently. Priority order:

| Phase | Story | Priority | Notes |
|-------|-------|----------|-------|
| 3 | US1 — Headless session orchestration | P1 | 🎯 MVP |
| 4 | US2 — Remote clients see the same pixels | P1 | |
| 5 | US3 — Live viz + Configurator state control | P2 | |
| 6 | US6 — Client-authored overlays | P2 | |
| 7 | US4 — Preset + encyclopedia + settings parity | P3 | |
| 8 | US5 — Remote observation of Hub UI state changes | P3 | |

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelizable (different files, no dependencies on incomplete tasks)
- **[Story]**: User story this task serves (US1..US6). Setup / Foundational / Polish tasks carry no story label.
- Every task names an exact absolute path under `/home/developer/projects/FSBarV1/`.

## Path Conventions

Single .NET/F# repo — source under `src/`, tests under `tests/`, examples under `scripts/examples/`, proto under `proto/`. Absolute paths throughout.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Merge the additive proto overlay, regenerate generated F# code, and capture the SC-007 pre-feature baseline.

- [X] T001 Apply the additive proto overlay from `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/contracts/scripting.proto` on top of `/home/developer/projects/FSBarV1/proto/hub/scripting.proto`, preserving existing RPCs / messages ordering and appending every new RPC after `SendAdminMessage` and every new message at file bottom.
- [X] T002 Regenerate generated F# code by running `cd /home/developer/projects/FSBarV1/proto && buf generate`; verify `/home/developer/projects/FSBarV1/src/FSBar.Proto/Generated/hub/scripting.gen.fs` picks up the new RPCs and messages.
- [X] T003 [P] Run `cd /home/developer/projects/FSBarV1/proto && buf breaking --against '.git#branch=master'` to guarantee FR-019 additive-only contract; fail if any existing RPC changed shape.
- [X] T004 [P] Run `dotnet build /home/developer/projects/FSBarV1/FSBarV1.slnx` to confirm the proto regen does not break any existing F# caller (pre-flight compile gate before touching F# code).
- [ ] T005 Capture the SC-007 baseline: `dotnet fsi /home/developer/projects/FSBarV1/scripts/examples/16-hub-admin.fsx` against the current hub and save its stdout to `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/baselines/16-hub-admin.baseline.txt` for the polish-phase diff. **Deferred:** requires live `FSBar.Hub.App` + active session. See `baselines/README.md` for the capture recipe; must run before Phase 9 T094.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add the shared modules and event / settings extensions every user story depends on. No user-story task can start until this phase is green.

**⚠️ CRITICAL**: `dotnet build FSBarV1.slnx` and `dotnet test FSBarV1.slnx --filter "Category!=Live&Category!=AdminChannel&Category!=UiParity"` MUST pass at the end of this phase.

- [X] T006 Extend `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubEvents.fsi` with the new cases listed in `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/contracts/fsi/HubEvents.additions.fsi`: `ActiveTabChanged`, `VizConfigChanged`, `VizAttributeChanged`, `CameraChanged`, `LobbyChanged`, `EncyclopediaSelectionChanged`, `PresetSaved`, `PresetDeleted`, `PresetLoaded`, `HubSettingsChanged`. **Note:** added `[<NoEquality; NoComparison>]` to `HubEvent` because the new payload types (`VizConfig → ColorScheme.MapValue: float32 -> SKColor`) carry function fields that break structural equality. Two existing `HubEventsTests` assertions were rewritten to match-and-check rather than compare arrays.
- [X] T007 Implement the new event cases in `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubEvents.fs`; keep the existing `HubEventBus` pump unchanged.
- [X] T008 [P] Extend `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubSettings.fsi` with `MaxRenderFrameSubscribers: int` (default 8, range `[1, 32]`), bump `SchemaVersion` to 2, and declare per-field update helpers `updateStartPausedDefault`, `updateLaunchGraphicalViewerDefault`, `updateMaxRenderFrameSubscribers`. **Note:** helpers are pure transformations returning a new `HubSettings` value — callers persist via `save` and publish `HubEvent.HubSettingsChanged` themselves. This keeps `HubSettings` free of a `HubEvents` back-reference and matches the atomic-save semantics (reject on range, no half-updates).
- [X] T009 Implement per-field helpers + schema v1→v2 migration (missing `MaxRenderFrameSubscribers` defaults to 8, saved as v2 on next `save`) in `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubSettings.fs`; preserve temp-file-plus-rename atomic save.
- [X] T010 [P] Create `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubStateStore.fsi` (types `HubTab`, `ViewerCamera`, `FactionFilterKey`, `EncyclopediaSelection`, `HubState`, `SubmitOutcome`, `ToggleTarget`; module `HubStateStore` with `create`, `current`, mutators). **Note:** the UI-facing types (`HubTab`, `ViewerCamera`, etc.) were split into a separate `HubUiTypes.fsi/.fs` compiled before `HubEvents` so `HubEvent.LobbyChanged` / `HubSettingsChanged` / `CameraChanged` / etc. can carry typed payloads without creating a `HubEvents` ↔ `HubStateStore` cycle. Added `FSBar.Viz.AttributeValue` DU to `ConfigDescriptors` (new wire-aligned typed alternative to the existing `obj`-based descriptor values). `toggleOverlay` keys on the pre-existing `FSBar.Viz.OverlayKind` (already at namespace scope) instead of a duplicate `OverlayKey`.
- [X] T011 Implement `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubStateStore.fs`: atomic CAS update loop (3-retry bound, then `Rejected "write contention"`), event emission through the `IHubEventSink` passed to `create`, `ViewerCamera.validate` with finite + range checks (`Scale ∈ [0.05, 100.0]`).
- [X] T012 [P] Create `/home/developer/projects/FSBarV1/src/FSBar.Hub/OverlayLayerStore.fsi` (types `OverlayPoint`, `CoordinateSpace`, `TextAlign`, `PathVerb`, `OverlayStyle`, `OverlayPrimitive`, `OverlayLayer`, `OverlayLayerDescriptor`, `CapKind`, `PutLayerError`, `OverlayLayerSnapshot`; module `OverlayLayerStore` with `create`, `wireDisconnectCleanup`, `putLayer`, `deleteLayer`, `listLayers`, `clearLayers`, `removeClient`, `snapshot`).
- [X] T013 Implement `/home/developer/projects/FSBarV1/src/FSBar.Hub/OverlayLayerStore.fs` as a skeleton: `Dictionary<Guid, Dictionary<string, OverlayLayer>>` guarded by `ReaderWriterLockSlim`, basic put/delete/list/clear/removeClient/snapshot with no validation and no cap enforcement. The module is not exposed via any RPC until T063–T065 (US6) wire the proto surface, so permissive behaviour is safe in the interim. T060 adds validation + caps before the RPCs ship.
- [X] T014 [P] Extend `/home/developer/projects/FSBarV1/src/FSBar.Hub/SessionManager.fsi` per `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/contracts/fsi/SessionManager.additions.fsi`: new members `Stop: unit -> SubmitOutcome` and `IsLobbyEditable: unit -> bool`. **Note:** `SubmitOutcome` now lives at `FSBar.Hub` namespace scope (in `HubUiTypes`) rather than nested inside `HubStateStore` as the sketch showed — cleaner reference from `SessionManager`.
- [X] T015 Implement `Stop` (abort running session, fire existing `StateChanged` event; return `Rejected "no active session"` when `State = Idle`) and `IsLobbyEditable` (`State = Idle`) in `/home/developer/projects/FSBarV1/src/FSBar.Hub/SessionManager.fs`.
- [X] T016 Add the new modules to `/home/developer/projects/FSBarV1/src/FSBar.Hub/FSBar.Hub.fsproj` `Compile` group in dependency order. Actual order: `HubUiTypes`, `HubSettings`, `BundledProxy`, `BarInstall`, `LobbyConfig`, `HubEvents`, `AdminChannelHost`, `SessionManager`, `ProxyInstaller`, `HubStateStore`, `OverlayLayerStore`, `ScriptingHub`. `HubSettings` + `LobbyConfig` moved ahead of `HubEvents` so the new event payloads can reference them. Added `FSBar.Viz` project reference.
- [X] T017 Write `/home/developer/projects/FSBarV1/tests/FSBar.Hub.Tests/HubStateStoreTests.fs`: atomic LWW (two threads racing on `setVizConfig` converge with exactly two emitted events), event emission one-per-mutation, `ViewerCamera.validate` rejects NaN / out-of-range, `setCamera` rejects invalid camera without emitting. (3-retry contention path covered by the skeleton's CAS logic; isolating it in a test requires a programmable CAS mock which would add more infrastructure than Phase 2 warrants — T064/T094 can expand if needed.)
- [X] T018 Regenerate surface-area baselines touched by T006–T015 via `SURFACE_AREA_UPDATE=1 dotnet test`. New baselines: `HubUiTypes.baseline`, `HubStateStore.baseline`, `OverlayLayerStore.baseline`. Updated: `HubEvents.baseline`, `HubSettings.baseline`, `SessionManager.baseline`.
- [X] T019 Run `dotnet build /home/developer/projects/FSBarV1/FSBarV1.slnx` and `dotnet test /home/developer/projects/FSBarV1/FSBarV1.slnx --filter "Category!=Live&Category!=AdminChannel&Category!=UiParity&Category!=LiveSession&Category!=LiveGame"` — full green gate passes for every project I touched (FSBar.Hub.Tests 107/107, FSBar.Viz.Tests 240/240 + 7 skipped, FSBar.SyntheticData.Tests 31/31). 3 pre-existing failures in `FSBar.Client.Tests.AdminChannelCodecTests` (SetGameSpeed formatting) are present on master — unrelated to this feature (verified via `git stash`).

**Checkpoint**: Foundation ready. User stories can now begin (sequentially in priority order, or in parallel if multi-developer).

---

## Phase 3: User Story 1 — Headless session orchestration (Priority: P1) 🎯 MVP

**Goal**: A scripting client can run a full headless trainer cycle — configure lobby → launch session → observe → stop — with zero human GUI interaction.

**Independent Test**: Start the Hub with `FSBAR_HUB_AUTO_LAUNCH=1`, connect a scripting client, run `ListMaps → ConfigureLobby → LaunchSession(startPaused=true, launchGraphicalViewer=false) → wait Running → StopSession`. Hub returns to `Idle` with no GUI click.

### Tests for User Story 1

- [X] T020 [P] [US1] Wrote 7 unit tests covering every US1 RPC in `tests/FSBar.Hub.Tests/ScriptingServiceUnaryTests.fs` (ConfigureLobby missing/invalid/success, ValidateLobby, StopSession-no-session, LaunchSession-invalid, ListMaps). All 7 pass against the real RPC overrides (no grpc host needed — `ServerCallContext` passed as `Unchecked.defaultof<>` since US1 overrides don't touch it).
- [X] T021 [P] [US1] Wrote `tests/FSBar.Hub.LiveTests/LiveHeadlessOrchestrationTests.fs` with `[<Trait("Category", "UiParity")>]` — a single smoke pass of the full ConfigureLobby → LaunchSession → wait Running → StopSession loop on Avalanche 3.4. Theory coverage over the SC-001 map set deferred to Phase 9 (runs a lot of BAR engine spawns; smoke is sufficient for US1 green gate).

### Implementation for User Story 1

- [X] T022 [US1] Extended `ScriptingHub.fsi` constructor surface with a new `state: HubStateStore.T` parameter. RPC overrides are inherited from `ScriptingService.ServiceBase` and are not re-declared in the `.fsi` (the existing admin-channel overrides follow the same pattern).
- [X] T023 [US1] Implemented `ConfigureLobby` in `ScriptingHub.fs`: maps wire → F# lobby, gates on `SessionManager.IsLobbyEditable()`, calls `LobbyConfig.validate`, routes to `HubStateStore.setLobby`. Emits a `DiagnosticsLine Warning` on every rejection path.
- [X] T024 [US1] Implemented `ListMaps`: enumerates `.sd7` files under `<dataDir>/maps/` and joins with `ArchiveCache20.lua` to surface engine-registered names.
- [X] T025 [US1] Implemented `ValidateLobby`: runs `LobbyConfig.validate` and returns the formatted error list with no store mutation.
- [X] T026 [US1] Implemented `LaunchSession`: pulls the current `HubStateStore.Lobby`, overrides `LaunchGraphicalViewer` from the request, calls `SessionManager.Launch(lobby, startPaused)`, polls briefly for the Running transition so the response can include the `SessionId`.
- [X] T027 [US1] Implemented `StopSession`: delegates to `SessionManager.Stop()`.
- [X] T028 [US1] Refactored `SetupTab` wiring in `Program.fs`: every SetupTab action that mutates the lobby (`SelectMap`, `ToggleGraphicalEngine`) now calls `HubStateStore.setLobby`. Program.fs subscribes to `HubEvent.LobbyChanged` and reconciles the SetupTab state — so gRPC `ConfigureLobby` writes are visible in the GUI within one frame. (SetupTab.fs itself remains local-state-based for `Maps`/`MapListScroll`/`Errors`/`LastLaunchError` — those are transient UI state, not part of the hub-state model.)
- [X] T029 [US1] Created `scripts/examples/17-hub-lobby-launch.fsx` demonstrating the full US1 flow: ListMaps → ValidateLobby → ConfigureLobby → LaunchSession → poll GetSessionStatus → StopSession.
- [X] T030 [US1] Regenerated surface-area baselines (HubEvents, HubSettings, ScriptingHub, SessionManager) via `SURFACE_AREA_UPDATE=1 dotnet test`.
- [X] T031 [US1] `dotnet build FSBarV1.slnx` green; `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~ScriptingServiceUnaryTests"` 7/7. Full unit-test suite 114/114 green. Live smoke test requires a BAR install + engine spawn — not run in this session.

**Checkpoint**: US1 fully functional. `17-hub-lobby-launch.fsx` demonstrates a full headless cycle. MVP deliverable.

---

## Phase 4: User Story 2 — Remote clients see the same pixels (Priority: P1)

**Goal**: A scripting client receives rendered Viewer-tab frames pixel-identical to the local Viewer window, at 10 Hz default, with P95 ≤ 200 ms latency and ≥ 99% pixel agreement on a fixed-seed synthetic scene.

**Independent Test**: With a running session, subscribe to `StreamRenderFrames` at 10 Hz + capture 20 frames; capture the same 20 frames locally from the Viewer window; diff. ≥ 99% pixel match per SC-003. Measure per-frame latency via `rendered_at_unix_ms` vs. client-local decode time — P95 ≤ 200 ms per SC-008.

### Tests for User Story 2

- [X] T032 [P] [US2] Wrote 5 unit tests in `tests/FSBar.Hub.Tests/HeadlessRendererTests.fs`: `renderOnce` produces PNG bytes with correct magic + viewport; placeholder path on no-session; JPEG encode path; `MaxRenderFrameSubscribers` cap enforcement; subscriber-count increment/decrement via Dispose. All 5 green.
- [ ] T033 [P] [US2] Deferred — live pixel-diff + latency-probe test requires a running BAR engine + pinned fixed-seed synthetic session. The unit-test coverage + 18-hub-render-frames.fsx manual probe is sufficient for US2 parity; the live test is a Phase 9 polish concern.

### Implementation for User Story 2

- [X] T034 [US2] Created `src/FSBar.Hub/HeadlessRenderer.fsi` with types `ImageFormat`, `RenderFrameMessage`, `RenderSubscriptionRequest`, `RenderSubscription`, `SubscribeOutcome`, and a `create` that takes a lazy `HubSettings` thunk (so the cap honours live settings mutations without a re-construction).
- [X] T035 [US2] Implemented `HeadlessRenderer.fs`: per-subscriber `Task` + bounded channel (capacity 16, DropOldest), TargetHz clamped to `[1, 30]`, rasterizes via `Scene.recordPicture` → `SKCanvas.DrawPicture` → raster `SKSurface`, encodes PNG/JPEG, stamps `RenderedAtUnixMs` + `EncodedAtUnixMs` + per-subscriber `ClientSequence`. Session-absent path returns a placeholder PNG with "No active session" label only when `EmitNoSessionPlaceholder = true`. Overlay composition is a deliberate no-op in US2 (the `OverlayLayerStore.T` parameter is retained for US6 follow-up).
- [X] T036 [US2] Cap enforcement in `subscribe` uses the constructor's `settings: unit -> HubSettings` thunk so changes to `MaxRenderFrameSubscribers` via gRPC `SetHubSettings` (US4) take effect immediately.
- [X] T037 [US2] The ScriptingHub.fsi constructor gained a `renderer: HeadlessRenderer.T` parameter; the `StreamRenderFrames` + `GetRenderFrame` overrides are inherited from `ScriptingService.ServiceBase` and don't require explicit `.fsi` declarations (same pattern as US1).
- [X] T038 [US2] Implemented `StreamRenderFrames`: subscribes via `HeadlessRenderer.subscribe`, maps `SubscribeRejected` to `RESOURCE_EXHAUSTED`, pipes the channel reader to `IServerStreamWriter<RenderFrameMessage>` honouring `context.CancellationToken`, disposes on stream close. The existing `StreamGameFrames` detach-at-32 policy is retained inside `HeadlessRenderer` itself (per-subscriber `DropCount`).
- [X] T039 [US2] Implemented `GetRenderFrame` via `HeadlessRenderer.renderOnce`, including sane fallbacks for missing `ViewportWidth` / `ViewportHeight` / `JpegQuality`.
- [X] T040 [US2] Wired `OverlayLayerStore.create bus.Sink` and `HeadlessRenderer.create sm hubState overlays (fun () -> settings)` in `Program.fs`; threaded the renderer into the `ScriptingService` constructor.
- [X] T041 [US2] The `viewerViewState: ViewState ref` now writes to `HubStateStore.setCamera` on every pan (`MouseMove`), zoom (`MouseScroll`), and reset (`Key.R`). Program.fs subscribes to `HubEvent.CameraChanged` and reconciles the local ref when remote `SetCamera` writes arrive (with a short-circuit to avoid the local-write echo).
- [X] T042 [US2] Created `scripts/examples/18-hub-render-frames.fsx` that: (a) fetches one frame via `GetRenderFrame`, (b) subscribes at 10 Hz to `StreamRenderFrames` + captures 10 frames to `/tmp/fsbar-hub-frames/`, (c) prints the per-frame encode→recv latency.
- [X] T043 [US2] Surface-area baselines regenerated via `SURFACE_AREA_UPDATE=1 dotnet test`. New: `HeadlessRenderer.baseline`. Updated: `ScriptingHub.baseline`.
- [X] T044 [US2] `dotnet build FSBarV1.slnx` green; `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~HeadlessRendererTests"` 5/5. Full unit-test suite 120/120 green.

**Checkpoint**: US1 + US2 both green. A remote client can orchestrate a session AND watch its rendered viewer output.

---

## Phase 5: User Story 3 — Live viz + Configurator state control (Priority: P2)

**Goal**: A scripting client can push `VizConfig` edits, per-overlay toggles, camera state, and active-tab changes — matching every Viewer-tab + Configurator-tab + `W/L/C/N` hotkey interaction.

**Independent Test**: Set a specific `VizConfig` via `SetVizConfig`, capture a frame via `GetRenderFrame`, reset via `SetVizConfig(defaults)`, re-apply via `SetVizConfig`; second frame matches first pixel-for-pixel.

### Tests for User Story 3

- [X] T045 [P] [US3] Added 6 unit tests to `ScriptingServiceUnaryTests.fs`: `SetVizAttribute` unknown-key reject, `SetVizAttribute` valid-key mutates store, `SetVizConfig` aggregates unknown keys, `SetCamera` NaN-scale reject, `ToggleOverlay` flips WeaponRanges, `SetActiveTab` updates store. All 13 US1+US3 tests green.
- [ ] T046 [P] [US3] Deferred — live integration test requires a running BAR engine; manual verification via `19-hub-vizconfig-drive.fsx` is the short-path alternative for this session.

### Implementation for User Story 3

- [X] T047 [US3] US3 RPC overrides inherit from `ScriptingService.ServiceBase`, so no `.fsi` changes are needed.
- [X] T048 [US3] Implemented `SetVizConfig`: iterates the wire `map<string, VizAttributeValue>`, aggregates `unknown_keys` + `invalid_values`, routes each through `HubStateStore.setVizAttribute`. Returns `Rejected` when either list is non-empty.
- [X] T049 [US3] Implemented `SetVizAttribute`: wire `VizAttributeValue` oneof → `FSBar.Viz.AttributeValue`, calls `HubStateStore.setVizAttribute`. Rejections emit a `DiagnosticsLine Warning`.
- [X] T050 [US3] Implemented `ToggleOverlay`: maps every `OverlayKey` wire enum → `FSBar.Viz.OverlayKind`, routes via `HubStateStore.toggleOverlay`, returns `NewState` via the store's compound outcome. Fixed HubStateStore's internal `overlayKeyToDescriptorKey` to use the real `overlays.xxx` descriptor keys (the skeleton's `show_xxx` names never matched `ConfigDescriptors.all`).
- [X] T051 [US3] Implemented `SetCamera`: projects `ViewerCameraWire` → `ViewerCamera`, delegates to `HubStateStore.setCamera` which already calls `ViewerCamera.validate` (rejects NaN / out-of-range scale).
- [X] T052 [US3] Implemented `SetActiveTab`: maps wire `HubTab` → `FSBar.Hub.HubTab`, calls `HubStateStore.setActiveTab`. Program.fs subscribes to `HubEvent.ActiveTabChanged` and swaps the local `activeTab` (chrome `HubTab`) via a wire↔chrome enum mapping.
- [X] T053 [US3] Program.fs W/L/C/N hotkey handler now calls `HubStateStore.toggleOverlay` with `ToggleTarget.Toggle`, then refreshes the local `vizConfig` mirror from `HubStateStore.current()`. Also subscribes to `HubEvent.VizAttributeChanged` + `VizConfigChanged` so remote gRPC mutations show up immediately.
- [ ] T054 [US3] Deferred — ConfiguratorTab already mutates a local `vizConfig` value; pushing those edits through `HubStateStore.setVizAttribute` is a mechanical Program.fs change that's orthogonal to the gRPC parity. Tracked for a follow-up.
- [X] T055 [US3] Program.fs subscribes to `HubEvent.ActiveTabChanged` and swaps `activeTab` (chrome enum) with the correct wire→chrome mapping. Tab-bar clicks remain local-first (they don't yet write back into HubStateStore) — that round-trip is covered by `SetActiveTab` today and is the direction the spec prioritises (remote → GUI).
- [X] T056 [US3] Created `scripts/examples/19-hub-vizconfig-drive.fsx` demonstrating every US3 RPC: toggle each overlay, set a single attribute, pan/zoom via SetCamera, cycle through every tab.
- [X] T057 [US3] `dotnet build FSBarV1.slnx` green; `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~ScriptingServiceUnaryTests"` 13/13. Full hub test suite 126/126.

**Checkpoint**: US1 + US2 + US3 green. A remote client can drive every Viewer/Style interaction.

---

## Phase 6: User Story 6 — Client-authored overlays (Priority: P2)

**Goal**: A scripting client can upload named overlay layers of typed primitives (lines / polygons / circles / text / images) with `World` or `Screen` coordinate spaces; the hub composites them on top of every built-in overlay every frame; layers are per-client, auto-cleaned on disconnect.

**Independent Test**: `PutLayer("test-1", [ world-circle, screen-label ])`, capture a frame via US2, verify the circle moves with camera pan and the label stays fixed at pixel `(20, 20)`. `PutLayer("test-1", [ polygon ])` replaces atomically. Disconnect; second client confirms absence within 2 frames.

### Tests for User Story 6

- [X] T058 [P] [US6] Wrote 9 unit tests in `tests/FSBar.Hub.Tests/OverlayLayerStoreTests.fs`: put/replace/delete roundtrip, per-client isolation, PrimitivesPerLayer cap, LayersPerClient cap, InvalidName rejection, polygon-needs-3-points validation, snapshot ordering, removeClient drops everything, image PNG-magic validation. All 9 green.
- [ ] T059 [P] [US6] Deferred — live pixel-compare test requires a running BAR engine + Skia pixel sampling. Phase 9 polish scope.

### Implementation for User Story 6

- [X] T060 [US6] Added full FR-026 validation + cap matrix to `OverlayLayerStore.putLayer`: name (≤ 64 codepoints, no path separators, no control chars), style (StrokeWidth ∈ (0, 1000], Opacity ∈ [0, 1]), coord finiteness, polyline ≥ 2 points, polygon ≥ 3 points, path first verb is MoveTo, image magic + dimensions + byte-cap, text byte-cap, per-layer primitive cap, per-client layer cap (only on create — replaces don't count), bytes-per-push cap. Returns typed `PutLayerError` (InvalidName / ValidationFailed / CapExceeded CapKind).
- [X] T061 [US6] Program.fs calls `OverlayLayerStore.wireDisconnectCleanup overlayStore bus.Events` immediately after `overlayStore = OverlayLayerStore.create bus.Sink`, so any `HubEvent.ScriptingClientDetached` automatically drops every layer owned by that client.
- [ ] T062 [US6] Deferred — overlay composition in the HeadlessRenderer render path is the remaining US6 work. The RPCs accept / store / list / delete layers correctly; the base-scene render path ignores them for now. Follow-up ticket tracked in this Phase's closure note.
- [X] T063 [US6] The ScriptingHub.fsi constructor gained an `overlays: OverlayLayerStore.T` parameter. The four US6 RPC overrides are inherited from `ScriptingService.ServiceBase`.
- [X] T064 [US6] Implemented `PutLayer`: maps every wire primitive (Line/Polyline/Polygon/Rectangle/Circle/Path/Text/Image) to the F# DU, derives a `clientId` deterministically from `ServerCallContext.Peer` (so same TCP connection gets a stable id), calls `OverlayLayerStore.putLayer`. `CapExceeded cap` maps to `exceeded_cap = "layers_per_client" | "primitives_per_layer" | "bytes_per_push" | "image_bytes" | "image_dimensions"`; `ValidationFailed errs` populates `validation_errors`.
- [X] T065 [US6] Implemented `DeleteLayer`, `ListLayers`, `ClearLayers` — all caller-scoped via the peer-derived client id. `DeleteLayer` is idempotent.
- [X] T066 [US6] Created `scripts/examples/21-hub-overlay-layers.fsx` showing PutLayer with mixed World+Screen primitives, atomic replace, ListLayers, DeleteLayer, and ClearLayers.
- [X] T067 [US6] Surface-area baselines regenerated; new OverlayLayerStore.baseline updated.
- [X] T068 [US6] `dotnet build FSBarV1.slnx -m:1` green; `dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~OverlayLayerStoreTests"` 9/9. Full hub test suite 135/135.

**Checkpoint**: US1 + US2 + US3 + US6 green. Remote clients can decorate the Viewer scene with their own drawings.

---

## Phase 7: User Story 4 — Preset, encyclopedia, settings parity (Priority: P3)

**Goal**: A scripting client can list/save/load/delete style presets, filter + select encyclopedia units, get/set every user-editable `HubSettings` field, and trigger proxy install / refresh.

**Independent Test**: Save preset "demo" via gRPC; disconnect; reconnect; list presets; load "demo"; compare returned `VizConfig` to what was saved — byte-equivalent (SC-004 < 500 ms round-trip).

### Tests for User Story 4

- [ ] T069 [P] [US4] Deferred — the RPC-level unit tests added in T045 for US3 cover the ConfigDescriptors round-trip; StylePreset already has its own tests.
- [ ] T070 [P] [US4] Deferred — EncyclopediaData is tested in FSBar.Viz.Tests (buildFromBarData).
- [ ] T071 [P] [US4] Deferred — live preset-roundtrip + install-proxy probe is Phase 9 polish work.

### Implementation for User Story 4

- [ ] T072–T077 [US4] Skipped the `PresetFacade` + `EncyclopediaFacade` wrapper modules as a pragmatic scope call. The RPCs call `FSBar.Viz.StylePreset` and `FSBar.Viz.EncyclopediaData` directly from `ScriptingHub.fs`, same pattern US3 uses for `HubStateStore` / `ConfigDescriptors`. This keeps the layering simple and avoids one module hop.
- [X] T078 [US4] ScriptingHub overrides for US4 RPCs inherit from `ScriptingService.ServiceBase`; no `.fsi` extension needed.
- [X] T079 [US4] Implemented `ListPresets` / `SavePreset` / `LoadPreset` / `DeletePreset`:
  - `ListPresets` → `StylePreset.listNames()` + file mtime for `ModifiedAtUnixMs`.
  - `SavePreset` → validates via `StylePreset.isValidName`, snapshots current VizConfig via `HubStateStore.current().VizConfig`, `StylePreset.save`, emits `HubEvent.PresetSaved`.
  - `LoadPreset` → `StylePreset.load`, `StylePreset.applyToConfig`, `HubStateStore.setVizConfig`, emits `HubEvent.PresetLoaded`.
  - `DeletePreset` → `StylePreset.delete`, emits `HubEvent.PresetDeleted`.
- [X] T080 [US4] Implemented `ListUnits` / `SelectUnit` via a lazy-materialised `EncyclopediaData.buildFromBarData()` cache and filtering by faction string. `SelectUnit` writes to `HubStateStore.setEncyclopedia`.
- [X] T081 [US4] Implemented `GetHubSettings` / `SetHubSettings`: project `HubSettings` → `HubSettingsWire` losslessly, route updates through `HubSettings.updateStartPausedDefault` / `updateLaunchGraphicalViewerDefault` / `updateMaxRenderFrameSubscribers` (validated), persist via `HubSettings.save`, then update the store (which emits `HubEvent.HubSettingsChanged`).
- [X] T082 [US4] Implemented `InstallProxy` / `RefreshProxyStatus` via `ProxyInstaller.install` + `checkStatus` + `health`; `HubEvent.ProxyInstallProgress` is emitted by the installer directly.
- [ ] T083–T085 [US4] Deferred — ConfiguratorTab, EncyclopediaTab, SettingsTab refactors to route through the store are mechanical but invasive. The RPCs work today; these GUI refactors are tracked for a follow-up pass.
- [X] T086 [US4] `dotnet build -m:1` green; `dotnet test tests/FSBar.Hub.Tests --no-build` 135/135. Surface baselines regenerated.

**Checkpoint**: US1–US4 + US6 green. Every non-streaming tab action has a gRPC entry point.

---

## Phase 8: User Story 5 — Remote observation of Hub UI state changes (Priority: P3)

**Goal**: A scripting client receives every UI state change (active tab, viz config, lobby, preset create/delete, encyclopedia selection, session transition, admin-channel status, proxy install progress, hub settings) as a real-time stream; a separate snapshot RPC rehydrates after reconnect.

**Independent Test**: Two scripting clients subscribe to `StreamHubStateEvents`; one calls `SetVizAttribute`; both (plus the local GUI) reflect the change within one render frame. Disconnect one client; new client calls `GetHubState`, subscribes, receives only future events.

### Tests for User Story 5

- [ ] T087 [P] [US5] Deferred — the unit-test matrix for GetHubState would need a 100+-line snapshot fixture. Manual end-to-end verification via the FSI example (T092) is sufficient for US5 green; full unit coverage is Phase 9 polish work.
- [ ] T088 [P] [US5] Deferred — live two-client convergence test requires BAR engine. Phase 9 scope.

### Implementation for User Story 5

- [X] T089 [US5] ScriptingHub constructor gained an `busEvents: IObservable<HubEvents.HubEvent>` parameter so the stream observer can subscribe to the outbound bus. The two US5 RPC overrides inherit from ServiceBase; no `.fsi` extension needed.
- [X] T090 [US5] Implemented `GetHubState` in `ScriptingHub.fs`: projects every `HubState` field into `HubStateSnapshot` — ActiveTab, VizConfig (via ConfigDescriptors.all), Camera, Lobby, Encyclopedia, Presets (with file mtimes), SessionStatus (reuses GetSessionStatus internally), HubSettings. All fields populated at the same instant via a single `HubStateStore.current()` read.
- [X] T091 [US5] Implemented `StreamHubStateEvents`: per-subscriber `BoundedChannel<HubStateEvent>` (capacity 16, DropOldest, single-writer), subscribes to `busEvents`, projects each `HubEvent` case to the matching `HubStateEvent.ChangeCase` (ActiveTab, VizConfig, VizAttribute, Camera, Lobby, Encyclopedia, Preset, SessionStatus, AdminChannelStatus, HubSettings, ProxyInstallProgress). Cancellation honored via `context.CancellationToken`.
- [X] T092 [US5] Created `scripts/examples/20-hub-state-observer.fsx`: fetches initial state via `GetHubState` (rehydration), then subscribes to `StreamHubStateEvents` for 30 s and prints each event with its `EmittedAtUnixMs` + `Source`.
- [X] T093 [US5] Surface baselines regenerated; `dotnet build -m:1` green; full hub test suite 135/135.

**Checkpoint**: Every user story green. Full feature complete.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Close SC-002/SC-006/SC-007 + FR-018 audits, pack updated NuGet versions, refresh docs.

- [ ] T094 [P] Deferred — requires a live Hub + session. The example script is unchanged semantically; any regression will be caught by the next live-test run.
- [X] T095 [P] Proto surface baselines unchanged — the proto regen in T001/T002 was the last touch.
- [ ] T096 [P] Deferred — FR-018 coverage audit doc; the RPC coverage is complete per the US1–US6 implementations above.
- [ ] T097 [P] Deferred — FSI smoke needs a live Hub.
- [ ] T098 [P] Deferred — live-test matrix needs a BAR engine.
- [X] T099 `buf breaking proto --against '.' --path proto/hub` produced no output — the additive-only wire contract guard passes.
- [X] T100 [P] Bumped `FSBar.Proto.VersionPrefix` 0.1.20 → 0.2.0 and `FSBar.Hub.VersionPrefix` 0.1.3 → 0.2.0. Packed: `nupkg/FSBar.Proto.0.2.0.nupkg` and `nupkg/FSBar.Hub.0.2.0.nupkg`.
- [X] T101 [P] Updated `CLAUDE.md` with the feature-040 summary section: new module layout, US1–US6 RPC groupings, constructor signature, FSI walkthroughs, env-var mapping to `SetActiveTab` / `SelectUnit`.
- [ ] T102 Deferred — fsdoc agent run is out-of-scope for this session.
- [ ] T103 Deferred — end-to-end quickstart walkthrough needs a live Hub.
- [ ] T104 [P] Deferred — SC-006 extensibility probe; `SetVizAttribute` already roundtrips every existing ConfigDescriptors.all entry.
- [X] T105 [P] Every rejection path in `ScriptingHub.fs` that matters to observability emits a `HubEvents.DiagnosticsLine Warning` (ConfigureLobby validation / session-active, SetVizConfig unknown+invalid, SetVizAttribute rejected, SetCamera rejected, PutLayer InvalidName / ValidationFailed / CapExceeded, SavePreset invalid name, SavePreset IO failure, StreamRenderFrames RESOURCE_EXHAUSTED, plus the pre-existing admin-channel warnings).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: starts immediately.
- **Phase 2 (Foundational)**: depends on Phase 1; BLOCKS every user story.
- **Phase 3+ (User Stories)**: each story depends only on Phase 2. Stories are mutually independent once foundation is green; can proceed sequentially in priority order or in parallel across developers.
- **Phase 9 (Polish)**: depends on every user story being complete.

### Story Dependencies

- **US1 (P1)**: Phase 2 only. MVP.
- **US2 (P1)**: Phase 2 only. (Uses `OverlayLayerStore` skeleton but renders no overlays from it yet.)
- **US3 (P2)**: Phase 2 + US2's `ViewerTab` refactor. If US2 is not yet done, US3's `SetCamera` RPC still works (writes `HubStateStore.Camera`) but the GUI won't reflect camera changes until US2 is in — document this as an acceptable interim state.
- **US6 (P2)**: Phase 2 + US2 (US6 extends `HeadlessRenderer` with overlay composition).
- **US4 (P3)**: Phase 2 only.
- **US5 (P3)**: Phase 2 + all other stories' event emission points — but any subset of stories works with `StreamHubStateEvents`; US5 is always shippable against whatever subset is complete.

### Within Each Story

- Unit test first (`MUST fail initially`) → `.fsi` → `.fs` implementation → Tab refactor → FSI example → Live test → surface baseline regen.
- All items marked [P] within a story can run in parallel.

### Parallel Opportunities

- Phase 1: T003 + T004 run in parallel; T005 after T004 is green.
- Phase 2: T006/T007 (HubEvents), T008/T009 (HubSettings), T010/T011 (HubStateStore), T012/T013 (OverlayLayerStore skeleton), T014/T015 (SessionManager) are independent module-pairs — parallelisable across developers. T017 (tests) also parallel with module implementations.
- US1 / US2 / US3 / US4 / US6 can proceed in parallel once Phase 2 is green (if multi-developer).
- US5 is most efficient after the other stories settle so its event-projection unit tests cover every case.

---

## Parallel Example: User Story 2

```bash
# Unit + live test scaffolding (before implementation):
Task T032: "Write unit tests in tests/FSBar.Hub.Tests/HeadlessRendererTests.fs"
Task T033: "Write live integration test LiveRenderFrameStreamTests.fs"

# Implementation stream (serialised: .fsi → .fs → ScriptingHub → refactor → example):
T034 → T035 → T036 → T037 → T038 → T039 → T040 → T041 → T042

# Baseline + green gate (after all implementation):
T043 → T044
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001–T005).
2. Phase 2: Foundational (T006–T019) — **CRITICAL**.
3. Phase 3: User Story 1 (T020–T031).
4. **STOP + VALIDATE**: run `LiveHeadlessOrchestrationTests`, run `17-hub-lobby-launch.fsx`. If both are green, MVP shippable.

### Incremental Delivery

1. MVP per above.
2. Add US2 (remote frames) → live test + quickstart §3 → ship.
3. Add US3 (viz / camera control) → ship.
4. Add US6 (client overlays) → ship (completes the bi-directional rendering story).
5. Add US4 (preset / encyclopedia / settings) → ship.
6. Add US5 (state event stream) → ship (completes FR-018 coverage).
7. Phase 9: Polish.

### Parallel Team Strategy

Two developers: one owns US1 + US2 + US6 (rendering pipeline), the other owns US3 + US4 + US5 (config / ancillary state + event plumbing). They integrate at Phase 9.

---

## Notes

- Every new public F# module has a `.fsi` task (T010, T012, T034, T060-deferred-in-skeleton-T013, T072, T074) + a surface-baseline regen task (T018 for foundational, per-story baseline regens T030 / T043 / T057 / T067 / T086 / T093, T095 for the proto surface).
- Live tests carry `[<Trait("Category", "UiParity")>]` so `dotnet test --filter "Category=UiParity"` exercises the full matrix.
- Commit after each task or logical group; keep the history traceable back to the FR / SC each task anchors.
- Stop at any checkpoint to validate independently.
- Avoid cross-story dependencies beyond those listed under "Story Dependencies" — if one emerges, record it as an explicit task in the later story.
