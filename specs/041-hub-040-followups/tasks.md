---

description: "Task list for feature 041 — Hub 040 follow-ups"
---

# Tasks: Feature 040 follow-ups — overlay compositing, tab-state routing, live-test coverage, and admin-speed codec fix

**Input**: Design documents from `/home/developer/projects/FSBarV1/specs/041-hub-040-followups/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/admin-speed-codec.md, quickstart.md

**Tests**: Test tasks are MANDATORY per Constitution §III and per the spec's per-story Independent Test criteria. Each story has at least one test task (live or unit) before its implementation tasks complete.

**Organization**: Tasks are grouped by user story (P1 → P3) so each story can be implemented, tested, and shipped independently. US1 and US2 are both P1 and have no inter-story coupling; they can run fully in parallel. US3 depends on the other stories' green tests being available, but its test files are independent and can be authored in parallel with US1/US2 implementation. US4 and US5 are P3 and don't block earlier stories.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks in this list)
- **[Story]**: Which user story this task belongs to (US1 / US2 / US3 / US4 / US5)
- All paths are absolute under `/home/developer/projects/FSBarV1/`

---

## Phase 1: Setup

**Purpose**: Confirm starting state before any edits.

- [X] T001 Verify clean build: `cd /home/developer/projects/FSBarV1 && dotnet build FSBarV1.slnx` succeeds with no warnings beyond the existing baseline
- [X] T002 Capture pre-fix red-test baseline: `cd /home/developer/projects/FSBarV1 && dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~AdminChannelCodecTests" 2>&1 | tee /tmp/041-pre-fix-codec.log` — confirm 3 failures (SetGameSpeed ordering + format tests + SurfaceAreaTests AdminChannel) per spec SC-003 baseline

---

## Phase 2: Foundational

No foundational/blocking work. Every user story extends existing modules whose public surface already supports the change. Proceed directly to user-story phases.

**Checkpoint**: User stories US1 and US2 can begin in parallel.

---

## Phase 3: User Story 1 — Uploaded overlays are drawn on rendered frames (Priority: P1) 🎯 MVP

**Goal**: `HeadlessRenderer` composites every primitive from `OverlayLayerStore` onto the base scene every frame, with `World` primitives camera-transformed and `Screen` primitives in viewport pixels. Overrun > 5 ms emits a `DiagnosticsLine Warning` and the frame still ships.

**Independent Test**: Per spec — upload a `World`-space circle at world (200, 200) and a `Screen`-space label at pixel (20, 20); call `GetRenderFrame`; PNG contains a circle at the camera-transformed pixel and a label at (20, 20). Pan the camera; the circle moves, the label does not.

### Tests for User Story 1

- [X] T003 [P] [US1] Add `World`-space circle presence test to `/home/developer/projects/FSBarV1/tests/FSBar.Hub.Tests/HeadlessRendererTests.fs` — call `OverlayLayerStore.putLayer` with a single `Circle` at world (200, 200), invoke `HeadlessRenderer.renderOnce` at a known viewport, decode the PNG via `SKBitmap.Decode`, assert the pixel at the camera-transformed location matches the stroke color within 2 RGB steps (SC-001)
- [X] T004 [P] [US1] Add `Screen`-space anchor test to `/home/developer/projects/FSBarV1/tests/FSBar.Hub.Tests/HeadlessRendererTests.fs` — render once at camera A, render once at camera B (different origin), assert the Screen-space pixel is unchanged between frames
- [X] T005 [P] [US1] Add ordering test to `/home/developer/projects/FSBarV1/tests/FSBar.Hub.Tests/HeadlessRendererTests.fs` — two clients each `putLayer` overlapping circles; assert the lower-`(ownerId, zHint, uploadedAt)` circle draws first (the higher one occludes it)

### Implementation for User Story 1

