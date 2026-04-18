# Implementation Plan: Hub admin/host channel

**Branch**: `039-hub-admin-channel` | **Date**: 2026-04-17 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/039-hub-admin-channel/spec.md`

## Summary

Feature 038 shipped a pause button that only freezes the Hub's rendered
view — the simulation keeps running. This feature replaces that cosmetic
pause with a real admin control link to the engine and exposes four
additional admin capabilities (resume, engine-speed change, force-end,
admin chat broadcast) on the Viewer tab and through the gRPC scripting
service.

Technical approach: open the engine's native **autohost UDP interface**
(`AutohostPort` / `AutohostIP` config vars, discovered at 2026-04-17 via
`spring-headless --list-config-vars`). The Hub allocates a free UDP port
at session launch, writes it into `springsettings.cfg` via the existing
`ScriptGenerator`, binds a loopback UDP socket before the engine starts,
and parses the engine→hub event stream (SERVER_STARTED, SERVER_QUIT,
PLAYER_CHAT, GAME_LUAMSG, ...) while sending hub→engine commands
(SETGAMESPEED, PAUSE, SAYMESSAGE, KILLSERVER). A new
`FSBar.Client.AdminChannel` module owns the wire protocol; a new
`FSBar.Hub.AdminChannelHost` serializes hub intents onto that channel,
de-duplicates rapid-fire clicks (FR-011), and maintains
`AdminChannelStatus`. `SessionManager` integrates the host, exposing
`Pause`, `Resume`, `SetSpeed`, `ForceEnd`, `SendAdminMessage`, and
replacing the vestigial chat-based `SetPaused` path. Viewer tab grows an
admin toolbar anchored next to the existing pause button; scripting
service gains five new RPCs in `proto/hub/scripting.proto`.

## Technical Context

**Language/Version**: F# 9 on .NET 10.0 (exclusive per Constitution §Engineering Constraints)
**Primary Dependencies**: Existing in-repo — `FSBar.Client`, `FSBar.Hub`, `FSBar.Viz`, `FSBar.Proto`. BCL `System.Net.Sockets.UdpClient` + `System.Threading.Channels` for the autohost socket. `Grpc.AspNetCore 2.67.0` / `Grpc.Core.Api 2.67.0` already in the graph for scripting. `SkiaViewer` 1.1.3-dev for UI. `xUnit 2.9.x` for tests. **No new NuGet dependencies.**
**Storage**: In-memory only — `AdminChannelStatus` lives for the session's lifetime; no persistence. `HubSettings` is not extended (engine speed does NOT persist across launches per Session 2026-04-17 Q5).
**Testing**: Unit tests for autohost wire-format encode/decode; live integration tests under `tests/FSBar.LiveTests/` exercising pause/resume/speed/force-end against a real `spring-headless` process (following the existing `EngineFixture` pattern).
**Target Platform**: Linux dev env (primary) via `dotnet` on .NET 10.0; cross-platform BCL UDP works wherever the engine runs.
**Project Type**: Desktop-app (SkiaViewer Hub GUI) + packable F# libraries + gRPC service. Existing mixed layout under `src/FSBar.*`, `src/FSBar.Hub.App/`, `proto/`.
**Performance Goals**: Admin commands land within one simulation tick of the request (FR-002, SC-001..005, ≤ 100 ms wall-clock at 1.0x). Channel-loss diagnostic surfaces within ten seconds (SC-006). UDP traffic is a few packets per user-action — no throughput concern.
**Constraints**: Loopback only (127.0.0.1); UDP socket bound before engine launch to guarantee port availability. Admin commands must work identically on `spring` and `spring-headless` (FR-012). De-duplicate back-to-back identical requests so N pause clicks resolve to the last click's state (FR-011). No silent failure if the channel never attaches (FR-009) — disable controls and show an inline reason in the Viewer-tab toolbar.
**Scale/Scope**: One session at a time (Hub has always been single-session per feature 035). Five admin capabilities. One new F# module in `FSBar.Client`, one new module in `FSBar.Hub`, one new tab toolbar block in `FSBar.Hub.App`, five new gRPC RPCs. ~400–600 LoC of production F#; ~300 LoC of tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Spec-First Delivery | ✅ | Spec with five answered clarifications, checklist under `specs/039-hub-admin-channel/checklists/requirements.md`, this plan traces every FR to a module. |
| II. Compiler-Enforced Structural Contracts | ✅ | Every new public module ships `.fsi` first; every changed module bumps its surface-area baseline under `tests/FSBar.*.Tests/Baselines/`. Explicit .fsi targets enumerated in §Project Structure. |
| III. Test Evidence Is Mandatory | ✅ | Phase 2 tasks will include: unit tests for `AdminChannel` codec; live tests for each user story (pause, speed, force-end, admin message); status-propagation test via `HubEvents`; scripting-service wiring test. |
| IV. Observability and Safe Failure Handling | ✅ | Channel attach / loss / degradation publishes `HubEvent.DiagnosticsLine` + a new `HubEvent.AdminChannelStatusChanged`. Disabled controls render an inline reason string adjacent to them (FR-009). No swallowed exceptions in the UDP receive loop. |
| V. Scripting Accessibility | ✅ | New `scripts/examples/NN-hub-admin.fsx` demonstrates pause / resume / speed / force-end / message via the gRPC `ScriptingService`. Existing `scripts/prelude.fsx` need not change (only API additions). |
| Engineering Constraints: F#-only | ✅ | No new languages. |
| Engineering Constraints: packable libs | ✅ | `FSBar.Client` and `FSBar.Hub` already produce packages; new modules ride that path. |
| Engineering Constraints: gRPC via fsgrpc | ✅ | Additions to `proto/hub/scripting.proto` regenerated via `cd proto && buf generate` using the existing `protoc-gen-fsgrpc` toolchain (documented in CLAUDE.md). |
| Engineering Constraints: no new dependencies | ✅ | Everything on the existing BCL + in-repo graph. |

**No gate violations. No Complexity Tracking entry required.**

## Project Structure

### Documentation (this feature)

```text
specs/039-hub-admin-channel/
├── plan.md              # This file
├── spec.md              # Feature spec (input)
├── research.md          # Phase 0 output — transport choice, wire format, UI layout
├── data-model.md        # Phase 1 output — AdminChannel / AdminChannelStatus / AdminCommand
├── quickstart.md        # Phase 1 output — manual + live-test smoke walkthrough
├── contracts/
│   ├── scripting-admin.proto   # Delta over existing proto/hub/scripting.proto
│   └── autohost-wire.md        # Engine autohost UDP wire-format notes
└── checklists/
    └── requirements.md  # Pre-existing requirements checklist
