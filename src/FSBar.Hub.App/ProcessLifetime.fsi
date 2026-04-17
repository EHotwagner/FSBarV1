namespace FSBar.Hub.App

open FSBar.Hub

/// Hub-side process-lifetime helper (feature 035-central-gui-hub T017).
///
/// Two responsibilities:
///   1. Track child-engine PIDs the hub has launched, and on hub-
///      initiated shutdown (SIGTERM, SIGINT, window-close) send
///      SIGTERM to each and SIGKILL after a short grace period.
///   2. Expose a pure predicate, `requestClose`, that the window-
///      close handler consults before tearing the session down
///      (spec.md Edge Case: user tries to close the hub mid-session).
///
/// Coverage gap documented: this module only handles *clean* exit
/// paths (signal or window-close). A hub process crash (SIGSEGV,
/// OOM) leaves the child engines running. Closing that gap requires
/// launching engines through `scripts/hub-spawn-engine.sh` with
/// `prctl(PR_SET_PDEATHSIG, SIGTERM)`, which in turn requires
/// `FSBar.Client.EngineLauncher` to honour a wrapper — out of
/// scope for this feature; tracked as a follow-up.
module ProcessLifetime =

    /// Outcome of a window-close / exit-request check.
    [<RequireQualifiedAccess>]
    type CloseDecision =
        /// Safe to proceed with teardown immediately.
        | AllowClose
        /// Caller MUST surface the message to the user and only
        /// proceed on confirmation.
        | RequireConfirm of message: string

    /// Pure function: decides whether a close request can proceed
    /// immediately or needs user confirmation.
    ///
    /// * `Idle` / `Failed` / `Ending` → `AllowClose`
    /// * `Running` / `Starting` → `RequireConfirm` so the GUI can
    ///   prompt "A session is running. Close anyway?".
    val requestClose:
        sessionState: SessionManager.SessionState ->
            CloseDecision

    /// Registers a child-engine PID. Safe to call multiple times
    /// with the same PID (idempotent).
    val register: pid: int -> unit

    /// Un-registers a PID the hub already stopped cleanly (so the
    /// shutdown hook doesn't double-kill on its way out).
    val unregister: pid: int -> unit

    /// Snapshot of currently-registered PIDs.
    val tracked: unit -> int list

    /// Installs POSIX signal handlers (SIGTERM + SIGINT) so that
    /// user-initiated process termination triggers the cleanup
    /// path. Idempotent — calling twice is a no-op.
    ///
    /// On handler fire:
    ///   1. Invoke the supplied `onShutdown` callback (hub passes a
    ///      closure that calls `SessionManager.End` + disposes the
    ///      viewer).
    ///   2. Wait up to `gracePeriodMs` ms (default 3000) for each
    ///      tracked PID to exit.
    ///   3. SIGKILL survivors.
    ///   4. Exit the hub process.
    val installSignalHandlers:
        onShutdown: (unit -> unit) ->
        gracePeriodMs: int option ->
            unit

    /// Run the post-shutdown cleanup path explicitly (used by the
    /// normal window-close flow, not just the signal handler).
    /// Sends SIGTERM to every tracked PID, waits up to
    /// `gracePeriodMs`, then SIGKILLs survivors.
    val sweepChildEngines:
        gracePeriodMs: int option ->
            unit
