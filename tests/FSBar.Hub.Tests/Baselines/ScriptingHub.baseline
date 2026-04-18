namespace FSBar.Hub

open FSBar.Client

/// The hub's gRPC scripting service — fans `SessionManager.Frames` out
/// to every connected client on an independent bounded channel,
/// isolates slow / dead clients, and exposes unary RPCs for command
/// submission, status queries, and UnitDef lookups (feature
/// 035-central-gui-hub US7).
///
/// Fan-out design (research.md R3):
///   * One internal subscriber to `SessionManager.Frames`.
///   * Per-client `System.Threading.Channels.BoundedChannel<GameFrameMessage>`
///     of configurable capacity (default 16) with
///     `BoundedChannelFullMode.DropOldest`. Slow / stuck clients cannot
///     back-pressure the producer or other consumers.
///   * Drop counter increments whenever the per-client channel is at
///     capacity at enqueue time. When cumulative drops reach
///     `MaxCumulativeDrops` (default 32) the client is detached with
///     `HubEvents.ScriptingClientDetached(OverflowDropLimit)`.
module ScriptingHub =

    /// Tunables surfaced on the public constructor so tests can pin
    /// capacity + drop threshold.
    type ScriptingHubOptions = {
        /// Per-client frame-buffer capacity. Drop-oldest when full.
        FrameBufferCapacity: int
        /// Cumulative drop count that trips the OverflowDropLimit
        /// detach. At 60 fps, 16 buffered + 32 drops ≈ 800 ms of
        /// tolerated backlog before a client is dropped.
        MaxCumulativeDrops: int
    }

    /// Default tunables: `FrameBufferCapacity = 16`,
    /// `MaxCumulativeDrops = 32`.
    val defaults: ScriptingHubOptions

    /// Rolled-up roster projection of one connected client for the
    /// gRPC `GetSessionStatus` response and the Settings tab's client
    /// list.
    type ConnectedClientInfo = {
        ClientId: System.Guid
        ClientLabel: string
        RemoteEndpoint: string
        AttachedAtUnixMs: int64
        CumulativeDroppedFrames: int
    }

    /// The hub-side gRPC service implementation. Constructed once per
    /// hub process; registered into the Kestrel host via
    /// `app.MapGrpcService<ScriptingService>()` in `Program.fs`.
    ///
    /// Constructor dependencies:
    ///   * `sessions` — the hub's one-and-only `SessionManager`.
    ///   * `events` — hub event sink for `ScriptingClient{Connected,Detached}`
    ///      and `DiagnosticsLine` emissions.
    ///   * `unitDefs` — thunk that returns the *currently* loaded
    ///      `UnitDefCache`. Caller threads a closure that reads from
    ///      the active `SessionManager.State.BarClient.GameState.UnitDefs`
    ///      when running, else a cached / empty fallback.
    ///   * `install` / `bundled` / `port` — filled into
    ///     `GetSessionStatusResponse`.
    ///   * `state` — authoritative hub-UI state store (feature 040).
    ///     Lobby-related RPCs (`ConfigureLobby`, `LaunchSession`) read
    ///     and write through this store so the local GUI and gRPC
    ///     clients never drift.
    ///   * `opts` — fan-out tunables.
    [<Sealed>]
    type ScriptingService =
        inherit Fsbar.Hub.Scripting.V1.ScriptingService.ServiceBase
        new:
            sessions: SessionManager.SessionManager *
            events: HubEvents.IHubEventSink *
            busEvents: System.IObservable<HubEvents.HubEvent> *
            unitDefs: (unit -> UnitDefCache) *
            install: BarInstall.BarInstall *
            bundled: BundledProxy.BundledProxyInfo *
            port: int *
            state: HubStateStore.T *
            renderer: HeadlessRenderer.T *
            overlays: OverlayLayerStore.T *
            opts: ScriptingHubOptions ->
                ScriptingService

        /// Snapshot of the currently-connected clients. Used by the
        /// gRPC tab for the roster display and by `GetSessionStatus`
        /// to populate `clients`.
        member Clients: ConnectedClientInfo list

        /// Count of clients that have been detached with
        /// `OverflowDropLimit` since the process started. Exposed for
        /// the Settings tab's diagnostics counter.
        member OverflowDetachCount: int

        // --- Internal helpers for FSBar.Hub.Tests ---
        //
        // Drive the fan-out pump end-to-end in-process without a
        // live gRPC host or BarClient. Gated via
        // `InternalsVisibleTo("FSBar.Hub.Tests")` (AssemblyInfo.fs).

        member internal PushTestFrame:
            frameNumber: int * teamId: int -> unit
        member internal AttachTestClient:
            label: string ->
                System.Guid * System.Threading.Channels.ChannelReader<Fsbar.Hub.Scripting.V1.GameFrameMessage>
        member internal DetachTestClient:
            id: System.Guid -> unit
        member internal DropCountFor:
            id: System.Guid -> int

        interface System.IDisposable