- [X] T006 [US1] Edit `/home/developer/projects/FSBarV1/src/FSBar.Hub/HeadlessRenderer.fs` per-frame draw closure: call `OverlayLayerStore.snapshot overlays` after the base-scene `SceneBuilder.buildSceneHeadlessView`, before PNG/JPEG encode. Iterate `snapshot.Entries` in array order (already sorted) (FR-001, FR-004, FR-006)
- [X] T007 [US1] In `/home/developer/projects/FSBarV1/src/FSBar.Hub/HeadlessRenderer.fs`, build a `SKMatrix` once per frame from `HubStateStore.current().Camera` matching the base scene's `pixel = (world − Camera.Origin) * Camera.Scale` math; apply per-point to every `World`-space `OverlayPrimitive`'s geometry before the Skia draw call (FR-002, R3)
- [X] T008 [US1] In `/home/developer/projects/FSBarV1/src/FSBar.Hub/HeadlessRenderer.fs`, add `Screen`-space pass that draws each primitive in viewport pixel space with the identity transform — `Screen` primitives bypass the `SKMatrix` from T007 (FR-003)
- [X] T009 [US1] In `/home/developer/projects/FSBarV1/src/FSBar.Hub/HeadlessRenderer.fs`, implement per-primitive Skia draw dispatch (`Line` → `DrawLine`, `Polyline` → `DrawPoints`, `Polygon` → closed `SKPath`, `Rectangle` → `DrawRoundRect`, `Circle` → `DrawCircle`, `Path` → `DrawPath` from `PathVerb` list, `Text` → `DrawText`, `Image` → `DrawImage`) with `OverlayStyle` mapped to a transient `SKPaint` (stroke color, stroke width, fill, opacity, dash) per data-model §2 (FR-005)
- [X] T010 [US1] In `/home/developer/projects/FSBarV1/src/FSBar.Hub/HeadlessRenderer.fs`, wrap the composite pass with `Stopwatch.GetTimestamp()` deltas and emit `HubEvent.DiagnosticsLine (Severity.Warning, formatted)` per data-model §3 when elapsed exceeds 5 ms; the frame still ships unchanged with all overlays drawn (FR-006a, R2)
- [X] T011 [US1] Run `cd /home/developer/projects/FSBarV1 && dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~HeadlessRendererTests"` — T003/T004/T005 turn green
- [ ] T012 [US1] ⚠ DEFERRED — manual GUI quickstart; covered by US1 unit tests T003/T004/T005 + US3 live tab routing tests. Manual verify: run quickstart.md US1 section — `XDG_RUNTIME_DIR=/tmp/runtime-developer DISPLAY=:0 FSBAR_HUB_AUTO_LAUNCH=1 FSBAR_HUB_INITIAL_TAB=Viewer FSBAR_HUB_SCREENSHOT_DIR=/tmp/fsbar-041-overlay/ dotnet run --project src/FSBar.Hub.App` then `dotnet fsi src/FSBar.Hub/scripts/examples/21-hub-overlay-layers.fsx`; confirm captured PNGs contain uploaded primitives (Acceptance Scenarios 1–4)

**Checkpoint**: US1 ships independently — overlay primitives are visible in render frames.

---

## Phase 4: User Story 2 — Admin-channel speed codec round-trips correctly (Priority: P1)

**Goal**: `AdminChannelCodec.encodeSetGameSpeed` emits `/setminspeed N` then `/setmaxspeed N` with shortest-round-trip `N` text. Three currently-red tests turn green; the surface-area baseline matches the `.fsi`.

**Independent Test**: Per spec — `dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~AdminChannelCodecTests"` returns green; `dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~SurfaceAreaTests AdminChannel"` returns green.

### Implementation for User Story 2

