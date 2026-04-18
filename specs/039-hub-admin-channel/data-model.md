# Phase 1 Data Model — Hub admin/host channel

**Feature**: 039-hub-admin-channel · **Date**: 2026-04-17

This document fixes the types the new modules expose and how they
flow between layers. Every public type listed is backed by an `.fsi`
signature file per Constitution §II.

## 1. `FSBar.Client.AdminChannel`

Low-level autohost UDP client. Owns one `UdpClient`, encode/decode,
and a receive pump. Public surface:

```fsharp
/// Outbound hub → engine admin command.
type AdminCommandOut =
    | Pause of bool                    // action 5
    | SetGameSpeed of float32          // action 4
    | SayMessage of string             // action 8
    | KillServer                       // action 0

/// Inbound engine → hub admin event. Unknown codes are preserved so
/// future engine revisions don't silently vanish from diagnostics.
type AdminEventIn =
    | ServerStarted                    // action 0
    | ServerQuit of reason: string     // action 1
    | ServerStartPlaying               // action 2
    | ServerGameOver                   // action 3
    | PlayerChat of playerId: int * text: string  // action 7
    | GameWarning of text: string      // action 11
    | Unknown of code: byte * payload: byte[]

/// Socket-level status — the raw "did the UDP bind + any receives
/// happen" view. The hub-level `AdminChannelStatus` (module
/// FSBar.Hub.AdminChannelHost) layers richer semantics on top.
type ChannelState =
    | NotBound
    | Bound of port: int
    | ReceivingFrom of engineEndpoint: System.Net.IPEndPoint
    | Closed of reason: string

[<Sealed>]
type AdminChannel =
    /// Current raw state.
    member State: ChannelState
    /// Allocated local port; valid once `State >= Bound`.
    member LocalPort: int option
    /// IObservable of inbound events; completes when channel closes.
    member Events: System.IObservable<AdminEventIn>
    /// Send one outbound command. Returns Ok when the UDP datagram
    /// has been written; Error if the socket is closed or in error.
    member Send: cmd: AdminCommandOut -> Result<unit, string>
    interface System.IDisposable

/// Binds a fresh UDP socket on IPAddress.Loopback:0 and returns it
/// in `Bound port` state. The caller writes `port` into
/// springsettings.cfg and launches the engine; once the first
/// datagram arrives the channel auto-transitions to
/// `ReceivingFrom`.
val bind: unit -> Result<AdminChannel, string>
```

No persistence; nothing here is `IObservable`-coalesced. The hub
layer handles coalescing and status upgrades.

## 2. `FSBar.Hub.AdminChannelHost`

Hub-level orchestrator. Owns one `AdminChannel`, serializes outbound
commands, maintains hub-level status, publishes to `HubEvents`.

```fsharp
/// Hub-visible status. Maps the spec's "Admin Channel Status" entity.
type AdminChannelStatus =
    /// Channel attached and the engine reports SERVER_STARTED.
    | Attached
    /// Channel could not be brought up at launch; admin controls
    /// must disable with this reason.
    | Unavailable of reason: string
    /// Channel previously Attached but has since failed.
    | Lost of reason: string

/// The outcome of one `submit` call. Commands coalesce per kind, so
/// a newer command may supersede an older pending one — the caller
/// sees `Coalesced` in that case.
type SubmitOutcome =
    | Sent
    | Coalesced of droppedCount: int
    | Rejected of reason: string

[<Sealed>]
type AdminChannelHost =
    /// Current status.
    member Status: AdminChannelStatus
    /// Convenience observable of status transitions.
    member StatusChanges: System.IObservable<AdminChannelStatus>
    /// Submit one admin command; idempotent per kind within the
    /// coalescing window (research.md R5). Returns immediately.
    member Submit: cmd: AdminCommandOut -> SubmitOutcome
    /// Hub's last-issued pause state — mirrors the spec's FR-010.
    member IsPaused: bool
    /// Hub's last-issued game speed; defaults to 1.0f.
    member CurrentSpeed: float32
    interface System.IDisposable

/// Attach to an already-bound `AdminChannel` and an `IHubEventSink`.
/// Publishes `HubEvent.AdminChannelStatusChanged` on every status
/// transition.
val attach:
    channel: FSBar.Client.AdminChannel *
    events: HubEvents.IHubEventSink ->
        AdminChannelHost
```

## 3. `FSBar.Hub.HubEvents` additions

One new variant on `HubEvent`:

