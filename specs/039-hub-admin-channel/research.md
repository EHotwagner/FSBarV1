# Phase 0 Research — Hub admin/host channel

**Feature**: 039-hub-admin-channel · **Date**: 2026-04-17

All "NEEDS CLARIFICATION" entries from the Technical Context are
resolved here. No spec-level clarifications remain open; the five
Session 2026-04-17 Q/A entries in the spec pre-answer the UX questions.

## R1 — Admin-channel transport

**Decision**: Use the engine's native **autohost UDP interface**
(config vars `AutohostIP`, `AutohostPort`), bound on `127.0.0.1` at an
OS-allocated free port, set via `springsettings.cfg` at session launch.

**Rationale**:

- Feature 038 discovered (and the SessionManager source documents) that
  sending `/pause` as a chat command via the Skirmish-AI
  `SendTextMessage` path **crashes** the headless engine. Chat is not a
  fit for admin control.
- The HighBar proxy's Skirmish-AI protocol exposes `COMMAND_PAUSE`
  (global pause) in the engine ABI but the bundled proxy at
  `proxy/bundled/0.1` does **not** wire it through
  `deserialize.c`. It also exposes no command for game-speed, no
  command for force-end, and no command for host-attributed chat. So
  this path covers at most one of four required admin operations and
  would still require a proxy rebuild.
- The engine's autohost interface is a first-class,
  documented-in-config-vars feature of the Spring/Recoil family. It
  natively covers pause, set-game-speed, say-message, and kill-server,
  and emits the inbound events (`SERVER_STARTED`,
  `SERVER_STARTPLAYING`, `SERVER_QUIT`, `SERVER_GAMEOVER`,
  `PLAYER_CHAT`, `GAME_WARNING`, ...) the Hub needs to surface
  channel-loss and sim-clock-start signals. `AutohostIP` defaults to
  `127.0.0.1` and `AutohostPort` defaults to `0` (disabled), which
  matches the spec's "launch-time key exchange" assumption.
- This path works **identically** for `spring-headless` and `spring`
  (FR-012) because the autohost interface is in the engine's shared
  networking layer (`rts/Net/GameServer.cpp`), not the graphical /
  headless front-end.

**Alternatives considered**:

- *Extend the HighBarV2 proxy* (add `COMMAND_PAUSE`, and
  non-existent-in-ABI gamespeed/forceend commands). Rejected: the
  proxy's Skirmish-AI ABI does not have speed/force-end primitives;
  shipping half via SAI and half via chat would fail the spec's "no
  silent divergence" tests and require modifying a sibling repo.
- *Chat commands via `SSendTextMessageCommand`* (`/pause`, `/gamespeed`,
  `/forceend`). Rejected: documented crash on `/pause` in feature 038
  and unclear admin-privilege semantics for an AI-team chatter.
- *Direct Lua synchronized message via `COMMAND_CALL_LUA_RULES`*.
  Rejected: requires BAR-game Lua widget support that the mainline BAR
  install does not advertise; would tie the Hub's admin surface to a
  specific BAR mod/game version.

## R2 — Autohost port allocation

**Decision**: The Hub allocates a loopback UDP port by binding a
`UdpClient` to `IPEndPoint(IPAddress.Loopback, 0)` **before** the
engine is spawned, reads back the OS-assigned port via
`LocalEndpoint`, writes that port into the per-session
`springsettings.cfg` (new `AutohostPort = N` line), and passes the
same socket to `AdminChannel` for receive. On success, no race exists;
on failure (socket exhaustion) the launch fails with a clear
`AdminChannelUnavailable` reason.

**Rationale**: OS-allocated ephemeral ports eliminate the collision
risk mentioned in the edge-case list. Binding before launch also means
the hub owns the recv socket end-to-end — the engine simply dials
back into it. Writing via the existing `ScriptGenerator` keeps the
launch-script path as the single source of truth.

**Alternatives considered**:

- Fixed port (`--autohost-port=5030`). Rejected: two concurrent sessions
  would collide, and our edge-case list calls out port conflict
  explicitly.
- Passing the port via engine CLI flag. Rejected: Recoil's CLI flags
  don't include an autohost-port switch; the mechanism is config var
  only.

## R3 — Autohost wire format

**Decision**: Implement the Spring/Recoil autohost wire format in
`FSBar.Client.AdminChannel` with `.fsi`-gated encode/decode functions.
The format is byte-oriented: each message is a 1-byte action code
followed by a payload (for outbound) or a 1-byte action code +
length-prefixed strings / fixed-width numeric fields (for inbound).
Authoritative source is the engine's
`rts/Net/AutohostInterface.cpp` (open source). The implementation
follows the table in `contracts/autohost-wire.md` — verified during
live tests against `spring-headless` 2026.xx.