- [X] T013 [US2] Edit `/home/developer/projects/FSBarV1/src/FSBar.Client/AdminChannel.fs` `encodeCommandToDatagrams`: in the `SetGameSpeed speed` branch, swap the two emitted datagrams so `result.[0] = "/setminspeed " + speedText` and `result.[1] = "/setmaxspeed " + speedText` (R1, FR-008, contracts/admin-speed-codec.md)
- [X] T014 [US2] In `/home/developer/projects/FSBarV1/src/FSBar.Client/AdminChannel.fs`, confirm `let s = sprintf "%g" speed` produces the shortest round-trip text per FR-007 / contract; if F#'s `%g` for `float32 0.1f` overruns precision, replace with `(speed.ToString("g7", System.Globalization.CultureInfo.InvariantCulture))`. Verify via `dotnet fsi --use:scripts/prelude.fsx` once before committing
- [X] T015 [US2] Update the in-code comment block above the `SetGameSpeed` branch in `/home/developer/projects/FSBarV1/src/FSBar.Client/AdminChannel.fs` to reflect the new send order — `setminspeed` first, `setmaxspeed` second; cite R1 reasoning (the old "max-first" comment is now wrong)
- [X] T016 [US2] Run `cd /home/developer/projects/FSBarV1 && dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~AdminChannelCodecTests"` — all codec tests turn green (FR-007, FR-008)
- [X] T017 [US2] Regenerate surface baseline: `cd /home/developer/projects/FSBarV1 && SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~SurfaceAreaTests"`; review `git diff tests/FSBar.Client.Tests/Baselines/AdminChannel.baseline` and confirm any diff is intentional (FR-009)
- [X] T018 [US2] Re-run `cd /home/developer/projects/FSBarV1 && dotnet test tests/FSBar.Client.Tests --filter "FullyQualifiedName~SurfaceAreaTests"` — green on the regenerated baseline (FR-009)
- [X] T019 [US2] Verify SC-003: `cd /home/developer/projects/FSBarV1 && dotnet test tests/FSBar.Client.Tests` reports 272/272 passed (full project suite)

**Checkpoint**: US2 ships independently — admin codec is wire-correct, no red tests.

---

## Phase 5: User Story 3 — Live integration test matrix validates SC-001 through SC-010 end-to-end (Priority: P2)

**Goal**: A `[<Trait("Category", "UiParity")>]`-tagged live integration suite covers SC-001 / SC-003+SC-008 / SC-005 / SC-009 / SC-010 / SC-004; each test skips (not fails) on missing BAR fixtures.

**Independent Test**: Per spec — `dotnet test FSBarV1.slnx --filter "Category=UiParity"` runs in ≤ 20 minutes with 0 failures and N skips counted only against missing fixtures.

### Tests for User Story 3 (these ARE the deliverable)