```fsharp
type HubEvent =
    | ...existing...
    /// The admin channel's hub-level status changed.
    | AdminChannelStatusChanged of status: AdminChannelStatus
```

Surface-area baselines under
`tests/FSBar.Hub.Tests/Baselines/HubEvents.baseline` must be updated
in tandem.

## 4. `FSBar.Hub.SessionManager` changes

Added members:

```fsharp
type SessionManager =
    // ...existing members...

    /// Pause the active match via the admin channel.
    member Pause: unit -> SubmitOutcome

    /// Resume the active match via the admin channel.
    member Resume: unit -> SubmitOutcome

    /// Set the engine speed multiplier. Values ≤ 0 are rejected
    /// locally; engine-range rejection arrives as a
    /// HubEvent.DiagnosticsLine Warning.
    member SetEngineSpeed: speed: float32 -> SubmitOutcome

    /// Force-end the active match (KILLSERVER + escalation).
    /// Completes synchronously once the command has been issued;
    /// session transitions to Ending/Idle asynchronously as the
    /// process exits.
    member ForceEnd: unit -> SubmitOutcome

    /// Broadcast an admin message into the engine's in-game chat
    /// log. Empty strings are rejected.
    member SendAdminMessage: text: string -> SubmitOutcome

    /// Current admin-channel status, or None when no session is
    /// running.
    member AdminStatus: AdminChannelStatus option
```

**Replaces** the vestigial `SetPaused(paused: bool)` +
`TogglePause()` chat-based implementation. `TogglePause()` survives
as a convenience member that picks `Pause` or `Resume` from
`IsPaused`.

## 5. `FSBar.Client.EngineConfig` changes

One optional field:

```fsharp
type EngineConfig = {
    // ...existing fields...

    /// Loopback UDP port the hub pre-bound for the engine's autohost
    /// interface. When `Some p`, `ScriptGenerator` emits
    /// `AutohostIP = 127.0.0.1` and `AutohostPort = p` into the
    /// per-session springsettings.cfg. When `None`, the feature-039
    /// admin channel is not available for this session (legacy
    /// launch path).
    AutohostPort: int option
}
```

Surface-area baseline `EngineConfig.baseline` is bumped.
`EngineConfig.create` gains the new field as optional with default
`None`; existing callers continue to compile.

## 6. `FSBar.Hub.App` admin toolbar model

Pure local state in `ViewerTab` — **explicitly excluded from the public
surface area** (neither `AdminToolbarAction` nor the `adminToolbarRect`
helper appear in `ViewerTab.fsi`). Listed here only so implementation
tasks have a concrete type shape to target.

- `adminToolbarRect: contentX y w h → float32 * float32 * float32 * float32`
  — mirrors `pauseButtonRect`.
- `AdminToolbarAction` discriminated union — emitted by the hit-test
  helper and routed back to `Program.fs`:

```fsharp
type AdminToolbarAction =
    | PauseOrResume
    | ForceEnd
    | SelectSpeedPreset of multiplier: float32
    | SubmitCustomSpeed of multiplier: float32
    | SubmitAdminMessage of text: string
```

`Program.fs` maps each action onto the corresponding
`SessionManager` member.

## 7. Scripting proto — admin slice

See `contracts/scripting-admin.proto` for the authoritative delta.
The Hub-facing F# wrapper in
`src/FSBar.Hub/ScriptingHub.fs` maps each new RPC through
`SessionManager` (identical to how `SendCommand` today maps through
`BarClient.SendCommands`).

## 8. Invariants

| # | Invariant | Enforced where |
|---|-----------|----------------|
| I1 | `AdminChannelHost.Status = Attached` ⇒ every `AdminChannel.Send` returns `Ok` within 5 ms. | Maintained by the status pump; violation triggers `Lost`. |
| I2 | `Submit(Pause(x))` + `Submit(Pause(y))` within < 100 ms ⇒ only one datagram for `y` reaches the engine. | `AdminChannelHost` coalescing. |
| I3 | Session `Idle` ⇒ `AdminStatus = None`. | `SessionManager` resets on `Ending → Idle`. |
| I4 | `startPaused = true` ⇒ the first `PAUSE(true)` is sent after `ServerStartPlaying`, before the first non-zero game-time frame. | `SessionManager.Launch` wiring. |
| I5 | `AdminChannelStatus = Unavailable(r)` or `Lost(r)` ⇒ every `Submit` returns `Rejected(r)` without touching the socket. | `AdminChannelHost.Submit`. |