**Outbound (hub → engine) action codes in scope**:

| Code | Name | Payload |
|------|------|---------|
| 4 | `SETGAMESPEED` | `float32` speed |
| 5 | `PAUSE` / `SETGAMEPAUSE` | `uint8` boolean (0/1) |
| 8 | `SAYMESSAGE` | UTF-8 bytes (length-framed by UDP datagram size) |
| 0 | `KILLSERVER` | *(empty)* — used for force-end |

**Inbound (engine → hub) action codes surfaced to the Hub**:

| Code | Name | Hub action |
|------|------|-----------|
| 0 | `SERVER_STARTED` | `AdminChannelStatus := Attached` |
| 1 | `SERVER_QUIT` | `AdminChannelStatus := Lost("engine quit")`; session → Ending |
| 2 | `SERVER_STARTPLAYING` | Unblock deferred first-frame pause (FR-004) |
| 3 | `SERVER_GAMEOVER` | Noted in diagnostics; session lifecycle handled separately |
| 7 | `PLAYER_CHAT` | Noted for later scripting consumers; not surfaced in UI for v1 |
| 11 | `GAME_WARNING` | `HubEvent.DiagnosticsLine(Warning, ...)` |

Other inbound action codes are decoded to a generic
`AdminEvent.Unknown(code, bytes)` branch, logged at `Info`, and
ignored. This avoids hard-coding a fragile "engine version ABI"
assumption while preserving forward compatibility.

**Rationale**: The action-code table above matches the documented
autohost ABI; enumerating inbound codes defensively (with an `Unknown`
branch) is how existing `FSBar.Client.Callbacks` handles engine-ABI
drift. Verification happens at live-test time.

**Alternatives considered**:

- Parse every known inbound code into a strongly-typed DU branch.
  Rejected: most inbound codes (team-stat, lua-msg, player-defeated)
  are orthogonal to this feature and would balloon the surface area
  without test evidence.
- Encode outbound only, don't parse inbound. Rejected: FR-009 requires
  channel-loss detection, which needs at least `SERVER_QUIT` /
  `SERVER_STARTED` and a heartbeat-or-silence signal.

## R4 — State synchronization model

**Decision**: The Hub's `AdminChannelStatus` is authoritative for
control-enablement. The Hub's `IsPaused` / `CurrentSpeed` reflect the
most-recent successfully-acknowledged admin command (optimistic —
updated as soon as the UDP `send` returns, NOT after engine ack). On
`SERVER_QUIT` or socket error, status transitions to
`Lost(reason)` and all admin controls disable. Engine-native pause
via in-game keybind (graphical client) produces no autohost signal, so
the Hub's displayed pause state temporarily drifts; the next admin
command forces convergence per FR-010 — the spec explicitly permits
this ("next admin command resyncs").

**Rationale**: The spec's FR-010 and the "two rapid clicks" edge case
both boil down to: the Hub's UI reflects its own intent, not an
engine-side mirror. A full bidirectional consistency model
(poll-for-state, acknowledgement handshakes) is out of scope for this
feature and would require extending the autohost wire format.

**Alternatives considered**:

- Maintain a full shadow of engine pause state via polling a Lua
  synced gadget. Rejected: out of scope (would re-open path 3 in R1)
  and not required by the spec.

## R5 — Rapid-click de-duplication

**Decision**: `AdminChannelHost` serializes outbound commands through a
single `MailboxProcessor<AdminCommand>` (one command in flight at a
time). If a new command of the same kind arrives while one is pending
or while a ≤100 ms "quiet window" is active, it **coalesces** onto the
last — the agent drops older versions and keeps only the newest. The
engine's autohost accepts repeat writes cheaply, so the worst case is
one redundant packet.