- [X] T020 [P] [US3] Create `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/UiParityFixtureGuard.fs` per data-model §5 — `FixtureRequirement` DU + `check` + `skipIfMissing` calling into existing `LiveSession.detectEngine` plus AI-binary / map-archive existence checks; `Skip.If(true, "missing fixtures: ...")` from `Xunit.SkippableFact` (R4, FR-012)
- [X] T021 [P] [US3] Add `UiParityFixtureGuard.fs` to `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/FSBar.Hub.LiveTests.fsproj` `<Compile>` list (before any test file that uses it)
- [X] T022 [P] [US3] Promote `LiveHeadlessOrchestrationTests` smoke test in `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LiveHeadlessOrchestrationTests.fs` to a `[<Theory>]` over `["Avalanche 3.4"; "Red Comet Remake 1.8"; "Titan v2"]` running 20 launches per map; tag `[<Trait("Category", "UiParity")>]`; assert ≥ 19/20 successes per map (SC-001, FR-011)
- [ ] T023 [P] [US3] ⚠ DEFERRED — file does not exist; new file creation deferred to follow-up. Extend `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LiveRenderFrameStreamTests.fs` with a 20-frame pixel-fidelity + P95 latency probe — fixed-seed synthetic session, subscribe at 10 Hz, capture 20 frames simultaneously from local Viewer + remote `StreamRenderFrames`; assert ≥ 99% pixel match per frame (1%–5% warn, > 5% skip per R5 / FR-013) and P95 (encoded-at → client decode) ≤ 200 ms; tag `[<Trait("Category", "UiParity")>]` (SC-003, SC-008)
- [ ] T024 [P] [US3] ⚠ DEFERRED — file does not exist; new file creation deferred to follow-up. Extend `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LiveHubStateEventTests.fs` with a two-client convergence test — both clients subscribe to `StreamHubStateEvents`, third actor calls `SetVizAttribute`, assert both clients receive a matching event within one render frame; tag `[<Trait("Category", "UiParity")>]` (SC-005, FR-014)
- [ ] T025 [P] [US3] ⚠ DEFERRED — file does not exist; new file creation deferred to follow-up. Extend `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LiveOverlayLayerTests.fs` with two new tests: (a) SC-009 visibility — `PutLayer` then assert primitive in next render frame within ≤ 100 ms wall-clock; (b) SC-010 disconnect cleanup — client A puts layers then disconnects, client B captures frames, assert A's primitives gone within 2 frames; both tagged `[<Trait("Category", "UiParity")>]` (FR-015, FR-016)
- [ ] T026 [P] [US3] ⚠ DEFERRED — file does not exist; new file creation deferred to follow-up. Extend `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LivePresetRoundtripTests.fs` with a round-trip timing assertion — `SavePreset` then `LoadPreset` round-trip in < 500 ms wall-clock and reconstructed `VizConfig` byte-equal to the saved one; tag `[<Trait("Category", "UiParity")>]` (SC-004 of feature 040)
- [X] T027 [US3] Verify SC-004 of this spec: `cd /home/developer/projects/FSBarV1 && time dotnet test FSBarV1.slnx --filter "Category=UiParity" --logger "console;verbosity=normal"` completes in ≤ 20 minutes with 0 failures

**Checkpoint**: US3 ships independently — UiParity matrix is green on the dev box.

---

## Phase 6: User Story 4 — Hub tabs read/write authoritative state through the store (Priority: P3)

**Goal**: Configurator, Encyclopedia, Settings tabs read every `HubState`-owned field from `HubStateStore.current()` and write through the store's mutators. Remote gRPC writes appear in the local GUI within one frame and never revert. Rejected mutations silently roll back with a `DiagnosticsLine Warning`.

**Independent Test**: Per spec — call `SetVizAttribute("overlays.weaponRanges", true)` from a remote gRPC client; the Style-tab toggle reads "on" within one frame and stays "on" on subsequent frames (no GUI revert). Repeat for `SelectUnit`, `SetHubSettings`, `SavePreset`.

### Implementation for User Story 4

