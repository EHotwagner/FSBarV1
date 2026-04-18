namespace FSBar.Hub

/// Central event bus for hub diagnostics and lifecycle signals.
///
/// The hub publishes discrete events (session-state transitions, engine-speed
/// adjustments, proxy-install step outcomes, scripting-client attach/detach,
/// plain diagnostic lines). Consumers — the status bar, settings diagnostics
/// pane, gRPC `GetSessionStatus` response assembly — observe the `IObservable`
/// projection.
///
/// Implementation is a single `System.Threading.Channels` pump with a list of
/// subscribed `IObserver`s: the publish side enqueues without blocking, a
/// background task drains the channel and fans out, and a slow observer
/// cannot wedge the producer. No `System.Reactive` dependency.
module HubEvents =

    /// Diagnostic-line severity tag surfaced by `DiagnosticsLine`.
    type Severity =
        | Info
        | Warning
        | Error

    /// Why a scripting client's `StreamGameFrames` RPC was terminated by the
    /// hub (as opposed to a clean client-initiated disconnect).
    type DetachReason =
        /// The client's underlying gRPC channel closed — either a clean
        /// cancellation or a network-level disconnect.
        | ClientDisconnected
        /// The client's per-client frame buffer exceeded the cumulative
        /// drop threshold; the hub terminated the stream to protect other
        /// consumers.
        | OverflowDropLimit
        /// The hub itself is shutting down.
        | ServerShutdown

    /// Discrete step of the proxy-installation flow, reported via
    /// `ProxyInstallProgress`.
    type ProxyInstallStep =
        /// Copy `libSkirmishAI.so` + `AIInfo.lua` + `AIOptions.lua` into
        /// `<engineDir>/AI/Skirmish/HighBarV2/<version>/`.
        | CopyAiFiles
        /// Ensure `<dataDir>/devmode.txt` exists.
        | TouchDevMode
        /// Edit `<dataDir>/LuaMenu/Config/IGL_data.lua` to set
        /// `simpleAiList = false` (targeted per-key rewrite).
        | ToggleSimpleAiList

    /// Outcome of a single `ProxyInstallStep`.
    type StepOutcome =
        /// Step was a no-op — the precondition was already satisfied.
        | Skipped
        /// Step performed a write.
        | Performed
        /// Step failed; the payload carries the operator-visible reason.
        | StepFailed of reason: string

    /// Hub-visible admin-channel status (feature 039). Mirrors
    /// `FSBar.Hub.AdminChannelHost.AdminChannelStatus` — kept here so
    /// `HubEvent.AdminChannelStatusChanged` can reference it without
    /// pulling `AdminChannelHost` ahead of `HubEvents` in the compile
    /// order.
    type AdminChannelStatus =
        /// Channel attached; admin commands are accepted.
        | Attached
        /// Channel could not be brought up at launch. `reason` populated.
        | Unavailable of reason: string
        /// Channel was `Attached` but has since failed. `reason` populated.
        | Lost of reason: string

    /// Coarse session-state tag surfaced by `StateChanged`.
    ///
    /// Phase 2 scope: `SessionManager` does not yet exist, so this enum
    /// captures only the five lifecycle labels the status bar needs to
    /// render. Phase 3 enriches the situation by publishing richer
    /// context via additional `DiagnosticsLine` events and exposing the
    /// full state DU through `SessionManager.State`.
    type SessionStateTag =
        | Idle
        | Starting
        | Running
        | Ending
        | Failed

    /// Hub-wide event payload.
    type HubEvent =
        /// The session lifecycle transitioned to a new tag.
        | StateChanged of tag: SessionStateTag
        /// The engine time multiplier was changed.
        | EngineSpeedChanged of speed: float32
        /// The engine's paused-ness was toggled.
        | SessionPaused of paused: bool
        /// A free-form diagnostic message was emitted.
        | DiagnosticsLine of severity: Severity * message: string
        /// A scripting-client successfully attached to `StreamGameFrames`.
        | ScriptingClientConnected of clientId: System.Guid * remote: string
        /// A scripting-client was detached; `reason` tells operators whether
        /// this was the client's doing or a hub-initiated teardown.
        | ScriptingClientDetached of clientId: System.Guid * reason: DetachReason
        /// One step of the proxy-install flow reported its outcome.
        | ProxyInstallProgress of step: ProxyInstallStep * outcome: StepOutcome
        /// The admin channel's hub-level status changed (feature 039).
        | AdminChannelStatusChanged of status: AdminChannelStatus

    /// Inbound-only handle for modules that publish events. Taking this
    /// instead of a full `HubEventBus` reference in constructors makes the
    /// data flow unambiguous — modules that only publish cannot
    /// accidentally subscribe.
    type IHubEventSink =
        abstract Publish: HubEvent -> unit

    /// The bus instance. Owned by `Program.fs`; constructed once per hub
    /// process. Disposing stops the pump task and completes all subscribed
    /// observers.
    [<Sealed>]
    type HubEventBus =
        /// Inbound sink surface; hand to modules that only need to publish.
        member Sink: IHubEventSink
        /// Outbound observable surface; subscribe from GUI chrome,
        /// diagnostics panels, and the gRPC status-query assembler.
        member Events: System.IObservable<HubEvent>
        interface System.IDisposable

    /// Construct a fresh bus. The caller owns disposal.
    val create: unit -> HubEventBus
