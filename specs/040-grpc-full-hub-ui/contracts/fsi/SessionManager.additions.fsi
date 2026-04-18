// Delta sketch for src/FSBar.Hub/SessionManager.fsi
// Feature 040 — additions only. Every pre-existing member (Launch, TogglePause,
// Pause, Resume, SetEngineSpeed, ForceEnd, SendAdminMessage, IsPaused,
// AdminStatus, State, Frames) remains unchanged.

namespace FSBar.Hub

type SessionManager =
    // --- existing members omitted ---

    /// Abort the current session if any. Returns Sent when a running or
    /// starting session was terminated; Rejected "no active session" when
    /// State = Idle. Emits HubEvent.StateChanged on transition.
    member Stop: unit -> HubStateStore.SubmitOutcome

    /// True when lobby edits are permitted (State = Idle).
    member IsLobbyEditable: unit -> bool