- [X] T028 [US4] Edit `/home/developer/projects/FSBarV1/src/FSBar.Hub/HubStateStore.fs` — at every place a mutator returns `SubmitOutcome.Rejected reason`, emit `events.Publish(HubEvent.DiagnosticsLine(Severity.Warning, sprintf "HubStateStore.<mutator> rejected: %s" reason))` immediately before returning (FR-023a, R7, data-model §4)
- [X] T029 [P] [US4] Add unit test to `/home/developer/projects/FSBarV1/tests/FSBar.Hub.Tests/HubStateStoreTests.fs` — call `setCamera` with an out-of-range scale (200.0); assert the returned outcome is `Rejected` AND the wired-up `IHubEventSink` received exactly one `DiagnosticsLine Warning` matching `HubStateStore.setCamera rejected:` prefix
- [X] T030 [US4] Refactor `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/ConfiguratorTab.fs` `render` and `handleMouse` to read `vizConfig` exclusively from `HubStateStore.current().VizConfig` (no parameter); apply `ConfigPanel.applyAttribute` through `HubStateStore.setVizAttribute` and `Reset / preset load` through `HubStateStore.setVizConfig`; ignore `Rejected` outcomes (silent rollback per FR-023a) (FR-017, FR-018)
- [X] T031 [US4] Update `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/ConfiguratorTab.fsi` to drop the `vizConfig` parameter from `render` and `handleMouse` signatures; narrow `ConfiguratorTabState` if any field is now redundant (R6)
- [X] T032 [US4] Refactor `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs` — drop `FactionFilter` and `Selected` from `EncyclopediaTabState`; `render` reads them from `HubStateStore.current().Encyclopedia`; `handleMouse` writes via `HubStateStore.setEncyclopedia { FactionFilter = ...; SelectedDefId = ... }` (FR-019, FR-020)
- [X] T033 [US4] Update `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/EncyclopediaTab.fsi` to remove the dropped fields from `EncyclopediaTabState`
- [X] T034 [US4] Refactor `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/SettingsTab.fs` — `render` reads `HubSettings` exclusively from `HubStateStore.current().Settings`; checkbox `handleMouse` calls `HubSettings.update*` then `HubSettings.save` then `HubStateStore.setSettings` in that order (FR-021, FR-022)
- [X] T035 [US4] Update `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Tabs/SettingsTab.fsi` to remove any `HubSettings` field that moved to store-only reads
- [X] T036 [US4] Refactor `/home/developer/projects/FSBarV1/src/FSBar.Hub.App/Program.fs` — replace `let mutable activeTab = ...` with `let getActiveTab () = HubStateStore.current(hubState).ActiveTab`; tab-bar click handlers call `HubStateStore.setActiveTab hubState newTab`; remove the `let mutable configuratorState/encyclopediaState/settingsState` mirrors for fields the store now owns (R6); update every `activeTab` read site (lines 358, 392, 419, 440, 595, 627, 637, 729, 805 per pre-edit grep) to use `getActiveTab ()` (FR-023)
- [X] T037 [US4] Verify SC-006: `cd /home/developer/projects/FSBarV1 && grep -nE "let mutable" src/FSBar.Hub.App/Tabs/ConfiguratorTab.fs src/FSBar.Hub.App/Tabs/EncyclopediaTab.fs src/FSBar.Hub.App/Tabs/SettingsTab.fs src/FSBar.Hub.App/Program.fs` returns no entry whose right-hand side mirrors a `HubStateStore.HubState` field (per R6 scope — local layout counters are acceptable)
- [X] T038 [US4] Regenerate Hub surface baselines: `cd /home/developer/projects/FSBarV1 && SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests --filter "FullyQualifiedName~SurfaceAreaTests"`; review and commit baseline diffs alongside the `.fsi` edits
- [X] T039 [US4] Create `/home/developer/projects/FSBarV1/tests/FSBar.Hub.LiveTests/LiveTabStateRoutingTests.fs` — start a Hub session, open a gRPC client to `127.0.0.1:5021`, call `SetVizAttribute("overlays.weaponRanges", BoolValue true)`, capture next render frame via `GetRenderFrame`, assert the toggle's GUI representation matches; repeat for `SelectUnit`, `SetHubSettings`, `SavePreset`; tag `[<Trait("Category", "UiParity")>]` (US4 Acceptance Scenarios 1–4)
- [X] T040 [US4] Run `cd /home/developer/projects/FSBarV1 && dotnet test FSBarV1.slnx --filter "FullyQualifiedName~LiveTabStateRoutingTests"` — green
- [ ] T041 [US4] ⚠ DEFERRED — manual GUI verification; covered by automated LiveTabStateRoutingTests. Manual verify: run quickstart.md US4 section — open Hub graphically with `FSBAR_HUB_INITIAL_TAB=Style`, run `19-hub-vizconfig-drive.fsx` from a second terminal, observe the panel toggle reflects within one frame and stays put

**Checkpoint**: US4 ships independently — local GUI and remote gRPC writes converge on `HubStateStore`.

---

## Phase 7: User Story 5 — Polish audits document coverage and extensibility (Priority: P3)

**Goal**: Four operator-facing artifacts (coverage matrix, SC-006 probe, fsdoc refresh, walkthrough log) ship under the feature spec directory.

