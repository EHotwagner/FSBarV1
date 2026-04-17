namespace FSBar.Hub

open FSBar.Client

/// Owns the hub's at-most-one live BAR session: lobby → engine launch
/// → BarClient lifecycle → frame stream → clean teardown. Publishes
/// every lifecycle transition onto a supplied `IHubEventSink` so the
/// status bar, diagnostics pane, and gRPC `GetSessionStatus` response
/// assembler all see a consistent view.
module SessionManager =

    /// Descriptor for the one currently-running session. The hub
    /// carries this opaquely — callers pull lifecycle info off the
    /// public `SessionManager` surface.
    type RunningSession = {
        Id: System.Guid
        Config: LobbyConfig.LobbyConfig
        EngineConfig: EngineConfig
        BarClient: BarClient
        GraphicalEngineProcess: System.Diagnostics.Process option
        StartedAt: System.DateTimeOffset
    }

    /// Full lifecycle state. `HubEvents.StateChanged` gets fired with a
    /// lightweight tag on every transition; consumers that need this
    /// richer shape (e.g. the status bar rendering the failure reason)
    /// read `SessionManager.State` directly.
    type SessionState =
        | Idle
        | Starting of LobbyConfig.LobbyConfig
        | Running of RunningSession
        | Ending of RunningSession
        | Failed of lobby: LobbyConfig.LobbyConfig * reason: string * infologExcerpt: string option

    /// The hub-wide session owner. Disposable — `Dispose` tears down
    /// any active session and unregisters the pump.
    [<Sealed>]
    type SessionManager =
        /// Current lifecycle state. Thread-safe snapshot.
        member State: SessionState

        /// Observable of `GameFrame` values sourced from the underlying
        /// `BarClient`. Subscribers only receive frames while a session
        /// is `Running`; transitions to `Idle` / `Failed` complete
        /// per-subscription streams as the underlying `BarClient`
        /// `Frames` observable completes.
        member Frames: System.IObservable<GameFrame>

        /// Launch a new session. Returns `Ok ()` after the state has
        /// transitioned to `Starting`; the actual connection work
        /// happens on a background thread and publishes a later
        /// `StateChanged Running` or `StateChanged Failed`. Returns
        /// `Error msg` if the lobby does not validate against the
        /// current `BarInstall`, or if a session is already running
        /// (caller must `End` first).
        member Launch: config: LobbyConfig.LobbyConfig -> Result<unit, string>

        /// Request an engine speed change. Phase-3 scope: emits
        /// `HubEvents.EngineSpeedChanged` and updates the hub-side
        /// target speed for display. Actual engine wire-up lands with
        /// the AI-command plumbing in Phase 9 / US7.
        member SetSpeed: speed: float32 -> unit

        /// Request pause / resume. Same Phase-3 scope as `SetSpeed`:
        /// emits `HubEvents.SessionPaused`, actual wire-up deferred.
        member SetPaused: paused: bool -> unit

        /// Tear down the active session. Safe to call from any state.
        /// Does not exit the hub process or close gRPC clients.
        member End: unit -> unit

        interface System.IDisposable

    /// Construct a `SessionManager` bound to a specific `BarInstall`
    /// and event sink. The caller owns disposal.
    val create:
        install: BarInstall.BarInstall ->
        events: HubEvents.IHubEventSink ->
            SessionManager
