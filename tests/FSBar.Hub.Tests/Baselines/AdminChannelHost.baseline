namespace FSBar.Hub

open FSBar.Client

/// <summary>
/// Hub-side orchestrator for the autohost <see cref="T:FSBar.Client.AdminChannel.AdminChannel"/>.
/// Serializes outbound admin commands onto the channel, de-duplicates rapid-fire
/// same-kind submits within a short quiet window (research.md §R5), maintains
/// hub-visible status, and publishes every transition through
/// <see cref="T:FSBar.Hub.HubEvents.IHubEventSink"/>. Callers submit intents;
/// the host decides when/whether they reach the socket (feature 039).
/// </summary>
module AdminChannelHost =

    /// <summary>Re-export of <see cref="T:FSBar.Hub.HubEvents.AdminChannelStatus"/>.
    /// Mirrors the spec's "Admin Channel Status" entity (data-model.md §2).</summary>
    type AdminChannelStatus = HubEvents.AdminChannelStatus

    /// <summary>The outcome of one <see cref="M:FSBar.Hub.AdminChannelHost.AdminChannelHost.Submit"/>
    /// call. Commands coalesce per kind, so a newer command may supersede an
    /// older pending one — the caller sees <c>Coalesced</c> in that case.</summary>
    type SubmitOutcome =
        /// <summary>The datagram was written to the autohost socket.</summary>
        | Sent
        /// <summary>A newer same-kind command superseded this one before it
        /// was sent. <c>droppedCount</c> reports how many requests (including
        /// this one) were coalesced away. The effective end state matches the
        /// last click.</summary>
        | Coalesced of droppedCount: int
        /// <summary>The request was rejected without touching the socket.
        /// <c>reason</c> explains why — typically the channel is not Attached,
        /// or the payload failed local validation.</summary>
        | Rejected of reason: string

    /// <summary>Serializing owner for one <see cref="T:FSBar.Client.AdminChannel.AdminChannel"/>.
    /// Dispose to tear down the coalescing agent and release the channel.</summary>
    [<Sealed>]
    type AdminChannelHost =
        /// <summary>Current hub-visible status.</summary>
        member Status: AdminChannelStatus
        /// <summary>Convenience observable of status transitions. Also
        /// published as <c>HubEvent.AdminChannelStatusChanged</c>.</summary>
        member StatusChanges: System.IObservable<AdminChannelStatus>
        /// <summary>Submit one admin command. Idempotent per kind within the
        /// 100 ms coalescing window (research.md §R5). Returns immediately.</summary>
        member Submit: cmd: AdminChannel.AdminCommandOut -> SubmitOutcome
        /// <summary>Hub's last-issued pause state — mirrors spec FR-010.</summary>
        member IsPaused: bool
        /// <summary>Hub's last-issued game speed; defaults to <c>1.0f</c>.</summary>
        member CurrentSpeed: float32
        interface System.IDisposable

    /// <summary>Attach to an already-bound <see cref="T:FSBar.Client.AdminChannel.AdminChannel"/>
    /// and a hub event sink. Publishes <c>HubEvent.AdminChannelStatusChanged</c>
    /// on every status transition.</summary>
    val attach:
        channel: AdminChannel.AdminChannel *
        events: HubEvents.IHubEventSink ->
            AdminChannelHost

    /// <summary>Construct an <see cref="T:FSBar.Hub.AdminChannelHost.AdminChannelHost"/>
    /// in the <c>Unavailable</c> state. Used when <see cref="M:FSBar.Client.AdminChannel.bind"/>
    /// fails at launch so the hub still has a host object whose <c>Submit</c>
    /// calls reject with the bind reason (FR-009).</summary>
    val unavailable:
        reason: string *
        events: HubEvents.IHubEventSink ->
            AdminChannelHost