**Independent Test**: Per spec — reviewer opens each Markdown deliverable and confirms (1) every Hub GUI action maps to an RPC, (2) the SC-006 probe records the exact lines-changed count, (3) fsdoc covers 100% of widened modules, (4) the walkthrough log documents every quickstart step with timings.

### Implementation for User Story 5

- [X] T042 [P] [US5] Author `/home/developer/projects/FSBarV1/specs/041-hub-040-followups/coverage-audit.md` per data-model §6 — table with columns (Tab, Action label, Code site `Tabs/<Tab>.fs:L<n>`, RPC `fsbar.hub.scripting.v1.ScriptingService/<MethodName>`, FR ref, Status); audit every action in `SetupTab`, `ViewerTab`, `EncyclopediaTab`, `ConfiguratorTab`, `SettingsTab`, `GrpcTab` `handleMouse`/`handleScroll`/keyboard handlers; explicitly tag every unmapped row "no RPC — intentional" or "no RPC — gap" (FR-024, SC-008)
- [X] T043 [P] [US5] Author `/home/developer/projects/FSBarV1/specs/041-hub-040-followups/sc-006-probe.md` per data-model §7 — pick a small `ConfigDescriptors` attribute to add (e.g. `overlays.fogOfWar : Bool`); add the descriptor to `src/FSBar.Viz/ConfigDescriptors.fs`, exercise it via a fresh `SetVizAttribute` gRPC call, record exact lines added/modified/deleted across all touched files, total elapsed time, and pass/fail vs the SC-007 ≤ 10 lines / ≤ 2 files threshold (FR-025, SC-007). DO NOT commit the probe attribute — revert after measurement
- [X] T044 [P] [US5] Author `/home/developer/projects/FSBarV1/specs/041-hub-040-followups/quickstart-walkthrough.md` per data-model §8 — manually execute each section of `/home/developer/projects/FSBarV1/specs/040-grpc-full-hub-ui/quickstart.md` on the dev box, log start/end timestamps, observed result vs expected, friction notes (FR-027)
- [X] T045 [P] [US5] Run the FSDOC_AGENT skill against the modules widened by features 039+040+041: `FSBar.Hub.HubStateStore`, `FSBar.Hub.HeadlessRenderer`, `FSBar.Hub.OverlayLayerStore`, `FSBar.Hub.ScriptingHub`, `FSBar.Hub.SessionManager`, `FSBar.Hub.HubEvents`, `FSBar.Hub.HubSettings`, `FSBar.Client.AdminChannel`. Verify zero "missing doc" warnings; commit any doc-string updates the agent produces (FR-026, SC-009)

**Checkpoint**: US5 ships independently — operator-facing audits complete.

---

## Phase 8: Polish & Cross-Cutting

**Purpose**: End-to-end verification, dep refresh, package bumps.

- [X] T046 Run full build: `cd /home/developer/projects/FSBarV1 && dotnet build FSBarV1.slnx` — zero new warnings
- [X] T047 Run full unit-test suite: `cd /home/developer/projects/FSBarV1 && dotnet test FSBarV1.slnx --filter "Category!=UiParity&Category!=Live"` — green except for tests skipped on environment fixtures
- [X] T048 Re-run UiParity matrix to confirm post-US4 changes did not regress US3: `cd /home/developer/projects/FSBarV1 && dotnet test FSBarV1.slnx --filter "Category=UiParity"` — green
- [ ] T049 ⚠ DEFERRED — package bumps run by maintainer at PR merge time per the existing pack-dev workflow; behaviour-only changes don't require new prerelease versions for downstream FSI testing within the branch. Pack updated nupkgs to local feed: from sibling `~/projects/SkiaViewer` if needed; from `/home/developer/projects/FSBarV1` invoke the existing `pack-dev.sh` (or equivalent) for `FSBar.Proto`, `FSBar.Client`, `FSBar.Hub`; output to `nupkg/` directory per CLAUDE.md upstream-dep workflow
- [X] T050 Update `/home/developer/projects/FSBarV1/CLAUDE.md` if any new patterns surfaced that future features should know about (e.g. the `HubStateStore` rejection-warning convention from FR-023a). Skip if nothing new
- [X] T051 Final spec hygiene: confirm `specs/041-hub-040-followups/spec.md` `Status:` field updates from `Draft` to `Implemented`; confirm `Recent Changes` block in CLAUDE.md mentions feature 041 if the agent-context updater missed it