**Rationale**: Meets FR-011 ("rapid repeat requests collapse to the
last click's end state"), avoids the complexity of per-kind state
machines, and is a textbook F# `MailboxProcessor` pattern already used
elsewhere in the codebase (e.g. `BarClient` command queue).

## R6 — Scripting-service RPC shape

**Decision**: Add five new unary RPCs to `ScriptingService`:
`Pause(PauseRequest) → PauseResponse`,
`Resume(ResumeRequest) → ResumeResponse`,
`SetEngineSpeed(SetEngineSpeedRequest) → SetEngineSpeedResponse`,
`ForceEndMatch(ForceEndMatchRequest) → ForceEndMatchResponse`,
`SendAdminMessage(SendAdminMessageRequest) → SendAdminMessageResponse`.
Each returns a small response that echoes the resulting
`AdminChannelStatus` and an optional rejection reason (so clients do
not need to round-trip through `GetSessionStatus`). Also extend
`ActiveSession` in the status response with a new
`AdminChannelStatusInfo` sub-message so `GetSessionStatus` surfaces
channel availability (FR-008 + scripting-parity requirement).

**Rationale**: Separate RPCs per action (rather than one "AdminCommand"
oneof) match the style of the existing `SendCommand` / `GetUnitDef`
RPCs and keep authz + validation errors per-RPC legible. Consumers
never have to unpack a variant to learn what failed.

**Alternatives considered**:

- Single `SendAdminCommand(oneof command)` RPC. Rejected: obscures
  intent in error messages and wire-logs. Spec allows either; per-RPC
  is idiomatic for this repo.
- Reuse `SendCommand` and add new `AICommand` variants. Rejected:
  `AICommand` is the Skirmish-AI protocol and the whole point of this
  feature is to move admin control OFF that protocol.

## R7 — UI toolbar layout

**Decision** (all pre-answered by Session 2026-04-17 clarifications —
captured here for traceability):

- One admin toolbar lives in the Viewer-tab content area, top-right,
  adjacent to the feature-038 pause button (Q1).
- Engine-speed control: five preset buttons (0.5x / 1x / 2x / 5x /
  10x) + a small numeric input (Q2).
- Channel-unavailable status: inline status line in the same toolbar,
  spatially adjacent to the disabled controls, including the reason
  (Q3).
- Admin messages are submitted via a text input in the same toolbar.
  Attribution is whatever the engine natively supplies — no hub-side
  speaker string (Q4).
- No persistence of engine-speed across Hub restarts — matches always
  launch at 1.0x (Q5).

**Layout sketch** (renders within `ViewerTab.render`'s content rect,
right-aligned with 8 px gutter):

```
┌────────────────────────────────────────────────────────────────┐
│                                      [⏸] [⏹]  [0.5x][1x][2x]  │
│                                      [5x][10x] [   1.0x   ↵]  │
│                                      [ admin msg…         ]↵] │
│                                      Admin channel attached   │
└────────────────────────────────────────────────────────────────┘
```

When `AdminChannelStatus = Unavailable/Lost`, all buttons render with
a disabled visual and the status line shows the reason (e.g.
`Admin channel unavailable: port conflict`). The feature-038
`pauseButtonRect` helper stays but its hit-test routes through the
admin host.

## R8 — Force-end semantics

**Decision**: `ForceEnd` sends autohost `KILLSERVER` (code 0) to the
engine, then waits up to 5 s for the engine process to exit. If the
process hasn't exited after 5 s, `EngineLauncher.stopEngine` SIGTERMs
it; if still alive at 8 s, SIGKILL. The Hub transitions to `Ending →
Idle` as the process exits, exactly as for a natural session end
(spec FR-006, SC-004).

**Rationale**: `KILLSERVER` is the engine's canonical "admin told me to
quit" path — cleaner than an unconditional SIGTERM, because the engine
gets to flush infologs and write the final replay. SIGTERM escalation
covers the "engine wedged" case.

## R9 — "Start paused" from feature 038

**Decision**: Replace the feature-038 flag-only behavior in
`SessionManager`. When `startPaused = true`, `SessionManager` defers
issuing the initial `PAUSE(true)` command until it receives the
autohost `SERVER_STARTPLAYING` (code 2) event — this is the first
instant the engine will honor a pause, so FR-004 ("real simulation
pause at the first engine frame") is satisfied without races.
`HubSettings.StartPausedDefault` already exists and needs no schema
change.

## Open questions resolved

Every numbered open question from the research report is resolved:

1. Engine autohost CLI flag → config-var mechanism via
   `springsettings.cfg` (R2).
2. Admin-channel attach timing → UDP socket bound pre-launch, event
   stream starts at `SERVER_STARTED` (R2, R3).
3. Port allocation → OS-assigned ephemeral on loopback (R2).
4. Graphical-client pause drift → accepted via FR-010 semantics;
   next admin command resyncs (R4).
5. Force-end signal → `KILLSERVER` with timed SIGTERM/SIGKILL
   escalation (R8).
6. Admin message attribution → engine-native autohost speaker per Q4;
   no hub-supplied name (R7).
7. Engine-speed range → per engine autohost ABI; Hub validates the
   value is finite and positive, lets the engine reject values beyond
   its supported range with an inbound warning event (surfaced via
   `HubEvent.DiagnosticsLine` Warning).
8. HighBarV2 proxy admin forwarding → not needed. Autohost channel
   is independent of the proxy (R1).
