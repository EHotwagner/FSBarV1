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
        /// MapGrid loaded from the engine when the session reaches
        /// Running. `None` until the warm-up load completes; consumers
        /// (e.g. `ViewerTab`) should fall back to a synthetic grid
        /// while this is `None`.
        MapGrid: MapGrid option
        /// Static metal-spot positions (x, y, z, density) sampled from
        /// the engine at session start. Empty when the engine-side
        /// callback failed or returned zero spots.
        MetalSpots: (float32 * float32 * float32 * float32) array
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
        ///
        /// `startPaused` — feature 038 FR-003/004: when `true`, the
        /// hub issues a single `/pause` chat command via
        /// `BarClient.SendCommands` on the first `Running` transition,
        /// before the engine produces a non-zero-time frame. The caller
        /// sources this from `HubSettings.StartPausedDefault`.
        member Launch:
            config: LobbyConfig.LobbyConfig *
            startPaused: bool ->
                Result<unit, string>

        /// Request an engine speed change. Phase-3 scope: emits
        /// `HubEvents.EngineSpeedChanged` and updates the hub-side
        /// target speed for display. Actual engine wire-up lands with
        /// the AI-command plumbing in Phase 9 / US7.
        member SetSpeed: speed: float32 -> unit

        /// Set the engine speed multiplier via the admin channel
        /// (feature 039). Values that are non-positive, NaN, or
        /// infinite are rejected locally without touching the socket.
        /// Engine-range rejection arrives as a
        /// `HubEvent.DiagnosticsLine Warning`.
        member SetEngineSpeed: speed: float32 -> AdminChannelHost.SubmitOutcome

        /// Force-end the active match (feature 039 US3). Sends
        /// `KILLSERVER` through the admin channel and arms a wall-clock
        /// watchdog that escalates to SIGTERM at 5 s and SIGKILL at 8 s
        /// if the engine hasn't exited (research.md §R8).
        member ForceEnd: unit -> AdminChannelHost.SubmitOutcome

        /// Broadcast an admin message into the engine's in-game chat log
        /// (feature 039 US4). Empty / whitespace-only strings reject
        /// locally without touching the socket.
        member SendAdminMessage: text: string -> AdminChannelHost.SubmitOutcome

        /// Pause the active match via the admin channel (feature 039).
        /// Returns `Rejected` when no session is active or when the
        /// admin channel is not `Attached`.
        member Pause: unit -> AdminChannelHost.SubmitOutcome

        /// Resume the active match via the admin channel (feature 039).
        /// Returns `Rejected` when no session is active or when the
        /// admin channel is not `Attached`.
        member Resume: unit -> AdminChannelHost.SubmitOutcome

        /// True when the hub has most recently issued a pause to the
        /// engine. Not a live mirror of the engine state — BAR's
        /// native UI can flip the engine pause out-of-band without
        /// the hub noticing (research.md §R2 pick A / §R4).
        member IsPaused: bool

        /// Flip pause/unpause in a single call. Safe from any state;
        /// emits `SessionPaused` exactly once per toggle. Backing the
        /// Viewer-tab pause button (FR-004b). Dispatches to `Pause` or
        /// `Resume` based on the current `IsPaused` reading.
        member TogglePause: unit -> unit

        /// Current admin-channel status, or `None` when no session is
        /// active (data-model.md §8 invariant I3). Feature 039.
        member AdminStatus: HubEvents.AdminChannelStatus option

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
