---
description: "Task list for feature 039-hub-admin-channel"
---

# Tasks: Hub admin/host channel

**Input**: Design documents from `/specs/039-hub-admin-channel/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are included — plan.md §Testing mandates unit tests for the wire codec and live integration tests for each user story.

**Organization**: Tasks are grouped by user story (P1→P3). Foundational phase delivers the autohost channel + port wiring + session integration that every US needs.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)

---

## Phase 1: Setup

**Purpose**: No new projects or packages are introduced. Feature 039 ships into the existing `FSBar.Client` / `FSBar.Hub` / `FSBar.Hub.App` / `FSBar.Proto` graph.

- [X] T001 Verify the feature branch builds clean before any edits: `dotnet build FSBarV1.slnx` (baseline for later regression). Record the baseline surface-area test state with `dotnet test FSBarV1.slnx --filter "FullyQualifiedName~SurfaceArea"`.
- [X] T002 [P] Confirm `protoc-gen-fsgrpc` is on PATH (`which protoc-gen-fsgrpc`) per CLAUDE.md "Hub scripting proto regeneration". If missing, run `~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh`. No repo edits.
- [X] T003 [P] Confirm live-test prerequisites pass: `tests/check-prerequisites.sh`. No repo edits.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Bring up the autohost channel end-to-end (wire codec + session integration + proto scaffolding) so every US phase can wire its SessionManager member, UI widget, and RPC against a working channel.

**⚠️ CRITICAL**: No US-phase task may start until this phase is done.

### 2A · Engine-side launch wiring

- [X] T004 Add `AutohostPort: int option` field to `EngineConfig` in `src/FSBar.Client/EngineConfig.fsi` and `src/FSBar.Client/EngineConfig.fs` (default `None`, doc comment references data-model.md §5). Keep existing `create` callable without the field.
- [X] T005 Emit `AutohostIP = 127.0.0.1` + `AutohostPort = <port>` lines into the generated `springsettings.cfg` when `EngineConfig.AutohostPort = Some p` in `src/FSBar.Client/ScriptGenerator.fs`. No-op when `None`.
- [X] T006 Add a unit test `tests/FSBar.Client.Tests/ScriptGeneratorAutohostTests.fs` asserting that (a) when `EngineConfig.AutohostPort = Some 12345`, the generated `springsettings.cfg` contains exactly one `AutohostIP = 127.0.0.1` line and one `AutohostPort = 12345` line; (b) when `AutohostPort = None`, neither line appears. `EngineLauncher` itself requires no code change — the hub binds the socket via `AdminChannel.bind()` before launch (see T018), reads `LocalPort`, threads it into `EngineConfig`, then calls the unmodified launch path per plan.md §Project Structure.

### 2B · Autohost wire client (`FSBar.Client.AdminChannel`)

- [X] T007 Create `src/FSBar.Client/AdminChannel.fsi` declaring `AdminCommandOut`, `AdminEventIn`, `ChannelState`, sealed `AdminChannel` class, and `val bind: unit -> Result<AdminChannel, string>` per data-model.md §1. Include XML docs for every public member.
- [X] T008 Implement `src/FSBar.Client/AdminChannel.fs`: outbound encode for `PAUSE`/`SETGAMESPEED`/`SAYMESSAGE`/`KILLSERVER` per `contracts/autohost-wire.md` + research.md R3; inbound decode for action codes 0/1/2/3/7/11 with `Unknown` fallthrough; `Events` as `IObservable<AdminEventIn>` backed by a receive loop on a dedicated thread; `Send` returning `Result<unit,string>`; `IDisposable` cleanup.
- [X] T009 Register `AdminChannel` public surface in `src/FSBar.Client/FSBar.Client.fsproj` (add both `.fsi` and `.fs` to the `<Compile>` group in the correct dependency order — after `EngineConfig`, before `BarClient`).
- [X] T010 [P] Add `tests/FSBar.Client.Tests/AdminChannelCodecTests.fs` covering round-trip encode→bytes and bytes→decode for every in-scope outbound code and every enumerated inbound code plus one `Unknown` case. Use xUnit 2.9.x style consistent with sibling tests.
- [X] T011 [P] Create `tests/FSBar.Client.Tests/Baselines/AdminChannel.baseline` by running the surface-area check with `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Client.Tests` — then verify the generated baseline captures exactly the types and members from data-model.md §1.

### 2C · Hub-side channel orchestrator (`FSBar.Hub.AdminChannelHost`)

- [X] T012 Create `src/FSBar.Hub/AdminChannelHost.fsi` declaring `AdminChannelStatus`, `SubmitOutcome`, sealed `AdminChannelHost`, and `val attach: AdminChannel * IHubEventSink -> AdminChannelHost` per data-model.md §2.
- [X] T013 Implement `src/FSBar.Hub/AdminChannelHost.fs`: one `MailboxProcessor<AdminCommandOut>` with per-kind coalescing + 100 ms quiet window (research.md R5); maintain `Status`, `IsPaused`, `CurrentSpeed`; subscribe to `AdminChannel.Events` to transition `Attached ↔ Lost` on `SERVER_STARTED` / `SERVER_QUIT` / socket close; publish every status transition through `HubEvents.IHubEventSink`; enforce invariants I2, I5 from data-model.md §8.
- [X] T014 Register `AdminChannelHost` in `src/FSBar.Hub/FSBar.Hub.fsproj` in the correct order (after `HubEvents`, before `SessionManager`).
- [X] T015 [P] Add `tests/FSBar.Hub.Tests/AdminChannelHostTests.fs` covering: (a) two rapid `Pause(true)` submits coalesce to a single datagram via a fake `AdminChannel`; (b) `Submit` returns `Rejected` when status is `Unavailable`/`Lost` without touching the fake channel (invariant I5); (c) status transitions publish `HubEvent.AdminChannelStatusChanged`.
- [X] T016 [P] Regenerate `tests/FSBar.Hub.Tests/Baselines/AdminChannelHost.baseline` via `SURFACE_AREA_UPDATE=1 dotnet test tests/FSBar.Hub.Tests` and verify.

### 2D · Hub events + session integration

- [X] T017 Add `AdminChannelStatusChanged of status: AdminChannelStatus` to the `HubEvent` DU in `src/FSBar.Hub/HubEvents.fsi` and `src/FSBar.Hub/HubEvents.fs`. Update `tests/FSBar.Hub.Tests/Baselines/HubEvents.baseline` via `SURFACE_AREA_UPDATE=1`.
- [X] T018 Wire the admin channel into `src/FSBar.Hub/SessionManager.fs`: on `Launch`, (1) call `AdminChannel.bind()` → capture socket + `LocalPort`, (2) set `EngineConfig.AutohostPort = Some port`, (3) call the unchanged `EngineLauncher.launch` path, (4) once launched, call `AdminChannelHost.attach(channel, eventSink)`, (5) store both for the session's lifetime. On `Ending → Idle`, dispose both and clear. When `AdminChannel.bind()` returns `Error`, do NOT construct an `AdminChannelHost`; instead publish `HubEvent.AdminChannelStatusChanged(Unavailable(reason))` directly and continue the launch in read-only mode so the Viewer-tab toolbar renders the reason (FR-009). Do NOT yet expose `Pause`/`Resume`/`SetEngineSpeed`/`ForceEnd`/`SendAdminMessage` — those land in their per-US phases.
- [X] T019 Add `AdminStatus: AdminChannelStatus option` member to `src/FSBar.Hub/SessionManager.fsi` and implement in `src/FSBar.Hub/SessionManager.fs` — returns `Some host.Status` when a session is active, `None` when Idle (invariant I3). Update `tests/FSBar.Hub.Tests/Baselines/SessionManager.baseline`.

### 2E · Proto scaffolding (common shapes only)

- [X] T020 Edit `proto/hub/scripting.proto` — add `message AdminChannelStatusInfo` (enum `State` with STATE_UNSPECIFIED/ATTACHED/UNAVAILABLE/LOST + `reason` string) and `message AdminSubmitResult` (enum `Outcome` with OUTCOME_UNSPECIFIED/SENT/COALESCED/REJECTED + `dropped_count` + `reason` + `admin_channel_status`) per `specs/039-hub-admin-channel/contracts/scripting-admin.proto`.
- [X] T021 Extend `ActiveSession` in `proto/hub/scripting.proto` with `optional AdminChannelStatusInfo admin_channel_status = 7;`.
- [X] T022 Regenerate `src/FSBar.Proto/Generated/hub/scripting.gen.fs` via `cd proto && buf generate`. Verify `dotnet build FSBarV1.slnx` succeeds.
- [X] T023 Populate `ActiveSession.admin_channel_status` from `SessionManager.AdminStatus` inside `src/FSBar.Hub/ScriptingHub.fs` (path-through only — no new RPCs yet).
- [X] T023a Add `tests/FSBar.Hub.LiveTests/LiveAdminChannelLossTests.fs` asserting SC-006 + FR-009 end-to-end: launch a headless session, wait for `AdminStatus = Some Attached`, externally SIGKILL the engine process, then assert within 10 s wall time that (a) `SessionManager.AdminStatus` becomes `Some (Lost reason)`, (b) one `HubEvent.AdminChannelStatusChanged(Lost _)` was published, (c) `SessionManager.Pause()` returns `SubmitOutcome.Rejected reason` without hitting the socket (invariant I5). Mark `[Trait("Category","AdminChannel")]`.

**Checkpoint**: Foundation ready. The autohost channel opens on every session launch, status propagates through `HubEvents`, and the proto scaffolding is in place. No user-visible behavior changes yet.

---

## Phase 3: User Story 1 — Real pause / resume (Priority: P1) 🎯 MVP

**Goal**: Replace the feature-038 cosmetic pause with a real simulation pause via the autohost channel. Deliver working pause/resume both from the Viewer-tab button and through the scripting service. Honor the Setup-tab "Start paused" checkbox.

**Independent Test**: Launch a match, click ⏸ on the Viewer tab, wait 15 s of wall time, confirm the in-game clock did not advance; click ▶, confirm it resumes. Toggle "Start paused" on Setup and confirm the next launch begins with a real sim pause at time zero.

### Tests for User Story 1

- [X] T024 [P] [US1] Add `tests/FSBar.LiveTests/LiveAdminPauseTests.fs` with three tests that assert: (a) SC-001 — clock stationary for ≥ 10 s after `SessionManager.Pause`; (b) SC-002 — clock advances and units move within 1 s of `SessionManager.Resume`; (c) FR-004 — `startPaused = true` yields clock == 0 until first `Resume`. Follow existing `EngineFixture` pattern (headless engine). Mark `[Trait("Category","AdminChannel")]`.

### Implementation for User Story 1

- [X] T025 [US1] Add `Pause: unit -> SubmitOutcome` and `Resume: unit -> SubmitOutcome` members to `src/FSBar.Hub/SessionManager.fsi`; implement in `src/FSBar.Hub/SessionManager.fs` by submitting `AdminCommandOut.Pause true`/`Pause false` through the host. Keep `TogglePause()` as a convenience that picks from `IsPaused` (data-model.md §4).
- [X] T026 [US1] Remove the vestigial chat-based `SetPaused(bool)` path from `SessionManager` (data-model.md §4 — "replaces"). Delete associated helpers if any. Update `tests/FSBar.Hub.Tests/Baselines/SessionManager.baseline` via `SURFACE_AREA_UPDATE=1`.
- [X] T027 [US1] Defer the initial `Pause true` for `startPaused = true` launches until the autohost `ServerStartPlaying` event (code 2) arrives (research.md R9). Subscribe inside `SessionManager.Launch` via `AdminChannelHost.StatusChanges` / channel events; issue one `Submit(Pause true)` and then unsubscribe.
- [X] T028 [US1] Route the feature-038 Viewer-tab pause button in `src/FSBar.Hub.App/Tabs/ViewerTab.fs` through `SessionManager.TogglePause` instead of the cosmetic flag. Render the button `disabled` when `SessionManager.AdminStatus <> Some Attached` and show the reason inline below the button per FR-009 + research.md R7.
- [X] T029 [US1] Do NOT add `AdminToolbarAction` or per-button rect helpers to `src/FSBar.Hub.App/Tabs/ViewerTab.fsi` — they stay module-internal per data-model.md §6. Only change `.fsi` if `pauseButtonRect`'s signature genuinely shifts. Bump the corresponding baseline only on an actual `.fsi` change.
- [X] T030 [US1] Add `rpc Pause(PauseRequest) returns (PauseResponse);` and `rpc Resume(ResumeRequest) returns (ResumeResponse);` with their message types to `proto/hub/scripting.proto`; regenerate via `cd proto && buf generate`.
- [X] T031 [US1] Implement `Pause` and `Resume` RPC handlers in `src/FSBar.Hub/ScriptingHub.fs` — delegate to `SessionManager.Pause` / `.Resume`, map the returned `SubmitOutcome` into `AdminSubmitResult`, and echo the current `AdminChannelStatusInfo`.
- [X] T032 [US1] Add scripting live test `tests/FSBar.Hub.LiveTests/LiveScriptingAdminPauseTests.fs` — opens a gRPC channel to the running Hub, calls `Pause` then `Resume`, and asserts `outcome = SENT` + the echoed status is `ATTACHED`.

**Checkpoint**: US1 ships. Pause/resume works from the button AND from scripting; "Start paused" produces a real engine pause at the first simulation frame.

---

## Phase 4: User Story 2 — Real-time engine speed (Priority: P2)

**Goal**: Let the user change engine speed live from the Viewer-tab toolbar via preset buttons (0.5x/1x/2x/5x/10x) and a numeric input for custom values.

**Independent Test**: With a match running, click 5x — confirm game clock advances ≈5× wall time over 10 s. Type `2.5` into the custom field — confirm the effective multiplier. Enter `-1` — confirm local rejection with a visible error and speed unchanged.

### Tests for User Story 2

- [X] T033 [P] [US2] Add `tests/FSBar.LiveTests/LiveAdminSpeedTests.fs` with: (a) SC-003 — 5x multiplier yields ≈50 s of game time over 10 s wall time (±10%); (b) non-positive speed values return `SubmitOutcome.Rejected` without touching the socket (invariant I5 + FR-005). Headless engine, `[Trait("Category","AdminChannel")]`.

### Implementation for User Story 2

- [X] T034 [US2] Add `SetEngineSpeed: speed: float32 -> SubmitOutcome` to `src/FSBar.Hub/SessionManager.fsi`; implement in `src/FSBar.Hub/SessionManager.fs` with local validation (finite, > 0) before submitting `AdminCommandOut.SetGameSpeed` through the host.
- [X] T035 [US2] Extend the Viewer-tab admin toolbar in `src/FSBar.Hub.App/Tabs/ViewerTab.fs` with five preset buttons (0.5x/1x/2x/5x/10x) and a single-line numeric input per research.md R7 + Session 2026-04-17 Q2. Clicking a preset calls `SessionManager.SetEngineSpeed`; typing + Enter in the input parses + calls the same. Non-numeric or non-positive input shows an inline validation error adjacent to the input.
- [X] T036 [US2] Add `AdminToolbarAction.SelectSpeedPreset` and `SubmitCustomSpeed` DU cases to the **internal** `AdminToolbarAction` type in `src/FSBar.Hub.App/Tabs/ViewerTab.fs` (do NOT export via `.fsi` — data-model.md §6 keeps this type module-local). Route both actions to `SessionManager.SetEngineSpeed` in `src/FSBar.Hub.App/Program.fs` via the existing hit-test callback shape.
- [X] T037 [US2] Add `rpc SetEngineSpeed(SetEngineSpeedRequest) returns (SetEngineSpeedResponse);` to `proto/hub/scripting.proto` with the `speed` float field; regenerate.
- [X] T038 [US2] Implement `SetEngineSpeed` RPC handler in `src/FSBar.Hub/ScriptingHub.fs` — delegate to `SessionManager.SetEngineSpeed` and map the outcome.
- [X] T039 [US2] Surface engine-side speed-range rejection (inbound `GameWarning`) via `HubEvent.DiagnosticsLine Warning` — verify the subscription already does this from T013; add a coverage note to `AdminChannelHostTests.fs`.
- [X] T039a [US2] Add `tests/FSBar.Hub.LiveTests/LiveScriptingAdminSpeedTests.fs` — gRPC call `SetEngineSpeed { speed = 2.0f }` against a running Hub, assert `outcome = SENT`, `admin_channel_status.state = ATTACHED`, and that `SessionManager.CurrentSpeed = 2.0f` after the call (SC-007 parity smoke for US2).

**Checkpoint**: US2 ships independently of US3/US4. Pause + Resume + Speed all work.

---

## Phase 5: User Story 3 — Force-end (Priority: P2)

**Goal**: Let the user terminate a running match cleanly from the Viewer tab without `pkill`-ing the engine. Session returns to Idle within 5 s.

**Independent Test**: Launch a match, click ⏹ on the Viewer tab. Within 5 s the Hub status bar reads "session ended", the engine process is gone, and a relaunch succeeds.

### Tests for User Story 3

- [X] T040 [P] [US3] Add `tests/FSBar.LiveTests/LiveAdminForceEndTests.fs` with: (a) SC-004 — session transitions `Running → Idle` within 5 s of `ForceEnd` on a running headless match; (b) `ForceEnd` on a paused match produces the same clean shutdown (acceptance scenario 2); (c) relaunch after force-end does not leak pause/speed state (acceptance scenario 2b).

### Implementation for User Story 3

- [X] T041 [US3] Add `ForceEnd: unit -> SubmitOutcome` to `src/FSBar.Hub/SessionManager.fsi`; implement in `src/FSBar.Hub/SessionManager.fs` by submitting `AdminCommandOut.KillServer` through the host, then arming a 5 s wall-clock watchdog that calls `EngineLauncher.stopEngine` on timeout, and SIGKILL at 8 s, per research.md R8.
- [X] T042 [US3] Add a force-end button (⏹) to the Viewer-tab admin toolbar in `src/FSBar.Hub.App/Tabs/ViewerTab.fs`; hit-test emits `AdminToolbarAction.ForceEnd`; `Program.fs` routes to `SessionManager.ForceEnd`. Button is disabled when `AdminStatus <> Some Attached` OR when the session is already `Ending`/`Idle`.
- [X] T043 [US3] Add `rpc ForceEndMatch(ForceEndMatchRequest) returns (ForceEndMatchResponse);` to `proto/hub/scripting.proto`; regenerate.
- [X] T044 [US3] Implement `ForceEndMatch` RPC handler in `src/FSBar.Hub/ScriptingHub.fs` — delegate to `SessionManager.ForceEnd`.
- [X] T044a [US3] Add `tests/FSBar.Hub.LiveTests/LiveScriptingAdminForceEndTests.fs` — launch a session, gRPC call `ForceEndMatch`, assert `outcome = SENT` and the session transitions to `Idle` within 5 s (SC-007 parity smoke for US3).

**Checkpoint**: US3 ships. Force-end works from button + scripting. Ending-state leaks are tested.

---

## Phase 6: User Story 4 — Admin message (Priority: P3)

**Goal**: Let the user (or a scripting client) broadcast a text message into the match's in-game chat log, attributed by the engine's native autohost speaker (no Hub-supplied name).

**Independent Test**: Launch the graphical engine. Type "phase-start" into the admin-toolbar text input and hit Enter. Confirm the message appears in the in-game chat log credited to the autohost/spectator speaker, not an AI team.

### Tests for User Story 4

- [X] T045 [P] [US4] Add `tests/FSBar.LiveTests/LiveAdminMessageTests.fs` gated on `FSBAR_GRAPHICAL_OK=1` (graphical engine only — headless has no visible chat). Assert SC-005: message appears in the engine's chat-log buffer within one simulation tick of `SendAdminMessage`. Empty-string input returns `Rejected` without touching the socket.

### Implementation for User Story 4

- [X] T046 [US4] Add `SendAdminMessage: text: string -> SubmitOutcome` to `src/FSBar.Hub/SessionManager.fsi`; implement in `src/FSBar.Hub/SessionManager.fs` with `string.IsNullOrWhiteSpace` → `Rejected("empty message")`, otherwise submit `AdminCommandOut.SayMessage` through the host.
- [X] T047 [US4] Add an admin-message text input to the Viewer-tab admin toolbar in `src/FSBar.Hub.App/Tabs/ViewerTab.fs`. Enter submits, empty-string submit shows an inline validation error, input clears on successful submit.
- [X] T048 [US4] Add `AdminToolbarAction.SubmitAdminMessage of text: string` DU case to the **internal** type in `src/FSBar.Hub.App/Tabs/ViewerTab.fs` (no `.fsi` change). Route through `src/FSBar.Hub.App/Program.fs` to `SessionManager.SendAdminMessage`.
- [X] T049 [US4] Add `rpc SendAdminMessage(SendAdminMessageRequest) returns (SendAdminMessageResponse);` with `string text = 1;` to `proto/hub/scripting.proto`; regenerate.
- [X] T050 [US4] Implement `SendAdminMessage` RPC handler in `src/FSBar.Hub/ScriptingHub.fs` — delegate to `SessionManager.SendAdminMessage`.
- [X] T050a [US4] Add `tests/FSBar.Hub.LiveTests/LiveScriptingAdminMessageTests.fs` — gRPC call `SendAdminMessage { text = "parity-smoke" }`, assert `outcome = SENT`, `admin_channel_status.state = ATTACHED`, and (when `FSBAR_GRAPHICAL_OK=1`) that the string appears in the engine's chat buffer. In headless mode, assert only the SENT outcome (SC-007 parity smoke for US4).

**Checkpoint**: All four user stories ship independently.

---

## Phase 7: Polish & Cross-cutting

- [X] T051 Create `scripts/examples/16-hub-admin.fsx` that opens a gRPC channel to `127.0.0.1:5021`, calls each of the five admin RPCs (`Pause`, `Resume`, `SetEngineSpeed 2.0`, `SendAdminMessage "hi"`, `ForceEndMatch`) and prints each `AdminSubmitResult`. Per Constitution §V Scripting Accessibility.
- [X] T052 [P] Update `CLAUDE.md` with a new "Hub admin channel (feature 039)" section describing `AdminChannel` / `AdminChannelHost` / toolbar layout / the five `SessionManager` members / the five new RPCs. Mirror the style of the existing "Central GUI hub" and "Hub pause + Start-paused" sections.
- [ ] T053 [P] Walk through `specs/039-hub-admin-channel/quickstart.md` end-to-end on a fresh Hub launch (US1, US1b, US2, US3, US4 graphical, channel-unavailable surface, scripting). File any observed discrepancies as follow-up tasks here before closing.
- [X] T054 Run `dotnet test FSBarV1.slnx` from the repo root and confirm: all pre-feature-039 surface-area baselines remain green (SC-008 — no regression), every `AdminChannel*` unit test passes, and the `AdminChannel` live-test category `dotnet test --filter "Category=AdminChannel"` is green against `spring-headless`.
- [X] T055 [P] Bump baselines `tests/FSBar.Client.Tests/Baselines/EngineConfig.baseline` and any other `.baseline` files touched by T004–T050 via `SURFACE_AREA_UPDATE=1 dotnet test FSBarV1.slnx`; commit the updated baselines. Review the diff to confirm only intended surface changes are introduced.
- [X] T056 [P] Pack and smoke `FSBar.Client` + `FSBar.Hub` prereleases into `nupkg/` using each project's existing `pack-dev.sh` pattern (CLAUDE.md §Upstream dependency workflow). Confirms the new public types export cleanly for downstream scripting consumers.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 Setup** — no dependencies; T002 + T003 can run in parallel with T001.
- **Phase 2 Foundational** — depends on Phase 1.
  - Within Phase 2: T004 → T005 → T006 sequence is linear (same launch flow).
  - T007 → T008 → T009 sequence is linear (same `AdminChannel` module).
  - T010 and T011 gated on T008 + T009 landing but are mutually parallel.
  - T012 → T013 → T014 sequence is linear (`AdminChannelHost` module).
  - T015 and T016 mutually parallel after T014.
  - T017 → T018 → T019 linear (each edits `SessionManager` or a dependency).
  - T020 → T021 → T022 → T023 linear (proto edits share one file + regen).
  - Subsections 2A, 2B, 2C, 2D, 2E are largely independent — 2A/2B can run in parallel with 2E; 2C depends on 2B's surface.
  - **Exit gate**: T018 + T019 + T022 + T023 + T023a all complete.
- **Phase 3 US1** — depends on Phase 2 exit.
- **Phases 4/5/6 US2/US3/US4** — each depends on Phase 2 exit. Can run in parallel with US1 and each other if staffed that way.
- **Phase 7 Polish** — depends on all desired user stories.

### User story dependencies

- **US1 (P1)**: Independent of US2/US3/US4 after Phase 2 exit.
- **US2 (P2)**: Independent. Uses the same `SessionManager`/`ScriptingHub`/`ViewerTab` files as US1 — sequence per-file edits or rebase carefully.
- **US3 (P2)**: Independent. Same file-conflict note as US2.
- **US4 (P3)**: Independent. Same file-conflict note.

### Within each user story

- Write the live test first (research-driven TDD per spec-kit convention); watch it fail against the current Phase-2-only tree.
- Then ship the `SessionManager` member, the Viewer-tab widget, the new RPC, and the RPC handler (any order within the story).
- Finish with the scripting live test (US1 only — the per-story scripting parity smoke).

### Parallel opportunities

- **Phase 1**: T002, T003 parallel to T001.
- **Phase 2**: Subsections 2A, 2B, 2E are cross-file and mostly parallel; 2C depends on 2B's surface; 2D mutates `SessionManager` and is best serialized.
- **Phase 3+**: Each US owns distinct test files (`LiveAdmin*Tests.fs`) that can be authored in parallel with the matching production code.
- **Phase 7**: T052, T053, T055, T056 are all independent.

---

## Parallel Example: Phase 2 launch

```bash
# Once T001 is green, kick off three parallel tracks:
Task: "T004..T006 — EngineConfig/ScriptGenerator/EngineLauncher autohost wiring"
Task: "T007..T011 — FSBar.Client.AdminChannel codec + unit tests"
Task: "T020..T023 — proto scaffolding + ScriptingHub status passthrough"
```

## Parallel Example: User Story live tests

```bash
# After Phase 2 exits, author all four US live test files in parallel:
Task: "T024 [US1] tests/FSBar.LiveTests/LiveAdminPauseTests.fs"
Task: "T033 [US2] tests/FSBar.LiveTests/LiveAdminSpeedTests.fs"
Task: "T040 [US3] tests/FSBar.LiveTests/LiveAdminForceEndTests.fs"
Task: "T045 [US4] tests/FSBar.LiveTests/LiveAdminMessageTests.fs"
```

---

## Implementation Strategy

### MVP scope (US1 only)

1. Complete Phase 1 Setup (T001–T003).
2. Complete Phase 2 Foundational (T004–T023) — the whole autohost stack lands here.
3. Complete Phase 3 US1 (T024–T032) — real pause/resume button + scripting.
4. **STOP and VALIDATE**: manual walkthrough of quickstart §1 + §2 + §7; `tests/run-all.sh --filter "Category=AdminChannel&Priority=1"`.
5. Merge the MVP or demo.

### Incremental delivery

1. MVP (US1) → merge.
2. Add US2 (speed) → merge; spec-kit users run quickstart §3.
3. Add US3 (force-end) → merge; quickstart §4.
4. Add US4 (admin message) → merge; quickstart §5 (graphical-only).
5. Each increment adds one admin capability end-to-end without touching the prior ones.

### Parallel team strategy

- Dev A: Phase 2A + Phase 2D (engine wiring + session integration).
- Dev B: Phase 2B + Phase 2C (AdminChannel + AdminChannelHost + baselines).
- Dev C: Phase 2E (proto + ScriptingHub passthrough).
- After Phase 2: A → US1, B → US2, C → US3; whoever finishes first picks up US4; everyone converges on Phase 7.

---

## Notes

- Every `.fs` edit that changes public surface MUST be paired with a matching `.fsi` edit and a regenerated baseline per Constitution §II.
- `protoc-gen-fsgrpc` is not on nuget.org — installs via `~/tools/fsGRPCSkills/fsgrpc-setup/scripts/install-protoc-gen-fsgrpc.sh` per CLAUDE.md.
- Live tests require `HIGHBAR_TEST_ENGINE` or the auto-detected engine in `~/.local/state/Beyond All Reason/engine/recoil_*`. US4 graphical tests gate on `FSBAR_GRAPHICAL_OK=1`.
- Never mark a failing test "passed"; skip with a reason when the engine build doesn't expose the autohost ABI the test expects.
- The `AdminChannel` receive loop runs on a dedicated thread and MUST NOT swallow exceptions — surface them as `ChannelState.Closed(reason)` + `HubEvent.DiagnosticsLine Error` per Constitution §IV.
- No new NuGet dependencies per plan.md §Technical Context — if a task "needs" one, stop and escalate.