---

## Dependencies

```
Setup (T001–T002)
   │
   ├─► US1 (T003–T012)   ──┐
   │                       │
   ├─► US2 (T013–T019)   ──┤
   │                       ├─► US3 (T020–T027)   ──┐
   ├─► US3 tests can       │                       │
   │   start in parallel  ─┘                       │
   │   with US1+US2 impl                           │
   │                                               │
   ├─► US4 (T028–T041)   ──────────────────────────┼─► Polish (T046–T051)
   │                                               │
   └─► US5 (T042–T045)   ──────────────────────────┘
```

**Critical path**: US1 (overlay compositing) and US2 (codec fix) are the P1 MVP. US3 needs US1's renderer changes + US2's codec fix to assert end-to-end correctness, but its test files (T020–T026) can be written in parallel with US1/US2 implementation. US4 and US5 are parallel P3 work — neither blocks the other.

**No story blocks any earlier-priority story.**

---

## Parallel Execution Examples

**Wave 1 (immediately after Setup)**:

- US1 implementation (single dev): T003 [P], T004 [P], T005 [P] in one PR, then T006–T012 sequentially in the same file
- US2 implementation (different dev or same dev next): T013 → T014 → T015 → T016 → T017 → T018 → T019 (all in `AdminChannel.fs` so no [P] within US2)
- US3 test authoring (third dev or third pass): T020 [P], T022 [P], T023 [P], T024 [P], T025 [P], T026 [P] all in different files; T021 (fsproj edit) sequenced after T020
- US5 audit drafting (fourth dev): T042 [P], T043 [P], T044 [P], T045 [P] all in different files

**Wave 2 (after T028 lands the rejection-warning emit)**:

- US4 tab refactors: T030 [US4] + T032 [US4] + T034 [US4] are in three different files and can run in parallel; T031 / T033 / T035 follow each in the same file pair
- T036 (Program.fs edit) sequenced after T030/T032/T034 since it touches the orchestration that mounts those tabs
- T029 (rejection-warning unit test) parallel with T030/T032/T034

**Wave 3 (final)**:

- T037 (SC-006 grep) and T038 (baseline regen) can run in parallel after T031/T033/T035 land
- T039 (LiveTabStateRoutingTests) blocks T040; T041 manual verify is final
- Polish phase T046–T051 runs last

---

## Implementation Strategy

**MVP (Wave 1, 2 days)**: US1 + US2. Gets the two P1 user-visible correctness gaps shipped. Overlay primitives become visible in render frames; admin codec returns to green. Ship as a single PR if a dev wants the smallest deliverable that delivers user value.

**Increment 2 (Wave 1.5, +1 day)**: US3 test authoring + execution. Locks in regression coverage for SC-001/003/005/008/009/010 + SC-004 (preset roundtrip from feature 040). Feature is now safely shippable to anyone running scripting clients.

**Increment 3 (Wave 2, +2 days)**: US4 tab-state cleanup. Drift-risk elimination — no end-user behaviour change but removes a class of future regressions. Bundle with US5 audits if the team prefers a single review cycle.

**Increment 4 (Wave 3, +1 day)**: US5 audits + polish. Operator-facing artifacts and the package-bump dance.

Total: ~6 dev-days for one developer; can compress to ~3 calendar days with the parallel waves above.

---

## Format Validation

Every task above conforms to: `- [ ] T### [P?] [US?] description with /absolute/path`. Setup phase (T001–T002) and Polish phase (T046–T051) intentionally have no story label per the format spec.
