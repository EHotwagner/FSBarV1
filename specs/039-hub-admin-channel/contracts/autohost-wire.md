# Autohost wire-format contract

**Feature**: 039-hub-admin-channel · **Date**: 2026-04-17

This document is the spec-side authoritative record of the engine's
autohost UDP wire format that `FSBar.Client.AdminChannel` speaks.
The canonical implementation-side reference is the Recoil engine's
`rts/Net/AutohostInterface.cpp`; the numbers below were cross-checked
against the `--list-config-vars` output on the repo's current engine
install (`recoil_2025.06.19` → `AutohostIP`, `AutohostPort`).

Any mismatch discovered during live testing MUST be resolved by
updating this document **before** adjusting code — the wire-format
contract leads the implementation.

## Transport

- **Protocol**: UDP
- **Engine side**: listens on `AutohostIP` (default `127.0.0.1`) :
  `AutohostPort` (default `0` — disabled; Hub sets a non-zero value).
- **Hub side**: binds a UDP socket on `127.0.0.1:0` and reads
  the OS-assigned port; writes it into the per-session
  `springsettings.cfg` via `FSBar.Client.ScriptGenerator`. The engine
  dials back and the first inbound datagram establishes the hub's
  view of the engine endpoint.
- **Framing**: one UDP datagram = one message. No stream reassembly.
- **Encoding**: first byte = action code; remainder = payload.
  Multi-byte numerics are little-endian (native Spring convention).

## Outbound action codes (hub → engine)

| Code | Name | Payload | Hub use |
|-----:|------|---------|---------|
| 0 | `KILLSERVER` | *(empty)* | Force-end (US3, FR-006) |
| 4 | `SETGAMESPEED` | `float32` (4 B, little-endian) | Speed control (US2, FR-005) |
| 5 | `PAUSE` | `uint8` boolean (1 B: 0 = resume, 1 = pause) | Pause / resume (US1, FR-002/003/004) |
| 8 | `SAYMESSAGE` | UTF-8 text, length = datagram size − 1 | Admin message (US4, FR-007) |

Codes 1 (`SILENCE`), 2 (`KICK`), 3 (`MUTE`), 6 (`SETMINSPEED`),
7 (`SETMAXSPEED`) exist but are **out of scope** for this feature.

## Inbound action codes (engine → hub)

| Code | Name | Payload shape | Hub action |
|-----:|------|---------------|------------|
| 0 | `SERVER_STARTED` | *(empty)* | `AdminChannelStatus := Attached` |
| 1 | `SERVER_QUIT` | *(empty)* | `AdminChannelStatus := Lost("engine quit")`; session → Ending |
| 2 | `SERVER_STARTPLAYING` | `gameId: 16 B` (ignored for v1) | Unblock deferred `startPaused` pause (FR-004) |
| 3 | `SERVER_GAMEOVER` | `playerId: 1 B` + winning team IDs | Diagnostics only |
| 4 | `PLAYER_JOINED` | `playerId: 1 B` + name | Diagnostics only |
| 5 | `PLAYER_LEFT` | `playerId: 1 B` + `reason: 1 B` | Diagnostics only |
| 6 | `PLAYER_READY` | `playerId: 1 B` + `state: 1 B` | Diagnostics only |
| 7 | `PLAYER_CHAT` | `playerId: 1 B` + `destination: 1 B` + text | v1: diagnostics only |
| 8 | `PLAYER_DEFEATED` | `playerId: 1 B` | Diagnostics only |
| 9 | `GAME_LUAMSG` | player + mode + script + msgsize + msg | Diagnostics only |
| 10 | `GAME_TEAMSTAT` | team-stat packet | Diagnostics only |
| 11 | `GAME_WARNING` | UTF-8 text | `HubEvent.DiagnosticsLine(Warning, text)` |

Inbound codes not listed here are decoded as
`AdminEventIn.Unknown(code, payload)`, logged at `Info`, and ignored —
so future engine revisions remain forward-compatible.

## Liveness model

- No heartbeat is emitted by the engine on this channel. The Hub
  treats `AdminChannelStatus = Attached` as persistent between
  `SERVER_STARTED` and `SERVER_QUIT` (or an unsolicited socket
  error).
- If the engine process exits without sending `SERVER_QUIT`
  (crash, SIGKILL), `AdminChannel` observes the socket failure on the
  next send, transitions to `ChannelState.Closed`, and
  `AdminChannelHost` emits `AdminChannelStatus := Lost("socket error: …")`.

## Validation rules (Hub-side)

1. `SETGAMESPEED` payloads must be finite and `> 0`. Non-positive or
   NaN values reject with `SubmitOutcome.Rejected "engine speed must be a positive number"`
   without touching the socket.
2. `SAYMESSAGE` text must be non-empty after trimming surrounding
   whitespace. Empty rejects locally.
3. `PAUSE` is idempotent from the Hub's perspective; submitting
   `Pause(true)` when `IsPaused = true` is a coalesced no-op.
4. Engine-side range enforcement (e.g. "speed > 30 clamped") surfaces
   as a `GAME_WARNING` inbound event. The Hub does not pre-clamp.

## Versioning

The wire format is defined by the engine and is not under this
project's versioning control. The `.fsi` for
`FSBar.Client.AdminChannel` is versioned — any newly-supported
inbound or outbound action code is an `AdminChannel` surface change
and MUST be accompanied by a spec/plan update and surface-area
baseline bump.
