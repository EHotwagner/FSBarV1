namespace FSBar.Hub

open FSBar.Client

/// Owns the hub's at-most-one live BAR session.
///
/// Feature 038 additions:
/// - `Launch` gains a `startPaused: bool` argument that wires the
///   start-paused-on-first-frame behaviour.
/// - `IsPaused` exposes the hub-known engine pause state.
/// - `TogglePause` flips pause/unpause via the internal `BarClient`.
/// - `SetPaused` becomes a real engine-wired call (was a stub).
module SessionManager =

    type RunningSession = {
        Id: System.Guid
        Config: LobbyConfig.LobbyConfig
        EngineConfig: EngineConfig
        BarClient: BarClient
        GraphicalEngineProcess: System.Diagnostics.Process option
        StartedAt: System.DateTimeOffset
        MapGrid: MapGrid option
        MetalSpots: (float32 * float32 * float32 * float32) array
    }

    type SessionState =
        | Idle
        | Starting of LobbyConfig.LobbyConfig
        | Running of RunningSession
        | Ending of RunningSession
        | Failed of lobby: LobbyConfig.LobbyConfig * reason: string * infologExcerpt: string option

    [<Sealed>]
    type SessionManager =
        member State: SessionState

        member Frames: System.IObservable<GameFrame>

        /// Launch a new session.
        ///
        /// `startPaused` — when `true`, the hub will issue a single
        /// `/pause` chat command via `BarClient.SendCommands` on the
        /// first `Running` transition, before the engine produces a
        /// non-zero-time frame. The caller is expected to source this
        /// from `HubSettings.StartPausedDefault`.
        ///
        /// Returns `Ok ()` after the state has transitioned to
        /// `Starting`; lifecycle pump publishes the subsequent
        /// `Running` / `Failed` events. Returns `Error msg` if the
        /// lobby does not validate against the current `BarInstall`,
        /// if a session is already active, or — when the lobby
        /// requests the graphical engine — if the graphical binary is
        /// unavailable (FR-008).
        member Launch:
            config: LobbyConfig.LobbyConfig *
            startPaused: bool ->
                Result<unit, string>

        member SetSpeed: speed: float32 -> unit

        /// Ensure pause state matches the argument. When
        /// `IsPaused <> paused`, issues a `/pause` chat command via
        /// the internal `BarClient` and publishes `SessionPaused`.
        /// No-op when the session is not `Running`.
        member SetPaused: paused: bool -> unit

        /// True when the hub has most recently issued a pause to the
        /// engine. Not a live mirror of the engine state — BAR's
        /// native UI can flip the engine pause out-of-band without
        /// the hub noticing.
        member IsPaused: bool

        /// Flip pause/unpause in a single call. Safe from any state;
        /// emits `SessionPaused` exactly once per toggle. Backing the
        /// Viewer-tab pause button (FR-004b).
        member TogglePause: unit -> unit

        member End: unit -> unit

        interface System.IDisposable

    val create:
        install: BarInstall.BarInstall ->
        events: HubEvents.IHubEventSink ->
            SessionManager
