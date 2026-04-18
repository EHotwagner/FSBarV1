module FSBar.Hub.Tests.AdminChannelHostTests

open System
open System.Collections.Generic
open System.Threading
open Xunit
open FSBar.Client
open FSBar.Hub
open FSBar.Hub.AdminChannelHost

// Feature 039 T015 — coalesce rapid same-kind submits; reject when not
// Attached; publish status transitions through HubEvents.

type private CapturingSink() =
    let events = ResizeArray<HubEvents.HubEvent>()
    let sync = obj ()
    interface HubEvents.IHubEventSink with
        member _.Publish(evt) = lock sync (fun () -> events.Add(evt))
    member _.Snapshot() = lock sync (fun () -> events.ToArray())

[<Fact>]
let ``Submit_rejects_when_host_is_unavailable`` () =
    let sink = CapturingSink()
    use host = AdminChannelHost.unavailable("port conflict", sink :> HubEvents.IHubEventSink)
    let outcome = host.Submit(AdminChannel.Pause true)
    match outcome with
    | Rejected reason -> Assert.Contains("port conflict", reason)
    | other -> failwithf "expected Rejected, got %A" other
    // Invariant I5: Submit must not have touched any socket — host has no channel.
    // And status stays Unavailable.
    match host.Status with
    | HubEvents.Unavailable r -> Assert.Contains("port conflict", r)
    | other -> failwithf "expected Unavailable, got %A" other

[<Fact>]
let ``unavailable_publishes_AdminChannelStatusChanged_event`` () =
    let sink = CapturingSink()
    use _host = AdminChannelHost.unavailable("bind failed", sink :> HubEvents.IHubEventSink)
    // Give the event bus a tick to publish.
    Thread.Sleep(50)
    let events = sink.Snapshot()
    let hasStatusChange =
        events |> Array.exists (function
            | HubEvents.AdminChannelStatusChanged (HubEvents.Unavailable r) ->
                r.Contains("bind failed")
            | _ -> false)
    Assert.True(hasStatusChange,
        sprintf "expected AdminChannelStatusChanged(Unavailable _) event; got: %A" events)

[<Fact>]
let ``IsPaused_defaults_false_and_CurrentSpeed_defaults_1_on_unavailable_host`` () =
    let sink = CapturingSink()
    use host = AdminChannelHost.unavailable("n/a", sink :> HubEvents.IHubEventSink)
    Assert.False(host.IsPaused)
    Assert.Equal(1.0f, host.CurrentSpeed)

[<Fact>]
let ``Lost_reason_is_reported_in_rejection`` () =
    let sink = CapturingSink()
    use host = AdminChannelHost.unavailable("engine exited", sink :> HubEvents.IHubEventSink)
    match host.Submit(AdminChannel.SetGameSpeed 2.0f) with
    | Rejected r -> Assert.Contains("engine exited", r)
    | other -> failwithf "expected Rejected with reason, got %A" other