```

### Source Code (repository root)

New files (`.fsi` + `.fs` paired where applicable):

```text
src/
├── FSBar.Client/
│   ├── AdminChannel.fsi               # NEW — autohost UDP client surface
│   └── AdminChannel.fs                # NEW — codec + socket pump
├── FSBar.Hub/
│   ├── AdminChannelHost.fsi           # NEW — hub-side channel owner
│   ├── AdminChannelHost.fs            # NEW — de-dupe, status, binds to SessionManager
│   ├── HubEvents.fsi                  # MODIFIED — add AdminChannelStatusChanged
│   ├── HubEvents.fs                   # MODIFIED — same
│   ├── SessionManager.fsi             # MODIFIED — add Pause/Resume/SetSpeed/ForceEnd/SendAdminMessage + AdminStatus
│   └── SessionManager.fs              # MODIFIED — launch autohost port, attach host, wire startPaused to real pause
└── FSBar.Hub.App/
    └── Tabs/
        ├── ViewerTab.fsi              # MODIFIED — add admin toolbar rect helpers + hit-test types
        └── ViewerTab.fs               # MODIFIED — render toolbar, route clicks

src/FSBar.Client/
└── ScriptGenerator.fs                 # MODIFIED — emit AutohostPort + AutohostIP into springsettings.cfg

src/FSBar.Client/
├── EngineConfig.fsi                   # MODIFIED — optional AutohostPort field
└── EngineConfig.fs                    # MODIFIED — same

src/FSBar.Client/
├── EngineLauncher.fsi                 # MODIFIED — (no signature change; env passthrough)
└── EngineLauncher.fs                  # MODIFIED — pre-bind UDP socket before spawn

src/FSBar.Hub.App/
└── Program.fs                         # MODIFIED — route admin toolbar actions to SessionManager

proto/hub/scripting.proto              # MODIFIED — add Pause/Resume/SetSpeed/ForceEndMatch/SendAdminMessage RPCs
src/FSBar.Proto/Generated/hub/...      # REGENERATED — `cd proto && buf generate`

tests/
├── FSBar.Client.Tests/
│   ├── AdminChannelCodecTests.fs      # NEW — encode/decode each inbound + outbound message
│   └── Baselines/AdminChannel.baseline # NEW
├── FSBar.Hub.Tests/
│   ├── AdminChannelHostTests.fs       # NEW — dedupe, status transitions, restart behavior
│   └── Baselines/AdminChannelHost.baseline # NEW
└── FSBar.LiveTests/
    ├── LiveAdminPauseTests.fs         # NEW — US1 / US2 / US3 live
    └── LiveAdminMessageTests.fs       # NEW — US4 live (graphical-only due to chat visibility)

scripts/examples/
└── NN-hub-admin.fsx                   # NEW — FSI walkthrough for all five admin ops
```

**Structure Decision**: The repository follows an established
`src/FSBar.*` library + `src/FSBar.Hub.App/` executable layout (see
CLAUDE.md "Central GUI hub" section). Feature 039 adds:

1. One library module pair in `FSBar.Client` (`AdminChannel`) — the
   wire-level autohost client, reusable from bots or scripts.
2. One library module pair in `FSBar.Hub` (`AdminChannelHost`) — the
   hub-side orchestrator that speaks `AdminChannel` and folds status
   into `HubEvents`. Kept out of `FSBar.Hub.App` so headless scripting
   clients can drive admin capabilities without a GUI dependency.
3. One UI toolbar block in `FSBar.Hub.App/Tabs/ViewerTab.fs`, anchored
   next to the feature-038 pause button per Session 2026-04-17 Q1.
4. Proto additions to `proto/hub/scripting.proto` regenerated via
   `buf generate` per CLAUDE.md "Hub scripting proto regeneration".

No new project, no new top-level directory, no new NuGet package.

## Complexity Tracking

> Constitution Check produced no violations, so this section is empty
> by design.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)* | | |
